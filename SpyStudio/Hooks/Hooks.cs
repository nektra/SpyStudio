#define TURN_THINAPP_OFF
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Win32;
using Nektra.Deviare2;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using SpyStudio.Database;
using SpyStudio.Dialogs;
using SpyStudio.Extensions;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using SpyStudio.Trace;
using SpyStudio.Hooks.Async;
using SpyStudio.Windows.Controls;
using Timer = System.Windows.Forms.Timer;
using SpyStudio.COM;

namespace SpyStudio.Hooks
{
    public class HookMgr
    {
        public AsyncHookMgr AsyncHookMgr;

        readonly Dictionary<IntPtr, DeviareHook> _hookMap = new Dictionary<IntPtr, DeviareHook>();

        //readonly Dictionary<uint, > 
        // processId moduleName
        readonly Dictionary<uint, List<ListView>> _connectedListViews = new Dictionary<uint, List<ListView>>();
        private readonly List<HookStateChange> _pendingStateChanges = new List<HookStateChange>();
        private readonly Timer _lvTimer = new Timer();
        private double _startTime;

        public double StartTime
        {
            get { return _startTime; }
        }

        readonly HashSet<IntPtr> _activeHooks = new HashSet<IntPtr>();
        readonly HashSet<IntPtr> _volatileHooks = new HashSet<IntPtr>();
        NktSpyMgr _spyMgr;
        private DeviareLiteInterop.HookLib _miniHookLib;
        bool _monitoring;
        DeviareRunTrace _devRunTrace;
        // pids that whose Com Servers modules were hooked
        readonly HashSet<uint> _processModuleHookProcessed = new HashSet<uint>();
        private readonly HashSet<uint> _processedLoadedModulesProcessIds = new HashSet<uint>();
        readonly Dictionary<string, Int64> _times = new Dictionary<string, long>();
        readonly Dictionary<string, Int64> _counts = new Dictionary<string, long>();
        private bool _hookNewUserProcesses = false;
        private bool _collectTimes = false;

        private string[] _blackList = new[] { "spystudio.exe", "devenv.exe", "msvsmon.exe" };
        #region Events
        public class MonitoringChangeEventArgs : EventArgs
        {
            public MonitoringChangeEventArgs(bool newValue)
            {
                IsMonitoring = newValue;
            }

            public bool IsMonitoring { get; private set; }
        }

        public delegate void MonitoringChangeEventHandler(object sender, MonitoringChangeEventArgs e);

        public event MonitoringChangeEventHandler MonitoringChange;

        public class PendingEventsChangeEventArgs : EventArgs
        {
            public PendingEventsChangeEventArgs(int eventsInQueue, UInt64 lastCallEventNumberCreated)
            {
                EventsInQueue = eventsInQueue;
                LastCallNumberCreated = lastCallEventNumberCreated;
            }

            public UInt64 LastCallNumberCreated { get; private set; }
            public int EventsInQueue { get; private set; }
        }

        public delegate void PendingEventsChangeEventHandler(object sender, PendingEventsChangeEventArgs e);

        #endregion Events

        public class HookStateChange
        {
            public HookStateChange(uint pid, IntPtr hookId, eNktHookState state)
            {
                Pid = pid;
                HookId = hookId;
                NewState = state;
            }
            public uint Pid;
            public IntPtr HookId;
            public eNktHookState NewState;
        }
        public class AsyncAction
        {
            public enum Type
            {
                Attach,
                Detach,
                ResumeProcess,
                HookLoadedModulesComServers,
                DetachAll
            }

            public Type ActionType;
            public AutoResetEvent ActionFinishedEvent = new AutoResetEvent(false);
        }

        public class AttachAction : AsyncAction
        {
            public AttachAction()
            {
                ActionType = Type.Attach;
            }
            public NktHook Hook;
            public NktHooksEnum Hooks;
            public NktProcess Proc;
        }
        public class DetachAction : AsyncAction
        {
            public DetachAction()
            {
                ActionType = Type.Detach;
            }
            public NktHook Hook;
            public NktHooksEnum Hooks;
            public NktProcess Proc;
        }
        public class DetachAllAction : AsyncAction
        {
            public DetachAllAction()
            {
                ActionType = Type.DetachAll;
            }
        }
        public class ResumeProcessAction : AsyncAction
        {
            public ResumeProcessAction()
            {
                ActionType = Type.ResumeProcess;
            }
            public KeyValuePair<object, NktProcess> ResumeProcessEvent;
        }
        public class HookLoadedModulesComServersAction : AsyncAction
        {
            public HookLoadedModulesComServersAction()
            {
                ActionType = Type.HookLoadedModulesComServers;
            }
            public NktProcess Proc;
        }

        public class HookProperties
        {
            public HookProperties(HookType hookType, int tag, string functionName, int flags, bool isSecondary)
            {
                HookType = hookType;
                Tag = tag;
                FunctionName = functionName;
                DisplayName = hookType.ToString();
                if (isSecondary != false)
                    DisplayName += " [BASE]";
                Flags = flags;
                IsSecondary = isSecondary;
            }
            public HookType HookType;
            public int Tag;
            public string FunctionName;
            public string DisplayName;
            public int Flags;
            public bool IsSecondary;
        }
        public readonly Dictionary<IntPtr, HookProperties> HookIdProps = new Dictionary<IntPtr, HookProperties>();
        readonly Dictionary<IntPtr, NktHook> _hookIdMap = new Dictionary<IntPtr, NktHook>();
        readonly ModulePath _modulePath;
        private readonly WindowClassNames _windowClassNames;
        readonly Dictionary<string, IntPtr> _dllGetClassObjectHookIds = new Dictionary<string, IntPtr>();
        private readonly string[] szSecondaryHook_NtDll_Apis = new string[] {
                "NtOpenKey", "NtOpenKeyEx", "NtCreateKey", "NtQueryKey", "NtQueryValueKey", "NtQueryMultipleValueKey",
                "NtSetValueKey", "NtDeleteValueKey", "NtDeleteKey", "NtEnumerateValueKey", "NtEnumerateKey",
                "NtRenameKey", "NtCreateFile", "NtOpenFile", "NtDeleteFile", "NtQueryDirectoryFile",
                "NtQueryAttributesFile"
        };

        // file system
        //IntPtr _createFileWId, _createDirectoryWId;

        // messages: see with which application is this application interacting

        // process creation
        //IntPtr _createProcessInternalWId;
        private readonly ParamHandlerManager _paramHandlerMgr = new ParamHandlerManager();

        public ParamHandlerManager ParamHandlerMgr
        {
            get { return _paramHandlerMgr; }
        }

        readonly Dictionary<uint, int> _coCreateThreadCount = new Dictionary<uint, int>();
        private readonly HashSet<int> _attachedPids = new HashSet<int>();
        readonly QueuedWorkerThread<AsyncAction> _delayedAction;
        readonly HookGroupMgr _hookGroupMgr = new HookGroupMgr();
        readonly HookStateMgr _hookStateMgr;
        readonly Dictionary<int, Dictionary<string, IntPtr>> _secondaryHooks = new Dictionary<int, Dictionary<string, IntPtr>>();
        private readonly ProcessInfo _processInfo;
        EventFilter.Filter _filter;

        /// <summary>
        /// Triggered in the context of initialization with InitializationForm displayed to do extra initialization in that context
        /// </summary>
        public event DeviareInitializer.InitializeFinishedEventHandler InitializationFinished;

        public HookMgr()
        {
            AsyncHookMgr = new AsyncHookMgr(this);
            _processInfo = new ProcessInfo();
            _modulePath = new ModulePath();

            _windowClassNames = new WindowClassNames();
            _hookStateMgr = new HookStateMgr(_hookGroupMgr);
            _delayedAction = new QueuedWorkerThread<AsyncAction>(DoAction);

            RegistryTools.Initialize();

            _delayedAction.Start();
        }

        public void Initialize(Form uiForm)
        {
            EventDatabaseMgr.GetInstance().Start(uiForm);

            DeviareInitializer.GetInstance().InitializationFinished += DeviareThreadOnInitializeFinished;
            DeviareInitializer.GetInstance().Start();
            //InitializeHooks();
        }

        public void SetInitializationProgress(int progress)
        {
            DeviareInitializer.GetInstance().Progress = progress;
        }
        private void DeviareThreadOnInitializeFinished(object sender, DeviareInitializer.InitializeFinishedEventArgs initializeFinishedEventArgs)
        {
            if(initializeFinishedEventArgs.Success)
            {
                SetSpyMgr(DeviareInitializer.GetInstance().SpyMgr);
                SetMiniHookLib(DeviareInitializer.GetInstance().MiniHookLib);

                InitializeHooks();

                // consume 30% of remaining progress
                DeviareInitializer.GetInstance().Progress = initializeFinishedEventArgs.MinimumProgress +
                                          30*
                                          (initializeFinishedEventArgs.MaximumProgress -
                                           initializeFinishedEventArgs.MinimumProgress)/100;

                //return true;

                _filter = EventFilter.Filter.GetMainWindowFilter();
                _devRunTrace = new DeviareRunTrace(_processInfo, _modulePath);
                _devRunTrace.SetFilter(_filter, true);

            }
            
            if(InitializationFinished != null)
            {
                InitializationFinished(this,
                                       new DeviareInitializer.InitializeFinishedEventArgs(
                                           initializeFinishedEventArgs.Success, DeviareInitializer.GetInstance().Progress,
                                           initializeFinishedEventArgs.MaximumProgress));
            }

            DeviareInitializer.GetInstance().Progress = initializeFinishedEventArgs.MaximumProgress;
        }

        public void Shutdown()
        {
            var database = EventDatabaseMgr.GetInstance(false);
            if(database != null)
                database.Shutdown();

            DetachAllProcesses(true);
            SetSpyMgr(null);
            DeviareInitializer.GetInstance().Shutdown();
            _delayedAction.Stop();
            AsyncHookMgr.Shutdown();
        }

        public NktSpyMgr SpyMgr { get { return _spyMgr; } }
        public ProcessInfo ProcessInfo { get { return _processInfo; } }
        public ModulePath ModulePath { get { return _modulePath; } }
        public DeviareRunTrace DeviareRunTrace { get { return _devRunTrace; } }
        public EventFilter.Filter CurrentFilter { get { return _filter; } }
        public bool Verbose { get; set; }
        public bool InstallerMode { get; set; }
        public bool EnableStackLogging = Settings.Default.FullStackInfo;
        public bool OmitCallStack
        {
            get { return InstallerMode || !EnableStackLogging; }
        }

        public bool MonitorDotNetGc = Settings.Default.MonitorDotNetGc;
        public bool MonitorDotNetJit = Settings.Default.MonitorDotNetJit;
        public bool MonitorDotNetObjectCreation = Settings.Default.MonitorDotNetObjectAllocations;
        public bool MonitorDotNetExceptions = Settings.Default.MonitorDotNetExceptions;

        public bool ExecuteProgramAndHook(string cmdLine)
        {
            return ExecuteProgramAndHook(cmdLine, "", "", false);
        }

