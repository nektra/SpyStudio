using System;
using System.Collections.Generic;
using System.Text;
using Aga.Controls.Tree.NodeControls;
using System.Drawing;

namespace Aga.Controls.Tree
{
	public struct NodeControlInfo
	{
		public static readonly NodeControlInfo Empty = new NodeControlInfo(null, Rectangle.Empty, null);

		private readonly NodeControl _control;
		public NodeControl Control
		{
			get { return _control; }
		}

		private readonly Rectangle _bounds;
		public Rectangle Bounds
		{
			get { return _bounds; }
		}

		private readonly TreeNodeAdv _treeNode;
		public TreeNodeAdv TreeNode
		{
			get { return _treeNode; }
		}

		public NodeControlInfo(NodeControl control, Rectangle bounds, TreeNodeAdv treeNode)
		{
			_control = control;
			_bounds = bounds;
			_treeNode = treeNode;
		}
	}
}
