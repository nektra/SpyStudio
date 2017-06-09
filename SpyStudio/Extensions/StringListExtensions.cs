using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Extensions
{
    public static class StringListExtensions
    {
        public static void RemoveRepetitions(this List<string> aStringList)
        {
            for (var i = 0; i < aStringList.Count; i++)
                for (var j = i + 1; j < aStringList.Count; j++)
                    if (aStringList[i] == aStringList[j])
                        aStringList.RemoveAt(j);
        }

        public static void AddRangeIfNotNullOrEmpty(this List<string> aStringList, IEnumerable<string> aStringEnumerable)
        {
            aStringList.AddRange(aStringEnumerable.Where(s => !string.IsNullOrEmpty(s)));
        }

        public static void AddIfNotNullOrEmpty(this List<string> aStringList, string aString)
        {
            if (!string.IsNullOrEmpty(aString))
                aStringList.Add(aString);
        }
    }
}
