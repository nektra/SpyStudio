using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aga.Controls.Tree.NodeControls
{
    public class NodeBar : BindableControl
    {
        public class NodeBarProperties
        {
            public Color Color { get; set; }
            public int BarSize { get; set; }
        }
        public NodeBar()
        {
            MaxBarSize = 5;
        }
        public override Size MeasureSize(TreeNodeAdv node, DrawContext context)
        {
            return new Size(50, 10);
        }

        public override void Draw(TreeNodeAdv node, DrawContext context)
        {
            var nodeProperties = GetBarProperties(node);
            var brush = new SolidBrush(nodeProperties.Color);
            var pen = new Pen(ControlPaint.Dark(nodeProperties.Color));

            var bounds = GetBounds(node, context);
            bounds.Width--;
            // this width must be the same width of a node which width is MaxBarSize
            bounds.Width = (bounds.Width/MaxBarSize)*MaxBarSize;
            var r = new Rectangle(bounds.X, bounds.Y, bounds.Width/MaxBarSize*nodeProperties.BarSize, bounds.Height);

            context.Graphics.FillRectangle(brush, r);
            context.Graphics.DrawRectangle(pen, bounds);
        }

        protected virtual NodeBarProperties GetBarProperties(TreeNodeAdv node)
        {
            return GetValue(node) as NodeBarProperties;
        }

        public int MaxBarSize { get; set; }
    }
}
