﻿using LibGit2Sharp;
using System;
using System.Reactive.Disposables;

namespace ProTagger.Utilities
{
    class RepositoryWrapper : IRepositoryWrapper
    {
        public RepositoryWrapper(string path)
        {
            _repository = new LibGit2Sharp.Repository(path);
            _refCountDisposable = new RefCountDisposable(_repository);
            _disposable = _refCountDisposable.GetDisposable();
        }

        private readonly LibGit2Sharp.Repository _repository;
        private readonly IDisposable _disposable;
        private readonly RefCountDisposable _refCountDisposable;

        public ICommitLog QueryCommits(CommitFilter filter)
            => _repository.Commits.QueryBy(filter);

        public BranchCollection Branches => _repository.Branches;

        public TagCollection Tags => _repository.Tags;
        public ReferenceCollection References => _repository.Refs;
        public Branch Head => _repository.Head;
        public RepositoryInformation Info => _repository.Info;

        public Diff Diff => _repository.Diff;

        public RepositoryStatus RetrieveStatus(StatusOptions options)
            => _repository.RetrieveStatus(options);

        public void Dispose()
            => _disposable.Dispose();

        public IDisposable AddRef()
            => _refCountDisposable.GetDisposable();

        public IDisposable? TryAddRef()
        {
            var res = _refCountDisposable.GetDisposable();
            if (_refCountDisposable.IsDisposed)
            {
                res.Dispose();
                return null;
            }
            return res;
        }
    }
}
