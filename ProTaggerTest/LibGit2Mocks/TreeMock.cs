using LibGit2Sharp;
using System;

namespace ProTaggerTest.LibGit2Mocks
{
    class TreeMock : Tree
    {
        public TreeMock(string sha)
            => _sha = sha;

        public override int Count => base.Count;

        public override ObjectId Id => throw new NotImplementedException();

        private readonly string _sha;
        public override string Sha => _sha;

        public override TreeEntry this[string relativePath] => throw new NotImplementedException();

        public override T Peel<T>() => throw new NotImplementedException();
    }
}
