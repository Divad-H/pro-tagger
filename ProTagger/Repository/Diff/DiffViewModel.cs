using LibGit2Sharp;
using ProTagger.Utilities;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProTagger.Repository.Diff
{
    using TSelectionInfo = Variant<string, Unexpected, List<TreeEntryChanges>, Commit>;
    public class CancellableChanges
    {
        public TreeEntryChanges TreeEntryChanges { get; }
        public CancellationTokenSource Cancellation { get; }
        public CancellableChanges(TreeEntryChanges treeEntryChanges)
            => (TreeEntryChanges, Cancellation) = (treeEntryChanges, new CancellationTokenSource());
    }

    public class CancellableChangesWithError
    {
        public CancellableChanges CancellableChanges { get; }
        public string Error { get; }
        public CancellableChangesWithError(CancellableChanges changes, string error)
            => (CancellableChanges, Error) = (changes, error);
    }

    class KeepSelectionComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? oldFile, object? newFile)
            => ((TreeEntryChanges?)oldFile)?.Path == ((TreeEntryChanges?)newFile)?.Path;

        public int GetHashCode(object file)
            => ((TreeEntryChanges)file).Path.GetHashCode();
    }

    public struct DiffViewModelInput
    {
        public DiffViewModelInput(IRepositoryWrapper repository,
            Variant<Commit, DiffTargets>? oldCommit,
            Variant<Commit, DiffTargets>? newCommit,
            CompareOptions compareOptions)
        {
            Repository = repository;
            OldCommit = oldCommit;
            NewCommit = newCommit;
            CompareOptions = compareOptions;
        }

        public readonly IRepositoryWrapper Repository;
        public readonly Variant<Commit, DiffTargets>? OldCommit;
        public readonly Variant<Commit, DiffTargets>? NewCommit;
        public readonly CompareOptions CompareOptions;
    }

    public class DiffViewModel : IDisposable
    {
        public const string NoCommitSelectedMessage = "No commit selected.";
        public const string NoFilesSelectedMessage = "No files selected.";
        
        public BatchList<Variant<PatchDiff, CancellableChangesWithError>> PatchDiff { get; }
            = new BatchList<Variant<PatchDiff, CancellableChangesWithError>>();

        public BatchList<Variant<PatchDiff, CancellableChangesWithError>> IndexPatchDiff { get; }
            = new BatchList<Variant<PatchDiff, CancellableChangesWithError>>();

        public BatchList<TreeEntryChanges> SelectedFiles { get; } = new BatchList<TreeEntryChanges>();
        public BatchList<TreeEntryChanges> SelectedIndexFiles { get; } = new BatchList<TreeEntryChanges>();

        public IEqualityComparer<object> KeepTreeDiffChangesSelectedRule { get; } = new KeepSelectionComparer();

        public ViewSubject<Variant<List<TreeEntryChanges>, Unexpected>> TreeDiff { get; }

        public ViewSubject<TSelectionInfo> SelectionInfo { get; }

        public ViewSubject<bool> IndexSelected { get; }
        public ICommand FocusChangedCommand { get; }

        public DiffViewModel(
            ISchedulers schedulers,
            IObservable<DiffViewModelInput> input)
        {
            TreeDiff = new ViewSubject<Variant<List<TreeEntryChanges>, Unexpected>>(new Unexpected(NoCommitSelectedMessage))
                .DisposeWith(_disposables);

            SelectionInfo = new ViewSubject<TSelectionInfo>(
                new TSelectionInfo(NoCommitSelectedMessage))
                .DisposeWith(_disposables);

            IndexSelected = new ViewSubject<bool>(false)
                .DisposeWith(_disposables);

            var focusChangedCommand = ReactiveCommand
                .Create<bool, bool>(indexSelected => indexSelected, schedulers.Dispatcher)
                .DisposeWith(_disposables);
            FocusChangedCommand = focusChangedCommand;

            focusChangedCommand
                .Merge(input
                    .Where(d => d.OldCommit is null)
                    .Where(d => d.NewCommit is null || !d.NewCommit.Is<DiffTargets>())
                    .Select(_ => false))
                .Subscribe(IndexSelected)
                .DisposeWith(_disposables);

            static string toString(Variant<Commit, DiffTargets> variant)
                => variant.Visit(commit => commit.Sha, diffTarget => diffTarget == DiffTargets.Index ? "index" : "working tree");

            static async Task<TSelectionInfo> castVariant(Task<Variant<List<TreeEntryChanges>, Unexpected>> task)
                => (await task).Visit(r => new TSelectionInfo(r), u => new TSelectionInfo(u));

            input.Select(d => Observable.FromAsync(ct => d.NewCommit is null ?
                        Task.FromResult(new TSelectionInfo(NoCommitSelectedMessage)) :
                        d.OldCommit is null ?
                            d.NewCommit.Visit(
                                commit => Task.FromResult(new TSelectionInfo(commit)),
                                _ => castVariant(Diff.TreeDiff.CreateDiff(d.Repository, ct, d.Repository.Head, null, DiffTargets.Index, d.CompareOptions))) :
                            Task.FromResult(new TSelectionInfo($"Displaying changes between {toString(d.OldCommit)} and {toString(d.NewCommit)}."))))
                .Switch()
                .Subscribe(SelectionInfo)
                .DisposeWith(_disposables);

            input.Select(o => Observable.FromAsync(ct =>
                    !(o.NewCommit is null) ?
                        Diff.TreeDiff.CreateDiff(o.Repository, ct, o.Repository.Head, o.OldCommit, o.NewCommit, o.CompareOptions) :
                        Task.FromResult(new Variant<List<TreeEntryChanges>, Unexpected>(new Unexpected(NoCommitSelectedMessage)))))
                .Switch()
                .Subscribe(TreeDiff)
                .DisposeWith(_disposables);

            ComputePatchDiff(schedulers,
                     SelectedFiles,
                     input,
                     PatchDiff)
                .DisposeWith(_disposables);

            ComputePatchDiff(schedulers,
                     SelectedIndexFiles,
                     input
                        .Where(d => d.OldCommit is null && d.NewCommit != null && d.NewCommit.Visit(c => false, d => true))
                        .Select(d => new DiffViewModelInput(d.Repository,
                            d.Repository.Head is null ? null : new Variant<Commit, DiffTargets>(d.Repository.Head.Tip),
                            new Variant<Commit, DiffTargets>(DiffTargets.Index),
                            d.CompareOptions)),
                     IndexPatchDiff)
                .DisposeWith(_disposables);
        }

        private static IDisposable ComputePatchDiff(ISchedulers schedulers,
            BatchList<TreeEntryChanges> selectedFiles,
            IObservable<DiffViewModelInput> input,
            BatchList<Variant<PatchDiff, CancellableChangesWithError>> patchDiff)
        {
            var disposables = new CompositeDisposable();

            var changedFilesSelectedObservable = selectedFiles
                .MakeObservable();

            var changedDiffSelection = changedFilesSelectedObservable
                .Select(args => args.NewItems?.Cast<TreeEntryChanges>())
                .SkipNull()
                //.WithLatestFrom(compareOptions, (treeChanges, options) => new { treeChanges, options })
                //.Merge(compareOptions
                //    .Select(options => new { treeChanges = (IEnumerable<TreeEntryChanges>)selectedFiles, options }))
                //.DistinctUntilChanged()
                //.WithLatestFrom(oldCommitObservable, (changes, oldCommit) => new { changes, oldCommit })
                //.WithLatestFrom(newCommitObservable, (data, newCommit) => new { data.changes, data.oldCommit, newCommit })
                .WithLatestFrom(input, (treeChanges, @in) => new { treeChanges, @in })
                .Select(data => new
                {
                    cancellableChanges = data.treeChanges
                        .Select(treeEntryChanges => new CancellableChanges(treeEntryChanges))
                        .ToList(),
                    data.@in.OldCommit,
                    data.@in.NewCommit,
                    data.@in.CompareOptions,
                    data.@in.Repository
                })
                .Publish();

            changedDiffSelection
                .MergeVariant(changedFilesSelectedObservable
                    .Select(args => args.OldItems?
                        .Cast<TreeEntryChanges>())
                    .SkipNull()
                    )
                    //.Merge(compareOptions
                    //    .Select(_ => (IEnumerable<TreeEntryChanges>)selectedFiles)))
                .Scan(new { activeCalculations = new HashSet<CancellableChanges>(), removedCalculations = new HashSet<CancellableChanges>() },
                    (acc, var) => var.Visit(
                        added => new {
                            activeCalculations = acc.activeCalculations.Concat(added.cancellableChanges).ToHashSet(),
                            removedCalculations = new HashSet<CancellableChanges>()
                        },
                        removed =>
                        {
                            var removedSet = removed.ToHashSet();
                            var removedCalculations = acc.activeCalculations
                                .Where(activeCalculation => removedSet.Contains(activeCalculation.TreeEntryChanges))
                                .ToHashSet();
                            return new
                            {
                                activeCalculations = acc.activeCalculations
                                .Where(active => !removedCalculations.Contains(active))
                                .ToHashSet(),
                                removedCalculations
                            };
                        }
                    ))
                .Select(calculations => calculations.removedCalculations)
                .Subscribe(removed =>
                {
                    foreach (var calculation in removed)
                    {
                        calculation.Cancellation.Cancel();
                        patchDiff.RemoveAll(old => old.Visit(s => s.CancellableChanges == calculation,
                            e => e.CancellableChanges == calculation), false);
                    }
                })
                .DisposeWith(disposables);

            changedDiffSelection
                .ObserveOn(schedulers.ThreadPool)
                .SelectMany(data => data.cancellableChanges
                    .Select(cancellableChanges =>
                        data.NewCommit is null ?
                        new Variant<IList<PatchDiff>, CancellableChangesWithError>(
                            new CancellableChangesWithError(cancellableChanges, NoFilesSelectedMessage)) :
                        Diff.PatchDiff.CreateDiff(data.Repository.Diff, data.Repository.TryAddRef(), data.Repository.Head, data.OldCommit, data.NewCommit, cancellableChanges
                            .Yield()
                            .SelectMany(o => o.TreeEntryChanges.Path
                                .Yield()
                                .Concat(o.TreeEntryChanges.OldPath.Yield())
                                .Distinct()
                                .Where(path => !string.IsNullOrEmpty(path)))
                            .ToList(), data.CompareOptions, cancellableChanges)))
                .ObserveOn(schedulers.Dispatcher)
                .Subscribe(added => added
                    .Visit(
                        newFiles =>
                        {
                            foreach (var item in newFiles)
                                if (!item.CancellableChanges.Cancellation.Token.IsCancellationRequested)
                                    patchDiff.Add(item);
                        },
                        error =>
                        {
                            if (!error.CancellableChanges.Cancellation.Token.IsCancellationRequested)
                                patchDiff.Add(error);
                        }))
                .DisposeWith(disposables);

            changedDiffSelection
                .Connect()
                .DisposeWith(disposables);

            return disposables;
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
