using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;
using Ionic.Zlib;
using ProtoBuf;
using SpyStudio.COM;
using SpyStudio.Properties;
using SpyStudio.Tools;
using Timer = System.Windows.Forms.Timer;

namespace SpyStudio.Database
{
    #region SQL Functions
    [SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
    class RegEx : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            return Regex.IsMatch(Convert.ToString(args[1]), Convert.ToString(args[0]));
        }
    }
    [SQLiteFunction(Name = "REGEXPNOCASE", Arguments = 2, FuncType = FunctionType.Scalar)]
    class RegExCaseInsensitive : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            return Regex.IsMatch(Convert.ToString(args[0]), Convert.ToString(args[1]),
                                                                RegexOptions.IgnoreCase);
        }
    }
    #endregion

    #region Event Args and Handlers
    public class TraceIdEventArgs : EventArgs
    {
        public TraceIdEventArgs(UInt64 traceId)
        {
            TraceId = traceId;
        }

        public UInt64 TraceId { get; private set; }
    }

    public class EventsReadyArgs : EventArgs
    {
        public EventsReadyArgs(List<CallEvent> events, UInt64 traceId)
        {
            Events = events;
            TraceId = traceId;
        }

        public List<CallEvent> Events { get; private set; }
        public UInt64 TraceId { get; private set; }
        public bool Canceled { get; set; }

        /// <summary>
        /// Total events remaing to report
        /// </summary>
        public int TotalEventCount { get; set; }
    }

    public class EventsRefreshArgs : EventsReadyArgs
    {
        public EventsRefreshArgs(List<CallEvent> events, UInt64 traceId, uint refreshId)
            : base(events, traceId)
        {
            RefreshId = refreshId;
        }

        public uint RefreshId { get; set; }
    }

    public class PendingEventsChangeEventArgs : EventArgs
    {
        public PendingEventsChangeEventArgs(int newValue)
        {
            PendingEvents = newValue;
        }

        public int PendingEvents { get; private set; }
    }

    public delegate void EventsReadyEventHandler(object sender, EventsReadyArgs e);

    public delegate void EventsRefreshEventHandler(object sender, EventsRefreshArgs e);

    public delegate void PendingEventsChangeEventHandler(object sender, PendingEventsChangeEventArgs e);

    public delegate void TraceIdEventHandler(object sender, TraceIdEventArgs e);

    [Flags]
    public enum EventType
    {
        None = 0,
        Registry = 1 << 0,
        FileSystem = 1 << 1,
        Com = 1 << 2,
        Window = 1 << 3,
        Services = 1 << 4,
        //If you add more flags, set End to twice the value of the last flag.
        End = 1 << 5,
        All = -1
    }

    [Flags]
    public enum EventFlags
    {
        None = 0,
        Registry = 1 << 0,
        FileSystem = 1 << 1,
        Com = 1 << 2,
        Window = 1 << 3,
        Services = 1 << 4,
        Virtualized = 1 << 5,
        Critical = 1 << 6,
        Generated = 1 << 7,
        ProcMon = 1 << 8,
        Before = 1 << 11,
        OnlyBefore = 1 << 12,
        TypeProcessed = 1 << 13,
        //If you add more flags, set End to twice the value of the last flag.
        End = 1 << 14,
        All = -1
    }

    public class EventsReportData
    {
        public enum EventResult
        {
            All = 0,
            Success = 1,
            Failed = 2
        }
        private bool _beginReported;
        private readonly object _refreshIdLock = new object();
        private uint _nextRefreshId;

        /// <param name="traceId">TraceId of the events to refresh</param>
        public EventsReportData(uint traceId)
        {
            Init(traceId);
        }
        private void Init(uint traceId)
        {
            RefreshId = GetNextRefreshId();
            TraceId = traceId;
            EventResultsIncluded = EventResult.All;
            EventsToReport = EventType.All;
            ReportBeforeEvents = true;
        }

        uint GetNextRefreshId()
        {
            lock (_refreshIdLock)
            {
                return ++_nextRefreshId;
            }
        }


        public uint RefreshId { get; private set; }
        public uint TraceId { get; set; }
        public EventResult EventResultsIncluded { get; set; }
        
        // Indicates if the events contains StackTraceString information or not.
        public bool StackTraceString { get; set; }
        // Indicates if the events contains Win32Function information or not.
        public bool Win32Function { get; set; }

        public ManualResetEvent Event { get; set; }
        public Control ControlInvoker { get; set; }
        public OnEventsReadyDeleage Notifier { get; set; }

        public EventType EventsToReport { get; set; }
        public bool ReportBeforeEvents { get; set; }

        public void Cancel()
        {
            EventDatabaseMgr.GetInstance().RefreshCancel(RefreshId);
        }

        public ulong MaxEventsToReport = 0;
        private ulong _eventsReported = 0;

        public delegate bool OnEventsReadyDeleage(List<CallEvent> evToReport, int totalEvents);

        public bool OnEventsReady(List<CallEvent> evToReport, int totalEvents)
        {
            if (EventsReady == null)
                return true;
            if (ControlInvoker != null && ControlInvoker.InvokeRequired)
            {
                return
                    (bool)
                    ControlInvoker.Invoke(new OnEventsReadyDeleage(OnEventsReady), evToReport, totalEvents);
            }
            if (!_beginReported)
            {
                _beginReported = true;
                if (ReportBegin != null)
                    ReportBegin(this, new EventArgs());
            }

            if (MaxEventsToReport > 0 && _eventsReported + (ulong)evToReport.Count > MaxEventsToReport)
            {
                var size = MaxEventsToReport - _eventsReported;
                if (size == 0)
                {
                    if (ReportEnd != null)
                        ReportEnd(this, new EventArgs());
                    return false;
                }
                var temp = new CallEvent[size];
                evToReport.CopyTo(0, temp, 0, temp.Length);
                evToReport = temp.ToList();
            }

            bool cancelled = false;

            if (evToReport != null && evToReport.Count > 0)
            {
                var args = new EventsRefreshArgs(evToReport, TraceId, RefreshId) {TotalEventCount = totalEvents};
                EventsReady(this, args);
                _eventsReported += (ulong)evToReport.Count;

                cancelled = args.Canceled;
            }

            if ((_eventsReported == (ulong) totalEvents || cancelled) && ReportEnd != null)
                ReportEnd(this, new EventArgs());

            return !cancelled;
        }

        public event EventsRefreshEventHandler EventsReady;

        /// <summary>
        /// Reported before the first event is reported
        /// </summary>
        public event EventHandler ReportBegin;

        /// <summary>
        /// Reported after the last event reported. If the process is canceled from this class this event is reported
        /// after the cancelation
        /// </summary>
        public event EventHandler ReportEnd;
    }

    #endregion

    public class EventDatabaseMgr
    {
        #region Database Operations
        private class DbOperation
        {
            public enum DatabaseOperationType
            {
                GetEvents,
                SearchEvents,
                GetFirstEvent,
                GetAllEvents,
                GetEventCount,
                Refresh,
                Clear,
                Lock,
                Unlock,
                UpdateEventProperties,
            }

            protected DbOperation(DatabaseOperationType type, uint traceId)
            {
                Type = type;
                TraceId = traceId;
                WaitEvent = new ManualResetEvent(false);
            }

            public uint TraceId { get; protected set; }
            public DatabaseOperationType Type { get; private set; }
            public ManualResetEvent WaitEvent { get; private set; }
        }

        private class RefreshEventsDbOperation : DbOperation
        {
            private EventsReportData _reportData;

            public RefreshEventsDbOperation(uint traceId, uint refreshId)
                : base(DatabaseOperationType.Refresh, traceId)
            {
                RefreshId = refreshId;
            }

            public List<CallEvent> PendingEvents { get; set; }
            public bool StackTraceString { get { return _reportData.StackTraceString; } }
            public bool Win32Function { get { return _reportData.Win32Function; } }
            public int TotalEvents { get; set; }
            public int ReportedEvents { get; set; }
            public bool DatabaseProcessingFinished { get; set; }
            public bool Canceled { get; set; }
            public uint RefreshId { get; private set; }

            public EventsReportData ReportData
            {
                get { return _reportData; }
                set
                {
                    _reportData = value;
                    _reportData.Event = WaitEvent;
                }
            }
        }

        private class SaveOrLoadVirtualizatoinStateDbOperation : DbOperation
        {
            protected SaveOrLoadVirtualizatoinStateDbOperation(uint traceId, DatabaseOperationType dbot, string state)
                : base(dbot, traceId)
            {
                TraceId = traceId;
                State = state;
            }
            public string State;
        }

        private class ClearDbOperation : DbOperation
        {
            public ClearDbOperation(uint traceId)
                : base(DatabaseOperationType.Clear, traceId)
            {
                TraceId = traceId;
            }
        }

        private class GetFirstEventDbOperation : DbOperation
        {
            public GetFirstEventDbOperation(uint traceId)
                : base(DatabaseOperationType.GetFirstEvent, traceId)
            {
                TraceId = traceId;
            }
            public CallEvent Event { get; set; }
        }

        private class GetEventsDbOperation : DbOperation
        {
            public GetEventsDbOperation(CallEventId[] eventIds, bool fullStackInfo)
                : base(DatabaseOperationType.GetEvents, eventIds[0].TraceId)
            {
                EventIds = eventIds;
                FullStackInfo = fullStackInfo;
            }

            public CallEventId[] EventIds { get; private set; }
            public CallEvent[] Events { get; set; }
            public bool FullStackInfo { get; private set; }
        }

        private class SearchEventsDbOperation : DbOperation
        {
            public SearchEventsDbOperation(uint traceId)
                : base(DatabaseOperationType.SearchEvents, traceId)
            {
            }

            public int MaxEventCountToRetrieve { get; set; }
            public string SearchText { get; set; }
            public bool MatchWholeWord { get; set; }
            public bool MatchCase { get; set; }
            public UInt64 StartCallNumber { get; set; }
            public bool SearchForward { get; set; }
            public HashSet<UInt64> CallNumberFilter { get; set; }
            public CallEvent[] Events { get; set; }
        }

        private class GetAllEventsDbOperation : DbOperation
        {
            public GetAllEventsDbOperation(uint traceId)
                : base(DatabaseOperationType.GetAllEvents, traceId)
            {
            }

            public CallEvent[] Events { get; set; }
        }

        private class GetEventCountDbOperation : DbOperation
        {
            public GetEventCountDbOperation(uint traceId)
                : base(DatabaseOperationType.GetEventCount, traceId)
            {
                TraceId = traceId;
            }

            public int Count { get; set; }
        }

        private class LockDbOperation : DbOperation
        {
            public LockDbOperation(uint traceId)
                : base(DatabaseOperationType.Lock, traceId)
            {
            }
        }

        private class UnlockDbOperation : DbOperation
        {
            public UnlockDbOperation(uint traceId)
                : base(DatabaseOperationType.Unlock, traceId)
            {
            }
        }

        private class UpdateEventPropertiesDbOperation : DbOperation
        {
            public UpdateEventPropertiesDbOperation(CallEventId eventId, EventProperties evProps)
                : base(DatabaseOperationType.UpdateEventProperties, eventId.TraceId)
            {
                EventId = eventId;
                EventProperties = evProps;
            }
            public CallEventId EventId { get; private set; }
            public EventProperties EventProperties { get; private set; }
        }
        #endregion

        public class EventProperties
        {
            private bool _setEventFlags;
            private EventFlags _eventFlags;
            private bool _setPriority;
            private int _priority;

            public EventProperties()
            {
            }

            public EventProperties(CallEvent callEvent)
            {
                Priority = callEvent.Priority;
                EventFlags = callEvent.EventFlags;
            }
            public EventFlags EventFlags
            {
                get { return _eventFlags; }
                set
                {
                    _setEventFlags = true;
                    _eventFlags = value;
                }
            }
            public int Priority
            {
                get { return _priority; }
                set
                {
                    _setPriority = true;
                    _priority = value;
                }
            }
            public bool EventFlagsSet
            {
                get { return _setEventFlags; }
            }
            public bool PrioritySet
            {
                get { return _setPriority; }
            }
        }

        public class EventThreadWorkerData
        {
            public List<CallEvent> Events { get; set; }
            public ManualResetEvent TerminationEvent = new ManualResetEvent(false);
            public ManualResetEvent WakeupEvent = new ManualResetEvent(false);
            public Thread Thread;
        }
        public class DatabaseInfo
        {
            public string EventDbPath;
            public string StackDbPath;
        }
        public class StackInfoToAdd
        {
            public UInt64 CallNumber;
            public string[] StackStrings;
            public byte[] StackSerialized;
        }
        #region private variables

        private Thread _dbThread, _dbStackThread;
        private Thread _completeThread;
        private ManualResetEvent _dbShutdownEvent;
        private readonly AutoResetEvent _stackDbEvent = new AutoResetEvent(false);
        private readonly object _stackDbLock = new object();

        private readonly Dictionary<uint, List<CallEvent>> _pendingEventsToAdd =
            new Dictionary<uint, List<CallEvent>>();

        private readonly Dictionary<uint, List<StackInfoToAdd>> _pendingStacksToAdd =
            new Dictionary<uint, List<StackInfoToAdd>>();

        private readonly Dictionary<uint, Dictionary<UInt64, CallEvent>> _pendingEventsToAddById = new Dictionary
            <uint, Dictionary<ulong, CallEvent>>();

        private readonly HashSet<uint> _lockedDbs = new HashSet<uint>();
        private readonly Dictionary<uint, List<CallEvent>> _pendingNewEventsToReport = new Dictionary<uint, List<CallEvent>>();
        private Dictionary<uint, List<CallEvent>> _pendingUpdatedEventsToReport = new Dictionary<uint, List<CallEvent>>();
        private readonly Timer _timer = new Timer();

        private readonly Dictionary<uint, RefreshEventsDbOperation> _refreshEventsData =
            new Dictionary<uint, RefreshEventsDbOperation>();

        private Dictionary<uint, List<CallEvent>> _eventsToComplete = new Dictionary<uint, List<CallEvent>>();
        private readonly AutoResetEvent _completeEvent = new AutoResetEvent(false);

        private readonly HashSet<uint> _clearedDbs = new HashSet<uint>();

        //private readonly HashSet<uint> _clearDatabaseData = new HashSet<uint>();
        //private readonly List<EventInfoData> _pendingGetEventOps = new List<EventInfoData>();
        private readonly List<DbOperation> _pendingOps = new List<DbOperation>();
        private readonly Dictionary<uint, DatabaseInfo> _dbTraceIdPath = new Dictionary<uint, DatabaseInfo>();
        public const int MaxEventCountProcessPerAction = 1800;
        public const int CaptureTraceId = 1;

        public const string SerializedEvent = "DbEvent";
        public const string SerializedEventCallStack = "DbEventCallStack";
        public const string SerializedEventParameters = "DbEventParameters";

        private string _insertEventString, _selectEventString, _selectEventStackString;

        private readonly EventPriorityAnalyzer _priorityAnalyzer;
        private readonly EventPeerMatching _eventPeerMatching;
        private int _clearId = 1;

        private static EventDatabaseMgr _instance;

        #endregion

        #region Events

        public event EventsReadyEventHandler EventsReady;
        public event EventsReadyEventHandler EventsUpdated;
        public event EventsRefreshEventHandler EventsRefresh;
        public event TraceIdEventHandler EventsCleared;

        #endregion

        public static EventDatabaseMgr GetInstance(bool force)
        {
            if(_instance == null && force)
                _instance = new EventDatabaseMgr();
            return _instance;
        }
        public static EventDatabaseMgr GetInstance()
        {
            return GetInstance(true);
        }

        private EventDatabaseMgr()
        {
            SQLiteFunction.RegisterFunction(typeof(RegExCaseInsensitive));

            // HACK: if there is another instance do not remove the old database since they may be being used
            // by the other instance
            Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            // only me
            if (processes.Length == 1)
                CleanDatabases();
            _priorityAnalyzer = new EventPriorityAnalyzer(this);
            _eventPeerMatching = new EventPeerMatching(this);
        }

        public void Shutdown()
        {
            if (_dbThread != null)
            {
                _dbShutdownEvent.Set();
                _dbThread.Join();
            }
            if (_dbStackThread != null)
            {
                _dbShutdownEvent.Set();
                _dbStackThread.Join();
            }
            if (_completeThread != null)
            {
                _completeThread.Join();
            }
        }

        public void CleanDatabases()
        {
            try
            {
                var dir = new DirectoryInfo(GetDatabaseDirectory());
                dir.Delete(true);
            }
            catch (Exception)
            {
            }
        }
        private delegate void StartDelegate(Control reportControlThread);

        /// <summary>
        /// Start Database thread
        /// </summary>
        /// <param name="reportControlThread">used to report events in the UI thread</param>
        public void Start(Control reportControlThread)
        {
            if (reportControlThread != null && reportControlThread.InvokeRequired)
            {
                reportControlThread.Invoke(new StartDelegate(Start), reportControlThread);
            }
            else
            {
                if (_dbThread == null)
                {
                    // if the database is started without Control owner it doesn't have an UI to report
                    if (reportControlThread != null)
                    {
                        _timer.Interval = Settings.Default.UIUpdateInterval;
                        _timer.Enabled = true;
                        _timer.Tick += ReportEvents;
                        _timer.Start();
                    }

                    _dbShutdownEvent = new ManualResetEvent(false);
                    _dbThread = new Thread(DatabaseThread);
                    _dbThread.Start(_dbShutdownEvent);

                    _dbStackThread = new Thread(StackDatabaseThread);
                    _dbStackThread.Start(_dbShutdownEvent);

                    _completeThread = new Thread(CompleteThread);
                    _completeThread.Start(_dbShutdownEvent);
                }
            }
        }


        /// <summary>
        /// Save space keeping only basic information of the event
        /// </summary>
        /// <param name="e"></param>
        public static void CleanEvent(CallEvent e)
        {
            e.CallStack = null;
        }

        private Dictionary<uint, CallEvent[]> GetEventsToReport()
        {
            var eventsToReportNow = new Dictionary<uint, CallEvent[]>();
            lock (_pendingNewEventsToReport)
            {
                if (_pendingNewEventsToReport.Count > 0)
                {
                    //int pendingAddEvents = 0;
                    var traceIds = _pendingNewEventsToReport.Keys.ToList();
                    foreach (var traceId in traceIds)
                    {
                        var eventList = _pendingNewEventsToReport[traceId];
                        if (eventList.Count > 0)
                        {
                            //pendingAddEvents += evList.Count;
                            if (eventList.Count > MaxEventCountProcessPerAction)
                            {
                                var eventsToProcess = eventList.GetRange(0, MaxEventCountProcessPerAction);
                                eventsToReportNow[traceId] = new CallEvent[MaxEventCountProcessPerAction];
                                eventsToProcess.CopyTo(eventsToReportNow[traceId]);
                                _pendingNewEventsToReport[traceId] = eventList.GetRange(MaxEventCountProcessPerAction,
                                                                                        eventList.Count -
                                                                                        MaxEventCountProcessPerAction);
                            }
                            else
                            {
                                eventsToReportNow[traceId] = new CallEvent[eventList.Count];
                                eventList.CopyTo(eventsToReportNow[traceId]);
                                eventList.Clear();
                            }
                        }
                    }
                }
            }
            return eventsToReportNow;
        }

        private void ReportEvents(object sender, EventArgs e)
        {
//#if DEBUG
//            // ReSharper disable TooWideLocalVariableScope
//            int count = 0;
//            // ReSharper restore TooWideLocalVariableScope
//            var sw = new Stopwatch();
//            sw.Start();
//#endif

            lock (_clearedDbs)
            {
                if (_clearedDbs.Count > 0)
                {
                    foreach (var clearedTraceId in _clearedDbs)
                    {
                        if (EventsCleared != null)
                            EventsCleared(this, new TraceIdEventArgs(clearedTraceId));
                    }
                    _clearedDbs.Clear();
                    return;
                }
            }

            _timer.Enabled = false;

            List<CallEvent> toReport = null;
            uint traceId = 0;
            uint refreshId = 0;
            var totalEvents = 0;
            var canceled = false;

            lock (_refreshEventsData)
            {
                foreach (var refreshDataPair in _refreshEventsData)
                {
                    var refreshData = refreshDataPair.Value;
                    lock (refreshData)
                    {
                        if (refreshData.PendingEvents != null && refreshData.PendingEvents.Count > 0)
                        {
                            toReport = refreshData.PendingEvents;
                            totalEvents = refreshData.TotalEvents;
                            traceId = refreshData.TraceId;
                            refreshId = refreshData.RefreshId;

                            refreshData.ReportedEvents += toReport.Count;
                            if (refreshData.ReportedEvents == refreshData.TotalEvents || refreshData.Canceled)
                            {
                                _refreshEventsData.Remove(refreshId);
                                if (refreshData.Canceled)
                                {
                                    canceled = true;
                                    toReport.Clear();
                                }
                            }
                            refreshData.PendingEvents = null;
                        }
                        else if (refreshData.DatabaseProcessingFinished && refreshData.TotalEvents == 0)
                        {
                            toReport = new List<CallEvent>();
                            totalEvents = 0;
                            traceId = refreshData.TraceId;
                            refreshId = refreshData.RefreshId;

                            _refreshEventsData.Remove(refreshId);
                        }
                    }
                    // one report for each timer tick
                    if (toReport != null)
                        break;
                }
            }
            if (toReport != null)
            {
                var args = new EventsRefreshArgs(toReport, traceId, refreshId) { TotalEventCount = totalEvents, Canceled = canceled };
                if (EventsRefresh != null)
                    EventsRefresh(this, args);
                if (args.Canceled)
                    RefreshCancel(traceId);
            }
            else
            {
                Dictionary<uint, List<CallEvent>> eventsUpdatesToReport;
                lock (_pendingUpdatedEventsToReport)
                {
                    eventsUpdatesToReport = _pendingUpdatedEventsToReport;
                    _pendingUpdatedEventsToReport = new Dictionary<uint, List<CallEvent>>();
                }

                foreach (var evList in eventsUpdatesToReport)
                {
                    if (EventsUpdated != null)
                        EventsUpdated(this, new EventsReadyArgs(evList.Value, evList.Key));
//#if DEBUG
//                    count += evList.Value.Count;
//#endif
                }

                var eventsToReport = GetEventsToReport();

                foreach (var evArray in eventsToReport)
                {
                    if (EventsReady != null)
                        EventsReady(this, new EventsReadyArgs(evArray.Value.ToList(), evArray.Key));
//#if DEBUG
//                    count += evList.Value.Count;
//#endif
                }
//#if DEBUG
//                if (count > 0)
//                    Debug.WriteLine("Events reported: " + count + " Total time: " + sw.Elapsed.TotalMilliseconds);
//#endif
            }

            _timer.Enabled = true;
        }

        public static string GetDatabaseDirectory()
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" +
                            AssemblyTools.AssemblyCompany + "\\" +
                            AssemblyTools.AssemblyProduct + "\\Databases";
            return directory;
        }

        public bool ExistDatabase(uint traceId)
        {
            lock (_dbTraceIdPath)
            {
                return _dbTraceIdPath.ContainsKey(traceId);
            }
        }

        private string GetDbPath(uint traceId, string baseName)
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" +
                AssemblyTools.AssemblyCompany + "\\" +
                AssemblyTools.AssemblyProduct + "\\Databases";
            var baseFilename = directory + "\\" + baseName + traceId;
            string ret = baseFilename;
            var i = 0;
            while (File.Exists(ret))
            {
                ret = baseFilename + i++;
            }
            return ret;
        }
        public DatabaseInfo GetDatabasePath(uint traceId)
        {
            return GetDatabasePath(traceId, false);
        }
        public DatabaseInfo GetDatabasePath(uint traceId, bool createAlways)
        {
            DatabaseInfo ret;
            lock (_dbTraceIdPath)
            {
                var exist = _dbTraceIdPath.TryGetValue(traceId, out ret);
                if (exist && createAlways)
                {
                    _dbTraceIdPath.Remove(traceId);

                    try
                    {
                        File.Delete(ret.EventDbPath);
                        File.Delete(ret.StackDbPath);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (!exist || createAlways)
                {
                    string eventsPath = GetDbPath(traceId, "EventsDb");
                    string stackPath = GetDbPath(traceId, "StackDb");

                    _dbTraceIdPath[traceId] = ret = new DatabaseInfo {EventDbPath = eventsPath, StackDbPath = stackPath};
                }
            }

            return ret;
        }

        private string GetDatabaseConnectionString(uint traceId)
        {
            return "Data Source=" + GetDatabasePath(traceId).EventDbPath + ";Connection Timeout=1;Version=3;PRAGMA synchronous=off;MultipleActiveResultSets=True;";
            //return "Data Source=" + GetDatabasePath(traceId) + ";;Connection Timeout=1;Version=3;PRAGMA cache_size=20000;PRAGMA page_size=32768;PRAGMA synchronous=off";
        }

        private string GetStackDatabaseConnectionString(uint traceId)
        {
            return "Data Source=" + GetDatabasePath(traceId).StackDbPath + ";Connection Timeout=1;Version=3;PRAGMA synchronous=off;MultipleActiveResultSets=True;";
        }

        public void CreateDatabase(uint traceId)
        {
            var dbInfo = GetDatabasePath(traceId, true);
            Directory.CreateDirectory(GetDatabaseDirectory());
            SQLiteConnection.CreateFile(dbInfo.EventDbPath);
            SQLiteConnection.CreateFile(dbInfo.StackDbPath);

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(traceId)))
            {
                con.Open();

                var sql = GetEventsTableCreationSql();
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
                sql = GetParametersTableCreationSql();
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
                sql = GetVirtualizationTableCreationSql();
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
                con.Close();
            }
            using (var con = new SQLiteConnection(GetStackDatabaseConnectionString(traceId)))
            {
                con.Open();

                var sql = GetStackTableCreationSql();
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        private string GetEventsTableCreationSql()
        {
            var eventsDbCreationString = new StringBuilder(
                @"CREATE TABLE Events (
                    CallNumber INTEGER PRIMARY KEY ASC,
                    Peer INTEGER,
                    Priority INTEGER,
                    EventFlags INTEGER,
                    Ancestors BLOB,
                    RetValue INTEGER,
                    HookType INTEGER,
                    ChainDepth INTEGER,
                    Cookie INTEGER,
                    Time DOUBLE,
                    GenerationTime DOUBLE,
                    ParamMainIndex INTEGER,
                    ProcessName TEXT,
                    Pid INTEGER,
                    Tid INTEGER,
                    CallModule TEXT, 
                    StackTraceString TEXT,
                    Function TEXT,
                    Win32Function TEXT,
                    Result TEXT,
                    Success BOOLEAN,
                    PropertiesStringMask INTEGER,
                    PropertiesByteArrayMask INTEGER,
                    ");
            var insertString1 =
                new StringBuilder(
                    @"INSERT INTO Events (
                                    CallNumber,
                                    Peer,
                                    Priority, 
                                    EventFlags,
                                    Ancestors,
                                    RetValue,
                                    HookType,
                                    ChainDepth,
                                    Cookie,
                                    Time,
                                    GenerationTime,
                                    ParamMainIndex,
                                    ProcessName,
                                    Pid,
                                    Tid,
                                    CallModule, 
                                    StackTraceString,
                                    Function,
                                    Win32Function,
                                    Result,
                                    Success,
                                    PropertiesStringMask,
                                    PropertiesByteArrayMask,
                                    ");
            var insertString2 =
                new StringBuilder(
                    @") VALUES (
                                    @CallNumber,
                                    @Peer,
                                    @Priority, 
                                    @EventFlags,
                                    @Ancestors,
                                    @RetValue,
                                    @HookType,
                                    @ChainDepth,
                                    @Cookie,
                                    @Time,
                                    @GenerationTime,
                                    @ParamMainIndex,
                                    @ProcessName,
                                    @Pid,
                                    @Tid,
                                    @CallModule, 
                                    @StackTraceString, 
                                    @Function,
                                    @Win32Function,
                                    @Result,
                                    @Success,
                                    @PropertiesStringMask,
                                    @PropertiesByteArrayMask,
                                    ");

            var selectEventString = new StringBuilder(
                "SELECT Events.CallNumber, Events.Peer, Events.Priority, Events.EventFlags, Events.Ancestors, Events.RetValue," +
                "Events.HookType, Events.ChainDepth, Events.Cookie, Events.Time, Events.GenerationTime, " +
                "Events.ParamMainIndex, Events.ProcessName, Events.Pid, Events.Tid, Events.CallModule, Events.StackTraceString, Events.Function, " +
                "Events.Win32Function, Events.Result, Events.Success, Parameters.Name, Parameters.Value, PropertiesStringMask, PropertiesByteArrayMask, ");
            //var selectEventString = new StringBuilder(
            //    "SELECT Events.CallNumber, Events.Peer, Events.Priority, Events.EventFlags, Events.Ancestors, Events.RetValue," +
            //    "Events.HookType, Events.ChainDepth, Events.Time, Events.GenerationTime, " +
            //    "Events.ParamMainIndex, Events.ProcessName, Events.Pid, Events.Tid, Events.CallModule, Events.StackTraceString, Events.Function, " +
            //    "Events.Win32Function, Events.Result, Events.Success, Parameters.Name, Parameters.Value, ");

            _cmdParamPropertiesString = new SQLiteParameter[CallEvent.MaxPropertyCountString];
            _cmdParamPropertiesUInt = new SQLiteParameter[CallEvent.MaxPropertyCountUInt];
            _cmdParamPropertiesByteArray = new SQLiteParameter[CallEvent.MaxPropertyCountByteArray];

            for (int i = 0; i < CallEvent.MaxPropertyCount; i++)
            {
                if (i < CallEvent.MaxPropertyCountString)
                {
                    eventsDbCreationString.Append("\nPropertyString" + i + " TEXT,");
                    insertString1.Append("\nPropertyString" + i + ",");
                    insertString2.Append("\n@PropertyString" + i + ",");
                    _cmdParamPropertiesString[i] = new SQLiteParameter("@PropertyString" + i);
                    selectEventString.Append("Events.PropertyString" + i + ", ");
                }

                if (i < CallEvent.MaxPropertyCountUInt)
                {
                    eventsDbCreationString.Append("\nPropertyUInt" + i + " INTEGER,");
                    insertString1.Append("\nPropertyUInt" + i + ",");
                    insertString2.Append("\n@PropertyUInt" + i + ",");
                    _cmdParamPropertiesUInt[i] = new SQLiteParameter("@PropertyUInt" + i);
                    selectEventString.Append("Events.PropertyUInt" + i + ", ");
                }
                if (i < CallEvent.MaxPropertyCountByteArray)
                {
                    eventsDbCreationString.Append("\nPropertyByteArray" + i + " BLOB,");
                    insertString1.Append("\nPropertyByteArray" + i + ",");
                    insertString2.Append("\n@PropertyByteArray" + i + ",");
                    _cmdParamPropertiesByteArray[i] = new SQLiteParameter("@PropertyByteArray" + i);
                    selectEventString.Append("Events.PropertyByteArray" + i + ", ");
                }
            }

            selectEventString.Append("Events.PropertiesBool ");
            _selectEventString = selectEventString.ToString();
            //selectEventString.Append(", Stack.FullStackInfo ");
            _selectEventStackString = "SELECT CallNumber, FullStackInfo FROM Stack ";
;

            eventsDbCreationString.Append(@"
                                PropertiesBool INTEGER
                            );");

            insertString1.Append("\nPropertiesBool");
            insertString2.Append("\n@PropertiesBool)");

            _insertEventString = insertString1 + insertString2.ToString();

            return eventsDbCreationString.ToString();
        }

        private string GetParametersTableCreationSql()
        {
            return
                @"CREATE TABLE Parameters (
                    CallNumber INTEGER ASC,
                    Number INTEGER ASC,
                    Name TEXT, 
                    Value TEXT,
                    PRIMARY KEY (CallNumber, Number)
                );";
        }

        private string GetStackTableCreationSql()
        {
//            return
//                @"CREATE TABLE Stack (
//                            CallNumber INTEGER PRIMARY KEY ASC,
//                            FullStackInfo TEXT
//                        );";
            return
                @"CREATE TABLE Stack (
                            CallNumber INTEGER PRIMARY KEY ASC,
                            FullStackInfo BLOB
                        );";
        }

        private string GetVirtualizationTableCreationSql()
        {
            return
                @"CREATE TABLE Virtualization (
                State TEXT
                );";
        }
        private Dictionary<uint, List<CallEvent>> GetEventsToComplete()
        {
            var eventsToProcess = new Dictionary<uint, List<CallEvent>>();

            lock (_eventsToComplete)
            {
                var newEventsToComplete = new Dictionary<uint, List<CallEvent>>();

                foreach (var eventToCompleteByTraceId in _eventsToComplete)
                {
                    if (eventToCompleteByTraceId.Value.Count > 0)
                    {
                        var traceId = eventToCompleteByTraceId.Key;
                        if (eventToCompleteByTraceId.Value.Count > MaxEventCountProcessPerAction)
                        {
                            eventsToProcess[traceId] =
                                eventToCompleteByTraceId.Value.GetRange(0, MaxEventCountProcessPerAction);
                            newEventsToComplete[traceId] =
                                eventToCompleteByTraceId.Value.GetRange(MaxEventCountProcessPerAction,
                                                                        eventToCompleteByTraceId.Value.Count -
                                                                        MaxEventCountProcessPerAction);
                        }
                        else
                        {
                            eventsToProcess[traceId] = _eventsToComplete[traceId];
                        }
                    }
                }
                _eventsToComplete = newEventsToComplete;
            }
            return eventsToProcess;
        }
        private void CompleteThread(object data)
        {
            var shutdownEvent = (ManualResetEvent)data;
            var waitHandles = new WaitHandle[2];
            waitHandles[0] = shutdownEvent;
            waitHandles[1] = _completeEvent;
            int index = WaitHandle.WaitAny(waitHandles);

            while (index == 1)
            {
                var eventsToComplete = GetEventsToComplete();

                var moreEvents = eventsToComplete.Count > 0;
                while (moreEvents)
                {
                    moreEvents = false;
                    if (eventsToComplete.Count > 0)
                    {
                        foreach (var eventsToCompleteByTraceId in eventsToComplete)
                        {
                            var list = eventsToCompleteByTraceId.Value;
                            foreach (var callEvent in list)
                            {
                                CompleteEvent(callEvent);
                            }
                            PendingEventsCountManager.GetInstance().EventsLeave(list.Count, PendingEventsCountManager.CompleteEventPhase);
                            PendingEventsCountManager.GetInstance().EventsEnter(list.Count, PendingEventsCountManager.DatabasePhase);
                            lock (_pendingEventsToAdd)
                            {
                                List<CallEvent> callEventList;
                                if (!_pendingEventsToAdd.TryGetValue(eventsToCompleteByTraceId.Key, out callEventList))
                                {
                                    _pendingEventsToAdd[eventsToCompleteByTraceId.Key] = callEventList = new List<CallEvent>();
                                }
                                callEventList.AddRange(eventsToCompleteByTraceId.Value);
                            }
                        }
                    }

                    if (!shutdownEvent.WaitOne(0))
                    {
                        eventsToComplete = GetEventsToComplete();
                        moreEvents = eventsToComplete.Count > 0;
                    }
                }

                index = WaitHandle.WaitAny(waitHandles);
            }
        }

        private void InsertStacks(Dictionary<uint, StackInfoToAdd[]> stacksToAddNow)
        {
            if (stacksToAddNow.Count > 0)
            {
#if DEBUG
                // ReSharper disable TooWideLocalVariableScope
                double totalConn = 0,
                       totalAdd = 0,
                       totalAddParams = 0,
                       totalCommit = 0;
                // ReSharper restore TooWideLocalVariableScope
                var sw = new Stopwatch();
                sw.Start();
#endif

                int prevClearId = _clearId;

#if DEBUG
                // ReSharper disable TooWideLocalVariableScope
                double prevTime;
                // ReSharper restore TooWideLocalVariableScope
                int count = 0;
#endif

                foreach (var stackDict in stacksToAddNow)
                {
#if DEBUG
                    prevTime = sw.Elapsed.TotalMilliseconds;
#endif

                    lock (_stackDbLock)
                    {
                        // database was cleared before locking
                        if (prevClearId != _clearId)
                            return;

                        using (var con = new SQLiteConnection(GetStackDatabaseConnectionString(stackDict.Key)))
                        {
                            con.Open();

                            using (var cmdStack = con.CreateCommand())
                            {
                                cmdStack.Parameters.Add(_cmdParamStackCallNumber);
                                cmdStack.Parameters.Add(_cmdParamStackFullStackInfo);

                                cmdStack.CommandText =
                                    @"INSERT INTO Stack (
                                        CallNumber,
                                        FullStackInfo
                                    ) VALUES (
                                        @CallNumber,
                                        @FullStackInfo
                                    )";

#if DEBUG
                                totalConn += sw.Elapsed.TotalMilliseconds - prevTime;
#endif

                                using (var tr = con.BeginTransaction())
                                {
                                    foreach (var stackInfo in stackDict.Value)
                                    {
                                        try
                                        {
#if DEBUG
                                            prevTime = sw.Elapsed.TotalMilliseconds;
#endif

                                            //Debug.Assert(stackInfo.StackSerialized != null);

                                            _cmdParamStackCallNumber.Value = stackInfo.CallNumber;
                                            //_cmdParamStackFullStackInfo.Value = stackInfo.StackSerialized;
                                            string stackString = string.Join("|", stackInfo.StackStrings);
                                            //byte[] bytes = Encoding.Default.GetBytes(stackString);
                                            //stackString = Encoding.UTF8.GetString(bytes);
                                            //string stackString = string.Join("|", stackInfo.StackStrings);
                                            _cmdParamStackFullStackInfo.Value = stackString;

#if DEBUG
                                            totalAddParams += sw.Elapsed.TotalMilliseconds - prevTime;
                                            prevTime = sw.Elapsed.TotalMilliseconds;
#endif
                                            cmdStack.ExecuteNonQuery();
#if DEBUG
                                            totalAdd += sw.Elapsed.TotalMilliseconds - prevTime;
                                            count++;
#endif
                                        }
                                        catch (Exception ex)
                                        {
                                            Error.WriteLine("Database Stack exception: " + ex.Message);
                                        }
                                    }
#if DEBUG
                                    prevTime = sw.Elapsed.TotalMilliseconds;
#endif
                                    tr.Commit();
#if DEBUG
                                    totalCommit += sw.Elapsed.TotalMilliseconds - prevTime;
#endif
                                }
                            }
                            con.Close();

                        }
                    }
                }
#if DEBUG && false
                double total = sw.Elapsed.TotalMilliseconds;
                Debug.WriteLine("\nDatabase Stack Insert" +
                                "\nEvents added:\t" + count +
                                "\nTotal time Stack:\t" + total +
                                "\nAdd Parameters to query:\t" + totalAddParams +
                                "\nAdd:\t" + totalAdd +
                                "\nCommit:\t" + totalCommit +
                                "\nConnection:\t" + totalConn +
                                "\nTotal per event:\t" + total/count);
#endif
            }
        }
        private Dictionary<uint, StackInfoToAdd[]> GetStacksToProcess()
        {
            var stacksToAddNow = new Dictionary<uint, StackInfoToAdd[]>();
            lock (_pendingStacksToAdd)
            {
                //stacksToAddNow = _pendingStacksToAdd;
                if (_pendingStacksToAdd.Count > 0)
                {
                    //int pendingAddEvents = 0;
                    var traceIds = _pendingStacksToAdd.Keys.ToList();
                    foreach (var traceId in traceIds)
                    {
                        if (!_lockedDbs.Contains(traceId))
                        {
                            var stackList = _pendingStacksToAdd[traceId];
                            if (stackList.Count > 0)
                            {
                                //pendingAddEvents += evList.Count;
                                if (stackList.Count > MaxEventCountProcessPerAction)
                                {
                                    var eventsToProcess = stackList.GetRange(0, MaxEventCountProcessPerAction);
                                    stacksToAddNow[traceId] = new StackInfoToAdd[MaxEventCountProcessPerAction];
                                    eventsToProcess.CopyTo(stacksToAddNow[traceId]);
                                    _pendingStacksToAdd[traceId] = stackList.GetRange(MaxEventCountProcessPerAction,
                                                                                      stackList.Count -
                                                                                      MaxEventCountProcessPerAction);
                                }
                                else
                                {
                                    stacksToAddNow[traceId] = new StackInfoToAdd[stackList.Count];
                                    stackList.CopyTo(stacksToAddNow[traceId]);
                                    stackList.Clear();
                                }
                            }
                        }
                    }
                }
            }
            return stacksToAddNow;
        }
        private void StackDatabaseThread(object data)
        {
            var shutdownEvent = (ManualResetEvent) data;
            var waitHandles = new WaitHandle[2];
            waitHandles[0] = shutdownEvent;
            waitHandles[1] = _stackDbEvent;
            int index = WaitHandle.WaitAny(waitHandles);

            while (index == 1)
            {
                var stacksToAddNow = GetStacksToProcess();

                var moreEvents = stacksToAddNow.Count > 0;
                while (moreEvents)
                {
                    InsertStacks(stacksToAddNow);

                    moreEvents = false;

                    if (!shutdownEvent.WaitOne(0))
                    {
                        lock (_pendingStacksToAdd)
                        {
                            if (_pendingStacksToAdd.Count > 0)
                            {
                                stacksToAddNow = GetStacksToProcess();
                                moreEvents = stacksToAddNow.Count > 0;
                            }
                        }
                    }
                }

                index = WaitHandle.WaitAny(waitHandles);
            }
        }

        private void DatabaseThread(object data)
        {
            var shutdownEvent = (ManualResetEvent)data;
            int waitMsecs = 100;

            while (!shutdownEvent.WaitOne(waitMsecs))
            {
                DbOperation dbOp = null;
                bool moreToProcess;
                lock (_pendingOps)
                {
                    if (_pendingOps.Count > 0)
                    {
                        foreach (var op in _pendingOps)
                        {
                            if (!_lockedDbs.Contains(op.TraceId) || op.Type == DbOperation.DatabaseOperationType.Unlock)
                            {
                                dbOp = op;
                                _pendingOps.Remove(op);
                                break;
                            }
                        }
                    }
                    moreToProcess = _pendingOps.Count > 0;
                }

                if (dbOp != null)
                {
                    switch (dbOp.Type)
                    {
                        case DbOperation.DatabaseOperationType.GetEventCount:
                            var getEventCountOp = (GetEventCountDbOperation)dbOp;
                            getEventCountOp.Count = DatabaseGetEventCount(getEventCountOp.TraceId);
                            break;
                        case DbOperation.DatabaseOperationType.GetAllEvents:
                            var getAllEventsOp = (GetAllEventsDbOperation)dbOp;
                            getAllEventsOp.Events = DatabaseGetAllEvents(getAllEventsOp.TraceId);
                            break;
                        case DbOperation.DatabaseOperationType.GetFirstEvent:
                            var getFirstEventOp = (GetFirstEventDbOperation)dbOp;
                            getFirstEventOp.Event = DatabaseGetFirstEvent(getFirstEventOp.TraceId);
                            break;
                        case DbOperation.DatabaseOperationType.GetEvents:
                            var getEventsOp = (GetEventsDbOperation)dbOp;
                            getEventsOp.Events = DatabaseGetEvents(getEventsOp.EventIds, getEventsOp.TraceId, getEventsOp.FullStackInfo);
                            break;
                        case DbOperation.DatabaseOperationType.SearchEvents:
                            var searchEventsOp = (SearchEventsDbOperation)dbOp;
                            searchEventsOp.Events = DatabaseSearchEvents(searchEventsOp);
                            break;
                        case DbOperation.DatabaseOperationType.Lock:
                            _lockedDbs.Add(dbOp.TraceId);
                            break;
                        case DbOperation.DatabaseOperationType.Unlock:
                            _lockedDbs.Remove(dbOp.TraceId);
                            break;
                        case DbOperation.DatabaseOperationType.Clear:
                            var clearOp = (ClearDbOperation)dbOp;
                            DatabaseClearData(clearOp.TraceId);
                            break;
                        case DbOperation.DatabaseOperationType.UpdateEventProperties:
                            var updateEventOp = (UpdateEventPropertiesDbOperation)dbOp;
                            DatabaseUpdateEventProperties(updateEventOp);
                            break;
                        case DbOperation.DatabaseOperationType.Refresh:
                            var refreshOp = (RefreshEventsDbOperation)dbOp;
                            DatabaseRefreshEvents(refreshOp);
                            break;
                    }
                    dbOp.WaitEvent.Set();
                }
                else
                    moreToProcess = DatabaseAddEvents();
                
                waitMsecs = moreToProcess ? 0 : 100;
            }
        }

        //private byte[] CallEventToByteArray(CallEvent callEvent)
        //{
        //    var msCallEvent = new MemoryStream();
        //    Serializer.Serialize(msCallEvent, callEvent);
        //    msCallEvent.Close();
        //    return msCallEvent.ToArray();
        //}

        //private byte[] ParametersToByteArray(Param[] parameters)
        //{
        //    var msCallEvent = new MemoryStream();
        //    Serializer.Serialize(msCallEvent, parameters);
        //    msCallEvent.Close();
        //    return msCallEvent.ToArray();
        //}

        //static private byte[] CallStackToByteArray(DeviareTools.DeviareStackFrame[] stackFrames)
        //{
        //    var msCallStack = new MemoryStream();
        //    Serializer.Serialize(msCallStack, stackFrames);
        //    msCallStack.Close();
        //    return msCallStack.ToArray();
        //}

        //static private byte[] CallStackToByteArray(string[] stackFrames)
        //{
        //    var ms = new MemoryStream();
        //    Serializer.Serialize(ms, stackFrames);
        //    ms.Close();
        //    return ms.ToArray();
        //}

        static private byte[] AncestorsToByteArray(UInt64[] ancestors)
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, ancestors);
            ms.Close();
            return ms.ToArray();
        }

        static private UInt64[] ByteArrayToAncestors(object obj)
        {
            var bytes = (byte[]) obj;

            var ms = new MemoryStream(bytes, 0, bytes.Length);
            var ancestors = Serializer.Deserialize<UInt64[]>(ms);
            return ancestors;
        }

        private void CompleteEvent(CallEvent callEvent)
        {
            if (callEvent.IsClearSignalEvent)
                return;

            _eventPeerMatching.MatchEvent(callEvent);

            if (callEvent.Type == HookType.Custom && !callEvent.Before && (callEvent.Function == "Ole32.CoGetClassObject" || callEvent.Function == "RpcRT4.NdrDllGetClassObject"))
            {
                callEvent.CreateEventParams(callEvent.Params[0].Value);
            }
            // WORKAROUND: for LoadResource which module isn't found
            if (!callEvent.Before && callEvent.Type == HookType.Custom && callEvent.Function == "LoadResource" && callEvent.ParamCount > 0 && callEvent.ParamMain.StartsWith("0x"))
            {
                FileSystemEvent.SetModuleNotFound(callEvent, true);
            }

            if (callEvent.IsFileSystem)
            {
                var fileSystemPath = !callEvent.NullParams ? callEvent.Params[0].Value : string.Empty;
                var eventPath = FileSystemTools.GetNormalizedPath(fileSystemPath);
                string filepart = null;
                var invalidPath = false;

                var setInvalidPath = false;

                if (FileSystemEvent.ModuleNotFound(callEvent))
                    setInvalidPath = true;
                else
                {
                    try
                    {

                        if (eventPath.StartsWith("\"") && eventPath.EndsWith("\""))
                            filepart = Path.GetFileName(eventPath.Trim('\"'));
                        else
                            filepart = Path.GetFileName(eventPath);

                        if (String.IsNullOrEmpty(filepart))
                            filepart = eventPath;
                        invalidPath = !File.Exists(fileSystemPath);
                        if (invalidPath)
                        {
                            invalidPath = !Directory.Exists(fileSystemPath);
                            if (!invalidPath)
                                FileSystemEvent.SetDirectory(callEvent, true);
                        }
                    }
                    catch (ArgumentException)
                    {
                        setInvalidPath = true;
                    }
                }
                if (setInvalidPath)
                {
                    // this can happen if the application tries to use an invalid path. It may be a \\ path or something unhandled
                    filepart = FileSystemTools.GetFileName(eventPath);

                    invalidPath = true;
                }

                FileSystemEvent.SetFilepart(callEvent, filepart);
                FileSystemEvent.SetInvalidPath(callEvent, invalidPath);
            }

            // WORKAROUND: FileVersionInfo.GetVersionInfo doesn't work called from 
            // different threads like in handlers code. We call it here from a single
            // thread.
            if (callEvent.IsFileSystem)
            {
                FileSystemEvent.SetInvalidPath(callEvent, true);

                if (!callEvent.NullParams)
                {
                    var fileSystemPath = callEvent.Params[0].Value;

                    if (!FileSystemEvent.IsDirectory(callEvent))
                    {
                        if (File.Exists(fileSystemPath))
                        {
                            FileSystemEvent.SetFileInfo(callEvent, fileSystemPath);

                            try
                            {
                                var attr = File.GetAttributes(fileSystemPath);

                                if ((attr & FileAttributes.Directory) ==
                                    FileAttributes.Directory)
                                {
                                    FileSystemEvent.SetDirectory(callEvent, true);
                                }
                                FileSystemEvent.SetInvalidPath(callEvent, false);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    var invalidPath = FileSystemEvent.IsInvalidPath(callEvent);
                    //if (!callEvent.Before && FileSystemEvent.ReferencesFile(callEvent) && !invalidPath)
                    if (!callEvent.Before && FileSystemEvent.ReferencesFile(callEvent))
                    {
                        //if (FileSystemEvent.IsInvalidPath(callEvent) && fileSystemPath.ToLower().EndsWith(".exe") && Debugger.IsAttached)
                        //    Debugger.Break();
                        callEvent.SetIcon(FileSystemTools.GetIcon(fileSystemPath, invalidPath));
                    }
                }
            }

            _priorityAnalyzer.AnalyzeEvent(callEvent);
        }

        private void InsertParameters(CallEvent callEvent, UInt64 callNumber, SQLiteCommand cmdParams,
                            SQLiteCommand cmdParamsDelete, bool deletePrevious)
        {
            if (callEvent.Params == null || callEvent.Params.Length == 0)
                return;
            if (deletePrevious)
            {
                cmdParamsDelete.Parameters.Add(new SQLiteParameter("@CallNumber", callNumber));
                cmdParamsDelete.ExecuteNonQuery();
            }

            for (int i = 0; i < callEvent.Params.Length; i++)
            {
                cmdParams.Parameters[0].Value = callNumber;
                cmdParams.Parameters[1].Value = i;
                cmdParams.Parameters[2].Value = callEvent.Params[i].Name;
                cmdParams.Parameters[3].Value = callEvent.Params[i].Value;

                cmdParams.ExecuteNonQuery();
            }
        }

        readonly SQLiteParameter _cmdParamCallNumber = new SQLiteParameter("@CallNumber");
        readonly SQLiteParameter _cmdParamPeer = new SQLiteParameter("@Peer");
        readonly SQLiteParameter _cmdParamPriority = new SQLiteParameter("@Priority");
        readonly SQLiteParameter _cmdParamProcessName = new SQLiteParameter("@ProcessName");
        readonly SQLiteParameter _cmdParamPid = new SQLiteParameter("@Pid");
        readonly SQLiteParameter _cmdParamTid = new SQLiteParameter("@Tid");
        readonly SQLiteParameter _cmdParamCallModule = new SQLiteParameter("@CallModule");
        readonly SQLiteParameter _cmdParamStackTraceString = new SQLiteParameter("@StackTraceString");
        readonly SQLiteParameter _cmdParamCallFunction = new SQLiteParameter("@Function");
        readonly SQLiteParameter _cmdParamWin32Function = new SQLiteParameter("@Win32Function");
        
        readonly SQLiteParameter _cmdParamEventFlags = new SQLiteParameter("@EventFlags");
        readonly SQLiteParameter _cmdParamAncestors = new SQLiteParameter("@Ancestors");
        readonly SQLiteParameter _cmdParamRetValue = new SQLiteParameter("@RetValue");
        readonly SQLiteParameter _cmdParamHookType = new SQLiteParameter("@HookType");
        readonly SQLiteParameter _cmdParamChainDepth = new SQLiteParameter("@ChainDepth");
        readonly SQLiteParameter _cmdParamCookie = new SQLiteParameter("@Cookie");
        readonly SQLiteParameter _cmdParamTime = new SQLiteParameter("@Time");
        readonly SQLiteParameter _cmdParamGenerationTime = new SQLiteParameter("@GenerationTime");
        readonly SQLiteParameter _cmdParamParamMainIndex = new SQLiteParameter("@ParamMainIndex");
        readonly SQLiteParameter _cmdParamResult = new SQLiteParameter("@Result");
        readonly SQLiteParameter _cmdParamSuccess = new SQLiteParameter("@Success");
        readonly SQLiteParameter _cmdParamPropertiesBool = new SQLiteParameter("@PropertiesBool");
        readonly SQLiteParameter _cmdParamPropertiesStringMask = new SQLiteParameter("@PropertiesStringMask");
        readonly SQLiteParameter _cmdParamPropertiesByteArrayMask = new SQLiteParameter("@PropertiesByteArrayMask");
        
        SQLiteParameter[] _cmdParamPropertiesString;
        SQLiteParameter[] _cmdParamPropertiesUInt;
        SQLiteParameter[] _cmdParamPropertiesByteArray;

        readonly SQLiteParameter _cmdParamStackCallNumber = new SQLiteParameter("@CallNumber");
        readonly SQLiteParameter _cmdParamStackFullStackInfo = new SQLiteParameter("@FullStackInfo");

        private bool DatabaseAddEvents()
        {
            bool moreEvents = false;
            var events = new Dictionary<uint, CallEvent[]>();
            var toReport = new Dictionary<uint, List<CallEvent>>();
#if DEBUG
            // ReSharper disable TooWideLocalVariableScope
            double totalPrepair = 0,
                   totalConn = 0,
                   totalUpdate = 0,
                   totalAdd = 0,
                   totalAddParams = 0,
                   totalAddAncestors = 0,
                   totalAddParamsToTable = 0,
                   totalCommit = 0;
            // ReSharper restore TooWideLocalVariableScope
            var sw = new Stopwatch();
            sw.Start();
#endif

            // collect all the events that we will add to the database in this run
            lock (_pendingEventsToAdd)
            {
                if (_pendingEventsToAdd.Count > 0)
                {
                    //int pendingAddEvents = 0;
                    var traceIds = _pendingEventsToAdd.Keys.ToList();
                    foreach (var traceId in traceIds)
                    {
                        if (!_lockedDbs.Contains(traceId))
                        {
                            var evList = _pendingEventsToAdd[traceId];
                            if (evList.Count > 0)
                            {
                                //pendingAddEvents += evList.Count;
                                if (_pendingEventsToAdd.ContainsKey(traceId) &&
                                    _pendingEventsToAdd[traceId].Count > MaxEventCountProcessPerAction)
                                {
                                    var eventsToProcess = evList.GetRange(0, MaxEventCountProcessPerAction);
                                    events[traceId] = new CallEvent[MaxEventCountProcessPerAction];
                                    eventsToProcess.CopyTo(events[traceId]);
                                    _pendingEventsToAdd[traceId] = evList.GetRange(MaxEventCountProcessPerAction,
                                                                                   evList.Count -
                                                                                   MaxEventCountProcessPerAction);
                                    moreEvents = true;
                                }
                                else
                                {
                                    events[traceId] = new CallEvent[evList.Count];
                                    evList.CopyTo(events[traceId]);
                                    evList.Clear();
                                }
                            }
                        }
                    }
#if DEBUG
                    totalPrepair = sw.Elapsed.TotalMilliseconds;
#endif
                }
            }

            if (events.Count > 0)
            {
#if DEBUG
                // ReSharper disable TooWideLocalVariableScope
                double prevTime;
                // ReSharper restore TooWideLocalVariableScope
                int count = 0;
#endif

                foreach (var evListTrace in events)
                {
#if DEBUG
                    prevTime = sw.Elapsed.TotalMilliseconds;
#endif

                    using (var con = new SQLiteConnection(GetDatabaseConnectionString(evListTrace.Key)))
                    {
                        con.Open();

                        using (var cmd = con.CreateCommand())
                        using (var cmdParams = con.CreateCommand())
                        using (var cmdParamsDelete = con.CreateCommand())
                            //using (var cmdStack = con.CreateCommand())
                        using (var cmdUpdate = con.CreateCommand())
                        {
                            cmd.Parameters.Add(_cmdParamCallNumber);
                            cmd.Parameters.Add(_cmdParamPeer);
                            cmd.Parameters.Add(_cmdParamPriority);
                            cmd.Parameters.Add(_cmdParamProcessName);
                            cmd.Parameters.Add(_cmdParamPid);
                            cmd.Parameters.Add(_cmdParamTid);
                            cmd.Parameters.Add(_cmdParamCallModule);
                            cmd.Parameters.Add(_cmdParamStackTraceString);
                            cmd.Parameters.Add(_cmdParamCallFunction);
                            cmd.Parameters.Add(_cmdParamWin32Function);

                            cmd.Parameters.Add(_cmdParamEventFlags);
                            cmd.Parameters.Add(_cmdParamAncestors);
                            cmd.Parameters.Add(_cmdParamRetValue);
                            cmd.Parameters.Add(_cmdParamHookType);
                            cmd.Parameters.Add(_cmdParamChainDepth);
                            cmd.Parameters.Add(_cmdParamCookie);
                            cmd.Parameters.Add(_cmdParamTime);
                            cmd.Parameters.Add(_cmdParamGenerationTime);
                            cmd.Parameters.Add(_cmdParamParamMainIndex);

                            cmd.Parameters.Add(_cmdParamResult);
                            cmd.Parameters.Add(_cmdParamSuccess);

                            cmd.Parameters.Add(_cmdParamPropertiesBool);
                            cmd.Parameters.Add(_cmdParamPropertiesStringMask);
                            cmd.Parameters.Add(_cmdParamPropertiesByteArrayMask);

                            for (int i = 0; i < CallEvent.MaxPropertyCountString; i++)
                            {
                                cmd.Parameters.Add(_cmdParamPropertiesString[i]);
                            }

                            for (int i = 0; i < CallEvent.MaxPropertyCountUInt; i++)
                            {
                                cmd.Parameters.Add(_cmdParamPropertiesUInt[i]);
                            }
                            for (int i = 0; i < CallEvent.MaxPropertyCountByteArray; i++)
                            {
                                cmd.Parameters.Add(_cmdParamPropertiesByteArray[i]);
                            }

                            cmdParams.Parameters.Add(new SQLiteParameter("@CallNumber"));
                            cmdParams.Parameters.Add(new SQLiteParameter("@Number"));
                            cmdParams.Parameters.Add(new SQLiteParameter("@Name"));
                            cmdParams.Parameters.Add(new SQLiteParameter("@Value"));

                            cmd.CommandText = _insertEventString;

                            cmdUpdate.CommandText =
                                @"UPDATE Events SET Peer = @Peer, " +
                                "Result = @Result, " +
                                "Ancestors = @Ancestors, " +
                                //"Priority = @Priority, " +
                                "Success = @Success " +
                                "WHERE CallNumber = @CallNumber";

                            cmdParams.CommandText =
                                @"INSERT INTO Parameters (
                                        CallNumber,
                                        Number,
                                        Name,
                                        Value
                                    ) VALUES (
                                        @CallNumber,
                                        @Number,
                                        @Name,
                                        @Value
                                    )";

                            cmdParamsDelete.CommandText =
                                "DELETE FROM Parameters WHERE CallNumber=@CallNumber; ";

#if DEBUG
                            totalConn += sw.Elapsed.TotalMilliseconds - prevTime;
#endif

                            using (var tr = con.BeginTransaction())
                            {
                                foreach (var callEvent in evListTrace.Value)
                                {
                                    try
                                    {
#if DEBUG
                                        prevTime = sw.Elapsed.TotalMilliseconds;
#endif

                                        lock (_pendingEventsToAddById)
                                        {
                                            _pendingEventsToAddById[callEvent.TraceId].Remove(callEvent.CallNumber);
                                        }

                                        _cmdParamCallNumber.Value = callEvent.CallNumber;
                                        _cmdParamPeer.Value = callEvent.Peer;
                                        _cmdParamPriority.Value = callEvent.Priority;
                                        _cmdParamProcessName.Value = callEvent.ProcessName;
                                        _cmdParamPid.Value = callEvent.Pid;
                                        _cmdParamTid.Value = callEvent.Tid;
                                        _cmdParamCallModule.Value = callEvent.CallModule;
                                        _cmdParamStackTraceString.Value = callEvent.StackTraceString;
                                        _cmdParamCallFunction.Value = callEvent.Function;
                                        _cmdParamWin32Function.Value = callEvent.Win32Function;

                                        _cmdParamEventFlags.Value = callEvent.EventFlags;
#if DEBUG
                                        var prevAncTime = sw.Elapsed.TotalMilliseconds;
#endif
                                        var ancestors = callEvent.Ancestors.Count > 0 ? AncestorsToByteArray(callEvent.Ancestors.ToArray()) : null;
                                        _cmdParamAncestors.Value = ancestors;
#if DEBUG
                                        totalAddAncestors += sw.Elapsed.TotalMilliseconds - prevAncTime;
#endif
                                        _cmdParamRetValue.Value = callEvent.RetValue;
                                        _cmdParamHookType.Value = (int) callEvent.Type;
                                        _cmdParamChainDepth.Value = callEvent.ChainDepth;
                                        _cmdParamCookie.Value = callEvent.Cookie;
                                        _cmdParamTime.Value = callEvent.Time;
                                        _cmdParamGenerationTime.Value = callEvent.GenerationTime;
                                        _cmdParamParamMainIndex.Value = callEvent.ParamMainIndex;

                                        _cmdParamResult.Value = callEvent.Result;
                                        _cmdParamSuccess.Value = callEvent.Success;

                                        _cmdParamPropertiesBool.Value = callEvent.PackedBoolProperties;

                                        int stringMask = 0;
                                        for (var i = 0; i < CallEvent.MaxPropertyCountString; i++)
                                        {
                                            string stringProp = callEvent.GetPropertiesString(i);
                                            if (string.IsNullOrEmpty(stringProp))
                                                _cmdParamPropertiesString[i].Value = DBNull.Value;
                                            else
                                            {
                                                _cmdParamPropertiesString[i].Value = stringProp;
                                                stringMask |= 1 << i;
                                            }
                                        }
                                        _cmdParamPropertiesStringMask.Value = stringMask;

                                        for (var i = 0; i < CallEvent.MaxPropertyCountUInt; i++)
                                        {
                                            _cmdParamPropertiesUInt[i].Value = callEvent.GetPropertiesUInt64(i);
                                        }
                                        int byteArrayMask = 0;
                                        for (var i = 0; i < CallEvent.MaxPropertyCountByteArray; i++)
                                        {
                                            byte[] byteArrayProp = callEvent.GetPropertiesByteArray(i);
                                            if (byteArrayProp == null)
                                                _cmdParamPropertiesByteArray[i].Value = DBNull.Value;
                                            else
                                            {
                                                _cmdParamPropertiesByteArray[i].Value = byteArrayProp;
                                                byteArrayMask |= 1 << i;
                                            }
                                            _cmdParamPropertiesByteArray[i].Value = callEvent.GetPropertiesByteArray(i);
                                        }
                                        _cmdParamPropertiesByteArrayMask.Value = byteArrayMask;

#if DEBUG
                                        totalAddParams += sw.Elapsed.TotalMilliseconds - prevTime;
                                        prevTime = sw.Elapsed.TotalMilliseconds;
#endif
                                        try
                                        {
                                            cmd.ExecuteNonQuery();
                                        }
                                        catch (Exception ex)
                                        {
                                            Error.WriteLine("Database exception: " + ex.Message);
                                        }
#if DEBUG
                                        totalAdd += sw.Elapsed.TotalMilliseconds - prevTime;
                                        prevTime = sw.Elapsed.TotalMilliseconds;
#endif
                                        if (callEvent.Peer != 0)
                                        {
                                            // update before event with the after parameters if found
                                            cmdUpdate.Parameters.Add(new SQLiteParameter("@CallNumber", callEvent.Peer));
                                            cmdUpdate.Parameters.Add(new SQLiteParameter("@Peer", callEvent.CallNumber));
                                            cmdUpdate.Parameters.Add(new SQLiteParameter("@Result", callEvent.Result));
                                            cmdUpdate.Parameters.Add(new SQLiteParameter("@Ancestors", ancestors));
                                            cmdUpdate.Parameters.Add(new SQLiteParameter("@Success",
                                                                                         callEvent.Success));
                                            //cmdUpdate.Parameters.Add(new SQLiteParameter("@Priority",
                                            //                                             callEvent.Priority));

                                            try
                                            {
                                                cmdUpdate.ExecuteNonQuery();
                                            }
                                            catch (Exception ex)
                                            {
                                                Error.WriteLine("Database exception: " + ex.Message);
                                            }

                                            InsertParameters(callEvent, callEvent.Peer, cmdParams, cmdParamsDelete, true);
#if DEBUG
                                            totalUpdate += sw.Elapsed.TotalMilliseconds - prevTime;
#endif
                                        }
#if DEBUG
                                        prevTime = sw.Elapsed.TotalMilliseconds;
#endif
                                        InsertParameters(callEvent, callEvent.CallNumber, cmdParams, cmdParamsDelete,
                                                         false);

#if DEBUG
                                        totalAddParamsToTable += sw.Elapsed.TotalMilliseconds - prevTime;
#endif

                                        // report only with Tag true
                                        if (callEvent.Tag != null && ((bool) callEvent.Tag))
                                        {
                                            List<CallEvent> evList;
                                            if (!toReport.TryGetValue(callEvent.TraceId,
                                                                      out evList))
                                            {
                                                toReport[callEvent.TraceId] = evList = new List<CallEvent>();
                                            }
                                            evList.Add(callEvent);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Error.WriteLine("Database exception: " + ex.Message);
                                    }
                                }
#if DEBUG
                                prevTime = sw.Elapsed.TotalMilliseconds;
#endif
                                tr.Commit();
#if DEBUG
                                totalCommit += sw.Elapsed.TotalMilliseconds - prevTime;
#endif
                            }
                        }
                        con.Close();

                        lock (_pendingNewEventsToReport)
                        {
                            foreach (var eventList in toReport)
                            {
#if DEBUG
                                count += eventList.Value.Count;
#endif

                                List<CallEvent> evList;
                                if (!_pendingNewEventsToReport.TryGetValue(eventList.Key, out evList))
                                {
                                    _pendingNewEventsToReport[eventList.Key] = evList = new List<CallEvent>();
                                }
                                evList.AddRange(eventList.Value);

                                var pecm = PendingEventsCountManager.GetInstance();
                                var n = eventList.Value.Count;
                                pecm.EventsLeave(n, PendingEventsCountManager.DatabasePhase);
                                pecm.EventsEnter(n, PendingEventsCountManager.GuiPhase);
                            }
                        }
                    }
                }
#if DEBUG
                double total = sw.Elapsed.TotalMilliseconds;
                Debug.WriteLine("\nDatabase Insert\nEvents added:\t" + count +
                                "\nTotal time:\t" + total +
                                "\nPrepare:\t" + totalPrepair +
                                "\nUpdate:\t" + totalUpdate +
                                "\nAdd Ancestors:\t" + totalAddAncestors +
                                "\nAdd Parameters to query:\t" + totalAddParams +
                                "\nAdd Event Parameters to Table:\t" + totalAddParamsToTable +
                                "\nAdd:\t" + totalAdd +
                                "\nCommit:\t" + totalCommit +
                                "\nConnection:\t" + totalConn +
                                "\nTotal per event:\t" + total/count);
#endif
            }

            // if timer is null, there is no UI thread so report events here
            if (_timer == null)
            {
                ReportEvents(this, null);
            }
            return moreEvents;
        }

#if DEBUG
        private double _totalGetParameters, _totalGetEvent, _totalSerializeEvent, _totalPropertiesEvent, _totalRead, _totalSerializeAncestors;
#endif

        private void DatabaseRefreshEvents(RefreshEventsDbOperation refreshData)
        {
#if DEBUG
            // ReSharper disable TooWideLocalVariableScope
            double totalReport, totalMilliseconds, totalExecReader;
            _totalGetParameters = _totalSerializeAncestors = _totalSerializeEvent = _totalPropertiesEvent = _totalRead = _totalGetEvent = 0;
            // ReSharper restore TooWideLocalVariableScope
#endif

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(refreshData.TraceId)))
            {
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    try
                    {
#if DEBUG
                        CallEvent.TimeProperties = CallEvent.TimeRest = 0;
                        var sw = new Stopwatch();
                        sw.Start();
#endif

                        // event count query
                        cmd.CommandText = "SELECT COUNT(CallNumber) FROM Events";
                        cmd.CommandType = CommandType.Text;

                        var whereString = new StringBuilder();
                        bool firstFilter = true;

                        if (refreshData.ReportData.EventsToReport != EventType.All)
                        {
                            whereString.Append(" WHERE");
                            whereString.Append(" (EventFlags & @EventFlags) != 0");
                            cmd.Parameters.Add(new SQLiteParameter("@EventFlags", refreshData.ReportData.EventsToReport));
                            firstFilter = false;
                        }

                        if (!refreshData.ReportData.ReportBeforeEvents)
                        {
                            whereString.Append(firstFilter ? " WHERE" : " AND");
                            whereString.Append(" (EventFlags & @FlagBefore) = 0");
                            cmd.Parameters.Add(new SQLiteParameter("@FlagBefore", EventFlags.Before));
                            firstFilter = false;
                        }

                        if (refreshData.ReportData.EventResultsIncluded != EventsReportData.EventResult.All)
                        {
                            whereString.Append(firstFilter ? " WHERE" : " AND");
                            whereString.Append(" (Success "
                                               + (refreshData.ReportData.EventResultsIncluded ==
                                                  EventsReportData.EventResult.Success
                                                      ? "!= 0)"
                                                      : " = 0)"));

                            //firstFilter = false;
                        }

                        cmd.CommandText += whereString.ToString();

                        refreshData.TotalEvents = Convert.ToInt32(cmd.ExecuteScalar());

#if DEBUG
                        totalMilliseconds = sw.Elapsed.TotalMilliseconds;
#endif

                        const string orderBy = "ORDER BY Events.CallNumber ";
                        cmd.CommandText = _selectEventString +
                                          "FROM Events " +
                                          "LEFT JOIN Parameters ON Events.CallNumber=Parameters.CallNumber ";
                        cmd.CommandText += whereString + " " + orderBy;

#if DEBUG
                        var lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif
                        var r = cmd.ExecuteReader();
#if DEBUG
                        totalExecReader = sw.Elapsed.TotalMilliseconds - lastElapsed;
                        _totalRead = totalReport = 0;
#endif

                        var evToReport = new List<CallEvent>();

#if DEBUG
                        lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif

                        UInt64 nextCallNumber = r.Read() ? (UInt64)(Int64)r["CallNumber"] : 0;
                        while (nextCallNumber != 0)
                        {
#if DEBUG
                            _totalRead += (sw.Elapsed.TotalMilliseconds - lastElapsed);
#endif
                            var callEvent = DatabaseGetEventFromReader(ref r, nextCallNumber,
                                                                       refreshData.StackTraceString,
                                                                       refreshData.Win32Function, refreshData.TraceId,
                                                                       out nextCallNumber);

                            lock (refreshData)
                            {
                                if (refreshData.Canceled)
                                    break;
                                if (refreshData.PendingEvents == null)
                                {
                                    refreshData.PendingEvents = new List<CallEvent>();
                                }
                                if (refreshData.ReportData != null)
                                {
                                    evToReport.Add(callEvent);
                                    if (evToReport.Count > MaxEventCountProcessPerAction)
                                    {
#if DEBUG
                                        lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif
                                        OnEventsInvokerReport(refreshData, evToReport);
                                        evToReport = new List<CallEvent>();
#if DEBUG
                                        totalReport += (sw.Elapsed.TotalMilliseconds - lastElapsed);
#endif
                                    }
                                }
                                else
                                {
                                    refreshData.PendingEvents.Add(callEvent);
                                }
                            }
#if DEBUG
                            lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif
                        }

                        if (refreshData.ReportData != null && (evToReport.Count > 0 || refreshData.TotalEvents == 0))
                        {
#if DEBUG
                            lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif
                            OnEventsInvokerReport(refreshData, evToReport);
#if DEBUG
                            totalReport += (sw.Elapsed.TotalMilliseconds - lastElapsed);
#endif
                        }
#if DEBUG
                        var total = sw.Elapsed.TotalMilliseconds;
                        Debug.WriteLine("Total Count:\t" + totalMilliseconds +
                            "\nTotal Exec Reader:\t" + totalExecReader +
                            "\nTotal Get Serialize:\t" + _totalSerializeEvent +
                            "\nTotal Properties Serialize:\t" + _totalPropertiesEvent +
                            "\nTotal Get Event:\t" + _totalGetEvent +
                            "\nTotal Ancestors:\t" + _totalSerializeAncestors +
                            "\nTotal CallEvent Props:\t" + CallEvent.TimeProperties+
                            "\nTotal CallEvent Rest:\t" + CallEvent.TimeRest +
                            "\nTotal Db Parameters:\t" + _totalGetParameters +
                            "\nTotal Read:\t" + _totalRead +
                                        "\nTotal Report:\t" + totalReport +
                                        "\nTotal Elapsed:\t" + total);
#endif
                        refreshData.DatabaseProcessingFinished = true;
                        r.Close();
                    }
                    catch (Exception ex)
                    {
                        Error.WriteLine("Database Exception: " + ex.Message);
                        refreshData.TotalEvents = 0;
                        refreshData.DatabaseProcessingFinished = true;
                    }
                }

                con.Close();
            }
        }

        private CallEvent[] DatabaseSearchEvents(SearchEventsDbOperation searchdata)
        {
            CallEvent[] ret = null;

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(searchdata.TraceId)))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    try
                    {
                        string condition = string.Empty;
                        if (searchdata.StartCallNumber != 0)
                        {
                            if (searchdata.SearchForward)
                            {
                                condition = "Events.CallNumber > " + searchdata.StartCallNumber;
                            }
                            else
                            {
                                condition = "Events.CallNumber < " + searchdata.StartCallNumber;
                            }
                        }
                        string whereString;
                        // sensitive search done using REGEXP
                        if (searchdata.MatchCase)
                        {
                            whereString =
                                "(ProcessName REGEXP @SearchString OR " +
                                "CallModule REGEXP @SearchString OR " +
                                "Function REGEXP @SearchString OR " +
                                "Result REGEXP @SearchString OR " +
                                "Name REGEXP @SearchString OR " +
                                "Value REGEXP @SearchString)";
                        }
                        // insensitive REGEXP
                        else if (searchdata.MatchWholeWord)
                        {
                            whereString =
                                "(REGEXPNOCASE(ProcessName, @SearchString) OR " +
                                "REGEXPNOCASE (CallModule, @SearchString) OR " +
                                "REGEXPNOCASE(Function, @SearchString) OR " +
                                "REGEXPNOCASE(Result, @SearchString) OR " +
                                "REGEXPNOCASE(Name, @SearchString) OR " +
                                "REGEXPNOCASE (Value, @SearchString))";
                        }
                        else
                        {
                            whereString =
                                "(ProcessName LIKE @SearchString OR " +
                                "CallModule LIKE @SearchString OR " +
                                "Function LIKE @SearchString OR " +
                                "Result LIKE @SearchString OR " +
                                "Name LIKE @SearchString  OR " +
                                "Value LIKE @SearchString)";
                        }
                        cmd.CommandText = _selectEventString + " FROM Events " +
                                          " LEFT JOIN Parameters ON Events.CallNumber=Parameters.CallNumber WHERE " +
                                          (condition != string.Empty ? (condition + " AND ") : string.Empty) +
                                          whereString;

                        if (searchdata.SearchForward)
                        {
                            cmd.CommandText += " ORDER BY Events.CallNumber";
                        }
                        else
                        {
                            cmd.CommandText += " ORDER BY Events.CallNumber desc";
                        }

                        if (searchdata.MatchWholeWord)
                        {
                            cmd.Parameters.Add(new SQLiteParameter("@SearchString",
                                                                   @"\b" + searchdata.SearchText + @"\b"));
                        }
                        else if (searchdata.MatchCase)
                        {
                            cmd.Parameters.Add(new SQLiteParameter("@SearchString",
                                                                   searchdata.SearchText));
                        }
                        else
                        {
                            cmd.Parameters.Add(new SQLiteParameter("@SearchString",
                                       "%" + searchdata.SearchText + "%"));
                        }

                        var evList = new List<CallEvent>();
                        cmd.CommandType = CommandType.Text;
                        var r = cmd.ExecuteReader();
                        while (evList.Count < searchdata.MaxEventCountToRetrieve && r.Read())
                        {
                            bool addEvent = true;
                            if (searchdata.CallNumberFilter != null)
                            {
                                var callNumber = (UInt64)(Int64)r["CallNumber"];
                                if (!searchdata.CallNumberFilter.Contains(callNumber))
                                //|| 
                                //(searchdata.MatchCase &&))
                                {
                                    addEvent = false;
                                }
                            }
                            if (addEvent)
                            {
                                var isBefore = ((Int64) r["EventFlags"] & (Int64) EventFlags.Before) != 0;
                                var peerCallNumber = (UInt64)(Int64)r["Peer"];
                                // select only before calls to avoid double hit of the same event. If the Peer event isn't 
                                // in the CallNumberFilter use this event since the before call won't be reported
                                if (isBefore || (searchdata.CallNumberFilter != null && !searchdata.CallNumberFilter.Contains(peerCallNumber)))
                                {
                                    var ev = DatabaseGetEventFromReader(ref r, searchdata.TraceId);
                                    evList.Add(ev);
                                }
                            }
                        }
                        ret = evList.ToArray();
                        r.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database Exception: " + ex.Message);
                    }
                }

                con.Close();
            }
            return ret;
        }

        private CallEvent DatabaseGetEventFromReader(ref SQLiteDataReader r, UInt64 callNumber, bool stackTraceString, 
                                                     bool win32Function, uint traceId, out UInt64 nextCallNumber)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            var parameters = new List<Param>();
            var callEvent = DatabaseGetEventFromReader(ref r, callNumber, stackTraceString, win32Function, traceId);
            while (true)
            {
#if DEBUG
                var lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif

                if (r["Name"] != DBNull.Value)
                {
                    var name = (string)r["Name"];
                    var value = (string)r["Value"];
                    parameters.Add(new Param(name, value));
                }
#if DEBUG
                _totalGetParameters += (sw.Elapsed.TotalMilliseconds - lastElapsed);
                lastElapsed = sw.Elapsed.TotalMilliseconds;
#endif
                nextCallNumber = (UInt64)(r.Read() ? (Int64)r["CallNumber"] : 0);
#if DEBUG
                _totalRead += (sw.Elapsed.TotalMilliseconds - lastElapsed);
#endif

                if (nextCallNumber != callNumber)
                {
                    callEvent.CreateParams(parameters.ToArray());
                    return callEvent;
                }
            }
        }
        private CallEvent DatabaseGetEventFromReader(ref SQLiteDataReader r, uint traceId)
        {
            return DatabaseGetEventFromReader(ref r, 0, false, false, traceId);
        }
        private CallEvent DatabaseGetEventFromReader(ref SQLiteDataReader r, UInt64 callNumber, bool stackTraceString,
                                                     bool win32Function, uint traceId)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            var obj = r["Ancestors"];
            List<ulong> ancestors = obj != DBNull.Value ? ByteArrayToAncestors(obj).ToList() : new List<ulong>();

