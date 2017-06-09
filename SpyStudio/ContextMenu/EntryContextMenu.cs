using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using SpyStudio.Dialogs;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Hooks;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.ContextMenu
{
    public class EntryContextMenu
    {
        private IInterpreter _interpreter;
        private readonly List<EntryPropertiesDialogBase> _entryPropertiesDialogsOpened = new List<EntryPropertiesDialogBase>();
        private ToolStripMenuItem _propertiesItem;
        private readonly bool _isTrace;
        private List<Control> _attachedControls = new List<Control>();


        public EntryContextMenu(IInterpreter interpreter)
        {
            _isTrace = false;
            Init(interpreter);
        }

        public EntryContextMenu(IInterpreter interpreter, bool isTrace)
        {
            _isTrace = isTrace;
            Init(interpreter);
        }

        void Init(IInterpreter interpreter)
        {
            _interpreter = interpreter;
            if (_interpreter.ContextMenuStrip != null)
            {
                ContextMenuTools.AddSeparatorIfNotEmpty(_interpreter.ContextMenuStrip);

                _propertiesItem = new ToolStripMenuItem("Properties");
                _propertiesItem.Click += ShowItemProperties;

                _interpreter.ContextMenuStrip.Items.Add(_propertiesItem);
                _interpreter.ContextMenuStrip.Opening += ContextMenuOpening;
            }
            var control = interpreter as Control;
            if(control != null)
            {
                _attachedControls.Add(control);
                AttachInputEvents(control);
            }
        }
        private void OnMouseDoubleClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!_interpreter.Controller.PropertiesVisible)
                return;
            if(ShowItemProperties())
            {
                var treeMouseArgs = mouseEventArgs as TreeNodeAdvMouseEventArgs;
                if(treeMouseArgs != null)
                {
                    treeMouseArgs.Handled = true;
                }
            }
        }

        protected IInterpreter CallInterpreter { get { return _interpreter; } }
        void ContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ContextMenuOpening();
        }
        public virtual void ContextMenuOpening()
        {
            if (_propertiesItem != null)
                _propertiesItem.Enabled = (_interpreter.SelectedEntries.Count() != 0);
        }

        private void ShowItemProperties(object sender, EventArgs e)
        {
            ShowItemProperties();
        }

        public bool ShowItemProperties()
        {
            IEntry selEntry = _interpreter.SelectedEntries.FirstOrDefault();
            if (selEntry != null)
            {
                var entryPropertiesDialog = _interpreter.SelectedEntries.First().GetPropertiesDialog();

                entryPropertiesDialog.UpClick += OnUpClick;
                entryPropertiesDialog.DownClick += OnDownClick;
                entryPropertiesDialog.Closed += EntryPropertiesDialogOnClosed;
                entryPropertiesDialog.Show(_interpreter.ParentControl);
                
                _entryPropertiesDialogsOpened.Add(entryPropertiesDialog);

                return true;
            }
            return false;
        }

        private void EntryPropertiesDialogOnClosed(object sender, EventArgs eventArgs)
        {
            GCTools.AsyncCollectDelayed(10000);
        }

        private void OnDownClick(IEntry anEntry)
        {
            _interpreter.SelectNextVisibleEntry(anEntry);
        }

        private void OnUpClick(IEntry anEntry)
        {
            _interpreter.SelectPreviousVisibleEntry(anEntry);
        }

        public void Close(bool detachEvents)
        {
            if(detachEvents)
            {
                foreach (var control in _attachedControls)
                    DetachInputEvents(control);
            }
            foreach (var entryPropertiesDialog in _entryPropertiesDialogsOpened)
                entryPropertiesDialog.Close();
            _entryPropertiesDialogsOpened.Clear();
        }

        public void DetachInputEvents(Control control)
        {
            var tree = control as TreeViewAdv;
            if(tree != null)
            {
                tree.NodeMouseDoubleClick -= OnMouseDoubleClick;
            }
            else
            {
                control.MouseDoubleClick -= OnMouseDoubleClick;
            }
        }
        public void AttachInputEvents(Control control)
        {
            var tree = control as TreeViewAdv;
            if (tree != null)
            {
                tree.NodeMouseDoubleClick += OnMouseDoubleClick;
            }
            else
            {
                control.MouseDoubleClick += OnMouseDoubleClick;
            }
        }
    }
}