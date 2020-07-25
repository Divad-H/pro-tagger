using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp;

namespace ProTaggerTest.LibGit2Mocks
{
    class ReferenceCollectionMock : ReferenceCollection
    {
        private Dictionary<string, Reference> _refs;

        public ReferenceCollectionMock(IList<TagMock> tags, IList<BranchMock> branches)
            => _refs = tags.Select(t => t.Reference).Concat(branches.Select(b => b.Reference)).ToDictionary(r => r.CanonicalName);
        public override IEnumerator<Reference> GetEnumerator()
        {
            return _refs
                .Select(keyVal => keyVal.Value)
                .GetEnumerator();
        }

        public override DirectReference Add(string name, ObjectId targetId)
            => throw new NotImplementedException();

        public override DirectReference Add(string name, ObjectId targetId, bool allowOverwrite)
            => throw new NotImplementedException();

        public override DirectReference Add(string name, ObjectId targetId, string logMessage)
            => throw new NotImplementedException();

        public override DirectReference Add(string name, ObjectId targetId, string logMessage, bool allowOverwrite)
            => throw new NotImplementedException();

        public override SymbolicReference Add(string name, Reference targetRef)
            => throw new NotImplementedException();

        public override SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite)
            => throw new NotImplementedException();

        public override SymbolicReference Add(string name, Reference targetRef, string logMessage)
            => throw new NotImplementedException();

        public override SymbolicReference Add(string name, Reference targetRef, string logMessage, bool allowOverwrite)
            => throw new NotImplementedException();

        public override Reference Add(string name, string canonicalRefNameOrObjectish)
            => throw new NotImplementedException();

        public override Reference Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite)
            => throw new NotImplementedException();

        public override Reference Add(string name, string canonicalRefNameOrObjectish, string logMessage)
            => throw new NotImplementedException();

        public override Reference Add(string name, string canonicalRefNameOrObjectish, string logMessage, bool allowOverwrite)
            => throw new NotImplementedException();

        public override IEnumerable<Reference> FromGlob(string pattern)
            => throw new NotImplementedException();

        public override Reference Head
            => throw new NotImplementedException();

        public override ReflogCollection Log(Reference reference)
            => throw new NotImplementedException();

        public override ReflogCollection Log(string canonicalName)
            => throw new NotImplementedException();

        public override IEnumerable<Reference> ReachableFrom(IEnumerable<Commit> targets)
            => throw new NotImplementedException();

        public override IEnumerable<Reference> ReachableFrom(IEnumerable<Reference> refSubset, IEnumerable<Commit> targets)
            => throw new NotImplementedException();

        public override void Remove(Reference reference)
            => throw new NotImplementedException();

        public override void Remove(string name)
            => throw new NotImplementedException();

        public override Reference Rename(Reference reference, string newName)
            => throw new NotImplementedException();

        public override Reference Rename(Reference reference, string newName, bool allowOverwrite)
            => throw new NotImplementedException();

        public override Reference Rename(Reference reference, string newName, string logMessage)
            => throw new NotImplementedException();

        public override Reference Rename(Reference reference, string newName, string logMessage, bool allowOverwrite)
            => throw new NotImplementedException();

        public override Reference Rename(string currentName, string newName)
            => throw new NotImplementedException();

        public override Reference Rename(string currentName, string newName, bool allowOverwrite)
            => throw new NotImplementedException();

        public override Reference Rename(string currentName, string newName, string logMessage)
            => throw new NotImplementedException();

        public override Reference Rename(string currentName, string newName, string logMessage, bool allowOverwrite)
            => throw new NotImplementedException();

        public override void RewriteHistory(RewriteHistoryOptions options, IEnumerable<Commit> commitsToRewrite)
            => throw new NotImplementedException();

        public override void RewriteHistory(RewriteHistoryOptions options, params Commit[] commitsToRewrite)
            => throw new NotImplementedException();

        public override Reference this[string name]
            => _refs[name];

        public override Reference UpdateTarget(Reference directRef, ObjectId targetId)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(Reference directRef, ObjectId targetId, string logMessage)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(Reference directRef, string objectish)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(Reference directRef, string objectish, string logMessage)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(Reference symbolicRef, Reference targetRef)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(Reference symbolicRef, Reference targetRef, string logMessage)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(string name, string canonicalRefNameOrObjectish)
            => throw new NotImplementedException();

        public override Reference UpdateTarget(string name, string canonicalRefNameOrObjectish, string logMessage)
            => throw new NotImplementedException();
    }
}
