using System;
using System.Collections.Generic;
using System.Text;
using IWshRuntimeLibrary;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;

namespace SpyStudio.Tools
{
    public static partial class Extensions
    {
        public static bool IsHex(this char c)
        {
            return (c >= '0' && c <= '9') ||
                     (c >= 'a' && c <= 'f') ||
                     (c >= 'A' && c <= 'F');
        }

        public static string JoinStrings(this IEnumerable<string> strings)
        {
            var ret = new StringBuilder();
            strings.ForEach(x => ret.Append(x));
            return ret.ToString();
        }

        public static string JoinSplitStrings(this IEnumerable<string> strings, string separator)
        {
            var ret = new StringBuilder();
            bool first = true;
            foreach (var s in strings)
            {
                if (first)
                    first = false;
                else
                    ret.Append(separator);
                ret.Append(s);
            }
            return ret.ToString();
        }

        public static string JoinPaths(this IEnumerable<string> strings)
        {
            return JoinSplitStrings(strings, "\\");
        }

        public static string GetRealTarget(this IWshShortcut shortcut)
        {
            return DarwinPathDecoder.GetMsiLinkTarget(shortcut.FullName) ?? shortcut.TargetPath;
        }
    }
}