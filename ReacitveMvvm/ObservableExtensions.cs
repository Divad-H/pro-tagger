using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace ReacitveMvvm
{
    public static class ObservableExtensions
    {
        public static IObservable<T> SkipNull<T>(this IObservable<T?> o)
            where T : class
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            => o.Where(v => !(v is null));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        public static IObservable<T> SkipNull<T>(this IObservable<T?> o)
            where T : struct
            => o.Where(v => v.HasValue)
                .Select(v => v!.Value);

        public static IObservable<TResult> SkipManyNull<T, TSource, TResult>(this IObservable<TSource> o)
            where T : class
            where TSource : IEnumerable<T?>
            where TResult : IEnumerable<T>
            => (IObservable<TResult>)o.Where(v => !(v is null) && !v.Any(o => o is null));

        public static IObservable<T> Many<T>(this IObservable<IEnumerable<T>> o)
            => o.SelectMany(c => c);
    }
}
