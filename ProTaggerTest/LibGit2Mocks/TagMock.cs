using LibGit2Sharp;
using System;

namespace ProTaggerTest.LibGit2Mocks
{
    class TagMock : Tag
    {
        public TagMock(string name, CommitMock target)
        {
            _canonicalName = name;
            _target = target;
            _reference = new ReferenceMock(this);
        }

        public override TagAnnotation Annotation => throw new NotImplementedException();

        private readonly string _canonicalName;
        public override string CanonicalName => _canonicalName;
        public override string FriendlyName => _canonicalName;
        public override bool IsAnnotated => throw new NotImplementedException();
        public override GitObject PeeledTarget => throw new NotImplementedException();
        private readonly ReferenceMock _reference;
        public override Reference Reference => _reference;
        private readonly CommitMock _target;
        public override GitObject Target => _target;
    }
}
