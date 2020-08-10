using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProTaggerTest.LibGit2Mocks
{
    class RepositoryStatusMock : RepositoryStatus
    {
        private readonly List<StatusEntry> _statusEntries;

        public RepositoryStatusMock()
            => _statusEntries = new List<StatusEntry>();

        public override IEnumerable<StatusEntry> Added
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Ignored
            => throw new NotImplementedException();

        public override bool IsDirty
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Missing
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Modified
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Removed
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> RenamedInIndex
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> RenamedInWorkDir
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Staged
            => throw new NotImplementedException();

        public override StatusEntry this[string path]
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Unaltered
            => throw new NotImplementedException();

        public override IEnumerable<StatusEntry> Untracked
            => throw new NotImplementedException();

        public override IEnumerator<StatusEntry> GetEnumerator()
            => _statusEntries.GetEnumerator();
    }
}
