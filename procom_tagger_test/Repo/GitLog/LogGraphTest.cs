﻿using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using procom_tagger.Repo.GitLog;
using procom_tagger.Utilities;
using procom_tagger_test.LibGit2Mocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace procom_tagger_test.Repo.GitLog
{
    [TestClass]
    public class LogGraphTest
    {
        static CommitMock GenerateCommit(PseudoShaGenerator shaGenerator, IEnumerable<CommitMock>? parents, int idx)
        {
            return new CommitMock(
                    $"Message {idx}\n(long)",
                    $"MessageShort {idx}",
                    new Signature("john", "jons@e.mail", new DateTimeOffset(DateTime.Now)),
                    new ObjectId(shaGenerator.Generate()),
                    parents ?? new List<CommitMock>());
        }
        class SimpleRepositoryFactoryMock : IRepositoryFactory
        {
            /// <summary>
            /// Expected Graph:
            /// X    5
            /// |
            /// | X  4
            /// |/
            /// X    3
            /// |\
            /// X |  2
            /// | |
            /// | X  1
            /// |/
            /// X    0
            /// </summary>
            public IRepositoryWrapper CreateRepository(string path)
            {
                var shaGenerator = new PseudoShaGenerator();
                
                var commits = new List<CommitMock>();
                commits.Add(GenerateCommit(shaGenerator, null, 0));
                commits.Add(GenerateCommit(shaGenerator, commits.Last().Yield().ToList(), 1));
                commits.Add(GenerateCommit(shaGenerator, commits.First().Yield().ToList(), 2));
                commits.Add(GenerateCommit(shaGenerator, commits.Skip(1).Take(2).ToList(), 3));
                commits.Add(GenerateCommit(shaGenerator, commits.Last().Yield().ToList(), 4));
                commits.Add(GenerateCommit(shaGenerator, commits.SkipLast(1).TakeLast(1).ToList(), 5));

                var branches = new Dictionary<string, BranchMock>();
                branches.Add("master", new BranchMock(true, false, "origin", commits.Last()));
                branches.Add("work", new BranchMock(false, false, "origin", commits.SkipLast(1).Last()));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphSimpleTest()
        {
            var graph = LogGraph.CreateGraph(new SimpleRepositoryFactoryMock(),
                                             "./",
                                             new List<string>() { "master", "work" });
            Assert.IsTrue(graph.Is<List<LogGraphNode>>(), "Expected result of CreateGraph to be a " + nameof(List<LogGraphNode>));
            var g = graph.Get<List<LogGraphNode>>();
            Assert.AreEqual(6, g.Count);

            var node = g[0];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.AreEqual(0, node.Directions.First().Next.First());

            node = g[1];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));

            node = g[2];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));

            node = g[3];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[4];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));

            node = g[5];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 0);

            AssertGraphConsistency(g);
        }

        private void AssertFromToCount(LogGraphNode node, int expectedFrom, int expectedTo)
        {
            Assert.AreEqual(expectedFrom, node.Directions
                .Select(dir => dir.Previous)
                .Sum(from => from.Count));
            Assert.AreEqual(expectedTo, node.Directions
                .Select(dir => dir.Next)
                .Sum(to => to.Count));
        }

        class UnrelatedHistoryRepositoryMock : IRepositoryFactory
        {
            /// <summary>
            /// Expected Graph:
            /// X    3
            /// |
            /// X    2
            /// |\
            /// | X  1
            /// |
            /// X    0
            /// 
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public IRepositoryWrapper CreateRepository(string path)
            {
                var shaGenerator = new PseudoShaGenerator();

                var commits = new List<CommitMock>();
                commits.Add(GenerateCommit(shaGenerator, null, 0));
                commits.Add(GenerateCommit(shaGenerator, null, 1));
                commits.Add(GenerateCommit(shaGenerator, commits.ToList(), 2));
                commits.Add(GenerateCommit(shaGenerator, commits.TakeLast(1).ToList(), 3));

                var branches = new Dictionary<string, BranchMock>();
                branches.Add("master", new BranchMock(true, false, "origin", commits.Last()));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphUnrelatedHistoryTest()
        {
            var graph = LogGraph.CreateGraph(new UnrelatedHistoryRepositoryMock(),
                                             "./",
                                             new List<string>() { "master" });
            Assert.IsTrue(graph.Is<List<LogGraphNode>>(), "Expected result of CreateGraph to be a " + nameof(List<LogGraphNode>));
            var g = graph.Get<List<LogGraphNode>>();
            Assert.AreEqual(4, g.Count);

            var node = g[0];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.IsTrue(node.Directions.First().Next.Contains(0));

            node = g[1];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions.First().Next.Contains(0));
            Assert.IsTrue(node.Directions.First().Next.Contains(1));

            node = g[2];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));

            node = g[3];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 0);

            AssertGraphConsistency(g);
        }

        class ThreeParentRepositoryFactoryMock : IRepositoryFactory
        {
            /// <summary>
            /// Expected Graph:
            /// X      6
            /// |
            /// | X    5
            /// | |
            /// | | X  4
            /// |/⟋
            /// X      3
            /// |\⟍
            /// | | X  2
            /// | |  
            /// | X    1
            /// |    
            /// X      0
            /// </summary>
            public IRepositoryWrapper CreateRepository(string path)
            {
                var shaGenerator = new PseudoShaGenerator();

                var commits = new List<CommitMock>();
                commits.Add(GenerateCommit(shaGenerator, null, 0));
                commits.Add(GenerateCommit(shaGenerator, null, 1));
                commits.Add(GenerateCommit(shaGenerator, null, 2));
                commits.Add(GenerateCommit(shaGenerator, commits.ToList(), 3));
                commits.Add(GenerateCommit(shaGenerator, commits.TakeLast(1).ToList(), 4));
                commits.Add(GenerateCommit(shaGenerator, commits.SkipLast(1).TakeLast(1).ToList(), 5));
                commits.Add(GenerateCommit(shaGenerator, commits.SkipLast(2).TakeLast(1).ToList(), 6));

                var branches = new Dictionary<string, BranchMock>();
                branches.Add("master", new BranchMock(true, false, "origin", commits.Last()));
                branches.Add("branch1", new BranchMock(false, false, "origin", commits.SkipLast(1).Last()));
                branches.Add("branch2", new BranchMock(false, false, "origin", commits.SkipLast(2).Last()));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphThreeParentsTest()
        {
            var graph = LogGraph.CreateGraph(new ThreeParentRepositoryFactoryMock(),
                                             "./",
                                             new List<string>() { "master", "branch1", "branch2" });
            Assert.IsTrue(graph.Is<List<LogGraphNode>>(), "Expected result of CreateGraph to be a " + nameof(List<LogGraphNode>));
            var g = graph.Get<List<LogGraphNode>>();
            Assert.AreEqual(7, g.Count);

            var node = g[0];
            Assert.AreEqual("MessageShort 6", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.AreEqual(0, node.Directions.First().Next.First());

            node = g[1];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.AreEqual(0, node.Directions[0].Next.First());
            Assert.AreEqual(1, node.Directions[1].Next.First());

            node = g[2];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 3);
            Assert.AreEqual(0, node.Directions[0].Next.First());
            Assert.AreEqual(0, node.Directions[1].Next.First());
            Assert.AreEqual(0, node.Directions[2].Next.First());

            node = g[3];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));
            Assert.IsTrue(node.Directions[0].Next.Contains(2));

            node = g[4];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[5];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));

            node = g[6];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 0);

            AssertGraphConsistency(g);
        }

        private void AssertGraphConsistency(List<LogGraphNode> graph)
        {
            graph
                .Zip(graph.Skip(1), (first, second) =>
                    first.Directions
                        .Select((firstDir, firstIndex) =>
                        {
                            // Assert that the redundant information in subsequent nodes is consistent
                            Assert.IsTrue(second.Directions.Count >= first.Directions.Count);
                            var intersectCount = second.Directions[firstIndex].Previous.Intersect(firstDir.Next).Count();
                            Assert.AreEqual(firstDir.Next.Count, intersectCount);
                            Assert.AreEqual(second.Directions[firstIndex].Previous.Count, intersectCount);
                            return 0;
                        })
                        .ToArray()
                      ).ToArray();

            graph
                .ForEach(node =>
                    node.Directions
                        .Select((dir, index) =>
                        {
                            var fromCount = node.Directions.Sum((dir) => dir.Previous.Where(i => i == index).Count());
                            if (index == node.GraphPosition)
                            {
                                // A node has at least one edge.
                                Assert.IsTrue(dir.Next.Count + fromCount > 0);
                                Assert.AreEqual(dir.Next.Count > 1, node.IsMerge);
                            }
                            else
                            {
                                // The graph ohly has nodes at GraphPosition
                                Assert.IsTrue(dir.Next.Count <= 1);
                                Assert.AreEqual(fromCount, dir.Next.Count);
                            }
                            return 0;
                        })
                        .ToArray()
                );
        }
    }
}
