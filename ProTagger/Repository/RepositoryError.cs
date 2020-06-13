using ProTagger.RepositorySelection;
using ProTagger.Utilities;

namespace ProTagger.Repository
{
    public class RepositoryError : Unexpected
    {
        public RepositoryDescription RepositoryDescription { get; }

        public RepositoryError(string message, RepositoryDescription repositoryDescription)
            : base(message)
            => RepositoryDescription = repositoryDescription;
    }
}
