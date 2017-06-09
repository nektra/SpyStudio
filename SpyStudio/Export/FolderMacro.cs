using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.FileSystem;
using System.Diagnostics;
using SpyStudio.Extensions;

namespace SpyStudio.Export
{
    public class FolderMacro
    {
        //protected readonly Regex Regex;
        protected readonly MatchEvaluator Evaluator;

        public string Path;
        public string Macro { get; private set; }
        public Regex Regex { get; private set; }

        public FolderMacro(string aMacro, string aPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(aPath) && !string.IsNullOrEmpty(aMacro), "Can't create FolderMacro with null path or macro.");

            Path = aPath.AsNormalizedPath();
            Macro = aMacro;
            var escaped = Regex.Escape(Path);
            Regex = new Regex("^(" + escaped + @")(?:\\.*|$)", RegexOptions.IgnoreCase);
            Evaluator = null;
        }

        private bool UsesRegex
        {
            get { return Regex != null; }
        }

        public FolderMacro(string pattern, MatchEvaluator eval)
        {
            Macro = null;
            Regex = new Regex(pattern, RegexOptions.IgnoreCase);
            Evaluator = eval;
        }

        private static string VariableReplace(string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        private static string CaseInsensitiveReplace(string str, string oldValue, string newValue)
        {
            return VariableReplace(str, oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public virtual string DoReplacement(string src, out int replacementSize)
        {
            replacementSize = 0;
            var ret = src;
            var match = Regex.Match(src);
            if (match.Success)
            {
                var replace = match.Groups[1];
                replacementSize = replace.Length;
                if (Evaluator != null)
                    ret = Regex.Replace(src, Evaluator);
                else
                    ret = Macro + src.Substring(replacementSize);
            }
            return ret;
        }
    }

    public class ResourcesFolderMacro : FolderMacro
    {
#region LanguageStrings
        private HashSet<string> _languageStrings = new HashSet<string>
                                                {
                                                    "ar",
                                                    "bg",
                                                    "ca",
                                                    "zh-Hans",
                                                    "cs",
                                                    "da",
                                                    "de",
                                                    "el",
                                                    "en",
                                                    "es",
                                                    "fi",
                                                    "fr",
                                                    "he",
                                                    "hu",
                                                    "is",
                                                    "it",
                                                    "ja",
                                                    "ko",
                                                    "nl",
                                                    "no",
                                                    "pl",
                                                    "pt",
                                                    "ro",
                                                    "ru",
                                                    "hr",
                                                    "sk",
                                                    "sq",
                                                    "sv",
                                                    "th",
                                                    "tr",
                                                    "ur",
                                                    "id",
                                                    "uk",
                                                    "be",
                                                    "sl",
                                                    "et",
                                                    "lv",
                                                    "lt",
                                                    "fa",
                                                    "vi",
                                                    "hy",
                                                    "az",
                                                    "eu",
                                                    "mk",
                                                    "af",
                                                    "ka",
                                                    "fo",
                                                    "hi",
                                                    "ms",
                                                    "kk",
                                                    "ky",
                                                    "sw",
                                                    "uz",
                                                    "tt",
                                                    "pa",
                                                    "gu",
                                                    "ta",
                                                    "te",
                                                    "kn",
                                                    "mr",
                                                    "sa",
                                                    "mn",
                                                    "gl",
                                                    "kok",
                                                    "syr",
                                                    "dv",
                                                    "ar-SA",
                                                    "bg-BG",
                                                    "ca-ES",
                                                    "zh-TW",
                                                    "cs-CZ",
                                                    "da-DK",
                                                    "de-DE",
                                                    "el-GR",
                                                    "en-US",
                                                    "es-ES_tradnl",
                                                    "fi-FI",
                                                    "fr-FR",
                                                    "he-IL",
                                                    "hu-HU",
                                                    "is-IS",
                                                    "it-IT",
                                                    "ja-JP",
                                                    "ko-KR",
                                                    "nl-NL",
                                                    "nb-NO",
                                                    "pl-PL",
                                                    "pt-BR",
                                                    "ro-RO",
                                                    "ru-RU",
                                                    "hr-HR",
                                                    "sk-SK",
                                                    "sq-AL",
                                                    "sv-SE",
                                                    "th-TH",
                                                    "tr-TR",
                                                    "ur-PK",
                                                    "id-ID",
                                                    "uk-UA",
                                                    "be-BY",
                                                    "sl-SI",
                                                    "et-EE",
                                                    "lv-LV",
                                                    "lt-LT",
                                                    "fa-IR",
                                                    "vi-VN",
                                                    "hy-AM",
                                                    "az-Latn-AZ",
                                                    "eu-ES",
                                                    "mk-MK",
                                                    "af-ZA",
                                                    "ka-GE",
                                                    "fo-FO",
                                                    "hi-IN",
                                                    "ms-MY",
                                                    "kk-KZ",
                                                    "ky-KG",
                                                    "sw-KE",
                                                    "uz-Latn-UZ",
                                                    "tt-RU",
                                                    "pa-IN",
                                                    "gu-IN",
                                                    "ta-IN",
                                                    "te-IN",
                                                    "kn-IN",
                                                    "mr-IN",
                                                    "sa-IN",
                                                    "mn-MN",
                                                    "gl-ES",
                                                    "kok-IN",
                                                    "syr-SY",
                                                    "dv-MV",
                                                    "ar-IQ",
                                                    "zh-CN",
                                                    "de-CH",
                                                    "en-GB",
                                                    "es-MX",
                                                    "fr-BE",
                                                    "it-CH",
                                                    "nl-BE",
                                                    "nn-NO",
                                                    "pt-PT",
                                                    "sr-Latn-CS",
                                                    "sv-FI",
                                                    "az-Cyrl-AZ",
                                                    "ms-BN",
                                                    "uz-Cyrl-UZ",
                                                    "ar-EG",
                                                    "zh-HK",
                                                    "de-AT",
                                                    "en-AU",
                                                    "es-ES",
                                                    "fr-CA",
                                                    "sr-Cyrl-CS",
                                                    "ar-LY",
                                                    "zh-SG",
                                                    "de-LU",
                                                    "en-CA",
                                                    "es-GT",
                                                    "fr-CH",
                                                    "ar-DZ",
                                                    "zh-MO",
                                                    "de-LI",
                                                    "en-NZ",
                                                    "es-CR",
                                                    "fr-LU",
                                                    "ar-MA",
                                                    "en-IE",
                                                    "es-PA",
                                                    "fr-MC",
                                                    "ar-TN",
                                                    "en-ZA",
                                                    "es-DO",
                                                    "ar-OM",
                                                    "en-JM",
                                                    "es-VE",
                                                    "ar-YE",
                                                    "en-029",
                                                    "es-CO",
                                                    "ar-SY",
                                                    "en-BZ",
                                                    "es-PE",
                                                    "ar-JO",
                                                    "en-TT",
                                                    "es-AR",
                                                    "ar-LB",
                                                    "en-ZW",
                                                    "es-EC",
                                                    "ar-KW",
                                                    "en-PH",
                                                    "es-CL",
                                                    "ar-AE",
                                                    "es-UY",
                                                    "ar-BH",
                                                    "es-PY",
                                                    "ar-QA",
                                                    "es-BO",
                                                    "es-SV",
                                                    "es-HN",
                                                    "es-NI",
                                                    "es-PR",
                                                    "zh-Hant",
                                                };
#endregion

        public ResourcesFolderMacro(string pattern, MatchEvaluator eval) : base(pattern, eval)
        {
        }

        public override string DoReplacement(string src, out int replacementSize)
        {
            replacementSize = 0;
            var ret = src;
            var match = Regex.Match(src);
            if (match.Success)
            {
                var replace = match.Groups[1];
                var langId = match.Groups[2].ToString();
                if (!_languageStrings.Contains(langId))
                    return ret;
                replacementSize = replace.Length;
                ret = Macro + src.Substring(replacementSize);
            }
            return ret;
        }
    }
}
