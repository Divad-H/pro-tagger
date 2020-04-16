using ReactiveMvvm;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ReacitveMvvm
{
    public class ReactiveBindingExtension : MarkupExtension
    {
        public string? Path { get; set; }
        public BindingMode Mode { get; set; } = BindingMode.Default;
        public IReactiveConverter? Converter { get; set; }

        public ReactiveBindingExtension() { }
        public ReactiveBindingExtension(string? path)
            => Path = path;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (!(serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget valueProvider)
                || !(valueProvider.TargetObject is DependencyObject element)
                || !(valueProvider.TargetProperty is DependencyProperty property))
                return this;

            var dataContextElement = FindFrameworkElement(element)
                ?? throw new InvalidOperationException("A ReactiveBinding must be set on a dependency property of a FrameworkElement.");

            var dataContextObservable = Observable
                .FromEvent<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    action => (sender, args) => action(args),
                    handler => dataContextElement.DataContextChanged += handler,
                    handler => { }/*dataContextElement.DataContextChanged -= handler*/)
                .Select(args => args.NewValue)
                .StartWith(dataContextElement?.DataContext);

            var _ = SetupBinding(element, property, dataContextObservable, Converter);

            return property.DefaultMetadata.DefaultValue;
        }

        public IDisposable SetupBinding(DependencyObject element, DependencyProperty property, IObservable<object?> dataContext, IReactiveConverter? converter)
        {
            var disposable = new CompositeDisposable();
            var path = Path?.Split('.') ?? new string[] { };

            if (Mode == BindingMode.Default)
                Mode = property.GetMetadata(element) is FrameworkPropertyMetadata metadata && metadata.BindsTwoWayByDefault
                    ? BindingMode.TwoWay
                    : BindingMode.OneWay;

            if (Mode == BindingMode.OneWayToSource || Mode == BindingMode.TwoWay)
            {
                // It is important that we first subscribe and unsubscribe the observer
                // in the view model to the dependency property changes, because otherwise
                // the dependency property will emit an invalid value to the previous
                // subscriber when the data context changes.
                var basePath = path.SkipLast(1).ToArray();
                var propertyObservable = dataContext
                    .Select(obj => new PathNode(obj, basePath))
                    .FindProperty() // ViewModel containing the observer/observable (IObservable<IObserver<T>?>)
                    .Select(vm => ObservableBindingHelpers.GetObject(vm, path.Last()))
                    .SubscribeObserver(element, property)
                    .DisposeWith(disposable);
            }
            if (Mode == BindingMode.OneTime || Mode == BindingMode.OneWay || Mode == BindingMode.TwoWay)
            {
                var valueObservable = dataContext
                    .Select(obj => new PathNode(obj, path))
                    .FindProperty();
                if (Mode == BindingMode.OneTime)
                    valueObservable = valueObservable.Take(1);

                if (converter != null)
                    valueObservable = converter.Convert(valueObservable);

                valueObservable
                    .Subscribe(val => element.SetValue(property,
                         property.PropertyType.IsAssignableFrom(val?.GetType())
                             ? val
                             : property.DefaultMetadata.DefaultValue))
                    .DisposeWith(disposable);
            }

            return disposable;
        }

        private static FrameworkElement? FindFrameworkElement(DependencyObject d)
        {
            DependencyObject? current = d;
            while (!(current is FrameworkElement) && current != null)
                current = LogicalTreeHelper.GetParent(current);
            return (FrameworkElement?)current;
        }
    }

    struct PathNode
    {
        public readonly object? @Object;
        public readonly string[] RemainingPath;

        public PathNode(object? @object, string[] remainingPath)
            => (@Object, RemainingPath) = (@object, remainingPath);
    }

    internal static class ObservableBindingHelpers
    {
        public static IObservable<object?> FindProperty(this IObservable<PathNode> obs)
            => obs
                .Select(node => node.Object == null || !node.RemainingPath.Any()
                    ? Observable.Return(node.Object)
                    : ObjectFromPath(node)
                        .FindProperty())
                .Switch();

        private static IObservable<PathNode> ObjectFromPath(PathNode node)
        {
            var current = node.Object;
            foreach (var property in node.RemainingPath.Select((name, i) => new { name, i }))
            {
                if (current == null)
                    break;
                current = GetObject(current, property.name);
                if (current != null)
                {
                    var obs = CastAsObservable(current);
                    if (obs != null)
                        return obs.Select(val => new PathNode(val, node.RemainingPath.Skip(property.i + 1).ToArray()));
                }
            }
            return Observable.Return(new PathNode(current, new string[] { }));
        }

        public static object? GetObject(object? owner, string name)
            => owner?.GetType().GetProperty(name)?.GetValue(owner);

        public static Type? GetObservableType(object observable)
        {
            return observable
                .GetType()
                .GetInterfaces()
                .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
                ?.GetGenericArguments()[0];
        }

        private static object? CallObservableFunction(string func, params object[] parameters)
        {
            var propertyType = GetObservableType(parameters[0]);
            if (propertyType == null)
                return null;
            return typeof(ObservableBindingHelpers)
                   .GetMethod(func, BindingFlags.NonPublic | BindingFlags.Static)
                   ?.MakeGenericMethod(propertyType)
                   .Invoke(null, parameters);
        }

        private static IObservable<object?>? CastAsObservable(object obj)
            => (IObservable<object?>?)CallObservableFunction(nameof(CastIntoObjectObservable), obj);

        private static IObservable<object?> CastIntoObjectObservable<T>(IObservable<T> observable)
            => observable
                .DistinctUntilChanged()
                .Select(val => (object?)val);

        public static IDisposable SubscribeObserver(this IObservable<object?> observer,
            DependencyObject element,
            DependencyProperty property)
        {
            return observer
                .Select(obs =>
                {
                    if (obs == null)
                        return Disposable.Empty;
                    var subscription = CallObservableFunction(nameof(SubscribeObserverT), obs, element, property);
                    return (IDisposable?)subscription ?? Disposable.Empty;
                })
                .Scan(new SerialDisposable(),
                    (serial, subsription) => { serial.Disposable = subsription; return serial; })
                .Subscribe();
        }

        private static IDisposable SubscribeObserverT<T>(IObserver<T> observer, DependencyObject element, DependencyProperty property)
        {
            if (property.OwnerType.IsAssignableFrom(element.GetType()))
                return element
                    .FromDependencyProperty<T>(property)
                    .Subscribe(observer);
            return Disposable.Empty;
        }
    }
}
