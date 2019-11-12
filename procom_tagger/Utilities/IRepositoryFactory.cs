using System;
using System.Collections.Generic;
using System.Text;

namespace procom_tagger.Utilities
{
    public interface IRepositoryFactory
    {
        IRepositoryWrapper CreateRepository(string path);
    }
}
