using System;
using System.Diagnostics;
using System.Text;
using System.Web;
using SpyStudio.FileSystem;
using SpyStudio.Tools;
using SpyStudio.Extensions;
using System.Linq;

namespace SpyStudio.Export.AppV
{
    public class AppvPathNormalizer : MacroBasedNormalizer
    {
        [Flags]
        public enum WorkingMode
        {
            None = 0,
            Manifest = 1,
            LengthenPath = 2,
            ManifestPlusLengthenPath = Manifest | LengthenPath,
            FileSystem = 4,
        }

        public WorkingMode Mode = WorkingMode.None;

        protected override OperationMode DefaultMode
        {
            get
            {
                if ((Mode & WorkingMode.LengthenPath) == WorkingMode.LengthenPath)
                    return OperationMode.LengthenPath;
                return base.DefaultMode;
            }
        }

        public static AppvPathNormalizer GetInstanceNone()
        {
            return NormalizerNone;
        }

        public static AppvPathNormalizer GetInstanceManifest()
        {
            return NormalizerManifest;
        }

        public static AppvPathNormalizer GetInstanceManifestPlusLengthenPath()
        {
            return NormalizerManifestPlusLengthenPath;
        }

        public static AppvPathNormalizer GetInstanceFileSystem()
        {
            return NormalizerFileSystem;
        }

        private void AddKnownPath(string path, string member)
        {
            var fieldInfo = typeof(Declarations.KnownFolder).GetField(member);
            if (fieldInfo == null)
                throw new ArgumentException("Declarations.KnownFolder doesn't have the field " + path);
            AddKnownPath(path, (Guid)fieldInfo.GetValue(null));
        }

        private void AddKnownPath(string path, Guid guid)
        {
            AddToPathsIfNotNullOrEmpty(path, SystemDirectories.GetSpecialDirectory(guid));
        }

        private static readonly string[] InitStrings =
        {
            "Common AppData", "ProgramData",
            "Device Metadata Store", "DeviceMetadataStore",
            "PublicGameTasks", null,
            "CommonRingtones", "PublicRingtones",
            "Common Start Menu", "CommonStartMenu",
            "Common Programs", "CommonPrograms",
            "Common Administrative Tools", "CommonAdminTools",
            "Common Startup", "CommonStartup",
            "Common Templates", "CommonTemplates",
            "UserProfiles", null,
            "Profile", "Profile",
            "Local AppData", "LocalAppData",
            "Application Shortcuts", "ApplicationShortcuts",
            "CD Burning", "CDBurning",
            "GameTasks", null,
            "History", null,
            "Ringtones", null,
            "Roamed Tile Images", "RoamedTileImages",
            "Roaming Tiles", "RoamingTiles",
            "Cache", "InternetCache",
            "LocalAppDataLow", null,
            "AppData", "RoamingAppData",
            "Quick Launch", "QuickLaunch",
            "User Pinned", "UserPinned",
            "ImplicitAppShortcuts", null,
            "AccountPictures", null,
            "Cookies", null,
            "Libraries", null,
            "DocumentsLibrary", null,
            "MusicLibrary", null,
            "PicturesLibrary", null,
            "VideosLibrary", null,
            "NetHood", null,
            "PrintHood", null,
            "Recent", null,
            "SendTo", null,
            "Start Menu", "StartMenu",
            "Programs", null,
            "Administrative Tools", "AdminTools",
            "Startup", null,
            "Templates", null,
            "Contacts", null,
            "Desktop", null,
            "Personal", "Documents",
            "Downloads", null,
            "Favorites", null,
            "Links", null,
            "My Music", "Music",
            "My Pictures", "Pictures",
            "SavedGames", null,
            "Searches", "SavedSearches",
            "My Video", "Videos",
            "AppVAllUsersDir", "ProgramData",
            "Public", null,
            "PublicAccountPictures", "PublicUserTiles",
            "Common Desktop", "PublicDesktop",
            "Common Documents", "PublicDocuments",
            "CommonDownloads", "PublicDownloads",
            "PublicLibraries", null,
            "RecordedTVLibrary", null,
            "CommonMusic", "PublicMusic",
            "CommonPictures", "PublicPictures",
            "CommonVideo", "PublicVideos",
            "Windows", null,
            "Fonts", null,
            "ResourceDir", null,
            "System", null,
            "SystemX86", null,
        };

        private static readonly string CurrentUserSid = MiscUtils.GetCurrentUserSid();
        private static readonly string ComputerName = Environment.MachineName;

