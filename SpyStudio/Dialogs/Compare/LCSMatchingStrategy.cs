using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.Compare
{
    internal class LCSMatchingStrategy : TraceCompareTreeLoadStrategy
    {
        #region Instantiation

        public static LCSMatchingStrategy For(DeviareTraceCompareTreeView aTree)
        {
            return new LCSMatchingStrategy(aTree);
        }

        private LCSMatchingStrategy(DeviareTraceCompareTreeView aTree)
        {
            TraceTree = aTree;

            IgnoredPivots = new List<string>
                {
                    "TerminateThread",
                    "ThreadExit"
                };
        }

        #endregion

        #region Matching rules

        private bool EventsMatch(EventInfo event1, EventInfo event2)
        {
            LastSyncPoint = new SyncPoint(event1, event2) {ResultsMatch = true};

            if (FunctionInfo.MatchFunctionResult)
            {
                if (event1.Event.Success != event2.Event.Success)
                {
                    LastSyncPoint.ResultsMatch = false;
                    LastSyncPoint.ResultMismatchString += event1.Event.Result.ForCompareString() + " / " + event2.Event.Result.ForCompareString();
                }
            }

            foreach (var matchRule in FunctionInfo.MatchInfos)
            {
                if (matchRule.IsResult)
                {
                    if (matchRule.IsSatisfiedBy(event1, event2))
                        continue;

                    LastSyncPoint.ResultsMatch = false;

                    if (!string.IsNullOrEmpty(LastSyncPoint.ResultMismatchString))
                        LastSyncPoint.ResultMismatchString += "\n";
                    LastSyncPoint.ResultMismatchString += (string.IsNullOrEmpty(event1.Event.ParamMain)
                                                               ? string.Empty
                                                               : matchRule.HelpString + ": ") +
                                                          matchRule.GetMatchStringFor(event1.Event).ForCompareString() + " / " +
                                                          matchRule.GetMatchStringFor(event2.Event).ForCompareString();
                    continue;
                }

                if (!matchRule.IsSatisfiedBy(event1, event2))
                    return false;
            }

            return true;
        }

        private bool EventsRespectOrder(EventInfo event1, EventInfo event2)
        {
            // We don't use event1 because we already know that it respects order.
            return (PreviousSyncPoint == null || PreviousSyncPoint.Value.Event2.Preceeds(event2)) 
                && (NextSyncPoint == null || NextSyncPoint.Value.Event2.Succeeds(event2));
        }

        private bool EventsHaveAMatchingHierarchy(EventInfo eventInfo1, EventInfo eventInfo2)
        {
            // Events belong to the same level, so it is not necessary to check it

            if (eventInfo1.Parent == null) // equivalent to eventInfo1.Level == 0, but faster
                return true;

            var event1Parent = eventInfo1.Parent;
            var event2Parent = eventInfo2.Parent;
            
            var parentsSyncPoint = SyncPointsToInsert.FindBackwardsStartingAt(PreviousSyncPoint,
                                                                                  sp =>
                                                                                  sp.Event1.BeforeCallNumber == event1Parent.BeforeCallNumber);

            //Debug.Assert(parentsSyncPoint != null, "A parent SyncPoint should be found when Level > 0");

            if (parentsSyncPoint == null) // Their parent didn't match
                return false;

            return event1Parent.BeforeCallNumber == parentsSyncPoint.Value.Event1.BeforeCallNumber &&
                   event2Parent.BeforeCallNumber == parentsSyncPoint.Value.Event2.BeforeCallNumber;
        }

        private bool EventsAreBetterMatchThanAnyFoundBefore(EventInfo info1, EventInfo info2)
        {
            LastMatchTypeFound = GetMatchType(info1.Event, info2.Event);

            return BestMatchType < LastMatchTypeFound;
        }

        #endregion

        #region Algorithm global variables

        protected LinkedList<EventInfo> Trace1EventInfos { get; set; }
        protected LinkedList<EventInfo> Trace2EventInfos { get; set; }

        protected FunctionInfo FunctionInfo { get; set; }

        protected LinkedListNode<SyncPoint> NextSyncPoint { get; set; }
        protected LinkedListNode<SyncPoint> PreviousSyncPoint { get; set; }
        protected SyncPoint LastSyncPoint { get; set; }
        protected SyncPoint CurrentSyncPoint { get; set; }

        protected EventMatchType BestMatchType { get; set; }
        protected EventMatchType LastMatchTypeFound { get; set; }

        protected LinkedList<SyncPoint> SyncPointsToInsert { get; set; }

        protected Dictionary<EventInfo, DeviareTraceCompareItem> ItemsByEventInfo { get; set; }

        protected Dictionary<int, Dictionary<string, LinkedList<EventInfo>>> Trace1EventsByFunctionByLevel { get; set; }
        protected Dictionary<int, Dictionary<string, LinkedList<EventInfo>>> Trace2EventsByFunctionByLevel { get; set; }

        protected IEnumerable<string> IgnoredPivots { get; set; }

        #endregion

        #region TraceCompareTreeLoadStrategy implementation

        public override event Action<CallEvent, DeviareTraceCompareItem> OnEventProcessed;

        private void TriggerOnEventAddedEvent(CallEvent aCallEvent, DeviareTraceCompareItem anItem)
        {
            if (OnEventProcessed != null)
                OnEventProcessed(aCallEvent, anItem);
        }

        public override void BeginUpdate()
        {
            PrepareToPerform();
        }

        private void PrepareToPerform()
        {
            Trace1EventInfos = new LinkedList<EventInfo>();
            Trace2EventInfos = new LinkedList<EventInfo>();
            Trace1EventsByFunctionByLevel = new Dictionary<int, Dictionary<string, LinkedList<EventInfo>>>();
            Trace2EventsByFunctionByLevel = new Dictionary<int, Dictionary<string, LinkedList<EventInfo>>>();
            ItemsByEventInfo = new Dictionary<EventInfo, DeviareTraceCompareItem>();
            SyncPointsToInsert = new LinkedList<SyncPoint>();
            PreviousSyncPoint = null;
            NextSyncPoint = null;
            FunctionInfo = null;            

        }

        public override void EndUpdate()
        {
            PrepareToIdle();
        }

        private void PrepareToIdle()
        {
            Trace1EventInfos = null;
            Trace2EventInfos = null;
            Trace1EventsByFunctionByLevel = null;
            Trace2EventsByFunctionByLevel = null;
            ItemsByEventInfo = null;
            SyncPointsToInsert = null;
            PreviousSyncPoint = null;
            NextSyncPoint = null;
            FunctionInfo = null;            
        }

        public override void OnTrace1EventAdded(EventInfo eventInfo1)
        {
            Trace1EventInfos.InsertOrdered(eventInfo1);

            if (!Trace1EventsByFunctionByLevel.ContainsKey(eventInfo1.Level))
                Trace1EventsByFunctionByLevel.Add(eventInfo1.Level, new Dictionary<string, LinkedList<EventInfo>>());

            if (!Trace1EventsByFunctionByLevel[eventInfo1.Level].ContainsKey(eventInfo1.Event.FunctionRoot))
                Trace1EventsByFunctionByLevel[eventInfo1.Level].Add(eventInfo1.Event.FunctionRoot, new LinkedList<EventInfo>());

            Trace1EventsByFunctionByLevel[eventInfo1.Level][eventInfo1.Event.FunctionRoot].InsertOrdered(eventInfo1);
        }

        public override void OnTrace2EventAdded(EventInfo eventInfo2)
        {
            Trace2EventInfos.InsertOrdered(eventInfo2);

            if (!Trace2EventsByFunctionByLevel.ContainsKey(eventInfo2.Level))
                Trace2EventsByFunctionByLevel.Add(eventInfo2.Level, new Dictionary<string, LinkedList<EventInfo>>());

            if (!Trace2EventsByFunctionByLevel[eventInfo2.Level].ContainsKey(eventInfo2.Event.FunctionRoot))
                Trace2EventsByFunctionByLevel[eventInfo2.Level].Add(eventInfo2.Event.FunctionRoot, new LinkedList<EventInfo>());

            Trace2EventsByFunctionByLevel[eventInfo2.Level][eventInfo2.Event.FunctionRoot].InsertOrdered(eventInfo2);
        }

        #endregion

        #region Utils

        private void RefreshFunctionMatchInfoTo(string aFunctionName)
        {
            FunctionInfo = ((FormDeviareCompare)TraceTree.Controller).FunctionInfoDict[aFunctionName];
        }

        public EventMatchType GetMatchType(CallEvent event1, CallEvent event2)
        {
            if (event1.Result != event2.Result && (event1.Result == "BUFFER_OVERFLOW" || event2.Result == "BUFFER_OVERFLOW"))
                return EventMatchType.NoneMatch;

            var stackString1 = event1.StackTraceString;
            var stackString2 = event2.StackTraceString;
            if (!string.IsNullOrEmpty(stackString1) && !string.IsNullOrEmpty(stackString2))
            {
                if (stackString1 == stackString2)
                {
                    return EventMatchType.ExactMatch;
                }

                var nearestSymbol1 = event1.NearestSymbol;
                var nearestSymbol2 = event2.NearestSymbol;
                if (!string.IsNullOrEmpty(nearestSymbol1) && nearestSymbol1 == nearestSymbol2)
                    return EventMatchType.AlmostExactMatch;
            }
            else if (string.Compare(event1.CallModule, event2.CallModule, true, CultureInfo.InvariantCulture) == 0)
            {
                return EventMatchType.BasicMatchWithCaller;
            }

            return EventMatchType.BasicMatch;
        }

        private void FindEncasingSyncPointsFor(EventInfo anEventInfo)
        {
            NextSyncPoint = SyncPointsToInsert.FindForwardStartingAt(NextSyncPoint, sp => sp.Event1.Succeeds(anEventInfo));

            PreviousSyncPoint = NextSyncPoint != null ? NextSyncPoint.Previous : SyncPointsToInsert.Last;

            Debug.Assert(PreviousSyncPoint == null || PreviousSyncPoint.Value.Event1.Preceeds(anEventInfo), "Wrong previous SyncPoint.");
        }

        private int ImportanceOf(KeyValuePair<string, LinkedList<EventInfo>> functionAndEvents)
        {
            var function = functionAndEvents.Key;
            var events = functionAndEvents.Value;

            if (IgnoredPivots.Contains(function))
                return int.MaxValue;

            return events.Count;
        }

        #endregion

        public override void Perform(ProgressReporter workerParams, int startProgress, int endProgress, ref bool cancelled)
        {
#if DEBUG

            Console.WriteLine();
            Console.WriteLine("== LCS Matching Strategy ==");

            var timer = new Stopwatch();
            timer.Start();

#endif

            workerParams.ProgressDlg.Message = "Calculating Sync Points";

            var totalEvents = Trace1EventInfos.Count;
            var processedEvents = 0;
            // iterate through events grouped by level
            Dictionary<string, LinkedList<EventInfo>> trace1EventsByFunction;
            for (var level = 0; Trace1EventsByFunctionByLevel.TryGetValue(level, out trace1EventsByFunction); level++)
            {
                Dictionary<string, LinkedList<EventInfo>> trace2EventsByFunction;
                if (!Trace2EventsByFunctionByLevel.TryGetValue(level, out trace2EventsByFunction))
                    break;

                // iterate through the events grouped by function
                foreach (var trace1EventsOfParticularFunction in trace1EventsByFunction.OrderBy(kv => ImportanceOf(kv)))
                {
                    LinkedList<EventInfo> trace2EventsOfParticularFunction;
                    if (
                        !trace2EventsByFunction.TryGetValue(trace1EventsOfParticularFunction.Key,
                                                            out trace2EventsOfParticularFunction))
                        continue;

                    var trace2CurrentEvent = trace2EventsOfParticularFunction.First;

                    RefreshFunctionMatchInfoTo(trace1EventsOfParticularFunction.Key);

                    NextSyncPoint = null;

                    foreach (var trace1Event in trace1EventsOfParticularFunction.Value)
                    {
                        if(workerParams.CancellationPending)
                        {
                            cancelled = true;
                            return;
                        }
                        workerParams.ReportProgress(startProgress, processedEvents, totalEvents);

                        processedEvents++;

                        LinkedListNode<EventInfo> bestMatchedEvent = null;
                        BestMatchType = EventMatchType.NoneMatch;

                        FindEncasingSyncPointsFor(trace1Event);

                        var trace2LastMatchingEvent = trace2CurrentEvent;

                        while (trace2CurrentEvent != null)
                        {
                            //if (MatchingRules.Any(isSatisfied => !isSatisfied(trace1Event, trace2CurrentEvent.Value)))
                            if (!EventsRespectOrder(trace1Event, trace2CurrentEvent.Value)
                                || !EventsMatch(trace1Event, trace2CurrentEvent.Value)
                                || !EventsHaveAMatchingHierarchy(trace1Event, trace2CurrentEvent.Value)
                                || !EventsAreBetterMatchThanAnyFoundBefore(trace1Event, trace2CurrentEvent.Value))
                            {
                                trace2CurrentEvent = trace2CurrentEvent.Next;
                                continue;
                            }

                            CurrentSyncPoint = LastSyncPoint;
                            BestMatchType = LastMatchTypeFound;
                            bestMatchedEvent = trace2CurrentEvent;
                            trace2LastMatchingEvent = trace2CurrentEvent;

                            if (BestMatchType == EventMatchType.ExactMatch && !CurrentSyncPoint.ResultsMatch)
                                break;

                            trace2CurrentEvent = trace2CurrentEvent.Next;
                        }

                        if (BestMatchType == EventMatchType.NoneMatch)
                        {
                            trace2CurrentEvent = trace2LastMatchingEvent;
                            continue;
                        }

                        CurrentSyncPoint.Type = BestMatchType;
                        CurrentSyncPoint.FunctionInfo = FunctionInfo;

                        trace2CurrentEvent = trace2LastMatchingEvent.Next;

                        Debug.Assert(!SyncPointsToInsert.Any() || PreviousSyncPoint != null || NextSyncPoint != null,
                                     "If at least one SyncPoint exists, a previous or next SyncPoint should be found.");

                        SyncPointsToInsert.AddBetween(CurrentSyncPoint, PreviousSyncPoint, NextSyncPoint);

                        Trace1EventInfos.Remove(CurrentSyncPoint.Event1);
                        Trace2EventInfos.Remove(CurrentSyncPoint.Event2);
                        trace2EventsOfParticularFunction.Remove(bestMatchedEvent);
                    }
                }
            }

            workerParams.ReportProgress(endProgress, processedEvents, totalEvents);

#if DEBUG

            timer.Stop();
            Console.WriteLine("XXX Syncpoint calculation Time: " + timer.Elapsed.TotalMilliseconds);

#endif

            InsertChronologically(workerParams, (startProgress + endProgress) / 2, endProgress);
        }

        private void InsertChronologically(ProgressReporter workerParams, int startProgress, int endProgress)
        {
#if DEBUG

            var timer = new Stopwatch();
            timer.Start();

#endif

            workerParams.ProgressDlg.Message = "Inserting in trace...";

            var totalEvents = (Trace1EventInfos.Count + Trace2EventInfos.Count + SyncPointsToInsert.Count);
            var insertedEvents = 0;

            while (Trace1EventInfos.Any() || Trace2EventInfos.Any() || SyncPointsToInsert.Any())
            {
                workerParams.ReportProgress(startProgress, insertedEvents++, totalEvents);

                var eventInfo1 = Trace1EventInfos.First == null ? null : Trace1EventInfos.First.Value;
                var eventInfo2 = Trace2EventInfos.First == null ? null : Trace2EventInfos.First.Value;
                var syncPoint = SyncPointsToInsert.First == null ? null : SyncPointsToInsert.First.Value;

                if (syncPoint != null && (syncPoint.Event1.PrecedesOrIsNull(eventInfo1) && syncPoint.Event2.PrecedesOrIsNull(eventInfo2)))
                {
                    var item = DeviareTraceCompareItem.From(syncPoint);

                    DeviareTraceCompareItem parentItem = null;

                    if (syncPoint.Event1.Parent != null)
                        ItemsByEventInfo.TryGetValue(syncPoint.Event1.Parent, out parentItem);

                    TraceTree.Add(item, parentItem);

                    ItemsByEventInfo.Add(syncPoint.Event1, item);
                    ItemsByEventInfo.Add(syncPoint.Event2, item);

                    SyncPointsToInsert.RemoveFirst();

                    TriggerOnEventAddedEvent(syncPoint.Event1.Event, item);
                    TriggerOnEventAddedEvent(syncPoint.Event2.Event, item);

                    continue;
                }

                if (eventInfo1 != null && (syncPoint == null || eventInfo1.PrecedesOrIsNull(syncPoint.Event1)))
                {
                    var item = DeviareTraceCompareItem.From(new SyncPoint(eventInfo1, null));

                    DeviareTraceCompareItem parentItem = null;

                    if (eventInfo1.Parent != null)
                        ItemsByEventInfo.TryGetValue(eventInfo1.Parent, out parentItem);

                    TraceTree.Add(item, parentItem);

                    ItemsByEventInfo.Add(eventInfo1, item);

                    Trace1EventInfos.RemoveFirst();

                    TriggerOnEventAddedEvent(eventInfo1.Event, item);

                    continue;
                }

                if (eventInfo2 != null && (syncPoint == null || eventInfo2.PrecedesOrIsNull(syncPoint.Event2)))
                {
                    var item = DeviareTraceCompareItem.From(new SyncPoint(null, eventInfo2));

                    DeviareTraceCompareItem parentItem = null;

                    if (eventInfo2.Parent != null)
                        ItemsByEventInfo.TryGetValue(eventInfo2.Parent, out parentItem);

                    TraceTree.Add(item, parentItem);

                    ItemsByEventInfo.Add(eventInfo2, item);

                    Trace2EventInfos.RemoveFirst();

                    TriggerOnEventAddedEvent(eventInfo2.Event, item);

                    continue;
                }

                Debug.Assert(false, "Entered an infinite loop.");
            }

            workerParams.ReportProgress(startProgress, totalEvents, totalEvents);

            TraceTree.InsertionFinished();

#if DEBUG

            timer.Stop();

            Console.WriteLine("XXX Insertion time: " + timer.Elapsed.TotalMilliseconds);

#endif
        }
    }
}
