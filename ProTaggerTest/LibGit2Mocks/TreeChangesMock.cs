using LibGit2Sharp;
using System.Collections.Generic;
using System.Linq;

namespace ProTaggerTest.LibGit2Mocks
{
    class TreeChangesMock : TreeChanges
    {
        public TreeChangesMock(IList<TreeEntryChanges> treeEntryChanges)
            => _treeEntryChanges = treeEntryChanges;

        private readonly IList<TreeEntryChanges> _treeEntryChanges;
        public override IEnumerator<TreeEntryChanges> GetEnumerator()
            => _treeEntryChanges.GetEnumerator();

        public override int Count => _treeEntryChanges.Count;

        public override IEnumerable<TreeEntryChanges> Added 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Added);
        public override IEnumerable<TreeEntryChanges> Conflicted 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Conflicted);
        public override IEnumerable<TreeEntryChanges> Copied 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Copied);
        public override IEnumerable<TreeEntryChanges> Deleted 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Deleted);
        public override IEnumerable<TreeEntryChanges> Modified 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Modified);
        public override IEnumerable<TreeEntryChanges> Renamed 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Renamed);
        public override IEnumerable<TreeEntryChanges> TypeChanged 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.TypeChanged);
        public override IEnumerable<TreeEntryChanges> Unmodified 
            => _treeEntryChanges.Where(c => c.Status == ChangeKind.Unmodified);
    }
}
