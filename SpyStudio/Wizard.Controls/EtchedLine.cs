using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Wizard.Controls
{
   public partial class EtchedLine : UserControl
   {
      public EtchedLine()
      {
         // This call is required by the Windows.Forms Form Designer.
         InitializeComponent();

         // Avoid receiving the focus.
         SetStyle(ControlStyles.Selectable, false);
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         base.OnPaint(e);

         Brush lightBrush = new SolidBrush(_lightColor);
         Brush darkBrush = new SolidBrush(_darkColor);
         Pen lightPen = new Pen(lightBrush, 1);
         Pen darkPen = new Pen(darkBrush, 1);

         switch (Edge)
         {
            case EtchEdge.Top:
               e.Graphics.DrawLine(darkPen, 0, 0, Width, 0);
               e.Graphics.DrawLine(lightPen, 0, 1, Width, 1);
               break;
            case EtchEdge.Bottom:
               e.Graphics.DrawLine(darkPen, 0, Height - 2, Width, Height - 2);
               e.Graphics.DrawLine(lightPen, 0, Height - 1, Width, Height - 1);
               break;
         }
      }

      protected override void OnResize(EventArgs e)
      {
         base.OnResize (e);

         Refresh();
      }

      Color _darkColor = SystemColors.ControlDark;

      [Category("Appearance")]
      Color DarkColor
      {
         get { return _darkColor; }

         set
         {
            _darkColor = value;
            Refresh();
         }
      }

      Color _lightColor = SystemColors.ControlLightLight;

      [Category("Appearance")]
      Color LightColor
      {
         get { return _lightColor; }

         set
         {
            _lightColor = value;
            Refresh();
         }
      }

      EtchEdge _edge = EtchEdge.Top;

      [Category("Appearance")]
      public EtchEdge Edge
      {
         get { return _edge; }
         set
         {
            _edge = value;
            Refresh();
         }
      }
   }

   public enum EtchEdge
   {
      Top, Bottom
   }
}
