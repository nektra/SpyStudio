using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using SpyStudio.Export.AppV;
using SpyStudio.FileSystem;
using SpyStudio.Registry;
using SpyStudio.Registry.Infos;
using RegistryWin32 = Microsoft.Win32.Registry;
using SpyStudio.Properties;
using SpyStudio.Extensions;

namespace SpyStudio.Tools.Registry
{
    public class RegistryTools
    {
        public const UInt64 HkeyMask = 0x80000000;
        public const UInt64 HkeyClassesRoot = HkeyMask | 0;
        public const UInt64 HkeyCurrentUser = HkeyMask | 1;
        public const UInt64 HkeyLocalMachine = HkeyMask | 2;
        public const UInt64 HkeyUsers = HkeyMask | 3;
        public const UInt64 HkeyPerformanceData = HkeyMask | 4;
        public const UInt64 HkeyCurrentConfig = HkeyMask | 5;
        public const UInt64 HkeyDynData = HkeyMask | 6;
        public const UInt64 HkeyCurrentUserLocalSettings = HkeyMask | 7;
        public const UInt64 HkeyPerformanceText = HkeyMask | 0x50;
        public const UInt64 HkeyPerformanceNlstext = HkeyMask | 0x60;

        public static UIntPtr HkeyClassesRootPtr = new UIntPtr(HkeyClassesRoot);
        public static UIntPtr HkeyCurrentUserPtr = new UIntPtr(HkeyCurrentUser);
        public static UIntPtr HkeyLocalMachinePtr = new UIntPtr(HkeyLocalMachine);

        // WORKAROUND: it should be an issue in Deviare, on 64 bits it returns keys like this
        public const UInt64 HkeyMask64 = 0xffffffff80000000;
        public const UInt64 HkeyClassesRoot64 = HkeyMask64 | 0;
        public const UInt64 HkeyCurrentUser64 = HkeyMask64 | 1;
        public const UInt64 HkeyLocalMachine64 = HkeyMask64 | 2;
        public const UInt64 HkeyUsers64 = HkeyMask64 | 3;
        public const UInt64 HkeyPerformanceData64 = HkeyMask64 | 4;
        public const UInt64 HkeyCurrentConfig64 = HkeyMask64 | 5;
        public const UInt64 HkeyDynData64 = HkeyMask64 | 6;
        public const UInt64 HkeyCurrentUserLocalSettings64 = HkeyMask64 | 7;
        public const UInt64 HkeyPerformanceText64 = HkeyMask64 | 0x50;
        public const UInt64 HkeyPerformanceNlstext64 = HkeyMask64 | 0x60;

        public static string HkeyClassesRoot64String = "HKEY_CLASSES_ROOT64";
        public static string HkeyCurrentUser64String = "HKEY_CURRENT_USER64";
        public static string HkeyLocalMachine64String = "HKEY_LOCAL_MACHINE64";
        public static string HkeyUsers64String = "HKEY_USERS64";
        public static string HkeyPerformanceData64String = "HKEY_PERFORMANCE_DATA64";
        public static string HkeyCurrentConfig64String = "HKEY_CURRENT_CONFIG64";
        public static string HkeyDynData64String = "HKEY_DYN_DATA64";
        public static string HkeyCurrentUserLocalSettings64String = "HKEY_CURRENT_USER_LOCAL_SETTINGS64";
        public static string HkeyPerformanceText64String = "HKEY_PERFORMANCE_TEXT64";
        public static string HkeyPerformanceNlstext64String = "HKEY_PERFORMANCE_NLSTEXT64";

        public static string HkeyClassesRootString = "HKEY_CLASSES_ROOT";
        public static string HkeyCurrentUserString = "HKEY_CURRENT_USER";
        public static string HkeyLocalMachineString = "HKEY_LOCAL_MACHINE";
        public static string HkeyUsersString = "HKEY_USERS";
        public static string HkeyPerformanceDataString = "HKEY_PERFORMANCE_DATA";
        public static string HkeyCurrentConfigString = "HKEY_CURRENT_CONFIG";
        public static string HkeyDynDataString = "HKEY_DYN_DATA";
        public static string HkeyCurrentUserLocalSettingsString = "HKEY_CURRENT_USER_LOCAL_SETTINGS";
        public static string HkeyPerformanceTextString = "HKEY_PERFORMANCE_TEXT";
        public static string HkeyPerformanceNlstextString = "HKEY_PERFORMANCE_NLSTEXT";

        public static string HkeyClassesRootSmallString = "HKCR";
        public static string HkeyCurrentUserSmallString = "HKCU";
        public static string HkeyLocalMachineSmallString = "HKLM";
        public static string HkeyUsersSmallString = "HKU";
        public static string HkeyPerformanceDataSmallString = "HKPD";
        public static string HkeyCurrentConfigSmallString = "HKCC";
        public static string HkeyDynDataSmallString = "HKDD";
        public static string HkeyCurrentUserLocalSettingsSmallString = "HKCULSS";
        public static string HkeyPerformanceTextSmallString = "HKPTSS";
        public static string HkeyPerformanceNlstextSmallString = "HKPNSS";

        public enum RegSam
        {
            QueryValue = 0x0001,
            SetValue = 0x0002,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Notify = 0x0010,
            CreateLink = 0x0020,
            Wow6432Key = 0x0200,
            Wow6464Key = 0x0100,
            Wow64Res = 0x0300,
            Read = 0x00020019,
            Write = 0x00020006,
            Execute = 0x00020019,
            AllAccess = 0x000f003f
        }

        public static readonly Dictionary<ulong, string> StandardKeys = new Dictionary<ulong, string>();
        public static readonly Dictionary<ulong, string> StandardSwvKeys = new Dictionary<ulong, string>();

        private static int _currentUsersIdentityKeyLength;
        private static string _currentUsersKeyClasses;
        private static int _currentUsersKeyClassesLength;
        private static string _currentUsersKey2;
        private static int _currentUsersKey2Length;
        private static string _localMachine;
        private static int _localMachineLength;
        private static string _currentIdentity;
        private static string _currentUsersIdentityKey;
        private static string _defaultUsersIdentityKey;
        private static int _defaultUsersIdentityKeyLength;
        private static string _currentUsersKey;
        private static int _currentUsersKeyLength;

        public static string GetStandardKey(ulong id)
        {
            return StandardKeys[id];
        }


        public static void Initialize()
        {
            StandardKeys[HkeyCurrentUser64] = HkeyCurrentUser64String;
            StandardKeys[HkeyClassesRoot64] = HkeyClassesRoot64String;
            StandardKeys[HkeyLocalMachine64] = HkeyLocalMachine64String;
            StandardKeys[HkeyUsers64] = HkeyUsers64String;
            StandardKeys[HkeyPerformanceData64] = HkeyPerformanceData64String;
            StandardKeys[HkeyCurrentConfig64] = HkeyCurrentConfig64String;
            StandardKeys[HkeyDynData64] = HkeyDynData64String;
            StandardKeys[HkeyCurrentUserLocalSettings64] = HkeyCurrentUserLocalSettings64String;
            StandardKeys[HkeyPerformanceText64] = HkeyPerformanceText64String;
            StandardKeys[HkeyPerformanceNlstext64] = HkeyPerformanceNlstext64String;

            StandardKeys[HkeyCurrentUser] = HkeyCurrentUserString;
            StandardKeys[HkeyClassesRoot] = HkeyClassesRootString;
            StandardKeys[HkeyLocalMachine] = HkeyLocalMachineString;
            StandardKeys[HkeyUsers] = HkeyUsersString;
            StandardKeys[HkeyPerformanceData] = HkeyPerformanceDataString;
            StandardKeys[HkeyCurrentConfig] = HkeyCurrentConfigString;
            StandardKeys[HkeyDynData] = HkeyDynDataString;
            StandardKeys[HkeyCurrentUserLocalSettings] = HkeyCurrentUserLocalSettingsString;
            StandardKeys[HkeyPerformanceText] = HkeyPerformanceTextString;
            StandardKeys[HkeyPerformanceNlstext] = HkeyPerformanceNlstextString;

            StandardSwvKeys[HkeyCurrentUser64] = @"HKEY_USERS64\USER_TEMPLATE";
            StandardSwvKeys[HkeyCurrentUser] = @"HKEY_USERS\USER_TEMPLATE";

            var identity = WindowsIdentity.GetCurrent();

            Debug.Assert(identity != null, "identity != null");
            Debug.Assert(identity.User != null, "identity.User != null");
            _currentIdentity = identity.User.ToString();
            _currentUsersKey = @"\REGISTRY\USER";
            _currentUsersKeyLength = _currentUsersKey.Length;
            _currentUsersIdentityKey = @"\REGISTRY\USER\" + _currentIdentity;
            _currentUsersIdentityKeyLength = _currentUsersIdentityKey.Length;
            _defaultUsersIdentityKey = @"\REGISTRY\USER\.DEFAULT";
            _defaultUsersIdentityKeyLength = _defaultUsersIdentityKey.Length;
            _currentUsersKeyClasses = @"\REGISTRY\USER\" + _currentIdentity + "_CLASSES";
            _currentUsersKeyClassesLength = _currentUsersKeyClasses.Length;
            _currentUsersKey2 = @"HKEY_USERS\" + _currentIdentity;
            _currentUsersKey2Length = _currentUsersKey2.Length;
            _localMachine = @"\REGISTRY\MACHINE";
            _localMachineLength = _localMachine.Length;
        }

