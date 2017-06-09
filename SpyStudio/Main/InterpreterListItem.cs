using System.Collections.Generic;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Tools;

namespace SpyStudio.Main
{
    public abstract class InterpreterListItem : ListViewItem, IEntry
    {
        #region Instantiation

        protected InterpreterListItem()
        {
            
        }

        protected InterpreterListItem(string aName) : base(aName)
        {

        }

        protected InterpreterListItem(string[] strings) : base(strings)
        {
        }

        #endregion

        #region Implementation of IEntry

        public IInterpreter Interpreter { get { return (IInterpreter)ListView; } }

        public abstract HashSet<CallEventId> CallEventIds { get; }
        public abstract HashSet<DeviareTraceCompareItem> CompareItems { get; }

        public IEntry NextVisibleEntry
        {
            get { return (IEntry)(Index + 1 >= ListView.Items.Count ? null : ListView.Items[Index + 1]); }
        }
        public IEntry PreviousVisibleEntry
        {
            get { return (IEntry)(Index - 1 < 0 ? null : ListView.Items[Index - 1]); }
        }

        public bool SupportsGoTo {get { return Interpreter.SupportsGoTo; } }
        
        public void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            foreach (var callEventId in CallEventIds)
                aTraceTreeView.InsertNode(callEventId);
        }

        #endregion

        #region Abstract Members

        public abstract string NameForDisplay { get; }
        public abstract bool Success { get; }
        public abstract void Accept(IEntryVisitor aVisitor);
        public abstract EntryPropertiesDialogBase GetPropertiesDialog();

        #endregion
    }
}
