using SpyStudio.ContextMenu;

namespace SpyStudio.COM.Controls.Compare
{
    class CompareComObjectListView : ComObjectListView, ICompareInterpreter
    {
        #region Implementation of ICompareInterpreter

        public uint Trace1ID { get { return ((ICompareInterpreterController) Controller).Trace1ID; } }
        public uint Trace2ID { get { return ((ICompareInterpreterController) Controller).Trace2ID; } }

        #endregion

        protected override ComObjectListViewItemBase CreateNewItemFrom(ComObjectInfo aComInfo)
        {
            return CompareComObjectListViewItem.From(aComInfo);
        }
    }
}