        public static Dictionary<string, string> KeyAbbreviations = new Dictionary<string, string>
                                                                        {
                                                                            {
                                                                                HkeyClassesRootSmallString,
                                                                                HkeyClassesRootString
                                                                                },
                                                                            {
                                                                                HkeyCurrentUserSmallString,
                                                                                HkeyCurrentUserString
                                                                                },
                                                                            {
                                                                                HkeyLocalMachineSmallString,
                                                                                HkeyLocalMachineString
                                                                                },
                                                                            {
                                                                                HkeyPerformanceDataSmallString,
                                                                                HkeyPerformanceDataString
                                                                                },
                                                                            {
                                                                                HkeyUsersSmallString,
                                                                                HkeyUsersString
                                                                                },
                                                                            {
                                                                                HkeyCurrentConfigSmallString,
                                                                                HkeyCurrentConfigString
                                                                                },
                                                                            {
                                                                                HkeyDynDataSmallString,
                                                                                HkeyDynDataString
                                                                                },
                                                                            {
                                                                                HkeyCurrentUserLocalSettingsSmallString
                                                                                ,
                                                                                HkeyCurrentUserLocalSettingsString
                                                                                },
                                                                            {
                                                                                HkeyPerformanceTextSmallString,
                                                                                HkeyPerformanceTextString
                                                                                },
                                                                            {
                                                                                HkeyPerformanceNlstextSmallString,
                                                                                HkeyPerformanceNlstextString
                                                                                },
                                                                        };

        public static uint KeyAllAccess = 0xF003F;
        public static uint KeyEnumerateSubKeys = 0x0008;
        public static uint KeyQueryValue = 0x0001;
        public static uint KeyRead = 0x20019;
        public static uint KeySetValue = 0x0002;
        public static uint KeyCreateSubKey = 0x0004;
        public static uint KeyWrite = 0x20006;

        public static RegistryValueKind GetValueTypeFromString(string valueType)
        {
            var ret = RegistryValueKind.Unknown;
            switch (valueType)
            {
                case "REG_BINARY":
                    ret = RegistryValueKind.Binary;
                    break;
                case "REG_DWORD_LITTLE_ENDIAN":
                case "REG_DWORD_BIG_ENDIAN":
                case "REG_DWORD":
                    ret = RegistryValueKind.DWord;
                    break;
                case "REG_EXPAND_SZ":
                    ret = RegistryValueKind.ExpandString;
                    break;
                case "REG_MULTI_SZ":
                    ret = RegistryValueKind.MultiString;
                    break;
                case "REG_LINK":
                case "REG_NONE":
                    ret = RegistryValueKind.Unknown;
                    break;
                case "REG_QWORD_LITTLE_ENDIAN":
                case "REG_QWORD":
                    ret = RegistryValueKind.QWord;
                    break;
                case "REG_SZ":
                    ret = RegistryValueKind.String;
                    break;
            }
            return ret;
        }

        public static string GetValueTypeString(RegistryValueKind valueType)
        {
            var ret = string.Empty;
            switch (valueType)
            {
                case RegistryValueKind.Binary:
                    ret = "REG_BINARY";
                    break;
                case RegistryValueKind.DWord:
                    ret = "REG_DWORD";
                    break;
                case RegistryValueKind.ExpandString:
                    ret = "REG_EXPAND_SZ";
                    break;
                case RegistryValueKind.MultiString:
                    ret = "REG_MULTI_SZ";
                    break;
                    //case RegistryValueKind.Unknown:
                    //    ret = "";
                    //    break;
                case RegistryValueKind.QWord:
                    ret = "REG_QWORD";
                    break;
                case RegistryValueKind.String:
                    ret = "REG_SZ";
                    break;
            }
            return ret;
        }

        public static string GetRegValueRepresentation(uint val)
        {
            string valueData;
            if (Settings.Default.ShowUINTAsHex)
                valueData = "0x" + val.ToString("X").PadLeft(8, '0') + " (" + val.ToString(CultureInfo.InvariantCulture) +
                            ")";
            else
                valueData = val.ToString(CultureInfo.InvariantCulture);
            return valueData;
        }

        public static string GetRegValueRepresentation(UInt64 val)
        {
            string valueData;
            if (Settings.Default.ShowUINTAsHex)
                valueData = "0x" + val.ToString("X").PadLeft(16, '0') + " (" +
                            val.ToString(CultureInfo.InvariantCulture) + ")";
            else
                valueData = val.ToString(CultureInfo.InvariantCulture);
            return valueData;
        }

        public static string GetRegValueRepresentation(string value, RegistryValueKind valueType)
        {
            var ret = value;
            try
            {
                switch (valueType)
                {
                    case RegistryValueKind.DWord:
                        {
                            var val = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                            ret = GetRegValueRepresentation(val);
                            break;
                        }
                    case RegistryValueKind.QWord:
                        {
                            if (!string.IsNullOrEmpty(value))
                            {
                                var val = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                                ret = GetRegValueRepresentation(val);
                            }
                            else
                            {
                                ret = null;
                            }
                            break;
                        }
                }
            }
            catch (Exception)
            {
                ret = null;
            }
            return ret;
        }

        public static object GetRegValue(RegValueInfo v)
        {
            object ret = null;

            switch (v.ValueType)
            {
                case RegistryValueKind.DWord:
                    {
                        if (v.Data.StartsWith("0x"))
                        {
                            var index = v.Data.IndexOf(' ');
                            var data = (index != -1 ? v.Data.Substring(2, index - 2) : v.Data.Substring(2));
                            ret = Convert.ToInt32(data, 16);
                        }
                        else
                        {
                            ret = Convert.ToInt32(v.Data, CultureInfo.InvariantCulture);
                        }
                        break;
                    }
                case RegistryValueKind.QWord:
                    {
                        if (v.Data.StartsWith("0x"))
                        {
                            var index = v.Data.IndexOf(' ');
                            var data = (index != -1 ? v.Data.Substring(2, index - 2) : v.Data.Substring(2));
                            ret = Convert.ToInt64(data, 16);
                        }
                        else
                        {
                            ret = Convert.ToInt64(v.Data, CultureInfo.InvariantCulture);
                        }
                        break;
                    }
                case RegistryValueKind.Unknown:
                    {
                        ret = null;
                        break;
                    }
                case RegistryValueKind.Binary:
                    {
                        var index = 0;
                        var data = new List<byte>();
                        while (index < v.Data.Length)
                        {
                            if (v.Data[index].IsHex())
                            {
                                var start = index;
                                while (index < v.Data.Length && v.Data[index].IsHex())
                                    index++;
                                var current = v.Data.Substring(start, index - start).Trim();
                                try
                                {
                                    var dataNumber = Convert.ToByte(current, 16);
                                    data.Add(dataNumber);
                                }
                                catch (Exception ex)
                                {
                                    Error.WriteLine(@"Cannot convert Byte: '" + current + "' " + ex.Message);
                                }
                            }
                            else
                            {
                                index++;
                            }
                        }
                        ret = data.ToArray();
                        break;
                    }
                case RegistryValueKind.MultiString:
                    {
                        ret = v.Data.Split('\0').Where(x => x.Length != 0).ToArray();
                        break;
                    }
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.String:
                    ret = v.Data;
                    break;
            }
            return ret;
        }

