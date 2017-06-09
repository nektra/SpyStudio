using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace SpyStudio.FileAssociation
{
    internal class RegistryClasses
    {
        /// <summary>
        /// Reads specified value from the HKEY_CLASSES_ROOT registry hive.
        /// </summary>
        /// <param name="path">Registry key path, minus root, that contains value.</param>
        /// <param name="valueName">Name of the value within key that will be read.</param>
        /// <returns>Read value if successful. Otherwise null.</returns>
        static public object Read(string path, string valueName)
        {
            RegistryKey key = Microsoft.Win32.Registry.ClassesRoot;

            if (path.Length == 0)
                return null;

            key = key.OpenSubKey(path);

            if (key == null)
                return null;
            return key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        }

        static public bool Exists(string subkey)
        {
            RegistryKey root = Microsoft.Win32.Registry.ClassesRoot;
            try
            {
                RegistryKey key = root.OpenSubKey(subkey);
                if (key == null)
                    return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

    }
}