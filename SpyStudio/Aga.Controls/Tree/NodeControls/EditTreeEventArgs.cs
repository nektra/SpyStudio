using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Aga.Controls.Tree.NodeControls
{
	public class EditTreeEventArgs : NodeTreeEventArgs
	{
		private Control _control;
		public Control Control
		{
			get { return _control; }
		}

		public EditTreeEventArgs(TreeNodeAdv node, Control control)
			: base(node)
		{
			_control = control;
		}
	}
}
