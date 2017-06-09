using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Export;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using System.IO;

namespace SpyStudio.FileSystem
{
    public class FileSystemTreeChecker : TreeViewAdvNodeChecker<FileSystemTree, FileSystemTreeNode>
    {
        #region Static Properties

        protected static uint NextID;

        #endregion

        #region Properties

        protected readonly uint ID;

        public FileSelect FileSelect { get; set; }
        protected AppBehaviourAnalyzer ApplicationAnalyzer { get; set; }
        protected PathNormalizer PathNormalizer { get; set; }

        protected readonly IEnumerable<Regex> DeviareDllNames;
        protected readonly IEnumerable<Regex> HardwareBrands;
        protected readonly List<Regex> NotAllowedPaths;
        protected readonly List<Regex> NotCheckablePaths;

        #endregion

        #region Instantiation and Initialization

        public static FileSystemTreeChecker For(VirtualizationExport anExport, AppBehaviourAnalyzer anAnalyzer)
        {
            var checker = new FileSystemTreeChecker { ApplicationAnalyzer = anAnalyzer };

            checker.SpecializeFor(anExport.CheckerType);

            return checker;
        }

        protected FileSystemTreeChecker()
        {
            ID = ++NextID;

            #if DEBUG
            Log = new StreamWriter("File Checker Report " + ID + ".txt") {AutoFlush = true};
            NodeNameForLog = "File";
            #endif

            NotAllowedPaths = new List<Regex>();
            NotCheckablePaths = new List<Regex>();

            DeviareDllNames = new[] {
                                        new Regex(@"DeviareCOM(?:64)?\.dll"),
                                        new Regex(@"SpyStudioHelperPlugin(?:64)?\.dll")
                                    };

            HardwareBrands = new[]
                                    {
                                        new Regex(@"nvidia", RegexOptions.IgnoreCase),
                                        new Regex(@"radeon", RegexOptions.IgnoreCase),
                                        new Regex(@"[\b]ATI[\b]", RegexOptions.IgnoreCase)
                                    };
        }

        private void InitializeIgnoredPaths()
        {
            NotCheckablePaths.Clear();
            NotCheckablePaths.Add(new Regex(@"^%SystemSystem(?:x64)?%(?:\\|$)", RegexOptions.IgnoreCase));

            NotAllowedPaths.Clear();
            NotAllowedPaths.Add(new Regex(@"^%History%(?:\\|$)"));
            NotAllowedPaths.Add(new Regex(@"^%Cookies%(?:\\|$)"));
            NotAllowedPaths.Add(new Regex(@"^%Internet Cache%(?:\\|$)"));
            NotAllowedPaths.Add(new Regex(@"^%TEMP%(?:\\|$)"));
            //IgnoredPaths.Add(new Regex(@"(?:.*\\|^)DeviareCOM(?:64)?.dll$"));
            //IgnoredPaths.Add(new Regex(@"(?:.*\\|^)SpyStudioHelperPlugin(?:64)?.dll$"));
            //IgnoredPaths.Add(new Regex(@"^%SystemSystem(?:x64)?%(?:\\|$)", RegexOptions.IgnoreCase));
            NotAllowedPaths.Add(new Regex(@"^%Local AppData%\\+Microsoft\\+Windows\\+Explorer\\.*", RegexOptions.IgnoreCase));
        }

