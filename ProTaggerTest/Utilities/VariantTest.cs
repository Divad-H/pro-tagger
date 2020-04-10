using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger;
using ProTagger.Utilities;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System;
using System.Reactive.Disposables;
using System.Linq;

namespace ProTaggerTest
{
    [TestClass]
    public class VariantTest
    {
        [TestMethod]
        public void IntStringVariant()
        {
            const int intVal = 3;
            const string strVal = "rat";
            var variant = new Variant<string, int>(intVal);
            Assert.IsTrue(variant.Is<int>());
            Assert.IsFalse(variant.Is<string>());
            Assert.AreEqual(intVal, variant.Get<int>());
            Assert.AreEqual(3, variant.Second);
            variant.Assign(strVal);
            Assert.IsTrue(variant.Is<string>());
            Assert.IsFalse(variant.Is<int>());
            Assert.AreEqual(strVal, variant.Get<string>());
            Assert.AreEqual(strVal, variant.First);
        }

        [TestMethod]
        public void ObservableMerge()
        {
            var first = new Subject<int>();
            var second = new Subject<string>();
            var merged = first.MergeVariant(second);
            Variant<int, string>? res = null;
            using var _ = merged.Subscribe(m => res = m);
            Assert.IsNull(res);
            first.OnNext(1);
            if (res == null)
            {
                Assert.IsNotNull(res);
                throw new Exception("Unreachable");
            }
            Assert.IsTrue(res.Is<int>());
            Assert.AreEqual(1, res.Get<int>());
            first.OnNext(2);
            Assert.IsTrue(res.Is<int>());
            Assert.AreEqual(2, res.Get<int>());
            second.OnNext("A");
            Assert.IsTrue(res.Is<string>());
            Assert.AreEqual("A", res.Get<string>());
            first.OnNext(3);
            Assert.IsTrue(res.Is<int>());
            Assert.AreEqual(3, res.Get<int>());
        }

        [TestMethod]
        public void ObservableFirstSecondVariant()
        {
            var obs = Observable.Create<Variant<int, string>>(o =>
            {
                o.OnNext(new Variant<int, string>(1));
                o.OnNext(new Variant<int, string>(2));
                o.OnNext(new Variant<int, string>("3"));
                o.OnNext(new Variant<int, string>(4));
                o.OnCompleted();
                return Disposable.Empty;
            });
            var first = obs
                .FirstVariant()
                .ToList()
                .Wait();
            var second = obs
                .SecondVariant()
                .ToList()
                .Wait();
            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(1, first[0]);
            Assert.AreEqual(2, first[1]);
            Assert.AreEqual(4, first[2]);
            Assert.AreEqual(1, second.Count);
            Assert.AreEqual("3", second[0]);
        }
    }
}
