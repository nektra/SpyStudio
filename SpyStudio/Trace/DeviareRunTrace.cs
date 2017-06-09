using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using SpyStudio.COM.Controls;
using SpyStudio.Database;
using SpyStudio.Dialogs;
using SpyStudio.FileSystem;
using SpyStudio.Loader;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Windows.Controls;
using EventHandler = System.EventHandler;
using SpyStudio.COM;

namespace SpyStudio.Trace
{
    public class DeviareRunTrace
    {
        public Guid ObjectId { get; private set; }

        private bool _hasCoCreateEvents, _hasWindowsEvents, _hasFileSystemEvents, _hasRegistryEvents;
        readonly Stopwatch _elapsedTimeWithoutEvents = new Stopwatch();

        //BackgroundWorker worker;
        private ProcessInfo _processInfo;
        EventFilter.Filter _filter;
        readonly object _dataLock = new object();
        private MonitorForm _formParent;
        private UInt64 _lastCallNumberProcessed;
        private UInt64 _lastCallNumberAddedToUI;

        int _reportsPending; 

        private bool _firstFileSystemEventNotified,
                     _firstWindowEventNotified,
                     _firstRegistryEventNotified,
                     _firstCoCreateEventNotified;

        private static uint _nextId = 1;
        private readonly static object NextIdLock = new object();
        private LogStore _loader;
        private readonly EventDatabaseMgr _databaseMgr = EventDatabaseMgr.GetInstance();
        private ProgressDialog _refreshDlg;
        private int _refreshEventCount;
        private int _refreshMinimum, _refreshMaximum;

        public DeviareRunTrace(ProcessInfo processInfo, ModulePath modPath)
        {
            ObjectId = Guid.NewGuid();
            _processInfo = processInfo;
            ModulePath = modPath;
            TraceId = GetNewId();

            _databaseMgr.CreateDatabase(TraceId);
            AttachToDatabase();
        }

        private void AttachToDatabase()
        {
            _databaseMgr.EventsReady += DatabaseMgrOnEventsReady;
            _databaseMgr.EventsRefresh += DatabaseMgrOnEventsRefresh;
            _databaseMgr.EventsCleared += DatabaseMgrOnEventsCleared;
            _databaseMgr.EventsUpdated += DatabaseMgrOnEventsUpdated;
        }

        public void DetachFromDatabase()
        {
            _databaseMgr.EventsReady -= DatabaseMgrOnEventsReady;
            _databaseMgr.EventsRefresh -= DatabaseMgrOnEventsRefresh;
            _databaseMgr.EventsCleared -= DatabaseMgrOnEventsCleared;
            _databaseMgr.EventsUpdated -= DatabaseMgrOnEventsUpdated;
        }

        ~DeviareRunTrace()
        {
            DetachFromDatabase();
            _databaseMgr.DestroyDatabase(TraceId);
        }

        private void DatabaseMgrOnEventsReady(object sender, EventsReadyArgs eventsReadyArgs)
        {
            if (eventsReadyArgs.TraceId != TraceId)
                return;

            if (IsDemonstration && MaximumEventsToLoad <= _eventsLoaded + eventsReadyArgs.Events.Count)
            {
                eventsReadyArgs.Canceled = true;
                var events = eventsReadyArgs.Events.Take((int)(MaximumEventsToLoad - _eventsLoaded)).ToList();
                var n = events.Count;
                ProcessEvents(events);
                _eventsLoaded += n;

                return;
            }

#if DEBUG
            //InitTimes();
            var sw = new Stopwatch();
            sw.Start();
#endif
            {
                var n = eventsReadyArgs.Events.Count;
                _eventsLoaded += n;
                ProcessEvents(eventsReadyArgs.Events);
            }
#if DEBUG
            Debug.WriteLine("DeviareRunTrace:" +
                            "\nAdded count:\t" + eventsReadyArgs.Events.Count +
                            "\nTime:\t" + sw.Elapsed.TotalMilliseconds);
            DumpTimes();
#endif
        }

        public bool IsDemonstration { get; set; }

        public long MaximumEventsToLoad { get; set; }

