using LibGit2Sharp;

namespace ProTagger.Utilities
{
    class RepositoryWrapper : IRepositoryWrapper
    {
        public RepositoryWrapper(string path)
        {
            _repository = new Repository(path);
            //_repository.Commits
        }

        private readonly Repository _repository;

        public ICommitLog QueryCommits(CommitFilter filter)
        {
            return _repository.Commits.QueryBy(filter);
        }

        public BranchCollection Branches => _repository.Branches;

        public TagCollection Tags => _repository.Tags;

        public Diff Diff => _repository.Diff;

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
