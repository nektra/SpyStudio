using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpyStudio.Tools
{
    class ContextMenuTools
    {
        public static ToolStripSeparator AddSeparatorIfNotEmpty(ContextMenuStrip menu)
        {
            ToolStripSeparator sep = null;
            if (menu.Items.Count != 0)
            {
                if (menu.Items[menu.Items.Count - 1].GetType() != typeof (ToolStripSeparator))
                {
                    sep = new ToolStripSeparator();
                    menu.Items.Add(sep);
                }
            }
            return sep;
        }
    }
}
