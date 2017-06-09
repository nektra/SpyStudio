using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Database;
using SpyStudio.Export;
using SpyStudio.Export.SWV;
using SpyStudio.Export.Templates;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Hooks;
using SpyStudio.Main;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class FileSelect : TemplatedVirtualizationPage
    {
        protected bool FilesDestinationNeedsUpdate
        {
            set { Wizard.SetStateFlag(WizardStateFlags.FilesDestinationNeedsUpdate, value); }
        }

        protected ExportWizard Wizard {get { return (ExportWizard) GetWizard(); }}
        protected readonly VirtualizationExport Export;

        protected ExportField<DeviareRunTrace> Trace;
        protected ExportField<IEnumerable<FileEntry>> OriginalFileEntries;
        protected ExportField<IEnumerable<FileEntry>> OriginalFiles;
        protected ExportField<IEnumerable<FileEntry>> Files;
        protected ExportField<IEnumerable<AppBehaviourAnalyzer>> AppAnalyzers;

        private List<FileSystemTreeNode> _runtimeAddedNodes;

        private List<string> _checkedRoaming;
        private List<string> _checkedLocal;
        private List<string> _checkedRuntime;
        private List<string> _uncheckedAncestorsRuntime;
        private HashSet<string> _runtimeStrings;

        // Context Menu
        private readonly ToolStripMenuItem _importItem = new ToolStripMenuItem("Import Missing Files Of This Directory");
        private readonly ToolStripSeparator _importDirItemSep = new ToolStripSeparator();
        private readonly ToolStripMenuItem _importDirItem = new ToolStripMenuItem("Import Directory From File System");
        readonly ToolStripMenuItem _importFileItem = new ToolStripMenuItem("Import File From File System");

        private string LocalAppDataPath = "%Local AppData%";
        private string RoamingAppDataPath = "%AppData%";
        private string RuntimePath = @"%SystemRoot%\winsxs";

        public enum ContextMenuItemClicked
        {
            Undefined,
            ImportMissingFilesOfThisDirectory,
            ImportDirectoryFromFileSystem,
        }

        public ContextMenuItemClicked GetContextMenuItemClicked(ToolStripItem tsi)
        {
            if (tsi == _importItem)
                return ContextMenuItemClicked.ImportMissingFilesOfThisDirectory;
            if (tsi == _importDirItem || tsi == _importFileItem)
                return ContextMenuItemClicked.ImportDirectoryFromFileSystem;
            return ContextMenuItemClicked.Undefined;
        }

        protected List<FileSystemTreeNode> ProgramFilesDirectoryUnaccessedItems { get; set; }

        public List<FileSystemTreeNode> RelatedToApplication { get; set; }

        public ThinAppIsolationOption DefaultIsolation
        {
            set { this.ExecuteInUIThreadAsynchronously(() => _defaultIsolationModeCombo.SelectedItem = value); }
        }

        public List<FileEntry> GetCheckedItems()
        {
            return filesView.GetCheckedItems();
        }

        private event Action OnCheckedItemsChanged;

        private void TriggerCheckedItemsChangedEvent()
        {
            if (OnCheckedItemsChanged != null)
                OnCheckedItemsChanged();
        }

        #region Instantiation

        public static FileSelect ForSwv(VirtualizationExport anExport, string aPageDescription)
        {
            var fileSelect = new FileSelect(anExport, aPageDescription, SwvPathNormalizer.GetInstance());

            fileSelect.ArrangeControlsForSwv();

            return fileSelect;
        }

        private void ArrangeControlsForSwv()
        {
            _customPanel.Controls.Clear();
            _customPanel.RowStyles.Clear();
            _customPanel.RowCount = 1;
            _customPanel.ColumnCount = 1;

            _customPanel.Controls.Add(filesView, 0, 0);
            filesView.Dock = DockStyle.Fill;
            _customPanel.Refresh();
        }

        public FileSelect(VirtualizationExport anExport, string aPageDescription,
                          PathNormalizer aPathNormalizer)
            : base(aPageDescription, anExport)
        {
            ProgramFilesDirectoryUnaccessedItems = new List<FileSystemTreeNode>();
            RelatedToApplication = new List<FileSystemTreeNode>();

            Export = anExport;

            LocalAppDataPath = aPathNormalizer.LocalAppDataPath;
            RoamingAppDataPath = aPathNormalizer.RoamingAppDataPath;
            RuntimePath = aPathNormalizer.RuntimePath;

            GetFieldsFrom(anExport);

            InitializeComponent();

            SetUpListView();

            filesView.Controller = this;

            // TODO: Ugly. Improve.
            filesView.HideQueryAttributes = Export is ThinAppExport;
            filesView.ShowStartupModules = true;

            filesView.PathNormalizer = aPathNormalizer;

            KeyPressed += OnKeyPressed;
            filesView.OnNodeCheckChanged += OnNodeCheckChangedHandler;

            _importItem.Click += ImportItemOnClick;
            _importDirItem.Click += ImportDirItemOnClick;
            _importFileItem.Click += ImportFileItemOnClick;

            filesView.ContextMenuStrip.Opening += ContextMenuStripOnOpening;
            filesView.ContextMenuStrip.ItemClicked += ToolStripItemClickedEventHandler;
            filesView.ContextMenuStrip.Items.Insert(0, _importDirItemSep);
            filesView.ContextMenuStrip.Items.Insert(0, _importDirItem);
            filesView.ContextMenuStrip.Items.Insert(0, _importFileItem);

            filesView.ShowIsolationOptions = anExport.ShowFileSystemIsolationOptions;

            filesView.TreeView.OnUserRequestedCheckStateChange += OnUserRequestedCheckStateChangeHandler;

            filesView.TreeView.ExpandFirstLevel = false;
        }

        private void OnUserRequestedCheckStateChangeHandler(TreeNodeAdv node, CheckState checkState)
        {
#if false
            var n = node.Node as FileSystemTreeNode;
            if (n == null)
                return;
            throw new NotImplementedException();
#endif
        }

        private FileSystemTreeNode FindNode(string path)
        {
            return filesView.TreeView.GetNodeByTreePath<FileSystemTreeNode>(path);
        }

        private void OnNodeCheckChangedHandler(Node node)
        {
            TriggerCheckedItemsChangedEvent();
        }

        private void ImportFileItemOnClick(object sender, EventArgs e)
        {
            string[] paths;
            using (var dialog = new OpenFileDialog { Multiselect = true })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                paths = dialog.FileNames;
            }
            foreach (var filePath in paths)
            {
                ImportFile(filePath);
            }
        }

        private void ImportDirItemOnClick(object sender, EventArgs eventArgs)
        {
            var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() != DialogResult.OK)
                return;
            var path = folderDialog.SelectedPath;
            ImportDirectory(path, true, true);
        }

        private void ToolStripItemClickedEventHandler(object sender, ToolStripItemClickedEventArgs e)
        {
#if false
            var clicked = (int)filesView.TreeView.GetContextMenuItemClicked(e.ClickedItem);
            if (clicked < 1 || clicked > 4)
                return;
            var state = clicked < 3 ? CheckState.Checked : CheckState.Unchecked;
            var recursive = clicked % 2 == 0;
            foreach (var node in filesView.TreeView.SelectedNodes)
            {
                var selectedNode = node as FileSystemTreeNode;
                if (selectedNode == null)
                    continue;
            }
#endif
        }

        private void ContextMenuStripOnOpening(object sender, CancelEventArgs cancelEventArgs)
        {
            var selectedNode = filesView.TreeView.SelectedNode as FileSystemTreeNode;
            if (selectedNode != null && selectedNode.IsDirectory)
            {
                filesView.ContextMenuStrip.Items.Insert(0, _importItem);
            }
            else
            {
                filesView.ContextMenuStrip.Items.Remove(_importItem);
            }
        }

        private void ImportItemOnClick(object sender, EventArgs eventArgs)
        {
            var selectedNode = filesView.TreeView.SelectedNode as FileSystemTreeNode;
            if (selectedNode != null)
            {
                //var selNodeSystemPath = filesView.PathNormalizer.Unnormalize(selectedNode.FilePath);
                var selNodeSystemPath = selectedNode.FileSystemPath;
                if (!ImportNodeDirectory(selNodeSystemPath, true))
                {
                    MessageBox.Show(this, "Cannot find path in the file system.",
                                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public bool ImportNodeDirectory(string selNodeSystemPath, bool recursive)
        {
            if (!Directory.Exists(selNodeSystemPath))
                return false;
            filesView.BeginUpdate();
            ImportDirectory(selNodeSystemPath, recursive, true);
            filesView.EndUpdate();
            return true;
        }

        private void GetFieldsFrom(VirtualizationExport anExport)
        {
            Trace = anExport.GetField<DeviareRunTrace>(ExportFieldNames.Trace);
            OriginalFileEntries = anExport.GetField<IEnumerable<FileEntry>>(ExportFieldNames.OriginalFileEntries);
            OriginalFiles = anExport.GetField<IEnumerable<FileEntry>>(ExportFieldNames.OriginalFiles);
            AppAnalyzers = Export.GetField<IEnumerable<AppBehaviourAnalyzer>>(ExportFieldNames.ApplicationBehaviourAnalizers);
            Files = Export.GetField<IEnumerable<FileEntry>>(ExportFieldNames.Files);
        }

        private void SetUpListView()
        {
            filesView.InitializeComponent();
            filesView.ArrangeForExportWizard();
            filesView.CheckBoxes = true;
            SetCustomMode(false);
        }

        #endregion

        private bool LoadFilesFromDataBase()
        {
            filesView.ClearData();

            if (Export.Canceled)
                return false;

            ProcessEvents();
            
            if (Export.Canceled)
                return false;
            return true;
        }

        protected virtual void SetDefaultIsolation(ThinAppIsolationOption option)
        {
            _defaultIsolationModeCombo.SelectedItem = option;
        }

        private void RestoreFilesSelected()
        {
            var files = VirtualizationTemplate.Value.GetFiles();
            foreach (var item in files)
            {
                if (string.IsNullOrEmpty(item.Path))
                {
                    SetDefaultIsolation(item.Isolation);
                    continue;
                }
                var fileSystemPath =
                    GeneralizedPathNormalizer.GetInstance().Unnormalize(item.Path);
                if (string.IsNullOrEmpty(fileSystemPath))
                    continue;
                var normalizer = filesView.PathNormalizer;
                var path = normalizer.Normalize(fileSystemPath);
                if (!(item.AllLeaves || item.AllBranches))
                {
                    var entry =
                        new ThinAppFileEntry(
                            FileEntry.ForPath(fileSystemPath, path),
                            item.Isolation) {IsDirectory = !item.IsLeaf};
                    var node = filesView.AddFileEntryUncolored(entry);
                    if (node == null)
                        continue;
                    Debug.Assert(item.IsChecked != null);
                    node.Checked = item.IsChecked.Value;
                }
                else
                {
                    if (item.IsLeaf)
                    {
                        if (item.AllLeaves)
                            ImportFile(fileSystemPath).IsChecked = true;
                    }
                    else
                    {
                        ImportDirectory(fileSystemPath, null, true, item.AllLeaves, item.AllBranches);
                        var node = filesView.TreeView.GetNodeByTreePath<InterpreterNode>(path);
                        if (node != null)
                            node.SetCheckStateForSelfAndDescendants(true);
                    }

                    Action<string> sanityCheck = null;
#if DEBUG
                    sanityCheck = s =>
                                      {
                                          var descendantFileSystemPath = normalizer.Unnormalize(s);
                                          Debug.Assert(!File.Exists(descendantFileSystemPath) &&
                                                       Directory.Exists(descendantFileSystemPath));
                                      };
#endif

                    if (item.UncheckedDescendants != null)
                        ChangeCheckedStateByList(item.Path, item.UncheckedDescendants, filesView.TreeView, false, sanityCheck);
                    if (item.CheckedDescendants != null)
                        ChangeCheckedStateByList(item.Path, item.CheckedDescendants, filesView.TreeView, true, sanityCheck);
                }
            }
        }

        public void LoadFiles()
        {
            filesView.ClearData();

            if (VirtualizationTemplate.Value.IsInUse)
            {
                BeginLoad();
                RestoreFilesSelected();
                EndLoad(true);
                return;
            }

            Export.FilesNeedUpdate = false;

            BeginLoad();

            if (!LoadFilesFromDataBase())
                return;

            var packageFiles = LoadFilesFromPackage();

            if (Export.CheckerType != CheckerType.None)
            {
                Analyze();

                foreach (var checker in Export.FileCheckers)
                {
                    checker.FileSelect = this;
                    filesView.ViewAccept(checker);
                }
            }

            // Package files are checked by default
            foreach (var packageFile in packageFiles)
                packageFile.CheckSelfAndParents();

            filesView.TreeView.Model.NodeCheckChanged += node => Export.FilesWereUpdated = true;

            EndLoad(false);
        }

        protected virtual IEnumerable<FileSystemTreeNode> LoadFilesFromPackage()
        {
            Debug.Assert(false, "Subclass responsibility.");

            return null;
        }

        private bool Analyze()
        {
            if (Trace.Value != null && !Trace.Value.IsEmpty())
            {
                var analyzers = AppBehaviourAnalyzer.ForMainExesOf(Trace.Value, filesView.TreeView, Export.CheckerType);
                ((List<AppBehaviourAnalyzer>)AppAnalyzers.Value).AddRange(analyzers);
            }

            foreach (var appAnalyzer in AppAnalyzers.Value)
            {
                appAnalyzer.PathNormalizer = filesView.PathNormalizer;

                if (Export.Canceled)
                    return false;

                appAnalyzer.Analyze(filesView.TreeView);

                if (Export.Canceled)
                    return false;
            }

            return true;
        }

        private bool PathExists(string path)
        {
            return filesView.TreeView.GetNode(path) != null;
        }
        public IEnumerable<FileSystemTreeNode> ImportDirectory(string directory, bool recursive, bool checkItems)
        {
            var nodes = ImportDirectory(directory, null, recursive);
            if (checkItems)
            {
                foreach (var n in nodes)
                {
                    n.CheckSelfAndParents();
                }
            }
            return nodes;
        }
        private IEnumerable<FileSystemTreeNode> ImportDirectory(string directory, bool recursive)
        {
            return ImportDirectory(directory, null, recursive);
        }

        private IEnumerable<FileSystemTreeNode> ImportDirectory(string directory, string fileFilter, bool recursive)
        {
            return ImportDirectory(directory, fileFilter, recursive, true, true);
        }

        private IEnumerable<FileSystemTreeNode> ImportDirectory(string directory, string fileFilter, bool recursive, bool includeFiles, bool includeDirectories)
        {
            var retAddedNodes = new List<FileSystemTreeNode>();

            var transformed = filesView.PathNormalizer.Normalize(PathNormalizer.EnsureSingleBackslashesIn(directory));
            if (!PathExists(transformed.FormattedForComparison()))
            {
                var newDirNode = filesView.AddFileEntry(FileEntry.ForPath(directory, transformed)) as FileSystemTreeNode;
                if (newDirNode != null)
                {
                    newDirNode.IsImported = true;
                    retAddedNodes.Add(newDirNode);
                }
            }
            else
            {
                var node = filesView.TreeView.GetNodeByTreePath<FileSystemTreeNode>(transformed);
                if (node != null)
                    node.IsImported = true;
            }

            try
            {
                if (includeFiles)
                {
                    var files = fileFilter == null
                                    ? Directory.GetFiles(directory)
                                    : Directory.GetFiles(directory, fileFilter);
                    foreach (var file in files)
                    {
                        if (Export.Canceled)
                            return retAddedNodes;

                        var node = ImportFile(file);
                        if (node != null)
                            retAddedNodes.Add(node);
                    }
                }
                if (recursive && includeDirectories)
                {
                    var subDirectories = Directory.GetDirectories(directory);
                    foreach (var subDirectory in subDirectories)
                    {
                        retAddedNodes.AddRange(ImportDirectory(subDirectory, fileFilter, true, true, true));
                    }
                }
            }
            catch (UnauthorizedAccessException){}
            catch (DirectoryNotFoundException){}
            return retAddedNodes;
        }

        private FileSystemTreeNode ImportFile(string file)
        {
            string transformed =
                filesView.PathNormalizer.Normalize(PathNormalizer.EnsureSingleBackslashesIn(file));
            if (PathExists(transformed.FormattedForComparison()))
            {
                return null;
            }

            var newFileNode = filesView.AddFileEntry(FileEntry.ForPath(file, transformed)) as FileSystemTreeNode;
            if (newFileNode != null)
                newFileNode.IsImported = true;
            return newFileNode;
        }

        private void ImportContentsOfFileSystemFolder(FileSystemTreeNode rootNode)
        {
            var rootNodeSystemPath = filesView.PathNormalizer.Unnormalize(rootNode.FilePath);
            if (Directory.Exists(rootNodeSystemPath))
            {
                filesView.BeginUpdate();
                ProgramFilesDirectoryUnaccessedItems.AddRange(ImportDirectory(rootNodeSystemPath, true));
                filesView.EndUpdate();
            }
        }

        private void ImportContentsOfFileSystemFolder(string aFolderPath)
        {
            if (string.IsNullOrEmpty(aFolderPath))
                return;
            filesView.BeginUpdate();
            ProgramFilesDirectoryUnaccessedItems.AddRange(ImportDirectory(aFolderPath, true));
            filesView.EndUpdate();

            //if (string.IsNullOrEmpty(aFolderPath))
            //    return;

            //IEnumerable<string> directories = new[] {aFolderPath};

            //while (directories.Any())
            //{
            //    var dirAccumulator = new List<string>();

            //    foreach (var directory in directories)
            //    {
            //        var subDirectories = Directory.GetDirectories(directory);

            //        ProgramFilesDirectoryUnaccessedItems.AddRange(ImportDirectoryFiles(directory));

            //        int index = 0;

            //        foreach (var subDirectory in subDirectories)
            //        {
            //            var transformed =
            //                filesView.PathNormalizer.Normalize(PathNormalizer.EnsureSingleBackslashesIn(subDirectory)).
            //                    FormattedForComparison();
            //            if (ExistPath(transformed))
            //            {
            //                index++;
            //                continue;
            //            }
            //            var newDirNode = filesView.AddFileEntry(FileEntry.ForPath(subDirectories[index], transformed, _iconCache));

            //            ProgramFilesDirectoryUnaccessedItems.Add(newDirNode);

            //            if (Export.Canceled)
            //                return;
            //            index++;
            //        }

            //        dirAccumulator.AddRange(subDirectories);
            //    }

            //    if (Export.Canceled)
            //        return;

            //    directories = dirAccumulator;
            //}
        }

        /// <summary>
        /// Add subdirectories of rootPath that match dirFilter and all their files
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="dirFilter"></param>
        private IEnumerable<FileSystemTreeNode> ImportContentsOfDirectories(string rootPath, string dirFilter)
        {
            var addedNodes = new List<FileSystemTreeNode>();
            var dirs = Directory.GetDirectories(rootPath, dirFilter);
            foreach (var subDirectory in dirs)
            {
                var transformed =
                    filesView.PathNormalizer.Normalize(PathNormalizer.EnsureSingleBackslashesIn(subDirectory));
                if (!PathExists(transformed.FormattedForComparison()))
                {
                    var newDirNode = filesView.AddFileEntry(FileEntry.ForPath(subDirectory, transformed)) as FileSystemTreeNode;
                    if (newDirNode != null)
                        addedNodes.Add(newDirNode);
                }
                addedNodes.AddRange(ImportDirectory(subDirectory, false));
            }
            return addedNodes;
        }

        private void ProcessEvents()
        {
            var refreshData = new EventsReportData(Export.TraceId)
            {
                ControlInvoker = this,
                EventsToReport = EventType.FileSystem,
                ReportBeforeEvents = false,
                EventResultsIncluded = EventsReportData.EventResult.Success
            };
            refreshData.EventsReady += RefreshDataOnEventsReady;
            EventDatabaseMgr.GetInstance().RefreshEvents(refreshData);
            EventDatabaseMgr.GetInstance().WaitProcessEvents(refreshData);
        }

        private void RefreshDataOnEventsReady(object sender, EventsRefreshArgs eventsRefreshArgs)
        {
            foreach (var e in eventsRefreshArgs.Events)
            {
                if (e.ProcessName.ToLower() == "searchindexer.exe")
                    continue;
                filesView.AddEvent(e, null);

                if (Export.Canceled)
                {
                    eventsRefreshArgs.Canceled = true;
                    break;
                }
            }
        }

        protected void BeginLoad()
        {
            checkBoxLocalAppData.CheckedChanged -= CheckBoxAppDataCheckedChanged;
            checkBoxRoamingData.CheckedChanged -= CheckBoxRoamingDataCheckedChanged;
            checkBoxAppRuntimes.CheckedChanged -= CheckBoxAppRuntimesCheckedChanged;
            checkBoxWholeProgramFiles.CheckedChanged -= CheckBoxIncludeWholeProgramFilesFolderChanged;

            filesView.BeginUpdate();
        }

        protected void EndLoad(bool simulating)
        {
            this.ExecuteInUIThreadSynchronously(() => EnableNextButton(true));

            filesView.EndUpdate();

            bool roamingAppPathIsChecked;
            var checkBoxRoamingDataEnabled =
                filesView.ExistItem(
                    filesView.PathNormalizer.Normalize(SystemDirectories.RoamingAppDataPath), out roamingAppPathIsChecked);

            bool localAppDataIsChecked;
            var checkBoxLocalAppDataEnabled =
                filesView.ExistItem(filesView.PathNormalizer.Normalize(SystemDirectories.LocalAppDataPath),
                                    out localAppDataIsChecked);

            bool appRuntimesIsChecked;
            var checkBoxAppRuntimesEnabled =
                filesView.ExistItem(filesView.PathNormalizer.Normalize(SystemDirectories.RuntimePath),
                                    out appRuntimesIsChecked);

            this.ExecuteInUIThreadSynchronously( () =>
                {
                    checkBoxRoamingData.Enabled = checkBoxRoamingDataEnabled;
                    checkBoxRoamingData.Checked = roamingAppPathIsChecked;

                    checkBoxLocalAppData.Enabled = checkBoxLocalAppDataEnabled;
                    checkBoxLocalAppData.Checked = localAppDataIsChecked;

                    checkBoxAppRuntimes.Enabled = checkBoxAppRuntimesEnabled;
                    checkBoxAppRuntimes.Checked = false;
                });

            foreach (var appAnalyzer in AppAnalyzers.Value)
            {
                bool programFilesFolderIsChecked = false;
                if (!string.IsNullOrEmpty(appAnalyzer.ProgramFilesFolderName))
                {
                    checkBoxWholeProgramFiles.Enabled =
                        filesView.ExistItem(
                            filesView.PathNormalizer.Normalize(appAnalyzer.ProgramFilesFolderPath),
                            out programFilesFolderIsChecked);

                    this.ExecuteInUIThreadSynchronously(() => checkBoxWholeProgramFiles.Checked = programFilesFolderIsChecked);
                }

                if (simulating)
                    continue;

                if (!string.IsNullOrEmpty(appAnalyzer.ExpandedProgramFilesFolderPath))
                {
                    ImportContentsOfFileSystemFolder(appAnalyzer.ExpandedProgramFilesFolderPath);
                    foreach (var relatedToAppNode in RelatedToApplication)
                    {
                        // only process those nodes that are child of any root node related to program files
                        if (relatedToAppNode.Depth == 2 &&
                            (relatedToAppNode.Parent.Text.StartsWith(filesView.PathNormalizer.ProgramFiles, true,
                                                                     CultureInfo.CurrentCulture)))
                            ImportContentsOfFileSystemFolder(relatedToAppNode);
                    }
                }

                if (!string.IsNullOrEmpty(appAnalyzer.ExpandedCommonProgramFilesFolderPath))
                {
                    ImportContentsOfFileSystemFolder(appAnalyzer.ExpandedCommonProgramFilesFolderPath);
                    foreach (var relatedToAppNode in RelatedToApplication)
                    {
                        // only process those nodes that are child of any root node related to program files
                        if (relatedToAppNode.Depth == 2 &&
                            relatedToAppNode.Parent.Text.StartsWith(filesView.PathNormalizer.ProgramFiles, true,
                                                                    CultureInfo.CurrentCulture))
                            ImportContentsOfFileSystemFolder(relatedToAppNode);
                    }
                }

                if (programFilesFolderIsChecked)
                    CheckBoxIncludeWholeProgramFilesFolderChanged();
            }

            checkBoxLocalAppData.CheckedChanged += CheckBoxAppDataCheckedChanged;
            checkBoxRoamingData.CheckedChanged += CheckBoxRoamingDataCheckedChanged;
            checkBoxAppRuntimes.CheckedChanged += CheckBoxAppRuntimesCheckedChanged;
            checkBoxWholeProgramFiles.CheckedChanged += CheckBoxIncludeWholeProgramFilesFolderChanged;

            // WORKAROUND: sometimes the cursor doesn't change by itself. Force an update.
            //Cursor = Cursors.Default;
            //Update();
        }

        private void FileSelectQueryCancel(object sender, CancelEventArgs e)
        {
            Export.Cancel();
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            // arrow up and down should select first or last item
            if (keyPressedEventArgs.KeyData == Keys.Up || keyPressedEventArgs.KeyData == Keys.Down)
            {
                if (!filesView.Focused)
                    filesView.Focus();
                if (!filesView.SelectedEntries.Any())
                {
                    if (keyPressedEventArgs.KeyData == Keys.Up)
                        filesView.SelectLastItem();
                    else
                        filesView.SelectFirstItem();
                    keyPressedEventArgs.Handled = true;
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control)
                filesView.CopySelectionToClipboard();

            if (e.KeyCode == Keys.A && e.Control)
                filesView.SelectAll();
        }

        protected virtual void FileSelectSetActive(object sender, WizardPageEventArgs e)
        {
            if (e.IsBackActionIn(Wizard))
                return;

            SetWizardButtons(WizardButtons.Back | WizardButtons.Next);
            EnableNextButton(false);
            EnableCancelButton(true);

            filesView.TreeView.Model.NodeCheckChanged += node => FilesDestinationNeedsUpdate = true;

            UseWaitCursor = true;
            this.DisableUI();

            if (Export.FilesNeedUpdate)
            {
                filesView.ClearData();
                Threading.ExecuteAsynchronously(LoadFiles, args =>
                    {
                        EnableNextButton(true);
                        this.EnableUI();
                        UseWaitCursor = false;
                    });
            }
            else
                EnableNextButton(true);                
        }

        public override void OnWizardNext(WizardPageEventArgs wizardPageEventArgs)
        {
            var checkedItems = GetCheckedItems();

            OriginalFileEntries.Value = checkedItems;
            Files.Value = checkedItems;
            OriginalFiles.Value = checkedItems.Where(x => !x.IsDirectory);

            foreach (var f in checkedItems)
            {
                f.FileSystemPath = filesView.PathNormalizer.Unnormalize(f.Path);
                if (!Export.FilesWereUpdated && f.Version.Contains("/")) // A different version of the file was selected
                    Export.FilesWereUpdated = true;
            }

            Export.EntryPointsNeedUpdate = Export.RegistryNeedUpdate = true;
            Export.RuntimesExported = _runtimeStrings == null ? new List<string>() : _runtimeStrings.ToList();

            var defaultIsolation = ThinAppIsolationOption.DefaultFileSystemIsolation;
            try
            {
                defaultIsolation = (ThinAppIsolationOption)_defaultIsolationModeCombo.SelectedItem;
            }
            catch (InvalidCastException)
            {
#if DEBUG
                throw;
#endif
            }
            catch
            {
            }
            VirtualizationTemplate.Value.SaveFilesSelected(filesView.TreeView, filesView.PathNormalizer, defaultIsolation);
            EnableNextButton(false);
        }

        private void SetCustomMode(bool custom)
        {
            if (custom)
            {
                checkBoxStandard.Checked = false;
                checkBoxCustom.Checked = true;
                if (_standardPanel.Visible)
                {
                    _standardPanel.Hide();
                }
                if (!_customPanel.Visible)
                {
                    _customPanel.Show();
                }
            }
            else
            {
                checkBoxStandard.Checked = true;
                checkBoxCustom.Checked = false;
                if (_customPanel.Visible)
                {
                    _customPanel.Hide();
                }
                if (!_standardPanel.Visible)
                {
                    _standardPanel.Show();
                }
            }
        }

        private void FlowLayoutPanelRightSizeChanged(object sender, EventArgs e)
        {
            _customPanel.Size = new Size(flowLayoutPanelRight.Size.Width - 2,
                                         flowLayoutPanelRight.Size.Height - 2);
            _standardPanel.Size = new Size(flowLayoutPanelRight.Size.Width - 2, flowLayoutPanelRight.Size.Height - 2);
        }

        private void CheckBoxStandardClick(object sender, EventArgs e)
        {
            SetCustomMode(false);
        }

        private void CheckBoxCustomClick(object sender, EventArgs e)
        {
            SetCustomMode(true);
        }

        private void CheckBoxAppDataCheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLocalAppData.Checked)
            {
                if (_checkedLocal != null)
                {
                    foreach (var path in _checkedLocal)
                    {
                        filesView.TreeView.CheckPath(path, true, false);
                    }
                }
                else
                {
                    filesView.TreeView.CheckPath(LocalAppDataPath, true, true);
                }
            }
            else
            {
                _checkedLocal = filesView.TreeView.GetCheckedPaths(LocalAppDataPath);
                filesView.TreeView.CheckPath(LocalAppDataPath, false, true);
            }
        }

        private void CheckBoxRoamingDataCheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRoamingData.Checked)
            {
                if (_checkedRoaming != null)
                {
                    foreach (var path in _checkedRoaming)
                    {
                        filesView.TreeView.CheckPath(path, true, false);
                    }
                }
                else
                {
                    filesView.TreeView.CheckPath(RoamingAppDataPath, true, true);
                }
            }
            else
            {
                _checkedRoaming = filesView.TreeView.GetCheckedPaths(RoamingAppDataPath);
                filesView.TreeView.CheckPath(RoamingAppDataPath, false, true);
            }
        }

        private void CheckBoxAppRuntimesCheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAppRuntimes.Checked)
            {
                var winsxsNode = filesView.TreeView.GetNode(RuntimePath);

                if (winsxsNode != null)
                {
                    filesView.BeginUpdate();

                    _checkedRuntime = filesView.TreeView.GetCheckedPaths(RuntimePath);
                    _uncheckedAncestorsRuntime = filesView.TreeView.GetUncheckedPathsOfAncestors(RuntimePath);
                    filesView.TreeView.CheckAncestorsPath(RuntimePath);
                    filesView.TreeView.CheckPath(RuntimePath, true, true);

                    if (_runtimeAddedNodes == null)
                    {
                        _runtimeAddedNodes = new List<FileSystemTreeNode>();
                        _runtimeStrings = null;

                        var winsxsfileSystemPath = filesView.PathNormalizer.Unnormalize(winsxsNode.FilePath);
                        var originalWinsxsNodes = winsxsNode.Nodes.ToList();
                        foreach (var runtime in originalWinsxsNodes)
                        {
                            int index = 0;
                            for (int i = 0; i < 3; i++)
                            {
                                index = runtime.Text.IndexOf("_", index + 1, StringComparison.InvariantCulture);
                                if (index == -1)
                                    break;
                            }
                            if (index != -1)
                            {
                                var runtimeString = runtime.Text.Substring(0, index);
                                if (_runtimeStrings == null)
                                    _runtimeStrings = new HashSet<string>();
                                _runtimeStrings.Add(runtimeString);
                                _runtimeAddedNodes.AddRange(ImportContentsOfDirectories(winsxsfileSystemPath,
                                                                                        runtimeString + "*"));
                            }
                        }

                        if (_runtimeStrings != null)
                        {
                            var manifestPath = winsxsfileSystemPath + "\\Manifests";

                            var transformedManifestPath = winsxsNode.FilePath + "\\Manifests";
                            if (!PathExists(transformedManifestPath))
                            {
                                var newDirNode = filesView.AddFileEntry(FileEntry.ForPath(manifestPath, transformedManifestPath)) as FileSystemTreeNode;
                                if (newDirNode != null)
                                    _runtimeAddedNodes.Add(newDirNode);
                            }

                            foreach (var runtimeString in _runtimeStrings)
                            {
                                _runtimeAddedNodes.AddRange(ImportDirectory(manifestPath,
                                                                              runtimeString + "*", false));
                            }
                        }
                    }
                    foreach (var item in _runtimeAddedNodes)
                    {
                        item.Checked = true;
                    }

                    filesView.EndUpdate();
                }
            }
            else
            {
                filesView.TreeView.CheckPath(RuntimePath, false, true);
                if (_checkedRuntime != null)
                {
                    foreach (var path in _checkedRuntime)
                    {
                        filesView.TreeView.CheckPath(path, true, false);
                    }
                    foreach (var path in _uncheckedAncestorsRuntime)
                    {
                        filesView.TreeView.CheckPath(path, false, false);
                    }
                }
                _runtimeStrings = null;
                foreach (var item in _runtimeAddedNodes)
                {
                    item.Checked = false;
                }
            }
        }

        private void CheckBoxIncludeWholeProgramFilesFolderChanged(object sender, EventArgs e)
        {
            CheckBoxIncludeWholeProgramFilesFolderChanged();
        }

        private void CheckBoxIncludeWholeProgramFilesFolderChanged()
        {
            if (checkBoxWholeProgramFiles.Checked)
            {
                foreach (var item in ProgramFilesDirectoryUnaccessedItems)
                    item.Checked = true;
            }
            else
            {
                foreach (var item in ProgramFilesDirectoryUnaccessedItems)
                    item.Checked = false;
            }
        }

        #region Implementation of IInterpreterController

        public void ShowInCom(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public void ShowInWindows(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public void ShowInFiles(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public void ShowInRegistry(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public bool ShowQueryAttributesInFiles { get; private set; }
        public bool ShowDirectoriesInFiles { get; private set; }
        public bool PropertiesGoToVisible { get { return false; } }

        public bool PropertiesVisible
        {
            get { return true; }
        }

        protected void HideIsolationSelectionRow()
        {
            _defaultIsolationModeCombo.Visible = false;
            label1.Visible = false;
            _customPanel.RowStyles[0].Height = 0;
        }

        #endregion

    }
}