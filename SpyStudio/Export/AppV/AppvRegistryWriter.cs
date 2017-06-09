using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.Extensions;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV
{
    public static class AppvRegistryWriter
    {
        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint OROpenHive(
            string lpHivePath,
            out uint phkResult
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORCreateHive(
            out uint phkResult
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORCloseHive(
            uint Handle
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORSaveHive(
            uint Handle,
            string lpHivePath,
            uint dwOsMajorVersion,
            uint dwOsMinorVersion
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint OROpenKey(
            uint Handle,
            [Optional] string lpSubKeyName,
            out uint phkResult
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORCloseKey(
            uint Handle
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORCreateKey(
            uint Handle,
            string lpSubKey,
            [Optional] string lpClass,
            [Optional] uint dwOptions,
            [Optional] IntPtr pSecurityDescriptor,
            out uint phkResult,
            out uint pdwDisposition
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORDeleteKey(
            uint Handle,
            [Optional] string lpSubKey
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint OREnumKey(
            uint Handle,
            uint dwIndex,
            StringBuilder lpName,
            ref uint lpcName,
            [Optional] StringBuilder lpClass,
            ref uint lpcClass,
            [Optional] IntPtr lpftLastWriteTime
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORDeleteValue(
            uint Handle,
            [Optional] string lpValueName
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORGetValue(
            uint Handle,
            [Optional] string lpSubKey,
            [Optional] string lpValue,
            out uint pdwType,
            [Optional] IntPtr pvData,
            ref uint pcbData
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint ORSetValue(
            uint Handle,
            [Optional] string lpValueName,
            uint dwType,
            [Optional] IntPtr lpData,
            uint cbData
        );

        [DllImport("offreg.dll", CharSet = CharSet.Unicode)]
        public static extern uint OREnumValue(
            uint Handle,
            uint dwIndex,
            out string lpValueName,
            ref uint lpcValueName,
            out uint lpType,
            [Optional] IntPtr lpData,
            ref uint lpcbData
        );

        private static void List(uint handle, string path, List<string> list)
        {
            const uint ERROR_NO_MORE_ITEMS = 0x103;
            for (uint i = 0; ; i++)
            {
                var name = new StringBuilder(0x1000);
                var nameLength = (uint)name.Capacity;
                uint unused = 0;

                uint result = OREnumKey(handle, i, name, ref nameLength, null, ref unused, IntPtr.Zero);
                if (result == ERROR_NO_MORE_ITEMS)
                    break;

                var s = path + "\\" + name;

                list.Add(s);
                uint subkey;
                var error = OROpenKey(handle, name.ToString(), out subkey);
                if (error != 0)
                    continue;
                try
                {
                    List(subkey, s, list);
                }
                finally
                {
                    ORCloseKey(subkey);
                }
            }
        }

        public static void Test()
        {
            uint handle;
            uint result = OROpenHive(@"c:\Users\Victor\Programming\Office2010.appv\Office2010\Registry.dat", out handle);

            var list = new List<string>();

            List(handle, string.Empty, list);

            ORCloseHive(handle);
        }

        private static readonly Regex LocalMachineRegex = new Regex(@"^HKEY_LOCAL_MACHINE($|\\.*)", RegexOptions.IgnoreCase);
        private static readonly Regex CurrentUserRegex = new Regex(@"^HKEY_CURRENT_USER($|\\.*)", RegexOptions.IgnoreCase);

        public class NativeErrorException : Exception
        {
            public uint Error;

            public NativeErrorException(uint error)
            {
                Error = error;
            }
        }

        public static void ExportRegistry(string dstFileName, IEnumerable<RegKeyInfo> keys)
        {
            uint BaseHandle;
            uint error = ORCreateHive(out BaseHandle);
            if (error != 0)
                throw new NativeErrorException(error);

            try
            {
                foreach (var regKeyInfo in keys)
                {
                    var path = regKeyInfo.OriginalPath;
                    var match = LocalMachineRegex.Match(path);
                    if (!match.Success)
                    {
                        match = CurrentUserRegex.Match(path);
                        if (!match.Success)
                            continue;

                        path = @"\REGISTRY\USER\[{AppVCurrentUserSID}]\" + match.Groups[1];
                    }
                    else
                    {
                        path = @"\REGISTRY\MACHINE\" + match.Groups[1];
                    }

                    var parts = path.SplitAsPath().ToArray();

                    var key = BaseHandle;
                    var closeKey = false;
                    try
                    {
                        foreach (var part in parts)
                        {
                            uint newKey;
                            uint disposition;
                            error = ORCreateKey(key, part, null, 0, IntPtr.Zero, out newKey, out disposition);
                            if (closeKey)
                                ORCloseKey(key);
                            if (error != 0)
                                throw new NativeErrorException(error);
                            key = newKey;
                            closeKey = true;
                        }
                        foreach (var value in regKeyInfo.ValuesByName.Values)
                        {
                            var bytes = RegistryTools.GetRegValueForWinApi(value);
                            IntPtr p = IntPtr.Zero;
                            int length = 0;
                            if (bytes != null)
                            {
                                length = bytes.Length;
                                p = Marshal.AllocHGlobal(length);
                                Marshal.Copy(bytes, 0, p, length);
                            }
                            error = ORSetValue(key, value.Name, (uint) value.ValueType, p, (uint) length);
                            if (error != 0)
                                throw new NativeErrorException(error);
                            if (bytes != null)
                                Marshal.FreeHGlobal(p);
                        }
                    }
                    finally
                    {
                        if (closeKey)
                            ORCloseKey(key);
                    }
                }

                error = ORSaveHive(BaseHandle, dstFileName, 6, 1);
                if (error != 0)
                    throw new NativeErrorException(error);
            }
            finally
            {
                ORCloseHive(BaseHandle);
            }
        }
    }
}
