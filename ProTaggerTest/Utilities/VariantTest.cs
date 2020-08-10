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
        public void CanCreateFirstVariant()
        {
            const string strVal = "rat";
            var variant = new Variant<string, int>(strVal);
            Assert.IsTrue(variant.Is<string>());
            Assert.IsFalse(variant.Is<int>());
            Assert.AreEqual(strVal, variant.Get<string>());
            Assert.AreEqual(strVal, variant.First);
        }

        [TestMethod]
        public void CanCreateSecondVariant()
        {
            const int intVal = 3;
            var variant = new Variant<string, int>(intVal);
            Assert.IsTrue(variant.Is<int>());
            Assert.IsFalse(variant.Is<string>());
            Assert.AreEqual(intVal, variant.Get<int>());
            Assert.AreEqual(intVal, variant.Second);
        }

        [TestMethod]
        public void CanCreateThreeVariantsVariant()
        {
            const int intVal = 3;
            var variant = new Variant<string, int, double>(intVal);
            Assert.IsTrue(variant.Is<int>());
            Assert.IsFalse(variant.Is<string>());
            Assert.IsFalse(variant.Is<double>());
            Assert.AreEqual(intVal, variant.Get<int>());
            Assert.AreEqual(intVal, variant.Second);
        }

        [TestMethod]
        public void CanCreateFourVariantsVariant()
        {
            const int intVal = 3;
            var variant = new Variant<string, bool, double, int>(intVal);
            Assert.IsTrue(variant.Is<int>());
            Assert.IsFalse(variant.Is<string>());
            Assert.IsFalse(variant.Is<double>());
            Assert.AreEqual(intVal, variant.Get<int>());
            Assert.AreEqual(intVal, variant.Fourth);
        }

        [TestMethod]
        public void IdenticalVariantsAreEqual()
        {
            const string strVal = "rat";
            var first = new Variant<string, int>(strVal);
            var second = new Variant<string, int>(strVal);
            Assert.AreEqual(first, second);
            Assert.IsTrue(first == second);
            Assert.IsFalse(first != second);
        }

        [TestMethod]
        public void UnidenticalVariantsAreNotEqual()
        {
            const string strVal = "rat";
            const int intVal = 2;
            var first = new Variant<string, int>(strVal);
            var second = new Variant<string, int>(intVal);
            Assert.AreNotEqual(first, second);
            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
        }

        [TestMethod]
        public void VariantsWithSameValueAreEqual()
        {
            const string strVal = "rat";
            var first = new Variant<string, int>(strVal);
            var second = new Variant<string, bool>(strVal);
            Assert.AreEqual(first, second);
            Assert.IsTrue(first == second);
            Assert.IsFalse(first != second);
        }

        [TestMethod]
        public void VariantsWithDifferentValuesAreNotEqual()
        {
            var first = new Variant<string, int>(0);
            var second = new Variant<string, bool>(false);
            Assert.AreNotEqual(first, second);
            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
        }

        [TestMethod]
        public void CanCompareNestedVariants()
        {
            const string strVal = "rat";
            var first = new Variant<string, Variant<int, string, bool>>(new Variant<int, string, bool>(strVal));
            var second = new Variant<string, bool>(strVal);
            Assert.AreNotEqual(first, second);
            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
            var third = new Variant<int, string, bool>(strVal);
            Assert.IsFalse(third.Equals(first));
            Assert.IsFalse(first.Equals(third));
            Assert.IsFalse(first == third);
            Assert.IsTrue(first != third);
            var fourth = new Variant<bool, int, double, Variant<bool, string>>(new Variant<bool, string>(strVal));
            Assert.AreEqual(first, fourth);
            Assert.IsTrue(first == fourth);
            Assert.IsFalse(first != fourth);
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
            if (res is null)
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
                o.OnNext(1);
                o.OnNext(2);
                o.OnNext("3");
                o.OnNext(4);
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
