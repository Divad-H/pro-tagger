using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger;
using ProTagger.RepositorySelection;
using ProTaggerTest.Mocks;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProTaggerTest.Repository
{
    [TestClass]
    public class RepositoryViewModelTest
    {
        [TestMethod]
        public async Task CanCreateRepositoryViewModel()
        {
            var schedulers = new TestSchedulers();
            var repositoryFactory = new TestRepositoryFactory();
            var repositoryDescription = new RepositoryDescription("Name", "PATH TO REPO");
            var res = await RepositoryViewModel.Create(
                schedulers,
                CancellationToken.None,
                repositoryFactory,
                repositoryDescription,
                Observable.Return(new CompareOptions()));
            Assert.IsTrue(res.Is<RepositoryViewModel>());
            using var repo = res.Get<RepositoryViewModel>();
            Assert.AreEqual(repositoryDescription, repo.RepositoryDescription);
        }
    }
}
