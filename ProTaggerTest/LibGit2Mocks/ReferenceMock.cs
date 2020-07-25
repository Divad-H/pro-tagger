using LibGit2Sharp;
using ProTagger;
using System;

namespace ProTaggerTest.LibGit2Mocks
{
    class ReferenceMock : Reference
    {
        private readonly Variant<BranchMock, TagMock> _data;

        public ReferenceMock(BranchMock branch)
            => _data = new Variant<BranchMock, TagMock>(branch);

        public ReferenceMock(TagMock tag)
            => _data = new Variant<BranchMock, TagMock>(tag);

        public override string CanonicalName
            => _data.Visit(b => b.CanonicalName, t => t.CanonicalName);
        public override bool IsLocalBranch
            => _data.Visit(b => !(b.IsRemote), t => false);
        public override bool IsNote
            => throw new NotImplementedException();
        public override bool IsRemoteTrackingBranch
            => throw new NotImplementedException();
        public override bool IsTag
            => _data.Is<TagMock>();
        public override string TargetIdentifier
            => throw new NotImplementedException();

        public override DirectReference ResolveToDirectReference()
            => throw new NotImplementedException();
    }
}
