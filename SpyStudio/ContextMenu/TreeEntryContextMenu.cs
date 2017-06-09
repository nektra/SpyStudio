using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SpyStudio.ContextMenu
{
    public class TreeEntryContextMenu : EntryContextMenu
    {
        private readonly ToolStripMenuItem _expandErrorsItem;
        private readonly ToolStripSeparator _expandErrorsSepItem;
        private readonly ITreeInterpreter _interpreter;

        public TreeEntryContextMenu(ITreeInterpreter interpreter)
            : base(interpreter, true)
        {
            _interpreter = interpreter;

            if (interpreter.ContextMenuStrip != null)
            {
                _expandErrorsSepItem = new ToolStripSeparator();
                _interpreter.ContextMenuStrip.Items.Insert(0, _expandErrorsSepItem);

                _expandErrorsItem = new ToolStripMenuItem("Expand Errors");
                _expandErrorsItem.Click += ExpandErrors;
                _interpreter.ContextMenuStrip.Items.Insert(0, _expandErrorsItem);
            }
        }

        private void ExpandErrors(object sender, EventArgs e)
        {
            IEntry entry = null;
            if (CallInterpreter.SelectedEntries.Count() != 0)
            {
                entry = CallInterpreter.SelectedEntries.First();
            }
            _interpreter.ExpandAllErrors(entry);
        }
    }
}
