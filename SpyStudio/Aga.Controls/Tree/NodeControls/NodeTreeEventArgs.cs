using System;
using System.Collections.Generic;
using System.Text;

namespace Aga.Controls.Tree.NodeControls
{
	public class NodeTreeEventArgs : EventArgs
	{
		private TreeNodeAdv _node;
		public TreeNodeAdv Node
		{
			get { return _node; }
		}

		public NodeTreeEventArgs(TreeNodeAdv node)
		{
			_node = node;
		}
	}

    public class NodeEventArgs : EventArgs
    {
        private readonly Node _node;
        public Node Node
        {
            get { return _node; }
        }

        public NodeEventArgs(Node node)
        {
            _node = node;
        }
    }
}
