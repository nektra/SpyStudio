using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    class StreamMerger
    {
        private readonly List<ProcessState> _states;
        private readonly CallEvent[] _callEvents;
        private readonly AsyncHookMgr _hookMgr;
        public StreamMerger(StreamSynchronizationPoint syncPoint, AsyncHookMgr hookMgr)
        {
            _hookMgr = hookMgr;
            foreach (var stream in syncPoint.Streams.Values)
                foreach (var buffer in stream.Buffers)
                    stream.ProcessState.Stream.AvailableBuffers.Enqueue(buffer);

            _states = syncPoint.Streams.Values.Select(x => x.ProcessState).ToList();

            _callEvents = _states.Select(x => x.Stream.OneMoreEvent(x, hookMgr)).ToArray();
        }

        public CallEvent OneMoreEvent()
        {
            int dequeFromWhere = -1;

            for (int i = 0; i < _callEvents.Length; i++)
            {
                var callEvent = _callEvents[i];
                if (callEvent == null)
                    continue;
                if (dequeFromWhere < 0 || callEvent.TimeStamp < _callEvents[dequeFromWhere].TimeStamp)
                    dequeFromWhere = i;
            }

            if (dequeFromWhere < 0)
                return null;

            var state = _states[dequeFromWhere];
            var ret = _callEvents[dequeFromWhere];
            _callEvents[dequeFromWhere] = state.Stream.OneMoreEvent(state, _hookMgr);

            return ret;
        }

        public void DiscardEverything()
        {
            foreach (var processState in _states)
            {
                processState.Stream.DiscardEverything();
            }
        }
    }
}