        private void ProcessEvents(List<CallEvent> events)
        {
            var pecm = PendingEventsCountManager.GetInstance();
            var n = events.Count;
            if (!_clearingData)
            {
#if DEBUG
                var sw = new Stopwatch();
                sw.Start();
#endif
                OnBegin();
#if DEBUG
                _beginUpdateTime += sw.Elapsed.TotalMilliseconds;
#endif
                foreach (var ev in events)
                {
                    ProcessUIEvent(ev);
                    _lastCallNumberAddedToUI = ev.CallNumber;
                }
#if DEBUG
                var previous = sw.Elapsed.TotalMilliseconds;
#endif
                OnEnd();
#if DEBUG
                _endUpdateTime += sw.Elapsed.TotalMilliseconds - previous;
#endif

                // WORKAROUND: clear GC after all CallEvents are reported.
                if (_lastCallNumberAddedToUI == _lastCallNumberProcessed && !_formParent.IsMonitoring)
                {
                    GCTools.AsyncCollectDelayed(1000);
                }
            }
            pecm.EventsLeave(n, PendingEventsCountManager.GuiPhase);
        }

        private void DatabaseMgrOnEventsRefreshDbThread(object sender, EventsRefreshArgs eventsRefreshArgs)
        {
            if (_clearingData || _refreshDlg != null && _refreshDlg.Cancelled())
            {
                eventsRefreshArgs.Canceled = true;
                return;
            }

            Interlocked.Increment(ref _reportsPending);

            if(_refreshDlg != null)
            {
                _refreshDlg.BeginInvoke(new DatabaseMgrOnEventsRefreshDelegate(DatabaseMgrOnEventsRefresh), this,
                                        eventsRefreshArgs);
                while (_reportsPending > 8)
                {
                    Thread.Sleep(200);
                }
            }
        }

        private delegate void DatabaseMgrOnEventsRefreshDelegate(object sender, EventsReadyArgs eventsReadyArgs);
        private void DatabaseMgrOnEventsRefresh(object sender, EventsReadyArgs eventsReadyArgs)
        {
            if (eventsReadyArgs.TraceId != TraceId || _clearingData)
            {
                Interlocked.Decrement(ref _reportsPending);
                return;
            }

            if (_refreshDlg != null && _refreshDlg.Cancelled())
            {
                return;
            }
            foreach (var ev in eventsReadyArgs.Events)
            {
                ProcessUIEvent(ev);
                if (_refreshDlg != null && _refreshDlg.Cancelled())
                {
                    return;
                }
            }
            _refreshEventCount += eventsReadyArgs.Events.Count;

            if (_refreshDlg != null && _refreshEventCount != eventsReadyArgs.TotalEventCount)
            {
                _refreshDlg.SetDetailedProgress(
                    _refreshMinimum +
                    (_refreshEventCount * (_refreshMaximum - _refreshMinimum) / eventsReadyArgs.TotalEventCount),
                    _refreshEventCount,
                    eventsReadyArgs.TotalEventCount);
            }
            Interlocked.Decrement(ref _reportsPending);
        }

        public event Action ClearCompleted;

        private void DatabaseMgrOnEventsCleared(object sender, TraceIdEventArgs traceIdEventArgs)
        {
            if (traceIdEventArgs.TraceId == TraceId)
                InternalClearAllData(false);
            if (ClearCompleted != null)
                ClearCompleted();
        }
        private void DatabaseMgrOnEventsUpdated(object sender, EventsReadyArgs eventsReadyArgs)
        {
            if (eventsReadyArgs.TraceId != TraceId)
                return;

            foreach (var ev in eventsReadyArgs.Events)
            {
                if (EventUpdated != null)
                    EventUpdated(this, new CallEventArgs(ev));
            }
        }

        public static uint GetNewId()
        {
            lock(NextIdLock)
            {
                return _nextId++;
            }
        }

        public bool ProcMonLog { get; set; }
        public uint TraceId { get; set; }
        public bool HasFileSystemEvents()
        {
            lock (_dataLock)
            {
                return _hasFileSystemEvents;
            }
        }
        public bool HasRegistryEvents()
        {
            lock (_dataLock)
            {
                return _hasRegistryEvents;
            }
        }
        public bool HasComEvents()
        {
            return _hasCoCreateEvents;
        }
        public bool HasWindowEvents()
        {
            return _hasWindowsEvents;
        }
        public bool IsEmpty()
        {
            return (_databaseMgr.GetEventCount(TraceId) == 0);
        }

        public double StartTime { get; set; }

