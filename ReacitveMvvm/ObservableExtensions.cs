using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace ReacitveMvvm
{
    public static class ObservableExtensions
    {
        public static IObservable<T> SkipNull<T>(this IObservable<T?> o)
            where T : class
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            => o.Where(v => v != null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        public static IObservable<T> SkipNull<T>(this IObservable<T?> o)
            where T : struct
            => o.Where(v => v.HasValue)
#pragma warning disable CS8629 // Nullable value type may be null.
                .Select(v => v.Value);
#pragma warning restore CS8629 // Nullable value type may be null.

        /// <summary>
        /// Creates an Observable from a dependency property of type <see cref="{TProperty}"/>
        /// </summary>
        /// <typeparam name="TProperty">Type of the dependency property</typeparam>
        /// <param name="element">The <see cref="DependencyObject"/> owning the property.</param>
        /// <param name="property">The <see cref="DependencyProperty"/></param>
        /// <returns>An observable emitting values when the property changes.</returns>
        public static IObservable<TProperty> FromDependencyProperty<TProperty>(this DependencyObject element, DependencyProperty property)
        {
            var propertyDesc = DependencyPropertyDescriptor.FromProperty(property, element.GetType());
            return Observable
                .FromEventPattern(
                    handler => propertyDesc.AddValueChanged(element, handler),
                    handler => propertyDesc.RemoveValueChanged(element, handler))
                .Select(ev => (TProperty)(ev.Sender as DependencyObject ?? throw new ArgumentNullException(nameof(ev.Sender)))
                    .GetValue(property));
        }
    }
}
