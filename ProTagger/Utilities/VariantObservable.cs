using System;
using System.Reactive.Linq;

namespace ProTagger.Utilities
{
    static class VariantObservable
    {
        public static IObservable<Variant<T1, T2>> MergeVariant<T1, T2>(this IObservable<T1> first, IObservable<T2> second) 
            where T1 : notnull
            where T2 : notnull
            => first
                .Select(v => new Variant<T1, T2>(v))
                .Merge(second
                    .Select(v => new Variant<T1, T2>(v)));

        public static IObservable<T1> FirstVariant<T1, T2>(this IObservable<Variant<T1, T2>> variant)
            where T1 : notnull
            where T2 : notnull
            => variant
                .Where(v => v.VariantIndex == 0)
                .Select(v => v.First);

        public static IObservable<T2> SecondVariant<T1, T2>(this IObservable<Variant<T1, T2>> variant)
            where T1 : notnull
            where T2 : notnull
            => variant
                .Where(v => v.VariantIndex == 1)
                .Select(v => v.Second);
    }
}
