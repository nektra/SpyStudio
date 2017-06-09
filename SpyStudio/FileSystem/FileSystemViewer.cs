using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Aga.Controls;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.Extensions;

namespace SpyStudio.FileSystem
{
    public class FileSystemViewer : Control, ITreeInterpreter
    {
        protected readonly object DataLock = new object();
        protected readonly HashSet<string> AddedCallers = new HashSet<string>();
        
        protected bool CompareModeActivated;
        protected bool Initialized;
        protected IFileSystemViewerControl ViewControl;
        protected TableLayoutPanel TableLayoutPanel;
        protected EntryContextMenu EntryProperties;

        #region Properties

        public bool MergeLayerPaths { get; set; }
        public bool MergeWowPaths { get; set; }
        public bool HideQueryAttributes { get; set; }
        public bool ShowStartupModules { get; set; }
        public Color File1BackgroundColor { get; set; }
        public Color File2BackgroundColor { get; set; }
        public uint File1TraceId { get; set; }
        public uint File2TraceId { get; set; }

        private PathNormalizer _pathNormalizer = new NullPathNormalizer();
        public PathNormalizer PathNormalizer
        {
            get { return _pathNormalizer; }
            set
            {
                _pathNormalizer = value ?? new NullPathNormalizer();
                if (ViewControl != null)
                {
                    ViewControl.PathNormalizer = _pathNormalizer;
                }
            }
        }

        private bool _showIsolationOptions;
        public bool ShowIsolationOptions
        {
            get { return _showIsolationOptions; }
            set
            {
                _showIsolationOptions = value;
                if (TreeMode && ViewControl != null)
                {
                    TreeView.ShowIsolationOptions = value;
                }
            }
        }

        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        public Control ParentControl
        {
            get { return this; }
        }

        protected bool TreeModeActivated;
        public bool TreeMode
        {
            get { return TreeModeActivated; }
            set
            {
                if (TreeModeActivated != value)
                {
                    TreeModeActivated = value;
                    if (Initialized)
                        InitializeComponent();
                }
            }
        }

        protected bool CheckBoxesActivated;
        public bool CheckBoxes
        {
            set
            {
                if (CheckBoxesActivated != value)
                {
                    CheckBoxesActivated = value;
                    if (ViewControl != null)
                        ViewControl.CheckBoxes = value;
                }
            }
        }

        public override bool Focused
        {
            get
            {
                var control = ViewControl as Control;
                if (control != null)
                    return control.Focused;
                return false;
            }
        }

        public bool CompareMode
        {
            set
            {
                if (CompareModeActivated != value || ViewControl.CompareMode != value)
                {
                    CompareModeActivated = value;
                    if (ViewControl != null)
                        ViewControl.CompareMode = value;
                    if (CompareModeActivated)
                    {
                        HideQueryAttributes = true;
                        ShowStartupModules = false;
                    }
                }
            }
        }

        public FileSystemTree TreeView
        {
            get { return ViewControl as FileSystemTree; }
        }

        public FileSystemList ListView
        {
            get { return ViewControl as FileSystemList; }
        }

        #endregion

        #region Events

        public event Action<Node> OnNodeCheckChanged;

        private void TriggerOnNodeCheckChangedEvent(Node node)
        {
            if (OnNodeCheckChanged != null)
                OnNodeCheckChanged(node);
        }

        #endregion

        #region DirMergeSwv