        public void ClearFilteredEvents()
        {
            lock (_dataLock)
            {
                _hasCoCreateEvents = _hasWindowsEvents = _hasFileSystemEvents = _hasRegistryEvents = false;
                _firstCoCreateEventNotified =
                    _firstFileSystemEventNotified = _firstRegistryEventNotified = _firstWindowEventNotified = false;
            }
        }
        public void ClearFilteredFileSystemEvents()
        {
            lock (_dataLock)
            {
                _hasFileSystemEvents = false;
                _firstFileSystemEventNotified = false;
            }
        }
        public void ClearFilteredRegistryEvents()
        {
            lock (_dataLock)
            {
                _hasRegistryEvents = false;
                _firstRegistryEventNotified = false;
            }
        }
        public void ClearInternalData()
        {
            lock (_dataLock)
            {
                //_databaseMgr.ClearDatabase(TraceId);
                ClearFilteredEvents();
                ComServerInfoMgr.GetInstance().Clear();
                //CallEvent.ResetLastCallNumber();
                LastProcessedCallNumber = 0;

                if ((_formParent == null || !_formParent.IsMonitoring) && _elapsedTimeWithoutEvents.Elapsed.TotalSeconds > 60)
                {
                    _processInfo.ClearData();
                    ModulePath.Clear();
                }
                
                StartTime = 0;

                if (_totalEvents != 0 || _filteredEvents != 0)
                {
                    _totalEvents = _filteredEvents = 0;

                    if (EventInfoChanged != null)
                        EventInfoChanged.Invoke(this, new EventInfoChangeArgs(_totalEvents, _filteredEvents));
                }
            }
        }
        public void SetParent(MonitorForm parentForm)
        {
            _formParent = parentForm;
        }
        public void SetFilter(EventFilter.Filter filter, bool applyChanges)
        {
            _filter = filter;
            if(applyChanges)
                _filter.Change += FilterChange;
        }

        public EventFilter.Filter Filter
        {
            get { return _filter; }
        }

        void OnBegin()
        {
            if (UpdateBegin != null)
                UpdateBegin(this, new EventArgs());
        }
        void OnBegin(object sender, EventArgs args)
        {
            if (UpdateBegin != null)
                UpdateBegin(this, new EventArgs());
        }
        void OnEnd(object sender, EventArgs args)
        {
            if (UpdateEnd != null)
                UpdateEnd(this, new EventArgs());
        }
        void OnEnd()
        {
            if (UpdateEnd != null)
                UpdateEnd(this, new EventArgs());
        }
        public DialogResult LoadLog(Form parent, string filename, out bool success, out string error)
        {
            return LoadLog(parent, true, true, filename, out success, out error);
        }

        public DialogResult LoadLog(Form parent, bool loadStackDb, bool refreshEvents, string filename, out bool success, out string error)
        {
            _loader = LogStore.CreateLoadLogStore(parent, filename);
            _loader.LoadStackDb = loadStackDb;
            _loader.RefreshEvents = refreshEvents;
            _loader.ModulePath = ModulePath;
            _loader.ProcessInfo = _processInfo;
            _loader.TraceId = TraceId;
            _loader.Begin += OnBegin;
            _loader.End += OnEnd;
            InitTimes();
            _loader.EventsReady += DatabaseMgrOnEventsReady;
            var res = _loader.Load();
            
            DumpTimes();
            success = _loader.Success;
            error = _loader.Error;
            ProcMonLog = _loader.ProcMonLog;
            _loader.EventsReady -= DatabaseMgrOnEventsReady;
            if (_loader.MiscLogData != null)
                ObjectId = new Guid(_loader.MiscLogData.TraceGuid);

            _loader = null;

            // if it is main DeviareRunTrace update last call event in case we start capturing after loading Trace
            if(TraceId == 1)
                CallEvent.SetLastCallNumber(_lastCallNumberAddedToUI);

            if (LoadEnd != null)
                LoadEnd(this, new EventArgs());

            return res;
        }

