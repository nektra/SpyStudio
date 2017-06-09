using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.FileSystem
{
    public enum VersionMismatch
    {
        None,
        FileNotFound,
        Minor,
        Major
    }
    class ToolTipProvider : IToolTipProvider
    {
        public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
        {
            var item = node.Node as FileSystemExplorerNode;
            if (item != null)
            {
                return FileSystemExplorer.GetTooltip(item);
            }
            return null;
        }
    }

    public class FileSystemExplorerNode : Node, TreeNodeAdvTools.ITreeViewAdvComparableNode
    {
        public FileSystemExplorerNode(string filepart, string path)
        {
            FileString = filepart;
            FilePath = path;
            Description = Company = Version = Product = OriginalFileName = string.Empty;
        }
        public FileSystemExplorerNode(string filepart, string path, FileSystemExplorerNode baseNode)
        {
            FileString = filepart;
            FilePath = path;

            Icon = baseNode.OriginalIcon ?? baseNode.Icon;
            IconExpanded = baseNode.OriginalIconExpanded ?? baseNode.IconExpanded;
            IsDirectory = baseNode.IsDirectory;
            Company = baseNode.Company;
            Description = baseNode.Description;
            Product = baseNode.Product;
            OriginalFileName = baseNode.OriginalFileName;
            Version = baseNode.Version;
            IsShortcut = baseNode.IsShortcut;
            FileSystemPath = baseNode.FileSystemPath;
            TargetPath = baseNode.TargetPath;
        }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Company { get; set; }
        public string Product { get; set; }
        public string OriginalFileName { get; set; }
        public Image Icon { get; set; }
        public Image IconExpanded { get; set; }
        public string CompareCache { get; set; }
        public string FileSystemPath { get; set; }
        public FileSystemAccess Access { get; set; }

        public override string Text { get { return FileString; } }
        public string FilePath { get; set; }
        public string TargetPath { get; set; }

        public Image OriginalIcon { get; set; }
        public Image OriginalIconExpanded { get; set; }

        public string FileString { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsShortcut { get; set; }
        public bool IsFileCreated { get; set; }

        public VersionMismatch VersionInfo { get; set; }

        public override bool IsLeaf
        {
            get
            {
                return (Nodes.Count == 0 && !IsDirectory);
            }
        }

        public string NameforDisplay
        {
            get { return FilePath; }
        }
    }

    public class LabelCancelEventArgs : LabelEventArgs
    {
        public LabelCancelEventArgs(bool cancel, object subject, string oldLabel, string newLabel)
            : base(subject, oldLabel, newLabel)
        {
            Cancel = cancel;
        }

        public bool Cancel { get; set; }
    }
    public class NodeTextBoxFilename : NodeTextBox
    {
        private FileSystemExplorerNode _editingNode;
        private Control _control;
        private FileSystemExplorer _tree;

        public NodeTextBoxFilename(FileSystemExplorer tree)
        {
            _tree = tree;
        }
        protected override Control CreateEditor(TreeNodeAdv node)
        {
            if(_tree.CreateComboEditor(node))
                _control = CreateCombo(node);
            else
                _control = CreateTextBox(node);
            _editingNode = (FileSystemExplorerNode) node.Node;
            return _control;
        }

        public event EventHandler<LabelCancelEventArgs> NodeChangeValidating;

        protected override void DisposeEditor(Control editor)
        {
            var textBox = editor as TextBox;
            if(textBox != null)
            {
                textBox.TextChanged -= EditorTextChanged;
                textBox.KeyDown -= EditorKeyDown;
            }
        }
        protected override Size CalculateEditorSize(EditorContext context)
        {
            var textBox = context.Editor as TextBox;
            if (textBox != null)
            {
                return CalculateTextBoxSize(context);
            }
            return CalculateComboBoxSize(context);
        }

        public List<object> DropDownItems { get; set; }
        #region TextBox
        protected Control CreateTextBox(TreeNodeAdv node)
        {
            TextBox textBox = CreateTextBox();
            textBox.TextAlign = TextAlign;
            textBox.Text = GetLabel(node);
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.TextChanged += EditorTextChanged;
            textBox.KeyDown += EditorKeyDown;
            _label = textBox.Text;
            SetEditControlProperties(textBox, node);
            return textBox;
        }
        private void EditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                EndEdit(false);
            else if (e.KeyCode == Keys.Enter)
                EndEdit(true);
        }

        private const int MinTextBoxWidth = 30;
        private string _label;
        private void EditorTextChanged(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if(textBox != null)
            {
                _label = textBox.Text;
                Parent.UpdateEditorBounds();
            }
        }
        protected Size CalculateTextBoxSize(EditorContext context)
        {
            if (Parent.UseColumns)
                return context.Bounds.Size;
            Size size = GetLabelSize(context.CurrentNode, context.DrawContext, _label);
            int width = Math.Max(size.Width + Font.Height, MinTextBoxWidth); // reserve a place for new typed character
            return new Size(width, size.Height);
        }
        #endregion

        #region ComboBox
        private Control CreateCombo(TreeNodeAdv node)
        {
            var comboBox = new ComboBox();
            if (DropDownItems != null)
                comboBox.Items.AddRange(DropDownItems.ToArray());
            comboBox.SelectedItem = GetValue(node);
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.DropDownClosed += EditorDropDownClosed;
            SetEditControlProperties(comboBox, node);
            return comboBox;
        }
        void EditorDropDownClosed(object sender, EventArgs e)
        {
            EndEdit(true);
        }

        private int _editorWidth = 100;
        [DefaultValue(100)]
        public int EditorWidth
        {
            get { return _editorWidth; }
            set { _editorWidth = value; }
        }
        private int _editorHeight = 100;
        [DefaultValue(100)]
        public int EditorHeight
        {
            get { return _editorHeight; }
            set { _editorHeight = value; }
        }

        protected Size CalculateComboBoxSize(EditorContext context)
        {
            if (Parent.UseColumns)
            {
                if (context.Editor is CheckedListBox)
                    return new Size(context.Bounds.Size.Width, EditorHeight);
                return context.Bounds.Size;
            }
            if (context.Editor is CheckedListBox)
                return new Size(EditorWidth, EditorHeight);
            return new Size(EditorWidth, context.Bounds.Height);
        }

        #endregion

        protected override void DoApplyChanges(TreeNodeAdv node, Control editor)
        {
            string newText;
            var textBox = (editor as TextBox);
            if(textBox == null)
            {
                var comboBox = (editor as ComboBox);
                Debug.Assert(comboBox != null);
                newText = comboBox.Text;
            }
            else
            {
                newText = textBox.Text;
            }

            if (NodeChangeValidating != null)
            {
                var e = new LabelCancelEventArgs(false, _editingNode, _editingNode.FileString, newText);
                NodeChangeValidating(this, e);
                if (e.Cancel)
                    return;
            }

            string oldLabel = GetLabel(node);
            if (oldLabel != newText)
            {
                SetLabel(node, newText);
                OnLabelChanged(node.Node, oldLabel, newText);
            }
        }
        public void Cut()
        {
            if(IsEditing())
            {
                var textBox = _control as TextBox;
                if(textBox != null)
                    textBox.Cut();
            }
        }
        public void Copy()
        {
            if (IsEditing())
            {
                var textBox = _control as TextBox;
                if (textBox != null)
                    textBox.Copy();
            }
        }
        public void Paste()
        {
            if (IsEditing())
            {
                var textBox = _control as TextBox;
                if (textBox != null)
                    textBox.Paste();
            }
        }
    }

    public class ClipboardOperation
    {
        public bool IsCut { get; set; }
        public List<Node> Nodes { get; set; } 

    }
    public class FileSystemExplorer : TreeViewAdv
    {
        private readonly TreeColumn _columnHeaderFilename;
        private readonly TreeColumn _columnHeaderVersion;
        private readonly TreeColumn _columnHeaderCompany;
        private readonly TreeColumn _columnHeaderDescription;

        private readonly NodeTextBoxFilename _columnHeaderFilenameNode;
        private readonly NodeStateIcon _columnHeaderFilenameIconNode;
        private readonly NodeTextBox _columnHeaderVersionNode;
        private readonly NodeTextBox _columnHeaderCompanyNode;
        private readonly NodeTextBox _columnHeaderDescriptionNode;

        readonly TreeNodeAdvTools.FileSystemTreeItemSorter _comparer = new TreeNodeAdvTools.FileSystemTreeItemSorter(SortOrder.Ascending);

        private readonly SortedTreeModel _model;
        readonly Dictionary<string, List<FileSystemExplorerNode>> _shortcutsInfos = new Dictionary<string, List<FileSystemExplorerNode>>();
        readonly Dictionary<string, FileSystemExplorerNode> _fileInfos = new Dictionary<string, FileSystemExplorerNode>();
        readonly Dictionary<string, FileSystemExplorerNode> _dirInfos = new Dictionary<string, FileSystemExplorerNode>();

        public static List<object> ValidSystemDirectories = new List<object>();
        public static List<object> ValidUserDirectories = new List<object>();

        private readonly ContextMenuStrip _contextMenuStripTreeViewExplorer;
        private readonly ToolStripMenuItem _newToolStripMenuItem;
        private readonly ToolStripMenuItem _deleteToolStripMenuItem;
        private readonly ToolStripMenuItem _renameToolStripMenuItem;
        private readonly ToolStripMenuItem _folderToolStripMenuItem;
        private readonly ToolStripMenuItem _shortcutToolStripMenuItem;
        private readonly ToolStripMenuItem _localFileToolStripMenuItem;
        private readonly ToolStripSeparator _editSepToolStripMenuItem;
        private readonly ToolStripMenuItem _cutToolStripMenuItem;
        private readonly ToolStripMenuItem _copyToolStripMenuItem;
        private readonly ToolStripMenuItem _pasteToolStripMenuItem;

        private ClipboardOperation _currentOperation;
        private readonly FileSystemExplorerNode _systemNode;
        private readonly FileSystemExplorerNode _userNode;

        public FileSystemExplorer()
        {
            ValidSystemDirectories.Add("[_B_]ALLUSERSPROFILE[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONADMINTOOLS[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONAPPDATA[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONDESKTOP[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONDOCUMENTS[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONFAVORITES[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONFILES64[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONFILES[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONMUSIC[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONPICTURES[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONPROGRAMS[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONSTARTMENU[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONSTARTUP[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONTEMPLATES[_E_]");
            ValidSystemDirectories.Add("[_B_]COMMONVIDEO[_E_]");
            ValidSystemDirectories.Add("[_B_]DEFAULTUSERPROFILE[_E_]");
            ValidSystemDirectories.Add("[_B_]FONTS[_E_]");
            ValidSystemDirectories.Add("[_B_]MEDIAPATH[_E_]");
            ValidSystemDirectories.Add("[_B_]MSSHAREDTOOLS64[_E_]");
            ValidSystemDirectories.Add("[_B_]MSSHAREDTOOLS[_E_]");
            ValidSystemDirectories.Add("[_B_]PROFILESDIRECTORY[_E_]");
            ValidSystemDirectories.Add("[_B_]PROGRAMFILES64[_E_]");
            ValidSystemDirectories.Add("[_B_]PROGRAMFILES[_E_]");
            ValidSystemDirectories.Add("[_B_]SYSTEM32[_E_]");
            ValidSystemDirectories.Add("[_B_]SYSTEM64[_E_]");
            ValidSystemDirectories.Add("[_B_]SYSTEMDRIVE[_E_]");
            ValidSystemDirectories.Add("[_B_]WINDIR[_E_]");

            ValidUserDirectories.Add("[_B_]ADMINTOOLS[_E_]");
            ValidUserDirectories.Add("[_B_]APPDATA[_E_]");
            ValidUserDirectories.Add("[_B_]CACHE[_E_]");
            ValidUserDirectories.Add("[_B_]CDBURNING[_E_]");
            ValidUserDirectories.Add("[_B_]COOKIES[_E_]");
            ValidUserDirectories.Add("[_B_]DESKTOP[_E_]");
            ValidUserDirectories.Add("[_B_]FAVORITES[_E_]");
            ValidUserDirectories.Add("[_B_]HISTORY[_E_]");
            ValidUserDirectories.Add("[_B_]LOCALAPPDATA[_E_]");
            ValidUserDirectories.Add("[_B_]LOCALSETTINGS[_E_]");
            ValidUserDirectories.Add("[_B_]MYMUSIC[_E_]");
            ValidUserDirectories.Add("[_B_]MYPICTURES[_E_]");
            ValidUserDirectories.Add("[_B_]MYVIDEO[_E_]");
            ValidUserDirectories.Add("[_B_]NETHOOD[_E_]");
            ValidUserDirectories.Add("[_B_]PERSONAL[_E_]");
            ValidUserDirectories.Add("[_B_]PRINTHOOD[_E_]");
            ValidUserDirectories.Add("[_B_]PROGRAMS[_E_]");
            ValidUserDirectories.Add("[_B_]RECENT[_E_]");
            ValidUserDirectories.Add("[_B_]SENDTO[_E_]");
            ValidUserDirectories.Add("[_B_]STARTMENU[_E_]");
            ValidUserDirectories.Add("[_B_]STARTUP[_E_]");
            ValidUserDirectories.Add("[_B_]TEMPLATES[_E_]");
            ValidUserDirectories.Add("[_B_]TEMP[_E_]");
            ValidUserDirectories.Add("[_B_]USERPROFILE[_E_]");

            DragDropMarkColor = Color.Black;
            FullRowSelect = true;
            LineColor = SystemColors.ControlDark;
            SelectionMode = Aga.Controls.Tree.TreeSelectionMode.Multi;
            ShowLines = false;
            ShowNodeToolTips = true;
            UseColumns = true;
            BorderStyle = BorderStyle.FixedSingle;

            _columnHeaderFilename = new TreeColumn();
            _columnHeaderVersion = new TreeColumn();
            _columnHeaderCompany = new TreeColumn();
            _columnHeaderDescription = new TreeColumn();

            _columnHeaderFilenameNode = new NodeTextBoxFilename(this)
            {
                Trimming = StringTrimming.EllipsisCharacter,
                IncrementalSearchEnabled = true,
                LeftMargin = 3,
                ParentColumn = _columnHeaderFilename,
                DataPropertyName = "FileString",
                DisplayHiddenContentInToolTip = true,
                EditEnabled = true,
                ToolTipProvider = new ToolTipProvider()
            };
            _columnHeaderFilenameNode.NodeChangeValidating += ColumnHeaderFilenameNodeOnNodeChangeValidating;
            
            _columnHeaderFilenameIconNode = new NodeStateIcon
            {
                DataPropertyName = "Icon",
                DataPropertyNameExpanded = "IconExpanded",
                LeftMargin = 1,
                ParentColumn = _columnHeaderFilename
            };

            _columnHeaderVersionNode = new NodeTextBox
            {
                Trimming = StringTrimming.EllipsisCharacter,
                LeftMargin = 3,
                ParentColumn = _columnHeaderVersion,
                DataPropertyName = "Version",
                DisplayHiddenContentInToolTip = true,
                ToolTipProvider = new ToolTipProvider()
            };
            _columnHeaderCompanyNode = new NodeTextBox
            {
                Trimming = StringTrimming.EllipsisCharacter,
                LeftMargin = 3,
                ParentColumn = _columnHeaderCompany,
                DataPropertyName = "Company",
                DisplayHiddenContentInToolTip = true,
                ToolTipProvider = new ToolTipProvider()
            };
            _columnHeaderDescriptionNode = new NodeTextBox
            {
                Trimming = StringTrimming.EllipsisCharacter,
                LeftMargin = 3,
                ParentColumn = _columnHeaderDescription,
                DataPropertyName = "Description",
                DisplayHiddenContentInToolTip = true,
                ToolTipProvider = new ToolTipProvider()
            };

            // 
            // columnHeaderFilename
            // 
            _columnHeaderFilename.Header = "Filename";
            _columnHeaderFilename.Width = 200;

            _columnHeaderVersion.Header = "Version";
            _columnHeaderVersion.Width = 100;
            _columnHeaderCompany.Header = "Company";
            _columnHeaderCompany.Width = 150;
            _columnHeaderDescription.Header = "Description";
            _columnHeaderDescription.Width = 200;

            Columns.Add(_columnHeaderFilename);
            Columns.Add(_columnHeaderVersion);
            Columns.Add(_columnHeaderCompany);
            Columns.Add(_columnHeaderDescription);

            NodeControls.Add(_columnHeaderFilenameIconNode);
            NodeControls.Add(_columnHeaderFilenameNode);
            NodeControls.Add(_columnHeaderVersionNode);
            NodeControls.Add(_columnHeaderCompanyNode);
            NodeControls.Add(_columnHeaderDescriptionNode);

            _contextMenuStripTreeViewExplorer = new ContextMenuStrip();
            _newToolStripMenuItem = new ToolStripMenuItem();
            _deleteToolStripMenuItem = new ToolStripMenuItem();
            _renameToolStripMenuItem = new ToolStripMenuItem();
            _folderToolStripMenuItem = new ToolStripMenuItem();
            _shortcutToolStripMenuItem = new ToolStripMenuItem();
            _localFileToolStripMenuItem = new ToolStripMenuItem();
            _cutToolStripMenuItem = new ToolStripMenuItem();
            _copyToolStripMenuItem = new ToolStripMenuItem();
            _pasteToolStripMenuItem = new ToolStripMenuItem();
            _editSepToolStripMenuItem = new ToolStripSeparator();

            _contextMenuStripTreeViewExplorer.Items.AddRange(new ToolStripItem[] {
            _newToolStripMenuItem});
            _contextMenuStripTreeViewExplorer.Name = "contextMenuStripTreeViewExplorer";
            _contextMenuStripTreeViewExplorer.Size = new Size(153, 48);
            // 
            // newToolStripMenuItem
            // 
            _newToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            _folderToolStripMenuItem,
            _shortcutToolStripMenuItem,
            _localFileToolStripMenuItem});
            _newToolStripMenuItem.Name = "newToolStripMenuItem";
            _newToolStripMenuItem.Size = new Size(152, 22);
            _newToolStripMenuItem.Text = "&New";
            // 
            // deleteToolStripMenuItem
            // 
            _deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            _deleteToolStripMenuItem.Size = new Size(152, 22);
            _deleteToolStripMenuItem.Text = "&Delete";
            _deleteToolStripMenuItem.Click += DeleteFolderToolStripMenuItemClick;
            // 
            // renameToolStripMenuItem
            // 
            _renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            _renameToolStripMenuItem.Size = new Size(152, 22);
            _renameToolStripMenuItem.Text = "&Rename";
            _renameToolStripMenuItem.ShortcutKeys = Keys.F2;
            _renameToolStripMenuItem.ShowShortcutKeys = true;
            _renameToolStripMenuItem.Click += RenameFolderToolStripMenuItemClick;
            // 
            // folderToolStripMenuItem
            // 
            _folderToolStripMenuItem.Name = "folderToolStripMenuItem";
            _folderToolStripMenuItem.Size = new Size(152, 22);
            _folderToolStripMenuItem.Text = "&Folder";
            _folderToolStripMenuItem.Click += NewFolderToolStripMenuItemClick;
            // 
            // shortcutToolStripMenuItem
            // 
            _shortcutToolStripMenuItem.Name = "shortcutToolStripMenuItem";
            _shortcutToolStripMenuItem.Size = new Size(152, 22);
            _shortcutToolStripMenuItem.Text = "&Shortcut";
            _shortcutToolStripMenuItem.Click += NewShortcutToolStripMenuItemClick;

            // 
            // localFileToolStripMenuItem
            // 
            _localFileToolStripMenuItem.Name = "localFileToolStripMenuItem";
            _localFileToolStripMenuItem.Size = new Size(152, 22);
            _localFileToolStripMenuItem.Text = "&Local File";
            _localFileToolStripMenuItem.Click += NewLocalFileToolStripMenuItemClick;

            _cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            _cutToolStripMenuItem.Text = "Cu&t";
            _cutToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            _cutToolStripMenuItem.ShowShortcutKeys = true;
            _cutToolStripMenuItem.Click += CutToolStripMenuItemOnClick;

            _copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            _copyToolStripMenuItem.Text = "&Copy";
            _copyToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            _copyToolStripMenuItem.ShowShortcutKeys = true;
            _copyToolStripMenuItem.Click += CopyToolStripMenuItemOnClick;

            _pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            _pasteToolStripMenuItem.Text = "&Paste";
            _pasteToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            _pasteToolStripMenuItem.ShowShortcutKeys = true;
            _pasteToolStripMenuItem.Click += PasteToolStripMenuItemOnClick;

            _contextMenuStripTreeViewExplorer.Items.Add(_deleteToolStripMenuItem);
            _contextMenuStripTreeViewExplorer.Items.Add(_renameToolStripMenuItem);
            _contextMenuStripTreeViewExplorer.Items.Add(_editSepToolStripMenuItem);
            _contextMenuStripTreeViewExplorer.Items.Add(_cutToolStripMenuItem);
            _contextMenuStripTreeViewExplorer.Items.Add(_copyToolStripMenuItem);
            _contextMenuStripTreeViewExplorer.Items.Add(_pasteToolStripMenuItem);
            
            _contextMenuStripTreeViewExplorer.Opening += ContextMenuStripTreeViewExplorerOnOpening;

            ContextMenuStrip = _contextMenuStripTreeViewExplorer;

            Model = _model = new SortedTreeModel(_comparer);

            _systemNode = new FileSystemExplorerNode("System", "System")
                              {
                                  IsDirectory = true,
                                  Icon = FileSystemTools.GetFolderClosedIcon(),
                                  IconExpanded = FileSystemTools.GetFolderOpenedIcon()
                              };
            _userNode = new FileSystemExplorerNode("User", "User")
                            {
                                IsDirectory = true,
                                Icon = FileSystemTools.GetFolderClosedIcon(),
                                IconExpanded = FileSystemTools.GetFolderOpenedIcon()
                            };
            _model.Root.Nodes.Add(_systemNode);
            _systemNode.IsExpanded = true;
            _model.Root.Nodes.Add(_userNode);
            _userNode.IsExpanded = true;
        }

        public List<string> SearchPaths { get; set; }

        public bool CreateComboEditor(TreeNodeAdv treeNode)
        {
            if(treeNode != null)
            {
                var node = treeNode.Node.Parent as FileSystemExplorerNode;
                if (node == _userNode)
                {
                    _columnHeaderFilenameNode.DropDownItems = ValidUserDirectories;
                    return true;
                }
                if (node == _systemNode)
                {
                    _columnHeaderFilenameNode.DropDownItems = ValidSystemDirectories;
                    return true;
                }
            }

            return false;
        }

        private void RenameFolderToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (SelectedNodes.Count != 1)
                return;
            _columnHeaderFilenameNode.BeginEdit();
        }

        public void DeleteFolderToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsEditing())
                return;

            var nodes = SelectedNodes.Select(tn => tn).ToList();
            if (nodes.Count <= 0) 
                return;

            var n = nodes[nodes.Count - 1];
            var nextNode = n.NextNode ?? n.PreviousNode;

            DeleteNodes(nodes);

            if (nextNode == null) 
                return;
            EnsureVisible(nextNode);
            nextNode.IsSelected = true;
        }

        private void DeleteNodes(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                var fileSystemNode = (FileSystemExplorerNode) node;
                var pathLower = fileSystemNode.FilePath.ToLower();

                if(node.Nodes.Count > 0)
                    DeleteNodes(node.Nodes.ToList());

                UpdateShortcuts(fileSystemNode, fileSystemNode.FilePath, null);
                _fileInfos.Remove(pathLower);
                _dirInfos.Remove(pathLower);
                if(fileSystemNode.IsShortcut)
                {
                    RemoveShortcut(fileSystemNode);
                }
                if (node.Parent == null)
                    _model.Nodes.Remove(node);
                else
                    node.Parent.Nodes.Remove(node);
            }
        }
        private bool GetFileProperties(string filePath, out string originalFileName, out string product,
            out string company, out string version, out string description)
        {
            company = string.Empty;
            version = string.Empty;
            description = string.Empty;
            product = string.Empty;
            originalFileName = string.Empty;

            return FileSystemTools.GetFileProperties(filePath, ref originalFileName, ref product, ref company, ref version,
                                                     ref description);
        }
        private void UpdateNodeColorAndVersionInfo(FileSystemExplorerNode node, string versionDefinition)
        {
            if (node.IsShortcut)
            {
                var targetItem = GetItem(node.TargetPath);
                node.ForeColor = targetItem != null ? EntryColors.ValidPathColor : EntryColors.InvalidPathColor;
                node.VersionInfo = VersionMismatch.None;
            }
            else
            {
                if (node.IsFileCreated)
                {
                    node.ForeColor = EntryColors.ValidPathColor;
                    node.VersionInfo = VersionMismatch.None;
                }
                else if(!string.IsNullOrEmpty(versionDefinition))
                {
                    bool minor;
                    bool major = FileSystemTools.MatchVersion(node.Version, versionDefinition, out minor);
                    if (major)
                    {
                        node.ForeColor = minor ? EntryColors.ValidPathColor : EntryColors.WarningMinorVersionColor;
                        node.VersionInfo = minor ? VersionMismatch.None : VersionMismatch.Minor;
                    }
                    else
                    {
                        node.ForeColor = EntryColors.WarningMajorVersionColor;
                        node.VersionInfo = VersionMismatch.Major;
                    }
                }
                else
                {
                    node.VersionInfo = VersionMismatch.FileNotFound;
                    node.ForeColor = EntryColors.InvalidPathColor;
                }
            }
        }
        private void AddNode(FileSystemExplorerNode newNode, Node parent)
        {
            if (newNode.IsDirectory)
                _dirInfos[newNode.FilePath.ToLower()] = newNode;
            else
            {
                var version = string.Empty;

                if (newNode.IsShortcut)
                {
                    var targetItem = GetItem(newNode.TargetPath);
                    newNode.Icon = FileSystemTools.GetShortcutIcon(targetItem != null ? targetItem.Icon : null);
                    AddShortcut(newNode);
                }
                else if(!newNode.IsFileCreated)
                {
                    string company, description, originalFileName, product;
                    if (GetFileProperties(newNode.FileSystemPath, out originalFileName, out product, out company, out version,
                                                          out description))
                    {
                        if (version != newNode.Version)
                            newNode.Version = newNode.Version.ForCompareString() + " / " + version.ForCompareString();
                    }
                    else
                    {
                        newNode.FileSystemPath = string.Empty;
                        string bestMatch = string.Empty,
                               matchCompany = string.Empty,
                               matchVersion = string.Empty,
                               matchDescription = string.Empty;
                        bool matchMajor = false, matchMinor = false;

                        foreach (var p in SearchPaths)
                        {
                            var path = p + @"\" + newNode.FileString;
                            path = Environment.ExpandEnvironmentVariables(path);
                            if (GetFileProperties(path, out originalFileName, out product, out company, out version,
                                                  out description))
                            {
                                bool thisMinor;
                                var thisMajor = FileSystemTools.MatchVersion(newNode.Version, version, out thisMinor);
                                if (string.IsNullOrEmpty(bestMatch) || thisMajor && !matchMajor ||
                                    thisMinor && !matchMinor)
                                {
                                    bestMatch = path;
                                    matchMajor = thisMajor;
                                    matchMinor = thisMinor;
                                    matchCompany = company;
                                    matchVersion = version;
                                    matchDescription = description;

                                    if (matchMajor && matchMinor)
                                        break;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(bestMatch))
                        {
                            version = string.Empty;
                        }
                        else
                        {
                            newNode.Company = matchCompany;
                            newNode.FileSystemPath = bestMatch;
                            version = matchVersion;
                            if (matchVersion != newNode.Version)
                                newNode.Version = newNode.Version.ForCompareString() + " / " + matchVersion.ForCompareString();
                            newNode.Description = matchDescription;
                            newNode.Icon = newNode.Icon ?? FileSystemTools.GetIcon(newNode.FileSystemPath);
                        }
                    }
                    UpdateShortcuts(newNode, null, newNode.FilePath);
                }
                _fileInfos[newNode.FilePath.ToLower()] = newNode;

                UpdateNodeColorAndVersionInfo(newNode, version);
            }

            if (parent != null)
                parent.Nodes.Add(newNode);
            else
            {
                if(ValidSystemDirectories.Contains(newNode.FileString))
                    _systemNode.Nodes.Add(newNode);
                else
                    _userNode.Nodes.Add(newNode);
            }
        }

        public event Action<FileSystemExplorerNode, FileSystemExplorerNode, bool> OnNodeCopyOrMove;

        private void CopyNodes(List<Node> nodes, Node parent, bool isCut)
        {
            foreach (var node in nodes)
            {
                var fileSystemNode = (FileSystemExplorerNode) node;
                var fileSystemParent = (FileSystemExplorerNode)parent;
                if (OnNodeCopyOrMove != null)
                    OnNodeCopyOrMove(fileSystemNode, fileSystemParent, isCut);
                
            }
        }

        public void CopyNode(FileSystemExplorerNode fileSystemNode, FileSystemExplorerNode fileSystemParent, bool isCut)
        {
            string path = fileSystemParent.FilePath + @"\" + fileSystemNode.FileString;
            string fileString = fileSystemNode.FileString;
            var pathLower = fileSystemNode.FilePath.ToLower();

            if (!isCut)
            {
                int i = 0;
                while (GetItem(path) != null)
                {
                    fileString = fileSystemNode.FileString + " (" + ++i + ")";
                    path = fileSystemParent.FilePath + @"\" + fileString;
                }
            }
            else if (GetItem(path) != null)
                return;

            var newNode = new FileSystemExplorerNode(fileString,
                                                     path)
            {
                Icon = fileSystemNode.OriginalIcon ?? fileSystemNode.Icon,
                ForeColor = fileSystemNode.ForeColor,
                IconExpanded = fileSystemNode.OriginalIconExpanded ?? fileSystemNode.IconExpanded,
                IsDirectory = fileSystemNode.IsDirectory,
                Company = fileSystemNode.Company,
                Description = fileSystemNode.Description,
                Version = fileSystemNode.Version,
                IsShortcut = fileSystemNode.IsShortcut,
                FileSystemPath = fileSystemNode.FileSystemPath,
                TargetPath = fileSystemNode.TargetPath,
                IsFileCreated = fileSystemNode.IsFileCreated
            };
            AddNode(newNode, fileSystemParent);
            if (isCut)
            {
                UpdateShortcuts(fileSystemNode, fileSystemNode.FilePath, newNode.FilePath);
                _fileInfos.Remove(pathLower);
                _dirInfos.Remove(pathLower);
            }
            CopyNodes(fileSystemNode.Nodes.ToList(), newNode, isCut);
        }

        private void PasteToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
        {
            if (SelectedNodes.Count == 1 && _currentOperation != null)
            {
                var parent = (FileSystemExplorerNode) SelectedNodes[0];

                if (!parent.IsDirectory && !_currentOperation.IsCut)
                {
                    var newParent = parent.Parent as FileSystemExplorerNode;
                    if (newParent != null && newParent.IsDirectory)
                    {
                        parent = newParent;
                    }
                }
                if(parent.IsDirectory)
                {
                    // verify that we aren't copying the nodes in the same branch that they belong
                    var parentVerify = parent as Node;
                    while(parentVerify != null)
                    {
                        if (_currentOperation.Nodes.Contains(parentVerify))
                            return;
                        parentVerify = parentVerify.Parent;
                    }

                    BeginUpdate();
                    CopyNodes(_currentOperation.Nodes, parent, _currentOperation.IsCut);
                    if (_currentOperation.IsCut)
                    {
                        foreach (var n in _currentOperation.Nodes)
                        {
                            var fileSystemNode = (FileSystemExplorerNode)n;
                            if (n.Parent != null)
                                n.Parent.Nodes.Remove(n);
                            else
                                _model.Nodes.Remove(n);
                            _fileInfos.Remove(fileSystemNode.FilePath.ToLower());
                            _dirInfos.Remove(fileSystemNode.FilePath.ToLower());
                        }
                        _currentOperation = null;
                    }
                    EndUpdate();
                }
            }
        }
        private void CopyToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
        {
            ClearClipboard();
            if (SelectedNodes.Count > 0)
            {
                var nodes = (from n in SelectedNodes where n.Parent != Model.Root select n).ToList();
                //var nodes = SelectedNodes.Select(item => (Node)item.Tag).ToList();

                if (nodes.Count > 0)
                    _currentOperation = new ClipboardOperation { IsCut = false, Nodes = nodes };
            }
        }

        private void CutToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
        {
            ClearClipboard();
            if (SelectedNodes.Count > 0)
            {
                var nodes = (from n in SelectedNodes where n.Parent != Model.Root select n).ToList();
                foreach(var n in nodes)
                {
                    if (n.Parent != null)
                    {
                        var fileSystemNode = (FileSystemExplorerNode)n;
                        fileSystemNode.OriginalIcon = fileSystemNode.Icon;
                        fileSystemNode.Icon = ImageTools.SetBrightness(fileSystemNode.Icon, 25);
                        if (fileSystemNode.IconExpanded != null)
                        {
                            fileSystemNode.OriginalIconExpanded = fileSystemNode.IconExpanded;
                            fileSystemNode.IconExpanded = ImageTools.SetBrightness(fileSystemNode.IconExpanded, 25);
                        }

                        fileSystemNode.NotifyUpdate();
                    }
                }

                if(nodes.Count > 0)
                    _currentOperation = new ClipboardOperation { IsCut = true, Nodes = nodes };
            }
        }
        public void ClearClipboard()
        {
            if(_currentOperation != null)
            {
                foreach(var n in _currentOperation.Nodes)
                {
                    var fileSystemNode = (FileSystemExplorerNode)n;
                    if(fileSystemNode.OriginalIcon != null)
                    {
                        fileSystemNode.Icon = fileSystemNode.OriginalIcon;
                        fileSystemNode.OriginalIcon = null;
                    }
                    if (fileSystemNode.OriginalIconExpanded != null)
                    {
                        fileSystemNode.IconExpanded = fileSystemNode.OriginalIconExpanded;
                        fileSystemNode.OriginalIconExpanded = null;
                    }
                    fileSystemNode.NotifyUpdate();
                }
                _currentOperation = null;
            }
        }
        public void Cut()
        {
            if(IsEditing())
            {
                _columnHeaderFilenameNode.Cut();
            }
            else
            {
                CutToolStripMenuItemOnClick(null, null);
            }
        }
        public void Copy()
        {
            if (IsEditing())
            {
                _columnHeaderFilenameNode.Copy();
            }
            else
            {
                CopyToolStripMenuItemOnClick(null, null);
            }
        }
        public void Paste()
        {
            if (IsEditing())
            {
                _columnHeaderFilenameNode.Paste();
            }
            else
            {
                PasteToolStripMenuItemOnClick(null, null);
            }
        }
        private void ColumnHeaderFilenameNodeOnNodeChangeValidating(object sender, LabelCancelEventArgs labelEventArgs)
        {
            var node = (FileSystemExplorerNode)labelEventArgs.Subject;
            if (!RenameItem(node.FilePath, labelEventArgs.NewLabel))
            {
                labelEventArgs.Cancel = true;
            }
        }

        private void ContextMenuStripTreeViewExplorerOnOpening(object sender, CancelEventArgs cancelEventArgs)
        {
            bool showShortcut = false;
            if(SelectedNodes.Count == 1)
            {
                var node = (FileSystemExplorerNode) SelectedNodes[0];
                if (!node.IsDirectory)
                    showShortcut = true;
                _editSepToolStripMenuItem.Visible = true;
            }
            else
            {
                _editSepToolStripMenuItem.Visible = false;
            }

            _shortcutToolStripMenuItem.Visible = showShortcut;
            _localFileToolStripMenuItem.Visible = showShortcut;
            _newToolStripMenuItem.Visible = (SelectedNodes.Count == 1) || showShortcut;
            _folderToolStripMenuItem.Visible = (SelectedNodes.Count == 1);
            _deleteToolStripMenuItem.Visible = (SelectedNodes.Count >= 1);
            _renameToolStripMenuItem.Visible = (SelectedNodes.Count == 1);
            _cutToolStripMenuItem.Enabled = SelectedNodes.Count >= 1;
            _copyToolStripMenuItem.Enabled = SelectedNodes.Count >= 1;
        }

        private delegate FileSystemExplorerNode AddFileDelegate(FileEntry file, bool expanded);

        public event Action<FileSystemExplorerNode> OnFolderCreated;
        public event Action<FileSystemExplorerNode, FileSystemExplorerNode> OnShortcutCreated;
        public event Action<FileSystemExplorerNode> OnLocalFileCreated;

        private void NewFolderToolStripMenuItemClick(object sender, EventArgs e)
        {
            FileSystemExplorerNode item = null;
            if (SelectedNodes.Count != 1)
                return;

            var parent = (FileSystemExplorerNode)SelectedNodes[0];

            if (!parent.IsDirectory)
                parent = (FileSystemExplorerNode)parent.Parent;

            if(parent == _userNode || parent == _systemNode)
            {
                int i = 0;

                List<object> root;
                if (parent == _userNode)
                    root = ValidUserDirectories;
                else
                    root = ValidSystemDirectories;

                // find the first that doesn't exist
                while (i < root.Count && GetItem((string)root[i]) != null)
                    i++;
                if (i < root.Count)
                {
                    item = GetContainerDirectory((string) root[i], true);
                }
            }
            else
            {
                int i = 0;


                string newPath = parent.FilePath + @"\New Folder";

                // find the first that doesn't exist
                while (GetItem(newPath) != null)
                {
                    newPath = parent.FilePath + @"\New Folder (" + ++i + ")";
                }

                item = GetContainerDirectory(newPath, true);
            }

            if (item == null)
                return;

            if (OnFolderCreated != null)
                OnFolderCreated(item);
            
            FinishNewDirectory(item, true);
        }

        public void CreateNewFolder(string newPath)
        {
            FinishNewDirectory(GetContainerDirectory(newPath, true), false);
        }

        private void FinishNewDirectory(FileSystemExplorerNode item, bool doEdit)
        {
            if (item == null)
                return;
            TreeNodeAdvTools.ClearSelection(this);
            EnsureVisible(item);
            item.IsSelected = true;
            if (doEdit)
                _columnHeaderFilenameNode.BeginEdit();
        }

        private void NewShortcutToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (SelectedNodes.Count != 1)
                return;

            var targetNode = (FileSystemExplorerNode)SelectedNodes[0];
            if (targetNode == null)
                return;

            var item = CreateNewShortcut(targetNode, null, null);
            if (OnShortcutCreated != null)
                OnShortcutCreated(item, targetNode);
        }

        public FileSystemExplorerNode CreateNewShortcut(FileSystemExplorerNode targetNode, string shortcutPath, string shortcutFileSystemPath)
        {
            var filename = FileSystemTools.GetFileName(targetNode.FilePath);
            var dir = FileSystemTools.GetDirectory(targetNode.FilePath);
            string shortcutSuffix = null;
            if (shortcutPath == null)
            {
                shortcutSuffix = " Shortcut.lnk";
                shortcutPath = dir + @"\" + filename + shortcutSuffix;
                for (var i = 1; GetItem(shortcutPath) != null; i++)
                {
                    shortcutSuffix = " Shortcut (" + i + ").lnk";
                    shortcutPath = dir + @"\" + filename + shortcutSuffix;
                }
            }

            var fileSystemName = FileSystemTools.GetFileName(targetNode.FileSystemPath);
            var fileSystemdir = FileSystemTools.GetDirectory(targetNode.FileSystemPath);
            if (shortcutFileSystemPath == null)
            {
                Debug.Assert(shortcutSuffix != null);
                shortcutFileSystemPath = fileSystemdir + @"\" + fileSystemName + shortcutSuffix;
            }

            var shortcut = new FileEntry(shortcutPath, shortcutFileSystemPath, FileSystemAccess.Read, true, targetNode.Company, targetNode.Version,
                                         targetNode.Description, targetNode.Product, targetNode.OriginalFileName, null) { TargetPath = targetNode.FilePath, IsShortcut = true };

            var item = AddFile(shortcut, true);
            item.IsShortcut = true;

            return item;
        }

        private void NewLocalFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (SelectedNodes.Count != 1)
                return;

            var node = (FileSystemExplorerNode)SelectedNodes[0];
            var item = CreateLocalFile(node);

            if (item != null && OnLocalFileCreated != null)
                OnLocalFileCreated(item);
        }

        public FileSystemExplorerNode CreateLocalFile(FileSystemExplorerNode node)
        {
            if (node == null)
                return null;

            var localFilePath = node.FilePath + ".local";
            var localFileSystemPath = node.FileSystemPath + ".local";

            if (ExistItem(localFilePath))
            {
                MessageBox.Show(this,
                                "File " + localFilePath + " already exists.",
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Image icon = FileSystemTools.GetIcon(localFilePath);
            var file = new FileEntry(localFilePath, localFileSystemPath, FileSystemAccess.Read, true, "", "",
                                     "", "", "", icon) { IsFileCreated = true };
            var item = AddFile(file, true);
            item.IsFileCreated = true;

            return item;
        }
        
        public bool IsEditing()
        {
            return _columnHeaderFilenameNode.IsEditing();
        }
        public void EndEdit(bool applyChanged)
        {
            _columnHeaderFilenameNode.EndEdit(applyChanged);
        }

        List<FileSystemExplorerNode> GetContainerDirectoryPath(string path)
        {
            return GetContainerDirectoryPath(path, false);
        }

        List<FileSystemExplorerNode> GetContainerDirectoryPath(string path, bool isDirectory)
        {
            var ret = new List<FileSystemExplorerNode>();
            var index = -1;

            // remove USER_TEMPLATE string since these directories go to User root
            if (path.StartsWith("USER_TEMPLATE"))
            {
                if (path == "USER_TEMPLATE")
                {
                    ret.Add(_userNode);
                    return ret;
                }
                // remove USER_TEMPLATE\
                path = path.Substring("USER_TEMPLATE".Length + 1);
            }

            if (!isDirectory)
            {
                // remove the file entry to keep only the directory with an ending '\'
                index = path.LastIndexOf('\\');
                if (index != -1)
                {
                    index++;
                    path = path.Substring(0, index);
                }
            }

            FileSystemExplorerNode parent = null;
            int prevIndex = 0;

            if (index != -1 || isDirectory)
            {
                index = 0;

                var newIndex = path.IndexOf('\\', index);
                // path of style [_B_]PROGRAMFILES[_E_]
                if (newIndex == -1)
                {
                    index = path.Length;
                }
                else
                {
                    index = path.IndexOf('\\', index) + 1;
                    // when the path doesn't begin with '%Drive%:\' remove trailing '\'
                    // this happens with Swv replacementes (e.g.: [_B_]PROGRAMFILES[_E_])
                    if (index > 3)
                        index--;
                }
            }
            while (index != -1)
            {
                var dir = path.Substring(0, index);

                FileSystemExplorerNode node;
                if (!_dirInfos.TryGetValue(dir.ToLower(), out node))
                {
                    var filepart = path.Substring(prevIndex, index - prevIndex);

                    node = new FileSystemExplorerNode(filepart, dir)
                    {
                        IsDirectory = true,
                        Icon = FileSystemTools.GetFolderClosedIcon(),
                        IconExpanded = FileSystemTools.GetFolderOpenedIcon()
                    };
                    AddNode(node, parent);
                }
                ret.Add(node);
                parent = node;
                while (index < path.Length && path[index] == '\\')
                    index++;
                if (index >= path.Length)
                {
                    index = -1;
                }
                else
                {
                    prevIndex = index;
                    index = path.IndexOf('\\', index);

                    // if it is a directory we have to use the last part of the path. If it a file the last part is the file.
                    if (index == -1 && isDirectory)
                    {
                        index = path.Length;
                    }
                }
            }
            return ret;
        }
        FileSystemExplorerNode GetContainerDirectory(string path, bool isDirectory)
        {
            FileSystemExplorerNode ret = null;
            var nodePath = GetContainerDirectoryPath(path, isDirectory);
            if(nodePath.Count > 0)
            {
                ret = nodePath[nodePath.Count - 1];
            }
            return ret;
        }

        bool ExistItem(string path)
        {
            return GetItem(path) != null;
        }
        protected FileSystemExplorerNode GetItem(string path)
        {
            FileSystemExplorerNode item;            
            var pathLower = path.ToLower();

            if (!_fileInfos.TryGetValue(pathLower, out item))
                item = null;

            if(item == null)
            {
                if (!_dirInfos.TryGetValue(pathLower, out item))
                    item = null;
            }

            return item;
        }
        public bool RenameItem(string oldPath, string newFilePart)
        {
            bool ret = false;
            FileSystemExplorerNode item = GetItem(oldPath);
            if (item != null)
            {
                string newPath;
                var index = item.FilePath.LastIndexOf(@"\", StringComparison.InvariantCulture);
                if (index == -1)
                {
                    newPath = newFilePart;
                }
                else
                {
                    newPath = item.FilePath.Substring(0, index + 1) + newFilePart;
                }

                ret = RenameItem(item, newPath, newFilePart);
            }
            else
            {
                Debug.Assert(false);
            }
            return ret;
        }
        /// <summary>
        /// Update shortcut info
        /// </summary>
        /// <param name="item"></param>
        /// <param name="oldPath">old target path if the shortcut already exists and the target item changed its path</param>
        /// <param name="newPath">new target path if the target item changed the path</param>
        protected void UpdateShortcuts(FileSystemExplorerNode item, string oldPath, string newPath)
        {
            List<FileSystemExplorerNode> shortList;
            // rename target path
            if (oldPath != null && newPath != null)
            {
                var pathLower = oldPath.ToLower();
                // set shortcut path as invalid if any pointing to this file
                if (_shortcutsInfos.TryGetValue(pathLower, out shortList))
                {
                    // change shortcut target to point to the new path
                    _shortcutsInfos.Remove(pathLower);
                    foreach (var shortcutItem in shortList)
                    {
                        shortcutItem.TargetPath = newPath;
                    }

                    List<FileSystemExplorerNode> newPathShortList;
                    if(!_shortcutsInfos.TryGetValue(newPath.ToLower(), out newPathShortList))
                    {
                        _shortcutsInfos[newPath.ToLower()] = new List<FileSystemExplorerNode>();
                    }
                    _shortcutsInfos[newPath.ToLower()].AddRange(shortList);
                }
            }
            // update all items poiting to the new path
            if(newPath != null)
            {
                if (_shortcutsInfos.TryGetValue(newPath.ToLower(), out shortList))
                {
                    foreach (var shortcutItem in shortList)
                    {
                        shortcutItem.ForeColor = EntryColors.ValidPathColor;
                        shortcutItem.Icon = FileSystemTools.GetShortcutIcon(item.Icon);
                        shortcutItem.NotifyUpdate();
                    }
                }
            }
            else if(oldPath != null)
            {
                // node deleted
                var pathLower = oldPath.ToLower();
                if (_shortcutsInfos.TryGetValue(pathLower, out shortList))
                {
                    foreach (var shortcutItem in shortList)
                    {
                        shortcutItem.ForeColor = EntryColors.InvalidPathColor;
                        shortcutItem.Icon = FileSystemTools.GetShortcutIcon(null);
                        shortcutItem.NotifyUpdate();
                    }
                }
            }
        }
        protected void AddShortcut(FileSystemExplorerNode item)
        {
            List<FileSystemExplorerNode> shortList;
            var targetLower = item.TargetPath.ToLower();
            if(!_shortcutsInfos.TryGetValue(targetLower, out shortList))
            {
                _shortcutsInfos[targetLower] = new List<FileSystemExplorerNode>();
            }
            _shortcutsInfos[targetLower].Add(item);
        }
        protected void RemoveShortcut(FileSystemExplorerNode item)
        {
            List<FileSystemExplorerNode> shortList;
            if(_shortcutsInfos.TryGetValue(item.TargetPath.ToLower(), out shortList))
            {
                shortList.Remove(item);
            }
        }
        protected bool RenameItem(FileSystemExplorerNode item, string newPath, string newFilePart)
        {
            var pathLower = item.FilePath.ToLower();
            bool ret;

            if (!item.IsDirectory)
            {
                ret = !_fileInfos.ContainsKey(newPath.ToLower()) && !_dirInfos.ContainsKey(newPath.ToLower());
                if(ret)
                {
                    _fileInfos.Remove(pathLower);
                    UpdateShortcuts(item, item.FilePath, newPath);
                    item.FilePath = newPath;
                    if(newFilePart != null)
                        item.FileString = newFilePart;
                    _fileInfos[newPath.ToLower()] = item;
                }
            }
            else
            {
                ret = !_fileInfos.ContainsKey(newPath.ToLower()) && !_dirInfos.ContainsKey(newPath.ToLower());
                if (ret)
                {
                    _dirInfos.Remove(pathLower);
                    item.FilePath = newPath;
                    if (newFilePart != null)
                        item.FileString = newFilePart;
                    _dirInfos[newPath.ToLower()] = item;

                    // rename all children
                    foreach(FileSystemExplorerNode child in item.Nodes)
                    {
                        RenameItem(child, newPath + @"\" + child.FileString, null);
                    }
                }
            }

            item.NotifyUpdate();

            return ret;
        }
        public FileSystemExplorerNode AddFile(FileEntry file, bool expanded)
        {
            FileSystemExplorerNode item = null;

            this.ExecuteInUIThreadSynchronously(() =>
                {
                    var pathLower = file.Path.ToLower();

                    if (file.IsDirectory)
                    {
                        item = GetContainerDirectory(file.Path, true);
                        item.FileSystemPath = file.FileSystemPath;
                    }
                    else
                    {
                        if (!_fileInfos.TryGetValue(pathLower, out item))
                        {
                            var filepart = FileSystemTools.GetFileName(file.Path);

                            item = new FileSystemExplorerNode(filepart, file.Path)
                            {
                                Company = file.Company,
                                Version = file.Version,
                                Description = file.Description,
                                Product = file.Product,
                                OriginalFileName = file.OriginalFileName,
                                FileSystemPath = file.FileSystemPath,
                                TargetPath = file.TargetPath,
                                Access = file.Access,
                                Icon = file.Icon,
                                IsShortcut = file.IsShortcut,
                                IsFileCreated = file.IsFileCreated
                            };
                            Node parent = null;
                            var nodePath = GetContainerDirectoryPath(file.Path);

                            if (expanded)
                            {
                                foreach (var n in nodePath)
                                {
                                    n.IsExpanded = true;
                                }
                            }
                            if (nodePath.Count > 0)
                            {
                                parent = nodePath[nodePath.Count - 1];
                            }
                            AddNode(item, parent);
                        }
                    }

                    item.NotifyUpdate();
                });

            return item;
        }

        public List<FileEntry> GetFiles()
        {
            var files =
                _fileInfos.Select(
                    item =>
                    new FileEntry(item.Value.FilePath, item.Value.FileSystemPath, 0, true, item.Value.Company, item.Value.Version,
                                  item.Value.Description, item.Value.Product, item.Value.OriginalFileName,
                                  item.Value.Icon)
                        {
                            IsDirectory = false,
                            IsShortcut = item.Value.IsShortcut,
                            IsFileCreated = item.Value.IsFileCreated,
                            FileSystemPath = item.Value.FileSystemPath,
                            TargetPath = item.Value.TargetPath,
                            Access = item.Value.Access
                        }).ToList();
            files.AddRange(_dirInfos.Select(
                item =>
                new FileEntry(item.Value.FilePath, item.Value.FileSystemPath, 0, true, item.Value.Company, item.Value.Version,
                              item.Value.Description, item.Value.Product, item.Value.OriginalFileName, item.Value.Icon)
                    {
                        IsDirectory = true,
                        IsShortcut = item.Value.IsShortcut,
                        TargetPath = item.Value.TargetPath,
                        Access = item.Value.Access
                    }).ToList());
            return files;
        }
        public void Clear()
        {
            ClearData();
        }

        public void ClearData()
        {
            _dirInfos.Clear();
            _fileInfos.Clear();
            BeginUpdate();                
            _systemNode.Nodes.Clear();
            _userNode.Nodes.Clear();
            EndUpdate();
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
        public static string GetTooltip(FileSystemExplorerNode node)
        {
            string tooltip;
            switch (node.VersionInfo)
            {
                case VersionMismatch.FileNotFound:
                    tooltip = "File not found";
                    break;
                case VersionMismatch.None:
                    tooltip = "File found and version matches";
                    break;
                case VersionMismatch.Minor:
                    tooltip = "File found but Minor version doesn't match";
                    break;
                case VersionMismatch.Major:
                    tooltip = "File found but Major version doesn't match";
                    break;
                default:
                    tooltip = string.Empty;
                    break;
            }
            return tooltip;
        }

    }
}
