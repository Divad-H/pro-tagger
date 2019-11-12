﻿using LibGit2Sharp;

namespace procom_tagger.Utilities
{
    class RepositoryWrapper : IRepositoryWrapper
    {
        public RepositoryWrapper(string path)
        {
            _repository = new Repository(path);
        }

        private readonly Repository _repository;

        public ICommitLog QueryCommits(CommitFilter filter)
        {
            return _repository.Commits.QueryBy(filter);
        }

        public BranchCollection Branches => _repository.Branches;

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