        public DialogResult LoadLogSync(string filename, out bool success, out string error, ProgressDialog progressDlg, int minimum, int maximum)
        {
            var loader = LogStore.CreateLoadLogStore(progressDlg, filename);
            loader.RefreshEvents = true;

            loader.StackTraceString = true;
            loader.LoadStackDb = true;
            loader.MinimumProgress = minimum;
            loader.MaximumProgress = maximum;
            loader.TraceId = TraceId;
            loader.ModulePath = ModulePath;
            loader.ProcessInfo = _processInfo;
            loader.Begin += OnBegin;
            loader.End += OnEnd;
            loader.EventsReady += DatabaseMgrOnEventsReady;
            var res = loader.Load();
            loader.EventsReady -= DatabaseMgrOnEventsReady;
            if (loader.MiscLogData != null)
                ObjectId = new Guid(loader.MiscLogData.TraceGuid);

            success = loader.Success;
            error = loader.Error;
            ProcMonLog = loader.ProcMonLog;

            return res;
        }

        public void SetProcessInfo(ProcessInfo processInfo)
        {
            _processInfo = processInfo;
        }

        public ProcessInfo GetProcessInfo()
        {
            return _processInfo;
        }

        public ModulePath ModulePath { get; set; }

        private bool _clearingData = false;

        public void Clear()
        {
            InternalClearAllData(true);
        }

        public void DatabaseClear()
        {
            _databaseMgr.ClearDatabase(TraceId);
        }

        void InternalClearAllData(bool newValue)
        {
            if (!_clearingData)
            {
                ClearControls();
                ClearInternalData();
            }
            _clearingData = newValue;
        }
        void ClearControls()
        {
            if (WindowClear != null)
                WindowClear();
            if (ComClear != null)
                ComClear();
            if (FileSystemClear != null)
                FileSystemClear();
            if (RegistryClear != null)
                RegistryClear();
            if (TraceClear != null)
                TraceClear();
        }
        void ClearFileSystem()
        {
            if (FileSystemClear != null)
                FileSystemClear();
        }
        void ClearRegistry()
        {
            if (RegistryClear != null)
                RegistryClear();
        }

        public delegate DialogResult ApplyFilterDelegate(ProgressDialog dlg, int minimum, int maximum);
        public DialogResult RefreshControls(ProgressDialog dlg, int minimum, int maximum)
        {
            lock (_dataLock)
            {
                Debug.Assert(dlg != null);

                InitTimes();
                _totalEvents = _filteredEvents = 0;

                OnBegin();
                ClearControls();
                ClearFilteredEvents();
                
                _refreshEventCount = 0;
                _refreshMinimum = minimum;
                _refreshMaximum = maximum;

                _refreshDlg = dlg;
                _reportsPending = 0;

                //var data = new EventsReportData(TraceId, false, dlg);
                var data = new EventsReportData(TraceId);
                data.EventsReady += DatabaseMgrOnEventsRefreshDbThread;
                //var data = new EventsReportData(TraceId, DatabaseMgrOnEventsRefreshDbThread);
                //data.EventsReady += DatabaseMgrOnEventsRefresh;
                _databaseMgr.RefreshEvents(data);
                while (!data.Event.WaitOne(50) || _reportsPending > 0)
                {
                    Application.DoEvents();
                }

                dlg.Progress = maximum;
                if(maximum == 100)
                {
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }

                //data.EventsReady -= DatabaseMgrOnEventsRefresh;

                OnEnd();
                DumpTimes();
            }
            return DialogResult.OK;
        }
        public DialogResult RefreshFileSystem(ProgressDialog dlg, int minimum, int maximum)
        {
            lock (_dataLock)
            {
                Debug.Assert(dlg != null);

                InitTimes();

                OnBegin();
                ClearFileSystem();
                ClearFilteredFileSystemEvents();

                _refreshEventCount = 0;
                _refreshMinimum = minimum;
                _refreshMaximum = maximum;

                _refreshDlg = dlg;

                _parcialRefresh = true;

                var data = new EventsReportData(TraceId) {EventsToReport = EventType.FileSystem, ControlInvoker = dlg};
                data.EventsReady += DatabaseMgrOnEventsRefresh;
                _databaseMgr.RefreshEvents(data);
                while (!data.Event.WaitOne(50))
                {
                    Application.DoEvents();
                }

                dlg.Progress = maximum;
                if (maximum == 100)
                {
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }

                data.EventsReady -= DatabaseMgrOnEventsRefresh;
                OnEnd();
                _parcialRefresh = false;
                DumpTimes();
            }
            return DialogResult.OK;
        }
        public DialogResult RefreshRegistry(ProgressDialog dlg, int minimum, int maximum)
        {
            lock (_dataLock)
            {
                Debug.Assert(dlg != null);

                InitTimes();

                OnBegin();
                ClearRegistry();
                ClearFilteredRegistryEvents();

                _refreshEventCount = 0;
                _refreshMinimum = minimum;
                _refreshMaximum = maximum;

                _refreshDlg = dlg;

                _parcialRefresh = true;

                var data = new EventsReportData(TraceId) {EventsToReport = EventType.Registry, ControlInvoker = dlg};
                data.EventsReady += DatabaseMgrOnEventsRefresh;
                _databaseMgr.RefreshEvents(data);
                while (!data.Event.WaitOne(50))
                {
                    Application.DoEvents();
                }

                dlg.Progress = maximum;
                if (maximum == 100)
                {
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }

                data.EventsReady -= DatabaseMgrOnEventsRefresh;
                OnEnd();
                _parcialRefresh = false;
                DumpTimes();
            }
            return DialogResult.OK;
        }
        [Conditional("DEBUG")]
        void InitTimes()
        {
            _times.Clear();
            _totalTime = _totalTrace = _beginUpdateTime = _endUpdateTime = 0;
            RegistryTree.InitTimes();
            FileSystemViewer.InitTimes();
        }

