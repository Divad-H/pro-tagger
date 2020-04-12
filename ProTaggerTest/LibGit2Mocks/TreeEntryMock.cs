using LibGit2Sharp;
using System;

namespace ProTaggerTest.LibGit2Mocks
{
    class TreeEntryMock : TreeEntry
    {
        public TreeEntryMock(Mode mode, string name, string path, TreeEntryTargetType targetType)
        {
            _mode = mode;
            _name = name;
            _path = path;
            _targetType = targetType;
        }

        private readonly Mode _mode;
        public override Mode Mode => _mode;

        private readonly string _name;
        public override string Name => _name;

        private readonly string _path;
        public override string Path => _path;

        public override GitObject Target => throw new NotImplementedException();

        TreeEntryTargetType _targetType;
        public override TreeEntryTargetType TargetType => _targetType;
    }
}
