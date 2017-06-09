using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpyStudio.FileSystem;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppPathNormalizer : MacroBasedNormalizer
    {
        private readonly Regex _driveLetterStripper = new Regex(@"([a-zA-Z])\:(\\.*)?$");

        //private readonly string _systemDisk;
        //private readonly string _currentUser;


        //private static string GetSystemDisk()
        //{
        //    var ret = Path.GetPathRoot(Environment.SystemDirectory);
        //    return ret == null ? "C" : ret.Substring(0, 1);
        //}

        //private static string GetCurrentUser()
        //{
        //    var current = WindowsIdentity.GetCurrent();
        //    var ret = current == null ? "" : current.Name;
        //    if (ret.Contains('\\'))
        //    {
        //        var l = ret.Split('\\');
        //        ret = l[l.Length - 1];
        //    }
        //    return ret;
        //}

        public override string LocalAppDataPath { get { return "%Local AppData%"; } }
        public override string RoamingAppDataPath { get { return "%AppData%"; } }
        public override string RuntimePath { get { return @"%SystemRoot%\winsxs"; } }

        public override string ProgramFiles
        {
            get
            {
                switch (IntPtr.Size)
                {
                    case 4:
                        return "%ProgramFilesDir%";
                    case 8:
                        return "%ProgramFilesDir(x64)%";
                    default:
                        throw new PlatformNotSupportedException();
                }
            }
        }

        public override string ProgramFiles86 { get { return "%ProgramFilesDir%"; } }

        private static readonly ThinAppPathNormalizer Instance = new ThinAppPathNormalizer();

        public static ThinAppPathNormalizer GetInstance()
        {
            return Instance;
        }

        protected ThinAppPathNormalizer()
        {
            IgnoredPaths = new[]
                        {
                            @"^.:\\$", 
                            @"^\\.",
                            @"^\\(?:Device|KnownDlls|REGISTRY|Sessions|SystemRoot)(?:$\\.*)"
                        };

            AddToPathsIfNotNullOrEmpty("%Profiles%", SystemDirectories.ProfilesDirectory);
            AddToPathsIfNotNullOrEmpty("%Profile%", SystemDirectories.CurrentProfileDirectory);
            AddToPathsIfNotNullOrEmpty("%AppData%", SystemDirectories.CurrentAppData);
            AddToPathsIfNotNullOrEmpty("%Cookies%", SystemDirectories.CurrentCookiesDirectory);
            AddToPathsIfNotNullOrEmpty("%Desktop%", SystemDirectories.CurrentDesktop);
            AddToPathsIfNotNullOrEmpty("%Favorites%", SystemDirectories.CurrentFavorites);
            AddToPathsIfNotNullOrEmpty("%Local AppData%", SystemDirectories.CurrentLocalAppData);
            AddToPathsIfNotNullOrEmpty("%CDBurnArea%", SystemDirectories.CurrentCdBurning);
            AddToPathsIfNotNullOrEmpty("%History%", SystemDirectories.CurrentHistory);
            AddToPathsIfNotNullOrEmpty("%TEMP%", SystemDirectories.CurrentTEMP);
            AddToPathsIfNotNullOrEmpty("%Internet Cache%", SystemDirectories.CurrentInternetCache);
            AddToPathsIfNotNullOrEmpty("%Personal%", SystemDirectories.CurrentPersonal);
            AddToPathsIfNotNullOrEmpty("%Recent%", SystemDirectories.CurrentRecent);
            AddToPathsIfNotNullOrEmpty("%NetHood%", SystemDirectories.CurrentNetHood);
            AddToPathsIfNotNullOrEmpty("%PrintHood%", SystemDirectories.CurrentPrintHood);
            AddToPathsIfNotNullOrEmpty("%SendTo%", SystemDirectories.CurrentSendTo);
            AddToPathsIfNotNullOrEmpty("%Programs%", SystemDirectories.CurrentPrograms);
            AddToPathsIfNotNullOrEmpty("%AdminTools%", SystemDirectories.CurrentAdminTools);
            AddToPathsIfNotNullOrEmpty("%Startup%", SystemDirectories.CurrentStartup);
            AddToPathsIfNotNullOrEmpty("%Templates%", SystemDirectories.CurrentTemplates);
            AddToPathsIfNotNullOrEmpty("%Common AppData%", SystemDirectories.CommonAppData);
            AddToPathsIfNotNullOrEmpty("%Common Desktop%", SystemDirectories.CommonDesktop);
            AddToPathsIfNotNullOrEmpty("%Common Documents%", SystemDirectories.CommonDocuments);
            AddToPathsIfNotNullOrEmpty("%Common Favorites%", SystemDirectories.CommonFavorites);
            AddToPathsIfNotNullOrEmpty("%Common StartMenu%", SystemDirectories.CommonStartMenu);
            AddToPathsIfNotNullOrEmpty("%Common Programs%", SystemDirectories.CommonPrograms);
            AddToPathsIfNotNullOrEmpty("%Common AdminTools%", SystemDirectories.CommonAdminTools);
            AddToPathsIfNotNullOrEmpty("%Common Startup%", SystemDirectories.CommonStartup);
            AddToPathsIfNotNullOrEmpty("%Common Templates%", SystemDirectories.CommonTemplates);
            AddToPathsIfNotNullOrEmpty("%SystemRoot%", SystemDirectories.Windows);
            AddToPathsIfNotNullOrEmpty("%Fonts%", SystemDirectories.Fonts);
            AddToPathsIfNotNullOrEmpty("%Resources%", SystemDirectories.Resources);

            if (PlatformTools.IsPlatform64Bits())
            {
                AddToPathsIfNotNullOrEmpty("%ProgramFilesDir%", SystemDirectories.ProgramFiles86);
                AddToPathsIfNotNullOrEmpty("%Program Files Common%", SystemDirectories.CommonProgramFiles86);
                AddToPathsIfNotNullOrEmpty("%SystemSystem%", SystemDirectories.SystemSystem86);
                AddToPathsIfNotNullOrEmpty("%ProgramFilesDir(x64)%", SystemDirectories.ProgramFiles64);
                AddToPathsIfNotNullOrEmpty("%Program Files Common(x64)%", SystemDirectories.CommonProgramFiles);
                AddToPathsIfNotNullOrEmpty("%SystemSystem(x64)%", SystemDirectories.SystemSystem);
            }
            else
            {
                AddToPathsIfNotNullOrEmpty("%ProgramFilesDir%", SystemDirectories.ProgramFiles);
                AddToPathsIfNotNullOrEmpty("%Program Files Common%", SystemDirectories.CommonProgramFiles);
                AddToPathsIfNotNullOrEmpty("%SystemSystem%", SystemDirectories.SystemSystem);
            }

            Paths.AddRange(Paths.Select(m => new FolderMacro(m.Macro, FileSystemTools.GetShortPathName(m.Path))).Where(m => !string.IsNullOrEmpty(m.Path)).ToList());


            //Take all the paths that have been generated. Besides normalizing X:\foo -> %Foo%, we also
            //normalize X?\foo -> %?Foo%
            //Tell Victor and write it down here if you ever figure out why.
            Paths.AddRange(Paths.Select(x => new FolderMacro(x.Macro.Substring(0, 1) + "?" + x.Macro.Substring(1), x.Path.Replace(":", "?"))).Where(m => !string.IsNullOrEmpty(m.Path) && !string.IsNullOrEmpty(m.Macro)).ToList());

            if (SystemDirectories.Resources != null)
            {
                var escaped = Regex.Escape(SystemDirectories.Resources.AsNormalizedPath() + "\\");
                Paths.Add(new ResourcesFolderMacro("(" + escaped + @"([a-zA-Z_-]*))(\\.*)",
                                           m => "%Resources Localized%" + m.Groups[3]));
            }
        }

        private static string DoDriveLetterStripping(Match m)
        {
            return String.Format("%Drive_{0}%{1}", m.Groups[1].ToString().ToUpper(), m.Groups[2]);
        }

        public override string InternalNormalize(string src, out bool replacementPerformed)
        {
            string ret = base.InternalNormalize(src, out replacementPerformed);

            if (_driveLetterStripper.IsMatch(src) && !replacementPerformed)
                ret = _driveLetterStripper.Replace(src, DoDriveLetterStripping).AsNormalizedPath();

            return ret;
        }

        private readonly Regex _driveRegex = new Regex(@"^\%drive_([A-Z])\%.*", RegexOptions.IgnoreCase);
        private readonly Regex _macroRegex = new Regex(@"^\%[^%]+\%");

        protected override Regex MacroRegex
        {
            get { return _macroRegex; }
        }

        public override string ExpandFolderMacro(string path, out bool expanded)
        {
            expanded = false;
            var match = _driveRegex.Match(path);
            if (match.Success)
            {
                expanded = true;
                const string drive = "%drive_X%";
                var ret = match.Groups[1] + ":" + path.Substring(drive.Length);
                if (drive.Length == path.Length)
                    ret += "\\";
                return ret;
            }

            string macro;
            MatchMacro(path, out macro);

            if (macro != null && macro.Equals("%resources localized%", StringComparison.InvariantCultureIgnoreCase))
                return path;


            return base.ExpandFolderMacro(path, out expanded);
        }

    }
}
