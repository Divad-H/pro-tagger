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
            => o.Where(v => v != null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        public static IObservable<T> SkipNull<T>(this IObservable<T?> o)
            where T : struct
            => o.Where(v => v.HasValue)
                .Select(v => v!.Value);

        public static IObservable<T> Many<T>(this IObservable<IEnumerable<T>> o)
            => o.SelectMany(c => c);
    }
}
