using System.Collections.Generic;
using SpyStudio.Main;
using SpyStudio.Swv;
using SpyStudio.Tools;

namespace SpyStudio.Export.SWV
{
    public class SwvPathFilter : FileSystemViewer.PathFilter
    {
        public override CallEvent InternalIncludeEvent(CallEvent originalEvent, string fileSystemPath)
        {
            bool systemFile, foundRootPath, error;
            string filepart, originalPath = originalEvent.ParamMain;
            var ce = originalEvent.Clone();

            if (ce.Properties.ContainsKey("ProcMon"))
            {
                ce.ParamMain = FileSystemTools.ResolveProcMonPath(ce.ParamMain);
            }
            ce.ParamMain = SymLayers.GetSwvPath(ce.ParamMain, out error, out filepart, out systemFile,
                                                out foundRootPath);

            if (!error && filepart != "" && !systemFile && foundRootPath)
            {
                //if(ce.ParamMain.Contains("_B_]DESKTOP"))
                //    Console.WriteLine("");
                return ce;
            }
            if (!foundRootPath)
            {
                Error.WriteLine("Cannot find root path: " + originalEvent.ParamMain);
            }
            return null;
        }
        public override string UndoFiltering(string path)
        {
            return OriginalPaths.ContainsKey(path) ? OriginalPaths[path] : path;
        }
    }
}