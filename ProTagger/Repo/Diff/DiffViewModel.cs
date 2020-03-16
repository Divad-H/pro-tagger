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

namespace ProTagger.Repo.Diff
{
    public class FilePatch
    {
        public List<DiffAnalyzer.Hunk> Hunks { get; }
        public PatchEntryChanges PatchEntryChanges { get; }
        public TreeEntryChanges TreeEntryChanges { get; }

        public FilePatch(List<DiffAnalyzer.Hunk> hunks, PatchEntryChanges patchEntryChanges, TreeEntryChanges treeEntryChanges)
          => (Hunks, PatchEntryChanges, TreeEntryChanges) = (hunks, patchEntryChanges, treeEntryChanges);
    }

    public class DiffViewModel : INotifyPropertyChanged, IDisposable
    {
        const string NoCommitSelectedMessage = "No commit selected.";
        const string NoFilesSelectedMessage = "No files selected.";

        readonly IObservable<Commit?> OldCommitObservable;
        private Commit? _oldCommit;
        public Commit? OldCommit
        {
            get
            {
                return _oldCommit;
            }
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
            get
            {
                return _newCommit;
            }
            private set
            {
                if (_newCommit == value)
                    return;
                _newCommit = value;
                NotifyPropertyChanged();
            }
        }

        private Variant<BatchList<FilePatch>, string> _patchDiff
            = new Variant<BatchList<FilePatch>, string>(NoFilesSelectedMessage);
        public Variant<BatchList<FilePatch>, string> PatchDiff
        {
            get
            {
                return _patchDiff;
            }
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
            get
            {
                return _selectedFiles;
            }
            set
            {
                if (_selectedFiles == value)
                    return;
                _selectedFiles = value;
                NotifyPropertyChanged();
            }
        }

        private Variant<List<TreeEntryChanges>, string> _filesDiff = new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage);
        public Variant<List<TreeEntryChanges>, string> FilesDiff
        {
            get
            {
                return _filesDiff;
            }
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
                .MakeObservable()
                .ObserveOn(schedulers.ThreadPool);

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
                .DisposeWith(_disposables);

            var addedPatchDiff = selectedFilesObservable
                .Select(args => args.NewItems)
                .SkipNull()
                .WithLatestFrom(OldCommitObservable, (addedSelection, oldCommit) => new { addedSelection, oldCommit })
                .WithLatestFrom(NewCommitObservable, (data, newCommit) => new { data.addedSelection, data.oldCommit, newCommit })
                .SelectMany(data => data.addedSelection.Cast<TreeEntryChanges>()
                    .Select(treeEntryChanges => 
                        data.newCommit == null ?
                        new Variant<PatchDiff, string>(NoFilesSelectedMessage) :
                        Diff.PatchDiff.CreateDiff(_repository, data.oldCommit, data.newCommit, treeEntryChanges
                            .Yield()
                            .SelectMany(o => o.Path
                                .Yield()
                                .Concat(o.OldPath.Yield())
                                .Distinct()
                                .Where(path => !string.IsNullOrEmpty(path)))
                            .ToList(), treeEntryChanges)))
                .Select(patchVariant => patchVariant.Visit(
                    patch => new Variant<IList<FilePatch>, string>(patch.Patch
                        .Select(patchEntry => new FilePatch(DiffAnalyzer.SplitIntoHunks(patchEntry.Patch), patchEntry, patch.TreeEntryChanges))
                        .ToList()),
                    error => new Variant<IList<FilePatch>, string>(error)));

            var removedPatchDíff = selectedFilesObservable
                .Select(args => args.OldItems?
                    .Cast<TreeEntryChanges>())
                .SkipNull();

            var addedRemoved = addedPatchDiff
                .Select(added => new Variant<Variant<IList<FilePatch>, string>, IEnumerable<TreeEntryChanges>>(added))
                .Merge(removedPatchDíff
                    .Select(removed => new Variant<Variant<IList<FilePatch>, string>, IEnumerable<TreeEntryChanges>>(removed)))
                .ObserveOn(schedulers.Dispatcher);

            var missedRemoves = new List<TreeEntryChanges>();
            addedRemoved
                .Subscribe(addRem => addRem.Visit(
                    added => added.Visit(
                        newFiles => PatchDiff.Visit(
                            patchDiff =>
                            {
                                foreach (var item in newFiles)
                                {
                                    var missed = missedRemoves.Find(missed => item.TreeEntryChanges.Path == missed.Path
                                        && item.TreeEntryChanges.OldPath == missed.OldPath
                                        && item.TreeEntryChanges.Oid == missed.Oid
                                        && item.TreeEntryChanges.OldOid == missed.OldOid);
                                    if (missed != null)
                                        missedRemoves.Remove(missed);
                                    else
                                        patchDiff.Add(item);
                                }
                            },
                            _ => PatchDiff = new Variant<BatchList<FilePatch>, string>(new BatchList<FilePatch>(newFiles))),
                        errorMsg => PatchDiff = new Variant<BatchList<FilePatch>, string>(errorMsg)),
                    removed => PatchDiff.Visit(
                        patchDiff =>
                        {
                            foreach (var item in removed)
                                if (!patchDiff.Remove(patchDiff.FirstOrDefault(old => old.TreeEntryChanges.Path == item.Path
                                     && old.TreeEntryChanges.OldPath == item.OldPath
                                     && old.TreeEntryChanges.Oid == item.Oid
                                     && old.TreeEntryChanges.OldOid == item.OldOid)))
                                    missedRemoves.Add(item);
                        },
                        _ => { })))
                .DisposeWith(_disposables);

            filesDiffObservable
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
