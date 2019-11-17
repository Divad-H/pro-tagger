using LibGit2Sharp;
using ProTagger.Repo.GitLog;
using ProTagger.Utilities;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProTagger
{
    public class BranchSelection
    {
        public string LongName { get; }
        public string PrettyName { get; }
        public bool Selected { get; }

        public BranchSelection (string longName, string prettyName, bool selected)
        {
            LongName = longName;
            PrettyName = prettyName;
            Selected = selected;
        }
    }

    public class BranchSelectionViewModel : INotifyPropertyChanged
    {
        private BranchSelection _branchSelection;
        public bool Selected 
        {
            get { return _branchSelection.Selected; }
            set
            {
                if (_branchSelection.Selected == value)
                    return;
                _branchSelection = new BranchSelection(_branchSelection.LongName,_branchSelection.PrettyName, value);
                NotifyPropertyChanged();
            }
        }
        public string PrettyName { get { return _branchSelection.PrettyName; } }

        public IObservable<BranchSelection> BranchSelectionObservable { get; }

        public BranchSelectionViewModel(Branch branch, bool selected)
        {
            _branchSelection = new BranchSelection(branch.CanonicalName, branch.FriendlyName, selected);
            BranchSelectionObservable = this
                    .FromProperty(vm => vm.Selected)
                    .Select(selected => new BranchSelection(branch.CanonicalName, branch.FriendlyName, Selected));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RepositoryViewModel : INotifyPropertyChanged, IDisposable
    {
        private LogGraph _graph;
        public LogGraph Graph
        {
            get { return _graph; }
            set
            {
                if (_graph == value)
                    return;
                _graph = value;
                NotifyPropertyChanged();
            }
        }

        public List<BranchSelectionViewModel> Branches { get; }
        public IObservable<IList<BranchSelection>> BranchesObservable { get; }

        public static async Task<RepositoryViewModel?> Create(CancellationToken ct, IRepositoryFactory repositoryFactory, string path)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (ct.IsCancellationRequested)
                return null;
            return new RepositoryViewModel(repositoryFactory, path);
        }

        public RepositoryViewModel(IRepositoryFactory repositoryFactory, string path)
        {
            var branches = new List<BranchSelectionViewModel>();
            try
            {
                using var repo = repositoryFactory.CreateRepository(path);
                foreach (var branch in repo.Branches)
                    branches.Add(new BranchSelectionViewModel(branch, branch.IsCurrentRepositoryHead));
            }
            catch (Exception)
            { }
            var branchesObservable = branches
                .Select(b => b.BranchSelectionObservable)
                .CombineLatest();

            Branches = branches;
            BranchesObservable = branchesObservable;

            _graph = new LogGraph(repositoryFactory, path, branchesObservable);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _graph.Dispose();
            _disposables.Dispose();
        }
    }
}
