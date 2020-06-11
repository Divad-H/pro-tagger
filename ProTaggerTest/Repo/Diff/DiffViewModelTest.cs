using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Repository.Diff;
using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using ProTaggerTest.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ReactiveMvvm;
using System.Threading;
using System.Threading.Tasks;
using ProTagger;

namespace ProTaggerTest.Repo.Diff
{
    [TestClass]
    public class DiffViewModelTest
    {
        [TestMethod]
        public void CanCreateInstance()
        {
            var diff = new DiffMock(null, null);
            var oldCommit = Observable.Return((Variant<Commit, DiffTargets>?)null).Concat(Observable.Never<Variant<Commit, DiffTargets>?>());
            var newCommit = Observable.Return((Variant<Commit, DiffTargets>?)null).Concat(Observable.Never<Variant<Commit, DiffTargets>?>());
            var compareOptions = Observable.Return(new CompareOptions()).Concat(Observable.Never<CompareOptions>());
            var repo = new RepositoryMock(new List<CommitMock>(), new BranchCollectionMock(new List<BranchMock>()), null, diff);

            var vm = new DiffViewModel(repo, new TestSchedulers(), null, oldCommit, newCommit, compareOptions);
            Variant<List<TreeEntryChanges>, string>? value = null;
            using var subscription = vm.TreeDiff.Subscribe(treeDiff => value = treeDiff);
            if (value is null)
                throw new Exception("Value was not set.");
            Assert.IsTrue(value.Is<string>());
            Assert.AreEqual(DiffViewModel.NoCommitSelectedMessage, value.Get<string>());
        }

        [TestMethod]
        public async Task UpdatesTreeDiff()
        {
            using var signal = new SemaphoreSlim(0, 1);
            
            var shaGenerator = new PseudoShaGenerator();
            var addedFile = new TreeEntryChangesMock(ChangeKind.Added, 
                true, Mode.NonExecutableFile, new ObjectId(shaGenerator.Generate()), "some/p.ath", 
                false, Mode.Nonexistent, null, null);
            var firstCommit = CommitMock.GenerateCommit(shaGenerator, null, 0);
            var secondCommit = CommitMock.GenerateCommit(shaGenerator, firstCommit.Yield(), 1);
            var compareOptions = new CompareOptions();
            var diff = new DiffMock(null, (oldTree, newTree, CompareOptions) 
                => (oldTree, newTree, CompareOptions) == (firstCommit.Tree, secondCommit.Tree, compareOptions)
                    ? new TreeChangesMock(((TreeEntryChanges)addedFile).Yield().ToList())
                    : throw new Exception("Compare was called with wrong arguments."));
            var oldCommit = Observable.Return(new Variant<Commit, DiffTargets>(firstCommit));
            var newCommit = Observable.Return(new Variant<Commit, DiffTargets>(secondCommit));
            var head = new BranchMock(true, false, null, secondCommit, "HEAD");
            var compareOptionsObs = Observable.Return(compareOptions);
            var repo = new RepositoryMock(new CommitMock[] { firstCommit, secondCommit }, new BranchCollectionMock(head.Yield().ToList()), null, diff);

            Variant<List<TreeEntryChanges>, string>? value = null;
            var vm = new DiffViewModel(repo, new TestSchedulers(), head, oldCommit, newCommit, compareOptionsObs);
            using var _ = vm
                .TreeDiff
                .Subscribe(val =>
                {
                    value = val;
                    if (val.Is<List<TreeEntryChanges>>())
                        signal.Release();
                });

            if (value is null)
                throw new Exception("TreeDiff was not set.");
            if (!value.Is<List<TreeEntryChanges>>())
                await signal.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(value.Is<List<TreeEntryChanges>>());
        }
    }
}
