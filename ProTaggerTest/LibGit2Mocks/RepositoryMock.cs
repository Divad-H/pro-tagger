using LibGit2Sharp;
using ProTagger.Utilities;
using System.Collections.Generic;

namespace ProTaggerTest.LibGit2Mocks
{
    class RepositoryMock : IRepositoryWrapper
    {
        public RepositoryMock(IEnumerable<CommitMock> commits, BranchCollectionMock branches)
        {
            _commits = commits;
            _branches = branches;
        }

        private readonly IEnumerable<CommitMock> _commits;
        private readonly BranchCollectionMock _branches;

        public BranchCollection Branches => _branches;

        public void Dispose()
        {}

        public ICommitLog QueryCommits(CommitFilter filter)
        {
            return new CommitLogMock(_commits);
        }
    }
}
