using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aga.Controls;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.ContextMenu;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Registry.Filters;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Registry.Controls
{
    public class RegistryTree : TreeViewAdv, ITreeInterpreter
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SortedDictionary<string, RegistryTreeNodeBase> BaseNodesByName { get; set; }

        public event Action<List<RegistryTreeNode>> ExportRequest;
        public List<RegInfoFilter> PathFilters;

        protected EntryContextMenu EntryProperties;

        private RegistryValueList _valuesView;
        private bool _recursiveCheck = true;
        private bool _checkBoxes;
        private bool _inCheck;
        private bool _disposed;
        private bool _showIsolationOptions;
        private SortedTreeModel _model;
        private TreeColumn _columnHeaderKey;
        private TreeColumn _columnHeaderResult;
        private TreeColumn _columnHeaderIsolation;

        protected string NextSelectedValue;
        private bool _lastFindWasKey;

        private NodeCheckBox _columnHeaderCheckBoxNode;
        private NodeControl _columnHeaderKeyNode;
        private NodeControl _columnHeaderResultNode;
        private NodeComboBox _columnHeaderIsolationNode;

        protected readonly ToolStripMenuItem ExpandDiffsItem = new ToolStripMenuItem("Expand all differences");
        protected readonly ToolStripMenuItem CheckItem = new ToolStripMenuItem("Check");
        protected readonly ToolStripMenuItem CheckWithChildrenItem = new ToolStripMenuItem("Check recursive");
        protected readonly ToolStripMenuItem UncheckItem = new ToolStripMenuItem("Uncheck");
        protected readonly ToolStripMenuItem UncheckWithChildrenItem = new ToolStripMenuItem("Uncheck recursive");
        private readonly ToolStripMenuItem _exportReg = new ToolStripMenuItem("Export");
        private ContextMenuStrip _contextMenuStripRegistry;

        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        public bool ShowIsolationOptions
        {
            get { return _showIsolationOptions; }
            set
            {
                if (_showIsolationOptions != value)
                {
                    _showIsolationOptions = value;
                    if (_showIsolationOptions)
                    {
                        if (!UseColumns)
                            UseColumns = true;

                        Columns.Add(_columnHeaderIsolation);
                        if (ShowIsolationOptions)
                            NodeControls.Add(_columnHeaderIsolationNode);
                    }
                }
            }
        }

        public uint File1TraceId { get; set; }
        public uint File2TraceId { get; set; }
        public bool GotoVisible { get; set; }

        public bool RedirectClasses
        {
            get { return PathFilters.Any(f => f is RegInfoRedirectClassesFilter); }
            set
            {
                if (value && !RedirectClasses)
                    PathFilters.Add(new RegInfoRedirectClassesFilter());

                if (!value && RedirectClasses)
                    PathFilters.RemoveAll(f => f is RegInfoRedirectClassesFilter);
            }
        }

        public bool MergeLayerPaths
        {
            get { return PathFilters.Any(f => f is RegInfoMergeLayerPathsFilter); }
            set
            {
                if (value && !MergeLayerPaths)
                    PathFilters.Add(new RegInfoMergeLayerPathsFilter());

                if (!value && MergeLayerPaths)
                    PathFilters.RemoveAll(f => f is RegInfoMergeLayerPathsFilter);
            }
        }

        public bool MergeWow
        {
            get { return PathFilters.Any(f => f is RegInfoMergeWowFilter); }
            set
            {
                if (value && !MergeWow)
                    PathFilters.Add(new RegInfoMergeWowFilter());

                if (!value && MergeWow)
                    PathFilters.RemoveAll(f => f is RegInfoMergeWowFilter);
            }
        }

        public RegistryValueList ValuesView
        {
            get { return _valuesView; }
            set
            {
                if (value == null)
                    return;
                _valuesView = value;
                SelectionChanged += (sender, args) => PopulateValuesView();
            }
        }

        #region Events

        public event Action<RegistryTreeNodeBase> OnNodeAdded;

        private void TriggerNodeAddedEvent(RegistryTreeNodeBase aNode)
        {
            if (OnNodeAdded != null)
                OnNodeAdded(aNode);
        }

        #endregion

        #region Instantiation/Initialization

        public RegistryTree()
        {
            Initialize();

            _model.NodeBeforeRemove += node =>
                {
                    if (node.ParentIsRoot)
                        BaseNodesByName.Remove(node.Text.ToLower());
                    else
                        ((RegistryTreeNodeBase) node.Parent).ChildrenByName.Remove(node.Text.ToLower());
                };
        }

        public NodeComboBox.SelectIndexChangedHandler NodeIsolationChanged
        {
            set { _columnHeaderIsolationNode.SelectedIndexChanged = value; }
        }

        private void Initialize()
        {
            UseColumns = false;
            LoadOnDemand = true;
            GotoVisible = true;
            BorderStyle = BorderStyle.FixedSingle;
            ShowLines = false;
            UseColumns = ShowIsolationOptions;

            PathFilters = new List<RegInfoFilter> {new RegInfoLinksFilter()};
            //LeafsByPath = new Dictionary<string, RegistryTreeNodeBase>();
            BaseNodesByName = new SortedDictionary<string, RegistryTreeNodeBase>();

            _columnHeaderKey = new TreeColumn();
            _columnHeaderResult = new TreeColumn();
            //_columnHeaderCheckBox = new TreeColumn();
            _columnHeaderIsolation = new TreeColumn();

            _columnHeaderKey.Header = "Key";
            _columnHeaderKey.SortOrder = SortOrder.None;
            _columnHeaderKey.TooltipText = null;
            _columnHeaderKey.Width = 430;

            _columnHeaderResult.Header = "Result";
            _columnHeaderResult.SortOrder = SortOrder.None;
            _columnHeaderResult.TooltipText = null;
            _columnHeaderResult.Width = 80;

            _columnHeaderIsolation.Header = "Isolation";
            _columnHeaderIsolation.SortOrder = SortOrder.None;
            _columnHeaderIsolation.TooltipText = null;
            _columnHeaderIsolation.Width = 150;

            Columns.Add(_columnHeaderKey);
            //if (ShowIsolationOptions)
            //    Columns.Add(_columnHeaderIsolation);

            var columnKeyTextBox = new NodeTextBox();
            var columnResultTextBox = new NodeTextBox();
            var columnIsolationComboBox = new NodeComboBox();

            _columnHeaderKeyNode = columnKeyTextBox;
            _columnHeaderResultNode = columnResultTextBox;
            _columnHeaderIsolationNode = columnIsolationComboBox;

            columnKeyTextBox.DataPropertyName = "KeyString";
            columnKeyTextBox.Trimming = StringTrimming.EllipsisCharacter;
            columnKeyTextBox.IncrementalSearchEnabled = true;
            columnKeyTextBox.LeftMargin = 3;
            columnKeyTextBox.ParentColumn = _columnHeaderKey;

            columnResultTextBox.DataPropertyName = "ResultString";
            columnResultTextBox.Trimming = StringTrimming.EllipsisCharacter;
            columnResultTextBox.IncrementalSearchEnabled = false;
            columnResultTextBox.LeftMargin = 3;
            columnResultTextBox.ParentColumn = _columnHeaderResult;

            columnIsolationComboBox.DataPropertyName = "Isolation";
            columnIsolationComboBox.Trimming = StringTrimming.EllipsisCharacter;
            columnIsolationComboBox.IncrementalSearchEnabled = true;
            columnIsolationComboBox.LeftMargin = 3;
            columnIsolationComboBox.ParentColumn = _columnHeaderIsolation;
            columnIsolationComboBox.DropDownItems = new List<object>
                {
                    ThinAppIsolationOption.Inherit,
                    ThinAppIsolationOption.Merged,
                    ThinAppIsolationOption.Full,
                    ThinAppIsolationOption.WriteCopy
                };
            columnIsolationComboBox.EditEnabled = true;
            columnIsolationComboBox.EditOnClick = true;

            _columnHeaderCheckBoxNode = new NodeCheckBox
                {
                    //ParentColumn = _columnHeaderCheckBox,
                    ParentColumn = _columnHeaderKey,
                    DataPropertyName = "CheckState",
                    EditEnabled = true
                };
            _columnHeaderCheckBoxNode.CheckStateChanged += RegistryTreeAfterCheck;

            NodeControls.Add(_columnHeaderKeyNode);
            //if (ShowIsolationOptions)
            //    NodeControls.Add(_columnHeaderIsolationNode);

            MouseClick += TreeViewRegistryMouseClick;
            //AfterLabelEdit += RegistryTree_AfterLabelEdit;


            //Sorted = true;

            Model = _model = new SortedTreeModel(new TreeNodeAdvTools.FolderItemSorter(SortOrder.Ascending));

            CreateContextMenuStrip();
        }

        private void CreateContextMenuStrip()
        {
            if (_contextMenuStripRegistry != null) return;
            _contextMenuStripRegistry = new ContextMenuStrip();
            ContextMenuStrip = _contextMenuStripRegistry;
            _contextMenuStripRegistry.Opening += ContextMenuOpening;

            ExpandDiffsItem.Click += ContextMenuExpandDiffs;
            CheckItem.Click += ContextMenuCheckItem;
            CheckWithChildrenItem.Click += ContextMenuCheckItemChildren;
            UncheckItem.Click += ContextMenuUncheckItem;
            UncheckWithChildrenItem.Click += ContextMenuUncheckItemChildren;
            _exportReg.Click += ExportRegOnClick;
            UpdateContextMenu();
        }

        #endregion

        #region UI Control

        public void CheckAllNodes()
        {
            if (!CheckBoxes)
                return;

            this.ExecuteInUIThreadAsynchronously(() =>
                {
                    foreach (var n in _model.Nodes)
                        TreeNodeAdvTools.CheckNode(n, true, true);
                });
        }

        protected bool ExpandDiffs(Node n)
        {
            var ret = false;
            foreach (var c in n.Nodes)
            {
                var keyNode = (RegistryTreeNodeBase)c;
                if (ExpandDiffs(keyNode))
                {
                    EnsureExpanded(keyNode);
                    //var treeNode = FindNodeByTag(keyNode);
                    //if(treeNode)
                    //treeNode.IsExpanded = true;
                    ret = true;
                }
                ret = ret || keyNode.IsDifference;
            }
            return ret;
        }

        public void FindEvent(FindEventArgs e)
        {
            Find(e);
        }

        public override bool Find(FindEventArgs e)
        {
            var text = e.Text;
            string startValue = null;
            if (!e.MatchCase)
                text = text.ToLower();

            var curViewNode = TreeNodeAdvTools.GetFirstNodeToSearch(this, e.SearchDown, true);
            if (curViewNode == null)
                return false;

            if (_valuesView.SelectedItems.Count > 0)
            {
                startValue = ((string) _valuesView.SelectedItems[0].Tag).ToLower();
            }
            var curModelNode = (RegistryTreeNodeBase) curViewNode;

            while (curModelNode != null)
            {
                // if there is a selected item in _valuesView start searching values first
                if (startValue == null && !_lastFindWasKey)
                {
                    if (StringHelpers.MatchString(curModelNode.KeyString, text, e))
                    {
                        EnsureExpanded(curModelNode);
                        ClearSelection();

                        curModelNode.IsSelected = true;
                        EnsureVisible(curModelNode);

                        //SelectedNode = curViewNode;
                        _lastFindWasKey = true;
                        break;
                    }
                }

                var valueHit = curModelNode.FindValue(startValue, text, e);
                if (valueHit != null)
                {
                    // if there is a startValue == a value of current key was selected before find operation -> don't wait until a new key is selected
                    // because the correct key is already selected. Just select the correct value.
                    if (SelectedNode == curModelNode)
                    {
                        _valuesView.SelectedItems.Clear();
                        foreach (RegistryValueItem item in _valuesView.Items)
                        {
                            if (item.Name.ToLower() == valueHit)
                            {
                                item.Selected = true;
                                _valuesView.EnsureVisible(item.Index);
                                break;
                            }
                        }
                    }
                    else
                    {
                        NextSelectedValue = valueHit;
                        SelectedNode = curModelNode;
                    }
                    break;
                }

                startValue = null;
                _lastFindWasKey = false;

                //curNode = e.SearchDown ? TreeNodeAdvTools.GetNextNode(curNode) : TreeNodeAdvTools.GetPreviousNode(curNode);
                curModelNode = e.SearchDown
                                   ? (RegistryTreeNodeBase) Aga.Controls.Tools.TreeViewAdvTools.GetNextModelNodeInView(curModelNode)
                                   : (RegistryTreeNodeBase) Aga.Controls.Tools.TreeViewAdvTools.GetPreviousModelNodeInView(curModelNode);
            }
            if (SelectedNode != null)
            {
                EnsureVisible(SelectedNode);
                return true;
            }

            return false;
        }

        protected virtual void PopulateValuesView()
        {
            if (_valuesView == null)
                return;

            _valuesView.Items.Clear();

            var currentNode = SelectedNode;

            if (currentNode == null || currentNode == Model.Root)
                return;

            var regKey = (RegistryTreeNode) currentNode;
            regKey.PopulateValueList(_valuesView, NextSelectedValue);

            NextSelectedValue = null;
        }

        public void SelectAll()
        {
            // Not implemented since the control has single selection
        }

        protected void ExpandAllErrors(Node node)
        {
            var keyNode = node as RegistryTreeNodeBase;
            if (keyNode != null && keyNode.HasErrors)
                EnsureExpanded(keyNode);

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

        public void CheckPath(string path, bool isChecked, bool recursive)
        {
            var item = GetNodeAt(path);
            if (item != null)
                TreeNodeAdvTools.CheckNode(item, isChecked, recursive);
        }

        public void ClearData()
        {
            this.ExecuteInUIThreadSynchronously(() =>
                {
                    if (EntryProperties != null)
                        EntryProperties.Close(false);
                    BaseNodesByName.Clear();
                    _model.Nodes.Clear();
                    //LeafsByPath.Clear();
                    if (ValuesView != null)
                        ValuesView.Items.Clear();
                });
        }

        public bool RecursiveCheck
        {
            set { _recursiveCheck = value; }
        }

        public bool CheckBoxes
        {
            protected get { return _checkBoxes; }
            set
            {
                if (_checkBoxes == value) 
                    return;

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

        #endregion

        #region Context Menu

        protected virtual void UpdateContextMenu()
        {
            if (ContextMenuStrip == null) 
                return;

            ContextMenuStrip.Items.Clear();
            
            if (CheckBoxes)
            {
                if (ContextMenuStrip.Items.Count > 0)
                    ContextMenuStrip.Items.Add(new ToolStripSeparator());

                ContextMenuStrip.Items.Add(CheckItem);
                ContextMenuStrip.Items.Add(UncheckItem);
                ContextMenuStrip.Items.Add(CheckWithChildrenItem);
                ContextMenuStrip.Items.Add(UncheckWithChildrenItem);
            }
            
            if (ContextMenuStrip.Items.Count > 0)
                ContextMenuStrip.Items.Add(new ToolStripSeparator());

            ContextMenuStrip.Items.Add(_exportReg);

            if (!(ContextMenuStrip.Items.Cast<ToolStripItem>().Last() is ToolStripSeparator))
                ContextMenuStrip.Items.Add(new ToolStripSeparator());
            EntryProperties = new TreeEntryContextMenu(this);
        }

        public void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            if (CheckBoxes)
            {
                bool anyChecked = false, anyUnchecked = false;
                foreach (var treeNode in SelectedNodes)
                {
                    var n = treeNode;
                    if (n.IsChecked)
                        anyChecked = true;
                    else
                        anyUnchecked = true;
                }
                CheckItem.Visible = anyUnchecked;
                UncheckItem.Visible = anyChecked;
            }
        }

        public void ContextMenuExpandDiffs(object sender, EventArgs e)
        {
            if (!_inCheck)
            {
                _inCheck = true;
                BeginUpdate();
                if (SelectedNodes == null || SelectedNodes.Count == 0)
                {
                    foreach (var node in Model.Root.Nodes)
                    {
                        if (ExpandDiffs(node))
                        {
                            node.IsExpanded = true;
                        }
                    }
                }
                else
                {
                    foreach (var node in SelectedNodes)
                    {
                        if (ExpandDiffs(node))
                        {
                            node.IsExpanded = true;
                        }
                    }
                }
                EndUpdate();
                _inCheck = false;
            }
        }

        public void ContextMenuCheckItem(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, true, false);
            EndUpdate();
        }

        public void ContextMenuCheckItemChildren(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, true, true);
            EndUpdate();
        }

        public void ContextMenuUncheckItem(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, false, false);
            EndUpdate();
        }

        public void ContextMenuUncheckItemChildren(object sender, EventArgs e)
        {
            BeginUpdate();
            TreeNodeAdvTools.CheckNodes(SelectedNodes, false, true);
            EndUpdate();
        }

        private void ExportRegOnClick(object sender, EventArgs eventArgs)
        {
            var selNodes = SelectedNodes.Any()
                               ? SelectedNodes.Select(n => n).Cast<RegistryTreeNode>().ToList()
                               : _model.Nodes.Cast<RegistryTreeNode>().ToList();

            if (selNodes.Count <= 0) return;

            var parentNodes = selNodes.Where(n => !TreeNodeAdvTools.IsAncestor(n, selNodes)).ToList();

            if (ExportRequest != null)
                ExportRequest(parentNodes);
        }

        #endregion

        #region Node adding

        public IEnumerable<RegistryTreeNodeBase> Add(IEnumerable<RegKeyInfo> regKeyInfos)
        {
            var addedNodes = new List<RegistryTreeNodeBase>();

            foreach (var regKeyInfo in regKeyInfos)
                addedNodes.Add(Add(regKeyInfo));

            return addedNodes;
        }

        public RegistryTreeNodeBase Add(RegKeyInfo keyInfo)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            _countKeys++;
