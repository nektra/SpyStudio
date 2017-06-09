using System.Collections.Generic;
using System.Windows.Forms;
using Aga.Controls;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.ContextMenu
{
    /// <summary>
    /// Any control that can interpret CallEvents
    /// </summary>
    public interface IInterpreter
    {
        IInterpreterController Controller { get; set; }
        ContextMenuStrip ContextMenuStrip { get; }
        IEnumerable<IEntry> SelectedEntries { get; }
        Control ParentControl { get; }
        EntryContextMenu ContextMenuController { get; }
        bool SupportsGoTo { get; }
        void SelectNextVisibleEntry(IEntry anEntry);
        void SelectPreviousVisibleEntry(IEntry anEntry);
        void SelectItemContaining(CallEventId eventId);
        void FindEvent(FindEventArgs e);
        void CopySelectionToClipboard();
        void SelectAll();
    }

    public interface ICompareInterpreter : IInterpreter
    {
        uint Trace1ID { get; }
        uint Trace2ID { get; }
    }

    /// <summary>
    /// Any item of an IInterpreter
    /// </summary>
    public interface IEntry
    {
        IInterpreter Interpreter { get; }
        HashSet<CallEventId> CallEventIds { get; }
        HashSet<DeviareTraceCompareItem> CompareItems { get; }
        IEntry NextVisibleEntry { get; }
        IEntry PreviousVisibleEntry { get; }
        EntryPropertiesDialogBase GetPropertiesDialog();
        string NameForDisplay { get; }
        bool Success { get; }
        void Accept(IEntryVisitor aVisitor);

        //TODO: Actually, this is only called with the regular Trace's and
        //TODO: Compare's nodes. I imagine eventually those two will implement
        //TODO: ITraceEntry. Once that's the case, SupportsGoTo() should be
        //TODO: moved there and removed from all the other IInterpreterEntry
        //TODO: subtypes.
        //  -Guille
        bool SupportsGoTo { get; }
        void AddCallEventsTo(TraceTreeView aTraceTreeView);
    }

    /// <summary>
    /// A controller that can manage different IInterpreter s
    /// </summary>
    public interface IInterpreterController
    {
        void ShowInCom(ITraceEntry anEntry);
        void ShowInWindows(ITraceEntry anEntry);
        void ShowInFiles(ITraceEntry anEntry);
        void ShowInRegistry(ITraceEntry anEntry);

        bool ShowQueryAttributesInFiles { get; }
        bool ShowDirectoriesInFiles { get; }
        bool PropertiesGoToVisible { get; }
        bool PropertiesVisible { get; }
    }

    public interface ICompareInterpreterController : IInterpreterController
    {
        uint Trace1ID { get; }
        uint Trace2ID { get; }
    }
}