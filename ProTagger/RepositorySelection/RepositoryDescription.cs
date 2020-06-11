using System;
using System.Collections.Generic;
using System.Text;

namespace ProTagger.RepositorySelection
{
    public class RepositoryDescription
    {
        public string Name { get; }
        public string Path { get; }

        public RepositoryDescription(string name, string path)
            => (Name, Path) = (name, path);
    }
}