#endif

            PathFilters.ForEach(filter => filter.ApplyTo(keyInfo));
            var originalPathMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            {
                var split = keyInfo.OriginalPath.SplitAsPath().ToList();
                var paths = new List<string>();
                for (int i = 1; i <= split.Count; i++)
                    paths.Add(split.Take(i).JoinPaths());
                foreach (var path in paths)
                {
                    string replacedPath = path;
                    PathFilters.ForEach(filter => filter.ApplyTo(ref replacedPath));
                    originalPathMap[replacedPath] = path;
                }
            }
            
#if DEBUG
            _timeFilter += sw.Elapsed.TotalMilliseconds;
            var previous = sw.Elapsed.TotalMilliseconds;
#endif

            var deepestPreExistentNode = GetDeepestExistentNodeInPath(keyInfo.NormalizedPath);
            if (deepestPreExistentNode == null)
                return null;

#if DEBUG
            _timeDeepest += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            var missingNodeNames = keyInfo.Path.Substring(deepestPreExistentNode.Path.Length).SplitAsPath();
            
#if DEBUG
            _timeMissing += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            var deepestPreExistentTreeNode = deepestPreExistentNode as RegistryTreeNode;
            var calculateOriginalPath = deepestPreExistentTreeNode != null;
            string accum = null;
            
            if(calculateOriginalPath)
                accum = deepestPreExistentTreeNode.KeyInfo.OriginalPath;

            // generate necessary nodes
            BeginUpdate();

            foreach (var nodeName in missingNodeNames)
            {
                var newNode = GenerateNodeNamed(nodeName);
                deepestPreExistentNode.AddChild(newNode);
                TriggerNodeAddedEvent(newNode);
                deepestPreExistentNode = newNode;
                if(calculateOriginalPath)
                {
                    if (string.IsNullOrEmpty(accum))
                        accum = nodeName;
                    else
                        accum += "\\" + nodeName;
                    string truePath;
                    if (originalPathMap.TryGetValue(accum, out truePath))
                        accum = truePath;
                }
                if(calculateOriginalPath)
                {
                    var treeNode = deepestPreExistentNode as RegistryTreeNode;
                    if (treeNode != null)
                    {
                        treeNode.KeyInfo.OriginalPath = accum;
                    }
                }
            }

            EndUpdate();
            
