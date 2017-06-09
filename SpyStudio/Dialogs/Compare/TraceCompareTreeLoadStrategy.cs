using System;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Compare
{
    public abstract class TraceCompareTreeLoadStrategy
    {
        protected DeviareTraceCompareTreeView TraceTree { get; set; }

        public abstract void BeginUpdate();
        public abstract void EndUpdate();
        public abstract void OnTrace1EventAdded(EventInfo obj);
        public abstract void OnTrace2EventAdded(EventInfo obj);

        public abstract void Perform(ProgressReporter workerParams, int i, int i1, ref bool cancelled);

        public virtual event Action<CallEvent, DeviareTraceCompareItem> OnEventProcessed;
    }
}