using System;

namespace Aga.Controls.Tree
{
	[Serializable]
	public sealed class TreeNodeAdv //: ISerializable
	{
		#region Properties

		private TreeViewAdv _tree;
		public TreeViewAdv Tree
		{
		    get { return _tree; }
            set { _tree = value; }
		}

	    private int _row;
		public int Row
		{
			get { return _row; }
			internal set { _row = value; }
		}

        /// <summary>
        /// Use this property when you want to create a data object to make the control virtual.
        /// </summary>
        public object DataObject { get; set; }

	    public bool IsSelected
	    {
	        get { return Node.IsSelected; }
	    }
		public bool IsLeaf
		{
            get { return Node.IsLeaf; }
        }

		public bool IsExpanded
		{
			get { return Node.IsExpanded; }
		}

        public int Level { get; set; }

        public bool CanExpand
        {
            get
            {
                return Node.CanExpand;
            }
        }

		private Node _node;
		public Node Node
		{
			get { return _node; }
            internal set { _node = value; }
		}
		private int? _rightBounds;
		internal int? RightBounds
		{
			get { return _rightBounds; }
			set { _rightBounds = value; }
		}

		private int? _height;
		internal int? Height
		{
			get { return _height; }
			set { _height = value; }
		}

		private bool _isExpandingNow;
		internal bool IsExpandingNow
		{
			get { return _isExpandingNow; }
			set { _isExpandingNow = value; }
		}

		private bool _autoExpandOnStructureChanged;
		public bool AutoExpandOnStructureChanged
		{
			get { return _autoExpandOnStructureChanged; }
			set { _autoExpandOnStructureChanged = value; }
		}

		#endregion

		public TreeNodeAdv(Node node)
			: this(null, node)
		{
		}

		internal TreeNodeAdv(TreeViewAdv tree, Node node)
		{
			_row = -1;
			_tree = tree;
			_node = node;
		}

		public override string ToString()
		{
			return Node.ToString();
		}

		#region ISerializable Members

        //private TreeNodeAdv(SerializationInfo info, StreamingContext context)
        //    : this(null, null)
        //{
            //var nodesCount = 0;
            //nodesCount = info.GetInt32("NodesCount");
            //_isExpanded = info.GetBoolean("IsExpanded");
            //_node = info.GetValue("Tag", typeof(object));

            //for (var i = 0; i < nodesCount; i++)
            //{
            //    var child = (TreeNodeAdv)info.GetValue("Child" + i, typeof(TreeNodeAdv));
            //    Nodes.Add(child);
            //}

        //}

        //[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.AddValue("IsExpanded", IsExpanded);
        //    info.AddValue("NodesCount", Nodes.Count);
        //    if ((Node != null) && Node.GetType().IsSerializable)
        //        info.AddValue("Tag", Node, Node.GetType());

        //    for (int i = 0; i < Nodes.Count; i++)
        //        info.AddValue("Child" + i, Nodes[i], typeof(TreeNodeAdv));

        //}

		#endregion
	}
}
