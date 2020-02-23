﻿using LibGit2Sharp;
using System;

namespace ProTagger.Utilities
{
    public interface IRepositoryWrapper : IDisposable
    {
        public ICommitLog QueryCommits(CommitFilter filter);

        public BranchCollection Branches { get; }
        public TagCollection Tags { get; }

        public Diff Diff { get; }
    }
}
