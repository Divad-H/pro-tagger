﻿using LibGit2Sharp;
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

namespace ProTagger.Repo.GitLog
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
            public BranchInfo(string longName, string shortName, bool isRemote, bool isHead) =>
                (LongName, ShortName, IsRemote, IsHead) = (longName, shortName, isRemote, isHead);
            
            public string LongName { get; }
            public string ShortName { get; }
            public bool IsRemote { get; }
            public bool IsHead { get; }
        }

        public readonly struct TagInfo
        {
            public TagInfo(string longName, string shortName)
                => (LongName, ShortName) = (longName, shortName);

            public string LongName { get; }
            public string ShortName { get; }
        }

        public LogGraphNode(
            TGraphPos graphPosition, 
            IList<DownwardDirections> directions, 
            Commit commit, 
            bool isMerge, 
            IList<BranchInfo> branches,
            IList<TagInfo> tags)
        {
            GraphPosition = graphPosition;
            Directions = directions;
            MessageShort = commit.MessageShort;
            Message = commit.Message;
            IsMerge = isMerge;
            Sha = commit.Sha;
            ShortSha = Sha.Substring(0, 7);
            Author = commit.Author;
            Committer = commit.Committer;
            Branches = branches;
            Tags = tags;
            Commit = commit;
        }

        public TGraphPos GraphPosition { get; }
        public IList<DownwardDirections> Directions { get; }
        public string MessageShort { get; }
        public string Message { get; }
        public bool IsMerge { get; }
        public string Sha { get; }
        public string ShortSha { get; }
        public Signature Author { get; }
        public Signature Committer { get; }
        public IList<BranchInfo> Branches { get; }
        public IList<TagInfo> Tags { get; }
        public Commit Commit;
    }

    public class LogGraph : IDisposable
    {
        private struct NodesData
        {
            public int Index;
            public IList<LogGraphNode> Batch;
        }

        public ICommand ScrolledBottom { get; }

        public ViewSubject<Variant<GraphType, string>> LogGraphNodes { get; }

        public ViewSubject<LogGraphNode?> SelectedNode { get; }

        public ViewSubject<LogGraphNode?> SecondarySelectedNode { get; }

        public Func<object, object?, bool> KeepSelectionRule { get; } = (object1, object2) =>
            {
                if (object2 == null)
                    return false;
                var node1 = object1 as LogGraphNode ?? throw new InvalidOperationException("Invalid type.");
                var node2 = object2 as LogGraphNode ?? throw new InvalidOperationException("Invalid type.");
                return node1.Sha == node2.Sha;
            };

        public LogGraph(ISchedulers schedulers, IRepositoryWrapper repository, IObservable<IList<BranchSelection>> selectedBranches)
        {
            var scrolledBottom = ReactiveCommand.Create<object?, object?>(Observable.Return(true), p => p, schedulers.Dispatcher);
            ScrolledBottom = scrolledBottom;

            LogGraphNodes = new ViewSubject<Variant<GraphType, string>>(new Variant<GraphType, string>(new GraphType()))
                .DisposeWith(_disposable);

            selectedBranches
                .Select(branches => branches.Where(branch => branch.Selected))
                .Select(branches
                    => CreateGraph(repository, branches)
                        .ToObservable(schedulers.ThreadPool)
                        .Buffer(1000)
                        .Zip(Observable.Return<object?>(null).Concat(scrolledBottom), (data, _) => data)
                        .Scan(
                            new NodesData(){ Index = 0, Batch = new GraphType() },
                            (last, current) => new NodesData() { Index = last.Index + 1, Batch = current })
                        .Select(data => new Variant<NodesData, string>(data))
                        .Catch((Exception e) => Observable.Return(new Variant<NodesData, string>(e.Message)))
                        .ObserveOn(schedulers.Dispatcher))
                .Switch()
                .Subscribe(var => var.Visit(
                    data =>
                    {
                        if (data.Index == 1)
                            LogGraphNodes.OnNext(new Variant<GraphType, string>(new GraphType(data.Batch)));
                        else
                            foreach (var node in data.Batch)
                                LogGraphNodes.Value.First.Add(node);
                    },
                    error => LogGraphNodes.OnNext(new Variant<GraphType, string>(error))))
                .DisposeWith(_disposable);

            SelectedNode = new ViewSubject<LogGraphNode?>(null)
                .DisposeWith(_disposable);
            SecondarySelectedNode = new ViewSubject<LogGraphNode?>(null)
                .DisposeWith(_disposable);
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
            => _disposable.Dispose();

        internal static IEnumerable<LogGraphNode> CreateGraph(
            IRepositoryWrapper repository, 
            IEnumerable<BranchSelection> branches)
        {
            using var delayDispose = repository.TryAddRef();
            if (delayDispose != null)
            {
                var selectedBranches = branches.Select(branch => repository.Branches[branch.LongName]).ToList();
                var tags = repository.Tags.ToList();
                var expectedIds = new List<ObjectId?>();
                var directions = new List<List<TGraphPos>>();
                var lastDirections = new List<List<TGraphPos>>();

                TGraphPos? lastPosition = null;
                Commit? lastCommit = null;
                bool lastMerge = false;

                var commitFilter = new CommitFilter()
                {
                    SortBy = CommitSortStrategies.Topological,
                    IncludeReachableFrom = selectedBranches
                };

                foreach (Commit c in repository.QueryCommits(commitFilter)/*.Take(1000)*/)
                {
                    var foundNextPosition = expectedIds.FindIndex((id) => id == c.Id);
                    if (foundNextPosition == -1)
                    {
                        // commit without visible children
                        foundNextPosition = expectedIds.FindIndex((id) => id == null);
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
                        if (expectedIds[i] == c.Id || expectedIds[i] == null)
                        {
                            if (parents.Count > parentIndex)
                            {
                                TGraphPos indexToChange = i;
                                if (expectedIds[i] == null && expectedIds[nextPosition] == c.Id)
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

                    if (lastPosition.HasValue && lastCommit != null)
                        yield return new LogGraphNode(
                            lastPosition.Value,
                            CreateGraphDirections(lastDirections, currentDirections),
                            lastCommit,
                            lastMerge,
                            selectedBranches
                                .Where(branch => branch.Tip == lastCommit)
                                .Select(CreateBranchInfo)
                                .ToList(),
                            tags
                                .Where(tag => tag.Target == lastCommit)
                                .Select(CreateTagInfo)
                                .ToList());
                    lastDirections = currentDirections;
                    lastPosition = nextPosition;
                    lastCommit = c;
                    lastMerge = parents.Count > 1;
                }
                if (lastPosition.HasValue && lastCommit != null)
                    yield return new LogGraphNode(
                        lastPosition.Value,
                        CreateGraphDirections(lastDirections, new List<List<TGraphPos>>()),
                        lastCommit,
                        false,
                        selectedBranches
                            .Where(branch => branch.Tip == lastCommit)
                            .Select(CreateBranchInfo)
                            .ToList(),
                        tags
                            .Where(tag => tag.Target == lastCommit)
                            .Select(CreateTagInfo)
                            .ToList());
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
