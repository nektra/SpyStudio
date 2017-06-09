using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpyStudio.Registry.Filters
{
    public class RegInfoMergeWowFilter : RegInfoFilter
    {
        private Regex WowRegex = new Regex(@"\\Wow6432Node", RegexOptions.IgnoreCase);

        protected override string Convert(string aPath)
        {
            var match = WowRegex.Match(aPath);
            return match.Success ? match.Result("$`$'") : aPath;
        }
    }
}