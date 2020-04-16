using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReacitveMvvm;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Data;

namespace ReactiveMvvmTest
{
    [TestClass]
    public class ReactiveBindingTest
    {
        class TestDependencyObject : DependencyObject
        {
            public int Test
            {
                get => (int)GetValue(TestProperty);
                set => SetValue(TestProperty, value);
            }
            public static readonly DependencyProperty TestProperty =
                DependencyProperty.Register(nameof(Test), typeof(int), typeof(TestDependencyObject), new PropertyMetadata(0));
        }

        public BehaviorSubject<int> TestSubject { get; } = new BehaviorSubject<int>(3);
        public IObservable<int> TestObservable { get; }

        public ReactiveBindingTest()
        {
            TestObservable = TestSubject;
        }

        [TestMethod]
        public void BindsToObservable()
        {
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension(nameof(TestObservable));
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, Observable.Return(this), null);
            Assert.AreEqual(3, element.Test);
            TestSubject.OnNext(13);
            Assert.AreEqual(13, element.Test);
            TestSubject.OnNext(14);
            Assert.AreEqual(14, element.Test);
        }

        [TestMethod]
        public void BindsOneTime()
        {
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension(nameof(TestObservable)) { Mode = BindingMode.OneTime };
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, Observable.Return(this), null);
            Assert.AreEqual(3, element.Test);
            TestSubject.OnNext(13);
            Assert.AreEqual(3, element.Test);
        }

        [TestMethod]
        public void BindsToSource()
        {
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension(nameof(TestSubject)) { Mode = BindingMode.OneWayToSource };
            using var subscription = binding.SetupBinding(element,
                TestDependencyObject.TestProperty,
                Observable
                    .Return(this)
                    .Concat(Observable.Never<ReactiveBindingTest>()),
                null);
            Assert.AreEqual(0, element.Test);
            element.Test = 4;
            Assert.AreEqual(4, element.Test);
            Assert.AreEqual(4, TestSubject.Value);
        }

        [TestMethod]
        public void BindsTwoWay()
        {
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension(nameof(TestSubject)) { Mode = BindingMode.TwoWay };
            using var subscription = binding.SetupBinding(element,
                TestDependencyObject.TestProperty,
                Observable
                    .Return(this)
                    .Concat(Observable.Never<ReactiveBindingTest>()),
                null);
            Assert.AreEqual(3, element.Test);
            element.Test = 4;
            Assert.AreEqual(4, element.Test);
            Assert.AreEqual(4, TestSubject.Value);
            TestSubject.OnNext(5);
            Assert.AreEqual(5, element.Test);
            Assert.AreEqual(5, TestSubject.Value);
        }

        [TestMethod]
        public void UpdatesOnDataContextChanged()
        {
            using var dataContextSubject = new BehaviorSubject<ReactiveBindingTest?>(null);
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension(nameof(TestObservable));
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, dataContextSubject, null);
            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(this);
            Assert.AreEqual(3, element.Test);
            TestSubject.OnNext(4);
            Assert.AreEqual(4, element.Test);
            dataContextSubject.OnNext(null);
            TestSubject.OnNext(5);
            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(this);
            Assert.AreEqual(5, element.Test);
            TestSubject.OnNext(6);
            Assert.AreEqual(6, element.Test);
        }

        [TestMethod]
        public void UpdatesOnDataContextChangedTwoWay()
        {
            using var dataContextSubject = new BehaviorSubject<ReactiveBindingTest?>(null);
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension(nameof(TestSubject)) { Mode = BindingMode.TwoWay };
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, dataContextSubject, null);
            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(this);
            Assert.AreEqual(3, element.Test);
            TestSubject.OnNext(4);
            Assert.AreEqual(4, element.Test);
            element.Test = 13;
            Assert.AreEqual(13, TestSubject.Value);
            dataContextSubject.OnNext(null);
            Assert.AreEqual(13, TestSubject.Value);
            TestSubject.OnNext(5);
            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(this);
            Assert.AreEqual(5, element.Test);
            element.Test = 14;
            Assert.AreEqual(14, TestSubject.Value);
            TestSubject.OnNext(6);
            Assert.AreEqual(6, element.Test);
        }

        class NestedViewModel
        {
            public BehaviorSubject<NestedViewModel?>? ChildSubject { get; set; }
            public NestedViewModel? ChildObject { get; set; }
            public BehaviorSubject<int>? ValueSubject { get; set; }
            public int Value { get; set; }
        }

        [TestMethod]
        public void UpdatesNestedViewModels()
        {
            var firstDataContext = new NestedViewModel() {
                ChildObject = new NestedViewModel() {
                    ChildSubject = new BehaviorSubject<NestedViewModel?>(null) }};
            var childViewModel = new NestedViewModel() {
                ChildSubject = new BehaviorSubject<NestedViewModel?>(null) };
            var childChildViewModel = new NestedViewModel() {
                ValueSubject = new BehaviorSubject<int>(1) };
            var alternativeChildChildViewModel = new NestedViewModel() {
                ValueSubject = new BehaviorSubject<int>(10) };
            var alternativeChildViewModel = new NestedViewModel() {
                ChildSubject = new BehaviorSubject<NestedViewModel?>(alternativeChildChildViewModel) };
            var secondDataContext = new NestedViewModel() {
                ChildObject = new NestedViewModel() {
                    ChildSubject = new BehaviorSubject<NestedViewModel?> (
                        new NestedViewModel() {
                            ChildSubject = new BehaviorSubject<NestedViewModel?> (
                                new NestedViewModel() {
                                    ValueSubject = new BehaviorSubject<int>(20) })})}};

            using var dataContextSubject = new BehaviorSubject<NestedViewModel?>(null);
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension("ChildObject.ChildSubject.ChildSubject.ValueSubject");
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, dataContextSubject, null);

            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(firstDataContext);
            Assert.AreEqual(0, element.Test);
            firstDataContext.ChildObject.ChildSubject.OnNext(childViewModel);
            Assert.AreEqual(0, element.Test);
            childViewModel.ChildSubject.OnNext(childChildViewModel);
            Assert.AreEqual(1, element.Test);
            childChildViewModel.ValueSubject.OnNext(2);
            Assert.AreEqual(2, element.Test);
            dataContextSubject.OnNext(secondDataContext);
            Assert.AreEqual(20, element.Test);
            childChildViewModel.ValueSubject.OnNext(3);
            Assert.AreEqual(20, element.Test);
            dataContextSubject.OnNext(firstDataContext);
            Assert.AreEqual(3, element.Test);
            firstDataContext.ChildObject.ChildSubject.OnNext(alternativeChildViewModel);
            Assert.AreEqual(10, element.Test);
            childChildViewModel.ValueSubject.OnNext(4);
            firstDataContext.ChildObject.ChildSubject.OnNext(null);
            Assert.AreEqual(0, element.Test);
            firstDataContext.ChildObject.ChildSubject.OnNext(childViewModel);
            Assert.AreEqual(4, element.Test);
        }

        [TestMethod]
        public void UpdatesNestedViewModelsTwoWay()
        {
            var firstDataContext = new NestedViewModel() {
                ChildObject = new NestedViewModel() {
                    ChildSubject = new BehaviorSubject<NestedViewModel?>(null) }};
            var childViewModel = new NestedViewModel() {
                ChildSubject = new BehaviorSubject<NestedViewModel?>(null) };
            var childChildViewModel = new NestedViewModel() {
                ValueSubject = new BehaviorSubject<int>(1) };
            var alternativeChildChildViewModel = new NestedViewModel() {
                ValueSubject = new BehaviorSubject<int>(10) };
            var alternativeChildViewModel = new NestedViewModel() {
                ChildSubject = new BehaviorSubject<NestedViewModel?>(alternativeChildChildViewModel) };
            var secondDataContext = new NestedViewModel() {
                ChildObject = new NestedViewModel() {
                    ChildSubject = new BehaviorSubject<NestedViewModel?> (
                        new NestedViewModel() {
                            ChildSubject = new BehaviorSubject<NestedViewModel?> (
                                new NestedViewModel() {
                                    ValueSubject = new BehaviorSubject<int>(20) })})}};

            using var dataContextSubject = new BehaviorSubject<NestedViewModel?>(null);
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension("ChildObject.ChildSubject.ChildSubject.ValueSubject")
                { Mode = BindingMode.TwoWay };
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, dataContextSubject, null);

            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(firstDataContext);
            Assert.AreEqual(0, element.Test);
            firstDataContext.ChildObject.ChildSubject.OnNext(childViewModel);
            Assert.AreEqual(0, element.Test);
            childViewModel.ChildSubject.OnNext(childChildViewModel);
            Assert.AreEqual(1, element.Test);
            childChildViewModel.ValueSubject.OnNext(2);
            Assert.AreEqual(2, element.Test);
            element.Test = 40;
            Assert.AreEqual(40, childChildViewModel.ValueSubject.Value);
            dataContextSubject.OnNext(secondDataContext);
            Assert.AreEqual(20, element.Test);
            childChildViewModel.ValueSubject.OnNext(3);
            Assert.AreEqual(20, element.Test);
            element.Test = 41;
            Assert.AreEqual(3, childChildViewModel.ValueSubject.Value);
            Assert.AreEqual(41,
                (secondDataContext.ChildObject.ChildSubject.Value?.ChildSubject?.Value?.ValueSubject
                    ?? throw new Exception()).Value);
            dataContextSubject.OnNext(firstDataContext);
            Assert.AreEqual(3, element.Test);
            firstDataContext.ChildObject.ChildSubject.OnNext(alternativeChildViewModel);
            Assert.AreEqual(10, element.Test);
            childChildViewModel.ValueSubject.OnNext(4);
            firstDataContext.ChildObject.ChildSubject.OnNext(null);
            Assert.AreEqual(0, element.Test);
            firstDataContext.ChildObject.ChildSubject.OnNext(childViewModel);
            Assert.AreEqual(4, element.Test);
        }

        [TestMethod]
        public void BindsToProperty()
        {
            var dataContext = new NestedViewModel()
            {
                ChildSubject = new BehaviorSubject<NestedViewModel?>(null)
            };

            using var dataContextSubject = new BehaviorSubject<NestedViewModel?>(dataContext);
            var element = new TestDependencyObject();
            var binding = new ReactiveBindingExtension("ChildSubject.Value");
            using var subscription = binding.SetupBinding(element, TestDependencyObject.TestProperty, dataContextSubject, null);

            Assert.AreEqual(0, element.Test);
            dataContext.ChildSubject.OnNext(new NestedViewModel() { Value = 1 });
            Assert.AreEqual(1, element.Test);
            dataContextSubject.OnNext(null);
            Assert.AreEqual(0, element.Test);
            dataContext.ChildSubject.OnNext(new NestedViewModel() { Value = 2 });
            Assert.AreEqual(0, element.Test);
            dataContextSubject.OnNext(dataContext);
            Assert.AreEqual(2, element.Test);
        }
    }
}