        [Conditional("DEBUG")]
        void DumpTimes()
        {
            Debug.WriteLine("DeviareRunTrace Times");
            foreach (var t in _times)
            {
                Debug.WriteLine(t.Key + "\t" + t.Value.Count + "\t" + t.Value.Time + "\t" + (t.Value.Time / t.Value.Count));
            }
            Debug.WriteLine("BeginUpdate:\t" + _beginUpdateTime + "\nEndUpdate:\t" + _endUpdateTime);
            Debug.WriteLine("Total:\t" + _totalTime + "\tTrace:\t" + _totalTrace);

            RegistryTree.DumpTimes();
            FileSystemViewer.DumpTimes();
        }

        /// <summary>
        /// Save collected events with a showing progress dialog to the user
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="filename"></param>
        /// <param name="xmlMode"> </param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public DialogResult SaveLog(Form parent, string filename, bool xmlMode, out bool success, out string error)
        {
            var logStore = LogStore.CreateSaveLogStore(parent, filename, ProcMonLog);
            logStore.XmlMode = xmlMode;
            logStore.ModulePath = ModulePath;
            logStore.ProcessInfo = _processInfo;
            logStore.StartTime = StartTime;
            logStore.TraceId = TraceId;
            logStore.MiscLogData = new MiscLogData
                                       {
                                           TraceGuid = ObjectId.ToString()
                                       };

            //logStore.Events = _callEvents;

            var res = logStore.Save();
            success = logStore.Success;
            error = logStore.Error;

            return res;
        }

        /// <summary>
        /// Save collected events UI less
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public DialogResult SaveLog(string filename, out bool success, out string error)
        {
            var logStore = LogStore.CreateSaveLogStore(filename, ProcMonLog);
            logStore.XmlMode = false;
            logStore.ModulePath = ModulePath;
            logStore.ProcessInfo = _processInfo;
            logStore.StartTime = StartTime;
            Debug.Assert(false);
            //logStore.Events = _callEvents;

            var res = logStore.Save();
            success = logStore.Success;
            error = logStore.Error;

            return res;
        }

        void FilterChange(object sender, EventArgs e)
        {
            if (_totalEvents == 0)
                return;

            var dlg = new ProgressDialog { Title = "Applying filter", Message = "Applying filter ..." };

            if(_formParent != null)
            {
                _formParent.BeginInvoke(new ApplyFilterDelegate(RefreshControls), dlg, 0, 100);
                dlg.ShowDialog(_formParent);
                GCTools.AsyncCollectDelayed(10000);
            }
        }

        /// <summary>
        /// Clear and insert all the events again. Useful when after UI change (e.g.: a control is changed and lost all its data)  
        /// </summary>
        public void RefreshEvents()
        {
            var dlg = new ProgressDialog { Title = "Refreshing", Message = "Inserting events ..." };

            if(_formParent != null)
            {
                _formParent.BeginInvoke(new ApplyFilterDelegate(RefreshControls), dlg, 0, 100);
                dlg.ShowDialog(_formParent);
            }
        }
        public void RefreshFileSystemEvents()
        {
            var dlg = new ProgressDialog { Title = "Refreshing File System Events", Message = "Inserting events ..." };

            if (_formParent != null)
            {
                _formParent.BeginInvoke(new ApplyFilterDelegate(RefreshFileSystem), dlg, 0, 100);
                dlg.ShowDialog(_formParent);
            }
        }
        public void RefreshRegistryEvents()
        {
            var dlg = new ProgressDialog { Title = "Refreshing Registry Events", Message = "Inserting events ..." };

            if (_formParent != null)
            {
                _formParent.BeginInvoke(new ApplyFilterDelegate(RefreshRegistry), dlg, 0, 100);
                dlg.ShowDialog(_formParent);
            }
        }