        public static byte[] GetRegValueForWinApi(RegValueInfo v)
        {
            var obj = GetRegValue(v);
            if (obj == null)
                return null;

            switch (v.ValueType)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return Encoding.Unicode.GetBytes((string)obj);
                case RegistryValueKind.Binary:
                    return (byte[])obj;
                case RegistryValueKind.DWord:
                    return BitConverter.GetBytes((int)obj);
                case RegistryValueKind.MultiString:
                {
                    var ret = new List<byte>();
                    foreach (var s in (string[]) obj)
                    {
                        ret.AddRange(Encoding.Unicode.GetBytes(s));
                        ret.Add(0);
                    }
                    ret.Add(0);
                    return ret.ToArray();
                }
                case RegistryValueKind.QWord:
                    return BitConverter.GetBytes((long)obj);
                case RegistryValueKind.Unknown:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToRegistryFormat(string valueData, RegistryValueKind valueKind)
        {
            string ret = null;

            switch (valueKind)
            {
                case RegistryValueKind.DWord:
                    {
                        UInt32 int32;
                        if (valueData.StartsWith("0x"))
                        {
                            var index = valueData.IndexOf(' ');
                            var data = (index != -1 ? valueData.Substring(2, index - 2) : valueData.Substring(2));
                            int32 = Convert.ToUInt32(data, 16);
                        }
                        else
                        {
                            int32 = Convert.ToUInt32(valueData, CultureInfo.InvariantCulture);
                        }
                        ret = "dword:" + String.Format("{0:x}", int32).PadLeft(8, '0');
                        break;
                    }
                case RegistryValueKind.QWord:
                    {
                        UInt64 int64;
                        if (valueData.StartsWith("0x"))
                        {
                            var index = valueData.IndexOf(' ');
                            var data = (index != -1 ? valueData.Substring(2, index - 2) : valueData.Substring(2));
                            int64 = Convert.ToUInt64(data, 16);
                        }
                        else
                        {
                            int64 = Convert.ToUInt64(valueData, CultureInfo.InvariantCulture);
                        }

                        ret = "hex(b):";

                        // convert to byte array because registry use this format for QWORD
                        var binData = BitConverter.GetBytes(int64);
                        for (var i = 0; i < binData.Length; i++)
                        {
                            if (i != 0)
                                ret += ",";
                            ret += Convert.ToByte(binData[i]).ToString("x2");
                        }

                        break;
                    }
                case RegistryValueKind.Unknown:
                case RegistryValueKind.Binary:
                    {
                        //if (v.Type == RegistryValueKind.Unknown)
                        //    Console.WriteLine("");
                        var binData = Regex.Split(valueData, " ");
                        if (binData.Length == 1 && string.IsNullOrEmpty(binData[0]))
                            binData = new string[0];
                        ret = (valueKind == RegistryValueKind.Unknown ? "hex(0):" : "hex:") + (binData.Length != 0
                                                                                                     ? "\\\r\n"
                                                                                                     : string.Empty);

                        for (var i = 0; i < binData.Length; i++)
                        {
                            if (i != 0)
                            {
                                ret += ",";
                                if (i % 20 == 0)
                                {
                                    ret += "\\\r\n";
                                }
                            }
                            ret += binData[i];
                        }

                        break;
                    }
                case RegistryValueKind.MultiString:
                    {
                        var binData = Regex.Split(valueData, " ");
                        if (binData.Length == 1 && string.IsNullOrEmpty(binData[0]))
                            binData = new string[0];
                        ret = "hex(7):" + (binData.Length != 0
                                               ? "\\\r\n"
                                               : string.Empty);
                        for (var i = 0; i < valueData.Length; i++)
                        {
                            if (i != 0)
                            {
                                ret += ",";
                                if (i % 10 == 0)
                                {
                                    ret += "\\\r\n";
                                }
                            }
                            var thisChar = ((int)valueData[i]).ToString("x4");
                            ret += thisChar.Substring(2, 2) + "," + thisChar.Substring(0, 2);
                        }
                        //if (ValueData.Length > 0)
                        //    ret += ",";
                        //ret += "\\\r\n00,00";
                        break;
                    }
                case RegistryValueKind.ExpandString:
                    {
                        ret = "hex(2):" + (valueData.Length != 0
                                               ? "\\\r\n"
                                               : string.Empty);
                        for (var i = 0; i < valueData.Length; i++)
                        {
                            if (i != 0)
                            {
                                ret += ",";
                                if (i % 10 == 0)
                                {
                                    ret += "\\\r\n";
                                }
                            }
                            var thisChar = ((int)valueData[i]).ToString("x4");
                            ret += thisChar.Substring(2, 2) + "," + thisChar.Substring(0, 2);
                        }
                        if (valueData.Length > 0)
                            ret += ",";
                        ret += "\\\r\n00,00";
                        break;
                    }
                case RegistryValueKind.String:
                    {
                        ret = "\"" + valueData.Replace(@"\", @"\\").Replace("\"", "\\\"") + "\"";
                    }
                    break;
            }
            return ret;
        }

        public static string ToRegistryFormat(RegValueInfo v)
        {
            return ToRegistryFormat(v.Data, v.ValueType);
        }

        public static byte[] GetHexRegistryValue(string valueString, int startIndex)
        {
            var success = true;
            var data = new List<byte>();
            var i = startIndex;
            while (i < valueString.Length)
            {
                // skip non numeric chars
                while (i < valueString.Length && !valueString[i].IsHex())
                {
                    i++;
                }

                var startByte = i;
                while (i < valueString.Length && valueString[i].IsHex())
                {
                    i++;
                }
                // bytes should be at least 2 chars
                if (i - startByte <= 2 && i - startByte > 0)
                {
                    try
                    {
                        data.Add(Convert.ToByte(valueString.Substring(startByte, i - startByte), 16));
                    }
                    catch (Exception)
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (!success)
                return null;
            return data.ToArray();
        }

        public static bool FromRegistryFormat(string valueString, ref RegValueInfo v)
        {
            var ret = false;

            if (valueString.StartsWith("dword:"))
            {
                v.ValueType = RegistryValueKind.DWord;
                var i = "dword:".Length;
                var startIndex = i;
                while (i < valueString.Length && valueString[i].IsHex())
                {
                    i++;
                }
                v.Data = "0x" + valueString.Substring(startIndex, i - startIndex);
                ret = true;
            }
                // QWord 56,34,12,90,78,56,34,12
            else if (valueString.StartsWith("hex(b):"))
            {
                v.ValueType = RegistryValueKind.QWord;

                var data = GetHexRegistryValue(valueString, "hex(b):".Length);

                if (data != null && data.Length == 8)
                {
                    var dataUInt64 = BitConverter.ToUInt64(data, 0);
                    v.Data = "0x" + dataUInt64.ToString("X");
                    ret = true;
                }
                else
                {
                    Error.WriteLine("Error parsing QWord: " + valueString);
                }
            }
                // Binary || REG_NONE
            else if (valueString.StartsWith("hex:") || valueString.StartsWith("hex(0):"))
            {
                v.ValueType = valueString.StartsWith("hex(0):")
                                  ? RegistryValueKind.Unknown
                                  : RegistryValueKind.Binary;

                var length = valueString.StartsWith("hex(0):") ? "hex(0):".Length : "hex:".Length;
                var data = GetHexRegistryValue(valueString, length);

                if (data != null)
                {
                    v.Data = "";
                    foreach (var b in data)
                    {
                        if (v.Data.Length > 0)
                            v.Data += " ";
                        v.Data += b.ToString("x2");
                    }
                    ret = true;
                }
                else
                {
                    Error.WriteLine("Error parsing Binary: " + valueString);
                }
            }
                // MultiString || ExpandString
            else if (valueString.StartsWith("hex(7):") || valueString.StartsWith("hex(2):"))
            {
                v.ValueType = valueString.StartsWith("hex(7):")
                                  ? RegistryValueKind.MultiString
                                  : RegistryValueKind.ExpandString;

                //if(valueString.StartsWith("hex(7):"))
                //    Console.WriteLine("");
                var data = GetHexRegistryValue(valueString, "hex(7):".Length);

                if (data != null)
                {
                    v.Data = "";
                    for (var i = 0; i < data.Length; i += 2)
                    {
                        var shortData = BitConverter.ToUInt16(data, i);
                        v.Data += Convert.ToChar(shortData);
                    }
                    ret = true;
                }
                else
                {
                    Error.WriteLine("Error parsing MultiString: " + valueString);
                }
            }
                // String
            else if (valueString.StartsWith("\""))
            {
                v.ValueType = RegistryValueKind.String;
                var endIndex = valueString.LastIndexOf("\"", StringComparison.InvariantCulture);
                if (endIndex != -1)
                {
                    v.Data = valueString.Substring(1, endIndex - 1);
                    v.Data = v.Data.Replace("\\\\", "\\").Replace("\\\"", "\"");
                    ret = true;
                }
            }
            else
            {
                Debug.WriteLine("Unknown Registry Format: " + valueString);
            }
            return ret;
        }

        public static string FixBackSlashesIn(string key)
        {
            var normalizedKey = key.Replace("\\\\", "\\");
            while (normalizedKey != key)
            {
                key = normalizedKey;
                normalizedKey = key.Replace("\\\\", "\\");
            }
            if (normalizedKey.EndsWith("\\"))
                normalizedKey = normalizedKey.Substring(0, normalizedKey.Length - 1);

            return normalizedKey;
        }
        public static string GetKeyPath(UInt64 hKey)
        {
            return GetKeyPath(hKey, (uint) Process.GetCurrentProcess().Id);
        }

        public static string GetKeyPath(UInt64 hKey, uint pid)
        {
            var ret = "";
            var localHKey = IntPtr.Zero;
            var ownedLocalHKey = false;

            if (pid != Process.GetCurrentProcess().Id)
            {
                var hRemoteProc = PlatformTools.GetProcessHandle(pid);
                if (hRemoteProc != IntPtr.Zero)
                {
                    if (!Declarations.DuplicateHandle(hRemoteProc, (IntPtr) hKey, Process.GetCurrentProcess().Handle,
                                                      out localHKey, 0, false, 0x2))
                    {
                        Error.WriteLine("Error DuplicateHandle: 0x" + hKey.ToString("X") + Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        ownedLocalHKey = true;
                    }
                }
            }
            else
            {
                localHKey = (IntPtr) hKey;
            }
            if (localHKey != IntPtr.Zero)
            {
                uint keyNameInfoLength = 0;
                Declarations.NtQueryKey(localHKey, 3, null, 0, ref keyNameInfoLength);
                if (keyNameInfoLength != 0)
                {
                    var nameInfoBuffer = new short[keyNameInfoLength/2];
                    if (
                        Declarations.NtQueryKey(localHKey, 3, nameInfoBuffer, keyNameInfoLength, ref keyNameInfoLength) ==
                        0)
                    {
                        // nameInfoBuffer is a KEY_NAME_INFORMATION struct. The first 4 bytes contain an ULong with the name length. Next bytes are the
                        // name in unicode characters.
                        var keyNameLengthByteArray = new[]
                                                         {
                                                             BitConverter.GetBytes(nameInfoBuffer[0])[0],
                                                             BitConverter.GetBytes(nameInfoBuffer[0])[1],
                                                             BitConverter.GetBytes(nameInfoBuffer[1])[0],
                                                             BitConverter.GetBytes(nameInfoBuffer[1])[1]
                                                         };

                        var keyNameLength = (int) BitConverter.ToUInt32(keyNameLengthByteArray, 0);

                        if (keyNameInfoLength != 0)
                        {
                            var keyNameArray =
                                nameInfoBuffer.ToList().GetRange(2, keyNameLength/2).Select(
                                    wChar => BitConverter.ToChar(BitConverter.GetBytes(wChar), 0)).ToArray();

                            ret = new string(keyNameArray);
                        }
                    }
                    else
                    {
                        Error.WriteLine("Error QueryKey: 0x" + localHKey.ToString("X"));
                    }
                }
                if (ownedLocalHKey)
                    Declarations.CloseHandle(localHKey);
            }
            return ret;
        }

        public static string ReplaceKnownKeys(string key, ref bool found)
        {
            var keyLength = key.Length;
            int pos;


            if (key.StartsWith(_defaultUsersIdentityKey, StringComparison.OrdinalIgnoreCase))
            {
                key = "HKEY_USERS\\.DEFAULT" + key.Substring(_defaultUsersIdentityKeyLength);
                found = true;
            }
            else if (key.StartsWith(_currentUsersKey, StringComparison.OrdinalIgnoreCase))
            {
                // replace HKEY_USERS\User_Sid by HKEY_CURRENT_USER
                pos = (keyLength < _currentUsersIdentityKeyLength
                           ? -1
                           : key.IndexOf(_currentUsersIdentityKey, 0, _currentUsersIdentityKeyLength,
                                         StringComparison.OrdinalIgnoreCase));
                if (pos != -1)
                {
                    pos = (keyLength < _currentUsersKeyClassesLength
                               ? -1
                               : key.IndexOf("_CLASSES", _currentUsersIdentityKeyLength,
                                             _currentUsersKeyClassesLength - _currentUsersIdentityKeyLength,
                                             StringComparison.OrdinalIgnoreCase));
                    if (pos != -1)
                    {
                        //key = @"HKEY_CURRENT_USER_CLASSES" + key.Substring(_currentUsersKeyClassesLength);
                        key = @"HKEY_CURRENT_USER\Software\Classes" + key.Substring(_currentUsersKeyClassesLength);
                    }
                    else
                    {
                        key = "HKEY_CURRENT_USER" + key.Substring(_currentUsersIdentityKeyLength);
                    }
                    found = true;
                    //key = key.Replace(CurrentUsersKey, "HKEY_CURRENT_USER");
                    //Console.WriteLine("HKEY_USER: " + key);
                }
                else
                {
                    key = @"HKEY_CURRENT_USER" + key.Substring(_currentUsersKeyLength);
                    found = true;
                }
                //if (key.StartsWith(@"HKEY_CURRENT_USER\Software\Classes", StringComparison.OrdinalIgnoreCase))
                //{
                //    key = @"HKEY_CURRENT_USER_CLASSES" + key.Substring(34);
                //}
            }
            else
            {
                pos = (keyLength < _localMachineLength
                           ? -1
                           : key.IndexOf(_localMachine, 0, _localMachineLength, StringComparison.OrdinalIgnoreCase));
                if (pos != -1)
                {
                    key = @"HKEY_LOCAL_MACHINE" + key.Substring(_localMachineLength);
                    found = true;
                }
                else
                {
                    pos = (keyLength < _currentUsersKey2Length
                               ? -1
                               : key.IndexOf(_currentUsersKey2, 0, _currentUsersKey2Length,
                                             StringComparison.OrdinalIgnoreCase));
                    if (pos != -1)
                    {
                        key = @"HKEY_CURRENT_USER" + key.Substring(_currentUsersKey2Length);
                        found = true;
                    }
                }
            }
            return key;
        }

        //public static bool SetNoneValue(UIntPtr hKey, string subKey, string valueName)
        //{
            //uint res;
            //if(!string.IsNullOrEmpty(subKey))
            //{
            //    UIntPtr hSubKey;
            //    res = Declarations.RegOpenKeyEx(hKey,
            //                                    subKey,
            //                                    0,
            //                                    0x0002, out hSubKey);
            //    if (res == 0)
            //    {
            //        res = Declarations.RegSetValueEx(hSubKey,
            //                                         valueName,
            //                                         0,
            //                                         RegistryValueKind.Unknown,
            //                                         IntPtr.Zero,
            //                                         0);
            //        Declarations.CloseHandle(hSubKey);
            //    }
            //}
            //else
            //{
            //    res = Declarations.RegSetValueEx(hKey,
            //                                     valueName,
            //                                     0,
            //                                     RegistryValueKind.Unknown,
            //                                     IntPtr.Zero,
            //                                     0);
            //}
            //return res == 0;
        //}

        public static string GetFullKey(UInt64 hKey, string subKey, uint pid, uint tid)
        {
            string key;
            if (hKey != 0)
            {
                if (hKey < 0x80000000)
                {
                    var keyPath = GetKeyPath(hKey, pid);
                    if (!string.IsNullOrEmpty(keyPath))
                    {
                        subKey = keyPath + "\\" + subKey;
                        hKey = 0;
                    }
                }
            }
            if (hKey == 0)
            {
                key = subKey;
            }
            else if (StandardKeys.TryGetValue(hKey, out key))
            {
                if (subKey != "")
                    key += "\\" + subKey;
                hKey = 0;
                // replace HKEY_USERS\User_Sid by HKEY_CURRENT_USER
                // new opened key
                //if (hSubKey != 0 && retValue == 0)
                //{
                //    string normalizedKey = GetNormalizedKey(key);
                //    //standardKeys[hSubKey] = normalizedKey;
                //    //Console.WriteLine("KEY FOUND: " + hSubKey);
                //}
            }
            else
            {
                if (subKey != "")
                    key += @"HKEY_UNKNOWN\" + subKey;
                else
                    key += @"HKEY_UNKNOWN";

                Error.WriteLine("Registry Key not found 0x" + ((uint) hKey).ToString("X") + " " + subKey);
                hKey = 0;
            }
            var found = false;
            key = ReplaceKnownKeys(key, ref found);
            if (!found && hKey != 0)
            {
                Error.WriteLine("Cannot find root Key: " + key);
            }
            key = FixBackSlashesIn(key);
            return key;
        }

        static public RegistryKey GetKeyFromFullPath(string aKeyPath)
        {
            string mainBranchName;
            var index = aKeyPath.IndexOf('\\');
            if (index != -1)
            {
                mainBranchName = aKeyPath.Substring(0, index);
            }
            else
            {
                mainBranchName = aKeyPath;
            }

            RegistryKey mainBranchKey;

            switch (mainBranchName.ToUpper())
            {
                case "HKEY_CURRENT_USER":
                    mainBranchKey = RegistryWin32.CurrentUser;
                    break;

                case "HKEY_LOCAL_MACHINE":
                    mainBranchKey = RegistryWin32.LocalMachine;
                    break;

                case "HKEY_CLASSES_ROOT":
                    mainBranchKey = RegistryWin32.ClassesRoot;
                    break;

                case "HKEY_USERS":
                    mainBranchKey = RegistryWin32.Users;
                    break;

                case "HKEY_CURRENT_CONFIG":
                    mainBranchKey = RegistryWin32.CurrentConfig;
                    break;

                default:
                    return null;
            }

            try
            {
                var subKey = aKeyPath.Replace(mainBranchName + "\\", "");
                if (aKeyPath == subKey)
                    return mainBranchKey;
                return mainBranchKey.OpenSubKey(aKeyPath.Replace(mainBranchName + "\\", ""),
                                                RegistryKeyPermissionCheck.ReadSubTree);
            }
            catch (SecurityException)
            {
            }

            return null;
        }


        public static RegistryKey GetBaseKey(RegKeyInfo key)
        {
            RegistryKey ret = null;

            switch (key.BasicKeyHandle)
            {
                case HkeyClassesRoot:
                    ret = RegistryWin32.ClassesRoot;
                    break;
                case HkeyCurrentUser:
                    ret = RegistryWin32.CurrentUser;
                    break;
                case HkeyLocalMachine:
                    ret = RegistryWin32.LocalMachine;
                    break;
                case HkeyUsers:
                    ret = RegistryWin32.Users;
                    break;
                case HkeyPerformanceData:
                    ret = RegistryWin32.PerformanceData;
                    break;
                case HkeyCurrentConfig:
                    ret = RegistryWin32.CurrentConfig;
                    break;
                default:
                    Error.WriteLine("Cannot convert key: 0x" + key.BasicKeyHandle.ToString("X"));
                    break;
            }
            return ret;
        }

        public static object GetRegValue(UIntPtr key, string valueName, ref RegistryValueKind type)
        {
            return null;
            //uint lpType = 0;
            //uint lpcbData = 1024;
            //var ageBuffer = new StringBuilder(1024);
            //if(Declarations.RegQueryValueEx(key, valueName, 0, ref lpType, null, ref lpcbData) == 0)
            //{
            //    type = (RegistryValueKind) lpType;
            //    string Age = ageBuffer.ToString();
            //    return Age;
            //}
            //Declarations.RegQueryValueEx(key, valueName)
        }

        public static IntPtr OpenRegKey(IntPtr baseKey, string subKey)
        {
            if (PlatformTools.IsPlatform64Bits())
            {
                return OpenRegKey64(baseKey, subKey);
            }
            IntPtr ret;
            var lResult = Declarations.RegOpenKeyEx(baseKey, subKey, 0, (uint) RegSam.QueryValue, out ret);
            if (0 != lResult)
                return IntPtr.Zero;
            return ret;
        }

        public static IntPtr OpenRegKey64(IntPtr baseKey, string subKey)
        {
            return OpenRegKey64(baseKey, subKey, RegSam.Wow6464Key);
        }

        public static IntPtr OpenRegKey32(IntPtr baseKey, string subKey)
        {
            return OpenRegKey64(baseKey, subKey, RegSam.Wow6432Key);
        }

        public static IntPtr OpenRegKey64(IntPtr baseKey, string subKey, RegSam in32Or64Key)
        {
            try
            {
                IntPtr ret;
                var lResult = Declarations.RegOpenKeyEx(baseKey, subKey, 0,
                                                        (uint) RegSam.QueryValue | (uint) in32Or64Key, out ret);
                if (0 != lResult)
                    return IntPtr.Zero;
                return ret;
                //uint lpType = 0;
                //uint lpcbData = 1024;
                //var ageBuffer = new StringBuilder(1024);
                //Declarations.RegQueryValueEx(hkey, inPropertyName, 0, ref lpType, ageBuffer, ref lpcbData);
                //string Age = ageBuffer.ToString();
                //return Age;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        public static IntPtr GetHandle(RegistryKey registryKey)
        {
            var type = Type.GetType("Microsoft.Win32.RegistryKey");
            Debug.Assert(type != null, "type != null");
            var info = type.GetField("hkey", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(info != null, "info != null");
            var handle = (SafeHandle) info.GetValue(registryKey);
            return handle.DangerousGetHandle();
            //Type type = registryKey.GetType();
            //FieldInfo fieldInfo = type.GetFieldValue("hkey", BindingFlags.Instance |
            //BindingFlags.NonPublic);
            //Debug.Assert(fieldInfo != null, "fieldInfo != null");
            //return (IntPtr)fieldInfo.GetValue(registryKey);
        }

        /// <summary>
        ///	decode a byte array as a string, and strip trailing nulls
        /// </summary>
        internal static string DecodeString(byte[] data)
        {
            var stringRep = Encoding.Unicode.GetString(data);
            var idx = stringRep.IndexOf('\0');
            if (idx != -1)
                stringRep = stringRep.TrimEnd('\0');
            return stringRep;
        }

        public static object GetValue(RegistryKey rkey, string name, object defaultValue, RegistryValueOptions options)
        {
            var handle = GetHandle(rkey);
            return GetValue(handle, name, defaultValue, options);
        }

        /// <summary>
        /// Acctually read a registry value. Requires knowledge of the
        /// value's type and size.
        /// </summary>
        public static object GetValue(IntPtr handle, string name, object defaultValue, RegistryValueOptions options)
        {
            RegistryValueKind type = 0;
            uint size = 0;
            object obj = null;
            var result = Declarations.RegQueryValueEx(handle, name, 0, ref type, IntPtr.Zero, ref size);

            if (result == Win32ResultCode.FileNotFound || result == Win32ResultCode.MarkedForDeletion)
            {
                return defaultValue;
            }

            if (result != Win32ResultCode.MoreData && result != Win32ResultCode.Success)
            {
                Error.WriteLine("Error RegQueryValueEx: " + name + " " + result.ToString(CultureInfo.InvariantCulture));
                return defaultValue;
            }
            if (type == RegistryValueKind.String)
            {
                byte[] data;
                result = GetBinaryValue(handle, name, type, out data, size);
                obj = DecodeString(data);
            }
            else if (type == RegistryValueKind.ExpandString)
            {
                byte[] data;
                result = GetBinaryValue(handle, name, type, out data, size);
                obj = DecodeString(data);
                if ((options & RegistryValueOptions.DoNotExpandEnvironmentNames) == 0)
                    obj = Environment.ExpandEnvironmentVariables((string) obj);
            }
            else if (type == RegistryValueKind.DWord)
            {
                uint data = 0;
                result = Declarations.RegQueryValueEx(handle, name, 0, ref type, ref data, ref size);
                obj = data;
            }
            else if (type == RegistryValueKind.Binary)
            {
                byte[] data;
                result = GetBinaryValue(handle, name, type, out data, size);
                obj = data;
            }
            else if (type == RegistryValueKind.MultiString)
            {
                byte[] data;
                result = GetBinaryValue(handle, name, type, out data, size);

                if (result == Win32ResultCode.Success)
                    obj = DecodeString(data).Split('\0');
            }
            else
            {
                // should never get here
                Error.WriteLine("Error RegQueryValueEx: " + name + " " + result.ToString(CultureInfo.InvariantCulture));
                return defaultValue;
            }

            // check result codes again:
            if (result != Win32ResultCode.Success)
            {
                Error.WriteLine("Error RegQueryValueEx: " + name + " " + result.ToString(CultureInfo.InvariantCulture));
                return defaultValue;
            }


            return obj;
        }

        /// <summary>
        ///	Get a binary value.
        /// </summary>
        private static int GetBinaryValue(IntPtr handle, string name, RegistryValueKind type, out byte[] data, uint size)
        {
            var internalData = new byte[size];
            var result = Declarations.RegQueryValueEx(handle, name, 0, ref type, internalData, ref size);
            data = internalData;
            return result;
        }

        /// <summary>
        /// Get the last part of the path that is identified to the value
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"> </param>
        /// <returns></returns>
        public static string GetRegValueFromPath(string path, out string key)
        {
            var i = path.Length - 1;
            bool openSingleQuotes = false, openDoubleQuotes = false;
            var ret = "";
            key = "";

            while (i > 0)
            {
                if (path[i] == '\'')
                    openSingleQuotes = !openSingleQuotes;
                else if (path[i] == '\"')
                    openDoubleQuotes = !openDoubleQuotes;
                else if (path[i] == '\\' && !openDoubleQuotes && !openSingleQuotes)
                {
                    ret = path.Substring(i + 1, path.Length - i - 1);
                    key = path.Substring(0, i);
                    break;
                }
                i--;
            }
            return ret;
        }

        public static UInt64 ToUInt64(IntPtr hkey)
        {
            if (IntPtr.Size == 4)
            {
                return (uint) hkey;
            }
            return (UInt64) hkey;
        }

        public static UInt64 ToUInt64(UIntPtr hkey)
        {
            if (UIntPtr.Size == 4)
            {
                return (uint)hkey;
            }
            return (UInt64)hkey;
        }

        public static string GetBasicKeyHandleString(UInt64 basicKeyHandle)
        {
            string ret;
            return !StandardKeys.TryGetValue(basicKeyHandle, out ret) ? string.Empty : ret;
        }

        public static UInt64 GetBasicHandleFromPath(string path)
        {
            var lower = path.ToLower();
            foreach (var keyPair in StandardKeys)
                if (lower == keyPair.Value || lower.StartsWith(keyPair.Value.ToLower() + "\\"))
                    return keyPair.Key;
            return 0;

        }
        public static bool ExportToReg(List<RegKeyInfo> keyInfos, StreamWriter file)
        {
            file.Write("Windows Registry Editor Version 5.00\r\n\r\n");
            foreach (var keyInfo in keyInfos)
            {
                if (keyInfo.Success)
                {
                    file.Write("[" + keyInfo.Path + "]" + "\r\n");
                    foreach (var value in keyInfo.ValuesByName)
                    {
                        if (value.Value.Success)
                        {
                            string valueString;
                            // default value
                            if (string.IsNullOrEmpty(value.Value.Name))
                            {
                                valueString = "@=";
                            }
                            else
                            {
                                valueString = "\"" + value.Value.Name + "\"=";
                            }

                            valueString += ToRegistryFormat(value.Value);
                            file.Write(valueString + "\r\n");
                        }
                    }
                    file.Write("\r\n");
                }
            }
            file.Flush();
            return true;
        }

        static public bool PathIsUnder(string aRegistryPath, string aParentRegistryPath)
        {
            return new Regex(@"^" + Regex.Escape(aParentRegistryPath) + @"\\", RegexOptions.IgnoreCase).
                IsMatch(aRegistryPath);
        }

        //private static IntPtr GetRegistryKeyHandle(RegistryKey pRegisteryKey)
        //{
        //    Type type = Type.GetType("Microsoft.Win32.RegistryKey");
        //    Debug.Assert(type != null, "type != null");
        //    FieldInfo info = type.GetField("hkey", BindingFlags.NonPublic | BindingFlags.Instance);

        //    Debug.Assert(info != null, "info != null");
        //    var handle = (SafeHandle)info.GetValue(pRegisteryKey);
        //    //var realHandle = handle.DangerousGetHandle();

        //    return handle.DangerousGetHandle();
        //}
        public static RegistryKey PointerToRegistryKey(UIntPtr hKey, bool pWritable,
            bool pOwnsHandle)
        {
            var intPtr = unchecked((IntPtr)(long)(ulong)hKey);
            return PointerToRegistryKey(intPtr, pWritable, pOwnsHandle);
        }
        public static RegistryKey PointerToRegistryKey(IntPtr hKey, bool pWritable,
            bool pOwnsHandle)
        {
            // Create a SafeHandles.SafeRegistryHandle from this pointer - this is a private class
            const BindingFlags privateConstructors = BindingFlags.Instance | BindingFlags.NonPublic;
            var safeRegistryHandleType = typeof (
                SafeHandleZeroOrMinusOneIsInvalid).Assembly.GetType(
                    "Microsoft.Win32.SafeHandles.SafeRegistryHandle");

            var safeRegistryHandleConstructorTypes = new[]
                                                         {
                                                             typeof (IntPtr),
                                                             typeof (Boolean)
                                                         };
            var safeRegistryHandleConstructor =
                safeRegistryHandleType.GetConstructor(privateConstructors,
                                                      null, safeRegistryHandleConstructorTypes, null);
            var safeHandle = safeRegistryHandleConstructor.Invoke(new Object[]
                                                                      {
                                                                          hKey,
                                                                          pOwnsHandle
                                                                      });

            // Create a new Registry key using the private constructor using the
            // safeHandle - this should then behave like 
            // a .NET natively opened handle and disposed of correctly
            var registryKeyType = typeof (RegistryKey);
            var registryKeyConstructorTypes = new[]
                                                  {
                                                      safeRegistryHandleType,
                                                      typeof (Boolean)
                                                  };
            var registryKeyConstructor =
                registryKeyType.GetConstructor(privateConstructors, null,
                                               registryKeyConstructorTypes, null);
            var result = (RegistryKey) registryKeyConstructor.Invoke(new[]
                                                                         {
                                                                             safeHandle, pWritable
                                                                         });
            return result;
        }


        //public void SetValue(RegistryKey rkey, string name, object value)
        //{
        //    Type type = value.GetType();
        //    int result;
        //    IntPtr handle = GetHandle(rkey);

        //    if (type == typeof(int))
        //    {
        //        int rawValue = (int)value;
        //        result = RegSetValueEx(handle, name, IntPtr.Zero, RegistryValueKind.DWord, ref rawValue, Int32ByteSize);
        //    }
        //    else if (type == typeof(byte[]))
        //    {
        //        byte[] rawValue = (byte[])value;
        //        result = RegSetValueEx(handle, name, IntPtr.Zero, RegistryValueKind.Binary, rawValue, rawValue.Length);
        //    }
        //    else if (type == typeof(string[]))
        //    {
        //        string[] vals = (string[])value;
        //        StringBuilder fullStringValue = new StringBuilder();
        //        foreach (string v in vals)
        //        {
        //            fullStringValue.Append(v);
        //            fullStringValue.Append('\0');
        //        }
        //        fullStringValue.Append('\0');

        //        byte[] rawValue = Encoding.Unicode.GetBytes(fullStringValue.ToString());

        //        result = Declarations.RegSetValueEx(handle, name, IntPtr.Zero, RegistryValueKind.MultiString, rawValue, rawValue.Length);
        //    }
        //    else if (type.IsArray)
        //    {
        //        throw new ArgumentException("Only string and byte arrays can written as registry values");
        //    }
        //    else
        //    {
        //        string rawValue = String.Format("{0}{1}", value, '\0');
        //        result = RegSetValueEx(handle, name, IntPtr.Zero, RegistryValueKind.String, rawValue,
        //                    rawValue.Length * NativeBytesPerCharacter);
        //    }

        //    if (result == Win32ResultCode.MarkedForDeletion)
        //        throw RegistryKey.CreateMarkedForDeletionException();

        //    // handle the result codes
        //    if (result != Win32ResultCode.Success)
        //    {
        //        GenerateException(result);
        //    }
        //}

        public static RegistryKey EnsureKeyExists(RegistryKey basicKey, string path)
        {
            var splitPath = path.SplitAsPath().Skip(1).ToList();
            var key = basicKey;
            foreach (var subkeyName in splitPath)
            {
                var subkey = key.OpenSubKey(subkeyName);
                if (subkey != null)
                {
                    key = subkey;
                    continue;
                }
                key = key.CreateSubKey(subkeyName);
                if (key == null)
                    break;
            }
            return key;
        }

        public static void CopyKeyContents(RegistryKey src, RegistryKey dst)
        {
            foreach (var valueName in src.GetValueNames())
            {
                var value = src.GetValue(valueName);
                var kind = src.GetValueKind(valueName);
                dst.SetValue(valueName, value, kind);
            }
            foreach (var subKeyName in src.GetSubKeyNames())
            {
                var src2 = src.OpenSubKey(subKeyName);
                if (src2 == null)
                    continue;
                var dst2 = dst.OpenOrCreateSubKey(subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                CopyKeyContents(src2, dst2);
            }
        }

        public static string ToSpyStudioDisplayFormat(object data, RegistryValueKind kind)
        {
            var ret = new StringBuilder();
            switch (kind)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return (string)data;
                case RegistryValueKind.MultiString:
                    var array = (string[]) data;
                    foreach (var s in array)
                    {
                        ret.Append(s);
                        ret.Append('\0');
                    }
                    ret.Append('\0');
                    return ret.ToString();
                case RegistryValueKind.DWord:
                    var dword = (int) data;
                    ret.Append("0x");
                    ret.Append(dword.ToString("X8"));
                    ret.Append(" (");
                    ret.Append(dword);
                    ret.Append(')');
                    return ret.ToString();
                case RegistryValueKind.QWord:
                    var qword = (long) data;
                    ret.Append("0x");
                    ret.Append(qword.ToString("X8"));
                    ret.Append(" (");
                    ret.Append(qword);
                    ret.Append(')');
                    return ret.ToString();
                case RegistryValueKind.Binary:
                    var buffer = (byte[])data;
                    bool first = true;
                    foreach (var b in buffer)
                    {
                        if (first)
                            first = false;
                        else
                            ret.Append(" ");
                        ret.Append(b.ToString("X2"));
                    }
                    return ret.ToString();
                case RegistryValueKind.Unknown:
                    return string.Empty;
            }
            return string.Empty;
        }

        public static string NormalizeWowSubPaths(string path)
        {
            path = path.AsNormalizedPath();
            var normalizedPath = path.ToLower() + '\\';

            var softwareClassesWow = @"hkey_local_machine\software\classes\wow6432node\";
            var softwareWowClasses = @"hkey_local_machine\software\wow6432node\classes\";

            if (normalizedPath.StartsWith(softwareClassesWow))
            {
                path = softwareWowClasses + path.Substring(softwareClassesWow.Length - 1);
                path = path.AsNormalizedPath();
                normalizedPath = path.ToLower() + '\\';
            }

            var subkeys = new[]
                              {
                                  @"appid\",
                                  @"protocols\",
                                  @"typelib\",
                              };

            foreach (var subkey in subkeys)
            {
                var testpathSrc = softwareWowClasses + @"wow6432node\" + subkey;
                if (!normalizedPath.StartsWith(testpathSrc))
                    continue;
                var testpathDst = softwareWowClasses + subkey;
                path = testpathDst + path.Substring(testpathSrc.Length - 1);
                path = path.AsNormalizedPath();
                break;
            }

            return path;
        }

        //{f8b8412b-dea3-4130-b36c-5e8be73106ac}
        private const string GuidRegexPattern = @"\{?[0-9a-f]{8}(?:\-[0-9a-f]{4}){3}\-[0-9a-f]{12}\}?";
        public static readonly Regex GuidRegex = new Regex("^" + GuidRegexPattern + "$", RegexOptions.IgnoreCase);

        public static HashSet<Guid> GetPreviewHandlers()
        {
            var ret = new HashSet<Guid>();
            var handlersKey = RegistryTools.GetKeyFromFullPath(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PreviewHandlers"
            );
            if (handlersKey == null)
                return ret;

            var guids = handlersKey.GetValueNames()
                .Where(x => GuidRegex.IsMatch(x))
                .Select(x => new Guid(x));
            ret.AddRange(guids);
            return ret;
        }

        public static string GetClassesPath()
        {
            return GetClassesPath(0);
        }

        public static string GetClassesPath(int bitness)
        {
            if (bitness == 0)
                bitness = IntPtr.Size*8;
            var ret = @"HKEY_LOCAL_MACHINE\Software";
            switch (bitness)
            {
                case 32:
                    ret += (IntPtr.Size * 8 == bitness ? "" : @"\Wow6432Node");
                    break;
                case 64:
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }
            return ret + @"\Classes";
        }

        public static RegistryKey OpenClassesKey()
        {
            return GetKeyFromFullPath(GetClassesPath());
        }

        public static RegistryKey OpenClassesKey(int bitness)
        {
            return GetKeyFromFullPath(GetClassesPath(bitness));
        }

        public static IEnumerable<RegistryKey> OpenVerbSubKeys(RegistryKey key)
        {
            var verbKey = key.OpenSubKey("verb");
            if (verbKey == null)
                return Enumerable.Empty<RegistryKey>();

            return verbKey.GetSubKeyNames()
                .Where(x => x.All(char.IsDigit))
                .Select(subkeyName => verbKey.OpenSubKey(subkeyName))
                .Where(subkey => subkey != null);
        }


        private const string ClassesString = @"^(?:HKEY_LOCAL_MACHINE\\SOFTWARE(?:\\Wow6432Node)?\\Classes|HKEY_CLASSES_ROOT)";

        private static readonly string[] DefaultIconRegexTuples =
        {
            @"^.*\\DefaultIcon\\$",
            ClassesString + @"\\[^\\]+\\shell\\[^\\]+\\Icon$",
            ClassesString + @"\\CLSID\\" + GuidRegexPattern + @"\\ToolboxBitmap32\\$",
            @"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Internet Explorer\\Extensions\\" + GuidRegexPattern + @"\\(?:Hot)?Icon",
        };


        private static readonly Regex[] DefaultIconRegexes =
            DefaultIconRegexTuples.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray();

        private static readonly Regex DefaultIconSplitter = new Regex(@"^(.*),(-?[0-9]+)$");

        private static readonly string[] CommandRegexStrings =
        {
            @".*\\(?:shell|search)\\[^\\]+\\command\\$",
            ClassesString + @"\\[^\\]+\\HTML Handler\\$",
            ClassesString + @"\\CLSID\\" + GuidRegexPattern + @"\\LocalServer(?:32)?\\$",
            @"HKEY_LOCAL_MACHINE\\SOFTWARE\\Clients\\.*\\InstallInfo\\(?:Reinstall|HideIcons|ShowIcons)Command",
            @"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\.*\\ExecString",
        };


        private static readonly Regex[] CommandRegexes =
            CommandRegexStrings.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray();

        private static bool ValueIsIcon(string keyPath, string valueName)
        {
            var path = keyPath + "\\" + valueName;
            return DefaultIconRegexes.Any(x => x.IsMatch(path));
        }

        private static bool ValueIsCommand(string keyPath, string valueName)
        {
            var path = keyPath + "\\" + valueName;
            return CommandRegexes.Any(x => x.IsMatch(path));
        }

        public static string NormalizePathsInValue(string value, string keyPath, string valueName, PathNormalizer normalizer)
        {
            Func<string, string> f = x => normalizer.Normalize(x, OperationMode.LengthenPath);

            if (value.StartsWith("@"))
                return "@" + f(value.Substring(1));

            if (ValueIsIcon(keyPath, valueName))
            {
                var match = DefaultIconSplitter.Match(value);
                if (match.Success)
                {
                    var path = match.Groups[1].ToString();
                    bool quote = !(path.StartsWith("\"") && path.EndsWith("\""));
                    path = f(path);
                    if (quote)
                        path = "\"" + path + "\"";
                    return path + "," + match.Groups[2].ToString();
                }
                return f(value);
            }

            if (ValueIsCommand(keyPath, valueName))
            {
                char terminator;
                int skipSize;
                string additional;
                if (value.StartsWith("\""))
                {
                    terminator = '"';
                    skipSize = 1;
                    additional = "";
                }
                else
                {
                    terminator = ' ';
                    skipSize = 0;
                    additional = "\"";
                }
                var index = value.IndexOf(terminator, skipSize);
                if (index < 0 || index >= value.Length)
                    return f(value);
                var path = value.Substring(skipSize, index - skipSize);
                path = f(path);
                return "\"" + path + additional + value.Substring(index);
            }

            return f(value);
        }
    }

    public static class RegistryKeyExtensions
    {
        public static RegistryKey OpenOrCreateSubKey(this RegistryKey parent, string subkey)
        {
            return OpenOrCreateSubKey(parent, subkey, RegistryKeyPermissionCheck.Default);
        }

        public static RegistryKey OpenOrCreateSubKey(this RegistryKey parent, string subkey, RegistryKeyPermissionCheck perm)
        {
            return parent.OpenSubKey(subkey, perm) ?? parent.CreateSubKey(subkey, perm);
        }

        public static object GetDefaultValueOfSubKey(this RegistryKey key, string subKeyName)
        {
            var subkey = key.OpenSubKey(subKeyName);
            if (subkey == null)
                return null;
            using (var _ = subkey)
            {
                return subkey.GetValue("");
            }
        }

        public static int GetDefaultIntValueOfSubKey(this RegistryKey key, string subKeyName)
        {
            var subkey = key.OpenSubKey(subKeyName);
            if (subkey == null)
                throw new RegKeyNotFoundException();
            using (var _ = subkey)
            {
                return subkey.GetIntValue("");
            }
        }

        public static string GetDefaultStringValueOfSubKey(this RegistryKey key, string subKeyName)
        {
            var subkey = key.OpenSubKey(subKeyName);
            if (subkey == null)
                return null;
            using (var _ = subkey)
            {
                return subkey.GetStringValue("");
            }
        }

        public static string GetDefaultStringValueOfSubKeyForAppVManifest(this RegistryKey key, string subKeyName)
        {
            return AppvPathNormalizer.GetInstanceManifest().Normalize(GetDefaultStringValueOfSubKey(key, subKeyName));
        }

        public static int GetIntValue(this RegistryKey key, string name)
        {
            int ret;
            if (!key.GetIntValue(name, out ret))
                throw new Exception("Registry value not found.");
            return ret;
        }

        public static bool GetIntValue(this RegistryKey key, string name, out int dst)
        {
            dst = 0;
            var val = key.GetValue(name);
            if (val == null)
                return false;
            string s;
            switch (key.GetValueKind(name))
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    s = (string)val;
                    break;
                case RegistryValueKind.DWord:
                    dst = (int)val;
                    return true;
                case RegistryValueKind.MultiString:
                    s = ((string[])val)[0];
                    break;
                case RegistryValueKind.QWord:
                    try
                    {
                        dst = (int)(long)val;
                    }
                    catch (OverflowException)
                    {
                        dst = int.MaxValue;
                    }
                    return true;
                default:
                    dst = 0;
                    return true;
            }
            try
            {
                dst = Convert.ToInt32(s);
            }
            catch
            {
                dst = 0;
            }
            return true;
        }

        public static string GetStringValue(this RegistryKey key, string name)
        {
            var val = key.GetValue(name);
            if (val == null)
                return null;
            switch (key.GetValueKind(name))
            {
                case RegistryValueKind.String:
                    return (string)val;
                case RegistryValueKind.ExpandString:
                    return Environment.ExpandEnvironmentVariables((string)val);
                case RegistryValueKind.DWord:
                    return ((int)val).ToString(CultureInfo.InvariantCulture);
                case RegistryValueKind.MultiString:
                    {
                        var array = (string[])val;
                        if (array.Length == 0)
                            return null;
                        return array[0];
                    }
                case RegistryValueKind.QWord:
                    return ((long)val).ToString(CultureInfo.InvariantCulture);
                default:
                    return null;
            }
        }

        public static string GetStringValueForAppVManifest(this RegistryKey key, string name)
        {
            return AppvPathNormalizer.GetInstanceManifest().Normalize(GetStringValue(key, name));
        }

        public static bool ValueExists(this RegistryKey key, string name)
        {
            return key.GetValue(name) != null;
        }

        public static string GetKeyName(this RegistryKey key)
        {
            var name = key.Name;
            int i = name.LastIndexOf('\\');
            if (i < 0 || i > name.Length)
                return name;
            return name.Substring(i + 1);
        }

        public static RegistryKey OpenSubKeyForRead(this RegistryKey key, string subkeyName)
        {
            RegistryKey ret = null;
            /*
            for (int i = 0; ; i++)
            {
            */
                ret = key.OpenSubKey(subkeyName, RegistryKeyPermissionCheck.ReadSubTree);
                /*
                if (ret != null)
                    break;
                if (i != 0)
                {
                    var exists = key.GetSubKeyNames().Contains(subkeyName, StringComparer.InvariantCultureIgnoreCase);
                    break;
                }
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }
            */
            return ret;
        }


        public static RegistryKey OpenParentKey(this RegistryKey key)
        {
            var index = key.Name.LastIndexOf('\\');
            if (index == -1)
                return null;

            var parentName = key.Name.Substring(0, index);
            return RegistryTools.GetKeyFromFullPath(parentName);
        }

    }
}
