using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Wpf;
using ProTaggerTest.Mocks;
using System;

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
            using var _ = vm.ValueObservable
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
            using var _ = vm.ValueObservable
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
            var vm = new IntegerInputViewModel(new TestSchedulers(), 50, 0, 100);
            using var _ =  vm.ValueObservable
                .Subscribe(v => value = v);
            Assert.AreEqual(50, value);
            vm.Text = "rat";
            Assert.AreEqual(50, value);
            Assert.IsFalse(vm.Valid);
            Assert.IsTrue(vm.Increase.CanExecute(null));
            vm.Increase.Execute(null);
            Assert.AreEqual(51, value);
            Assert.IsTrue(vm.Valid);
        }
    }
}