        public bool ExecuteProgramAndHook(string cmdLine, string userName, string password, bool installer)
        {
            var ret = false;
            object contEvent;

            InstallerMode = installer;
            Start();

            var proc = string.IsNullOrEmpty(userName) ?
                _spyMgr.CreateProcess(cmdLine, true, out contEvent) :
                _spyMgr.CreateProcessWithLogon(cmdLine, userName, password, true, out contEvent);

            if (proc != null)
            {
                Attach(proc, true, new KeyValuePair<object, NktProcess>(contEvent, proc));
                ret = true;
            }
            else
            {
                StopAnalysis();
            }

            //UpdateButtonsState();
            return ret;
        }
        public void WatchProcesses(string procName, string userName)
        {
            NktProcessesEnum enumProcs = _spyMgr.Processes();

            Start();

            procName = procName.ToLower();

            foreach (NktProcess proc in enumProcs)
            {
                if (IsHookeable(proc))
                {
                    var currentProcName = proc.Name.ToLower();

                    if((string.IsNullOrEmpty(procName) || String.Compare(currentProcName, procName, StringComparison.OrdinalIgnoreCase) == 0) && 
                    (string.IsNullOrEmpty(userName) || String.Compare(proc.UserName, userName, StringComparison.OrdinalIgnoreCase) == 0) && 
                        !_blackList.Contains(currentProcName))
                    {
                        try
                        {
                            _spyMgr.LoadAgent(proc.Id);
                            if (Verbose)
                            {
                                Error.WriteLine("Watching " + proc.Name + " [" + proc.Id + "]");
                            }
                        }
                        catch (Exception ex)
                        {
                            Error.WriteLine("Cannot watch " + proc.Name + " [" + proc.Id + "]: " + ex.Message);
                        }
                    }
                }
            }
        }
        public void WatchProcessesByName(string procName)
        {
            WatchProcesses(procName, "");
        }
        public void WatchAllUserProcesses()
        {
            NktProcessesEnum enumProcs = _spyMgr.Processes();
            NktProcess currentProc = enumProcs.GetById(Process.GetCurrentProcess().Id);
            if(currentProc == null)
            {
                Error.WriteLine("Cannot get current process");
                return;
            }

            WatchProcesses("", currentProc.UserName);
        }
        public void TerminateProcess(string procName)
        {
            NktProcessesEnum enumProcs = _spyMgr.Processes();

            foreach (NktProcess proc in enumProcs)
            {
                if (String.Compare(proc.Name, procName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    try
                    {
                        var p = Process.GetProcessById(proc.Id);
                        p.Kill();
                        if (Verbose)
                            Error.WriteLine("Process terminated: " + procName + " [" + proc.Id + "]");
                    }
                    catch (Exception ex)
                    {
                        if (Verbose)
                            Error.WriteLine("Error terminating process: " + procName + " [" + proc.Id + "]" + ex.Message);
                    }
                }
            }
        }
        public void HookProcessByName(string procName)
        {
            NktProcessesEnum enumProcs = _spyMgr.Processes();
            foreach (NktProcess proc in enumProcs)
            {
                if (proc.Name == procName)
                {
                    Attach(proc, true);
                    if (Verbose)
                        Error.WriteLine(procName + " [" + proc.Id + "] Attached");
                }
            }
        }
        public void StopAnalysis()
        {
            StopAnalysis(false);
        }
        public void StopAnalysis(bool waitCompletion)
        {
            if (!IsMonitoring)
                return;
            DetachAllProcesses(waitCompletion);
        }

        public bool AnyAttachedPid()
        {
            lock(_attachedPids)
            {
                return _attachedPids.Any();
            }
        }
        public bool IsAttached(int pid)
        {
            lock (_attachedPids)
            {
                return _attachedPids.Contains(pid);
            }
        }
        public void ProcessTerminated(NktProcess proc)
        {
            ProcessInfo.ProcessTerminated((uint) proc.Id);
            RemoveAttachedPid(proc.Id, true);
#if !TURN_THINAPP_OFF
            RemoveSecondaryHooks(proc.Id);
#endif
        }
        public void Clear()
        {
            if (_collectTimes)
            {
                lock(_times)
                {
                    foreach (var t in _times)
                    {
                        Error.WriteLine(t.Key + ":\t" + _counts[t.Key] + "\t" + t.Value);
                    }
                    _times.Clear();
                    _counts.Clear();
                }
            }
            _modulePath.Clear();
            _windowClassNames.Clear();
            DeviareTools.StackCache.Clear();
            lock (_processedLoadedModulesProcessIds)
            {
                _processedLoadedModulesProcessIds.Clear();
            }
            lock (_processModuleHookProcessed)
            {
                _processModuleHookProcessed.Clear();
            }
            AsyncHookMgr.Clear();
            _startTime = 0;
        }

        public void SetMiniHookLib(DeviareLiteInterop.HookLib miniHookLib)
        {
            _miniHookLib = miniHookLib;
        }

        public void SetSpyMgr(NktSpyMgr spyMgr)
        {
            if (_spyMgr != null)
            {
                _spyMgr.OnHookStateChanged -= Deviare_OnStateChanged;
                _spyMgr.OnFunctionCalled -= Deviare_OnFunctionCalled;
                _spyMgr.OnCreateProcessCall -= Deviare_OnCreateProcessCall;
                _spyMgr.OnProcessStarted -= Deviare_OnProcessStarted;
                _spyMgr.OnProcessTerminated -= Deviare_OnProcessTerminated;
                _spyMgr.OnCustomMessage -= AsyncHookMgr.Deviare_OnCustomMessageEvent;
                _spyMgr.OnAgentLoad -= Deviare_OnAgentLoad;
                _spyMgr.OnAgentUnload -= Deviare_OnAgentUnload;
                _spyMgr.OnLoadLibraryCall -= Deviare_OnLoadLibrary;
                _spyMgr.OnHookOverwritten -= Deviare_OnHookOverwritten;
            }
            _spyMgr = spyMgr;
            if (spyMgr != null)
            {
                _spyMgr.OnHookStateChanged += Deviare_OnStateChanged;
                _spyMgr.OnFunctionCalled += Deviare_OnFunctionCalled;
                _spyMgr.OnCreateProcessCall += Deviare_OnCreateProcessCall;
                _spyMgr.OnProcessStarted += Deviare_OnProcessStarted;
                _spyMgr.OnProcessTerminated += Deviare_OnProcessTerminated;
                _spyMgr.OnCustomMessage += AsyncHookMgr.Deviare_OnCustomMessageEvent;
                _spyMgr.OnLoadLibraryCall += Deviare_OnLoadLibrary;
                _spyMgr.OnAgentLoad += Deviare_OnAgentLoad;
                _spyMgr.OnAgentUnload += Deviare_OnAgentUnload;
                _spyMgr.OnHookOverwritten += Deviare_OnHookOverwritten;
                _processInfo.SetSpyMgr(_spyMgr);
            }
            _hookStateMgr.SetSpyMgr(_spyMgr);
        }

        public bool IsHookeable(NktProcess proc)
        {
            return !(proc.PlatformBits < 32 || proc.PlatformBits > IntPtr.Size * 8) && (Process.GetCurrentProcess().Id != proc.Id);
        }
        public void InsertProcessData(int procId, string procName, string procPath)
        {
            lock (_processInfo)
            {
                if (!_processInfo.Contains((uint) procId))
                {
                    // System process is blank
                    if (string.IsNullOrEmpty(procName) && procId == 4)
                    {
                        procName = "System";
                    }
                    string path;
                    var procIcon = IconGetter.GetIcon(out path, procId, procPath, _processInfo);
                    _processInfo.Add(procName, path, (uint) procId, procIcon);
                }
            }
        }

        private void Deviare_OnProcessTerminated(NktProcess proc)
        {
            ProcessTerminated(proc);
        }

        public void EnsureServicesAreHooked()
        {
            if (!InstallerMode || _servicesTryHooked)
                return;
            _dcomPid = SvcHostTools.FindDCOMSvcHostPid();
            var dcomProcess = SpyMgr.ProcessFromPID(_dcomPid);
            _servicesPid = dcomProcess.ParentId;

            if (dcomProcess.PlatformBits > 0 && dcomProcess.PlatformBits <= IntPtr.Size * 8)
            {
                Attach((uint)_servicesPid, false);
                Attach(dcomProcess, false);
            }

            _servicesTryHooked = true;
        }

        public void AllowCommandLineHash(uint hash)
        {
            EnsureServicesAreHooked();
            AllowableNewServices.Add(hash);
        }

        private bool _servicesTryHooked = false;
        private int _dcomPid = 0;
        private int _servicesPid = 0;
        private bool _msiAllowed = false;
        public HashSet<uint> AllowableNewServices = new HashSet<uint>();

        bool AsyncMode()
        {
            return true;
            //return Settings.Default.AsyncReg &&
            //       DeviareInitializer.CheckFeature(DeviareInitializer.FeatureSupported.AsynchHooking);
        }

        private void Deviare_OnLoadLibrary(NktProcess proc, string dllName, object moduleHandle)
        {
            Int64 p = 0;
            if (moduleHandle is Int32)
                p = (Int32) moduleHandle;
            else if (moduleHandle is Int64)
                p = (Int64) moduleHandle;
            LoadDllModule(proc, (IntPtr) p, true);

            if (Regex.IsMatch(dllName, @"(?:.*\\|^)msi.dll", RegexOptions.IgnoreCase))
            {
                EnsureServicesAreHooked();
                _msiAllowed = true;
            }
        }

        private void Deviare_OnProcessStarted(NktProcess proc)
        {
            if (HookNewProcesses && IsHookeable(proc) && !IsAttached(proc.Id))
                Attach(proc, true);
            //AttachProcess(proc, null);

            if (proc.get_IsActive(10))
            {
                InsertProcessData(proc.Id, proc.Name, proc.Path);
                //ProcessStarted(proc.Id, proc.Name, proc.Path, IsHookeable(proc));
            }
        }

        private void Deviare_OnAgentUnload(NktProcess proc)
        {
            //Debug.WriteLine(proc.Id);
        }

        private void Deviare_OnAgentLoad(NktProcess proc, int errorcode)
        {
            if (errorcode != 0)
            {
                RemoveAttachedPid(proc.Id, true);
            }
        }

        public bool HookNewProcesses
        {
            get { return _hookNewUserProcesses; }
            set
            {
                if(_hookNewUserProcesses != value)
                {
                    _hookNewUserProcesses = value;
                    if(_hookNewUserProcesses)
                    {
                        Start();
                    }
                }
            }
        }

        public bool IsMonitoring
        {
            get { return _monitoring; }
            set
            {
                if(_monitoring != value)
                {
                    _monitoring = value;
                    if(MonitoringChange != null)
                    {
                        MonitoringChange(this, new MonitoringChangeEventArgs(_monitoring));
                    }
                }
            }
        }
        public void CreateHook(NktModule mod, string function, bool before, string group, HookType hookType)
        {
            CreateHook(mod, function, before, true, group, hookType, 0);
        }

        public void CreateHook(NktModule mod, string function, bool before, bool after, string group, HookType hookType)
        {
            CreateHook(mod, function, before, after, group, hookType, 0);
        }

        public void CreateHook(NktModule mod, string function, bool before, string group, HookType hookType, int tag)
        {
            CreateHook(mod, function, before, true, group, hookType, tag);
        }

        string GetPluginDllString()
        {
            if (DeviareTools.GetPlatformBits(_spyMgr) == 32)
                return "SpyStudioHelperPlugin.dll";
            return "SpyStudioHelperPlugin64.dll";
        }
        public static int GetBaseHookFlags(string function)
        {
            //if (function.EndsWith("LdrLoadDll"))
            //    return (int) eNktHookFlags.flgInvalidateCache;
            //if (function.Contains("QWidget"))
            //    return (int)eNktHookFlags.flgAutoHookChildProcess;
            return 0; // TODO: Invalidate cache only when necessary
        }

        public NktHook CreateHook(NktModule mod, string function, bool before, bool after, string group, HookType hookType, int tag)
        {
            NktHook h = null;
            NktExportedFunction functionObj = mod.FunctionByName(function);
            if (functionObj != null)
            {
                int flags = GetBaseHookFlags(function);
                if (before && !after)
                    flags |= (int)eNktHookFlags.flgOnlyPreCall;
                else if (!before && after)
                    flags |= (int)eNktHookFlags.flgOnlyPostCall;

                bool async = AsyncMode();
                if (async)
                    flags |= (int)eNktHookFlags.flgAsyncCallbacks;

                h = AddHook(functionObj, flags, group);
                if(h != null)
                {
                    var hookId = h.Id;
                    var props = new HookProperties(hookType, tag, h.FunctionName, flags, false);
                    lock (HookIdProps)
                    {
                        HookIdProps[hookId] = props;
                    }
                    AsyncHookMgr.CreateHook(hookId, h, mod, function, props);
                    if (async)
                    {
                        var agentInitializationParameters = AsyncHookMgr.SetUpAgentParameters(flags);
                        h.AddCustomHandler(GetPluginDllString(), 0, agentInitializationParameters);
                    }
                }
            }
            return h;
        }

        void InitializeOle32Hooks(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("ole32.dll");
            if (mod == null)
                return;
            CreateHook(mod, "CoCreateInstance", true, "ActiveX", HookType.CoCreate, 0);
            CreateHook(mod, "CoCreateInstanceEx", true, "ActiveX", HookType.CoCreate, 1);
        }

        void InitializeNtDllHooks(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("ntdll.dll");
            if (mod == null)
                return;

            CreateHook(mod, "LdrLoadDll", true, true, "Process", HookType.LoadLibrary, 0);
            CreateHook(mod, "NtOpenKey", false, "Registry", HookType.RegOpenKey, 0);
            CreateHook(mod, "NtOpenKeyEx", false, "Registry", HookType.RegOpenKey, 1);
            CreateHook(mod, "NtCreateKey", false, "Registry", HookType.RegCreateKey, 0);
            CreateHook(mod, "NtQueryKey", false, "Registry", HookType.RegQueryKey, 0);
            CreateHook(mod, "NtQueryValueKey", false, "Registry", HookType.RegQueryValue, 0);
            CreateHook(mod, "NtQueryMultipleValueKey", false, "Registry", HookType.RegQueryMultipleValues, 0);
            CreateHook(mod, "NtSetValueKey", false, "Registry", HookType.RegSetValue, 0);
            CreateHook(mod, "NtDeleteValueKey", false, "Registry", HookType.RegDeleteValue, 0);
            CreateHook(mod, "NtDeleteKey", true, true, "Registry", HookType.RegDeleteKey, 0);
            CreateHook(mod, "NtEnumerateValueKey", false, "Registry", HookType.RegEnumerateValueKey, 0);
            CreateHook(mod, "NtEnumerateKey", false, "Registry", HookType.RegEnumerateKey, 0);
            CreateHook(mod, "NtRenameKey", false, "Registry", HookType.RegRenameKey, 0);
            CreateHook(mod, "NtCreateFile", false, "Files", HookType.CreateFile, 0);
            CreateHook(mod, "NtOpenFile", false, "Files", HookType.OpenFile, 0);
            //NtReadFile/NtWriteFile no estan en la base de datos?
            //CreateHook(mod, "NtReadFile", false, "Files", HookType.ReadFile, 0);
            //CreateHook(mod, "NtWriteFile", false, "Files", HookType.WriteFile, 0);
            CreateHook(mod, "NtDeleteFile", false, "Files", HookType.DeleteFile, 0);
            CreateHook(mod, "NtQueryDirectoryFile", false, "Files", HookType.QueryDirectoryFile, 0);
            CreateHook(mod, "NtQueryAttributesFile", false, "Files", HookType.QueryAttributesFile, 0);
            CreateHook(mod, "NtRaiseException", true, false, "Exceptions", HookType.RaiseException, 0);
            //CreateHook(mod, "RtlRaiseException", true, false, "Exceptions", HookType.RaiseException, 1); //this calls "NtRaiseException"
            CreateHook(mod, "NtRaiseHardError", true, false, "Exceptions", HookType.RaiseException, 2);
            CreateHook(mod, "RtlUnhandledExceptionFilter2", true, false, "Exceptions",
                HookType.RaiseException, 3);
        }

        void InitializeUser32Hooks(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("user32.dll");
            if (mod == null)
                return;

            CreateHook(mod, "CreateWindowExW", true, true, "Windows Creation", HookType.CreateWindow, 1);
            CreateHook(mod, "CreateWindowExA", true, true, "Windows Creation", HookType.CreateWindow, 1);
            CreateHook(mod, "CreateDialogIndirectParamAorW", true, true, "Windows Creation",
                HookType.CreateDialog, 1);
            CreateHook(mod, "DialogBoxIndirectParamAorW", true, true, "Windows Creation",
                HookType.CreateDialog, 1);
        }

        void InitializeKernelBaseHooks(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("kernelbase.dll");
            if (mod == null)
                return;

            state.CreateProcessInternalWCreated = (CreateHook(mod, "CreateProcessInternalW", true, true,
                "Process", HookType.CreateProcess, 0) != null);
            state.UnhandledExceptionFilterCreated = (CreateHook(mod, "UnhandledExceptionFilter", true,
                false,
                "Exceptions", HookType.RaiseException, 4) != null);

            state.RegOpenKeyExWCreated =
                (CreateHook(mod, "RegOpenKeyExW", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                 null);
            state.RegCloseKeyCreated =
                (CreateHook(mod, "RegCloseKey", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                 null);
            state.RegQueryValueExWCreated =
                (CreateHook(mod, "RegQueryValueExW", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                 null);

            //    createProcessInternalWCreated = (CreateHook(kernelBase, "CreateProcessInternalW", true, true,
            //                                                "Process", HookType.CreateProcess, 0) != null) ? true : false;
            //    unhandledExceptionFilterCreated = (CreateHook(kernelBase, "UnhandledExceptionFilter", true, false,
            //                                                 "Exceptions", HookType.RaiseException, 4) != null) ? true : false;
        }

        void InitializeKernel32Hooks(NktModulesEnum mods, HooksInitializationState state)
        {
            NktModule mod = mods.GetByName("kernel32.dll");
            if (mod == null)
                return;

            CreateHook(mod, "LoadResource", true, true, "Resources", HookType.LoadResource, 0);
            if (!state.CreateProcessInternalWCreated)
                CreateHook(mod, "CreateProcessInternalW", true, true, "Process", HookType.CreateProcess,
                    0);
            if (!state.UnhandledExceptionFilterCreated)
                CreateHook(mod, "UnhandledExceptionFilter", true, false, "Exceptions",
                    HookType.RaiseException, 4);

#if DEBUG || true
            if (!state.RegOpenKeyExWCreated)
                state.RegOpenKeyExWCreated =
                    (CreateHook(mod, "RegOpenKeyExW", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                     null);
            if (!state.RegCloseKeyCreated)
                state.RegCloseKeyCreated =
                    (CreateHook(mod, "RegCloseKey", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                     null);
            if (!state.RegQueryValueExWCreated)
                state.RegQueryValueExWCreated =
                    (CreateHook(mod, "RegQueryValueExW", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                     null);
#endif
        }

        void InitializeFindResourceExHook(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("kernelbase.dll");
            if (mod == null)
            {
                mod = mods.GetByName("kernel32.dll");
                if (mod == null)
                    return;
            }

            CreateHook(mod, "FindResourceExW", true, "Resources", HookType.FindResource, 0);
        }

        void InitializeAdvApiHooks(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("advapi32.dll");
            if (mod == null)
                return;
            CreateHook(mod, "CreateServiceA", true, true, "Process", HookType.CreateService, 0);
            CreateHook(mod, "CreateServiceW", true, true, "Process", HookType.CreateService, 0);
            CreateHook(mod, "OpenServiceA", true, true, "Process", HookType.OpenService, 0);
            CreateHook(mod, "OpenServiceW", true, true, "Process", HookType.OpenService, 0);

#if DEBUG || true
            if (!state.RegOpenKeyExWCreated)
                state.RegOpenKeyExWCreated =
                    (CreateHook(mod, "RegOpenKeyExW", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                     null);
            if (!state.RegCloseKeyCreated)
                state.RegCloseKeyCreated =
                    (CreateHook(mod, "RegCloseKey", true, false, ".NET", HookType.DotNetProfiler, 0) != null);
            if (!state.RegQueryValueExWCreated)
                state.RegQueryValueExWCreated =
                    (CreateHook(mod, "RegQueryValueExW", true, false, ".NET", HookType.DotNetProfiler, 0) !=
                     null);
#endif
        }

        void InitializeWinInetHooks(NktModulesEnum mods, HooksInitializationState state)
        {
            var mod = mods.GetByName("wininet.dll");
            if (mod == null)
                return;

            CreateHook(mod, "HttpAddRequestHeadersA", false, true, "WinInet",
                HookType.HttpAddRequestHeaders);
            CreateHook(mod, "HttpAddRequestHeadersW", false, true, "WinInet",
                HookType.HttpAddRequestHeaders);
        }
        
        //void InitializeQtHooks(NktModulesEnum mods, HooksInitializationState state)
        //{
        //    NktModule mod = mods.GetByName("Qt5Core.dll");
        //    NktModule modQWidget = mods.GetByName("Qt5Widgets.dll");

        //    if (mod != null)
        //    {
        //        CreateHook(mod, "??0QObject@@IAE@AAVQObjectPrivate@@PAV0@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //        CreateHook(mod, "??0QObject@@QAE@PAV0@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //    }
        //    if (modQWidget != null)
        //    {
        //        CreateHook(modQWidget, "??0QTableWidget@@QAE@PAVQWidget@@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //        CreateHook(modQWidget, "??0QTableView@@QAE@PAVQWidget@@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //        CreateHook(modQWidget, "??0QWidget@@QAE@PAV0@V?$QFlags@W4WindowType@Qt@@@@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //        CreateHook(modQWidget, "??0QWidget@@IAE@AAVQWidgetPrivate@@PAV0@V?$QFlags@W4WindowType@Qt@@@@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //        CreateHook(modQWidget, "?setModel@QTableView@@UAEXPAVQAbstractItemModel@@@Z", true, true, "Qt5", HookType.QTObject, 0);
        //    }

        //    INktDbObjectsEnum functions = _spyMgr.DbFunctions(32);
        //    foreach (INktDbObject function in functions)
        //    {
        //        string name = function.Name;
        //        if (name.Contains("setModel"))
        //            Debugger.Break();
        //    }
        //}

        void InitializeOtherHooks(NktModulesEnum mods, HooksInitializationState state)
        {
            XmlDocument otherHooks = null;
            otherHooks = HookXml.GetHooksXml();
            if (otherHooks == null)
                return;

            XmlNodeList functionList = otherHooks.SelectNodes("/hooks/hook");
            Debug.Assert(functionList != null, "functionList != null");
            NktDbObjectsEnum fncEnum = _spyMgr.DbFunctions(DeviareTools.GetPlatformBits(_spyMgr));
            var functions = new List<string>();
            
            foreach (XmlNode f in functionList)
            {
                if (f.Attributes != null &&
                    (f.Attributes["hook"] == null || f.Attributes["hook"].Value != "false"))
                {
                    string modPart = "";

                    functions.Clear();
                    var fncNode = f["function"];
                    if (fncNode != null)
                    {
                        var fncName = fncNode.InnerText;
                        int index = fncName.IndexOf("!", StringComparison.Ordinal);
                        if (index != -1)
                        {
                            modPart = fncName.Substring(0, index + 1);
                            fncName = fncName.Substring(index + 1);
                        }

                        NktDbObject fnc = fncEnum.GetByName(fncName);
                        if (fnc != null)
                        {
                            functions.Add(modPart + fncName);
                        }
                        else if (f.Attributes["force"] != null &&
                                 f.Attributes["force"].Value.ToLower() == "true")
                        {
                            functions.Add(modPart + fncName);
                        }
                        fnc = fncEnum.GetByName(fncName + "A");
                        if (fnc != null)
                        {
                            functions.Add(modPart + fncName + "A");
                        }
                        fnc = fncEnum.GetByName(fncName + "W");
                        if (fnc != null)
                        {
                            functions.Add(modPart + fncName + "W");
                        }
                    }

                    foreach (var functionName in functions)
                    {
                        IntPtr hookId = AddHook(f, functionName);
                    }
                }
            }
            _paramHandlerMgr.ParseContexts(otherHooks);
        }

        class HooksInitializationState
        {
            public bool CreateProcessInternalWCreated;
            public bool UnhandledExceptionFilterCreated;
            public bool RegOpenKeyExWCreated;
            public bool RegCloseKeyCreated;
            public bool RegQueryValueExWCreated;
        }

        void InitializeHooks()
        {
            var pid = Process.GetCurrentProcess().Id;
            var proc = _spyMgr.ProcessFromPID(pid);
            if (proc != null)
            {
                var wininet = Declarations.LoadLibrary("WinInet.dll");
                //var qt5 = Declarations.LoadLibrary("Qt5Core.dll");
                //var qt5Widgets = Declarations.LoadLibrary("Qt5Widgets.dll");

                try
                {
                    Action<NktModulesEnum, HooksInitializationState>[] initializers =
                    {
                        InitializeOle32Hooks,
                        InitializeNtDllHooks,
                        InitializeUser32Hooks,
                        InitializeKernelBaseHooks,
                        InitializeKernel32Hooks,
                        InitializeFindResourceExHook,
                        InitializeAdvApiHooks,
                        InitializeWinInetHooks,
                        //InitializeQtHooks,
                        InitializeOtherHooks,
                    };

                    var mods = proc.Modules();
                    var state = new HooksInitializationState();
                    initializers.ForEach(x => x(mods, state));
                }
                finally
                {
                    Declarations.FreeLibrary(wininet);
                    //Declarations.FreeLibrary(qt5);
                    //Declarations.FreeLibrary(qt5Widgets);
                }
            }

            var defActiveGroups = new HashSet<string>();
            Settings.Default.ActiveHookGroups
                .Split(',')
                .ForEach(x => defActiveGroups.Add(x));
            
            ActiveGroups = defActiveGroups;
        }
        //public void SetTrace(DeviareRunTrace devRunTrace)
        //{
        //    _devRunTrace = devRunTrace;
        //}
        private NktHook AddHook(NktExportedFunction fnc, int props, string hookGroup)
        {
            NktHook hook = _spyMgr.CreateHook(fnc, props);
            lock (_hookIdMap)
            {
                _hookIdMap[hook.Id] = hook;
            }
            _hookGroupMgr.AddHookToGroup(hook.Id, hookGroup);

            return hook;
        }

        public IntPtr AddHook(XmlNode h, string functionName)
        {
            var hook = new DeviareHook(h, functionName, _spyMgr, _paramHandlerMgr) { ModulePath = _modulePath, ProcessInfo = _processInfo, WindowClassNames = _windowClassNames };

            bool async = AsyncMode();
            int flags = GetBaseHookFlags(functionName);
            if (async)
                flags |= (int)eNktHookFlags.flgAsyncCallbacks;
            var hookId = hook.CreateDeviareObject(flags, out flags);
            lock (_hookMap)
            {
                _hookMap[hookId] = hook;
            }
            _hookGroupMgr.AddHookToGroup(hookId, hook.Group);
            lock (_hookIdMap)
            {
                _hookIdMap[hookId] = hook.HookObject;
            }
            var props = new HookProperties(HookType.Custom, 0, functionName, flags, false);
            lock (HookIdProps)
            {
                HookIdProps[hookId] = props;
            }

            if (async)
                AsyncHookMgr.CreateCustomHook(hook.HookObject, functionName, props);

            //Debug.WriteLine("New CustomHook added: " + functionName);
            if (async)
            {
                var agentInitializationParameters = AsyncHookMgr.SetUpAgentParameters(h, flags);
                hook.HookObject.AddCustomHandler(GetPluginDllString(), 0, agentInitializationParameters);
            }
            return hookId;
        }

        // Add a hook that will be available only for this process and this run.
        public IntPtr AddVolatileHook(DeviareHook hook, NktProcess proc, string modPath)
        {
            IntPtr hookId;

            bool async = AsyncMode();
            int flags = GetBaseHookFlags(hook.FunctionName);
            if (async)
                flags |= (int) eNktHookFlags.flgAsyncCallbacks;
            if (hook.HookObject == null)
                hookId = hook.CreateDeviareObject(flags);
            else
                hookId = hook.HookObject.Id;
            lock (_volatileHooks)
            {
                _volatileHooks.Add(hookId);
            }
            var props = new HookProperties(HookType.GetClassObject, 0, hook.FunctionName, flags, false);
            lock (HookIdProps)
            {
                HookIdProps[hookId] = props;
            }
            lock (_hookMap)
            {
                _hookMap[hookId] = hook;
            }
            lock (_hookIdMap)
            {
                _hookIdMap[hookId] = hook.HookObject;
            }
            _hookStateMgr.AddVolatileHook(hook.HookObject, (uint) proc.Id);
            if (hook.FunctionName.EndsWith("!DllGetClassObject"))
                AsyncHookMgr.CreateVolatileDllGetClassObjectHook(hookId, modPath, props);
            if (async)
            {
                var agentInitializationParameters = AsyncHookMgr.SetUpAgentParameters(flags);
                hook.HookObject.AddCustomHandler(GetPluginDllString(), 0, agentInitializationParameters);
            }
            
            return hookId;
        }

        // Add a hook that will be available only for this process and this run.
        public IntPtr AddVolatileHook(NktProcess proc, IntPtr function, string functionName, HookType type, bool activateImmediately)
        {
            var hook = new DeviareHook(SpyMgr);

            bool async = AsyncMode();
            int flags = GetBaseHookFlags(functionName);
            if (async)
                flags |= (int)eNktHookFlags.flgAsyncCallbacks;

            hook.OnlyAfter = true;
            var hookId = hook.CreateDeviareObject(function, functionName, flags, out flags);

            lock (_volatileHooks)
            {
                _volatileHooks.Add(hookId);
            }
            var props = new HookProperties(type, 0, functionName, flags, false);
            lock (HookIdProps)
            {
                HookIdProps[hookId] = props;
            }
            lock (_hookMap)
            {
                _hookMap[hookId] = hook;
            }
            lock (_hookIdMap)
            {
                _hookIdMap[hookId] = hook.HookObject;
            }
            _hookStateMgr.AddVolatileHook(hook.HookObject, (uint)proc.Id);
            if (async)
            {
                var agentInitializationParameters = AsyncHookMgr.SetUpAgentParameters(flags);
                hook.HookObject.AddCustomHandler(GetPluginDllString(), 0, agentInitializationParameters);
            }

            if (activateImmediately)
                ActivateHook(hookId, hook, proc, true);

            return hookId;
        }

        public NktHook FindHookObject(IntPtr hook)
        {
            lock (_hookIdMap)
            {
                NktHook ret;
                if (!_hookIdMap.TryGetValue(hook, out ret))
                    return null;
                return ret;
            }
        }
        
        public void ActivateHook(IntPtr hookId, DeviareHook hook, NktProcess proc, bool attach)
        {
            if (!IsMonitoring)
                return;

            lock (_activeHooks)
            {
                _activeHooks.Add(hookId);
            }
            if (attach)
            {
                hook.HookObject.Attach(proc, true);
            }
            hook.HookObject.Hook(true);
        }

        public void DeactivateHook(IntPtr hookId, NktProcess proc)
        {
            if (!IsMonitoring || !_activeHooks.Contains(hookId))
                return;

            var hook = _hookIdMap[hookId];
            hook.Unhook(true);
            hook.Detach(proc, true);

            lock (_activeHooks)
            {
                _activeHooks.Remove(hookId);
            }
        }

        public void AddSecondaryVolatileHook(IntPtr addr, NktProcess proc, string functionName, HookType hookType, int tag, int flags, IntPtr primaryHookId)
        {
            bool async = AsyncMode();
            if (async)
                flags |= (int)eNktHookFlags.flgAsyncCallbacks;
            flags |= (int)eNktHookFlags.flgDontCheckAddress;
            flags |= (int)eNktHookFlags.flgDontSkipJumps;
            NktHook hook = _spyMgr.CreateHookForAddress(addr, functionName, flags);
            lock (_volatileHooks)
            {
                _volatileHooks.Add(hook.Id);
            }
            var props = new HookProperties(hookType, tag, hook.FunctionName, flags, true);
            lock (HookIdProps)
            {
                HookIdProps[hook.Id] = props;
            }
            lock (_hookIdMap)
            {
                _hookIdMap[hook.Id] = hook;
            }
            _hookStateMgr.AddVolatileHook(hook, (uint)proc.Id);
            AsyncHookMgr.CreateVolatileSecondaryHook(hook, props);
            if (async)
            {
                var agentInitializationParameters = AsyncHookMgr.SetUpAgentParameters(primaryHookId, flags);
                hook.AddCustomHandler(GetPluginDllString(), 0, agentInitializationParameters);
            }
            if (IsMonitoring)
            {
                lock (_activeHooks)
                {
                    _activeHooks.Add(hook.Id);
                }
                hook.Attach(proc, true);
                hook.Hook(true);
            }
        }

        public void HookLoadedModulesComServers(NktProcess proc)
        {
            lock (_processModuleHookProcessed)
            {
                if (!_processModuleHookProcessed.Contains((uint)proc.Id))
                {
                    _processModuleHookProcessed.Add((uint)proc.Id);

                    try
                    {
                        if (proc.PlatformBits > 0)
                        {
                            var mods = proc.Modules();
                            if (mods != null)
                            {
                                NktHooksEnum hooks = _spyMgr.CreateHooksCollection();
                                var mod = mods.First();
                                while (mod != null)
                                {
                                    if (mod.PlatformBits > 0)
                                    {
                                        _modulePath.AddModule(mod.Path, (uint) proc.Id, (UInt64) mod.BaseAddress);
                                        NktHook h = AddComHooks(mod, proc, mod.Path, mod.BaseAddress, false);
                                        if (h != null)
                                        {
                                            hooks.Add(h);
                                        }
                                    }

                                    mod = mods.Next();
                                }
                                if (hooks.Count > 0)
                                {
                                    hooks.Attach(proc.Id, true);
                                }
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
        }
        public NktHook AddComHooks(NktModule mod, NktProcess proc, string dllName, IntPtr addr, bool attach)
        {
            NktHook devHook = null;
            bool needActivation = false;
            var dllLower = dllName.ToLower();
            IntPtr hookId;
            DllGetClassObjectHook hook = null;
            lock (_dllGetClassObjectHookIds)
            {
                if (!_dllGetClassObjectHookIds.TryGetValue(dllLower, out hookId))
                {
                    // if the dll is in this Dictionary -> the hook was attached with the rest of the hooks that are in the hooks array
                    if (mod == null)
                    {
                        mod = proc.Modules().GetByAddress(addr, eNktSearchMode.smGetNearest);
                    }
                    if (mod != null)
                    {
                        hookId = IntPtr.Zero;

                        // is a Com server?
                        var function = mod.FunctionByName("DllGetClassObject");
                        var modName = mod.Name;
                        if (function != null)
                        {
                            hook = new DllGetClassObjectHook(_spyMgr, modName, mod.Path, function);
                            hookId = AddVolatileHook(hook, proc, mod.Path);
                            devHook = hook.HookObject;
                            // activate outside the lock
                            needActivation = true;
                        }
                        _dllGetClassObjectHookIds.Add(dllLower, hookId);
                    }
                    else if (hookId != IntPtr.Zero)
                    {
                        var h = _spyMgr.Hooks().GetById(hookId);
                        if (h != null)
                        {
                            if (h.State(proc) != eNktHookState.stActive)
                            {
                                devHook = h;
                            }
                        }
                    }
                }
            }
            if(needActivation)
            {
                ActivateHook(hookId, hook, proc, attach);
            }
            return devHook;
        }
        public List<AsyncAction> Attach(uint pid, bool background)
        {
            return Attach(pid, background, new KeyValuePair<object, NktProcess>());
        }
        public List<AsyncAction> Attach(uint pid, bool background, KeyValuePair<object, NktProcess> resumeProcessEvent)
        {
            NktProcess proc = _spyMgr.ProcessFromPID((int)pid);
            if (proc != null && IsHookeable(proc))
                return Attach(proc, background, resumeProcessEvent);
            return null;
        }
        public List<AsyncAction> Attach(NktProcess proc, bool background)
        {
            return Attach(proc, background, new KeyValuePair<object, NktProcess>());
        }
        public List<AsyncAction> Attach(NktProcess proc, bool background, KeyValuePair<object, NktProcess> resumeProcessEvent)
        {
            var ret = new List<AsyncAction>();
            lock (_attachedPids)
            {
                if (!_attachedPids.Contains(proc.Id) && !_blackList.Contains(proc.Name.ToLower()))
                {
                    Start();

                    _attachedPids.Add(proc.Id);

                    var hooks = _spyMgr.Hooks();
                    var hooksToAttach = _spyMgr.CreateHooksCollection();

                    lock (_activeHooks)
                    {
                        foreach (var hookId in _activeHooks)
                            hooksToAttach.Add(hooks.GetById(hookId));
                    }
                    if(!background)
                    {
                        hooksToAttach.Attach(proc, true);
                        //because we only add secondary hooks to children of the main executable, in this
                        //case background will be always false
#if !TURN_THINAPP_OFF
                        AttachSecondaryHooks(proc);
#endif
                    }
                    else
                    {
                        var action = new AttachAction { Hooks = hooksToAttach, Proc = proc };
                        ret.Add(action);
                        _delayedAction.QueueAction(action);
                    }
                    if (resumeProcessEvent.Key != null)
                    {
                        if (!background)
                        {
                            _spyMgr.ResumeProcess(resumeProcessEvent.Value, resumeProcessEvent.Key);
                        }
                        else
                        {
                            // HACK: perform a background ResumeProcess using the same AttachDetachAction to be sure that it will be done after
                            // the attachs
                            var action = new ResumeProcessAction { ResumeProcessEvent = resumeProcessEvent };
                            ret.Add(action);
                            _delayedAction.QueueAction(action);
                        }
                    }
                    // intercept Com functions of loaded modules
                    if(_hookGroupMgr.IsActiveXActive())
                    {
                        var processAction = new HookLoadedModulesComServersAction { Proc = proc };
                        ret.Add(processAction);
                        _delayedAction.QueueAction(processAction);
                    }
                }
            }
            return ret;
        }
        public void Detach(uint pid, bool background)
        {
            NktProcess proc = _spyMgr.ProcessFromPID((int)pid);
            if (proc != null)
                Detach(proc, background);
        }
        public void Detach(NktProcess proc, bool background)
        {
            lock (_attachedPids)
            {
                if (_attachedPids.Contains(proc.Id))
                {
                    NktHooksEnum hooks = _spyMgr.Hooks();
                    if(!background)
                    {
                        hooks.Detach(proc, true);
                    }
                    else
                    {
                        var action = new DetachAction { Hooks = hooks, Proc = proc };
                        _delayedAction.QueueAction(action);
                    }
                }
            }
        }

        public void Start()
        {
            if (IsMonitoring)
                return;

            IsMonitoring = true;
            lock(_activeHooks)
            {
                var hookIds = _hookGroupMgr.GetActiveHooks();
                NktHooksEnum hooks = _spyMgr.Hooks();
                foreach (var hookId in hookIds)
                {
                    var hook = hooks.GetById(hookId);
                    hook.Hook(true);
                    _activeHooks.Add(hookId);
                }
            }
        }

        public HashSet<string> Groups
        {
            get { return _hookGroupMgr.Groups; }
        }

        public HashSet<string> ActiveGroups
        {
            set { _hookGroupMgr.ActiveGroups = value; }
        }

        public bool GetHookProperties(IntPtr hookId, out HookType hookType, out int tag, out string functionName, out string displayName, out int flags)
        {
            return GetHookProperties(hookId, out hookType, out tag, out functionName, out displayName, out flags, false);
        }

        public HookProperties HookPropertiesFromFunctionName(string functionName)
        {
            return HookIdProps.Values.FirstOrDefault(hookProperties => hookProperties.FunctionName == functionName);
        }

        public bool GetHookProperties(IntPtr hookId, out HookType hookType, out int tag, out string functionName, out string displayName, out int flags, bool isSimulated)
        {
            HookProperties hookProps;
            bool entryExists;

            lock (HookIdProps)
            {
                if (!isSimulated)
                    entryExists = HookIdProps.TryGetValue(hookId, out hookProps);
                else
                {
                    hookProps = AsyncHookMgr.HookPropertiesFromSimulatedHookId(hookId);
                    entryExists = hookProps != null;
                }
            }
            if (entryExists)
            {
                hookType = hookProps.HookType;
                tag = hookProps.Tag;
                functionName = hookProps.FunctionName;
                displayName = hookProps.DisplayName;
                flags = hookProps.Flags;
            }
            else
            {
                hookType = HookType.Custom;
                displayName = functionName = "";
                tag = 0;
                flags = 0;
            }
            return entryExists;
        }

        public bool ProcessEvent(bool before, IntPtr hookId, NktHookCallInfo callInfo, CallEvent callEvent, NktProcess proc)
        {
            DeviareHook deviareHook;

            lock (_hookMap)
            {
                deviareHook = _hookMap[hookId];
            }
            if (deviareHook.OnlyBefore || deviareHook.StackBefore)
            {
                DeviareTools.SetStackInfo(callEvent, callInfo, _modulePath);
            }
            return deviareHook.ProcessEvent(before, callInfo, callEvent, proc);
        }

#if TURN_THINAPP_OFF
        public void CreateSecondaryHooks(NktProcess parentProc, int pid)
        {
        }
#else
        public bool CreateSecondaryHooks(NktProcess parentProc, int pid)
        {
            NktProcess proc;
            Nektra.DeviareLite.NktHookInfo[] hookInfo;
            Dictionary<string, IntPtr> tempDic;
            NktProcessMemory procMem;
            INktExportedFunction func;
            NktModule mod;
            GCHandle pinnedBuffer;
            IntPtr tmpAddr, tmpLen;
            int i;
            bool b;
            uint temp32;
            ulong temp64;

            //async?
            if (AsyncMode() == false)
                return true;
            //parent is thinapp?
            if (parentProc.ModuleByName("nt0_dll.dll") == null && parentProc.ModuleByName("nt0_dll64.dll") == null)
                return true;
            try
            {
                proc = _spyMgr.ProcessFromPID(pid);
            }
            catch (System.Exception)
            {
                proc = null;
            }
            if (proc == null)
                return false;
            //create common trampoline
            byte[] buffer = new byte[64];
            IntPtr bufferLen = new IntPtr(buffer.Length);
            for (i=0; i<32; i++)
                buffer[i] = 0x90;
            switch (proc.PlatformBits)
            {
                case 32:
                    buffer[32] = 0xE9;
                    buffer[33] = buffer[34] = buffer[35] = buffer[36] = 0x0;
                    break;
                case 64:
                    buffer[32] = 0x48;
                    buffer[33] = 0xFF;
                    buffer[34] = 0x25;
                    buffer[35] = buffer[36] = buffer[37] = buffer[38] = 0x0;
                    break;
                default:
                    return false;
            }
            //init holders and stuff
            hookInfo = new Nektra.DeviareLite.NktHookInfo[szSecondaryHook_NtDll_Apis.Length];
            tempDic = new Dictionary<string, IntPtr>();
            procMem = proc.Memory();
            mod = proc.ModuleByName("ntdll.dll");
            //create trampoline for each hook
            for (i=0; i<szSecondaryHook_NtDll_Apis.Length; i++)
            {
                func = mod.FunctionByName(szSecondaryHook_NtDll_Apis[i]);
                hookInfo[i] = new Nektra.DeviareLite.NktHookInfo();
                hookInfo[i].OrigProcAddr = func.Addr;
                hookInfo[i].NewProcAddr = procMem.AllocMem(bufferLen, true);
                tempDic.Add("ntdll.dll!" + szSecondaryHook_NtDll_Apis[i], hookInfo[i].NewProcAddr);
                pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    b = (procMem.WriteMem(hookInfo[i].NewProcAddr, pinnedBuffer.AddrOfPinnedObject(), bufferLen) == bufferLen);
                }
                catch (System.Exception)
                {
                    b = false;
                }
                pinnedBuffer.Free();
                if (b == false)
                    return false;
            }
            //install secondary hooks
            try
            {
                _miniHookLib.RemoteHook(hookInfo, proc.Id, (int)Nektra.DeviareLite.eNktHookFlags.hfDontRemoveOnUnhook);
            }
            catch (System.Exception)
            {
                return false;
            }
            //end setting up trampolines
            for (i = 0; i < szSecondaryHook_NtDll_Apis.Length; i++)
            {
                switch (proc.PlatformBits)
                {
                    case 32:
                        temp32 = (uint)(hookInfo[i].CallOriginalAddr.ToInt32());
                        temp32 = temp32 - ((uint)(hookInfo[i].NewProcAddr.ToInt32()) + 37);
                        pinnedBuffer = GCHandle.Alloc(temp32, GCHandleType.Pinned);
                        tmpAddr = new IntPtr(hookInfo[i].NewProcAddr.ToInt32() + 33);
                        tmpLen = new IntPtr(4);
                        try
                        {
                            b = (procMem.WriteMem(tmpAddr, pinnedBuffer.AddrOfPinnedObject(), tmpLen) == tmpLen);
                        }
                        catch (System.Exception)
                        {
                            b = false;
                        }
                        pinnedBuffer.Free();
                        if (b == false)
                            return false;
                        break;

                    case 64:
                        temp64 = (ulong)(hookInfo[i].CallOriginalAddr.ToInt64());
                        pinnedBuffer = GCHandle.Alloc(temp64, GCHandleType.Pinned);
                        tmpAddr = new IntPtr(hookInfo[i].NewProcAddr.ToInt64() + 39);
                        tmpLen = new IntPtr(8);
                        try
                        {
                            b = (procMem.WriteMem(tmpAddr, pinnedBuffer.AddrOfPinnedObject(), tmpLen) == tmpLen);
                        }
                        catch (System.Exception)
                        {
                            b = false;
                        }
                        pinnedBuffer.Free();
                        if (b == false)
                            return false;
                        break;
                }
            }
            lock (_secondaryHooks)
            {
                _secondaryHooks.Add(proc.Id, tempDic);
            }
            return true;
        }

        private void RemoveSecondaryHooks(int pid)
        {
            lock (_secondaryHooks)
            {
                _secondaryHooks.Remove(pid);
            }
        }

        private void AttachSecondaryHooks(NktProcess proc)
        {
            if (!AsyncMode())
                return;

            Dictionary<string, IntPtr> secondaryHookDict;
            lock (_secondaryHooks)
            {
                if (!_secondaryHooks.TryGetValue(proc.Id, out secondaryHookDict))
                    return;
            }

            foreach (var pair in secondaryHookDict)
            {
                IntPtr primaryHookId = IntPtr.Zero;
                lock (_strToHookIdMap)
                {
                    if (_strToHookIdMap.ContainsKey(pair.Key))
                        primaryHookId = _strToHookIdMap[pair.Key];
                }
                Debug.Assert(primaryHookId != IntPtr.Zero);
                if (primaryHookId != IntPtr.Zero)
                {
                    //add a volatile secondary hook at address pair.Value+10h
                    //NktHook h;
                    HookType hookType;
                    int tag, flags;
                    string functionName, displayName;

                    var h = _spyMgr.Hooks().GetById(primaryHookId);
                    Debug.Assert(h != null);
                    flags = h.Flags & ((int)eNktHookFlags.flgOnlyPreCall | (int)eNktHookFlags.flgOnlyPostCall);
                    GetHookProperties(primaryHookId, out hookType, out tag, out functionName, out displayName);
                    AddSecondaryVolatileHook(new IntPtr(pair.Value.ToInt64() + 16), proc, functionName, hookType, tag, flags, primaryHookId);
                }
            }
        }
#endif

        #region DeviareHandler
        void Deviare_OnFunctionCalled(NktHook hook, NktProcess proc, NktHookCallInfo callInfo)
        {
            if (FormMain.ApplicationShutdown || !FormMain.CollectingData)
                return;

            Stopwatch sw = null;
            if(_collectTimes)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            IntPtr hookId = hook.Id;
            var pid = (uint)proc.Id;
            var tid = (uint)callInfo.ThreadId;
            ulong retValue = proc.PlatformBits == 32 ? Convert.ToUInt64(callInfo.Result().CastTo("ULONG").ULongVal) : callInfo.Result().CastTo("ULONGLONG").ULongLongVal;
            var time = callInfo.ElapsedTimeMs-callInfo.ChildsElapsedTimeMs;

            ProcessEvent(hookId, callInfo, proc, time, pid, tid, retValue);

            if (_collectTimes && sw != null)
            {
                sw.Stop();
                lock (_times)
                {
                    Int64 timeHandled;
                    if (!_times.TryGetValue(hook.FunctionName, out timeHandled))
                    {
                        timeHandled = 0;
                        _counts[hook.FunctionName] = 0;
                    }
                    else
                        _counts[hook.FunctionName]++;

                    timeHandled += sw.ElapsedTicks;
                    _times[hook.FunctionName] = timeHandled;
                }
            }
            //Error.WriteLine("Elapsed " + sw.Elapsed);
        }
        void Deviare_OnCreateProcessCall(NktProcess proc, int childPid, int mainThreadId, bool is64BitProcess, bool canHookNow)
        {
            if (!IsMonitoring)
                return;
            bool attach = true;
            if (_servicesTryHooked && (proc.Id == _servicesPid || proc.Id == _dcomPid))
            {
                string commandLine = ProcessTools.GetCommandLineFromPid((uint)childPid);
                if (commandLine != null)
                {
                    if (!_msiAllowed || !commandLine.ToLower().Contains("msiexec.exe"))
                    {
                        uint hash = StringHash.MultiplicationHash(commandLine);
                        if (!AllowableNewServices.Contains(hash))
                            attach = false;
                    }
                }
            }
            if (attach)
            {
                Attach((uint)childPid, false, new KeyValuePair<object, NktProcess>());
            }
        }

        void Deviare_OnStateChanged(NktHook hook, NktProcess proc, eNktHookState newState, eNktHookState oldState)
        {
            var pid = (uint)proc.Id;
            var hookId = hook.Id;

            _hookStateMgr.SetHookState(pid, hook, newState);

            lock (_pendingStateChanges)
            {
                if (_connectedListViews.Count > 0)
                    _pendingStateChanges.Add(new HookStateChange(pid, hookId, newState));
            }
            
            if(newState == eNktHookState.stActive)
                ProcessLoadedModules(proc);
        }

        void Deviare_OnHookOverwritten(NktHook hook, NktProcess proc)
        {
            /*
            NktHook h;
            NktModule mod;
            HookType hookType;
            int index, tag;
            string functionName, modName, displayName;

            if (GetHookProperties(hook.Id, out hookType, out tag, out functionName, out displayName) == false)
                return;
            functionName = hook.FunctionName;
            index = functionName.IndexOf("!", StringComparison.Ordinal);
            if (index == -1)
                return;
            modName = functionName.Substring(0, index);
            functionName = functionName.Substring(index + 1);
            mod = proc.ModuleByName(modName);
            if (mod == null)
                return;
            h = CreateHook(mod, functionName, (hook.Flags & (int)eNktHookFlags.flgOnlyPostCall) == 0,
                           (hook.Flags & (int)eNktHookFlags.flgOnlyPreCall) == 0,
                            "Rehook", //temporary name
                            hookType, tag);
            if (h == null)
                return;
            h.Attach(proc, true);
            h.Hook(true);
            */
        }

        void UpdateIsMonitoring()
        {
            bool stopAnalysis = false;
            lock(_attachedPids)
            {
                if (!_delayedAction.AnyPendingAction() && !_hookNewUserProcesses)
                {
                    var attachedPids = new int[_attachedPids.Count];
                    _attachedPids.CopyTo(attachedPids);

                    int attachedCount = attachedPids.Length;

                    if(_servicesTryHooked)
                    {
                        if (attachedPids.Contains(_dcomPid))
                            attachedCount--;
                        if (attachedPids.Contains(_servicesPid))
                            attachedCount--;
                    }

                    if (attachedCount == 0)
                        stopAnalysis = true;
                }
                //if (_attachedPids.Count == 0 && !_delayedAction.AnyPendingAction() && !_hookNewUserProcesses)
                //    stopAnalysis = true;
            }
            if (stopAnalysis)
            {
                IsMonitoring = false;
                if (_servicesTryHooked)
                {
                    if (_attachedPids.Contains(_dcomPid))
                        Detach((uint) _dcomPid, true);
                    if (_attachedPids.Contains(_servicesPid))
                        Detach((uint) _servicesPid, true);
                    _servicesTryHooked = false;
                }

                InstallerMode = false;
                ClearObjects();
            }
        }
        void RemoveAttachedPid(int pid, bool testToStopAnalysis)
        {
            bool updateMonitoring = false;
            lock (_attachedPids)
            {
                if(_attachedPids.Remove(pid))
                {
                    updateMonitoring = true;
                }
            }
            if (updateMonitoring)
            {
                UpdateIsMonitoring();
            }
        }
        public void ProcessEvent(IntPtr hookId, NktHookCallInfo callInfo, NktProcess proc, double time, uint pid, uint tid, UInt64 retValue)
        {
            HookType hookType;
            int tag;
            string functionName, displayName;
            int flags;

            GetHookProperties(hookId, out hookType, out tag, out functionName, out displayName, out flags);

            //Stopwatch sw = new Stopwatch();
            //Error.WriteLine("ProcessEvent: " + hookType.ToString() + " Before: " + ((callInfo.HookFlags & (int)DeviareCommonLib.NktHookFlags._call_before) != 0 ? "true" : "false"));
            //sw.Start();
            //if(string.IsNullOrEmpty(functionName))
            //{
            //    Console.WriteLine(callInfo.Hook().FunctionName);
            //}

            var e = new CallEvent(true)
                        {
                            Before = true,
                            Win32Function = functionName,
                            Function = displayName,
                            Cookie = (uint) callInfo.Cookie,
                            Type = hookType,
                            Pid = pid,
                            Tid = tid,
                            Time = time,
                            GenerationTime = callInfo.CurrentTimeMs,
                            RetValue = retValue
                        };
            
            if (callInfo.IsPreCall)
            {
                e.Before = true;

                switch (hookType)
                {
                    case HookType.RegOpenKey:
                        ProcessRegOpenKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegCreateKey:
                        ProcessRegCreateKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegQueryValue:
                        ProcessRegQueryValueBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegQueryMultipleValues:
                        ProcessRegQueryMultipleValuesBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegQueryKey:
                        ProcessRegQueryKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegEnumerateValueKey:
                        ProcessRegEnumerateValueKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegEnumerateKey:
                        ProcessRegEnumerateKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegRenameKey:
                        ProcessRegRenameKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegSetValue:
                        ProcessRegSetValueBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegDeleteValue:
                        ProcessRegDeleteValueBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegDeleteKey:
                        ProcessRegDeleteKeyBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.LoadLibrary:
                        ProcessLoadLibraryBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.FindResource:
                        ProcessFindResourceBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.LoadResource:
                        ProcessLoadResourceBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.OpenFile:
                    case HookType.CreateFile:
                        ProcessCreateFileBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.DeleteFile:
                        ProcessDeleteFileBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.QueryDirectoryFile:
                        ProcessQueryDirectoryFileBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.QueryAttributesFile:
                        ProcessQueryAttributesFileBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateDirectory:
                        ProcessCreateDirectoryBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CoCreate:
                        ProcessCoCreateBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateDialog:
                        ProcessCreateDialogBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateWindow:
                        ProcessCreateWindowBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateProcess:
                        ProcessCreateProcessBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RaiseException:
                        ProcessRaiseExceptionBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateService:
                        ProcessCreateServiceBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.OpenService:
                        ProcessOpenServiceBefore(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.GetClassObject:
                    case HookType.ReadFile:
                    case HookType.WriteFile:
                    case HookType.Custom:
                        ProcessCustomBefore(e, hookId, callInfo, proc, hookType, tag);
                        break;
                    case HookType.DotNetProfiler:
                        break;
                }
            }
            else
            {
                e.Before = false;

                if (_startTime.Equals(0))
                {
                    _startTime = callInfo.CurrentTimeMs;
                    _devRunTrace.StartTime = _startTime;
                    //_startTime = (double)DateTime.Now.Ticks / 10000;
                }
                e.GenerationTime = callInfo.CurrentTimeMs - _startTime;

                switch (hookType)
                {
                    case HookType.RegOpenKey:
                        ProcessRegOpenKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegCreateKey:
                        ProcessRegCreateKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegQueryValue:
                        ProcessRegQueryValue(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegQueryMultipleValues:
                        ProcessRegQueryMultipleValues(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegQueryKey:
                        ProcessRegQueryKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegEnumerateValueKey:
                        ProcessRegEnumerateValueKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegEnumerateKey:
                        ProcessRegEnumerateKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegRenameKey:
                        ProcessRegRenameKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegSetValue:
                        ProcessRegSetValue(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegDeleteValue:
                        ProcessRegDeleteValue(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.RegDeleteKey:
                        ProcessRegDeleteKey(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.LoadLibrary:
                        ProcessLoadLibrary(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.FindResource:
                        ProcessFindResource(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.LoadResource:
                        ProcessLoadResource(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.OpenFile:
                    case HookType.CreateFile:
                        ProcessCreateFile(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.DeleteFile:
                        ProcessDeleteFile(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.QueryDirectoryFile:
                        ProcessQueryDirectoryFile(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.QueryAttributesFile:
                        ProcessQueryAttributesFile(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateDirectory:
                        ProcessCreateDirectory(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CoCreate:
                        ProcessCoCreate(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateDialog:
                        ProcessCreateDialog(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateWindow:
                        ProcessCreateWindow(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateProcess:
                        ProcessCreateProcess(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.CreateService:
                        ProcessCreateService(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.OpenService:
                        ProcessOpenService(e, callInfo, proc, hookType, tag);
                        break;
                    case HookType.GetClassObject:
                    case HookType.Custom:
                    case HookType.ReadFile:
                    case HookType.WriteFile:
                        ProcessCustom(e, hookId, callInfo, proc, hookType, tag);
                        break;
                    case HookType.DotNetProfiler:
                        break;
                }
            }
            //sw.StopAnalysis();

            //Error.WriteLine("Elapsed " + sw.Elapsed);
        }

        RegistryValueKind GetValueType(NktParam paramType, bool sizeAndTypeArePtr)
        {
            var valueType = (uint)RegistryValueKind.Unknown;
            if (sizeAndTypeArePtr)
            {
                if (paramType.IsNullPointer == false)
                {
                    valueType = paramType.Evaluate().ULongVal;
                }
            }
            else
            {
                valueType = paramType.ULongVal;
            }
            RegistryValueKind vType;
            if (Enum.IsDefined(typeof(RegistryValueKind), (int)valueType))
                vType = (RegistryValueKind)valueType;
            else
                vType = RegistryValueKind.Unknown;
            return vType;
        }
                        //valueData = GetValueData(pid, pms.GetAt(3), fields.GetAt(5), fields.GetAt(3), fields.GetAt(2), fields.GetAt(4), out valueType);

        public string GetValueData(uint pid, NktParam paramType, NktParam paramData, NktParam paramSize, NktParam offset, NktParam structStart, out RegistryValueKind vType)
        {
            vType = GetValueType(paramType, false);
            string valueData = null;

            // no data!
            if (paramData != null && paramSize != null && !paramData.IsNullPointer)
            {
                uint dataSize = paramSize.ULongVal;

                if(dataSize != 0)
                {
                    IntPtr memAddress;
                    if (offset != null)
                    {
                        Int64 offsetValue = offset.ULongVal;
                        memAddress = new IntPtr(structStart.Evaluate().Address.ToInt64() + offsetValue);
                    }
                    else
                    {
                        memAddress = paramData.Address;
                    }
                    if (vType != RegistryValueKind.Unknown)
                    {
                        NktProcessMemory procMem = _spyMgr.ProcessMemoryFromPID((int)pid);
                        switch (vType)
                        {
                            case RegistryValueKind.Binary:
                                {
                                    var buffer = new byte[dataSize];

                                    GCHandle pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                                    IntPtr pDest = pinnedBuffer.AddrOfPinnedObject();
                                    Int64 bytesReaded = procMem.ReadMem(pDest, memAddress, (IntPtr)dataSize).ToInt64();
                                    pinnedBuffer.Free();

                                    valueData = "";
                                    for (int i = 0; i < bytesReaded; i++)
                                    {
                                        if (i != 0)
                                            valueData += " ";
                                        valueData += Convert.ToByte(buffer[i]).ToString("X2");
                                    }
                                }
                                break;

                            case RegistryValueKind.MultiString:
                                {
                                    var buffer = new short[dataSize / 2];

                                    GCHandle pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                                    IntPtr pDest = pinnedBuffer.AddrOfPinnedObject();
                                    Int64 bytesReaded = procMem.ReadMem(pDest, memAddress, (IntPtr)dataSize).ToInt64();
                                    pinnedBuffer.Free();

                                    valueData = "";
                                    for (int i = 0; i < bytesReaded / 2; i++)
                                    {
                                        valueData += Convert.ToChar(buffer[i]);
                                    }
                                }
                                break;
                            case RegistryValueKind.String:
                            case RegistryValueKind.ExpandString:
                                valueData = procMem.ReadStringN(memAddress, false, (int)dataSize / 2);
                                //valueData = paramData.CastTo("LPWSTR").ReadStringN((int) dataSize/2);
                                break;

                            case RegistryValueKind.DWord:
                                {
                                    uint val = procMem.get_ULongVal(memAddress);
                                    valueData = RegistryTools.GetRegValueRepresentation(val);
                                }
                                //if (paramData.IsNullPointer == false)
                                //{
                                //    uint val = paramData.Evaluate().CastTo("DWORD").ULongVal;
                                //    valueData = RegistryTools.GetRegValueRepresentation(val);
                                //}
                                break;

                            case RegistryValueKind.QWord:
                                {
                                    UInt64 val = procMem.get_ULongLongVal(memAddress);
                                    valueData = RegistryTools.GetRegValueRepresentation(val);
                                }
                                //if (paramData.IsNullPointer == false)
                                //{
                                //    UInt64 val = paramData.Evaluate().CastTo("ULONGLONG").ULongLongVal;
                                //    valueData = RegistryTools.GetRegValueRepresentation(val);
                                //}
                                break;
                        }
                    }
                }
            }

            return valueData;
        }

        /// <summary>
        /// When a caller wasn't loaded using LoadLibrary we generate a dummy event that loads this library. It's useful to show the user
        /// all the files that are actually used by the application even when the application doesn't load them because they are loaded when 
        /// the application starts (no LoadLibrary called).
        /// </summary>
        /// <param name="proc"> </param>
        public void ProcessLoadedModules(NktProcess proc)
        {
            var pid = (uint)proc.Id;
            lock (_processedLoadedModulesProcessIds)
            {
                if (_processedLoadedModulesProcessIds.Contains(pid))
                {
                    return;
                }
                _processedLoadedModulesProcessIds.Add(pid);
            }
            var mods = proc.Modules();
            var eventList = new List<CallEvent>();
            if (mods != null)
            {
                var p = Process.GetProcessById((int)pid);
                var threads = p.Threads;
                var tid = (uint)(threads.Count == 0 ? 0 : threads[0].Id);

                var mod = mods.First();
                while (mod != null)
                {
                    string modPath = mod.Path;

                    if (!modPath.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!modPath.EndsWith("DvAgent.dll") && !modPath.EndsWith("DvAgent64.dll"))
                        {

                            var callerDummyEvent =
                                new CallEvent(HookType.LoadLibrary, 0, 0, "", null, 0, pid,
                                              tid);

                            LoadLibraryEvent.CreateEventParams(callerDummyEvent,
                                                               FileSystemTools.GetCanonicalPathName(pid, modPath,
                                                                                                    _processInfo),
                                                               (UInt64)mod.BaseAddress);
                            callerDummyEvent.IsGenerated = true;
                            eventList.Add(callerDummyEvent);
                        }
                    }
                    else
                    {
                        var callerDummyEvent =
                            new CallEvent(HookType.ProcessStarted, 0, 1, "", null, 0, pid, tid);
                        CreateProcessEvent.CreateEventParams(callerDummyEvent,
                                                           FileSystemTools.GetCanonicalPathName(pid, modPath,
                                                                                                _processInfo),
                                                           "", pid);
                        callerDummyEvent.IsGenerated = true;

                        if (eventList.Count > 0)
                        {
                            // change call numbers so the .exe is before the others and insert it at the beginning of the list.
                            ulong previousCallNumber = callerDummyEvent.CallNumber;
                            callerDummyEvent.CallNumber = 0;
                            foreach (var callEvent in eventList)
                            {
                                if (callerDummyEvent.CallNumber == 0)
                                    callerDummyEvent.CallNumber = callEvent.CallNumber;
                                if (callEvent.CallNumber < previousCallNumber)
                                {
                                    callEvent.CallNumber++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        eventList.Insert(0, callerDummyEvent);
                    }

                    mod = mods.Next();
                }
            }
            _devRunTrace.ProcessListOfNewEvents(eventList);
        }

        void ProcessRegOpenKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(2);
            if (!p.IsNullPointer)
            {
                _devRunTrace.ProcessNewEvent(e);
            }
        }
        void ProcessRegOpenKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            Stopwatch sw = null;
            
            if(_collectTimes)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            if(_collectTimes && sw != null)
            {
                lock (_times)
                {
                    Int64 timeHandled;
                    if (!_times.TryGetValue("OpenKeyStack", out timeHandled))
                    {
                        timeHandled = 0;
                        _counts["OpenKeyStack"] = 0;
                    }
                    else
                        _counts["OpenKeyStack"]++;

                    timeHandled += sw.ElapsedTicks;
                    _times["OpenKeyStack"] = timeHandled;
                }
            }
            //Error.WriteLine("RegistryOpen: Stack: " + (e.RetValue == 0) + " " + e.Tid + " " + sw.Elapsed.ToString());

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(2);
            if(!p.IsNullPointer)
            {
                NktParam objAttr = p.Evaluate();
                NktParamsEnum fields = objAttr.Fields();

                p = fields.GetAt(1).CastTo("HKEY");
                //p = objAttr.Fields().GetAt(1);
                var hKey = RegistryTools.ToUInt64(p.SizeTVal);
                p = fields.GetAt(2);
                string subKey;

                //Error.WriteLine("RegistryOpen: 0: " + e.Tid + " " + sw.Elapsed.ToString());
                if (e.RetValue == 0)
                {
                    p = pms.GetAt(0);
                    var retKey = RegistryTools.ToUInt64(p.Evaluate().SizeTVal);
                    //subKey = RegistryTools.GetFullKey(hKey, subKey, pid, tid);
                    //if (subKey.ToUpper().Contains("HKEY_CURRENT_USER_CLASSES"))
                    //{
                    //    Console.WriteLine("");
                    //}
                    subKey = RegistryTools.GetFullKey(retKey, "", e.Pid, e.Tid);
                }
                else
                {
                    subKey = NativeApiTools.GetUnicodeString(p);
                    subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
                }
                //Error.WriteLine("RegistryOpen: 1: " + e.Tid + " " + sw.Elapsed.ToString());

                RegOpenKeyEvent.CreateEventParams(e, subKey);

                _devRunTrace.ProcessNewEvent(e);
                //Error.WriteLine("RegistryOpen: 2: " + e.Tid + " " + sw.Elapsed.ToString());
            }
        }

        void ProcessRegCreateKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(2);
            if (!p.IsNullPointer)
            {
                _devRunTrace.ProcessNewEvent(e);
            }
        }
        void ProcessRegCreateKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(2);
            if (!p.IsNullPointer)
            {
                NktParam objAttr = p.Evaluate();
                NktParamsEnum fields = objAttr.Fields();
                p = fields.GetAt(1).CastTo("HKEY");
                //p = objAttr.Fields().GetAt(1);
                var hKey = RegistryTools.ToUInt64(p.SizeTVal);
                p = fields.GetAt(2);
                string subKey;

                if (e.RetValue == 0)
                {
                    p = pms.GetAt(0);
                    var retKey = RegistryTools.ToUInt64(p.Evaluate().SizeTVal);
                    subKey = RegistryTools.GetFullKey(retKey, "", e.Pid, e.Tid);
                }
                else
                {
                    subKey = NativeApiTools.GetUnicodeString(p);
                    subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
                }

                RegOpenKeyEvent.CreateEventParams(e, subKey);

                _devRunTrace.ProcessNewEvent(e);
            }
        }

        public void GetValueInfo(NktParam valueParam, NktParam infoClassParam, NktParam valueInfoParam, uint pid, out string valueName,
            out string valueData, out RegistryValueKind valueType)
        {
            // VALUE NAME
            if (valueParam != null)
            {
                valueName = NativeApiTools.GetUnicodeString(valueParam);
            }
            else
            {
                valueName = null;
            }
            valueData = null;

            // KEY_VALUE_INFORMATION_CLASS 
            int infoType = infoClassParam.LongVal;
            valueType = RegistryValueKind.Unknown;

            if (valueInfoParam != null && !valueInfoParam.IsNullPointer)
            {
                NktParamsEnum fields = null;
                int nameIndex = -1, nameLengthIndex = -1;

                switch (infoType)
                {
                    case NativeApiTools.KeyValueBasicInformation:
                        fields = valueInfoParam.CastTo("PKEY_VALUE_BASIC_INFORMATION").Evaluate().Fields();
                        valueData = GetValueData(pid, fields.GetAt(1), null, null, null, null, out valueType);
                        nameLengthIndex = 2;
                        nameIndex = 3;
                        break;
                    case NativeApiTools.KeyValuePartialInformation:
                        fields = valueInfoParam.CastTo("PKEY_VALUE_PARTIAL_INFORMATION").Evaluate().Fields();
                        valueData = GetValueData(pid, fields.GetAt(1), fields.GetAt(3), fields.GetAt(2), null, null,
                                                 out valueType);
                        break;
                    case NativeApiTools.KeyValueFullInformation:
                        fields = valueInfoParam.CastTo("PKEY_VALUE_FULL_INFORMATION").Evaluate().Fields();
                        valueData = GetValueData(pid, fields.GetAt(1), fields.GetAt(5), fields.GetAt(3), fields.GetAt(2),
                                                 valueInfoParam, out valueType);
                        nameLengthIndex = 4;
                        nameIndex = 5;
                        break;
                    default:
                        break;
                }
                if(fields != null && valueName == null && nameIndex != -1)
                {
                    NktParam p = fields.GetAt(nameLengthIndex);
                    int nameSize = p.LongVal;
                    p = fields.GetAt(nameIndex);
                    valueName = p.ReadStringN(nameSize / 2);
                }
            }
        }

        void GetKeyInfo(NktParam infoClassParam, NktParam valueInfoParam, uint pid, Dictionary<string, string> parsedFields)
        {
            // KEY_VALUE_INFORMATION_CLASS 
            int infoType = infoClassParam.LongVal;

            if (!valueInfoParam.IsNullPointer)
            {
                NktParamsEnum fields = null;
                int nameIndex = -1, nameLengthIndex = -1;
                NktParam p;

                switch (infoType)
                {
                    case NativeApiTools.KeyBasicInformation:
                        fields = valueInfoParam.CastTo("PKEY_BASIC_INFORMATION").Evaluate().Fields();
                        nameLengthIndex = 2;
                        nameIndex = 3;
                        break;
                    case NativeApiTools.KeyNodeInformation:
                        {
                            fields = valueInfoParam.CastTo("PKEY_NODE_INFORMATION").Evaluate().Fields();
                            p = fields.GetAt(2);
                            int offset = p.LongVal;
                            p = fields.GetAt(3);
                            int length = p.LongVal;
                            if(offset != -1 && length != 0)
                            {
                                var memAddress = new IntPtr(valueInfoParam.Evaluate().Address.ToInt64() + offset);
                                NktProcessMemory procMem = _spyMgr.ProcessMemoryFromPID((int)pid);
                                string className = procMem.ReadStringN(memAddress, false, length / 2);
                                parsedFields["ClassName"] = className;
                            }
                            nameLengthIndex = 4;
                            nameIndex = 5;
                        }
                        break;
                    case NativeApiTools.KeyFullInformation:
                        {
                            // get class name
                            fields = valueInfoParam.CastTo("PKEY_FULL_INFORMATION").Evaluate().Fields();
                            p = fields.GetAt(2);
                            int offset = p.LongVal;
                            p = fields.GetAt(3);
                            int length = p.LongVal;
                            var memAddress = new IntPtr(valueInfoParam.Evaluate().Address.ToInt64() + offset);
                            NktProcessMemory procMem = _spyMgr.ProcessMemoryFromPID((int) pid);
                            string className = procMem.ReadStringN(memAddress, false, length/2);
                            parsedFields["ClassName"] = className;
                            p = fields.GetAt(4);
                            parsedFields["SubKeys"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                            p = fields.GetAt(7);
                            parsedFields["Values"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        }
                        break;
                    case NativeApiTools.KeyNameInformation:
                        {
                            fields = valueInfoParam.CastTo("PKEY_NAME_INFORMATION").Evaluate().Fields();
                            nameLengthIndex = 0;
                            nameIndex = 1;
                        }
                        break;
                    case NativeApiTools.KeyCachedInformation:
                        fields = valueInfoParam.CastTo("PKEY_CACHED_INFORMATION").Evaluate().Fields();
                        p = fields.GetAt(2);
                        parsedFields["SubKeys"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        p = fields.GetAt(4);
                        parsedFields["Values"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        p = fields.GetAt(7);
                        parsedFields["NameLength"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        break;
                    case NativeApiTools.KeyFlagsInformation:
                        Error.WriteLine("NativeApiTools.KeyCachedInformation not supported");
                        break;
                    case NativeApiTools.KeyVirtualizationInformation:
                        fields = valueInfoParam.CastTo("PKEY_VIRTUALIZATION_INFORMATION").Evaluate().Fields();
                        p = fields.GetAt(0);
                        parsedFields["VirtualizationCandidate"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        p = fields.GetAt(1);
                        parsedFields["VirtualizationEnabled"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        p = fields.GetAt(2);
                        parsedFields["VirtualTarget"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        p = fields.GetAt(3);
                        parsedFields["VirtualStore"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        p = fields.GetAt(4);
                        parsedFields["VirtualSource"] = p.LongVal.ToString(CultureInfo.InvariantCulture);
                        break;
                    case NativeApiTools.KeyHandleTagsInformation:
                        break;
                        
                    default:
                        Error.WriteLine("Unknown KEY_INFORMATION_CLASS " + infoType.ToString(CultureInfo.InvariantCulture));
                        break;
                }
                if (fields != null && nameIndex != -1)
                {
                    p = fields.GetAt(nameLengthIndex);
                    int nameSize = p.LongVal;
                    p = fields.GetAt(nameIndex);
                    if(nameSize != 0)
                    {
                        string name = p.ReadStringN(nameSize / 2);
                        parsedFields["Name"] = name;
                    }
                }
            }
        }

        void ProcessRegQueryValueBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegQueryValue(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);
            string subKey = "";
            string valueName;
            string valueData;

            // KEY_VALUE_INFORMATION_CLASS 
            RegistryValueKind valueType;

            // if retValue != 0 param 3 doesn't contain useful information
            GetValueInfo(pms.GetAt(1), pms.GetAt(2), e.RetValue == 0 ? pms.GetAt(3) : null, e.Pid, out valueName, out valueData, out valueType);

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
            RegQueryValueEvent.CreatePath(e, subKey, valueName, valueData, valueType);
            RegQueryValueEvent.SetDataComplete(e, true);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            //var e = new RegQueryValueEvent(subKey, (uint)callInfo.Cookie, valueName, valueData, valueType, retValue, module,
            //                                    stackTrace, time, pid, tid) { Win32Function = callInfo.Hook().FunctionName };
            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegQueryMultipleValuesBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegQueryMultipleValues(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);
            string subKey = "";

            p = pms.GetAt(2);
            int entryCount = p.LongVal;

            p = pms.GetAt(3);
            IntPtr baseAddress = IntPtr.Zero;
            if (!p.IsNullPointer)
            {
                baseAddress = p.Evaluate().Address;
            }

            p = pms.GetAt(1);

            var values = new string[entryCount, 2];
            var types = new RegistryValueKind[entryCount];
            bool dataAvailable = false;

            if(entryCount > 0)
            {
                NktProcessMemory procMem = _spyMgr.ProcessMemoryFromPID((int)e.Pid);

                // parsing KEY_VALUE_ENTRY Array
                for (int i = 0; i < entryCount; i++)
                {
                    NktParam keyValueEntry = p.IndexedEvaluate(i);
                    NktParamsEnum fields = keyValueEntry.Fields();
                    NktParam m = fields.GetAt(0);

                    values[i, 0] = NativeApiTools.GetUnicodeString(m);
                    m = fields.GetAt(3);
                    types[i] = (RegistryValueKind)m.LongVal;
                    m = fields.GetAt(1);
                    int dataLength = m.LongVal;
                    m = fields.GetAt(2);
                    int dataOffset = m.LongVal;

                    if(dataLength != -1 && dataLength != 0 && baseAddress != IntPtr.Zero)
                    {
                        var memAddress = new IntPtr(baseAddress.ToInt64() + dataOffset);
                        values[i, 1] = procMem.ReadStringN(memAddress, false, dataLength / 2);
                        dataAvailable = true;
                    }
                }
            }

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);
            e.CreateParams(dataAvailable ? 1 + entryCount * 3 : 1 + entryCount * 2);
            e.Params[0].Name = "Path";
            e.Params[0].Value = subKey;
            RegQueryValueEvent.SetDataAvailable(e, dataAvailable);

            for (int i = 0; i < entryCount; i++)
            {
                int index = (dataAvailable ? i*3 : i*2) + 1;
                e.Params[index].Name = "Value" + i;
                e.Params[index].Value = values[i, 0];
                e.Params[index + 1].Name = "Type" + i;
                e.Params[index].Value = RegistryTools.GetValueTypeString(types[i]);
                if (dataAvailable)
                {
                    e.Params[index + 2].Name = "Data" + i;
                    e.Params[index + 2].Value = values[i, 1];
                }
            }
            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegSetValueBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegSetValue(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);

            // VALUE NAME
            p = pms.GetAt(1);
            
            string valueName = NativeApiTools.GetUnicodeString(p);
            RegistryValueKind valueType;
            NktParam dataParam = pms.GetAt(4);
            string valueData = GetValueData(e.Pid, pms.GetAt(3), dataParam == null ? null : pms.GetAt(4).Evaluate(), pms.GetAt(5), null, null, out valueType);
            string subKey = "";

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);

            RegQueryValueEvent.CreatePath(e, subKey, valueName, valueData, valueType);
            RegQueryValueEvent.SetDataComplete(e, true);
            RegQueryValueEvent.SetWrite(e, true);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            //var e = new RegSetValueEvent(subKey, (uint)callInfo.Cookie, valueName, valueData, valueType, retValue, module,
            //                                  stackTrace, time, pid, tid) { Win32Function = callInfo.Hook().FunctionName };
            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegEnumerateValueKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegEnumerateValueKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);
            string subKey = "";
            string valueName;
            string valueData;

            // Index
            p = pms.GetAt(1);
            int index = p.LongVal;

            // KEY_VALUE_INFORMATION_CLASS 
            RegistryValueKind valueType = RegistryValueKind.Unknown;

            if(e.RetValue == 0)
            {
                GetValueInfo(null, pms.GetAt(2), pms.GetAt(3), e.Pid, out valueName, out valueData, out valueType);
            }
            else
            {
                valueName = null;
                valueData = null;
            }

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);

            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            e.CreateParams(5);
            RegQueryValueEvent.CreatePath(false, e, subKey, valueName, valueData, valueType);
            e.Params[4].Name = "Index";
            e.Params[4].Value = index.ToString(CultureInfo.InvariantCulture);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegEnumerateKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegEnumerateKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);
            string subKey = "";

            // Index
            p = pms.GetAt(1);
            int index = p.LongVal;
            var parsedFields = new Dictionary<string, string>();

            if (e.RetValue == 0)
            {
                GetKeyInfo(pms.GetAt(2), pms.GetAt(3), e.Pid, parsedFields);
            }

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            e.CreateParams(2 + parsedFields.Count);
            if (parsedFields.ContainsKey("Name"))
            {
                e.Params[0].Value = subKey + "\\" + parsedFields["Name"];
            }
            else
            {
                e.Params[0].Value = subKey;
            }

            RegQueryValueEvent.SetParentKey(e, subKey);

            e.Params[0].Name = "Path";

            e.Params[1].Name = "Index";
            e.Params[1].Value = index.ToString(CultureInfo.InvariantCulture);

            int i = 2;
            foreach (var f in parsedFields)
            {
                e.Params[i].Name = f.Key;
                e.Params[i++].Value = f.Value;
            }

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegQueryKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegQueryKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);
            string subKey = "";

            var parsedFields = new Dictionary<string, string>();

            if (e.RetValue == 0)
            {
                GetKeyInfo(pms.GetAt(1), pms.GetAt(2), e.Pid, parsedFields);
            }

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            e.CreateParams(1 + parsedFields.Count);
            e.Params[0].Name = "Path";
            e.Params[0].Value = subKey;

            int i = 1;
            foreach(var f in parsedFields)
            {
                e.Params[i].Name = f.Key;
                e.Params[i++].Value = f.Value;
            }

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegDeleteValueBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegDeleteValue(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);

            // VALUE NAME
            p = pms.GetAt(1);
            string valueName = NativeApiTools.GetUnicodeString(p);
            string subKey = "";

            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            RegQueryValueEvent.CreatePath(e, subKey, valueName, null, RegistryValueKind.Unknown);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegDeleteKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegDeleteKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);

            string subKey = "";
            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);

            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            e.CreateParams(1);
            e.Params[0].Value = subKey;
            e.Params[0].Name = "Path";

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessRegRenameKeyBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessRegRenameKey(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            // HKEY
            UInt64 hKey = RegistryTools.ToUInt64(p.SizeTVal);

            p = pms.GetAt(1);
            string newName = NativeApiTools.GetUnicodeString(p);

            string subKey = "";
            subKey = RegistryTools.GetFullKey(hKey, subKey, e.Pid, e.Tid);

            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            e.CreateParams(2);
            e.Params[0].Value = subKey;
            e.Params[0].Name = "Path";
            e.Params[1].Value = newName;
            e.Params[1].Name = "NewName";

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessLoadLibraryBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            string modParam = "";

            NktParam p = callInfo.Params().GetAt(2);
            if (p.IsNullPointer == false)
            {
                p = p.Evaluate().Field(2);
                if (p.IsWideString)
                {
                    modParam = p.ReadString();
                    //if(modParam.ToLower().Contains("esxelqozjezb"))
                    //    Console.WriteLine("");
                }
            }
            LoadLibraryEvent.CreateEventParams(e, modParam, 0);

            _devRunTrace.ProcessNewEvent(e);
        }
        public string LoadDllModule(NktProcess proc, IntPtr addr, bool definitelyLoad)
        {
            string dllName;
            NktModule mod = null;
            if (!_modulePath.TryGetPathByAddress((uint)proc.Id, (UInt64)addr.ToInt64(), out dllName))
            {
                proc = _spyMgr.Processes().GetById(proc.Id);
                if (proc != null)
                {
                    mod = proc.Modules().GetByAddress(addr, eNktSearchMode.smFindContaining);
                    // it should be loaded!
                    if (mod == null)
                    {
                        proc.InvalidateCache(IntPtr.Zero);
                        mod = proc.Modules().GetByAddress(addr, eNktSearchMode.smFindContaining);
                    }
                    if (mod != null)
                    {
                        dllName = mod.Path;
                        if (!_modulePath.Contains((uint)proc.Id, (UInt64)addr.ToInt64()))
                        {
                            dllName = FileSystemTools.GetCanonicalPathName((uint)proc.Id, dllName, _processInfo);
                            if (definitelyLoad)
                                _modulePath.AddModule(dllName, (uint)proc.Id, (UInt64)addr.ToInt64());
                        }

                        //Console.WriteLine("LoadModule XXXXX: " + loadModule);
                        //if (_moduleLoadPending.ContainsKey(pid) && _moduleLoadPending[pid].Contains(loadModule))
                        //{
                        //    //Console.WriteLine("Hooking XXXXX: " + loadModule);
                        //    var hooksByMod = _hookStateMgr.GetHooksByModule(pid, loadModule);
                        //    foreach (var h in hooksByMod)
                        //    {
                        //        h.Attach(proc, false);
                        //    }d
                        //    _moduleLoadPending[pid].Remove(loadModule);
                        //}
                    }
                }
            }
            //Debug.Assert(mod != null);
            if (definitelyLoad && !string.IsNullOrEmpty(dllName) && _hookGroupMgr.IsActiveXActive())
            {
                //Debug.WriteLine("Loading DLL: " + dllName + "; Address: " + addr.ToString("X"));
                AddComHooks(mod, proc, dllName, addr, true);
            }
            return dllName;
        }
        void ProcessLoadLibrary(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            string dllName = "";
            IntPtr addr = IntPtr.Zero;

            e.Success = (e.RetValue < 0x80000000); /* NT_SUCCESS */
            if (e.Success)
            {
                e.RetValue = 0;
                addr = callInfo.Params().GetAt(3).Evaluate().CastTo("SIZE_T").SizeTVal;

                //if (addr == new IntPtr(0x74460000))
                //    Error.WriteLine("");
                // at this point we use the address, we don't care about the string to load the module
                dllName = LoadDllModule(proc, addr, false);
                if (AsyncMode())
                    return;
            }
            else
            {
                if (AsyncMode())
                    return;
                NktParam p = callInfo.Params().GetAt(2);
                dllName = NativeApiTools.GetUnicodeString(p);
            }
            if (!string.IsNullOrEmpty(dllName))
                dllName = FileSystemTools.TryEnsurePathIsAbsolute(dllName, e, _spyMgr);
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);
            LoadLibraryEvent.CreateEventParams(e, dllName, (UInt64) addr.ToInt64());

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessFindResourceBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessLoadResourceBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessFindResource(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            //// on XP FindResourceA, FindResourceExA and FindResourceW calls FindResourceExW so we need to keep only the first one
            //// if previous in the stack has the same root we will keep the first one
            //if (e.CallStack.Count > 0 && e.CallStack[0].NearestSymbol.StartsWith("kernel32.dll!FindResource"))
            //{
            //    //Trace.WriteLine(stackTrace[0].nearestSymbol);
            //    return;
            //}

            NktParam p = callInfo.Params().GetAt(1);
            string type = "", name = "";

            if (p.IsNullPointer == false)
            {
                var valInt = (int)p.IntResourceString; //return > 0 if atom

                if (valInt > 0)
                {
                    type = "0x" + valInt.ToString("X");
                }
                else
                {
                    type = p.ReadString();
                }
            }
            p = callInfo.Params().GetAt(2);
            if (p.IsNullPointer == false)
            {
                var valInt = p.IntResourceString; //return > 0 if atom

                if (valInt > 0)
                {
                    name = valInt.ToString("X");
                }
                else
                {
                    name = p.ReadString();
                }
            }
            var language = "";
            p = callInfo.Params().GetAt(3);
            int langId = p.IntResourceString;
            // current == 0
            if (langId != 0)
            {
                try
                {
                    var ci = new CultureInfo(langId);
                    language = ci.ToString();
                }
                catch (Exception)
                {
                }
            }
            if (string.IsNullOrEmpty(language))
            {
                language = CultureInfo.CurrentCulture.ToString();
            }

            string dllPath;

            IntPtr addr = callInfo.Params().GetAt(0).Evaluate().CastTo("SIZE_T").SizeTVal;

            // at this point we use the address, we don't care the string to load the module
            if (!_modulePath.TryGetPathByAddress(e.Pid, (UInt64) addr.ToInt64(), out dllPath))
            {
                proc = _spyMgr.Processes().GetById((int)e.Pid);
                if (proc != null)
                {
                    NktModule mod = proc.Modules().GetByAddress(addr, eNktSearchMode.smGetNearest);
                    // it should be loaded!
                    if (mod == null)
                    {
                        System.Threading.Thread.Sleep(0);
                        mod = proc.Modules().GetByAddress(addr, eNktSearchMode.smGetNearest);
                    }
                    if (mod != null)
                    {
                        dllPath = mod.Path;
                        if (!_modulePath.Contains(e.Pid, (UInt64)addr.ToInt64()))
                        {
                            dllPath = FileSystemTools.GetCanonicalPathName((uint) proc.Id, dllPath, _processInfo);
                            _modulePath.AddModule(dllPath, (uint) proc.Id, (UInt64) addr.ToInt64());
                        }
                    }
                }
            }
            if(string.IsNullOrEmpty(dllPath))
            {
                dllPath = "<0x" + addr.ToString("X") + ">";
            }
            //if (!string.IsNullOrEmpty(dllPath))
            //{
            //    dllPath = FileSystemTools.GetResourcePath(dllPath, language);
            //}
            //else
            //{
            //    dllPath = "0x" + addr.ToString("X");
            //}

            e.Success = (e.RetValue != 0);
            e.Result = ((e.RetValue != 0) ? "SUCCESS" : "ERROR");

            e.CreateParams(4);
            e.Params[0] = new Param("Path", dllPath);
            e.Params[1] = new Param("Type", type);
            e.Params[2] = new Param("Name", name);
            e.Params[3] = new Param("Language", language);
            FileSystemEvent.SetAccess(e, FileSystemAccess.Resource);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessLoadResource(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            IntPtr hmodule = callInfo.Params().GetAt(0).SizeTVal;

            var module = callInfo.Process().ModuleByAddress(hmodule, eNktSearchMode.smFindContaining);

            e.Success = (e.RetValue != 0);
            e.Result = ((e.RetValue != 0) ? "SUCCESS" : "ERROR");

            e.CreateParams(1);
            e.Params[0] = new Param("Path", module != null ? module.Path : "<0x" + ((uint)hmodule).ToString("X") + ">");
            if (module == null)
                FileSystemEvent.SetModuleNotFound(e, true);
            FileSystemEvent.SetAccess(e, FileSystemAccess.Resource);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessCreateFileBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParam p = callInfo.Params().GetAt(2);
            if(!p.IsNullPointer)
            {
                NktParamsEnum fields = p.Fields();

                string filename = FileSystemTools.GetFileHandlePath(fields.GetAt(2), e.Pid);
                CreateFileEvent.CreateEventParams(e, filename, 0, false);

                //var e = new CreateFileEvent(filename, (uint) callInfo.Cookie, pid, tid)
                //            {Win32Function = callInfo.Hook().FunctionName};
            }
            _devRunTrace.ProcessNewEvent(e);
        }
        public FileSystemAccess AccessFlagToFileSystemAccess(uint access)
        {
            FileSystemAccess openedAccess = FileSystemAccess.None;

            // GENERIC_WRITE || WRITE_OWNER || WRITE_DAC || WRITE_DATA || FILE_APPEND_DATA
            if ((access & 0x400C0006) != 0)
                openedAccess |= FileSystemAccess.Write;
            // FILE_EXECUTE
            if ((access & 0x00000020) != 0)
                openedAccess |= FileSystemAccess.Execute;
            // GENERIC_READ || READ_CONTROL || STANDARD_RIGHTS_REQUIRED
            if ((access & 0x800F0001) != 0)
                openedAccess |= FileSystemAccess.Read;
            // SYNCHRONIZE 
            if ((access & 0x00100000) != 0)
                openedAccess |= FileSystemAccess.Synchronize;
            // DELETE
            if ((access & 0x00010000) != 0)
                openedAccess |= FileSystemAccess.Delete;
            // FILE_READ_ATTRIBUTES || FILE_READ_EA
            if ((access & 0x0088) != 0)
                openedAccess |= FileSystemAccess.ReadAttributes;
            // FILE_WRITE_ATTRIBUTES || FILE_WRITE_EA
            if ((access & 0x0110) != 0)
                openedAccess |= FileSystemAccess.WriteAttributes;

            return openedAccess;
        }

        void ProcessCreateFile(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            string returnedFilename = "";
            NktParamsEnum pms = callInfo.Params();
            if (e.RetValue == 0)
            {
                NktParam p = pms.GetAt(0);
                if(!p.IsNullPointer)
                {
                    IntPtr hFile = p.Evaluate().SizeTVal;
                    if(hFile != IntPtr.Zero)
                        returnedFilename = FileSystemTools.GetFileHandlePath(hFile, e.Pid);
                }
            }

            if (!string.IsNullOrEmpty(returnedFilename))
                returnedFilename = FileSystemTools.GetCanonicalPathName((uint)proc.Id, returnedFilename, _processInfo);

            string passedFilename = FileSystemTools.GetFileHandlePath(pms.GetAt(2), e.Pid);

            if (!string.IsNullOrEmpty(passedFilename))
                passedFilename = FileSystemTools.GetCanonicalPathName((uint)proc.Id, passedFilename, _processInfo);

            string desiredAccess = _paramHandlerMgr.TranslateParam("ACCESS_MASK", pms.GetAt(1).ULongVal);
            string attributes;
            uint access = pms.GetAt(1).ULongVal;
            string share;
            string createDisposition;
            string options;
            uint optionsLong;

            if(hookType == HookType.OpenFile)
            {
                share = _paramHandlerMgr.TranslateParam("SHARE_MASK", pms.GetAt(4).ULongVal);
                optionsLong = pms.GetAt(5).ULongVal;
                options = _paramHandlerMgr.TranslateParam("FILE_OPEN_OPTIONS", optionsLong);
                createDisposition = attributes = "";
            }
            else
            {
                attributes = _paramHandlerMgr.TranslateParam("FILE_ATTRIBUTE", pms.GetAt(5).ULongVal);
                share = _paramHandlerMgr.TranslateParam("SHARE_MASK", pms.GetAt(6).ULongVal);
                createDisposition = _paramHandlerMgr.TranslateParam("CREATE_DISPOSITION_MASK", pms.GetAt(7).ULongVal);
                optionsLong = pms.GetAt(8).ULongVal;
                options = _paramHandlerMgr.TranslateParam("FILE_OPEN_OPTIONS", optionsLong);
            }

            FileSystemAccess openedAccess = AccessFlagToFileSystemAccess(access);

            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            returnedFilename = FileSystemTools.TryEnsurePathIsAbsolute(returnedFilename, e, _spyMgr);

            CreateFileEvent.CreateEventParams(e, passedFilename, returnedFilename, openedAccess, desiredAccess, attributes, share, options, createDisposition, false);
            if (hookType == HookType.OpenFile)
            {
                e.Function = "OpenFile";
            }

            // FILE_DIRECTORY_FILE
            if ((optionsLong & 0x00000001) != 0)
                FileSystemEvent.SetDirectory(e, true);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessDeleteFileBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessDeleteFile(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            string filename = "";
            NktParam p = callInfo.Params().GetAt(2);
            if (!p.IsNullPointer)
            {
                NktParamsEnum fields = p.Fields();

                filename = FileSystemTools.GetFileHandlePath(fields.GetAt(0), e.Pid);
            }

            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            e.Result = Declarations.NtStatusToString(e.RetValue);
            e.Success = (e.RetValue == 0);

            e.CreateParams(1);
            e.Params[0].Name = "Path";
            e.Params[0].Value = filename;
            FileSystemEvent.SetAccess(e, FileSystemAccess.Delete);
            
            _devRunTrace.ProcessNewEvent(e);
        }

        int GetFileInformation(uint fileInfoClass, NktParam fileInfo, int index, List<FileSystemTools.FileInformation> parsedFields)
        {
            NktParamsEnum fields = null;
            int nameIndex = -1, nameLengthIndex = -1, fileAttrIndex = -1;
            
            int nextItemOffset = 0;
            
            // FILE_INFORMATION_CLASS 
            switch (fileInfoClass)
            {
                case NativeApiTools.FileBothDirectoryInformation:
                    {
                        nameLengthIndex = 9;
                        nameIndex = 13;
                        fileAttrIndex = 8;
                        fields = fileInfo.CastTo("FILE_BOTH_DIR_INFORMATION").Fields();
                    }
                    break;
                case NativeApiTools.FileDirectoryInformation:
                    {
                        nameLengthIndex = 9;
                        nameIndex = 10;
                        fileAttrIndex = 8;
                        fields = fileInfo.CastTo("FILE_DIRECTORY_INFORMATION").Fields();
                    }
                    break;
                case NativeApiTools.FileFullDirectoryInformation:
                    {
                        nameLengthIndex = 9;
                        nameIndex = 11;
                        fileAttrIndex = 8;
                        fields = fileInfo.CastTo("FILE_FULL_DIR_INFORMATION").Fields();
                    }
                    break;
                case NativeApiTools.FileIdBothDirectoryInformation:
                    {
                        nameLengthIndex = 9;
                        nameIndex = 14;
                        fileAttrIndex = 8;
                        fields = fileInfo.CastTo("FILE_ID_BOTH_DIR_INFORMATION").Fields();
                    }
                    break;
                case NativeApiTools.FileIdFullDirectoryInformation:
                    {
                        nameLengthIndex = 9;
                        nameIndex = 12;
                        fileAttrIndex = 8;
                        fields = fileInfo.CastTo("FILE_ID_FULL_DIR_INFORMATION").Fields();
                    }
                    break;
                case NativeApiTools.FileNamesInformation:
                    {
                        nameLengthIndex = 2;
                        nameIndex = 3;
                        fields = fileInfo.CastTo("FILE_NAMES_INFORMATION").Fields();
                    }
                    break;
                case NativeApiTools.FileObjectIdInformation:
                    break;
                case NativeApiTools.FileReparsePointInformation:
                    break;

                default:
                    Error.WriteLine("Unknown FILE_INFORMATION_CLASS " + fileInfoClass.ToString(CultureInfo.InvariantCulture));
                    break;
            }
            if (fields != null && nameIndex != -1)
            {
                // name
                NktParam p = fields.GetAt(nameIndex);
                // name length
                int length = fields.GetAt(nameLengthIndex).LongVal;
                Int64 attributes = fileAttrIndex == -1 ? 0 : (Int64) fields.GetAt(fileAttrIndex).ULongVal;
                if (length != 0)
                {
                    parsedFields.Add(new FileSystemTools.FileInformation("Name" + index, p.ReadStringN(length/2),
                                                                         (FileSystemTools.FileAttribute) attributes));
                }
                nextItemOffset = fields.GetAt(0).LongVal;
            }
            return nextItemOffset;
        }

        void ProcessQueryDirectoryFileBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessQueryDirectoryFile(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(0);

            IntPtr hFile = p.SizeTVal;
            string path = FileSystemTools.GetFileHandlePath(hFile, e.Pid);
            path = FileSystemTools.GetCanonicalPathName(e.Pid, path, _processInfo);

            bool restartScan = (pms.GetAt(10).ULongVal != 0);
            string wildCard = NativeApiTools.GetUnicodeString(pms.GetAt(9));
            uint fileInfoClass = pms.GetAt(7).ULongVal;

            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            e.Result = Declarations.NtStatusToString(e.RetValue);
            e.Success = (e.RetValue == 0);

            List<FileSystemTools.FileInformation> parsedFields = null;

            p = pms.GetAt(5);
            if (e.Success && !p.IsNullPointer)
            {
                int i = 0;
                int nextItemOffset;
                p = p.Evaluate();

                parsedFields = new List<FileSystemTools.FileInformation>();

                do
                {
                    nextItemOffset = GetFileInformation(fileInfoClass, p, i++, parsedFields);
                    if (nextItemOffset != 0)
                    {
                        var memAddress = new IntPtr(p.Address.ToInt64() + nextItemOffset);
                        NktProcessMemory procMem = _spyMgr.ProcessMemoryFromPID((int)e.Pid);
                        p = procMem.BuildParam(memAddress, "PVOID");
                    }
                } while (nextItemOffset != 0);
            }

            e.CreateParams(4 + (parsedFields == null ? 0 : parsedFields.Count));
            e.Params[0].Name = "Path";
            e.Params[0].Value = path;
            e.Params[1].Name = "Wildcard";
            e.Params[1].Value = wildCard;
            e.Params[2].Name = "FileInfoClass";
            e.Params[2].Value = fileInfoClass.ToString(CultureInfo.InvariantCulture);
            e.Params[3].Name = "RestartScan";
            e.Params[3].Value = restartScan ? "TRUE" : "FALSE";
            FileSystemEvent.SetDirectory(e, true);
            FileSystemEvent.SetAccess(e, FileSystemAccess.Read);
            FileSystemEvent.SetQueryAttributes(e, true);

            if (parsedFields != null)
            {
                int i = 0;
                foreach (var param in parsedFields)
                {
                    e.Params[4 + i].Name = "File" + (i + 1);
                    e.Params[4 + i++].Value = param.FieldName;
                    //e.SetProperty(param.FieldName, (uint) param.Attributes);
                }
            }
            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessQueryAttributesFileBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParam p = callInfo.Params().GetAt(0);
            FileSystemEvent.SetQueryAttributes(e, true);

            if (p.IsNullPointer == false)
            {
                _devRunTrace.ProcessNewEvent(e);
            }
        }
        void ProcessQueryAttributesFile(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            FileSystemEvent.SetQueryAttributes(e, true);

            NktParam p = callInfo.Params().GetAt(0);
            if (p.IsNullPointer == false)
            {
                string filename = FileSystemTools.GetFileHandlePath(p, e.Pid);
                filename = FileSystemTools.GetCanonicalPathName((uint) proc.Id, filename, _processInfo);
                filename = FileSystemTools.TryEnsurePathIsAbsolute(filename, e, _spyMgr);

                DeviareTools.SetStackInfo(e, callInfo, _modulePath);
                e.CreateParams(2);
                e.Params[0].Name = "Path";
                e.Params[0].Value = filename;
                e.Params[1].Name = "Access";
                e.Params[1].Value = FileSystemTools.GetAccessString(FileSystemAccess.ReadAttributes);
                FileSystemEvent.SetAccess(e, FileSystemAccess.ReadAttributes);

                e.Result = Declarations.NtStatusToString(e.RetValue);
                e.Success = (e.RetValue == 0);
                _devRunTrace.ProcessNewEvent(e);
            }
        }

        void ProcessCreateDirectoryBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParam p = callInfo.Params().GetAt(0);

            if (p.IsNullPointer == false)
            {
                string filename = p.ReadString();

                CreateDirectoryEvent.CreateEventParams(e, filename);
                FileSystemEvent.SetDirectory(e, true);
                _devRunTrace.ProcessNewEvent(e);
            }
        }
        void ProcessCreateDirectory(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParam p = callInfo.Params().GetAt(0);

            if (p.IsNullPointer == false)
            {
                string filename = p.ReadString();

                DeviareTools.SetStackInfo(e, callInfo, _modulePath);
                CreateDirectoryEvent.CreateEventParams(e, filename);
                FileSystemEvent.SetDirectory(e, true);
                _devRunTrace.ProcessNewEvent(e);
            }
        }

        private void AllowComServer(string clsid)
        {
            if (clsid == null)
                return;
            var normalizeBraces = new Regex("^{?(.*)}?$");
            var match = normalizeBraces.Match(clsid);
            clsid = match.Groups[1].ToString();
            if (clsid == "")
                return;
            string path = @"HKEY_CLASSES_ROOT\CLSID\{" + clsid + @"}\InProcServer32";
            var valueValue = Microsoft.Win32.Registry.GetValue(path, "", null) as string;
            if (valueValue == null)
                return;
            AllowCommandLineHash(StringHash.MultiplicationHash(valueValue));
            valueValue += " -Embedding";
            AllowCommandLineHash(StringHash.MultiplicationHash(valueValue));
        }

        void ProcessCoCreateBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            var stackTrace = new List<DeviareTools.DeviareStackFrame>();
            DeviareTools.GetStackTrace(callInfo, stackTrace, _modulePath);

            // WORKAROUND: we've detect NVIDIA disabling a hook in CoCreateInstance so we have to check if the CoCreateInstance was processed
            // Otherwise, we will loose a call.
            int val;
            // no Ex version
            if(tag == 0)
            {
                if(!_coCreateThreadCount.ContainsKey(e.Tid))
                {
                    _coCreateThreadCount[e.Tid] = 0;
                }
                _coCreateThreadCount[e.Tid]++;
            }
            else if (_coCreateThreadCount.TryGetValue(e.Tid, out val) && val > 0)
            {
                // CoCreateInstance may call the ex version on some o.s.
                if (stackTrace.Count > 0 && stackTrace[0].NearestSymbol.StartsWith("ole32.dll!CoCreateInstance"))
                {
                    //Trace.WriteLine(stackTrace[0].nearestSymbol);
                    return;
                }
            }

            NktParam p = callInfo.Params().GetAt(0);
            if (!p.IsNullPointer)
            {
                p = p.Evaluate();
                var clsid = DeviareTools.ClsIdStruct2String(p);
                AllowComServer(clsid);
                e.CreateEventParams(clsid);
                //var e = new CoCreateEvent(clsid, (uint)callInfo.Cookie, pid, tid)
                //            {Win32Function = callInfo.Hook().FunctionName};
                _devRunTrace.ProcessNewEvent(e);
            }
        }
        void ProcessCoCreate(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            int val;

            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            // WORKAROUND: we've detect NVIDIA disabling a hook in CoCreateInstance so we have to check if the CoCreateInstance was processed
            // Otherwise, we will loose a call.
            if (tag == 0)
            {
                // it should be there because ProcessCoCreateBefore should have processed it
                if (!_coCreateThreadCount.ContainsKey(e.Tid))
                {
                    Error.WriteLine("ProcessCoCreate: Cannot find tid " + e.Tid);
                }
                else
                {
                    _coCreateThreadCount[e.Tid]--;
                }
            }
            else if (_coCreateThreadCount.TryGetValue(e.Tid, out val) && val > 0)
            {
                // CoCreateInstance may call the ex version on some o.s.
                if (e.CallStack.Count > 0 && e.CallStack[0].NearestSymbol.StartsWith("ole32.dll!CoCreateInstance"))
                {
                    //Trace.WriteLine(stackTrace[0].nearestSymbol);
                    return;
                }
            }

            NktParam p = callInfo.Params().GetAt(0);
            if (p.IsNullPointer == false)
            {
                p = p.Evaluate();
                var clsid = DeviareTools.ClsIdStruct2String(p);

                e.CreateEventParams(clsid);
                _devRunTrace.ProcessNewEvent(e);
            }
        }

        void ProcessCreateDialogBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessCreateDialog(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            string className = null;

            // UNICODE?
            NktParam p = pms.GetAt(5);
            bool isUnicode = (p.LongVal == 0);

            // HINSTANCE
            p = pms.GetAt(0);
            ulong hModule = RegistryTools.ToUInt64(p.SizeTVal);
            if (e.RetValue != 0)
            {
                className = _windowClassNames.GetClassName((UIntPtr)e.RetValue);
            }

            // LPCDLGTEMPLATE
            if(className == null)
            {
                p = pms.GetAt(1);

                if (p.IsNullPointer == false)
                {
                    var valInt = p.IntResourceString; //return > 0 if atom

                    if (valInt > 0)
                    {
                        className = "0x" + valInt.ToString("X");
                    }
                    else
                    {
                        className = (isUnicode ? p.CastTo("LPWSTR").ReadString() : p.CastTo("LPSTR").ReadString());
                    }
                }
            }

            CreateWindowEvent.CreateEventParams(e, className, null, hModule);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessCreateWindowBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            // on XP CreateWindowExA calls CreateWindowExW so we need to keep only the first one
            // if previous in the stack has the same root we will keep the first one
            if (e.CallStack.Count > 0 && e.CallStack[0].NearestSymbol.StartsWith("user32.dll!CreateWindowEx"))
            {
                //Trace.WriteLine(stackTrace[0].nearestSymbol);
                return;
            }

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(1);

            if (p.IsNullPointer == false)
            {
                var valInt = p.IntResourceString; //return > 0 if atom
                string className;

                if (valInt > 0)
                {
                    className = "0x" + valInt.ToString("X");
                }
                else
                {
                    className = p.ReadString();
                }

                CreateWindowEvent.CreateEventParams(e, className, "", 0);

                _devRunTrace.ProcessNewEvent(e);
            }
        }
        void ProcessCreateWindow(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            // on XP CreateWindowExA calls CreateWindowExW so we need to keep only the first one
            // if previous in the stack has the same root we will keep the first one
            if (e.CallStack.Count > 0 && e.CallStack[0].NearestSymbol.StartsWith("user32.dll!CreateWindowEx"))
            {
                return;
            }

            NktParamsEnum pms = callInfo.Params();
            string className = null;
            string wndName = "";

            NktParam p = pms.GetAt(2);
            if (p.IsNullPointer == false)
            {
                wndName = p.ReadString();
            }
            p = pms.GetAt(10);
            ulong hModule = RegistryTools.ToUInt64(p.SizeTVal);
            if (e.RetValue != 0)
            {
                className = _windowClassNames.GetClassName((UIntPtr)e.RetValue);
            }
            else
            {
                p = pms.GetAt(1);

                if (p.IsNullPointer == false)
                {
                    var valInt = p.IntResourceString; //return > 0 if atom

                    if (valInt > 0)
                    {
                        className = "0x" + valInt.ToString("X");
                    }
                    else
                    {
                        className = p.ReadString();
                    }
                }
            }
            CreateWindowEvent.CreateEventParams(e, className, wndName, hModule);

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessCreateProcessBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(1);
            string processPath = "";
            string cmdLine = "";

            if (p.IsNullPointer == false)
            {
                processPath = p.ReadString();
            }
            p = pms.GetAt(2);
            if (p.IsNullPointer == false)
            {
                cmdLine = p.ReadString();
            }
            CreateProcessEvent.CreateEventParams(e, processPath, cmdLine);

            _devRunTrace.ProcessNewEvent(e);
        }
        uint ProcessCreateProcess(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);

            NktParamsEnum pms = callInfo.Params();
            NktParam p = pms.GetAt(1);
            string processPath = "";
            string cmdLine = "";

            if (p.IsNullPointer == false)
            {
                processPath = p.ReadString();
            }
            p = pms.GetAt(2);
            if (p.IsNullPointer == false)
            {
                cmdLine = p.ReadString();
            }
            // LPPROCESS_INFORMATION
            p = pms.GetAt(10);

            uint newPid = 0;
            // get created pid
            if (!p.IsNullPointer)
            {
                p = p.Evaluate().Fields().GetAt(2);
                newPid = p.ULongVal;
            }

            CreateProcessEvent.CreateEventParams(e, processPath, cmdLine, newPid);

            _devRunTrace.ProcessNewEvent(e);
            return newPid;
        }

        private void ProcessCreateServiceBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);
            var pms = callInfo.Params();
            var p = pms.GetAt(7);
            e.CreateParams(1);
            e.Params[0].Name = "CommandLine";
            string s = p.IsNullPointer ? null : p.ReadString();
            e.Params[0].Value = s;
            e.ParamMainIndex = 0;
            AllowCommandLineHash(StringHash.MultiplicationHash(s));
            _devRunTrace.ProcessNewEvent(e);
        }

        private void ProcessCreateService(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            ProcessCreateServiceBefore(e, callInfo, proc, hookType, tag);
        }

        private void ProcessOpenServiceBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);
            var pms = callInfo.Params();
            var p = pms.GetAt(1);
            e.CreateParams(1);
            e.Params[0].Name = "ServiceName";
            string s = p.IsNullPointer ? null : p.ReadString();
            e.Params[0].Value = s;
            e.ParamMainIndex = 0;
            AsyncHookMgr.ReadAndAllowExistingService(s);
            _devRunTrace.ProcessNewEvent(e);
        }

        private void ProcessOpenService(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            ProcessOpenServiceBefore(e, callInfo, proc, hookType, tag);
        }

        void ParseDefaultException(uint paramCount, NktParam p, List<Param> processedParams, uint unicodeMask)
        {
            ParseDefaultException(paramCount, p, processedParams, unicodeMask, null);
        }

        void ParseDefaultException(uint paramCount, NktParam p, List<Param> processedParams, uint unicodeMask, string[] names)
        {
            //processedParams.Add(new Param("ParamCount", paramCount.ToString(CultureInfo.InvariantCulture)));
            for (int i = 0; i < paramCount; i++)
            {
                string name = (names != null && names.Length > i ? names[i] : "P" + i.ToString(CultureInfo.InvariantCulture));
                // is unicode string?
                if((unicodeMask & 1<<i) != 0)
                {
                    processedParams.Add(new Param(name,
                                                  NativeApiTools.GetUnicodeString(
                                                      p.IndexedEvaluate(i).CastTo("PUNICODE_STRING"))));
                }
                else
                {
                    // parse as value
                    uint paramValue = p.IndexedEvaluate(i).ULongVal;
                    processedParams.Add(new Param(name,
                                                  paramValue.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        void ParseExceptionParams(uint code, NktParam countParam, NktParam unicodeMaskParam, NktParam paramArrayParam, List<Param> processedParams)
        {
            if (countParam.ULongVal != 0)
            {
                uint count = countParam.ULongVal;
                uint unicodeMask = unicodeMaskParam != null ? unicodeMaskParam.ULongVal : 0xffffffff;

                switch (code)
                {
                    // ORDINAL_NOT_FOUND
                    case 0xC0000138:
                        ParseDefaultException(count, paramArrayParam, processedParams, unicodeMask, new[] { "Ordinal", "Module" });
                        //ParseOrdinalNotFoundException(count, p, processedParams, unicodeMask);
                        break;
                    // ENTRYPOINT_NOT_FOUND
                    case 0xC0000139:
                        ParseDefaultException(count, paramArrayParam, processedParams, unicodeMask, new[] { "EntryPoint", "Module" });
                        //ParseEntryPointNotFoundException(count, p, processedParams, unicodeMask);
                        break;
                    // CANNOT_LOAD_REGISTRY_FILE
                    case 0xC0000218:
                        ParseDefaultException(count, paramArrayParam, processedParams, unicodeMask, new[] { "Path" });
                        //ParseCannotLoadRegistryFileException(count, p, processedParams, unicodeMask);
                        break;
                    // INVALID_IMAGE_FORMAT
                    case 0xC000007B:
                        ParseDefaultException(count, paramArrayParam, processedParams, unicodeMask, new[] { "Path" });
                        break;
                    // STATUS_FATAL_APP_EXIT
                    // HARDERROR_OVERRIDE_ERRORMODE
                    case 0x40000015:
                        ParseDefaultException(count, paramArrayParam, processedParams, unicodeMask, new[] { "Message" });
                        break;
                    default:
                        ParseDefaultException(count, paramArrayParam, processedParams, unicodeMask);
                        break;
                }
            }
        }

        void ProcessRaiseExceptionBefore(CallEvent e, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath, true);

            NktParamsEnum pms = callInfo.Params();
            NktParam excepRecord, context, p;
            uint flags, code;
            string codeString;

            var processedParams = new List<Param>();
            e.OnlyBefore = true;
            e.Critical = true;

            switch (tag)
            {
                case 0:
                case 1:
                    // PEXCEPTION_RECORD
                    excepRecord = pms.GetAt(0);
                    context = (tag == 0) ? pms.GetAt(1) : null;
cont1:
                    ulong faultingAddr = ulong.MaxValue;
                    //switch (proc.PlatformBits)
                    //{
                    //    case 32:
                    //        {
                    //            ExceptionContext.CONTEXT_i386 ctx32;

                    //            //try to get some info from context record. because CONTEXT struct is
                    //            //processor dependant we manually extract needed fields
                    //            if (ExceptionContext.ReadContext32(context, out ctx32) != false)
                    //            {
                    //                faultingAddr = (ulong)ctx32.Eip;
                    //            }
                    //        }
                    //        break;

                    //    case 64:
                    //        {
                    //            ExceptionContext.CONTEXT_amd64 ctx64;

                    //            //try to get some info from context record. because CONTEXT struct is
                    //            //processor dependant we manually extract needed fields
                    //            if (ExceptionContext.ReadContext64(context, out ctx64) != false)
                    //            {
                    //                faultingAddr = ctx64.Rip;
                    //            }
                    //        }
                    //        break;
                    //}
                    if (!excepRecord.IsNullPointer)
                    {
                        excepRecord = excepRecord.Evaluate();
                        //goto the end of the chain
                        //while (true)
                        //{
                        //    excepRecordFields = excepRecord.Fields();
                        //    p = excepRecordFields.GetAt(2);
                        //    if (p.IsNullPointer)
                        //        break;
                        //    excepRecord = p.Evaluate();
                        //}
                        NktParamsEnum excepRecordFields = excepRecord.Fields();
                        code = excepRecordFields.GetAt(0).ULongVal;
                        if (code == 0x40010006) // DBG_PRINTEXCEPTION_C
                        {
                            //ignore OutputDebugString generated exceptions
                            return;
                        }
                        codeString = Declarations.NtStatusToString(code);
                        processedParams.Add(new Param("Code", codeString));

                        flags = excepRecordFields.GetAt(1).ULongVal;
                        if ((flags & NativeApiTools.ExceptionNoncontinuable) != 0)
                        {
                            processedParams.Add(new Param("Flags", "NON_CONTINUABLE"));
                        }
                        if (faultingAddr == ulong.MaxValue)
                        {
                            p = excepRecordFields.GetAt(3);
                            faultingAddr = (!p.IsNullPointer) ? ((ulong)(p.PointerVal.ToInt64())) : 0;
                        }
                        if (faultingAddr != 0)
                            processedParams.Add(new Param("Address", DeviareTools.GetAddressRepresentation(callInfo, (IntPtr)faultingAddr)));
                        e.ParamMainIndex = 0;
                    }
                    break;

                case 2: // NtRaiseHardError
                    e.Function = "RaiseHardError";
                    code = pms.GetAt(0).ULongVal;
                    codeString = Declarations.NtStatusToString(code);
                    processedParams.Add(new Param("Code", codeString));

                    ParseExceptionParams(code, pms.GetAt(1), pms.GetAt(2), pms.GetAt(3), processedParams);
                    e.ParamMainIndex = 0;
                    break;

                case 3:
                case 4:
                    //PEXCEPTION_POINTERS
                    NktParam excepPtr = pms.GetAt(0);
                    if (!excepPtr.IsNullPointer)
                    {
                        excepRecord = excepPtr.Evaluate().Field(0);
                        context = excepPtr.Evaluate().Field(1);
                        goto cont1;
                    }
                    break;
            }

            e.Params = processedParams.ToArray();

            _devRunTrace.ProcessNewEvent(e);
        }

        void ProcessCustomBefore(CallEvent e, IntPtr hookId, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            if (ProcessEvent(true, hookId, callInfo, e, proc))
                _devRunTrace.ProcessNewEvent(e);
        }
        void ProcessCustom(CallEvent e, IntPtr hookId, NktHookCallInfo callInfo, NktProcess proc, HookType hookType, int tag)
        {
            DeviareTools.SetStackInfo(e, callInfo, _modulePath);
            if (ProcessEvent(false, hookId, callInfo, e, proc))
                _devRunTrace.ProcessNewEvent(e);
        }
        #endregion DeviareHandler

        #region ListView
        public void ConnectListView(ListView listView, uint pid)
        {
            lock (_pendingStateChanges)
            {
                _hookStateMgr.AddHooks(listView, pid);
                if (_connectedListViews.Count == 0)
                {
                    _lvTimer.Interval = 1000;
                    _lvTimer.Enabled = true;
                    _lvTimer.Tick += UpdateListViewHookState;
                    _lvTimer.Start();
                }
                if (!_connectedListViews.ContainsKey(pid))
                {
                    _connectedListViews[pid] = new List<ListView>();
                }
                _connectedListViews[pid].Add(listView);
            }
        }

        public void DisconnectListView(ListView lv, uint pid)
        {
            List<ListView> listViews;
            if(_connectedListViews.TryGetValue(pid, out listViews))
            {
                listViews.Remove(lv);
                if (listViews.Count == 0)
                    _connectedListViews.Remove(pid);
            }
            if(_connectedListViews.Count == 0 && _lvTimer.Enabled)
            {
                _lvTimer.Tick -= UpdateListViewHookState;
                _lvTimer.Stop();
            }
        }

        void UpdateListViewHookState(object sender, EventArgs eArgs)
        {
            lock (_pendingStateChanges)
            {
                foreach (var pendingUpdate in _pendingStateChanges)
                {
                    if (_connectedListViews.ContainsKey(pendingUpdate.Pid))
                    {
                        foreach (var lv in _connectedListViews[pendingUpdate.Pid])
                        {
                            _hookStateMgr.ChangeListViewItemHookState(lv, pendingUpdate.Pid, pendingUpdate.HookId, pendingUpdate.NewState);
                        }
                    }
                }
                _pendingStateChanges.Clear();
            }
        }

        #endregion

        private void DoAction(AsyncAction asyncAction)
        {
            try
            {
                switch (asyncAction.ActionType)
                {
                    case AsyncAction.Type.Attach:
                        {
                            var action = (AttachAction)asyncAction;
                            if (action.Hooks != null)
                            {
                                action.Hooks.Attach(action.Proc, true);
                            }
                            if (action.Hook != null)
                                action.Hook.Attach(action.Proc, true);
                            break;
                        }
                    case AsyncAction.Type.Detach:
                        {
                            var action = (DetachAction)asyncAction;
                            if (action.Hooks != null)
                                action.Hooks.Detach(action.Proc, true);
                            if (action.Hook != null)
                                action.Hook.Detach(action.Proc, true);
                            _spyMgr.UnloadAgent(action.Proc);
                            _processModuleHookProcessed.Remove((uint) action.Proc.Id);
                            RemoveAttachedPid(action.Proc.Id, true);
                            break;
                        }
                    case AsyncAction.Type.ResumeProcess:
                        {
                            var action = (ResumeProcessAction)asyncAction;
                            _spyMgr.ResumeProcess(action.ResumeProcessEvent.Value, action.ResumeProcessEvent.Key);
                            UpdateIsMonitoring();
                        }
                        break;
                    case AsyncAction.Type.HookLoadedModulesComServers:
                        {
                            var action = (HookLoadedModulesComServersAction)asyncAction;
                            HookLoadedModulesComServers(action.Proc);
                            UpdateIsMonitoring();
                        }
                        break;
                    case AsyncAction.Type.DetachAll:
                        {
                            if (_spyMgr != null)
                            {
                                var hooks = _spyMgr.Hooks();
                                var hooksToDetach = _spyMgr.CreateHooksCollection();

                                lock (_activeHooks)
                                {
                                    foreach (var hookId in _activeHooks)
                                        hooksToDetach.Add(hooks.GetById(hookId));
                                }

                                lock (_attachedPids)
                                {
                                    foreach (var processId in _attachedPids)
                                    {
                                        hooksToDetach.Detach(processId, true);
                                        _spyMgr.UnloadAgent(processId);
                                    }
                                    _attachedPids.Clear();
                                }

                                ClearObjects();
                            }
                        }
                        break;
                }
            }
            finally
            {
                asyncAction.ActionFinishedEvent.Set();
            }
        }

        private void ClearObjects()
        {
            lock (_activeHooks)
            {
                _activeHooks.Clear();
            }
            lock (_volatileHooks)
            {
                foreach(var hookId in _volatileHooks)
                {
                    var h = _spyMgr.Hooks().GetById(hookId);
                    if(h != null)
                        h.Destroy();
                }
                _volatileHooks.Clear();
            }
            lock (_dllGetClassObjectHookIds)
            {
                _dllGetClassObjectHookIds.Clear();
            }
            lock (_processedLoadedModulesProcessIds)
            {
                _processedLoadedModulesProcessIds.Clear();
            }
            lock (_processModuleHookProcessed)
            {
                _processModuleHookProcessed.Clear();
            }
        }

        public IEnumerable<HookStateMgr.HookInfo> HookStatesForPid(int aPid)
        {
            return _hookStateMgr.GetHookStatesForPid(aPid);
        }

        public void DetachAllProcesses(bool waitCompletion)
        {
            DetachAllProcesses();
            while (_delayedAction.AnyPendingAction())
            {
                Application.DoEvents();
            }
        }

        public void DetachAllProcesses()
        {
            IsMonitoring = false;

            _delayedAction.QueueAction(new DetachAllAction());
        }

    }
}
