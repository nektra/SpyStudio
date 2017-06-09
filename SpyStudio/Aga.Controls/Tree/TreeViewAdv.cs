using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;
using Aga.Controls.Tools;
using Aga.Controls.Tree.Input;
using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
    /// <summary>
    /// Extensible advanced <see cref="TreeView"/> implemented in 100% managed C# code.
    /// Features: Model/View architecture. Multiple column per node. Ability to select
    /// multiple tree nodes. Different types of controls for each node column: 
    /// <see cref="CheckBox"/>, Icon, Label... Drag and Drop highlighting. Load on
    /// demand of nodes. Incremental search of nodes.
    /// </summary>
    public partial class TreeViewAdv : Control
    {
        private const int LeftMargin = 2;
        internal const int ItemDragSensivity = 4;
        private readonly int _columnHeaderHeight;
        private const int DividerWidth = 9;

        private Pen _linePen;
        private Pen _markPen;
        private int _suspendUpdate;
        private bool _needVisibleNodesUpdate, _needScrollbarUpdate;
        private int _newFocusRow = -1;
        private bool _fireSelectionEvent;
        private readonly NodePlusMinus _plusMinus;
        private readonly ToolTip _toolTip;
        private DrawContext _measureContext;
        private TreeColumn _hotColumn;
        private readonly IncrementalSearch _search;
        private readonly List<TreeNodeAdv> _expandingNodes = new List<TreeNodeAdv>();
        private Node _lastMatchingNode;
        private readonly List<Node> _toBeExpanded = new List<Node>();

        #region Public Events

        [Category("Action")]
        public event ItemDragEventHandler ItemDrag;

        private void OnItemDrag(MouseButtons buttons, object item)
        {
            if (ItemDrag != null)
                ItemDrag(this, new ItemDragEventArgs(buttons, item));
        }

        [Category("Behavior")]
        public event EventHandler<TreeNodeAdvMouseEventArgs> NodeMouseClick;

        private void OnNodeMouseClick(TreeNodeAdvMouseEventArgs args)
        {
            if (NodeMouseClick != null)
                NodeMouseClick(this, args);
        }

        [Category("Behavior")]
        public event EventHandler<TreeNodeAdvMouseEventArgs> NodeMouseDoubleClick;

        private void OnNodeMouseDoubleClick(TreeNodeAdvMouseEventArgs args)
        {
            if (NodeMouseDoubleClick != null)
                NodeMouseDoubleClick(this, args);
        }

        [Category("Behavior")]
        public event EventHandler<TreeColumnEventArgs> ColumnWidthChanged;

        internal void OnColumnWidthChanged(TreeColumn column)
        {
            if (ColumnWidthChanged != null)
                ColumnWidthChanged(this, new TreeColumnEventArgs(column));
        }

        [Category("Behavior")]
        public event EventHandler<TreeColumnEventArgs> ColumnReordered;

        internal void OnColumnReordered(TreeColumn column)
        {
            if (ColumnReordered != null)
                ColumnReordered(this, new TreeColumnEventArgs(column));
        }

        [Category("Behavior")]
        public event EventHandler<TreeColumnEventArgs> ColumnClicked;

        internal void OnColumnClicked(TreeColumn column)
        {
            if (ColumnClicked != null)
                ColumnClicked(this, new TreeColumnEventArgs(column));
        }

        [Category("Behavior")]
        public event EventHandler SelectionChanged;

        internal void OnSelectionChanged()
        {
            if (SuspendSelectionEvent)
                _fireSelectionEvent = true;
            else
            {
                _fireSelectionEvent = false;
                if (SelectionChanged != null)
                    SelectionChanged(this, EventArgs.Empty);
            }
        }

        [Category("Behavior")]
        public event EventHandler<TreeModelNodeEventArgs> Collapsing;

        private void OnCollapsing(Node node)
        {
            if (Collapsing != null)
                Collapsing(this, new TreeModelNodeEventArgs(node));
        }

        [Category("Behavior")]
        public event EventHandler<TreeModelNodeEventArgs> Collapsed;

        private void OnCollapsed(Node node)
        {
            if (Collapsed != null)
                Collapsed(this, new TreeModelNodeEventArgs(node));
        }

        [Category("Behavior")]
        public event EventHandler<TreeModelNodeEventArgs> Expanding;

        private void OnExpanding(Node node)
        {
            if (Expanding != null)
                Expanding(this, new TreeModelNodeEventArgs(node));
        }

        [Category("Behavior")]
        public event EventHandler<TreeModelNodeEventArgs> Expanded;

        private void OnExpanded(Node node)
        {
            if (Expanded != null)
                Expanded(this, new TreeModelNodeEventArgs(node));
        }

        [Category("Behavior")]
        public event EventHandler<ReachableNodeInsertedEventArgs> ReachableNodeInserted;

        [Category("Behavior")]
        public event EventHandler GridLineStyleChanged;

        private void OnGridLineStyleChanged()
        {
            if (GridLineStyleChanged != null)
                GridLineStyleChanged(this, EventArgs.Empty);
        }

        [Category("Behavior")]
        public event ScrollEventHandler Scroll;

        protected virtual void OnScroll(ScrollEventArgs e)
        {
            if (Scroll != null)
                Scroll(this, e);
        }

        [Category("Behavior")]
        public event EventHandler<TreeViewRowDrawEventArgs> RowDraw;

        protected virtual void OnRowDraw(PaintEventArgs e, TreeNodeAdv node, DrawContext context, int row,
                                         Rectangle rowRect)
        {
            if (RowDraw != null)
            {
                var args = new TreeViewRowDrawEventArgs(e.Graphics, e.ClipRectangle, node, context, row, rowRect);
                RowDraw(this, args);
            }
        }

        /// <summary>
        /// Fires when control is going to draw. Can be used to change text or back color
        /// </summary>
        [Category("Behavior")]
        public event EventHandler<DrawTreeEventArgs> DrawControl;

        internal bool DrawControlMustBeFired()
        {
            return DrawControl != null;
        }

        internal void FireDrawControl(DrawTreeEventArgs args)
        {
            OnDrawControl(args);
        }

        protected virtual void OnDrawControl(DrawTreeEventArgs args)
        {
            if (DrawControl != null)
                DrawControl(this, args);
        }


        [Category("Drag Drop")]
        public event EventHandler<DropNodeValidatingEventArgs> DropNodeValidating;

        protected virtual void OnDropNodeValidating(Point point, ref TreeNodeAdv node)
        {
            if (DropNodeValidating != null)
            {
                var args = new DropNodeValidatingEventArgs(point, node);
                DropNodeValidating(this, args);
                node = args.Node;
            }
        }

        [Category("Virtual Mode")]
        public event EventHandler VisibleNodesChanged;

        #endregion

        public TreeViewAdv()
        {
            ShowVScrollBar = ShowHScrollBar = true;
            UnloadCollapsedOnReload = false;
            DefaultToolTipProvider = null;
            ShowNodeToolTips = false;
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.Selectable
                     , true);

            _columnHeaderHeight = Application.RenderWithVisualStyles ? 26 : 23;
            AutoSizeColumnsOnlyWithVisibleNodes = true;

            //BorderStyle = BorderStyle.Fixed3D;
            _hScrollBar.Height = SystemInformation.HorizontalScrollBarHeight;
            _vScrollBar.Width = SystemInformation.VerticalScrollBarWidth;
            _rowLayout = new FixedRowHeightLayout(this, RowHeight);
            _selection = new List<Node>();
            //_readonlySelection = new ReadOnlyCollection<Node>(_selection);
            _columns = new TreeColumnCollection(this);
            _toolTip = new ToolTip();

            _measureContext = new DrawContext {Font = Font, Graphics = Graphics.FromImage(new Bitmap(1, 1))};

            EnabledChanged += TreeViewAdvEnabledChanged;
            VisibleChanged += OnVisibleChanged;
            Input = new NormalInputState(this);
            _search = new IncrementalSearch(this);
            CreatePens();

            ArrangeControls();

            _plusMinus = new NodePlusMinus();
            _controls = new NodeControlsCollection(this);

            Font = _font;
            ExpandingIcon.IconChanged += ExpandingIconChanged;
        }

        private void OnVisibleChanged(object sender, EventArgs eventArgs)
        {
            UpdateScrollBars();
        }

        private void TreeViewAdvEnabledChanged(object sender, EventArgs e)
        {
            BackColor = Enabled ? SystemColors.Window : SystemColors.Control;
        }

        private void ExpandingIconChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new MethodInvoker(DrawIcons));
        }

        private void DrawIcons()
        {
            using (var gr = Graphics.FromHwnd(Handle))
            {
                //Apply the same Graphics Transform logic as used in OnPaint.
                var y = 0;
                if (UseColumns)
                {
                    y += ColumnHeaderHeight;
                    if (Columns.Count == 0)
                        return;
                }
                var firstRowY = _rowLayout.GetRowBounds(FirstVisibleRow).Y;
                y -= firstRowY;
                gr.ResetTransform();
                gr.TranslateTransform(-OffsetX, y);

                var context = new DrawContext();
                context.Graphics = gr;
                foreach (var t in _expandingNodes)
                {
                    foreach (var item in GetNodeControls(t))
                    {
                        if (!(item.Control is ExpandingIcon)) 
                            continue;

                        var bounds = item.Bounds;
                        if (item.TreeNode.Node.Parent == null && UseColumns)
                            bounds.Location = Point.Empty; // display root expanding icon at 0,0

                        context.Bounds = bounds;
                        item.Control.Draw(item.TreeNode, context);
                    }
                }
            }
        }

        #region Public Methods

        public TreeNodeAdv GetNodeAt(Point point)
        {
            var info = GetNodeControlInfoAt(point);
            return info.TreeNode;
        }

        public NodeControlInfo GetNodeControlInfoAt(Point point)
        {
            if (point.X < 0 || point.Y < 0)
                return NodeControlInfo.Empty;

            var row = _rowLayout.GetRowAt(point);
            if (row < ReachableNodeCount && row >= 0)
                return GetNodeControlInfoAt(GetTreeNodeByRow(row), point);
            
            return NodeControlInfo.Empty;
        }

        private NodeControlInfo GetNodeControlInfoAt(TreeNodeAdv node, Point point)
        {
            var rect = _rowLayout.GetRowBounds(FirstVisibleRow);
            point.Y += (rect.Y - ColumnHeaderHeight);
            point.X += OffsetX;
            foreach (var info in GetNodeControls(node))
                if (info.Bounds.Contains(point))
                    return info;

            return FullRowSelect ? new NodeControlInfo(null, Rectangle.Empty, node) : NodeControlInfo.Empty;
        }

        public void EnsureExpanded(Node node)
        {
            //if(_suspendUpdate == 0)
            //{
                while(node.Parent != null)
                {
                    if(!node.IsExpanded)
                        node.IsExpanded = true;
                    node = node.Parent;
                }
            //}
            //else
            //{
            //    _toBeExpanded.Add(node);
            //}
        }

        public virtual void BeginUpdate()
        {
            _suspendUpdate++;
            SuspendSelectionEvent = true;
        }

        public virtual void EndUpdate()
        {
            if (--_suspendUpdate == 0)
            {
                //if(_toBeExpanded.Count > 0)
                //{
                //    foreach(var n in _toBeExpanded)
                //        EnsureExpanded(n);
                //}
                SuspendSelectionEvent = false;
                _toBeExpanded.Clear();
                
                ForceUpdateControl(_needScrollbarUpdate, _needVisibleNodesUpdate);
            }

            Debug.Assert(_suspendUpdate >= 0);
        }

        public void ExpandAll()
        {
            Model.Root.ExpandAll();
        }

        public void CollapseAll()
        {
            Model.Root.CollapseAll();
        }
        public enum ScrollType
        {
            Middle,
            Any
        }
        public void ScrollTo(int focusRow, ScrollType scrollType)
        {
            int row = -1;
            if (focusRow < FirstVisibleRow)
            {
                if (scrollType == ScrollType.Any)
                    row = focusRow;
                else
                {
                    row = focusRow - _rowLayout.PageRowCount / 2;
                    if (row < 0)
                        row = 0;
                }
            }
            else
            {
                if (scrollType == ScrollType.Middle)
                {
                    focusRow += _rowLayout.PageRowCount/2;
                    if (focusRow > _vScrollBar.Maximum)
                        focusRow = _vScrollBar.Maximum;
                }
                var pageStart = _rowLayout.GetRowBounds(FirstVisibleRow).Top;
                var rowBottom = _rowLayout.GetRowBounds(focusRow).Bottom;
                if (rowBottom > pageStart + DisplayRectangle.Height - ColumnHeaderHeight || 
                    scrollType == ScrollType.Middle)
                    row = _rowLayout.GetFirstRow(focusRow);
            }

            if (row >= _vScrollBar.Minimum && row <= _vScrollBar.Maximum)
                _vScrollBar.Value = row;
        }

        public void ScrollTo(int focusRow)
        {
            ScrollTo(focusRow, ScrollType.Any);
        }

        #endregion

        protected override void OnSizeChanged(EventArgs e)
        {
            ArrangeControls();
            UpdateControl(true, true);
            base.OnSizeChanged(e);
        }

        private void ArrangeControls()
        {
            var hBarSize = _hScrollBar.Height;
            var vBarSize = _vScrollBar.Width;
            var clientRect = ClientRectangle;

            _hScrollBar.SetBounds(clientRect.X, clientRect.Bottom - hBarSize,
                                  clientRect.Width - vBarSize, hBarSize);

            _vScrollBar.SetBounds(clientRect.Right - vBarSize, clientRect.Y,
                                  vBarSize, clientRect.Height - hBarSize);
        }

        private void UpdateScrollBars()
        {
            UpdateVScrollBar();
            UpdateHScrollBar();
            UpdateVScrollBar();
            UpdateHScrollBar();
            _hScrollBar.Width = DisplayRectangle.Width;
            _vScrollBar.Height = DisplayRectangle.Height;
        }

        private void UpdateHScrollBar()
        {
            _hScrollBar.Maximum = ContentWidth;
            _hScrollBar.LargeChange = Math.Max(DisplayRectangle.Width, 0);
            _hScrollBar.SmallChange = 5;
            _hScrollBar.Visible = ShowHScrollBar && (_hScrollBar.LargeChange < _hScrollBar.Maximum);
            _hScrollBar.Value = Math.Min(_hScrollBar.Value, _hScrollBar.Maximum - _hScrollBar.LargeChange + 1);
        }

        private void UpdateVScrollBar()
        {
            _vScrollBar.Maximum = Math.Max(ReachableNodeCount - 1, 0);
            _vScrollBar.LargeChange = _rowLayout.PageRowCount;
            _vScrollBar.Visible = ShowVScrollBar && (ReachableNodeCount > 0) && (_vScrollBar.LargeChange <= _vScrollBar.Maximum);
            _vScrollBar.Value = Math.Min(FirstVisibleRow, _vScrollBar.Maximum - _vScrollBar.LargeChange + 1);
        }
        internal void ChangeColumnWidth(TreeColumn column)
        {
            if (!(_input is ResizeColumnState))
            {
                UpdateControl(true, true);
                OnColumnWidthChanged(column);
            }
        }
        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                var res = base.CreateParams;
                switch (BorderStyle)
                {
                    case BorderStyle.FixedSingle:
                        res.Style |= 0x800000;
                        break;
                    case BorderStyle.Fixed3D:
                        res.ExStyle |= 0x200;
                        break;
                }
                return res;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            UpdateView();
            ChangeInput();
            base.OnGotFocus(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _measureContext.Font = Font;
            UpdateControl(true, true);
        }

        internal IEnumerable<NodeControlInfo> GetNodeControls(TreeNodeAdv treeNode)
        {
            if (treeNode == null)
                yield break;

            var rowRect = _rowLayout.GetRowBounds(treeNode.Row);
            foreach (var n in GetNodeControls(treeNode, rowRect))
                yield return n;
        }

        internal IEnumerable<NodeControlInfo> GetNodeControls(TreeNodeAdv node, Rectangle rowRect)
        {
            if (node == null)
                yield break;

            var y = rowRect.Y;
            var x = (node.Level - 1)*_indent + LeftMargin;
            int width;
            if (node.Row == 0 && ShiftFirstNode)
                x -= _indent;
            Rectangle rect;

            if (ShowPlusMinus)
            {
                width = _plusMinus.GetActualSize(node, _measureContext).Width;
                rect = new Rectangle(x, y, width, rowRect.Height);
                if (UseColumns && Columns.Count > 0 && Columns[0].Width < rect.Right)
                    rect.Width = Columns[0].Width - x;

                yield return new NodeControlInfo(_plusMinus, rect, node);
                x += width;
            }

            if (!UseColumns)
            {
                foreach (var c in NodeControls)
                {
                    var s = c.GetActualSize(node, _measureContext);
                    if (!s.IsEmpty)
                    {
                        width = s.Width;
                        rect = new Rectangle(x, y, width, rowRect.Height);
                        x += rect.Width;
                        yield return new NodeControlInfo(c, rect, node);
                    }
                }
            }
            else
            {
                var right = 0;
                foreach (var col in Columns)
                {
                    if (col.IsVisible && col.Width > 0)
                    {
                        right += col.Width;
                        for (var i = 0; i < NodeControls.Count; i++)
                        {
                            var nc = NodeControls[i];
                            if (nc.ParentColumn == col)
                            {
                                var s = nc.GetActualSize(node, _measureContext);
                                if (!s.IsEmpty)
                                {
                                    var isLastControl = true;
                                    for (var k = i + 1; k < NodeControls.Count; k++)
                                        if (NodeControls[k].ParentColumn == col)
                                        {
                                            isLastControl = false;
                                            break;
                                        }

                                    width = right - x;
                                    if (!isLastControl)
                                        width = s.Width;
                                    var maxWidth = Math.Max(0, right - x);
                                    rect = new Rectangle(x, y, Math.Min(maxWidth, width), rowRect.Height);
                                    x += width;
                                    yield return new NodeControlInfo(nc, rect, node);
                                }
                            }
                        }
                        x = right;
                    }
                }
            }
        }

        internal static double Dist(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private delegate void UpdateControlDelegate(bool updateScrollbars, bool updateVisibleNodes);

        public void UpdateControl(bool updateScrollbars, bool updateVisibleNodes)
        {
            if (_suspendUpdate != 0)
            {
                _needScrollbarUpdate = _needScrollbarUpdate || updateScrollbars;
                _needVisibleNodesUpdate = _needVisibleNodesUpdate || updateVisibleNodes;
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke(new UpdateControlDelegate(UpdateControl), updateScrollbars, updateVisibleNodes);
                return;
            }
            ForceUpdateControl(updateScrollbars, updateVisibleNodes);
        }

        void ForceUpdateControl(bool updateScrollbars, bool updateVisibleNodes)
        {
            HideEditor();
            if (_newFocusRow != -1)
            {
                ScrollTo(_newFocusRow);
                _newFocusRow = -1;
            }
            if (updateVisibleNodes || _needVisibleNodesUpdate)
            {
                _needVisibleNodesUpdate = false;
                UpdateVisibleNodes();
            }
            if (updateScrollbars || _needScrollbarUpdate)
            {
                _needScrollbarUpdate = false;
                UpdateScrollBars();
            }
            UpdateView();
        }

        internal void UpdateView()
        {
            if (_suspendUpdate != 0)
                return;
            
            Invalidate(false);
            Update();
        }

        internal void UpdateHeaders()
        {
            Invalidate(new Rectangle(0, 0, Width, ColumnHeaderHeight));
        }

        internal void UpdateColumns()
        {
            UpdateControl(true, true);
        }

        private void CreateNodes()
        {
            Selection.Clear();
            SelectionStart = null;
            Model.Root.IsExpanded = true;
            CurrentNode = null;
        }

        public void SetIsExpanded(Node node, bool expand, bool ignoreChildren)
        {
            SetIsExpanded(node, expand);
            if(!ignoreChildren)
            {
                SetIsExpandedRecursive(node, expand);
            }
        }
        public void SetIsExpanded(Node node, bool expand)
        {
            // Can't collapse root node and cannot expand collapse leaves
            if (Model.Root == node && !expand || node.IsLeaf)
                return;

            if (node.IsExpanded == expand)
                return;

            if (expand)
            {
                _expandedNodes.Add(node, new ExpandedNodeInfo());
                OnExpanding(node);
                node.OnExpanding();
            }
            else
            {
                _expandedNodes.Remove(node);
                OnCollapsing(node);
                node.OnCollapsing();
            }

            int visibleChildrenCount;

            if (!expand)
            {
                // count before because they aren't visible after the expanded is set
                visibleChildrenCount = GetVisibleChildrenCount(node);
                node.AssignIsExpanded(false);
            }
            else
            {
                // count after because they aren't visible until the expanded is set
                node.AssignIsExpanded(true);
                visibleChildrenCount = GetVisibleChildrenCount(node);
            }

            int difference = 0;
            if(node.Parent != null && IsPathExpanded(node.Parent))
            {
                if(expand)
                {
                    _reachableNodeCount += visibleChildrenCount;
                    difference = visibleChildrenCount;
                }
                else
                {
                    _reachableNodeCount -= visibleChildrenCount;
                    difference = -visibleChildrenCount;
                }
            }
            if (difference != 0)
            {
                int row = GetNodeRow(node);
                if (difference > 0)
                {
                    if (row < _firstVisibleNodesRow)
                    {
                        ApplyOffsetToRowArray(_firstVisibleNodesRow, _lastVisibleNodesRow, visibleChildrenCount);
                        _firstVisibleNodesRow += visibleChildrenCount;
                        _lastVisibleNodesRow += visibleChildrenCount;
                        VerifyVisibleNodes();

                        UpdateControl(true, true);
                    }
                    else if (row <= _lastVisibleNodesRow)
                    {
                        // remove all nodes after expanded node
                        int nodesRemoved = 0;
                        for (int i = row + 1; i <= _lastVisibleNodesRow; i++)
                        {
                            RemoveTreeNodeAt(i);
                            nodesRemoved++;
                        }

                        _lastVisibleNodesRow -= nodesRemoved;

                        VerifyVisibleNodes();
                        UpdateControl(true, true);
                    }
                    else
                    {
                        VerifyVisibleNodes();
                        // no new visible nodes but there are new nodes that the user can reach so update scrollbars
                        UpdateControl(true, false);
                    }
                }
                else
                {
                    if (row < FirstVisibleRow)
                    {
                        ApplyOffsetToRowArray(_firstVisibleNodesRow, _lastVisibleNodesRow, difference);
                        _firstVisibleNodesRow -= visibleChildrenCount;
                        _lastVisibleNodesRow -= visibleChildrenCount;
                        VerifyVisibleNodes();
                        UpdateControl(true, true);
                    }
                    else if (row <= _lastVisibleNodesRow)
                    {
                        // remove all nodes after collapsed node
                        int nodesRemoved = 0;
                        for (int i = row + 1; i <= _lastVisibleNodesRow; i++)
                        {
                            RemoveTreeNodeAt(i);
                            nodesRemoved++;
                        }

                        _lastVisibleNodesRow -= nodesRemoved;

                        VerifyVisibleNodes();
                        UpdateControl(true, true);
                    }
                    else
                    {
                        VerifyVisibleNodes();
                        UpdateControl(true, false);
                    }
                }
            }

            if (expand)
            {
                OnExpanded(node);
                node.OnExpanded();
            }
            else
            {
                OnCollapsed(node);
                node.OnCollapsed();
            }
            //Debug.WriteLine("Visible rows: " + _visibleRows);
        }
        [Conditional("DEBUG")]
        void VerifyVisibleNodes()
        {
            if(_visibleNodes.Count > 0)
            {
                int last = _visibleNodes.First().Value.Row, first = _visibleNodes.First().Value.Row;
                foreach (var visNode in _visibleNodes)
                {
                    if (visNode.Key > last)
                        last = visNode.Key;
                    if (visNode.Key < first)
                        first = visNode.Key;
                }
                Debug.Assert(_lastVisibleNodesRow == last && _firstVisibleNodesRow == first);
            }
            else
            {
                Debug.Assert(_firstVisibleNodesRow == 0 && _lastVisibleNodesRow == -1);
            }
        }
        internal Node GetNodeByRow(int row)
        {
            Node node = null;
            TreeNodeAdv treeNode;
            if (!_visibleNodes.TryGetValue(row, out treeNode))
            {
                if (_visibleNodes.Count > 0)
                {
                    if (row > _lastVisibleNodesRow)
                    {
                        int curRow = _lastVisibleNodesRow;
                        node = _visibleNodes[_lastVisibleNodesRow].Node;
                        while (node != null && curRow != row)
                        {
                            node = GetNextVisibleNode(node);
                            curRow++;
                        }
                    }
                    else if (row < _firstVisibleNodesRow)
                    {
                        int curRow = _firstVisibleNodesRow;
                        node = _visibleNodes[_firstVisibleNodesRow].Node;
                        while (node != null && curRow != row)
                        {
                            node = GetPreviousVisibleNode(node);
                            curRow--;
                        }
                    }
                }
                else
                {
                    int curRow = 0;
                    node = GetNextVisibleNode(Model.Root);
                    while (node != null && curRow != row)
                    {
                        node = GetNextVisibleNode(node);
                        curRow++;
                    }
                }
            }
            else
            {
                node = treeNode.Node;
            }
            return node;
        }
        internal void SetIsExpandedRecursive(Node root, bool value)
        {
            foreach (var node in root.Nodes)
            {
                node.IsExpanded = value;
                SetIsExpandedRecursive(node, value);
            }
        }

        internal int GetNodeRow(TreeNodeAdv node)
        {
            return node.Row;
        }
        internal TreeNodeAdv GetTreeNodeByRow(int rowno)
        {
            TreeNodeAdv node;
            if(_visibleNodes.TryGetValue(rowno, out node))
            {
                return node;
            }
            return null;
        }

        private void UpdateVisibleNodes()
        {
            if (_reachableNodeCount == 0)
            {
                Debug.Assert(_firstVisibleNodesRow == 0 && _lastVisibleNodesRow == -1);
                _emptySpaceInDisplay = true;
            }
            else
            {
                int row = FirstVisibleRow;

                Rectangle displayRect = DisplayRectangle;
                int remainingHeight = displayRect.Height;
                if (UseColumns)
                {
                    remainingHeight -= ColumnHeaderHeight;
                }

                Node node = GetNodeByRow(row);

                if (node == null)
                    return;

                var oldRowMap = _visibleNodes;
                _treeNodes.Clear();

                _visibleNodes = new Dictionary<int, TreeNodeAdv>();

                Debug.Assert(_treeNodes.Count == _visibleNodes.Count);

                _firstVisibleNodesRow = row = FirstVisibleRow;
                _lastVisibleNodesRow = -1;

                while (node != null && remainingHeight > 0)
                {
                    var b = _rowLayout.GetRowBounds(row);
                    remainingHeight -= b.Height;
                    TreeNodeAdv treeNode;
                    if (!oldRowMap.TryGetValue(row, out treeNode))
                    {
                        treeNode = CreateVisibleTreeNode(node, row);
                    }
                    else
                    {
                        _visibleNodes[row] = treeNode;
                        _treeNodes[treeNode.Node] = treeNode;
                        treeNode.Row = row;
                        oldRowMap.Remove(row);
                        Debug.Assert(_treeNodes.Count == _visibleNodes.Count);
                    }

                    _lastVisibleNodesRow = row;
                    if (!UseColumns)
                    {
                        _contentWidth = Math.Max(_contentWidth, GetNodeWidth(treeNode));
                    }
                    node = GetNextVisibleNode(node);
                    row++;
                }
                Debug.Assert(_treeNodes.Count == _visibleNodes.Count);
#if DEBUG
                foreach(var n in _visibleNodes)
                {
                    Debug.Assert(n.Key == n.Value.Row && _treeNodes.ContainsValue(n.Value));
                }
#endif

                _emptySpaceInDisplay = remainingHeight > 0;

                if (UseColumns)
                {
                    _contentWidth = 0;
                    foreach (var col in _columns)
                        if (col.IsVisible)
                            _contentWidth += col.Width;
                }
            }
            _suspendUpdate++;
            if (VisibleNodesChanged != null)
                VisibleNodesChanged.Invoke(this, new EventArgs());
            _suspendUpdate--;
        }

        private void VScrollBarValueChanged(object sender, EventArgs e)
        {
            FirstVisibleRow = _vScrollBar.Value;
        }

        private void HScrollBarValueChanged(object sender, EventArgs e)
        {
            OffsetX = _hScrollBar.Value;
        }

        private void VScrollBarScroll(object sender, ScrollEventArgs e)
        {
            OnScroll(e);
        }

        private void HScrollBarScroll(object sender, ScrollEventArgs e)
        {
            OnScroll(e);
        }

        public virtual bool Find(FindEventArgs findArgs)
        {
            var text = findArgs.Text;
            if (!findArgs.MatchCase)
                text = text.ToLower();

            var curModelNode = TreeViewAdvTools.GetFirstNodeToSearch(this, findArgs.SearchDown, true);

            if (curModelNode == null)
                return false;

            while (curModelNode != null)
            {
                if (curModelNode == _lastMatchingNode)
                {
                    curModelNode = findArgs.SearchDown
                                       ? TreeViewAdvTools.GetNextModelNodeInView(curModelNode)
                                       : TreeViewAdvTools.GetPreviousModelNodeInView(curModelNode);
                    continue;
                }

                _lastMatchingNode = null;

                if (IsMatch(text, curModelNode, findArgs))
                    break;

                curModelNode = findArgs.SearchDown
                                   ? TreeViewAdvTools.GetNextModelNodeInView(curModelNode)
                                   : TreeViewAdvTools.GetPreviousModelNodeInView(curModelNode);
            }

            if (curModelNode == null)
                return false;

            _lastMatchingNode = curModelNode;

            EnsureExpanded(curModelNode);

            ClearSelection();

            curModelNode.IsSelected = true;

            return true;
        }

        public virtual bool IsMatch(string wholeString, Node curModelNode, FindEventArgs findArgs)
        {
            return false;
        }

        #region ModelEvents

        private void BindModelEvents()
        {
            _model.NodeChanged += ModelNodeChanged;
            _model.NodeInserted += ModelNodeInserted;
            _model.NodeBeforeRemove += ModelNodeBeforeRemove;
            _model.NodeRemoved += ModelOnNodeRemoved;
            _model.NodesBeforeClear += ModelNodesBeforeClear;
            _model.StructureChanged += ModelStructureChanged;
            _model.NodeSelectedChanged += ModelNodeSelectedChanged;
            _model.NodeExpandedChanged += ModelNodeExpandedChanged;
            _model.NodeCheckChanged += ModelOnNodeCheckChanged;
        }

        private void UnbindModelEvents()
        {
            _model.NodeChanged -= ModelNodeChanged;
            _model.NodeInserted -= ModelNodeInserted;
            _model.NodeBeforeRemove -= ModelNodeBeforeRemove;
            _model.StructureChanged -= ModelStructureChanged;
            _model.NodeSelectedChanged -= ModelNodeSelectedChanged;
            _model.NodeExpandedChanged -= ModelNodeExpandedChanged;
            _model.NodeCheckChanged -= ModelOnNodeCheckChanged;
        }

        private void ModelStructureChanged(TreePath treePath)
        {
            ClearVisibleNodes();
            RecalculateVisibleRowCount();
            UpdateControl(true, true);
        }
        private void ModelOnNodeCheckChanged(Node node)
        {
            UpdateControl(false, false);
        }

        #endregion

        public virtual IEnumerable<Node> AllModelNodes
        {
            get { return _model.Nodes.SelectMany(n => GetCompleteBranchUnder(n)); }
        }

        private IEnumerable<Node> GetCompleteBranchUnder(Node aNode)
        {
            var completeBranch = new List<Node> { aNode };

            foreach (var node in aNode.Nodes)
                completeBranch.AddRange(GetCompleteBranchUnder(node));

            return completeBranch;
        }

        public delegate void UserRequestedCheckStateChangeHandler(TreeNodeAdv node, CheckState value);

        public UserRequestedCheckStateChangeHandler OnUserRequestedCheckStateChange;

        public void NotifyUserRequestedCheckStateChange(TreeNodeAdv node, CheckState value)
        {
            if (OnUserRequestedCheckStateChange != null)
                OnUserRequestedCheckStateChange(node, value);
        }

        private static readonly char[] Splitter = new[] {'\\'};

        public T GetNodeByTreePath<T>(string nodePath) where T : Node
        {
            var nodes = Model.Nodes;
            var list = nodePath.Split(Splitter, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; ; )
            {
                var dir = list[i++];
                //index = nodes.map(x => x.Text).IndexOf(dir)
                int index = nodes.TakeWhile(node => node.Text != dir).Count();
                if (index >= nodes.Count)
                    return null;
                if (i == list.Count)
                    return nodes[index] as T;
                nodes = nodes[index].Nodes;
            }
        }
    }
}