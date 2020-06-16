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
#pragma warning disable CS8629 // Nullable value type may be null.
                .Select(v => v.Value);
#pragma warning restore CS8629 // Nullable value type may be null.

        public static IObservable<T> Many<T>(this IObservable<IEnumerable<T>> o)
            => o.SelectMany(c => c);
    }
}
