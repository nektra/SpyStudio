using System;
using System.Collections.Generic;
using System.Text;

namespace Aga.Controls.Tree
{
	public class TreeModelEventArgs: TreePathEventArgs
	{
		private readonly object[] _children;
		public object[] Children
		{
			get { return _children; }
		}

		private readonly int[] _indices;
		public int[] Indices
		{
			get { return _indices; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent">Path to a parent node</param>
		/// <param name="children">Child nodes</param>
		public TreeModelEventArgs(TreePath parent, object[] children)
			: this(parent, null, children)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent">Path to a parent node</param>
		/// <param name="indices">Indices of children in parent nodes collection</param>
		/// <param name="children">Child nodes</param>
		public TreeModelEventArgs(TreePath parent, int[] indices, object[] children)
			: base(parent)
		{
			if (children == null)
				throw new ArgumentNullException();

			if (indices != null && indices.Length != children.Length)
				throw new ArgumentException("indices and children arrays must have the same length");

			_indices = indices;
			_children = children;
		}
	}
    public class TreeModelNodesEventArgs : EventArgs
    {
        public TreeModelNodesEventArgs(IEnumerable<Node> nodes)
        {
            Nodes = nodes;
        }

        public IEnumerable<Node> Nodes { get; set; }
    }
    public class ReachableNodeInsertedEventArgs : TreeModelNodeEventArgs
    {
        public ReachableNodeInsertedEventArgs(int row, Node node)
            : base(node)
        {
            Row = row;
        }

        public int Row { get; set; }
    }
    public class TreeModelNodeEventArgs : EventArgs
    {
        public TreeModelNodeEventArgs(Node node)
        {
            Node = node;
        }

        public Node Node { get; set; }
    }
    public class TreeModelNodesRemovedEventArgs : EventArgs
    {
        public TreeModelNodesRemovedEventArgs(Node parent, int index, IEnumerable<Node> nodes)
        {
            Parent = parent;
            Index = index;
            Nodes = nodes;
        }

        public IEnumerable<Node> Nodes { get; set; }
        public Node Parent { get; set; }
        public int Index { get; set; }
    }
    public class TreeModelNodeRemovedEventArgs : EventArgs
    {
        public TreeModelNodeRemovedEventArgs(Node parent, int index, Node node)
        {
            Parent = parent;
            Index = index;
            Node = node;
        }

        public Node Node { get; set; }
        public Node Parent { get; set; }
        public int Index { get; set; }
    }
    public class TreeModelExpandEventArgs : TreeModelNodeEventArgs
    {
        public TreeModelExpandEventArgs(Node node, bool expand, bool ignoreChildren)
            : base(node)
        {
            Expand = expand;
            IgnoreChildren = ignoreChildren;
        }

        public bool Expand { get; set; }
        public bool IgnoreChildren { get; set; }
    }
}
