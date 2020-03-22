﻿using LibGit2Sharp;
using ProTagger.Utilities;
using ProTagger.Wpf;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        readonly IObservable<Commit?> OldCommitObservable;
        readonly IObservable<Commit?> NewCommitObservable;
        
        private BatchList<Variant<FilePatch, CancellableChangesWithError>> _patchDiff = new BatchList<Variant<FilePatch, CancellableChangesWithError>>();
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

        private readonly IRepositoryWrapper _repository;

        public DiffViewModel(IRepositoryWrapper repository, ISchedulers schedulers, IObservable<Commit?> oldCommit, IObservable<Commit?> newCommit)
        {
            _repository = repository;

            OldCommitObservable = oldCommit;
            NewCommitObservable = newCommit;

            var filesDiffObservable = NewCommitObservable
                .CombineLatest(OldCommitObservable, (newCommit, oldCommit) =>
                    newCommit != null ?
                        FileDiff.CreateDiff(_repository, oldCommit, newCommit)
                            .SelectResult(treeEntriesChanges => treeEntriesChanges
                                .ToList()) :
                        new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage))
                .Publish();

            int dummy = 3;
            var compareOptionsObservable = Observable
                .Interval(TimeSpan.FromSeconds(3), schedulers.Dispatcher)
                .StartWith(0)
                .Select(_ => new CompareOptions()
                {
                    Algorithm = DiffAlgorithm.Myers,
                    ContextLines = dummy++,
                    IncludeUnmodified = false,
                    IndentHeuristic = false,
                    InterhunkLines = 0,
                    Similarity = new SimilarityOptions()
                });

            var changedFilesSelectedObservable = SelectedFiles
                .MakeObservable();

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
                .DisposeWith(_disposables);

            var addedSelection = changedFilesSelectedObservable
                .Select(args => args.NewItems?.Cast<TreeEntryChanges>())
                .SkipNull();

            var refreshedSelection = compareOptionsObservable
                .Select(options => new ChangesAndSource(SelectedFiles, options));

            var changedDiffSelection = addedSelection
                .WithLatestFrom(compareOptionsObservable, (treeChanges, options) => new ChangesAndSource(treeChanges, options))
                .Merge(refreshedSelection)
                .WithLatestFrom(OldCommitObservable, (changes, oldCommit) => new { changes, oldCommit })
                .WithLatestFrom(NewCommitObservable, (data, newCommit) => new { data.changes, data.oldCommit, newCommit })
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

            var addedPatchDiff = changedDiffSelection
                .ObserveOn(schedulers.ThreadPool)
                .SelectMany(data => data.cancellableChanges
                    .Select(cancellableChanges => 
                        data.newCommit == null ?
                        new Variant<PatchDiff, Variant<CancellableChanges, CancellableChangesWithError>>(
                            new Variant<CancellableChanges, CancellableChangesWithError>(new CancellableChangesWithError(cancellableChanges, NoFilesSelectedMessage))) :
                        Diff.PatchDiff.CreateDiff(_repository, data.oldCommit, data.newCommit, cancellableChanges
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
                .ObserveOn(schedulers.Dispatcher);

            var removedPatchDíff = changedFilesSelectedObservable
                .Select(args => args.OldItems?
                    .Cast<TreeEntryChanges>())
                .SkipNull()
                .Merge(compareOptionsObservable
                    .Select(_ => (IEnumerable<TreeEntryChanges>)SelectedFiles));

            var runningCalculations = new List<CancellableChanges>();

            changedDiffSelection
                .Subscribe(o =>
                {
                    foreach (var c in o.cancellableChanges)
                        runningCalculations.Add(c);
                })
                .DisposeWith(_disposables);

            removedPatchDíff
                .Subscribe(removed =>
                {
                    var stoppedCalcuations = runningCalculations
                        .Where(runningCalculation => removed
                            .Any(rem => rem == runningCalculation.TreeEntryChanges))
                        .ToList();
                    foreach (var stoppedCalculation in stoppedCalcuations)
                        runningCalculations.Remove(stoppedCalculation);

                    foreach (var calculation in stoppedCalcuations)
                        calculation.Cancellation.Cancel();
                    foreach (var item in stoppedCalcuations)
                        PatchDiff.RemoveAll(old => old.Visit(s => s.CancellableChanges == item, e => e.CancellableChanges == item));
                })
                .DisposeWith(_disposables);

            addedPatchDiff
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

            filesDiffObservable
                .Connect()
                .DisposeWith(_disposables);

            changedDiffSelection
                .Connect()
                .DisposeWith(_disposables);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
