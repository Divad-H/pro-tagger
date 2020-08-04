using ProTagger.Utilities;
using System;

namespace ProTaggerTest.Mocks
{
    class TestFileSystemWatcher : IFileSystemWatcher
    {
        public event EventHandler<object?>? Changed;

        public void TriggerChangedEvent()
            => Changed?.Invoke(this, null);

        public void Dispose()
        {}
    }

    class TestFileSystem : IFileSystem
    {
        public IFileSystemWatcher CreateFileSystemWatcher(string gitDirectory, string filter)
            => new TestFileSystemWatcher();

        public string? SelectGitRepositoryDialog(string description, Func<string, bool> validationCallback)
            => "Selected directory";
    }
}
