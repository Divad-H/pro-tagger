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
using System.Windows.Input;

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
        public readonly BranchSelection BranchSelection;
        public ViewObservable<bool> Selected { get; }
        public string PrettyName => BranchSelection.PrettyName;

        public IObservable<BranchSelection> BranchSelectionObservable { get; }

        public BranchSelectionViewModel(Branch branch, bool selected)
        {
            Selected = new ViewObservable<bool>(selected);
            BranchSelection = new BranchSelection(branch.CanonicalName, branch.FriendlyName, selected);
            BranchSelectionObservable = Selected
                .Select(selected => new BranchSelection(branch.CanonicalName, branch.FriendlyName, selected));
        }
    }

    public class RepositoryViewModel : IDisposable
    {
        public LogGraph Graph { get; }

        public ViewSubject<IList<BranchSelectionViewModel>> Branches { get; }
        public IObservable<IList<BranchSelection>> BranchesObservable { get; }
        public DiffViewModel Diff { get; }
        public RepositoryDescription RepositoryDescription { get; }
        public ICommand RefreshCommand { get; }

        public static async Task<Variant<RepositoryViewModel, RepositoryError>> Create(ISchedulers schedulers,
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
                    return new Variant<RepositoryViewModel, RepositoryError>(new RepositoryError("Cancelled", description));
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
            try
            {
                RepositoryDescription = description;
                _repository = repositoryFactory.CreateRepository(description.Path)
                    .DisposeWith(_disposables);

                var refreshCommand = ReactiveCommand.Create<object?, object?>(p => p, schedulers.Dispatcher);
                RefreshCommand = refreshCommand;

                IList<BranchSelectionViewModel> createBranchesVM(IList<BranchSelectionViewModel>? lastBranchSelection)
                    => _repository.Branches
                        .Select(branch => new BranchSelectionViewModel(branch, lastBranchSelection is null
                            ? branch.IsCurrentRepositoryHead
                            : lastBranchSelection.Any(b => b.Selected.Value && b.BranchSelection.LongName == branch.CanonicalName)))
                        .ToList();

                var firstBranchVms = createBranchesVM(null);

                var nextBranchVms = refreshCommand
                    .Scan(firstBranchVms, (last, _) => createBranchesVM(last))
                    .Publish();

                var branchesVmObservable = Observable
                    .Return(firstBranchVms)
                    .Concat(nextBranchVms);

                var branchesObservable = branchesVmObservable
                    .Select(branches => branches
                        .Select(b => b.BranchSelectionObservable)
                        .CombineLatest())
                    .Switch();

                Branches = new ViewSubject<IList<BranchSelectionViewModel>>(firstBranchVms);

                branchesVmObservable
                    .Subscribe(Branches)
                    .DisposeWith(_disposables);

                BranchesObservable = branchesObservable;

                Graph = new LogGraph(schedulers, _repository, branchesObservable)
                    .DisposeWith(_disposables);
            
                var selectedCommit = Graph.SelectedNode
                    .Select(node => node?.Commit);
                var secondarySelectedCommit = Graph.SecondarySelectedNode
                    .Select(node => node?.Commit);

                Diff = new DiffViewModel(_repository, schedulers, _repository.Head, secondarySelectedCommit, selectedCommit, compareOptions)
                    .DisposeWith(_disposables);

                nextBranchVms
                    .Connect()
                    .DisposeWith(_disposables);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }
}
