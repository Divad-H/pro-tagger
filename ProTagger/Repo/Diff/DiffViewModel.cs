using LibGit2Sharp;
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
    public class CancelableChanges
    {
        public TreeEntryChanges TreeEntryChanges { get; }
        public CancellationTokenSource Cancellation { get; }
        public CancelableChanges(TreeEntryChanges treeEntryChanges)
            => (TreeEntryChanges, Cancellation) = (treeEntryChanges, new CancellationTokenSource());
    }

    public class CancelableChangesWithError
    {
        public CancelableChanges CancelableChanges { get; }
        public string Error { get; }
        public CancelableChangesWithError(CancelableChanges changes, string error)
            => (CancelableChanges, Error) = (changes, error);
    }

    public class FilePatch
    {
        public List<DiffAnalyzer.Hunk> Hunks { get; }
        public PatchEntryChanges PatchEntryChanges { get; }
        public CancelableChanges CancelableChanges { get; }

        public FilePatch(List<DiffAnalyzer.Hunk> hunks, PatchEntryChanges patchEntryChanges, CancelableChanges cancelableChanges)
          => (Hunks, PatchEntryChanges, CancelableChanges) = (hunks, patchEntryChanges, cancelableChanges);
    }

    public class DiffViewModel : INotifyPropertyChanged, IDisposable
    {
        const string NoCommitSelectedMessage = "No commit selected.";
        const string NoFilesSelectedMessage = "No files selected.";

        readonly IObservable<Commit?> OldCommitObservable;
        private Commit? _oldCommit;
        public Commit? OldCommit
        {
            get => _oldCommit;
            private set
            {
                if (_oldCommit == value)
                    return;
                _oldCommit = value;
                NotifyPropertyChanged();
            }
        }


        readonly IObservable<Commit?> NewCommitObservable;
        private Commit? _newCommit;
        public Commit? NewCommit
        {
            get => _newCommit;
            private set
            {
                if (_newCommit == value)
                    return;
                _newCommit = value;
                NotifyPropertyChanged();
            }
        }

        private BatchList<Variant<FilePatch, CancelableChangesWithError>> _patchDiff = new BatchList<Variant<FilePatch, CancelableChangesWithError>>();
        public BatchList<Variant<FilePatch, CancelableChangesWithError>> PatchDiff
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

        private BatchList<TreeEntryChanges> _selectedFiles = new BatchList<TreeEntryChanges>();
        public BatchList<TreeEntryChanges> SelectedFiles
        {
            get => _selectedFiles;
            set
            {
                if (_selectedFiles == value)
                    return;
                _selectedFiles = value;
                NotifyPropertyChanged();
            }
        }

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

            OldCommitObservable
                .Subscribe((oldCommit) => OldCommit = oldCommit)
                .DisposeWith(_disposables);

            NewCommitObservable
                .Subscribe((newCommit) => NewCommit = newCommit)
                .DisposeWith(_disposables);

            var filesDiffObservable = NewCommitObservable
                .CombineLatest(OldCommitObservable, (newCommit, oldCommit) =>
                    newCommit != null ?
                        FileDiff.CreateDiff(_repository, oldCommit, newCommit)
                            .SelectResult(treeEntriesChanges => treeEntriesChanges
                                .ToList()) :
                        new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage))
                .Publish();

            var selectedFilesObservable = SelectedFiles
                .MakeObservable();

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
                .DisposeWith(_disposables);

            var addedSelection = selectedFilesObservable
                .Select(args => args.NewItems)
                .SkipNull()
                .WithLatestFrom(OldCommitObservable, (addedSelection, oldCommit) => new { addedSelection = addedSelection
                    .Cast<TreeEntryChanges>(), oldCommit })
                .WithLatestFrom(NewCommitObservable, (data, newCommit) => new { data.addedSelection, data.oldCommit, newCommit })
                .Select(data => new
                {
                    addedSelection = data.addedSelection
                        .Select(treeEntryChanges => new CancelableChanges(treeEntryChanges))
                        .ToList(),
                    data.oldCommit,
                    data.newCommit
                })
                .Publish();

            var addedPatchDiff = addedSelection
                .ObserveOn(schedulers.ThreadPool)
                .SelectMany(data => data.addedSelection
                    .Select(cancelableChanges => 
                        data.newCommit == null ?
                        new Variant<PatchDiff, Variant<CancelableChanges, CancelableChangesWithError>>(
                            new Variant<CancelableChanges, CancelableChangesWithError>(new CancelableChangesWithError(cancelableChanges, NoFilesSelectedMessage))) :
                        Diff.PatchDiff.CreateDiff(_repository, data.oldCommit, data.newCommit, cancelableChanges
                            .Yield()
                            .SelectMany(o => o.TreeEntryChanges.Path
                                .Yield()
                                .Concat(o.TreeEntryChanges.OldPath.Yield())
                                .Distinct()
                                .Where(path => !string.IsNullOrEmpty(path)))
                            .ToList(), cancelableChanges)))
                .Select(patchVariant => patchVariant.Visit(
                    patch => new Variant<IList<FilePatch>, CancelableChangesWithError>(patch.Patch
                        .Select(patchEntry => new FilePatch(DiffAnalyzer.SplitIntoHunks(patchEntry.Patch, patch.CancelableChanges.Cancellation.Token), patchEntry, patch.CancelableChanges))
                        .ToList()),
                    unsuccess => unsuccess.Visit(
                        cancelled => new Variant<IList<FilePatch>, CancelableChangesWithError>(new List<FilePatch>()),
                        error => new Variant<IList<FilePatch>, CancelableChangesWithError>(error))))
                .ObserveOn(schedulers.Dispatcher);

            var removedPatchDíff = selectedFilesObservable
                .Select(args => args.OldItems?
                    .Cast<TreeEntryChanges>())
                .SkipNull();

            var runningCalculations = new List<CancelableChanges>();

            addedSelection
                .Subscribe(o =>
                {
                    foreach (var c in o.addedSelection)
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
                        PatchDiff.RemoveAll(old => old.Visit(s => s.CancelableChanges == item, e => e.CancelableChanges == item));
                })
                .DisposeWith(_disposables);

            addedPatchDiff
                .Subscribe(added => added.Visit(
                        newFiles => 
                            {
                                foreach (var item in newFiles)
                                {
                                    if (!item.CancelableChanges.Cancellation.Token.IsCancellationRequested)
                                        PatchDiff.Add(new Variant<FilePatch, CancelableChangesWithError>(item));
                                }
                            },
                        error =>
                        {
                            if (!error.CancelableChanges.Cancellation.Token.IsCancellationRequested)
                                PatchDiff.Add(new Variant<FilePatch, CancelableChangesWithError>(error));
                        }))
                .DisposeWith(_disposables);

            filesDiffObservable
                .Connect()
                .DisposeWith(_disposables);

            addedSelection
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
