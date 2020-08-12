using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger;
using ProTagger.RepositorySelection;
using ProTaggerTest.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
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
                new TestFileSystem(),
                repositoryDescription,
                Observable.Return(new CompareOptions()));
            Assert.IsTrue(res.Is<RepositoryViewModel>());
            using var repo = res.Get<RepositoryViewModel>();
            Assert.AreEqual(repositoryDescription, repo.RepositoryDescription);
        }

        [TestMethod]
        public async Task CanSelectBranches()
        {
            var schedulers = new TestSchedulers();
            var repositoryFactory = new SimpleRepositoryFactoryMock();
            var repositoryDescription = new RepositoryDescription("Name", "PATH TO REPO");
            var res = await RepositoryViewModel.Create(
                schedulers,
                CancellationToken.None,
                repositoryFactory,
                new TestFileSystem(),
                repositoryDescription,
                Observable.Return(new CompareOptions()));
            Assert.IsTrue(res.Is<RepositoryViewModel>());
            using var repo = res.Get<RepositoryViewModel>();
            var refs = repo.References.Value;
            Assert.IsNotNull(refs);
            Assert.AreEqual(2, refs!.Branches.Refs.Count);
            var master = refs.Branches.Refs.Where(b => b.FriendlyName == "master").FirstOrDefault()
                ?? throw new InvalidOperationException("Branch master not found.");
            var work = refs.Branches.Refs.Where(b => b.FriendlyName == "work").FirstOrDefault()
                ?? throw new InvalidOperationException("Branch work not found.");
            Assert.IsTrue(master.Selected.Value, "Branch master (HEAD) should be selected.");
            Assert.IsFalse(work.Selected.Value, "Branch work should not be selected.");
            Assert.IsTrue(repo.Graph.LogGraphNodes.Value.VariantIndex == 0);
            var logGraphNodes = repo.Graph.LogGraphNodes.Value.First;
            using var _1 = repo.Graph.LogGraphNodes
                .Subscribe(nodes => logGraphNodes = nodes.VariantIndex == 0
                        ? nodes.First : throw new InvalidOperationException(nodes.Second.Message));
            IList<RefSelection>? selectedBranches = null;
            refs = repo.References.Value;
            using var _2 = refs!.Branches.SelectedRefs
                .Subscribe(sb => selectedBranches = sb);
            Assert.AreEqual(1, selectedBranches.Where(b => b.Selected).Count());
            Assert.AreEqual(7, logGraphNodes.Count); // The mock doesn't filter unreachable commits.
            var nodesWithBranch = logGraphNodes.Where(c => c.Branches.Any());
            Assert.AreEqual(2, nodesWithBranch.Count());
            Assert.AreEqual("master", nodesWithBranch.First().Branches.First().FirendlyName);
            work.SelectCommand.Execute(true);
            Assert.AreEqual(2, logGraphNodes.Where(c => c.Branches.Any()).Count());
            _ = selectedBranches ?? throw new InvalidOperationException("Selected branches were not set.");
            Assert.AreEqual(2, selectedBranches.Where(b => b.Selected).Count());
        }
    }
}
