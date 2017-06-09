using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree.Input;
using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
	public partial class TreeViewAdv
	{
		private Cursor _innerCursor = null;

		public override Cursor Cursor
		{
			get
			{
                if (_innerCursor != null)
                    return _innerCursor;
                else
					return base.Cursor;
			}
			set
			{
				base.Cursor = value;
			}
		}

		#region Internal Properties

		private IRowLayout _rowLayout;

		private bool _dragMode;
		private bool DragMode
		{
			get { return _dragMode; }
			set
			{
				_dragMode = value;
				if (!value)
				{
					StopDragTimer();
					if (_dragBitmap != null)
						_dragBitmap.Dispose();
					_dragBitmap = null;
				}
				else
					StartDragTimer();
			}
		}

		internal int ColumnHeaderHeight
		{
			get
			{
			    if (UseColumns)
					return _columnHeaderHeight;
			    return 0;
			}
		}

	    public IEnumerable<TreeNodeAdv> VisibleNodes
	    {
	        get
	        {
	            return _visibleNodes.Select(n => n.Value).ToList();
	        }
	    }

        public bool IsUpdating { get { return _suspendUpdate > 0 || SuspendSelectionEvent; } }

		private bool _suspendSelectionEvent;
		protected bool SuspendSelectionEvent
		{
			get { return _suspendSelectionEvent; }
			set
			{
				if (value != _suspendSelectionEvent)
				{
					_suspendSelectionEvent = value;
					if (!_suspendSelectionEvent && _fireSelectionEvent)
						OnSelectionChanged();
				}
			}
		}

        private int _firstVisibleNodesRow, _lastVisibleNodesRow = -1, _reachableNodeCount;
	    private bool _emptySpaceInDisplay = true;
        //private Dictionary<int, TreeNodeAdv> _rowMap;

        //internal List<TreeNodeAdv> RowMap
        //{
        //    get { return _rowMap; }
        //}
	    internal Node SelectionStart { get; set; }

	    private InputState _input;
		internal InputState Input
		{
			get { return _input; }
			set
			{
				_input = value;
			}
		}

	    internal bool ItemDragMode { get; set; }

	    internal Point ItemDragStart { get; set; }


	    /// <summary>
		/// Number of rows fits to the current page
		/// </summary>
		internal int CurrentPageSize
		{
			get
			{
				return _rowLayout.CurrentPageSize;
			}
		}

		/// <summary>
		/// Number of all potential visible nodes which parent is expanded or it is Root's child
		/// </summary>
		internal int ReachableNodeCount
		{
			get { return _reachableNodeCount; }
		}

		private int _contentWidth;
		private int ContentWidth
		{
			get
			{
				return _contentWidth;
			}
		}

		private int _firstVisibleRow;
		internal int FirstVisibleRow
		{
			get { return _firstVisibleRow; }
			set
			{
                if(value != _firstVisibleRow)
                {
                    HideEditor();

                    _firstVisibleRow = value;
                    
                    UpdateControl(false, true);
                    //UpdateVisibleNodes();
                    //UpdateView();
                }
			}
		}

        public bool AutoSizeColumnsOnlyWithVisibleNodes { get; set; }

		private int _offsetX;
		public int OffsetX
		{
			get { return _offsetX; }
			private set
			{
				HideEditor();
				_offsetX = value;
				UpdateView();
			}
		}

        public bool ShowVScrollBar { get; set; }
        public bool ShowHScrollBar { get; set; }
        
        public override Rectangle DisplayRectangle
		{
			get
			{
				var r = ClientRectangle;
				int w = _vScrollBar.Visible ? _vScrollBar.Width : 0;
				int h = _hScrollBar.Visible ? _hScrollBar.Height : 0;
				return new Rectangle(r.X, r.Y, r.Width - w, r.Height - h);
			}
		}

		private readonly List<Node> _selection;
		internal List<Node> Selection
		{
			get { return _selection; }
		}

		#endregion

		#region Public Properties

		#region DesignTime

	    [DefaultValue(false), Category("Behavior")]
	    public bool ShiftFirstNode { get; set; }

	    [DefaultValue(false), Category("Behavior")]
	    public bool DisplayDraggingNodes { get; set; }

	    private bool _fullRowSelect;
		[DefaultValue(false), Category("Behavior")]
		public bool FullRowSelect
		{
			get { return _fullRowSelect; }
			set
			{
				_fullRowSelect = value;
				UpdateView();
			}
		}

		private bool _useColumns;
		[DefaultValue(false), Category("Behavior")]
		public bool UseColumns
		{
			get { return _useColumns; }
			set
			{
				_useColumns = value;
				UpdateControl(true, true);
			}
		}

	    [DefaultValue(false), Category("Behavior")]
	    public bool AllowColumnReorder { get; set; }

	    private bool _showLines = true;
		[DefaultValue(true), Category("Behavior")]
		public bool ShowLines
		{
			get { return _showLines; }
			set
			{
				_showLines = value;
				UpdateView();
			}
		}

		private bool _showPlusMinus = true;
		[DefaultValue(true), Category("Behavior")]
		public bool ShowPlusMinus
		{
			get { return _showPlusMinus; }
			set
			{
				_showPlusMinus = value;
				UpdateControl(true, true);
			}
		}

	    [DefaultValue(false), Category("Behavior")]
	    public bool ShowNodeToolTips { get; set; }

	    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), DefaultValue(true), Category("Behavior"), Obsolete("No longer used")]
		public bool KeepNodesExpanded
		{
			get { return true; }
			set {}
		}

		private ITreeModel _model;
        /// <Summary>
        /// The model associated with this <see cref="TreeViewAdv"/>.
        /// </Summary>
        /// <seealso cref="ITreeModel"/>
        /// <seealso cref="TreeModel"/>
        [Browsable(false)]
		public ITreeModel Model
		{
			get { return _model; }
			set
			{
				if (_model != value)
				{
					if (_model != null)
						UnbindModelEvents();
					_model = value;
                    if (_model != null)
                        BindModelEvents();
                    CreateNodes();
					UpdateControl(true, true);
				}
			}
		}

        // Tahoma is the default font
        private static readonly Font _font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)), false);
        /// <summary>
        /// The font to render <see cref="TreeViewAdv"/> content in.
        /// </summary>
        [Category("Appearance"), Description("The font to render TreeViewAdv content in.")]
        public override Font Font
        {
            get
            {
                return (base.Font);
            }
            set
            {
                if (value == null)
                    base.Font = _font;
                else
                {
                    if (value == DefaultFont)
                        base.Font = _font;
                    else
                        base.Font = value;
                }
            }
        }
        public override void ResetFont()
        {
            Font = null;
        }
        private bool ShouldSerializeFont()
        {
            return (!Font.Equals(_font));
        }
        // End font property

		private BorderStyle _borderStyle = BorderStyle.Fixed3D;
		[DefaultValue(BorderStyle.Fixed3D), Category("Appearance")]
		public BorderStyle BorderStyle
		{
			get
			{
				return this._borderStyle;
			}
			set
			{
				if (_borderStyle != value)
				{
					_borderStyle = value;
					base.UpdateStyles();
				}
			}
		}

		private bool _autoRowHeight;
		/// <summary>
		/// Set to true to expand each row's height to fit the text of it's largest column.
		/// </summary>
		[DefaultValue(false), Category("Appearance"), Description("Expand each row's height to fit the text of it's largest column.")]
		public bool AutoRowHeight
		{
			get
			{
				return _autoRowHeight;
			}
			set
			{
				_autoRowHeight = value;
				if (value)
					_rowLayout = new AutoRowHeightLayout(this, RowHeight);
				else
					_rowLayout = new FixedRowHeightLayout(this, RowHeight);
				UpdateControl(true, true);
			}
		}

        private GridLineStyle _gridLineStyle = GridLineStyle.None;
        [DefaultValue(GridLineStyle.None), Category("Appearance")]
        public GridLineStyle GridLineStyle
        {
            get
            {
                return _gridLineStyle;
            }
            set
            {
				if (value != _gridLineStyle)
				{
					_gridLineStyle = value;
					UpdateView();
					OnGridLineStyleChanged();
				}
            }
        }

		private int _rowHeight = 16;
		[DefaultValue(16), Category("Appearance")]
		public int RowHeight
		{
			get
			{
				return _rowHeight;
			}
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException("value");

				_rowHeight = value;
				_rowLayout.PreferredRowHeight = value;
				UpdateControl(true, true);
			}
		}

		private TreeSelectionMode _selectionMode = TreeSelectionMode.Single;
		[DefaultValue(TreeSelectionMode.Single), Category("Behavior")]
		public TreeSelectionMode SelectionMode
		{
			get { return _selectionMode; }
			set { _selectionMode = value; }
		}

		private bool _hideSelection;
		[DefaultValue(false), Category("Behavior")]
		public bool HideSelection
		{
			get { return _hideSelection; }
			set
			{
				_hideSelection = value;
				UpdateView();
			}
		}

		private float _topEdgeSensivity = 0.3f;
		[DefaultValue(0.3f), Category("Behavior")]
		public float TopEdgeSensivity
		{
			get { return _topEdgeSensivity; }
			set
			{
				if (value < 0 || value > 1)
					throw new ArgumentOutOfRangeException();
				_topEdgeSensivity = value;
			}
		}

		private float _bottomEdgeSensivity = 0.3f;
		[DefaultValue(0.3f), Category("Behavior")]
		public float BottomEdgeSensivity
		{
			get { return _bottomEdgeSensivity; }
			set
			{
				if (value < 0 || value > 1)
					throw new ArgumentOutOfRangeException("value should be from 0 to 1");
				_bottomEdgeSensivity = value;
			}
		}

	    [DefaultValue(false), Category("Behavior")]
	    public bool LoadOnDemand { get; set; }

	    [DefaultValue(false), Category("Behavior")]
	    public bool UnloadCollapsedOnReload { get; set; }

	    private int _indent = 7;
		[DefaultValue(19), Category("Behavior")]
		public int Indent
		{
			get { return _indent; }
			set
			{
				_indent = value;
				UpdateView();
			}
		}

		private Color _lineColor = SystemColors.ControlDark;
		[Category("Behavior")]
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				_lineColor = value;
				CreateLinePen();
				UpdateView();
			}
		}

		private Color _dragDropMarkColor = Color.Black;
		[Category("Behavior")]
		public Color DragDropMarkColor
		{
			get { return _dragDropMarkColor; }
			set
			{
				_dragDropMarkColor = value;
				CreateMarkPen();
			}
		}

		private float _dragDropMarkWidth = 3.0f;
		[DefaultValue(3.0f), Category("Behavior")]
		public float DragDropMarkWidth
		{
			get { return _dragDropMarkWidth; }
			set
			{
				_dragDropMarkWidth = value;
				CreateMarkPen();
			}
		}

		private bool _highlightDropPosition = true;
		[DefaultValue(true), Category("Behavior")]
		public bool HighlightDropPosition
		{
			get { return _highlightDropPosition; }
			set { _highlightDropPosition = value; }
		}

		private readonly TreeColumnCollection _columns;
		[Category("Behavior"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public Collection<TreeColumn> Columns
		{
			get { return _columns; }
		}

		private readonly NodeControlsCollection _controls;
		[Category("Behavior"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Editor(typeof(NodeControlCollectionEditor), typeof(UITypeEditor))]
		public Collection<NodeControl> NodeControls
		{
			get
			{
				return _controls;
			}
		}

	    /// <summary>
	    /// When set to true, node contents will be read in background thread.
	    /// </summary>
	    [Category("Behavior"), DefaultValue(false), Description("Read children in a background thread when expanding.")]
	    public bool AsyncExpanding { get; set; }

	    #endregion

		#region RunTime

	    [Browsable(false)]
	    public IToolTipProvider DefaultToolTipProvider { get; set; }

	    [Browsable(false)]
		public IEnumerable<Node> AllNodes
		{
			get
			{
				if (Model.Root.Nodes.Count > 0)
				{
                    var node = Model.Root.Nodes[0];
					while (node != null)
					{
						yield return node;
						if (node.Nodes.Count > 0)
							node = node.Nodes[0];
						else if (node.NextNode != null)
							node = node.NextNode;
						else
							node = node.BottomNode;
					}
				}
			}
		}

		private DropPosition _dropPosition;
		[Browsable(false)]
		public DropPosition DropPosition
		{
			get { return _dropPosition; }
			set { _dropPosition = value; }
		}

		[Browsable(false)]
        public ReadOnlyCollection<Node> SelectedNodes
		{
			get
			{
                return new ReadOnlyCollection<Node>(_selection);
			}
		}

		[Browsable(false)]
		public Node SelectedNode
		{
			get
			{
			    if (Selection.Count > 0)
				{
				    if (CurrentNode != null && CurrentNode.IsSelected)
						return CurrentNode;
				    return Selection[0];
				}
			    return null;
			}
		    set
		    {
		        SelectNode(value);
			}
		}

	    [Browsable(false)]
	    public Node CurrentNode { get; internal set; }

		/// <summary>
		/// Indicates the distance the content is scrolled to the left
		/// </summary>
		[Browsable(false)]
		public int HorizontalScrollPosition
		{
			get
			{
			    if (_hScrollBar.Visible)
					return _hScrollBar.Value;
			    return 0;
			}
		}

		#endregion

		#endregion

	}
}
