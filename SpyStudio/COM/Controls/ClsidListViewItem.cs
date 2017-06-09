using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.COM.Controls
{
    public class ClsidListViewItem : ListViewItem, IEntry
    {
        double _time, _time1, _time2;
        public HashSet<string> CallerModules = new HashSet<string>();
        private readonly HashSet<DeviareTraceCompareItem> _compareItems = new HashSet<DeviareTraceCompareItem>();

        public ClsidListViewItem(string[] columns, string clsid, string description, string serverPath, ulong retValue, bool compareMode)
            : base(columns)
        {
            Clsid = clsid;
            Description = description;
            ServerPath = serverPath;
            ReturnValue = retValue;
            _time = _time1 = _time2 = 0;
            Count = 0;
            IsForCompare = compareMode;
            CallEventIds = new HashSet<CallEventId>();
        }

        public string Clsid { get; set; }

        public string Description { get; set; }

        public string ServerPath { get; set; }

        public UInt64 ReturnValue { get; set; }

        public uint Count { get; set; }

        public uint Count1 { get; set; }

        public uint Count2 { get; set; }

        public bool Success { get; set; }

        public bool Success1 { get; set; }

        public bool Success2 { get; set; }

        public IInterpreter Interpreter { get { return (IInterpreter)ListView; } }
        public HashSet<CallEventId> CallEventIds { get; private set; }

        public HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return _compareItems; }
        }

        IEntry IEntry.NextVisibleEntry
        {
            get { return (IEntry)(Index + 1 >= ListView.Items.Count ? null : ListView.Items[Index + 1]); }
        }

        IEntry IEntry.PreviousVisibleEntry
        {
            get { return (IEntry)(Index - 1 < 0 ? null : ListView.Items[Index - 1]); }
        }

        public EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return IsForCompare
                       ? (EntryPropertiesDialogBase)new EntryComparePropertiesDialog(this)
                       : new EntryPropertiesDialog(this);
        }

        public string NameForDisplay
        {
            get { return string.IsNullOrEmpty(Description) ? Clsid : Description; }
        }

        public bool IsForCompare { get; private set; }
        public CompareItemType ItemType { get; set; }

        public bool SupportsGoTo()
        {
            throw new NotImplementedException();
        }

        public void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            foreach (var eventId in CallEventIds)
                aTraceTreeView.InsertNode(eventId);
        }

        public void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        public bool Contains(CallEventId eventId)
        {
            return CallEventIds.Contains(eventId);
        }

        public double Time
        {
            get { return _time; }
            set { throw new NotImplementedException(); }
        }
        public double Time1
        {
            get { return _time1; }
            set { throw new NotImplementedException(); }
        }

        public double Time2
        {
            get { return _time2; }
            set { throw new NotImplementedException(); }
        }

        public void AddCall(CallEvent e)
        {
            CallEventIds.Add(e.EventId);
            Count++;
            _time += e.Time;
        }
        public void AddCall1(CallEvent e)
        {
            CallEventIds.Add(e.EventId);
            Count1++;
            _time1 += e.Time;
        }
        public void AddCall2(CallEvent e)
        {
            CallEventIds.Add(e.EventId);
            Count2++;
            _time2 += e.Time;
        }
    }
}