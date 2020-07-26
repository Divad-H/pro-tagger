using LibGit2Sharp;
using System;

namespace ProTaggerTest.LibGit2Mocks
{
    class RepositoryInformationMock : RepositoryInformation
    {
        public override CurrentOperation CurrentOperation
            => throw new NotImplementedException();

        public override bool IsBare
            => throw new NotImplementedException();

        public override bool IsHeadDetached
            => false;

        public override bool IsHeadUnborn
            => throw new NotImplementedException();

        public override bool IsShallow
            => throw new NotImplementedException();

        public override string Message
            => throw new NotImplementedException();

        public override string Path
            => throw new NotImplementedException();

        public override string WorkingDirectory
            => throw new NotImplementedException();
    }
}
