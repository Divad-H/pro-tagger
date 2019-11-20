using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProTaggerTest.LibGit2Mocks
{
    class BranchCollectionMock : BranchCollection
    {
        public BranchCollectionMock(IList<BranchMock> branches)
        {
            _branches = branches.ToDictionary(b => b.CanonicalName);
        }
        private readonly IDictionary<string, BranchMock> _branches;
        public override IEnumerator<Branch> GetEnumerator()
        {
            return _branches
                .Select(keyVal => keyVal.Value)
                .GetEnumerator();
        }
        public override Branch this[string name] => _branches[name];

        public override Branch Add(string name, Commit commit)
        {
            throw new NotImplementedException();
        }
        public override Branch Add(string name, Commit commit, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override Branch Add(string name, string committish)
        {
            throw new NotImplementedException();
        }
        public override Branch Add(string name, string committish, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override void Remove(Branch branch)
        {
            throw new NotImplementedException();
        }
        public override void Remove(string name)
        {
            throw new NotImplementedException();
        }
        public override void Remove(string name, bool isRemote)
        {
            throw new NotImplementedException();
        }
        public override Branch Rename(Branch branch, string newName)
        {
            throw new NotImplementedException();
        }
        public override Branch Rename(Branch branch, string newName, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override Branch Rename(string currentName, string newName)
        {
            throw new NotImplementedException();
        }
        public override Branch Rename(string currentName, string newName, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override Branch Update(Branch branch, params Action<BranchUpdater>[] actions)
        {
            throw new NotImplementedException();
        }
    }
}
