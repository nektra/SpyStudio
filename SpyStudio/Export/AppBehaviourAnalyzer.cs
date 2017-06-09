using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SpyStudio.Database;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Registry.Controls;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using SpyStudio.Trace;

namespace SpyStudio.Export
{
    public class AppBehaviourAnalyzer
    {
        #region Fields

        private List<string> _applicationRelatedWords;

        #endregion

        #region Properties

        public FileEntry ExeFile { get; protected set; }

        public string ProcessName { get; protected set; }
        public string ProgramFilesFolderPath { get; protected set; }
        public string CommonProgramFilesFolderPath { get; protected set; }
        public string ExpandedProgramFilesFolderPath { get; protected set; }
        public string ExpandedCommonProgramFilesFolderPath { get; protected set; }
        public string ProgramFilesFolderName { get; protected set; }
        public string CommonProgramFilesFolderName { get; protected set; }
        public string AppDataFolderPath { get; protected set; }
        public string LocalAppDataFolderPath { get; protected set; }
        public string ProcessFileName { get; protected set; }
        public string ProcessOriginalFileName { get; protected set; }
        public string CompanyName { get; protected set; }
        public string ProductName { get; protected set; }
        public string MainRegistryKey { get; protected set; }

        public PathNormalizer PathNormalizer;

        protected IEnumerable<string> NonRepresentativeWords = new List<string>
                                                                   {
                                                                       "corporation",
                                                                       "corp",
                                                                       "corp.",
                                                                       "incorporated",
                                                                       "inc",
                                                                       "inc.",
                                                                       "foundation",
                                                                       "llc",
                                                                       "llc.",
                                                                       "software",
                                                                       "microsoft",                                                                    
                                                                       "windows",
                                                                       "for",
                                                                       "about",
                                                                       "atop",
                                                                       "into",
                                                                       "off",
                                                                       "onto",
                                                                       "out",
                                                                       "per",
                                                                       "pro",
                                                                       "with"
                                                                   };

