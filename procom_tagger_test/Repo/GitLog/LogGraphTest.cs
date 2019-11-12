using LibGit2Sharp;
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
        class TestRepositoryFactory : IRepositoryFactory
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
            /// <param name="path"></param>
            /// <returns></returns>
            public IRepositoryWrapper CreateRepository(string path)
            {
                var pseudoShaGenerator = new PseudoShaGenerator();
                Func<IEnumerable<CommitMock>?, int, CommitMock> generateCommit = (IEnumerable<CommitMock>? parents, int idx) =>
                {
                    return new CommitMock(
                        $"Message {idx}\n(long)",
                        $"MessageShort {idx}",
                        new Signature("john", "jons@e.mail", new DateTimeOffset(DateTime.Now)),
                        new ObjectId(pseudoShaGenerator.Generate()),
                        parents ?? new List<CommitMock>());
                };

                var commits = new List<CommitMock>();
                commits.Add(generateCommit(null, 0));
                commits.Add(generateCommit(commits.Last().Yield().ToList(), 1));
                commits.Add(generateCommit(commits.First().Yield().ToList(), 2));
                commits.Add(generateCommit(commits.Skip(1).Take(2).ToList(), 3));
                commits.Add(generateCommit(commits.Last().Yield().ToList(), 4));
                commits.Add(generateCommit(commits.SkipLast(1).TakeLast(1).ToList(), 5));

                var branches = new Dictionary<string, BranchMock>();
                branches.Add("master", new BranchMock(true, false, "origin", commits.Last()));
                branches.Add("work", new BranchMock(false, false, "origin", commits.SkipLast(1).Last()));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphTest()
        {
            var graph = LogGraph.CreateGraph(new TestRepositoryFactory(),
                                             "./",
                                             new List<string>() { "master", "work" });
            Assert.IsTrue(graph.Is<List<LogGraphNode>>(), "Expected result of CreateGraph to be a " + nameof(List<LogGraphNode>));
            var g = graph.Get<List<LogGraphNode>>();
            Assert.AreEqual(6, g.Count);

            var node = g[0];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.AreEqual(0, node.Directions.First().To.First());

            node = g[1];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions[0].To.Contains(0));
            Assert.IsTrue(node.Directions[1].To.Contains(0));

            node = g[2];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].To.Contains(0));
            Assert.IsTrue(node.Directions[0].To.Contains(1));

            node = g[3];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].To.Contains(0));
            Assert.IsTrue(node.Directions[1].To.Contains(1));

            node = g[4];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].To.Contains(0));
            Assert.IsTrue(node.Directions[1].To.Contains(0));

            node = g[5];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 0);

            AssertGraphConsistency(g);
        }

        private void AssertFromToCount(LogGraphNode node, int expectedFrom, int expectedTo)
        {
            Assert.AreEqual(expectedFrom, node.Directions
                .Select(dir => dir.From)
                .Sum(from => from.Count));
            Assert.AreEqual(expectedTo, node.Directions
                .Select(dir => dir.To)
                .Sum(to => to.Count));
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
                            var intersectCount = second.Directions[firstIndex].From.Intersect(firstDir.To).Count();
                            Assert.AreEqual(firstDir.To.Count, intersectCount);
                            Assert.AreEqual(second.Directions[firstIndex].From.Count, intersectCount);
                            return 0;
                        })
                        .ToArray()
                      ).ToArray();

            graph
                .ForEach(node =>
                    node.Directions
                        .Select((dir, index) =>
                        {
                            var fromCount = node.Directions.Sum((dir) => dir.From.Where(i => i == index).Count());
                            if (index == node.GraphPosition)
                            {
                                // A node has at least one edge.
                                Assert.IsTrue(dir.To.Count + fromCount > 0);
                                Assert.AreEqual(dir.To.Count > 1, node.IsMerge);
                            }
                            else
                            {
                                // The graph ohly has nodes at GraphPosition
                                Assert.IsTrue(dir.To.Count <= 1);
                                Assert.AreEqual(fromCount, dir.To.Count);
                            }
                            return 0;
                        })
                        .ToArray()
                );
        }
    }
}
