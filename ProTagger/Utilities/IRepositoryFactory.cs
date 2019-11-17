using System;
using System.Collections.Generic;
using System.Text;

namespace ProTagger.Utilities
{
    public interface IRepositoryFactory
    {
        IRepositoryWrapper CreateRepository(string path);
    }
}