        bool IsFiltered(CallEvent callEvent)
        {
            var filtered = false;
            if (_filter != null)
            {
                filtered = _filter.IsFiltered(callEvent);
            }
            return filtered;
        }

        public int PendingEventsToAdd()
        {
            return _databaseMgr.PendingEventsToAdd();
        }
        //List<CallEvent> _events = new List<CallEvent>(); 
        public CallEvent ProcessNewEventWithoutAdding(CallEvent callEvent)
        {
            _elapsedTimeWithoutEvents.Start();

            //if (callEvent.Type == HookType.CreateDialog)
            //    Console.WriteLine();

            callEvent.TraceId = TraceId;
            if (string.IsNullOrEmpty(callEvent.ProcessName))
                callEvent.ProcessName = _processInfo.GetName(callEvent.Pid);

            // when the process is created the path isn't available -> we wait until it's available. Meanwhile, keep it in _pendingEvents
            if (callEvent.Type == HookType.CreateProcess)
            {
                if (string.IsNullOrEmpty(callEvent.Params[0].Value))
                {
                    var pid = CreateProcessEvent.GetNewPid(callEvent);
                    if (pid != 0)
                    {
                        var path = _processInfo.GetPath(pid);
                        //Debug.Assert(!String.IsNullOrEmpty(path));
                        callEvent.Params[0].Value = path;
                    }
                    else
                    {
                        // there is no information
                        callEvent.Params[0].Value = string.Empty;
                    }
                }
            }

            _lastCallNumberProcessed = callEvent.CallNumber;
            return callEvent;
        }

        public void ProcessNewEvent(CallEvent callEvent)
        {
            _databaseMgr.AddEvent(ProcessNewEventWithoutAdding(callEvent));
        }

        private void AddEventListToDb(IEnumerable<CallEvent> callEvents)
        {
            _databaseMgr.AddEventRange(callEvents);
        }

        public void ProcessListOfNewEvents(List<CallEvent> callEvents)
        {
            var pecm = PendingEventsCountManager.GetInstance();
            pecm.EventsEnter(callEvents.Count, PendingEventsCountManager.ProcessNewEventPhase);
            for (int i = 0; i < callEvents.Count; i++)
                callEvents[i] = ProcessNewEventWithoutAdding(callEvents[i]);
            pecm.EventsLeave(callEvents.Count, PendingEventsCountManager.ProcessNewEventPhase);
            pecm.EventsEnter(callEvents.Count, PendingEventsCountManager.CompleteEventPhase);
            AddEventListToDb(callEvents);
        }

        public class EventInfoChangeArgs : EventArgs
        {
            public EventInfoChangeArgs(int totalEvents, int filteredEvents)
            {
                TotalEvents = totalEvents;
                FilteredEvents = filteredEvents;
            }

            public int TotalEvents { get; private set; }

            public int FilteredEvents { get; private set; }
        }

        public delegate void EventInfoHandler(object sender, EventInfoChangeArgs e);


        public event Action<RegValueInfo> QueryValueAdd;
        public event Action<RegValueInfo> SetValueAdd;
        public event Action<RegValueInfo> DeleteValueAdd;
        public event Action<RegKeyInfo> OpenKeyAdd;
        public event Action<RegKeyInfo> CreateKeyAdd;
        public event Action<RegKeyInfo> EnumerateKeyAdd;
        public event CallEventHandler LoadLibraryAdd;
        public event CallEventHandler FindResourceAdd;
        public event CallEventHandler OpenFileAdd;
        public event CallEventHandler QueryDirectoryFileAdd;
        public event CallEventHandler QueryAttributesFileAdd;
        public event CallEventHandler CreateDirectoryAdd;
        public event CallEventHandler CreateProcessAdd;
        public event Action<ComObjectInfo> CoCreateAdd;
        public event Action<WindowInfo> CreateWindowAdd;
        public event CallEventHandler CustomAdd;
        public event CallEventHandler EventAdd;

