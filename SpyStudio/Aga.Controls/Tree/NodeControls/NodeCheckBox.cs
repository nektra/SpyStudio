using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Aga.Controls.Properties;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.ComponentModel;

namespace Aga.Controls.Tree.NodeControls
{
	public class NodeCheckBox : InteractiveControl
	{
		public const int ImageSize = 13;

		private readonly Bitmap _check;
		private readonly Bitmap _uncheck;
		private readonly Bitmap _unknown;
	    public string ThreeStatePropertyName = null;

		#region Properties

		private bool _threeState;
		[DefaultValue(false)]
		public bool ThreeState
		{
			get { return _threeState; }
			set { _threeState = value; }
		}

		#endregion

		public NodeCheckBox()
			: this(string.Empty)
		{
		}

		public NodeCheckBox(string propertyName)
		{
			_check = Resources.check;
			_uncheck = Resources.uncheck;
			_unknown = Resources.unknown;
			DataPropertyName = propertyName;
			LeftMargin = 0;
		}

		public override Size MeasureSize(TreeNodeAdv node, DrawContext context)
		{
			return new Size(ImageSize, ImageSize);
		}

		public override void Draw(TreeNodeAdv node, DrawContext context)
		{
			Rectangle bounds = GetBounds(node, context);
			CheckState state = GetCheckState(node);
			if (Application.RenderWithVisualStyles)
			{
				VisualStyleRenderer renderer;
				if (state == CheckState.Indeterminate)
					renderer = new VisualStyleRenderer(VisualStyleElement.Button.CheckBox.MixedNormal);
				else if (state == CheckState.Checked)
					renderer = new VisualStyleRenderer(VisualStyleElement.Button.CheckBox.CheckedNormal);
				else
					renderer = new VisualStyleRenderer(VisualStyleElement.Button.CheckBox.UncheckedNormal);
				renderer.DrawBackground(context.Graphics, new Rectangle(bounds.X, bounds.Y, ImageSize, ImageSize));
			}
			else
			{
				Image img;
				if (state == CheckState.Indeterminate)
					img = _unknown;
				else if (state == CheckState.Checked)
					img = _check;
				else
					img = _uncheck;
				context.Graphics.DrawImage(img, bounds.Location);
			}
		}

	    protected bool GetThreeState(TreeNodeAdv node)
	    {
	        if (string.IsNullOrEmpty(ThreeStatePropertyName))
	            return false;
	        try
	        {
	            var type = node.Node.GetType();
	            var prop = type.GetProperty(ThreeStatePropertyName);
	            if (prop != null)
	                return (bool)prop.GetValue(node.Node, null);
	            var field = type.GetField(ThreeStatePropertyName);
	            if (field != null)
	                return (bool) field.GetValue(node.Node);
	        }
	        catch
	        {
	        }
            return false;
        }

		protected virtual CheckState GetCheckState(TreeNodeAdv node)
		{
			object obj = GetValue(node);
			if (obj is CheckState)
				return (CheckState)obj;
		    if (obj is bool)
		        return (bool)obj ? CheckState.Checked : CheckState.Unchecked;
		    return CheckState.Unchecked;
		}

        protected virtual CheckState GetCheckState(Node node)
        {
            return node.CheckState;
        }

        protected virtual void SetCheckState(TreeNodeAdv node, CheckState value)
		{
			if (VirtualMode)
			{
				SetValue(node, value);
                NotifyCheckStateChanged(node.Node);
			}
			else
			{
				Type type = GetPropertyType(node);
				if (type == typeof(CheckState))
				{
					SetValue(node, value);
                    NotifyCheckStateChanged(node.Node);
				}
				else if (type == typeof(bool))
				{
					SetValue(node, value != CheckState.Unchecked);
                    NotifyCheckStateChanged(node.Node);
				}
			}
		}

		public override void MouseDown(TreeNodeAdvMouseEventArgs args)
		{
			if (args.Button == MouseButtons.Left && IsEditEnabled(args.TreeNode.Node))
			{
			    var context = new DrawContext {Bounds = args.ControlBounds};
			    Rectangle rect = GetBounds(args.TreeNode, context);
				if (rect.Contains(args.ViewLocation))
				{
					CheckState state = GetCheckState(args.TreeNode);
					state = GetNewState(state, GetThreeState(args.TreeNode));
                    args.TreeNode.Tree.NotifyUserRequestedCheckStateChange(args.TreeNode, state);
					SetCheckState(args.TreeNode, state);
					Parent.UpdateView();
					args.Handled = true;
				}
			}
		}

		public override void MouseDoubleClick(TreeNodeAdvMouseEventArgs args)
		{
			args.Handled = true;
		}

		private CheckState GetNewState(CheckState state, bool tristate)
		{
		    if (state == CheckState.Indeterminate)
				return CheckState.Unchecked;
		    if(state == CheckState.Unchecked)
		        return CheckState.Checked;
		    return ThreeState && tristate ? CheckState.Indeterminate : CheckState.Unchecked;
		}

	    public override void KeyDown(KeyEventArgs args)
		{
			if (args.KeyCode == Keys.Space && EditEnabled)
			{
				Parent.BeginUpdate();
				try
				{
					if (Parent.CurrentNode != null)
					{
					    var parentNode = Parent.CurrentNode;
						CheckState value = GetNewState(GetCheckState(parentNode), parentNode.ThreeState);
                        if(Parent.Selection.Count > 0)
                        {
                            var selection = new Node[Parent.Selection.Count];
                            Parent.Selection.CopyTo(selection);
                            foreach (var node in selection)
                                if (IsEditEnabled(node))
                                    node.CheckState = value;
                        }
					}
				}
				finally
				{
					Parent.EndUpdate();
				}
				args.Handled = true;
			}
		}

        public void NotifyCheckStateChanged(Node node)
        {
            OnCheckStateChanged(new TreeModelNodeEventArgs(node));
        }

        public event EventHandler<TreeModelNodeEventArgs> CheckStateChanged;
		protected void OnCheckStateChanged(TreeModelNodeEventArgs args)
		{
			if (CheckStateChanged != null)
				CheckStateChanged(this, args);
		}
	}
}
