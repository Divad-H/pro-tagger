using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace procom_tagger_test.LibGit2Mocks
{
    class CommitLogMock : ICommitLog
    {
        public CommitLogMock(IEnumerable<CommitMock> commits)
        {
            _commits = commits;
        }

        private readonly IEnumerable<CommitMock> _commits;

        public CommitSortStrategies SortedBy => throw new NotImplementedException();

        public IEnumerator<Commit> GetEnumerator()
        {
            return _commits.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _commits.GetEnumerator();
        }
    }
}
