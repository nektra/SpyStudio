using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.FileSystem;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export
{
    public class GeneralizedPathNormalizer : ThinApp.ThinAppPathNormalizer
    {

        private static readonly GeneralizedPathNormalizer Instance = new GeneralizedPathNormalizer();

        public new static GeneralizedPathNormalizer GetInstance()
        {
            return Instance;
        }

        protected GeneralizedPathNormalizer(){}

        static string ProcessRegistry(string path)
        {
            var ret = new StringBuilder();
            for (int i = 0; i < path.Length; )
            {
                if (path[i] == '%')
                {
                    if (RecognizePercentLiteral(path, ref i, false))
                    {
                        ret.Append('%');
                    }
                    else
                    {
                        string expanded;
                        int saved = i - 1;
                        if (!ProcessMacro(path, ref i, out expanded))
                        {
                            ret.Append(path.Substring(saved, i - saved));
                        }
                        else
                        {
                            ret.Append(expanded);
                        }

                    }
                }
                else
                {
                    ret.Append(path[i++]);
                }
            }
            return ret.ToString();
        }

        static bool RecognizePercentLiteral(string path, ref int i, bool inside)
        {
            i++;
            if (i >= path.Length)
                return !inside;
            if (path[i] == '%')
            {
                i++;
                return true;
            }
            return false;
        }

        static int HandleLoop(string path, ref int i, StringBuilder sb, char terminator)
        {
            if (path[i] == '%')
            {
                if (RecognizePercentLiteral(path, ref i, true))
                    sb.Append('%');
                else
                {
                    return -1;
                }
            }
            else if (terminator != '\0' && path[i] == terminator)
            {
                i++;
                return 0;
            }
            else
            {
                sb.Append(path[i]);
                i++;
            }
            return 1;
        }

        static bool ProcessMacro(string path, ref int i, out string macro)
        {
            var macroName = new StringBuilder();
            for (; i < path.Length; )
            {
                int hl = HandleLoop(path, ref i, macroName, ':');
                if (hl < 0)
                {
                    macro = null;
                    return false;
                }
                if (hl == 0)
                    break;
            }
            if (macroName.Length == 0 || macroName.ToString() != "reg")
            {
                macro = null;
                return false;
            }
            return ProcessRegMacro(path, ref i, out macro);
        }

        static bool ProcessRegMacro(string path, ref int i, out string macro)
        {
            var keyName = new StringBuilder();
            for (; i < path.Length; )
            {
                int hl = HandleLoop(path, ref i, keyName, ',');
                if (hl < 0)
                {
                    macro = null;
                    return false;
                }
                if (hl == 0)
                    break;
            }
            if (keyName.Length == 0)
            {
                macro = null;
                return false;
            }
            return ProcessRegMacro(path, ref i, keyName.ToString(), out macro);
        }

        static bool ProcessRegMacro(string path, ref int i, string keyName, out string macro)
        {
            var valueName = new StringBuilder();
            for (; i < path.Length; )
            {
                int hl = HandleLoop(path, ref i, valueName, '\0');
                if (hl < 0)
                    break;
            }
            var key = RegistryTools.GetKeyFromFullPath(keyName);
            if (key == null)
            {
                macro = string.Empty;
                return true;
            }
            var valueName2 = valueName.ToString();
            var value = key.GetStringValue(valueName2);
            macro = value ?? string.Empty;
            return true;
        }

        public override string Unnormalize(string path)
        {
            var performRegistryPostProcessing = path.StartsWith("$");
            if (performRegistryPostProcessing)
                path = path.Substring(1);
            var ret = base.Unnormalize(path);
            if (performRegistryPostProcessing)
                ret = ProcessRegistry(ret);
            return ret;
        }

        public static string Generalize(string path, PathNormalizer normalizer)
        {
            return GetInstance().Normalize(normalizer.Unnormalize(path));
        }

        public static string Specificize(string path, PathNormalizer normalizer)
        {
            return normalizer.Normalize(GetInstance().Unnormalize(path));
        }
    }
}
