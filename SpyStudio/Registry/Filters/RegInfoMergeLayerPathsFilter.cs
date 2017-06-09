using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpyStudio.Registry.Filters
{
    public class RegInfoMergeLayerPathsFilter : RegInfoFilter
    {
        #region Layer Replacements

        private static readonly List<KeyValuePair<Regex, string>> LayerReplacements = new List
            <KeyValuePair<Regex, string>>
            {
                new KeyValuePair
                    <Regex, string>(
                    new Regex(@"^HKEY_LOCAL_MACHINE\\_SWV_LAYER_\w*\\HLM", RegexOptions.IgnoreCase),
                    @"HKEY_LOCAL_MACHINE"),
                new KeyValuePair
                    <Regex, string>(
                    new Regex(@"^HKEY_LOCAL_MACHINE\\_SWV_LAYER_\w*\\HLM64", RegexOptions.IgnoreCase),
                    @"HKEY_LOCAL_MACHINE"),
                new KeyValuePair
                    <Regex, string>(
                    new Regex(@"^HKEY_LOCAL_MACHINE\\_SWV_LAYER_\w*\\HU\\S\-[0-9|\-]*", RegexOptions.IgnoreCase),
                    @"HKEY_CURRENT_USER"),
                new KeyValuePair
                    <Regex, string>(
                    new Regex(@"^HKEY_LOCAL_MACHINE\\_SWV_LAYER_\w*\\HU64\\S\-[0-9|\-]*", RegexOptions.IgnoreCase),
                    @"HKEY_CURRENT_USER"),
                new KeyValuePair
                    <Regex, string>(
                    new Regex(@"^HKEY_LOCAL_MACHINE\\_SWV_LAYER_\w*\\HU", RegexOptions.IgnoreCase),
                    @"HKEY_CURRENT_USER"),
            };

        #endregion

        protected override string Convert(string aPath)
        {
            var actualPath = aPath;

            bool pathWasModified;

            do
            {
                pathWasModified = false;

                foreach (var dir in LayerReplacements)
                {
                    if (!dir.Key.IsMatch(actualPath))
                        continue;

                    actualPath = dir.Key.Replace(aPath, dir.Value);
                    pathWasModified = true;
                    break;
                }

            } while (pathWasModified);

            return actualPath;
        }
    }
}