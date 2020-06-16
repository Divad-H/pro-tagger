using ProTagger.Utilities;
using System;

namespace ProTaggerTest.Mocks
{
    class TestFileSystem : IFileSystem
    {
        public string? SelectGitRepositoryDialog(string description, Func<string, bool> validationCallback)
            => "Selected directory";
    }
}
