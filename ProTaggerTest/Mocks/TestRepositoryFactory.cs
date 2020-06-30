using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using System.Collections.Generic;

namespace ProTaggerTest.Mocks
{
    class TestRepositoryFactory : IRepositoryFactory
    {
        public IRepositoryWrapper CreateRepository(string path)
            => new RepositoryMock(new List<CommitMock>(), new BranchCollectionMock(new List<BranchMock>()));

        public string? DiscoverRepository(string path)
            => path;

        public bool IsValidRepository(string path)
            => true;

        public string? RepositoryNameFromPath(string path)
            => "Repository name";
    }
}