#if DEBUG
            _timeForeach += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif
            
            deepestPreExistentNode.Merge(keyInfo);

#if DEBUG
            _timeMerge += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            deepestPreExistentNode.UpdateAppearance();

#if DEBUG
            _timeUpdateAppearance += sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            deepestPreExistentNode.PropagateInfoToRoot();

#if DEBUG
            _timePropagate += sw.Elapsed.TotalMilliseconds - previous;
            _timeTotal += sw.Elapsed.TotalMilliseconds;
#endif

            return deepestPreExistentNode;
        }

        public void Add(IEnumerable<RegValueInfo> aValueInfos)
        {
            foreach (var regValueInfo in aValueInfos)
                Add(regValueInfo);
        }

        public RegistryTreeNodeBase Add(RegValueInfo aValueInfo)
        {
            foreach (var pathFilter in PathFilters)
                pathFilter.ApplyTo(aValueInfo);

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            _countValues++;
#endif

            var containerNode = Add(RegKeyInfo.ParentOf(aValueInfo));
            
#if DEBUG
            _timeValue += sw.Elapsed.TotalMilliseconds;
#endif

            if (containerNode != null)
                containerNode.Add(aValueInfo);

            return containerNode;
        }

        protected virtual RegistryTreeNodeBase GenerateNodeNamed(string nodeName)
        {
            var newNode = RegistryTreeNode.For(this);
            newNode.Text = nodeName;
            return newNode;
        }

        #endregion

        #region Node Access

        public List<RegKeyInfo> GetAccessedKeys()
        {
            var regKeys = new List<RegKeyInfo>();

            foreach (var item in _model.Nodes)
            {
                var keyNode = (RegistryTreeNode) item;
                regKeys.AddRange(keyNode.GetAccessedRegistryKeys(false));
            }

            return regKeys;
        }

        public List<RegKeyInfo> GetKeys(IEnumerable<RegistryTreeNode> keys)
        {
            var regKeys = new List<RegKeyInfo>();

            foreach (var keyNode in keys)
            {
                regKeys.AddRange(keyNode.GetAccessedRegistryKeys(true));
            }

            return regKeys;
        }

        public IEnumerable<RegKeyInfo> GetCheckedKeys()
        {
            return GetCheckedKeys(ThinAppIsolationOption.Inherit);
        }

        public List<RegKeyInfo> GetCheckedKeys(ThinAppIsolationOption isolation)
        {
            var regKeys = new List<RegKeyInfo>();

            if (CheckBoxes)
            {
                foreach (var item in _model.Nodes)
                {
                    var keyNode = (RegistryTreeNode) item;
                    regKeys.AddRange(keyNode.GetCheckedKeys(isolation));
                }
            }
            return regKeys;
        }

        private RegistryTreeNodeBase GetNodeAt(string keyPath)
        {
            var keyList = keyPath.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries).ToList();

            RegistryTreeNodeBase resultNode = null;

            var nodeMapKey = keyList.First().ToLower();

            if (!BaseNodesByName.TryGetValue(nodeMapKey, out resultNode))
                return null;

            keyList.RemoveAt(0);

            while (keyList.Any())
            {
                var subKeys = resultNode.ChildrenByName;
                nodeMapKey = keyList.First().ToLower();

                if (!subKeys.TryGetValue(nodeMapKey, out resultNode))
                    return null;

                keyList.RemoveAt(0);
            }

            return resultNode;
        }

        public List<string> GetCheckedPaths(string rootPath)
        {
            var ret = new List<string>();
            var checkedKeys = GetCheckedKeys();
            if (checkedKeys != null)
            {
                var rootPathLower = rootPath.ToLower();
                ret.AddRange(
                    checkedKeys.Select(keyInfo => keyInfo.NormalizedPath).Where(
                        keyPath => keyPath.StartsWith(rootPathLower)));
            }
            return ret;
        }

        private RegistryTreeNodeBase GetDeepestExistentNodeInPath(string aPath)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif

            var nodeNamesInPath = aPath.ToLower().SplitAsPath().ToArray();

