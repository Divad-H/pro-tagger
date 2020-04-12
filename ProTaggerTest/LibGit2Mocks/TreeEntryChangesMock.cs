using LibGit2Sharp;

namespace ProTaggerTest.LibGit2Mocks
{
    class TreeEntryChangesMock : TreeEntryChanges
    {
        public TreeEntryChangesMock(ChangeKind status,
            bool exists,
            Mode mode,
            ObjectId? oid,
            string? path,
            bool oldExists,
            Mode oldMode,
            ObjectId? oldOid,
            string? oldPath)
        {
            _status = status;
            _exists = exists;
            _mode = mode;
            _oid = oid;
            _path = path;
            _oldExists = oldExists;
            _oldMode = oldMode;
            _oldOid = oldOid;
            _oldPath = oldPath;
        }

        private readonly bool _exists;
        public override bool Exists => _exists;

        private readonly Mode _mode;
        public override Mode Mode => _mode;

        private readonly ObjectId? _oid;
        public override ObjectId? Oid => _oid;

        private readonly bool _oldExists;
        public override bool OldExists => _oldExists;

        private readonly Mode _oldMode;
        public override Mode OldMode => _oldMode;

        private readonly ObjectId? _oldOid;
        public override ObjectId? OldOid => _oldOid;

        private readonly string? _oldPath;
        public override string? OldPath => _oldPath;

        private readonly string? _path;
        public override string? Path => _path;

        private readonly ChangeKind _status;
        public override ChangeKind Status => _status;
    }
}
