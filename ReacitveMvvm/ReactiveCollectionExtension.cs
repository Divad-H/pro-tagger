using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Text;

namespace ReacitveMvvm
{
    public static class ReactiveCollectionExtension
    {
        public static IObservable<NotifyCollectionChangedEventArgs> MakeObservable(this INotifyCollectionChanged collection)
        {
            return Observable
                .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    h => collection.CollectionChanged += h,
                    h => collection.CollectionChanged -= h)
                .Select(args => args.EventArgs);
        }
    }
}