#if DEBUG
            _timeSplit += sw.Elapsed.TotalMilliseconds;
#endif

            if (nodeNamesInPath.Length == 0)
                return null;

            RegistryTreeNodeBase deepestExistentNodeInPath;
            if (!BaseNodesByName.TryGetValue(nodeNamesInPath[0], out deepestExistentNodeInPath))
                //return Model.Root as RegistryTreeNodeBase;
                return RegistryTreeRootNode.Of(this);

            for (var i = 1; i < nodeNamesInPath.Length; i++)
            {
                RegistryTreeNodeBase newNode;
                if (!deepestExistentNodeInPath.ChildrenByName.TryGetValue(nodeNamesInPath[i],
                                                                          out newNode))
                    break;
                deepestExistentNodeInPath = newNode;
            }

            return deepestExistentNodeInPath;
        }

        #endregion

        #region Event handling

        private void TreeViewRegistryMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || ContextMenuStrip == null)
                return;

            var node = GetNodeAt(e.Location);
            CheckItem.Visible = (node != null);
            CheckWithChildrenItem.Visible = (node != null);
            UncheckItem.Visible = (node != null);
            UncheckWithChildrenItem.Visible = (node != null);
        }

        private void RegistryTreeAfterCheck(object sender, TreeModelNodeEventArgs e)
        {
            if (!_recursiveCheck || _inCheck)
                return;

            _inCheck = true;
            var n = e.Node;

            if (n.IsChecked)
                TreeNodeAdvTools.CheckNode(n, n.IsChecked, true);
            _inCheck = false;
        }

        public void CopySelectionToClipboard()
        {
            var tempStr = new StringBuilder("");

            foreach (var n in SelectedNodes)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = (RegistryTreeNodeBase) n;
                tempStr.Append(node.Path);
                tempStr.Append("\t");
                tempStr.Append(node.ResultString);
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
        }

        #endregion

        #region IInterpreter implementation

        public IInterpreterController Controller { get; set; }

        public EntryContextMenu ContextMenuController { get { return EntryProperties; } }

        public IEnumerable<IEntry> SelectedEntries
        {
            get
            {
                return
                    SelectedNodes.Select(
                        node => (IEntry) node);
            }
        }

        public Control ParentControl
        {
            get { return this; }
        }

        public virtual void SelectNextVisibleEntry(IEntry anEntry)
        {
            TreeNodeAdvTools.SelectNextNode((Node) anEntry, this);
        }

        public virtual void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            TreeNodeAdvTools.SelectPreviousNode((Node) anEntry, this);
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //dispose managed ressources
                    base.Dispose(true);
                }
            }

            base.Dispose(disposing);
            //dispose unmanaged ressources
            _disposed = true;
        }

        #endregion

        #region Profiling

