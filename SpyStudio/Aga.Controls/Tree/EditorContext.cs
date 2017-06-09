using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
	public struct EditorContext
	{
	    public TreeNodeAdv CurrentNode { get; set; }

	    public Control Editor { get; set; }

	    public NodeControl Owner { get; set; }

	    public Rectangle Bounds { get; set; }

	    public DrawContext DrawContext { get; set; }
	}
}
