using System;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Compare
{
    public class SyncPoint
    {
        public SyncPoint()
        {
            Type = EventMatchType.NoneMatch;
        }

        public SyncPoint(EventInfo eventInfo1, EventInfo eventInfo2)
        {
            if (eventInfo1 != null)
            {
                Event1 = eventInfo1;
            }
            if (eventInfo2 != null)
            {
                Event2 = eventInfo2;
            }

            ResultsMatch = true;
        }

        public EventInfo Event1 { get; set; }
        public EventInfo Event2 { get; set; }
        public EventMatchType Type { get; set; }
        public bool ResultsMatch { get; set; }
        public string ResultMismatchString { get; set; }

        public FunctionInfo FunctionInfo { get; set; }

        public bool HasSingleEvent
        {
            get { return Event1 == null || Event2 == null || Event1.Event == null || Event2.Event == null; }
        }

        public CompareItemType CompareItemType
        {
            get
            {
                if (!HasSingleEvent)
                    return CompareItemType.Item1AndItem2;
                return Event1 == null || Event1.Event == null ? CompareItemType.Item2 : CompareItemType.Item1;
            }
        }

        public CallEvent GetFirstNonNullEvent()
        {
            return Event1 == null || Event1.Event == null ? Event2.Event : Event1.Event;
        }

        public EventInfo GetFirstNonNullEventInfo()
        {
            return Event1 ?? Event2;
        }
    }
}