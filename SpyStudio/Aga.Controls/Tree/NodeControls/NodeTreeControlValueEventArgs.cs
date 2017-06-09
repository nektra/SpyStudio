using System;
using System.Collections.Generic;
using System.Text;

namespace Aga.Controls.Tree.NodeControls
{
	public class NodeTreeControlValueEventArgs : NodeTreeEventArgs
	{
	    public object Value { get; set; }

	    public NodeTreeControlValueEventArgs(TreeNodeAdv node)
			:base(node)
		{
		}
	}

	public class NodeControlValueEventArgs : NodeEventArgs
	{
	    public object Value { get; set; }

	    public NodeControlValueEventArgs(Node node)
			:base(node)
		{
		}
	}
    
}
