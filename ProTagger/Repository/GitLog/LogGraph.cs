using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveMvvm;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReacitveMvvm;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ProTagger.Repository.GitLog
{
    using TGraphPos = UInt16;
    using GraphType = ObservableCollection<LogGraphNode>;
    public class LogGraphNode
    {
        /// <summary>
        /// Contains two lists that describe the column the edges point to (column of the parent commit)
        /// 
        /// Note that the starting point of the edges is not described here.
        /// </summary>
        public struct DownwardDirections
        {
            /// <summary>
            /// Directions of the previous LogGraphNode
            /// </summary>
            public List<TGraphPos> Previous;
            /// <summary>
            /// Directions of the current LogGraphNode
            /// </summary>
            public List<TGraphPos> Next;
        }

        public readonly struct BranchInfo
        {
            public BranchInfo(string canonicalName, string friendlyName, bool isRemote, bool isHead) =>
                (CanonicalName, FirendlyName, IsRemote, IsHead) = (canonicalName, friendlyName, isRemote, isHead);
            
            public string CanonicalName { get; }
            public string FirendlyName { get; }
            public bool IsRemote { get; }
            public bool IsHead { get; }
        }

        public readonly struct TagInfo
        {
            public TagInfo(string canonicalName, string friendlyName)
                => (CanonicalName, FirendlyName) = (canonicalName, friendlyName);

            public string CanonicalName { get; }
            public string FirendlyName { get; }
        }

        public LogGraphNode(
            TGraphPos graphPosition, 
            IList<DownwardDirections> directions,
            Variant<Commit, DiffTargets> commit, 
            bool isMerge, 
            IList<BranchInfo> branches,
            IList<TagInfo> tags,
            bool isDetachedHead)
        {
            GraphPosition = graphPosition;
            Directions = directions;
            IsMerge = isMerge;
            Branches = branches;
            Tags = tags;
            Commit = commit;
            IsDetachedHead = isDetachedHead;
        }

        public TGraphPos GraphPosition { get; }
        public IList<DownwardDirections> Directions { get; }
        public string MessageShort
            => Commit.Visit(commit => commit.MessageShort, diffTarget => "Working tree");
        public string Message
            => Commit.Visit(commit => commit.Message, diffTarget => "Working tree");
        public bool IsMerge { get; }
        public string Sha
            => Commit.Visit(commit => commit.Sha, diffTarget => string.Empty);
        public string ShortSha
            => Commit.Visit(commit => commit.Sha.Substring(0, 7), diffTarget => string.Empty);
        public IList<BranchInfo> Branches { get; }
        public IList<TagInfo> Tags { get; }
        public Variant<Commit, DiffTargets> Commit;
        public bool IsDetachedHead { get; }
    }

    public class LogGraph : IDisposable
    {
        public class NoBranchSelected : Exception
        {
            public NoBranchSelected(string message)
                : base(message)
            { }
        }

        private struct NodesData
        {
            public int Index;
            public IList<LogGraphNode> Batch;
        }

        public ICommand ScrolledBottom { get; }

        public ViewSubject<Variant<GraphType, Unexpected>> LogGraphNodes { get; }

        public ViewSubject<LogGraphNode?> SelectedNode { get; }

        public ViewSubject<LogGraphNode?> SecondarySelectedNode { get; }

        public Func<object, object?, bool> KeepSelectionRule { get; } = (object1, object2) =>
            {
                if (object2 is null)
                    return false;
                var node1 = object1 as LogGraphNode ?? throw new InvalidOperationException("Invalid type.");
                var node2 = object2 as LogGraphNode ?? throw new InvalidOperationException("Invalid type.");
                return node1.Sha == node2.Sha;
            };

        public LogGraph(ISchedulers schedulers, IRepositoryWrapper repository, IObservable<IList<RefSelection>> selectedBranches)
        {
            const int chunkSize = 1000;
            
            var scrolledBottom = ReactiveCommand.Create<object?, object?>(p => p, schedulers.Dispatcher);
            ScrolledBottom = scrolledBottom;

            LogGraphNodes = new ViewSubject<Variant<GraphType, Unexpected>>(new Variant<GraphType, Unexpected>(new GraphType()))
                .DisposeWith(_disposable);

            SelectedNode = new ViewSubject<LogGraphNode?>(null)
                .DisposeWith(_disposable);
            SecondarySelectedNode = new ViewSubject<LogGraphNode?>(null)
                .DisposeWith(_disposable);

            selectedBranches
                .Select(branches => branches.Where(branch => branch.Selected))
                .WithLatestFrom(LogGraphNodes, (branches, oldNodes) =>
                {
                    var firstChunk = oldNodes.Visit(
                        graph => Math.Max(chunkSize, graph.Count),
                        _ => chunkSize);
                    return CreateGraph(repository, branches!)
                        .ToObservable(schedulers.ThreadPool)
                        .Select((node, i) => new { node, i })
                        .GroupBy(data => data.i < firstChunk)
                        .Select(g => g.Buffer(g.Key ? firstChunk : chunkSize))
                        .Switch()
                        .Zip(Observable.Return<object?>(null).Concat(scrolledBottom), (data, _) => data.Select(data => data.node).ToList())
                        .Scan(
                            new NodesData() { Index = 0, Batch = new GraphType() },
                            (last, current) => new NodesData() { Index = last.Index + 1, Batch = current })
                        .Select(data => new Variant<NodesData, string>(data))
                        .Catch((Exception e) => Observable.Return(new Variant<NodesData, string>(e.Message)))
                        .ObserveOn(schedulers.Dispatcher);
                })
                .Switch()
                .Subscribe(var => var.Visit(
                    data =>
                    {
                        if (data.Index == 1)
                            LogGraphNodes.OnNext(new Variant<GraphType, Unexpected>(new GraphType(data.Batch)));
                        else
                            foreach (var node in data.Batch)
                                LogGraphNodes.Value.First.Add(node);
                    },
                    error => LogGraphNodes.OnNext(new Variant<GraphType, Unexpected>(new Unexpected(error)))))
                .DisposeWith(_disposable);
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
            => _disposable.Dispose();

        internal static IEnumerable<LogGraphNode> CreateGraph(
            IRepositoryWrapper repository, 
            IEnumerable<RefSelection> Refs)
        {
            using var delayDispose = repository.TryAddRef();
            if (delayDispose != null)
            {
                if (repository.Head is null)
                    yield break;

                var selectedRefs = Refs
                    .Select(@ref => repository.References[@ref.CanonicalName])
                    .Where(@ref => !(@ref is null))
                    .ToList();
                var allTags = repository.Tags.ToList();
                var allBranches = repository.Branches.ToList();
                var detachedHead = repository.Info.IsHeadDetached ? repository.Head : null;

                var commitFilter = new CommitFilter()
                {
                    SortBy = CommitSortStrategies.Topological,
                    IncludeReachableFrom = selectedRefs
                };
                var query = repository.QueryCommits(commitFilter);

                var head = repository.Head;
                bool headSelected = selectedRefs.Any(@ref => @ref.CanonicalName == head.CanonicalName);
                var firstCommit = headSelected ? null : query.FirstOrDefault();
                if (!headSelected && firstCommit is null)
                    throw new NoBranchSelected("No branch is selected.");

                var expectedIds = firstCommit is null ? new List<ObjectId?>() { repository.Head.Tip.Id } : firstCommit.Parents.Select(c => c.Id).ToList<ObjectId?>();

                var directions = new List<List<TGraphPos>>() { headSelected ? new List<TGraphPos>() { 0 } : expectedIds.Select((_, i) => (TGraphPos)i).ToList() };
                for (int i = 1; i < directions.First().Count; ++i)
                    directions.Add(new List<TGraphPos>());
                var lastDirections = new List<List<TGraphPos>>();

                TGraphPos lastPosition = 0;
                Variant<Commit, DiffTargets> lastCommit = firstCommit is null ? new Variant<Commit, DiffTargets>(DiffTargets.WorkingDirectory) : new Variant<Commit, DiffTargets>(firstCommit);
                bool lastMerge = expectedIds.Count > 1;

                foreach (Commit c in query.Skip(headSelected ? 0 : 1))
                {
                    var foundNextPosition = expectedIds.FindIndex((id) => id == c.Id);
                    if (foundNextPosition == -1)
                    {
                        // commit without visible children
                        foundNextPosition = expectedIds.FindIndex((id) => id is null);
                        if (foundNextPosition == -1)
                        {
                            foundNextPosition = expectedIds.Count;
                            expectedIds.Add(null);
                            directions.Add(new List<TGraphPos>());
                        }
                    }
                    TGraphPos nextPosition = (TGraphPos)foundNextPosition;

                    var currentDirections = directions;
                    directions = currentDirections.Select((dir) => dir.Take(0).ToList()).ToList();
                    for (TGraphPos i = 0; i < directions.Count; ++i)
                        if (expectedIds[i] != null)
                            directions[i].Add(i);
                    for (TGraphPos i = 0; i < currentDirections.Count; ++i)
                    {
                        if (expectedIds[i] == c.Id && i != nextPosition)
                        {
                            foreach (var direction in currentDirections)
                                if (direction.Remove(i))
                                    direction.Add(nextPosition);
                            directions[i].Clear();
                        }
                    }

                    var parents = c.Parents.ToList();
                    TGraphPos parentIndex = 0;

                    for (TGraphPos i = 0; i < expectedIds.Count; ++i)
                    {
                        if (expectedIds[i] == c.Id || expectedIds[i] is null)
                        {
                            if (parents.Count > parentIndex)
                            {
                                TGraphPos indexToChange = i;
                                if (expectedIds[i] is null && expectedIds[nextPosition] == c.Id)
                                {
                                    indexToChange = nextPosition;
                                    --i;
                                }
                                expectedIds[indexToChange] = parents[parentIndex++].Id;
                                if (!directions[nextPosition].Contains(indexToChange))
                                    directions[nextPosition].Add(indexToChange);
                            }
                            else
                            {
                                expectedIds[i] = null;
                                if (parents.Count == 0)
                                    directions[nextPosition].Clear();
                            }
                        }
                    }

                    for (; parentIndex < parents.Count; ++parentIndex)
                    {
                        expectedIds.Add(parents[parentIndex].Id);
                        directions.Add(new List<TGraphPos>());
                        directions[nextPosition].Add((TGraphPos)(directions.Count - 1));
                    }
                    yield return new LogGraphNode(
                        lastPosition,
                        CreateGraphDirections(lastDirections, currentDirections),
                        lastCommit,
                        lastMerge,
                        allBranches
                            .Where(branch => lastCommit.Is<Commit>() && branch.Tip == lastCommit.Get<Commit>())
                            .Select(CreateBranchInfo)
                            .ToList(),
                        allTags
                            .Where(tag => lastCommit.Is<Commit>() && tag.Target == lastCommit.Get<Commit>())
                            .Select(CreateTagInfo)
                            .ToList(),
                        !(detachedHead is null) && lastCommit.Is<Commit>() && lastCommit.Get<Commit>() == detachedHead.Tip);
                    lastDirections = currentDirections;
                    lastPosition = nextPosition;
                    lastCommit = new Variant<Commit, DiffTargets>(c);
                    lastMerge = parents.Count > 1;
                }
                yield return new LogGraphNode(
                    lastPosition,
                    CreateGraphDirections(lastDirections, new List<List<TGraphPos>>()),
                    lastCommit,
                    false,
                    allBranches
                        .Where(branch => lastCommit.Is<Commit>() && branch.Tip == lastCommit.Get<Commit>())
                        .Select(CreateBranchInfo)
                        .ToList(),
                    allTags
                        .Where(tag => lastCommit.Is<Commit>() && tag.Target == lastCommit.Get<Commit>())
                        .Select(CreateTagInfo)
                        .ToList(),
                    !(detachedHead is null) && lastCommit.Is<Commit>() && lastCommit.Get<Commit>() == detachedHead.Tip);
            }
        }

        private static List<LogGraphNode.DownwardDirections> CreateGraphDirections(IEnumerable<List<TGraphPos>> lastDirections, IEnumerable<List<TGraphPos>> nextDirections)
            => lastDirections
                .GreaterZip(nextDirections, (first, second)
                    => new LogGraphNode.DownwardDirections() { Previous = first, Next = second }, new List<TGraphPos>(), new List<TGraphPos>())
                .ToList();

        private static LogGraphNode.BranchInfo CreateBranchInfo(Branch branch)
            => new LogGraphNode.BranchInfo(
                branch.CanonicalName,
                branch.FriendlyName,
                branch.IsRemote,
                branch.IsCurrentRepositoryHead
            );

        private static LogGraphNode.TagInfo CreateTagInfo(Tag tag)
            => new LogGraphNode.TagInfo(tag.CanonicalName, tag.FriendlyName);
    }
}
