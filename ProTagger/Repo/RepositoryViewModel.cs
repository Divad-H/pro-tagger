using LibGit2Sharp;
using ProTagger.Repo.GitLog;
using ProTagger.Utilities;
using ReacitveMvvm;
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

        public BranchSelectionViewModel(ISchedulers schedulers, Branch branch, bool selected)
        {
            _branchSelection = new BranchSelection(branch.CanonicalName, branch.FriendlyName, selected);
            BranchSelectionObservable = this
                    .FromProperty(vm => vm.Selected)
                    .ObserveOn(schedulers.Dispatcher)
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

        private LogGraphNode? _selectedCommit;
        public LogGraphNode? SelectedCommit
        {
            get { return _selectedCommit; }
            set
            {
                if (_selectedCommit == value)
                    return;
                _selectedCommit = value;
                NotifyPropertyChanged();
            }
        }

        public static async Task<RepositoryViewModel?> Create(ISchedulers schedulers, CancellationToken ct, IRepositoryFactory repositoryFactory, string path)
        {
            var repositoryViewModel = await Task.Run(() => new RepositoryViewModel(schedulers, repositoryFactory, path));
            if (ct.IsCancellationRequested)
                return null;
            return repositoryViewModel;
        }

        public RepositoryViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, string path)
        {
            var branches = new List<BranchSelectionViewModel>();
            try
            {
                using var repo = repositoryFactory.CreateRepository(path);
                foreach (var branch in repo.Branches)
                    branches.Add(new BranchSelectionViewModel(schedulers, branch, branch.IsCurrentRepositoryHead));
            }
            catch (Exception)
            { }
            var branchesObservable = branches
                .Select(b => b.BranchSelectionObservable)
                .CombineLatest();

            Branches = branches;
            BranchesObservable = branchesObservable;

            _graph = new LogGraph(repositoryFactory, path, branchesObservable);

            _graph.SelectedNodeObservable
                .Subscribe(node => SelectedCommit = node)
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
            _graph.Dispose();
            _disposables.Dispose();
        }
    }
}
