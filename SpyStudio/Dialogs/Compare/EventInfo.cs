using System;
using System.Collections.Generic;
using System.Diagnostics;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Compare
{
    public class EventInfo
    {
        public EventInfo(CallEvent aCallEvent)
        {
            BeforeCallNumber = aCallEvent.CallNumber;
            Event = aCallEvent;
        }

        public UInt64 BeforeCallNumber{ get; set; }
        public CallEvent Event { get; set; }
        public EventInfo Parent { get; set; }

        private List<EventInfo> _ancestors;

        public List<EventInfo> Ancestors
        {
            get { return _ancestors; }
            set
            {
                _ancestors = value;
                Parent = _ancestors != null && _ancestors.Count > 0 ? _ancestors[0] : null;
            }
        }

        public int Level
        {
            get
            {
                return Ancestors == null ? 0 : Ancestors.Count;
            }
        }

        #region Ordering

        protected readonly Dictionary<ulong, bool> PreceedsCache = new Dictionary<ulong, bool>();

        public bool Preceeds(EventInfo anEventInfo)
        {
            Debug.Assert(anEventInfo != null, "Tried to stablish order in relation to a null EventInfo!");
            Debug.Assert(anEventInfo.Event.TraceId == Event.TraceId, "Tried to stablish an order between EventInfos from different traces.");

            if (BeforeCallNumber == anEventInfo.BeforeCallNumber)
                return false;

            var shallowerEvent = Level < anEventInfo.Level ? this : anEventInfo;

            for (var i = 0; i < shallowerEvent.Level + 1; i++)
            {
                var thisCallNumber = GetCallNumberForComparissonOf(i == Level ? Event : Ancestors[i].Event);
                var anotherCallNumber =
                    GetCallNumberForComparissonOf(i == anEventInfo.Level
                                                      ? anEventInfo.Event
                                                      : anEventInfo.Ancestors[i].Event);

                if (thisCallNumber == anotherCallNumber)
                    continue;

                return thisCallNumber < anotherCallNumber;
            }

            return shallowerEvent == this;
        }

        public bool Succeeds(EventInfo anEventInfo)
        {
            return !Preceeds(anEventInfo) && BeforeCallNumber != anEventInfo.BeforeCallNumber;
        }

        private ulong GetCallNumberForComparissonOf(CallEvent aCallEvent)
        {
            // if possible, return "after" call number. Otherwise, return the "before" call number.

            if (aCallEvent.Before && aCallEvent.Peer != 0)
                return aCallEvent.Peer;

            return aCallEvent.CallNumber;
        }

        public bool PrecedesOrIsNull(EventInfo anotherEvent)
        {
            if (anotherEvent == null)
                return true;

            return Preceeds(anotherEvent);
        }

        #endregion
    }
}