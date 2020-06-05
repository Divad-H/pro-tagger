using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger;
using ProTagger.Repo.GitLog;
using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTaggerTest.Repo.GitLog
{
    [TestClass]
    public class LogGraphTest
    {
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
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 0));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.Last().Yield().ToList(), 1));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.First().Yield().ToList(), 2));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.Skip(1).Take(2).ToList(), 3));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.Last().Yield().ToList(), 4));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.SkipLast(1).TakeLast(1).ToList(), 5));

                var branches = new List<BranchMock>();
                branches.Add(new BranchMock(true, false, "origin", commits.Last(), "master"));
                branches.Add(new BranchMock(false, false, "origin", commits.SkipLast(1).Last(), "work"));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphSimpleTest()
        {
            using var repository = new SimpleRepositoryFactoryMock().CreateRepository("./");
            var graph = LogGraph.CreateGraph(repository,
                                             new List<BranchSelection> 
                                             { 
                                                 new BranchSelection("master", "master", true),
                                                 new BranchSelection("work", "work", true)
                                             });
            var g = graph.ToList();
            Assert.AreEqual(7, g.Count);

            var node = g[0];
            Assert.AreEqual("Working tree", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.AreEqual(0, node.Branches.Count);

            node = g[1];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 1);
            Assert.AreEqual(0, node.Directions.First().Next.First());
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("master", node.Branches.First().LongName);

            node = g[2];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("work", node.Branches.First().LongName);

            node = g[3];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));

            node = g[4];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[5];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));

            node = g[6];
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
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 0));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 1));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.ToList(), 2));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(1).ToList(), 3));

                var branches = new List<BranchMock>();
                branches.Add(new BranchMock(true, false, "origin", commits.Last(), "master"));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphUnrelatedHistoryTest()
        {
            using var repository = new UnrelatedHistoryRepositoryMock().CreateRepository("./");
            var graph = LogGraph.CreateGraph(repository,
                                             new List<BranchSelection>
                                             {
                                                 new BranchSelection("master", "master", true),
                                             });
            var g = graph.ToList();
            Assert.AreEqual(5, g.Count);

            var node = g[0];
            Assert.AreEqual("Working tree", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.AreEqual(0, node.Branches.Count);

            node = g[1];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 1);
            Assert.IsTrue(node.Directions.First().Next.Contains(0));
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("master", node.Branches.First().LongName);

            node = g[2];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions.First().Next.Contains(0));
            Assert.IsTrue(node.Directions.First().Next.Contains(1));

            node = g[3];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));

            node = g[4];
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
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 0));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 1));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 2));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.ToList(), 3));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(1).ToList(), 4));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.SkipLast(1).TakeLast(1).ToList(), 5));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.SkipLast(2).TakeLast(1).ToList(), 6));

                var branches = new List<BranchMock>();
                branches.Add(new BranchMock(true, false, "origin", commits.Last(), "master"));
                branches.Add(new BranchMock(false, false, "origin", commits.SkipLast(1).Last(), "branch1"));
                branches.Add(new BranchMock(false, false, "origin", commits.SkipLast(2).Last(), "branch2"));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphThreeParentsTest()
        {
            using var repository = new ThreeParentRepositoryFactoryMock().CreateRepository("./");
            var graph = LogGraph.CreateGraph(repository,
                                             new List<BranchSelection>
                                             {
                                                 new BranchSelection("master", "master", true),
                                                 new BranchSelection("branch1", "branch1", true),
                                                 new BranchSelection("branch2", "branch2", true),
                                             });
            var g = graph.ToList();
            Assert.AreEqual(8, g.Count);


            var node = g[0];
            Assert.AreEqual("Working tree", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.AreEqual(0, node.Branches.Count);

            node = g[1];
            Assert.AreEqual("MessageShort 6", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 1);
            Assert.AreEqual(0, node.Directions.First().Next.First());
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("master", node.Branches.First().LongName);

            node = g[2];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.AreEqual(0, node.Directions[0].Next.First());
            Assert.AreEqual(1, node.Directions[1].Next.First());
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("branch1", node.Branches.First().LongName);

            node = g[3];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 3);
            Assert.AreEqual(0, node.Directions[0].Next.First());
            Assert.AreEqual(0, node.Directions[1].Next.First());
            Assert.AreEqual(0, node.Directions[2].Next.First());
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("branch2", node.Branches.First().LongName);

            node = g[4];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));
            Assert.IsTrue(node.Directions[0].Next.Contains(2));

            node = g[5];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[6];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));

            node = g[7];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 0);

            AssertGraphConsistency(g);
        }

        class ReusedColumnRepositoryMock : IRepositoryFactory
        {
            /// <summary>
            /// Expected Graph:
            /// X     8
            /// |
            /// X     7
            /// |\
            /// | X   6
            /// | |\ 
            /// | | X 5
            /// | | |
            /// | X | 4
            /// |/  |
            /// X   | 3
            /// |\  |
            /// | X | 2
            /// | | |
            /// X─┼─╯ 1
            /// |/
            /// X     0
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public IRepositoryWrapper CreateRepository(string path)
            {
                var shaGenerator = new PseudoShaGenerator();

                var commits = new List<CommitMock>();
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 0));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.ToList(), 1));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.Take(1).ToList(), 2));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(2).ToList(), 3));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(1).ToList(), 4));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.Skip(1).Take(1).ToList(), 5));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(2).ToList(), 6));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.Skip(3).Take(1).Concat(commits.TakeLast(1)).ToList(), 7));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(1).ToList(), 8));

                var branches = new List<BranchMock>();
                branches.Add(new BranchMock(true, false, "origin", commits.Last(), "master"));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphReusedColumnTest()
        {
            using var repository = new ReusedColumnRepositoryMock().CreateRepository("./");
            var graph = LogGraph.CreateGraph(repository,
                                             new List<BranchSelection>
                                             {
                                                 new BranchSelection("master", "master", true),
                                             });
            var g = graph.ToList();
            Assert.AreEqual(10, g.Count);

            var node = g[0];
            Assert.AreEqual("Working tree", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.AreEqual(0, node.Branches.Count);

            node = g[1];
            Assert.AreEqual("MessageShort 8", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 1, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("master", node.Branches.First().LongName);

            node = g[2];
            Assert.AreEqual("MessageShort 7", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));

            node = g[3];
            Assert.AreEqual("MessageShort 6", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 2, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));
            Assert.IsTrue(node.Directions[1].Next.Contains(2));

            node = g[4];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));
            Assert.IsTrue(node.Directions[2].Next.Contains(2));

            node = g[5];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));
            Assert.IsTrue(node.Directions[2].Next.Contains(2));

            node = g[6];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));
            Assert.IsTrue(node.Directions[2].Next.Contains(2));

            node = g[7];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));
            Assert.IsTrue(node.Directions[2].Next.Contains(0));

            node = g[8];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));

            node = g[9];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 0);

            AssertGraphConsistency(g);
        }

        class SwitchingParentOrderRepositoryMock : IRepositoryFactory
        {
            /// <summary>
            /// Expected Graph:
            /// X    5
            /// |\
            /// | X  4
            /// |/|
            /// X |  3
            /// |\|
            /// | X  2
            /// |/|
            /// X |  1
            /// |/
            /// X    0
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public IRepositoryWrapper CreateRepository(string path)
            {
                var shaGenerator = new PseudoShaGenerator();

                var commits = new List<CommitMock>();
                commits.Add(CommitMock.GenerateCommit(shaGenerator, null, 0));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.ToList(), 1));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.ToList(), 2));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(2).ToList(), 3));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(2).ToList(), 4));
                commits.Add(CommitMock.GenerateCommit(shaGenerator, commits.TakeLast(2).ToList(), 5));

                var branches = new List<BranchMock>();
                branches.Add(new BranchMock(true, false, "origin", commits.Last(), "master"));

                return new RepositoryMock(commits.AsEnumerable().Reverse(), new BranchCollectionMock(branches));
            }
        }

        [TestMethod]
        public void CreateGraphSwitchingParentOrderTest()
        {
            using var repository = new SwitchingParentOrderRepositoryMock().CreateRepository("./");
            var graph = LogGraph.CreateGraph(repository,
                                             new List<BranchSelection>
                                             {
                                                 new BranchSelection("master", "master", true),
                                             });
            var g = graph.ToList();
            Assert.AreEqual(7, g.Count);

            var node = g[0];
            Assert.AreEqual("Working tree", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 0, 1);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.AreEqual(0, node.Branches.Count);

            node = g[1];
            Assert.AreEqual("MessageShort 5", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 1, 2);
            Assert.IsTrue(node.Directions.First().Next.Contains(0));
            Assert.IsTrue(node.Directions.First().Next.Contains(1));
            Assert.AreEqual(1, node.Branches.Count);
            Assert.AreEqual("master", node.Branches.First().LongName);

            node = g[2];
            Assert.AreEqual("MessageShort 4", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 2, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[3];
            Assert.AreEqual("MessageShort 3", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[0].Next.Contains(1));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[4];
            Assert.AreEqual("MessageShort 2", node.MessageShort);
            Assert.IsTrue(node.IsMerge);
            AssertFromToCount(node, 3, 3);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(1));

            node = g[5];
            Assert.AreEqual("MessageShort 1", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 3, 2);
            Assert.IsTrue(node.Directions[0].Next.Contains(0));
            Assert.IsTrue(node.Directions[1].Next.Contains(0));

            node = g[6];
            Assert.AreEqual("MessageShort 0", node.MessageShort);
            Assert.IsFalse(node.IsMerge);
            AssertFromToCount(node, 2, 0);

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
