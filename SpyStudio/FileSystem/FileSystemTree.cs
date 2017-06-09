using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Aga.Controls;
using Aga.Controls.Tools;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Tools;

namespace SpyStudio.FileSystem
{
    public class FileSystemTree : TreeViewAdv, IFileSystemViewerControl
    {
        private class ToolTipProvider : IToolTipProvider
        {
            public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
            {
                var item = node.Node as IFileSystemViewerItem;
                if (item != null)
                {
                    return FileSystemViewer.GetTooltip(item);
                }
                return null;
            }
        }

        private readonly Dictionary<string, FileSystemTreeNode> _fileInfos =
            new Dictionary<string, FileSystemTreeNode>();

        private readonly Dictionary<string, FileSystemTreeNode> _dirInfos = new Dictionary<string, FileSystemTreeNode>();
        private ContextMenuStrip _contextMenuStrip;

        private SortedTreeModel _model;
        private bool _checkBoxes;
        private FileSystemViewer _viewer;

        private TreeColumn _columnHeaderFilename;
        private TreeColumn _columnHeaderAccess;
        private TreeColumn _columnHeaderIsolation;
        private TreeColumn _columnHeaderFileResult;
        private TreeColumn _columnHeaderFileCount;
        private TreeColumn _columnHeaderFileTime;
        private TreeColumn _columnHeaderFileCountCompare;
        private TreeColumn _columnHeaderFileTimeCompare;
        //private readonly TreeColumn _columnHeaderCheckBox;
        private TreeColumn _columnHeaderVersion;
        private TreeColumn _columnHeaderCompany;
        private TreeColumn _columnHeaderDescription;

        private NodeTextBox _columnHeaderFilenameNode;
        private NodeStateIcon _columnHeaderFilenameIconNode;
        private NodeTextBox _columnHeaderAccessNode;
        private NodeComboBox _columnHeaderIsolationNode;
        private NodeTextBox _columnHeaderFileResultNode;
        private NodeTextBox _columnHeaderFileCountNode;
        private NodeTextBox _columnHeaderFileTimeNode;
        private NodeTextBox _columnHeaderFileCountCompareNode;
        private NodeTextBox _columnHeaderFileTimeCompareNode;
        private NodeTextBox _columnHeaderVersionNode;
        private NodeTextBox _columnHeaderCompanyNode;
        private NodeTextBox _columnHeaderDescriptionNode;
        private NodeCheckBox _columnHeaderCheckBoxNode;

        private readonly TreeNodeAdvTools.FileSystemTreeItemSorter _comparer =
            new TreeNodeAdvTools.FileSystemTreeItemSorter(SortOrder.Ascending);

        private bool _compareMode;
        private bool _inCheck;

        private readonly ToolStripMenuItem _checkItem = new ToolStripMenuItem("Check");
        private readonly ToolStripMenuItem _checkWithChildrenItem = new ToolStripMenuItem("Check recursive");
        private readonly ToolStripMenuItem _uncheckItem = new ToolStripMenuItem("Uncheck");
        private readonly ToolStripMenuItem _uncheckWithChildrenItem = new ToolStripMenuItem("Uncheck recursive");
        private readonly ToolStripMenuItem _sortByVersionItem = new ToolStripMenuItem("Sort by version");
        private readonly ToolStripSeparator _sortByVersionSeparatorItem = new ToolStripSeparator();

        public enum ContextMenuItemClicked
        {
            Undefined        = 0,
            Check            = 1,
            CheckRecursive   = 2,
            Uncheck          = 3,
            UncheckRecursive = 4,
            SortByVersion    = 5,
        }

        public PathNormalizer PathNormalizer { get; set; }

        public ContextMenuItemClicked GetContextMenuItemClicked(ToolStripItem tsi)
        {
            if (tsi == _checkItem)
                return ContextMenuItemClicked.Check;
            if (tsi == _checkWithChildrenItem)
                return ContextMenuItemClicked.CheckRecursive;
            if (tsi == _uncheckItem)
                return ContextMenuItemClicked.Uncheck;
            if (tsi == _uncheckWithChildrenItem)
                return ContextMenuItemClicked.UncheckRecursive;
            if (tsi == _sortByVersionItem)
                return ContextMenuItemClicked.SortByVersion;
            return ContextMenuItemClicked.Undefined;
        }

        public FileSystemTree(bool showIsolationOptions)
        {
            ShowIsolationOptions = showIsolationOptions;
            Initialize();
        }

        public FileSystemTree()
        {
            ShowIsolationOptions = false;
            Initialize();
        }

