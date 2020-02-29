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
    static class SingleFileDiffHelper
    {
        public static List<SingleFileDiff> EnsureElementSelected(this List<SingleFileDiff> elements)
        {
            if (elements.Any() && !elements.Any(element => element.IsSelected))
                elements.First().IsSelected = true;
            return elements;
        }
    }

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
        private bool _isSelected;
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

        public SingleFileDiff(TreeEntryChanges treeEntryChanges, bool isSelected)
        {
            _treeEntryChanges = treeEntryChanges;
            _isSelected = isSelected;
            IsSelectedObservable = this
                .FromProperty(vm => vm.IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class SingleFileDiffComparer : IEqualityComparer<SingleFileDiff>
        {
            public bool Equals(SingleFileDiff x, SingleFileDiff y)
            {
                return x.TreeEntryChanges.Path == y.TreeEntryChanges.Path;
            }

            public int GetHashCode(SingleFileDiff obj)
            {
                return obj.TreeEntryChanges.Path.GetHashCode();
            }
        }

        public static bool ApplySelections(IList<SingleFileDiff> from, IList<SingleFileDiff> to)
        {
            bool similarFound = false;
            foreach (var x in to.Intersect(from.Where(f => f.IsSelected), new SingleFileDiffComparer()))
            {
                x.IsSelected = true;
                similarFound = true;
            }
            return similarFound;
        }
    }

    public class FilePatch
    {
        public List<DiffAnalyzer.Hunk> Hunks { get; }
        public PatchEntryChanges PatchEntryChanges { get; }

        public FilePatch(List<DiffAnalyzer.Hunk> hunks, PatchEntryChanges patchEntryChanges)
        {
            Hunks = hunks;
            PatchEntryChanges = patchEntryChanges;
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

        private Variant<IList<FilePatch>, string> _patchDiff = new Variant<IList<FilePatch>, string>(NoFilesSelectedMessage);
        public Variant<IList<FilePatch>, string> PatchDiff
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
                                .Select(treeEntriesChanges => new SingleFileDiff(treeEntriesChanges, false))
                                .ToList()) :
                        new Variant<List<SingleFileDiff>, string>(NoCommitSelectedMessage))
                .Scan((last, current) =>
                {
                    if (current.Is<List<SingleFileDiff>>() && last.Is<List<SingleFileDiff>>())
                        if (!SingleFileDiff.ApplySelections(last.Get<List<SingleFileDiff>>(), current.Get<List<SingleFileDiff>>()))
                            current.Get<List<SingleFileDiff>>().First().IsSelected = true;
                    return current;
                })
                .Select(variant => variant.SelectResult(fileDiffs => fileDiffs.EnsureElementSelected()))
                .Publish();

            var selectedFilesObservable = filesDiffObservable
                .Select(singleFileDiffs => singleFileDiffs.Is<string>() ?
                    Observable.Return(new List<string>()) :
                    singleFileDiffs.Get<List<SingleFileDiff>>()
                        .Select(singleFileDiff => singleFileDiff.IsSelectedObservable
                            .Select(isSelected => new { singleFileDiff.TreeEntryChanges, isSelected }))
                        .CombineLatest()
                        .Select(treeEntriesChanges => treeEntriesChanges
                            .Where(d => d.isSelected)
                            .Select(d => d.TreeEntryChanges.Path)))
                .Switch();

            filesDiffObservable
                .Subscribe(filesDiff => FilesDiff = filesDiff)
                .DisposeWith(_disposables);

            var patchDiff = selectedFilesObservable
                .WithLatestFrom(OldCommitObservable, (selectedFiles, oldCommit) => new { selectedFiles, oldCommit })
                .WithLatestFrom(NewCommitObservable, (data, newCommit) => new { data.selectedFiles, data.oldCommit, newCommit })
                .Select(data => data.newCommit == null || !data.selectedFiles.Any() ?
                    new Variant<Patch, string>(NoFilesSelectedMessage) :
                    Diff.PatchDiff.CreateDiff(_repository, data.oldCommit, data.newCommit, data.selectedFiles))
                .Select(patchVariant => patchVariant.Visit(
                    patch => new Variant<IList<FilePatch>, string>(patch
                        .Select(patchEntry => new FilePatch(DiffAnalyzer.SplitIntoHunks(patchEntry.Patch), patchEntry))
                        .ToList()),
                    error => new Variant<IList<FilePatch>, string>(error)));

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
