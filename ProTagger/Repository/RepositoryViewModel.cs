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
    public class RefSelection
    {
        public string LongName { get; }
        public string PrettyName { get; }
        public bool Selected { get; }

        public RefSelection (string longName, string prettyName, bool selected)
        {
            LongName = longName;
            PrettyName = prettyName;
            Selected = selected;
        }
    }

    public class RefSelectionViewModel
    {
        public readonly RefSelection RefSelection;
        public ViewObservable<bool> Selected { get; }
        public string PrettyName => RefSelection.PrettyName;

        public IObservable<RefSelection> RefSelectionObservable { get; }

        public RefSelectionViewModel(Branch branch, bool selected)
        {
            Selected = new ViewObservable<bool>(selected);
            RefSelection = new RefSelection(branch.CanonicalName, branch.FriendlyName, selected);
            RefSelectionObservable = Selected
                .Select(selected => new RefSelection(branch.CanonicalName, branch.FriendlyName, selected));
        }

        public RefSelectionViewModel(Tag tag, bool selected)
        {
            Selected = new ViewObservable<bool>(selected);
            RefSelection = new RefSelection(tag.CanonicalName, tag.FriendlyName, selected);
            RefSelectionObservable = Selected
                .Select(selected => new RefSelection(tag.CanonicalName, tag.FriendlyName, selected));
        }
    }

    public class RepositoryViewModel : IDisposable
    {
        public LogGraph Graph { get; }

        public ViewSubject<IList<RefSelectionViewModel>> Branches { get; }
        public ViewSubject<IList<RefSelectionViewModel>> Tags { get; }
        public IObservable<IList<RefSelection>> RefsObservable { get; }
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

                IList<RefSelectionViewModel> createBranchesVM(IList<RefSelectionViewModel>? lastBranchSelection)
                    => _repository.Branches
                        .Select(branch => new RefSelectionViewModel(branch, lastBranchSelection is null
                            ? branch.IsCurrentRepositoryHead
                            : lastBranchSelection.Any(b => b.Selected.Value && b.RefSelection.LongName == branch.CanonicalName)))
                        .ToList();

                IList<RefSelectionViewModel> createTagsVM(IList<RefSelectionViewModel>? lastTagSelection)
                    => _repository.Tags
                        .Select(tag => new RefSelectionViewModel(tag, lastTagSelection is null
                            || lastTagSelection.Any(b => b.Selected.Value && b.RefSelection.LongName == tag.CanonicalName)))
                        .ToList();

                var firstBranchVms = createBranchesVM(null);
                var firstTagVms = createTagsVM(null);

                var nextRefVms = refreshCommand
                    .Scan(new { branchVMs = firstBranchVms, tagVMs = firstTagVms }, (last, _) => new { branchVMs = createBranchesVM(last.branchVMs), tagVMs = createTagsVM(last.tagVMs) })
                    .Publish();

                var refsVmObservable = Observable
                    .Return(new { branchVMs = firstBranchVms, tagVMs = firstTagVms })
                    .Concat(nextRefVms);

                var selectedRefsObservable = refsVmObservable
                    .Select(d => d.branchVMs
                        .Select(b => b.RefSelectionObservable)
                        .Concat(d.tagVMs
                            .Select(t => t.RefSelectionObservable))
                        .CombineLatest())
                    .Switch();

                Branches = new ViewSubject<IList<RefSelectionViewModel>>(firstBranchVms);
                Tags = new ViewSubject<IList<RefSelectionViewModel>>(firstTagVms);

                refsVmObservable
                    .Select(refs => refs.branchVMs)
                    .Subscribe(Branches)
                    .DisposeWith(_disposables);

                refsVmObservable
                    .Select(refs => refs.tagVMs)
                    .Subscribe(Tags)
                    .DisposeWith(_disposables);

                RefsObservable = selectedRefsObservable;

                Graph = new LogGraph(schedulers, _repository, selectedRefsObservable)
                    .DisposeWith(_disposables);
            
                var selectedCommit = Graph.SelectedNode
                    .Select(node => node?.Commit);
                var secondarySelectedCommit = Graph.SecondarySelectedNode
                    .Select(node => node?.Commit);

                Diff = new DiffViewModel(_repository, schedulers, _repository.Head, secondarySelectedCommit, selectedCommit, compareOptions)
                    .DisposeWith(_disposables);

                nextRefVms
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