        private static readonly List<KeyValuePair<string, string>> DirMergeSwv = new List<KeyValuePair<string, string>>
                                                                                     {
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[a-z|0-9]*\\\[_B_\]ALLUSERSPROFILE\[_E_\]",
                                                                                             @"$`:\Users\All Users$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[a-z|0-9]*\\\[_B_\]PROGRAMFILES\[_E_\]",
                                                                                             @"$`:\Program Files$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[a-z|0-9]*\\\[_B_\]PROGRAMFILES64\[_E_\]",
                                                                                             @"$`:\Program Files$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[a-z|0-9]*\\\[_B_\]PROFILESDIRECTORY\[_E_\]",
                                                                                             @"$`:\Users$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]SYSTEM32\[_E_\]",
                                                                                             @"$`:\Windows\System32$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]SYSTEM64\[_E_\]",
                                                                                             @"$`:\Windows\System32$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]WINDIR\[_E_\]",
                                                                                             @"$`:\Windows$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]SYSTEMDRIVE\[_E_\]",
                                                                                             @"$`:\$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]MSSHAREDTOOLS\[_E_\]",
                                                                                             @"$`:\Program Files\Common Files\Microsoft Shared$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]COMMONFILES\[_E_\]",
                                                                                             @"$`:\Program Files\Common Files$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]COMMONFILES\[_E_\]",
                                                                                             @"$`:\Program Files\Common Files$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]COMMONFILES\[_E_\]",
                                                                                             @"$`:\Program Files\Common Files$'"),
                                                                                         //new KeyValuePair<string, string>(@":\\fslrdr\\[1-9|a-z]*\\S-[1-9|\-]*", @"$`:\Users\CurrentUser$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\S-[\d\-]*",
                                                                                             @"$`:\Users\CurrentUser$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\program files\\internet explorer [1-9]*",
                                                                                             @"$`:\Program Files\Internet Explorer$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\fslrdr\\[1-9|a-z]*\\\[_B_\]APPDATA\[_E_\]\\LocalLow",
                                                                                             @"$`:\Program Files\Common Files$'"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\users\\\\[a-z|0-9]+\\\\appdata\\\\locallow", "[_B_]APPDATA[_E_]\\\\LocalLow"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\users\\\\[a-z|0-9]+\\\\appdata\\\\local", "[_B_]LOCALAPPDATA[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\users\\\\[a-z|0-9]+\\\\Favorites", "[_B_]FAVORITES[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\users\\\\[a-z|0-9]+\\\\appdata\\\\Roaming", "[_B_]APPDATA[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\users\\\\[a-z|0-9]+\\\\appdata", "[_B_]APPDATA[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\Users\\\\[a-z|0-9]+\\\\Desktop", "[_B_]DESKTOP[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\users\\\\[a-z|0-9]+", "[_B_]DEFAULTUSERPROFILE[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\Documents and Settings\\\\[a-z|0-9]+\\\\Desktop", "[_B_]DESKTOP[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\Documents and Settings\\\\[a-z|0-9]+\\\\Favorites", "[_B_]FAVORITES[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\Documents and Settings\\\\[a-z|0-9]+\\\\Application Data", "[_B_]APPDATA[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\Documents and Settings\\\\[a-z|0-9]+\\\\Local Settings\\\\Application Data", "[_B_]LOCALAPPDATA[_E_]"),
                                                                                         //new KeyValuePair<string, string> (".:\\\\Documents and Settings\\\\[a-z|0-9]+\\\\Local Settings", "[_B_]LOCALSETTINGS[_E_]")
                                                                                     };

        private static readonly List<KeyValuePair<string, string>> DirMergeWow = new List<KeyValuePair<string, string>>
                                                                                     {
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @"\\Program Files \(x86\)",
                                                                                             @"$`\Program Files$'"),
                                                                                         new KeyValuePair
                                                                                             <string, string>(
                                                                                             @":\\Windows\\SysWow64",
                                                                                             @"$`:\Windows\System32$'"),
                                                                                     };

        #endregion

        #region Instantiation

        public FileSystemViewer()
        {
            Initialize(false);
        }

        private void Initialize(bool showIsolationOptions)
        {
            MergeLayerPaths = false;

            TreeMode = true;
            ShowStartupModules = true;
            ShowIsolationOptions = showIsolationOptions;

            InitializeComponent();
        }

        protected override void OnCreateControl()
        {
            if (DesignMode)
                InitializeComponent();
            base.OnCreateControl();
        }

        public void InitializeComponent()
        {
            if (Initialized)
            {
                TableLayoutPanel.Controls.Remove(ViewControl.Control);
                var tree = ViewControl as FileSystemTree;
                if (tree != null)
                {
                    tree.OnNodeCheckChanged -= TriggerOnNodeCheckChangedEvent;
                    tree.Dispose();
                }

                EntryProperties.Close(true);
            }

            if (TreeMode)
            {
                var control = new FileSystemTree(ShowIsolationOptions);
                control.Padding = control.Margin = new Padding(0);
                control.SizeChanged += (sender, args) => AutoSizeColumns();
                control.OnNodeCheckChanged += TriggerOnNodeCheckChangedEvent;
                control.Controller = Controller;
                ViewControl = control;
                EntryProperties = new TreeEntryContextMenu(this);
            }
            else
            {
                var control = new FileSystemList();
                control.Padding = control.Margin = new Padding(0);
                control.Controller = Controller;
                ViewControl = control;
                EntryProperties = new EntryContextMenu(this);
            }

            ViewControl.CheckBoxes = CheckBoxesActivated;
            if (!Initialized)
            {
                Initialized = true;
                TableLayoutPanel = new TableLayoutPanel();
                TableLayoutPanel.Margin = new Padding(0);
                TableLayoutPanel.Padding = new Padding(0);

                Controls.Add(TableLayoutPanel);

                TableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
                TableLayoutPanel.Location = new Point(0, 0);
                TableLayoutPanel.Name = "tableLayoutPanel1";
                TableLayoutPanel.Size = new Size(200, 100);
                TableLayoutPanel.TabIndex = 0;

                TableLayoutPanel.ColumnCount = 1;
                TableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                TableLayoutPanel.Controls.Add(ViewControl.Control, 0, 0);
                ViewControl.Control.Dock = DockStyle.Fill;
                TableLayoutPanel.Dock = DockStyle.Fill;
                TableLayoutPanel.Location = new Point(0, 0);
                TableLayoutPanel.Margin = new Padding(0);
                TableLayoutPanel.Name = "tableLayoutPanel1";
                TableLayoutPanel.RowCount = 1;
                TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                TableLayoutPanel.Size = new Size(666, 293);
                TableLayoutPanel.TabIndex = 1;

            }
            else
            {
                TableLayoutPanel.Controls.Add(ViewControl.Control, 0, 0);
                ViewControl.Control.Dock = DockStyle.Fill;
            }

            ViewControl.PathNormalizer = _pathNormalizer;
            ViewControl.CompareMode = CompareModeActivated;
            EntryProperties.AttachInputEvents(ViewControl.Control);
        }
        #endregion

        #region Item helpers

        public static string GetTooltip(IFileSystemViewerItem item)
        {
            var i = 1;
            var tooltip = item.FilePath;
            if (!string.IsNullOrEmpty(item.Company))
                tooltip += "\nCompany: " + item.Company;
            if (!string.IsNullOrEmpty(item.Version))
                tooltip += "\nVersion: " + item.Version;
            if (!string.IsNullOrEmpty(item.Description))
                tooltip += "\nDescription: " + item.Description;
            foreach (var mod in item.CallerModules)
            {
                if (i > Settings.Default.MaxTooltipModules)
                    break;
                if (!string.IsNullOrEmpty(mod))
                {
                    if (i++ == 1)
                        tooltip += "\nCalled from: ";
                    else
                        tooltip += " ";
                    tooltip += ModulePath.ExtractModuleName(mod);
                }
            }
            return tooltip;
        }

        public static string GetAccessString(IFileSystemViewerFileDetailItem item)
        {
            if(item.CompareMode)
            {
                if (item.Count1 > 0 && item.Count2 > 0)
                {
                    var access1 = FileSystemTools.GetAccessString(item.Access1, false);
                    var access2 = FileSystemTools.GetAccessString(item.Access2, false);

                    if (access1 != access2)
                        return (access1.ForCompareString() + " / " + access2.ForCompareString());
                    return access1;
                }
                if (item.Count1 > 0)
                {
                    return FileSystemTools.GetAccessString(item.Access1, false);
                }
                return FileSystemTools.GetAccessString(item.Access2, false);
            }
            if (item.Access != FileSystemAccess.None)
            {
                return FileSystemTools.GetAccessString(item.Access, false);
            }
            return string.Empty;
        }

        public static string GetCountString(IFileSystemViewerFileDetailItem item)
        {
            if (item.CompareMode)
            {
                if (item.Count1 > 0 && item.Count2 > 0)
                {
                    return string.Format("{0} / {1}", item.Count1.ToString(CultureInfo.InvariantCulture),
                                         item.Count2.ToString(CultureInfo.InvariantCulture));
                }
                if (item.Count1 > 0)
                {
                    return item.Count1.ToString(CultureInfo.InvariantCulture);
                }
                return item.Count2.ToString(CultureInfo.InvariantCulture);
            }
            return item.Count.ToString(CultureInfo.InvariantCulture);
        }

        public static string GetTimeString(IFileSystemViewerFileDetailItem item)
        {
            if (item.CompareMode)
            {
                if (item.Count1 > 0 && item.Count2 > 0)
                {
                    return string.Format("{0:N2} / {1:N2}", item.Time1, item.Time2);
                }
                if (item.Count1 > 0)
                {
                    return string.Format("{0:N2}", item.Time1);
                }
                return string.Format("{0:N2}", item.Time2);
            }
            return string.Format("{0:N2}", item.Time);
        }

        #endregion Item helpers

        #region Selection

        public void CopySelectionToClipboard()
        {
            ViewControl.CopySelectionToClipboard();
        }

        public void SelectAll()
        {
            ViewControl.SelectAll();
        }

        public void ExpandAllErrors(IEntry entry)
        {
            if(TreeMode)
            {
                var fileSystemTree = (FileSystemTree)ViewControl;
                fileSystemTree.ExpandAllErrors(entry);
            }
        }

        public void SelectFirstItem()
        {
            ViewControl.SelectFirstItem();
        }

        public void SelectLastItem()
        {
            ViewControl.SelectLastItem();
        }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            ViewControl.SelectNextVisibleEntry(anEntry);
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            ViewControl.SelectPreviousVisibleEntry(anEntry);
        }

        public EntryContextMenu ContextMenuController { get { return EntryProperties; } }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return ViewControl.SelectedEntries; }
        }

        #endregion

        #region Entry addition
        
        public IFileSystemViewerItem AddFileEntry(FileEntry aFileEntry)
        {
            return ViewControl.AddFileEntry(aFileEntry);
        }

        public IFileSystemViewerItem AddFileEntryUncolored(FileEntry aFileEntry)
        {
            return ViewControl.AddFileEntryUncolored(aFileEntry);
        }

        public void AddEvent(CallEvent e, DeviareTraceCompareItem item)
        {
            this.ExecuteInUIThreadAsynchronously(() => ProcessEvent(e, item));
        }

        private bool ShouldProcessEvent(CallEvent originalEvent)
        {
            if (originalEvent.Type == HookType.QueryDirectoryFile && originalEvent.ParamCount >= 5 && !originalEvent.IsProcMon)
                return false;

            if (FileSystemEvent.IsQueryAttributes(originalEvent) && HideQueryAttributes ||
                     (!ShowStartupModules && originalEvent.IsGenerated))
                return false;

            if (FileSystemEvent.ModuleNotFound(originalEvent))
                return false;

            return true;
        }

