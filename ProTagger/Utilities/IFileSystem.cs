using System;

namespace ProTagger.Utilities
{
    public interface IFileSystem
    {
        string? SelectGitRepositoryDialog(string description, Func<string, bool> validationCallback);

        IFileSystemWatcher CreateFileSystemWatcher(string directory, string filter);
    }
}
