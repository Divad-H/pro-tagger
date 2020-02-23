using LibGit2Sharp;
using ProTagger.Utilities;
using System.Collections.Generic;

namespace ProTaggerTest.LibGit2Mocks
{
    class RepositoryMock : IRepositoryWrapper
    {
        public RepositoryMock(IEnumerable<CommitMock> commits, BranchCollectionMock branches, IList<TagMock>? tags = null)
        {
            _commits = commits;
            _branches = branches;
            _tags = new TagCollectionMock(tags ?? new List<TagMock>());
        }

        private readonly IEnumerable<CommitMock> _commits;
        private readonly BranchCollectionMock _branches;
        private readonly TagCollectionMock _tags;

        public BranchCollection Branches => _branches;

        public TagCollection Tags => _tags;

        public Diff Diff => throw new System.NotImplementedException();

        public void Dispose()
        {}

        public ICommitLog QueryCommits(CommitFilter filter)
        {
            return new CommitLogMock(_commits);
        }
    }
}
