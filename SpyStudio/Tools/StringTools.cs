using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SpyStudio.Extensions;

namespace SpyStudio.Tools
{
    public class StringTools
    {
        static public string GetDecodedString(string encString)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encString);
            string ret = Encoding.ASCII.GetString(encodedDataAsBytes);
            return ret;
        }
        static public string EncodeString(string plainString)
        {
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainString);
            string ret = Convert.ToBase64String(plainBytes);
            return ret;
        }
        public static string GetSummaryText(string text, int maxLen)
        {
            return GetSummaryText(text, 0, text.Length, maxLen);
        }
        public static string GetSummaryText(string text, int startIndex, int len, int maxLen)
        {
            if (maxLen < 20)
                maxLen = 20;
            return (len > maxLen)
                       ? (text.Substring(startIndex, maxLen/2 - 2) + "..." +
                          text.Substring(startIndex + len - maxLen/2 - 2, maxLen/2 - 2))
                       : text.Substring(startIndex, len);
        }
        public static string GetFirstLines(string text, int lines)
        {
            return GetFirstLines(text, lines, true);
        }

        public static string GetFirstLines(string text, int lines, bool compact)
        {
            return GetFirstLines(text, lines, compact ? 90 : -1);
        }

        public static string GetFirstLines(string text, int lines, int maxLen)
        {
            var ret = "";
            var i = 0;
            var lastIndex = 0;
            var index = text.IndexOf('\n');
            if (index != -1)
            {
                while (index != -1 && i++ < lines)
                {
                    if (lastIndex != 0)
                    {
                        // if it's not the first line -> skip \n and add a \n to the begining to separate previous line
                        lastIndex++;
                        ret += "\n";
                    }
                    ret += maxLen != -1
                               ? GetSummaryText(text, lastIndex, index - lastIndex, maxLen)
                               : text.Substring(lastIndex, index - lastIndex);
                    lastIndex = index;
                    index = text.IndexOf('\n', index + 1);
                }
            }
            else
            {
                ret = maxLen != -1 ? GetSummaryText(text, maxLen) : text;
            }
            return ret;
        }
        public static int IndexOfIgnoreQuotes(string str, char value)
        {
            return IndexOfIgnoreQuotes(str, value, 0);
        }
        public static int IndexOfIgnoreQuotes(string str, char value, int index)
        {
            return SearchCharIgnoreQuotes(str, value, index, true);
        }

        public static int SearchCharIgnoreQuotes(string str, char value, int index, bool forward)
        {
            bool openSingleQuotes = false, openDoubleQuotes = false;
            while(index < str.Length && index >= 0)
            {
                if (str[index] == '\'')
                {
                    openSingleQuotes = !openSingleQuotes;
                }
                else if (str[index] == '\"')
                {
                    openDoubleQuotes = !openDoubleQuotes;
                }
                else if (str[index] == value && !openSingleQuotes && !openDoubleQuotes)
                {
                    return index;
                }
                if (forward)
                    index++;
                else
                    index--;
            }
            return -1;
        }
        public static int LastIndexOfIgnoreQuotes(string str, char value)
        {
            return LastIndexOfIgnoreQuotes(str, value, 0);
        }
        public static int LastIndexOfIgnoreQuotes(string str, char value, int index)
        {
            return SearchCharIgnoreQuotes(str, value, index, false);
        }
        public static UInt64 ConvertToUInt64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            UInt64 ret = value.StartsWith("0x") ? UInt64.Parse(value.Substring(2), NumberStyles.AllowHexSpecifier) : UInt64.Parse(value);
            return ret;
        }
        public static UInt32 ConvertToUInt32(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            UInt32 ret = value.StartsWith("0x") ? UInt32.Parse(value.Substring(2), NumberStyles.AllowHexSpecifier) : UInt32.Parse(value);
            return ret;
        }
        public static string Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var ms = new MemoryStream();
            using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;
            //var outStream = new MemoryStream();

            var compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            var gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return Convert.ToBase64String(gzBuffer);
        }

        public static bool IsLineFeed(string str, int index)
        {
            return str[index] == '\n' || str[index] == '\r';
        }
        /// <summary>
        /// Get the index after lfCount '\n'. Also pass the '\r' without counting them
        /// </summary>
        /// <param name="str"></param>
        /// <param name="startIndex"></param>
        /// <param name="lfCount"></param>
        /// <returns></returns>
        public static int PassLineFeed(string str, int startIndex, int lfCount)
        {
            int retIndex = startIndex;
            int passCount = 0;
            while (retIndex < str.Length && str[retIndex] == '\n' && passCount < lfCount ||
                str[retIndex] == '\r')
            {
                if (str[retIndex] == '\n')
                    passCount++;
                retIndex++;
            }
            // if they are equal -> EOF
            return (retIndex >= str.Length ? -1 : retIndex);
        }

        /// <summary>
        /// Find the last index before \n and \r
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static int LastIndexOfThisLine(string text, int startIndex)
        {
            while (startIndex < text.Length && text[startIndex] != '\n' && text[startIndex] != '\r')
                startIndex++;
            if (startIndex >= text.Length)
                return -1;
            return startIndex - 1;
        }
        /// <summary>
        /// Find the index of the first character of next line. Pass 1 time \n and \r
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static int IndexOfNextLine(string text, int startIndex)
        {
            bool foundN = false;
            while (startIndex < text.Length && ((text[startIndex] == '\n' && !foundN) || text[startIndex] == '\r' ||
                !IsLineFeed(text, startIndex) && !foundN))
            {
                if (text[startIndex] == '\n')
                    foundN = true;
                startIndex++;
            }
            if (startIndex >= text.Length)
                return -1;
            return startIndex;
        }
        /// <summary>
        /// Find the index of the first character that is not in excludeChars
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <param name="excludeChars"></param>
        /// <returns></returns>
        public static int IndexOfFirstThatIsNot(string text, int startIndex, char[] excludeChars)
        {
            while (startIndex < text.Length && excludeChars.Any(c => c == text[startIndex]))
                startIndex++;
            if (startIndex >= text.Length)
                return -1;
            return startIndex;
        }

        private static int IsSlash(char c)
        {
            return c == '\\' || c == '/' ? 1 : 0;
        }

        public static int PathComparison(string a, string b)
        {
            for (int i = 0; ; i++)
            {
                if (i == a.Length)
                    return i == b.Length ? 0 : -1;
                if (i == b.Length)
                    return 1;
                int d = IsSlash(b[i]) - IsSlash(a[i]);
                if (d != 0)
                    return d;
                d = char.ToLower(a[i]) - char.ToLower(b[i]);
                if (d != 0)
                    return d;
            }
        }


        public static string JoinStack(Stack<string> stack, int minimum)
        {
            if (stack.Count < minimum)
                return string.Empty;
            return string.Join("\\", stack.Take(stack.Count - minimum + 1).Reverse().ToArray());
        }

        public static string JoinStack(Stack<string> stack)
        {
            return JoinStack(stack, 1);
        }

        public static void ReverseLastCharacters(StringBuilder sb, int n)
        {
            if (n > sb.Length)
                n = sb.Length;
            var m = n / 2;
            for (var i = 0; i < m; i++)
            {
                var a = sb.Length - 1 - i;
                var b = sb.Length - n + i;
                var temp = sb[a];
                sb[a] = sb[b];
                sb[b] = temp;
            }
        }

        public static string PackGuid(Guid guid)
        {
            var temp = guid.ToString().Replace("{", "").Replace("}", "").Replace("-", "");

            var ret = new StringBuilder();
            ret.Append(temp.Substring(0, 8));
            ReverseLastCharacters(ret, 8);
            ret.Append(temp.Substring(8, 4));
            ReverseLastCharacters(ret, 4);
            ret.Append(temp.Substring(12, 4));
            ReverseLastCharacters(ret, 4);
            for (var i = 0; i < 8; i++)
            {
                ret.Append(temp.Substring(16 + i * 2, 2));
                ReverseLastCharacters(ret, 2);
            }

            return ret.ToString();
        }

        public static Guid UnpackGuid(string packedGuid)
        {
            var ret = new StringBuilder();
            ret.Append('{');
            ret.Append(packedGuid.Substring(0, 8));
            ReverseLastCharacters(ret, 8);
            ret.Append('-');
            ret.Append(packedGuid.Substring(8, 4));
            ReverseLastCharacters(ret, 4);
            ret.Append('-');
            ret.Append(packedGuid.Substring(12, 4));
            ReverseLastCharacters(ret, 4);
            ret.Append('-');
            for (var i = 0; i < 8; i++)
            {
                ret.Append(packedGuid.Substring(16 + i * 2, 2));
                ReverseLastCharacters(ret, 2);
                if (i != 1)
                    continue;
                ret.Append('-');
            }
            ret.Append('}');

            return new Guid(ret.ToString());
        }

        public static DateTime? ToDateTimeYYYYMMDD(string s)
        {
            if (s == null)
                return null;
            try
            {
                return DateTime.ParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static string RandomLowerCaseString(int length)
        {
            var bytes = new byte[length];
            new Random().NextBytes(bytes);
            return new string(bytes.Select(x => (char)(x % ('z' - 'a') + 'a')).ToArray());
        }
    }
}