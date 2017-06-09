using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Aga.Controls;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using SpyStudio.Export.AppV.Manifest;
using SpyStudio.Extensions;
using SpyStudio.Hooks;
using SpyStudio.Loader;
using SpyStudio.Properties;

namespace SpyStudio.Tools
{
    public static class SpyStudioConstants
    {
        public static string TempDirectory
        {
            get
            {
                var tempDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" +
                       AssemblyTools.AssemblyCompany + "\\" +
                       AssemblyTools.AssemblyProduct;

                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                return tempDir;
            }
        }

        public static string TemplatesTempDirectory
        {
            get
            {
                var templatesTempDir = TempDirectory + "\\Templates";

                if (!Directory.Exists(templatesTempDir))
                    Directory.CreateDirectory(templatesTempDir);

                return templatesTempDir;
            }
        }

        public static string CatalogsTempDirectory
        {
            get
            {
                var catalogsTempDir = TempDirectory + "\\Catalogs";

                if (!Directory.Exists(catalogsTempDir))
                    Directory.CreateDirectory(catalogsTempDir);

                return catalogsTempDir;
            }
        }

        public static string GetZipPassword()
        {
            var softwareKey = Settings.Default.FilterKey;
            if (!string.IsNullOrEmpty(softwareKey))
            {
                var encKey = Encoding.ASCII.GetBytes("ieHha93LUejadOkaJha834%!");
                var iv = Encoding.ASCII.GetBytes("uwrD3221");
                //byte[] dataKey = Encoding.ASCII.GetBytes(softwareKey);
                var dataKey = Convert.FromBase64String(softwareKey);
                //byte[] dataEmail = Encoding.ASCII.GetBytes(userEmail);
                var tdes = TripleDES.Create();
                tdes.IV = iv;
                tdes.Key = encKey;
                tdes.Mode = CipherMode.CBC;
                tdes.Padding = PaddingMode.Zeros;
                //ICryptoTransform ict = tdes.CreateEncryptor();
                //enc = ict.TransformFinalBlock(data, 0, data.Length);
                var ict = tdes.CreateDecryptor();
                var dec = ict.TransformFinalBlock(dataKey, 0, dataKey.Length);
                softwareKey = Encoding.ASCII.GetString(dec);
            }

            if (string.IsNullOrEmpty(softwareKey))
                return "";

            softwareKey = NormalizeKey(softwareKey);



            return CreateSHAHash(softwareKey).Substring(0, 32);

            //var shaAlgorithm = SHA512.Create();
            //var zipPassInBytes = shaAlgorithm.ComputeHash(softwareKey.ToByteArray(), 4, softwareKey.Length - 4);

            //var sb = new StringBuilder();
            //foreach (var b in zipPassInBytes)
            //    sb.Append(b.ToString("X2"));

            //return sb.ToString().Substring(0, 32);
        }

        public static string CreateSHAHash(string Phrase)
        {
            var HashTool = new SHA512Managed();
            var PhraseAsByte = System.Text.Encoding.UTF8.GetBytes(string.Concat(Phrase));
            var EncryptedBytes = HashTool.ComputeHash(PhraseAsByte);
            HashTool.Clear();
            return Convert.ToBase64String(EncryptedBytes);
        }

        private static string NormalizeKey(string aSpyStudioKey)
        {
            if (aSpyStudioKey.StartsWith("-----BEGIN DEVIARE LICENSE-----"))
                aSpyStudioKey = aSpyStudioKey.Substring("-----BEGIN DEVIARE LICENSE-----".Length);

            if (aSpyStudioKey.EndsWith("-----END DEVIARE LICENSE-----"))
                aSpyStudioKey = aSpyStudioKey.Substring(0, aSpyStudioKey.Length - "-----END DEVIARE LICENSE-----".Length - 1);

            if (aSpyStudioKey.EndsWith("-----END DEVIARE LICENSE-----\0"))
                aSpyStudioKey = aSpyStudioKey.Substring(0, aSpyStudioKey.Length - "-----END DEVIARE LICENSE-----\0".Length - 1);

            return aSpyStudioKey.Trim();
        }
    }
    

    #region Win32Declarations

