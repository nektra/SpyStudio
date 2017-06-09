using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SpyStudio.Database;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Export
{
    public class ApplicationBehaviourAnalyzer
    {
        public string MainProcessName { get; protected set; }
        public string ProgramFilesFolderPath { get; protected set; }
        public string CommonProgramFilesFolderPath { get; protected set; }
        public string ExpandedProgramFilesFolderPath { get; protected set; }
        public string ExpandedCommonProgramFilesFolderPath { get; protected set; }
        public string ProgramFilesFolderName { get; protected set; }
        public string CommonProgramFilesFolderName { get; protected set; }
        public string AppDataFolderPath { get; protected set; }
        public string LocalAppDataFolderPath { get; protected set; }
        public string MainProcessOriginalFileName { get; protected set; }
        public string CompanyName { get; protected set; }
        public string ProductName { get; protected set; }
        public string MainProcessFileName { get; protected set; }

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

        private List<string> _applicationRelatedWords;

        public ApplicationBehaviourAnalyzer()
        {
            MainProcessName = string.Empty;
            ProgramFilesFolderPath = string.Empty;
            ExpandedProgramFilesFolderPath = string.Empty;
            ProgramFilesFolderName = string.Empty;
            AppDataFolderPath = string.Empty;
            LocalAppDataFolderPath = string.Empty;
            MainProcessOriginalFileName = string.Empty;
            CompanyName = string.Empty;
            ProductName = string.Empty;
            MainProcessFileName = string.Empty;
        }

        public IEnumerable<string> ApplicationRelatedWords
        {
            get
            {
                if(_applicationRelatedWords == null)
                {
                    _applicationRelatedWords = new List<string>();

                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(MainProcessName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(Path.GetFileNameWithoutExtension(MainProcessFileName).SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(Path.GetFileNameWithoutExtension(MainProcessOriginalFileName).SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(ProgramFilesFolderName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(CommonProgramFilesFolderName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(CompanyName.SplitInWords());
                    _applicationRelatedWords.AddRangeIfNotNullOrEmpty(ProductName.SplitInWords());

                    _applicationRelatedWords.RemoveRepetitions();
                }

                return _applicationRelatedWords.Where(w => w.Length > 2).Except(NonRepresentativeWords).ToList();
            }
        }

        #region DeviareRunTrace Analysis

        public void Analyze(DeviareRunTrace aTrace)
        {
            ExtractProbableMainProcessName(aTrace);
        }

        private void ExtractProbableMainProcessName(DeviareRunTrace aTrace)
        {
            CallEvent firstCallEvent = EventDatabaseMgr.GetInstance().GetFirstEvent(aTrace.TraceId);

            if (firstCallEvent != null)
                MainProcessName = firstCallEvent.ProcessName;
        }

        #endregion

        #region FileSystemTree Analysis

        public void Analyze(FileSystemTree aFileSystemTree)
        {
            ExtractMainProcessPropertiesFrom(aFileSystemTree);
            ExtractProgramFilesFolderPathFrom(aFileSystemTree);
            ExtractCommonProgramFilesFolderPathFrom(aFileSystemTree);
            ExtractAppDataFolderPathFrom(aFileSystemTree);
            ExtractLocalAppDataFolderPathFrom(aFileSystemTree);
        }

        private void ExtractMainProcessPropertiesFrom(FileSystemTree aFileSystemTree)
        {
            var mainProcessFileNode = aFileSystemTree.AllModelNodes.FirstOrDefault(n => n.FileString.ToLower().Equals(MainProcessName.ToLower()));

            CallEvent[] mainProcessFileNodeEvents;
            if (mainProcessFileNode != null)
            {
                MainProcessFileName = mainProcessFileNode.FileString;

                mainProcessFileNodeEvents = mainProcessFileNode.CallEventIds.FetchEvents().ToArray();
            }
            else
            {
                MainProcessFileName = string.Empty;
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
                    MainProcessOriginalFileName = mainProcessOriginalFileName;
            }
        }

        private void ExtractProgramFilesFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var allNodes = aFileSystemTree.AllModelNodes;

            var mainProcessFileNode = allNodes.FirstOrDefault(n => n.FileString.ToLower().Equals(MainProcessName.ToLower()));

            if (mainProcessFileNode == null)
            {
                ProgramFilesFolderPath = "";
                ProgramFilesFolderName = "";
                return;
            }

            //var programFilesFolderRegex = new Regex(@"^%ProgramFilesDir(?:\(x64\))?%\\[^\\]+");
            var path = PathNormalizer.UndoNormalizationOf(mainProcessFileNode.FilePath);

            //if (mainProcessFileNode == null || !programFilesFolderRegex.IsMatch(PathNormalizer.UndoNormalizationOf(mainProcessFileNode.FilePath)))
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
            var programFilesRegex = new Regex(Regex.Escape(ExpandedProgramFilesFolderPath.Split('\\').ElementAt(1)), RegexOptions.IgnoreCase);
            
            ExpandedProgramFilesFolderPath = programFilesRegex.Replace(ExpandedProgramFilesFolderPath, programFilesFolderNameToReplace);
        }

        private void ExtractCommonProgramFilesFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var allNodes = aFileSystemTree.AllModelNodes;

            var mainProcessFileNode = allNodes.FirstOrDefault(n => n.FileString.ToLower().Equals(MainProcessName.ToLower()));

            var commonProgramFilesFolderRegex = new Regex(@"^%Program Files Common%\\[^\\]+");

            if (mainProcessFileNode == null || !commonProgramFilesFolderRegex.IsMatch(mainProcessFileNode.FilePath))
            {
                CommonProgramFilesFolderPath = "";
                CommonProgramFilesFolderName = "";
                return;
            }

            CommonProgramFilesFolderPath = commonProgramFilesFolderRegex.Match(mainProcessFileNode.FilePath).Value;
            CommonProgramFilesFolderName = CommonProgramFilesFolderPath.TrimEnd('\\').Split('\\').Last();

            ExpandedCommonProgramFilesFolderPath = PathNormalizer.EnsureSingleBackslashesIn(PathNormalizer.UndoNormalizationOf(CommonProgramFilesFolderPath));
        }

        private void ExtractAppDataFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var appDataFolderRegex = new Regex(@"^%AppData%\\[^\\]+");

            var candidateNode = aFileSystemTree.AllModelNodes.
                Where(n => n.FilePath.StartsWith(@"%AppData%")).
                FirstOrDefault(n => ApplicationRelatedWords.Any(w => n.FilePath.ToLower().Contains(w)));

            AppDataFolderPath = candidateNode == null
                                               ? ""
                                               : appDataFolderRegex.Match(candidateNode.FilePath).Value;
        }

        private void ExtractLocalAppDataFolderPathFrom(FileSystemTree aFileSystemTree)
        {
            var localAppDataFolderRegex = new Regex(@"^%Local AppData%\\[^\\]+");

            var candidateNode = aFileSystemTree.AllModelNodes.
                Where(n => n.FilePath.StartsWith(@"%Local AppData%")).
                FirstOrDefault(n => ApplicationRelatedWords.Any(w => n.FilePath.ToLower().Contains(w)));

            LocalAppDataFolderPath = candidateNode == null
                                                     ? ""
                                                     : localAppDataFolderRegex.Match(candidateNode.FilePath).Value;
        }

        #endregion

        #region EntryPoint Analysis

        public void Analyze(ListView anEntryPointListView)
        {
           
        }

        #endregion
    }
}