using Microsoft.VisualStudio.TestTools.UnitTesting;
using procom_tagger;
using procom_tagger.Utilities;
using procom_tagger_test.LibGit2Mocks;
using ReacitveMvvm;
using ReactiveMvvm;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace procom_tagger_test
{
    [TestClass]
    public class ProcomTaggerTest
    {
        class TestSchedulers : ISchedulers
        {
            public IScheduler Dispatcher => Scheduler.Immediate;
        }

        class TestRepositoryFactory : IRepositoryFactory
        {
            public IRepositoryWrapper CreateRepository(string path)
            {
                return new RepositoryMock(new List<CommitMock>(), new BranchCollectionMock(new Dictionary<string, BranchMock>()));
            }
        }

        [TestMethod]
        public async Task RefreshRepositoryTest()
        {
            using var _diposables = new CompositeDisposable();
            using var signal = new SemaphoreSlim(0, 1);
            using var vm = new ProcomTagger(new TestRepositoryFactory(), new TestSchedulers());
            RepositoryViewModel? repositoryViewModel = null;
            vm.RepositoryObservable
                .Subscribe(repoVM =>
                {
                    repositoryViewModel = repoVM;
                    signal.Release();
                })
                .DisposeWith(_diposables);
            await signal.WaitAsync(TimeSpan.FromSeconds(10));
            vm.RepositoryPath = @"../../";
            Assert.IsTrue(vm.RefreshCommand.CanExecute(null));
            vm.RefreshCommand.Execute(null);
            await signal.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsNotNull(repositoryViewModel);
        }
    }
}
