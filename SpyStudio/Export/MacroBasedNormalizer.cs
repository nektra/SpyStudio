using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Tools;

namespace SpyStudio.Export
{
    public abstract class MacroBasedNormalizer : PathNormalizer
    {
        protected readonly List<FolderMacro> Paths = new List<FolderMacro>();

        protected override string InternalNormalize(string src)
        {
            bool replacementPerformed;
            return InternalNormalize(src, out replacementPerformed);
        }

        public virtual bool NormalizeSeparators
        {
            get { return true; }
        }

        public virtual string InternalNormalize(string src, out bool replacementPerformed)
        {
            var maxSize = 0;
            string ret = null;

            foreach (var folderMacro in Paths)
            {
                int size;
                var temp = folderMacro.DoReplacement(src, out size);
                if (size <= maxSize)
                    continue;
                maxSize = size;
                ret = temp;
            }
            replacementPerformed = ret != null;
            ret = ret ?? src;
            if (NormalizeSeparators)
                ret = ret.AsNormalizedPath();
            return ret;
        }


        public override CallEvent InternalIncludeEvent(CallEvent originalEvent, string fileSystemPath)
        {
            var ce = originalEvent.Clone();
            if (ce.IsProcMon)
                ce.ParamMain = FileSystemTools.ResolveProcMonPath(ce.ParamMain);
            ce.ParamMain = Normalize(ce.ParamMain);

            return ce;
        }

        public override string Unnormalize(string path)
        {
            if (OriginalPaths.ContainsKey(path))
                return OriginalPaths[path];
            bool wasExpanded;
            var expandedString = ExpandFolderMacro(path, out wasExpanded);
            return wasExpanded ? expandedString : path;
        }

        protected virtual Regex MacroRegex { get { return null; } }

        protected Match MatchMacro(string path, out string macro)
        {
            macro = null;
            var match = MacroRegex.Match(path);
            if (match.Success)
                macro = match.Groups[0].ToString().ToLower();
            return match;
        }

        public virtual string ExpandFolderMacro(string path, out bool expanded)
        {
            expanded = false;
            string macro;
            var match = MatchMacro(path, out macro);
            if (!match.Success)
                return path;

            foreach (var folderMacro in Paths)
            {
                if (string.IsNullOrEmpty(folderMacro.Macro) || folderMacro.Macro.ToLower() != macro.ToLower())
                    continue;

                expanded = true;
                var ret = folderMacro.Path + path.Substring(macro.Length);
                return ret;
            }

            return path;
        }

        protected void AddToPathsIfNotNullOrEmpty(string aMacro, string aPath)
        {
            if (string.IsNullOrEmpty(aMacro) || string.IsNullOrEmpty(aPath))
                return;

            Paths.Add(new FolderMacro(aMacro, aPath));
        }
    }
}
