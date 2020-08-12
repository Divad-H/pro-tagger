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
        public string CanonicalName { get; }
        public string FriendlyName { get; }
        public bool Selected { get; }

        public RefSelection (string canonicalName, string friendlyName, bool selected)
        {
            CanonicalName = canonicalName;
            FriendlyName = friendlyName;
            Selected = selected;
        }
    }

    public class RefSelectionViewModel : IDisposable
    {
        public string CanonicalName { get; }
        public string FriendlyName { get; }
        public ViewSubject<bool> Selected { get; }
        public ReactiveCommand<bool, bool> SelectCommand { get; }

        public IObservable<RefSelection> RefSelectionObservable { get; }

        public RefSelectionViewModel(string friendlyName, string canonicalName, bool selected, ISchedulers schedulers)
        {
            Selected = new ViewSubject<bool>(selected)
                .DisposeWith(_disposables);
            CanonicalName = canonicalName;
            FriendlyName = friendlyName;
            SelectCommand = ReactiveCommand.Create<bool, bool>(p => p, schedulers.Dispatcher)
                .DisposeWith(_disposables);
            RefSelectionObservable = SelectCommand
                .Select(s => new RefSelection(canonicalName, friendlyName, s))
                .StartWith(new RefSelection(canonicalName, friendlyName, selected));
        }
        public RefSelectionViewModel(Branch branch, bool selected, ISchedulers schedulers)
            : this(branch.FriendlyName, branch.CanonicalName, selected, schedulers)
        {}

        public RefSelectionViewModel(Tag tag, bool selected, ISchedulers schedulers)
            : this(tag.FriendlyName, tag.CanonicalName, selected, schedulers)
        { }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }

    public class RefsViewModel : IDisposable
    {
        public IList<RefSelectionViewModel> Refs { get; }

        /// <summary>
        /// View => ViewModel
        /// </summary>
        public ICommand SelectAllRefsCommand { get; }

        /// <summary>
        /// ViewModel => View
        /// </summary>
        public ViewSubject<bool?> AllRefsSelected { get; }
        public bool HeadSelected { get; private set; }

        public IObservable<IList<RefSelection>> SelectedRefs { get; }

        private enum SelectAllType
        {
            Undetermined,
            AllSelected,
            NoneSelected,
        }

        public RefsViewModel(List<RefSelectionViewModel> refs, string? headCanonicalName, ISchedulers schedulers)
        {
            Refs = refs;

            var selectAllRefsCommand = ReactiveCommand.Create<bool?, bool?>(isChecked => isChecked, schedulers.Dispatcher);
            SelectAllRefsCommand = selectAllRefsCommand;

            AllRefsSelected = new ViewSubject<bool?>(refs.All(r => r.Selected.Value) ? true : (refs.Any(r => r.Selected.Value) ? null : (bool?)false));
            
            var singleSelections = refs
                .Select((r, i) => r.RefSelectionObservable
                    .Select(rs => new { rs, i }))
                .Merge()
                .Select(s => new Variant<Tuple<RefSelection, int>, SelectAllType>(Tuple.Create(s.rs, s.i)))
                .Merge(selectAllRefsCommand
                    .StartWith(AllRefsSelected.Value)
                    .Select(sa => new Variant<Tuple<RefSelection, int>, SelectAllType>(!sa.HasValue ? SelectAllType.Undetermined : sa.Value ? SelectAllType.AllSelected : SelectAllType.NoneSelected)))
                .Scan(
                    Tuple.Create(refs.Select(_ => (RefSelection?)null).ToList(), SelectAllType.Undetermined),
                    (last, current) =>  current.Visit(
                        newSelection =>
                        {
                            var res = last.Item2 == SelectAllType.Undetermined ? last.Item1.ToList()
                                : last.Item1.Select(rs => rs is null ? null : new RefSelection(rs.CanonicalName, rs.FriendlyName, last.Item2 == SelectAllType.AllSelected)).ToList();
                            res[newSelection.Item2] = newSelection.Item1;
                            return Tuple.Create(res, SelectAllType.Undetermined);
                        },
                        sa => Tuple.Create(last.Item1, sa == SelectAllType.Undetermined && last.Item1.All(r => r?.Selected ?? false) ? SelectAllType.NoneSelected : sa)))
                .Select(x => x.Item1.ToList())
                .SkipManyNull<RefSelection, List<RefSelection?>, List<RefSelection>>();

            var selections = singleSelections
                .StartWith(refs
                    .Select(r => new RefSelection(r.CanonicalName, r.FriendlyName, r.Selected.Value))
                    .ToList())
                .Merge(selectAllRefsCommand
                    .WithLatestFrom(singleSelections, (isChecked, refSelections) => new { isChecked, refSelections })
                    .Select(d =>
                    {
                        var isChecked = (d.isChecked is bool) ? d.isChecked : (d.refSelections.All(r => r.Selected) ? false : d.isChecked);
                        return d.refSelections.Select(@ref => new RefSelection(@ref.CanonicalName, @ref.FriendlyName, isChecked is null ? @ref.Selected : (bool)isChecked)).ToList();
                    }));

            static bool? allSelected(IList<RefSelection> s)
                => s.All(r => r.Selected) ? true : (s.Any(r => r.Selected) ? null : (bool?)false);

            selections
                .Select(allSelected)
                .Subscribe(AllRefsSelected)
                .DisposeWith(_disposables);

            selections
                .Select(l => l
                    .Where(r => r.CanonicalName == headCanonicalName)
                    .FirstOrDefault())
                .Where(r => !(r is null))
                .Select(r => r.Selected)
                .Subscribe(s => HeadSelected = s)
                .DisposeWith(_disposables);

            SelectedRefs = selections;

            foreach (var @ref in refs)
                selections
                    .Select(s => s
                        .Where(r => r.CanonicalName == @ref.CanonicalName)
                        .FirstOrDefault())
                    .Distinct()
                    .SkipNull()
                    .Select(r => r.Selected)
                    .Subscribe(@ref.Selected)
                    .DisposeWith(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
        {
            _disposables.Dispose();
            foreach (var @ref in Refs)
                @ref.Dispose();
        }
    }

    public class AllRefsViewModel : IDisposable
    {
        public RefsViewModel Branches { get; }
        public RefsViewModel Tags { get; }

        public AllRefsViewModel(List<RefSelectionViewModel> branches, List<RefSelectionViewModel> tags, string? headCanonicalName, ISchedulers schedulers)
        {
            Branches = new RefsViewModel(branches, headCanonicalName, schedulers)
                .DisposeWith(_disposables);
            Tags = new RefsViewModel(tags, null, schedulers)
                .DisposeWith(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public void Dispose()
            => _disposables.Dispose();
    }

    public class RepositoryViewModel : IDisposable
    {
        public LogGraph Graph { get; }

        public ViewSubject<AllRefsViewModel?> References { get; }
        public DiffViewModel Diff { get; }
        public RepositoryDescription RepositoryDescription { get; }
        public ICommand RefreshCommand { get; }

        public static async Task<Variant<RepositoryViewModel, RepositoryError>> Create(ISchedulers schedulers,
            CancellationToken ct, 
            IRepositoryFactory repositoryFactory,
            IFileSystem fileSystem,
            RepositoryDescription description, 
            IObservable<CompareOptions> compareOptions)
        {
            try
            {
                var repositoryViewModel = await Task.Run(() => new RepositoryViewModel(schedulers, repositoryFactory, fileSystem, description, compareOptions));
                if (ct.IsCancellationRequested)
                {
                    repositoryViewModel?.Dispose();
                    return new RepositoryError("Cancelled", description);
                }
                return repositoryViewModel;
            }
            catch (Exception e)
            {
                return new RepositoryError(e.Message, description);
            }
        }

        public RepositoryViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, IFileSystem fileSystem, RepositoryDescription description, IObservable<CompareOptions> compareOptions)
        {
            try
            {
                RepositoryDescription = description;

                var refreshCommand = ReactiveCommand.Create<object?, object?>(p => p, schedulers.Dispatcher);
                RefreshCommand = refreshCommand;

                var repositoryObservable = Observable
                    .Create<IRepositoryWrapper>(o =>
                    {
                        var serial = new SerialDisposable();
                        return new CompositeDisposable(
                            refreshCommand
                                .StartWith((object?)null)
                                .Merge(new FileSystemObservable(System.IO.Directory.GetParent(RepositoryDescription.Path).FullName, "", fileSystem)
                                    .Throttle(TimeSpan.FromMilliseconds(250), schedulers.Dispatcher))
                                .Select(_ => repositoryFactory.CreateRepository(description.Path))
                                .Do(x => serial.Disposable = x)
                                .Subscribe(o),
                            serial);
                    })
                    .Publish();

                List<RefSelectionViewModel> createBranchVMs(IRepositoryWrapper repository, IList<RefSelectionViewModel>? lastBranchSelection, bool lastHeadSelected)
                    => repository.Branches
                        .Select(branch => new RefSelectionViewModel(branch, lastBranchSelection is null
                            ? branch.IsCurrentRepositoryHead
                            : lastBranchSelection.Any(b => b.Selected.Value && b.CanonicalName == branch.CanonicalName), schedulers))
                        .Concat(repository.Head.Yield().Where(b => repository.Info.IsHeadDetached && !(b is null))
                            .Select(head => new RefSelectionViewModel("Detached HEAD", head!.Reference.CanonicalName, lastHeadSelected, schedulers)))
                        .ToList();

                List<RefSelectionViewModel> createTagVMs(IRepositoryWrapper repository, IList<RefSelectionViewModel>? lastTagSelection)
                    => repository.Tags
                        .Select(tag => new RefSelectionViewModel(tag, lastTagSelection is null
                            || lastTagSelection.Any(b => b.Selected.Value && b.CanonicalName == tag.CanonicalName), schedulers))
                        .ToList();

                var refVmsSource = repositoryObservable
                    .Select(repository => new { repository, refs = new AllRefsViewModel(createBranchVMs(repository, null, true),
                        createTagVMs(repository, null),
                        repository.Info.IsHeadDetached ? repository.Head?.Reference.CanonicalName : repository.Head?.CanonicalName,
                        schedulers)
                    })
                    .Scan((last, current)
                        => new {
                            current.repository,
                            refs = new AllRefsViewModel(createBranchVMs(current.repository, last.refs?.Branches?.Refs, last?.refs?.Branches?.HeadSelected ?? true),
                               createTagVMs(current.repository, last?.refs?.Tags?.Refs),
                               current.repository.Info.IsHeadDetached ? current.repository.Head?.Reference.CanonicalName : current.repository.Head?.CanonicalName,
                               schedulers) })
                    .Select(current => current.refs);

                var refVms = Observable
                    .Create<AllRefsViewModel>(o =>
                    {
                        var serial = new SerialDisposable();
                        return new CompositeDisposable(
                            refVmsSource.Do(x => serial.Disposable = x).Subscribe(o),
                            serial);
                    })
                    .Publish();

                References = new ViewSubject<AllRefsViewModel?>(null);
                refVms
                    .Subscribe(References)
                    .DisposeWith(_disposables);

                var logGraphInput = refVms
                    .Select(d => d.Branches.SelectedRefs
                        .CombineLatest(d.Tags.SelectedRefs, (bs, ts) => bs.Concat(ts).ToList()))
                    .Switch()
                    .WithLatestFrom(repositoryObservable, (refSelection, repository) => new LogGraphInput(repository, refSelection));

                Graph = new LogGraph(schedulers, logGraphInput)
                    .DisposeWith(_disposables);
            
                var selectedCommit = Graph.SelectedNode
                    .Select(node => node?.Commit);
                var secondarySelectedCommit = Graph.SecondarySelectedNode
                    .Select(node => node?.Commit);

                var diffViewModelInput = Observable
                    .CombineLatest(repositoryObservable, secondarySelectedCommit, selectedCommit, compareOptions,
                        (repo, oldCommit, newCommit, co) => new DiffViewModelInput(repo, oldCommit, newCommit, co));

                Diff = new DiffViewModel(schedulers, diffViewModelInput)
                    .DisposeWith(_disposables);

                refVms
                    .Connect()
                    .DisposeWith(_disposables);

                repositoryObservable
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
