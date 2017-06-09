//#define LOG_BUFFERS
//#define DUMP_BUFFERS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using Nektra.Deviare2;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Hooks.Async
{
    enum CustomMessageCodes
    {
        SendBuffer = 1,
        SendMutex = 2, // unused
        SendFirstBuffer = 3,
        GetServerHandle = 4,
        AllowCommandLineHash = 5,
        OpenServiceWasCalled = 6,
        RequestSettings = 7,
        ThinAppCreateProcess = 8,
        RequestExecutableMemory = 9,
        RequestInternetStatusCallbackHook = 10,
        RequestInternetStatusCallbackWHook = 11,
        RequestInternetStatusCallbackUnhook = 12,
        RequestInternetStatusCallbackWUnhook = 13,
    };

    enum NonDeviareEventId
    {
        ThreadExit = 0,
        DotNetProfiling = 1,
    }

    public class AsyncHookMgr
    {
        public class AsyncMessageAction
        {
            public enum Type
            {
                PushBuffer,
                ReceiveMutex,
                ClearEvents
            }

            public ulong Pid;
            public Type ActionType;
            public NktProcess Proc;
            public IntPtr Pointer;
        }

        public class ProcessEventsTrigger
        {
            public StreamSynchronizationPoint SyncPoint;
        }

        public class ClearEventsAction : AsyncMessageAction
        {
            public ClearEventsAction()
            {
                ActionType = Type.ClearEvents;
            }
        }

        readonly Dictionary<string, EventHandler> _strToHandlerMap = new Dictionary<string, Async.EventHandler>();
        readonly Dictionary<string, IntPtr> _strToHookIdMap = new Dictionary<string, IntPtr>();
        readonly Dictionary<IntPtr, EventHandler> _hookIdToHandlerMap = new Dictionary<IntPtr, Async.EventHandler>();

        private readonly List<EventHandler> _nonDeviareEventIdToHandlerMap = new List<Async.EventHandler>
                                                                                       {
                                                                                           new HandleThreadDetach(),
                                                                                           new HandleDotNetProfiling(),
                                                                                       };

        readonly Dictionary<ulong, ProcessState> _asyncStreams = new Dictionary<ulong, ProcessState>();
        private readonly StreamSynchronizationPointWrapper _syncPoint = new StreamSynchronizationPointWrapper();
        public delegate string PostProcessor(string s, CallContext ctx);
        public Dictionary<string, PostProcessor> PostProcessors = new Dictionary<string, PostProcessor>();
        private delegate void CustomMessageHandler(NktProcess proc, object msgCode, object msgParam, ref object retVal);
        readonly Dictionary<CustomMessageCodes, CustomMessageHandler> _customMessageCodeToHandlerMap = new Dictionary<CustomMessageCodes, CustomMessageHandler>();
        readonly QueuedWorkerThread<AsyncMessageAction> _asyncAction;
        readonly QueuedWorkerThread<ProcessEventsTrigger> _eventsProcessor;

#if DUMP_BUFFERS
        public static BufferFileSink BufferFileSink = new BufferFileSink(true);
#else
        public static BufferFileSink BufferFileSink = new BufferFileSink(false);
#endif

        public readonly AgentInitializationParameters AgentInitializationParameters = new AgentInitializationParameters
        {
            ServerPid = (UInt32)Process.GetCurrentProcess().Id,
        };

        const int HandleTimeoutInterval = 1300;

        public HookMgr HookMgr { get; set; }

        public DeviareRunTrace DeviareRunTrace
        {
            get { return HookMgr.DeviareRunTrace; }
        }

        private bool IsExiting;

        public AsyncHookMgr(HookMgr hookMgr)
        {
            HookMgr = hookMgr;
            _asyncAction = new QueuedWorkerThread<AsyncMessageAction>(DoAction, HandleTimeout, HandleTimeoutInterval);
            _eventsProcessor = new QueuedWorkerThread<ProcessEventsTrigger>(ProcessEventsThread);

            InitializeHandlerMap();
            InitializePPMap();
            InitializeCustomMessageHandlerMap();

            _asyncAction.Start();
            _eventsProcessor.Start();
        }

        public void Shutdown()
        {
            IsExiting = true;
            _asyncAction.Stop();
            _eventsProcessor.Stop();
            BufferFileSink.Shutdown();
        }

        private void InitializeHandlerMap()
        {
            _strToHandlerMap["ole32.dll!CoCreateInstance"]                  =
            _strToHandlerMap["ole32.dll!CoCreateInstanceEx"]                = new HandleCoCreateInstance();
            _strToHandlerMap["ntdll.dll!LdrLoadDll"]                        = new HandleLoadLibrary();
            _strToHandlerMap["ntdll.dll!NtOpenKey"]                         =
            _strToHandlerMap["ntdll.dll!NtOpenKeyEx"]                       = new HandleOpenKey();
            _strToHandlerMap["ntdll.dll!NtCreateKey"]                       = new HandleCreateKey();
            _strToHandlerMap["ntdll.dll!NtQueryKey"]                        = new HandleQueryKey();
            _strToHandlerMap["ntdll.dll!NtQueryValueKey"]                   = new HandleQueryValue();
            _strToHandlerMap["ntdll.dll!NtQueryMultipleValueKey"]           = new HandleQueryMultipleValues();
            _strToHandlerMap["ntdll.dll!NtSetValueKey"]                     = new HandleSetValue();
            _strToHandlerMap["ntdll.dll!NtDeleteValueKey"]                  = new HandleDeleteValue();
            _strToHandlerMap["ntdll.dll!NtDeleteKey"]                       = new HandleDeleteKey();
            _strToHandlerMap["ntdll.dll!NtEnumerateValueKey"]               = new HandleEnumerateValueKey();
            _strToHandlerMap["ntdll.dll!NtEnumerateKey"]                    = new HandleEnumerateKey();
            _strToHandlerMap["ntdll.dll!NtRenameKey"]                       = new HandleRenameKey();
            _strToHandlerMap["ntdll.dll!NtCreateFile"]                      = new HandleCreateFile();
            _strToHandlerMap["ntdll.dll!NtOpenFile"]                        = new HandleOpenFile();
            _strToHandlerMap["ntdll.dll!NtDeleteFile"]                      = new HandleDeleteFile();
            _strToHandlerMap["ntdll.dll!NtQueryDirectoryFile"]              = new HandleQueryDirectoryFile();
            _strToHandlerMap["ntdll.dll!NtQueryAttributesFile"]             = new HandleQueryAttributesFile();
            _strToHandlerMap["ntdll.dll!NtRaiseException"]                  = new HandleRaiseException();
            _strToHandlerMap["ntdll.dll!NtRaiseHardError"]                  = new HandleRaiseHardError();
            _strToHandlerMap["ntdll.dll!RtlUnhandledExceptionFilter2"]      =
            _strToHandlerMap["KernelBase.dll!UnhandledExceptionFilter"]     =
            _strToHandlerMap["kernel32.dll!UnhandledExceptionFilter"]       = new HandleUnhandledException();
            _strToHandlerMap["user32.dll!CreateWindowExA"]                  =
            _strToHandlerMap["user32.dll!CreateWindowExW"]                  = new HandleCreateWindowEx();
            _strToHandlerMap["user32.dll!CreateDialogIndirectParamAorW"]    = new HandleCreateDialogIndirectParam();
            _strToHandlerMap["user32.dll!DialogBoxIndirectParamAorW"]       = new HandleDialogBoxIndirectParamParam();
            _strToHandlerMap["KernelBase.dll!CreateProcessInternalW"]       =
            _strToHandlerMap["kernel32.dll!CreateProcessInternalW"]         = new HandleCreateProcessInternal();
            _strToHandlerMap["advapi32.dll!CreateServiceA"]                 =
            _strToHandlerMap["advapi32.dll!CreateServiceW"]                 = new HandleCreateService();
            _strToHandlerMap["advapi32.dll!OpenServiceA"]                   =
            _strToHandlerMap["advapi32.dll!OpenServiceW"]                   = new HandleOpenService();
            _strToHandlerMap["KernelBase.dll!FindResourceExW"]              =
            _strToHandlerMap["kernel32.dll!FindResourceExW"]                = new HandleFindResource();
            _strToHandlerMap["kernel32.dll!LoadResource"]                   = new HandleLoadResource();
            _strToHandlerMap["DllGetClassObject"]                           = new HandleGetClassObject();
            //_strToHandlerMap["Qt5Widgets.dll!?setModel@QTableView@@UAEXPAVQAbstractItemModel@@@Z"] =
            //_strToHandlerMap["Qt5Widgets.dll!??0QTableWidget@@QAE@PAVQWidget@@@Z"] =
            //_strToHandlerMap["Qt5Widgets.dll!??0QTableView@@QAE@PAVQWidget@@@Z"] =
            //_strToHandlerMap["Qt5Widgets.dll!??0QWidget@@QAE@PAV0@V?$QFlags@W4WindowType@Qt@@@@@Z"] =
            //_strToHandlerMap["Qt5Widgets.dll!??0QWidget@@IAE@AAVQWidgetPrivate@@PAV0@V?$QFlags@W4WindowType@Qt@@@@@Z"] =
            //_strToHandlerMap["Qt5Core.dll!??0QObject@@IAE@AAVQObjectPrivate@@PAV0@@Z"] = 
            //_strToHandlerMap["Qt5Core.dll!??0QObject@@QAE@PAV0@@Z"] = new HandleQTObject();
            _strToHandlerMap["custom"]                                      = new HandleCustomHook();
        }

        public string PostProcessHEXINT(string s, CallContext ctx)
        {
            if (Settings.Default.ShowUINTAsHex)
                return "0x" + Convert.ToUInt64(s, CultureInfo.InvariantCulture).ToString("X");
            return s;
        }

        public string PostProcessFILENAME(string s, CallContext ctx)
        {
            return s;
        }

        public string PostProcessBOOLRES(string s, CallContext ctx)
        {
            return (Convert.ToUInt32(s, CultureInfo.InvariantCulture) != 0) ? "SUCCESS" : "FAIL";
        }

        public string PostProcessHRESULT(string s, CallContext ctx)
        {
            return Declarations.HresultErrorToString(Convert.ToUInt32(s, CultureInfo.InvariantCulture));
        }

        public string PostProcessNTSTATUS(string s, CallContext ctx)
        {
            return Declarations.NtStatusToString(Convert.ToUInt32(s, CultureInfo.InvariantCulture));
        }

        public string PostProcessHKEY(string s, CallContext ctx)
        {
            return TextReplacement.RenameKey(s);
        }

        public string PostProcessHMODULE(string s, CallContext ctx)
        {
            return s;
        }
        private void InitializePPMap()
        {
            PostProcessors["HEXINT"]    = PostProcessHEXINT;
            PostProcessors["FILENAME"]  = PostProcessFILENAME;
            PostProcessors["HRESULT"]   = PostProcessHRESULT;
            PostProcessors["NTSTATUS"]  = PostProcessNTSTATUS;
            PostProcessors["BOOLRES"]   = PostProcessBOOLRES;
            PostProcessors["HKEY"]      = PostProcessHKEY;
            PostProcessors["HMODULE"]   = PostProcessHMODULE;
        }

        private void InitializeCustomMessageHandlerMap()
        {
            _customMessageCodeToHandlerMap[CustomMessageCodes.SendBuffer]                           = OnCustomSendBuffer;
            //_customMessageCodeToHandlerMap[CustomMessageCodes.SendMutex]                          = OnCustomSendMutex;
            _customMessageCodeToHandlerMap[CustomMessageCodes.SendFirstBuffer]                      = OnCustomSendFirstBuffer;
            _customMessageCodeToHandlerMap[CustomMessageCodes.GetServerHandle]                      = OnCustomGetServerHandle;
            _customMessageCodeToHandlerMap[CustomMessageCodes.AllowCommandLineHash]                 = OnCustomAllowCommandLineHash;
            _customMessageCodeToHandlerMap[CustomMessageCodes.OpenServiceWasCalled]                 = OnCustomOpenServiceWasCalled;
            _customMessageCodeToHandlerMap[CustomMessageCodes.RequestSettings]                      = OnCustomRequestSettings;
            _customMessageCodeToHandlerMap[CustomMessageCodes.ThinAppCreateProcess]                 = OnCustomThinAppCreateProcess;
            _customMessageCodeToHandlerMap[CustomMessageCodes.RequestExecutableMemory]              = OnCustomRequestExecMemory;
            _customMessageCodeToHandlerMap[CustomMessageCodes.RequestInternetStatusCallbackHook]    = OnCustomRequestInternetStatusCallbackHook;
            _customMessageCodeToHandlerMap[CustomMessageCodes.RequestInternetStatusCallbackWHook]   = OnCustomRequestInternetStatusCallbackWHook;
            _customMessageCodeToHandlerMap[CustomMessageCodes.RequestInternetStatusCallbackUnhook]  = OnCustomRequestInternetStatusCallbackUnhook;
            _customMessageCodeToHandlerMap[CustomMessageCodes.RequestInternetStatusCallbackWUnhook] = OnCustomRequestInternetStatusCallbackWUnhook;
        }

        public string SetUpAgentParameters(XmlNode h, int flags)
        {
            var sw = new StringWriter();
            var writer = new CustomXmlWriter(sw);
            h.WriteTo(writer);
            AgentInitializationParameters.HookFlags = (UInt32)flags;
            AgentInitializationParameters.Xml = sw.GetStringBuilder().ToString();
            return AgentInitializationParameters.Serialize();
        }

        public string SetUpAgentParameters(int flags)
        {
            AgentInitializationParameters.HookFlags = (UInt32)flags;
            return AgentInitializationParameters.Serialize();
        }

        public string SetUpAgentParameters(IntPtr primaryHookId, int flags)
        {
            AgentInitializationParameters.HookFlags = (UInt32)flags | 0x80000000;
            AgentInitializationParameters.PrimaryHook = (UInt64)primaryHookId;
            return AgentInitializationParameters.Serialize();
        }

        public void CreateHook(IntPtr hookId, NktHook h, NktModule mod, string function, HookMgr.HookProperties properties)
        {
            string s = null;
            if (_strToHandlerMap.ContainsKey(h.FunctionName))
            {
                s = h.FunctionName;
                lock (_hookIdToHandlerMap)
                    _hookIdToHandlerMap[hookId] = _strToHandlerMap[h.FunctionName];
            }
            else if (h.FunctionName.EndsWith("!DllGetClassObject"))
            {
                s = "DllGetClassObject";
                lock (_hookIdToHandlerMap)
                    _hookIdToHandlerMap[hookId] = new HandleGetClassObject();
            }
            if (s != null)
                BufferFileSink.AddHookId(h.Id, s, properties);
            lock (_strToHookIdMap)
                _strToHookIdMap[mod.FileName + "!" + function] = hookId;
        }

        public void CreateCustomHook(NktHook hook, string functionName, HookMgr.HookProperties properties)
        {
            string s = functionName.EndsWith("!DllGetClassObject") ? "DllGetClassObject" : "custom";
            var handler = _strToHandlerMap[s];
            BufferFileSink.AddHookId(hook.Id, s, properties);
            lock (_hookIdToHandlerMap)
                _hookIdToHandlerMap[hook.Id] = handler;
        }

        public void CreateVolatileDllGetClassObjectHook(IntPtr hookId, string modPath, HookMgr.HookProperties properties)
        {
            BufferFileSink.AddHookId(hookId, "DllGetClassObject", properties);
            lock (_hookIdToHandlerMap)
                _hookIdToHandlerMap[hookId] = new HandleGetClassObject(modPath);
        }

        public void CreateVolatileSecondaryHook(NktHook hook, HookMgr.HookProperties properties)
        {
            if (_strToHandlerMap.ContainsKey(hook.FunctionName))
            {
                BufferFileSink.AddHookId(hook.Id, hook.FunctionName, properties);
                lock (_hookIdToHandlerMap)
                    _hookIdToHandlerMap[hook.Id] = _strToHandlerMap[hook.FunctionName];
            }
        }

        private void OnCustomSendBuffer(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            var ama = new AsyncMessageAction
            {
                ActionType = AsyncMessageAction.Type.PushBuffer,
                Proc = proc
            };
            if (msgParam is Int32)
                ama.Pointer = (IntPtr)(int)msgParam;
            else
                ama.Pointer = (IntPtr)(long)msgParam;
            ama.Pid = new BufferHeader(ama.Pointer).LongPid;
            _asyncAction.QueueAction(ama);
        }

        private void OnCustomSendFirstBuffer(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            IntPtr buffer;
            if (msgParam is Int32)
                buffer = (IntPtr)(int)msgParam;
            else
                buffer = (IntPtr)(long)msgParam;
            var header = new BufferHeader(buffer);
            var pid = header.LongPid;
            ProcessState state;
            lock (_asyncStreams)
            {
                if (!_asyncStreams.ContainsKey(pid))
                    state = _asyncStreams[pid] = new ProcessState();
                else
                    state = _asyncStreams[pid];
                lock (state)
                {
                    state.NextBuffer = buffer;
                    state.Proc = proc;
                    var mutexHandle = header.OptionalMutex;
                    unchecked
                    {
                        if (IntPtr.Size == 4)
                            state.Mutex = (IntPtr)(int)(long)mutexHandle;
                        else if (IntPtr.Size == 8)
                            state.Mutex = (IntPtr)(long)mutexHandle;
                    }
                }
            }
        }

        private void OnCustomGetServerHandle(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            IntPtr me = Declarations.GetCurrentProcess();
            IntPtr target;
            const int DUPLICATE_SAME_ACCESS = 2;
            var handle = proc.Handle((int)ProcessTools.PROCESS_ALL_ACCESS);
            try
            {
                if (!Declarations.DuplicateHandle(me, me, handle, out target, 0, false, DUPLICATE_SAME_ACCESS))
                    throw new Exception();
                retVal = (proc.PlatformBits == 32) ? (int)target : (long)target;
            }
            finally
            {
                Declarations.CloseHandle(handle);
            }
        }

        private void OnCustomAllowCommandLineHash(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            if (!HookMgr.InstallerMode)
            {
                retVal = 0;
                return;
            }
            retVal = 1;
            uint hash;
            if (msgParam is Int32)
                hash = (uint)(int)msgParam;
            else
                hash = (uint)(long)msgParam;
            HookMgr.AllowCommandLineHash(hash);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SizedString32
        {
            public uint StringSize;
            public Int32 String;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SizedString64
        {
            public uint StringSize;
            public Int64 String;
        }

        public void ReadAndAllowExistingService(string serviceName)
        {
            HookMgr.EnsureServicesAreHooked();
            var valueValue = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\" + serviceName, "ImagePath", null);
            if (!(valueValue is string))
                return;
            HookMgr.AllowableNewServices.Add(StringHash.MultiplicationHash((string)valueValue));
        }

        private void OnCustomRequestSettings(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            UInt32 flags = 0;
            var array = new[]
                            {
                                HookMgr.OmitCallStack,
                                HookMgr.MonitorDotNetGc,
                                HookMgr.MonitorDotNetJit,
                                HookMgr.MonitorDotNetObjectCreation,
                                HookMgr.MonitorDotNetExceptions,
                            };
            uint bit = 1;
            foreach (var b in array)
            {
                if (b)
                    flags |= bit;
                bit <<= 1;
            }
            retVal = flags;
        }

        private void OnCustomOpenServiceWasCalled(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            if (!HookMgr.InstallerMode)
            {
                retVal = 0;
                return;
            }
            retVal = 1;
            IntPtr pointer;
            if (msgParam is Int32)
                pointer = (IntPtr)(int)msgParam;
            else
                pointer = (IntPtr)(long)msgParam;
            IntPtr handle = proc.Handle((int)ProcessTools.PROCESS_ALL_ACCESS);
            string serviceName = null;
            try
            {
                if (proc.PlatformBits == 32)
                {
                    SizedString32 ss;
                    if (!ProcessTools.ReadProcessMemory(out ss, handle, pointer))
                        return;
                    var buffer = new byte[ss.StringSize * 2];
                    uint ignored;
                    var res = Declarations.ReadProcessMemory(handle, (IntPtr)ss.String, buffer, (uint)buffer.Length,
                                                             out ignored);
                    if (res == 0)
                        return;
                    serviceName = Encoding.Unicode.GetString(buffer);
                }
                else if (proc.PlatformBits == 64)
                {
                    SizedString64 ss;
                    if (!ProcessTools.ReadProcessMemory(out ss, handle, pointer))
                        return;
                    var buffer = new byte[ss.StringSize * 2];
                    uint ignored;
                    var res = Declarations.ReadProcessMemory(handle, (IntPtr)ss.String, buffer, (uint)buffer.Length,
                                                             out ignored);
                    if (res == 0)
                        return;
                    serviceName = Encoding.Unicode.GetString(buffer);
                }
            }
            finally
            {
                Declarations.CloseHandle(handle);
            }
            Debug.Assert(serviceName != null);
            ReadAndAllowExistingService(serviceName);
        }

        private void OnCustomThinAppCreateProcess(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            int pid;
            if (msgParam is Int32)
                pid = (int)msgParam;
            else
                pid = (int)(long)msgParam;
            HookMgr.CreateSecondaryHooks(proc, pid);
        }

        private void OnCustomRequestExecMemory(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            uint size;
            if (msgParam is Int32)
                size = (uint)(int)msgParam;
            else
                size = (uint)(long)msgParam;
            var handle = proc.Handle((int)ProcessTools.PROCESS_ALL_ACCESS);
            IntPtr ret;
            try
            {

                ret = Declarations.VirtualAllocEx(handle, IntPtr.Zero, size, Declarations.AllocationType.Commit,
                                            Declarations.MemoryProtection.ExecuteReadWrite);
            }
            finally
            {
                Declarations.CloseHandle(handle);
            }
            retVal = ret.ToInt64();
        }

        private class InternetStatusCallbackHooks
        {
            public IntPtr AnsiHook;
            public IntPtr AnsiFunction;
            public IntPtr WideHook;
            public IntPtr WideFunction;
        }

        private Dictionary<uint, InternetStatusCallbackHooks> _internetStatusCallbackHooks = new Dictionary<uint, InternetStatusCallbackHooks>();

        private IntPtr IntPtrFromMsgParam(object msgParam)
        {
            if (msgParam is Int32)
                return (IntPtr)(int)msgParam;
            return (IntPtr)(long)msgParam;
        }

        public IntPtr HookInternetStatusCallback(NktProcess proc, object msgParam, out IntPtr function)
        {
            function = IntPtrFromMsgParam(msgParam);
            return HookMgr.AddVolatileHook(proc, function, "INTERNET_STATUS_CALLBACK", HookType.InternetStatusCallback, true);
        }

        private InternetStatusCallbackHooks GetInternetStatusCallbackHooks(NktProcess proc, bool create)
        {
            InternetStatusCallbackHooks hooks;
            if (!_internetStatusCallbackHooks.TryGetValue((uint) proc.Id, out hooks))
            {
                if (!create)
                    return null;
                _internetStatusCallbackHooks[(uint) proc.Id] = hooks = new InternetStatusCallbackHooks();
            }
            return hooks;
        }

        private void OnCustomRequestInternetStatusCallbackHook(NktProcess proc, object msgCode, object msgParam,
            ref object retVal)
        {
            InternetStatusCallbackHooks hooks = GetInternetStatusCallbackHooks(proc, true);
            hooks.AnsiHook = HookInternetStatusCallback(proc, msgParam, out hooks.AnsiFunction);
        }

        private void OnCustomRequestInternetStatusCallbackWHook(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            InternetStatusCallbackHooks hooks = GetInternetStatusCallbackHooks(proc, true);
            hooks.WideHook = HookInternetStatusCallback(proc, msgParam, out hooks.AnsiFunction);
        }

        private void UnhookInternetStatusCallback(ref IntPtr intPtr, NktProcess proc, object msgCode, object msgParam)
        {
            var function = IntPtrFromMsgParam(msgParam);

            HookMgr.DeactivateHook(intPtr, proc);
            intPtr = IntPtr.Zero;
        }

        private void OnCustomRequestInternetStatusCallbackUnhook(NktProcess proc, object msgCode, object msgParam,
            ref object retVal)
        {
            InternetStatusCallbackHooks hooks = GetInternetStatusCallbackHooks(proc, false);
            var function = IntPtrFromMsgParam(msgParam);
            if (hooks == null || hooks.AnsiHook == IntPtr.Zero || hooks.AnsiFunction != function)
                return;
            UnhookInternetStatusCallback(ref hooks.AnsiHook, proc, msgCode, msgParam);
        }

        private void OnCustomRequestInternetStatusCallbackWUnhook(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            InternetStatusCallbackHooks hooks = GetInternetStatusCallbackHooks(proc, false);
            var function = IntPtrFromMsgParam(msgParam);
            if (hooks == null || hooks.WideHook == IntPtr.Zero || hooks.WideFunction != function)
                return;
            UnhookInternetStatusCallback(ref hooks.WideHook, proc, msgCode, msgParam);
        }

        public void Deviare_OnCustomMessageEvent(NktProcess proc, object msgCode, object msgParam, ref object retVal)
        {
            CustomMessageCodes code;
            if (msgCode is Int32)
                code = (CustomMessageCodes)(int)msgCode;
            else
                code = (CustomMessageCodes)(long)msgCode;
            CustomMessageHandler handler;
            if (_customMessageCodeToHandlerMap.TryGetValue(code, out handler))
                handler(proc, msgCode, msgParam, ref retVal);
            else
                Debug.Assert(false);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        static extern bool ReleaseMutex(IntPtr hMutex);

        enum WaitForSingleObjectResult
        {
            WaitObject0 = 0,
            WaitAbandoned = 0x80,
            WaitTimeout = 0x102
        }

        private class GlobalMutex : IDisposable
        {
            private IntPtr Mutex;
            public GlobalMutex(IntPtr m, uint timeout)
            {
                Mutex = m;
                WaitForSingleObject(Mutex, timeout);
            }
            public void Dispose()
            {
                ReleaseMutex(Mutex);
            }
        }

        private List<ulong> LockMutexes()
        {
            var ret = new List<ulong>();
            foreach (var s in _asyncStreams)
            {
                var res = (WaitForSingleObjectResult)WaitForSingleObject(s.Value.Mutex, Int32.MaxValue);
                if (res == WaitForSingleObjectResult.WaitAbandoned)
                    ret.Add(s.Key);
            }
            return ret;
        }

        private void UnlockMutexes(List<ulong> list)
        {
            foreach (var s in _asyncStreams)
                ReleaseMutex(s.Value.Mutex);
            foreach (var i in list)
                _asyncStreams.Remove(i);
        }

        private CallContext CreateCallContext(ProcessState state, AsyncStream.Event e, out Async.EventHandler handler)
        {
            NktProcess proc = state.Proc;
            IntPtr hookId = IntPtr.Zero;
            var actualHookId = e.GetString();
            handler = null;
            if (actualHookId != "null")
            {
                if (!Simulating)
                {
                    if (IntPtr.Size == 4)
                        hookId = (IntPtr) Convert.ToInt32(actualHookId);
                    else if (IntPtr.Size == 8)
                    {
                        long l = Convert.ToInt64(actualHookId);
                        ;
                        if (proc.PlatformBits == 32)
                            hookId = (IntPtr) ((ulong) l & 0xFFFFFFFF);
                        else
                            hookId = (IntPtr) l;
                    }
                    if (!_hookIdToHandlerMap.ContainsKey(hookId))
                    {
                        if (!HookMgr.HookIdProps.ContainsKey(hookId))
                        {
#if DEBUG
                            var copy = new Queue<string>(e.Strings);
#endif
                            e.Discard(5);
                            string functionName = e.GetString();
                            Debug.WriteLine("Could not find handler for " + hookId +
                                            ". The hook id is unknown. The salvaged function name is " + functionName);
                            return null;
                        }
                        handler = _strToHandlerMap["custom"];
                    }
                    else
                    {
                        handler = _hookIdToHandlerMap[hookId];
                    }
                }
                else
                {
                    var temp64 = Convert.ToInt64(actualHookId);
                    for (int i = 0; i < 2; i++)
                    {
                        hookId = IntPtrTools.ToIntPtr(temp64);
                        BufferFileSink.QueuedHook hook;
                        if (SimulationHookIds.TryGetValue(temp64, out hook))
                            handler = _strToHandlerMap[hook.Handler];
                        else
                            temp64 &= 0xFFFFFFFF;
                    }
                }
            }
            else
            {
                handler = _nonDeviareEventIdToHandlerMap[e.GetInt()];
            }

            return new CallContext(e, this, proc) { HookId = hookId, CookieToCepDict = state.CookieToCepDict };
        }

        public CallEvent CallEventFromAsyncEvent(ProcessState state, AsyncStream.Event e)
        {
            EventHandler handler;
            var ctx = CreateCallContext(state, e, out handler);
            if (handler == null)
                return null;

            var ret = handler.ProcessEvent(ctx);

            return ret;
        }

        private int HandleTimeout()
        {
            if (DeviareRunTrace == null)
                return HandleTimeoutInterval;
            lock (_asyncStreams)
            {
                var eliminationList = LockMutexes();

                try
                {
                    lock (_syncPoint)
                    {
                        var thereIsAClearSignal = _syncPoint.IsClearSet;

                        foreach (var s in _asyncStreams)
                        {
                            // Flush synchronizes all the streams as a side effect.
                            Flush(s.Value, thereIsAClearSignal);
                        }

                        _eventsProcessor.QueueAction(new ProcessEventsTrigger
                        {
                            SyncPoint = _syncPoint.Get(_asyncStreams)
                        });
                    }
                }
                finally
                {
                    UnlockMutexes(eliminationList);
                    _lastAsyncTimeOut = GetTimestamp();
                }
            }

            return HandleTimeoutInterval;
        }
        
        private const int MaxEventQueue = 20000;

        private void ProcessEventsThread(ProcessEventsTrigger action)
        {
            var pecm = PendingEventsCountManager.GetInstance();

            var merger = new StreamMerger(action.SyncPoint, this);

            var noMoreEvents = false;
            while (!noMoreEvents)
            {
                while (DeviareRunTrace.PendingEventsToAdd() > MaxEventQueue && !IsExiting)
                    Thread.Sleep(100);
                if (IsExiting)
                {
                    merger.DiscardEverything();
                    return;
                }
                var events = new List<CallEvent>();
                while (events.Count < 2000)
                {
                    var e = merger.OneMoreEvent();
                    if (e == null)
                    {
                        noMoreEvents = true;
                        break;
                    }
                    e.CallNumber = CallEvent.GetNextCallNumber();
                    events.Add(e);
                }

                if (events.Count > 0)
                {
                    DeviareRunTrace.ProcessListOfNewEvents(events);
                }
            }
            if (action.SyncPoint.HasClearSignal)
            {
                pecm.EventsEnter(1, PendingEventsCountManager.ProcessNewEventPhase);
                DeviareRunTrace.ProcessNewEvent(CallEvent.CreateDummyEvent(HookType.DummyClear, DeviareRunTrace.TraceId));
                pecm.EventsLeave(1, PendingEventsCountManager.ProcessNewEventPhase);
            }
        }


#if LOG_BUFFERS
        private static int count = 0;
#endif

        public static BufferHeader ReadBufferHeader(ProcessState state)
        {
            try
            {
                return new BufferHeader(state.NextBuffer);
            }
            catch (Exception)
            {
                Declarations.CloseHandle(state.NextBuffer);
                throw;
            }
        }

        private static void AssumeControlOfBuffer(ProcessState state)
        {
            var memory = Declarations.MapViewOfFile(state.NextBuffer, Declarations.FileMapAccess.FileMapAllAccess,
                                                       0, 0,
                                                       (UIntPtr)1);
            var buffer = new byte[1];
            Marshal.Copy(memory, buffer, 0, buffer.Length);
            buffer[0] = 0;
            Marshal.Copy(buffer, 0, memory, buffer.Length);
            Declarations.UnmapViewOfFile(memory);
        }

        private IntPtr PopBufferFromProcess(ProcessState state)
        {
            var ret = state.NextBuffer;
            state.NextBuffer = IntPtr.Zero;
            return ret;
        }

        public static byte[] ReadEntireBuffer(IntPtr handle, uint bufferLength)
        {
            var ret = new byte[bufferLength + BufferHeader.HeaderLength];
            var memory = Declarations.MapViewOfFile(handle, Declarations.FileMapAccess.FileMapAllAccess,
                                                       0, 0,
                                                       (UIntPtr)ret.Length);
            Marshal.Copy(memory, ret, 0, ret.Length);
#if LOG_BUFFERS
            var f = File.Open("incoming_buffer" + count++ + ".txt", FileMode.Create);
            f.Write(ret, 0, ret.Length);
            f.Close();
#endif
            Declarations.UnmapViewOfFile(memory);
            return ret;
        }

        public static byte[] ReadAndDiscardEntireBuffer(IntPtr handle, uint bufferLength)
        {
            var ret = ReadEntireBuffer(handle, bufferLength);
            DiscardBuffer(handle);
            return ret;
        }

        public static void DiscardBuffer(IntPtr handle)
        {
            Declarations.CloseHandle(handle);
        }

        private void Flush(ProcessState state, bool discardData)
        {
            lock (state)
            {
                if (state.NextBuffer == IntPtr.Zero)
                    return;

                var header = ReadBufferHeader(state);

                var bufferLength = header.Length;
                var stopReading = !header.Active || bufferLength == 0;
                if (stopReading)
                    return;

                AssumeControlOfBuffer(state);

                var buffer = PopBufferFromProcess(state);

                BufferFileSink.AddBuffer(buffer);
                var cheapBuffer = new CheapSharedBuffer(buffer, header, discardData);
                _syncPoint.PushBuffer(state, cheapBuffer);
            }
        }

        public static double GetTimestamp()
        {
            return Stopwatch.GetTimestamp() /
                        (double)Stopwatch.Frequency * 1000.0;
        }

        private double _lastAsyncTimeOut = -1;

        private void DoAction(AsyncMessageAction ama)
        {
            lock (_asyncStreams)
            {
                if (ama.Proc != null)
                {
                    var isNew = !_asyncStreams.ContainsKey(ama.Pid);
                    ProcessState state;
                    if (isNew)
                        state = _asyncStreams[ama.Pid] = new ProcessState();
                    else
                        state = _asyncStreams[ama.Pid];
                    lock (state)
                    {
                        switch (ama.ActionType)
                        {
                            case AsyncMessageAction.Type.PushBuffer:
                                if (isNew)
                                    state.NextBuffer = ama.Pointer;
                                else
                                {
                                    using (var am = new GlobalMutex(state.Mutex, Int32.MaxValue))
                                    {
                                        Flush(state, false);
                                        state.NextBuffer = ama.Pointer;
                                    }
                                }
                                break;
                        }
                    }
                }
                else
                {
                    switch (ama.ActionType)
                    {
                        case AsyncMessageAction.Type.ClearEvents:
                            _syncPoint.SetClear();
#if DUMP_BUFFERS
                            BufferFileSink.Clear(true);
#endif
                            break;
                    }
                    if (_lastAsyncTimeOut < 0)
                        return;
                    double now = GetTimestamp();
                    double to = HandleTimeoutInterval - (now - _lastAsyncTimeOut);
                    _asyncAction.Timeout = to < 0 ? 0 : (int)to;
                }
            }
        }

        public void Clear()
        {
            foreach (var v in _asyncStreams)
            {
                v.Value.CookieToCepDict.Clear();
            }
            _asyncAction.QueueAction(new ClearEventsAction());
        }

        public Dictionary<long, BufferFileSink.QueuedHook> SimulationHookIds;

        private bool Simulating
        {
            get { return SimulationHookIds != null; }
        }

        public void StartSimulation()
        {
            _asyncAction.Stop();
            SimulationHookIds = _syncPoint.SetUpSimulation(_asyncStreams).HookIds;
            _eventsProcessor.QueueAction(new ProcessEventsTrigger
            {
                SyncPoint = _syncPoint.Get(_asyncStreams)
            });
        }

        public HookMgr.HookProperties HookPropertiesFromSimulatedHookId(IntPtr pHookId)
        {
            var hookId = IntPtrTools.ToLong(pHookId);
            for (int i = 0; i < 2; i++)
            {
                BufferFileSink.QueuedHook ret;
                if (SimulationHookIds.TryGetValue(hookId, out ret))
                    return ret.Properties;
                hookId &= 0xFFFFFFFF;
            }
            return null;
        }
    }
}
