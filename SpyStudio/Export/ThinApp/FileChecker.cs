using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Export.Appv;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Tools;

namespace SpyStudio.Export.ThinApp
{
    class FileChecker : FileSystemViewChecker
    {
        protected readonly VirtualizationExport Export;

        protected readonly ExportField<ApplicationBehaviourAnalyzer> ApplicationAnalyzer;

        protected readonly ThinAppPathNormalizer PathNormalizer;

        protected readonly IEnumerable<Regex> DeviareDllNames;
        protected readonly IEnumerable<Regex> HardwareBrands;
        protected readonly List<Regex> IgnoredPaths;

        #region Instantiation

        public static FileSystemViewChecker For(ThinAppExport aThinAppExport)
        {
            var checker = new FileChecker(aThinAppExport);
            var captureType = (CaptureType)aThinAppExport.GetFieldValue(ExportFieldNames.CaptureMode);

            switch (captureType)
            {
                case CaptureType.Runtime:
                    checker.InitializeForRuntimeCapture();
                    break;

                case CaptureType.Installation:
                    checker.InitializeForInstallationCapture();
                    break;

                default:
                    throw new Exception("Unknown CaptureType");
            }

            return checker;
        }

        public static FileSystemViewChecker For(AppvExport anAppvExport)
        {
            var checker = new FileChecker(anAppvExport);
            var captureType = (CaptureType)anAppvExport.GetFieldValue(ExportFieldNames.CaptureMode);

            switch (captureType)
            {
                case CaptureType.Runtime:
                    checker.InitializeForRuntimeCapture();
                    break;

                case CaptureType.Installation:
                    checker.InitializeForInstallationCapture();
                    break;

                default:
                    throw new Exception("Unknown CaptureType");
            }

            return checker;
        }

        protected FileChecker(VirtualizationExport anExport)
        {
            Export = anExport;
            PathNormalizer = ThinAppPathNormalizer.GetInstance();
            IgnoredPaths = new List<Regex>();

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

            ApplicationAnalyzer = Export.GetField<ApplicationBehaviourAnalyzer>(ExportFieldNames.ApplicationBehaviourAnalizer);
        }

        private void InitializeIgnoredPaths()
        {
            IgnoredPaths.Clear();
            IgnoredPaths.Add(new Regex(@"^%History%(?:\\|$)"));
            IgnoredPaths.Add(new Regex(@"^%Cookies%(?:\\|$)"));
            IgnoredPaths.Add(new Regex(@"^%Internet Cache%(?:\\|$)"));
            IgnoredPaths.Add(new Regex(@"^%TEMP%(?:\\|$)"));
            //IgnoredPaths.Add(new Regex(@"(?:.*\\|^)DeviareCOM(?:64)?.dll$"));
            //IgnoredPaths.Add(new Regex(@"(?:.*\\|^)SpyStudioHelperPlugin(?:64)?.dll$"));
            IgnoredPaths.Add(new Regex(@"^%SystemSystem%(?:\\|$)", RegexOptions.IgnoreCase));
            IgnoredPaths.Add(new Regex(@"^%Local AppData%\\+Microsoft\\+Windows\\+Explorer\\.*", RegexOptions.IgnoreCase));
        }

