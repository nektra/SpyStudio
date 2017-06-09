using System;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.COM.Controls;
using SpyStudio.COM.Controls.Compare;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.FileSystem;
using SpyStudio.Main;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Controls.Compare;
using SpyStudio.Tools;
using SpyStudio.Windows.Controls;
using SpyStudio.Windows.Controls.Compare;

namespace SpyStudio.ContextMenu
{
    public class EntryPropertiesDialogContextMenu : IEntryVisitor
    {
        protected readonly EntryPropertiesDialogBase PropertiesDialog;
        protected readonly IInterpreter View;
        protected readonly ToolStripMenuItem GoToItem;

        public EntryPropertiesDialogContextMenu(EntryPropertiesDialogBase propertiesDialog, IInterpreter view)
        {
            PropertiesDialog = propertiesDialog;

            View = view;

            if (propertiesDialog.Entry.Interpreter.SupportsGoTo)
            {
                GoToItem = new ToolStripMenuItem("Go To");
                GoToItem.Click += (a, b) => GoToEntry();
                if (view.ContextMenuStrip != null)
                {
                    ContextMenuTools.AddSeparatorIfNotEmpty(view.ContextMenuStrip);
                    view.ContextMenuStrip.Items.Add(GoToItem);
                    view.ContextMenuStrip.Opening += ContextMenuOpening;
                }
            }
        }

        void ContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GoToItem == null) 
                return;

            GoToItem.Enabled = View.SelectedEntries.Any();
        }

        private void GoToEntry()
        {
            if (View.SelectedEntries.Any())
                View.SelectedEntries.First().Accept(this);
        }

        public bool GoToEnabled
        {
            set
            {
                if (GoToItem != null)
                    GoToItem.Enabled = value;
            }
            get { return (GoToItem != null && GoToItem.Enabled); }
        }
        public void GoTo()
        {
            if (GoToItem != null)
                GoToEntry();
        }

        #region IInterpreterEntryVisitor implementation

        public void Visit(TraceTreeView.TraceNode aTraceNode)
        {
            var callDump = ((FormMain)PropertiesDialog.Owner).callDump;
            callDump.SelectItemWithCallEventId(aTraceNode.AfterEvent.EventId);

            PropertiesDialog.Owner.BringToFront();
            callDump.DisplayTraceTab();
            PropertiesDialog.Close();
        }

        public void Visit(DeviareTraceCompareItem aCompareTraceNode)
        {
            if (!View.SelectedEntries.Any())
                return;

            var deviareCompare = (FormDeviareCompare)PropertiesDialog.Owner;
            deviareCompare.SelectItemWithCallEventId(aCompareTraceNode.MainCallEventIds.First());

            PropertiesDialog.Owner.BringToFront();
            deviareCompare.DisplayTraceTab();
            PropertiesDialog.Close();
        }

        public void Visit(RegistryTreeNode aRegistryTreeNode)
        {
            throw new NotImplementedException();
        }

        public void Visit(FileSystemTreeNode aFileSystemTreeNode)
        {
            throw new NotImplementedException();
        }

        public void Visit(FileSystemList.FileSystemListItem aFileSystemListItem)
        {
            throw new NotImplementedException();
        }

        public void Visit(ComObjectListViewItem aComListItem)
        {
            throw new NotImplementedException();
        }

        public void Visit(WindowListViewItem aWindowListItem)
        {
            throw new NotImplementedException();
        }

        public void Visit(RegistryValueItem aRegistryValueItem)
        {
            throw new NotImplementedException();
        }

        public void Visit(CompareComObjectListViewItem aCompareComObjectListViewItem)
        {
            throw new NotImplementedException();
        }

        public void Visit(CompareRegistryValueItem aCompareRegistryValueItem)
        {
            throw new NotImplementedException();
        }

        public void Visit(CompareWindowListViewItem aCompareWindowListViewItem)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}