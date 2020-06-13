using LibGit2Sharp;
using ProTagger.Repository;
using ProTagger.Repository.Diff;
using ProTagger.Repository.GitLog;
using ProTagger.RepositorySelection;
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
        public RepositoryDescription RepositoryDescription { get; }

        public static async Task<Variant<RepositoryViewModel, RepositoryError>?> Create(ISchedulers schedulers,
            CancellationToken ct, 
            IRepositoryFactory repositoryFactory,
            RepositoryDescription description, 
            IObservable<CompareOptions> compareOptions)
        {
            try
            {
                var repositoryViewModel = await Task.Run(() => new RepositoryViewModel(schedulers, repositoryFactory, description, compareOptions));
                if (ct.IsCancellationRequested)
                {
                    repositoryViewModel?.Dispose();
                    return null;
                }
                return new Variant<RepositoryViewModel, RepositoryError>(repositoryViewModel);
            }
            catch (Exception e)
            {
                return new Variant<RepositoryViewModel, RepositoryError>(new RepositoryError(e.Message, description));
            }
        }

        private readonly IRepositoryWrapper _repository;

        public RepositoryViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, RepositoryDescription description, IObservable<CompareOptions> compareOptions)
        {
            RepositoryDescription = description;
            var branches = new List<BranchSelectionViewModel>();
            _repository = repositoryFactory.CreateRepository(description.Path)
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

                Graph = new LogGraph(schedulers, _repository, branchesObservable)
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

            Diff = new DiffViewModel(_repository, schedulers, _repository.Head, secondarySelectedCommit, selectedCommit, compareOptions)
                .DisposeWith(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
