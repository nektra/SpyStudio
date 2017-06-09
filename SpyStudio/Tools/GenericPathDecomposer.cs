using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;

namespace SpyStudio.Tools
{
    public abstract class DecomposerState
    {
        public bool AnyPathsWereAdded { get; private set; }
        public bool IsNull;

        public virtual void Add(string str, bool isPath)
        {
            AnyPathsWereAdded |= isPath;
        }
        public virtual void MergeWith(DecomposerState state)
        {
            AnyPathsWereAdded |= state.AnyPathsWereAdded;
            IsNull &= state.IsNull;
        }
        public abstract DecomposerState New();

        public virtual bool PathExists(string path)
        {
            return FileSystemTools.DirectoryOrFileOrExecutableExists(path);
        }
    }

    public class GenericPathDecomposer
    {
        public static readonly char[] Splitters = { '"', ',', ' ' };

        private static DecomposerState Decompose(string path, int index, DecomposerState state)
        {
            if (index >= Splitters.Length)
            {
                state.IsNull = true;
                return state;
            }
            state.IsNull = false;

            var first = true;
            var splitter = Splitters[index];
            foreach (var s in path.Split(splitter))
            {
                if (first)
                    first = false;
                else
                    state.Add(splitter.ToString(), false);

                if (state.PathExists(s))
                {
                    state.Add(s, true);
                    continue;
                }

                var normalized = FileSystemTools.NormalizeQuestionMarkPath(s);
                if (state.PathExists(normalized))
                {
                    state.Add(normalized, true);
                    continue;
                }

                DecomposerState newState;
                for (int i = index + 1;; i++)
                {
                    newState = Decompose(s, i, state.New());

                    if (newState.IsNull)
                    {
                        newState = state.New();
                        state.Add(s, false);
                        break;
                    }
                    if (newState.AnyPathsWereAdded)
                        break;
                }
                state.MergeWith(newState);
            }
            return state;
        }
        
        private static readonly Func<string, DecomposerState, bool>[] SpecialCaseDecomposers =
        {
            DecomposeResUrl,
            DecomposeResourcePath,
        };

        public static void Decompose(string path, DecomposerState state)
        {
            if (path == null || SpecialCaseDecomposers.Any(f => f(path, state)))
                return;
            Decompose(path, 0, state);
        }
        
        private static readonly Regex[] ResUrlRegexes =
        {
            new Regex("^res://([^/]+)/([^/]+)/([^/]+)$", RegexOptions.IgnoreCase),
            new Regex("^res://([^/]+)/([^/]+)$", RegexOptions.IgnoreCase),
        };

        private static bool DecomposeResUrl(string path, DecomposerState state)
        {
            Match match = null;
            var success = false;

            foreach (var resUrlRegex in ResUrlRegexes)
            {
                match = resUrlRegex.Match(path);
                if (match.Success)
                {
                    success = true;
                    break;
                }
            }

            if (!success)
                return false;

            var groups = new string[match.Groups.Count];
            for (int i = 0; i < groups.Length; i++)
                groups[i] = match.Groups[i].ToString();

            path = HttpUtility.UrlDecode(groups[0]).AsNormalizedPath();
            state.Add("res://", false);
            state.Add(path, true);

            groups.Skip(1).ForEach(x => state.Add(x, false));

            return true;
        }

        
        private static readonly Regex ResourcePathRegex = new Regex("^@(.*),(-?[0-9]+)$");

        private static bool DecomposeResourcePath(string s, DecomposerState state)
        {
            var match = ResourcePathRegex.Match(s);
            if (!match.Success)
                return false;

            state.Add("@", false);
            state.Add(match.Groups[1].ToString(), true);
            state.Add("," + match.Groups[2].ToString(), false);

            return true;
        }
    }

    public static class PathExtractor
    {
        private class ExtractorDecomposerState : DecomposerState
        {
            public readonly List<string> Result = new List<string>();

            public override void Add(string str, bool isPath)
            {
                base.Add(str, isPath);
                if (isPath)
                    Result.Add(str);
            }

            public override DecomposerState New()
            {
                return new ExtractorDecomposerState();
            }

            public override void MergeWith(DecomposerState state)
            {
                base.MergeWith(state);
                Result.AddRange(((ExtractorDecomposerState)state).Result);
            }
        }

        public static IEnumerable<string> GetAllPathsInString(string str)
        {
            var ret = new ExtractorDecomposerState();
            GenericPathDecomposer.Decompose(str, ret);
            return ret.Result;
        }
    }

    public static class AppvPathExtractor
    {
        private class ExtractorDecomposerState : DecomposerState
        {
            public readonly List<string> Result = new List<string>();

            public override void Add(string str, bool isPath)
            {
                base.Add(str, isPath);
                if (isPath)
                    Result.Add(str);
            }

            public override DecomposerState New()
            {
                return new ExtractorDecomposerState();
            }

            public override void MergeWith(DecomposerState state)
            {
                base.MergeWith(state);
                Result.AddRange(((ExtractorDecomposerState)state).Result);
            }

            public override bool PathExists(string path)
            {
                return base.PathExists(AppvPathNormalizer.GetInstanceManifest().Unnormalize(path));
            }
        }

        public static IEnumerable<string> GetAllPathsInString(string str)
        {
            var ret = new ExtractorDecomposerState();
            GenericPathDecomposer.Decompose(str, ret);
            return ret.Result;
        }
    }
}
