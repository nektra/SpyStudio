using System.Collections.Generic;
using System.Threading;
using SpyStudio.Hooks;
using SpyStudio.Hooks.Async;

namespace SpyStudio.Tools
{
    public class QueuedWorkerThread<TActionObject>
    {
        private Queue<TActionObject> _queue;
        private ManualResetEvent[] _events;
        private Thread _worker;
        private int _tid;
        private int _timeout;
        //private int _unmanagedTid;

        public int Timeout
        {
            set
            {
                _timeout = value;
            }
        }

        public delegate void ExecuteFunction(TActionObject action);
        public delegate int TimeoutFunction();

        private void Init(ExecuteFunction func, TimeoutFunction tofunc, int timeout)
        {
            _queue = new Queue<TActionObject>();
            _events = new ManualResetEvent[2];
            _events[0] = new ManualResetEvent(false); //quit event
            _events[1] = new ManualResetEvent(false); //start event
            _func = func;
            _tofunc = tofunc;
            _worker = null;
            _timeout = timeout < 0 ? System.Threading.Timeout.Infinite : timeout;
        }

        public QueuedWorkerThread(ExecuteFunction func, TimeoutFunction tofunc, int timeout)
        {
            Init(func, tofunc, timeout);
        }

        public QueuedWorkerThread(ExecuteFunction func)
        {
            Init(func, null, -1);
        }

        public void Start()
        {
            _queue.Clear();
            _events[0].Reset();
            _events[1].Reset();
            _worker = new Thread(DoWork);
            _worker.SetApartmentState(ApartmentState.MTA);
            _worker.Start();
        }

        public void Stop()
        {
            if (_worker == null) return;
            _events[0].Set();
            _worker.Join();
        }

        public void QueueAction(TActionObject action)
        {
            lock (_queue)
            {
                _queue.Enqueue(action);
            }
            _events[1].Set();
        }

        private bool ExecutingAction { get; set; }

        public bool AnyPendingAction()
        {
            lock(_queue)
            {
                // if calling thread is the DoWork thread don't evaluate ExecutingAction because it will be always true
                return Thread.CurrentThread.ManagedThreadId != _tid && ExecutingAction || _queue.Count > 0;
            }
        }

        private void DoWork()
        {
            bool stop = false;

            //_unmanagedTid = AppDomain.GetCurrentThreadId();
            _tid = Thread.CurrentThread.ManagedThreadId;

            double lastTimeout = -1;

            while (stop == false)
            {
                int index = WaitHandle.WaitAny(_events, 100, false);
                if (index == 0)
                    break;
                if (index != WaitHandle.WaitTimeout)
                {
                    _events[1].Reset();
                    //process actions
                    while (true)
                    {
                        var action = default(TActionObject);

                        if (_events[0].WaitOne(1, false))
                        {
                            stop = true;
                            break;
                        }
                        var gotAction = false;
                        lock (_queue)
                        {
                            if (_queue.Count > 0)
                            {
                                action = _queue.Dequeue();
                                ExecutingAction = true;
                                gotAction = true;
                            }
                        }
                        if (!gotAction)
                            break;
                        _func(action);
                        ExecutingAction = false;
                        if (_tofunc != null)
                        {
                            double now = AsyncHookMgr.GetTimestamp();
                            if (lastTimeout < 0 || now < lastTimeout || now - lastTimeout >= _timeout)
                            {
                                _timeout = _tofunc();
                                lastTimeout = AsyncHookMgr.GetTimestamp();
                            }
                        }
                    }
                }
                else if (_tofunc != null)
                {
                    double now = AsyncHookMgr.GetTimestamp();
                    if (lastTimeout < 0 || now < lastTimeout || now - lastTimeout >= _timeout)
                    {
                        _timeout = _tofunc();
                        lastTimeout = AsyncHookMgr.GetTimestamp();
                    }
                }
            }
        }

        private ExecuteFunction _func;
        private TimeoutFunction _tofunc;
    }
}
