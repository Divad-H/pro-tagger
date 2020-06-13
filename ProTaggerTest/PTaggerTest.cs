using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger;
using ProTagger.Utilities;
using ProTaggerTest.LibGit2Mocks;
using ProTaggerTest.Mocks;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
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
