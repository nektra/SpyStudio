using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Swv;
using SpyStudio.Tools;

namespace SpyStudio.Export.SWV
{
    public class SwvPathNormalizer : PathNormalizer
    {
        protected SwvPathNormalizer()
        {
            IgnoredPaths = new[] { @"^.:\\$", @"^\\." };
        }

        public override string LocalAppDataPath { get { return @"[_B_]LocalAppData[_E_]"; } }
        public override string RoamingAppDataPath { get { return @"[_B_]AppData[_E_]"; } }
        public override string RuntimePath { get { return @"[_B_]WINDIR[_E_]\winsxs"; } }
        public override string ProgramFiles { get { return Normalize(SystemDirectories.ProgramFiles); } }
        public override string ProgramFiles86 { get { return Normalize(SystemDirectories.ProgramFiles86); } }

        public override CallEvent InternalIncludeEvent(CallEvent originalEvent, string fileSystemPath)
        {
            var ce = originalEvent.Clone();

            if (ce.IsProcMon)
            {
                ce.ParamMain = FileSystemTools.ResolveProcMonPath(ce.ParamMain);
            }

            ce.ParamMain = Normalize(ce.ParamMain);

            if (ce.ParamMain != originalEvent.ParamMain)
                return ce;
            return null;
        }

        protected override bool ShouldIgnoreEvent(CallEvent e)
        {
            return e.Type == HookType.CreateDirectory || !e.Success || ShouldIgnorePath(e.ParamMain);
        }

        protected override string InternalNormalize(string path)
        {
            bool error, foundRootPath, systemFile;
            string filepart;
            var ret = SwvLayers.GetSwvPath(path, out error, out filepart, out systemFile, out foundRootPath);
            if (!error && filepart != "" && !systemFile && foundRootPath)
            {
                OriginalPaths[ret] = path;

                return ret;
            }
            /*if (!foundRootPath)
            {
                Error.WriteLine("Cannot find root path: " + path);
            }*/
            return path;
        }

        public override string Unnormalize(string path)
        {
            //return SymLayers.GetBasePath(path);
            return OriginalPaths.ContainsKey(path) ? OriginalPaths[path] : SwvLayers.GetBasePath(path);
        }

        public static PathNormalizer Instance;

        public static PathNormalizer GetInstance()
        {
            return Instance ?? (Instance = new SwvPathNormalizer());
        }
    }
}