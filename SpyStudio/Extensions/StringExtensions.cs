using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.FileSystem;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Extensions
{
    public static class StringExtensions
    {
        public static byte[] ToByteArray(this string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string ForCompareString(this string aString)
        {
            return string.IsNullOrEmpty(aString) ? "<empty>" : aString;
        }

        public static string TrimStart(this string aString, string stringToReplace)
        {
            if (aString.StartsWith(stringToReplace))
                aString = aString.Substring(stringToReplace.Length);
            return aString.Trim('\\');
        }

        public static bool EqualsIgnoringCase(this string aString, string anotherString)
        {
            return aString.Equals(anotherString, StringComparison.InvariantCultureIgnoreCase);
        }

        private static readonly char[] Splitter = new[] {'\\'};

        public static IEnumerable<string> SplitAsPath(this string aPath)
        {
            return aPath.Split(Splitter, StringSplitOptions.RemoveEmptyEntries);
        }

        //Pre:  s != null
        //Post: !s.Contains("\\\\") && !s.EndsWith("\\");
        public static string AsNormalizedPath(this string s)
        {
            if (!s.Contains("\\\\"))
            {
                if (s.EndsWith("\\"))
                    return s.Substring(0, s.Length - 1);
                return s;
            }
            var ret = new StringBuilder {Capacity = s.Length};
            bool lastWasSlash = false;
            foreach (var character in s)
            {
                if (lastWasSlash && character == '\\')
                    continue;
                ret.Append(character);
                lastWasSlash = character == '\\';
            }
            if (ret[ret.Length - 1] == '\\')
                ret.Length--;
            return ret.ToString();
        }

        static readonly Regex EnvironmentVariableRegex = new Regex(@"%[^%]+%");

        public static string ExpandEnvironmentVariables(this string aString, IDictionary environmentVariables)
        {
            var expandedString = aString;

            foreach (Match match in EnvironmentVariableRegex.Matches(aString))
                expandedString.Replace(match.Value, (string) environmentVariables[match.Value]);

            return expandedString;
        }

        public static bool EqualsPath(this string aPath, string anotherPath)
        {
            var pathNormalized = aPath.FormattedForComparison();
            var anotherPathNormalized = anotherPath.FormattedForComparison();

            return pathNormalized.Equals(anotherPathNormalized);
        }

        public static string FormattedForComparison(this string aPath)
        {
            return PathNormalizer.EnsureSingleBackslashesIn(aPath).ToLower();
        }

        public static bool IsDriveLetterPath(this string aPath)
        {
            return new Regex(@"^[a-z|A-Z]:\\*$", RegexOptions.IgnoreCase).IsMatch(aPath);
        }

        public static IEnumerable<string> SplitInWords(this string aString)
        {
            return aString.ToLower().Split(' ').Where(w => !string.IsNullOrEmpty(w));
        }

        public static bool SeemsSimilarTo(this string aString, string modelString)
        {
            var splitSelf = aString.SplitInWords();

            return 
                modelString.SplitInWords().
                    Any(selfWord =>
                    splitSelf.Any(
                        anotherStringWord => anotherStringWord.Contains(selfWord)));
        }

        public static bool SeemsRelatedToPath(this string aString, string aPath)
        {
            var wordsInPath = aPath.SplitInWords();

            return wordsInPath.Any(pathPiece => pathPiece.SeemsSimilarTo(aString));
        }
        public static string FirstChars(this string aString, int len)
        {
            if(aString.Length >= len)
            {
                return aString.Substring(0, len) + "...";
            }
            return aString;
        }
    }
}
