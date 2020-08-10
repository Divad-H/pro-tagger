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

        public ViewSubject<AllRefsViewModel> References { get; }
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

        private readonly IRepositoryWrapper _repository;

        public RepositoryViewModel(ISchedulers schedulers, IRepositoryFactory repositoryFactory, IFileSystem fileSystem, RepositoryDescription description, IObservable<CompareOptions> compareOptions)
        {
            try
            {
                RepositoryDescription = description;
                _repository = repositoryFactory.CreateRepository(description.Path)
                    .DisposeWith(_disposables);

                var refreshCommand = ReactiveCommand.Create<object?, object?>(p => p, schedulers.Dispatcher);
                RefreshCommand = refreshCommand;

                List<RefSelectionViewModel> createBranchVMs(IList<RefSelectionViewModel>? lastBranchSelection, bool lastHeadSelected)
                    => _repository.Branches
                        .Select(branch => new RefSelectionViewModel(branch, lastBranchSelection is null
                            ? branch.IsCurrentRepositoryHead
                            : lastBranchSelection.Any(b => b.Selected.Value && b.CanonicalName == branch.CanonicalName), schedulers))
                        .Concat(_repository.Head.Yield().Where(b => _repository.Info.IsHeadDetached && !(b is null))
                            .Select(head => new RefSelectionViewModel("Detached HEAD", head!.Reference.CanonicalName, lastHeadSelected, schedulers)))
                        .ToList();

                List<RefSelectionViewModel> createTagVMs(IList<RefSelectionViewModel>? lastTagSelection)
                    => _repository.Tags
                        .Select(tag => new RefSelectionViewModel(tag, lastTagSelection is null
                            || lastTagSelection.Any(b => b.Selected.Value && b.CanonicalName == tag.CanonicalName), schedulers))
                        .ToList();

                var firstRefsVM = new AllRefsViewModel(createBranchVMs(null, true),
                    createTagVMs(null),
                    _repository.Info.IsHeadDetached ? _repository.Head?.Reference.CanonicalName : _repository.Head?.CanonicalName,
                    schedulers);

                var refsFileSystemObservable
                    = new RefFileSystemObservable(RepositoryDescription.Path, fileSystem);

                var nextRefVmsSource = refreshCommand
                    .Merge(refsFileSystemObservable
                        .Throttle(TimeSpan.FromMilliseconds(100), schedulers.Dispatcher))
                    .Scan(firstRefsVM, (last, _)
                        => new AllRefsViewModel(createBranchVMs(last.Branches.Refs, last.Branches.HeadSelected),
                            createTagVMs(last.Tags.Refs),
                            _repository.Info.IsHeadDetached ? _repository.Head?.Reference.CanonicalName : _repository.Head?.CanonicalName,
                            schedulers));

                var nextRefVms = Observable
                    .Create<AllRefsViewModel>(o =>
                    {
                        var serial = new SerialDisposable();
                        return new CompositeDisposable(
                            nextRefVmsSource.Do(x => serial.Disposable = x).Subscribe(o),
                            serial);
                    })
                    .Publish();

                References = new ViewSubject<AllRefsViewModel>(firstRefsVM);
                nextRefVms
                    .Subscribe(References)
                    .DisposeWith(_disposables);

                var refsVmObservable = Observable
                    .Return(firstRefsVM)
                    .Concat(nextRefVms);

                var selectedRefsObservable = refsVmObservable
                    .Select(d => d.Branches.SelectedRefs
                        .CombineLatest(d.Tags.SelectedRefs, (bs, ts) => bs.Concat(ts).ToList()))
                    .Switch();

                Graph = new LogGraph(schedulers, _repository, selectedRefsObservable)
                    .DisposeWith(_disposables);
            
                var selectedCommit = Graph.SelectedNode
                    .Select(node => node?.Commit);
                var secondarySelectedCommit = Graph.SecondarySelectedNode
                    .Select(node => node?.Commit);

                Diff = new DiffViewModel(_repository, schedulers, secondarySelectedCommit, selectedCommit, compareOptions)
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
