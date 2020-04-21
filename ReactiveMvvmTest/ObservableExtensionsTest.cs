using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReacitveMvvm;
using System;
using System.Reactive.Linq;

namespace ReactiveMvvmTest
{
    [TestClass]
    public class ObservableExtensionsTest
    {
        [TestMethod]
        public void SkipNullTest()
        {
            var sources = Observable
                .Return<string?>("str")
                .Concat(Observable.Return((string?)null))
                .Repeat(3);

            var result = sources.SkipNull();
            Assert.IsTrue(result is IObservable<string>);

            int numValues = 0;
            result.Subscribe(val =>
            {
                ++numValues;
                Assert.IsNotNull(val);
            }).Dispose();
            Assert.AreEqual(3, numValues);
        }
    }
}
