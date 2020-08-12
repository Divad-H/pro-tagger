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
using System.Threading;
using System.Threading.Tasks;
using ProTagger;
using System.Reactive.Subjects;

namespace ProTaggerTest.Repository.Diff
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
            var repoObservable = Observable.Return(repo).Concat(Observable.Never<IRepositoryWrapper>());
            var input = Observable
                .CombineLatest(oldCommit, newCommit, compareOptions, repoObservable, (o, n, c, r) => new DiffViewModelInput(r, o, n, c));

            var vm = new DiffViewModel(new TestSchedulers(), input);
            Variant<List<TreeEntryChanges>, Unexpected>? value = null;
            using var subscription = vm.TreeDiff.Subscribe(treeDiff => value = treeDiff);
            if (value is null)
                throw new Exception("Value was not set.");
            Assert.IsTrue(value.Is<Unexpected>());
            Assert.AreEqual(DiffViewModel.NoCommitSelectedMessage, value.Get<Unexpected>().Message);
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
            var repoObservable = Observable.Return(repo).Concat(Observable.Never<IRepositoryWrapper>());
            var input = Observable
                .CombineLatest(oldCommit, newCommit, compareOptionsObs, repoObservable, (o, n, c, r) => new DiffViewModelInput(r, o, n, c));

            Variant<List<TreeEntryChanges>, Unexpected>? value = null;
            var vm = new DiffViewModel(new TestSchedulers(), input);
            using var _ = vm.TreeDiff
                .Subscribe(val =>
                {
                    value = val;
                    if (val.Is<List<TreeEntryChanges>>())
                        signal.Release();
                });

            if (value is null)
                value = vm.TreeDiff.Value;
            if (value is null)
                throw new Exception("TreeDiff was not set.");
            if (!value.Is<List<TreeEntryChanges>>())
                await signal.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(value.Is<List<TreeEntryChanges>>());
        }

        [TestMethod]
        public void UpdatesSelectionInfo()
        {
            var shaGenerator = new PseudoShaGenerator();
            var firstCommit = CommitMock.GenerateCommit(shaGenerator, null, 0);
            var secondCommit = CommitMock.GenerateCommit(shaGenerator, firstCommit.Yield(), 1);
            var diff = new DiffMock(null, (_1, _2, _3) => new TreeChangesMock(new List<TreeEntryChanges>()));
            var head = new BranchMock(true, false, null, secondCommit, "HEAD");
            var compareOptions = new CompareOptions();
            var compareOptionsObs = Observable.Return(compareOptions);
            var repo = new RepositoryMock(new CommitMock[] { firstCommit, secondCommit }, new BranchCollectionMock(head.Yield().ToList()), null, diff);
            using var oldCommit = new BehaviorSubject<Variant<Commit, DiffTargets>?>(null);
            using var newCommit = new BehaviorSubject<Variant<Commit, DiffTargets>?>(null);
            var repoObservable = Observable.Return(repo).Concat(Observable.Never<IRepositoryWrapper>());
            var input = Observable
                .CombineLatest(oldCommit, newCommit, compareOptionsObs, repoObservable, (o, n, c, r) => new DiffViewModelInput(r, o, n, c));
            using var vm = new DiffViewModel(new TestSchedulers(), input);
            var selectionInfo = new List<Variant<string, Unexpected, List<TreeEntryChanges>, Commit>>();
            var resetEvent = new AutoResetEvent(false);
            using var _ = vm.SelectionInfo
                .Subscribe(info =>
                {
                    selectionInfo.Add(info);
                    resetEvent.Set();
                });
            resetEvent.WaitOne(TimeSpan.FromSeconds(5));
            newCommit.OnNext(DiffTargets.WorkingDirectory);
            resetEvent.WaitOne(TimeSpan.FromSeconds(5));
            oldCommit.OnNext(secondCommit);
            resetEvent.WaitOne(TimeSpan.FromSeconds(5));
            newCommit.OnNext(firstCommit);
            resetEvent.WaitOne(TimeSpan.FromSeconds(5));
            oldCommit.OnNext(null);
            resetEvent.WaitOne(TimeSpan.FromSeconds(5));

            Assert.AreEqual(5, selectionInfo.Count);
            Assert.AreEqual(0, selectionInfo[0].VariantIndex);
            Assert.AreEqual(2, selectionInfo[1].VariantIndex);
            Assert.AreEqual(0, selectionInfo[2].VariantIndex);
            Assert.AreEqual(0, selectionInfo[3].VariantIndex);
            Assert.AreEqual(3, selectionInfo[4].VariantIndex);
            Assert.AreEqual(firstCommit, selectionInfo[4].Fourth);
        }
    }
}