#if DEBUG
            _totalSerializeAncestors += sw.Elapsed.TotalMilliseconds;
            var previous = sw.Elapsed.TotalMilliseconds;
#endif

            var processNameObj = r["ProcessName"];
            var functionObj = r["Function"];
            object win32FunctionObj = win32Function ? r["Win32Function"] : DBNull.Value;
            var callModuleObj = r["CallModule"];
            object stackTraceStringObj = stackTraceString ? r["StackTraceString"] : DBNull.Value;
            var resultOBj = r["Result"];

            var callEvent = new CallEvent(traceId,
                                          callNumber == 0 ? (UInt64) (Int64) r["CallNumber"] : callNumber,
                                          (UInt64) (Int64) r["Peer"],
                                          (int) (Int64) r["Priority"],
                                          (string) (processNameObj == DBNull.Value ? string.Empty : processNameObj),
                                          (EventFlags) (Int64) r["EventFlags"],
                                          (UInt64) (Int64) r["RetValue"],
                                          (string) (functionObj == DBNull.Value ? string.Empty : functionObj),
                                          (string) (win32FunctionObj == DBNull.Value ? string.Empty : win32FunctionObj),
                                          (HookType) (Int64) r["HookType"],
                                          (uint) (Int64) r["ChainDepth"],
                                          (uint) (Int64) r["Cookie"],
                                          (double) r["Time"],
                                          (double) r["GenerationTime"],
                                          (int) (Int64) r["ParamMainIndex"],
                                          (uint) (Int64) r["Pid"],
                                          (uint) (Int64) r["Tid"],
                                          (string) (callModuleObj == DBNull.Value ? string.Empty : callModuleObj),
                                          (string)
                                          (stackTraceStringObj == DBNull.Value ? string.Empty : stackTraceStringObj),
                                          (string) (resultOBj == DBNull.Value ? string.Empty : resultOBj),
                                          (bool) r["Success"],
                                          ancestors);
