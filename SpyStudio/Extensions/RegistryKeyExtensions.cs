using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using RegistryWin32 = Microsoft.Win32.Registry;

namespace SpyStudio.Extensions
{
    public static class RegistryKeyExtensions
    {
        public static IEnumerable<string> GetStringValues(this RegistryKey aRegistryKey)
        {
            var values = new List<string>();

            var stringValueNames =
                aRegistryKey.GetValueNames().Where(
                    valueName => aRegistryKey.GetValueKind(valueName) == RegistryValueKind.String);

            foreach (var valueName in stringValueNames)
                values.Add((string)aRegistryKey.GetValue(valueName));

            return values;
        }

        public static IEnumerable<RegistryKey> GetSubKeys(this RegistryKey aRegistryKey)
        {
            var subKeys = new List<RegistryKey>();

            var subKeyNames = aRegistryKey.GetSubKeyNames();

            foreach (var subKeyName in subKeyNames)
                subKeys.Add(aRegistryKey.OpenSubKey(subKeyName));

            return subKeys;
        }

        public static IEnumerable<RegistryKey> GetSubKeysRecursively(this RegistryKey aRegistryKey)
        {
            var allSubKeys = new List<RegistryKey>();

            var subKeys = aRegistryKey.GetSubKeys();
            allSubKeys.AddRange(subKeys);

            foreach (var subKey in subKeys)
                allSubKeys.AddRange(subKey.GetSubKeysRecursively());

            return allSubKeys;
        }

        public static IEnumerable<string> GetStringValuesRecursively(this RegistryKey aRegistryKey)
        {
            var allValues = new List<string>();

            allValues.AddRange(aRegistryKey.GetStringValues());

            var subKeys = aRegistryKey.GetSubKeysRecursively();

            foreach (var subKey in subKeys)
                allValues.AddRange(subKey.GetStringValuesRecursively());

            return allValues;
        }

        public static bool Contains(this RegistryKey aRegistryKey, string aSubKey)
        {
            var key = aRegistryKey.OpenSubKey(aSubKey);

            if (key == null)
                return false;

            key.Close();
            return true;
        }
    }
}
