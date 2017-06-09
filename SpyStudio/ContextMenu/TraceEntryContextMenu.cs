using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SpyStudio.ContextMenu
{
    public class TraceEntryContextMenu : EntryContextMenu
    {
        private readonly ToolStripMenuItem _showInComItem,
                                           _showInWindowsItem,
                                           _showInFileSystemItem,
                                           _showInRegistryItem;
        private readonly List<ToolStripItem> _showInItems = new List<ToolStripItem>();
        private readonly ToolStripSeparator _showInSepItem;
        private readonly IInterpreterController _interpreterController;
        private readonly IInterpreter _interpreter;

        public TraceEntryContextMenu(IInterpreter interpreter, IInterpreterController interpreterController)
            : base(interpreter, true)
        {
            _interpreterController = interpreterController;
            _interpreter = interpreter;

            if (interpreter.ContextMenuStrip != null)
            {
                _showInComItem = new ToolStripMenuItem("Show in COM Objects");
                _showInComItem.Click += ShowInCom;
                _showInItems.Add(_showInComItem);
                _showInWindowsItem = new ToolStripMenuItem("Show in Windows");
                _showInWindowsItem.Click += ShowInWindows;
                _showInItems.Add(_showInWindowsItem);
                _showInFileSystemItem = new ToolStripMenuItem("Show in Files");
                _showInFileSystemItem.Click += ShowInFiles;
                _showInItems.Add(_showInFileSystemItem);
                _showInRegistryItem = new ToolStripMenuItem("Show in Registry");
                _showInRegistryItem.Click += ShowInRegistry;
                _showInItems.Add(_showInRegistryItem);

                _showInSepItem = new ToolStripSeparator();
            }
        }

        public override void ContextMenuOpening()
        {
            base.ContextMenuOpening();

            ITraceEntry traceEntry = null;
            if (CallInterpreter.SelectedEntries.Count() != 0)
            {
                var entry = CallInterpreter.SelectedEntries.First();
                traceEntry = entry as ITraceEntry;
            }
            if (traceEntry != null && _interpreter.ContextMenuStrip != null)
            {
                _showInComItem.Tag = traceEntry.IsCom;
                _showInWindowsItem.Tag = traceEntry.IsWindow;
                _showInFileSystemItem.Tag = (!traceEntry.IsDirectory || _interpreterController.ShowDirectoriesInFiles) &&
                                            (traceEntry.IsQueryAttributes &&
                                             _interpreterController.ShowQueryAttributesInFiles ||
                                             !traceEntry.IsQueryAttributes &&
                                             traceEntry.IsFile);
                _showInRegistryItem.Tag = traceEntry.IsRegistry;

                _interpreter.ContextMenuStrip.Items.Remove(_showInSepItem);
                _showInSepItem.Tag = false;
                foreach (var item in _showInItems)
                {
                    _interpreter.ContextMenuStrip.Items.Remove(item);
                    if ((bool)item.Tag)
                    {
                        if (!(bool)_showInSepItem.Tag)
                        {
                            _showInSepItem.Tag = true;
                            _interpreter.ContextMenuStrip.Items.Insert(0, _showInSepItem);
                        }
                        _interpreter.ContextMenuStrip.Items.Insert(0, item);
                    }
                }
            }
            else
            {
                _showInComItem.Visible =
                    _showInWindowsItem.Visible =
                    _showInFileSystemItem.Visible = _showInRegistryItem.Visible = _showInSepItem.Visible = false;
            }
        }

        private void ShowInRegistry(object sender, EventArgs e)
        {
            if (CallInterpreter.SelectedEntries.Count() != 0)
            {
                var entry = CallInterpreter.SelectedEntries.First();
                var traceEntry = entry as ITraceEntry;
                if (traceEntry != null)
                    _interpreterController.ShowInRegistry(traceEntry);
            }
        }

        private void ShowInFiles(object sender, EventArgs e)
        {
            if (CallInterpreter.SelectedEntries.Count() != 0)
            {
                var entry = CallInterpreter.SelectedEntries.First();
                var traceEntry = entry as ITraceEntry;
                if (traceEntry != null)
                    _interpreterController.ShowInFiles(traceEntry);
            }
        }

        private void ShowInWindows(object sender, EventArgs e)
        {
            if (CallInterpreter.SelectedEntries.Count() != 0)
            {
                var entry = CallInterpreter.SelectedEntries.First();
                var traceEntry = entry as ITraceEntry;
                if (traceEntry != null)
                    _interpreterController.ShowInWindows(traceEntry);
            }
        }

        private void ShowInCom(object sender, EventArgs e)
        {
            if (CallInterpreter.SelectedEntries.Count() != 0)
            {
                var entry = CallInterpreter.SelectedEntries.First();
                var traceEntry = entry as ITraceEntry;
                if (traceEntry != null)
                    _interpreterController.ShowInCom(traceEntry);
            }
        }
    }
}
