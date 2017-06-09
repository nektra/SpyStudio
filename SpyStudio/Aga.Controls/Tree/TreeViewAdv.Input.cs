using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using Aga.Controls.Tree.Input;
using Aga.Controls.Tree.NodeControls;
using System.Drawing.Imaging;

namespace Aga.Controls.Tree
{
	public partial class TreeViewAdv
	{
		#region Keys

		protected override bool IsInputChar(char charCode)
		{
			return true;
		}

		protected override bool IsInputKey(Keys keyData)
		{
		    if (((keyData & Keys.Up) == Keys.Up)
				|| ((keyData & Keys.Down) == Keys.Down)
				|| ((keyData & Keys.Left) == Keys.Left)
				|| ((keyData & Keys.Right) == Keys.Right))
				return true;
		    return base.IsInputKey(keyData);
		}

	    internal void ChangeInput()
		{
			if ((ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				if (!(Input is InputWithShift))
					Input = new InputWithShift(this);
			}
			else if ((ModifierKeys & Keys.Control) == Keys.Control)
			{
				if (!(Input is InputWithControl))
					Input = new InputWithControl(this);
			}
			else
			{
				if (Input.GetType() != typeof(NormalInputState))
					Input = new NormalInputState(this);
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!e.Handled)
			{
				if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey)
					ChangeInput();
				Input.KeyDown(e);
				if (!e.Handled && CurrentNode != null)
				{
				    var treeNode = GetTreeNode(CurrentNode);
                    if (treeNode != null)
                    {
                        var nodeControls = GetNodeControls(treeNode);
                        foreach (var item in nodeControls)
                        {
                            item.Control.KeyDown(e);
                            if (e.Handled)
                                break;
                        }
                    }
				}
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
            if (!e.Handled)
            {
                if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey)
                    ChangeInput();
                if (!e.Handled && CurrentNode != null)
                {
                    var treeNode = GetTreeNode(CurrentNode);
                    if (treeNode != null)
                    {
                        var nodeControls = GetNodeControls(treeNode);
                        foreach (var item in nodeControls)
                        {
                            item.Control.KeyUp(e);
                            if (e.Handled)
                                break;
                        }
                    }
                }
            }
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			if (!e.Handled)
				_search.Search(e.KeyChar);
		}

		#endregion

		#region Mouse