        private void Initialize()
        {
            DefaultToolTipProvider = null;
            DragDropMarkColor = Color.Black;
            FullRowSelect = true;
            LineColor = SystemColors.ControlDark;
            SelectedNode = null;
            SelectionMode = TreeSelectionMode.Multi;
            ShowLines = false;
            ShowNodeToolTips = true;
            UseColumns = true;
            LoadOnDemand = true;
            BorderStyle = BorderStyle.FixedSingle;
            ExpandFirstLevel = true;

            //VisibleChanged += TreeVisibleChanged;

            _columnHeaderFilename = new TreeColumn();
            _columnHeaderAccess = new TreeColumn();
            _columnHeaderIsolation = new TreeColumn();
            _columnHeaderFileResult = new TreeColumn();
            _columnHeaderFileCount = new TreeColumn();
            _columnHeaderFileTime = new TreeColumn();
            _columnHeaderFileCountCompare = new TreeColumn();
            _columnHeaderFileTimeCompare = new TreeColumn();
            _columnHeaderVersion = new TreeColumn();
            _columnHeaderCompany = new TreeColumn();
            _columnHeaderDescription = new TreeColumn();

            _columnHeaderFilenameNode = new NodeTextBox
                                            {
                                                Trimming = StringTrimming.EllipsisCharacter,
                                                IncrementalSearchEnabled = true,
                                                LeftMargin = 3,
                                                ParentColumn = _columnHeaderFilename,
                                                DataPropertyName = "FileString",
                                                DisplayHiddenContentInToolTip = true,
                                                ToolTipProvider = new ToolTipProvider()
                                            };
            ShowNodeToolTips = true;
            _columnHeaderFilenameIconNode = new NodeStateIcon
                                                {
                                                    DataPropertyName = "Icon",
                                                    DataPropertyNameExpanded = "IconExpanded",
                                                    LeftMargin = 1,
                                                    ParentColumn = _columnHeaderFilename
                                                };


            _columnHeaderAccessNode = new NodeTextBox
                                          {
                                              Trimming = StringTrimming.EllipsisCharacter,
                                              LeftMargin = 3,
                                              ParentColumn = _columnHeaderAccess,
                                              DataPropertyName = "AccessString",
                                              DisplayHiddenContentInToolTip = true
                                          };
            _columnHeaderIsolationNode = new NodeComboBox
                                             {
                                                 Trimming = StringTrimming.EllipsisCharacter,
                                                 IncrementalSearchEnabled = true,
                                                 LeftMargin = 3,
                                                 ParentColumn = _columnHeaderIsolation,
                                                 DataPropertyName = "Isolation",
                                                 DisplayHiddenContentInToolTip = true,
                                                 DropDownItems =
                                                     new List<object>
                                                         {
                                                             ThinAppIsolationOption.Inherit,
                                                             ThinAppIsolationOption.Merged,
                                                             ThinAppIsolationOption.Full,
                                                             ThinAppIsolationOption.WriteCopy
                                                         },
                                                 EditEnabled = true,
                                                 EditOnClick = true
                                             };


            _columnHeaderFileResultNode = new NodeTextBox
                                              {
                                                  Trimming = StringTrimming.EllipsisCharacter,
                                                  LeftMargin = 3,
                                                  ParentColumn = _columnHeaderFileResult,
                                                  DataPropertyName = "Result",
                                                  DisplayHiddenContentInToolTip = true
                                              };

            _columnHeaderFileCountNode = new NodeTextBox
                                             {
                                                 Trimming = StringTrimming.EllipsisCharacter,
                                                 LeftMargin = 3,
                                                 ParentColumn = _columnHeaderFileCount,
                                                 DataPropertyName = "CountString",
                                                 DisplayHiddenContentInToolTip = true
                                             };

            _columnHeaderFileTimeNode = new NodeTextBox
                                            {
                                                Trimming = StringTrimming.EllipsisCharacter,
                                                LeftMargin = 3,
                                                ParentColumn = _columnHeaderFileTime,
                                                DataPropertyName = "TimeString",
                                                DisplayHiddenContentInToolTip = true,
                                                TextAlign = HorizontalAlignment.Right
                                            };

            _columnHeaderFileCountCompareNode = new NodeTextBox
                                                    {
                                                        Trimming = StringTrimming.EllipsisCharacter,
                                                        LeftMargin = 3,
                                                        ParentColumn = _columnHeaderFileCountCompare,
                                                        DataPropertyName = "CountString",
                                                        DisplayHiddenContentInToolTip = true,
                                                        TextAlign = HorizontalAlignment.Right
                                                    };

            _columnHeaderFileTimeCompareNode = new NodeTextBox
                                                   {
                                                       Trimming = StringTrimming.EllipsisCharacter,
                                                       LeftMargin = 3,
                                                       ParentColumn = _columnHeaderFileTimeCompare,
                                                       DataPropertyName = "TimeString",
                                                       DisplayHiddenContentInToolTip = true,
                                                       TextAlign = HorizontalAlignment.Right
                                                   };
            _columnHeaderCheckBoxNode = new NodeCheckBox
                                            {
                                                //ParentColumn = _columnHeaderCheckBox,
                                                ParentColumn = _columnHeaderFilename,
                                                DataPropertyName = "CheckState",
                                                EditEnabled = true
                                            };
            _columnHeaderVersionNode = new NodeTextBox
                                           {
                                               Trimming = StringTrimming.EllipsisCharacter,
                                               LeftMargin = 3,
                                               ParentColumn = _columnHeaderVersion,
                                               DataPropertyName = "Version",
                                               DisplayHiddenContentInToolTip = true
                                           };
            _columnHeaderCompanyNode = new NodeTextBox
                                           {
                                               Trimming = StringTrimming.EllipsisCharacter,
                                               LeftMargin = 3,
                                               ParentColumn = _columnHeaderCompany,
                                               DataPropertyName = "Company",
                                               DisplayHiddenContentInToolTip = true
                                           };
            _columnHeaderDescriptionNode = new NodeTextBox
                                               {
                                                   Trimming = StringTrimming.EllipsisCharacter,
                                                   LeftMargin = 3,
                                                   ParentColumn = _columnHeaderDescription,
                                                   DataPropertyName = "Description",
                                                   DisplayHiddenContentInToolTip = true
                                               };

            _columnHeaderCheckBoxNode.CheckStateChanged += FileTreeAfterCheck;

            // 
            // columnHeaderFilename
            // 
            _columnHeaderFilename.Header = "Filename";
            _columnHeaderFilename.Width = 200;
            // 
            // columnHeaderAccess
            // 
            _columnHeaderAccess.Header = "Access";
            _columnHeaderAccess.Width = 150;

            _columnHeaderIsolation.Header = "Isolation";
            _columnHeaderIsolation.Width = 150;
            // 
            // columnHeaderFileResult
            // 
            _columnHeaderFileResult.Header = "Result";
            _columnHeaderFileResult.Width = 80;
            // 
            // columnHeaderFileCount
            // 
            _columnHeaderFileCount.Header = "Count";
            _columnHeaderFileCount.Width = 60;
            _columnHeaderFileCount.TextAlign = HorizontalAlignment.Right;

            // 
            // columnHeaderFileTime
            // 
            _columnHeaderFileTime.Header = "Time";
            _columnHeaderFileTime.Width = 60;
            _columnHeaderFileTime.TextAlign = HorizontalAlignment.Right;

            // 
            // columnHeaderFileCountCompare
            // 
            _columnHeaderFileCountCompare.Header = "Count";
            _columnHeaderFileCountCompare.Width = 100;
            _columnHeaderFileCountCompare.TextAlign = HorizontalAlignment.Right;

            // 
            // columnHeaderFileTimeCompare
            // 
            _columnHeaderFileTimeCompare.Header = "Time";
            _columnHeaderFileTimeCompare.Width = 100;
            _columnHeaderFileTimeCompare.TextAlign = HorizontalAlignment.Right;

            _columnHeaderVersion.Header = "Version";
            _columnHeaderVersion.Width = 100;
            _columnHeaderCompany.Header = "Company";
            _columnHeaderCompany.Width = 150;
            _columnHeaderDescription.Header = "Description";
            _columnHeaderDescription.Width = 200;

            Columns.Add(_columnHeaderFilename);
            Columns.Add(_columnHeaderAccess);
            if (ShowIsolationOptions)
                Columns.Add(_columnHeaderIsolation);
            Columns.Add(_columnHeaderFileResult);
            Columns.Add(_columnHeaderFileCount);
            Columns.Add(_columnHeaderFileTime);
            Columns.Add(_columnHeaderVersion);
            Columns.Add(_columnHeaderCompany);
            Columns.Add(_columnHeaderDescription);

            NodeControls.Add(_columnHeaderFilenameIconNode);
            NodeControls.Add(_columnHeaderFilenameNode);
            NodeControls.Add(_columnHeaderAccessNode);
            if (ShowIsolationOptions)
                NodeControls.Add(_columnHeaderIsolationNode);
            NodeControls.Add(_columnHeaderFileResultNode);
            NodeControls.Add(_columnHeaderFileCountNode);
            NodeControls.Add(_columnHeaderFileTimeNode);
            NodeControls.Add(_columnHeaderVersionNode);
            NodeControls.Add(_columnHeaderCompanyNode);
            NodeControls.Add(_columnHeaderDescriptionNode);

            Model = _model = new SortedTreeModel(_comparer);

            _model.NodeCheckChanged += TriggerNodeCheckChangedEvent;
            _model.NodeRemoved += OnNodeRemoved;


            CreateContextMenu();
        }