#if DEBUG
        private static double _timeFilter,
                       _timeDeepest,
                       _timeMissing,
                       _timeForeach,
                       _timeMerge,
                       _timeUpdateAppearance,
                       _timePropagate, _timeTotal, _timeSplit, _timeValue;

        private static int _countKeys, _countValues;
#endif

        [Conditional("DEBUG")]
        public static void InitTimes()
        {
#if DEBUG
            _timeFilter = _timeTotal =
                          _timeDeepest =
                          _timeMissing =
                          _timeForeach = _timeMerge = _timeUpdateAppearance = _timePropagate = _timeSplit = _timeValue = 0;
            _countKeys = _countValues = 0;
#endif
        }
        [Conditional("DEBUG")]
        public static void DumpTimes()
        {
#if DEBUG
            Debug.WriteLine("\nTotal time: " + _timeTotal +
                            "\nTime Deepest: " + _timeDeepest +
                            "\nTotal Split: " + _timeSplit +
                            "\nTotal Value: " + _timeValue +
                            "\nTotal Filter: " + _timeFilter +
                            "\nTotal Missing: " + _timeMissing +
                            "\nTotal Foreach: " + _timeForeach +
                            "\nTotal Merge: " + _timeMerge +
                            "\nTotal Appearance: " + _timeUpdateAppearance +
                            "\nTotal Propagate: " + _timePropagate +
                            "\nCount Keys: " + _countKeys +
                            "\nCount Values: " + _countValues);
#endif

        }

        #endregion

        public void Attach(DeviareRunTrace devRunTrace)
        {
            devRunTrace.OpenKeyAdd += keyInfo => Add(keyInfo);
            devRunTrace.CreateKeyAdd += keyInfo => Add(keyInfo);
            devRunTrace.EnumerateKeyAdd += keyInfo => Add(keyInfo);
            devRunTrace.QueryValueAdd += valueInfo => Add(valueInfo);
            devRunTrace.SetValueAdd += valueInfo => Add(valueInfo);
            devRunTrace.DeleteValueAdd += valueInfo => Add(valueInfo);

            devRunTrace.UpdateBegin += (sender, args) => this.ExecuteInUIThreadAsynchronously(BeginUpdate);
            devRunTrace.UpdateEnd += (sender, args) => this.ExecuteInUIThreadAsynchronously(EndUpdate);
            devRunTrace.RegistryClear += ClearData;
        }

        public enum ContextMenuItemClicked
        {
            Undefined        = 0,
            Check            = 1,
            CheckRecursive   = 2,
            Uncheck          = 3,
            UncheckRecursive = 4,
        }

        public ContextMenuItemClicked GetContextMenuItemClicked(ToolStripItem clickedItem)
        {
            if (clickedItem == CheckItem)
                return ContextMenuItemClicked.Check;
            if (clickedItem == CheckWithChildrenItem)
                return ContextMenuItemClicked.CheckRecursive;
            if (clickedItem == UncheckItem)
                return ContextMenuItemClicked.Uncheck;
            if (clickedItem == UncheckWithChildrenItem)
                return ContextMenuItemClicked.UncheckRecursive;
            return ContextMenuItemClicked.Undefined;
        }
    }
}