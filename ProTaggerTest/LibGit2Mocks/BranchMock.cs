using LibGit2Sharp;
using System;

namespace ProTaggerTest.LibGit2Mocks
{
    class BranchMock : Branch
    {
        public BranchMock(bool isCurrentRepositoryHead, bool isRemote, string? remoteName, CommitMock tip)
        {
            _isCurrentRepositoryHead = isCurrentRepositoryHead;
            _isRemote = isRemote;
            _remoteName = remoteName;
            _tip = tip;
        }

        public override string CanonicalName => throw new NotImplementedException();
        public override ICommitLog Commits => throw new NotImplementedException();
        public override string FriendlyName => throw new NotImplementedException();

        private readonly bool _isCurrentRepositoryHead;
        public override bool IsCurrentRepositoryHead => _isCurrentRepositoryHead;

        private readonly bool _isRemote;
        public override bool IsRemote => _isRemote;

        public override bool IsTracking => throw new NotImplementedException();
        public override Reference Reference => throw new NotImplementedException();

        private readonly string? _remoteName;
        public override string? RemoteName => _remoteName;

        public override TreeEntry this[string relativePath] => throw new NotImplementedException();

        private readonly CommitMock _tip;
        public override Commit Tip => _tip;
        
        public override Branch TrackedBranch => throw new NotImplementedException();
        public override BranchTrackingDetails TrackingDetails => throw new NotImplementedException();
        public override string UpstreamBranchCanonicalName => throw new NotImplementedException();
    }
}