        public NodeComboBox.SelectIndexChangedHandler NodeIsolationChanged
        {
            set { _columnHeaderIsolationNode.SelectedIndexChanged = value; }
        }

        public event Action<Node> OnNodeCheckChanged;

        private void TriggerNodeCheckChangedEvent(Node node)
        {
            if (OnNodeCheckChanged != null)
                OnNodeCheckChanged(node);
        }

        private bool _showIsolationOptions;

        public bool ShowIsolationOptions
        {
            get { return _showIsolationOptions; }
            set { 
                if(_showIsolationOptions != value)
                {
                    _showIsolationOptions = value;
                    if (_columnHeaderFileResult != null)
                    {
                        if (_showIsolationOptions)
                        {
                            var index = Columns.IndexOf(_columnHeaderFileResult);
                            Debug.Assert(index != -1);

                            Columns.Insert(index, _columnHeaderIsolation);
                            index = NodeControls.IndexOf(_columnHeaderFileResultNode);
                            Debug.Assert(index != -1);

                            NodeControls.Insert(index, _columnHeaderIsolationNode);
                        }
                        else
                        {
                            Columns.Remove(_columnHeaderIsolation);
                            NodeControls.Remove(_columnHeaderIsolationNode);
                        }
                    }
                }
            }
        }

        private void CreateContextMenu()
        {
            if (_contextMenuStrip != null) return;
            _contextMenuStrip = new ContextMenuStrip();
            ContextMenuStrip = _contextMenuStrip;
            _contextMenuStrip.Opening += ContextMenuOpening;

            _checkItem.Click += ContextMenuCheckItem;
            _checkWithChildrenItem.Click += ContextMenuCheckItemChildren;
            _uncheckItem.Click += ContextMenuUncheckItem;
            _uncheckWithChildrenItem.Click += ContextMenuUncheckItemChildren;
            _sortByVersionItem.Click += ContextMenuSortByVersion;
            _sortByVersionItem.Checked = false;
            _sortByVersionItem.CheckOnClick = true;

            UpdateContextMenu();
        }

        private void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            if (CheckBoxes)
            {
                bool anyChecked = false, anyUnchecked = false;
                foreach (var node in SelectedNodes)
                {
                    if (node.IsChecked)
                        anyChecked = true;
                    else
                        anyUnchecked = true;
                }
                _checkItem.Visible = anyUnchecked;
                _uncheckItem.Visible = anyChecked;
            }
        }

