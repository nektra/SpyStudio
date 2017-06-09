using System.Collections.Generic;
using SpyStudio.ContextMenu;

namespace SpyStudio.EventSummary
{
    class EventSummaryCmd
    {
        public enum EventSummaryCmdType
        {
            BeginUpdate,
            EndUpdate,
            Clear,
            AddRange,
            RemoveRange,
            InsertRange,
            Update
        }

        private static int _nextCmdId = 1;
        public EventSummaryCmd(EventSummaryCmdType type)
        {
            CmdType = type;
            CmdId = _nextCmdId++;
        }
        public EventSummaryCmdType CmdType { get; set; }
        public ITraceEntry[] Entries { get; set; }
        public int CmdId { get; set; }
    }

    class BeginUpdateEventSummaryCmd : EventSummaryCmd
    {
        public BeginUpdateEventSummaryCmd()
            : base(EventSummaryCmdType.BeginUpdate)
        {
        }
    }
    class EndUpdateEventSummaryCmd : EventSummaryCmd
    {
        public EndUpdateEventSummaryCmd()
            : base(EventSummaryCmdType.EndUpdate)
        {
        }
    }
    class ClearEventSummaryCmd : EventSummaryCmd
    {
        public ClearEventSummaryCmd()
            : base(EventSummaryCmdType.Clear)
        {
        }
    }
    class AddRangeEventSummaryCmd : EventSummaryCmd
    {
        public AddRangeEventSummaryCmd(List<ITraceEntry> entries)
            : base(EventSummaryCmdType.AddRange)
        {
            Entries = entries.ToArray();
        }
    }
    class RemoveRangeEventSummaryCmd : EventSummaryCmd
    {
        public RemoveRangeEventSummaryCmd(List<ITraceEntry> entries)
            : base(EventSummaryCmdType.RemoveRange)
        {
            Entries = entries.ToArray();
        }
    }
    class InsertRangeEventSummaryCmd : EventSummaryCmd
    {
        public ITraceEntry EntryBefore { get; set; }

        public InsertRangeEventSummaryCmd(ITraceEntry entryBefore, List<ITraceEntry> entries)
            : base(EventSummaryCmdType.InsertRange)
        {
            EntryBefore = entryBefore;
            Entries = entries.ToArray();
        }
    }
    class UpdateEventSummaryCmd : EventSummaryCmd
    {
        public UpdateEventSummaryCmd(ITraceEntry entry)
            : base(EventSummaryCmdType.Update)
        {
            Entries = new[] {entry};
        }
    }
}