        public AppvPathNormalizer()
        {
            //AddToPathsIfNotNullOrEmpty("ProgramFiles", SystemDirectories.ProgramFiles);
            AddToPathsIfNotNullOrEmpty("ProgramFilesX86", SystemDirectories.ProgramFiles86);
            //AddToPathsIfNotNullOrEmpty("ProgramFilesCommon", SystemDirectories.CommonProgramFiles);
            AddToPathsIfNotNullOrEmpty("ProgramFilesCommonX86", SystemDirectories.CommonProgramFiles86);
            if (IntPtr.Size > 4)
            {
                AddToPathsIfNotNullOrEmpty("ProgramFilesX64", SystemDirectories.ProgramFiles64);
                AddToPathsIfNotNullOrEmpty("ProgramFilesCommonX64", SystemDirectories.CommonProgramFiles64);
            }

            for (var i = 0; i < InitStrings.Length; i+=2)
                AddKnownPath(InitStrings[i], InitStrings[i + 1] ?? InitStrings[i]);

            AddToPathsIfNotNullOrEmpty("CredentialManager", SystemDirectories.RoamingAppDataPath + @"\Microsoft\Credentials");
            AddToPathsIfNotNullOrEmpty("CryptoKeys", SystemDirectories.RoamingAppDataPath + @"\Microsoft\Crypto");
            AddToPathsIfNotNullOrEmpty("DpapiKeys", SystemDirectories.RoamingAppDataPath + @"\Microsoft\Protect");
            AddToPathsIfNotNullOrEmpty("SystemCertificates", SystemDirectories.RoamingAppDataPath + @"\Microsoft\SystemCertificates");
            AddToPathsIfNotNullOrEmpty("Podcast Library", SystemDirectories.GetSpecialDirectory(Declarations.KnownFolder.Libraries) + @"\Podcasts.library-ms");
            AddToPathsIfNotNullOrEmpty("Podcasts", SystemDirectories.CurrentProfileDirectory + @"\Podcasts");
            AddToPathsIfNotNullOrEmpty("AppVSystem32Catroot", SystemDirectories.SystemSystem + @"\catroot");
            AddToPathsIfNotNullOrEmpty("AppVSystem32Catroot2", SystemDirectories.SystemSystem + @"\catroot2");
            AddToPathsIfNotNullOrEmpty("AppVSystem32DriversEtc", SystemDirectories.SystemSystem + @"\drivers\etc");
            AddToPathsIfNotNullOrEmpty("AppVSystem32Driverstore", SystemDirectories.SystemSystem + @"\driverstore");
            AddToPathsIfNotNullOrEmpty("AppVSystem32Logfiles", SystemDirectories.SystemSystem + @"\logfiles");
            AddToPathsIfNotNullOrEmpty("AppVSystem32Spool", SystemDirectories.SystemSystem + @"\spool");
            var windows = SystemDirectories.Windows;
            AddToPathsIfNotNullOrEmpty("AppVPackageDrive", windows.Substring(0, windows.IndexOf('\\')));
        }

        public override string LocalAppDataPath
        {
            get
            {
                switch (Mode)
                {
                    case WorkingMode.None:
                        return "Local AppData";
                    case WorkingMode.Manifest:
                    case WorkingMode.ManifestPlusLengthenPath:
                        return "[{Local AppData}]";
                    case WorkingMode.FileSystem:
                        return "Local%20AppData";
                }
                Debug.Assert(false);
                return null;
            }
        }

        public override string RoamingAppDataPath
        {
            get
            {
                switch (Mode)
                {
                    case WorkingMode.None:
                    case WorkingMode.FileSystem:
                        return "AppData";
                    case WorkingMode.Manifest:
                    case WorkingMode.ManifestPlusLengthenPath:
                        return "[{AppData}]";
                }
                Debug.Assert(false);
                return null;
            }
        }

        public override string RuntimePath
        {
            get
            {
                switch (Mode)
                {
                    case WorkingMode.None:
                    case WorkingMode.FileSystem:
                        return @"Windows\winsxs";
                    case WorkingMode.Manifest:
                    case WorkingMode.ManifestPlusLengthenPath:
                        return @"[{Windows}]\winsxs";
                }
                Debug.Assert(false);
                return null;
            }
        }

