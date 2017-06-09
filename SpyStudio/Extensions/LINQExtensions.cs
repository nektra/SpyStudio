using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Extensions
{
    public static class LINQExtensions
    {
        // Returns the elements in anEnumerable except the last "count" elements.
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> anEnumerable, int count)
        {
            return anEnumerable.Take(anEnumerable.Count() - count);
        }

        // Returns the elements in anEnumerable except the last one.
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> anEnumerable)
        {
            return anEnumerable.SkipLast(1);
        }

        public static void ForEach<T>(this IEnumerable<T> anEnumerable, Action<T> anAction)
        {
            foreach (var element in anEnumerable)
                anAction(element);
        }

        public static void ForEachNth<T>(this IEnumerable<T> xs, params Action<T>[] fs)
        {
            int n = fs.Length;
            int i = 0;
            foreach (var x in xs)
            {
                var f = fs[i++ % n];
                if (f != null)
                    f(x);
            }

        }

        public static IEnumerable<T> WhereEven<T>(this IEnumerable<T> xs)
        {
            var i = 0;
            return xs.Where(x => i++ % 2 == 0);
        }

        public static IEnumerable<T> WhereOdd<T>(this IEnumerable<T> xs)
        {
            var i = 0;
            return xs.Where(x => i++ % 2 == 1);
        }

        public static IEnumerable<T> ToEnumerable<T>(this T obj)
        {
            yield return obj;
        }

    }
}
