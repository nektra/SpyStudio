using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using SpyStudio.Export;
using SpyStudio.Tools;
using System.Diagnostics;
using SpyStudio.Extensions;

namespace SpyStudio.FileSystem
{
    public abstract class PathNormalizer
    {
        protected string[] IgnoredPaths;

        protected PathNormalizer()
        {
            IgnoredPaths = new string[] { };
        }

        protected Dictionary<string, string> OriginalPaths = new Dictionary<string, string>();

        public bool IncludeEvent(CallEvent originalEvent, out CallEvent modifiedEvent, out string fileSystemPath)
        {
            modifiedEvent = originalEvent;
            fileSystemPath = originalEvent.ParamMain;

            if (ShouldIgnoreEvent(originalEvent))
                return false;

            var ret = InternalIncludeEvent(originalEvent, fileSystemPath);

            if (ret != null)
            {
                modifiedEvent = ret;
                //OriginalPaths[modifiedEvent.ParamMain] = fileSystemPath;
            }

            if (modifiedEvent.Params != null && modifiedEvent.Params.Length > 0)
                modifiedEvent.Params[0].Value = FileSystemTools.NormalizeSystemPaths(modifiedEvent.Params[0].Value);

            fileSystemPath = FileSystemTools.NormalizeSystemPaths(fileSystemPath);

            return ret != null;
        }

        public abstract string LocalAppDataPath { get; }
        public abstract string RoamingAppDataPath { get; }
        public abstract string RuntimePath { get; }
        public abstract string ProgramFiles { get; }
        public abstract string ProgramFiles86 { get; }

        public abstract CallEvent InternalIncludeEvent(CallEvent originalEvent, string fileSystemPath);

        protected abstract string InternalNormalize(string aPath);

        public string NonRecursiveNormalize(string path, OperationMode mode)
        {
            if (String.IsNullOrEmpty(path))
                return String.Empty;

            var longPath = path;
            Tuple<string[], string[]> split = null;
            try
            {
                split = FileSystemTools.GetCombinedPath(path);
                longPath = split.Item2.JoinPaths();
            }
            catch { }
            var normalized = InternalNormalize(longPath);
            if (split == null)
                return normalized;
            var normalizedSplit = normalized.SplitAsPath().ToArray();
            Debug.Assert(normalizedSplit.Length <= split.Item1.Length);
            var reassembledList = new List<string>();
            for (int i = 1; i <= normalizedSplit.Length; i++)
            {
                var originalName = split.Item1[split.Item1.Length - i];
                var longName = split.Item2[split.Item2.Length - i];
                var normalizedName = normalizedSplit[normalizedSplit.Length - i];
                string defaultingName;
                switch (mode)
                {
                    case OperationMode.MaintainPathLength:
                        defaultingName = originalName;
                        break;
                    case OperationMode.LengthenPath:
                        defaultingName = longName;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mode");
                }
                reassembledList.Add(normalizedName != longName ? normalizedName : defaultingName);
            }
            reassembledList.Reverse();
            return reassembledList.JoinPaths();
        }

        private class NormalizingDecomposerState : DecomposerState
        {
            public readonly StringBuilder Result = new StringBuilder();
            private readonly OperationMode _mode;
            private readonly PathNormalizer _normalizer;

            public NormalizingDecomposerState(PathNormalizer normalizer, OperationMode mode)
            {
                _mode = mode;
                _normalizer = normalizer;
            }

            public override void Add(string str, bool isPath)
            {
                base.Add(str, isPath);
                if (isPath)
                    str = _normalizer.NonRecursiveNormalize(str, _mode);
                Result.Append(str);
            }

            public override DecomposerState New()
            {
                return new NormalizingDecomposerState(_normalizer, _mode);
            }

            public override void MergeWith(DecomposerState state)
            {
                base.MergeWith(state);
                Result.Append(((NormalizingDecomposerState) state).Result);
            }
        }

        public string Normalize(string path, OperationMode mode)
        {
            if (path == null)
                return null;
            var state = new NormalizingDecomposerState(this, mode);
            GenericPathDecomposer.Decompose(path, state);
            if (!state.AnyPathsWereAdded)
            {
                var last = path.LastIndexOf('\\');
                if (last >= 0)
                    return Normalize(path.Substring(0, last), mode) + path.Substring(last);
            }
            return state.Result.ToString();
        }

        protected virtual OperationMode DefaultMode
        {
            get { return OperationMode.MaintainPathLength; }
        }

        public string Normalize(string path)
        {
            return Normalize(path, DefaultMode);
        }

        public abstract string Unnormalize(string path);

        protected virtual bool ShouldIgnoreEvent(CallEvent e)
        {
            return ShouldIgnorePath(e.ParamMain);
        }

        protected bool ShouldIgnorePath(string path)
        {
            return IgnoredPaths.Any(ig => Regex.IsMatch(path, ig, RegexOptions.IgnoreCase));
        }

        public static string EnsureSingleBackslashesIn(string aPath)
        {
            if (aPath == null)
                return "";

            return aPath.AsNormalizedPath();


        }
    }

    public enum OperationMode
    {
        //Leaves short file/directory names unchanged.
        MaintainPathLength,
        //Forces long versions of names.
        LengthenPath,
    }
}