        public override string ProgramFiles
        {
            get
            {
                switch (Mode)
                {
                    case WorkingMode.None:
                    case WorkingMode.FileSystem:
                        return "ProgramFiles";
                    case WorkingMode.Manifest:
                    case WorkingMode.ManifestPlusLengthenPath:
                        return "[{ProgramFiles}]";
                }
                Debug.Assert(false);
                return null;
            }
        }

        public override string ProgramFiles86
        {
            get
            {
                switch (Mode)
                {
                    case WorkingMode.None:
                    case WorkingMode.FileSystem:
                        return "ProgramFilesX86";
                    case WorkingMode.Manifest:
                    case WorkingMode.ManifestPlusLengthenPath:
                        return "[{ProgramFilesX86}]";
                }
                Debug.Assert(false);
                return null;
            }
        }

        protected override string InternalNormalize(string path)
        {
            bool replaced;
            var ret = base.InternalNormalize(path, out replaced);
            if (replaced && Mode != WorkingMode.None)
            {
                var temp = ret.SplitAsPath().ToArray();
                if ((Mode & WorkingMode.Manifest) == WorkingMode.Manifest)
                {
                    bool isShort = FileSystemTools.IsPathShort(path);
                    temp[0] = "[{" + temp[0] + (isShort ? "}{~}]" : "}]");
                }
                else if (Mode == WorkingMode.FileSystem)
                {
                    temp = temp.Select(x => HttpUtility.UrlEncode(x).Replace("+", "%20")).ToArray();
                }
                ret = temp.Aggregate((x, y) => x + "\\" + y);
            }
            else if (!replaced)
            {
                switch (Mode)
                {
                    case WorkingMode.None:
                    case WorkingMode.FileSystem:
                        ret = path
                            .Replace(ComputerName, "AppVComputerName")
                            .Replace(CurrentUserSid, "AppVCurrentUserSID");
                        break;
                    case WorkingMode.Manifest:
                    case WorkingMode.ManifestPlusLengthenPath:
                        ret = path
                            .Replace(ComputerName, "[{AppVComputerName}]")
                            .Replace(CurrentUserSid, "[{AppVCurrentUserSID}]");
                        break;
                }
            }
            return ret;
        }

        private static readonly string[] Separator = new[] {"}{"};

        private string ProcessBracketsContents(string contents)
        {
            if (!contents.StartsWith("{") || !contents.EndsWith("}"))
                return null;
            contents = contents.Substring(1, contents.Length - 2);
            var parameters = contents.Split(Separator, StringSplitOptions.None).ToList();
            var macro = Paths.FirstOrDefault(x => x.Macro == parameters[0]);
            if (macro == null)
                return null;
            var ret = macro.Path;
            if (parameters.Count > 1 && parameters[1] == "~")
                ret = FileSystemTools.GetShortPathName(ret);
            return ret;
        }

        public override string Unnormalize(string path)
        {
            var ret = new StringBuilder();
            var lbrackets = path.Split('[').ToList();
            ret.Append(lbrackets[0]);

            foreach (var s in lbrackets.Skip(1))
            {
                var rbrackets = s.Split(']').ToList();
                if (rbrackets.Count == 1)
                {
                    ret.Append('[');
                    ret.Append(rbrackets[0]);
                    continue;
                }

                var result = ProcessBracketsContents(rbrackets[0]);

                if (result != null)
                    ret.Append(result);
                else
                {
                    ret.Append('[');
                    ret.Append(rbrackets[0]);
                    ret.Append(']');
                }

                if (rbrackets.Count < 2)
                    continue;

                foreach (var rbracket in rbrackets.Take(rbrackets.Count - 1).Skip(1))
                {
                    ret.Append(rbracket);
                    ret.Append(']');
                }
                ret.Append(rbrackets.Last());
            }
            return ret.ToString();
        }

        public override bool NormalizeSeparators
        {
            get
            {
                return (Mode & WorkingMode.Manifest) != WorkingMode.Manifest;
            }
        }

        private static readonly AppvPathNormalizer NormalizerNone = new AppvPathNormalizer();
        private static readonly AppvPathNormalizer NormalizerManifest = new AppvPathNormalizer
        {
            Mode = WorkingMode.Manifest
        };
        private static readonly AppvPathNormalizer NormalizerManifestPlusLengthenPath = new AppvPathNormalizer
        {
            Mode = WorkingMode.ManifestPlusLengthenPath
        };
        private static readonly AppvPathNormalizer NormalizerFileSystem = new AppvPathNormalizer
        {
            Mode = WorkingMode.FileSystem
        };

    }
}