#if DEBUG
        private static double _timeNormalize,
                       _timeReplacements,
                       _timeIcon,
                       _timeProcess;

        private static int _countTotal, _countProcessed;
#endif

        [Conditional("DEBUG")]
        public static void InitTimes()
        {
#if DEBUG
            _timeNormalize = _timeReplacements =
                             _timeIcon =
                             _timeProcess = 0;
            _countTotal = _countProcessed = 0;
#endif
        }
        [Conditional("DEBUG")]
        public static void DumpTimes()
        {
#if DEBUG
            Debug.WriteLine("\nTotal Normalize: " + _timeNormalize +
                            "\nTime Replacements: " + _timeReplacements +
                            "\nTotal Icon: " + _timeIcon +
                            "\nTotal Process: " + _timeProcess +
                            "\nCount Processed: " + _countProcessed +
                            "\nCount Total: " + _countTotal);
#endif

        }

        public void ProcessEvent(CallEvent originalEvent, DeviareTraceCompareItem item)
        {
#if DEBUG
            _countTotal++;
            var sw = new Stopwatch();
            sw.Start();
#endif
            if (!ShouldProcessEvent(originalEvent))
                return;

#if DEBUG
            _countProcessed++;
            var previous = sw.Elapsed.TotalMilliseconds;
#endif

            CallEvent modifiedCallEvent;
            string fileSystemPath;
            string eventPath;

            if (!PathNormalizer.IncludeEvent(originalEvent, out modifiedCallEvent, out fileSystemPath))
                    return;
            eventPath = modifiedCallEvent.Params[0].Value;

            //else
            //{
            //    modifiedCallEvent = originalEvent;
            //    fileSystemPath = modifiedCallEvent.ParamCount > 0 ? modifiedCallEvent.Params[0].Value : string.Empty;
            //    eventPath = GetNormalizedPath(fileSystemPath);
            //}

#if DEBUG
            _timeNormalize += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            // these files aren't useful
            if (eventPath.EndsWith(@"\.") || eventPath.EndsWith(@"\..") ||
                eventPath.Length == 2 && eventPath.EndsWith(":"))
                return;

            eventPath = ProcessReplacements(eventPath);
#if DEBUG
            _timeReplacements += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            var icon = modifiedCallEvent.GetIcon();
#if DEBUG
            _timeIcon += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            ViewControl.ProcessEvent(eventPath, fileSystemPath, modifiedCallEvent, icon, item);

#if DEBUG
            _timeProcess += sw.Elapsed.TotalMilliseconds - previous;
#endif
        }

        public void BeginUpdate()
        {
            this.ExecuteInUIThreadAsynchronously(ViewControl.BeginUpdate);
        }

        public void EndUpdate()
        {
            this.ExecuteInUIThreadAsynchronously(ViewControl.EndUpdate);
        }

        #endregion

        #region Entry removal

        public void ClearData()
        {
            this.ExecuteInUIThreadAsynchronously(() =>
                {
                    if(EntryProperties != null)
                        EntryProperties.Close(false);

                    lock (DataLock)
                    {
                        AddedCallers.Clear();
                        if (ViewControl != null)
                            ViewControl.Clear();
                    } 
                });
        }

        #endregion

        #region Entry retrieval

        public List<FileEntry> GetAccessedFiles()
        {
            return ViewControl.GetAccessedFiles();
        }

        public List<FileEntry> GetCheckedItems()
        {
            return ViewControl.GetCheckedItems();
        }

        #endregion

        #region Testing

        public bool ExistItem(string path, out bool isChecked)
        {
            return ViewControl.ExistItem(path, out isChecked);
        }

        #endregion

        public void ArrangeForExportWizard()
        {
            if (TreeView != null)
                TreeView.ArrangeForExportWizard();

            if (ListView != null)
                ListView.ArrangeForExportWizard();
        }

        public void AttachTo(DeviareRunTrace aTrace)
        {
            aTrace.CreateDirectoryAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.LoadLibraryAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.FindResourceAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.OpenFileAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.CreateProcessAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.QueryDirectoryFileAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.QueryAttributesFileAdd += (sender, args) => AddEvent(args.Event, null);
            aTrace.UpdateBegin += (sender, args) => this.ExecuteInUIThreadAsynchronously(ViewControl.BeginUpdate);
            aTrace.UpdateEnd += (sender, args) => this.ExecuteInUIThreadAsynchronously(ViewControl.EndUpdate);
            aTrace.FileSystemClear += ClearData;
        }

        private void AutoSizeColumns()
        {
            var fileSystemTree = (FileSystemTree)ViewControl;

            var columns = fileSystemTree.Columns;

            var fixedColumnsWidth = columns.Sum(c => c.Width) - columns.Last().Width;

            columns.Last().Width = fileSystemTree.ClientSize.Width - fixedColumnsWidth;
        }

        public new void Focus()
        {
            var control = ViewControl as Control;
            if (control != null)
                control.Focus();
        }
        
        private string ProcessReplacements(string keyPath)
        {
            var ret = keyPath;
            if (MergeLayerPaths)
            {
                bool success;
                do
                {
                    success = false;
                    var i = 0;
                    foreach (var dir in DirMergeSwv)
                    {
                        var match = Regex.Match(ret, dir.Key, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            // users session
                            if (i == 12)
                            {
                                ret = match.Result(dir.Value);
                                ret = ret.Replace("[_B_]", "");
                                ret = ret.Replace("[_E_]", "");
                            }
                            else
                            {
                                ret = match.Result(dir.Value);
                            }
                            success = true;
                            break;
                        }
                        i++;
                    }
                } while (success);
            }
            if (MergeWowPaths)
            {
                bool success;
                do
                {
                    success = false;
                    foreach (var dir in DirMergeWow)
                    {
                        var match = Regex.Match(ret, dir.Key, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            ret = match.Result(dir.Value);
                            success = true;
                            break;
                        }
                    }
                } while (success);
            }
            return ret;
        }

        private string GetNormalizedPath(string path)
        {
            if (path.EndsWith(":favicon"))
            {
                return path.Substring(0, path.Length - 8);
            }
            return path;
        }

        public void FindEvent(FindEventArgs e)
        {
            ViewControl.Find(e);
        }

        public IInterpreterController Controller
        {
            get { return _controller; }
            set
            {
                ViewControl.Controller = value;
                _controller = value;
            }
        }

        public new ContextMenuStrip ContextMenuStrip
        {
            get { return ViewControl.ContextMenuStrip; }
        }

        #region Visitors

        public void ViewAccept(FileSystemTreeChecker aFileChecker)
        {
            ViewControl.Accept(aFileChecker);
        }

        #endregion

        #region Dispose

        private IInterpreterController _controller;

        //protected override void Dispose(bool disposing)
        //{
        //    base.Dispose(disposing);
        //}

        #endregion

        public void SelectItemContaining(CallEventId eventId)
        {
            ViewControl.SelectItemContaining(eventId);
        }

        public void ExpandAll()
        {
            if (!TreeMode) 
                return;

            var fileSystemTree = (FileSystemTree)ViewControl;
            fileSystemTree.ExpandAll();
        }
    }
}