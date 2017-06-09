using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Database
{
    public class ThreadInfo
    {
        public uint Cookie;
        public CallEvent Event;

        public ThreadInfo(uint cookie, CallEvent e)
        {
            Cookie = cookie;
            Event = e;
        }
    }

    class EventPeerMatching
    {
        // thread id -> ThreadInfo
        readonly Dictionary<uint, Stack<ThreadInfo>> _threadInfos = new Dictionary<uint, Stack<ThreadInfo>>();
        private readonly EventDatabaseMgr _db;
        public EventPeerMatching(EventDatabaseMgr db)
        {
            _db = db;
        }
        public void MatchEvent(CallEvent callEvent)
        {
            lock (_threadInfos)
            {
                Stack<ThreadInfo> threadInfoQueue;
                if (_threadInfos.TryGetValue(callEvent.Tid, out threadInfoQueue))
                {
                    if (threadInfoQueue.Count > 0)
                    {
                        // found the cookie -> so we've created the item before in the call before
                        var info = threadInfoQueue.Peek();
                        if (info.Cookie == callEvent.Cookie)
                        {
                            threadInfoQueue.Pop();
                            callEvent.Peer = info.Event.EventId.CallNumber;
                        }
                        else
                        {
                            // if the call is root (ChainDepth == 1) and there are some open calls it means that there
                            // is a call that woun't get its peer call. This situation could happen because of an exception
                            // or some other anomalies. That's why we set the call as critical.
                            if (callEvent.ChainDepth != 0 && threadInfoQueue.Count >= callEvent.ChainDepth)
                            {
                                while (info.Cookie != callEvent.Cookie)
                                {
                                    var infoQueue = threadInfoQueue.Pop();
                                    infoQueue.Event.Critical = true;
                                    infoQueue.Event.Priority = 1;
                                    _db.UpdateEventProperties(infoQueue.Event.EventId,
                                                              new EventDatabaseMgr.EventProperties(infoQueue.Event));
                                    if (threadInfoQueue.Count == 0)
                                        break;
                                    info = threadInfoQueue.Peek();
                                }
                                if (info.Cookie == callEvent.Cookie)
                                {
                                    threadInfoQueue.Pop();
                                    callEvent.Peer = info.Event.EventId.CallNumber;
                                }
                            }
                            else
                            {
                                var ancestors = info.Event.Ancestors.ToList();
                                ancestors.Insert(0, info.Event.CallNumber);
                                callEvent.Ancestors = ancestors;
                            }
                        }
                    }
                }
                else
                {
                    threadInfoQueue = _threadInfos[callEvent.Tid] = new Stack<ThreadInfo>();
                }
                // if OnlyBefore we won't get peer event
                if (callEvent.Before && !callEvent.OnlyBefore)
                {
                    threadInfoQueue.Push(new ThreadInfo(callEvent.Cookie, callEvent));
                }
            }
        }
        public void Clear()
        {
            lock (_threadInfos)
            {
                _threadInfos.Clear();
            }
        }
    }
}
