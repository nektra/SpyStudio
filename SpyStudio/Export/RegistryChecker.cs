using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Extensions;
using System.Linq;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export
{
    public class RegistryChecker : TreeViewAdvNodeChecker<RegistryTree, RegistryTreeNode>
    {
        #region Properties

        protected readonly uint ID;
        protected readonly VirtualizationExport Export;
        protected AppBehaviourAnalyzer ApplicationAnalyzer;
        public RegistrySelect RegistrySelect { get; set; }

        protected readonly List<string> IncludedUUIDs;
        protected readonly ExportField<IEnumerable<FileEntry>> FilesToIncludeInPackage;

        #endregion

        #region Static constants

        protected static uint NextID;

        private static readonly Regex DefaultUserPath = new Regex(@"^HKEY_USERS\\+.DEFAULT");
        private static readonly Regex HKeyUnknownPath = new Regex(@"^HKEY_UNKNOWN", RegexOptions.IgnoreCase);
        private static readonly Regex SoftwareMicrosoftKey = new Regex(@"^(?:HKEY_CURRENT_USER|HKEY_LOCAL_MACHINE)\\+Software\\+Microsoft(?:\\|$)", RegexOptions.IgnoreCase);
        private static readonly Regex SystemKey = new Regex(@"^(?:HKEY_CURRENT_USER|HKEY_LOCAL_MACHINE)\\+System(?:\\|$)", RegexOptions.IgnoreCase);

        private static readonly Regex[] HardIgnores = new[]
                                                {
                                                    HKeyUnknownPath
                                                };

        private static readonly Regex[] SoftIgnores = new[]
                                                {
                                                    DefaultUserPath,
                                                    SoftwareMicrosoftKey,
                                                    SystemKey
                                                };

        #endregion

        #region Instantiation and Initialization

        private RegistryChecker(VirtualizationExport anExport)
        {
            ID = ++NextID;

            Export = anExport;

            IncludedUUIDs = new List<string>();

            FilesToIncludeInPackage = Export.GetField<IEnumerable<FileEntry>>(ExportFieldNames.OriginalFiles);

            #if DEBUG
            Log = new StreamWriter("Registry Checker Report " + ID + ".txt") { AutoFlush = true };
            NodeNameForLog = "Registry Key";
            #endif
        }
        
        public static RegistryChecker For(VirtualizationExport anExport, AppBehaviourAnalyzer analyzer)
        {
            var checker = new RegistryChecker(anExport) {ApplicationAnalyzer = analyzer};
            
            checker.SpecializeFor(anExport.CheckerType);

            return checker;
        }

        private void InitializeAllowabilityFunctions()
        {
            NodeFunctions.Add(new AllowabilityFunction<RegistryTreeNode>(GetAllowabilityFromSoftIgnores));
            NodeFunctions.Add(new AllowabilityFunction<RegistryTreeNode>(GetAllowabilityFromHardIgnores));
            //NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.AllowabilityFunction, KeyExists));
        }

        protected override void InitializeForInstallation()
        {
            InitializeAllowabilityFunctions();

            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfWasWritten));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsWinSxsKeyWithIncludedLib));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, UncheckIfIsWinSxsKeyWithoutIncludedLib));
        }

        protected override void InitializeForApplication()
        {
            InitializeAllowabilityFunctions();

            //NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfWasWritten));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfPathSeemsRelatedToTheApplication));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsCLSIDEntryAndRefersToAnIncludedFile));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsTypeLibEntryAndRefersToAnIncludedFile));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInterfaceEntryAndRefersToAnIncludedFileOrUUID));

            //These two rules MUST be the last ones.
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsWinSxsKeyWithIncludedLib));
            NodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsWinSxsKeyWithoutIncludedLib));

            SecondPassNodeFunctions.Add(new CheckFunction<RegistryTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsAppIDEntryAndTheAppIdIsChecked));
        }

        protected override void InitializeForAutoUpdate()
        {
            InitializeForApplication();
        }

        #endregion

        #region Node Allowability Rules

        private bool PathIsSideBySideWinners(RegistryTreeNode aRegistryTreeNode)
        {
            return Regex.IsMatch(aRegistryTreeNode.Path,
                                 @"^hkey_local_machine\\+software\\+microsoft\\+windows\\+currentversion\\+sidebyside\\+winners\\+.*",
                                 RegexOptions.IgnoreCase);
        }

        private Allowability GetAllowabilityFromHardIgnores(RegistryTreeNode aRegistryTreeNode)
        {
            var x = !HardIgnores.Any(p => p.IsMatch(aRegistryTreeNode.Path));
            return x ? Allowability.Max : Allowability.NotAllowed;
        }

        private Allowability GetAllowabilityFromSoftIgnores(RegistryTreeNode aRegistryTreeNode)
        {
            var x = PathIsSideBySideWinners(aRegistryTreeNode) ||
                    !SoftIgnores.Any(p => p.IsMatch(aRegistryTreeNode.Path));
            return x ? Allowability.Max : Allowability.NotCheckable;
        }

        #endregion

        #region Node Checkers

        private bool CheckIfPathSeemsRelatedToTheApplication(RegistryTreeNode aRegistryTreeNode)
        {
            var wordsInPath = aRegistryTreeNode.Path.SplitAsPath().Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s));

            var shouldBeChecked = wordsInPath.Any(w1 => ApplicationAnalyzer.ApplicationRelatedWords.Any(w1.SeemsSimilarTo));

            if(shouldBeChecked)
            {
                RegistrySelect.AddRelatedToApplicationNode(aRegistryTreeNode);
                aRegistryTreeNode.CheckSelfAndParentsAndChildrenIf(true);
                return true;
            }
            return false;
        }

        private bool CheckIfWasWritten(RegistryTreeNode aRegistryTreeNode)
        {
            var shouldBeChecked = (aRegistryTreeNode.Access & RegistryKeyAccess.Write) != 0;

            return aRegistryTreeNode.CheckSelfAndParentsIf(shouldBeChecked);
        }

        private bool CheckIfIsTypeLibEntryAndRefersToAnIncludedFile(RegistryTreeNode aRegistryTreeNode)
        {
            var shouldBeChecked = RegistryTools.PathIsUnder(aRegistryTreeNode.Path, @"HKEY_CLASSES_ROOT\TypeLib")
                                  && RefersToAnIncludedFile(aRegistryTreeNode);

            if (shouldBeChecked)
                IncludedUUIDs.Add(ExtractUUIDFrom(aRegistryTreeNode.Path));

            return aRegistryTreeNode.CheckWholeUUIDGroupIf(shouldBeChecked);
        }

        private bool CheckIfIsCLSIDEntryAndRefersToAnIncludedFile(RegistryTreeNode aRegistryTreeNode)
        {
            var shouldBeChecked = RegistryTools.PathIsUnder(aRegistryTreeNode.Path, @"HKEY_CLASSES_ROOT\CLSID")
                                  && RefersToAnIncludedFile(aRegistryTreeNode);
            if (shouldBeChecked)
                IncludedUUIDs.Add(ExtractUUIDFrom(aRegistryTreeNode.Path));

            return aRegistryTreeNode.CheckWholeUUIDGroupIf(shouldBeChecked);
        }

        private bool CheckIfIsInterfaceEntryAndRefersToAnIncludedFileOrUUID(RegistryTreeNode aRegistryTreeNode)
        {
            var shouldBeChecked = RegistryTools.PathIsUnder(aRegistryTreeNode.Path, @"HKEY_CLASSES_ROOT\Interface")
                                  && (RefersToAnIncludedFileOrUUID(aRegistryTreeNode));

            if (shouldBeChecked)
                IncludedUUIDs.Add(ExtractUUIDFrom(aRegistryTreeNode.Path));

            return aRegistryTreeNode.CheckWholeUUIDGroupIf(shouldBeChecked);
        }

        private List<string> _appIdsChecked; 
        private bool CheckIfIsAppIDEntryAndTheAppIdIsChecked(RegistryTreeNode aRegistryTreeNode)
        {
            var shouldBeChecked = false;
            var inAppId = RegistryTools.PathIsUnder(aRegistryTreeNode.Path, @"HKEY_CLASSES_ROOT\AppID");
            if (inAppId && aRegistryTreeNode.Text.StartsWith("{"))
            {
                var appId = aRegistryTreeNode.Parent;
                if (appId != null && appId.Text.ToLower() == "appid")
                {
                    if (_appIdsChecked == null)
                    {
                        _appIdsChecked = new List<string>();
                        // navigate AppID children looking for checked applications
                        // keep all IIDs of the checked Apps in the AppID value
                        foreach (var app in appId.Nodes)
                        {
                            if (!app.Text.StartsWith("{") && app.Checked)
                            {
                                var appRegNode = app as RegistryTreeNode;
                                if (appRegNode != null)
                                {
                                    RegValueInfo appIdValue;
                                    if (appRegNode.ValuesByName.TryGetValue("appid", out appIdValue))
                                    {
                                        if (!appIdValue.IsDataNull && appIdValue.Data.StartsWith("{"))
                                            _appIdsChecked.Add(appIdValue.Data.ToLower());
                                    }
                                }
                            }
                        }
                    }

                    // check IIDs nodes which App is checked
                    shouldBeChecked = _appIdsChecked.Contains(aRegistryTreeNode.Text.ToLower());
                }
            }

            return aRegistryTreeNode.CheckSelfAndParentsAndChildrenIf(shouldBeChecked);
        }
        #endregion

        #region Tools

        private static readonly Regex SxsSplitter = new Regex(@"^hkey_local_machine\\software\\microsoft\\windows\\currentversion\\sidebyside\\winners\\([^_]+)_([^_]+)_([0-9a-f]{16})_([^_]+)_([0-9a-f]{16})\\([^\\]*)", RegexOptions.IgnoreCase);

        private bool NodeIsSxsLike(RegistryTreeNode aRegistryTreeNode)
        {
            var s = aRegistryTreeNode.Path;
            var ret = SxsSplitter.IsMatch(s);
            return ret;
        }

        private static string GetSubValuesRegex(RegistryTreeNode node)
        {
            // find values which data is "01"
            var l = node.ValuesByName.Where(x => x.Key != "" && x.Value.Data == "01").ToList();
            if (l.Count == 0)
                return "";
            if (l.Count == 1)
                return Regex.Escape(l[0].Key);
            var ret = new StringBuilder();
            ret.Append("(?:");
            var first = true;
            foreach (var keyValuePair in l)
            {
                if (!first)
                    ret.Append("|");
                ret.Append(Regex.Escape(keyValuePair.Key));
                first = false;
            }
            ret.Append(")");
            return ret.ToString();
        }

        private bool NodeRefersToSideBySideLibrary(RegistryTreeNode aRegistryTreeNode)
        {
            var match = SxsSplitter.Match(aRegistryTreeNode.Path);
            Debug.Assert(match.Success);
            var generatedRegexString = "^" + Regex.Escape(SystemDirectories.Windows + "\\winsxs\\" + match.Groups[1]
                                 + "_" + match.Groups[2] + "_" + match.Groups[3])
                                 + "_" + GetSubValuesRegex(aRegistryTreeNode)
                                 + Regex.Escape("_" + match.Groups[4] + "_" /*+ match.Groups[5]*/) + @"[0-9a-fA-F]{16}(?:$|\\)";
            var regex = new Regex(generatedRegexString, RegexOptions.IgnoreCase);
            var ret = FilesToIncludeInPackage.Value.Any(x => regex.IsMatch(x.FileSystemPath));
            return ret;
        }

        private bool CheckIfIsWinSxsKeyWithIncludedLib(RegistryTreeNode aRegistryTreeNode)
        {
            var ret = NodeIsSxsLike(aRegistryTreeNode) && NodeRefersToSideBySideLibrary(aRegistryTreeNode);
            if (ret)
                aRegistryTreeNode.CheckSelfAndParents();
            return ret;
        }

        private bool UncheckIfIsWinSxsKeyWithoutIncludedLib(RegistryTreeNode aRegistryTreeNode)
        {
            var ret = NodeIsSxsLike(aRegistryTreeNode) && !NodeRefersToSideBySideLibrary(aRegistryTreeNode);
            if (ret)
                aRegistryTreeNode.CheckSelfAndParents();
            return ret;
        }

        readonly IDictionary _environmentVariables = Environment.GetEnvironmentVariables();
        List<string> _filepartsInPackage;
 
        private bool RefersToAnIncludedFile(RegistryTreeNode aRegistryTreeNode)
        {
            if(_filepartsInPackage == null)
            {
                _filepartsInPackage = new List<string>();
                foreach(var path in FilesToIncludeInPackage.Value)
                {
                    var filepart = FileSystemTools.GetFileName(path.Path);
                    if(!string.IsNullOrEmpty(filepart))
                    {
                        _filepartsInPackage.Add(filepart);
                    }
                }
            }
            var key =
                Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(aRegistryTreeNode.Path.Replace(@"HKEY_CLASSES_ROOT\", ""));

            if(key != null)
            {
                return
                    (from value in key.GetStringValues()
                     from filepart in _filepartsInPackage
                     where value.EndsWith(filepart)
                     select value).Any();
            }

            return false;
        }

        private bool RefersToAnIncludedFileOrUUID(RegistryTreeNode aRegistryTreeNode)
        {
            var key = RegistryTools.GetKeyFromFullPath(aRegistryTreeNode.Path);

            if (key == null && aRegistryTreeNode.AlternatePath != null)
                key = RegistryTools.GetKeyFromFullPath(aRegistryTreeNode.AlternatePath);

            if (key == null)
                return false;

            var matchingFile = key.GetStringValues().FirstOrDefault(
                value => IncludedUUIDs.Contains(value.ToLower())
                    || FilesToIncludeInPackage.Value.Any(f =>
                    f.FileSystemPath.EqualsPath(value.ExpandEnvironmentVariables(_environmentVariables))));

            var result = matchingFile != null;

            return result;
        }

        static Regex UUIDRegex = new Regex(@"{(\w|-)*}", RegexOptions.IgnoreCase);

        private string ExtractUUIDFrom(string aRegistryPath)
        {
            return UUIDRegex.Match(aRegistryPath).Value.ToLower();
        }

        #endregion

        public override void PerformCheckingOn(RegistryTree aTree)
        {
            base.PerformCheckingOn(aTree);

#if DEBUG
            Log.WriteLine("\n\nIncluded UUIDs:\n");
            foreach (var uuid in IncludedUUIDs)
                Log.WriteLine(uuid);
#endif
        }
    }
}