using LibGit2Sharp;
using ProTagger.Utilities;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace ProTagger.Repo.Diff
{
    public class SingleFileDiff : INotifyPropertyChanged
    {
        private TreeEntryChanges _treeEntryChanges;
        public TreeEntryChanges TreeEntryChanges
        {
            get
            {
                return _treeEntryChanges;
            }
            set
            {
                if (_treeEntryChanges == value)
                    return;
                _treeEntryChanges = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isSelected = false;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }
        public readonly IObservable<bool> IsSelectedObservable;

        public SingleFileDiff(TreeEntryChanges treeEntryChanges)
        {
            _treeEntryChanges = treeEntryChanges;
            IsSelectedObservable = this
                .FromProperty(vm => vm.IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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

        private Variant<Patch, string> _patchDiff = new Variant<Patch, string>(NoFilesSelectedMessage);
        public Variant<Patch, string> PatchDiff
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


        private Variant<List<SingleFileDiff>, string> _filesDiff = new Variant<List<SingleFileDiff>, string>(NoCommitSelectedMessage);
        public Variant<List<SingleFileDiff>, string> FilesDiff
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

        public DiffViewModel(IRepositoryWrapper repository, IObservable<Commit?> oldCommit, IObservable<Commit?> newCommit)
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
                                .Select(treeEntriesChanges => new SingleFileDiff(treeEntriesChanges))
                                .ToList()) :
                        new Variant<List<SingleFileDiff>, string>(NoCommitSelectedMessage))
                .Publish();

            var selectedFilesObservable = filesDiffObservable
                .Select(singleFileDiffs => singleFileDiffs.Is<string>() ?
                  Observable.Return(new List<string>()) :
                  singleFileDiffs.Get<List<SingleFileDiff>>()
                      .Select(singleFileDiff => singleFileDiff.IsSelectedObservable
                          .Select(isSelected => Tuple.Create(singleFileDiff.TreeEntryChanges, isSelected)))
                      .CombineLatest()
                      .Select(treeEntriesChanges => treeEntriesChanges
                          .Where(tup => tup.Item2)
                          .Select(tup => tup.Item1.Path)))
                .Switch();

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
                .DisposeWith(_disposables);

            var patchDiff = selectedFilesObservable
                .WithLatestFrom(OldCommitObservable, (selectedFiles, oldCommit) => new { SelectedFiles = selectedFiles, OldCommit = oldCommit })
                .WithLatestFrom(NewCommitObservable, (data, newCommit) => new { data.SelectedFiles, data.OldCommit, NewCommit = newCommit })
                .Select(data => data.NewCommit == null || !data.SelectedFiles.Any() ?
                    new Variant<Patch, string>(NoFilesSelectedMessage) :
                    Diff.PatchDiff.CreateDiff(_repository, data.OldCommit, data.NewCommit, data.SelectedFiles));

            patchDiff
                .Subscribe(patchDiff => PatchDiff = patchDiff)
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