#if DEBUG
            _totalGetEvent += sw.Elapsed.TotalMilliseconds -  previous;
#endif

#if DEBUG
            _totalSerializeEvent += sw.Elapsed.TotalMilliseconds;
            previous = sw.Elapsed.TotalMilliseconds;
#endif
            var stringMask = (Int64) r["PropertiesStringMask"];
            for (int i = 0; i < CallEvent.MaxPropertyCountString; i++)
            {
                var stringValue = string.Empty;
                if ((1 << i & stringMask) != 0)
                {
                    var valueObj = r["PropertyString" + i];
                    if (valueObj != DBNull.Value)
                        stringValue = (string) valueObj;
                }
                callEvent.PropertiesString[i] = stringValue;
            }

            for (int i = 0; i < CallEvent.MaxPropertyCountUInt; i++)
            {
                callEvent.PropertiesUInt64[i] = (UInt64)(Int64)r["PropertyUInt" + i];
            }
            var byteArrayMask = (Int64)r["PropertiesByteArrayMask"];
            for (int i = 0; i < CallEvent.MaxPropertyCountByteArray; i++)
            {
                byte[] byteArray = null;
                if ((1 << i & byteArrayMask) != 0)
                {
                    var valueObj = r["PropertyByteArray" + i];
                    if (valueObj != DBNull.Value)
                        byteArray = (byte[])valueObj;
                }
                callEvent.PropertiesByteArray[i] = byteArray;
            }
            callEvent.PackedBoolProperties = (int)(Int64)r["PropertiesBool"];
