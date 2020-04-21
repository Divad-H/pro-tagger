using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Wpf;
using ProTaggerTest.Mocks;
using System;
using System.Reactive.Disposables;

namespace ProTaggerTest.Wpf
{
    [TestClass]
    public class IntegerInputViewModelTest
    {
        [TestMethod]
        public void IncreaseCommand()
        {
            int value = -1;
            var vm = new IntegerInputViewModel(new TestSchedulers(), 100, 0, 102);
            using var _ = vm.Value
                .Subscribe(v => value = v);
            Assert.AreEqual(100, value);
            Assert.IsTrue(vm.Increase.CanExecute(null));
            vm.Increase.Execute(null);
            Assert.AreEqual(101, value);
            Assert.IsTrue(vm.Increase.CanExecute(null));
            vm.Increase.Execute(null);
            Assert.AreEqual(102, value);
            Assert.IsFalse(vm.Increase.CanExecute(null));
        }

        [TestMethod]
        public void DecreaseCommand()
        {
            int value = 100;
            var vm = new IntegerInputViewModel(new TestSchedulers(), 2, 0, 102);
            using var _ = vm.Value
                .Subscribe(v => value = v);
            Assert.AreEqual(2, value);
            Assert.IsTrue(vm.Decrease.CanExecute(null));
            vm.Decrease.Execute(null);
            Assert.AreEqual(1, value);
            Assert.IsTrue(vm.Decrease.CanExecute(null));
            vm.Decrease.Execute(null);
            Assert.AreEqual(0, value);
            Assert.IsFalse(vm.Decrease.CanExecute(null));
        }

        [TestMethod]
        public void InvalidInput()
        {
            int value = 100;
            bool? valid = null;
            var vm = new IntegerInputViewModel(new TestSchedulers(), 50, 0, 100);
            using var _ =  vm.Value
                .Subscribe(v => value = v);
            using var _1 = vm.Valid
                .Subscribe(v => valid = v);
            Assert.AreEqual(50, value);
            vm.Text.OnNext("rat");
            Assert.AreEqual(50, value);
            Assert.IsFalse(valid ?? true);
            Assert.IsTrue(vm.Increase.CanExecute(null));
            vm.Increase.Execute(null);
            Assert.AreEqual(51, value);
            Assert.AreEqual("51", vm.Text.Value);
            Assert.IsTrue(valid ?? false);
        }
    }
}
