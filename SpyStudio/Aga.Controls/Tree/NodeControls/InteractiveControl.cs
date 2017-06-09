using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.ComponentModel;

namespace Aga.Controls.Tree.NodeControls
{
	public abstract class InteractiveControl : BindableControl
	{
	    public InteractiveControl()
	    {
	        EditEnabled = false;
	    }

	    [DefaultValue(false)]
	    public bool EditEnabled { get; set; }

	    protected bool IsEditEnabled(Node node)
		{
			if (EditEnabled)
			{
                var args = new NodeControlValueEventArgs(node);
				args.Value = true;
				OnIsEditEnabledValueNeeded(args);
                return Convert.ToBoolean(args.Value, CultureInfo.InvariantCulture);
			}
			else
				return false;
		}

        public event EventHandler<NodeControlValueEventArgs> IsEditEnabledValueNeeded;
        private void OnIsEditEnabledValueNeeded(NodeControlValueEventArgs args)
		{
			if (IsEditEnabledValueNeeded != null)
				IsEditEnabledValueNeeded(this, args);
		}
	}
}
