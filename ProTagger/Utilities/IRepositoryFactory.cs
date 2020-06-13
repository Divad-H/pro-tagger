using System;
using System.Collections.Generic;
using System.Text;

namespace ProTagger.Utilities
{
    public interface IRepositoryFactory
    {
        IRepositoryWrapper CreateRepository(string path);

        string? DiscoverRepository(string path);

        bool IsValidRepository(string path);

        string? RepositoryNameFromPath(string path);
    }
}
