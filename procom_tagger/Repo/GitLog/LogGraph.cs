﻿using LibGit2Sharp;
using procom_tagger.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace procom_tagger.Repo.GitLog
{
    public class LogGraphNode
    {
        public struct FromTo
        {
            public List<int> From;
            public List<int> To;
        }
        public LogGraphNode(int graphPosition, List<FromTo> directions, Commit commit, bool isMerge)
        {
            GraphPosition = graphPosition;
            Directions = directions;
            MessageShort = commit.MessageShort;
            Message = commit.Message;
            IsMerge = isMerge;
            Sha = commit.Sha;
            ShortSha = Sha.Substring(0, 6);
            Author = commit.Author;
            Committer = commit.Committer;
        }

        public int GraphPosition { get; }
        public List<FromTo> Directions { get; }
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

    public class LogGraph : INotifyPropertyChanged
    {
        private List<LogGraphNode> _logGraphNodes;
        public List<LogGraphNode> LogGraphNodes
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

        public LogGraph(string path)
        {
            _logGraphNodes = LogGraph.CreateGraph(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static List<LogGraphNode> CreateGraph(string path)
        {
            var result = new List<LogGraphNode>();
            using (var repo = new Repository(path))
            {
                var expectedIds = new List<ObjectId?>();
                var directions = new List<List<int>>();
                var lastDirections = new List<List<int>>();

                int? lastPosition = null;
                Commit? lastCommit = null;
                bool lastMerge = false;
                
                foreach (Commit c in repo.Commits.QueryBy(new CommitFilter() { SortBy = CommitSortStrategies.Topological, IncludeReachableFrom = new List<Branch>() { repo.Branches["master"], repo.Branches["origin/maint/v0.24"] } }).Take(1000))
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
                      result.Add(new LogGraphNode(lastPosition.Value, createGraphDirections(lastDirections, currentDirections), lastCommit, lastMerge));
                    lastDirections = currentDirections;
                    lastPosition = nextPosition;
                    lastCommit = c;
                    lastMerge = parents.Count > 1;
                }
                if (lastPosition.HasValue && lastCommit != null)
                    result.Add(new LogGraphNode(lastPosition.Value, createGraphDirections(lastDirections, new List<List<int>>()), lastCommit, false));
            }
            return result;
        }

        private static List<LogGraphNode.FromTo> createGraphDirections(IEnumerable<List<int>> lastDirections, IEnumerable<List<int>> nextDirections)
        {
            return lastDirections.GreaterZip(nextDirections,
                                             (first, second) => new LogGraphNode.FromTo() { From = first, To = second }, new List<int>(), new List<int>())
                                 .ToList();
        }
    }
}