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

        public static IObservable<Variant<T1, T2, T3>> MergeVariant<T1, T2, T3>(this IObservable<T1> first, IObservable<T2> second, IObservable<T3> third)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            => first
                .Select(v => new Variant<T1, T2, T3>(v))
                .Merge(second
                    .Select(v => new Variant<T1, T2, T3>(v)))
                .Merge(third
                    .Select(v => new Variant<T1, T2, T3>(v)));

        public static IObservable<Variant<T1, T2, T3, T4>> MergeVariant<T1, T2, T3, T4>(this IObservable<T1> first, IObservable<T2> second, IObservable<T3> third, IObservable<T4> fourth)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            => first
                .Select(v => new Variant<T1, T2, T3, T4>(v))
                .Merge(second
                    .Select(v => new Variant<T1, T2, T3, T4>(v)))
                .Merge(third
                    .Select(v => new Variant<T1, T2, T3, T4>(v)))
                .Merge(fourth
                    .Select(v => new Variant<T1, T2, T3, T4>(v)));

        public static IObservable<T1> FirstVariant<T1, T2>(this IObservable<Variant<T1, T2>> variant)
            where T1 : notnull
            where T2 : notnull
            => variant
                .Where(v => v.VariantIndex == 0)
                .Select(v => v.First);

        public static IObservable<T1> FirstVariant<T1, T2, T3>(this IObservable<Variant<T1, T2, T3>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            => variant
                .Where(v => v.VariantIndex == 0)
                .Select(v => v.First);

        public static IObservable<T1> FirstVariant<T1, T2, T3, T4>(this IObservable<Variant<T1, T2, T3, T4>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            => variant
                .Where(v => v.VariantIndex == 0)
                .Select(v => v.First);

        public static IObservable<T2> SecondVariant<T1, T2>(this IObservable<Variant<T1, T2>> variant)
            where T1 : notnull
            where T2 : notnull
            => variant
                .Where(v => v.VariantIndex == 1)
                .Select(v => v.Second);

        public static IObservable<T2> SecondVariant<T1, T2, T3>(this IObservable<Variant<T1, T2, T3>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            => variant
                .Where(v => v.VariantIndex == 1)
                .Select(v => v.Second);

        public static IObservable<T2> SecondVariant<T1, T2, T3, T4>(this IObservable<Variant<T1, T2, T3, T4>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            => variant
                .Where(v => v.VariantIndex == 1)
                .Select(v => v.Second);

        public static IObservable<T3> ThirdVariant<T1, T2, T3>(this IObservable<Variant<T1, T2, T3>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            => variant
                .Where(v => v.VariantIndex == 2)
                .Select(v => v.Third);

        public static IObservable<T3> ThirdVariant<T1, T2, T3, T4>(this IObservable<Variant<T1, T2, T3, T4>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            => variant
                .Where(v => v.VariantIndex == 2)
                .Select(v => v.Third);

        public static IObservable<T4> FourthVariant<T1, T2, T3, T4>(this IObservable<Variant<T1, T2, T3, T4>> variant)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            => variant
                .Where(v => v.VariantIndex == 3)
                .Select(v => v.Fourth);
    }
}
