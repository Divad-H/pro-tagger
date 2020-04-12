using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProTaggerTest.LibGit2Mocks
{
    class PatchMock : Patch
    {
        public PatchMock(string content, int linesAdded, int linesDeleted, IList<PatchEntryChanges> patchEntryChanges)
        {
            _content = content;
            _linesAdded = linesAdded;
            _linesDeleted = linesDeleted;
            _patchEntryChanges = patchEntryChanges;
        }

        private readonly string _content;
        public override string Content => _content;
        private readonly int _linesAdded;
        public override int LinesAdded => _linesAdded;
        private readonly int _linesDeleted;
        public override int LinesDeleted => _linesDeleted;
        private IList<PatchEntryChanges> _patchEntryChanges;
        public override IEnumerator<PatchEntryChanges> GetEnumerator()
            => _patchEntryChanges.GetEnumerator();
        public override PatchEntryChanges this[string path] => throw new NotImplementedException();
    }
}
