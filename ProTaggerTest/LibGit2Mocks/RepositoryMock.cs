using LibGit2Sharp;
using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
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
            _refs = new ReferenceCollectionMock(tags ?? new List<TagMock>(), branches.Cast<BranchMock>().ToList());
            _diff = diff ?? new DiffMock(null, null);
            _repositoryStatus = new RepositoryStatusMock();
        }

        private readonly IEnumerable<CommitMock> _commits;
        private readonly BranchCollectionMock _branches;
        private readonly TagCollectionMock _tags;
        private readonly ReferenceCollectionMock _refs;
        private readonly RepositoryStatusMock _repositoryStatus;

        public BranchCollection Branches => _branches;

        public TagCollection Tags => _tags;
        public ReferenceCollection References => _refs;

        private readonly Diff _diff;
        public Diff Diff => _diff;

        public Branch? Head => _branches.FirstOrDefault();

        private readonly RepositoryInformationMock _info = new RepositoryInformationMock();
        public RepositoryInformation Info => _info;

        public void Dispose()
        {}

        public ICommitLog QueryCommits(CommitFilter filter)
            => new CommitLogMock(_commits);

        public IDisposable AddRef()
            => Disposable.Empty;

        public IDisposable? TryAddRef()
            => Disposable.Empty;

        public RepositoryStatus RetrieveStatus(StatusOptions options)
            => _repositoryStatus;
    }
}
