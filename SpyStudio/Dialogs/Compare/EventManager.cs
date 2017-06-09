using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Compare
{
    public class EventManager
    {
        private readonly List<EventInfo> _trace1Events = new List<EventInfo>();
        private readonly List<EventInfo> _rootEvents1 = new List<EventInfo>();

        private readonly List<EventInfo> _trace2Events = new List<EventInfo>();
        private readonly List<EventInfo> _rootEvents2 = new List<EventInfo>();

        public void ProcessTrace1(Dictionary<ulong, CallEvent> allEvents1, CallEvent[] events, ProgressReporter workerParams, int minimum, int maximum, ref bool cancelled)
        {
            ProcessTrace(allEvents1, events, _trace1Events,
                         _rootEvents1,
                         workerParams, minimum, maximum, ref cancelled);
        }

        public void ProcessTrace2(Dictionary<ulong, CallEvent> allEvents2, CallEvent[] events, ProgressReporter workerParams, int minimum, int maximum, ref bool cancelled)
        {
            ProcessTrace(allEvents2, events, _trace2Events,
                         _rootEvents2,
                         workerParams, minimum, maximum, ref cancelled);
        }
        private void ProcessTrace(Dictionary<ulong, CallEvent> allEventsByCallNumber, CallEvent[] events, ICollection<EventInfo> processedEvents, IList rootEvents, ProgressReporter workerParams, int minimum, int maximum, ref bool cancelled)
        {
            var itemsProcessed = 0;
            var totalItems = events.Count();

            processedEvents.Clear();
            rootEvents.Clear();

            var callNumberToEventInfo = new Dictionary<UInt64, EventInfo>();

            foreach (var e in events)
            {
                EventInfo eventInfo = null;

                if (e.Peer != 0 && !e.Before)
                    callNumberToEventInfo.TryGetValue(e.Peer, out eventInfo);

                if (eventInfo == null)
                {
                    var ancestors = new List<EventInfo>();

                    var ancestorCallNumber = e.Parent;

                    EventInfo ancestorEvent = null;
                    while (ancestorCallNumber != 0)
                    {
                        if(callNumberToEventInfo.TryGetValue(ancestorCallNumber, out ancestorEvent))
                            ancestors.Insert(0, ancestorEvent);
                        ancestorCallNumber = allEventsByCallNumber[ancestorCallNumber].Parent;
                    }

                    eventInfo = new EventInfo(e)
                                    {Ancestors = ancestors.Count > 0 ? ancestors : null, Parent = ancestorEvent};

                    processedEvents.Add(eventInfo);
                    // use the first callNumber: if the call has a before and after use before one
                    eventInfo.BeforeCallNumber = e.CallNumber;
                    //eventInfo.Parent = ancestorEvent;
                    callNumberToEventInfo[eventInfo.BeforeCallNumber] = eventInfo;
                }
                else
                {
                    //MergeStackInfo(eventInfo.Event, e);
                    eventInfo.Event = e;
                    callNumberToEventInfo[e.CallNumber] = eventInfo;
                }

                //UpdateEventMaps(eventInfo, rootEvents, traceIndex);

                workerParams.ReportProgress(minimum + (++itemsProcessed*(maximum - minimum)/totalItems));
                if (workerParams.CancellationPending)
                {
                    cancelled = true;
                    return;
                }
            }
        }

        public void InsertEvents(ProgressReporter workerParams, int progressStart, int progressEnd, ref bool cancelled)
        {
            var totalItems = _trace1Events.Count + _trace2Events.Count;
            var itemsProcessed = 0;

            if (totalItems != 0)
            {

                workerParams.ReportProgress(progressStart + (itemsProcessed*(progressEnd - progressStart)/totalItems),
                                            itemsProcessed, totalItems);

                foreach (var trace1Event in _trace1Events)
                {
                    itemsProcessed++;
                    workerParams.ReportProgress(
                        progressStart + (itemsProcessed*(progressEnd - progressStart)/totalItems), itemsProcessed,
                        totalItems);

                    if (workerParams.CancellationPending)
                    {
                        cancelled = true;
                        return;
                    }
                    AddTrace1Event(trace1Event);
                }

                foreach (var trace2Event in _trace2Events)
                {
                    itemsProcessed++;
                    workerParams.ReportProgress(
                        progressStart + (itemsProcessed*(progressEnd - progressStart)/totalItems), itemsProcessed,
                        totalItems);

                    if (workerParams.CancellationPending)
                    {
                        cancelled = true;
                        return;
                    }
                    AddTrace2Event(trace2Event);
                }
            }

            workerParams.ReportProgress(progressEnd);
        }

        public event Action<EventInfo> Trace1EventAdded;
        public event Action<EventInfo> Trace2EventAdded;

        private void AddTrace1Event(EventInfo anEventInfo)
        {
            if (Trace1EventAdded != null)
                Trace1EventAdded(anEventInfo);
        }

        private void AddTrace2Event(EventInfo anEventInfo)
        {
            if (Trace2EventAdded != null)
                Trace2EventAdded(anEventInfo);
        }
    }
}