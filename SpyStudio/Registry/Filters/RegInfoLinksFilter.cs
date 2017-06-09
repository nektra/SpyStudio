using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpyStudio.Registry.Filters
{
    public class RegInfoLinksFilter : RegInfoFilter
    {
        #region Key links

        // this keys are links to another -> so we have to use the correct key to avoid repetitions
        private static readonly List<KeyValuePair<Regex, string>> KeyLinks = new List<KeyValuePair<Regex, string>>
            {
                new KeyValuePair<Regex, string>(
                    new Regex(@"^REGISTRY\\USER", RegexOptions.IgnoreCase),
                    @"HKEY_CURRENT_USER"),
            };

        #endregion

        public RegInfoLinksFilter()
        {
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"System\Select");
                if (key == null) 
                    return;

                var value = key.GetValue("Current");
                if (value != null)
                {
                    var current = (int)value;
                    KeyLinks.Add(
                        new KeyValuePair<Regex, string>(
                            new Regex(@"^HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet" + current.ToString("D3"), RegexOptions.IgnoreCase),
                            @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet"));
                }

                key.Close();
            }
            catch (Exception)
            {
            }
        }

        protected override string Convert(string aPath)
        {
            foreach (var dir in KeyLinks)
            {
                if (!dir.Key.IsMatch(aPath))
                    continue;

                return dir.Key.Replace(aPath, dir.Value);
            }

            return aPath;
        }
    }
}