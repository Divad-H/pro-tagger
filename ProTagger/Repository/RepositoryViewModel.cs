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

    public class RefSelectionViewModel : IDisposable
    {
        public string LongName { get; }
        public string PrettyName { get; }
        public ViewSubject<bool> Selected { get; }
        public ReactiveCommand<bool, bool> SelectCommand { get; }

        public IObservable<RefSelection> RefSelectionObservable { get; }

        private RefSelectionViewModel(string friendlyName, string canonicalName, bool selected, ISchedulers schedulers)
        {
            Selected = new ViewSubject<bool>(selected)
                .DisposeWith(_disposables);
            LongName = canonicalName;
            PrettyName = friendlyName;
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

        public IObservable<IList<RefSelection>> SelectedRefs { get; }

        private enum SelectAllType
        {
            Undetermined,
            AllSelected,
            NoneSelected,
            NoValue,
        }

        public RefsViewModel(IList<RefSelectionViewModel> refs, ISchedulers schedulers)
        {
            Refs = refs;

            var selectAllRefsCommand = ReactiveCommand.Create<bool?, bool?>(isChecked => isChecked, schedulers.Dispatcher);
            SelectAllRefsCommand = selectAllRefsCommand;

            AllRefsSelected = new ViewSubject<bool?>(refs.All(r => r.Selected.Value) ? true : (refs.Any(r => r.Selected.Value) ? null : (bool?)false));
            static bool? allSelected(IList<RefSelection> s)
                => s.All(r => r.Selected) ? true : (s.Any(r => r.Selected) ? null : (bool?)false);

            var singleSelectionA = refs
                .Select((r, i) => r.RefSelectionObservable
                    .Select(rs => new { rs, i }))
                .Merge();

            var singleSelections = singleSelectionA
                .Select(s => new Variant<Tuple<RefSelection, int>, SelectAllType>(Tuple.Create(s.rs, s.i)))
                .Merge(selectAllRefsCommand
                    .StartWith(AllRefsSelected.Value)
                    .Select(sa => new Variant<Tuple<RefSelection, int>, SelectAllType>(!sa.HasValue ? SelectAllType.Undetermined : sa.Value ? SelectAllType.AllSelected : SelectAllType.NoneSelected)))
                .Scan(
                    Tuple.Create(refs
                        .Select(_ => (RefSelection?)null)
                        .ToList(), SelectAllType.NoValue),
                    (last, current) =>
                    {
                        return current.Visit(
                            newSelection =>
                            {
                                var res = last.Item2 == SelectAllType.AllSelected ? last.Item1.Select<RefSelection?, RefSelection?>(rs => new RefSelection(rs!.LongName, rs.PrettyName, true)).ToList()
                                    : last.Item2 == SelectAllType.NoneSelected ? last.Item1.Select<RefSelection?, RefSelection?>(rs => new RefSelection(rs!.LongName, rs.PrettyName, false)).ToList()
                                        : last.Item1.ToList();
                                res[newSelection.Item2] = newSelection.Item1;
                                return Tuple.Create(res, SelectAllType.NoValue);
                            },
                            sa => Tuple.Create(last.Item1, sa == SelectAllType.Undetermined && last.Item1.All(r => r!.Selected) ? SelectAllType.NoneSelected : sa));
                    })
                .Select(x => x.Item1.ToList())
                .SkipManyNull<RefSelection, List<RefSelection?>, List<RefSelection>>();

            var selections = singleSelections
                .StartWith(refs
                    .Select(r => new RefSelection(r.LongName, r.PrettyName, r.Selected.Value))
                    .ToList())
                .Merge(selectAllRefsCommand
                    .WithLatestFrom(singleSelections, (isChecked, refSelections) => new { isChecked, refSelections })
                    .Select(d =>
                    {
                        var isChecked = (d.isChecked is bool) ? d.isChecked : (d.refSelections.All(r => r.Selected) ? false : d.isChecked);
                        return d.refSelections.Select(@ref => new RefSelection(@ref.LongName, @ref.PrettyName, isChecked is null ? @ref.Selected : (bool)isChecked)).ToList();
                    }));

            selections
                .Select(allSelected)
                .Subscribe(AllRefsSelected)
                .DisposeWith(_disposables);

            SelectedRefs = selections;

            foreach (var @ref in refs)
                selections
                    .Select(s => s
                        .Where(r => r.LongName == @ref.LongName)
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

        public AllRefsViewModel(IList<RefSelectionViewModel> branches, IList<RefSelectionViewModel> tags, ISchedulers schedulers)
        {
            Branches = new RefsViewModel(branches, schedulers)
                .DisposeWith(_disposables);
            Tags = new RefsViewModel(tags, schedulers)
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


                IList<RefSelectionViewModel> createBranchVMs(IList<RefSelectionViewModel>? lastBranchSelection)
                    => _repository.Branches
                        .Select(branch => new RefSelectionViewModel(branch, lastBranchSelection is null
                            ? branch.IsCurrentRepositoryHead
                            : lastBranchSelection.Any(b => b.Selected.Value && b.LongName == branch.CanonicalName), schedulers))
                        .ToList();

                IList<RefSelectionViewModel> createTagVMs(IList<RefSelectionViewModel>? lastTagSelection)
                    => _repository.Tags
                        .Select(tag => new RefSelectionViewModel(tag, lastTagSelection is null
                            || lastTagSelection.Any(b => b.Selected.Value && b.LongName == tag.CanonicalName), schedulers))
                        .ToList();

                var firstRefsVM = new AllRefsViewModel(createBranchVMs(null), createTagVMs(null), schedulers);

                var nextRefVmsSource = refreshCommand
                    .Scan(firstRefsVM, (last, _)
                        => new AllRefsViewModel(createBranchVMs(last.Branches.Refs), createTagVMs(last.Tags.Refs), schedulers));

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
