using LibGit2Sharp;
using ProTagger.Utilities;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProTagger.Repo.Diff
{
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

    public class DiffViewModel : IDisposable
    {
        public const string NoCommitSelectedMessage = "No commit selected.";
        public const string NoFilesSelectedMessage = "No files selected.";
        
        public BatchList<Variant<PatchDiff, CancellableChangesWithError>> PatchDiff { get; }
            = new BatchList<Variant<PatchDiff, CancellableChangesWithError>>();

        public BatchList<TreeEntryChanges> SelectedFiles { get; } = new BatchList<TreeEntryChanges>();

        public Func<object, object, bool> KeepTreeDiffChangesSelectedRule 
            => (oldFile, newFile) => ((TreeEntryChanges)oldFile).Path == ((TreeEntryChanges)newFile).Path;

        public ViewSubject<Variant<List<TreeEntryChanges>, string>> TreeDiff { get; }

        public DiffViewModel(LibGit2Sharp.Diff diff,
            IRefCount repositoryRefCounter,
            ISchedulers schedulers,
            Branch? head,
            IObservable<Variant<Commit, DiffTargets>?> oldCommitObservable, 
            IObservable<Variant<Commit, DiffTargets>?> newCommitObservable,
            IObservable<CompareOptions> compareOptions)
        {
            TreeDiff = new ViewSubject<Variant<List<TreeEntryChanges>, string>>(new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage))
                .DisposeWith(_disposables);

            Observable
                .CombineLatest(newCommitObservable, oldCommitObservable, compareOptions,
                    (newCommit, oldCommit, compareOptions) => new { newCommit, oldCommit, compareOptions })
                .Select(o =>
                    o.newCommit != null ?
                        Diff.TreeDiff.CreateDiff(repositoryRefCounter, diff, head, o.oldCommit, o.newCommit, o.compareOptions) :
                        Task.FromResult(new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage)))
                .Switch()
                .Subscribe(TreeDiff)
                .DisposeWith(_disposables);

            var changedFilesSelectedObservable = SelectedFiles
                .MakeObservable();

            var changedDiffSelection = changedFilesSelectedObservable
                .Select(args => args.NewItems?.Cast<TreeEntryChanges>())
                .SkipNull()
                .WithLatestFrom(compareOptions, (treeChanges, options) => new { treeChanges , options })
                .Merge(compareOptions
                    .Select(options => new { treeChanges = (IEnumerable<TreeEntryChanges>)SelectedFiles, options }))
                .DistinctUntilChanged()
                .WithLatestFrom(oldCommitObservable, (changes, oldCommit) => new { changes, oldCommit })
                .WithLatestFrom(newCommitObservable, (data, newCommit) => new { data.changes, data.oldCommit, newCommit })
                .Select(data => new
                {
                    cancellableChanges = data.changes.treeChanges
                        .Select(treeEntryChanges => new CancellableChanges(treeEntryChanges))
                        .ToList(),
                    data.oldCommit,
                    data.newCommit,
                    data.changes.options
                })
                .Publish();

            changedDiffSelection
                .MergeVariant(changedFilesSelectedObservable
                    .Select(args => args.OldItems?
                        .Cast<TreeEntryChanges>())
                    .SkipNull()
                    .Merge(compareOptions
                        .Select(_ => (IEnumerable<TreeEntryChanges>)SelectedFiles)))
                .Scan(new { activeCalculations = new List<CancellableChanges>(), removedCalculations = new List<CancellableChanges>() }, 
                    (acc, var) => var.Visit(
                        added => new { activeCalculations = acc.activeCalculations.Concat(added.cancellableChanges).ToList(),
                            removedCalculations = new List<CancellableChanges>() },
                        removed =>
                        {
                            var removedCalculations = acc.activeCalculations
                                .Where(activeCalculation => removed
                                    .Any(rem => rem == activeCalculation.TreeEntryChanges))
                                .ToList();
                            return new { activeCalculations = acc.activeCalculations
                                .Where(active => !removedCalculations.Contains(active))
                                .ToList(), removedCalculations };
                        }
                    ))
                .Select(calculations => calculations.removedCalculations)
                .Subscribe(removed =>
                {
                    foreach (var calculation in removed)
                    {
                        calculation.Cancellation.Cancel();
                        PatchDiff.RemoveAll(old => old.Visit(s => s.CancellableChanges == calculation,
                            e => e.CancellableChanges == calculation), false);
                    }
                })
                .DisposeWith(_disposables);

            changedDiffSelection
                .ObserveOn(schedulers.ThreadPool)
                .SelectMany(data => data.cancellableChanges
                    .Select(cancellableChanges =>
                        data.newCommit == null ?
                        new Variant<IList<PatchDiff>, CancellableChangesWithError>(
                            new CancellableChangesWithError(cancellableChanges, NoFilesSelectedMessage)) :
                        Diff.PatchDiff.CreateDiff(diff, repositoryRefCounter.TryAddRef(), head, data.oldCommit, data.newCommit, cancellableChanges
                            .Yield()
                            .SelectMany(o => o.TreeEntryChanges.Path
                                .Yield()
                                .Concat(o.TreeEntryChanges.OldPath.Yield())
                                .Distinct()
                                .Where(path => !string.IsNullOrEmpty(path)))
                            .ToList(), data.options, cancellableChanges)))
                .ObserveOn(schedulers.Dispatcher)
                .Subscribe(added => added
                    .Visit(
                        newFiles => 
                        {
                            foreach (var item in newFiles)
                                if (!item.CancellableChanges.Cancellation.Token.IsCancellationRequested)
                                    PatchDiff.Add(new Variant<PatchDiff, CancellableChangesWithError>(item));
                        },
                        error =>
                        {
                            if (!error.CancellableChanges.Cancellation.Token.IsCancellationRequested)
                                PatchDiff.Add(new Variant<PatchDiff, CancellableChangesWithError>(error));
                        }))
                .DisposeWith(_disposables);

            changedDiffSelection
                .Connect()
                .DisposeWith(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
