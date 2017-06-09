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
using SpyStudio.Export.Templates;
using SpyStudio.Swv;
using SpyStudio.Tools;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class IsolationRulesSelect : TemplatedVirtualizationPage
    {
        public class RuleNode : Node
        {
            public RuleNode(SwvIsolationRuleEntry entry)
            {
                Type = entry.Type.ToString();
                ProcessWildcard = entry.ProcessWildcard;
                KeyWildcard = entry.KeyWildcard;
            }

            public RuleNode()
            {
                Type = SwvIsolationRuleType.LayerCannotSeeBaseKey.ToString();
                ProcessWildcard = string.Empty;
                KeyWildcard = string.Empty;
            }

            public string Type { get; set; }

            public string ProcessWildcard { get; set; }
            public string KeyWildcard { get; set; }
        }

        private readonly TreeColumn _columnHeaderType;
        private readonly TreeColumn _columnHeaderProcess;
        private readonly TreeColumn _columnHeaderKey;

        private readonly NodeComboBox _columnHeaderTypeNode;
        private readonly NodeTextBox _columnHeaderProcessNode;
        private readonly NodeTextBox _columnHeaderKeyNode;

        private readonly ContextMenuStrip _contextMenuStrip;
        private readonly ToolStripMenuItem _newRuleToolStripMenuItem;
        private readonly ToolStripMenuItem _deleteRuleToolStripMenuItem;

        private readonly TreeModel _model;
        //private readonly SwvExportData _data;

        protected ExportField<List<SwvIsolationRuleEntry>> IsolationRules;

        protected ExportWizard Wizard;
        protected SwvExport Export;

        protected bool IsolationRulesChanged
        {
            get { return (bool)Wizard.GetStateFlag(WizardStateFlags.IsolationRulesChanged); }
            set { Wizard.SetStateFlag(WizardStateFlags.IsolationRulesChanged, value); }
        }

        public IsolationRulesSelect(ExportWizard aWizard, SwvExport anExport) : base("", anExport)
        {
            Wizard = aWizard;
            Export = anExport;
            IsolationRules = anExport.GetField<List<SwvIsolationRuleEntry>>(ExportFieldNames.IsolationRules);

            InitializeRequiredStateRegisters();

            InitializeComponent();

            WizardNext += OnWizardNext;
            
            _treeViewRules.UseColumns = true;
            _treeViewRules.ShowLines = false;
            _treeViewRules.ShowPlusMinus = false;

            _columnHeaderType = new TreeColumn();
            _columnHeaderProcess = new TreeColumn();
            _columnHeaderKey = new TreeColumn();

            _columnHeaderType.Header = "Type";
            _columnHeaderType.Width = 140;
            _columnHeaderProcess.Header = "Process";
            _columnHeaderProcess.Width = 200;
            _columnHeaderKey.Header = "Key";
            _columnHeaderKey.Width = 200;

            _columnHeaderTypeNode = new NodeComboBox
                                        {
                                            Trimming = StringTrimming.EllipsisCharacter,
                                            IncrementalSearchEnabled = true,
                                            LeftMargin = 3,
                                            ParentColumn = _columnHeaderType,
                                            DataPropertyName = "Type",
                                            DisplayHiddenContentInToolTip = true,
                                            EditEnabled = true,
                                            DropDownItems =
                                                Enum.GetNames(typeof (SwvIsolationRuleType)).Cast<object>().
                                                ToList(),
                                            EditOnClick = true
                                        };

            _columnHeaderProcessNode = new NodeTextBox
                                           {
                                               Trimming = StringTrimming.EllipsisCharacter,
                                               LeftMargin = 3,
                                               ParentColumn = _columnHeaderProcess,
                                               DataPropertyName = "ProcessWildcard",
                                               EditEnabled = true,
                                               DisplayHiddenContentInToolTip = true,
                                               EditOnClick = true
                                           };

            _columnHeaderKeyNode = new NodeTextBox
                                       {
                                           DataPropertyName = "KeyWildcard",
                                           LeftMargin = 3,
                                           EditEnabled = true,
                                           ParentColumn = _columnHeaderKey,
                                           EditOnClick = true
                                       };

            _columnHeaderTypeNode.ChangesApplied += TypeOnChangesApplied;
            _columnHeaderKeyNode.ChangesApplied += ProcessOnChangesApplied;
            _columnHeaderProcessNode.ChangesApplied += KeyOnChangesApplied;

            _treeViewRules.Columns.Add(_columnHeaderType);
            _treeViewRules.Columns.Add(_columnHeaderProcess);
            _treeViewRules.Columns.Add(_columnHeaderKey);

            _treeViewRules.NodeControls.Add(_columnHeaderTypeNode);
            _treeViewRules.NodeControls.Add(_columnHeaderProcessNode);
            _treeViewRules.NodeControls.Add(_columnHeaderKeyNode);

            _contextMenuStrip = new ContextMenuStrip {Name = "contextMenuStripTreeViewRule", Size = new Size(153, 48)};

            _newRuleToolStripMenuItem = new ToolStripMenuItem
                                            {
                                                Name = "newToolStripMenuItem",
                                                Text = "&New",
                                                ShortcutKeys = Keys.Control | Keys.N,
                                                ShowShortcutKeys = true
                                            };
            _newRuleToolStripMenuItem.Click += NewRuleToolStripMenuItemClick;

            _deleteRuleToolStripMenuItem = new ToolStripMenuItem
                                               {
                                                   Name = "deleteToolStripMenuItem",
                                                   Text = "&Delete",
                                                   ShortcutKeys = Keys.Delete,
                                                   ShowShortcutKeys = true
                                               };
            _deleteRuleToolStripMenuItem.Click += DeleteRuleToolStripMenuItemClick;

            _contextMenuStrip.Items.AddRange(new ToolStripItem[]
                                                 {
                                                     _newRuleToolStripMenuItem, _deleteRuleToolStripMenuItem
                                                 });
            _contextMenuStrip.Opening += ContextMenuStripTreeViewOnOpening;
            ContextMenuStrip = _contextMenuStrip;

            KeyPressed += OnKeyPressed;

            _treeViewRules.Model = _model = new TreeModel();
        }

        private void InitializeRequiredStateRegisters()
        {
            IsolationRulesChanged = true;
        }

        private void OnWizardNext(object sender, WizardPageEventArgs wizardPageEventArgs)
        {
            var rules = GetRules();
            IsolationRules.Value = rules;
            VirtualizationTemplate.Value.SaveExplicitIsolationRules(rules);
        }

        private void RestoreExplicitIsolationRules()
        {
            _model.Nodes.Clear();
            foreach (var rule in VirtualizationTemplate.Value.ExplicitIsolationRules)
            {
                var node = new RuleNode();
                rule.SetNode(node);
                _model.Nodes.Add(node);
            }
        }

        private void TypeOnChangesApplied(object sender, EventArgs eventArgs)
        {
        }

        private void ProcessOnChangesApplied(object sender, EventArgs eventArgs)
        {
        }

        private void KeyOnChangesApplied(object sender, EventArgs eventArgs)
        {
        }

        private void ContextMenuStripTreeViewOnOpening(object sender, CancelEventArgs e)
        {
            _deleteRuleToolStripMenuItem.Visible = (_treeViewRules.SelectedNodes.Count >= 1);
        }

        private void DeleteRuleToolStripMenuItemClick(object sender, EventArgs e)
        {
            var nodes = _treeViewRules.SelectedNodes.Select(tn => tn).ToList();
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

        private void NewRuleToolStripMenuItemClick(object sender, EventArgs e)
        {
            var node = new RuleNode();
            _model.Nodes.Add(node);
            _treeViewRules.SelectedNode = node;
            _columnHeaderTypeNode.BeginEdit();
        }

        public List<SwvIsolationRuleEntry> GetRules()
        {
            return _treeViewRules.AllNodes.Select(node => new SwvIsolationRuleEntry((RuleNode)node)).ToList();
        }

        private void OnSetActive(object sender, CancelEventArgs e)
        {
            if (IsolationRulesChanged)
            {
                IsolationRulesChanged = false;
                _treeViewRules.BeginUpdate();

                if (VirtualizationTemplate.Value.IsInUse)
                {
                    RestoreExplicitIsolationRules();
                }
                else
                {
                    _model.Nodes.Clear();

                    var ruleEntries = IsolationRules.Value;

                    foreach (var r in ruleEntries)
                        _model.Nodes.Add(new RuleNode(r));

                    if (Export.Layer != null)
                        foreach (var isolationRule in Export.Layer.IsolationRules)
                            _model.Nodes.Add(new RuleNode(isolationRule));
                }

                _model.NodeInserted += n => Export.IsolationRulesWereUpdated = true;
                _model.NodeRemoved += (a, b, c) => Export.IsolationRulesWereUpdated = true;
                _model.NodeChanged += n => Export.IsolationRulesWereUpdated = true;

                _treeViewRules.EndUpdate();
            }

            //SetWizardButtons(WizardButtons.Back | WizardButtons.Next);
            EnableCancelButton(true);
        }

        private void FileSelectQueryCancel(object sender, CancelEventArgs e)
        {
        }

        public bool IsEditing()
        {
            return _columnHeaderTypeNode.IsEditing() || _columnHeaderProcessNode.IsEditing() ||
                   _columnHeaderKeyNode.IsEditing();
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            if (IsEditing())
            {
                if (keyPressedEventArgs.KeyData == Keys.Escape || keyPressedEventArgs.KeyData == Keys.Enter)
                {
                    var apply = keyPressedEventArgs.KeyData == Keys.Enter;

                    if (_columnHeaderTypeNode.IsEditing())
                        _columnHeaderTypeNode.EndEdit(apply);
                    else if (_columnHeaderProcessNode.IsEditing())
                        _columnHeaderProcessNode.EndEdit(apply);
                    else if (_columnHeaderKeyNode.IsEditing())
                        _columnHeaderKeyNode.EndEdit(apply);
                    keyPressedEventArgs.Handled = true;
                }
            }
            else
            {
                // arrow up and down should select first or last item
                if (keyPressedEventArgs.KeyData == Keys.Up || keyPressedEventArgs.KeyData == Keys.Down)
                {
                    if (!_treeViewRules.Focused)
                        _treeViewRules.Focus();
                    if (!_treeViewRules.SelectedNodes.Any())
                    {
                        if (keyPressedEventArgs.KeyData == Keys.Up)
                            TreeNodeAdvTools.SelectFirstNode(_treeViewRules);
                        else
                            TreeNodeAdvTools.SelectLastNode(_treeViewRules);
                        keyPressedEventArgs.Handled = true;
                    }
                }
            }
        }

        public void CopySelectionToClipboard()
        {
            var tempStr = new StringBuilder("");

            foreach (var n in _treeViewRules.SelectedNodes)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = (RuleNode) n;

                CopyNode(node, tempStr);
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
        }

        private void CopyNode(RuleNode node, StringBuilder tempStr)
        {
            tempStr.Append(node.Type);
            tempStr.Append("\t");
            tempStr.Append(node.ProcessWildcard);
            tempStr.Append("\t");
            tempStr.Append(node.KeyWildcard);
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control)
            {
                CopySelectionToClipboard();
            }
            if (e.KeyCode == Keys.A && e.Control)
            {
                TreeNodeAdvTools.SelectAllNodes(_treeViewRules);
            }
            if (e.KeyCode == Keys.Tab)
            {
                if (_columnHeaderTypeNode.IsEditing())
                {
                    _columnHeaderTypeNode.EndEdit(true);
                    if (!e.Shift)
                        _columnHeaderProcessNode.BeginEdit();
                    e.Handled = true;
                }
                else if (_columnHeaderProcessNode.IsEditing())
                {
                    _columnHeaderProcessNode.EndEdit(true);
                    if (!e.Shift)
                        _columnHeaderKeyNode.BeginEdit();
                    else
                        _columnHeaderTypeNode.BeginEdit();
                    e.Handled = true;
                }
                else if (_columnHeaderKeyNode.IsEditing())
                {
                    _columnHeaderKeyNode.EndEdit(true);
                    if (e.Shift)
                        _columnHeaderProcessNode.BeginEdit();
                    e.Handled = true;
                }
            }
            //if(e.KeyCode == Keys.Up)
            //{
            //    _treeViewRules.SelectLastItem();
            //}
            //if (e.KeyCode == Keys.Down)
            //{
            //    _treeViewRules.SelectFirstItem();
            //}
        }
    }
}