using LibGit2Sharp;
using ProTagger.Utilities;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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

    public class FilePatch
    {
        public List<DiffAnalyzer.Hunk> Hunks { get; }
        public PatchEntryChanges PatchEntryChanges { get; }
        public CancellableChanges CancellableChanges { get; }

        public FilePatch(List<DiffAnalyzer.Hunk> hunks, PatchEntryChanges patchEntryChanges, CancellableChanges cancellableChanges)
          => (Hunks, PatchEntryChanges, CancellableChanges) = (hunks, patchEntryChanges, cancellableChanges);
    }

    public class DiffViewModel : INotifyPropertyChanged, IDisposable
    {
        private class ChangesAndSource
        {
            public IEnumerable<TreeEntryChanges> Changes { get; }
            public CompareOptions Options { get; }
            public ChangesAndSource(IEnumerable<TreeEntryChanges> changes, CompareOptions options)
                => (Changes, Options) = (changes, options);
        }

        const string NoCommitSelectedMessage = "No commit selected.";
        const string NoFilesSelectedMessage = "No files selected.";

        private BatchList<Variant<FilePatch, CancellableChangesWithError>> _patchDiff 
            = new BatchList<Variant<FilePatch, CancellableChangesWithError>>();
        public BatchList<Variant<FilePatch, CancellableChangesWithError>> PatchDiff
        {
            get => _patchDiff;
            private set
            {
                if (_patchDiff == value)
                    return;
                _patchDiff = value;
                NotifyPropertyChanged();
            }
        }

        public BatchList<TreeEntryChanges> SelectedFiles { get; } = new BatchList<TreeEntryChanges>();

        public Func<object, object, bool> KeepTreeDiffChangesSelectedRule 
            => (oldFile, newFile) => ((TreeEntryChanges)oldFile).Path == ((TreeEntryChanges)newFile).Path;

        private Variant<List<TreeEntryChanges>, string> _filesDiff = new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage);
        public Variant<List<TreeEntryChanges>, string> FilesDiff
        {
            get => _filesDiff;
            set
            {
                if (_filesDiff == value)
                    return;
                _filesDiff = value;
                NotifyPropertyChanged();
            }
        }

        public DiffViewModel(IRepositoryWrapper repository, 
            ISchedulers schedulers, 
            IObservable<Commit?> oldCommitObservable, 
            IObservable<Commit?> newCommitObservable,
            IObservable<CompareOptions> compareOptions)
        {
            var filesDiffObservable = Observable
                .CombineLatest(newCommitObservable, oldCommitObservable, compareOptions,
                    (newCommit, oldCommit, compareOptions) => new { newCommit, oldCommit, compareOptions })
                .ObserveOn(schedulers.ThreadPool)
                .Select(o =>
                    o.newCommit != null ?
                        FileDiff.CreateDiff(repository, o.oldCommit, o.newCommit, o.compareOptions) :
                        new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage))
                .ObserveOn(schedulers.Dispatcher);

            var changedFilesSelectedObservable = SelectedFiles
                .MakeObservable();

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
                .DisposeWith(_disposables);

            var addedSelection = changedFilesSelectedObservable
                .Select(args => args.NewItems?.Cast<TreeEntryChanges>())
                .SkipNull();

            var refreshedSelection = compareOptions
                .Select(options => new ChangesAndSource(SelectedFiles, options));

            var changedDiffSelection = addedSelection
                .WithLatestFrom(compareOptions, (treeChanges, options) => new ChangesAndSource(treeChanges, options))
                .Merge(refreshedSelection)
                .WithLatestFrom(oldCommitObservable, (changes, oldCommit) => new { changes, oldCommit })
                .WithLatestFrom(newCommitObservable, (data, newCommit) => new { data.changes, data.oldCommit, newCommit })
                .Select(data => new
                {
                    cancellableChanges = data.changes.Changes
                        .Select(treeEntryChanges => new CancellableChanges(treeEntryChanges))
                        .ToList(),
                    data.oldCommit,
                    data.newCommit,
                    data.changes.Options
                })
                .Publish();

            var removedPatchDíff = changedFilesSelectedObservable
                .Select(args => args.OldItems?
                    .Cast<TreeEntryChanges>())
                .SkipNull()
                .Merge(compareOptions
                    .Select(_ => (IEnumerable<TreeEntryChanges>)SelectedFiles));

            changedDiffSelection
                .MergeVariant(removedPatchDíff)
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
                        new Variant<PatchDiff, Variant<CancellableChanges, CancellableChangesWithError>>(
                            new Variant<CancellableChanges, CancellableChangesWithError>(new CancellableChangesWithError(cancellableChanges, NoFilesSelectedMessage))) :
                        Diff.PatchDiff.CreateDiff(repository, data.oldCommit, data.newCommit, cancellableChanges
                            .Yield()
                            .SelectMany(o => o.TreeEntryChanges.Path
                                .Yield()
                                .Concat(o.TreeEntryChanges.OldPath.Yield())
                                .Distinct()
                                .Where(path => !string.IsNullOrEmpty(path)))
                            .ToList(), data.Options, cancellableChanges)))
                .Select(patchVariant => patchVariant.Visit(
                    patch => new Variant<IList<FilePatch>, CancellableChangesWithError>(patch.Patch
                        .Select(patchEntry => new FilePatch(DiffAnalyzer.SplitIntoHunks(patchEntry.Patch, patch.CancellableChanges.Cancellation.Token), patchEntry, patch.CancellableChanges))
                        .ToList()),
                    unsuccess => unsuccess.Visit(
                        cancelled => new Variant<IList<FilePatch>, CancellableChangesWithError>(new List<FilePatch>()),
                        error => new Variant<IList<FilePatch>, CancellableChangesWithError>(error))))
                .ObserveOn(schedulers.Dispatcher)
                .Subscribe(added => added.Visit(
                        newFiles => 
                            {
                                foreach (var item in newFiles)
                                {
                                    if (!item.CancellableChanges.Cancellation.Token.IsCancellationRequested)
                                        PatchDiff.Add(new Variant<FilePatch, CancellableChangesWithError>(item));
                                }
                            },
                        error =>
                        {
                            if (!error.CancellableChanges.Cancellation.Token.IsCancellationRequested)
                                PatchDiff.Add(new Variant<FilePatch, CancellableChangesWithError>(error));
                        }))
                .DisposeWith(_disposables);

            changedDiffSelection
                .Connect()
                .DisposeWith(_disposables);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
