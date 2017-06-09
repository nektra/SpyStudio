using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class SynchronizedStream
    {
        public ProcessState ProcessState;
        public List<AbstractBuffer> Buffers = new List<AbstractBuffer>();
        public SynchronizedStream(ProcessState state)
        {
            ProcessState = state;
        }
    }

    public class StreamSynchronizationPoint
    {
        public Dictionary<ulong, SynchronizedStream> Streams = new Dictionary<ulong, SynchronizedStream>();
        public StreamSynchronizationPoint()
        {
        }

        public StreamSynchronizationPoint(Dictionary<ulong, ProcessState> streams)
        {
            foreach (var processState in streams)
            {
                var pid = processState.Key >> 32;
                //Debug.Assert(pid == (ulong)processState.Value.Proc.Id);
                Streams[pid] = new SynchronizedStream(processState.Value);
            }
        }
        
        public void PushBuffer(ProcessState state, AbstractBuffer buffer)
        {
            SynchronizedStream stream;
            var pid = buffer.Header.LongPid;
            if (!Streams.TryGetValue(pid, out stream))
                stream = Streams[pid] = new SynchronizedStream(state);
            PendingEventsCountManager.GetInstance().EventsEnter((int)buffer.Header.EventCount, PendingEventsCountManager.BufferPhase);
            stream.Buffers.Add(buffer);
        }

        public bool HasClearSignal;
    }

    public class StreamSynchronizationPointWrapper
    {
        StreamSynchronizationPoint _point = new StreamSynchronizationPoint();
        public StreamSynchronizationPointWrapper(){}
        public void PushBuffer(ProcessState state, AbstractBuffer buffer)
        {
            lock (this)
            {
                _point.PushBuffer(state, buffer);
            }
        }
        public BufferFileSource SetUpSimulation(Dictionary<ulong, ProcessState> streams)
        {
            lock (this)
            {
                var ret = new BufferFileSource();
                _point = ret.SetUpSimulation(streams);
                return ret;
            }
        }
        public StreamSynchronizationPoint Get(Dictionary<ulong, ProcessState> streams)
        {
            lock (this)
            {
                var ret = _point;
                _point = new StreamSynchronizationPoint(streams);
                return ret;
            }
        }
        public void SetClear()
        {
            lock (this)
            {
                _point.HasClearSignal = true;
            }
        }
        public bool IsClearSet
        {
            get
            {
                lock (this)
                {
                    return _point.HasClearSignal;
                }
            }
        }
    }
}
