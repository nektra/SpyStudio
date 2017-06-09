using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpyStudio.Database;
using SpyStudio.Tools;

namespace SpyStudio.Extensions
{
    public static class CallEventIdsEnumerableExtensions
    {
        public static IEnumerable<CallEvent> FetchEvents(this IEnumerable<CallEventId> callEventIds)
        {
            return EventDatabaseMgr.GetInstance().GetEvents(callEventIds, false);
        }
        public static IEnumerable<CallEvent> FetchEvents(this IEnumerable<CallEventId> callEventIds, bool fullStackInfo)
        {
            return EventDatabaseMgr.GetInstance().GetEvents(callEventIds, fullStackInfo);
        }

        public static IEnumerable<CallEvent> FetchNonGeneratedEvents(this IEnumerable<CallEventId> callEventIds)
        {
            return EventDatabaseMgr.GetInstance().GetEvents(callEventIds, false, true).Where(e => !e.IsGenerated);
        }
        public static IEnumerable<CallEvent> FetchNonGeneratedEvents(this IEnumerable<CallEventId> callEventIds, bool fullStackInfo)
        {
            return EventDatabaseMgr.GetInstance().GetEvents(callEventIds, fullStackInfo, true).Where(e => !e.IsGenerated);
        }
    }
}
