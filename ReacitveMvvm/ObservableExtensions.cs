using System;
using System.Reactive.Linq;

namespace ReacitveMvvm
{
    public static class ObservableExtensions
    {
        public static IObservable<T> SkipNull<T>(this IObservable<T?> o)
            where T : class
        {
            return o
                .Where(v => v != null)
                .Select(v => v ?? throw new InvalidOperationException("Unreachable code reached."));
        }
    }
}
