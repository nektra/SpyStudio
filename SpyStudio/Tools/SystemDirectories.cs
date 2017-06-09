using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SpyStudio.Extensions;

namespace SpyStudio.Tools
{
    public static class SystemDirectories
    {
        //E.g. C:\Users
        public static string ProfilesDirectory
        {
            get { return GetProfilesDirectory(); }
        }

        //E.g. C:\Users\<user>
        public static string CurrentProfileDirectory
        {
            get { return GetCurrentProfileDirectory(); }
        }

        //E.g. C:\Users\<user>\AppData
        public static string CurrentAppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Cookies
        public static string CurrentCookiesDirectory
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COOKIES); }
        }

        //E.g. C:\Users\<user>\Desktop
        public static string CurrentDesktop
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        }

        //E.g. C:\Users\<user>\Favorites
        public static string CurrentFavorites
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Favorites); }
        }

        //E.g. C:\Users\<user>\AppData\Local
        public static string CurrentLocalAppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); }
        }

        //E.g. C:\Users\<user>\AppData\LocalLow
        public static string CurrentLocalAppDataLow
        {
            get { return GetSpecialDirectory(Declarations.KnownFolder.LocalAppDataLow); }
        }

        //E.g. C:\Users\<user>\AppData\Local\Microsoft\Windows\Burn\Burn
        public static string CurrentCdBurning
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_CDBURN_AREA); }
        }

        //E.g. C:\Users\<user>\AppData\Local\Microsoft\Windows\History
        public static string CurrentHistory
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.History); }
        }

        //E.g. C:\Users\<user>\AppData\Local\Temp\
        public static string CurrentTEMP
        {
            get { return Path.GetTempPath().AsNormalizedPath(); }
        }

        //E.g. C:\Users\<user>\AppData\Local\Microsoft\Windows\Temporary Internet Files
        public static string CurrentInternetCache
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_INTERNET_CACHE); }
        }

        //E.g. C:\Users\<user>\Documents
        public static string CurrentPersonal
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Personal); }
        }

        public static string CurrentDocuments
        {
            get { return CurrentPersonal; }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Recent
        public static string CurrentRecent
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Recent); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Network Shortcuts
        public static string CurrentNetHood
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_NETHOOD); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Printer Shortcuts
        public static string CurrentPrintHood
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_PRINTHOOD); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\SendTo
        public static string CurrentSendTo
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.SendTo); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Start Menu\Programs
        public static string CurrentPrograms
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Programs); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Administrative Tools
        public static string CurrentAdminTools
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_ADMINTOOLS); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
        public static string CurrentStartup
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Startup); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming\Microsoft\Windows\Templates
        public static string CurrentTemplates
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Templates); }
        }

        //E.g. C:\ProgramData
        public static string CommonAppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData); }
        }

        //E.g. C:\Users\Public\Desktop
        public static string CommonDesktop
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_DESKTOPDIRECTORY); }
        }

        //E.g. C:\Users\Public\Documents
        public static string CommonDocuments
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_DOCUMENTS); }
        }

        //E.g. C:\Users\<user>\Favorites
        public static string CommonFavorites
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_FAVORITES); }
        }

        //E.g. C:\ProgramData\Microsoft\Windows\Start Menu
        public static string CommonStartMenu
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_STARTMENU); }
        }

        //E.g. C:\ProgramData\Microsoft\Windows\Start Menu\Programs
        public static string CommonPrograms
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_PROGRAMS); }
        }

        //E.g. C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Administrative Tools
        public static string CommonAdminTools
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_ADMINTOOLS); }
        }

        //E.g. C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup
        public static string CommonStartup
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_STARTUP); }
        }

        //E.g. C:\ProgramData\Microsoft\Windows\Templates
        public static string CommonTemplates
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_COMMON_TEMPLATES); }
        }

        //E.g. C:\Program Files (x86)
        public static string ValidProgramFiles86
        {
            get
            {
                var ret = ProgramFiles86;
                return !string.IsNullOrEmpty(ret)
                    ? ret
                    : ProgramFiles;
            }
        }

        //E.g. C:\Program Files
        public static string ProgramFiles
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_PROGRAM_FILES); }
        }

        //E.g. C:\Program Files (x86)
        public static string ProgramFiles86
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_PROGRAM_FILESX86) ?? ProgramFiles; }
        }

        //E.g. C:\Program Files
        public static string ProgramFiles64
        {
            get { return GetSpecialDirectory(Declarations.KnownFolder.ProgramFilesX64) ?? ProgramFiles; }
        }

        //E.g. C:\Program Files\Common Files
        public static string CommonProgramFiles
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_PROGRAM_FILES_COMMON); }
        }

        //E.g. C:\Program Files\Common Files
        public static string CommonProgramFiles64
        {
            get { return CommonProgramFiles; }
        }

        //E.g. C:\Program Files (x86)\Common Files
        public static string CommonProgramFiles86
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_PROGRAM_FILES_COMMONX86) ?? CommonProgramFiles86; }
        }

        //E.g. C:\Windows
        public static string Windows
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_WINDOWS); }
        }

        //E.g. C:\Windows\Fonts
        public static string Fonts
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_FONTS); }
        }

        //E.g. C:\Windows\resources
        public static string Resources
        {
            get { return GetSpecialDirectory(Declarations.KnownFolder.ResourceDir); }
        }

        //E.g. C:\Windows\system32
        public static string SystemSystem
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_SYSTEM); }
        }

        //E.g. C:\Windows\SysWOW64
        public static string SystemSystem86
        {
            get { return GetSpecialDirectory(Declarations.SpecialFolderCSIDL.CSIDL_SYSTEMX86); }
        }

        //E.g. C:\Users\<user>\AppData\Roaming
        public static string RoamingAppDataPath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        }

        //E.g. C:\Users\<user>\AppData\Local
        public static string LocalAppDataPath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); }
        }

        //E.g. C:\Windows\winsxs
        public static string RuntimePath
        {
            get { return Windows + @"\winsxs"; }
        }

        private static readonly Dictionary<Declarations.SpecialFolderCSIDL, string> MemoizedSpecialDirectories1 =
            new Dictionary<Declarations.SpecialFolderCSIDL, string>();

        public static string GetSpecialDirectory(Declarations.SpecialFolderCSIDL csidl)
        {
            string ret;
            if (MemoizedSpecialDirectories1.TryGetValue(csidl, out ret))
                return ret;

            var temp = new StringBuilder(Declarations.MaxPath + 1);
            var result = Declarations.SHGetFolderPath(
                IntPtr.Zero,
                (int) csidl,
                IntPtr.Zero,
                (int) Declarations.SHGFP_TYPE.SHGFP_TYPE_CURRENT,
                temp
            );
            if (result != 0)
                return null;
            ret = temp.ToString();
            MemoizedSpecialDirectories1[csidl] = ret;
            return ret;
        }

        private static readonly Dictionary<Guid, string> MemoizedSpecialDirectories2 =
            new Dictionary<Guid, string>();

        public static string GetSpecialDirectory(Guid kf)
        {
            string ret;
            if (MemoizedSpecialDirectories2.TryGetValue(kf, out ret))
                return ret;

            var output = IntPtr.Zero;
            try
            {
                int result;
                try
                {
                    result = Declarations.SHGetKnownFolderPath(kf, 0x00004000, IntPtr.Zero, out output);
                }
                catch (EntryPointNotFoundException)
                {
                    return null;
                }
                if (result != 0)
                    return null;
                ret = Marshal.PtrToStringUni(output);
            }
            finally
            {
                Declarations.CoTaskMemFree(output);
            }
            MemoizedSpecialDirectories2[kf] = ret;
            return ret;
        }

        private static T GetValueOfType<T>(string key, string value, T defaultValue)
        {
            var ret = Microsoft.Win32.Registry.GetValue(key, value, defaultValue);
            if (!(ret is T))
                return defaultValue;
            return (T) ret;
        }

        private static string GetProfilesDirectory()
        {
            return GetValueOfType<string>(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList",
                "ProfilesDirectory",
                null
                );
        }

        private static string GetCurrentProfileDirectory()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Volatile Environment");
            if (key == null)
                return null;
            var homedrive = key.GetValue("HOMEDRIVE", null);
            var homepath = key.GetValue("HOMEPATH", null);
            if (homedrive == null || homepath == null || !(homedrive is string && homepath is string))
                return null;
            return (string) homedrive + (string) homepath;
        }
    }
}