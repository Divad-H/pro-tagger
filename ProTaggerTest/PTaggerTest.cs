using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger;
using ProTagger.RepositorySelection;
using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using ProTaggerTest.Mocks;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProTaggerTest
{
    [TestClass]
    public class PTaggerTest
    {
        class TestRepositoryFactory : IRepositoryFactory
        {
            public IRepositoryWrapper CreateRepository(string path)
                => new RepositoryMock(new List<CommitMock>(), new BranchCollectionMock(new List<BranchMock>()));

            public string? DiscoverRepository(string path)
                => path;

            public bool IsValidRepository(string path)
                => true;

            public string? RepositoryNameFromPath(string path)
                => "Repository name";
        }

        [TestMethod]
        public void CanCreatePTagger()
        {
            using var _ = new PTagger(new TestRepositoryFactory(), new TestSchedulers(), new TestFileSystem(), Observable.Never<string>());
        }

        [TestMethod]
        public void CanOpenNewTab()
        {
            using var pTagger = new PTagger(new TestRepositoryFactory(), new TestSchedulers(), new TestFileSystem(), Observable.Never<string>());
            Assert.IsTrue(pTagger.NewTabCommand.CanExecute(null));
            var numOpenTabsAtBeginning = pTagger.OpenedRepositories.Count;
            pTagger.NewTabCommand.Execute(null);
            Assert.AreEqual(numOpenTabsAtBeginning + 1, pTagger.OpenedRepositories.Count);
            Assert.IsTrue(pTagger.OpenedRepositories.Last().Is<RepositorySelectionViewModel>());
        }

        [TestMethod]
        public void CanCloseTab()
        {
            using var pTagger = new PTagger(new TestRepositoryFactory(), new TestSchedulers(), new TestFileSystem(), Observable.Never<string>());
            Assert.IsTrue(pTagger.NewTabCommand.CanExecute(null));
            var numOpenTabsAtBeginning = pTagger.OpenedRepositories.Count;
            pTagger.NewTabCommand.Execute(null);
            Assert.AreEqual(numOpenTabsAtBeginning + 1, pTagger.OpenedRepositories.Count);
            Assert.IsTrue(pTagger.CloseTabCommand.CanExecute(null));
            pTagger.CloseTabCommand.Execute(pTagger.OpenedRepositories.Last());
            Assert.AreEqual(numOpenTabsAtBeginning, pTagger.OpenedRepositories.Count);
        }

        [TestMethod]
        public void CanCloseAllTabs()
        {
            using var pTagger = new PTagger(new TestRepositoryFactory(), new TestSchedulers(), new TestFileSystem(), Observable.Never<string>());
            pTagger.NewTabCommand.Execute(null);
            pTagger.NewTabCommand.Execute(null);
            pTagger.NewTabCommand.Execute(null);
            Assert.IsTrue(pTagger.CloseAllTabsCommand.CanExecute(null));
            pTagger.CloseAllTabsCommand.Execute(null);
            Assert.IsFalse(pTagger.CloseAllTabsCommand.CanExecute(null));
            Assert.IsFalse(pTagger.OpenedRepositories.Any());
        }

        //[TestMethod]
        //public async Task RefreshRepositoryTest()
        //{
        //    using var _diposables = new CompositeDisposable();
        //    using var signal = new SemaphoreSlim(0, 1);
        //    using var vm = new PTagger(new TestRepositoryFactory(), new TestSchedulers());
        //    RepositoryViewModel? repositoryViewModel = null;
        //    bool first = true;
        //    vm.Repository
        //        .Subscribe(repoVM =>
        //        {
        //            if (first)
        //            {
        //                first = false;
        //                Assert.IsTrue(repoVM.Is<string>());
        //                return;
        //            }
        //            Assert.IsTrue(repoVM.Is<RepositoryViewModel>());
        //            repositoryViewModel = repoVM.Get<RepositoryViewModel>();
        //            signal.Release();
        //        })
        //        .DisposeWith(_diposables);
        //    await signal.WaitAsync(TimeSpan.FromSeconds(10));
        //    vm.RepositoryPath.Value = @"../../";
        //    Assert.IsTrue(vm.RefreshCommand.CanExecute(null));
        //    vm.RefreshCommand.Execute(null);
        //    await signal.WaitAsync(TimeSpan.FromSeconds(10));
        //    Assert.IsNotNull(repositoryViewModel);
        //}
    }
}
