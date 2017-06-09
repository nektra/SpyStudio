using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Properties;

namespace SpyStudio.Windows.Controls.Compare
{
    class CompareWindowListView : WindowListView, ICompareInterpreter
    {
        public override EntryPropertiesDialogBase GetPropertiesDialogFor(IEntry anEntry)
        {
            return new EntryComparePropertiesDialog(anEntry);
        }

        protected override WindowListViewItemBase CreateNewItemNamed(string aName)
        {
            return CompareWindowListViewItem.Named(aName);
        }

        #region Implementation of ICompareInterpreter

        public uint Trace1ID { get { return ((ICompareInterpreterController)Controller).Trace1ID; } }
        public uint Trace2ID { get { return ((ICompareInterpreterController)Controller).Trace2ID; } }

        #endregion
    }
}