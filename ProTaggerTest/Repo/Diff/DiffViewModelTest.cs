using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Repo.Diff;
using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using ProTaggerTest.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using ReactiveMvvm;
using System.Threading;
using System.Threading.Tasks;

namespace ProTaggerTest.Repo.Diff
{
    [TestClass]
    public class DiffViewModelTest
    {
        [TestMethod]
        public void CanCreateInstance()
        {
            var diff = new DiffMock(null, null);
            var oldCommit = Observable.Return((Commit?)null);
            var newCommit = Observable.Return((Commit?)null);
            var compareOptions = Observable.Return(new CompareOptions());

            var vm = new DiffViewModel(diff, new TestSchedulers(), oldCommit, newCommit, compareOptions);
            Assert.IsTrue(vm.TreeDiff.Is<string>());
            Assert.AreEqual(DiffViewModel.NoCommitSelectedMessage, vm.TreeDiff.Get<string>());
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
            var oldCommit = Observable.Return(firstCommit);
            var newCommit = Observable.Return(secondCommit);
            var compareOptionsObs = Observable.Return(compareOptions);

            var vm = new DiffViewModel(diff, new TestSchedulers(), oldCommit, newCommit, compareOptionsObs);
            using var _ = vm
                .FromProperty(vm => vm.TreeDiff)
                .Subscribe(val =>
                {
                    if (val.Is<List<TreeEntryChanges>>())
                        signal.Release();
                });

            if (!vm.TreeDiff.Is<List<TreeEntryChanges>>())
                await signal.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(vm.TreeDiff.Is<List<TreeEntryChanges>>());
        }
    }
}
