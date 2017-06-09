using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace Aga.Controls.Tree.NodeControls
{
	public abstract class EditableControl : InteractiveControl
	{
		private Timer _timer;
		private bool _editFlag;
	    private bool _isEditing;

		#region Properties

		private bool _editOnClick = false;
		[DefaultValue(false)]
		public bool EditOnClick
		{
			get { return _editOnClick; }
			set { _editOnClick = value; }
		}

		#endregion

		protected EditableControl()
		{
		    _timer = new Timer {Interval = 500};
		    _timer.Tick += TimerTick;
		}

		private void TimerTick(object sender, EventArgs e)
		{
			_timer.Stop();
			if (_editFlag)
				BeginEdit();
			_editFlag = false;
		}

		public void SetEditorBounds(EditorContext context)
		{
			Size size = CalculateEditorSize(context);
			context.Editor.Bounds = new Rectangle(context.Bounds.X, context.Bounds.Y,
				Math.Min(size.Width, context.Bounds.Width),
				Math.Min(size.Height, Parent.ClientSize.Height - context.Bounds.Y)
			);
		}

		protected abstract Size CalculateEditorSize(EditorContext context);

		protected virtual bool CanEdit(TreeNodeAdv node)
		{
			return (node.Node != null) && IsEditEnabled(node.Node);
		}

        public bool IsEditing()
        {
            return _isEditing;
        }

		public void BeginEdit()
		{
            if(Parent == null || Parent.CurrentNode == null)
                return;

		    var treeNode = Parent.GetTreeNode(Parent.CurrentNode);
			if (treeNode != null && CanEdit(treeNode))
			{
				var args = new CancelEventArgs();
				OnEditorShowing(args);
				if (!args.Cancel)
				{
					var editor = CreateEditor(treeNode);
					Parent.DisplayEditor(editor, this);
				    _isEditing = true;
				}
			}
		}

		public void EndEdit(bool applyChanges)
		{
			if (Parent != null)
				if (Parent.HideEditor(applyChanges))
					OnEditorHided();
		    _isEditing = false;
		}

		public virtual void UpdateEditor(Control control)
		{
		}

		internal void ApplyChanges(TreeNodeAdv node, Control editor)
		{
			DoApplyChanges(node, editor);
			OnChangesApplied();
		}

		internal void DoDisposeEditor(Control editor)
		{
			DisposeEditor(editor);
		}

		protected abstract void DoApplyChanges(TreeNodeAdv node, Control editor);

		protected abstract Control CreateEditor(TreeNodeAdv node);

		protected abstract void DisposeEditor(Control editor);

		public virtual void Cut(Control control)
		{
		}

		public virtual void Copy(Control control)
		{
		}

		public virtual void Paste(Control control)
		{
		}

		public virtual void Delete(Control control)
		{
		}

		public override void MouseDown(TreeNodeAdvMouseEventArgs args)
		{
			_editFlag = (!EditOnClick && args.Button == MouseButtons.Left
				&& args.ModifierKeys == Keys.None && args.TreeNode.IsSelected);
		}

		public override void MouseUp(TreeNodeAdvMouseEventArgs args)
		{
			if (args.TreeNode.IsSelected)
			{
				if (EditOnClick && args.Button == MouseButtons.Left && args.ModifierKeys == Keys.None)
				{
					Parent.ItemDragMode = false;
					BeginEdit();
					args.Handled = true;
				}
				else if (_editFlag)// && args.Node.IsSelected)
					_timer.Start();
			}
		}

		public override void MouseDoubleClick(TreeNodeAdvMouseEventArgs args)
		{
			_editFlag = false;
			_timer.Stop();
		}

		protected override void Dispose(bool disposing)
		{
            if (disposing)
                _timer.Dispose();

            _timer.Tick -= TimerTick;
            _timer.Stop();

            base.Dispose(disposing);
		}

		#region Events

		public event CancelEventHandler EditorShowing;
		protected void OnEditorShowing(CancelEventArgs args)
		{
			if (EditorShowing != null)
				EditorShowing(this, args);
		}

		public event EventHandler EditorHided;
		protected void OnEditorHided()
		{
			if (EditorHided != null)
				EditorHided(this, EventArgs.Empty);
		}

		public event EventHandler ChangesApplied;
		protected void OnChangesApplied()
		{
			if (ChangesApplied != null)
				ChangesApplied(this, EventArgs.Empty);
		}

		#endregion
	}
}