        private void InitializeAllowabilityFunctions()
        {
            FileSystemTreeChecker.NodeFunctions.Add(
                new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromPathNullity));
            FileSystemTreeChecker.NodeFunctions.Add(
                new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromFileExistence));
            FileSystemTreeChecker.NodeFunctions.Add(
                new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromPathIgnores));
            FileSystemTreeChecker.NodeFunctions.Add(
                new AllowabilityFunction<FileSystemTreeNode>(GetAllowabilityFromHardwareSpecificity));
        }

        public void InitializeForInstallationCapture()
        {
            InitializeIgnoredPaths();
            InitializeAllowabilityFunctions();

            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfHasWriteAccess));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIrrelevantWinSxSSubdirs));
        }

        public void InitializeForRuntimeCapture()
        {
            InitializeIgnoredPaths();
            InitializeAllowabilityFunctions();

            //FileSystemTreeChecker.NodeFunctions.Add(
            //    new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfHasWriteAccess));
            //FileSystemTreeChecker.NodeFunctions.Add(
            //    new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfHasLoadAccess));

            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInApplicationProgramFilesFolder));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInApplicationAppDataFolder));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfIsInApplicationLocalAppDataFolder));

            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsStrictlyRelatedToTheHookingProcess));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsShellExtension));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsInSystemRootOrSystemSystemAndIsNotRelatedToTheApplication));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIfIsInADifferentProgramFilesFolderThanTheApplication));
            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.UncheckFunction, UncheckIrrelevantWinSxSSubdirs));

            FileSystemTreeChecker.NodeFunctions.Add(
                new CheckFunction<FileSystemTreeNode>(CheckFunctionType.CheckFunction, CheckIfFilePropertiesSeemToBeRelatedToTheApplication));
        }

        #endregion

        #region Allowability Rules

        protected bool DoesNotHaveAnIgnoredPath(FileSystemTreeNode aNode)
        {
            return IgnoredPaths.All(x => !x.IsMatch(aNode.FilePath));
        }

        protected Allowability GetAllowabilityFromPathIgnores(FileSystemTreeNode aNode)
        {
            return DoesNotHaveAnIgnoredPath(aNode) ? Allowability.Max : Allowability.NotAllowed;
        }

        protected bool FileExistsOrIsDirectory(FileSystemTreeNode aNode)
        {
            return aNode.IsDirectory || File.Exists(PathNormalizer.UndoNormalizationOf(aNode.FilePath));
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
            if (string.IsNullOrEmpty(ApplicationAnalyzer.Value.ProgramFilesFolderPath))
                return false;

            return aNode.CheckSelfAndParentsIf(aNode.FilePath.StartsWith(ApplicationAnalyzer.Value.ProgramFilesFolderPath));
        }

        protected bool CheckIfIsInApplicationAppDataFolder(FileSystemTreeNode aNode)
        {
            if (string.IsNullOrEmpty(ApplicationAnalyzer.Value.AppDataFolderPath))
                return false;

            return aNode.CheckSelfAndParentsIf(aNode.FilePath.StartsWith(ApplicationAnalyzer.Value.AppDataFolderPath));
        }

        protected bool CheckIfIsInApplicationLocalAppDataFolder(FileSystemTreeNode aNode)
        {
            if (string.IsNullOrEmpty(ApplicationAnalyzer.Value.LocalAppDataFolderPath))
                return false;

            return aNode.CheckSelfAndParentsIf(aNode.FilePath.StartsWith(ApplicationAnalyzer.Value.LocalAppDataFolderPath));
        }

        protected bool CheckIfFilePropertiesSeemToBeRelatedToTheApplication(FileSystemTreeNode aFileSystemTreeNode)
        {
            var shouldBeChecked = aFileSystemTreeNode.GetAllProperties().Any(p => ApplicationAnalyzer.Value.ApplicationRelatedWords.Any(p.SeemsSimilarTo));
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
                if(shellExKey.EndsWith("}"))
                {
                    var clsidStart = shellExKey.LastIndexOf('{');
                    if(clsidStart != -1)
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

                if (RegQueryValueEvent.HasData(ev))
                {
                    key = RegQueryValueEvent.GetKey(ev).ToLower();
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
                Any(p => ApplicationAnalyzer.Value.ApplicationRelatedWords.Any(p.SeemsSimilarTo));

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
            if (string.IsNullOrEmpty(ApplicationAnalyzer.Value.ProgramFilesFolderPath))
                return false;

            var isInProgramFilesFolder = ProgramFilesRegex.IsMatch(aFileSystemTreeNode.FilePath);

            if (!isInProgramFilesFolder)
                return false;

            var testant = aFileSystemTreeNode.FilePath;
            var tester = ApplicationAnalyzer.Value.ProgramFilesFolderPath;

            var isInApplicationProgramFilesFolder =
                testant.Equals(tester, StringComparison.InvariantCultureIgnoreCase) ||
                testant.StartsWith(tester + "\\", StringComparison.InvariantCultureIgnoreCase);

            // At this point, the node must be in the program files folder.
            var shouldBeUnchecked = !isInApplicationProgramFilesFolder;

            return aFileSystemTreeNode.UncheckSelfAndParentsWithoutCheckedChildrenIf(shouldBeUnchecked);
        }

        #endregion

        public new void PerformCheckingOn(FileSystemTree aFileSystemTree)
        {
            ApplicationAnalyzer.Value.Analyze(aFileSystemTree);

            base.PerformCheckingOn(aFileSystemTree);
        }
    }
}
