using LibGit2Sharp;
using procom_tagger.Utilities;
using System.Collections.Generic;

namespace procom_tagger_test.LibGit2Mocks
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