        private void UpdateContextMenu()
        {
            if (ContextMenuStrip != null)
            {
                if (CheckBoxes && !ContextMenuStrip.Items.Contains(_checkItem))
                {
                    ContextMenuStrip.Items.Insert(0, _uncheckWithChildrenItem);
                    ContextMenuStrip.Items.Insert(0, _checkWithChildrenItem);
                    ContextMenuStrip.Items.Insert(0, _uncheckItem);
                    ContextMenuStrip.Items.Insert(0, _checkItem);
                    if (!ContextMenuStrip.Items.Contains(_sortByVersionSeparatorItem))
                    {
                        ContextMenuStrip.Items.Insert(4, _sortByVersionSeparatorItem);
                    }
                }
                if (!ContextMenuStrip.Items.Contains(_sortByVersionItem))
                {
                    ContextMenuStrip.Items.Add(_sortByVersionItem);
                }
            }
        }

        public void ContextMenuCheckItem(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, true, false);
            CheckedChanged = true;
            EndUpdate();
        }

        public bool ExistItem(string path, out bool isChecked)
        {
            var item = GetNode(path);
            if(item != null)
            {
                isChecked = item.IsChecked;
                return true;
            }
            isChecked = false;
            return false;
        }
        public void CheckPath(string path, bool isChecked, bool recursive)
        {
            var item = GetNode(path);
            if (item != null)
            {
                TreeNodeAdvTools.CheckNode(item, isChecked, recursive);
            }
        }
        public void CheckAncestorsPath(string path)
        {
            var node = GetNode(path);
            if (node != null)
            {
                node = node.Parent as FileSystemTreeNode;
                while(node != null)
                {
                    TreeNodeAdvTools.CheckNode(node, true, false);
                    node = node.Parent as FileSystemTreeNode;
                }
            }
        }

        protected void ExpandAllErrors(Node node)
        {
            var fsNode = node as FileSystemTreeNode;
            if (fsNode != null && !fsNode.Success)
                EnsureExpanded(fsNode);

            foreach (var n in node.Nodes)
            {
                ExpandAllErrors(n);
            }
        }

        public void ExpandAllErrors(IEntry entry)
        {
            Node node = entry == null ? _model.Root : entry as Node;

            if (node != null)
            {
                BeginUpdate();
                ExpandAllErrors(node);
                EndUpdate();
            }
        }

        public IEnumerable<IFileSystemViewerItem> GetAllItems()
        {
            return AllModelNodes.Cast<IFileSystemViewerItem>();
        }

        public IFileSystemViewerItem AddFileEntry(FileEntry aFileEntry)
        {
            return AddFileEntry(aFileEntry, true);
        }

        public IFileSystemViewerItem AddFileEntryUncolored(FileEntry aFileEntry)
        {
            return AddFileEntry(aFileEntry, false);
        }

        public void ArrangeForExportWizard()
        {
            Columns.Clear();
            NodeControls.Clear();

            Columns.Add(_columnHeaderFilename);
            Columns.Add(_columnHeaderVersion);
            Columns.Add(_columnHeaderCompany);
            Columns.Add(_columnHeaderDescription);

            NodeControls.Add(_columnHeaderFilenameIconNode);
            NodeControls.Add(_columnHeaderFilenameNode);
            NodeControls.Add(_columnHeaderVersionNode);
            NodeControls.Add(_columnHeaderCompanyNode);
            NodeControls.Add(_columnHeaderDescriptionNode);

            Refresh();
        }

        public IFileSystemViewerItem AddFileEntry(FileEntry aFileEntry, bool colored)
        {
            FileSystemTreeNode node;

            if (aFileEntry.IsDirectory)
            {
                node = CreateDirectoryNode(aFileEntry.Path, aFileEntry.Success);
            }
            else
            {
                var existingNode = GetNode(aFileEntry.Path);

                if (existingNode != null)
                {
                    existingNode.MergeVersions(aFileEntry);
                    return existingNode;
                }

                node = CreateFileNode(
                Path.GetFileName(aFileEntry.FileSystemPath),
                aFileEntry.Path,
                aFileEntry.FileSystemPath,
                aFileEntry.Icon,
                aFileEntry.Success,
                "",
                aFileEntry.Company,
                aFileEntry.Version,
                aFileEntry.Description,
                aFileEntry.Product,
                aFileEntry.OriginalFileName
                );
            }

            if (colored)
                node.ForeColor = EntryColors.NonCaptured;
            node.NotifyUpdate();

            return node;
        }

        public void ContextMenuCheckItemChildren(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, true, true);
            CheckedChanged = true;
            EndUpdate();
        }

        public void ContextMenuUncheckItem(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, false, false);
            CheckedChanged = true;
            EndUpdate();
        }

        public void ContextMenuUncheckItemChildren(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, false, true);
            CheckedChanged = true;
            EndUpdate();
        }

        public void ContextMenuSortByVersion(object sender, EventArgs e)
        {
            _comparer.SortByVersion = _sortByVersionItem.Checked;
            // force OnStructureChanged event
            (Model as SortedTreeModel).Comparer = _comparer;
        }

        public void InitializeComponent()
        {
        }

        private void FileTreeAfterCheck(object sender, TreeModelNodeEventArgs e)
        {
            if (_inCheck) return;
            _inCheck = true;
            CheckedChanged = true;
            var n = e.Node;

            if (n.IsChecked)
                CheckNode(n, n.IsChecked, true);

            _inCheck = false;
        }

        public void CheckNode(Node n, bool checkState, bool recursive)
        {
            if (n.IsChecked != checkState)
            {
                n.CheckState = CheckState.Checked;
                CheckedChanged = true;
            }
            if (recursive)
            {
                foreach (var c in n.Nodes)
                {
                    CheckNode(c, checkState, true);
                }
            }
        }

        public FileSystemViewer Viewer
        {
            get
            {
                var ret = _viewer;
                if (ret == null)
                {
                    ret = Parent as FileSystemViewer;
                    var current = Parent;
                    while (ret == null && current != null)
                    {
                        ret = current.Parent as FileSystemViewer;
                        current = current.Parent;
                    }
                    _viewer = ret;
                }

                return ret;
            }
        }

        public bool CheckBoxes
        {
            get { return _checkBoxes; }
            set
            {
                if (_checkBoxes != value)
                {
                    _checkBoxes = value;
                    if (_checkBoxes)
                    {
                        //Columns.Insert(0, _columnHeaderCheckBox);
                        NodeControls.Insert(0, _columnHeaderCheckBoxNode);
                    }
                    else
                    {
                        //Columns.RemoveAt(0);
                        NodeControls.RemoveAt(0);
                    }
                    UpdateContextMenu();
                }
            }
        }

        #region IFileSystemViewerControl

        //private long _totalFirstPart = 0, _totalCreate = 0, _totalAddCallEventToNodeAndParents = 0, _totalEventInfo = 0, _totalNotifyUpdate = 0;
        //private int _totalEvents = 0;

        public void ProcessEvent(string eventPath, string fileSystemPath, CallEvent callEvent, Image icon,
                                 DeviareTraceCompareItem item1)
        {
            ////var sw = new Stopwatch();
            ////sw.Start();
            //var lastElapsed = sw.ElapsedMilliseconds;
            FileSystemTreeNode item = null;
            var pathLower = eventPath.ToLower();
            var key = pathLower + callEvent.Success;

            _fileInfos.TryGetValue(key, out item);

            if (item == null)
                if (_dirInfos.TryGetValue(pathLower, out item))
                    item.Access = item.Access | FileSystemTools.GetEventAccess(callEvent);

            //_totalFirstPart += sw.ElapsedMilliseconds - lastElapsed;

            if (item == null)
            {
                //lastElapsed = sw.ElapsedMilliseconds;
                if (FileSystemEvent.IsDirectory(callEvent))
                {
                    item = CreateDirectoryNode(eventPath, callEvent.Success);
                    item.Icon = FileSystemTools.GetFolderClosedIcon();
                    item.IconExpanded = FileSystemTools.GetFolderOpenedIcon();
                    item.IsDirectory = true;
                }
                else
                {
                    item = CreateFileNode(eventPath, fileSystemPath, callEvent, icon);
                    _fileInfos[key] = item;
                }
                //_totalCreate += sw.ElapsedMilliseconds - lastElapsed;
            }

            //lastElapsed = sw.ElapsedMilliseconds;

            AddCallEventToNodeAndParents(callEvent, item);

            //_totalAddCallEventToNodeAndParents += sw.ElapsedMilliseconds - lastElapsed;
            
            //lastElapsed = sw.ElapsedMilliseconds;

            var access = FileSystemTools.GetEventAccess(callEvent);

            if (CompareMode)
            {
                // skip auto-generated (CallStack processing)
                if (!callEvent.IsGenerated)
                {
                    if (Viewer.File1TraceId == callEvent.TraceId)
                    {
                        item.AddCall1(callEvent.Time);
                        item.AddAccess1(access);
                    }
                    else if (Viewer.File2TraceId == callEvent.TraceId)
                    {
                        item.AddCall2(callEvent.Time);
                        item.AddAccess2(access);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                // skip 0 time calls because are auto-generated (CallStack processing)
                if (!callEvent.IsGenerated)
                {
                    item.AddCall(callEvent);
                }

                item.CallerModules.Add(callEvent.CallModule);
                item.Pids.Add(callEvent.Pid);
                item.AddAccess(access);

                if (FileSystemEvent.IsFileInfoSet(callEvent) && (string.IsNullOrEmpty(item.Version) || item.Version == "0.0.0.0"))
                {
                    item.Company = FileSystemEvent.GetCompany(callEvent);
                    item.Version = FileSystemEvent.GetVersion(callEvent);
                    item.Description = FileSystemEvent.GetDescription(callEvent);
                }

                if (string.IsNullOrEmpty(item.Version))
                    item.Version = string.Empty;
            }

            //_totalEventInfo += sw.ElapsedMilliseconds - lastElapsed;

            //lastElapsed = sw.ElapsedMilliseconds;
            item.NotifyUpdate();
            //_totalNotifyUpdate += sw.ElapsedMilliseconds - lastElapsed;
            //if ((++_totalEvents % 1000) == 0)
            //{
            //    _totalEvents = 0;
                //Error.WriteLine("_totalFirstPart: " + _totalFirstPart + " _totalCreate: " + _totalCreate +
                //                " _totalAddCallEventToNodeAndParents: " + _totalAddCallEventToNodeAndParents +
                //                " _totalEventInfo: " + _totalEventInfo + " _totalNotifyUpdate: " +
                //                _totalNotifyUpdate);
            //}
        }

        private static void AddCallEventToNodeAndParents(CallEvent callEvent, FileSystemTreeNode item)
        {
            item.CallEventIds.Add(callEvent.EventId);

            var parentItem = item.Parent;
            while (!TreeViewAdvTools.IsRoot(parentItem))
            {
                ((FileSystemTreeNode) parentItem).CallEventIds.Add(callEvent.EventId);
                parentItem = parentItem.Parent;
            }
        }

        private FileSystemTreeNode CreateFileNode(string eventPath, string fileSystemPath, CallEvent callEvent, Image icon)
        {
            var filepart = FileSystemTools.GetFileName(eventPath);

            var node = new FileSystemTreeNode(filepart, eventPath, fileSystemPath, callEvent.Success, this) { Result = callEvent.Result, ForeColor = !callEvent.Success ? Color.Red : Color.Black };

            if (FileSystemEvent.IsFileInfoSet(callEvent))
            {
                node.Company = FileSystemEvent.GetCompany(callEvent);
                node.Version = FileSystemEvent.GetVersion(callEvent);
                node.Description = FileSystemEvent.GetDescription(callEvent);
                node.Product = FileSystemEvent.GetProduct(callEvent);
                node.OriginalFileName = FileSystemEvent.GetOriginalFileName(callEvent);
            }

            node.Icon = icon;

            var parent = GetContainerDirectory(eventPath, node.Success) ?? _model.Root;

            parent.Nodes.Add(node);

            if (node.Depth == 1 && ExpandFirstLevel && !node.Parent.IsExpanded)
            {
                EnsureExpanded(node.Parent);
            }

            return node;
        }

        private FileSystemTreeNode CreateFileNode(string aFileName, string aPath, string aFileSystemPath, Image anIcon, bool success, string aResult, string aCompany, string aVersion, string aDescription, string aProduct, string anOriginalFileName)
        {
            var item = new FileSystemTreeNode(aFileName, aPath, aFileSystemPath, success, this)
                           {
                               Result = aResult,
                               ForeColor = success ? Color.Black : Color.Red,
                               Company = aCompany,
                               Version = aVersion,
                               Description = aDescription,
                               Product = aProduct,
                               OriginalFileName = anOriginalFileName,
                               Icon = anIcon
                           };
            
            var parent = GetContainerDirectory(aPath, item.Success);

            var nodes = parent != null ? parent.Nodes : _model.Nodes;

            

            //if (parent != null)
            //    nodes = parent.Nodes;

            //if (parent == null)
            //    _model.Nodes.Add(item);

            //Debug.Assert(nodes != null);
            
            nodes.Add(item);

            _fileInfos[aPath.ToLower() + success] = item;

            return item;
        }

        public bool CompareMode
        {
            get { return _compareMode; }
            set
            {
                if (_compareMode != value)
                {
                    _compareMode = value;
                    if (_compareMode)
                    {
                        Columns.RemoveAt(4);
                        Columns.RemoveAt(3);
                        NodeControls.RemoveAt(5);
                        NodeControls.RemoveAt(4);
                        Columns.Add(_columnHeaderFileCountCompare);
                        Columns.Add(_columnHeaderFileTimeCompare);
                        NodeControls.Add(_columnHeaderFileCountCompareNode);
                        NodeControls.Add(_columnHeaderFileTimeCompareNode);
                    }
                    else
                    {
                        Columns.RemoveAt(4);
                        Columns.RemoveAt(3);
                        NodeControls.RemoveAt(5);
                        NodeControls.RemoveAt(4);
                        Columns.Add(_columnHeaderFileCount);
                        Columns.Add(_columnHeaderFileTime);
                        NodeControls.Add(_columnHeaderFileCountNode);
                        NodeControls.Add(_columnHeaderFileTimeNode);
                    }
                }
            }
        }

        public bool CheckedChanged { get; set; }

        public bool IsEmpty()
        {
            return (_model.Nodes.Count == 0);
        }

        public List<FileEntry> GetAccessedFiles()
        {
            var files = new List<FileEntry>();

            foreach (var item in _fileInfos)
            {
                var fileEntry = item.Value.ToFileEntry();
                files.AddRange(fileEntry.GetEntries());
            }

            return files;
        }

        #region Entry checking

        private List<FileEntry> GetCheckedThings(FileSystemTreeNode rootNode, bool files, bool directories)
        {
            var ret = new List<FileEntry>();
            if (rootNode.CheckState == CheckState.Checked &&
                    (!rootNode.IsDirectoryOrBranch && files || rootNode.IsDirectoryOrBranch && directories))
            {
                var fileEntry = rootNode.ToThinAppFileEntry();
                ret.AddRange(fileEntry.GetEntries());
            }
            foreach(var node in rootNode.Nodes)
            {
                var treeNode = node as FileSystemTreeNode;
                if (treeNode != null)
                    ret.AddRange(GetCheckedThings(treeNode, files, directories));
            }
            return ret;
        }

        public List<FileEntry> GetCheckedThings(string rootPath, bool files, bool directories)
        {
            var ret = new List<FileEntry>();
            if (!CheckBoxes)
                return ret;

            if(string.IsNullOrEmpty(rootPath))
            {
                foreach(var node in Model.Root.Nodes)
                {
                    var fsNode = node as FileSystemTreeNode;
                    if (fsNode != null)
                        ret.AddRange(GetCheckedThings(fsNode, files, directories));
                }
            }
            else
            {
                var rootNode = GetNode(rootPath);
                if (rootNode != null)
                    return GetCheckedThings(rootNode, files, directories);
            }
            return ret;

            //rootPath = rootPath.ToLower();
            //foreach (var node in AllModelNodes)
            //{
            //    if (node.FilePath.ToLower().StartsWith(rootPath) &&
            //            node.CheckState == CheckState.Checked &&
            //            (!node.IsDirectoryOrBranch && files || node.IsDirectoryOrBranch && directories))
            //    {
            //        var fileEntry = node.ToThinAppFileEntry();
            //        ret.AddRange(fileEntry.GetEntries());
            //    }
            //}
            //return ret;
        }

        public List<string> GetUncheckedPathsOfAncestors(string path)
        {
            var entries = new List<FileEntry>();
            var node = GetNode(path);
            while(node != null && node.Parent != null)
            {
                node = node.Parent as FileSystemTreeNode;
                if(node != null && !node.Checked)
                {
                    var fileEntry = node.ToThinAppFileEntry();
                    entries.AddRange(fileEntry.GetEntries());
                }
            }

            return entries.Select(entry => entry.Path).ToList();
        }

        public List<FileEntry> GetCheckedFiles()
        {
            return GetCheckedThings(string.Empty, true, false);
        }

        public List<FileEntry> GetCheckedItems()
        {
            return GetCheckedThings(string.Empty, true, true);
        }

        public List<string> GetCheckedPaths(string rootPath)
        {
            var entries = GetCheckedThings(rootPath, true, true);
            return entries.Select(entry => entry.Path).Distinct().ToList();
        }

        public List<string> GetCheckedAncestors(string rootPath)
        {
            var entries = GetCheckedThings(rootPath, true, true);
            return entries.Select(entry => entry.Path).Distinct().ToList();
        }

        #endregion

        public new bool Find(FindEventArgs findEvent)
        {
            return base.Find(findEvent);
        }

        public override bool IsMatch(string stringToMatch, Node aNode, FindEventArgs findArgs)
        {
            if (aNode == null)
                return false;

            var node = (FileSystemTreeNode) aNode;

            if (findArgs.MatchCase)
            {
                if (findArgs.MatchWhole)
                {
                    var wholeString = @"\b" + stringToMatch + @"\b";

                    if (Regex.IsMatch(node.AccessString, wholeString) ||
                        Regex.IsMatch(node.Company, wholeString) ||
                        Regex.IsMatch(node.Description, wholeString) ||
                        Regex.IsMatch(node.FileString, wholeString) ||
                        Regex.IsMatch(node.TimeString, wholeString) ||
                        Regex.IsMatch(node.Version, wholeString) ||
                        Regex.IsMatch(node.CountString, wholeString))
                    {
                        return true;
                        //SelectedNode = curViewNode;
                        //resultFound = true;
                        //break;
                    }
                }
                else
                {
                    if (node.AccessString.Contains(stringToMatch) ||
                        node.Company.Contains(stringToMatch) ||
                        node.Description.Contains(stringToMatch) ||
                        node.FileString.Contains(stringToMatch) ||
                        node.TimeString.Contains(stringToMatch) ||
                        node.Version.Contains(stringToMatch) ||
                        node.CountString.Contains(stringToMatch)
                        )
                    {
                        return true;
                        //SelectedNode = curViewNode;
                        //resultFound = true;
                        //break;
                    }
                }
            }
            else
            {
                if (findArgs.MatchWhole)
                {
                    var wholeString = @"\b" + stringToMatch + @"\b";

                    if (Regex.IsMatch(node.AccessString, wholeString, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(node.Company, wholeString, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(node.Description, wholeString, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(node.FileString, wholeString, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(node.TimeString, wholeString, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(node.Version, wholeString, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(node.CountString, wholeString, RegexOptions.IgnoreCase))
                    {
                        return true;
                        //SelectedNode = curViewNode;
                        //resultFound = true;
                        //break;
                    }
                }
                else
                {
                    if (node.AccessString.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        node.Company.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        node.Description.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        node.FileString.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        node.TimeString.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        node.Version.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        node.CountString.IndexOf(stringToMatch, StringComparison.OrdinalIgnoreCase) >= 0
                        )
                    {
                        return true;
                        //SelectedNode = curViewNode;
                        //resultFound = true;
                        //break;
                    }
                }
            }

            return false;
        }

        public void CopySelectionToClipboard()
        {
            var tempStr = new StringBuilder("");

            foreach (var n in SelectedNodes)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = (FileSystemTreeNode) n;

                CopyNode(node, tempStr);
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
        }

        public void SelectAll()
        {
            //TreeNodeAdvTools.SelectAllNodes(this);
        }

        public void SelectFirstItem()
        {
            if (Model.Root.Nodes.Count > 0)
            {
                Model.Root.Nodes[0].IsSelected = true;
            }
        }

        public void SelectLastItem()
        {
            if (Model.Root.Nodes.Count > 0)
                Model.Root.Nodes[Model.Root.Nodes.Count - 1].IsSelected = true;
        }

        public void FindEvent(FindEventArgs findEvent)
        {
            Find(findEvent);
        }

        public IInterpreterController Controller { get; set; }

        public EntryContextMenu ContextMenuController { get; set; }

        public IEnumerable<IEntry> SelectedEntries
        {
            get
            {
                return
                    SelectedNodes.Select(
                        node => (IEntry) (FileSystemTreeNode) node);
            }
        }

        public Control ParentControl { get { return Parent; } }
        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }

        public Control Control
        {
            get { return this; }
        }

        public new IEnumerable<FileSystemTreeNode> AllModelNodes
        {
            get
            {
                return _model.Nodes.SelectMany(n => GetCompleteBranchUnder(n)).Cast<FileSystemTreeNode>();
            }
        }

        private IEnumerable<Node> GetCompleteBranchUnder(Node aNode)
        {
            var completeBranch = new List<Node> { aNode };

            foreach (var node in aNode.Nodes)
                completeBranch.AddRange(GetCompleteBranchUnder(node));

            return completeBranch;
        }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            var n = anEntry as Node;
            if (n == null)
                return;
            TreeNodeAdvTools.SelectNextNode(n, this);
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            var n = anEntry as Node;
            if (n == null)
                return;
            TreeNodeAdvTools.SelectPreviousNode(n, this);
        }

        public void Accept(FileSystemTreeChecker aFileChecker)
        {
            aFileChecker.PerformCheckingOn(this);
        }

        #endregion IFileSystemViewerControl

        public void Clear()
        {
            ClearData();
        }

        public void ClearData()
        {
            _dirInfos.Clear();
            _fileInfos.Clear();
            BeginUpdate();
            _model.Nodes.Clear();
            EndUpdate();
        }
        void IFileSystemViewerControl.BeginUpdate()
        {
            BeginUpdate();
            //_sortedModel.BeginUpdate();
        }

        void IFileSystemViewerControl.EndUpdate()
        {
            //_sortedModel.EndUpdate();
            EndUpdate();
        }

        private FileSystemTreeNode GetContainerDirectory(string path, bool success)
        {
            // remove the file entry to keep only the directory with an ending '\'
            var index = path.LastIndexOf('\\');
            if (index != -1)
            {
                index++;
                path = path.Substring(0, index);
            }
            if(index != -1)
            {
                return CreateDirectoryNode(path, success);
            }
            return null;
        }

        private FileSystemTreeNode CreateDirectoryNode(string aPath, bool success)
        {
            //var normalizedPath = PathNormalizer.EnsureSingleBackslashesIn(aPath);
            var normalizedPath = aPath;

            FileSystemTreeNode node = null;
            var nodeParent = _model.Root;

            var foldersInPath = new List<string>();
            if (normalizedPath.StartsWith("\\"))
                foldersInPath.Add("\\");
            foldersInPath.AddRange(normalizedPath.SplitAsPath().ToList());

            var level = 0;
            foreach (var folderName in foldersInPath)
            {
                var nodeName = folderName;

                var foldersInFolderPath = foldersInPath.Take(level + 1);
                var folderPath = foldersInFolderPath.Aggregate("", (pathAccum, folder) => pathAccum += folder + "\\").TrimEnd('\\');

                if (folderPath.IsDriveLetterPath())
                {
                    nodeName += "\\";
                    folderPath += "\\";
                }

                if (!_dirInfos.TryGetValue(folderPath.ToLower(), out node))
                {
                    node = new FileSystemTreeNode(nodeName, folderPath, PathNormalizer.Unnormalize(folderPath), success, this) { IsDirectory = true, Version = "0.0.0.0"};

                    _dirInfos[folderPath.ToLower()] = node;

                    nodeParent.Nodes.Add(node);

                    var removedItem = RemovePathFromFileInfos(folderPath);

                    if (removedItem != null && removedItem.Success)
                        node.Access |= removedItem.Access;
                    
                    node.Icon = FileSystemTools.GetFolderClosedIcon();
                    node.IconExpanded = FileSystemTools.GetFolderOpenedIcon();

                    if(level == 1 && ExpandFirstLevel && !node.Parent.IsExpanded)
                    {
                        EnsureExpanded(node.Parent);
                    }
                }
                if (!node.Success && success)
                {
                    node.Success = true;
                    node.ForeColor = Color.Black;
                    node.NotifyUpdate();
                }

                nodeParent = node;
                level++;
            }

            if (node == null)
            {
                node = CreateDirectoryNode("INVALID", success);
            }
            return node;
        }

        public bool ExpandFirstLevel { get; set; }

        private void OnNodeRemoved(Node parent, int index, Node node)
        {
            var queue = new Queue<Node>();
            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                var _this = queue.Dequeue();
                var children = _this.Nodes;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        queue.Enqueue(child);
                    }
                }
                var castedFile = _this as FileSystemTreeNode;
                if (castedFile != null)
                {
                    var key = castedFile.FilePath + castedFile.Success;
                    if (_fileInfos.ContainsKey(key))
                        _fileInfos.Remove(key);
                    if (_dirInfos.ContainsKey(key))
                        _dirInfos.Remove(key);
                }
            }
        }

        private FileSystemTreeNode RemovePathFromFileInfos(string aPath)
        {
            var key = aPath.ToLower() + false;
            FileSystemTreeNode item;
            // if there is a failed CreateDirectory delete it because we
            // will have 2 entries: one for the failed CreateDirectory and 
            // another for the directory node that we are creating here
            if (_fileInfos.TryGetValue(key, out item))
            {
                _fileInfos.Remove(key);
                item.Parent.Nodes.Remove(item);
            }
            // if there is a node with the item that was successful join it 
            // with the new directory node
            key = aPath.ToLower() + true;
            if (_fileInfos.TryGetValue(key, out item))
            {
                _fileInfos.Remove(key);
                item.Parent.Nodes.Remove(item);
            }

            return item;
        }

        public FileSystemTreeNode GetNode(string path)
        {
            var pathLower = path.ToLower();
            var key = pathLower;
            FileSystemTreeNode item;

            if (!_fileInfos.TryGetValue(key + true, out item) && !_fileInfos.TryGetValue(key + false, out item))
            {
                // if the path is a directory and exist use
                if (!_dirInfos.TryGetValue(pathLower, out item))
                    item = null;
            }
            return item;
        }

        private void CopyNode(FileSystemTreeNode node, StringBuilder tempStr)
        {
            var level = TreeNodeAdvTools.GetLevel(node);
            for (var i = 0; i < level; i++)
                tempStr.Append("\t");
            tempStr.Append(node.FilePath);
            tempStr.Append("\t");
            tempStr.Append(node.AccessString);
            tempStr.Append("\t");
            tempStr.Append(node.Company);
            tempStr.Append("\t");
            tempStr.Append(node.Version);
            tempStr.Append("\t");
            tempStr.Append(node.Description);
            tempStr.Append("\t");
            tempStr.Append(node.CountString);
            tempStr.Append("\t");
            tempStr.Append(node.TimeString);
        }
    }
}