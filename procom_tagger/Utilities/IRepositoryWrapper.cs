using LibGit2Sharp;
using System;

namespace procom_tagger.Utilities
{
    public interface IRepositoryWrapper : IDisposable
    {
        public ICommitLog QueryCommits(CommitFilter filter);

        public BranchCollection Branches { get; }
    }
}
