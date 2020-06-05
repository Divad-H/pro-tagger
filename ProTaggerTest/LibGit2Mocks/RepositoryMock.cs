using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace ProTaggerTest.LibGit2Mocks
{
    class RepositoryMock : IRepositoryWrapper
    {
        public RepositoryMock(IEnumerable<CommitMock> commits, 
            BranchCollectionMock branches, 
            IList<TagMock>? tags = null,
            Diff? diff = null)
        {
            _commits = commits;
            _branches = branches;
            _tags = new TagCollectionMock(tags ?? new List<TagMock>());
            _diff = diff ?? new DiffMock(null, null);
        }

        private readonly IEnumerable<CommitMock> _commits;
        private readonly BranchCollectionMock _branches;
        private readonly TagCollectionMock _tags;

        public BranchCollection Branches => _branches;

        public TagCollection Tags => _tags;

        private readonly Diff _diff;
        public Diff Diff => _diff;

        public Branch Head => throw new NotImplementedException();

        public void Dispose()
        {}

        public ICommitLog QueryCommits(CommitFilter filter)
            => new CommitLogMock(_commits);

        public IDisposable AddRef()
            => Disposable.Empty;

        public IDisposable? TryAddRef()
            => Disposable.Empty;
    }
}
