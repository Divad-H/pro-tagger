using System;

namespace ProTagger.Utilities
{
    public static class VariantExtensions
    {
        public static Variant<TResult, TError> SelectResult<TResult, TIn, TError>(this Variant<TIn, TError> variant, Func<TIn, TResult> func)
            where TResult : notnull
            where TIn : notnull
            where TError : notnull
        {
            if (variant.Is<TError>())
                return variant.Get<TError>();
            return func(variant.Get<TIn>());
        }
    }
}
