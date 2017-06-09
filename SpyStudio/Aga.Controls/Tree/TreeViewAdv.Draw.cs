using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Aga.Controls.Tree.Input;
using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
	public partial class TreeViewAdv
	{
        private int GetNodeWidth(TreeNodeAdv node)
        {
            if (node.RightBounds == null)
            {
                var res = GetNodeBounds(GetNodeControls(node, Rectangle.Empty));
                node.RightBounds = res.Right;
            }
            return node.RightBounds.Value;
        }
        internal Rectangle GetNodeBounds(TreeNodeAdv node)
        {
            return GetNodeBounds(GetNodeControls(node));
        }

        private Rectangle GetNodeBounds(IEnumerable<NodeControlInfo> nodeControls)
        {
            var res = Rectangle.Empty;
            foreach (var info in nodeControls)
            {
                res = res == Rectangle.Empty ? info.Bounds : Rectangle.Union(res, info.Bounds);
            }
            return res;
        }

        private int GetNodeTextWidthInColumn(TreeNodeAdv treeNode, TreeColumn column)
        {
            int w = 0;
            var controls = GetNodeControls(treeNode);
            foreach (var nc in controls)
            {
                if (column.Index == 0 && nc.Control is NodePlusMinus)
                {
                    w += nc.Bounds.Right;
                    //w += NodePlusMinus.Width + (node.Level - 1) * _indent;
                }
                if (nc.Control.ParentColumn == column)
                {
                    Font oldFont = null;

                    if (treeNode.Node.BoldSet && treeNode.Node.Bold)
                    {
                        oldFont = _measureContext.Font;
                        _measureContext.Font = new Font(_measureContext.Font, FontStyle.Bold);
                    }

                    w += nc.Control.GetActualSize(treeNode, _measureContext).Width;

                    if (oldFont != null)
                    {
                        _measureContext.Font = oldFont;
                    }
                }
            }
            return w;
        }
		public void AutoSizeColumn(TreeColumn column)
		{
			if (!Columns.Contains(column))
				throw new ArgumentException("column");

		    var context = new DrawContext {Graphics = Graphics.FromImage(new Bitmap(1, 1)), Font = Font};

		    var s = column.GetMinimumSizeToFitHeader(context.Graphics, Font);
            new Size(s.Width + LeftMargin, s.Height);

            int res = s.Width + LeftMargin;

            if (Model.Root.Nodes.Count == 0)
                return;

		    TreeNodeAdv treeNode = null;
            if(AutoSizeColumnsOnlyWithVisibleNodes)
            {
                if (_visibleNodes.Count == 0)
                    return;
                foreach (var treeNodePair in _visibleNodes)
                {
                    treeNode = treeNodePair.Value;
                    var w = GetNodeTextWidthInColumn(treeNode, column);
                    res = Math.Max(res, w);
                }
            }
            else
            {
                if (Model.Root.Nodes.Count == 0)
                    return;

                var node = Model.Root.Nodes[0];
                for (int row = 0; node != null; row++)
                {
                    treeNode = GetTempTreeNode(treeNode, node, true);
                    if(treeNode != null)
                    {
                        var w = GetNodeTextWidthInColumn(treeNode, column);
                        res = Math.Max(res, w);
                    }

                    node = GetNextVisibleNode(node);
                }
            }

			if (res > 0)
				column.Width = res;
		}

		private void CreatePens()
		{
			CreateLinePen();
			CreateMarkPen();
		}

		private void CreateMarkPen()
		{
			var path = new GraphicsPath();
			path.AddLines(new[] { new Point(0, 0), new Point(1, 1), new Point(-1, 1), new Point(0, 0) });
			var cap = new CustomLineCap(null, path);
			cap.WidthScale = 1.0f;

			_markPen = new Pen(_dragDropMarkColor, _dragDropMarkWidth);
			_markPen.CustomStartCap = cap;
			_markPen.CustomEndCap = cap;
		}

		private void CreateLinePen()
		{
			_linePen = new Pen(_lineColor);
			_linePen.DashStyle = DashStyle.Dot;
		}

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_suspendUpdate > 0)
                return;

            BeginPerformanceCount();
			PerformanceAnalyzer.Start("OnPaint");

            var context = new DrawContext {Graphics = e.Graphics, Font = Font, Enabled = Enabled};

            int y = 0;
            int gridHeight = 0;

            if (UseColumns)
            {
				DrawColumnHeaders(e.Graphics);
				y += ColumnHeaderHeight;
                if (Columns.Count == 0 || e.ClipRectangle.Height <= y)
                    return;
            }

			int firstRowY = _rowLayout.GetRowBounds(FirstVisibleRow).Y;
            y -= firstRowY;

            e.Graphics.ResetTransform();
            e.Graphics.TranslateTransform(-OffsetX, y);
            Rectangle displayRect = DisplayRectangle;
            for (int row = _firstVisibleRow; row <= _lastVisibleNodesRow; row++)
            {
                Rectangle rowRect = _rowLayout.GetRowBounds(row);
                gridHeight += rowRect.Height;
                if (rowRect.Y + y > displayRect.Bottom)
                    break;
                else
                    DrawRow(e, ref context, row, rowRect);
            }

			if ((GridLineStyle & GridLineStyle.Vertical) == GridLineStyle.Vertical && UseColumns)
				DrawVerticalGridLines(e.Graphics, firstRowY);

			if (_dropPosition.Node != null && DragMode && HighlightDropPosition)
                DrawDropMark(e.Graphics);

            e.Graphics.ResetTransform();
            DrawScrollBarsBox(e.Graphics);

            if (DragMode && _dragBitmap != null)
                e.Graphics.DrawImage(_dragBitmap, PointToClient(MousePosition));

			PerformanceAnalyzer.Finish("OnPaint");
			EndPerformanceCount(e);
        }

		private void DrawRow(PaintEventArgs e, ref DrawContext context, int row, Rectangle rowRect)
		{
			TreeNodeAdv treeNode = GetTreeNodeByRow(row);
            Debug.Assert(treeNode != null);
			context.DrawSelection = DrawSelectionMode.None;
			context.CurrentEditorOwner = CurrentEditorOwner;
			if (DragMode)
			{
				if ((_dropPosition.Node == treeNode) && _dropPosition.Position == NodePosition.Inside && HighlightDropPosition)
					context.DrawSelection = DrawSelectionMode.Active;
			}
			else
			{
				if (treeNode.Node.IsSelected && Focused)
					context.DrawSelection = DrawSelectionMode.Active;
				else if (treeNode.Node.IsSelected && !Focused && !HideSelection)
					context.DrawSelection = DrawSelectionMode.Inactive;
			}
			context.DrawFocus = Focused && CurrentNode == treeNode.Node;
			
			OnRowDraw(e, treeNode, context, row, rowRect);

			if (FullRowSelect)
			{
				context.DrawFocus = false;
				if (context.DrawSelection == DrawSelectionMode.Active || context.DrawSelection == DrawSelectionMode.Inactive)
				{
					Rectangle focusRect = new Rectangle(OffsetX, rowRect.Y, ClientRectangle.Width, rowRect.Height);
                    if (context.DrawSelection == DrawSelectionMode.Active || 
                        (context.DrawSelection == DrawSelectionMode.Inactive && !this.HideSelection))
					{
						e.Graphics.FillRectangle(SystemBrushes.Highlight, focusRect);
						context.DrawSelection = DrawSelectionMode.FullRowSelect;
					}
					else
					{
						e.Graphics.FillRectangle(SystemBrushes.InactiveBorder, focusRect);
						context.DrawSelection = DrawSelectionMode.None;
					}
				}
                else
                {
                    Rectangle focusRect = new Rectangle(OffsetX, rowRect.Y, ClientRectangle.Width, rowRect.Height);
                    Node n = (Node)treeNode.Node;
                    if (n.Brush != null)
                    {
                        e.Graphics.FillRectangle(n.Brush, focusRect);
                    }
                }
            }

            if ((GridLineStyle & GridLineStyle.Horizontal) == GridLineStyle.Horizontal)
				e.Graphics.DrawLine(SystemPens.InactiveBorder, 0, rowRect.Bottom, e.Graphics.ClipBounds.Right, rowRect.Bottom);

			if (ShowLines)
				DrawLines(e.Graphics, treeNode, rowRect);

			DrawNode(treeNode, context);
		}

		private void DrawVerticalGridLines(Graphics gr, int y)
		{
			int x = 0;
			foreach (TreeColumn c in Columns)
			{
				if (c.IsVisible)
				{
					x += c.Width;
					gr.DrawLine(SystemPens.InactiveBorder, x - 1, y, x - 1, gr.ClipBounds.Bottom);
				}
			}
		}

		private void DrawColumnHeaders(Graphics gr)
		{
			PerformanceAnalyzer.Start("DrawColumnHeaders");
			ReorderColumnState reorder = Input as ReorderColumnState;
			int x = 0;
            TreeColumn.DrawBackground(gr, new Rectangle(0, 0, ClientRectangle.Width + 2, ColumnHeaderHeight - 1), false, false);
			gr.TranslateTransform(-OffsetX, 0);
			foreach (TreeColumn c in Columns)
			{
				if (c.IsVisible)
				{
					if (x >= OffsetX && x - OffsetX < this.Bounds.Width)// skip invisible columns
					{
                        Rectangle rect = new Rectangle(x, 0, c.Width, ColumnHeaderHeight - 1);
                        gr.SetClip(rect);
						bool pressed = ((Input is ClickColumnState || reorder != null) && ((Input as ColumnState).Column == c));
						c.Draw(gr, rect, Font, pressed, _hotColumn == c);
						gr.ResetClip();

						if (reorder != null && reorder.DropColumn == c)
							TreeColumn.DrawDropMark(gr, rect);
					}
					x += c.Width;
				}
			}

			if (reorder != null)
			{
				if (reorder.DropColumn == null)
					TreeColumn.DrawDropMark(gr, new Rectangle(x, 0, 0, ColumnHeaderHeight));
				gr.DrawImage(reorder.GhostImage, new Point(reorder.Location.X +  + reorder.DragOffset, reorder.Location.Y));
			}
			PerformanceAnalyzer.Finish("DrawColumnHeaders");
		}

		public void DrawNode(TreeNodeAdv node, DrawContext context)
		{
			foreach (NodeControlInfo item in GetNodeControls(node))
			{
				if (item.Bounds.Right >= OffsetX && item.Bounds.X - OffsetX < this.Bounds.Width)// skip invisible nodes
				{
					context.Bounds = item.Bounds;
					context.Graphics.SetClip(context.Bounds);
					item.Control.Draw(node, context);
					context.Graphics.ResetClip();
				}
			}
		}

		private void DrawScrollBarsBox(Graphics gr)
		{
			Rectangle r1 = DisplayRectangle;
			Rectangle r2 = ClientRectangle;
			gr.FillRectangle(SystemBrushes.Control,
				new Rectangle(r1.Right, r1.Bottom, r2.Width - r1.Width, r2.Height - r1.Height));
		}

		private void DrawDropMark(Graphics gr)
		{
			if (_dropPosition.Position == NodePosition.Inside)
				return;

			Rectangle rect = GetNodeBounds(_dropPosition.Node);
			int right = DisplayRectangle.Right - LeftMargin + OffsetX;
			int y = rect.Y;
			if (_dropPosition.Position == NodePosition.After)
				y = rect.Bottom;
			gr.DrawLine(_markPen, rect.X, y, right, y);
		}

		private void DrawLines(Graphics gr, TreeNodeAdv node, Rectangle rowRect)
		{
			if (UseColumns && Columns.Count > 0)
				gr.SetClip(new Rectangle(0, rowRect.Y, Columns[0].Width, rowRect.Bottom));

			TreeNodeAdv curNode = node;
			while (curNode != null)
			{
				int level = curNode.Level;
				int x = (level - 1) * _indent + NodePlusMinus.ImageSize / 2 + LeftMargin;
				int width = NodePlusMinus.Width - NodePlusMinus.ImageSize / 2;
				int y = rowRect.Y;
				int y2 = y + rowRect.Height;

				if (curNode == node)
				{
					int midy = y + rowRect.Height / 2;
					gr.DrawLine(_linePen, x, midy, x + width, midy);
					if (curNode.Node.NextNode == null)
						y2 = y + rowRect.Height / 2;
				}

				if (node.Row == 0)
					y = rowRect.Height / 2;
                if (curNode.Node.NextNode != null || curNode == node)
					gr.DrawLine(_linePen, x, y, x, y2);

			    curNode = GetTreeNode(curNode.Node.Parent);
			}

			gr.ResetClip();
		}

		#region Performance

		private double _totalTime;
		private int _paintCount;

		[Conditional("PERF_TEST")]
		private void BeginPerformanceCount()
		{
			_paintCount++;
			TimeCounter.Start();
		}

		[Conditional("PERF_TEST")]
		private void EndPerformanceCount(PaintEventArgs e)
		{
			double time = TimeCounter.Finish();
			_totalTime += time;
			string debugText = string.Format("FPS {0:0.0}; Avg. FPS {1:0.0}",
				1 / time, 1 / (_totalTime / _paintCount));
			e.Graphics.FillRectangle(Brushes.White, new Rectangle(DisplayRectangle.Width - 150, DisplayRectangle.Height - 20, 150, 20));
			e.Graphics.DrawString(debugText, Control.DefaultFont, Brushes.Gray,
				new PointF(DisplayRectangle.Width - 150, DisplayRectangle.Height - 20));
		}
		#endregion

	}
}