    public class Declarations
    {
        #region Win32
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("kernel32")]
        public extern static uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public extern static IntPtr LoadLibrary(string libraryName);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public extern static bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Ansi)]
        public extern static IntPtr GetProcAddress(uint hwnd, string procedureName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcess(string lpApplicationName,
           string lpCommandLine, IntPtr lpProcessAttributes,
           IntPtr lpThreadAttributes, bool bInheritHandles,
           uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            UInt32 dwProcessId
            );

        //[DllImport("kernel32.dll")]
        //public static extern UInt32 GetThreadId(
        //    IntPtr hThread
        //    );

        [DllImport("kernel32.dll")]
        public static extern Int32 ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [In, Out] byte[] buffer,
            UInt32 dwSize, 
            out UInt32 lpNumberOfBytesRead
            );

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        public static extern IntPtr CloseHandle(UIntPtr handle);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        public static extern IntPtr CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource,
                                                 UInt64 dwMessageId, uint dwLanguageId, [Out] StringBuilder
                                                                                            lpBuffer,
                                                 uint nSize, string[] Arguments);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCreateKeyEx(
                    UIntPtr hKey,
                    string lpSubKey,
                    int Reserved,
                    string lpClass,
                    uint dwOptions,
                    uint samDesired,
                    IntPtr lpSecurityAttributes,
                    out UIntPtr phkResult,
                    out UIntPtr lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern uint RegOpenKeyEx(
          IntPtr hKey,
          string subKey,
          uint ulOptions,
          uint samDesired,
          out IntPtr hkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern uint RegOpenKeyEx(
          UIntPtr hKey,
          string subKey,
          uint ulOptions,
          uint samDesired,
          out UIntPtr hkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegEnumValue(
              UIntPtr hKey,
              uint dwIndex,
              StringBuilder lpValueName,
              ref uint lpcValueName,
              UIntPtr lpReserved,
              out uint lpType,
              byte[] lpData,
              ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegEnumValue(
              UIntPtr hKey,
              uint dwIndex,
              StringBuilder lpValueName,
              ref uint lpcValueName,
              UIntPtr lpReserved,
              out uint lpType,
              StringBuilder lpData,
              ref uint lpcbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegSetValueEx(
            UIntPtr hKey,
            [MarshalAs(UnmanagedType.LPStr)] string lpValueName,
            int Reserved,
            Microsoft.Win32.RegistryValueKind dwType,
            [MarshalAs(UnmanagedType.LPStr)] string lpData,
            int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueEx")]
        public static extern int RegQueryValueEx(
            IntPtr hKey, string lpValueName,
            int lpReserved,
            ref uint lpType,
            StringBuilder lpData,
            ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueEx")]
        public static extern int RegQueryValueEx(
            IntPtr hKey, string lpValueName,
            int lpReserved,
            ref RegistryValueKind lpType,
            IntPtr lpData,
            ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueEx")]
        public static extern int RegQueryValueEx(IntPtr keyBase,
                string valueName, int reserved, ref RegistryValueKind type,
                byte[] data, ref uint dataSize);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueEx")]
        public static extern int RegQueryValueEx(IntPtr keyBase,
                string valueName, int reserved, ref RegistryValueKind type,
                ref uint data, ref uint dataSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint RegCloseKey(UIntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegEnumKeyEx")]
        public extern static uint RegEnumKeyEx(
            UIntPtr hKey,
            uint dwIndex,
            StringBuilder lpName,
            out uint lpcchName,
            uint lpReserved,
            StringBuilder lpClass,
            out uint lpcchClass,
            out FILETIME lastWriteTime);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint RegCloseKey(IntPtr hKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetLongPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder longPath,
                 int longPathLength
                 );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetShortPathName(
           [MarshalAs(UnmanagedType.LPTStr)] string lpszLongPath,
           [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszShortPath,
           uint cchBuffer
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(UIntPtr hWnd, StringBuilder lpClassName,
                int nMaxCount);
        
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [return: MarshalAs(UnmanagedType.I4)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
           IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
           uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint NtQueryKey(IntPtr keyHandle,
           int keyInformationClass, [In, Out] short[] buffer/*ref KEY_NAME_INFORMATION keyInfo*/, uint length,
            ref UInt32 lpNumberOfBytesRead);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct KeyNameInformation {
            public ulong NameLength;
            public char[] Name;
        };


        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint NtQueryInformationThread(IntPtr hThread, int threadInformationClass,
                                                           ref THREAD_BASIC_INFORMATION tbi, int sizeTbi, IntPtr retWritten);

        [DllImport("wininet.dll", SetLastError = true)]
        static extern bool HttpQueryInfo(IntPtr hInternet, int dwInfoLevel, [Out] StringBuilder lpBuffer/*ref long lpBuffer*/, 
            ref long lpdwBufferLength, ref long lpdwIndex);

        [DllImport("shell32.dll")]
        public static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, StringBuilder lpIconPath, ref ushort lpiIcon);

        [DllImport("shell32.dll")]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
                    //[In, Out] byte[] buffer,

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string szFileName, int nIconIndex,
           IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            FileMapAccess dwDesiredAccess,
            UInt32 dwFileOffsetHigh,
            UInt32 dwFileOffsetLow,
            UIntPtr dwNumberOfBytesToMap);

        [Flags]
        public enum FileMapAccess : uint
        {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            FileMapExecute = 0x0020,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetLastError();

        //public static extern uint NtQueryKey)(
        //        HANDLE  KeyHandle,
        //        int KeyInformationClass,
        //        PVOID  KeyInformation,
        //        ULONG  Length,
        //        PULONG  ResultLength);


        public static uint ErrorFileNotFound = 2;
        public static int MaxPath = 260;

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public UIntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct KEY_NAME_INFORMATION
        {
            public uint NameLength;
            public StringBuilder Name;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct CLIENT_ID
        {
            public uint UniqueProcess;
            public uint UniqueThread;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct THREAD_BASIC_INFORMATION
        {
            public uint ExitStatus;
            public IntPtr TebBaseAddress;
            public CLIENT_ID ClientId;
            public IntPtr AffinityMask;
            public IntPtr Priority;
            public IntPtr BasePriority;
        }

        [DllImport("kernel32.dll")]
        public static extern uint QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, uint ucchMax);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("shell32.dll")]
        public static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken,
           uint dwFlags, [Out] StringBuilder pszPath);

        public enum SpecialFolderCSIDL : int
        {
            CSIDL_DESKTOP = 0x0000,                     // <desktop>
            CSIDL_INTERNET = 0x0001,                    // Internet Explorer (icon on desktop)
            CSIDL_PROGRAMS = 0x0002,                    // Start Menu\Programs
            CSIDL_CONTROLS = 0x0003,                    // My Computer\Control Panel
            CSIDL_PRINTERS = 0x0004,                    // My Computer\Printers
            CSIDL_PERSONAL = 0x0005,                    // My Documents
            CSIDL_FAVORITES = 0x0006,                   // <user name>\Favorites
            CSIDL_STARTUP = 0x0007,                     // Start Menu\Programs\Startup
            CSIDL_RECENT = 0x0008,                      // <user name>\Recent
            CSIDL_SENDTO = 0x0009,                      // <user name>\SendTo
            CSIDL_BITBUCKET = 0x000a,                   // <desktop>\Recycle Bin
            CSIDL_STARTMENU = 0x000b,                   // <user name>\Start Menu
            CSIDL_DESKTOPDIRECTORY = 0x0010,            // <user name>\Desktop
            CSIDL_DRIVES = 0x0011,                      // My Computer
            CSIDL_NETWORK = 0x0012,                     // Network Neighborhood
            CSIDL_NETHOOD = 0x0013,                     // <user name>\nethood
            CSIDL_FONTS = 0x0014,                       // windows\fonts
            CSIDL_TEMPLATES = 0x0015,
            CSIDL_COMMON_STARTMENU = 0x0016,            // All Users\Start Menu
            CSIDL_COMMON_PROGRAMS = 0x0017,             // All Users\Programs
            CSIDL_COMMON_STARTUP = 0x0018,              // All Users\Startup
            CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019,     // All Users\Desktop
            CSIDL_APPDATA = 0x001a,                     // <user name>\Application Data
            CSIDL_PRINTHOOD = 0x001b,                   // <user name>\PrintHood
            CSIDL_LOCAL_APPDATA = 0x001c,               // <user name>\Local Settings\Application Data (non roaming)
            CSIDL_ALTSTARTUP = 0x001d,                  // non localized startup
            CSIDL_COMMON_ALTSTARTUP = 0x001e,           // non localized common startup
            CSIDL_COMMON_FAVORITES = 0x001f,
            CSIDL_INTERNET_CACHE = 0x0020,
            CSIDL_COOKIES = 0x0021,
            CSIDL_HISTORY = 0x0022,
            CSIDL_COMMON_APPDATA = 0x0023,              // All Users\Application Data
            CSIDL_WINDOWS = 0x0024,                     // GetWindowsDirectory()
            CSIDL_SYSTEM = 0x0025,                      // GetSystemDirectory()
            CSIDL_PROGRAM_FILES = 0x0026,               // C:\Program Files
            CSIDL_MYPICTURES = 0x0027,                  // C:\Program Files\My Pictures
            CSIDL_PROFILE = 0x0028,                     // USERPROFILE
            CSIDL_SYSTEMX86 = 0x0029,                   // x86 system directory on RISC
            CSIDL_PROGRAM_FILESX86 = 0x002a,            // x86 C:\Program Files on RISC
            CSIDL_PROGRAM_FILES_COMMON = 0x002b,        // C:\Program Files\Common
            CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c,     // x86 Program Files\Common on RISC
            CSIDL_COMMON_TEMPLATES = 0x002d,            // All Users\Templates
            CSIDL_COMMON_DOCUMENTS = 0x002e,            // All Users\Documents
            CSIDL_COMMON_ADMINTOOLS = 0x002f,           // All Users\Start Menu\Programs\Administrative Tools
            CSIDL_ADMINTOOLS = 0x0030,                  // <user name>\Start Menu\Programs\Administrative Tools
            CSIDL_CONNECTIONS = 0x0031,                 // Network and Dial-up Connections
            CSIDL_CDBURN_AREA = 0x003B,                 // Data for burning with interface ICDBurn
        };

        public enum SHGFP_TYPE : int
        {
            SHGFP_TYPE_CURRENT = 0,   // current value for user, verify it exists
            SHGFP_TYPE_DEFAULT = 1,   // default value, may not exist
        }

        [DllImport("shell32.dll")]
        public static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        [DllImport("ole32.dll")]
        public static extern void CoTaskMemFree(IntPtr pv);

        public const int AwHide = 0x10000;
        public const int AwActivate = 0x20000;
        public const int AwHorPositive = 0x1;
        public const int AwHorNegative = 0x2;
        public const int AwVerPositive = 0x00000004;
        public const int AwVerNegative = 0x8;
        public const int AwSlide = 0x40000;
        public const int AwBlend = 0x80000;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int AnimateWindow
            (IntPtr hWnd, int dwTime, int dwFlags);

        // C#
        public static class KnownFolder
        {
            public static readonly Guid AccountPictures = new Guid("008ca0b1-55b4-4c56-b8a8-4de4b299d3be");
            public static readonly Guid AddNewPrograms = new Guid("de61d971-5ebc-4f02-a3a9-6c82895e5c04");
            public static readonly Guid AdminTools = new Guid("724EF170-A42D-4FEF-9F26-B60E846FBA4F");
            public static readonly Guid ApplicationShortcuts = new Guid("A3918781-E5F2-4890-B3D9-A7E54332328C");
            public static readonly Guid AppUpdates = new Guid("a305ce99-f527-492b-8b1a-7e76fa98d6e4");
            public static readonly Guid CDBurning = new Guid("9E52AB10-F80D-49DF-ACB8-4330F5687855");
            public static readonly Guid ChangeRemovePrograms = new Guid("df7266ac-9274-4867-8d55-3bd661de872d");
            public static readonly Guid CommonAdminTools = new Guid("D0384E7D-BAC3-4797-8F14-CBA229B392B5");
            public static readonly Guid CommonOEMLinks = new Guid("C1BAE2D0-10DF-4334-BEDD-7AA20B227A9D");
            public static readonly Guid CommonPrograms = new Guid("0139D44E-6AFE-49F2-8690-3DAFCAE6FFB8");
            public static readonly Guid CommonStartMenu = new Guid("A4115719-D62E-491D-AA7C-E74B8BE3B067");
            public static readonly Guid CommonStartup = new Guid("82A5EA35-D9CD-47C5-9629-E15D2F714E6E");
            public static readonly Guid CommonTemplates = new Guid("B94237E7-57AC-4347-9151-B08C6C32D1F7");
            public static readonly Guid ComputerFolder = new Guid("0AC0837C-BBF8-452A-850D-79D08E667CA7");
            public static readonly Guid ConflictFolder = new Guid("4bfefb45-347d-4006-a5be-ac0cb0567192");
            public static readonly Guid ConnectionsFolder = new Guid("6F0CD92B-2E97-45D1-88FF-B0D186B8DEDD");
            public static readonly Guid Contacts = new Guid("56784854-C6CB-462b-8169-88E350ACB882");
            public static readonly Guid ControlPanelFolder = new Guid("82A74AEB-AEB4-465C-A014-D097EE346D63");
            public static readonly Guid Cookies = new Guid("2B0F765D-C0E9-4171-908E-08A611B84FF6");
            public static readonly Guid Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
            public static readonly Guid DeviceMetadataStore = new Guid("5CE4A5E9-E4EB-479D-B89F-130C02886155");
            public static readonly Guid Documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
            public static readonly Guid DocumentsLibrary = new Guid("7B0DB17D-9CD2-4A93-9733-46CC89022E7C");
            public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            public static readonly Guid Favorites = new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD");
            public static readonly Guid Fonts = new Guid("FD228CB7-AE11-4AE3-864C-16F3910AB8FE");
            public static readonly Guid Games = new Guid("CAC52C1A-B53D-4edc-92D7-6B2E8AC19434");
            public static readonly Guid GameTasks = new Guid("054FAE61-4DD8-4787-80B6-090220C4B700");
            public static readonly Guid History = new Guid("D9DC8A3B-B784-432E-A781-5A1130A75963");
            public static readonly Guid ImplicitAppShortcuts = new Guid("BCB5256F-79F6-4CEE-B725-DC34E402FD46");
            public static readonly Guid InternetCache = new Guid("352481E8-33BE-4251-BA85-6007CAEDCF9D");
            public static readonly Guid InternetFolder = new Guid("4D9F7874-4E0C-4904-967B-40B0D20C3E4B");
            public static readonly Guid Libraries = new Guid("1B3EA5DC-B587-4786-B4EF-BD1DC332AEAE");
            public static readonly Guid Links = new Guid("bfb9d5e0-c6a9-404c-b2b2-ae6db6af4968");
            public static readonly Guid LocalAppData = new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
            public static readonly Guid LocalAppDataLow = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
            public static readonly Guid LocalizedResourcesDir = new Guid("2A00375E-224C-49DE-B8D1-440DF7EF3DDC");
            public static readonly Guid Music = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
            public static readonly Guid MusicLibrary = new Guid("2112AB0A-C86A-4FFE-A368-0DE96E47012E");
            public static readonly Guid NetHood = new Guid("C5ABBF53-E17F-4121-8900-86626FC2C973");
            public static readonly Guid NetworkFolder = new Guid("D20BEEC4-5CA8-4905-AE3B-BF251EA09B53");
            public static readonly Guid OriginalImages = new Guid("2C36C0AA-5812-4b87-BFD0-4CD0DFB19B39");
            public static readonly Guid PhotoAlbums = new Guid("69D2CF90-FC33-4FB7-9A0C-EBB0F0FCB43C");
            public static readonly Guid Pictures = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
            public static readonly Guid PicturesLibrary = new Guid("A990AE9F-A03B-4E80-94BC-9912D7504104");
            public static readonly Guid Playlists = new Guid("DE92C1C7-837F-4F69-A3BB-86E631204A23");
            public static readonly Guid PrintersFolder = new Guid("76FC4E2D-D6AD-4519-A663-37BD56068185");
            public static readonly Guid PrintHood = new Guid("9274BD8D-CFD1-41C3-B35E-B13F55A758F4");
            public static readonly Guid Profile = new Guid("5E6C858F-0E22-4760-9AFE-EA3317B67173");
            public static readonly Guid ProgramData = new Guid("62AB5D82-FDC1-4DC3-A9DD-070D1D495D97");
            public static readonly Guid ProgramFiles = new Guid("905e63b6-c1bf-494e-b29c-65b732d3d21a");
            public static readonly Guid ProgramFilesX64 = new Guid("6D809377-6AF0-444b-8957-A3773F02200E");
            public static readonly Guid ProgramFilesX86 = new Guid("7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E");
            public static readonly Guid ProgramFilesCommon = new Guid("F7F1ED05-9F6D-47A2-AAAE-29D317C6F066");
            public static readonly Guid ProgramFilesCommonX64 = new Guid("6365D5A7-0F0D-45E5-87F6-0DA56B6A4F7D");
            public static readonly Guid ProgramFilesCommonX86 = new Guid("DE974D24-D9C6-4D3E-BF91-F4455120B917");
            public static readonly Guid Programs = new Guid("A77F5D77-2E2B-44C3-A6A2-ABA601054A51");
            public static readonly Guid Public = new Guid("DFDF76A2-C82A-4D63-906A-5644AC457385");
            public static readonly Guid PublicDesktop = new Guid("C4AA340D-F20F-4863-AFEF-F87EF2E6BA25");
            public static readonly Guid PublicDocuments = new Guid("ED4824AF-DCE4-45A8-81E2-FC7965083634");
            public static readonly Guid PublicDownloads = new Guid("3D644C9B-1FB8-4f30-9B45-F670235F79C0");
            public static readonly Guid PublicGameTasks = new Guid("DEBF2536-E1A8-4c59-B6A2-414586476AEA");
            public static readonly Guid PublicLibraries = new Guid("48DAF80B-E6CF-4F4E-B800-0E69D84EE384");
            public static readonly Guid PublicMusic = new Guid("3214FAB5-9757-4298-BB61-92A9DEAA44FF");
            public static readonly Guid PublicPictures = new Guid("B6EBFB86-6907-413C-9AF7-4FC2ABF07CC5");
            public static readonly Guid PublicUserTiles = new Guid("0482af6c-08f1-4c34-8c90-e17ec98b1e17");
            public static readonly Guid PublicRingtones = new Guid("E555AB60-153B-4D17-9F04-A5FE99FC15EC");
            public static readonly Guid PublicVideos = new Guid("2400183A-6185-49FB-A2D8-4A392A602BA3");
            public static readonly Guid QuickLaunch = new Guid("52a4f021-7b75-48a9-9f6b-4b87a210bc8f");
            public static readonly Guid Recent = new Guid("AE50C081-EBD2-438A-8655-8A092E34987A");
            public static readonly Guid RecordedTVLibrary = new Guid("1A6FDBA2-F42D-4358-A798-B74D745926C5");
            public static readonly Guid RecycleBinFolder = new Guid("B7534046-3ECB-4C18-BE4E-64CD4CB7D6AC");
            public static readonly Guid ResourceDir = new Guid("8AD10C31-2ADB-4296-A8F7-E4701232C972");
            public static readonly Guid Ringtones = new Guid("C870044B-F49E-4126-A9C3-B52A1FF411E8");
            public static readonly Guid RoamedTileImages = new Guid("AAA8D5A5-F1D6-4259-BAA8-78E7EF60835E");
            public static readonly Guid RoamingAppData = new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D");
            public static readonly Guid RoamingTiles = new Guid("00BCFC5A-ED94-4e48-96A1-3F6217F21990");
            public static readonly Guid SampleMusic = new Guid("B250C668-F57D-4EE1-A63C-290EE7D1AA1F");
            public static readonly Guid SamplePictures = new Guid("C4900540-2379-4C75-844B-64E6FAF8716B");
            public static readonly Guid SamplePlaylists = new Guid("15CA69B3-30EE-49C1-ACE1-6B5EC372AFB5");
            public static readonly Guid SampleVideos = new Guid("859EAD94-2E85-48AD-A71A-0969CB56A6CD");
            public static readonly Guid SavedGames = new Guid("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");
            public static readonly Guid SavedSearches = new Guid("7d1d3a04-debb-4115-95cf-2f29da2920da");
            public static readonly Guid SEARCH_CSC = new Guid("ee32e446-31ca-4aba-814f-a5ebd2fd6d5e");
            public static readonly Guid SEARCH_MAPI = new Guid("98ec0e18-2098-4d44-8644-66979315a281");
            public static readonly Guid SearchHome = new Guid("190337d1-b8ca-4121-a639-6d472d16972a");
            public static readonly Guid SendTo = new Guid("8983036C-27C0-404B-8F08-102D10DCFD74");
            public static readonly Guid SidebarDefaultParts = new Guid("7B396E54-9EC5-4300-BE0A-2482EBAE1A26");
            public static readonly Guid SidebarParts = new Guid("A75D362E-50FC-4fb7-AC2C-A8BEAA314493");
            public static readonly Guid StartMenu = new Guid("625B53C3-AB48-4EC1-BA1F-A1EF4146FC19");
            public static readonly Guid Startup = new Guid("B97D20BB-F46A-4C97-BA10-5E3608430854");
            public static readonly Guid SyncManagerFolder = new Guid("43668BF8-C14E-49B2-97C9-747784D784B7");
            public static readonly Guid SyncResultsFolder = new Guid("289a9a43-be44-4057-a41b-587a76d7e7f9");
            public static readonly Guid SyncSetupFolder = new Guid("0F214138-B1D3-4a90-BBA9-27CBC0C5389A");
            public static readonly Guid System = new Guid("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7");
            public static readonly Guid SystemX86 = new Guid("D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27");
            public static readonly Guid Templates = new Guid("A63293E8-664E-48DB-A079-DF759E0509F7");
            public static readonly Guid TreeProperties = new Guid("5b3749ad-b49f-49c1-83eb-15370fbd4882");
            public static readonly Guid UserPinned = new Guid("9E3995AB-1F9C-4F13-B827-48B24B6C7174");
            public static readonly Guid UserProfiles = new Guid("0762D272-C50A-4BB0-A382-697DCD729B80");
            public static readonly Guid UsersFiles = new Guid("f3ce0f7c-4901-4acc-8648-d5d44b04ef8f");
            public static readonly Guid Videos = new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
            public static readonly Guid VideosLibrary = new Guid("491E922F-5643-4AF4-A7EB-4E7A138D8174");
            public static readonly Guid Windows = new Guid("F38BF404-1D43-42F2-9305-67DE0B28FC23");
        }

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumServicesStatusEx(IntPtr hSCManager,
        int infoLevel, int dwServiceType,
        int dwServiceState, IntPtr lpServices, UInt32 cbBufSize,
        out uint pcbBytesNeeded, out uint lpServicesReturned,
        ref uint lpResumeHandle, string pszGroupName);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ENUM_SERVICE_STATUS_PROCESS
        {
            internal static readonly int SizePack4 = Marshal.SizeOf(typeof(ENUM_SERVICE_STATUS_PROCESS));

            /// <summary>
            /// sizeof(ENUM_SERVICE_STATUS_PROCESS) allow Packing of 8 on 64 bit machines
            /// </summary>
            internal static readonly int SizePack8 = Marshal.SizeOf(typeof(ENUM_SERVICE_STATUS_PROCESS)) + 4;

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            internal string pServiceName;

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            internal string pDisplayName;

            internal SERVICE_STATUS_PROCESS ServiceStatus;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS_PROCESS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            public int processId;
            public int serviceFlags;
        }


        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr processHandle,
           int processInformationClass, IntPtr processInformation, uint processInformationLength,
           IntPtr returnLength);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;

            public static int Size
            {
                get { return (int)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PEB
        {
            public byte InheritedAddressSpace;
            public byte ReadImageFileExecOptions;
            public byte BeingDebugged;
            public byte SpareBool;
            public IntPtr Mutant;
            public IntPtr ImageBaseAddress;
            public IntPtr Ldr;
            public IntPtr ProcessParameters;

            public static int Size
            {
                get { return (int)Marshal.SizeOf(typeof(PEB)); }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct UNICODE_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct CURDIR
        {
            UNICODE_STRING DosPath;
            public IntPtr Handle;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct RTL_USER_PROCESS_PARAMETERS
        {
            public UInt32 MaximumLength;
            public UInt32 Length;
            public UInt32 Flags;
            public UInt32 DebugFlags;
            public IntPtr ConsoleHandle;
            public UInt32 ConsoleFlags; //???
            public IntPtr StandardInput;
            public IntPtr StandardOutput;
            public IntPtr StandardError;
            public CURDIR CurrentDirectory;
            public UNICODE_STRING DllPath;
            public UNICODE_STRING ImagePathName;
            public UNICODE_STRING CommandLine;

            public static int Size
            {
                get { return (int)Marshal.SizeOf(typeof(RTL_USER_PROCESS_PARAMETERS)); }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentDirectory(uint nBufferLength, [Out] StringBuilder lpBuffer);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        public enum VzLayerAttribute {
	        VzLayerAttributeLayerId = 0,			// @emem	The GUID of the layer
	        VzLayerAttributeName = 1,				// @emem	The name of the layer
	        VzLayerAttributeCompany = 2,			// @emem	The name of your company
	        VzLayerAttributeAuthor = 3,				// @emem	The name of the person who made the layer
	        VzLayerAttributeNotes = 4,				// @emem	Any comment you may want about the layer
	        VzLayerAttributeVersion = 5,			// @emem	The version of the layer
	        VzLayerAttributeActiveOnStart = 6,		// @emem	Tells if the layer is active on start
	        VzLayerAttributeActive = 7,				// @emem	Tells if the layer is active
	        VzLayerAttributeFileRedirPath = 8,		// @emem	Contains the location of the file redirect area
	        VzLayerAttributeRegRedirPath = 9,		// @emem	Contains the location of the registry redirect area
	        VzLayerAttributeFlags = 10,				// @emem	The Flags for the layer
	        VzLayerAttributeLayerMajorVersion = 11,	// @emem	The Major version of the layer
	        VzLayerAttributeLayerMinorVersion = 12,  // @emem	The Minor version of the layer
	        VzLayerAttributeReadOnly = 13,			// @emem	Tells if the layer is a read only layer
	        VzLayerAttributeType = 14,				// @emem	Tells the type of the layer
	        VzLayerAttributePeerLayerId = 15,		// @emem	The GUID of the peer layer
        }

        public enum VzDataType {
	        VzDataTypeDword,					// @emem	The data type is a DWORD
	        VzDataTypeString					// @emem	The data type is a string
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
           uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        #endregion

        #region FSL2

        public const uint FSL2_MAXNAMELEN = 64;
        public const uint FSL2_PRODUCT_ID_SVS = 71;
        public const uint FSL2_MAX_KEY_LENGTH = 150;
        public const uint FSL2_MAXIDLEN = 200;
        public const uint VZ_MOUNT_EDIT = 0x1;
        public const uint VZ_DISMOUNT_EDIT = 0x1;

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2FindFirstFile(string fslGUID, string fileName, out WIN32_FIND_DATA fileData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern bool FSL2FindNextFile(uint hFindFile, out WIN32_FIND_DATA fileData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2FindClose(uint hFindFile);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2RenameLayer(string fslGUID, string newName);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2DeleteLayer(string fslGUID, bool deletePeer);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2GetProductKey([In] uint productId, 
            [Out] StringBuilder lpFilename, [In] uint cbProductKeyBuffer);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2InitSystem(string origProductKey, uint productId,
            ref uint numDaysRemaining);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2GetLayerInfo(string fslGUID, ref FSL2_INFO pInfo);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2FindFirstLayer(ref FSL2_FIND fslFind, 
            StringBuilder fslGUID, int includePeers);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2FindNextLayer(ref FSL2_FIND fslFind, 
            StringBuilder fslGUID);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2FindCloseLayer(ref FSL2_FIND fslFind);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2RegOpenKeyEx(string fslGUID, UIntPtr hKey,
            string lpSubKey, uint ulOptions, uint samDesired, out UIntPtr phkResult);

        [DllImport("fsllib32")]
        public extern static uint FSL2RegCloseKey(UIntPtr hKey);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegCreateKeyEx(string fslGUID, UIntPtr hKey,
                                                     string lpSubKey, uint reserved, string lpClass, uint dwOptions,
                                                     uint samDesired, uint lpSecurityAttributes, out UIntPtr phkResult,
                                                     out uint lpdwDisposition);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegSetValueEx(UIntPtr hKey,
                                                    string lpValueName, uint reserved, RegistryValueKind dwType,
                                                    object lpData,
                                                    int cbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegSetValueEx(UIntPtr hKey, string lpValueName, int reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegSetValueEx(UIntPtr hKey, string lpValueName, int reserved, RegistryValueKind dwType, ref int lpData, int cbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegSetValueEx(UIntPtr hKey, string lpValueName, int reserved, RegistryValueKind dwType, ref long lpData, int cbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegSetValueEx(UIntPtr hKey, string lpValueName, int reserved, RegistryValueKind dwType, string lpData, int cbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint VzCreateProcessFromLayerW(
            string fslGuid,
            IntPtr lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint VzMergeLayerW(string layerGUID, string userSID, IntPtr CBInfoFunc, IntPtr pUserData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2SetLayerEditState(string fslGUID, uint dwState,
            int bSpecialDisplay, uint dwSecondsToWait);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint VzGetLayerAttribute(string fslGUID, VzLayerAttribute attribute,
                                                      out VzDataType dataType,
                                                      [Out] StringBuilder buffer, ref uint cbBuffer);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint VzGetLayerMetadirectory(string fslGUID, [Out] StringBuilder buffer, uint cbBuffer);

        //[DllImport("fsllib32", CharSet = CharSet.Unicode)]
        //public extern static uint FSL2RegSetValueEx(UIntPtr hKey, string lpValueName,
        //    uint Reserved, uint dwType,
        //    __in_bcount(cbData)CONST BYTE *lpData,
        //    __in DWORD cbData
        //);
        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2RegCopyValue(UIntPtr srcKey, string lpValueName,
            UIntPtr destKey, int overwrite, int removeAfterCopy);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2RegEnumKeyEx(
            UIntPtr hKey, 
            uint dwIndex, 
            StringBuilder lpName, 
            out uint lpcchName, 
            uint lpReserved, 
            StringBuilder lpClass, 
            out uint lpcchClass, 
            out FILETIME lastWriteTime);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2RegQueryValue(
            UIntPtr hKey,
	        string lpValueName,
	        uint lpReserved,
	        StringBuilder lpType,
	        StringBuilder lpData,
	        out uint lpcbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2RegEnumValue(
            UIntPtr hKey,
            uint dwIndex,
            StringBuilder lpValueName,
            out uint lpcchValueName,
            uint lpReserved,
            out uint lpType,
            StringBuilder lpData,
            out uint lpcbData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2RegCopyKey(UIntPtr srcKey, string srcKeyName,
            UIntPtr destParentKey, int overwrite, int removeAfterCopy);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2DeleteFile(string fslGUID, string fslFilePath);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2AddFile(string fslGUID, string filePath,
	        string fslFilePath, uint callback, uint pDisposition, uint pUserData);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2ActivateLayerW(string fslGUID, bool bRunAppsInStartupFolder);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static uint FSL2DeactivateLayerW(string fslGUID, bool force, IntPtr zero);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public extern static bool FSL2CreateDirectory(string fslGUID, string fullDirectoryName,
	        uint lpSecurityAttributes);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2GetFullNonLayerPath(string fslGUID,
                                                          string path, [Out] StringBuilder lpFilename,
                                                          uint cbNonLayerPath
            );

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2VariablizePath(string path, [Out] StringBuilder resultPath,
                                                     uint cbResultPath);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2DevariablizePath(string path, [Out] StringBuilder resultPath,
                                                        uint cbResultPath);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2CreateLayer(string fslName, uint layerType, bool createPeer,
                                                  [Out] StringBuilder lpFilename);

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2ReloadIsolationRules();

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2ReloadKeepInLayerList();

        [DllImport("fsllib32", CharSet = CharSet.Unicode)]
        public static extern uint FSL2ResetPeer(string fslGuid, bool force, out uint pPid);

        //[DllImport("fsllib32", CharSet = CharSet.Unicode)]
        //public extern static uint FSL2RegSetValueEx(UIntPtr hKey, string lpValueName, uint Reserved, uint dwType, CONST BYTE *lpData, uint cbData);

        #region FSL2_ERROR_CODES
        public const uint FSL2_ERROR_SUCCESS = 0;
        public const uint FSL2_ERROR_LAYER_ALREADY_EXISTS = 1001;
        public const uint FSL2_ERROR_NO_SUCH_NON_PEER = 1002;
        public const uint FSL2_ERROR_NO_SUCH_GROUP = 1003;
        public const uint FSL2_ERROR_NOT_GROUP_MEMBER = 1004;
        public const uint FSL2_ERROR_FILE_COPY = 1005;
        public const uint FSL2_ERROR_BAD_ARGS = 1006;
        public const uint FSL2_ERROR_OPEN = 1007;
        public const uint FSL2_ERROR_LAYER_DELETE = 1008;
        public const uint FSL2_ERROR_ARCHIVE_CREATE = 1009;
        public const uint FSL2_ERROR_ARCHIVE_EXTRACT = 1010;
        public const uint FSL2_ERROR_INVALID_ARCHIVE = 1011;
        public const uint FSL2_ERROR_DELETE_FILE = 1012;
        public const uint FSL2_ERROR_CREATE_FILE = 1013;
        public const uint FSL2_ERROR_GET_VERSION = 1014;
        public const uint FSL2_ERROR_CREATE_GROUP = 1015;
        public const uint FSL2_ERROR_ADD_TO_GROUP = 1016;
        public const uint FSL2_GROUP_ALREADY_EXISTS = 1017;
        public const uint FSL2_ERROR_CREATE_DIRECTORY = 1018;
        public const uint FSL2_ERROR_INVALID_VARIABLIZED_NAME = 1019;
        public const uint FSL2_ERROR_UNSUPPORTED_PLATFORM = 1020;
        public const uint FSL2_ERROR_LOAD_LIBRARY = 1021;
        public const uint FSL2_ERROR_GET_PROC_ADDRESS = 1022;
        public const uint FSL2_ERROR_GET_MODULE = 1023;
        public const uint FSL2_ERROR_TERMINATE_PROCESS = 1024;
        public const uint FSL2_ERROR_OPEN_PROCESS = 1025;
        public const uint FSL2_ERROR_SNAPSHOT = 1026;
        public const uint FSL2_ERROR_DELETE_PEER = 1027;
        public const uint FSL2_ERROR_FLUSH = 1028;
        public const uint FSL2_ERROR_ENUM_PROCESSES = 1029;
        public const uint FSL2_ERROR_BUFFER_TOO_SMALL = 1030;
        public const uint FSL2_ERROR_KEY_INVALID = 1031;
        public const uint FSL2_ERROR_STARTING_THREAD = 1032;
        public const uint FSL2_ERROR_RENAME_FILE = 1033;
        public const uint FSL2_ERROR_GET_ATTRS = 1034;
        public const uint FSL2_ERROR_SECURITY = 1035;
        public const uint FSL2_ERROR_IS_NOT_NTFS = 1036;
        public const uint FSL2_ERROR_NOMEM = 1037;
        public const uint FSL2_ERROR_LAYER_NAME_INVALID = 1038;
        public const uint FSL2_ERROR_INVALID_OP_FOR_LAYER_STATE = 1039;
        public const uint FSL2_ERROR_LAYER_REG_OPERATION = 1040;
        public const uint FSL2_ERROR_LAYER_NOT_FOUND = 1041;
        public const uint FSL2_ERROR_ENUM_COMPLETE = 1041;
        public const uint FSL2_ERROR_WRONGVERSION = 1042;
        public const uint FSL2_ERROR_FILEPATH_ALREADY_EXISTS = 1043;
        public const uint FSL2_ERROR_BADHANDLE = 1044;
        public const uint FSL2_ERROR_BADNODE = 1045;
        public const uint FSL2_ERROR_NORESOURCES = 1046;
        public const uint FSL2_ERROR_ITEMNOTFOUND = 1047;
        public const uint FSL2_ERROR_NOTIMPLEMENTED = 1048;
        public const uint FSL2_ERROR_ALREADYACTIVE = 1049;
        public const uint FSL2_ERROR_FILEIOERROR = 1050;
        public const uint FSL2_ERROR_NOT_LOADED = 1051;
        public const uint FSL2_ERROR_PIDHASHANDLEOPEN = 1052;
        public const uint FSL2_ERROR_PIDRUNNINGFROMLAYER = 1053;
        public const uint FSL2_ERROR_SYSTEMHASOPENFILE = 1054;
        public const uint FSL2_ERROR_SYSTEM_NOT_INITIALIZED = 1055;
        public const uint FSL2_ERROR_ZIP_DLL_NOT_LOADED = 1056;
        public const uint FSL2_ERROR_MORE_DATA = 1057;
        public const uint FSL2_ERROR_USER_CANCELLED = 1058;
        public const uint FSL2_ERROR_UNSUPPORTED_VERSION = 1059;
        public const uint FSL2_ERROR_MULTIPLE_LAYERS_SAME_NAME = 1060;
        public const uint FSL2_ERROR_STRING_CHH_FAIL = 1062;
        public const uint FSL2_ERROR_CREATE_PROCESS = 1063;
        public const uint FSL2_ERROR_COM_ERROR = 1064;
        public const uint FSL2_ERROR_NO_SUCH_USER = 1065;
        public const uint FSL2_ERROR_NOT_RUNTIME_LAYER = 1066;
        public const uint FSL2_ERROR_INVALID_GUID = 1067;
        public const uint FSL2_ERROR_LAYER_BEING_EDITED = 1068;
        public const uint FSL2_ERROR_LAYER_NOT_BEING_EDITED = 1069;
        public const uint FSL2_ERROR_UNSUPPORTED_ARCHIVE_VER = 1070;
        public const uint FSL2_ERROR_AN_APP_STARTUP_FAILED = 1071;
        public const uint FSL2_PATH_TOO_LONG = 1072;
        public const uint FSL2_ERROR_NO_REDIR_VOLUMES = 1073;
        public const uint FSL2_ERROR_METADATA_VALUE = 1074;
        public const uint FSL2_ERROR_NOTIFY_VALUE_NOT_FOUND = 1076;
        public const uint FSL2_ERROR_AEXCLIENT_NOT_FOUND = 1077;
        public const uint FSL2_ERROR_LAYER_ATTR_VALUE_NOT_FOUND = 1078;
        public const uint FSL2_ERROR_METADATA_KEY = 1079;
        public const uint FSL2_ERROR_LAYER_ATTR_UNDEFINED = 1080;
        public const uint FSL2_ERROR_NOTIFY_EVENT_UNDEFINED = 1081;
        public const uint FSL2_ERROR_FILEPATH_NOT_FOUND = 1082;
        public const uint FSL2_ERROR_KEY_ALREADY_EXISTS = 1083;
        public const uint FSL2_ERROR_FILE_DELETE = 1084;
        public const uint FSL2_ERROR_SERVICE_DELETE = 1085;
        public const uint FSL2_ERROR_GROUP_NAME_INVALID = 1086;
        public const uint FSL2_ERROR_PROCESS_NOT_FOUND = 1087;
        public const uint FSL2_ERROR_STRING_MB_TO_WIDE = 1088;
        public const uint FSL2_ERROR_DRIVER_INVALID_STATE = 1089;
        public const uint FSL2_ERROR_VALUE_ALREADY_EXISTS = 1090;
        public const uint FSL2_ERROR_NOTIFY_SERVER_NOT_ENABLED = 1091;
        public const uint FSL2_ERROR_ARCHIVE_NOT_RUNTIME = 1092;
        public const uint FSL2_ERROR_FSLLIB32_UNINITIALIZED = 1093;
        public const uint FSL2_ERROR_CHILKAT_NOT_UNLOCKED = 1094;
        public const uint FSL2_ERROR_CHILKAT_ZIP_OPEN = 1095;
        public const uint FSL2_ERROR_ACTIVATE_DEPENDENT = 1096;
        public const uint FSL2_ERROR_EXPORT_REMOVE_SIDS = 1097;
        public const uint FSL2_ERROR_EXPORT_ADD_BACK_SIDS = 1098;
        public const uint FSL2_ERROR_INVALID_METAFILE = 1099;
        public const uint FSL2_ERROR_PRODUCT_KEY_INVALID = 2000;
        public const uint FSL2_ERROR_PRODUCT_KEY_NOT_YET_VALID = 2001;
        public const uint FSL2_PRODUCT_KEY_VALID = 2002;
        public const uint FSL2_PRODUCT_KEY_WILL_EXPIRE = 2003;
        public const uint FSL2_ERROR_PRODUCT_KEY_EXPIRED = 2004;
        public const uint FSL2_ERROR_NO_PRODUCT_KEY = 2005;
        public const uint FSL2_ERROR_DEPRECATED = 2006;
        public const uint FSL2_INVALID_PRODUCT_FUNCTION = 2050;
#endregion

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct FSL2_FIND
{
            public UInt32 dwStructSize;
            public uint index;
            public IntPtr key;
            public int includePeers;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct FSL2_INFO
        {
            public UInt32 dwStructSize;
            public int bIsGroup;
            public uint dwLayerType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) FSL2_MAXNAMELEN)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) FSL2_MAXNAMELEN)]
            public string peerName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) 260)]
            public string regPath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) 260)]
            public string fileRedirPath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)260)]
            public string regRedirPath;
            public uint active;
            public uint activeOnStart;
            public uint enabled;
            public uint majorVersion;
            public uint minorVersion;
            public uint lastActivatedTime;
            public uint createTime;
            public uint lastRefreshTime;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)FSL2_MAXIDLEN)]
            public string fslGUID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int) FSL2_MAXIDLEN)]
            public string peerGUID;
            public uint compressionLevel;
            public uint flags;
            public uint visibilityFlags;
        }

        // The CharSet must match the CharSet of the corresponding PInvoke signature
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

        #endregion

        #region ERROR_CODES
        const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        #region Win32ErrorCodes
        static readonly Dictionary<UInt64, string> Win32ErrorCodes = new Dictionary<UInt64, string>
        {
            {0, "SUCCESS"},
            {1, "ERROR_INVALID_FUNCTION"},
            {2, "ERROR_FILE_NOT_FOUND"},
            {3, "ERROR_PATH_NOT_FOUND"},
            {4, "ERROR_TOO_MANY_OPEN_FILES"},
            {5, "ERROR_ACCESS_DENIED"},
            {6, "ERROR_INVALID_HANDLE"},
            {7, "ERROR_ARENA_TRASHED"},
            {8, "ERROR_NOT_ENOUGH_MEMORY"},
            {9, "ERROR_INVALID_BLOCK"},
            {10, "ERROR_BAD_ENVIRONMENT"},
            {11, "ERROR_BAD_FORMAT"},
            {12, "ERROR_INVALID_ACCESS"},
            {13, "ERROR_INVALID_DATA"},
            {14, "ERROR_OUTOFMEMORY"},
            {15, "ERROR_INVALID_DRIVE"},
            {16, "ERROR_CURRENT_DIRECTORY"},
            {17, "ERROR_NOT_SAME_DEVICE"},
            {18, "ERROR_NO_MORE_FILES"},
            {19, "ERROR_WRITE_PROTECT"},
            {20, "ERROR_BAD_UNIT"},
            {21, "ERROR_NOT_READY"},
            {22, "ERROR_BAD_COMMAND"},
            {23, "ERROR_CRC"},
            {24, "ERROR_BAD_LENGTH"},
            {25, "ERROR_SEEK"},
            {26, "ERROR_NOT_DOS_DISK"},
            {27, "ERROR_SECTOR_NOT_FOUND"},
            {28, "ERROR_OUT_OF_PAPER"},
            {29, "ERROR_WRITE_FAULT"},
            {30, "ERROR_READ_FAULT"},
            {31, "ERROR_GEN_FAILURE"},
            {32, "ERROR_SHARING_VIOLATION"},
            {33, "ERROR_LOCK_VIOLATION"},
            {34, "ERROR_WRONG_DISK"},
            {36, "ERROR_SHARING_BUFFER_EXCEEDED"},
            {38, "ERROR_HANDLE_EOF"},
            {39, "ERROR_HANDLE_DISK_FULL"},
            {50, "ERROR_NOT_SUPPORTED"},
            {51, "ERROR_REM_NOT_LIST"},
            {52, "ERROR_DUP_NAME"},
            {53, "ERROR_BAD_NETPATH"},
            {54, "ERROR_NETWORK_BUSY"},
            {55, "ERROR_DEV_NOT_EXIST"},
            {56, "ERROR_TOO_MANY_CMDS"},
            {57, "ERROR_ADAP_HDW_ERR"},
            {58, "ERROR_BAD_NET_RESP"},
            {59, "ERROR_UNEXP_NET_ERR"},
            {60, "ERROR_BAD_REM_ADAP"},
            {61, "ERROR_PRINTQ_FULL"},
            {62, "ERROR_NO_SPOOL_SPACE"},
            {63, "ERROR_PRINT_CANCELLED"},
            {64, "ERROR_NETNAME_DELETED"},
            {65, "ERROR_NETWORK_ACCESS_DENIED"},
            {66, "ERROR_BAD_DEV_TYPE"},
            {67, "ERROR_BAD_NET_NAME"},
            {68, "ERROR_TOO_MANY_NAMES"},
            {69, "ERROR_TOO_MANY_SESS"},
            {70, "ERROR_SHARING_PAUSED"},
            {71, "ERROR_REQ_NOT_ACCEP"},
            {72, "ERROR_REDIR_PAUSED"},
            {80, "ERROR_FILE_EXISTS"},
            {82, "ERROR_CANNOT_MAKE"},
            {83, "ERROR_FAIL_I24"},
            {84, "ERROR_OUT_OF_STRUCTURES"},
            {85, "ERROR_ALREADY_ASSIGNED"},
            {86, "ERROR_INVALID_PASSWORD"},
            {87, "ERROR_INVALID_PARAMETER"},
            {1223, "ERROR_CANCELLED"},
            {1221, "ERROR_DUP_DOMAINNAME"},
            

        };
        #endregion
        #region NtStatusCodes
        static readonly Dictionary<UInt64, string> NtStatusCodes = new Dictionary<UInt64, string>
        {
            {0x00000000, "SUCCESS"},
            {0x00000001, "WAIT_1"},
            {0x00000002, "WAIT_2"},
            {0x00000003, "WAIT_3"},
            {0x0000003f, "WAIT_63"},
            {0x00000080, "ABANDONED"},
            {0x000000bf, "ABANDONED_WAIT_63"},
            {0x000000c0, "USER_APC"},
            {0x00000101, "ALERTED"},
            {0x00000102, "TIMEOUT"},
            {0x00000103, "PENDING"},
            {0x00000104, "REPARSE"},
            {0x00000105, "MORE_ENTRIES"},
            {0x00000106, "NOT_ALL_ASSIGNED"},
            {0x00000107, "SOME_NOT_MAPPED"},
            {0x00000108, "OPLOCK_BREAK_IN_PROGRESS"},
            {0x00000109, "VOLUME_MOUNTED"},
            {0x0000010a, "RXACT_COMMITTED"},
            {0x0000010b, "NOTIFY_CLEANUP"},
            {0x0000010c, "NOTIFY_ENUM_DIR"},
            {0x0000010d, "NO_QUOTAS_FOR_ACCOUNT"},
            {0x0000010e, "PRIMARY_TRANSPORT_CONNECT_FAILED"},
            {0x00000110, "PAGE_FAULT_TRANSITION"},
            {0x00000111, "PAGE_FAULT_DEMAND_ZERO"},
            {0x00000112, "PAGE_FAULT_COPY_ON_WRITE"},
            {0x00000113, "PAGE_FAULT_GUARD_PAGE"},
            {0x00000114, "PAGE_FAULT_PAGING_FILE"},
            {0x00000115, "CACHE_PAGE_LOCKED"},
            {0x00000116, "CRASH_DUMP"},
            {0x00000117, "BUFFER_ALL_ZEROS"},
            {0x00000118, "REPARSE_OBJECT"},
            {0x00000119, "RESOURCE_REQUIREMENTS_CHANGED"},
            {0x00000120, "TRANSLATION_COMPLETE"},
            {0x00000121, "DS_MEMBERSHIP_EVALUATED_LOCALLY"},
            {0x00000122, "NOTHING_TO_TERMINATE"},
            {0x00000123, "PROCESS_NOT_IN_JOB"},
            {0x00000124, "PROCESS_IN_JOB"},
            {0x00000125, "VOLSNAP_HIBERNATE_READY"},
            {0x00000126, "FSFILTER_OP_COMPLETED_SUCCESSFULLY"},
            {0x00000127, "INTERRUPT_VECTOR_ALREADY_CONNECTED"},
            {0x00000128, "INTERRUPT_STILL_CONNECTED"},
            {0x00000129, "PROCESS_CLONED"},
            {0x0000012a, "FILE_LOCKED_WITH_ONLY_READERS"},
            {0x0000012b, "FILE_LOCKED_WITH_WRITERS"},
            {0x00000202, "RESOURCEMANAGER_READ_ONLY"},
            {0x00000367, "WAIT_FOR_OPLOCK"},
            {0x00010001, "EXCEPTION_HANDLED"},
            {0x00010002, "CONTINUE"},
            {0x001c0001, "FLT_IO_COMPLETE"},
            {0x40000000, "OBJECT_NAME_EXISTS"},
            {0x40000001, "THREAD_WAS_SUSPENDED"},
            {0x40000002, "WORKING_SET_LIMIT_RANGE"},
            {0x40000003, "IMAGE_NOT_AT_BASE"},
            {0x40000004, "RXACT_STATE_CREATED"},
            {0x40000005, "SEGMENT_NOTIFICATION"},
            {0x40000006, "LOCAL_USER_SESSION_KEY"},
            {0x40000007, "BAD_CURRENT_DIRECTORY"},
            {0x40000008, "SERIAL_MORE_WRITES"},
            {0x40000009, "REGISTRY_RECOVERED"},
            {0x4000000a, "FT_READ_RECOVERY_FROM_BACKUP"},
            {0x4000000b, "FT_WRITE_RECOVERY"},
            {0x4000000c, "SERIAL_COUNTER_TIMEOUT"},
            {0x4000000d, "NULL_LM_PASSWORD"},
            {0x4000000e, "IMAGE_MACHINE_TYPE_MISMATCH"},
            {0x4000000f, "RECEIVE_PARTIAL"},
            {0x40000010, "RECEIVE_EXPEDITED"},
            {0x40000011, "RECEIVE_PARTIAL_EXPEDITED"},
            {0x40000012, "EVENT_DONE"},
            {0x40000013, "EVENT_PENDING"},
            {0x40000014, "CHECKING_FILE_SYSTEM"},
            {0x40000015, "FATAL_APP_EXIT"},
            {0x40000016, "PREDEFINED_HANDLE"},
            {0x40000017, "WAS_UNLOCKED"},
            {0x40000018, "SERVICE_NOTIFICATION"},
            {0x40000019, "WAS_LOCKED"},
            {0x4000001a, "LOG_HARD_ERROR"},
            {0x4000001b, "ALREADY_WIN32"},
            {0x4000001c, "WX86_UNSIMULATE"},
            {0x4000001d, "WX86_CONTINUE"},
            {0x4000001e, "WX86_SINGLE_STEP"},
            {0x4000001f, "WX86_BREAKPOINT"},
            {0x40000020, "WX86_EXCEPTION_CONTINUE"},
            {0x40000021, "WX86_EXCEPTION_LASTCHANCE"},
            {0x40000022, "WX86_EXCEPTION_CHAIN"},
            {0x40000023, "IMAGE_MACHINE_TYPE_MISMATCH_EXE"},
            {0x40000024, "NO_YIELD_PERFORMED"},
            {0x40000025, "TIMER_RESUME_IGNORED"},
            {0x40000026, "ARBITRATION_UNHANDLED"},
            {0x40000027, "CARDBUS_NOT_SUPPORTED"},
            {0x40000028, "WX86_CREATEWX86TIB"},
            {0x40000029, "MP_PROCESSOR_MISMATCH"},
            {0x4000002a, "HIBERNATED"},
            {0x4000002b, "RESUME_HIBERNATION"},
            {0x4000002c, "FIRMWARE_UPDATED"},
            {0x4000002d, "DRIVERS_LEAKING_LOCKED_PAGES"},
            {0x4000002e, "MESSAGE_RETRIEVED"},
            {0x4000002f, "SYSTEM_POWERSTATE_TRANSITION"},
            {0x40000030, "ALPC_CHECK_COMPLETION_LIST"},
            {0x40000031, "SYSTEM_POWERSTATE_COMPLEX_TRANSITION"},
            {0x40000032, "ACCESS_AUDIT_BY_POLICY"},
            {0x40000033, "ABANDON_HIBERFILE"},
            {0x40000034, "BIZRULES_NOT_ENABLED"},
            {0x40000294, "WAKE_SYSTEM"},
            {0x40000370, "DS_SHUTTING_DOWN"},
            {0x40010001, "REPLY_LATER"},
            {0x40010002, "UNABLE_TO_PROVIDE_HANDLE"},
            {0x40010003, "TERMINATE_THREAD"},
            {0x40010004, "TERMINATE_PROCESS"},
            {0x40010005, "CONTROL_C"},
            {0x40010006, "PRINTEXCEPTION_C"},
            {0x40010007, "RIPEXCEPTION"},
            {0x40010008, "CONTROL_BREAK"},
            {0x40010009, "COMMAND_EXCEPTION"},
            {0x40020056, "NT_UUID_LOCAL_ONLY"},
            {0x400200af, "NT_SEND_INCOMPLETE"},
            {0x400a0004, "CTX_CDM_CONNECT"},
            {0x400a0005, "CTX_CDM_DISCONNECT"},
            {0x4015000d, "SXS_RELEASE_ACTIVATION_CONTEXT"},
            {0x40190034, "RECOVERY_NOT_NEEDED"},
            {0x40190035, "RM_ALREADY_STARTED"},
            {0x401a000c, "LOG_NO_RESTART"},
            {0x401b00ec, "VIDEO_DRIVER_DEBUG_REPORT_REQUEST"},
            {0x401e000a, "GRAPHICS_PARTIAL_DATA_POPULATED"},
            {0x401e0117, "GRAPHICS_DRIVER_MISMATCH"},
            {0x401e0307, "GRAPHICS_MODE_NOT_PINNED"},
            {0x401e031e, "GRAPHICS_NO_PREFERRED_MODE"},
            {0x401e034b, "GRAPHICS_DATASET_IS_EMPTY"},
            {0x401e034c, "GRAPHICS_NO_MORE_ELEMENTS_IN_DATASET"},
            {0x401e0351, "GRAPHICS_PATH_CONTENT_GEOMETRY_TRANSFORMATION_NOT_PINNED"},
            {0x401e042f, "GRAPHICS_UNKNOWN_CHILD_STATUS"},
            {0x401e0437, "GRAPHICS_LEADLINK_START_DEFERRED"},
            {0x401e0439, "GRAPHICS_POLLING_TOO_FREQUENTLY"},
            {0x401e043a, "GRAPHICS_START_DEFERRED"},
            {0x40230001, "NDIS_INDICATION_REQUIRED"},
            {0x406d1388, "THREAD_NAMING_EXCEPTION"},
            {0x80000001, "GUARD_PAGE_VIOLATION"},
            {0x80000002, "DATATYPE_MISALIGNMENT"},
            {0x80000003, "BREAKPOINT"},
            {0x80000004, "SINGLE_STEP"},
            {0x80000005, "BUFFER_OVERFLOW"},
            {0x80000006, "NO_MORE_FILES"},
            {0x80000007, "WAKE_SYSTEM_DEBUGGER"},
            {0x8000000a, "HANDLES_CLOSED"},
            {0x8000000b, "NO_INHERITANCE"},
            {0x8000000c, "GUID_SUBSTITUTION_MADE"},
            {0x8000000d, "PARTIAL_COPY"},
            {0x8000000e, "DEVICE_PAPER_EMPTY"},
            {0x8000000f, "DEVICE_POWERED_OFF"},
            {0x80000010, "DEVICE_OFF_LINE"},
            {0x80000011, "DEVICE_BUSY"},
            {0x80000012, "NO_MORE_EAS"},
            {0x80000013, "INVALID_EA_NAME"},
            {0x80000014, "EA_LIST_INCONSISTENT"},
            {0x80000015, "INVALID_EA_FLAG"},
            {0x80000016, "VERIFY_REQUIRED"},
            {0x80000017, "EXTRANEOUS_INFORMATION"},
            {0x80000018, "RXACT_COMMIT_NECESSARY"},
            {0x8000001a, "NO_MORE_ENTRIES"},
            {0x8000001b, "FILEMARK_DETECTED"},
            {0x8000001c, "MEDIA_CHANGED"},
            {0x8000001d, "BUS_RESET"},
            {0x8000001e, "END_OF_MEDIA"},
            {0x8000001f, "BEGINNING_OF_MEDIA"},
            {0x80000020, "MEDIA_CHECK"},
            {0x80000021, "SETMARK_DETECTED"},
            {0x80000022, "NO_DATA_DETECTED"},
            {0x80000023, "REDIRECTOR_HAS_OPEN_HANDLES"},
            {0x80000024, "SERVER_HAS_OPEN_HANDLES"},
            {0x80000025, "ALREADY_DISCONNECTED"},
            {0x80000026, "LONGJUMP"},
            {0x80000027, "CLEANER_CARTRIDGE_INSTALLED"},
            {0x80000028, "PLUGPLAY_QUERY_VETOED"},
            {0x80000029, "UNWIND_CONSOLIDATE"},
            {0x8000002a, "REGISTRY_HIVE_RECOVERED"},
            {0x8000002b, "DLL_MIGHT_BE_INSECURE"},
            {0x8000002c, "DLL_MIGHT_BE_INCOMPATIBLE"},
            {0x8000002d, "STOPPED_ON_SYMLINK"},
            {0x80000288, "DEVICE_REQUIRES_CLEANING"},
            {0x80000289, "DEVICE_DOOR_OPEN"},
            {0x80000803, "DATA_LOST_REPAIR"},
            {0x80010001, "EXCEPTION_NOT_HANDLED"},
            {0x80130001, "CLUSTER_NODE_ALREADY_UP"},
            {0x80130002, "CLUSTER_NODE_ALREADY_DOWN"},
            {0x80130003, "CLUSTER_NETWORK_ALREADY_ONLINE"},
            {0x80130004, "CLUSTER_NETWORK_ALREADY_OFFLINE"},
            {0x80130005, "CLUSTER_NODE_ALREADY_MEMBER"},
            {0x80190009, "COULD_NOT_RESIZE_LOG"},
            {0x80190029, "NO_TXF_METADATA"},
            {0x80190031, "CANT_RECOVER_WITH_HANDLE_OPEN"},
            {0x80190041, "TXF_METADATA_ALREADY_PRESENT"},
            {0x80190042, "TRANSACTION_SCOPE_CALLBACKS_NOT_SET"},
            {0x801b00eb, "VIDEO_HUNG_DISPLAY_DRIVER_THREAD_RECOVERED"},
            {0x801c0001, "FLT_BUFFER_TOO_SMALL"},
            {0x80210001, "FVE_PARTIAL_METADATA"},
            {0x80210002, "FVE_TRANSIENT_STATE"},
            {0xc0000001, "UNSUCCESSFUL"},
            {0xc0000002, "NOT_IMPLEMENTED"},
            {0xc0000003, "INVALID_INFO_CLASS"},
            {0xc0000004, "INFO_LENGTH_MISMATCH"},
            {0xc0000005, "ACCESS_VIOLATION"},
            {0xc0000006, "IN_PAGE_ERROR"},
            {0xc0000007, "PAGEFILE_QUOTA"},
            {0xc0000008, "INVALID_HANDLE"},
            {0xc0000009, "BAD_INITIAL_STACK"},
            {0xc000000a, "BAD_INITIAL_PC"},
            {0xc000000b, "INVALID_CID"},
            {0xc000000c, "TIMER_NOT_CANCELED"},
            {0xc000000d, "INVALID_PARAMETER"},
            {0xc000000e, "NO_SUCH_DEVICE"},
            {0xc000000f, "NO_SUCH_FILE"},
            {0xc0000010, "INVALID_DEVICE_REQUEST"},
            {0xc0000011, "END_OF_FILE"},
            {0xc0000012, "WRONG_VOLUME"},
            {0xc0000013, "NO_MEDIA_IN_DEVICE"},
            {0xc0000014, "UNRECOGNIZED_MEDIA"},
            {0xc0000015, "NONEXISTENT_SECTOR"},
            {0xc0000016, "MORE_PROCESSING_REQUIRED"},
            {0xc0000017, "NO_MEMORY"},
            {0xc0000018, "CONFLICTING_ADDRESSES"},
            {0xc0000019, "NOT_MAPPED_VIEW"},
            {0xc000001a, "UNABLE_TO_FREE_VM"},
            {0xc000001b, "UNABLE_TO_DELETE_SECTION"},
            {0xc000001c, "INVALID_SYSTEM_SERVICE"},
            {0xc000001d, "ILLEGAL_INSTRUCTION"},
            {0xc000001e, "INVALID_LOCK_SEQUENCE"},
            {0xc000001f, "INVALID_VIEW_SIZE"},
            {0xc0000020, "INVALID_FILE_FOR_SECTION"},
            {0xc0000021, "ALREADY_COMMITTED"},
            {0xc0000022, "ACCESS_DENIED"},
            {0xc0000023, "BUFFER_TOO_SMALL"},
            {0xc0000024, "OBJECT_TYPE_MISMATCH"},
            {0xc0000025, "NONCONTINUABLE_EXCEPTION"},
            {0xc0000026, "INVALID_DISPOSITION"},
            {0xc0000027, "UNWIND"},
            {0xc0000028, "BAD_STACK"},
            {0xc0000029, "INVALID_UNWIND_TARGET"},
            {0xc000002a, "NOT_LOCKED"},
            {0xc000002b, "PARITY_ERROR"},
            {0xc000002c, "UNABLE_TO_DECOMMIT_VM"},
            {0xc000002d, "NOT_COMMITTED"},
            {0xc000002e, "INVALID_PORT_ATTRIBUTES"},
            {0xc000002f, "PORT_MESSAGE_TOO_LONG"},
            {0xc0000030, "INVALID_PARAMETER_MIX"},
            {0xc0000031, "INVALID_QUOTA_LOWER"},
            {0xc0000032, "DISK_CORRUPT_ERROR"},
            {0xc0000033, "OBJECT_NAME_INVALID"},
            {0xc0000034, "OBJECT_NAME_NOT_FOUND"},
            {0xc0000035, "OBJECT_NAME_COLLISION"},
            {0xc0000037, "PORT_DISCONNECTED"},
            {0xc0000038, "DEVICE_ALREADY_ATTACHED"},
            {0xc0000039, "OBJECT_PATH_INVALID"},
            {0xc000003a, "OBJECT_PATH_NOT_FOUND"},
            {0xc000003b, "OBJECT_PATH_SYNTAX_BAD"},
            {0xc000003c, "DATA_OVERRUN"},
            {0xc000003d, "DATA_LATE_ERROR"},
            {0xc000003e, "DATA_ERROR"},
            {0xc000003f, "CRC_ERROR"},
            {0xc0000040, "SECTION_TOO_BIG"},
            {0xc0000041, "PORT_CONNECTION_REFUSED"},
            {0xc0000042, "INVALID_PORT_HANDLE"},
            {0xc0000043, "SHARING_VIOLATION"},
            {0xc0000044, "QUOTA_EXCEEDED"},
            {0xc0000045, "INVALID_PAGE_PROTECTION"},
            {0xc0000046, "MUTANT_NOT_OWNED"},
            {0xc0000047, "SEMAPHORE_LIMIT_EXCEEDED"},
            {0xc0000048, "PORT_ALREADY_SET"},
            {0xc0000049, "SECTION_NOT_IMAGE"},
            {0xc000004a, "SUSPEND_COUNT_EXCEEDED"},
            {0xc000004b, "THREAD_IS_TERMINATING"},
            {0xc000004c, "BAD_WORKING_SET_LIMIT"},
            {0xc000004d, "INCOMPATIBLE_FILE_MAP"},
            {0xc000004e, "SECTION_PROTECTION"},
            {0xc000004f, "EAS_NOT_SUPPORTED"},
            {0xc0000050, "EA_TOO_LARGE"},
            {0xc0000051, "NONEXISTENT_EA_ENTRY"},
            {0xc0000052, "NO_EAS_ON_FILE"},
            {0xc0000053, "EA_CORRUPT_ERROR"},
            {0xc0000054, "FILE_LOCK_CONFLICT"},
            {0xc0000055, "LOCK_NOT_GRANTED"},
            {0xc0000056, "DELETE_PENDING"},
            {0xc0000057, "CTL_FILE_NOT_SUPPORTED"},
            {0xc0000058, "UNKNOWN_REVISION"},
            {0xc0000059, "REVISION_MISMATCH"},
            {0xc000005a, "INVALID_OWNER"},
            {0xc000005b, "INVALID_PRIMARY_GROUP"},
            {0xc000005c, "NO_IMPERSONATION_TOKEN"},
            {0xc000005d, "CANT_DISABLE_MANDATORY"},
            {0xc000005e, "NO_LOGON_SERVERS"},
            {0xc000005f, "NO_SUCH_LOGON_SESSION"},
            {0xc0000060, "NO_SUCH_PRIVILEGE"},
            {0xc0000061, "PRIVILEGE_NOT_HELD"},
            {0xc0000062, "INVALID_ACCOUNT_NAME"},
            {0xc0000063, "USER_EXISTS"},
            {0xc0000064, "NO_SUCH_USER"},
            {0xc0000065, "GROUP_EXISTS"},
            {0xc0000066, "NO_SUCH_GROUP"},
            {0xc0000067, "MEMBER_IN_GROUP"},
            {0xc0000068, "MEMBER_NOT_IN_GROUP"},
            {0xc0000069, "LAST_ADMIN"},
            {0xc000006a, "WRONG_PASSWORD"},
            {0xc000006b, "ILL_FORMED_PASSWORD"},
            {0xc000006c, "PASSWORD_RESTRICTION"},
            {0xc000006d, "LOGON_FAILURE"},
            {0xc000006e, "ACCOUNT_RESTRICTION"},
            {0xc000006f, "INVALID_LOGON_HOURS"},
            {0xc0000070, "INVALID_WORKSTATION"},
            {0xc0000071, "PASSWORD_EXPIRED"},
            {0xc0000072, "ACCOUNT_DISABLED"},
            {0xc0000073, "NONE_MAPPED"},
            {0xc0000074, "TOO_MANY_LUIDS_REQUESTED"},
            {0xc0000075, "LUIDS_EXHAUSTED"},
            {0xc0000076, "INVALID_SUB_AUTHORITY"},
            {0xc0000077, "INVALID_ACL"},
            {0xc0000078, "INVALID_SID"},
            {0xc0000079, "INVALID_SECURITY_DESCR"},
            {0xc000007a, "PROCEDURE_NOT_FOUND"},
            {0xc000007b, "INVALID_IMAGE_FORMAT"},
            {0xc000007c, "NO_TOKEN"},
            {0xc000007d, "BAD_INHERITANCE_ACL"},
            {0xc000007e, "RANGE_NOT_LOCKED"},
            {0xc000007f, "DISK_FULL"},
            {0xc0000080, "SERVER_DISABLED"},
            {0xc0000081, "SERVER_NOT_DISABLED"},
            {0xc0000082, "TOO_MANY_GUIDS_REQUESTED"},
            {0xc0000083, "GUIDS_EXHAUSTED"},
            {0xc0000084, "INVALID_ID_AUTHORITY"},
            {0xc0000085, "AGENTS_EXHAUSTED"},
            {0xc0000086, "INVALID_VOLUME_LABEL"},
            {0xc0000087, "SECTION_NOT_EXTENDED"},
            {0xc0000088, "NOT_MAPPED_DATA"},
            {0xc0000089, "RESOURCE_DATA_NOT_FOUND"},
            {0xc000008a, "RESOURCE_TYPE_NOT_FOUND"},
            {0xc000008b, "RESOURCE_NAME_NOT_FOUND"},
            {0xc000008c, "ARRAY_BOUNDS_EXCEEDED"},
            {0xc000008d, "FLOAT_DENORMAL_OPERAND"},
            {0xc000008e, "FLOAT_DIVIDE_BY_ZERO"},
            {0xc000008f, "FLOAT_INEXACT_RESULT"},
            {0xc0000090, "FLOAT_INVALID_OPERATION"},
            {0xc0000091, "FLOAT_OVERFLOW"},
            {0xc0000092, "FLOAT_STACK_CHECK"},
            {0xc0000093, "FLOAT_UNDERFLOW"},
            {0xc0000094, "INTEGER_DIVIDE_BY_ZERO"},
            {0xc0000095, "INTEGER_OVERFLOW"},
            {0xc0000096, "PRIVILEGED_INSTRUCTION"},
            {0xc0000097, "TOO_MANY_PAGING_FILES"},
            {0xc0000098, "FILE_INVALID"},
            {0xc0000099, "ALLOTTED_SPACE_EXCEEDED"},
            {0xc000009a, "INSUFFICIENT_RESOURCES"},
            {0xc000009b, "DFS_EXIT_PATH_FOUND"},
            {0xc000009c, "DEVICE_DATA_ERROR"},
            {0xc000009d, "DEVICE_NOT_CONNECTED"},
            {0xc000009e, "DEVICE_POWER_FAILURE"},
            {0xc000009f, "FREE_VM_NOT_AT_BASE"},
            {0xc00000a0, "MEMORY_NOT_ALLOCATED"},
            {0xc00000a1, "WORKING_SET_QUOTA"},
            {0xc00000a2, "MEDIA_WRITE_PROTECTED"},
            {0xc00000a3, "DEVICE_NOT_READY"},
            {0xc00000a4, "INVALID_GROUP_ATTRIBUTES"},
            {0xc00000a5, "BAD_IMPERSONATION_LEVEL"},
            {0xc00000a6, "CANT_OPEN_ANONYMOUS"},
            {0xc00000a7, "BAD_VALIDATION_CLASS"},
            {0xc00000a8, "BAD_TOKEN_TYPE"},
            {0xc00000a9, "BAD_MASTER_BOOT_RECORD"},
            {0xc00000aa, "INSTRUCTION_MISALIGNMENT"},
            {0xc00000ab, "INSTANCE_NOT_AVAILABLE"},
            {0xc00000ac, "PIPE_NOT_AVAILABLE"},
            {0xc00000ad, "INVALID_PIPE_STATE"},
            {0xc00000ae, "PIPE_BUSY"},
            {0xc00000af, "ILLEGAL_FUNCTION"},
            {0xc00000b0, "PIPE_DISCONNECTED"},
            {0xc00000b1, "PIPE_CLOSING"},
            {0xc00000b2, "PIPE_CONNECTED"},
            {0xc00000b3, "PIPE_LISTENING"},
            {0xc00000b4, "INVALID_READ_MODE"},
            {0xc00000b5, "IO_TIMEOUT"},
            {0xc00000b6, "FILE_FORCED_CLOSED"},
            {0xc00000b7, "PROFILING_NOT_STARTED"},
            {0xc00000b8, "PROFILING_NOT_STOPPED"},
            {0xc00000b9, "COULD_NOT_INTERPRET"},
            {0xc00000ba, "FILE_IS_A_DIRECTORY"},
            {0xc00000bb, "NOT_SUPPORTED"},
            {0xc00000bc, "REMOTE_NOT_LISTENING"},
            {0xc00000bd, "DUPLICATE_NAME"},
            {0xc00000be, "BAD_NETWORK_PATH"},
            {0xc00000bf, "NETWORK_BUSY"},
            {0xc00000c0, "DEVICE_DOES_NOT_EXIST"},
            {0xc00000c1, "TOO_MANY_COMMANDS"},
            {0xc00000c2, "ADAPTER_HARDWARE_ERROR"},
            {0xc00000c3, "INVALID_NETWORK_RESPONSE"},
            {0xc00000c4, "UNEXPECTED_NETWORK_ERROR"},
            {0xc00000c5, "BAD_REMOTE_ADAPTER"},
            {0xc00000c6, "PRINT_QUEUE_FULL"},
            {0xc00000c7, "NO_SPOOL_SPACE"},
            {0xc00000c8, "PRINT_CANCELLED"},
            {0xc00000c9, "NETWORK_NAME_DELETED"},
            {0xc00000ca, "NETWORK_ACCESS_DENIED"},
            {0xc00000cb, "BAD_DEVICE_TYPE"},
            {0xc00000cc, "BAD_NETWORK_NAME"},
            {0xc00000cd, "TOO_MANY_NAMES"},
            {0xc00000ce, "TOO_MANY_SESSIONS"},
            {0xc00000cf, "SHARING_PAUSED"},
            {0xc00000d0, "REQUEST_NOT_ACCEPTED"},
            {0xc00000d1, "REDIRECTOR_PAUSED"},
            {0xc00000d2, "NET_WRITE_FAULT"},
            {0xc00000d3, "PROFILING_AT_LIMIT"},
            {0xc00000d4, "NOT_SAME_DEVICE"},
            {0xc00000d5, "FILE_RENAMED"},
            {0xc00000d6, "VIRTUAL_CIRCUIT_CLOSED"},
            {0xc00000d7, "NO_SECURITY_ON_OBJECT"},
            {0xc00000d8, "CANT_WAIT"},
            {0xc00000d9, "PIPE_EMPTY"},
            {0xc00000da, "CANT_ACCESS_DOMAIN_INFO"},
            {0xc00000db, "CANT_TERMINATE_SELF"},
            {0xc00000dc, "INVALID_SERVER_STATE"},
            {0xc00000dd, "INVALID_DOMAIN_STATE"},
            {0xc00000de, "INVALID_DOMAIN_ROLE"},
            {0xc00000df, "NO_SUCH_DOMAIN"},
            {0xc00000e0, "DOMAIN_EXISTS"},
            {0xc00000e1, "DOMAIN_LIMIT_EXCEEDED"},
            {0xc00000e2, "OPLOCK_NOT_GRANTED"},
            {0xc00000e3, "INVALID_OPLOCK_PROTOCOL"},
            {0xc00000e4, "INTERNAL_DB_CORRUPTION"},
            {0xc00000e5, "INTERNAL_ERROR"},
            {0xc00000e6, "GENERIC_NOT_MAPPED"},
            {0xc00000e7, "BAD_DESCRIPTOR_FORMAT"},
            {0xc00000e8, "INVALID_USER_BUFFER"},
            {0xc00000e9, "UNEXPECTED_IO_ERROR"},
            {0xc00000ea, "UNEXPECTED_MM_CREATE_ERR"},
            {0xc00000eb, "UNEXPECTED_MM_MAP_ERROR"},
            {0xc00000ec, "UNEXPECTED_MM_EXTEND_ERR"},
            {0xc00000ed, "NOT_LOGON_PROCESS"},
            {0xc00000ee, "LOGON_SESSION_EXISTS"},
            {0xc00000ef, "INVALID_PARAMETER_1"},
            {0xc00000f0, "INVALID_PARAMETER_2"},
            {0xc00000f1, "INVALID_PARAMETER_3"},
            {0xc00000f2, "INVALID_PARAMETER_4"},
            {0xc00000f3, "INVALID_PARAMETER_5"},
            {0xc00000f4, "INVALID_PARAMETER_6"},
            {0xc00000f5, "INVALID_PARAMETER_7"},
            {0xc00000f6, "INVALID_PARAMETER_8"},
            {0xc00000f7, "INVALID_PARAMETER_9"},
            {0xc00000f8, "INVALID_PARAMETER_10"},
            {0xc00000f9, "INVALID_PARAMETER_11"},
            {0xc00000fa, "INVALID_PARAMETER_12"},
            {0xc00000fb, "REDIRECTOR_NOT_STARTED"},
            {0xc00000fc, "REDIRECTOR_STARTED"},
            {0xc00000fd, "STACK_OVERFLOW"},
            {0xc00000fe, "NO_SUCH_PACKAGE"},
            {0xc00000ff, "BAD_FUNCTION_TABLE"},
            {0xc0000100, "VARIABLE_NOT_FOUND"},
            {0xc0000101, "DIRECTORY_NOT_EMPTY"},
            {0xc0000102, "FILE_CORRUPT_ERROR"},
            {0xc0000103, "NOT_A_DIRECTORY"},
            {0xc0000104, "BAD_LOGON_SESSION_STATE"},
            {0xc0000105, "LOGON_SESSION_COLLISION"},
            {0xc0000106, "NAME_TOO_LONG"},
            {0xc0000107, "FILES_OPEN"},
            {0xc0000108, "CONNECTION_IN_USE"},
            {0xc0000109, "MESSAGE_NOT_FOUND"},
            {0xc000010a, "PROCESS_IS_TERMINATING"},
            {0xc000010b, "INVALID_LOGON_TYPE"},
            {0xc000010c, "NO_GUID_TRANSLATION"},
            {0xc000010d, "CANNOT_IMPERSONATE"},
            {0xc000010e, "IMAGE_ALREADY_LOADED"},
            {0xc000010f, "ABIOS_NOT_PRESENT"},
            {0xc0000110, "ABIOS_LID_NOT_EXIST"},
            {0xc0000111, "ABIOS_LID_ALREADY_OWNED"},
            {0xc0000112, "ABIOS_NOT_LID_OWNER"},
            {0xc0000113, "ABIOS_INVALID_COMMAND"},
            {0xc0000114, "ABIOS_INVALID_LID"},
            {0xc0000115, "ABIOS_SELECTOR_NOT_AVAILABLE"},
            {0xc0000116, "ABIOS_INVALID_SELECTOR"},
            {0xc0000117, "NO_LDT"},
            {0xc0000118, "INVALID_LDT_SIZE"},
            {0xc0000119, "INVALID_LDT_OFFSET"},
            {0xc000011a, "INVALID_LDT_DESCRIPTOR"},
            {0xc000011b, "INVALID_IMAGE_NE_FORMAT"},
            {0xc000011c, "RXACT_INVALID_STATE"},
            {0xc000011d, "RXACT_COMMIT_FAILURE"},
            {0xc000011e, "MAPPED_FILE_SIZE_ZERO"},
            {0xc000011f, "TOO_MANY_OPENED_FILES"},
            {0xc0000120, "CANCELLED"},
            {0xc0000121, "CANNOT_DELETE"},
            {0xc0000122, "INVALID_COMPUTER_NAME"},
            {0xc0000123, "FILE_DELETED"},
            {0xc0000124, "SPECIAL_ACCOUNT"},
            {0xc0000125, "SPECIAL_GROUP"},
            {0xc0000126, "SPECIAL_USER"},
            {0xc0000127, "MEMBERS_PRIMARY_GROUP"},
            {0xc0000128, "FILE_CLOSED"},
            {0xc0000129, "TOO_MANY_THREADS"},
            {0xc000012a, "THREAD_NOT_IN_PROCESS"},
            {0xc000012b, "TOKEN_ALREADY_IN_USE"},
            {0xc000012c, "PAGEFILE_QUOTA_EXCEEDED"},
            {0xc000012d, "COMMITMENT_LIMIT"},
            {0xc000012e, "INVALID_IMAGE_LE_FORMAT"},
            {0xc000012f, "INVALID_IMAGE_NOT_MZ"},
            {0xc0000130, "INVALID_IMAGE_PROTECT"},
            {0xc0000131, "INVALID_IMAGE_WIN_16"},
            {0xc0000132, "LOGON_SERVER_CONFLICT"},
            {0xc0000133, "TIME_DIFFERENCE_AT_DC"},
            {0xc0000134, "SYNCHRONIZATION_REQUIRED"},
            {0xc0000135, "DLL_NOT_FOUND"},
            {0xc0000136, "OPEN_FAILED"},
            {0xc0000137, "IO_PRIVILEGE_FAILED"},
            {0xc0000138, "ORDINAL_NOT_FOUND"},
            {0xc0000139, "ENTRYPOINT_NOT_FOUND"},
            {0xc000013a, "CONTROL_C_EXIT"},
            {0xc000013b, "LOCAL_DISCONNECT"},
            {0xc000013c, "REMOTE_DISCONNECT"},
            {0xc000013d, "REMOTE_RESOURCES"},
            {0xc000013e, "LINK_FAILED"},
            {0xc000013f, "LINK_TIMEOUT"},
            {0xc0000140, "INVALID_CONNECTION"},
            {0xc0000141, "INVALID_ADDRESS"},
            {0xc0000142, "DLL_INIT_FAILED"},
            {0xc0000143, "MISSING_SYSTEMFILE"},
            {0xc0000144, "UNHANDLED_EXCEPTION"},
            {0xc0000145, "APP_INIT_FAILURE"},
            {0xc0000146, "PAGEFILE_CREATE_FAILED"},
            {0xc0000147, "NO_PAGEFILE"},
            {0xc0000148, "INVALID_LEVEL"},
            {0xc0000149, "WRONG_PASSWORD_CORE"},
            {0xc000014a, "ILLEGAL_FLOAT_CONTEXT"},
            {0xc000014b, "PIPE_BROKEN"},
            {0xc000014c, "REGISTRY_CORRUPT"},
            {0xc000014d, "REGISTRY_IO_FAILED"},
            {0xc000014e, "NO_EVENT_PAIR"},
            {0xc000014f, "UNRECOGNIZED_VOLUME"},
            {0xc0000150, "SERIAL_NO_DEVICE_INITED"},
            {0xc0000151, "NO_SUCH_ALIAS"},
            {0xc0000152, "MEMBER_NOT_IN_ALIAS"},
            {0xc0000153, "MEMBER_IN_ALIAS"},
            {0xc0000154, "ALIAS_EXISTS"},
            {0xc0000155, "LOGON_NOT_GRANTED"},
            {0xc0000156, "TOO_MANY_SECRETS"},
            {0xc0000157, "SECRET_TOO_LONG"},
            {0xc0000158, "INTERNAL_DB_ERROR"},
            {0xc0000159, "FULLSCREEN_MODE"},
            {0xc000015a, "TOO_MANY_CONTEXT_IDS"},
            {0xc000015b, "LOGON_TYPE_NOT_GRANTED"},
            {0xc000015c, "NOT_REGISTRY_FILE"},
            {0xc000015d, "NT_CROSS_ENCRYPTION_REQUIRED"},
            {0xc000015e, "DOMAIN_CTRLR_CONFIG_ERROR"},
            {0xc000015f, "FT_MISSING_MEMBER"},
            {0xc0000160, "ILL_FORMED_SERVICE_ENTRY"},
            {0xc0000161, "ILLEGAL_CHARACTER"},
            {0xc0000162, "UNMAPPABLE_CHARACTER"},
            {0xc0000163, "UNDEFINED_CHARACTER"},
            {0xc0000164, "FLOPPY_VOLUME"},
            {0xc0000165, "FLOPPY_ID_MARK_NOT_FOUND"},
            {0xc0000166, "FLOPPY_WRONG_CYLINDER"},
            {0xc0000167, "FLOPPY_UNKNOWN_ERROR"},
            {0xc0000168, "FLOPPY_BAD_REGISTERS"},
            {0xc0000169, "DISK_RECALIBRATE_FAILED"},
            {0xc000016a, "DISK_OPERATION_FAILED"},
            {0xc000016b, "DISK_RESET_FAILED"},
            {0xc000016c, "SHARED_IRQ_BUSY"},
            {0xc000016d, "FT_ORPHANING"},
            {0xc000016e, "BIOS_FAILED_TO_CONNECT_INTERRUPT"},
            {0xc0000172, "PARTITION_FAILURE"},
            {0xc0000173, "INVALID_BLOCK_LENGTH"},
            {0xc0000174, "DEVICE_NOT_PARTITIONED"},
            {0xc0000175, "UNABLE_TO_LOCK_MEDIA"},
            {0xc0000176, "UNABLE_TO_UNLOAD_MEDIA"},
            {0xc0000177, "EOM_OVERFLOW"},
            {0xc0000178, "NO_MEDIA"},
            {0xc000017a, "NO_SUCH_MEMBER"},
            {0xc000017b, "INVALID_MEMBER"},
            {0xc000017c, "KEY_DELETED"},
            {0xc000017d, "NO_LOG_SPACE"},
            {0xc000017e, "TOO_MANY_SIDS"},
            {0xc000017f, "LM_CROSS_ENCRYPTION_REQUIRED"},
            {0xc0000180, "KEY_HAS_CHILDREN"},
            {0xc0000181, "CHILD_MUST_BE_VOLATILE"},
            {0xc0000182, "DEVICE_CONFIGURATION_ERROR"},
            {0xc0000183, "DRIVER_INTERNAL_ERROR"},
            {0xc0000184, "INVALID_DEVICE_STATE"},
            {0xc0000185, "IO_DEVICE_ERROR"},
            {0xc0000186, "DEVICE_PROTOCOL_ERROR"},
            {0xc0000187, "BACKUP_CONTROLLER"},
            {0xc0000188, "LOG_FILE_FULL"},
            {0xc0000189, "TOO_LATE"},
            {0xc000018a, "NO_TRUST_LSA_SECRET"},
            {0xc000018b, "NO_TRUST_SAM_ACCOUNT"},
            {0xc000018c, "TRUSTED_DOMAIN_FAILURE"},
            {0xc000018d, "TRUSTED_RELATIONSHIP_FAILURE"},
            {0xc000018e, "EVENTLOG_FILE_CORRUPT"},
            {0xc000018f, "EVENTLOG_CANT_START"},
            {0xc0000190, "TRUST_FAILURE"},
            {0xc0000191, "MUTANT_LIMIT_EXCEEDED"},
            {0xc0000192, "NETLOGON_NOT_STARTED"},
            {0xc0000193, "ACCOUNT_EXPIRED"},
            {0xc0000194, "POSSIBLE_DEADLOCK"},
            {0xc0000195, "NETWORK_CREDENTIAL_CONFLICT"},
            {0xc0000196, "REMOTE_SESSION_LIMIT"},
            {0xc0000197, "EVENTLOG_FILE_CHANGED"},
            {0xc0000198, "NOLOGON_INTERDOMAIN_TRUST_ACCOUNT"},
            {0xc0000199, "NOLOGON_WORKSTATION_TRUST_ACCOUNT"},
            {0xc000019a, "NOLOGON_SERVER_TRUST_ACCOUNT"},
            {0xc000019b, "DOMAIN_TRUST_INCONSISTENT"},
            {0xc000019c, "FS_DRIVER_REQUIRED"},
            {0xc000019d, "IMAGE_ALREADY_LOADED_AS_DLL"},
            {0xc000019e, "INCOMPATIBLE_WITH_GLOBAL_SHORT_NAME_REGISTRY_SETTING"},
            {0xc000019f, "SHORT_NAMES_NOT_ENABLED_ON_VOLUME"},
            {0xc00001a0, "SECURITY_STREAM_IS_INCONSISTENT"},
            {0xc00001a1, "INVALID_LOCK_RANGE"},
            {0xc00001a2, "INVALID_ACE_CONDITION"},
            {0xc00001a3, "IMAGE_SUBSYSTEM_NOT_PRESENT"},
            {0xc00001a4, "NOTIFICATION_GUID_ALREADY_DEFINED"},
            {0xc0000201, "NETWORK_OPEN_RESTRICTION"},
            {0xc0000202, "NO_USER_SESSION_KEY"},
            {0xc0000203, "USER_SESSION_DELETED"},
            {0xc0000204, "RESOURCE_LANG_NOT_FOUND"},
            {0xc0000205, "INSUFF_SERVER_RESOURCES"},
            {0xc0000206, "INVALID_BUFFER_SIZE"},
            {0xc0000207, "INVALID_ADDRESS_COMPONENT"},
            {0xc0000208, "INVALID_ADDRESS_WILDCARD"},
            {0xc0000209, "TOO_MANY_ADDRESSES"},
            {0xc000020a, "ADDRESS_ALREADY_EXISTS"},
            {0xc000020b, "ADDRESS_CLOSED"},
            {0xc000020c, "CONNECTION_DISCONNECTED"},
            {0xc000020d, "CONNECTION_RESET"},
            {0xc000020e, "TOO_MANY_NODES"},
            {0xc000020f, "TRANSACTION_ABORTED"},
            {0xc0000210, "TRANSACTION_TIMED_OUT"},
            {0xc0000211, "TRANSACTION_NO_RELEASE"},
            {0xc0000212, "TRANSACTION_NO_MATCH"},
            {0xc0000213, "TRANSACTION_RESPONDED"},
            {0xc0000214, "TRANSACTION_INVALID_ID"},
            {0xc0000215, "TRANSACTION_INVALID_TYPE"},
            {0xc0000216, "NOT_SERVER_SESSION"},
            {0xc0000217, "NOT_CLIENT_SESSION"},
            {0xc0000218, "CANNOT_LOAD_REGISTRY_FILE"},
            {0xc0000219, "DEBUG_ATTACH_FAILED"},
            {0xc000021a, "SYSTEM_PROCESS_TERMINATED"},
            {0xc000021b, "DATA_NOT_ACCEPTED"},
            {0xc000021c, "NO_BROWSER_SERVERS_FOUND"},
            {0xc000021d, "VDM_HARD_ERROR"},
            {0xc000021e, "DRIVER_CANCEL_TIMEOUT"},
            {0xc000021f, "REPLY_MESSAGE_MISMATCH"},
            {0xc0000220, "MAPPED_ALIGNMENT"},
            {0xc0000221, "IMAGE_CHECKSUM_MISMATCH"},
            {0xc0000222, "LOST_WRITEBEHIND_DATA"},
            {0xc0000223, "CLIENT_SERVER_PARAMETERS_INVALID"},
            {0xc0000224, "PASSWORD_MUST_CHANGE"},
            {0xc0000225, "NOT_FOUND"},
            {0xc0000226, "NOT_TINY_STREAM"},
            {0xc0000227, "RECOVERY_FAILURE"},
            {0xc0000228, "STACK_OVERFLOW_READ"},
            {0xc0000229, "FAIL_CHECK"},
            {0xc000022a, "DUPLICATE_OBJECTID"},
            {0xc000022b, "OBJECTID_EXISTS"},
            {0xc000022c, "CONVERT_TO_LARGE"},
            {0xc000022d, "RETRY"},
            {0xc000022e, "FOUND_OUT_OF_SCOPE"},
            {0xc000022f, "ALLOCATE_BUCKET"},
            {0xc0000230, "PROPSET_NOT_FOUND"},
            {0xc0000231, "MARSHALL_OVERFLOW"},
            {0xc0000232, "INVALID_VARIANT"},
            {0xc0000233, "DOMAIN_CONTROLLER_NOT_FOUND"},
            {0xc0000234, "ACCOUNT_LOCKED_OUT"},
            {0xc0000235, "HANDLE_NOT_CLOSABLE"},
            {0xc0000236, "CONNECTION_REFUSED"},
            {0xc0000237, "GRACEFUL_DISCONNECT"},
            {0xc0000238, "ADDRESS_ALREADY_ASSOCIATED"},
            {0xc0000239, "ADDRESS_NOT_ASSOCIATED"},
            {0xc000023a, "CONNECTION_INVALID"},
            {0xc000023b, "CONNECTION_ACTIVE"},
            {0xc000023c, "NETWORK_UNREACHABLE"},
            {0xc000023d, "HOST_UNREACHABLE"},
            {0xc000023e, "PROTOCOL_UNREACHABLE"},
            {0xc000023f, "PORT_UNREACHABLE"},
            {0xc0000240, "REQUEST_ABORTED"},
            {0xc0000241, "CONNECTION_ABORTED"},
            {0xc0000242, "BAD_COMPRESSION_BUFFER"},
            {0xc0000243, "USER_MAPPED_FILE"},
            {0xc0000244, "AUDIT_FAILED"},
            {0xc0000245, "TIMER_RESOLUTION_NOT_SET"},
            {0xc0000246, "CONNECTION_COUNT_LIMIT"},
            {0xc0000247, "LOGIN_TIME_RESTRICTION"},
            {0xc0000248, "LOGIN_WKSTA_RESTRICTION"},
            {0xc0000249, "IMAGE_MP_UP_MISMATCH"},
            {0xc0000250, "INSUFFICIENT_LOGON_INFO"},
            {0xc0000251, "BAD_DLL_ENTRYPOINT"},
            {0xc0000252, "BAD_SERVICE_ENTRYPOINT"},
            {0xc0000253, "LPC_REPLY_LOST"},
            {0xc0000254, "IP_ADDRESS_CONFLICT1"},
            {0xc0000255, "IP_ADDRESS_CONFLICT2"},
            {0xc0000256, "REGISTRY_QUOTA_LIMIT"},
            {0xc0000257, "PATH_NOT_COVERED"},
            {0xc0000258, "NO_CALLBACK_ACTIVE"},
            {0xc0000259, "LICENSE_QUOTA_EXCEEDED"},
            {0xc000025a, "PWD_TOO_SHORT"},
            {0xc000025b, "PWD_TOO_RECENT"},
            {0xc000025c, "PWD_HISTORY_CONFLICT"},
            {0xc000025e, "PLUGPLAY_NO_DEVICE"},
            {0xc000025f, "UNSUPPORTED_COMPRESSION"},
            {0xc0000260, "INVALID_HW_PROFILE"},
            {0xc0000261, "INVALID_PLUGPLAY_DEVICE_PATH"},
            {0xc0000262, "DRIVER_ORDINAL_NOT_FOUND"},
            {0xc0000263, "DRIVER_ENTRYPOINT_NOT_FOUND"},
            {0xc0000264, "RESOURCE_NOT_OWNED"},
            {0xc0000265, "TOO_MANY_LINKS"},
            {0xc0000266, "QUOTA_LIST_INCONSISTENT"},
            {0xc0000267, "FILE_IS_OFFLINE"},
            {0xc0000268, "EVALUATION_EXPIRATION"},
            {0xc0000269, "ILLEGAL_DLL_RELOCATION"},
            {0xc000026a, "LICENSE_VIOLATION"},
            {0xc000026b, "DLL_INIT_FAILED_LOGOFF"},
            {0xc000026c, "DRIVER_UNABLE_TO_LOAD"},
            {0xc000026d, "DFS_UNAVAILABLE"},
            {0xc000026e, "VOLUME_DISMOUNTED"},
            {0xc000026f, "WX86_INTERNAL_ERROR"},
            {0xc0000270, "WX86_FLOAT_STACK_CHECK"},
            {0xc0000271, "VALIDATE_CONTINUE"},
            {0xc0000272, "NO_MATCH"},
            {0xc0000273, "NO_MORE_MATCHES"},
            {0xc0000275, "NOT_A_REPARSE_POINT"},
            {0xc0000276, "IO_REPARSE_TAG_INVALID"},
            {0xc0000277, "IO_REPARSE_TAG_MISMATCH"},
            {0xc0000278, "IO_REPARSE_DATA_INVALID"},
            {0xc0000279, "IO_REPARSE_TAG_NOT_HANDLED"},
            {0xc0000280, "REPARSE_POINT_NOT_RESOLVED"},
            {0xc0000281, "DIRECTORY_IS_A_REPARSE_POINT"},
            {0xc0000282, "RANGE_LIST_CONFLICT"},
            {0xc0000283, "SOURCE_ELEMENT_EMPTY"},
            {0xc0000284, "DESTINATION_ELEMENT_FULL"},
            {0xc0000285, "ILLEGAL_ELEMENT_ADDRESS"},
            {0xc0000286, "MAGAZINE_NOT_PRESENT"},
            {0xc0000287, "REINITIALIZATION_NEEDED"},
            {0xc000028a, "ENCRYPTION_FAILED"},
            {0xc000028b, "DECRYPTION_FAILED"},
            {0xc000028c, "RANGE_NOT_FOUND"},
            {0xc000028d, "NO_RECOVERY_POLICY"},
            {0xc000028e, "NO_EFS"},
            {0xc000028f, "WRONG_EFS"},
            {0xc0000290, "NO_USER_KEYS"},
            {0xc0000291, "FILE_NOT_ENCRYPTED"},
            {0xc0000292, "NOT_EXPORT_FORMAT"},
            {0xc0000293, "FILE_ENCRYPTED"},
            {0xc0000295, "WMI_GUID_NOT_FOUND"},
            {0xc0000296, "WMI_INSTANCE_NOT_FOUND"},
            {0xc0000297, "WMI_ITEMID_NOT_FOUND"},
            {0xc0000298, "WMI_TRY_AGAIN"},
            {0xc0000299, "SHARED_POLICY"},
            {0xc000029a, "POLICY_OBJECT_NOT_FOUND"},
            {0xc000029b, "POLICY_ONLY_IN_DS"},
            {0xc000029c, "VOLUME_NOT_UPGRADED"},
            {0xc000029d, "REMOTE_STORAGE_NOT_ACTIVE"},
            {0xc000029e, "REMOTE_STORAGE_MEDIA_ERROR"},
            {0xc000029f, "NO_TRACKING_SERVICE"},
            {0xc00002a0, "SERVER_SID_MISMATCH"},
            {0xc00002a1, "DS_NO_ATTRIBUTE_OR_VALUE"},
            {0xc00002a2, "DS_INVALID_ATTRIBUTE_SYNTAX"},
            {0xc00002a3, "DS_ATTRIBUTE_TYPE_UNDEFINED"},
            {0xc00002a4, "DS_ATTRIBUTE_OR_VALUE_EXISTS"},
            {0xc00002a5, "DS_BUSY"},
            {0xc00002a6, "DS_UNAVAILABLE"},
            {0xc00002a7, "DS_NO_RIDS_ALLOCATED"},
            {0xc00002a8, "DS_NO_MORE_RIDS"},
            {0xc00002a9, "DS_INCORRECT_ROLE_OWNER"},
            {0xc00002aa, "DS_RIDMGR_INIT_ERROR"},
            {0xc00002ab, "DS_OBJ_CLASS_VIOLATION"},
            {0xc00002ac, "DS_CANT_ON_NON_LEAF"},
            {0xc00002ad, "DS_CANT_ON_RDN"},
            {0xc00002ae, "DS_CANT_MOD_OBJ_CLASS"},
            {0xc00002af, "DS_CROSS_DOM_MOVE_FAILED"},
            {0xc00002b0, "DS_GC_NOT_AVAILABLE"},
            {0xc00002b1, "DIRECTORY_SERVICE_REQUIRED"},
            {0xc00002b2, "REPARSE_ATTRIBUTE_CONFLICT"},
            {0xc00002b3, "CANT_ENABLE_DENY_ONLY"},
            {0xc00002b4, "FLOAT_MULTIPLE_FAULTS"},
            {0xc00002b5, "FLOAT_MULTIPLE_TRAPS"},
            {0xc00002b6, "DEVICE_REMOVED"},
            {0xc00002b7, "JOURNAL_DELETE_IN_PROGRESS"},
            {0xc00002b8, "JOURNAL_NOT_ACTIVE"},
            {0xc00002b9, "NOINTERFACE"},
            {0xc00002c1, "DS_ADMIN_LIMIT_EXCEEDED"},
            {0xc00002c2, "DRIVER_FAILED_SLEEP"},
            {0xc00002c3, "MUTUAL_AUTHENTICATION_FAILED"},
            {0xc00002c4, "CORRUPT_SYSTEM_FILE"},
            {0xc00002c5, "DATATYPE_MISALIGNMENT_ERROR"},
            {0xc00002c6, "WMI_READ_ONLY"},
            {0xc00002c7, "WMI_SET_FAILURE"},
            {0xc00002c8, "COMMITMENT_MINIMUM"},
            {0xc00002c9, "REG_NAT_CONSUMPTION"},
            {0xc00002ca, "TRANSPORT_FULL"},
            {0xc00002cb, "DS_SAM_INIT_FAILURE"},
            {0xc00002cc, "ONLY_IF_CONNECTED"},
            {0xc00002cd, "DS_SENSITIVE_GROUP_VIOLATION"},
            {0xc00002ce, "PNP_RESTART_ENUMERATION"},
            {0xc00002cf, "JOURNAL_ENTRY_DELETED"},
            {0xc00002d0, "DS_CANT_MOD_PRIMARYGROUPID"},
            {0xc00002d1, "SYSTEM_IMAGE_BAD_SIGNATURE"},
            {0xc00002d2, "PNP_REBOOT_REQUIRED"},
            {0xc00002d3, "POWER_STATE_INVALID"},
            {0xc00002d4, "DS_INVALID_GROUP_TYPE"},
            {0xc00002d5, "DS_NO_NEST_GLOBALGROUP_IN_MIXEDDOMAIN"},
            {0xc00002d6, "DS_NO_NEST_LOCALGROUP_IN_MIXEDDOMAIN"},
            {0xc00002d7, "DS_GLOBAL_CANT_HAVE_LOCAL_MEMBER"},
            {0xc00002d8, "DS_GLOBAL_CANT_HAVE_UNIVERSAL_MEMBER"},
            {0xc00002d9, "DS_UNIVERSAL_CANT_HAVE_LOCAL_MEMBER"},
            {0xc00002da, "DS_GLOBAL_CANT_HAVE_CROSSDOMAIN_MEMBER"},
            {0xc00002db, "DS_LOCAL_CANT_HAVE_CROSSDOMAIN_LOCAL_MEMBER"},
            {0xc00002dc, "DS_HAVE_PRIMARY_MEMBERS"},
            {0xc00002dd, "WMI_NOT_SUPPORTED"},
            {0xc00002de, "INSUFFICIENT_POWER"},
            {0xc00002df, "SAM_NEED_BOOTKEY_PASSWORD"},
            {0xc00002e0, "SAM_NEED_BOOTKEY_FLOPPY"},
            {0xc00002e1, "DS_CANT_START"},
            {0xc00002e2, "DS_INIT_FAILURE"},
            {0xc00002e3, "SAM_INIT_FAILURE"},
            {0xc00002e4, "DS_GC_REQUIRED"},
            {0xc00002e5, "DS_LOCAL_MEMBER_OF_LOCAL_ONLY"},
            {0xc00002e6, "DS_NO_FPO_IN_UNIVERSAL_GROUPS"},
            {0xc00002e7, "DS_MACHINE_ACCOUNT_QUOTA_EXCEEDED"},
            {0xc00002e8, "MULTIPLE_FAULT_VIOLATION"},
            {0xc00002e9, "CURRENT_DOMAIN_NOT_ALLOWED"},
            {0xc00002ea, "CANNOT_MAKE"},
            {0xc00002eb, "SYSTEM_SHUTDOWN"},
            {0xc00002ec, "DS_INIT_FAILURE_CONSOLE"},
            {0xc00002ed, "DS_SAM_INIT_FAILURE_CONSOLE"},
            {0xc00002ee, "UNFINISHED_CONTEXT_DELETED"},
            {0xc00002ef, "NO_TGT_REPLY"},
            {0xc00002f0, "OBJECTID_NOT_FOUND"},
            {0xc00002f1, "NO_IP_ADDRESSES"},
            {0xc00002f2, "WRONG_CREDENTIAL_HANDLE"},
            {0xc00002f3, "CRYPTO_SYSTEM_INVALID"},
            {0xc00002f4, "MAX_REFERRALS_EXCEEDED"},
            {0xc00002f5, "MUST_BE_KDC"},
            {0xc00002f6, "STRONG_CRYPTO_NOT_SUPPORTED"},
            {0xc00002f7, "TOO_MANY_PRINCIPALS"},
            {0xc00002f8, "NO_PA_DATA"},
            {0xc00002f9, "PKINIT_NAME_MISMATCH"},
            {0xc00002fa, "SMARTCARD_LOGON_REQUIRED"},
            {0xc00002fb, "KDC_INVALID_REQUEST"},
            {0xc00002fc, "KDC_UNABLE_TO_REFER"},
            {0xc00002fd, "KDC_UNKNOWN_ETYPE"},
            {0xc00002fe, "SHUTDOWN_IN_PROGRESS"},
            {0xc00002ff, "SERVER_SHUTDOWN_IN_PROGRESS"},
            {0xc0000300, "NOT_SUPPORTED_ON_SBS"},
            {0xc0000301, "WMI_GUID_DISCONNECTED"},
            {0xc0000302, "WMI_ALREADY_DISABLED"},
            {0xc0000303, "WMI_ALREADY_ENABLED"},
            {0xc0000304, "MFT_TOO_FRAGMENTED"},
            {0xc0000305, "COPY_PROTECTION_FAILURE"},
            {0xc0000306, "CSS_AUTHENTICATION_FAILURE"},
            {0xc0000307, "CSS_KEY_NOT_PRESENT"},
            {0xc0000308, "CSS_KEY_NOT_ESTABLISHED"},
            {0xc0000309, "CSS_SCRAMBLED_SECTOR"},
            {0xc000030a, "CSS_REGION_MISMATCH"},
            {0xc000030b, "CSS_RESETS_EXHAUSTED"},
            {0xc0000320, "PKINIT_FAILURE"},
            {0xc0000321, "SMARTCARD_SUBSYSTEM_FAILURE"},
            {0xc0000322, "NO_KERB_KEY"},
            {0xc0000350, "HOST_DOWN"},
            {0xc0000351, "UNSUPPORTED_PREAUTH"},
            {0xc0000352, "EFS_ALG_BLOB_TOO_BIG"},
            {0xc0000353, "PORT_NOT_SET"},
            {0xc0000354, "DEBUGGER_INACTIVE"},
            {0xc0000355, "DS_VERSION_CHECK_FAILURE"},
            {0xc0000356, "AUDITING_DISABLED"},
            {0xc0000357, "PRENT4_MACHINE_ACCOUNT"},
            {0xc0000358, "DS_AG_CANT_HAVE_UNIVERSAL_MEMBER"},
            {0xc0000359, "INVALID_IMAGE_WIN_32"},
            {0xc000035a, "INVALID_IMAGE_WIN_64"},
            {0xc000035b, "BAD_BINDINGS"},
            {0xc000035c, "NETWORK_SESSION_EXPIRED"},
            {0xc000035d, "APPHELP_BLOCK"},
            {0xc000035e, "ALL_SIDS_FILTERED"},
            {0xc000035f, "NOT_SAFE_MODE_DRIVER"},
            {0xc0000361, "ACCESS_DISABLED_BY_POLICY_DEFAULT"},
            {0xc0000362, "ACCESS_DISABLED_BY_POLICY_PATH"},
            {0xc0000363, "ACCESS_DISABLED_BY_POLICY_PUBLISHER"},
            {0xc0000364, "ACCESS_DISABLED_BY_POLICY_OTHER"},
            {0xc0000365, "FAILED_DRIVER_ENTRY"},
            {0xc0000366, "DEVICE_ENUMERATION_ERROR"},
            {0xc0000368, "MOUNT_POINT_NOT_RESOLVED"},
            {0xc0000369, "INVALID_DEVICE_OBJECT_PARAMETER"},
            {0xc000036a, "MCA_OCCURED"},
            {0xc000036b, "DRIVER_BLOCKED_CRITICAL"},
            {0xc000036c, "DRIVER_BLOCKED"},
            {0xc000036d, "DRIVER_DATABASE_ERROR"},
            {0xc000036e, "SYSTEM_HIVE_TOO_LARGE"},
            {0xc000036f, "INVALID_IMPORT_OF_NON_DLL"},
            {0xc0000371, "NO_SECRETS"},
            {0xc0000372, "ACCESS_DISABLED_NO_SAFER_UI_BY_POLICY"},
            {0xc0000373, "FAILED_STACK_SWITCH"},
            {0xc0000374, "HEAP_CORRUPTION"},
            {0xc0000380, "SMARTCARD_WRONG_PIN"},
            {0xc0000381, "SMARTCARD_CARD_BLOCKED"},
            {0xc0000382, "SMARTCARD_CARD_NOT_AUTHENTICATED"},
            {0xc0000383, "SMARTCARD_NO_CARD"},
            {0xc0000384, "SMARTCARD_NO_KEY_CONTAINER"},
            {0xc0000385, "SMARTCARD_NO_CERTIFICATE"},
            {0xc0000386, "SMARTCARD_NO_KEYSET"},
            {0xc0000387, "SMARTCARD_IO_ERROR"},
            {0xc0000388, "DOWNGRADE_DETECTED"},
            {0xc0000389, "SMARTCARD_CERT_REVOKED"},
            {0xc000038a, "ISSUING_CA_UNTRUSTED"},
            {0xc000038b, "REVOCATION_OFFLINE_C"},
            {0xc000038c, "PKINIT_CLIENT_FAILURE"},
            {0xc000038d, "SMARTCARD_CERT_EXPIRED"},
            {0xc000038e, "DRIVER_FAILED_PRIOR_UNLOAD"},
            {0xc000038f, "SMARTCARD_SILENT_CONTEXT"},
            {0xc0000401, "PER_USER_TRUST_QUOTA_EXCEEDED"},
            {0xc0000402, "ALL_USER_TRUST_QUOTA_EXCEEDED"},
            {0xc0000403, "USER_DELETE_TRUST_QUOTA_EXCEEDED"},
            {0xc0000404, "DS_NAME_NOT_UNIQUE"},
            {0xc0000405, "DS_DUPLICATE_ID_FOUND"},
            {0xc0000406, "DS_GROUP_CONVERSION_ERROR"},
            {0xc0000407, "VOLSNAP_PREPARE_HIBERNATE"},
            {0xc0000408, "USER2USER_REQUIRED"},
            {0xc0000409, "STACK_BUFFER_OVERRUN"},
            {0xc000040a, "NO_S4U_PROT_SUPPORT"},
            {0xc000040b, "CROSSREALM_DELEGATION_FAILURE"},
            {0xc000040c, "REVOCATION_OFFLINE_KDC"},
            {0xc000040d, "ISSUING_CA_UNTRUSTED_KDC"},
            {0xc000040e, "KDC_CERT_EXPIRED"},
            {0xc000040f, "KDC_CERT_REVOKED"},
            {0xc0000410, "PARAMETER_QUOTA_EXCEEDED"},
            {0xc0000411, "HIBERNATION_FAILURE"},
            {0xc0000412, "DELAY_LOAD_FAILED"},
            {0xc0000413, "AUTHENTICATION_FIREWALL_FAILED"},
            {0xc0000414, "VDM_DISALLOWED"},
            {0xc0000415, "HUNG_DISPLAY_DRIVER_THREAD"},
            {0xc0000416, "INSUFFICIENT_RESOURCE_FOR_SPECIFIED_SHARED_SECTION_SIZE"},
            {0xc0000417, "INVALID_CRUNTIME_PARAMETER"},
            {0xc0000418, "NTLM_BLOCKED"},
            {0xc0000419, "DS_SRC_SID_EXISTS_IN_FOREST"},
            {0xc000041a, "DS_DOMAIN_NAME_EXISTS_IN_FOREST"},
            {0xc000041b, "DS_FLAT_NAME_EXISTS_IN_FOREST"},
            {0xc000041c, "INVALID_USER_PRINCIPAL_NAME"},
            {0xc000041d, "FATAL_USER_CALLBACK_EXCEPTION"},
            {0xc0000420, "ASSERTION_FAILURE"},
            {0xc0000421, "VERIFIER_STOP"},
            {0xc0000423, "CALLBACK_POP_STACK"},
            {0xc0000424, "INCOMPATIBLE_DRIVER_BLOCKED"},
            {0xc0000425, "HIVE_UNLOADED"},
            {0xc0000426, "COMPRESSION_DISABLED"},
            {0xc0000427, "FILE_SYSTEM_LIMITATION"},
            {0xc0000428, "INVALID_IMAGE_HASH"},
            {0xc0000429, "NOT_CAPABLE"},
            {0xc000042a, "REQUEST_OUT_OF_SEQUENCE"},
            {0xc000042b, "IMPLEMENTATION_LIMIT"},
            {0xc000042c, "ELEVATION_REQUIRED"},
            {0xc000042d, "NO_SECURITY_CONTEXT"},
            {0xc000042e, "PKU2U_CERT_FAILURE"},
            {0xc0000432, "BEYOND_VDL"},
            {0xc0000433, "ENCOUNTERED_WRITE_IN_PROGRESS"},
            {0xc0000434, "PTE_CHANGED"},
            {0xc0000435, "PURGE_FAILED"},
            {0xc0000440, "CRED_REQUIRES_CONFIRMATION"},
            {0xc0000441, "CS_ENCRYPTION_INVALID_SERVER_RESPONSE"},
            {0xc0000442, "CS_ENCRYPTION_UNSUPPORTED_SERVER"},
            {0xc0000443, "CS_ENCRYPTION_EXISTING_ENCRYPTED_FILE"},
            {0xc0000444, "CS_ENCRYPTION_NEW_ENCRYPTED_FILE"},
            {0xc0000445, "CS_ENCRYPTION_FILE_NOT_CSE"},
            {0xc0000446, "INVALID_LABEL"},
            {0xc0000450, "DRIVER_PROCESS_TERMINATED"},
            {0xc0000451, "AMBIGUOUS_SYSTEM_DEVICE"},
            {0xc0000452, "SYSTEM_DEVICE_NOT_FOUND"},
            {0xc0000453, "RESTART_BOOT_APPLICATION"},
            {0xc0000454, "INSUFFICIENT_NVRAM_RESOURCES"},
            {0xc0000460, "NO_RANGES_PROCESSED"},
            {0xc0000463, "DEVICE_FEATURE_NOT_SUPPORTED"},
            {0xc0000464, "DEVICE_UNREACHABLE"},
            {0xc0000465, "INVALID_TOKEN"},
            {0xc0000467, "FILE_NOT_AVAILABLE"},
            {0xc0000500, "INVALID_TASK_NAME"},
            {0xc0000501, "INVALID_TASK_INDEX"},
            {0xc0000502, "THREAD_ALREADY_IN_TASK"},
            {0xc0000503, "CALLBACK_BYPASS"},
            {0xc0000602, "FAIL_FAST_EXCEPTION"},
            {0xc0000603, "IMAGE_CERT_REVOKED"},
            {0xc0000700, "PORT_CLOSED"},
            {0xc0000701, "MESSAGE_LOST"},
            {0xc0000702, "INVALID_MESSAGE"},
            {0xc0000703, "REQUEST_CANCELED"},
            {0xc0000704, "RECURSIVE_DISPATCH"},
            {0xc0000705, "LPC_RECEIVE_BUFFER_EXPECTED"},
            {0xc0000706, "LPC_INVALID_CONNECTION_USAGE"},
            {0xc0000707, "LPC_REQUESTS_NOT_ALLOWED"},
            {0xc0000708, "RESOURCE_IN_USE"},
            {0xc0000709, "HARDWARE_MEMORY_ERROR"},
            {0xc000070a, "THREADPOOL_HANDLE_EXCEPTION"},
            {0xc000070b, "THREADPOOL_SET_EVENT_ON_COMPLETION_FAILED"},
            {0xc000070c, "THREADPOOL_RELEASE_SEMAPHORE_ON_COMPLETION_FAILED"},
            {0xc000070d, "THREADPOOL_RELEASE_MUTEX_ON_COMPLETION_FAILED"},
            {0xc000070e, "THREADPOOL_FREE_LIBRARY_ON_COMPLETION_FAILED"},
            {0xc000070f, "THREADPOOL_RELEASED_DURING_OPERATION"},
            {0xc0000710, "CALLBACK_RETURNED_WHILE_IMPERSONATING"},
            {0xc0000711, "APC_RETURNED_WHILE_IMPERSONATING"},
            {0xc0000712, "PROCESS_IS_PROTECTED"},
            {0xc0000713, "MCA_EXCEPTION"},
            {0xc0000714, "CERTIFICATE_MAPPING_NOT_UNIQUE"},
            {0xc0000715, "SYMLINK_CLASS_DISABLED"},
            {0xc0000716, "INVALID_IDN_NORMALIZATION"},
            {0xc0000717, "NO_UNICODE_TRANSLATION"},
            {0xc0000718, "ALREADY_REGISTERED"},
            {0xc0000719, "CONTEXT_MISMATCH"},
            {0xc000071a, "PORT_ALREADY_HAS_COMPLETION_LIST"},
            {0xc000071b, "CALLBACK_RETURNED_THREAD_PRIORITY"},
            {0xc000071c, "INVALID_THREAD"},
            {0xc000071d, "CALLBACK_RETURNED_TRANSACTION"},
            {0xc000071e, "CALLBACK_RETURNED_LDR_LOCK"},
            {0xc000071f, "CALLBACK_RETURNED_LANG"},
            {0xc0000720, "CALLBACK_RETURNED_PRI_BACK"},
            {0xc0000721, "CALLBACK_RETURNED_THREAD_AFFINITY"},
            {0xc0000800, "DISK_REPAIR_DISABLED"},
            {0xc0000801, "DS_DOMAIN_RENAME_IN_PROGRESS"},
            {0xc0000802, "DISK_QUOTA_EXCEEDED"},
            {0xc0000804, "CONTENT_BLOCKED"},
            {0xc0000805, "BAD_CLUSTERS"},
            {0xc0000806, "VOLUME_DIRTY"},
            {0xc0000901, "FILE_CHECKED_OUT"},
            {0xc0000902, "CHECKOUT_REQUIRED"},
            {0xc0000903, "BAD_FILE_TYPE"},
            {0xc0000904, "FILE_TOO_LARGE"},
            {0xc0000905, "FORMS_AUTH_REQUIRED"},
            {0xc0000906, "VIRUS_INFECTED"},
            {0xc0000907, "VIRUS_DELETED"},
            {0xc0000908, "BAD_MCFG_TABLE"},
            {0xc0000909, "CANNOT_BREAK_OPLOCK"},
            {0xc0009898, "WOW_ASSERTION"},
            {0xc000a000, "INVALID_SIGNATURE"},
            {0xc000a001, "HMAC_NOT_SUPPORTED"},
            {0xc000a010, "IPSEC_QUEUE_OVERFLOW"},
            {0xc000a011, "ND_QUEUE_OVERFLOW"},
            {0xc000a012, "HOPLIMIT_EXCEEDED"},
            {0xc000a013, "PROTOCOL_NOT_SUPPORTED"},
            {0xc000a080, "LOST_WRITEBEHIND_DATA_NETWORK_DISCONNECTED"},
            {0xc000a081, "LOST_WRITEBEHIND_DATA_NETWORK_SERVER_ERROR"},
            {0xc000a082, "LOST_WRITEBEHIND_DATA_LOCAL_DISK_ERROR"},
            {0xc000a083, "XML_PARSE_ERROR"},
            {0xc000a084, "XMLDSIG_ERROR"},
            {0xc000a085, "WRONG_COMPARTMENT"},
            {0xc000a086, "AUTHIP_FAILURE"},
            {0xc000a087, "DS_OID_MAPPED_GROUP_CANT_HAVE_MEMBERS"},
            {0xc000a088, "DS_OID_NOT_FOUND"},
            {0xc000a100, "HASH_NOT_SUPPORTED"},
            {0xc000a101, "HASH_NOT_PRESENT"},
            {0xc000a2a1, "OFFLOAD_READ_FLT_NOT_SUPPORTED"},
            {0xc000a2a2, "OFFLOAD_WRITE_FLT_NOT_SUPPORTED"},
            {0xc000a2a3, "OFFLOAD_READ_FILE_NOT_SUPPORTED"},
            {0xc000a2a4, "OFFLOAD_WRITE_FILE_NOT_SUPPORTED"},
            {0xc0010001, "NO_STATE_CHANGE"},
            {0xc0010002, "APP_NOT_IDLE"},
            {0xc0020001, "NT_INVALID_STRING_BINDING"},
            {0xc0020002, "NT_WRONG_KIND_OF_BINDING"},
            {0xc0020003, "NT_INVALID_BINDING"},
            {0xc0020004, "NT_PROTSEQ_NOT_SUPPORTED"},
            {0xc0020005, "NT_INVALID_RPC_PROTSEQ"},
            {0xc0020006, "NT_INVALID_STRING_UUID"},
            {0xc0020007, "NT_INVALID_ENDPOINT_FORMAT"},
            {0xc0020008, "NT_INVALID_NET_ADDR"},
            {0xc0020009, "NT_NO_ENDPOINT_FOUND"},
            {0xc002000a, "NT_INVALID_TIMEOUT"},
            {0xc002000b, "NT_OBJECT_NOT_FOUND"},
            {0xc002000c, "NT_ALREADY_REGISTERED"},
            {0xc002000d, "NT_TYPE_ALREADY_REGISTERED"},
            {0xc002000e, "NT_ALREADY_LISTENING"},
            {0xc002000f, "NT_NO_PROTSEQS_REGISTERED"},
            {0xc0020010, "NT_NOT_LISTENING"},
            {0xc0020011, "NT_UNKNOWN_MGR_TYPE"},
            {0xc0020012, "NT_UNKNOWN_IF"},
            {0xc0020013, "NT_NO_BINDINGS"},
            {0xc0020014, "NT_NO_PROTSEQS"},
            {0xc0020015, "NT_CANT_CREATE_ENDPOINT"},
            {0xc0020016, "NT_OUT_OF_RESOURCES"},
            {0xc0020017, "NT_SERVER_UNAVAILABLE"},
            {0xc0020018, "NT_SERVER_TOO_BUSY"},
            {0xc0020019, "NT_INVALID_NETWORK_OPTIONS"},
            {0xc002001a, "NT_NO_CALL_ACTIVE"},
            {0xc002001b, "NT_CALL_FAILED"},
            {0xc002001c, "NT_CALL_FAILED_DNE"},
            {0xc002001d, "NT_PROTOCOL_ERROR"},
            {0xc002001f, "NT_UNSUPPORTED_TRANS_SYN"},
            {0xc0020021, "NT_UNSUPPORTED_TYPE"},
            {0xc0020022, "NT_INVALID_TAG"},
            {0xc0020023, "NT_INVALID_BOUND"},
            {0xc0020024, "NT_NO_ENTRY_NAME"},
            {0xc0020025, "NT_INVALID_NAME_SYNTAX"},
            {0xc0020026, "NT_UNSUPPORTED_NAME_SYNTAX"},
            {0xc0020028, "NT_UUID_NO_ADDRESS"},
            {0xc0020029, "NT_DUPLICATE_ENDPOINT"},
            {0xc002002a, "NT_UNKNOWN_AUTHN_TYPE"},
            {0xc002002b, "NT_MAX_CALLS_TOO_SMALL"},
            {0xc002002c, "NT_STRING_TOO_LONG"},
            {0xc002002d, "NT_PROTSEQ_NOT_FOUND"},
            {0xc002002e, "NT_PROCNUM_OUT_OF_RANGE"},
            {0xc002002f, "NT_BINDING_HAS_NO_AUTH"},
            {0xc0020030, "NT_UNKNOWN_AUTHN_SERVICE"},
            {0xc0020031, "NT_UNKNOWN_AUTHN_LEVEL"},
            {0xc0020032, "NT_INVALID_AUTH_IDENTITY"},
            {0xc0020033, "NT_UNKNOWN_AUTHZ_SERVICE"},
            {0xc0020034, "NT_INVALID_ENTRY"},
            {0xc0020035, "NT_CANT_PERFORM_OP"},
            {0xc0020036, "NT_NOT_REGISTERED"},
            {0xc0020037, "NT_NOTHING_TO_EXPORT"},
            {0xc0020038, "NT_INCOMPLETE_NAME"},
            {0xc0020039, "NT_INVALID_VERS_OPTION"},
            {0xc002003a, "NT_NO_MORE_MEMBERS"},
            {0xc002003b, "NT_NOT_ALL_OBJS_UNEXPORTED"},
            {0xc002003c, "NT_INTERFACE_NOT_FOUND"},
            {0xc002003d, "NT_ENTRY_ALREADY_EXISTS"},
            {0xc002003e, "NT_ENTRY_NOT_FOUND"},
            {0xc002003f, "NT_NAME_SERVICE_UNAVAILABLE"},
            {0xc0020040, "NT_INVALID_NAF_ID"},
            {0xc0020041, "NT_CANNOT_SUPPORT"},
            {0xc0020042, "NT_NO_CONTEXT_AVAILABLE"},
            {0xc0020043, "NT_INTERNAL_ERROR"},
            {0xc0020044, "NT_ZERO_DIVIDE"},
            {0xc0020045, "NT_ADDRESS_ERROR"},
            {0xc0020046, "NT_FP_DIV_ZERO"},
            {0xc0020047, "NT_FP_UNDERFLOW"},
            {0xc0020048, "NT_FP_OVERFLOW"},
            {0xc0020049, "NT_CALL_IN_PROGRESS"},
            {0xc002004a, "NT_NO_MORE_BINDINGS"},
            {0xc002004b, "NT_GROUP_MEMBER_NOT_FOUND"},
            {0xc002004c, "NT_CANT_CREATE"},
            {0xc002004d, "NT_INVALID_OBJECT"},
            {0xc002004f, "NT_NO_INTERFACES"},
            {0xc0020050, "NT_CALL_CANCELLED"},
            {0xc0020051, "NT_BINDING_INCOMPLETE"},
            {0xc0020052, "NT_COMM_FAILURE"},
            {0xc0020053, "NT_UNSUPPORTED_AUTHN_LEVEL"},
            {0xc0020054, "NT_NO_PRINC_NAME"},
            {0xc0020055, "NT_NOT_RPC_ERROR"},
            {0xc0020057, "NT_SEC_PKG_ERROR"},
            {0xc0020058, "NT_NOT_CANCELLED"},
            {0xc0020062, "NT_INVALID_ASYNC_HANDLE"},
            {0xc0020063, "NT_INVALID_ASYNC_CALL"},
            {0xc0020064, "NT_PROXY_ACCESS_DENIED"},
            {0xc0030001, "NT_NO_MORE_ENTRIES"},
            {0xc0030002, "NT_SS_CHAR_TRANS_OPEN_FAIL"},
            {0xc0030003, "NT_SS_CHAR_TRANS_SHORT_FILE"},
            {0xc0030004, "NT_SS_IN_NULL_CONTEXT"},
            {0xc0030005, "NT_SS_CONTEXT_MISMATCH"},
            {0xc0030006, "NT_SS_CONTEXT_DAMAGED"},
            {0xc0030007, "NT_SS_HANDLES_MISMATCH"},
            {0xc0030008, "NT_SS_CANNOT_GET_CALL_HANDLE"},
            {0xc0030009, "NT_NULL_REF_POINTER"},
            {0xc003000a, "NT_ENUM_VALUE_OUT_OF_RANGE"},
            {0xc003000b, "NT_BYTE_COUNT_TOO_SMALL"},
            {0xc003000c, "NT_BAD_STUB_DATA"},
            {0xc0030059, "NT_INVALID_ES_ACTION"},
            {0xc003005a, "NT_WRONG_ES_VERSION"},
            {0xc003005b, "NT_WRONG_STUB_VERSION"},
            {0xc003005c, "NT_INVALID_PIPE_OBJECT"},
            {0xc003005d, "NT_INVALID_PIPE_OPERATION"},
            {0xc003005e, "NT_WRONG_PIPE_VERSION"},
            {0xc003005f, "NT_PIPE_CLOSED"},
            {0xc0030060, "NT_PIPE_DISCIPLINE_ERROR"},
            {0xc0030061, "NT_PIPE_EMPTY"},
            {0xc0040035, "PNP_BAD_MPS_TABLE"},
            {0xc0040036, "PNP_TRANSLATION_FAILED"},
            {0xc0040037, "PNP_IRQ_TRANSLATION_FAILED"},
            {0xc0040038, "PNP_INVALID_ID"},
            {0xc0040039, "IO_REISSUE_AS_CACHED"},
            {0xc00a0001, "CTX_WINSTATION_NAME_INVALID"},
            {0xc00a0002, "CTX_INVALID_PD"},
            {0xc00a0003, "CTX_PD_NOT_FOUND"},
            {0xc00a0006, "CTX_CLOSE_PENDING"},
            {0xc00a0007, "CTX_NO_OUTBUF"},
            {0xc00a0008, "CTX_MODEM_INF_NOT_FOUND"},
            {0xc00a0009, "CTX_INVALID_MODEMNAME"},
            {0xc00a000a, "CTX_RESPONSE_ERROR"},
            {0xc00a000b, "CTX_MODEM_RESPONSE_TIMEOUT"},
            {0xc00a000c, "CTX_MODEM_RESPONSE_NO_CARRIER"},
            {0xc00a000d, "CTX_MODEM_RESPONSE_NO_DIALTONE"},
            {0xc00a000e, "CTX_MODEM_RESPONSE_BUSY"},
            {0xc00a000f, "CTX_MODEM_RESPONSE_VOICE"},
            {0xc00a0010, "CTX_TD_ERROR"},
            {0xc00a0012, "CTX_LICENSE_CLIENT_INVALID"},
            {0xc00a0013, "CTX_LICENSE_NOT_AVAILABLE"},
            {0xc00a0014, "CTX_LICENSE_EXPIRED"},
            {0xc00a0015, "CTX_WINSTATION_NOT_FOUND"},
            {0xc00a0016, "CTX_WINSTATION_NAME_COLLISION"},
            {0xc00a0017, "CTX_WINSTATION_BUSY"},
            {0xc00a0018, "CTX_BAD_VIDEO_MODE"},
            {0xc00a0022, "CTX_GRAPHICS_INVALID"},
            {0xc00a0024, "CTX_NOT_CONSOLE"},
            {0xc00a0026, "CTX_CLIENT_QUERY_TIMEOUT"},
            {0xc00a0027, "CTX_CONSOLE_DISCONNECT"},
            {0xc00a0028, "CTX_CONSOLE_CONNECT"},
            {0xc00a002a, "CTX_SHADOW_DENIED"},
            {0xc00a002b, "CTX_WINSTATION_ACCESS_DENIED"},
            {0xc00a002e, "CTX_INVALID_WD"},
            {0xc00a002f, "CTX_WD_NOT_FOUND"},
            {0xc00a0030, "CTX_SHADOW_INVALID"},
            {0xc00a0031, "CTX_SHADOW_DISABLED"},
            {0xc00a0032, "RDP_PROTOCOL_ERROR"},
            {0xc00a0033, "CTX_CLIENT_LICENSE_NOT_SET"},
            {0xc00a0034, "CTX_CLIENT_LICENSE_IN_USE"},
            {0xc00a0035, "CTX_SHADOW_ENDED_BY_MODE_CHANGE"},
            {0xc00a0036, "CTX_SHADOW_NOT_RUNNING"},
            {0xc00a0037, "CTX_LOGON_DISABLED"},
            {0xc00a0038, "CTX_SECURITY_LAYER_ERROR"},
            {0xc00a0039, "TS_INCOMPATIBLE_SESSIONS"},
            {0xc00b0001, "MUI_FILE_NOT_FOUND"},
            {0xc00b0002, "MUI_INVALID_FILE"},
            {0xc00b0003, "MUI_INVALID_RC_CONFIG"},
            {0xc00b0004, "MUI_INVALID_LOCALE_NAME"},
            {0xc00b0005, "MUI_INVALID_ULTIMATEFALLBACK_NAME"},
            {0xc00b0006, "MUI_FILE_NOT_LOADED"},
            {0xc00b0007, "RESOURCE_ENUM_USER_STOP"},
            {0xc0130001, "CLUSTER_INVALID_NODE"},
            {0xc0130002, "CLUSTER_NODE_EXISTS"},
            {0xc0130003, "CLUSTER_JOIN_IN_PROGRESS"},
            {0xc0130004, "CLUSTER_NODE_NOT_FOUND"},
            {0xc0130005, "CLUSTER_LOCAL_NODE_NOT_FOUND"},
            {0xc0130006, "CLUSTER_NETWORK_EXISTS"},
            {0xc0130007, "CLUSTER_NETWORK_NOT_FOUND"},
            {0xc0130008, "CLUSTER_NETINTERFACE_EXISTS"},
            {0xc0130009, "CLUSTER_NETINTERFACE_NOT_FOUND"},
            {0xc013000a, "CLUSTER_INVALID_REQUEST"},
            {0xc013000b, "CLUSTER_INVALID_NETWORK_PROVIDER"},
            {0xc013000c, "CLUSTER_NODE_DOWN"},
            {0xc013000d, "CLUSTER_NODE_UNREACHABLE"},
            {0xc013000e, "CLUSTER_NODE_NOT_MEMBER"},
            {0xc013000f, "CLUSTER_JOIN_NOT_IN_PROGRESS"},
            {0xc0130010, "CLUSTER_INVALID_NETWORK"},
            {0xc0130011, "CLUSTER_NO_NET_ADAPTERS"},
            {0xc0130012, "CLUSTER_NODE_UP"},
            {0xc0130013, "CLUSTER_NODE_PAUSED"},
            {0xc0130014, "CLUSTER_NODE_NOT_PAUSED"},
            {0xc0130015, "CLUSTER_NO_SECURITY_CONTEXT"},
            {0xc0130016, "CLUSTER_NETWORK_NOT_INTERNAL"},
            {0xc0130017, "CLUSTER_POISONED"},
            {0xc0140001, "ACPI_INVALID_OPCODE"},
            {0xc0140002, "ACPI_STACK_OVERFLOW"},
            {0xc0140003, "ACPI_ASSERT_FAILED"},
            {0xc0140004, "ACPI_INVALID_INDEX"},
            {0xc0140005, "ACPI_INVALID_ARGUMENT"},
            {0xc0140006, "ACPI_FATAL"},
            {0xc0140007, "ACPI_INVALID_SUPERNAME"},
            {0xc0140008, "ACPI_INVALID_ARGTYPE"},
            {0xc0140009, "ACPI_INVALID_OBJTYPE"},
            {0xc014000a, "ACPI_INVALID_TARGETTYPE"},
            {0xc014000b, "ACPI_INCORRECT_ARGUMENT_COUNT"},
            {0xc014000c, "ACPI_ADDRESS_NOT_MAPPED"},
            {0xc014000d, "ACPI_INVALID_EVENTTYPE"},
            {0xc014000e, "ACPI_HANDLER_COLLISION"},
            {0xc014000f, "ACPI_INVALID_DATA"},
            {0xc0140010, "ACPI_INVALID_REGION"},
            {0xc0140011, "ACPI_INVALID_ACCESS_SIZE"},
            {0xc0140012, "ACPI_ACQUIRE_GLOBAL_LOCK"},
            {0xc0140013, "ACPI_ALREADY_INITIALIZED"},
            {0xc0140014, "ACPI_NOT_INITIALIZED"},
            {0xc0140015, "ACPI_INVALID_MUTEX_LEVEL"},
            {0xc0140016, "ACPI_MUTEX_NOT_OWNED"},
            {0xc0140017, "ACPI_MUTEX_NOT_OWNER"},
            {0xc0140018, "ACPI_RS_ACCESS"},
            {0xc0140019, "ACPI_INVALID_TABLE"},
            {0xc0140020, "ACPI_REG_HANDLER_FAILED"},
            {0xc0140021, "ACPI_POWER_REQUEST_FAILED"},
            {0xc0150001, "SXS_SECTION_NOT_FOUND"},
            {0xc0150002, "SXS_CANT_GEN_ACTCTX"},
            {0xc0150003, "SXS_INVALID_ACTCTXDATA_FORMAT"},
            {0xc0150004, "SXS_ASSEMBLY_NOT_FOUND"},
            {0xc0150005, "SXS_MANIFEST_FORMAT_ERROR"},
            {0xc0150006, "SXS_MANIFEST_PARSE_ERROR"},
            {0xc0150007, "SXS_ACTIVATION_CONTEXT_DISABLED"},
            {0xc0150008, "SXS_KEY_NOT_FOUND"},
            {0xc0150009, "SXS_VERSION_CONFLICT"},
            {0xc015000a, "SXS_WRONG_SECTION_TYPE"},
            {0xc015000b, "SXS_THREAD_QUERIES_DISABLED"},
            {0xc015000c, "SXS_ASSEMBLY_MISSING"},
            {0xc015000e, "SXS_PROCESS_DEFAULT_ALREADY_SET"},
            {0xc015000f, "SXS_EARLY_DEACTIVATION"},
            {0xc0150010, "SXS_INVALID_DEACTIVATION"},
            {0xc0150011, "SXS_MULTIPLE_DEACTIVATION"},
            {0xc0150012, "SXS_SYSTEM_DEFAULT_ACTIVATION_CONTEXT_EMPTY"},
            {0xc0150013, "SXS_PROCESS_TERMINATION_REQUESTED"},
            {0xc0150014, "SXS_CORRUPT_ACTIVATION_STACK"},
            {0xc0150015, "SXS_CORRUPTION"},
            {0xc0150016, "SXS_INVALID_IDENTITY_ATTRIBUTE_VALUE"},
            {0xc0150017, "SXS_INVALID_IDENTITY_ATTRIBUTE_NAME"},
            {0xc0150018, "SXS_IDENTITY_DUPLICATE_ATTRIBUTE"},
            {0xc0150019, "SXS_IDENTITY_PARSE_ERROR"},
            {0xc015001a, "SXS_COMPONENT_STORE_CORRUPT"},
            {0xc015001b, "SXS_FILE_HASH_MISMATCH"},
            {0xc015001c, "SXS_MANIFEST_IDENTITY_SAME_BUT_CONTENTS_DIFFERENT"},
            {0xc015001d, "SXS_IDENTITIES_DIFFERENT"},
            {0xc015001e, "SXS_ASSEMBLY_IS_NOT_A_DEPLOYMENT"},
            {0xc015001f, "SXS_FILE_NOT_PART_OF_ASSEMBLY"},
            {0xc0150020, "ADVANCED_INSTALLER_FAILED"},
            {0xc0150021, "XML_ENCODING_MISMATCH"},
            {0xc0150022, "SXS_MANIFEST_TOO_BIG"},
            {0xc0150023, "SXS_SETTING_NOT_REGISTERED"},
            {0xc0150024, "SXS_TRANSACTION_CLOSURE_INCOMPLETE"},
            {0xc0150025, "SMI_PRIMITIVE_INSTALLER_FAILED"},
            {0xc0150026, "GENERIC_COMMAND_FAILED"},
            {0xc0150027, "SXS_FILE_HASH_MISSING"},
            {0xc0190001, "TRANSACTIONAL_CONFLICT"},
            {0xc0190002, "INVALID_TRANSACTION"},
            {0xc0190003, "TRANSACTION_NOT_ACTIVE"},
            {0xc0190004, "TM_INITIALIZATION_FAILED"},
            {0xc0190005, "RM_NOT_ACTIVE"},
            {0xc0190006, "RM_METADATA_CORRUPT"},
            {0xc0190007, "TRANSACTION_NOT_JOINED"},
            {0xc0190008, "DIRECTORY_NOT_RM"},
            {0xc019000a, "TRANSACTIONS_UNSUPPORTED_REMOTE"},
            {0xc019000b, "LOG_RESIZE_INVALID_SIZE"},
            {0xc019000c, "REMOTE_FILE_VERSION_MISMATCH"},
            {0xc019000f, "CRM_PROTOCOL_ALREADY_EXISTS"},
            {0xc0190010, "TRANSACTION_PROPAGATION_FAILED"},
            {0xc0190011, "CRM_PROTOCOL_NOT_FOUND"},
            {0xc0190012, "TRANSACTION_SUPERIOR_EXISTS"},
            {0xc0190013, "TRANSACTION_REQUEST_NOT_VALID"},
            {0xc0190014, "TRANSACTION_NOT_REQUESTED"},
            {0xc0190015, "TRANSACTION_ALREADY_ABORTED"},
            {0xc0190016, "TRANSACTION_ALREADY_COMMITTED"},
            {0xc0190017, "TRANSACTION_INVALID_MARSHALL_BUFFER"},
            {0xc0190018, "CURRENT_TRANSACTION_NOT_VALID"},
            {0xc0190019, "LOG_GROWTH_FAILED"},
            {0xc0190021, "OBJECT_NO_LONGER_EXISTS"},
            {0xc0190022, "STREAM_MINIVERSION_NOT_FOUND"},
            {0xc0190023, "STREAM_MINIVERSION_NOT_VALID"},
            {0xc0190024, "MINIVERSION_INACCESSIBLE_FROM_SPECIFIED_TRANSACTION"},
            {0xc0190025, "CANT_OPEN_MINIVERSION_WITH_MODIFY_INTENT"},
            {0xc0190026, "CANT_CREATE_MORE_STREAM_MINIVERSIONS"},
            {0xc0190028, "HANDLE_NO_LONGER_VALID"},
            {0xc0190030, "LOG_CORRUPTION_DETECTED"},
            {0xc0190032, "RM_DISCONNECTED"},
            {0xc0190033, "ENLISTMENT_NOT_SUPERIOR"},
            {0xc0190036, "FILE_IDENTITY_NOT_PERSISTENT"},
            {0xc0190037, "CANT_BREAK_TRANSACTIONAL_DEPENDENCY"},
            {0xc0190038, "CANT_CROSS_RM_BOUNDARY"},
            {0xc0190039, "TXF_DIR_NOT_EMPTY"},
            {0xc019003a, "INDOUBT_TRANSACTIONS_EXIST"},
            {0xc019003b, "TM_VOLATILE"},
            {0xc019003c, "ROLLBACK_TIMER_EXPIRED"},
            {0xc019003d, "TXF_ATTRIBUTE_CORRUPT"},
            {0xc019003e, "EFS_NOT_ALLOWED_IN_TRANSACTION"},
            {0xc019003f, "TRANSACTIONAL_OPEN_NOT_ALLOWED"},
            {0xc0190040, "TRANSACTED_MAPPING_UNSUPPORTED_REMOTE"},
            {0xc0190043, "TRANSACTION_REQUIRED_PROMOTION"},
            {0xc0190044, "CANNOT_EXECUTE_FILE_IN_TRANSACTION"},
            {0xc0190045, "TRANSACTIONS_NOT_FROZEN"},
            {0xc0190046, "TRANSACTION_FREEZE_IN_PROGRESS"},
            {0xc0190047, "NOT_SNAPSHOT_VOLUME"},
            {0xc0190048, "NO_SAVEPOINT_WITH_OPEN_FILES"},
            {0xc0190049, "SPARSE_NOT_ALLOWED_IN_TRANSACTION"},
            {0xc019004a, "TM_IDENTITY_MISMATCH"},
            {0xc019004b, "FLOATED_SECTION"},
            {0xc019004c, "CANNOT_ACCEPT_TRANSACTED_WORK"},
            {0xc019004d, "CANNOT_ABORT_TRANSACTIONS"},
            {0xc019004e, "TRANSACTION_NOT_FOUND"},
            {0xc019004f, "RESOURCEMANAGER_NOT_FOUND"},
            {0xc0190050, "ENLISTMENT_NOT_FOUND"},
            {0xc0190051, "TRANSACTIONMANAGER_NOT_FOUND"},
            {0xc0190052, "TRANSACTIONMANAGER_NOT_ONLINE"},
            {0xc0190053, "TRANSACTIONMANAGER_RECOVERY_NAME_COLLISION"},
            {0xc0190054, "TRANSACTION_NOT_ROOT"},
            {0xc0190055, "TRANSACTION_OBJECT_EXPIRED"},
            {0xc0190056, "COMPRESSION_NOT_ALLOWED_IN_TRANSACTION"},
            {0xc0190057, "TRANSACTION_RESPONSE_NOT_ENLISTED"},
            {0xc0190058, "TRANSACTION_RECORD_TOO_LONG"},
            {0xc0190059, "NO_LINK_TRACKING_IN_TRANSACTION"},
            {0xc019005a, "OPERATION_NOT_SUPPORTED_IN_TRANSACTION"},
            {0xc019005b, "TRANSACTION_INTEGRITY_VIOLATED"},
            {0xc0190060, "EXPIRED_HANDLE"},
            {0xc0190061, "TRANSACTION_NOT_ENLISTED"},
            {0xc01a0001, "LOG_SECTOR_INVALID"},
            {0xc01a0002, "LOG_SECTOR_PARITY_INVALID"},
            {0xc01a0003, "LOG_SECTOR_REMAPPED"},
            {0xc01a0004, "LOG_BLOCK_INCOMPLETE"},
            {0xc01a0005, "LOG_INVALID_RANGE"},
            {0xc01a0006, "LOG_BLOCKS_EXHAUSTED"},
            {0xc01a0007, "LOG_READ_CONTEXT_INVALID"},
            {0xc01a0008, "LOG_RESTART_INVALID"},
            {0xc01a0009, "LOG_BLOCK_VERSION"},
            {0xc01a000a, "LOG_BLOCK_INVALID"},
            {0xc01a000b, "LOG_READ_MODE_INVALID"},
            {0xc01a000d, "LOG_METADATA_CORRUPT"},
            {0xc01a000e, "LOG_METADATA_INVALID"},
            {0xc01a000f, "LOG_METADATA_INCONSISTENT"},
            {0xc01a0010, "LOG_RESERVATION_INVALID"},
            {0xc01a0011, "LOG_CANT_DELETE"},
            {0xc01a0012, "LOG_CONTAINER_LIMIT_EXCEEDED"},
            {0xc01a0013, "LOG_START_OF_LOG"},
            {0xc01a0014, "LOG_POLICY_ALREADY_INSTALLED"},
            {0xc01a0015, "LOG_POLICY_NOT_INSTALLED"},
            {0xc01a0016, "LOG_POLICY_INVALID"},
            {0xc01a0017, "LOG_POLICY_CONFLICT"},
            {0xc01a0018, "LOG_PINNED_ARCHIVE_TAIL"},
            {0xc01a0019, "LOG_RECORD_NONEXISTENT"},
            {0xc01a001a, "LOG_RECORDS_RESERVED_INVALID"},
            {0xc01a001b, "LOG_SPACE_RESERVED_INVALID"},
            {0xc01a001c, "LOG_TAIL_INVALID"},
            {0xc01a001d, "LOG_FULL"},
            {0xc01a001e, "LOG_MULTIPLEXED"},
            {0xc01a001f, "LOG_DEDICATED"},
            {0xc01a0020, "LOG_ARCHIVE_NOT_IN_PROGRESS"},
            {0xc01a0021, "LOG_ARCHIVE_IN_PROGRESS"},
            {0xc01a0022, "LOG_EPHEMERAL"},
            {0xc01a0023, "LOG_NOT_ENOUGH_CONTAINERS"},
            {0xc01a0024, "LOG_CLIENT_ALREADY_REGISTERED"},
            {0xc01a0025, "LOG_CLIENT_NOT_REGISTERED"},
            {0xc01a0026, "LOG_FULL_HANDLER_IN_PROGRESS"},
            {0xc01a0027, "LOG_CONTAINER_READ_FAILED"},
            {0xc01a0028, "LOG_CONTAINER_WRITE_FAILED"},
            {0xc01a0029, "LOG_CONTAINER_OPEN_FAILED"},
            {0xc01a002a, "LOG_CONTAINER_STATE_INVALID"},
            {0xc01a002b, "LOG_STATE_INVALID"},
            {0xc01a002c, "LOG_PINNED"},
            {0xc01a002d, "LOG_METADATA_FLUSH_FAILED"},
            {0xc01a002e, "LOG_INCONSISTENT_SECURITY"},
            {0xc01a002f, "LOG_APPENDED_FLUSH_FAILED"},
            {0xc01a0030, "LOG_PINNED_RESERVATION"},
            {0xc01b00ea, "VIDEO_HUNG_DISPLAY_DRIVER_THREAD"},
            {0xc01c0001, "FLT_NO_HANDLER_DEFINED"},
            {0xc01c0002, "FLT_CONTEXT_ALREADY_DEFINED"},
            {0xc01c0003, "FLT_INVALID_ASYNCHRONOUS_REQUEST"},
            {0xc01c0004, "FLT_DISALLOW_FAST_IO"},
            {0xc01c0005, "FLT_INVALID_NAME_REQUEST"},
            {0xc01c0006, "FLT_NOT_SAFE_TO_POST_OPERATION"},
            {0xc01c0007, "FLT_NOT_INITIALIZED"},
            {0xc01c0008, "FLT_FILTER_NOT_READY"},
            {0xc01c0009, "FLT_POST_OPERATION_CLEANUP"},
            {0xc01c000a, "FLT_INTERNAL_ERROR"},
            {0xc01c000b, "FLT_DELETING_OBJECT"},
            {0xc01c000c, "FLT_MUST_BE_NONPAGED_POOL"},
            {0xc01c000d, "FLT_DUPLICATE_ENTRY"},
            {0xc01c000e, "FLT_CBDQ_DISABLED"},
            {0xc01c000f, "FLT_DO_NOT_ATTACH"},
            {0xc01c0010, "FLT_DO_NOT_DETACH"},
            {0xc01c0011, "FLT_INSTANCE_ALTITUDE_COLLISION"},
            {0xc01c0012, "FLT_INSTANCE_NAME_COLLISION"},
            {0xc01c0013, "FLT_FILTER_NOT_FOUND"},
            {0xc01c0014, "FLT_VOLUME_NOT_FOUND"},
            {0xc01c0015, "FLT_INSTANCE_NOT_FOUND"},
            {0xc01c0016, "FLT_CONTEXT_ALLOCATION_NOT_FOUND"},
            {0xc01c0017, "FLT_INVALID_CONTEXT_REGISTRATION"},
            {0xc01c0018, "FLT_NAME_CACHE_MISS"},
            {0xc01c0019, "FLT_NO_DEVICE_OBJECT"},
            {0xc01c001a, "FLT_VOLUME_ALREADY_MOUNTED"},
            {0xc01c001b, "FLT_ALREADY_ENLISTED"},
            {0xc01c001c, "FLT_CONTEXT_ALREADY_LINKED"},
            {0xc01c0020, "FLT_NO_WAITER_FOR_REPLY"},
            {0xc01d0001, "MONITOR_NO_DESCRIPTOR"},
            {0xc01d0002, "MONITOR_UNKNOWN_DESCRIPTOR_FORMAT"},
            {0xc01d0003, "MONITOR_INVALID_DESCRIPTOR_CHECKSUM"},
            {0xc01d0004, "MONITOR_INVALID_STANDARD_TIMING_BLOCK"},
            {0xc01d0005, "MONITOR_WMI_DATABLOCK_REGISTRATION_FAILED"},
            {0xc01d0006, "MONITOR_INVALID_SERIAL_NUMBER_MONDSC_BLOCK"},
            {0xc01d0007, "MONITOR_INVALID_USER_FRIENDLY_MONDSC_BLOCK"},
            {0xc01d0008, "MONITOR_NO_MORE_DESCRIPTOR_DATA"},
            {0xc01d0009, "MONITOR_INVALID_DETAILED_TIMING_BLOCK"},
            {0xc01d000a, "MONITOR_INVALID_MANUFACTURE_DATE"},
            {0xc01e0000, "GRAPHICS_NOT_EXCLUSIVE_MODE_OWNER"},
            {0xc01e0001, "GRAPHICS_INSUFFICIENT_DMA_BUFFER"},
            {0xc01e0002, "GRAPHICS_INVALID_DISPLAY_ADAPTER"},
            {0xc01e0003, "GRAPHICS_ADAPTER_WAS_RESET"},
            {0xc01e0004, "GRAPHICS_INVALID_DRIVER_MODEL"},
            {0xc01e0005, "GRAPHICS_PRESENT_MODE_CHANGED"},
            {0xc01e0006, "GRAPHICS_PRESENT_OCCLUDED"},
            {0xc01e0007, "GRAPHICS_PRESENT_DENIED"},
            {0xc01e0008, "GRAPHICS_CANNOTCOLORCONVERT"},
            {0xc01e000b, "GRAPHICS_PRESENT_REDIRECTION_DISABLED"},
            {0xc01e000c, "GRAPHICS_PRESENT_UNOCCLUDED"},
            {0xc01e0100, "GRAPHICS_NO_VIDEO_MEMORY"},
            {0xc01e0101, "GRAPHICS_CANT_LOCK_MEMORY"},
            {0xc01e0102, "GRAPHICS_ALLOCATION_BUSY"},
            {0xc01e0103, "GRAPHICS_TOO_MANY_REFERENCES"},
            {0xc01e0104, "GRAPHICS_TRY_AGAIN_LATER"},
            {0xc01e0105, "GRAPHICS_TRY_AGAIN_NOW"},
            {0xc01e0106, "GRAPHICS_ALLOCATION_INVALID"},
            {0xc01e0107, "GRAPHICS_UNSWIZZLING_APERTURE_UNAVAILABLE"},
            {0xc01e0108, "GRAPHICS_UNSWIZZLING_APERTURE_UNSUPPORTED"},
            {0xc01e0109, "GRAPHICS_CANT_EVICT_PINNED_ALLOCATION"},
            {0xc01e0110, "GRAPHICS_INVALID_ALLOCATION_USAGE"},
            {0xc01e0111, "GRAPHICS_CANT_RENDER_LOCKED_ALLOCATION"},
            {0xc01e0112, "GRAPHICS_ALLOCATION_CLOSED"},
            {0xc01e0113, "GRAPHICS_INVALID_ALLOCATION_INSTANCE"},
            {0xc01e0114, "GRAPHICS_INVALID_ALLOCATION_HANDLE"},
            {0xc01e0115, "GRAPHICS_WRONG_ALLOCATION_DEVICE"},
            {0xc01e0116, "GRAPHICS_ALLOCATION_CONTENT_LOST"},
            {0xc01e0200, "GRAPHICS_GPU_EXCEPTION_ON_DEVICE"},
            {0xc01e0300, "GRAPHICS_INVALID_VIDPN_TOPOLOGY"},
            {0xc01e0301, "GRAPHICS_VIDPN_TOPOLOGY_NOT_SUPPORTED"},
            {0xc01e0302, "GRAPHICS_VIDPN_TOPOLOGY_CURRENTLY_NOT_SUPPORTED"},
            {0xc01e0303, "GRAPHICS_INVALID_VIDPN"},
            {0xc01e0304, "GRAPHICS_INVALID_VIDEO_PRESENT_SOURCE"},
            {0xc01e0305, "GRAPHICS_INVALID_VIDEO_PRESENT_TARGET"},
            {0xc01e0306, "GRAPHICS_VIDPN_MODALITY_NOT_SUPPORTED"},
            {0xc01e0308, "GRAPHICS_INVALID_VIDPN_SOURCEMODESET"},
            {0xc01e0309, "GRAPHICS_INVALID_VIDPN_TARGETMODESET"},
            {0xc01e030a, "GRAPHICS_INVALID_FREQUENCY"},
            {0xc01e030b, "GRAPHICS_INVALID_ACTIVE_REGION"},
            {0xc01e030c, "GRAPHICS_INVALID_TOTAL_REGION"},
            {0xc01e0310, "GRAPHICS_INVALID_VIDEO_PRESENT_SOURCE_MODE"},
            {0xc01e0311, "GRAPHICS_INVALID_VIDEO_PRESENT_TARGET_MODE"},
            {0xc01e0312, "GRAPHICS_PINNED_MODE_MUST_REMAIN_IN_SET"},
            {0xc01e0313, "GRAPHICS_PATH_ALREADY_IN_TOPOLOGY"},
            {0xc01e0314, "GRAPHICS_MODE_ALREADY_IN_MODESET"},
            {0xc01e0315, "GRAPHICS_INVALID_VIDEOPRESENTSOURCESET"},
            {0xc01e0316, "GRAPHICS_INVALID_VIDEOPRESENTTARGETSET"},
            {0xc01e0317, "GRAPHICS_SOURCE_ALREADY_IN_SET"},
            {0xc01e0318, "GRAPHICS_TARGET_ALREADY_IN_SET"},
            {0xc01e0319, "GRAPHICS_INVALID_VIDPN_PRESENT_PATH"},
            {0xc01e031a, "GRAPHICS_NO_RECOMMENDED_VIDPN_TOPOLOGY"},
            {0xc01e031b, "GRAPHICS_INVALID_MONITOR_FREQUENCYRANGESET"},
            {0xc01e031c, "GRAPHICS_INVALID_MONITOR_FREQUENCYRANGE"},
            {0xc01e031d, "GRAPHICS_FREQUENCYRANGE_NOT_IN_SET"},
            {0xc01e031f, "GRAPHICS_FREQUENCYRANGE_ALREADY_IN_SET"},
            {0xc01e0320, "GRAPHICS_STALE_MODESET"},
            {0xc01e0321, "GRAPHICS_INVALID_MONITOR_SOURCEMODESET"},
            {0xc01e0322, "GRAPHICS_INVALID_MONITOR_SOURCE_MODE"},
            {0xc01e0323, "GRAPHICS_NO_RECOMMENDED_FUNCTIONAL_VIDPN"},
            {0xc01e0324, "GRAPHICS_MODE_ID_MUST_BE_UNIQUE"},
            {0xc01e0325, "GRAPHICS_EMPTY_ADAPTER_MONITOR_MODE_SUPPORT_INTERSECTION"},
            {0xc01e0326, "GRAPHICS_VIDEO_PRESENT_TARGETS_LESS_THAN_SOURCES"},
            {0xc01e0327, "GRAPHICS_PATH_NOT_IN_TOPOLOGY"},
            {0xc01e0328, "GRAPHICS_ADAPTER_MUST_HAVE_AT_LEAST_ONE_SOURCE"},
            {0xc01e0329, "GRAPHICS_ADAPTER_MUST_HAVE_AT_LEAST_ONE_TARGET"},
            {0xc01e032a, "GRAPHICS_INVALID_MONITORDESCRIPTORSET"},
            {0xc01e032b, "GRAPHICS_INVALID_MONITORDESCRIPTOR"},
            {0xc01e032c, "GRAPHICS_MONITORDESCRIPTOR_NOT_IN_SET"},
            {0xc01e032d, "GRAPHICS_MONITORDESCRIPTOR_ALREADY_IN_SET"},
            {0xc01e032e, "GRAPHICS_MONITORDESCRIPTOR_ID_MUST_BE_UNIQUE"},
            {0xc01e032f, "GRAPHICS_INVALID_VIDPN_TARGET_SUBSET_TYPE"},
            {0xc01e0330, "GRAPHICS_RESOURCES_NOT_RELATED"},
            {0xc01e0331, "GRAPHICS_SOURCE_ID_MUST_BE_UNIQUE"},
            {0xc01e0332, "GRAPHICS_TARGET_ID_MUST_BE_UNIQUE"},
            {0xc01e0333, "GRAPHICS_NO_AVAILABLE_VIDPN_TARGET"},
            {0xc01e0334, "GRAPHICS_MONITOR_COULD_NOT_BE_ASSOCIATED_WITH_ADAPTER"},
            {0xc01e0335, "GRAPHICS_NO_VIDPNMGR"},
            {0xc01e0336, "GRAPHICS_NO_ACTIVE_VIDPN"},
            {0xc01e0337, "GRAPHICS_STALE_VIDPN_TOPOLOGY"},
            {0xc01e0338, "GRAPHICS_MONITOR_NOT_CONNECTED"},
            {0xc01e0339, "GRAPHICS_SOURCE_NOT_IN_TOPOLOGY"},
            {0xc01e033a, "GRAPHICS_INVALID_PRIMARYSURFACE_SIZE"},
            {0xc01e033b, "GRAPHICS_INVALID_VISIBLEREGION_SIZE"},
            {0xc01e033c, "GRAPHICS_INVALID_STRIDE"},
            {0xc01e033d, "GRAPHICS_INVALID_PIXELFORMAT"},
            {0xc01e033e, "GRAPHICS_INVALID_COLORBASIS"},
            {0xc01e033f, "GRAPHICS_INVALID_PIXELVALUEACCESSMODE"},
            {0xc01e0340, "GRAPHICS_TARGET_NOT_IN_TOPOLOGY"},
            {0xc01e0341, "GRAPHICS_NO_DISPLAY_MODE_MANAGEMENT_SUPPORT"},
            {0xc01e0342, "GRAPHICS_VIDPN_SOURCE_IN_USE"},
            {0xc01e0343, "GRAPHICS_CANT_ACCESS_ACTIVE_VIDPN"},
            {0xc01e0344, "GRAPHICS_INVALID_PATH_IMPORTANCE_ORDINAL"},
            {0xc01e0345, "GRAPHICS_INVALID_PATH_CONTENT_GEOMETRY_TRANSFORMATION"},
            {0xc01e0346, "GRAPHICS_PATH_CONTENT_GEOMETRY_TRANSFORMATION_NOT_SUPPORTED"},
            {0xc01e0347, "GRAPHICS_INVALID_GAMMA_RAMP"},
            {0xc01e0348, "GRAPHICS_GAMMA_RAMP_NOT_SUPPORTED"},
            {0xc01e0349, "GRAPHICS_MULTISAMPLING_NOT_SUPPORTED"},
            {0xc01e034a, "GRAPHICS_MODE_NOT_IN_MODESET"},
            {0xc01e034d, "GRAPHICS_INVALID_VIDPN_TOPOLOGY_RECOMMENDATION_REASON"},
            {0xc01e034e, "GRAPHICS_INVALID_PATH_CONTENT_TYPE"},
            {0xc01e034f, "GRAPHICS_INVALID_COPYPROTECTION_TYPE"},
            {0xc01e0350, "GRAPHICS_UNASSIGNED_MODESET_ALREADY_EXISTS"},
            {0xc01e0352, "GRAPHICS_INVALID_SCANLINE_ORDERING"},
            {0xc01e0353, "GRAPHICS_TOPOLOGY_CHANGES_NOT_ALLOWED"},
            {0xc01e0354, "GRAPHICS_NO_AVAILABLE_IMPORTANCE_ORDINALS"},
            {0xc01e0355, "GRAPHICS_INCOMPATIBLE_PRIVATE_FORMAT"},
            {0xc01e0356, "GRAPHICS_INVALID_MODE_PRUNING_ALGORITHM"},
            {0xc01e0357, "GRAPHICS_INVALID_MONITOR_CAPABILITY_ORIGIN"},
            {0xc01e0358, "GRAPHICS_INVALID_MONITOR_FREQUENCYRANGE_CONSTRAINT"},
            {0xc01e0359, "GRAPHICS_MAX_NUM_PATHS_REACHED"},
            {0xc01e035a, "GRAPHICS_CANCEL_VIDPN_TOPOLOGY_AUGMENTATION"},
            {0xc01e035b, "GRAPHICS_INVALID_CLIENT_TYPE"},
            {0xc01e035c, "GRAPHICS_CLIENTVIDPN_NOT_SET"},
            {0xc01e0400, "GRAPHICS_SPECIFIED_CHILD_ALREADY_CONNECTED"},
            {0xc01e0401, "GRAPHICS_CHILD_DESCRIPTOR_NOT_SUPPORTED"},
            {0xc01e0430, "GRAPHICS_NOT_A_LINKED_ADAPTER"},
            {0xc01e0431, "GRAPHICS_LEADLINK_NOT_ENUMERATED"},
            {0xc01e0432, "GRAPHICS_CHAINLINKS_NOT_ENUMERATED"},
            {0xc01e0433, "GRAPHICS_ADAPTER_CHAIN_NOT_READY"},
            {0xc01e0434, "GRAPHICS_CHAINLINKS_NOT_STARTED"},
            {0xc01e0435, "GRAPHICS_CHAINLINKS_NOT_POWERED_ON"},
            {0xc01e0436, "GRAPHICS_INCONSISTENT_DEVICE_LINK_STATE"},
            {0xc01e0438, "GRAPHICS_NOT_POST_DEVICE_DRIVER"},
            {0xc01e043b, "GRAPHICS_ADAPTER_ACCESS_NOT_EXCLUDED"},
            {0xc01e0500, "GRAPHICS_OPM_NOT_SUPPORTED"},
            {0xc01e0501, "GRAPHICS_COPP_NOT_SUPPORTED"},
            {0xc01e0502, "GRAPHICS_UAB_NOT_SUPPORTED"},
            {0xc01e0503, "GRAPHICS_OPM_INVALID_ENCRYPTED_PARAMETERS"},
            {0xc01e0504, "GRAPHICS_OPM_PARAMETER_ARRAY_TOO_SMALL"},
            {0xc01e0505, "GRAPHICS_OPM_NO_PROTECTED_OUTPUTS_EXIST"},
            {0xc01e0506, "GRAPHICS_PVP_NO_DISPLAY_DEVICE_CORRESPONDS_TO_NAME"},
            {0xc01e0507, "GRAPHICS_PVP_DISPLAY_DEVICE_NOT_ATTACHED_TO_DESKTOP"},
            {0xc01e0508, "GRAPHICS_PVP_MIRRORING_DEVICES_NOT_SUPPORTED"},
            {0xc01e050a, "GRAPHICS_OPM_INVALID_POINTER"},
            {0xc01e050b, "GRAPHICS_OPM_INTERNAL_ERROR"},
            {0xc01e050c, "GRAPHICS_OPM_INVALID_HANDLE"},
            {0xc01e050d, "GRAPHICS_PVP_NO_MONITORS_CORRESPOND_TO_DISPLAY_DEVICE"},
            {0xc01e050e, "GRAPHICS_PVP_INVALID_CERTIFICATE_LENGTH"},
            {0xc01e050f, "GRAPHICS_OPM_SPANNING_MODE_ENABLED"},
            {0xc01e0510, "GRAPHICS_OPM_THEATER_MODE_ENABLED"},
            {0xc01e0511, "GRAPHICS_PVP_HFS_FAILED"},
            {0xc01e0512, "GRAPHICS_OPM_INVALID_SRM"},
            {0xc01e0513, "GRAPHICS_OPM_OUTPUT_DOES_NOT_SUPPORT_HDCP"},
            {0xc01e0514, "GRAPHICS_OPM_OUTPUT_DOES_NOT_SUPPORT_ACP"},
            {0xc01e0515, "GRAPHICS_OPM_OUTPUT_DOES_NOT_SUPPORT_CGMSA"},
            {0xc01e0516, "GRAPHICS_OPM_HDCP_SRM_NEVER_SET"},
            {0xc01e0517, "GRAPHICS_OPM_RESOLUTION_TOO_HIGH"},
            {0xc01e0518, "GRAPHICS_OPM_ALL_HDCP_HARDWARE_ALREADY_IN_USE"},
            {0xc01e051a, "GRAPHICS_OPM_PROTECTED_OUTPUT_NO_LONGER_EXISTS"},
            {0xc01e051b, "GRAPHICS_OPM_SESSION_TYPE_CHANGE_IN_PROGRESS"},
            {0xc01e051c, "GRAPHICS_OPM_PROTECTED_OUTPUT_DOES_NOT_HAVE_COPP_SEMANTICS"},
            {0xc01e051d, "GRAPHICS_OPM_INVALID_INFORMATION_REQUEST"},
            {0xc01e051e, "GRAPHICS_OPM_DRIVER_INTERNAL_ERROR"},
            {0xc01e051f, "GRAPHICS_OPM_PROTECTED_OUTPUT_DOES_NOT_HAVE_OPM_SEMANTICS"},
            {0xc01e0520, "GRAPHICS_OPM_SIGNALING_NOT_SUPPORTED"},
            {0xc01e0521, "GRAPHICS_OPM_INVALID_CONFIGURATION_REQUEST"},
            {0xc01e0580, "GRAPHICS_I2C_NOT_SUPPORTED"},
            {0xc01e0581, "GRAPHICS_I2C_DEVICE_DOES_NOT_EXIST"},
            {0xc01e0582, "GRAPHICS_I2C_ERROR_TRANSMITTING_DATA"},
            {0xc01e0583, "GRAPHICS_I2C_ERROR_RECEIVING_DATA"},
            {0xc01e0584, "GRAPHICS_DDCCI_VCP_NOT_SUPPORTED"},
            {0xc01e0585, "GRAPHICS_DDCCI_INVALID_DATA"},
            {0xc01e0586, "GRAPHICS_DDCCI_MONITOR_RETURNED_INVALID_TIMING_STATUS_BYTE"},
            {0xc01e0587, "GRAPHICS_DDCCI_INVALID_CAPABILITIES_STRING"},
            {0xc01e0588, "GRAPHICS_MCA_INTERNAL_ERROR"},
            {0xc01e0589, "GRAPHICS_DDCCI_INVALID_MESSAGE_COMMAND"},
            {0xc01e058a, "GRAPHICS_DDCCI_INVALID_MESSAGE_LENGTH"},
            {0xc01e058b, "GRAPHICS_DDCCI_INVALID_MESSAGE_CHECKSUM"},
            {0xc01e058c, "GRAPHICS_INVALID_PHYSICAL_MONITOR_HANDLE"},
            {0xc01e058d, "GRAPHICS_MONITOR_NO_LONGER_EXISTS"},
            {0xc01e05e0, "GRAPHICS_ONLY_CONSOLE_SESSION_SUPPORTED"},
            {0xc01e05e1, "GRAPHICS_NO_DISPLAY_DEVICE_CORRESPONDS_TO_NAME"},
            {0xc01e05e2, "GRAPHICS_DISPLAY_DEVICE_NOT_ATTACHED_TO_DESKTOP"},
            {0xc01e05e3, "GRAPHICS_MIRRORING_DEVICES_NOT_SUPPORTED"},
            {0xc01e05e4, "GRAPHICS_INVALID_POINTER"},
            {0xc01e05e5, "GRAPHICS_NO_MONITORS_CORRESPOND_TO_DISPLAY_DEVICE"},
            {0xc01e05e6, "GRAPHICS_PARAMETER_ARRAY_TOO_SMALL"},
            {0xc01e05e7, "GRAPHICS_INTERNAL_ERROR"},
            {0xc01e05e8, "GRAPHICS_SESSION_TYPE_CHANGE_IN_PROGRESS"},
            {0xc0210000, "FVE_LOCKED_VOLUME"},
            {0xc0210001, "FVE_NOT_ENCRYPTED"},
            {0xc0210002, "FVE_BAD_INFORMATION"},
            {0xc0210003, "FVE_TOO_SMALL"},
            {0xc0210004, "FVE_FAILED_WRONG_FS"},
            {0xc0210005, "FVE_FAILED_BAD_FS"},
            {0xc0210006, "FVE_FS_NOT_EXTENDED"},
            {0xc0210007, "FVE_FS_MOUNTED"},
            {0xc0210008, "FVE_NO_LICENSE"},
            {0xc0210009, "FVE_ACTION_NOT_ALLOWED"},
            {0xc021000a, "FVE_BAD_DATA"},
            {0xc021000b, "FVE_VOLUME_NOT_BOUND"},
            {0xc021000c, "FVE_NOT_DATA_VOLUME"},
            {0xc021000d, "FVE_CONV_READ_ERROR"},
            {0xc021000e, "FVE_CONV_WRITE_ERROR"},
            {0xc021000f, "FVE_OVERLAPPED_UPDATE"},
            {0xc0210010, "FVE_FAILED_SECTOR_SIZE"},
            {0xc0210011, "FVE_FAILED_AUTHENTICATION"},
            {0xc0210012, "FVE_NOT_OS_VOLUME"},
            {0xc0210013, "FVE_KEYFILE_NOT_FOUND"},
            {0xc0210014, "FVE_KEYFILE_INVALID"},
            {0xc0210015, "FVE_KEYFILE_NO_VMK"},
            {0xc0210016, "FVE_TPM_DISABLED"},
            {0xc0210017, "FVE_TPM_SRK_AUTH_NOT_ZERO"},
            {0xc0210018, "FVE_TPM_INVALID_PCR"},
            {0xc0210019, "FVE_TPM_NO_VMK"},
            {0xc021001a, "FVE_PIN_INVALID"},
            {0xc021001b, "FVE_AUTH_INVALID_APPLICATION"},
            {0xc021001c, "FVE_AUTH_INVALID_CONFIG"},
            {0xc021001d, "FVE_DEBUGGER_ENABLED"},
            {0xc021001e, "FVE_DRY_RUN_FAILED"},
            {0xc021001f, "FVE_BAD_METADATA_POINTER"},
            {0xc0210020, "FVE_OLD_METADATA_COPY"},
            {0xc0210021, "FVE_REBOOT_REQUIRED"},
            {0xc0210022, "FVE_RAW_ACCESS"},
            {0xc0210023, "FVE_RAW_BLOCKED"},
            {0xc0210026, "FVE_NO_FEATURE_LICENSE"},
            {0xc0210027, "FVE_POLICY_USER_DISABLE_RDV_NOT_ALLOWED"},
            {0xc0210028, "FVE_CONV_RECOVERY_FAILED"},
            {0xc0210029, "FVE_VIRTUALIZED_SPACE_TOO_BIG"},
            {0xc0210030, "FVE_VOLUME_TOO_SMALL"},
            {0xc0220001, "FWP_CALLOUT_NOT_FOUND"},
            {0xc0220002, "FWP_CONDITION_NOT_FOUND"},
            {0xc0220003, "FWP_FILTER_NOT_FOUND"},
            {0xc0220004, "FWP_LAYER_NOT_FOUND"},
            {0xc0220005, "FWP_PROVIDER_NOT_FOUND"},
            {0xc0220006, "FWP_PROVIDER_CONTEXT_NOT_FOUND"},
            {0xc0220007, "FWP_SUBLAYER_NOT_FOUND"},
            {0xc0220008, "FWP_NOT_FOUND"},
            {0xc0220009, "FWP_ALREADY_EXISTS"},
            {0xc022000a, "FWP_IN_USE"},
            {0xc022000b, "FWP_DYNAMIC_SESSION_IN_PROGRESS"},
            {0xc022000c, "FWP_WRONG_SESSION"},
            {0xc022000d, "FWP_NO_TXN_IN_PROGRESS"},
            {0xc022000e, "FWP_TXN_IN_PROGRESS"},
            {0xc022000f, "FWP_TXN_ABORTED"},
            {0xc0220010, "FWP_SESSION_ABORTED"},
            {0xc0220011, "FWP_INCOMPATIBLE_TXN"},
            {0xc0220012, "FWP_TIMEOUT"},
            {0xc0220013, "FWP_NET_EVENTS_DISABLED"},
            {0xc0220014, "FWP_INCOMPATIBLE_LAYER"},
            {0xc0220015, "FWP_KM_CLIENTS_ONLY"},
            {0xc0220016, "FWP_LIFETIME_MISMATCH"},
            {0xc0220017, "FWP_BUILTIN_OBJECT"},
            {0xc0220018, "FWP_TOO_MANY_BOOTTIME_FILTERS"},
            {0xc0220019, "FWP_NOTIFICATION_DROPPED"},
            {0xc022001a, "FWP_TRAFFIC_MISMATCH"},
            {0xc022001b, "FWP_INCOMPATIBLE_SA_STATE"},
            {0xc022001c, "FWP_NULL_POINTER"},
            {0xc022001d, "FWP_INVALID_ENUMERATOR"},
            {0xc022001e, "FWP_INVALID_FLAGS"},
            {0xc022001f, "FWP_INVALID_NET_MASK"},
            {0xc0220020, "FWP_INVALID_RANGE"},
            {0xc0220021, "FWP_INVALID_INTERVAL"},
            {0xc0220022, "FWP_ZERO_LENGTH_ARRAY"},
            {0xc0220023, "FWP_NULL_DISPLAY_NAME"},
            {0xc0220024, "FWP_INVALID_ACTION_TYPE"},
            {0xc0220025, "FWP_INVALID_WEIGHT"},
            {0xc0220026, "FWP_MATCH_TYPE_MISMATCH"},
            {0xc0220027, "FWP_TYPE_MISMATCH"},
            {0xc0220028, "FWP_OUT_OF_BOUNDS"},
            {0xc0220029, "FWP_RESERVED"},
            {0xc022002a, "FWP_DUPLICATE_CONDITION"},
            {0xc022002b, "FWP_DUPLICATE_KEYMOD"},
            {0xc022002c, "FWP_ACTION_INCOMPATIBLE_WITH_LAYER"},
            {0xc022002d, "FWP_ACTION_INCOMPATIBLE_WITH_SUBLAYER"},
            {0xc022002e, "FWP_CONTEXT_INCOMPATIBLE_WITH_LAYER"},
            {0xc022002f, "FWP_CONTEXT_INCOMPATIBLE_WITH_CALLOUT"},
            {0xc0220030, "FWP_INCOMPATIBLE_AUTH_METHOD"},
            {0xc0220031, "FWP_INCOMPATIBLE_DH_GROUP"},
            {0xc0220032, "FWP_EM_NOT_SUPPORTED"},
            {0xc0220033, "FWP_NEVER_MATCH"},
            {0xc0220034, "FWP_PROVIDER_CONTEXT_MISMATCH"},
            {0xc0220035, "FWP_INVALID_PARAMETER"},
            {0xc0220036, "FWP_TOO_MANY_SUBLAYERS"},
            {0xc0220037, "FWP_CALLOUT_NOTIFICATION_FAILED"},
            {0xc0220038, "FWP_INCOMPATIBLE_AUTH_CONFIG"},
            {0xc0220039, "FWP_INCOMPATIBLE_CIPHER_CONFIG"},
            {0xc022003c, "FWP_DUPLICATE_AUTH_METHOD"},
            {0xc0220100, "FWP_TCPIP_NOT_READY"},
            {0xc0220101, "FWP_INJECT_HANDLE_CLOSING"},
            {0xc0220102, "FWP_INJECT_HANDLE_STALE"},
            {0xc0220103, "FWP_CANNOT_PEND"},
            {0xc0230002, "NDIS_CLOSING"},
            {0xc0230004, "NDIS_BAD_VERSION"},
            {0xc0230005, "NDIS_BAD_CHARACTERISTICS"},
            {0xc0230006, "NDIS_ADAPTER_NOT_FOUND"},
            {0xc0230007, "NDIS_OPEN_FAILED"},
            {0xc0230008, "NDIS_DEVICE_FAILED"},
            {0xc0230009, "NDIS_MULTICAST_FULL"},
            {0xc023000a, "NDIS_MULTICAST_EXISTS"},
            {0xc023000b, "NDIS_MULTICAST_NOT_FOUND"},
            {0xc023000c, "NDIS_REQUEST_ABORTED"},
            {0xc023000d, "NDIS_RESET_IN_PROGRESS"},
            {0xc023000f, "NDIS_INVALID_PACKET"},
            {0xc0230010, "NDIS_INVALID_DEVICE_REQUEST"},
            {0xc0230011, "NDIS_ADAPTER_NOT_READY"},
            {0xc0230014, "NDIS_INVALID_LENGTH"},
            {0xc0230015, "NDIS_INVALID_DATA"},
            {0xc0230016, "NDIS_BUFFER_TOO_SHORT"},
            {0xc0230017, "NDIS_INVALID_OID"},
            {0xc0230018, "NDIS_ADAPTER_REMOVED"},
            {0xc0230019, "NDIS_UNSUPPORTED_MEDIA"},
            {0xc023001a, "NDIS_GROUP_ADDRESS_IN_USE"},
            {0xc023001b, "NDIS_FILE_NOT_FOUND"},
            {0xc023001c, "NDIS_ERROR_READING_FILE"},
            {0xc023001d, "NDIS_ALREADY_MAPPED"},
            {0xc023001e, "NDIS_RESOURCE_CONFLICT"},
            {0xc023001f, "NDIS_MEDIA_DISCONNECTED"},
            {0xc0230022, "NDIS_INVALID_ADDRESS"},
            {0xc023002a, "NDIS_PAUSED"},
            {0xc023002b, "NDIS_INTERFACE_NOT_FOUND"},
            {0xc023002c, "NDIS_UNSUPPORTED_REVISION"},
            {0xc023002d, "NDIS_INVALID_PORT"},
            {0xc023002e, "NDIS_INVALID_PORT_STATE"},
            {0xc023002f, "NDIS_LOW_POWER_STATE"},
            {0xc02300bb, "NDIS_NOT_SUPPORTED"},
            {0xc023100f, "NDIS_OFFLOAD_POLICY"},
            {0xc0231012, "NDIS_OFFLOAD_CONNECTION_REJECTED"},
            {0xc0231013, "NDIS_OFFLOAD_PATH_REJECTED"},
            {0xc0232000, "NDIS_DOT11_AUTO_CONFIG_ENABLED"},
            {0xc0232001, "NDIS_DOT11_MEDIA_IN_USE"},
            {0xc0232002, "NDIS_DOT11_POWER_STATE_INVALID"},
            {0xc0232003, "NDIS_PM_WOL_PATTERN_LIST_FULL"},
            {0xc0232004, "NDIS_PM_PROTOCOL_OFFLOAD_LIST_FULL"},
            {0xc0360001, "IPSEC_BAD_SPI"},
            {0xc0360002, "IPSEC_SA_LIFETIME_EXPIRED"},
            {0xc0360003, "IPSEC_WRONG_SA"},
            {0xc0360004, "IPSEC_REPLAY_CHECK_FAILED"},
            {0xc0360005, "IPSEC_INVALID_PACKET"},
            {0xc0360006, "IPSEC_INTEGRITY_CHECK_FAILED"},
            {0xc0360007, "IPSEC_CLEAR_TEXT_DROP"},
            {0xc0360008, "IPSEC_AUTH_FIREWALL_DROP"},
            {0xc0360009, "IPSEC_THROTTLE_DROP"},
            {0xc0368000, "IPSEC_DOSP_BLOCK"},
            {0xc0368001, "IPSEC_DOSP_RECEIVED_MULTICAST"},
            {0xc0368002, "IPSEC_DOSP_INVALID_PACKET"},
            {0xc0368003, "IPSEC_DOSP_STATE_LOOKUP_FAILED"},
            {0xc0368004, "IPSEC_DOSP_MAX_ENTRIES"},
            {0xc0368005, "IPSEC_DOSP_KEYMOD_NOT_ALLOWED"},
            {0xc0368006, "IPSEC_DOSP_MAX_PER_IP_RATELIMIT_QUEUES"},
            {0xc038005b, "VOLMGR_MIRROR_NOT_SUPPORTED"},
            {0xc038005c, "VOLMGR_RAID5_NOT_SUPPORTED"},
            {0xc03a0014, "VIRTDISK_PROVIDER_NOT_FOUND"},
            {0xc03a0015, "VIRTDISK_NOT_VIRTUAL_DISK"},
            {0xc03a0016, "VHD_PARENT_VHD_ACCESS_DENIED"},
            {0xc03a0017, "VHD_CHILD_PARENT_SIZE_MISMATCH"},
            {0xc03a0018, "VHD_DIFFERENCING_CHAIN_CYCLE_DETECTED"},
            {0xc03a0019, "VHD_DIFFERENCING_CHAIN_ERROR_IN_PARENT"},
            {0xe06d7363, "CPP_EXCEPTION"},
            {0xCAFEBEE1, "THINAPP_INJECTION_CAFEBEE1"},
            {0xCAFEBEE2, "THINAPP_INJECTION_CAFEBEE2"},
            {0x800706B5, "UNKNOWN_INTERFACE"},
        };
        #endregion
        #region FslErrorCodes
        static readonly Dictionary<uint, string> FslErrorCodes = new Dictionary<uint, string>
        {
            {0, "SUCCESS"},
            {1001, "FSL2_ERROR_LAYER_ALREADY_EXISTS"},
            {1002, "FSL2_ERROR_NO_SUCH_NON_PEER"},
            {1003, "FSL2_ERROR_NO_SUCH_GROUP"},
            {1004, "FSL2_ERROR_NOT_GROUP_MEMBER"},
            {1005, "FSL2_ERROR_FILE_COPY"},
            {1006, "FSL2_ERROR_BAD_ARGS"},
            {1007, "FSL2_ERROR_OPEN"},
            {1008, "FSL2_ERROR_LAYER_DELETE"},
            {1009, "FSL2_ERROR_ARCHIVE_CREATE"},
            {1010, "FSL2_ERROR_ARCHIVE_EXTRACT"},
            {1011, "FSL2_ERROR_INVALID_ARCHIVE"},
            {1012, "FSL2_ERROR_DELETE_FILE"},
            {1013, "FSL2_ERROR_CREATE_FILE"},
            {1014, "FSL2_ERROR_GET_VERSION"},
            {1015, "FSL2_ERROR_CREATE_GROUP"},
            {1016, "FSL2_ERROR_ADD_TO_GROUP"},
            {1017, "FSL2_GROUP_ALREADY_EXISTS"},
            {1018, "FSL2_ERROR_CREATE_DIRECTORY"},
            {1019, "FSL2_ERROR_INVALID_VARIABLIZED_NAME"},
            {1020, "FSL2_ERROR_UNSUPPORTED_PLATFORM"},
            {1021, "FSL2_ERROR_LOAD_LIBRARY"},
            {1022, "FSL2_ERROR_GET_PROC_ADDRESS"},
            {1023, "FSL2_ERROR_GET_MODULE"},
            {1024, "FSL2_ERROR_TERMINATE_PROCESS"},
            {1025, "FSL2_ERROR_OPEN_PROCESS"},
            {1026, "FSL2_ERROR_SNAPSHOT"},
            {1027, "FSL2_ERROR_DELETE_PEER"},
            {1028, "FSL2_ERROR_FLUSH"},
            {1029, "FSL2_ERROR_ENUM_PROCESSES"},
            {1030, "FSL2_ERROR_BUFFER_TOO_SMALL"},
            {1031, "FSL2_ERROR_KEY_INVALID"},
            {1032, "FSL2_ERROR_STARTING_THREAD"},
            {1033, "FSL2_ERROR_RENAME_FILE"},
            {1034, "FSL2_ERROR_GET_ATTRS"},
            {1035, "FSL2_ERROR_SECURITY"},
            {1036, "FSL2_ERROR_IS_NOT_NTFS"},
            {1037, "FSL2_ERROR_NOMEM"},
            {1038, "FSL2_ERROR_LAYER_NAME_INVALID"},
            {1039, "FSL2_ERROR_INVALID_OP_FOR_LAYER_STATE"},
            {1040, "FSL2_ERROR_LAYER_REG_OPERATION"},
            {1041, "FSL2_ERROR_LAYER_NOT_FOUND"},
            {1042, "FSL2_ERROR_WRONGVERSION"},
            {1043, "FSL2_ERROR_FILEPATH_ALREADY_EXISTS"},
            {1044, "FSL2_ERROR_BADHANDLE"},
            {1045, "FSL2_ERROR_BADNODE"},
            {1046, "FSL2_ERROR_NORESOURCES"},
            {1047, "FSL2_ERROR_ITEMNOTFOUND"},
            {1048, "FSL2_ERROR_NOTIMPLEMENTED"},
            {1049, "FSL2_ERROR_ALREADYACTIVE"},
            {1050, "FSL2_ERROR_FILEIOERROR"},
            {1051, "FSL2_ERROR_NOT_LOADED"},
            {1052, "FSL2_ERROR_PIDHASHANDLEOPEN"},
            {1053, "FSL2_ERROR_PIDRUNNINGFROMLAYER"},
            {1054, "FSL2_ERROR_SYSTEMHASOPENFILE"},
            {1055, "FSL2_ERROR_SYSTEM_NOT_INITIALIZED"},
            {1056, "FSL2_ERROR_ZIP_DLL_NOT_LOADED"},
            {1057, "FSL2_ERROR_MORE_DATA"},
            {1058, "FSL2_ERROR_USER_CANCELLED"},
            {1059, "FSL2_ERROR_UNSUPPORTED_VERSION"},
            {1060, "FSL2_ERROR_MULTIPLE_LAYERS_SAME_NAME"},
            {1062, "FSL2_ERROR_STRING_CHH_FAIL"},
            {1063, "FSL2_ERROR_CREATE_PROCESS"},
            {1064, "FSL2_ERROR_COM_ERROR"},
            {1065, "FSL2_ERROR_NO_SUCH_USER"},
            {1066, "FSL2_ERROR_NOT_RUNTIME_LAYER"},
            {1067, "FSL2_ERROR_INVALID_GUID"},
            {1068, "FSL2_ERROR_LAYER_BEING_EDITED"},
            {1069, "FSL2_ERROR_LAYER_NOT_BEING_EDITED"},
            {1070, "FSL2_ERROR_UNSUPPORTED_ARCHIVE_VER"},
            {1071, "FSL2_ERROR_AN_APP_STARTUP_FAILED"},
            {1072, "FSL2_PATH_TOO_LONG"},
            {1073, "FSL2_ERROR_NO_REDIR_VOLUMES"},
            {1074, "FSL2_ERROR_METADATA_VALUE"},
            {1076, "FSL2_ERROR_NOTIFY_VALUE_NOT_FOUND"},
            {1077, "FSL2_ERROR_AEXCLIENT_NOT_FOUND"},
            {1078, "FSL2_ERROR_LAYER_ATTR_VALUE_NOT_FOUND"},
            {1079, "FSL2_ERROR_METADATA_KEY"},
            {1080, "FSL2_ERROR_LAYER_ATTR_UNDEFINED"},
            {1081, "FSL2_ERROR_NOTIFY_EVENT_UNDEFINED"},
            {1082, "FSL2_ERROR_FILEPATH_NOT_FOUND"},
            {1083, "FSL2_ERROR_KEY_ALREADY_EXISTS"},
            {1084, "FSL2_ERROR_FILE_DELETE"},
            {1085, "FSL2_ERROR_SERVICE_DELETE"},
            {1086, "FSL2_ERROR_GROUP_NAME_INVALID"},
            {1087, "FSL2_ERROR_PROCESS_NOT_FOUND"},
            {1088, "FSL2_ERROR_STRING_MB_TO_WIDE"},
            {1089, "FSL2_ERROR_DRIVER_INVALID_STATE"},
            {1090, "FSL2_ERROR_VALUE_ALREADY_EXISTS"},
            {1091, "FSL2_ERROR_NOTIFY_SERVER_NOT_ENABLED"},
            {1092, "FSL2_ERROR_ARCHIVE_NOT_RUNTIME"},
            {1093, "FSL2_ERROR_FSLLIB32_UNINITIALIZED"},
            {1094, "FSL2_ERROR_CHILKAT_NOT_UNLOCKED"},
            {1095, "FSL2_ERROR_CHILKAT_ZIP_OPEN"},
            {1096, "FSL2_ERROR_ACTIVATE_DEPENDENT"},
            {1097, "FSL2_ERROR_EXPORT_REMOVE_SIDS"},
            {1098, "FSL2_ERROR_EXPORT_ADD_BACK_SIDS"},
            {1099, "FSL2_ERROR_INVALID_METAFILE"},
            {2000, "FSL2_ERROR_PRODUCT_KEY_INVALID"},
            {2001, "FSL2_ERROR_PRODUCT_KEY_NOT_YET_VALID"},
            {2002, "FSL2_PRODUCT_KEY_VALID"},
            {2003, "FSL2_PRODUCT_KEY_WILL_EXPIRE"},
            {2004, "FSL2_ERROR_PRODUCT_KEY_EXPIRED"},
            {2005, "FSL2_ERROR_NO_PRODUCT_KEY"},
            {2006, "FSL2_ERROR_DEPRECATED"}
        };
        #endregion
        #region HRESULTErrorCodes
        static readonly Dictionary<UInt64, string> HRESULTErrorCodes = new Dictionary<UInt64, string>
        {
            {0, "SUCCESS"},
            {1, "SUCCESS_FALSE"},
            {0x10000000, "HARDERROR_OVERRIDE_ERRORMODE"},
            {0x40000015, "STATUS_FATAL_APP_EXIT"},
            {0x8000000A, "E_PENDING"},
            {0x80004001, "E_NOTIMPL"},
            {0x80004002, "E_NOINTERFACE"},
            {0x80004003, "E_POINTER"},
            {0x80004004, "E_ABORT"},
            {0x80004005, "E_FAIL"},
            {0x80004006, "CO_E_INIT_TLS"},
            {0x80004007, "CO_E_INIT_SHARED_ALLOCATOR"},
            {0x80004008, "CO_E_INIT_MEMORY_ALLOCATOR"},
            {0x80004009, "CO_E_INIT_CLASS_CACHE"},
            {0x8000400A, "CO_E_INIT_RPC_CHANNEL"},
            {0x8000400B, "CO_E_INIT_TLS_SET_CHANNEL_CONTROL"},
            {0x8000400C, "CO_E_INIT_TLS_CHANNEL_CONTROL"},
            {0x8000400D, "CO_E_INIT_UNACCEPTED_USER_ALLOCATOR"},
            {0x8000400E, "CO_E_INIT_SCM_MUTEX_EXISTS"},
            {0x8000400F, "CO_E_INIT_SCM_FILE_MAPPING_EXISTS"},
            {0x80004010, "CO_E_INIT_SCM_MAP_VIEW_OF_FILE"},
            {0x80004011, "CO_E_INIT_SCM_EXEC_FAILURE"},
            {0x80004012, "CO_E_INIT_ONLY_SINGLE_THREADED"},
            {0x80004013, "CO_E_CANT_REMOTE"},
            {0x80004014, "CO_E_BAD_SERVER_NAME"},
            {0x80004015, "CO_E_WRONG_SERVER_IDENTITY"},
            {0x80004016, "CO_E_OLE1DDE_DISABLED"},
            {0x80004017, "CO_E_RUNAS_SYNTAX"},
            {0x80004018, "CO_E_CREATEPROCESS_FAILURE"},
            {0x80004019, "CO_E_RUNAS_CREATEPROCESS_FAILURE"},
            {0x8000401A, "CO_E_RUNAS_LOGON_FAILURE"},
            {0x8000401B, "CO_E_LAUNCH_PERMISSION_DENIED"},
            {0x8000401C, "CO_E_START_SERVICE_FAILURE"},
            {0x8000401D, "CO_E_REMOTE_COMMUNICATION_FAILURE"},
            {0x8000401E, "CO_E_SERVER_START_TIMEOUT"},
            {0x8000401F, "CO_E_CLSREG_INCONSISTENT"},
            {0x80004020, "CO_E_IIDREG_INCONSISTENT"},
            {0x80004021, "CO_E_NOT_SUPPORTED"},
            {0x80004022, "CO_E_RELOAD_DLL"},
            {0x80004023, "CO_E_MSI_ERROR"},
            {0x80004024, "CO_E_ATTEMPT_TO_CREATE_OUTSIDE_CLIENT_CONTEXT"},
            {0x80004025, "CO_E_SERVER_PAUSED"},
            {0x80004026, "CO_E_SERVER_NOT_PAUSED"},
            {0x80004027, "CO_E_CLASS_DISABLED"},
            {0x80004028, "CO_E_CLRNOTAVAILABLE"},
            {0x80004029, "CO_E_ASYNC_WORK_REJECTED"},
            {0x8000402A, "CO_E_SERVER_INIT_TIMEOUT"},
            {0x8000402B, "CO_E_NO_SECCTX_IN_ACTIVATE"},
            {0x80004030, "CO_E_TRACKER_CONFIG"},
            {0x80004031, "CO_E_THREADPOOL_CONFIG"},
            {0x80004032, "CO_E_SXS_CONFIG"},
            {0x80004033, "CO_E_MALFORMED_SPN"},
            {0x00041300, "SCHED_S_TASK_READY"},
            {0x00041301, "SCHED_S_TASK_RUNNING"},
            {0x00041302, "SCHED_S_TASK_DISABLED"},
            {0x00041303, "SCHED_S_TASK_HAS_NOT_RUN"},
            {0x00041304, "SCHED_S_TASK_NO_MORE_RUNS"},
            {0x00041305, "SCHED_S_TASK_NOT_SCHEDULED"},
            {0x00041306, "SCHED_S_TASK_TERMINATED"},
            {0x00041307, "SCHED_S_TASK_NO_VALID_TRIGGERS"},
            {0x00041308, "SCHED_S_EVENT_TRIGGER"},
            {0x80041309, "SCHED_E_TRIGGER_NOT_FOUND"},
            {0x8004130A, "SCHED_E_TASK_NOT_READY"},
            {0x8004130B, "SCHED_E_TASK_NOT_RUNNING"},
            {0x8004130C, "SCHED_E_SERVICE_NOT_INSTALLED"},
            {0x8004130D, "SCHED_E_CANNOT_OPEN_TASK"},
            {0x8004130E, "SCHED_E_INVALID_TASK"},
            {0x8004130F, "SCHED_E_ACCOUNT_INFORMATION_NOT_SET"},
            {0x80041310, "SCHED_E_ACCOUNT_NAME_NOT_FOUND"},
            {0x80041311, "SCHED_E_ACCOUNT_DBASE_CORRUPT"},
            {0x80041312, "SCHED_E_NO_SECURITY_SERVICES"},
            {0x80041313, "SCHED_E_UNKNOWN_OBJECT_VERSION"},
            {0x80041314, "SCHED_E_UNSUPPORTED_ACCOUNT_OPTION"},
            {0x80041315, "SCHED_E_SERVICE_NOT_RUNNING"},
            {0x80041316, "SCHED_E_UNEXPECTEDNODE"},
            {0x80041317, "SCHED_E_NAMESPACE"},
            {0x80041318, "SCHED_E_INVALIDVALUE"},
            {0x80041319, "SCHED_E_MISSINGNODE"},
            {0x8004131A, "SCHED_E_MALFORMEDXML"},
            {0x0004131B, "SCHED_S_SOME_TRIGGERS_FAILED"},
            {0x0004131C, "SCHED_S_BATCH_LOGON_PROBLEM"},
            {0x8004131D, "SCHED_E_TOO_MANY_NODES"},
            {0x8004131E, "SCHED_E_PAST_END_BOUNDARY"},
            {0x8004131F, "SCHED_E_ALREADY_RUNNING"},
            {0x80041320, "SCHED_E_USER_NOT_LOGGED_ON"},
            {0x80041321, "SCHED_E_INVALID_TASK_HASH"},
            {0x80041322, "SCHED_E_SERVICE_NOT_AVAILABLE"},
            {0x80041323, "SCHED_E_SERVICE_TOO_BUSY"},
            {0x80041324, "SCHED_E_TASK_ATTEMPTED"},
            {0x00041325, "SCHED_S_TASK_QUEUED"},
            {0x80041326, "SCHED_E_TASK_DISABLED"},
            {0x80041327, "SCHED_E_TASK_NOT_V1_COMPAT"},
            {0x80041328, "SCHED_E_START_ON_DEMAND"},
            {0x8000FFFF, "E_UNEXPECTED"},
            {0x80010001, "RPC_E_CALL_REJECTED"},
            {0x80010002, "RPC_E_CALL_CANCELED"},
            {0x80010003, "RPC_E_CANTPOST_INSENDCALL"},
            {0x80010004, "RPC_E_CANTCALLOUT_INASYNCCALL"},
            {0x80010005, "RPC_E_CANTCALLOUT_INEXTERNALCALL"},
            {0x80010006, "RPC_E_CONNECTION_TERMINATED"},
            {0x80010007, "RPC_E_SERVER_DIED"},
            {0x80010008, "RPC_E_CLIENT_DIED"},
            {0x80010009, "RPC_E_INVALID_DATAPACKET"},
            {0x8001000A, "RPC_E_CANTTRANSMIT_CALL"},
            {0x8001000B, "RPC_E_CLIENT_CANTMARSHAL_DATA"},
            {0x8001000C, "RPC_E_CLIENT_CANTUNMARSHAL_DATA"},
            {0x8001000D, "RPC_E_SERVER_CANTMARSHAL_DATA"},
            {0x8001000E, "RPC_E_SERVER_CANTUNMARSHAL_DATA"},
            {0x8001000F, "RPC_E_INVALID_DATA"},
            {0x80010010, "RPC_E_INVALID_PARAMETER"},
            {0x80010011, "RPC_E_CANTCALLOUT_AGAIN"},
            {0x80010012, "RPC_E_SERVER_DIED_DNE"},
            {0x80010100, "RPC_E_SYS_CALL_FAILED"},
            {0x80010101, "RPC_E_OUT_OF_RESOURCES"},
            {0x80010102, "RPC_E_ATTEMPTED_MULTITHREAD"},
            {0x80010103, "RPC_E_NOT_REGISTERED"},
            {0x80010104, "RPC_E_FAULT"},
            {0x80010105, "RPC_E_SERVERFAULT"},
            {0x80010106, "RPC_E_CHANGED_MODE"},
            {0x80010107, "RPC_E_INVALIDMETHOD"},
            {0x80010108, "RPC_E_DISCONNECTED"},
            {0x80010109, "RPC_E_RETRY"},
            {0x8001010A, "RPC_E_SERVERCALL_RETRYLATER"},
            {0x8001010B, "RPC_E_SERVERCALL_REJECTED"},
            {0x8001010C, "RPC_E_INVALID_CALLDATA"},
            {0x8001010D, "RPC_E_CANTCALLOUT_ININPUTSYNCCALL"},
            {0x8001010E, "RPC_E_WRONG_THREAD"},
            {0x8001010F, "RPC_E_THREAD_NOT_INIT"},
            {0x80010110, "RPC_E_VERSION_MISMATCH"},
            {0x80010111, "RPC_E_INVALID_HEADER"},
            {0x80010112, "RPC_E_INVALID_EXTENSION"},
            {0x80010113, "RPC_E_INVALID_IPID"},
            {0x80010114, "RPC_E_INVALID_OBJECT"},
            {0x80010115, "RPC_S_CALLPENDING"},
            {0x80010116, "RPC_S_WAITONTIMER"},
            {0x80010117, "RPC_E_CALL_COMPLETE"},
            {0x80010118, "RPC_E_UNSECURE_CALL"},
            {0x80010119, "RPC_E_TOO_LATE"},
            {0x8001011A, "RPC_E_NO_GOOD_SECURITY_PACKAGES"},
            {0x8001011B, "RPC_E_ACCESS_DENIED"},
            {0x8001011C, "RPC_E_REMOTE_DISABLED"},
            {0x8001011D, "RPC_E_INVALID_OBJREF"},
            {0x8001011E, "RPC_E_NO_CONTEXT"},
            {0x8001011F, "RPC_E_TIMEOUT"},
            {0x80010120, "RPC_E_NO_SYNC"},
            {0x8001FFFF, "RPC_E_UNEXPECTED"},
            {0x80020001, "DISP_E_UNKNOWNINTERFACE"},
            {0x80020003, "DISP_E_MEMBERNOTFOUND"},
            {0x80020004, "DISP_E_PARAMNOTFOUND"},
            {0x80020005, "DISP_E_TYPEMISMATCH"},
            {0x80020006, "DISP_E_UNKNOWNNAME"},
            {0x80020007, "DISP_E_NONAMEDARGS"},
            {0x80020008, "DISP_E_BADVARTYPE"},
            {0x80020009, "DISP_E_EXCEPTION"},
            {0x8002000A, "DISP_E_OVERFLOW"},
            {0x8002000B, "DISP_E_BADINDEX"},
            {0x8002000C, "DISP_E_UNKNOWNLCID"},
            {0x8002000D, "DISP_E_ARRAYISLOCKED"},
            {0x8002000E, "DISP_E_BADPARAMCOUNT"},
            {0x8002000F, "DISP_E_PARAMNOTOPTIONAL"},
            {0x80020010, "DISP_E_BADCALLEE"},
            {0x80020011, "DISP_E_NOTACOLLECTION"},
            {0x80020012, "DISP_E_DIVBYZERO"},
            {0x80028016, "TYPE_E_BUFFERTOOSMALL"},
            {0x80028017, "TYPE_E_FIELDNOTFOUND"},
            {0x80028018, "TYPE_E_INVDATAREAD"},
            {0x80028019, "TYPE_E_UNSUPFORMAT"},
            {0x8002801C, "TYPE_E_REGISTRYACCESS"},
            {0x8002801D, "TYPE_E_LIBNOTREGISTERED"},
            {0x80028027, "TYPE_E_UNDEFINEDTYPE"},
            {0x80028028, "TYPE_E_QUALIFIEDNAMEDISALLOWED"},
            {0x80028029, "TYPE_E_INVALIDSTATE"},
            {0x8002802A, "TYPE_E_WRONGTYPEKIND"},
            {0x8002802B, "TYPE_E_ELEMENTNOTFOUND"},
            {0x8002802C, "TYPE_E_AMBIGUOUSNAME"},
            {0x8002802D, "TYPE_E_NAMECONFLICT"},
            {0x8002802E, "TYPE_E_UNKNOWNLCID"},
            {0x8002802F, "TYPE_E_DLLFUNCTIONNOTFOUND"},
            {0x800288BD, "TYPE_E_BADMODULEKIND"},
            {0x800288C5, "TYPE_E_SIZETOOBIG"},
            {0x800288C6, "TYPE_E_DUPLICATEID"},
            {0x800288CF, "TYPE_E_INVALIDID"},
            {0x80028CA0, "TYPE_E_TYPEMISMATCH"},
            {0x80028CA1, "TYPE_E_OUTOFBOUNDS"},
            {0x80028CA2, "TYPE_E_IOERROR"},
            {0x80028CA3, "TYPE_E_CANTCREATETMPFILE"},
            {0x80029C4A, "TYPE_E_CANTLOADLIBRARY"},
            {0x80029C83, "TYPE_E_INCONSISTENTPROPFUNCS"},
            {0x80029C84, "TYPE_E_CIRCULARTYPE"},
            {0x00030200, "STG_S_CONVERTED"},
            {0x00030201, "STG_S_BLOCK"},
            {0x00030202, "STG_S_RETRYNOW"},
            {0x00030203, "STG_S_MONITORING"},
            {0x00030204, "STG_S_MULTIPLEOPENS"},
            {0x00030205, "STG_S_CONSOLIDATIONFAILED"},
            {0x00030206, "STG_S_CANNOTCONSOLIDATE"},
            {0x80030001, "STG_E_INVALIDFUNCTION"},
            {0x80030002, "STG_E_FILENOTFOUND"},
            {0x80030003, "STG_E_PATHNOTFOUND"},
            {0x80030004, "STG_E_TOOMANYOPENFILES"},
            {0x80030005, "STG_E_ACCESSDENIED"},
            {0x80030006, "STG_E_INVALIDHANDLE"},
            {0x80030008, "STG_E_INSUFFICIENTMEMORY"},
            {0x80030009, "STG_E_INVALIDPOINTER"},
            {0x80030012, "STG_E_NOMOREFILES"},
            {0x80030013, "STG_E_DISKISWRITEPROTECTED"},
            {0x80030019, "STG_E_SEEKERROR"},
            {0x8003001D, "STG_E_WRITEFAULT"},
            {0x8003001E, "STG_E_READFAULT"},
            {0x80030020, "STG_E_SHAREVIOLATION"},
            {0x80030021, "STG_E_LOCKVIOLATION"},
            {0x80030050, "STG_E_FILEALREADYEXISTS"},
            {0x80030057, "STG_E_INVALIDPARAMETER"},
            {0x80030070, "STG_E_MEDIUMFULL"},
            {0x800300FA, "STG_E_ABNORMALAPIEXIT"},
            {0x800300FB, "STG_E_INVALIDHEADER"},
            {0x800300FC, "STG_E_INVALIDNAME"},
            {0x800300FD, "STG_E_UNKNOWN"},
            {0x800300FE, "STG_E_UNIMPLEMENTEDFUNCTION"},
            {0x800300FF, "STG_E_INVALIDFLAG"},
            {0x80030100, "STG_E_INUSE"},
            {0x80030101, "STG_E_NOTCURRENT"},
            {0x80030102, "STG_E_REVERTED"},
            {0x80030103, "STG_E_CANTSAVE"},
            {0x80030104, "STG_E_OLDFORMAT"},
            {0x80030105, "STG_E_OLDDLL"},
            {0x80030106, "STG_E_SHAREREQUIRED"},
            {0x80030107, "STG_E_NOTFILEBASEDSTORAGE"},
            {0x80030108, "STG_E_EXTANTMARSHALLINGS"},
            {0x80030109, "STG_E_DOCFILECORRUPT"},
            {0x80030305, "STG_E_STATUS_COPY_PROTECTION_FAILURE"},
            {0x80030306, "STG_E_CSS_AUTHENTICATION_FAILURE"},
            {0x80030307, "STG_E_CSS_KEY_NOT_PRESENT"},
            {0x80030308, "STG_E_CSS_KEY_NOT_ESTABLISHED"},
            {0x80030309, "STG_E_CSS_SCRAMBLED_SECTOR"},
            {0x8003030A, "STG_E_CSS_REGION_MISMATCH"},
            {0x8003030B, "STG_E_RESETS_EXHAUSTED"},
            {0x00040000, "OLE_S_USEREG"},
            {0x00040001, "OLE_S_STATIC"},
            {0x00040002, "OLE_S_MAC_CLIPFORMAT"},
            {0x80040000, "OLE_E_OLEVERB"},
            {0x80040001, "OLE_E_ADVF"},
            {0x80040002, "OLE_E_ENUM_NOMORE"},
            {0x80040003, "OLE_E_ADVISENOTSUPPORTED"},
            {0x80040004, "OLE_E_NOCONNECTION"},
            {0x80040005, "OLE_E_NOTRUNNING"},
            {0x80040006, "OLE_E_NOCACHE"},
            {0x80040007, "OLE_E_BLANK"},
            {0x80040008, "OLE_E_CLASSDIFF"},
            {0x80040009, "OLE_E_CANT_GETMONIKER"},
            {0x8004000A, "OLE_E_CANT_BINDTOSOURCE"},
            {0x8004000B, "OLE_E_STATIC"},
            {0x8004000C, "OLE_E_PROMPTSAVECANCELLED"},
            {0x8004000D, "OLE_E_INVALIDRECT"},
            {0x8004000E, "OLE_E_WRONGCOMPOBJ"},
            {0x8004000F, "OLE_E_INVALIDHWND"},
            {0x80040010, "OLE_E_NOT_INPLACEACTIVE"},
            {0x80040011, "OLE_E_CANTCONVERT"},
            {0x80040012, "OLE_E_NOSTORAGE"},
            {0x80040064, "DV_E_FORMATETC"},
            {0x80040065, "DV_E_DVTARGETDEVICE"},
            {0x80040066, "DV_E_STGMEDIUM"},
            {0x80040067, "DV_E_STATDATA"},
            {0x80040068, "DV_E_LINDEX"},
            {0x80040069, "DV_E_TYMED"},
            {0x8004006A, "DV_E_CLIPFORMAT"},
            {0x8004006B, "DV_E_DVASPECT"},
            {0x8004006C, "DV_E_DVTARGETDEVICE_SIZE"},
            {0x8004006D, "DV_E_NOIVIEWOBJECT"},
            {0x00040100, "DRAGDROP_S_DROP"},
            {0x00040101, "DRAGDROP_S_CANCEL"},
            {0x00040102, "DRAGDROP_S_USEDEFAULTCURSORS"},
            {0x80040100, "DRAGDROP_E_NOTREGISTERED"},
            {0x80040101, "DRAGDROP_E_ALREADYREGISTERED"},
            {0x80040102, "DRAGDROP_E_INVALIDHWND"},
            {0x80040110, "CLASS_E_NOAGGREGATION"},
            {0x80040111, "CLASS_E_CLASSNOTAVAILABLE"},
            {0x80040112, "CLASS_E_NOTLICENSED"},
            {0x00040130, "DATA_S_SAMEFORMATETC"},
            {0x00040140, "VIEW_S_ALREADY_FROZEN"},
            {0x80040140, "VIEW_E_DRAW"},
            {0x80040150, "REGDB_E_READREGDB"},
            {0x80040151, "REGDB_E_WRITEREGDB"},
            {0x80040152, "REGDB_E_KEYMISSING"},
            {0x80040153, "REGDB_E_INVALIDVALUE"},
            {0x80040154, "REGDB_E_CLASSNOTREG"},
            {0x80040155, "REGDB_E_IIDNOTREG"},
            {0x80040160, "CAT_E_CATIDNOEXIST"},
            {0x80040161, "CAT_E_NODESCRIPTION"},
            {0x00040170, "CACHE_S_FORMATETC_NOTSUPPORTED"},
            {0x00040171, "CACHE_S_SAMECACHE"},
            {0x00040172, "CACHE_S_SOMECACHES_NOTUPDATED"},
            {0x80040170, "CACHE_E_NOCACHE_UPDATED"},
            {0x00040180, "OLEOBJ_S_INVALIDVERB"},
            {0x00040181, "OLEOBJ_S_CANNOT_DOVERB_NOW"},
            {0x00040182, "OLEOBJ_S_INVALIDHWND"},
            {0x80040180, "OLEOBJ_E_NOVERBS"},
            {0x80040181, "OLEOBJ_E_INVALIDVERB"},
            {0x000401A0, "INPLACE_S_TRUNCATED"},
            {0x800401A0, "INPLACE_E_NOTUNDOABLE"},
            {0x800401A1, "INPLACE_E_NOTOOLSPACE"},
            {0x000401C0, "CONVERT10_S_NO_PRESENTATION"},
            {0x800401C0, "CONVERT10_E_OLESTREAM_GET"},
            {0x800401C1, "CONVERT10_E_OLESTREAM_PUT"},
            {0x800401C2, "CONVERT10_E_OLESTREAM_FMT"},
            {0x800401C3, "CONVERT10_E_OLESTREAM_BITMAP_TO_DIB"},
            {0x800401C4, "CONVERT10_E_STG_FMT"},
            {0x800401C5, "CONVERT10_E_STG_NO_STD_STREAM"},
            {0x800401C6, "CONVERT10_E_STG_DIB_TO_BITMAP"},
            {0x800401D0, "CLIPBRD_E_CANT_OPEN"},
            {0x800401D1, "CLIPBRD_E_CANT_EMPTY"},
            {0x800401D2, "CLIPBRD_E_CANT_SET"},
            {0x800401D3, "CLIPBRD_E_BAD_DATA"},
            {0x800401D4, "CLIPBRD_E_CANT_CLOSE"},
            {0x000401E2, "MK_S_REDUCED_TO_SELF"},
            {0x000401E4, "MK_S_ME"},
            {0x000401E5, "MK_S_HIM"},
            {0x000401E6, "MK_S_US"},
            {0x000401E7, "MK_S_MONIKERALREADYREGISTERED"},
            {0x800401E0, "MK_E_CONNECTMANUALLY"},
            {0x800401E1, "MK_E_EXCEEDEDDEADLINE"},
            {0x800401E2, "MK_E_NEEDGENERIC"},
            {0x800401E3, "MK_E_UNAVAILABLE"},
            {0x800401E4, "MK_E_SYNTAX"},
            {0x800401E5, "MK_E_NOOBJECT"},
            {0x800401E6, "MK_E_INVALIDEXTENSION"},
            {0x800401E7, "MK_E_INTERMEDIATEINTERFACENOTSUPPORTED"},
            {0x800401E8, "MK_E_NOTBINDABLE"},
            {0x800401E9, "MK_E_NOTBOUND"},
            {0x800401EA, "MK_E_CANTOPENFILE"},
            {0x800401EB, "MK_E_MUSTBOTHERUSER"},
            {0x800401EC, "MK_E_NOINVERSE"},
            {0x800401ED, "MK_E_NOSTORAGE"},
            {0x800401EE, "MK_E_NOPREFIX"},
            {0x800401EF, "MK_E_ENUMERATION_FAILED"},
            {0x800401F0, "CO_E_NOTINITIALIZED"},
            {0x800401F1, "CO_E_ALREADYINITIALIZED"},
            {0x800401F2, "CO_E_CANTDETERMINECLASS"},
            {0x800401F3, "CO_E_CLASSSTRING"},
            {0x800401F4, "CO_E_IIDSTRING"},
            {0x800401F5, "CO_E_APPNOTFOUND"},
            {0x800401F6, "CO_E_APPSINGLEUSE"},
            {0x800401F7, "CO_E_ERRORINAPP"},
            {0x800401F8, "CO_E_DLLNOTFOUND"},
            {0x800401F9, "CO_E_ERRORINDLL"},
            {0x800401FA, "CO_E_WRONGOSFORAPP"},
            {0x800401FB, "CO_E_OBJNOTREG"},
            {0x800401FC, "CO_E_OBJISREG"},
            {0x800401FD, "CO_E_OBJNOTCONNECTED"},
            {0x800401FE, "CO_E_APPDIDNTREG"},
            {0x800401FF, "CO_E_RELEASED"},
            {0x80040200, "CO_E_FAILEDTOIMPERSONATE"},
            {0x80040201, "CO_E_FAILEDTOGETSECCTX"},
            {0x80040202, "CO_E_FAILEDTOOPENTHREADTOKEN"},
            {0x80040203, "CO_E_FAILEDTOGETTOKENINFO"},
            {0x80040204, "CO_E_TRUSTEEDOESNTMATCHCLIENT"},
            {0x80040205, "CO_E_FAILEDTOQUERYCLIENTBLANKET"},
            {0x80040206, "CO_E_FAILEDTOSETDACL"},
            {0x80040207, "CO_E_ACCESSCHECKFAILED"},
            {0x80040208, "CO_E_NETACCESSAPIFAILED"},
            {0x80040209, "CO_E_WRONGTRUSTEENAMESYNTAX"},
            {0x8004020A, "CO_E_INVALIDSID"},
            {0x8004020B, "CO_E_CONVERSIONFAILED"},
            {0x8004020C, "CO_E_NOMATCHINGSIDFOUND"},
            {0x8004020D, "CO_E_LOOKUPACCSIDFAILED"},
            {0x8004020E, "CO_E_NOMATCHINGNAMEFOUND"},
            {0x8004020F, "CO_E_LOOKUPACCNAMEFAILED"},
            {0x80040210, "CO_E_SETSERLHNDLFAILED"},
            {0x80040211, "CO_E_FAILEDTOGETWINDIR"},
            {0x80040212, "CO_E_PATHTOOLONG"},
            {0x80040213, "CO_E_FAILEDTOGENUUID"},
            {0x80040214, "CO_E_FAILEDTOCREATEFILE"},
            {0x80040215, "CO_E_FAILEDTOCLOSEHANDLE"},
            {0x80040216, "CO_E_EXCEEDSYSACLLIMIT"},
            {0x80040217, "CO_E_ACESINWRONGORDER"},
            {0x80040218, "CO_E_INCOMPATIBLESTREAMVERSION"},
            {0x80040219, "CO_E_FAILEDTOOPENPROCESSTOKEN"},
            {0x8004021A, "CO_E_DECODEFAILED"},
            {0x8004021B, "CO_E_ACNOTINITIALIZED"},
            {0x80070005, "E_ACCESSDENIED"},
            {0x80070006, "E_HANDLE"},
            {0x8007000E, "E_OUTOFMEMORY"},
            {0x80070057, "E_INVALIDARG"},
            {0x00080012, "CO_S_NOTALLINTERFACES"},
            {0x80080001, "CO_E_CLASS_CREATE_FAILED"},
            {0x80080002, "CO_E_SCM_ERROR"},
            {0x80080003, "CO_E_SCM_RPC_FAILURE"},
            {0x80080004, "CO_E_BAD_PATH"},
            {0x80080005, "CO_E_SERVER_EXEC_FAILURE"},
            {0x80080006, "CO_E_OBJSRV_RPC_FAILURE"},
            {0x80080007, "MK_E_NO_NORMALIZED"},
            {0x80080008, "CO_E_SERVER_STOPPING"},
            {0x80080009, "MEM_E_INVALID_ROOT"},
            {0x80080010, "MEM_E_INVALID_LINK"},
            {0x80080011, "MEM_E_INVALID_SIZE"},
            {0x80090001, "NTE_BAD_UID"},
            {0x80090002, "NTE_BAD_HASH"},
            {0x80090003, "NTE_BAD_KEY"},
            {0x80090004, "NTE_BAD_LEN"},
            {0x80090005, "NTE_BAD_DATA"},
            {0x80090006, "NTE_BAD_SIGNATURE"},
            {0x80090007, "NTE_BAD_VER"},
            {0x80090008, "NTE_BAD_ALGID"},
            {0x80090009, "NTE_BAD_FLAGS"},
            {0x8009000A, "NTE_BAD_TYPE"},
            {0x8009000B, "NTE_BAD_KEY_STATE"},
            {0x8009000C, "NTE_BAD_HASH_STATE"},
            {0x8009000D, "NTE_NO_KEY"},
            {0x8009000E, "NTE_NO_MEMORY"},
            {0x8009000F, "NTE_EXISTS"},
            {0x80090010, "NTE_PERM"},
            {0x80090011, "NTE_NOT_FOUND"},
            {0x80090012, "NTE_DOUBLE_ENCRYPT"},
            {0x80090013, "NTE_BAD_PROVIDER"},
            {0x80090014, "NTE_BAD_PROV_TYPE"},
            {0x80090015, "NTE_BAD_PUBLIC_KEY"},
            {0x80090016, "NTE_BAD_KEYSET"},
            {0x80090017, "NTE_PROV_TYPE_NOT_DEF"},
            {0x80090018, "NTE_PROV_TYPE_ENTRY_BAD"},
            {0x80090019, "NTE_KEYSET_NOT_DEF"},
            {0x8009001A, "NTE_KEYSET_ENTRY_BAD"},
            {0x8009001B, "NTE_PROV_TYPE_NO_MATCH"},
            {0x8009001C, "NTE_SIGNATURE_FILE_BAD"},
            {0x8009001D, "NTE_PROVIDER_DLL_FAIL"},
            {0x8009001E, "NTE_PROV_DLL_NOT_FOUND"},
            {0x8009001F, "NTE_BAD_KEYSET_PARAM"},
            {0x80090020, "NTE_FAIL"},
            {0x80090021, "NTE_SYS_ERR"},
            {0x80090300, "SEC_E_INSUFFICIENT_MEMORY"},
            {0x80090301, "SEC_E_INVALID_HANDLE"},
            {0x80090302, "SEC_E_UNSUPPORTED_FUNCTION"},
            {0x80090303, "SEC_E_TARGET_UNKNOWN"},
            {0x80090304, "SEC_E_INTERNAL_ERROR"},
            {0x80090305, "SEC_E_SECPKG_NOT_FOUND"},
            {0x80090306, "SEC_E_NOT_OWNER"},
            {0x80090307, "SEC_E_CANNOT_INSTALL"},
            {0x80090308, "SEC_E_INVALID_TOKEN"},
            {0x80090309, "SEC_E_CANNOT_PACK"},
            {0x8009030A, "SEC_E_QOP_NOT_SUPPORTED"},
            {0x8009030B, "SEC_E_NO_IMPERSONATION"},
            {0x8009030C, "SEC_E_LOGON_DENIED"},
            {0x8009030D, "SEC_E_UNKNOWN_CREDENTIALS"},
            {0x8009030E, "SEC_E_NO_CREDENTIALS"},
            {0x8009030F, "SEC_E_MESSAGE_ALTERED"},
            {0x80090310, "SEC_E_OUT_OF_SEQUENCE"},
            {0x80090311, "SEC_E_NO_AUTHENTICATING_AUTHORITY"},
            {0x00090312, "SEC_I_CONTINUE_NEEDED"},
            {0x00090313, "SEC_I_COMPLETE_NEEDED"},
            {0x00090314, "SEC_I_COMPLETE_AND_CONTINUE"},
            {0x00090315, "SEC_I_LOCAL_LOGON"},
            {0x80090316, "SEC_E_BAD_PKGID"},
            {0x80090317, "SEC_E_CONTEXT_EXPIRED"},
            {0x00090317, "SEC_I_CONTEXT_EXPIRED"},
            {0x80090318, "SEC_E_INCOMPLETE_MESSAGE"},
            {0x80090320, "SEC_E_INCOMPLETE_CREDENTIALS"},
            {0x00090320, "SEC_I_INCOMPLETE_CREDENTIALS"},
            {0x80090321, "SEC_E_BUFFER_TOO_SMALL"},
            {0x00090321, "SEC_I_RENEGOTIATE"},
            {0x80090322, "SEC_E_WRONG_PRINCIPAL"},
            {0x00090323, "SEC_I_NO_LSA_CONTEXT"},
            {0x80090324, "SEC_E_TIME_SKEW"},
            {0x80090325, "SEC_E_UNTRUSTED_ROOT"},
            {0x80090326, "SEC_E_ILLEGAL_MESSAGE"},
            {0x80090327, "SEC_E_CERT_UNKNOWN"},
            {0x80090328, "SEC_E_CERT_EXPIRED"},
            {0x80090329, "SEC_E_ENCRYPT_FAILURE"},
            {0x80090330, "SEC_E_DECRYPT_FAILURE"},
            {0x80090331, "SEC_E_ALGORITHM_MISMATCH"},
            {0x80090332, "SEC_E_SECURITY_QOS_FAILED"},
            {0x80090333, "SEC_E_UNFINISHED_CONTEXT_DELETED"},
            {0x80090334, "SEC_E_NO_TGT_REPLY"},
            {0x80090335, "SEC_E_NO_IP_ADDRESSES"},
            {0x80090336, "SEC_E_WRONG_CREDENTIAL_HANDLE"},
            {0x80090337, "SEC_E_CRYPTO_SYSTEM_INVALID"},
            {0x80090338, "SEC_E_MAX_REFERRALS_EXCEEDED"},
            {0x80090339, "SEC_E_MUST_BE_KDC"},
            {0x8009033A, "SEC_E_STRONG_CRYPTO_NOT_SUPPORTED"},
            {0x8009033B, "SEC_E_TOO_MANY_PRINCIPALS"},
            {0x8009033C, "SEC_E_NO_PA_DATA"},
            {0x8009033D, "SEC_E_PKINIT_NAME_MISMATCH"},
            {0x8009033E, "SEC_E_SMARTCARD_LOGON_REQUIRED"},
            {0x8009033F, "SEC_E_SHUTDOWN_IN_PROGRESS"},
            {0x80090340, "SEC_E_KDC_INVALID_REQUEST"},
            {0x80090341, "SEC_E_KDC_UNABLE_TO_REFER"},
            {0x80090342, "SEC_E_KDC_UNKNOWN_ETYPE"},
            {0x80090343, "SEC_E_UNSUPPORTED_PREAUTH"},
            {0x80090345, "SEC_E_DELEGATION_REQUIRED"},
            {0x80090346, "SEC_E_BAD_BINDINGS"},
            {0x80090347, "SEC_E_MULTIPLE_ACCOUNTS"},
            {0x80090348, "SEC_E_NO_KERB_KEY"},
            {0x80090349, "SEC_E_CERT_WRONG_USAGE"},
            {0x80090350, "SEC_E_DOWNGRADE_DETECTED"},
            {0x80090351, "SEC_E_SMARTCARD_CERT_REVOKED"},
            {0x80090352, "SEC_E_ISSUING_CA_UNTRUSTED"},
            {0x80090353, "SEC_E_REVOCATION_OFFLINE_C"},
            {0x80090354, "SEC_E_PKINIT_CLIENT_FAILURE"},
            {0x80090355, "SEC_E_SMARTCARD_CERT_EXPIRED"},
            {0x80090356, "SEC_E_NO_S4U_PROT_SUPPORT"},
            {0x80090357, "SEC_E_CROSSREALM_DELEGATION_FAILURE"},
            {0x80090358, "SEC_E_REVOCATION_OFFLINE_KDC"},
            {0x80090359, "SEC_E_ISSUING_CA_UNTRUSTED_KDC"},
            {0x8009035A, "SEC_E_KDC_CERT_EXPIRED"},
            {0x8009035B, "SEC_E_KDC_CERT_REVOKED"},
            {0x80091001, "CRYPT_E_MSG_ERROR"},
            {0x80091002, "CRYPT_E_UNKNOWN_ALGO"},
            {0x80091003, "CRYPT_E_OID_FORMAT"},
            {0x80091004, "CRYPT_E_INVALID_MSG_TYPE"},
            {0x80091005, "CRYPT_E_UNEXPECTED_ENCODING"},
            {0x80091006, "CRYPT_E_AUTH_ATTR_MISSING"},
            {0x80091007, "CRYPT_E_HASH_VALUE"},
            {0x80091008, "CRYPT_E_INVALID_INDEX"},
            {0x80091009, "CRYPT_E_ALREADY_DECRYPTED"},
            {0x8009100A, "CRYPT_E_NOT_DECRYPTED"},
            {0x8009100B, "CRYPT_E_RECIPIENT_NOT_FOUND"},
            {0x8009100C, "CRYPT_E_CONTROL_TYPE"},
            {0x8009100D, "CRYPT_E_ISSUER_SERIALNUMBER"},
            {0x8009100E, "CRYPT_E_SIGNER_NOT_FOUND"},
            {0x8009100F, "CRYPT_E_ATTRIBUTES_MISSING"},
            {0x80091010, "CRYPT_E_STREAM_MSG_NOT_READY"},
            {0x80091011, "CRYPT_E_STREAM_INSUFFICIENT_DATA"},
            {0x80091012, "CRYPT_I_NEW_PROTECTION_REQUIRED"},
            {0x80092001, "CRYPT_E_BAD_LEN"},
            {0x80092002, "CRYPT_E_BAD_ENCODE"},
            {0x80092003, "CRYPT_E_FILE_ERROR"},
            {0x80092004, "CRYPT_E_NOT_FOUND"},
            {0x80092005, "CRYPT_E_EXISTS"},
            {0x80092006, "CRYPT_E_NO_PROVIDER"},
            {0x80092007, "CRYPT_E_SELF_SIGNED"},
            {0x80092008, "CRYPT_E_DELETED_PREV"},
            {0x80092009, "CRYPT_E_NO_MATCH"},
            {0x8009200A, "CRYPT_E_UNEXPECTED_MSG_TYPE"},
            {0x8009200B, "CRYPT_E_NO_KEY_PROPERTY"},
            {0x8009200C, "CRYPT_E_NO_DECRYPT_CERT"},
            {0x8009200D, "CRYPT_E_BAD_MSG"},
            {0x8009200E, "CRYPT_E_NO_SIGNER"},
            {0x8009200F, "CRYPT_E_PENDING_CLOSE"},
            {0x80092010, "CRYPT_E_REVOKED"},
            {0x80092011, "CRYPT_E_NO_REVOCATION_DLL"},
            {0x80092012, "CRYPT_E_NO_REVOCATION_CHECK"},
            {0x80092013, "CRYPT_E_REVOCATION_OFFLINE"},
            {0x80092014, "CRYPT_E_NOT_IN_REVOCATION_DATABASE"},
            {0x80092020, "CRYPT_E_INVALID_NUMERIC_STRING"},
            {0x80092021, "CRYPT_E_INVALID_PRINTABLE_STRING"},
            {0x80092022, "CRYPT_E_INVALID_IA5_STRING"},
            {0x80092023, "CRYPT_E_INVALID_X500_STRING"},
            {0x80092024, "CRYPT_E_NOT_CHAR_STRING"},
            {0x80092025, "CRYPT_E_FILERESIZED"},
            {0x80092026, "CRYPT_E_SECURITY_SETTINGS"},
            {0x80092027, "CRYPT_E_NO_VERIFY_USAGE_DLL"},
            {0x80092028, "CRYPT_E_NO_VERIFY_USAGE_CHECK"},
            {0x80092029, "CRYPT_E_VERIFY_USAGE_OFFLINE"},
            {0x8009202A, "CRYPT_E_NOT_IN_CTL"},
            {0x8009202B, "CRYPT_E_NO_TRUSTED_SIGNER"},
            {0x8009202C, "CRYPT_E_MISSING_PUBKEY_PARA"},
            {0x80093000, "CRYPT_E_OSS_ERROR"},
            {0x80093001, "OSS_MORE_BUF"},
            {0x80093002, "OSS_NEGATIVE_UINTEGER"},
            {0x80093003, "OSS_PDU_RANGE"},
            {0x80093004, "OSS_MORE_INPUT"},
            {0x80093005, "OSS_DATA_ERROR"},
            {0x80093006, "OSS_BAD_ARG"},
            {0x80093007, "OSS_BAD_VERSION"},
            {0x80093008, "OSS_OUT_MEMORY"},
            {0x80093009, "OSS_PDU_MISMATCH"},
            {0x8009300A, "OSS_LIMITED"},
            {0x8009300B, "OSS_BAD_PTR"},
            {0x8009300C, "OSS_BAD_TIME"},
            {0x8009300D, "OSS_INDEFINITE_NOT_SUPPORTED"},
            {0x8009300E, "OSS_MEM_ERROR"},
            {0x8009300F, "OSS_BAD_TABLE"},
            {0x80093010, "OSS_TOO_LONG"},
            {0x80093011, "OSS_CONSTRAINT_VIOLATED"},
            {0x80093012, "OSS_FATAL_ERROR"},
            {0x80093013, "OSS_ACCESS_SERIALIZATION_ERROR"},
            {0x80093014, "OSS_NULL_TBL"},
            {0x80093015, "OSS_NULL_FCN"},
            {0x80093016, "OSS_BAD_ENCRULES"},
            {0x80093017, "OSS_UNAVAIL_ENCRULES"},
            {0x80093018, "OSS_CANT_OPEN_TRACE_WINDOW"},
            {0x80093019, "OSS_UNIMPLEMENTED"},
            {0x8009301A, "OSS_OID_DLL_NOT_LINKED"},
            {0x8009301B, "OSS_CANT_OPEN_TRACE_FILE"},
            {0x8009301C, "OSS_TRACE_FILE_ALREADY_OPEN"},
            {0x8009301D, "OSS_TABLE_MISMATCH"},
            {0x8009301E, "OSS_TYPE_NOT_SUPPORTED"},
            {0x8009301F, "OSS_REAL_DLL_NOT_LINKED"},
            {0x80093020, "OSS_REAL_CODE_NOT_LINKED"},
            {0x80093021, "OSS_OUT_OF_RANGE"},
            {0x80093022, "OSS_COPIER_DLL_NOT_LINKED"},
            {0x80093023, "OSS_CONSTRAINT_DLL_NOT_LINKED"},
            {0x80093024, "OSS_COMPARATOR_DLL_NOT_LINKED"},
            {0x80093025, "OSS_COMPARATOR_CODE_NOT_LINKED"},
            {0x80093026, "OSS_MEM_MGR_DLL_NOT_LINKED"},
            {0x80093027, "OSS_PDV_DLL_NOT_LINKED"},
            {0x80093028, "OSS_PDV_CODE_NOT_LINKED"},
            {0x80093029, "OSS_API_DLL_NOT_LINKED"},
            {0x8009302A, "OSS_BERDER_DLL_NOT_LINKED"},
            {0x8009302B, "OSS_PER_DLL_NOT_LINKED"},
            {0x8009302C, "OSS_OPEN_TYPE_ERROR"},
            {0x8009302D, "OSS_MUTEX_NOT_CREATED"},
            {0x8009302E, "OSS_CANT_CLOSE_TRACE_FILE"},
            {0x80093100, "CRYPT_E_ASN1_ERROR"},
            {0x80093101, "CRYPT_E_ASN1_INTERNAL"},
            {0x80093102, "CRYPT_E_ASN1_EOD"},
            {0x80093103, "CRYPT_E_ASN1_CORRUPT"},
            {0x80093104, "CRYPT_E_ASN1_LARGE"},
            {0x80093105, "CRYPT_E_ASN1_CONSTRAINT"},
            {0x80093106, "CRYPT_E_ASN1_MEMORY"},
            {0x80093107, "CRYPT_E_ASN1_OVERFLOW"},
            {0x80093108, "CRYPT_E_ASN1_BADPDU"},
            {0x80093109, "CRYPT_E_ASN1_BADARGS"},
            {0x8009310A, "CRYPT_E_ASN1_BADREAL"},
            {0x8009310B, "CRYPT_E_ASN1_BADTAG"},
            {0x8009310C, "CRYPT_E_ASN1_CHOICE"},
            {0x8009310D, "CRYPT_E_ASN1_RULE"},
            {0x8009310E, "CRYPT_E_ASN1_UTF8"},
            {0x80093133, "CRYPT_E_ASN1_PDU_TYPE"},
            {0x80093134, "CRYPT_E_ASN1_NYI"},
            {0x80093201, "CRYPT_E_ASN1_EXTENDED"},
            {0x80093202, "CRYPT_E_ASN1_NOEOD"},
            {0x80096001, "TRUST_E_SYSTEM_ERROR"},
            {0x80096002, "TRUST_E_NO_SIGNER_CERT"},
            {0x80096003, "TRUST_E_COUNTER_SIGNER"},
            {0x80096004, "TRUST_E_CERT_SIGNATURE"},
            {0x80096005, "TRUST_E_TIME_STAMP"},
            {0x80096010, "TRUST_E_BAD_DIGEST"},
            {0x80096019, "TRUST_E_BASIC_CONSTRAINTS"},
            {0x8009601E, "TRUST_E_FINANCIAL_CRITERIA"},
            {0x800B0001, "TRUST_E_PROVIDER_UNKNOWN"},
            {0x800B0002, "TRUST_E_ACTION_UNKNOWN"},
            {0x800B0003, "TRUST_E_SUBJECT_FORM_UNKNOWN"},
            {0x800B0004, "TRUST_E_SUBJECT_NOT_TRUSTED"},
            {0x800B0100, "TRUST_E_NOSIGNATURE"},
            {0x800B0101, "CERT_E_EXPIRED"},
            {0x800B0102, "CERT_E_VALIDITYPERIODNESTING"},
            {0x800B0103, "CERT_E_ROLE"},
            {0x800B0104, "CERT_E_PATHLENCONST"},
            {0x800B0105, "CERT_E_CRITICAL"},
            {0x800B0106, "CERT_E_PURPOSE"},
            {0x800B0107, "CERT_E_ISSUERCHAINING"},
            {0x800B0108, "CERT_E_MALFORMED"},
            {0x800B0109, "CERT_E_UNTRUSTEDROOT"},
            {0x800B010A, "CERT_E_CHAINING"},
            {0x800B010B, "TRUST_E_FAIL"},
            {0x800B010C, "CERT_E_REVOKED"},
            {0x800B010D, "CERT_E_UNTRUSTEDTESTROOT"},
            {0x800B010E, "CERT_E_REVOCATION_FAILURE"},
            {0x800B010F, "CERT_E_CN_NO_MATCH"},
            {0x800B0110, "CERT_E_WRONG_USAGE"},
            {0x800B0111, "TRUST_E_EXPLICIT_DISTRUST"},
            {0x800B0112, "CERT_E_UNTRUSTEDCA"},
            {0x800B0113, "CERT_E_INVALID_POLICY"},
            {0x800B0114, "CERT_E_INVALID_NAME"},
            {0x800F0000, "SPAPI_E_EXPECTED_SECTION_NAME"},
            {0x800F0001, "SPAPI_E_BAD_SECTION_NAME_LINE"},
            {0x800F0002, "SPAPI_E_SECTION_NAME_TOO_LONG"},
            {0x800F0003, "SPAPI_E_GENERAL_SYNTAX"},
            {0x800F0100, "SPAPI_E_WRONG_INF_STYLE"},
            {0x800F0101, "SPAPI_E_SECTION_NOT_FOUND"},
            {0x800F0102, "SPAPI_E_LINE_NOT_FOUND"},
            {0x800F0103, "SPAPI_E_NO_BACKUP"},
            {0x800F0200, "SPAPI_E_NO_ASSOCIATED_CLASS"},
            {0x800F0201, "SPAPI_E_CLASS_MISMATCH"},
            {0x800F0202, "SPAPI_E_DUPLICATE_FOUND"},
            {0x800F0203, "SPAPI_E_NO_DRIVER_SELECTED"},
            {0x800F0204, "SPAPI_E_KEY_DOES_NOT_EXIST"},
            {0x800F0205, "SPAPI_E_INVALID_DEVINST_NAME"},
            {0x800F0206, "SPAPI_E_INVALID_CLASS"},
            {0x800F0207, "SPAPI_E_DEVINST_ALREADY_EXISTS"},
            {0x800F0208, "SPAPI_E_DEVINFO_NOT_REGISTERED"},
            {0x800F0209, "SPAPI_E_INVALID_REG_PROPERTY"},
            {0x800F020A, "SPAPI_E_NO_INF"},
            {0x800F020B, "SPAPI_E_NO_SUCH_DEVINST"},
            {0x800F020C, "SPAPI_E_CANT_LOAD_CLASS_ICON"},
            {0x800F020D, "SPAPI_E_INVALID_CLASS_INSTALLER"},
            {0x800F020E, "SPAPI_E_DI_DO_DEFAULT"},
            {0x800F020F, "SPAPI_E_DI_NOFILECOPY"},
            {0x800F0210, "SPAPI_E_INVALID_HWPROFILE"},
            {0x800F0211, "SPAPI_E_NO_DEVICE_SELECTED"},
            {0x800F0212, "SPAPI_E_DEVINFO_LIST_LOCKED"},
            {0x800F0213, "SPAPI_E_DEVINFO_DATA_LOCKED"},
            {0x800F0214, "SPAPI_E_DI_BAD_PATH"},
            {0x800F0215, "SPAPI_E_NO_CLASSINSTALL_PARAMS"},
            {0x800F0216, "SPAPI_E_FILEQUEUE_LOCKED"},
            {0x800F0217, "SPAPI_E_BAD_SERVICE_INSTALLSECT"},
            {0x800F0218, "SPAPI_E_NO_CLASS_DRIVER_LIST"},
            {0x800F0219, "SPAPI_E_NO_ASSOCIATED_SERVICE"},
            {0x800F021A, "SPAPI_E_NO_DEFAULT_DEVICE_INTERFACE"},
            {0x800F021B, "SPAPI_E_DEVICE_INTERFACE_ACTIVE"},
            {0x800F021C, "SPAPI_E_DEVICE_INTERFACE_REMOVED"},
            {0x800F021D, "SPAPI_E_BAD_INTERFACE_INSTALLSECT"},
            {0x800F021E, "SPAPI_E_NO_SUCH_INTERFACE_CLASS"},
            {0x800F021F, "SPAPI_E_INVALID_REFERENCE_STRING"},
            {0x800F0220, "SPAPI_E_INVALID_MACHINENAME"},
            {0x800F0221, "SPAPI_E_REMOTE_COMM_FAILURE"},
            {0x800F0222, "SPAPI_E_MACHINE_UNAVAILABLE"},
            {0x800F0223, "SPAPI_E_NO_CONFIGMGR_SERVICES"},
            {0x800F0224, "SPAPI_E_INVALID_PROPPAGE_PROVIDER"},
            {0x800F0225, "SPAPI_E_NO_SUCH_DEVICE_INTERFACE"},
            {0x800F0226, "SPAPI_E_DI_POSTPROCESSING_REQUIRED"},
            {0x800F0227, "SPAPI_E_INVALID_COINSTALLER"},
            {0x800F0228, "SPAPI_E_NO_COMPAT_DRIVERS"},
            {0x800F0229, "SPAPI_E_NO_DEVICE_ICON"},
            {0x800F022A, "SPAPI_E_INVALID_INF_LOGCONFIG"},
            {0x800F022B, "SPAPI_E_DI_DONT_INSTALL"},
            {0x800F022C, "SPAPI_E_INVALID_FILTER_DRIVER"},
            {0x800F022D, "SPAPI_E_NON_WINDOWS_NT_DRIVER"},
            {0x800F022E, "SPAPI_E_NON_WINDOWS_DRIVER"},
            {0x800F022F, "SPAPI_E_NO_CATALOG_FOR_OEM_INF"},
            {0x800F0230, "SPAPI_E_DEVINSTALL_QUEUE_NONNATIVE"},
            {0x800F0231, "SPAPI_E_NOT_DISABLEABLE"},
            {0x800F0232, "SPAPI_E_CANT_REMOVE_DEVINST"},
            {0x800F0233, "SPAPI_E_INVALID_TARGET"},
            {0x800F0234, "SPAPI_E_DRIVER_NONNATIVE"},
            {0x800F0235, "SPAPI_E_IN_WOW64"},
            {0x800F0236, "SPAPI_E_SET_SYSTEM_RESTORE_POINT"},
            {0x800F0237, "SPAPI_E_INCORRECTLY_COPIED_INF"},
            {0x800F0238, "SPAPI_E_SCE_DISABLED"},
            {0x800F1000, "SPAPI_E_ERROR_NOT_INSTALLED"},
            {0x80100001, "SCARD_F_INTERNAL_ERROR"},
            {0x80100002, "SCARD_E_CANCELLED"},
            {0x80100003, "SCARD_E_INVALID_HANDLE"},
            {0x80100004, "SCARD_E_INVALID_PARAMETER"},
            {0x80100005, "SCARD_E_INVALID_TARGET"},
            {0x80100006, "SCARD_E_NO_MEMORY"},
            {0x80100007, "SCARD_F_WAITED_TOO_LONG"},
            {0x80100008, "SCARD_E_INSUFFICIENT_BUFFER"},
            {0x80100009, "SCARD_E_UNKNOWN_READER"},
            {0x8010000A, "SCARD_E_TIMEOUT"},
            {0x8010000B, "SCARD_E_SHARING_VIOLATION"},
            {0x8010000C, "SCARD_E_NO_SMARTCARD"},
            {0x8010000D, "SCARD_E_UNKNOWN_CARD"},
            {0x8010000E, "SCARD_E_CANT_DISPOSE"},
            {0x8010000F, "SCARD_E_PROTO_MISMATCH"},
            {0x80100010, "SCARD_E_NOT_READY"},
            {0x80100011, "SCARD_E_INVALID_VALUE"},
            {0x80100012, "SCARD_E_SYSTEM_CANCELLED"},
            {0x80100013, "SCARD_F_COMM_ERROR"},
            {0x80100014, "SCARD_F_UNKNOWN_ERROR"},
            {0x80100015, "SCARD_E_INVALID_ATR"},
            {0x80100016, "SCARD_E_NOT_TRANSACTED"},
            {0x80100017, "SCARD_E_READER_UNAVAILABLE"},
            {0x80100018, "SCARD_P_SHUTDOWN"},
            {0x80100019, "SCARD_E_PCI_TOO_SMALL"},
            {0x8010001A, "SCARD_E_READER_UNSUPPORTED"},
            {0x8010001B, "SCARD_E_DUPLICATE_READER"},
            {0x8010001C, "SCARD_E_CARD_UNSUPPORTED"},
            {0x8010001D, "SCARD_E_NO_SERVICE"},
            {0x8010001E, "SCARD_E_SERVICE_STOPPED"},
            {0x8010001F, "SCARD_E_UNEXPECTED"},
            {0x80100020, "SCARD_E_ICC_INSTALLATION"},
            {0x80100021, "SCARD_E_ICC_CREATEORDER"},
            {0x80100022, "SCARD_E_UNSUPPORTED_FEATURE"},
            {0x80100023, "SCARD_E_DIR_NOT_FOUND"},
            {0x80100024, "SCARD_E_FILE_NOT_FOUND"},
            {0x80100025, "SCARD_E_NO_DIR"},
            {0x80100026, "SCARD_E_NO_FILE"},
            {0x80100027, "SCARD_E_NO_ACCESS"},
            {0x80100028, "SCARD_E_WRITE_TOO_MANY"},
            {0x80100029, "SCARD_E_BAD_SEEK"},
            {0x8010002A, "SCARD_E_INVALID_CHV"},
            {0x8010002B, "SCARD_E_UNKNOWN_RES_MNG"},
            {0x8010002C, "SCARD_E_NO_SUCH_CERTIFICATE"},
            {0x8010002D, "SCARD_E_CERTIFICATE_UNAVAILABLE"},
            {0x8010002E, "SCARD_E_NO_READERS_AVAILABLE"},
            {0x8010002F, "SCARD_E_COMM_DATA_LOST"},
            {0x80100030, "SCARD_E_NO_KEY_CONTAINER"},
            {0x80100031, "SCARD_E_SERVER_TOO_BUSY"},
            {0x80100065, "SCARD_W_UNSUPPORTED_CARD"},
            {0x80100066, "SCARD_W_UNRESPONSIVE_CARD"},
            {0x80100067, "SCARD_W_UNPOWERED_CARD"},
            {0x80100068, "SCARD_W_RESET_CARD"},
            {0x80100069, "SCARD_W_REMOVED_CARD"},
            {0x8010006A, "SCARD_W_SECURITY_VIOLATION"},
            {0x8010006B, "SCARD_W_WRONG_CHV"},
            {0x8010006C, "SCARD_W_CHV_BLOCKED"},
            {0x8010006D, "SCARD_W_EOF"},
            {0x8010006E, "SCARD_W_CANCELLED_BY_USER"},
            {0x8010006F, "SCARD_W_CARD_NOT_AUTHENTICATED"},
            {0x80100070, "SCARD_W_CACHE_ITEM_NOT_FOUND"},
            {0x80100071, "SCARD_W_CACHE_ITEM_STALE"},
            {0x80100072, "SCARD_W_CACHE_ITEM_TOO_BIG"},
            {0xC0090001, "ERROR_AUDITING_DISABLED"},
            {0xC0090002, "ERROR_ALL_SIDS_FILTERED"}
        };
        #endregion
        #endregion


        static public string HresultErrorToString(UInt64 errorCode)
        {
            string errorString;
            if (!HRESULTErrorCodes.TryGetValue(errorCode, out errorString))
            {
                errorString = "0x" + errorCode.ToString("X");
            }
            return errorString;
        }
        static public string NtStatusToString(UInt64 ntstatus)
        {
            string errorString;
            if (!NtStatusCodes.TryGetValue(ntstatus, out errorString))
            {
                errorString = "0x" + ntstatus.ToString("X");
            }
            return errorString;
        }
        static public string FslErrorToString(uint fslErrorCode)
        {
            string errorString;
            if (!FslErrorCodes.TryGetValue(fslErrorCode, out errorString))
            {
                errorString = "0x" + fslErrorCode.ToString("X");
            }
            return errorString;
        }

    }
    
    #endregion

    #region StringHelpers
    public class StringHelpers
    {
        /// <summary>
        /// Removes control characters and other non-UTF-8 characters
        /// </summary>
        /// <param name="inString">The string to process</param>
        /// <returns>A string with no control characters or entities above 0x00FD</returns>
        public static string RemoveTroublesomeCharacters(string inString)
        {
            if (inString == null) return null;

            var newString = new StringBuilder();
            char ch;

            for (int i = 0; i < inString.Length; i++)
            {

                ch = inString[i];
                // remove any characters outside the valid UTF-8 range as well as all control characters
                // except tabs and new lines
                if ((ch < 0x00FD && ch > 0x001F) || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    newString.Append(ch);
                }
            }
            return newString.ToString();

        }
        static public bool MatchString(string text, string findText, FindEventArgs e)
        {
            bool ret = false;
            if (e.MatchCase)
            {
                if (e.MatchWhole)
                {
                    if (Regex.IsMatch(text, @"\b" + findText + @"\b"))
                    {
                        ret = true;
                    }
                }
                else
                {
                    if (text.Contains(findText))
                    {
                        ret = true;
                    }
                }
            }
            else
            {
                if (e.MatchWhole)
                {
                    string match = @"\b" + findText + @"\b";
                    if (Regex.IsMatch(text, match, RegexOptions.IgnoreCase))
                    {
                        ret = true;
                    }
                }
                else
                {
                    if (text.IndexOf(findText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ret = true;
                    }
                }
            }
            return ret;
        }

    }
    #endregion

    public class Error
    {
        private static string _filename = null;

        public static void WriteLine(string msg)
        {
            if (string.IsNullOrEmpty(_filename))
            {
                System.Diagnostics.Trace.WriteLine(Properties.Settings.Default.AppName + ": " + msg);
            }
            else
            {
                try
                {
                    using (StreamWriter r = File.AppendText(_filename))
                    {
                        r.WriteLine(Properties.Settings.Default.AppName + ": " + msg);
                    }
                }

                catch (Exception ex)
                {
                    var filename = _filename;
                    _filename = null;
                    WriteLine("Error opening file: " + filename + " " + ex.Message);
                }
            }
        }
        public static void MessageBox(string msg)
        {
            System.Windows.Forms.MessageBox.Show(msg, Properties.Settings.Default.AppName, MessageBoxButtons.OK,
                                                 MessageBoxIcon.Error);
        }
        public static void SetOutputToFile(string filename)
        {
            try
            {
                using (StreamWriter r = File.CreateText(filename))
                {
                }
                _filename = filename;
            }
            catch (Exception ex)
            {
                _filename = null;
                WriteLine("Error creating file: " + filename + " " + ex.Message);
            }
        }
    }

    public static class IntPtrTools
    {
        public static ulong ToUlong(IntPtr p)
        {
            Debug.Assert(IntPtr.Size == 4 || IntPtr.Size == 8);
            if (IntPtr.Size == 4)
                return (ulong)(uint)p;
            return (ulong)p;
        }

        public static long ToLong(IntPtr p)
        {
            Debug.Assert(IntPtr.Size == 4 || IntPtr.Size == 8);
            if (IntPtr.Size == 4)
                return (long)(int)p;
            return (long)p;
        }

        public static IntPtr ToIntPtr(long p)
        {
            Debug.Assert(IntPtr.Size == 4 || IntPtr.Size == 8);
            if (IntPtr.Size == 4)
                return (IntPtr)(int)(p&0xFFFFFFFF);
            return (IntPtr)p;
        }

        public static IntPtr ToIntPtr(int p)
        {
            Debug.Assert(IntPtr.Size == 4 || IntPtr.Size == 8);
            if (IntPtr.Size == 4)
                return (IntPtr)p;
            return (IntPtr)(uint)p;
        }
    }

    public static class XmlTools
    {
        static public void SerializeClass<T>(T obj, Stream stream)
        {
            var serializer = new XmlSerializer(typeof(T));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
            };
            var writer = new StreamWriter(stream, Encoding.UTF8);
            serializer.Serialize(XmlWriter.Create(writer, settings), obj);
        }
        static public T DeserializeClass<T>(Stream stream)
        {
            var reader = new StreamReader(stream, Encoding.UTF8);
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(reader);
        }

        public static void SerializeAppvManifestObject<T>(T manifestObject, XmlTextWriter xml)
        {
            var fields = manifestObject.GetType().GetFields();
            SerializeFields(xml, fields, manifestObject, true);
            SerializeFields(xml, fields, manifestObject, false);
        }

        private static void SerializeFields(XmlTextWriter xml, IEnumerable<FieldInfo> fields, object manifestObject, bool onlyAttributes)
        {
            foreach (var fieldInfo in fields)
            {
                var props = new AttributeProperties(fieldInfo.Name);
                fieldInfo.GetCustomAttributes(true)
                    .Select(x => x as ManifestSerializationAttribute)
                    .Where(x => x != null)
                    .ForEach(x => x.SetProperties(props));

                if (props.AsAttribute != onlyAttributes || props.Ignore)
                    continue;

                var obj = fieldInfo.GetValue(manifestObject);
                if (obj == null)
                    continue;

                var list = obj as IList;
                if (list != null)
                {
                    var appv = props.OmitAppvNamespace ? "" : "appv:";
                    if (!props.OpenList)
                        xml.WriteStartElement(appv + props.NodeName);
                    props.NodeName = props.ElementName;
                    foreach (var item in list)
                        SerializeObject(xml, item, props);
                    if (!props.OpenList)
                        xml.WriteEndElement();
                }
                else
                    SerializeObject(xml, obj, props);
            }
        }

        private static void SerializeObject(XmlTextWriter xml, object obj, AttributeProperties props)
        {
            var appv = props.OmitAppvNamespace ? "" : "appv:";
            {
                var xmlGenerator = obj as XmlGenerator;
                if (xmlGenerator != null)
                {
                    xmlGenerator.GenerateXml(xml);
                    return;
                }
            }
            {
                if (obj is SpecialManifestData || obj is Version)
                {
                    if (!props.AsAttribute)
                        xml.WriteElementString(appv + props.NodeName, obj.ToString());
                    else
                        xml.WriteAttributeString(props.NodeName, obj.ToString());
                    return;
                }
            }
            

            var type = obj.GetType();

            if (!type.IsPrimitive && !type.IsEnum && !type.IsValueType && !(obj is string))
                throw new Exception("Ill-defined class structure.");

            var i = 0;
            if (props.OmitZero || props.ToInt)
                i = Convert.ToInt32(obj);
            if (i == 0 && props.OmitZero)
                return;
            if (props.ToInt)
                obj = i;
            var value = obj.ToString();

            if (!props.AsAttribute)
                xml.WriteElementString(appv + props.NodeName, value);
            else
                xml.WriteAttributeString(props.NodeName, value);
        }
    }

    public static class PipeUtils
    {
        public static NamedPipeServerStream CreateRandomlyNamedServerPipe(PipeDirection dir, out string name)
        {
            NamedPipeServerStream ret;
            while (true)
            {
                name = "SpyStudioPipe_" + StringTools.RandomLowerCaseString(8);
                try
                {
                    ret = new NamedPipeServerStream(name, PipeDirection.InOut);
                }
                catch (IOException)
                {
                    continue;
                }
                break;
            }
            return ret;
        }

        public static void SendStringAndWait(this PipeStream pipe, string s)
        {
            var buffer = Encoding.UTF8.GetBytes(s);
            var lengthBuffer = BitConverter.GetBytes(buffer.Length);
            pipe.Write(lengthBuffer, 0, lengthBuffer.Length);
            pipe.Write(buffer, 0, buffer.Length);
            pipe.WaitForPipeDrain();
        }

        private const int InfiniteTimeout = -1;

        public static string ReceiveString(this PipeStream pipe)
        {
            return pipe.ReceiveString(InfiniteTimeout);
        }

        public static string ReceiveString(this PipeStream pipe, int timeout)
        {
            var lengthBuffer = new byte[4];
            if (timeout < 0)
            {
                pipe.Read(lengthBuffer, 0, 4);
                var buffer = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
                pipe.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                var ev = new AutoResetEvent(false);
                pipe.BeginRead(lengthBuffer, 0, 4, OnCallback, ev);
                if (!ev.WaitOne(timeout))
                {
                    pipe.EndRead(null);
                    return null;
                }
                var buffer = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
                pipe.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer);
            }
        }

        private static void OnCallback(IAsyncResult ar)
        {
            var e = (AutoResetEvent)ar.AsyncState;
            if (ar.IsCompleted)
                e.Set();
        }

    }

    public static class MiscUtils
    {
        public static string GetCurrentUserSid()
        {
            return UserPrincipal.Current.Sid.ToString();
        }
    }
}

