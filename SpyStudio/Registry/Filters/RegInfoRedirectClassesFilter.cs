using System;
using System.Collections.Generic;

namespace SpyStudio.Registry.Filters
{
    public class RegInfoRedirectClassesFilter : RegInfoFilter
    {
        #region Classes replacements

        private static readonly List<KeyValuePair<string, string>> ClassesReplacements = new List
            <KeyValuePair<string, string>>
            {
                new KeyValuePair
                    <string, string>(
                    //@"^HKEY_LOCAL_MACHINE\\Software\\Classes",
                    @"HKEY_LOCAL_MACHINE\Software\Classes",
                    @"HKEY_CLASSES_ROOT"),
                new KeyValuePair
                    <string, string>(
                    //@"^HKEY_CURRENT_USER\\Software\\Classes",
                    @"HKEY_CURRENT_USER\Software\Classes",
                    @"HKEY_CLASSES_ROOT"),
            };

        #endregion

        protected override string Convert(string aPath)
        {
            foreach (var dir in ClassesReplacements)
            {
                if (!aPath.StartsWith(dir.Key, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                return aPath.Replace(aPath.Substring(0, dir.Key.Length), dir.Value);
            }

            return aPath;

            //foreach (var dir in ClassesReplacements)
            //{
            //    var regex = new Regex(dir.Key, RegexOptions.IgnoreCase);

            //    if (!regex.IsMatch(aPath))
            //        continue;

            //    return regex.Replace(aPath, dir.Value);
            //}

            //return aPath;
        }
    }
}