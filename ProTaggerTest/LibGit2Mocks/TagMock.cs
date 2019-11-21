using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProTaggerTest.LibGit2Mocks
{
    class TagMock : Tag
    {
        public TagMock(string name, CommitMock target)
        {
            _canonicalName = name;
            _target = target;
        }

        public override TagAnnotation Annotation => throw new NotImplementedException();

        private readonly string _canonicalName;
        public override string CanonicalName => _canonicalName;
        public override string FriendlyName => _canonicalName;
        public override bool IsAnnotated => throw new NotImplementedException();
        public override GitObject PeeledTarget => throw new NotImplementedException();
        public override Reference Reference => throw new NotImplementedException();
        private readonly CommitMock _target;
        public override GitObject Target => _target;
    }
}
