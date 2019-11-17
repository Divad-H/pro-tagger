using System;
using System.Collections.Generic;

namespace ProTagger.Utilities
{
    public static class LinqExtensions
    {
        public static IEnumerable<TResult> GreaterZip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector, TFirst defaultFirst, TSecond defaultSecond)
        {
            var it1 = first.GetEnumerator();
            var it2 = second.GetEnumerator();
            bool firstValid = it1.MoveNext();
            bool secondValid = it2.MoveNext();
            while (firstValid || secondValid)
            {
                yield return resultSelector(firstValid ? it1.Current : defaultFirst, secondValid ? it2.Current : defaultSecond);
                firstValid = it1.MoveNext();
                secondValid = it2.MoveNext();
            }
        }

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}
