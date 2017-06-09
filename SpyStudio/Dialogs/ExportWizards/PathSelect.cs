using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.Export;
using SpyStudio.Export.SWV;
using SpyStudio.Loader;
using SpyStudio.Tools;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class PathSelect : InternalWizardPage
    {
        public class PathNode : Node
        {
            public PathNode(string path)
            {
                Path = path;
            }

            public PathNode()
            {
                Path = string.Empty;
            }

            public string Path { get; set; }
        }

        private readonly TreeColumn _columnHeaderPath;

        private readonly NodePath _columnHeaderPathNode;

        private readonly ContextMenuStrip _contextMenuStrip;
        private readonly ToolStripMenuItem _newPathToolStripMenuItem;
        private readonly ToolStripMenuItem _deletePathToolStripMenuItem;

        private readonly TreeModel _model;
        private readonly List<string> _oldPaths = new List<string>();

        protected ExportWizard Wizard;
        protected SwvExport Export;

        protected bool FilesDestinationNeedsUpdate
        {
            set { Wizard.SetStateFlag(WizardStateFlags.FilesDestinationNeedsUpdate, value); }
        }

        public PathSelect(ExportWizard aWizard, SwvExport anExport)
        {
            Wizard = aWizard;
            Export = anExport;


            InitializeComponent();

            _treeViewPath.Model.NodeCheckChanged += node => FilesDestinationNeedsUpdate = Export.PathsWereUpdated = true;

            WizardNext += OnWizardNext;

            _treeViewPath.UseColumns = true;
            _treeViewPath.ShowLines = false;
            _treeViewPath.ShowPlusMinus = false;

            _columnHeaderPath = new TreeColumn();

            _columnHeaderPath.Header = "Path";
            _columnHeaderPath.Width = 300;

            _columnHeaderPathNode = new NodePath
                                        {
                                            Trimming = StringTrimming.EllipsisCharacter,
                                            IncrementalSearchEnabled = true,
                                            LeftMargin = 3,
                                            ParentColumn = _columnHeaderPath,
                                            DataPropertyName = "Path",
                                            DisplayHiddenContentInToolTip = true,
                                            EditEnabled = true,
                                            EditOnClick = true
                                        };

            _treeViewPath.Columns.Add(_columnHeaderPath);

            _treeViewPath.NodeControls.Add(_columnHeaderPathNode);

            _contextMenuStrip = new ContextMenuStrip {Name = "contextMenuStripTreeViewPath", Size = new Size(153, 48)};

            _newPathToolStripMenuItem = new ToolStripMenuItem
                                            {
                                                Name = "newToolStripMenuItem",
                                                Text = "&New",
                                                ShortcutKeys = Keys.Control | Keys.N,
                                                ShowShortcutKeys = true
                                            };
            _newPathToolStripMenuItem.Click += NewPathToolStripMenuItemClick;

            _deletePathToolStripMenuItem = new ToolStripMenuItem
                                               {
                                                   Name = "deleteToolStripMenuItem",
                                                   Text = "&Delete",
                                                   ShortcutKeys = Keys.Delete,
                                                   ShowShortcutKeys = true
                                               };
            _deletePathToolStripMenuItem.Click += DeletePathToolStripMenuItemClick;

            _contextMenuStrip.Items.AddRange(new ToolStripItem[]
                                                 {
                                                     _newPathToolStripMenuItem, _deletePathToolStripMenuItem
                                                 });
            _contextMenuStrip.Opening += ContextMenuStripTreeViewOnOpening;
            ContextMenuStrip = _contextMenuStrip;

            KeyPressed += OnKeyPressed;

            _treeViewPath.Model = _model = new TreeModel();
        }

        private void OnWizardNext(object sender, WizardPageEventArgs wizardPageEventArgs)
        {
            UpdatePaths();
        }

        private void ContextMenuStripTreeViewOnOpening(object sender, CancelEventArgs e)
        {
            _deletePathToolStripMenuItem.Visible = (_treeViewPath.SelectedNodes.Count >= 1);
        }

        private void DeletePathToolStripMenuItemClick(object sender, EventArgs e)
        {
            var nodes = _treeViewPath.SelectedNodes.Select(tn => tn).ToList();
            if (nodes.Count > 0)
            {
                foreach (var node in nodes)
                {
                    if (node.Parent == null)
                        _model.Nodes.Remove(node);
                    else
                        node.Parent.Nodes.Remove(node);
                }
            }
        }

        private void NewPathToolStripMenuItemClick(object sender, EventArgs e)
        {
            var node = new PathNode();
            _model.Nodes.Add(node);
            //var treeNode = TreeNodeAdvTools.GetTreeNode(_treeViewPath, node);
            //if (treeNode != null)
            //{
                _treeViewPath.SelectedNode = node;
                _columnHeaderPathNode.BeginEdit();
            //}
        }

        public void UpdatePaths()
        {
            var i = 0;
            var update = (_oldPaths.Count != _model.Nodes.Count);

            foreach (PathNode p in _model.Nodes)
            {
                if (_oldPaths.Count == _model.Nodes.Count)
                {
                    if (_oldPaths[i++] != p.Path)
                        update = true;
                }

            }
            if (update)
            {
                FilesDestinationNeedsUpdate = true;
            }
        }

        private void OnSetActive(object sender, CancelEventArgs e)
        {
            _oldPaths.Clear();
            _model.Nodes.Clear();

            //SetWizardButtons(WizardButtons.Back | WizardButtons.Next);
            EnableCancelButton(true);
        }

        public bool IsEditing()
        {
            return _columnHeaderPathNode.IsEditing();
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            if (IsEditing())
            {
                if (keyPressedEventArgs.KeyData == Keys.Escape || keyPressedEventArgs.KeyData == Keys.Enter)
                {
                    var apply = keyPressedEventArgs.KeyData == Keys.Enter;

                    if (_columnHeaderPathNode.IsEditing())
                        _columnHeaderPathNode.EndEdit(apply);
                    keyPressedEventArgs.Handled = true;
                }
            }
            else
            {
                // arrow up and down should select first or last item
                if (keyPressedEventArgs.KeyData == Keys.Up || keyPressedEventArgs.KeyData == Keys.Down)
                {
                    if (!_treeViewPath.Focused)
                        _treeViewPath.Focus();
                    if (!_treeViewPath.SelectedNodes.Any())
                    {
                        if (keyPressedEventArgs.KeyData == Keys.Up)
                            TreeNodeAdvTools.SelectFirstNode(_treeViewPath);
                        else
                            TreeNodeAdvTools.SelectLastNode(_treeViewPath);
                        keyPressedEventArgs.Handled = true;
                    }
                }
            }
        }

        public void CopySelectionToClipboard()
        {
            var tempStr = new StringBuilder("");

            foreach (var n in _treeViewPath.SelectedNodes)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = (PathNode) n;

                CopyNode(node, tempStr);
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
        }

        private void CopyNode(PathNode node, StringBuilder tempStr)
        {
            tempStr.Append(node.Path);
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control)
            {
                CopySelectionToClipboard();
            }
            if (e.KeyCode == Keys.A && e.Control)
            {
                TreeNodeAdvTools.SelectAllNodes(_treeViewPath);
            }
            //if (e.KeyCode == Keys.Tab)
            //{
            //    if (_columnHeaderPathNode.IsEditing())
            //    {
            //        _columnHeaderPathNode.EndEdit(true);
            //        if (!e.Shift)
            //            _columnHeaderProcessNode.BeginEdit();
            //        e.Handled = true;
            //    }
            //    else if (_columnHeaderProcessNode.IsEditing())
            //    {
            //        _columnHeaderProcessNode.EndEdit(true);
            //        if (!e.Shift)
            //            _columnHeaderKeyNode.BeginEdit();
            //        else
            //            _columnHeaderTypeNode.BeginEdit();
            //        e.Handled = true;
            //    }
            //    else if (_columnHeaderKeyNode.IsEditing())
            //    {
            //        _columnHeaderKeyNode.EndEdit(true);
            //        if (e.Shift)
            //            _columnHeaderProcessNode.BeginEdit();
            //        e.Handled = true;
            //    }
            //}
            //if(e.KeyCode == Keys.Up)
            //{
            //    _treeViewPath.SelectLastItem();
            //}
            //if (e.KeyCode == Keys.Down)
            //{
            //    _treeViewPath.SelectFirstItem();
            //}
        }
    }
}