        public event CallEventHandler EventUpdated;

        public event EventHandler UpdateBegin;
        public event EventHandler UpdateEnd;
        public event EventHandler LoadEnd;
        public event Action WindowClear;
        public event Action ComClear;
        public event Action FileSystemClear;
        public event Action RegistryClear;
        public event Action TraceClear;

        public event EventHandler FirstFileSystemEvent;
        public event EventHandler FirstRegistryEvent;
        public event EventHandler FirstCoCreateEvent;
        public event EventHandler FirstWindowEvent;

        public event EventInfoHandler EventInfoChanged;

        private int _totalEvents, _filteredEvents;
        private bool _parcialRefresh;

        class TimeElapsed
        {
            public double Time { get; set; }
            public int Count { get; set; }
        }

        readonly Dictionary<string, TimeElapsed> _times = new Dictionary<string, TimeElapsed>();
        private double _totalTime, _totalTrace, _beginUpdateTime, _endUpdateTime;
        private long _eventsLoaded;

        public UInt64 LastProcessedCallNumber { get; set; }

        void ProcessUIEvent(CallEvent callEvent)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            //ProcessNewEventWithoutAdding(callEvent);
#endif
            callEvent.IsFiltered = IsFiltered(callEvent);
            if (LastProcessedCallNumber < callEvent.CallNumber)
                LastProcessedCallNumber = callEvent.CallNumber;

            if (!callEvent.IsFiltered && !callEvent.Before)
            {
                //if (!_parcialRefresh && !callEvent.IsGenerated && (!callEvent.Before || callEvent.OnlyBefore))
                //    _filteredEvents++;
                if (!_parcialRefresh && (!callEvent.Before || callEvent.OnlyBefore))
                    _filteredEvents++;

                switch (callEvent.Type)
                {
                    case HookType.RegEnumerateValueKey:
                    case HookType.RegQueryValue:
                        if (QueryValueAdd != null)
                            QueryValueAdd(RegValueInfo.From(callEvent));

                        break;
                    case HookType.RegQueryMultipleValues:

                        // TODO: implement for multiple values
                        //AddComServerInfo(callEvent);
                        //Debug.Assert(false, "Should implement RegQueryMultipleValues handling logic.");
                        //if (QueryMultipleValuesAdd != null)
                        //    QueryMultipleValuesAdd(RegValueInfo.FromMultipleQuery(callEvent));

                        break;
                    case HookType.RegSetValue:

                        if (SetValueAdd != null)
                            SetValueAdd(RegValueInfo.From(callEvent));

                        break;
                    case HookType.RegDeleteValue:

                        if (DeleteValueAdd != null)
                            DeleteValueAdd(RegValueInfo.From(callEvent));

                        break;
                    case HookType.RegCreateKey:

                        if (CreateKeyAdd != null)
                            CreateKeyAdd(RegKeyInfo.From(callEvent));

                        break;
                    case HookType.RegEnumerateKey:

                        if (EnumerateKeyAdd != null)
                            EnumerateKeyAdd(RegKeyInfo.From(callEvent));

                        break;
                    case HookType.RegQueryKey:
                    case HookType.RegOpenKey:

                        if (OpenKeyAdd != null)
                            OpenKeyAdd(RegKeyInfo.From(callEvent));

                        break;
                    case HookType.LoadLibrary:

                        if (LoadLibraryAdd != null)
                            LoadLibraryAdd(this, new CallEventArgs(callEvent));

                        break;
                    case HookType.FindResource:

                        if (FindResourceAdd != null)
                            FindResourceAdd(this, new CallEventArgs(callEvent));

                        break;
                    case HookType.OpenFile:
                    case HookType.CreateFile:
                    case HookType.ReadFile:
                    case HookType.WriteFile:
                    case HookType.SetAttributesFile:
                    case HookType.CloseFile:
                        if (OpenFileAdd != null)
                            OpenFileAdd(this, new CallEventArgs(callEvent));

                        break;
                    case HookType.QueryDirectoryFile:

                        if (QueryDirectoryFileAdd != null)
                            QueryDirectoryFileAdd(this, new CallEventArgs(callEvent));

                        break;
                    case HookType.QueryAttributesFile:

                        if (QueryAttributesFileAdd != null)
                            QueryAttributesFileAdd(this, new CallEventArgs(callEvent));

                        break;
                    case HookType.CreateDirectory:

                        if (CreateDirectoryAdd != null)
                            CreateDirectoryAdd(this, new CallEventArgs(callEvent));

                        break;
                    case HookType.GetClassObject:
                    case HookType.CoCreate:

                        if (CoCreateAdd != null)
                            CoCreateAdd(ComObjectInfo.From(callEvent));
                        if (!_firstCoCreateEventNotified)
                        {
                            _firstCoCreateEventNotified = true;
                            if (FirstCoCreateEvent != null)
                                FirstCoCreateEvent(this, new EventArgs());
                        }
                        _hasCoCreateEvents = true;

                        break;
                    case HookType.CreateDialog:
                    case HookType.CreateWindow:

                        if (CreateWindowAdd != null)
                            CreateWindowAdd(WindowInfo.From(callEvent, ModulePath));
                        if (!_firstWindowEventNotified)
                        {
                            _firstWindowEventNotified = true;
                            if (FirstWindowEvent != null)
                                FirstWindowEvent(this, new EventArgs());
                        }
                        _hasWindowsEvents = true;

                        break;
                    case HookType.CreateProcess:
                    case HookType.ProcessStarted:

                        if (CreateProcessAdd != null)
                            CreateProcessAdd(this, new CallEventArgs(callEvent));

                        break;
                    default:

                        switch (callEvent.Function)
                        {
                            case "RpcRT4.NdrDllGetClassObject":
                            case "Ole32.CoGetClassObject":
                                if (CoCreateAdd != null)
                                    CoCreateAdd(ComObjectInfo.From(callEvent));
                                if (!_firstCoCreateEventNotified)
                                {
                                    _firstCoCreateEventNotified = true;
                                    if (FirstCoCreateEvent != null)
                                        FirstCoCreateEvent(this, new EventArgs());
                                }
                                _hasCoCreateEvents = true;
                                break;
                            case "LoadResource":
                                if (FindResourceAdd != null)
                                    FindResourceAdd(this, new CallEventArgs(callEvent));
                                break;
                            default:
                                if (CustomAdd != null)
                                    CustomAdd(this, new CallEventArgs(callEvent));
                                break;
                        }

                        break;
                }
                
                if (!_firstFileSystemEventNotified && callEvent.IsFileSystem)
                {
                    _firstFileSystemEventNotified = true;
                    _hasFileSystemEvents = true;

                    if (FirstFileSystemEvent != null)
                        FirstFileSystemEvent(this, new EventArgs());
                }
                else if (!_firstRegistryEventNotified && callEvent.IsRegistry)
                {
                    _firstRegistryEventNotified = true;
                    _hasRegistryEvents = true;
                    if (FirstRegistryEvent != null)
                        FirstRegistryEvent(this, new EventArgs());
                }    
            }

