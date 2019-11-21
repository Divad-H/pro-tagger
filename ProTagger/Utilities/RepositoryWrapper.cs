using LibGit2Sharp;

namespace ProTagger.Utilities
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

        public TagCollection Tags => _repository.Tags;

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
