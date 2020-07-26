using LibGit2Sharp;
using System;

namespace ProTagger.Utilities
{
    public interface IRefCount : IDisposable
    {
        public IDisposable AddRef();

        public IDisposable? TryAddRef();
    }

    public interface IRepositoryWrapper : IRefCount
    {
        public ICommitLog QueryCommits(CommitFilter filter);

        public BranchCollection Branches { get; }
        public TagCollection Tags { get; }
        public ReferenceCollection References { get; }

        public Diff Diff { get; }
        public Branch? Head { get; }
        public RepositoryInformation Info { get; }
        public RepositoryStatus RetrieveStatus(StatusOptions options);
    }
}
