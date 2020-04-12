using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace ProTaggerTest.LibGit2Mocks
{
    class DiffMock : Diff
    {
        private readonly Func<Tree, Tree, IEnumerable<string>, CompareOptions, Patch>? _patchMock;
        private readonly Func<Tree, Tree, CompareOptions, TreeChanges>? _treeDiffMock;

        public DiffMock(Func<Tree, Tree, IEnumerable<string>, CompareOptions, Patch>? patchMock, 
            Func<Tree, Tree, CompareOptions, TreeChanges>? treeDiffMock)
            => (_patchMock, _treeDiffMock) = (patchMock, treeDiffMock);

        public override T Compare<T>(Tree oldTree, Tree newTree, IEnumerable<string> paths, CompareOptions compareOptions)
            => _patchMock?.Invoke(oldTree, newTree, paths, compareOptions) as T ?? throw new ArgumentException();

        public override T Compare<T>(Tree oldTree, Tree newTree, CompareOptions compareOptions)
            => _treeDiffMock?.Invoke(oldTree, newTree, compareOptions) as T ?? throw new ArgumentException();
    }
}