        public IEnumerable<string> ApplicationRelatedWords
        {
            get
            {
                if (_applicationRelatedWords == null)
                {
                    _applicationRelatedWords = new List<string>();

                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(ProcessName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(Path.GetFileNameWithoutExtension(ProcessFileName).SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(Path.GetFileNameWithoutExtension(ProcessOriginalFileName).SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(ProgramFilesFolderName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(CommonProgramFilesFolderName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(CompanyName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(ProductName.SplitInWords());

                    _applicationRelatedWords.RemoveRepetitions();
                }

                return _applicationRelatedWords.Where(w => w.Length > 2).Except(NonRepresentativeWords).ToList();
            }
        }

        #endregion

        #region Instantiation and Initialization

        public static List<AppBehaviourAnalyzer> ForMainExesOf(DeviareRunTrace aTrace, FileSystemTree aFileTree, CheckerType checkerType)
        {
            var ret = new List<AppBehaviourAnalyzer>();
            IEnumerable<FileSystemTreeNode> nodes;
            if (checkerType == CheckerType.Installer)
            {
                nodes = aFileTree.AllModelNodes.Where(x => (x.Access | FileSystemAccess.Write) == FileSystemAccess.Write && Path.GetExtension(x.FileSystemPath).ToLower() == ".exe");
            }
            else
            {
                var firstCallEvent = EventDatabaseMgr.GetInstance().GetFirstEvent(aTrace.TraceId);

                nodes =
                    aFileTree.AllModelNodes.Where(
                        n => Path.GetFileName(n.FileSystemPath).Equals(firstCallEvent.ProcessName));
            }
            ret.AddRange(nodes.Select(fileSystemTreeNode => For(fileSystemTreeNode.ToFileEntry())));
            Debug.Assert(ret.Count != 0, "Could not find main exe(s) for AppBehaviourAnalyzer.");
            return ret;
        }

        public static AppBehaviourAnalyzer For(FileEntry anExeFile)
        {
            var analyzer = new AppBehaviourAnalyzer { ExeFile = anExeFile };

            analyzer.ExtractProcessName();

            return analyzer;
        }

        private void ExtractProcessName()
        {
            ProcessName = Path.GetFileName(ExeFile.Path);
        }

        public AppBehaviourAnalyzer()
        {
            ProcessName = string.Empty;
            ProgramFilesFolderPath = string.Empty;
            ExpandedProgramFilesFolderPath = string.Empty;
            ProgramFilesFolderName = string.Empty;
            AppDataFolderPath = string.Empty;
            LocalAppDataFolderPath = string.Empty;
            ProcessOriginalFileName = string.Empty;
            CompanyName = string.Empty;
            ProductName = string.Empty;
            ProcessFileName = string.Empty;
        }

        #endregion

        #region Analysis

        public void Analyze(FileSystemTree aFileSystemTree)
        {
            ExtractProcessPropertiesFrom(aFileSystemTree);
            ExtractProgramFilesFolderPathFrom(aFileSystemTree);
            ExtractCommonProgramFilesFolderPathFrom(aFileSystemTree);
            ExtractAppDataFolderPathFrom(aFileSystemTree);
            ExtractLocalAppDataFolderPathFrom(aFileSystemTree);
        }

        private void ExtractProcessPropertiesFrom(FileSystemTree aFileSystemTree)
        {
            var mainProcessFileNode = aFileSystemTree.AllModelNodes.FirstOrDefault(n => n.FileString.ToLower().Equals(ProcessName.ToLower()));

            CallEvent[] mainProcessFileNodeEvents;
            if (mainProcessFileNode != null)
            {
                ProcessFileName = mainProcessFileNode.FileString;

                mainProcessFileNodeEvents = mainProcessFileNode.CallEventIds.FetchEvents().ToArray();
            }
            else
            {
                ProcessFileName = string.Empty;
                mainProcessFileNodeEvents = new CallEvent[0];
            }

            foreach (var callEvent in mainProcessFileNodeEvents)
            {
                var productName = FileSystemEvent.GetProduct(callEvent);
                var companyName = FileSystemEvent.GetCompany(callEvent);
                var mainProcessOriginalFileName = FileSystemEvent.GetOriginalFileName(callEvent);

                if (!string.IsNullOrEmpty(productName))
                    ProductName = productName;

                if (!string.IsNullOrEmpty(companyName))
                    CompanyName = companyName;

                if (!string.IsNullOrEmpty(mainProcessOriginalFileName))
                    ProcessOriginalFileName = mainProcessOriginalFileName;
            }
        }

        private void ExtractProgramFilesFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var allNodes = aFileSystemTree.AllModelNodes;

            var mainProcessFileNode = allNodes.FirstOrDefault(n => n.FileString.ToLower().Equals(ProcessName.ToLower()));

            if (mainProcessFileNode == null)
            {
                ProgramFilesFolderPath = "";
                ProgramFilesFolderName = "";
                return;
            }

            //var programFilesFolderRegex = new Regex(@"^%ProgramFilesDir(?:\(x64\))?%\\[^\\]+");
            var path = PathNormalizer.Unnormalize(mainProcessFileNode.FilePath);

            //if (mainProcessFileNode == null || !programFilesFolderRegex.IsMatch(PathNormalizer.Unnormalize(mainProcessFileNode.FilePath)))
            if (!path.StartsWith(SystemDirectories.ProgramFiles) && !path.StartsWith(SystemDirectories.ProgramFiles86))
            {
                ProgramFilesFolderPath = "";
                ProgramFilesFolderName = "";
                return;
            }

            //ProgramFilesFolderPath = programFilesFolderRegex.Match(mainProcessFileNode.FilePath).Value;
            ProgramFilesFolderPath = Path.GetDirectoryName(path);
            ProgramFilesFolderPath = string.Join("\\", ProgramFilesFolderPath.SplitAsPath().Take(3).ToArray());
            ProgramFilesFolderName = ProgramFilesFolderPath.TrimEnd('\\').Split('\\').Last();

            ExpandedProgramFilesFolderPath = PathNormalizer.EnsureSingleBackslashesIn(ProgramFilesFolderPath);

            if (Directory.Exists(ExpandedProgramFilesFolderPath))
                return;

            var programFilesFolderNameToReplace = ExpandedProgramFilesFolderPath.Contains("x86")
                                                      ? "Program Files"
                                                      : "Program Files (x86)";
            var programFilesRegex = new Regex(Regex.Escape(ExpandedProgramFilesFolderPath.SplitAsPath().ElementAt(1)), RegexOptions.IgnoreCase);
            
            ExpandedProgramFilesFolderPath = programFilesRegex.Replace(ExpandedProgramFilesFolderPath, programFilesFolderNameToReplace);
        }

        private void ExtractCommonProgramFilesFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var allNodes = aFileSystemTree.AllModelNodes;

            var mainProcessFileNode = allNodes.FirstOrDefault(n => n.FileString.ToLower().Equals(ProcessName.ToLower()));

            var commonProgramFilesFolderRegex = new Regex(@"^%Program Files Common%\\[^\\]+");

            if (mainProcessFileNode == null || !commonProgramFilesFolderRegex.IsMatch(mainProcessFileNode.FilePath))
            {
                CommonProgramFilesFolderPath = "";
                CommonProgramFilesFolderName = "";
                return;
            }

            CommonProgramFilesFolderPath = commonProgramFilesFolderRegex.Match(mainProcessFileNode.FilePath).Value;
            CommonProgramFilesFolderName = CommonProgramFilesFolderPath.TrimEnd('\\').SplitAsPath().Last();

            ExpandedCommonProgramFilesFolderPath = PathNormalizer.EnsureSingleBackslashesIn(PathNormalizer.Unnormalize(CommonProgramFilesFolderPath));
        }

        private Func<string, string> GenerateNormalizationFunction(FileSystemTree aFileSystemTree)
        {
            return s =>
                   GeneralizedPathNormalizer.GetInstance().Normalize(
                       aFileSystemTree.PathNormalizer.Unnormalize(s));
        }

        private void ExtractAppDataFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var f = GenerateNormalizationFunction(aFileSystemTree);

            var appDataFolderRegex = new Regex(@"^%AppData%(?:\\.*|$)");

            var candidateNode = aFileSystemTree.AllModelNodes.
                Where(n => f(n.FilePath).StartsWith(@"%AppData%")).
                FirstOrDefault(n => ApplicationRelatedWords.Any(w => n.FilePath.ToLower().Contains(w)));

            AppDataFolderPath = candidateNode == null
                                               ? ""
                                               : appDataFolderRegex.Match(f(candidateNode.FilePath)).Value;
        }

        private void ExtractLocalAppDataFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var f = GenerateNormalizationFunction(aFileSystemTree);

            var localAppDataFolderRegex = new Regex(@"^%Local AppData%(?:\\.*|$)");

            var candidateNode = aFileSystemTree.AllModelNodes.
                Where(n => f(n.FilePath).StartsWith(@"%Local AppData%")).
                FirstOrDefault(n => ApplicationRelatedWords.Any(w => n.FilePath.ToLower().Contains(w)));

            LocalAppDataFolderPath = candidateNode == null
                                                     ? ""
                                                     : localAppDataFolderRegex.Match(f(candidateNode.FilePath)).Value;
        }

        public void Analyze(RegistryTree registryTree)
        {
            ExtractMainRegistryKeyFrom(registryTree);
        }

        private void ExtractMainRegistryKeyFrom(RegistryTree registryTree)
        {
            var company = CompanyName;
            var productCandidates = new List<string>();
            productCandidates.AddRange(ApplicationRelatedWords);
            productCandidates.Add(ProductName);
            foreach (var product in productCandidates)
            {
                var path = @"HKEY_LOCAL_MACHINE\Software\" + company + @"\" + product;
                var key = RegistryTools.GetKeyFromFullPath(path);
                if (key != null)
                {
                    MainRegistryKey = path;
                    return;
                }
            }
            {
                var path = @"HKEY_LOCAL_MACHINE\Software\" + company;
                var key = RegistryTools.GetKeyFromFullPath(path);
                if (key != null)
                    MainRegistryKey = path;
            }
        }

        #endregion
    }
}