		private TreeNodeAdvMouseEventArgs CreateMouseArgs(MouseEventArgs e)
		{
		    var args = new TreeNodeAdvMouseEventArgs(e)
		                   {
		                       ViewLocation = new Point(e.X + OffsetX,
		                                                e.Y + _rowLayout.GetRowBounds(FirstVisibleRow).Y - ColumnHeaderHeight),
		                       ModifierKeys = ModifierKeys,
		                       TreeNode = GetNodeAt(e.Location)
		                   };
		    var info = GetNodeControlInfoAt(args.TreeNode, e.Location);
			args.ControlBounds = info.Bounds;
			args.Control = info.Control;
			return args;
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			_search.EndSearch();
			if (SystemInformation.MouseWheelScrollLines > 0)
			{
				var lines = e.Delta / 120 * SystemInformation.MouseWheelScrollLines;
				var newValue = _vScrollBar.Value - lines;
				newValue = Math.Min(_vScrollBar.Maximum - _vScrollBar.LargeChange + 1, newValue);
				newValue = Math.Min(_vScrollBar.Maximum, newValue);
				_vScrollBar.Value = Math.Max(_vScrollBar.Minimum, newValue);
			}
			base.OnMouseWheel(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (CurrentEditorOwner != null)
			{
				CurrentEditorOwner.EndEdit(true);
				return;
			}

			if (!Focused)
				Focus();

			_search.EndSearch();
			if (e.Button == MouseButtons.Left)
			{
			    var c = GetColumnDividerAt(e.Location);
				if (c != null)
				{
					Input = new ResizeColumnState(this, c, e.Location);
					return;
				}
				c = GetColumnAt(e.Location);
				if (c != null)
				{
					Input = new ClickColumnState(this, c, e.Location);
					UpdateView();
					return;
				}
			}

			ChangeInput();
			var args = CreateMouseArgs(e);

			if (args.TreeNode != null && args.Control != null)
				args.Control.MouseDown(args);

			if (!args.Handled)
				Input.MouseDown(args);

			base.OnMouseDown(e);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			//TODO: Disable when click on plusminus icon
			var args = CreateMouseArgs(e);
			if (args.TreeNode != null)
				OnNodeMouseClick(args);

			base.OnMouseClick(e);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			var args = CreateMouseArgs(e);

		    bool doubleClickOnNode = args.TreeNode != null && args.TreeNode.Row >= FirstVisibleRow;

			if (doubleClickOnNode && args.Control != null)
				args.Control.MouseDoubleClick(args);

			if (!args.Handled)
			{
                if (doubleClickOnNode)
					OnNodeMouseDoubleClick(args);
				else
					Input.MouseDoubleClick(args);

				if (!args.Handled)
				{
                    if (doubleClickOnNode && args.Button == MouseButtons.Left)
						args.TreeNode.Node.IsExpanded = !args.TreeNode.IsExpanded;
				}
			}

            if (doubleClickOnNode)
                base.OnMouseDoubleClick(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			
            var args = CreateMouseArgs(e);
			if (Input is ResizeColumnState)
				Input.MouseUp(args);
			else
			{
				if (args.TreeNode != null && args.Control != null)
					args.Control.MouseUp(args);
				if (!args.Handled)
					Input.MouseUp(args);

				base.OnMouseUp(e);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Input.MouseMove(e))
				return;

			base.OnMouseMove(e);
			SetCursor(e);
			UpdateToolTip(e);
			if (ItemDragMode && Dist(e.Location, ItemDragStart) > ItemDragSensivity
				&& CurrentNode != null && CurrentNode.IsSelected)
			{
				ItemDragMode = false;
				_toolTip.Active = false;
				OnItemDrag(e.Button, Selection.ToArray());
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_hotColumn = null;
			UpdateHeaders();
			base.OnMouseLeave(e);
		}

		private void SetCursor(MouseEventArgs e)
		{
		    var col = GetColumnDividerAt(e.Location);
			if (col == null)
				_innerCursor = null;
			else
			{
				_innerCursor = col.Width == 0 ? ResourceHelper.DVSplitCursor : Cursors.VSplit;
			}

			col = GetColumnAt(e.Location);
		    if (col == _hotColumn) return;
		    _hotColumn = col;
		    UpdateHeaders();
		}

		internal TreeColumn GetColumnAt(Point p)
		{
			if (p.Y > ColumnHeaderHeight)
				return null;

			var x = -OffsetX;
			foreach (var col in Columns)
			{
			    if (!col.IsVisible) continue;

			    var rect = new Rectangle(x, 0, col.Width, ColumnHeaderHeight);
			    x += col.Width;
			    if (rect.Contains(p))
			        return col;
			}
			return null;
		}

		internal int GetColumnX(TreeColumn column)
		{
			var x = -OffsetX;
			foreach (var col in Columns)
			{
			    if (!col.IsVisible) continue;

			    if (column == col)
			        return x;

			    x += col.Width;
			}
		    return x;
		}

		internal TreeColumn GetColumnDividerAt(Point p)
		{
			if (p.Y > ColumnHeaderHeight)
				return null;

			var x = -OffsetX;
			TreeColumn prevCol = null;
			Rectangle left;
			foreach (var col in Columns)
			{
			    if (!col.IsVisible) continue;

			    if (col.Width > 0)
			    {
			        left = new Rectangle(x, 0, DividerWidth / 2, ColumnHeaderHeight);
			        var right = new Rectangle(x + col.Width - (DividerWidth / 2), 0, DividerWidth / 2, ColumnHeaderHeight);
			        if (left.Contains(p) && prevCol != null)
			            return prevCol;
			        if (right.Contains(p))
			            return col;
			    }
			    prevCol = col;
			    x += col.Width;
			}

			left = new Rectangle(x, 0, DividerWidth / 2, ColumnHeaderHeight);
			if (left.Contains(p) && prevCol != null)
				return prevCol;

			return null;
		}

		TreeColumn _tooltipColumn;
		private void UpdateToolTip(MouseEventArgs e)
		{
			var col = GetColumnAt(e.Location);
			if (col != null)
			{
				if (col != _tooltipColumn)
					SetTooltip(col.TooltipText);
			}
			else
				DisplayNodesTooltip(e);
			_tooltipColumn = col;
		}

		TreeNodeAdv _hotNode;
		NodeControl _hotControl;
		private void DisplayNodesTooltip(MouseEventArgs e)
		{
			if (ShowNodeToolTips)
			{
				var args = CreateMouseArgs(e);
				if (args.TreeNode != null && args.Control != null)
				{
					if (args.TreeNode != _hotNode || args.Control != _hotControl)
						SetTooltip(GetNodeToolTip(args));
				}
				else
					_toolTip.SetToolTip(this, null);

				_hotControl = args.Control;
				_hotNode = args.TreeNode;
			}
			else
				_toolTip.SetToolTip(this, null);
		}

		private void SetTooltip(string text)
		{
			if (!String.IsNullOrEmpty(text))
			{
				_toolTip.Active = false;
				_toolTip.SetToolTip(this, text);
				_toolTip.Active = true;
			}
			else
				_toolTip.SetToolTip(this, null);
		}

		private string GetNodeToolTip(TreeNodeAdvMouseEventArgs args)
		{
			var msg = args.Control.GetToolTip(args.TreeNode);

			var btc = args.Control as BaseTextControl;
			if (btc != null && btc.DisplayHiddenContentInToolTip && String.IsNullOrEmpty(msg))
			{
				var ms = btc.GetActualSize(args.TreeNode, _measureContext);
				if (ms.Width > args.ControlBounds.Size.Width || ms.Height > args.ControlBounds.Size.Height
					|| args.ControlBounds.Right - OffsetX > DisplayRectangle.Width)
					msg = btc.GetLabel(args.TreeNode);
			}

			if (String.IsNullOrEmpty(msg) && DefaultToolTipProvider != null)
				msg = DefaultToolTipProvider.GetToolTip(args.TreeNode, args.Control);

			return msg;
		}

		#endregion

		#region DragDrop

		private bool _dragAutoScrollFlag;
		private Bitmap _dragBitmap;
		private System.Threading.Timer _dragTimer;

		private void StartDragTimer()
		{
			if (_dragTimer == null)
				_dragTimer = new System.Threading.Timer(DragTimerTick, null, 0, 100);
		}

		private void StopDragTimer()
		{
		    if (_dragTimer == null) return;

		    _dragTimer.Dispose();
		    _dragTimer = null;
		}

		private void SetDropPosition(Point pt)
		{
			var node = GetNodeAt(pt);
			OnDropNodeValidating(pt, ref node);
			_dropPosition.Node = node;
		    if (node == null) return;

		    var first = _rowLayout.GetRowBounds(FirstVisibleRow);
		    var bounds = _rowLayout.GetRowBounds(node.Row);
		    var pos = (pt.Y + first.Y - ColumnHeaderHeight - bounds.Y) / (float)bounds.Height;
		    if (pos < TopEdgeSensivity)
		        _dropPosition.Position = NodePosition.Before;
		    else if (pos > (1 - BottomEdgeSensivity))
		        _dropPosition.Position = NodePosition.After;
		    else
		        _dropPosition.Position = NodePosition.Inside;
		}

		private void DragTimerTick(object state)
		{
			_dragAutoScrollFlag = true;
		}

		private void DragAutoScroll()
		{
			_dragAutoScrollFlag = false;
			var pt = PointToClient(MousePosition);
			if (pt.Y < 20 && _vScrollBar.Value > 0)
				_vScrollBar.Value--;
			else if (pt.Y > Height - 20 && _vScrollBar.Value <= _vScrollBar.Maximum - _vScrollBar.LargeChange)
				_vScrollBar.Value++;
		}

		public void DoDragDropSelectedNodes(DragDropEffects allowedEffects)
		{
		    if (SelectedNodes.Count <= 0) return;

		    var nodes = new Node[SelectedNodes.Count];
		    SelectedNodes.CopyTo(nodes, 0);
		    DoDragDrop(nodes, allowedEffects);
		}

		private void CreateDragBitmap(IDataObject data)
		{
			if (UseColumns || !DisplayDraggingNodes)
				return;

			var nodes = data.GetData(typeof(TreeNodeAdv[])) as TreeNodeAdv[];
		    if (nodes == null || nodes.Length <= 0) return;

		    var rect = DisplayRectangle;
		    var bitmap = new Bitmap(rect.Width, rect.Height);
		    using (var gr = Graphics.FromImage(bitmap))
		    {
		        gr.Clear(BackColor);
		        var context = new DrawContext {Graphics = gr, Font = Font, Enabled = true};
		        var y = 0;
		        var maxWidth = 0;
		        foreach (var node in nodes)
		        {
		            if (node.Tree != this) continue;

		            var x = 0;
		            var height = _rowLayout.GetRowBounds(node.Row).Height;
		            foreach (var c in NodeControls)
		            {
		                var s = c.GetActualSize(node, context);
		                if (s.IsEmpty) continue;
		                var width = s.Width;
		                rect = new Rectangle(x, y, width, height);
		                x += (width + 1);
		                context.Bounds = rect;
		                c.Draw(node, context);
		            }
		            y += height;
		            maxWidth = Math.Max(maxWidth, x);
		        }

		        if (maxWidth > 0 && y > 0)
		        {
		            _dragBitmap = new Bitmap(maxWidth, y, PixelFormat.Format32bppArgb);
		            using (var tgr = Graphics.FromImage(_dragBitmap))
		                tgr.DrawImage(bitmap, Point.Empty);
		            BitmapHelper.SetAlphaChanelValue(_dragBitmap, 150);
		        }
		        else
		            _dragBitmap = null;
		    }
		}

		protected override void OnDragOver(DragEventArgs drgevent)
		{
			ItemDragMode = false;
			var pt = PointToClient(new Point(drgevent.X, drgevent.Y));
			if (_dragAutoScrollFlag)
				DragAutoScroll();
			SetDropPosition(pt);
			UpdateView();
			base.OnDragOver(drgevent);
		}

		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			_search.EndSearch();
			DragMode = true;
			CreateDragBitmap(drgevent.Data);
			base.OnDragEnter(drgevent);
		}

		protected override void OnDragLeave(EventArgs e)
		{
			DragMode = false;
			UpdateView();
			base.OnDragLeave(e);
		}

		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			DragMode = false;
			UpdateView();
			base.OnDragDrop(drgevent);
		}

		#endregion
	}
}
