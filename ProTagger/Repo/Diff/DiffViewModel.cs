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
    public class DiffViewModel : INotifyPropertyChanged, IDisposable
    {
        const string NoCommitSelectedMessage = "No commit selected.";

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
                        FileDiff.CreateDiff(_repository, oldCommit, newCommit) :
                        new Variant<List<TreeEntryChanges>, string>(NoCommitSelectedMessage));

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
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