            //if (!_parcialRefresh && !callEvent.IsGenerated && (!callEvent.Before || callEvent.OnlyBefore))
            if (!_parcialRefresh && (!callEvent.Before || callEvent.OnlyBefore))
                _totalEvents++;

#if DEBUG
            var prevTime = sw.Elapsed.TotalMilliseconds;
#endif

            if (!_parcialRefresh && EventAdd != null)
                EventAdd(this, new CallEventArgs(callEvent, callEvent.IsFiltered));
#if DEBUG
            _totalTrace += sw.Elapsed.TotalMilliseconds - prevTime;
#endif
            if (!_parcialRefresh && EventInfoChanged != null)
                EventInfoChanged.Invoke(this, new EventInfoChangeArgs(_totalEvents, _filteredEvents));

#if DEBUG
            if (!_times.ContainsKey(callEvent.Function))
                _times.Add(callEvent.Function, new TimeElapsed());
            _times[callEvent.Function].Time += sw.Elapsed.TotalMilliseconds;
            _times[callEvent.Function].Count++;
            _totalTime += sw.Elapsed.TotalMilliseconds;
#endif
        }

        public void ClearOnLogLoadFinished()
        {
            if (_loader != null && _loader.IsLoading)
                _loader.LoadProcessFinished += ClearAllDataAndUnsubscribe;
            else
                Clear();
        }

        private void ClearAllDataAndUnsubscribe()
        {
            _loader.LoadProcessFinished -= ClearAllDataAndUnsubscribe;
            Clear();
        }
    }
}
