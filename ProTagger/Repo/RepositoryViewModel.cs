using LibGit2Sharp;
using ProTagger.Repo.Diff;
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
                .Select(selected => new BranchSelection(branch.CanonicalName, branch.FriendlyName, Selected));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private DiffViewModel _diff;
        public DiffViewModel Diff
        {
            get { return _diff; }
            set
            {
                if (_diff == value)
                    return;
                _diff = value;
                NotifyPropertyChanged();
            }
        }

        public static async Task<Variant<RepositoryViewModel, string>?> Create(ISchedulers schedulers,
            CancellationToken ct, 
            IRepositoryFactory repositoryFactory, 
            string path, 
            IObservable<CompareOptions> compareOptions)
        {
            try
            {
                var repositoryViewModel = await Task.Run(() => new RepositoryViewModel(schedulers, repositoryFactory, path, compareOptions));
                if (ct.IsCancellationRequested)
                {
                    repositoryViewModel?.Dispose();
                    return null;
                }
                return new Variant<RepositoryViewModel, string>(repositoryViewModel);
            }
            catch (Exception e)
            {
                return new Variant<RepositoryViewModel, string>(e.Message);
            }
        }

        readonly IRepositoryWrapper _repository;

        public RepositoryViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, string path, IObservable<CompareOptions> compareOptions)
        {
            var branches = new List<BranchSelectionViewModel>();
            _repository = repositoryFactory.CreateRepository(path);
            try
            {
                foreach (var branch in _repository.Branches)
                    branches.Add(new BranchSelectionViewModel(schedulers, branch, branch.IsCurrentRepositoryHead));

                var branchesObservable = branches
                    .Select(b => b.BranchSelectionObservable)
                    .CombineLatest();

                Branches = branches;
                BranchesObservable = branchesObservable;

                _graph = new LogGraph(_repository, branchesObservable);
            }
            catch (Exception)
            {
                _repository.Dispose();
                throw;
            }
            var selectedCommit = _graph.SelectedNodeObservable
                .Select(node => node?.Commit);
            var secondarySelectedCommit = _graph.SecondarySelectedNodeObservable
                .Select(node => node?.Commit);

            _diff = new DiffViewModel(_repository.Diff, schedulers, secondarySelectedCommit, selectedCommit, compareOptions);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _diff.Dispose();
            _repository.Dispose();
            _graph.Dispose();
            _disposables.Dispose();
        }
    }
}
