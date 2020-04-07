using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTaggerTest.Wpf
{
    [TestClass]
    public class EnumViewModelTest
    {
        public enum TestEnum
        {
            First,
            Second,
            Third,
        }

        [TestMethod]
        public void SetValue()
        {
            var value = TestEnum.Third;
            var vm = new EnumViewModel<TestEnum>(TestEnum.First);
            using var _ = vm.ValueObservable
                .Subscribe(newVal => value = newVal);
            Assert.AreEqual(TestEnum.First, value);
            vm.Value = TestEnum.Second;
            Assert.AreEqual(TestEnum.Second, value);
        }

        [TestMethod]
        public void Values()
        {
            var vm = new EnumViewModel<TestEnum>(TestEnum.First);
            Assert.IsTrue(new List<TestEnum>() { TestEnum.First, TestEnum.Second, TestEnum.Third }
                .Zip(vm.Values)
                .All((vals) => vals.First == vals.Second));
        }
    }
}
