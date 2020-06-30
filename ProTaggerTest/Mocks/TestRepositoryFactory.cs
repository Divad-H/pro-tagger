using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using System.Collections.Generic;
using System.Linq;

namespace ProTaggerTest.Mocks
{
    abstract class RepositoryFactoryMockBase : IRepositoryFactory
    {
        public abstract IRepositoryWrapper CreateRepository(string path);
        public string? DiscoverRepository(string path)
            => path;

        public bool IsValidRepository(string path)
            => true;

        public string? RepositoryNameFromPath(string path)
            => "Repository name";
    }

    class TestRepositoryFactory : RepositoryFactoryMockBase
    {
        public override IRepositoryWrapper CreateRepository(string path)
            => new RepositoryMock(new List<CommitMock>(), new BranchCollectionMock(new List<BranchMock>()));
    }

    class SimpleRepositoryFactoryMock : RepositoryFactoryMockBase
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
        public override IRepositoryWrapper CreateRepository(string path)
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
}