#if DEBUG
            _totalPropertiesEvent += sw.Elapsed.TotalMilliseconds - previous;
#endif

            return callEvent;
        }

        private List<DeviareTools.DeviareStackFrame> DatabaseGetEventCallStackFromReader(ref SQLiteDataReader r)
        {
            var obj = r["FullStackInfo"];
            var bytes = (byte[]) obj;

            var callStackStringString = Encoding.Default.GetString(bytes);

            var callStackString = callStackStringString.Split('|');
            var count = callStackString.Length / 5;
            DeviareTools.DeviareStackFrame[] callStack;

            if (count != 0)
            {
                callStack = new DeviareTools.DeviareStackFrame[count];

                for (int i = 0; i < count; i++)
                {
                    var strBaseAddress = callStackString[i*5 + 1];
                    var strFrameAddress = callStackString[i*5 + 2];
                    var strOffset = callStackString[i*5 + 4];
                    var modPath = callStackString[i*5];
                    var modName = (modPath == null ? String.Empty : FileSystemTools.GetFileName(modPath)).ToLower();
                    var modBaseAdress = StringTools.ConvertToUInt64(strBaseAddress);
                    UInt64 frameAddress = StringTools.ConvertToUInt64(strFrameAddress);
                    UInt64 offset = StringTools.ConvertToUInt64(strOffset);
                    
                    var nearestSymbol = callStackString[i * 5 + 3];
                    var frame = new DeviareTools.DeviareStackFrame(modPath, modName, nearestSymbol, frameAddress, offset,
                                                                   modBaseAdress);
                    callStack[i] = frame;
                }
            }
            else
                return null;

            return callStack.ToList();
        }

        /// <summary>
        /// </summary>
        /// <param name="refreshData"> </param>
        /// <param name="evToReport"></param>
        private void OnEventsInvokerReport(RefreshEventsDbOperation refreshData, List<CallEvent> evToReport)
        {
            var notifier = refreshData.ReportData.Notifier ?? refreshData.ReportData.OnEventsReady;
            if (refreshData.ReportData.ControlInvoker != null)
            {
                var result = refreshData.ReportData.ControlInvoker.Invoke(notifier, evToReport, refreshData.TotalEvents);
                if(!(bool) result)
                    refreshData.Canceled = true;
            }
            else
            {
                if (!notifier.Invoke(evToReport, refreshData.TotalEvents))
                {
                    refreshData.Canceled = true;
                }
            }
        }

        public void DatabaseClearData(uint traceId)
        {
            // stack database runs on a separate thread so lock it
            lock(_stackDbLock)
            {
                if (ExistDatabase(traceId))
                {
                    DestroyDatabase(traceId);
                }
                CreateDatabase(traceId);
                _clearId++;
            }
        }

        private Dictionary<ulong, List<DeviareTools.DeviareStackFrame>> DatabaseGetStacks(uint traceId, string inText)
        {
            var stacksToRetrieve = new Dictionary<ulong, List<DeviareTools.DeviareStackFrame>>();
            lock(_stackDbLock)
            {
                try
                {
                    using (var con = new SQLiteConnection(GetStackDatabaseConnectionString(traceId)))
                    {
                        con.Open();

                        using (var cmdStack = con.CreateCommand())
                        {
                            cmdStack.Parameters.Add(_cmdParamStackCallNumber);
                            cmdStack.Parameters.Add(_cmdParamStackFullStackInfo);

                            cmdStack.CommandText = _selectEventStackString + " WHERE Stack.CallNumber " + inText;
                            var r = cmdStack.ExecuteReader();
                            while (r.Read())
                            {
                                var callNumber = (UInt64) (Int64) r["CallNumber"];
                                stacksToRetrieve[callNumber] = DatabaseGetEventCallStackFromReader(ref r);
                            }

                            con.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Stack Database Connection Exception: " + ex.Message);
                }
            }
            return stacksToRetrieve;
        }
        string GetInText(IEnumerable<CallEventId> callIds)
        {
            string inText = " in (";
            var any = false;
            const string commaString = ", ";
            foreach (var callId in callIds)
            {
                any = true;
                inText += callId.CallNumber;
                inText += commaString;
            }
            if(!any)
                return "()";
            return inText.Substring(0, inText.Length - commaString.Length) + ")";
        }
        public CallEvent[] DatabaseGetEvents(CallEventId[] callIds, uint traceId, bool fullStackInfo)
        {
            CallEvent[] ret = null;

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(traceId)))
            {
                con.Open();

                var stacksToRetrieve =
                    new Dictionary<CallEventId, List<DeviareTools.DeviareStackFrame>>();

                using (var cmd = con.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = _selectEventString +
                                          " FROM Events LEFT JOIN Parameters ON Events.CallNumber=Parameters.CallNumber";

                        string inText = GetInText(callIds);

                        cmd.CommandText += " WHERE Events.CallNumber " + inText + " ORDER BY Events.CallNumber;";
 
                        var evList = new List<CallEvent>();
                        cmd.CommandType = CommandType.Text;
                        var r = cmd.ExecuteReader();

                        UInt64 nextCallNumber = r.Read() ? (UInt64)(Int64)r["CallNumber"] : 0;
                        while (nextCallNumber != 0)
                        {
                            var callEvent = DatabaseGetEventFromReader(ref r, nextCallNumber,
                                                                       true, true,
                                                                       traceId,
                                                                       out nextCallNumber);
                            evList.Add(callEvent);
                            stacksToRetrieve[callEvent.EventId] = null;
                            if (callEvent.Peer != 0)
                                stacksToRetrieve[new CallEventId(callEvent.Peer, callEvent.TraceId)] = null;
                        }

                        ret = evList.ToArray();
                        r.Close();

                        if (fullStackInfo)
                        {
                            var stacks = DatabaseGetStacks(traceId, GetInText(stacksToRetrieve.Keys));
                            foreach(var callEvent in ret)
                            {
                                List<DeviareTools.DeviareStackFrame> stackInfo;
                                if(stacks.TryGetValue(callEvent.CallNumber, out stackInfo))
                                {
                                    callEvent.CallStack = stackInfo;
                                }
                                else if (stacks.TryGetValue(callEvent.Peer, out stackInfo))
                                {
                                    callEvent.CallStack = stackInfo;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database Exception: " + ex.Message);
                    }
                }

                con.Close();
            }
            return ret;
        }
        public CallEvent DatabaseGetFirstEvent(uint traceId)
        {
            CallEvent ret = null;

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(traceId)))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = _selectEventString +
                                          "FROM Events " +
                                          "LEFT JOIN Parameters ON Events.CallNumber=Parameters.CallNumber ";
                        cmd.CommandType = CommandType.Text;
                        var r = cmd.ExecuteReader();

                        while (r.Read())
                        {
                            var callNumber = (UInt64)(Int64)r["CallNumber"];
                            var callEvent = DatabaseGetEventFromReader(ref r, callNumber,
                                                                       false, false, traceId,
                                                                       out callNumber);
                            ret = callEvent;
                            break;
                        }

                        r.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database Exception: " + ex.Message);
                    }
                }

                con.Close();
            }
            return ret;
        }

        public CallEvent[] DatabaseGetAllEvents(uint traceId)
        {
            CallEvent[] ret = null;

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(traceId)))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = _selectEventString +
                                          "FROM Events LEFT JOIN Parameters ON Events.CallNumber=Parameters.CallNumber ";

                        cmd.CommandText += " ORDER BY CallNumber;";

                        var evList = new List<CallEvent>();
                        cmd.CommandType = CommandType.Text;
                        var r = cmd.ExecuteReader();

                        UInt64 nextCallNumber = r.Read() ? (UInt64)(Int64)r["CallNumber"] : 0;
                        while (nextCallNumber != 0)
                        {
                            var callEvent = DatabaseGetEventFromReader(ref r, nextCallNumber,
                                                                       false, false,
                                                                       traceId,
                                                                       out nextCallNumber);
                            evList.Add(callEvent);
                        }

                        ret = evList.ToArray();
                        r.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database Exception: " + ex.Message);
                    }
                }

                con.Close();
            }
            return ret;
        }

        private int DatabaseGetEventCount(uint traceId)
        {
            int ret;

            using (var con = new SQLiteConnection(GetDatabaseConnectionString(traceId)))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT COUNT(CallNumber) FROM Events";
                        cmd.CommandType = CommandType.Text;

                        ret = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database Exception: " + ex.Message);
                        ret = 0;
                    }
                }

                con.Close();
            }
            return ret;
        }
        private void UpdateCallEvent(CallEvent callEvent, EventProperties eventProperties)
        {
            if (eventProperties.EventFlagsSet)
                callEvent.EventFlags = eventProperties.EventFlags;
            if (eventProperties.PrioritySet)
                callEvent.Priority = eventProperties.Priority;
        }
        private void DatabaseUpdateEventProperties(UpdateEventPropertiesDbOperation updateEventOp)
        {
            CallEvent callEvent = null;
            using (var con = new SQLiteConnection(GetDatabaseConnectionString(updateEventOp.TraceId)))
            {
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    using (var tr = con.BeginTransaction())
                    {
                        try
                        {
                            cmd.CommandText =
                                @"UPDATE Events SET ";
                            if(updateEventOp.EventProperties.EventFlagsSet)
                            {
                                cmd.CommandText += @"EventFlags = @EventFlags ";

                                cmd.Parameters.Add(new SQLiteParameter("@EventFlags",
                                                                       updateEventOp.EventProperties.EventFlags));
                            }
                            if (updateEventOp.EventProperties.PrioritySet)
                            {
                                cmd.CommandText += (updateEventOp.EventProperties.EventFlagsSet ? ", " : string.Empty) +
                                                   @"Priority = @Priority ";
                                cmd.Parameters.Add(new SQLiteParameter("@Priority",
                                                                       updateEventOp.EventProperties.Priority));
                            }

                            //UpdateCallEvent(callEvent, updateEventOp.EventProperties);

                            cmd.CommandText +=
                                @"WHERE CallNumber = @CallNumber";

                            cmd.Parameters.Add(new SQLiteParameter("@CallNumber", updateEventOp.EventId.CallNumber));
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Error.WriteLine("Database exception: " + ex.Message);
                        }
                        tr.Commit();

                        var callEventArray = DatabaseGetEvents(new[] {updateEventOp.EventId}, updateEventOp.EventId.TraceId,
                                          false);
                        if (callEventArray != null && callEventArray.Length == 1)
                            callEvent = callEventArray[0];
                    }
                }
                con.Close();
            }

            if (callEvent != null)
            {
                lock (_pendingUpdatedEventsToReport)
                {
                    List<CallEvent> evList;
                    if (!_pendingUpdatedEventsToReport.TryGetValue(updateEventOp.TraceId, out evList))
                    {
                        _pendingUpdatedEventsToReport[updateEventOp.TraceId] = evList = new List<CallEvent>();
                    }
                    evList.Add(callEvent);
                }
            }
        }


        #region API

        public void DestroyDatabase(uint traceId)
        {
            IconCache.GetInstance().Clear();
            var pecm = PendingEventsCountManager.GetInstance();
            lock (_pendingNewEventsToReport)
            {
                List<CallEvent> events;
                if (_pendingNewEventsToReport.TryGetValue(traceId, out events))
                {
                    pecm.EventsLeave(events.Count, PendingEventsCountManager.GuiPhase);
                    _pendingNewEventsToReport.Remove(traceId);
                }
            }


            // Capture happens in TraceId == 1
            lock (_pendingNewEventsToReport)
            {
                List<CallEvent> events;
                if (_pendingNewEventsToReport.TryGetValue(traceId, out events))
                {
                    pecm.EventsLeave(events.Count, PendingEventsCountManager.DatabasePhase);
                    _pendingNewEventsToReport.Remove(traceId);
                }
            }
            if (traceId == CaptureTraceId)
            {
                _priorityAnalyzer.Clear();
                _eventPeerMatching.Clear();
            }

            lock (_pendingEventsToAddById)
            {
                _pendingEventsToAddById.Remove(traceId);
            }
            lock (_eventsToComplete)
            {
                pecm.EventsLeave(_eventsToComplete.Count, PendingEventsCountManager.CompleteEventPhase);
                _eventsToComplete.Clear();
            }
            lock (_pendingStacksToAdd)
            {
                _pendingStacksToAdd.Remove(traceId);
            }

            lock (_pendingEventsToAdd)
            {
                List<CallEvent> events;
                if (_pendingEventsToAdd.TryGetValue(traceId, out events))
                {
                    pecm.EventsLeave(events.Count, PendingEventsCountManager.DatabasePhase);
                    _pendingEventsToAdd.Remove(traceId);
                }
            }
            lock (_refreshEventsData)
            {
                RefreshEventsDbOperation refreshDbOp;
                if (_refreshEventsData.TryGetValue(traceId, out refreshDbOp))
                {
                    refreshDbOp.WaitEvent.Set();
                    _refreshEventsData.Remove(traceId);
                }
            }
            DatabaseInfo dbInfo;
            lock (_dbTraceIdPath)
            {
                if (_dbTraceIdPath.TryGetValue(traceId, out dbInfo))
                    _dbTraceIdPath.Remove(traceId);
            }
            try
            {
                if (dbInfo.EventDbPath != default(String))
                    File.Delete(dbInfo.EventDbPath);
                if (dbInfo.StackDbPath != default(String))
                    File.Delete(dbInfo.StackDbPath);
            }
            catch (Exception)
            {
            }
            lock (_clearedDbs)
            {
                _clearedDbs.Add(traceId);
            }
        }

        public void UpdateEventProperties(CallEventId callEventId, EventProperties evProps)
        {
            lock (_pendingEventsToAddById)
            {
                Dictionary<UInt64, CallEvent> eventDict;
                CallEvent callEvent;
                if (_pendingEventsToAddById.TryGetValue(callEventId.TraceId, out eventDict))
                {
                    if (eventDict.TryGetValue(callEventId.CallNumber, out callEvent))
                    {
                        UpdateCallEvent(callEvent, evProps);
                        return;
                    }
                }
            }
            lock (_pendingOps)
            {
                _pendingOps.Add(new UpdateEventPropertiesDbOperation(callEventId, evProps));
            }
        }

        public void AddEventRange(IEnumerable<CallEvent> callEvents)
        {
            AddEventRange(callEvents, true);
        }
        public void AddEventRange(IEnumerable<CallEvent> callEvents, bool report)
        {
            uint traceId = 0;
            bool anyStack = false;
            var eventsToAdd = new List<CallEvent>();
            var stackToAdd = new List<StackInfoToAdd>();
            bool eliminating = false;
            foreach (var callEvent in callEvents)
            {
                if (eliminating)
                {
                    if (callEvent.IsClearSignalEvent)
                        PendingEventsCountManager.GetInstance().EventsLeave(1, PendingEventsCountManager.DatabasePhase);
                    continue;
                }
                traceId = callEvent.TraceId;
                if (callEvent.IsClearSignalEvent)
                {
                    PendingEventsCountManager.GetInstance().EventsLeave(1, PendingEventsCountManager.DatabasePhase);
                    ClearDatabase(callEvent.TraceId);
                    eliminating = true;
                    continue;
                }
                lock (_pendingEventsToAddById)
                {
                    Dictionary<UInt64, CallEvent> eventDict;
                    if (!_pendingEventsToAddById.TryGetValue(callEvent.TraceId, out eventDict))
                    {
                        _pendingEventsToAddById[callEvent.TraceId] = eventDict = new Dictionary<ulong, CallEvent>();
                    }
                    eventDict[callEvent.CallNumber] = callEvent;
                }

                callEvent.Tag = report;
                //CompleteEvent(callEvent);
                eventsToAdd.Add(callEvent);
                if (callEvent.CallStackStrings != null && callEvent.CallStackStrings.Length > 0)
                {
                    stackToAdd.Add(new StackInfoToAdd
                                       {
                                           CallNumber = callEvent.CallNumber,
                                           StackStrings = callEvent.CallStackStrings
                                       });
                    anyStack = true;
                }
            }
            if (traceId != 0)
            {
                lock (_eventsToComplete)
                {
                    List<CallEvent> evList;
                    if (!_eventsToComplete.TryGetValue(traceId, out evList))
                    {
                        _eventsToComplete[traceId] = evList = new List<CallEvent>();
                    }
                    evList.AddRange(eventsToAdd);
                    _completeEvent.Set();
                }
                if (anyStack)
                {
                    lock (_pendingStacksToAdd)
                    {
                        List<StackInfoToAdd> evList;
                        if (!_pendingStacksToAdd.TryGetValue(traceId, out evList))
                        {
                            _pendingStacksToAdd[traceId] = evList = new List<StackInfoToAdd>();
                        }
                        evList.AddRange(stackToAdd);
                        _stackDbEvent.Set();
                    }
                }
            }
        }

        public void AddEvent(CallEvent callEvent)
        {
            AddEvent(callEvent, true);
        }

        public void AddEvent(CallEvent callEvent, bool report)
        {
            AddEventRange(new[] {callEvent});
        }

        public void ClearDatabase(uint traceId)
        {
            lock (_pendingOps)
            {
                _pendingOps.Add(new ClearDbOperation(traceId));
            }
        }

        /// <summary>
        /// Get all events.
        /// </summary>
        public void RefreshEvents(EventsReportData reportData)
        {
            var data = new RefreshEventsDbOperation(reportData.TraceId, reportData.RefreshId)
                           {ReportData = reportData};
            lock (_pendingOps)
            {
                _pendingOps.Add(data);
            }
        }

        public void RefreshCancel(uint refreshId)
        {
            lock (_refreshEventsData)
            {
                RefreshEventsDbOperation refreshData;
                if (_refreshEventsData.TryGetValue(refreshId, out refreshData))
                {
                    refreshData.Canceled = true;
                }
            }
        }
        public void WaitProcessEvents(EventsReportData reportData)
        {
            WaitEventAndShutdown(reportData.Event, true);
        }

        public CallEvent[] SearchEvents(uint traceId, string searchText, bool matchWholeWord,
                                        bool matchCase, bool forward, int maxEventsToRetrieve,
                                        UInt64 startCallNumber,
                                        HashSet<UInt64> callNumberFilter)
        {
            var op = new SearchEventsDbOperation(traceId)
            {
                MatchCase = matchCase,
                MatchWholeWord = matchWholeWord,
                SearchForward = forward,
                SearchText = searchText,
                StartCallNumber = startCallNumber,
                CallNumberFilter = callNumberFilter,
                MaxEventCountToRetrieve = maxEventsToRetrieve
            };
            lock (_pendingOps)
            {
                _pendingOps.Add(op);
            }
            WaitEventAndShutdown(op.WaitEvent, false);

            return op.Events;
        }

        /// <summary>
        /// Return false if the database is shutdowning. If ev is signaled it return true
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="processEvents"></param>
        /// <returns></returns>
        public bool WaitEventAndShutdown(EventWaitHandle ev, bool processEvents)
        {
            var waitHandles = new WaitHandle[2];
            waitHandles[0] = _dbShutdownEvent;
            waitHandles[1] = ev;

            int index;
            if (processEvents)
            {
                index = WaitHandle.WaitAny(waitHandles, 50);
                while (index == WaitHandle.WaitTimeout)
                {
                    Application.DoEvents();
                    index = WaitHandle.WaitAny(waitHandles, 50);
                }
            }
            else
            {
                index = WaitHandle.WaitAny(waitHandles);
            }
            return index != 0;
        }
        public CallEvent GetFirstEvent(uint traceId)
        {
            GetFirstEventDbOperation operation;
            lock (_pendingOps)
            {
                operation = new GetFirstEventDbOperation(traceId);
                _pendingOps.Add(operation);
            }
            
            WaitEventAndShutdown(operation.WaitEvent, false);

            return operation.Event;
        }

        public CallEvent GetEvent(CallEventId callId, bool fullStackInfo)
        {
            return GetEvent(callId, fullStackInfo, false);
        }

        public CallEvent GetEvent(CallEventId callId, bool fullStackInfo, bool processEvent)
        {
            var eventIds = new CallEventId[1];
            eventIds[0] = callId;
            var events = GetEvents(eventIds, fullStackInfo, false);

            return events != null && events.Length > 0 ? events[0] : null;
        }

        public CallEvent[] GetEvents(IEnumerable<CallEventId> callIds, bool fullStackInfo)
        {
            return GetEvents(callIds, fullStackInfo, false);
        }

        public CallEvent[] GetEvents(IEnumerable<CallEventId> callIds, bool fullStackInfo, bool processEvent)
        {
            var callIdsByTraceId = new Dictionary<uint, List<CallEventId>>();
            var ret = new List<CallEvent>();

            foreach (var callId in callIds)
            {
                List<CallEventId> callIdList;
                if (!callIdsByTraceId.TryGetValue(callId.TraceId, out callIdList))
                {
                    callIdList = callIdsByTraceId[callId.TraceId] = new List<CallEventId>();
                }
                callIdList.Add(callId);
            }
            foreach (var callId in callIdsByTraceId.Values)
            {
                GetEventsDbOperation operation;
                lock (_pendingOps)
                {
                    operation = new GetEventsDbOperation(callId.ToArray(), fullStackInfo);
                    _pendingOps.Add(operation);
                }

                if(WaitEventAndShutdown(operation.WaitEvent, processEvent))
                {
                    ret.AddRange(operation.Events);
                }
            }
            return ret.ToArray();
        }

        public CallEvent[] GetAllEvents(uint traceId)
        {
            GetAllEventsDbOperation data;
            lock (_pendingOps)
            {
                data = new GetAllEventsDbOperation(traceId);
                _pendingOps.Add(data);
            }
            if (WaitEventAndShutdown(data.WaitEvent, true))
            {
                return data.Events;
            }

            return new CallEvent[0];
        }

        public int GetEventCount(uint traceId)
        {
            return GetEventCount(traceId, false);
        }

        public int GetEventCount(uint traceId, bool processEvents)
        {
            GetEventCountDbOperation data;
            lock (_pendingOps)
            {
                data = new GetEventCountDbOperation(traceId);
                _pendingOps.Add(data);
            }
            if (WaitEventAndShutdown(data.WaitEvent, processEvents))
            {
                return data.Count;
            }

            return 0;
        }

        public void LockDatabase(uint traceId)
        {
            LockDbOperation op;
            lock (_pendingOps)
            {
                op = new LockDbOperation(traceId);
                _pendingOps.Add(op);
            }
            WaitEventAndShutdown(op.WaitEvent, false);
        }

        public void UnlockDatabase(uint traceId)
        {
            UnlockDbOperation op;
            lock (_pendingOps)
            {
                op = new UnlockDbOperation(traceId);
                _pendingOps.Add(op);
            }
            // wait until processed
            WaitEventAndShutdown(op.WaitEvent, false);
        }

        public bool Save(uint traceId, bool xmlMode, string filename, out string error)
        {
            error = string.Empty;
            try
            {
                var dbInfo = GetDatabasePath(traceId);
                using (var zip = new ZipFile())
                {
                    //zip.AddProgress()
                    zip.CompressionLevel = CompressionLevel.BestSpeed;
                    zip.AddFile(dbInfo.EventDbPath, "");
                    zip.AddFile(dbInfo.StackDbPath, "");
                    zip.Save(filename);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            return true;
        }

        #endregion API

        public bool AnyUpdateOperationPending()
        {
            lock (_pendingEventsToAdd)
                lock (_pendingStacksToAdd)
                    lock (_eventsToComplete)
                    {
                        if (_pendingEventsToAdd.Any(pendingInTraceId => pendingInTraceId.Value.Count > 0) ||
                            _pendingStacksToAdd.Any(pendingInTraceId => pendingInTraceId.Value.Count > 0) ||
                            _eventsToComplete.Count > 0)
                        {
                            return true;
                        }
                    }
            return false;
        }

        public int PendingEventsToAdd()
        {
            lock (_pendingEventsToAdd)
                lock (_pendingStacksToAdd)
                    lock (_eventsToComplete)
                    {
                        return
                            Math.Max(
                                _pendingEventsToAdd.Sum(ev => ev.Value.Count) +
                                _eventsToComplete.Sum(ev => ev.Value.Count),
                                _pendingStacksToAdd.Sum(ev => ev.Value.Count));
                    }
        }
    }
}