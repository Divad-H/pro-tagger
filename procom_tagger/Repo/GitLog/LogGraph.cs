using LibGit2Sharp;
using procom_tagger.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveMvvm;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace procom_tagger.Repo.GitLog
{
    using GraphType = List<LogGraphNode>;
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
            public List<int> Previous;
            /// <summary>
            /// Directions of the current LogGraphNode
            /// </summary>
            public List<int> Next;
        }
        public LogGraphNode(int graphPosition, List<DownwardDirections> directions, Commit commit, bool isMerge)
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
        }

        public int GraphPosition { get; }
        public List<DownwardDirections> Directions { get; }
        public string MessageShort { get; }
        public string Message { get; }
        public bool IsMerge { get; }
        public string Sha { get; }
        public string ShortSha { get; }
        public Signature Author { get; }
        public Signature Committer { get; }
    }

    //public class BranchInfo
    //{
    //    public bool IsHead { get; }
    //    public 
    //}

    public class LogGraph : INotifyPropertyChanged, IDisposable
    {
        private Variant<GraphType, string> _logGraphNodes;
        public Variant<GraphType, string> LogGraphNodes
        {
            get { return _logGraphNodes; }
            set
            {
                if (_logGraphNodes == value)
                    return;
                _logGraphNodes = value;
                NotifyPropertyChanged();
            }
        }

        public LogGraph(IRepositoryFactory repositoryFactory, string path, IObservable<IEnumerable<string>> selectedBranchNames)
        {
            _logGraphNodes = new Variant<GraphType, string>(new GraphType());

            var logGraphNodes = selectedBranchNames
                .Select(branchNames => CreateGraph(repositoryFactory, path, branchNames))
                .Retry();

            logGraphNodes
                .Subscribe(graphNodes => LogGraphNodes = graphNodes)
                .DisposeWith(_disposable);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
        {
            _disposable.Dispose();
        }

        internal static Variant<GraphType, string> CreateGraph(IRepositoryFactory repositoryFactory, string path, IEnumerable<string> branches)
        {
            try
            {
                using var repo = repositoryFactory.CreateRepository(path);
                var result = new GraphType();
                var expectedIds = new List<ObjectId?>();
                var directions = new List<List<int>>();
                var lastDirections = new List<List<int>>();

                int? lastPosition = null;
                Commit? lastCommit = null;
                bool lastMerge = false;

                var commitFilter = new CommitFilter()
                {
                    SortBy = CommitSortStrategies.Topological,
                    IncludeReachableFrom = branches.Select(name => repo.Branches[name])
                };

                foreach (Commit c in repo.QueryCommits(commitFilter).Take(1000))
                {
                    var nextPosition = expectedIds.FindIndex((id) => id == c.Id);
                    if (nextPosition == -1)
                    {
                        // commit without visible children
                        nextPosition = expectedIds.FindIndex((id) => id == null);
                        if (nextPosition == -1)
                        {
                            nextPosition = expectedIds.Count;
                            expectedIds.Add(null);
                            directions.Add(new List<int>());
                        }
                    }

                    var currentDirections = directions;
                    directions = currentDirections.Select((dir) => dir.Take(0).ToList()).ToList();
                    for (int i = 0; i < directions.Count; ++i)
                        if (expectedIds[i] != null)
                            directions[i].Add(i);
                    for (int i = 0; i < currentDirections.Count; ++i)
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
                    int parentIndex = 0;

                    for (int i = 0; i < expectedIds.Count; ++i)
                    {
                        if (expectedIds[i] == c.Id || expectedIds[i] == null)
                        {
                            if (parents.Count > parentIndex)
                            {
                                int indexToChange = i;
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
                        directions.Add(new List<int>());
                        directions[nextPosition].Add(directions.Count - 1);
                    }

                    if (lastPosition.HasValue && lastCommit != null)
                        result.Add(new LogGraphNode(lastPosition.Value, CreateGraphDirections(lastDirections, currentDirections), lastCommit, lastMerge));
                    lastDirections = currentDirections;
                    lastPosition = nextPosition;
                    lastCommit = c;
                    lastMerge = parents.Count > 1;
                }
                if (lastPosition.HasValue && lastCommit != null)
                    result.Add(new LogGraphNode(lastPosition.Value, CreateGraphDirections(lastDirections, new List<List<int>>()), lastCommit, false));
                return new Variant<GraphType, string>(result);
            }
            catch (Exception e)
            {
                return new Variant<GraphType, string>(e.Message);
            }
        }

        private static List<LogGraphNode.DownwardDirections> CreateGraphDirections(IEnumerable<List<int>> lastDirections, IEnumerable<List<int>> nextDirections)
        {
            return lastDirections.GreaterZip(nextDirections,
                                             (first, second) => new LogGraphNode.DownwardDirections() { Previous = first, Next = second }, new List<int>(), new List<int>())
                                 .ToList();
        }
    }
}
