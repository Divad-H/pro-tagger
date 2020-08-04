using System;

namespace ProTagger.Utilities
{
    public interface IFileSystemWatcher : IDisposable
    {
        public event EventHandler<object?>? Changed;
    }
}
