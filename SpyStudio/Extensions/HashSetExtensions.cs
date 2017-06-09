using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Extensions
{
    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> aHashSet, IEnumerable<T> anEnumerable)
        {
            foreach (var element in anEnumerable)
                aHashSet.Add(element);
        }

    }
}
