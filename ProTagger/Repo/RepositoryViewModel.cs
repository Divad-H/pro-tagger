using LibGit2Sharp;
using ProTagger.Repo.Diff;
using ProTagger.Repo.GitLog;
using ProTagger.Utilities;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

    public class BranchSelectionViewModel
    {
        private readonly BranchSelection _branchSelection;
        public ViewObservable<bool> Selected { get; }
        public string PrettyName => _branchSelection.PrettyName;

        public IObservable<BranchSelection> BranchSelectionObservable { get; }

        public BranchSelectionViewModel(ISchedulers schedulers, Branch branch, bool selected)
        {
            Selected = new ViewObservable<bool>(selected);
            _branchSelection = new BranchSelection(branch.CanonicalName, branch.FriendlyName, selected);
            BranchSelectionObservable = Selected
                .Select(selected => new BranchSelection(branch.CanonicalName, branch.FriendlyName, selected));
        }
    }

    public class RepositoryViewModel : IDisposable
    {
        public LogGraph Graph { get; }

        public List<BranchSelectionViewModel> Branches { get; }
        public IObservable<IList<BranchSelection>> BranchesObservable { get; }
        public DiffViewModel Diff { get; }

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

        private readonly IRepositoryWrapper _repository;

        public RepositoryViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, string path, IObservable<CompareOptions> compareOptions)
        {
            var branches = new List<BranchSelectionViewModel>();
            _repository = repositoryFactory.CreateRepository(path)
                .DisposeWith(_disposables);
            try
            {
                foreach (var branch in _repository.Branches)
                    branches.Add(new BranchSelectionViewModel(schedulers, branch, branch.IsCurrentRepositoryHead));

                var branchesObservable = branches
                    .Select(b => b.BranchSelectionObservable)
                    .CombineLatest();

                Branches = branches;
                BranchesObservable = branchesObservable;

                Graph = new LogGraph(_repository, branchesObservable)
                    .DisposeWith(_disposables);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
            var selectedCommit = Graph.SelectedNode
                .Select(node => node?.Commit);
            var secondarySelectedCommit = Graph.SecondarySelectedNode
                .Select(node => node?.Commit);

            Diff = new DiffViewModel(_repository.Diff, _repository, schedulers, secondarySelectedCommit, selectedCommit, compareOptions)
                .DisposeWith(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
