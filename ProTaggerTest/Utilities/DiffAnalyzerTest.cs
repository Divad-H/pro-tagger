using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Utilities;
using System.Linq;

namespace ProTaggerTest.Utilities
{
    [TestClass]
    public class DiffAnalyzerTest
    {
        [TestMethod]
        public void SplitLineTest()
        {
            var result = DiffAnalyzer.SplitLine("Ein brauner(Hund   legt,,Eier").ToList();
            Assert.AreEqual(9, result.Count, "Unexpected number of tokens");
            Assert.AreEqual("Ein", result[0]);
            Assert.AreEqual(" ", result[1]);
            Assert.AreEqual("brauner", result[2]);
            Assert.AreEqual("(", result[3]);
            Assert.AreEqual("Hund", result[4]);
            Assert.AreEqual("   ", result[5]);
            Assert.AreEqual("legt", result[6]);
            Assert.AreEqual(",,", result[7]);
            Assert.AreEqual("Eier", result[8]);
        }

        [TestMethod]
        public void SplitIntoHunksTest()
        {
            var result = DiffAnalyzer.SplitIntoHunks(
@"diff --git a/ProTagger/PTagger.cs b/ProTagger/PTagger.cs
index ecddd45..cc38031 100644
--- a/ProTagger/PTagger.cs
+++ b/ProTagger/PTagger.cs
@@ -29,15 +29,15 @@ namespace ProTagger
             }
         }

-        private RepositoryViewModel? _repository;
-        public RepositoryViewModel? Repository
+        private Variant<RepositoryViewModel, string> _repository = new Variant<RepositoryViewModel, string>(""No repository selected."");
+        public Variant<RepositoryViewModel, string> Repository
         {
             get { return _repository; }
             set
             {
                 if (_repository == value)
                     return;
-                _repository?.Dispose();
+                (_repository?.Get() as IDisposable)?.Dispose();
                 _repository = value;
                 NotifyPropertyChanged();
             }
@@ -45,7 +45,7 @@ namespace ProTagger

         public ICommand RefreshCommand { get; }

-        public IObservable<RepositoryViewModel> RepositoryObservable { get; }
+        public IObservable<Variant<RepositoryViewModel, string>> RepositoryObservable { get; }

         public PTagger(IRepositoryFactory repositoryFactory, ISchedulers schedulers)
         {
@@ -91,7 +91,7 @@ namespace ProTagger
         private readonly CompositeDisposable _disposable = new CompositeDisposable();
         public void Dispose()
         {
-            _repository?.Dispose();
+            (_repository?.Get() as IDisposable)?.Dispose();
             _disposable.Dispose();
         }
     }");
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(14, result[0].Diff.Count);
            Assert.IsTrue(result[0].Diff[3].Is<DiffAnalyzer.WordDiff>());
            Assert.AreEqual(2, result[0].Diff[3].Get<DiffAnalyzer.WordDiff>().NewText.Count);
            Assert.AreEqual(2, result[0].Diff[3].Get<DiffAnalyzer.WordDiff>().OldText.Count);
            Assert.AreEqual(29, result[0].NewBeginLine);
            Assert.AreEqual(29, result[0].OldBeginLine);
            Assert.AreEqual(15, result[0].NewHunkLength);
            Assert.AreEqual(15, result[0].OldHunkLength);
            Assert.AreEqual("namespace ProTagger", result[0].AdditionalInfo);
        }

    }
}
