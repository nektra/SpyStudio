using System.Collections.Generic;
using System.Windows.Forms;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.Registry.Controls
{
    public class RegistryValueList : ListViewSorted, IInterpreter
    {
        public uint File1TraceId;
        public uint File2TraceId;

        private readonly ColumnHeader _columnValueName;
        private readonly ColumnHeader _columnValueType;
        private readonly ColumnHeader _columnValueData;
        
        public RegistryValueList()
        {
            FullRowSelect = true;

            View = View.Details;
            _columnValueName = new ColumnHeader();
            _columnValueType = new ColumnHeader();
            _columnValueData = new ColumnHeader();
            // 
            // listViewValues
            // 
            Columns.AddRange(new[] {
            _columnValueName,
            _columnValueType,
            _columnValueData});
            // 
            // columnValueName
            // 
            _columnValueName.Text = "Name";
            _columnValueName.Width = 298;
            // 
            // columnValueType
            // 
            _columnValueType.Text = "Type";
            _columnValueType.Width = 298;
            // 
            // columnValueData
            // 
            _columnValueData.Text = "Data";
            _columnValueData.Width = 203;
        }

        public IInterpreterController Controller { get; set; }

        public EntryContextMenu ContextMenuController { get { return null; } }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return SelectedItems.Cast<IEntry>(); }
        }

        public Control ParentControl
        {
            get { return Parent; }
        }

        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            var nextEntry = (ListViewItem) anEntry.NextVisibleEntry;

            if (nextEntry == null)
                return;

            SelectedItems.Clear();
            nextEntry.Selected = true;
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            var previousEntry = ((ListViewItem)anEntry.PreviousVisibleEntry);

            if (previousEntry == null)
                return;

            SelectedItems.Clear();
            previousEntry.Selected = true;
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }
        public void FindEvent(FindEventArgs e)
        {
        }
        public void CopySelectionToClipboard()
        {
        }
        public void SelectAll()
        {
        }

    }
}