        private void InitializeAllowabilityFunctions()
        {
            NodeFunctions.Add( new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromPathNullity));
            NodeFunctions.Add( new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromFileExistence));
            NodeFunctions.Add( new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromPathIgnores));
            NodeFunctions.Add( new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromHardwareSpecificity));
        }

        protected override void InitializeForInstallation()
        {
            InitializeIgnoredPaths();
            InitializeAllowabilityFunctions();

            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfHasWriteAccess));
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIrrelevantWinSxSSubdirs));
        }

        protected override void InitializeForApplication()
        {
            InitializeIgnoredPaths();
            InitializeAllowabilityFunctions();

            //FileSystemTreeChecker.NodeFunctions.Add(
            //    new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfHasWriteAccess));
            //FileSystemTreeChecker.NodeFunctions.Add(
            //    new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfHasLoadAccess));

            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInApplicationProgramFilesFolder));
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInApplicationAppDataFolder));
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInApplicationLocalAppDataFolder));

            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsStrictlyRelatedToTheHookingProcess));
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsShellExtension));
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsInSystemRootOrSystemSystemAndIsNotRelatedToTheApplication));
            NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsInADifferentProgramFilesFolderThanTheApplication));
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIrrelevantWinSxSSubdirs));
                       
            NodeFunctions.Add( new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfFilePropertiesSeemToBeRelatedToTheApplication));
        }

        protected override void InitializeForAutoUpdate()
        {
            InitializeForApplication();
        }

        #endregion

        #region Allowability Rules

        protected bool PathIsNotAllowed(FileSystemTreeNode aNode)
        {
            return NotAllowedPaths.Any(x => x.IsMatch(aNode.FilePath));
        }

        protected bool PathIsNotCheckable(FileSystemTreeNode aNode)
        {
            return NotCheckablePaths.Any(x => x.IsMatch(aNode.FilePath));
        }

        protected Allowability GetAllowabilityFromPathIgnores(FileSystemTreeNode aNode)
        {
            if (PathIsNotAllowed(aNode))
                return Allowability.NotAllowed;

            return PathIsNotCheckable(aNode) ? Allowability.NotCheckable : Allowability.Max;
        }

        protected bool FileExistsOrIsDirectory(FileSystemTreeNode aNode)
        {
            return aNode.IsDirectory || File.Exists(PathNormalizer.Unnormalize(aNode.FilePath));
        }

        protected Allowability GetAllowabilityFromFileExistence(FileSystemTreeNode aNode)
        {
            return FileExistsOrIsDirectory(aNode) ? Allowability.Max : Allowability.NotAllowed;
        }

        protected Allowability GetAllowabilityFromPathNullity(FileSystemTreeNode aNode)
        {
            return aNode.FilePath != null ? Allowability.Max : Allowability.NotAllowed;
        }

        protected bool IsNotHardwareSpecific(FileSystemTreeNode aNode)
        {
            if (aNode == null)
                return true;

            var hasNoHardwareSpecificParents = aNode.Parent == null || IsNotHardwareSpecific(aNode.Parent as FileSystemTreeNode);

            return hasNoHardwareSpecificParents && HardwareBrands.All(brand => !brand.IsMatch(aNode.FilePath) && !brand.IsMatch(aNode.Company));
        }

        protected Allowability GetAllowabilityFromHardwareSpecificity(FileSystemTreeNode aNode)
        {
            return IsNotHardwareSpecific(aNode) ? Allowability.Max : Allowability.NotCheckable;
        }

        #endregion

        #region Entry Checkers

        protected bool CheckIfHasLoadAccess(FileSystemTreeNode aNode)
        {
            const FileSystemAccess mask = FileSystemAccess.LoadLibrary | FileSystemAccess.CreateProcess;

            return aNode.CheckSelfAndParentsIf((aNode.ToFileEntry().Access & mask) != 0);
        }

        protected bool CheckIfHasWriteAccess(FileSystemTreeNode aNode)
        {
            const FileSystemAccess mask = FileSystemAccess.Write | FileSystemAccess.CreateDirectory | FileSystemAccess.WriteAttributes;
            return aNode.CheckSelfAndParentsIf((aNode.ToFileEntry().Access & mask) != 0);
        }

        protected bool CheckIfIsInApplicationProgramFilesFolder(FileSystemTreeNode aNode)
        {
            if (string.IsNullOrEmpty(ApplicationAnalyzer.ProgramFilesFolderPath))
                return false;

            return aNode.CheckSelfAndParentsIf(aNode.FilePath.StartsWith(ApplicationAnalyzer.ProgramFilesFolderPath));
        }

        protected bool CheckIfIsInApplicationAppDataFolder(FileSystemTreeNode aNode)
        {
            if (string.IsNullOrEmpty(ApplicationAnalyzer.AppDataFolderPath))
                return false;

            return aNode.CheckSelfAndParentsIf(aNode.FilePath.StartsWith(ApplicationAnalyzer.AppDataFolderPath));
        }

        protected bool CheckIfIsInApplicationLocalAppDataFolder(FileSystemTreeNode aNode)
        {
            if (string.IsNullOrEmpty(ApplicationAnalyzer.LocalAppDataFolderPath))
                return false;

            return aNode.CheckSelfAndParentsIf(aNode.FilePath.StartsWith(ApplicationAnalyzer.LocalAppDataFolderPath));
        }

        protected bool CheckIfFilePropertiesSeemToBeRelatedToTheApplication(FileSystemTreeNode aFileSystemTreeNode)
        {
            var shouldBeChecked = aFileSystemTreeNode.GetAllProperties().Any(p => ApplicationAnalyzer.ApplicationRelatedWords.Any(p.SeemsSimilarTo));
            if (aFileSystemTreeNode.IsDirectory && shouldBeChecked)
            {
                FileSelect.RelatedToApplication.Add(aFileSystemTreeNode);
                aFileSystemTreeNode.CheckSelfAndParentsAndChildrenIf(true);
                return true;
            }

            return aFileSystemTreeNode.CheckSelfAndParentsIf(shouldBeChecked);
        }

        #endregion

        #region Entry Uncheckers

        protected bool UncheckIfIsStrictlyRelatedToTheHookingProcess(FileSystemTreeNode aFileSystemTreeNode)
        {
            var shouldBeUnchecked = DeviareDllNames.Any(n => n.IsMatch(aFileSystemTreeNode.FileString));

            //if (((FileSystemTreeNode)parent).FileString.StartsWith(@"%Drive_") && parent.Nodes.All(n => !n.IsChecked))
            //    parent.GetUnchecked();

            return aFileSystemTreeNode.UncheckSelfAndParentsWithoutCheckedChildrenIf(shouldBeUnchecked);
        }

        Dictionary<string, string> _comServers = null;
        readonly HashSet<string> _shellExtClsid = new HashSet<string>();
        readonly HashSet<string> _shellExtFiles = new HashSet<string>();
        readonly Regex _clsidString = new Regex(@"^hkey_local_machine\\software\\classes\\clsid\\{.*}\\inprocserver.*");
        readonly Regex _clsidStringWowClasses = new Regex(@"^hkey_local_machine\\software\\wow6432node\\classes\\clsid\\{.*}\\inprocserver.*");
        readonly Regex _clsidStringClassesWow = new Regex(@"^hkey_local_machine\\software\\classes\\wow6432node\\clsid\\{.*}\\inprocserver.*");
        readonly Regex _classesStringAsterisk = new Regex(@"^hkey_local_machine\\software\\classes\\\*\\shellex\\.*");
        readonly Regex _classesStringAsteriskWowClasses = new Regex(@"^hkey_local_machine\\software\\wow6432node\\classes\\\*\\shellex\\.*");
        readonly Regex _classesStringAsteriskClassesWow = new Regex(@"^hkey_local_machine\\software\\classes\\wow6432node\\\*\\shellex\\.*");

        readonly Regex _shellExAssociation = new Regex(@"^hkey_local_machine\\software\\classes\\\..*\\shellex\\.*");
        readonly Regex _shellExAssociationClassesWow = new Regex(@"^hkey_local_machine\\software\\classes\\wow6432node\\\..*\\shellex\\.*");
        readonly Regex _shellExAssociationWowClasses = new Regex(@"^hkey_local_machine\\software\\wow6432node\\classes\\\..*\\shellex\\.*");
        //\\"hkey_local_machine\\software\\classes\\wow6432node\\clsid\\{1f486a52-3cb1-48fd-8f50-b8dc300d9f9d}\\inprocserver32"
        readonly Regex _shellExIconOverlay = new Regex(@"^hkey_local_machine\\software\\microsoft\\windows\\currentversion\\explorer\\shelliconoverlayidentifiers\\.*");
        //\\"hkey_local_machine\\software\\classes\\wow6432node\\clsid\\{1f486a52-3cb1-48fd-8f50-b8dc300d9f9d}\\inprocserver32"
        readonly Regex _shellExIconOverlayWow = new Regex(@"^hkey_local_machine\\software\\wow6432node\\microsoft\\windows\\currentversion\\explorer\\shelliconoverlayidentifiers\\.*");

        void AddComServer(string clsid, string valueData)
        {
            if (!string.IsNullOrEmpty(clsid))
            {
                var file = FileSystemTools.GetFileName(valueData);
                if (!string.IsNullOrEmpty(valueData))
                {
                    _comServers[clsid] = file;
                }
            }
        }
        void AddShellEx(string shellExKey, string valueName, string valueData)
        {
            if (!string.IsNullOrEmpty(shellExKey))
            {
                // type [HKEY_CLASSES_ROOT\*\shellex\PropertySheetHandlers\{3EA48300-8CF6-101B-84FB-666CCB9BCD32}]
                if (shellExKey.EndsWith("}"))
                {
                    var clsidStart = shellExKey.LastIndexOf('{');
                    if (clsidStart != -1)
                    {
                        var clsid = shellExKey.Substring(clsidStart);
                        _shellExtClsid.Add(clsid);
                    }
                }
                else if (string.IsNullOrEmpty(valueName) && valueData.StartsWith("{") && valueData.EndsWith("}"))
                {
                    _shellExtClsid.Add(valueData);
                }
            }
        }

        private int _processedEventsCount;
        private bool EventsShellExtensionFinder(IEnumerable<CallEvent> evToReport, int totalEvents)
        {
            foreach (var ev in evToReport)
            {
                string key;
                string valueName;
                string valueData;

                if (RegQueryValueEvent.IsDataComplete(ev))
                {
                    key = RegQueryValueEvent.GetParentKey(ev).ToLower();
                    valueName = RegQueryValueEvent.GetName(ev).ToLower();
                    valueData = RegQueryValueEvent.GetData(ev).ToLower();

                    if (string.IsNullOrEmpty(valueName) && !string.IsNullOrEmpty(valueData))
                    {
                        if (_clsidString.IsMatch(key) || _clsidStringWowClasses.IsMatch(key) || _clsidStringClassesWow.IsMatch(key))
                        {
                            var firstCharClsid = key.LastIndexOf("{", StringComparison.InvariantCulture);
                            var lastCharClsid = key.LastIndexOf("}", StringComparison.InvariantCulture);

                            var clsid = key.Substring(firstCharClsid, lastCharClsid - firstCharClsid + 1);
                            AddComServer(clsid, valueData);
                        }
                    }
                }
                else
                {
                    key = ev.ParamMain;
                    valueName = string.Empty;
                    valueData = string.Empty;
                }

                if (_classesStringAsterisk.IsMatch(key) || _classesStringAsteriskClassesWow.IsMatch(key) ||
                    _classesStringAsteriskWowClasses.IsMatch(key))
                {
                    AddShellEx(key, valueName, valueData);
                }
                else if (_shellExAssociation.IsMatch(key) || _shellExAssociationWowClasses.IsMatch(key) ||
                    _shellExAssociationClassesWow.IsMatch(key))
                {
                    AddShellEx(key, valueName, valueData);
                }
                else if (_shellExIconOverlay.IsMatch(key) || _shellExIconOverlayWow.IsMatch(key))
                {
                    AddShellEx(key, valueName, valueData);
                }
                if (++_processedEventsCount == 500)
                {
                    Application.DoEvents();
                    _processedEventsCount = 0;
                }
            }
            return true;
        }

        protected bool UncheckIfIsShellExtension(FileSystemTreeNode aNode)
        {
            //if(_comServers == null)
            //{
            //    _comServers = new Dictionary<string, string>();

            //    var data = new EventsReportData(Export.TraceId, false, EventsShellExtensionFinder)
            //                   {
            //                       EventsToReport = EventsReportData.EventType.Registry,
            //                       ReportBeforeEvents = false,
            //                       EventResultsIncluded = EventsReportData.EventResult.Success
            //                   };
            //    EventDatabaseMgr.GetInstance().RefreshEvents(data);
            //    data.Event.WaitOne();

            //    foreach(var shellEx in _shellExtClsid)
            //    {
            //        string path;
            //        if(_comServers.TryGetValue(shellEx, out path))
            //        {
            //            _shellExtFiles.Add(path);
            //        }
            //    }
            //}

            //if (_shellExtFiles.Contains(aNode.FileString.ToLower()))
            //{
            //    var isRelatedToTheApplication = aNode.GetAllProperties().
            //        Any(p => ApplicationAnalyzer.Value.ApplicationRelatedWords.Any(w => w.SeemsSimilarTo(p)));
            //    return aNode.UncheckSelfAndParentsWithoutCheckedChildrenIf(!isRelatedToTheApplication);
            //}

            return false;
        }

        private static readonly Regex SystemSystemRegex = new Regex(@"^%SystemSystem%(?:\\|$)", RegexOptions.IgnoreCase);
        private static readonly Regex SystemRootRegex = new Regex(@"^%SystemRoot%(?:\\|$)", RegexOptions.IgnoreCase);

        protected bool UncheckIfIsInSystemRootOrSystemSystemAndIsNotRelatedToTheApplication(FileSystemTreeNode aFileSystemTreeNode)
        {

            var isInSystemRootOrSystemSystem = SystemSystemRegex.IsMatch(aFileSystemTreeNode.FilePath)
                                               || SystemRootRegex.IsMatch(aFileSystemTreeNode.FilePath);

            var isRelatedToTheApplication = aFileSystemTreeNode.GetAllProperties().
                Any(p => ApplicationAnalyzer.ApplicationRelatedWords.Any(p.SeemsSimilarTo));

            return
                aFileSystemTreeNode.UncheckSelfAndParentsWithoutCheckedChildrenIf(isInSystemRootOrSystemSystem &&
                                                                                  !isRelatedToTheApplication);
        }

        private static readonly Regex[] IrrelevantWinSxSSubdirRegexes = new[]
                                                                            {
                                                                                new Regex(
                                                                                    @"^%SystemRoot%\\+WinSxS\\+[^_]+_.+_[0-9a-fA-F]{16}_[0-9.]+_[^_]+_[0-9a-fA-F]{16}(?:\\|$)",
                                                                                    RegexOptions.IgnoreCase),
                                                                                new Regex(
                                                                                    @"^%SystemRoot%\\+WinSxS\\+Manifests(?:\\|$)",
                                                                                    RegexOptions.IgnoreCase),
                                                                                new Regex(
                                                                                    @"^%SystemRoot%\\+WinSxS\\+Catalogs(?:\\|$)",
                                                                                    RegexOptions.IgnoreCase)
                                                                            };
        private static readonly Regex IsInWinSxsDirRegex = new Regex(@"^%SystemRoot%\\+WinSxS\\+[^\\]+", RegexOptions.IgnoreCase);

        protected bool UncheckIrrelevantWinSxSSubdirs(FileSystemTreeNode aFileSystemTreeNode)
        {
            var path = aFileSystemTreeNode.FilePath;
            var none = !IrrelevantWinSxSSubdirRegexes.Any(x => x.IsMatch(path));
            //#if DEBUG
            //            if (!any && IsInWinSxsDirRegex.IsMatch(path) && Debugger.IsAttached)
            //                Debugger.Break();
            //#endif
            return aFileSystemTreeNode.UncheckSelfAndParentsWithoutCheckedChildrenIf(none && IsInWinSxsDirRegex.IsMatch(path));
        }

        private static readonly Regex ProgramFilesRegex = new Regex(@"^%ProgramFilesDir(?:\(x64\))?%(?:\\|$)", RegexOptions.IgnoreCase);

        private bool UncheckIfIsInADifferentProgramFilesFolderThanTheApplication(FileSystemTreeNode aFileSystemTreeNode)
        {
            if (string.IsNullOrEmpty(ApplicationAnalyzer.ProgramFilesFolderPath))
                return false;

            var isInProgramFilesFolder = ProgramFilesRegex.IsMatch(aFileSystemTreeNode.FilePath);

            if (!isInProgramFilesFolder)
                return false;

            var testant = aFileSystemTreeNode.FileSystemPath;
            var tester = ApplicationAnalyzer.ProgramFilesFolderPath;

            var isInApplicationProgramFilesFolder =
                testant.Equals(tester, StringComparison.InvariantCultureIgnoreCase) ||
                testant.StartsWith(tester + "\\", StringComparison.InvariantCultureIgnoreCase);

            // At this point, the node must be in the program files folder.
            var shouldBeUnchecked = !isInApplicationProgramFilesFolder;

            return aFileSystemTreeNode.UncheckSelfAndParentsWithoutCheckedChildrenIf(shouldBeUnchecked);
        }

        #endregion

        public override void PerformCheckingOn(FileSystemTree aTree)
        {
            PathNormalizer = aTree.PathNormalizer;
            base.PerformCheckingOn(aTree);
        }

        #region DEBUG

        [Conditional("DEBUG")]
        public void ReleaseLogFile()
        {
#if DEBUG
            Log.Flush();
            Log.Close();
#endif
        }

        #endregion
    }
}
