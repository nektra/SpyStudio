using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using ProtoBuf;
using SpyStudio.Database;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Tools
{
    [Serializable]
    public enum HookType
    {
        Invalid = 0,
        RegOpenKey = 1,
        RegCreateKey = 2,
        RegRenameKey = 3,
        RegQueryValue = 4,
        RegQueryKey = 5,
        RegQueryMultipleValues = 6,
        RegSetValue = 7,
        RegEnumerateValueKey = 8,
        RegEnumerateKey = 9,
        RegDeleteKey = 10,
        RegDeleteValue = 11,
        OpenFile = 12,
        CreateFile = 13,
        DeleteFile = 14,
        QueryDirectoryFile = 15,
        QueryAttributesFile = 16,
        CreateDirectory = 17,
        LoadLibrary = 18,
        FindResource = 19,
        LoadResource = 20,
        CoCreate = 21,
        GetClassObject = 22,
        CreateDialog = 23,
        CreateWindow = 24,
        CreateProcess = 25,
        RaiseException = 26,
        Custom = 27,
        ReadFile = 28,
        WriteFile = 29,
        CreateService = 30,
        OpenService = 31,
        MsiInstallMissingComponent = 32,
        MsiInstallMissingFile = 33,
        MsiInstallProduct = 34,
        DotNetProfiler = 35,
        SetAttributesFile = 36,
        CloseFile = 37,
        ProcessStarted = 38,
        DummyClear = 39, // Not an actual event!
        ThreadDetach = 40, // Doesn't correspond to any actual function.
        InternetStatusCallback = 41,
        HttpAddRequestHeaders = 42,
        //QTObject = 43,
        End = 42,
    }

    public class CallEventArgs : EventArgs
    {
        public CallEventArgs(CallEvent ev)
        {
            Event = ev;
        }

        public CallEventArgs(CallEvent ev, bool filtered)
        {
            Event = ev;
            Filtered = filtered;
        }

        public CallEvent Event { get; private set; }

        public bool Filtered { get; private set; }
    }

    public delegate void CallEventHandler(object sender, CallEventArgs e);

    //[Serializable]
    [ProtoContract]
    public class Param
    {
        private string _value = string.Empty;

        public Param()
        {
            Name = string.Empty;
        }

        public Param(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Value
        {
            get { return _value; }
            set {
                _value = _value == null ? string.Empty : value;
            }
        }
    }

    //[Serializable]
    [ProtoContract]
    public class CallEventId
    {
        // Needed by the database. DO NOT REMOVE.
        public CallEventId()
        {
            
        }

        public CallEventId(UInt64 callNumber, uint traceId)
        {
            CallNumber = callNumber;
            TraceId = traceId;
        }

        public CallEventId(CallEvent ev)
        {
            FromEvent(ev);
        }

        public void FromEvent(CallEvent ev)
        {
            CallNumber = ev.CallNumber;
            TraceId = ev.TraceId;
        }

        [ProtoMember(1)]
        public UInt64 CallNumber { get; set; }
        [ProtoMember(2)]
        public uint TraceId { get; set; }

        public CallEvent Fetch()
        {
            return EventDatabaseMgr.GetInstance().GetEvent(this, false);
        }

        public CallEvent FetchWithStackInfo()
        {
            return EventDatabaseMgr.GetInstance().GetEvent(this, true);
        }

        public bool IsNull
        {
            get { return CallNumber == 0 && TraceId == 0; }
        }

        protected bool Equals(CallEventId other)
        {
            return TraceId == other.TraceId && CallNumber == other.CallNumber;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((CallEventId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) TraceId*397) ^ CallNumber.GetHashCode();
            }
        }
    }


    //[Serializable]
    [ProtoContract]
    public class CallEvent
    {
        public const int MaxPropertyCount = 7;
        public const int MaxPropertyCountBool = 7;
        public const int MaxPropertyCountByteArray = 2;
        public const int MaxPropertyCountUInt = 3;
        public const int MaxPropertyCountString = 6;

        private const int IconByteArrayPropIndex = 0;
        //private const int StackSerializationByteArrayPropIndex = 1;
        //private const int EventSerializationByteArrayPropIndex = 2;

        private string _function;
        private static UInt64 _nextCallNumber = 1;
        private static readonly object LockNextCallNumber = new object();
        private string _paramDetails;
        private HookType _type = HookType.Invalid;
        private Image _image;

        public static UInt64 GetNextCallNumber()
        {
            lock (LockNextCallNumber)
            {
                return ++_nextCallNumber;
            }
        }

        public static UInt64 PeekLastCallNumber()
        {
            lock (LockNextCallNumber)
            {
                return _nextCallNumber;
            }
        }

        public static void SetLastCallNumber(UInt64 lastCallNumber)
        {
            lock (LockNextCallNumber)
            {
                _nextCallNumber = lastCallNumber;
            }
        }

        public void SetCallerInfo(string caller, List<DeviareTools.DeviareStackFrame> stackTrace)
        {
            CallModule = caller;
            CallStack = stackTrace;
        }

        public static void ResetLastCallNumber()
        {
            lock (LockNextCallNumber)
            {
                _nextCallNumber = 1;
            }
        }

        public CallEvent()
        {
            Ancestors = new List<ulong>();
            //ProcessName = "";
            //Init(0, HookType.Invalid, 0, 0, "", null, 0, 0, 0);
            //Before = true;
        }

        public static CallEvent CreateDummyEvent(HookType type, uint traceId)
        {
            var ret = new CallEvent
                          {
                              EventId = new CallEventId(GetNextCallNumber(), traceId),
                              Type = type,
                              TimeStamp = 0,
                          };
            return ret;
        }
               
        public CallEvent(bool generateCallNumber)
        {
            ProcessName = string.Empty;
            Init(generateCallNumber ? GetNextCallNumber() : 0, HookType.Invalid, 0, 0, string.Empty, null, 0, 0, 0);
            Before = true;
        }

        public CallEvent(HookType type, uint cookie, UInt64 retVal, string module,
                         List<DeviareTools.DeviareStackFrame> stackTrace, double time, uint pid, uint tid)
        {
            ProcessName = string.Empty;
            Init(GetNextCallNumber(), type, cookie, retVal, module, stackTrace, time, pid, tid);
            Before = false;
        }

        public CallEvent(UInt64 callNumber, bool before, HookType type, uint cookie, UInt64 retVal, string module,
                         List<DeviareTools.DeviareStackFrame> stackTrace, double time, uint pid, uint tid)
        {
            ProcessName = string.Empty;
            Init(callNumber, type, cookie, retVal, module, stackTrace, time, pid, tid);
            Before = before;
        }

        public CallEvent(HookType type, uint cookie, uint pid, uint tid)
        {
            ProcessName = string.Empty;
            Init(GetNextCallNumber(), type, cookie, 0, string.Empty, null, 0, pid, tid);
            Before = true;
        }

        public CallEvent(HookType type, uint cookie, uint pid, uint tid, string function)
        {
            ProcessName = string.Empty;
            Function = function;
            Init(GetNextCallNumber(), type, cookie, 0, string.Empty, null, 0, pid, tid);
            Before = true;
        }

        static public double TimeProperties, TimeRest;
        private void Init(UInt64 callNumber, HookType type, uint cookie, UInt64 retVal, string module,
                          List<DeviareTools.DeviareStackFrame> stackTrace, double time, uint pid, uint tid)
        {
            InitBasic(callNumber, type, cookie, retVal, module, stackTrace, time, pid, tid);

            Ancestors = new List<ulong>();
            EventId = new CallEventId(callNumber, 0);
            ProcessName =
                ProcessPath = Result = StackTraceString = NearestSymbol = Win32Function = string.Empty;
            Time = time;
            Pid = pid;
            Tid = tid;
            Type = type;
            IsGenerated = false;
            ParamMainIndex = -1;
        }

        public CallEvent(uint traceId, UInt64 callNumber, UInt64 peer, int priority, string processName, EventFlags eventFlags, UInt64 retValue, 
                         string functionName, string win32Function,
                         HookType hookType, uint chainDepth, uint cookie, double time, double generationTime, int paramMainIndex, uint pid, uint tid, string callModule,
                         string stackTraceString, string result, bool success, List<UInt64> ancestors)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif

            EventFlags = eventFlags;
            InitBasic(callNumber, hookType, Cookie, retValue, callModule, null, time, pid, tid);
#if DEBUG
            TimeProperties += sw.Elapsed.TotalMilliseconds;
            var previous = sw.Elapsed.TotalMilliseconds;
#endif
            EventId = new CallEventId(callNumber, traceId);
            Ancestors = ancestors;
            Peer = peer;
            _function = functionName;
            Win32Function = win32Function;
            ChainDepth = chainDepth;
            GenerationTime = generationTime;
            Priority = priority;
            Cookie = cookie;
            Success = success;
            ProcessName = processName;
            ProcessPath = string.Empty;
            Result = result;
            StackTraceString = stackTraceString;
            Win32Function = win32Function;
            ParamMainIndex = paramMainIndex;

#if DEBUG
            TimeRest += sw.Elapsed.TotalMilliseconds - previous;
#endif
        }
        private void InitBasic(UInt64 callNumber, HookType type, uint cookie, UInt64 retVal, string module,
                          List<DeviareTools.DeviareStackFrame> stackTrace, double time, uint pid, uint tid)
        {
            PropertiesString = new string[MaxPropertyCountString];
            PropertiesUInt64 = new UInt64[MaxPropertyCountUInt];
            PropertiesBool = new bool[MaxPropertyCountBool];
            PropertiesByteArray = new byte[MaxPropertyCountByteArray][];

            Cookie = cookie;
            CallModule = module;
            CallStack = stackTrace;
            RetValue = retVal;
            Time = time;
            Pid = pid;
            Tid = tid;
            Type = type;
        }

        [ProtoMember(1)]
        public CallEventId EventId { get; private set; }

        public uint TraceId
        {
            get { return EventId.TraceId; }
            set { EventId.TraceId = value; }
        }

        public UInt64 CallNumber
        {
            get { return EventId.CallNumber; }
            set { EventId.CallNumber = value; }
        }

        [ProtoMember(2)]
        public uint Pid { get; set; }

        [ProtoMember(3)]
        public string ProcessPath { get; set; }

        [ProtoMember(4)]
        public bool Virtualized { get; set; }

        [ProtoMember(5)]
        public uint Tid { get; set; }

        [ProtoMember(6)]
        public string ProcessName { get; set; }

        [ProtoMember(7)]
        public string CallModule { get; set; }

        [ProtoMember(8)]
        public string Win32Function { get; set; }

        /// <summary>
        /// Critical events are exceptions or something that can make the application crash.
        /// </summary>
        [ProtoMember(9)]
        public bool Critical { get; set; }

        /// <summary>
        /// Events with high priority (1) are the most important and low priority (5) less important.
        /// </summary>
        [ProtoMember(10)]
        public int Priority { get; set; }

        /// <summary>
        /// When true the event was generated. Non generated events are calls handled. On the other hand, generated events are generated by 
        /// this application.
        /// </summary>
        [ProtoMember(11)]
        public bool IsGenerated { get; set; }

        /// <summary>
        /// Get peer CallEvent call number. When a function generates 2 CallEvent (after and before the function is executed) the Peer is the other.
        /// </summary>
        [ProtoMember(12)]
        public UInt64 Peer { get; set; }

        /// <summary>
        /// Get peer CallEvent call number. When a function generates 2 CallEvent (after and before the function is executed) the Peer is the other.
        /// </summary>
        public UInt64 Parent
        {
            get { return Ancestors.Count > 0 ? Ancestors.First() : 0; }
        }

        /// <summary>
        /// Get peer CallEvent call number. When a function generates 2 CallEvent (after and before the function is executed) the Peer is the other.
        /// </summary>
        [ProtoMember(13)]
        public List<UInt64> Ancestors { get; set; }

        /// <summary>
        /// When true the event was loaded from a log.
        /// </summary>
        //public bool IsLoaded
        //{
        //    get { return Properties.ContainsKey("IsLoaded") && (bool) Properties["IsLoaded"]; }
        //    set { Properties["IsLoaded"] = value; }
        //}
        [ProtoMember(14)]
        public bool IsProcMon { get; set; }

        [ProtoMember(15)]
        public string NearestSymbol { get; set; }

        private string _stackTraceString;

        [ProtoMember(16)]
        public string StackTraceString
        {
            get { return _stackTraceString; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var index = value.IndexOf(' ');
                    if (index != -1)
                    {
                        NearestSymbol = value.Substring(0, index);
                    }
                    else
                    {
                        NearestSymbol = value;
                    }
                }
                else
                {
                    NearestSymbol = string.Empty;
                }
                _stackTraceString = value;
            }
        }

        private List<DeviareTools.DeviareStackFrame> _callStack;

        [ProtoMember(17)]
        public List<DeviareTools.DeviareStackFrame> CallStack
        {
            get { return _callStack; }
            set
            {
                _callStack = value;
                //if(_callStack != null)
                //{
                    //var sf = DeviareTools.GetNonSystemStackFrame(_callStack); //GetStackFrame();
                    //NearestSymbol = sf == null ? string.Empty : sf.NearestSymbol;
                    //StackTraceString = sf == null ? string.Empty : sf.StackTraceString;
                //}
            }
        }

        public void CleanStack()
        {
            _callStack = null;
        }

        /// <summary>
        /// Get those frames that doesn't point to a system module
        /// </summary>
        public List<DeviareTools.DeviareStackFrame> GetNonSystemStack()
        {
            List<DeviareTools.DeviareStackFrame> ret = null;
            if (CallStack != null)
            {
                ret = CallStack.Where(frame => !DeviareTools.SystemModules.Contains(frame.ModuleName)).ToList();
            }
            return ret;
        }

        [NonSerialized] private object _tag;

        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        [ProtoMember(18)]
        public bool Success { get; set; }

        [ProtoMember(19)]
        public UInt64 RetValue { get; set; }

        [ProtoMember(50)]
        public HookType Type
        {
            get { return _type; }
            set
            {
                _type = value;
                if(!TypeProcessed)
                {
                    TypeProcessed = true;
                    switch (Type)
                    {
                        case HookType.Invalid:
                            TypeProcessed = false;
                            break;
                        case HookType.CoCreate:
                        case HookType.GetClassObject:
                            IsCom = true;
                            break;
                        case HookType.RegEnumerateValueKey:
                        case HookType.RegQueryValue:
                        case HookType.RegQueryMultipleValues:
                        case HookType.RegSetValue:
                        case HookType.RegDeleteValue:
                        case HookType.RegEnumerateKey:
                        case HookType.RegCreateKey:
                        case HookType.RegQueryKey:
                        case HookType.RegOpenKey:
                            IsRegistry = true;
                            break;
                        case HookType.LoadLibrary:
                        case HookType.FindResource:
                        case HookType.OpenFile:
                        case HookType.CreateFile:
                        case HookType.ReadFile:
                        case HookType.QueryDirectoryFile:
                        case HookType.WriteFile:
                        case HookType.QueryAttributesFile:
                        case HookType.SetAttributesFile:
                        case HookType.CreateDirectory:
                        case HookType.CreateProcess:
                        case HookType.ProcessStarted:
                        case HookType.CloseFile:
                            IsFileSystem = true;
                            break;
                        case HookType.CreateDialog:
                        case HookType.CreateWindow:
                            IsWindow = true;
                            break;
                        case HookType.CreateService:
                        case HookType.OpenService:
                            IsServices = true;
                            break;
                        default:
                            {
                                if (Function == "Ole32.CoGetClassObject" || Function == "RpcRT4.NdrDllGetClassObject")
                                {
                                    IsCom = true;
                                }
                                else if (Function == "LoadResource")
                                {
                                    IsFileSystem = true;
                                    FileSystemEvent.SetAccess(this, FileSystemEvent.GetAccess(this) | FileSystemAccess.Resource);
                                    if (ParamMain.StartsWith("0x"))
                                        FileSystemEvent.SetModuleNotFound(this, true);
                                }
                                break;
                            }
                    }
                }
            }
        }

        [ProtoMember(51)]
        public string Function
        {
            get
            {
                return _function ?? Type.ToString();
            }
            set
            {
                //Debug.Assert(!string.IsNullOrEmpty(value), "Got a call event with no function name.");
                _function = value;
                // WORKAROUND: some type behavior is related to the Function so update it.
                Type = Type;
                //if (Type == HookType.Custom && _function == "Ole32.CoGetClassObject")
                //{
                //    IsCom = true;
                //}
            }
        }

        [ProtoMember(52)]
        public uint ChainDepth { get; set; }

        [ProtoMember(21)]
        public double Time { get; set; }

        [ProtoMember(22)]
        public double GenerationTime { get; set; }

        //System-wide timestamp used to sort asynchronous events.
        public double TimeStamp { get; set; }

        /// <summary>
        /// call cookie to identify both parts of the call: before and after.
        /// </summary>
        [ProtoMember(23)]
        public uint Cookie { get; set; }

        [ProtoMember(24)]
        public bool Before { get; set; }

        [ProtoMember(25)]
        public bool OnlyBefore { get; set; }

        [ProtoMember(26)]
        public string Result { get; set; }

        public bool IsFiltered { get; set; }

        public void CreateParams(Param[] pms)
        {
            Params = pms;
            if (ParamCount > 0)
                ParamMainIndex = 0;
        }

        public void CreateParams(int count)
        {
            if (count > 0)
            {
                ParamMainIndex = 0;
                Params = new Param[count];
                for (var i = 0; i < count; i++)
                {
                    Params[i] = new Param();
                }
            }
            else
            {
                Params = null;
            }
        }

        public int ParamCount
        {
            get { return Params == null ? 0 : Params.Length; }
        }

        public Image GetIcon()
        {
            if(_image == null)
            {
                var imageArray = PropertiesByteArray[IconByteArrayPropIndex];
                if (imageArray != null)
                    _image = ImageTools.ByteArrayToImage(imageArray);
            }
            return _image;
        }

        public void SetIcon(Image icon)
        {
            PropertiesByteArray[IconByteArrayPropIndex] = icon != null ? ImageTools.ImageToByteArray(icon) : null;
            _image = icon;
        }

        //public byte[] GetSerializedStack()
        //{
        //    return PropertiesByteArray[StackSerializationByteArrayPropIndex];
        //}

        //public void SetSerializedStack(byte[] serializedStack)
        //{
        //    PropertiesByteArray[StackSerializationByteArrayPropIndex] = serializedStack;
        //}

        //public byte[] GetSerializedEvent()
        //{
        //    return PropertiesByteArray[EventSerializationByteArrayPropIndex];
        //}

        //public void SetSerializedEvent(byte[] serializedEvent)
        //{
        //    PropertiesByteArray[EventSerializationByteArrayPropIndex] = serializedEvent;
        //}

        [ProtoMember(27)]
        public Param[] Params { get; set; }

        [ProtoMember(28)]
        public int ParamMainIndex { get; set; }

        //[ProtoMember(29)]
        public string[] PropertiesString { get; set; }

        //[ProtoMember(30)]
        public bool[] PropertiesBool { get; set; }

        //[ProtoMember(32)]
        public UInt64[] PropertiesUInt64 { get; set; }

        //[ProtoMember(33)]
        public byte[][] PropertiesByteArray { get; set; }

        public string[] CallStackStrings { get; set; }

        public string GetParamValue(int index)
        {
            return Params[index].Value;
        }

        public string GetParamName(int index)
        {
            return Params[index].Name;
        }

        public string GetPropertiesString(int index)
        {
            Debug.Assert(index < PropertiesString.Length);
            return PropertiesString[index];
        }
        public bool GetPropertiesBool(int index)
        {
            Debug.Assert(index < PropertiesBool.Length);
            return PropertiesBool[index];
        }
        //public uint GetPropertiesUInt(int index)
        //{
        //    Debug.Assert(index < PropertiesUInt.Length);
        //    return PropertiesUInt[index];
        //}
        public UInt64 GetPropertiesUInt64(int index)
        {
            Debug.Assert(index < PropertiesUInt64.Length);
            return PropertiesUInt64[index];
        }
        public byte[] GetPropertiesByteArray(int index)
        {
            Debug.Assert(index < PropertiesByteArray.Length);
            return PropertiesByteArray[index];
        }
        public bool NullParams
        {
            get { return Params == null || Params.Length == 0; }
        }

        public string ParamMain
        {
            get
            {
                if (ParamMainIndex != -1 && ParamCount > ParamMainIndex)
                {
                    return Params[ParamMainIndex].Value;
                }
                return String.Empty;
            }
            set
            {
                Debug.Assert(ParamMainIndex != -1 && ParamCount > ParamMainIndex);
                Params[ParamMainIndex].Value = value;
            }
        }

        public string ParamDetails
        {
            get
            {
                if (_paramDetails == null)
                {
                    var paramDetails = new StringBuilder();
                    if (Params != null)
                    {
                        for (var i = 0; i < Params.Length; i++)
                        {
                            var prefix = new StringBuilder();
                            if (paramDetails.Length > 0)
                            {
                                prefix.Append(" ");
                            }
                            if (i != ParamMainIndex)
                            {
                                if (!string.IsNullOrEmpty(Params[i].Value))
                                {
                                    if (!string.IsNullOrEmpty(Params[i].Name))
                                    {
                                        prefix.Append(Params[i].Name);
                                        prefix.Append(": ");
                                    }
                                    if (paramDetails.Length + prefix.Length + Params[i].Value.Length <= 1024)
                                    {
                                        paramDetails.Append(prefix);
                                        paramDetails.Append(Params[i].Value);
                                    }
                                }
                            }
                        }
                    }
                    _paramDetails = paramDetails.ToString();
                }

                return _paramDetails;
            }
            set { _paramDetails = value; }
        }

        public void CleanParams()
        {
            Params = null;
            _paramDetails = string.Empty;
        }
        public string GetParamValueByName(string name)
        {
            var ret = "";
            var i = 0;
            foreach (var n in Params)
            {
                if (n.Name == name)
                {
                    ret = Params[i].Value;
                    break;
                }
                i++;
            }
            return ret;
        }

        public Param GetParamByName(string name)
        {
            Param ret = null;
            var i = 0;
            foreach (var n in Params)
            {
                if (n.Name == name)
                {
                    ret = Params[i];
                    break;
                }
                i++;
            }
            return ret;
        }

        public bool TypeProcessed { get; set; }

        public bool IsRegistry { get; set; }

        public bool IsFileSystem { get; set; }

        public bool IsCom { get; set; }

        public bool IsWindow { get; set; }

        public bool IsServices { get; set; }

        public DeviareTools.DeviareStackFrame GetStackFrame()
        {
            DeviareTools.DeviareStackFrame ret = null;
            var frames = GetNonSystemStack();
            if (frames != null && frames.Any())
            {
                ret = frames.First();
            }
            else
            {
                frames = CallStack;
                if (frames != null && frames.Any())
                {
                    ret = frames.First();
                }
            }
            return ret;
        }

        public string ToBase64()
        {
            var ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, this);
            var ret = Convert.ToBase64String(ms.ToArray());
            ms.Close();
            return ret;
        }
        public static CallEvent FromBase64(byte[] buffer)
        {
            return FromBase64(Encoding.ASCII.GetString(buffer));
        }
        public static CallEvent FromBase64(string base64)
        {
            return FromSerializedBuffer(Convert.FromBase64String(base64));
        }
        public static CallEvent FromSerializedBuffer(byte[] buffer)
        {
            var ms = new MemoryStream(buffer, 0, buffer.Length) {Position = 0};
            //IINM, the above constructor already performs this next line
            //ms.Write(buffer, 0, buffer.Length);
            return new BinaryFormatter().Deserialize(ms) as CallEvent;
        }

        public virtual XmlNode ToXml(XmlDocument doc)
        {
            var evt = doc.CreateElement("event");

            var e = doc.CreateElement("callNumber");
            e.InnerText = CallNumber.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);
            e = doc.CreateElement("cookie");
            e.InnerText = Cookie.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);
            e = doc.CreateElement("peer");
            e.InnerText = Peer.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);
            e = doc.CreateElement("parent");
            e.InnerText = Parent.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);
            e = doc.CreateElement("callModule");
            e.InnerText = CallModule;
            evt.AppendChild(e);

            e = doc.CreateElement("ancestors");
            foreach (var ancestor in Ancestors)
            {
                var n = doc.CreateElement("ancestor");
                n.InnerText = ancestor.ToString(CultureInfo.InvariantCulture);
                e.AppendChild(n);
            }
            evt.AppendChild(e);

            e = doc.CreateElement("callStack");
            if (CallStack != null)
            {
                foreach (var frame in CallStack)
                {
                    var frameNode = doc.CreateElement("frame");
                    e.AppendChild(frameNode);

                    var n = doc.CreateElement("eip");
                    n.InnerText = frame.Eip.ToString("X");
                    frameNode.AppendChild(n);

                    n = doc.CreateElement("modulePath");
                    n.InnerText = frame.ModulePath;
                    frameNode.AppendChild(n);

                    n = doc.CreateElement("moduleName");
                    n.InnerText = frame.ModuleName;
                    frameNode.AppendChild(n);

                    n = doc.CreateElement("moduleAddress");
                    n.InnerText = frame.ModuleAddress.ToString("X");
                    frameNode.AppendChild(n);

                    n = doc.CreateElement("nearestSymbol");
                    n.InnerText = frame.NearestSymbol;
                    frameNode.AppendChild(n);

                    n = doc.CreateElement("offset");
                    n.InnerText = frame.Offset.ToString("X");
                    frameNode.AppendChild(n);

                    n = doc.CreateElement("stackTraceString");
                    n.InnerText = frame.StackTraceString;
                    frameNode.AppendChild(n);
                }
            }
            evt.AppendChild(e);

            e = doc.CreateElement("success");
            e.InnerText = Success.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("critical");
            e.InnerText = Critical.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("priority");
            e.InnerText = Priority.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("generated");
            e.InnerText = IsGenerated.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("procMon");
            e.InnerText = IsProcMon.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("retValue");
            e.InnerText = RetValue.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("time");
            e.InnerText = Time.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("generationTime");
            e.InnerText = GenerationTime.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("pid");
            e.InnerText = Pid.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("virtualizedCall");
            e.InnerText = Virtualized.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("processName");
            e.InnerText = ProcessName.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("tid");
            e.InnerText = Tid.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("type");
            e.InnerText = Type.ToString();
            evt.AppendChild(e);

            e = doc.CreateElement("function");
            e.InnerText = Function;
            evt.AppendChild(e);

            e = doc.CreateElement("win32Function");
            e.InnerText = Win32Function;
            evt.AppendChild(e);

            e = doc.CreateElement("before");
            e.InnerText = Before.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("onlyBefore");
            e.InnerText = OnlyBefore.ToString(CultureInfo.InvariantCulture);
            evt.AppendChild(e);

            e = doc.CreateElement("error");
            e.InnerText = Result;
            evt.AppendChild(e);

            var pms = doc.CreateElement("params");
            evt.AppendChild(pms);

            e = doc.CreateElement("main");
            pms.AppendChild(e);
            e.InnerText = ParamMainIndex.ToString(CultureInfo.InvariantCulture);

            if (Params != null)
            {
                foreach (var t in Params)
                {
                    var p = doc.CreateElement("param");
                    pms.AppendChild(p);

                    e = doc.CreateElement("name");
                    p.AppendChild(e);
                    e.InnerText = t.Name;
                    e = doc.CreateElement("value");
                    p.AppendChild(e);
                    e.InnerText = StringHelpers.RemoveTroublesomeCharacters(t.Value);
                }
            }

            pms = doc.CreateElement("properties");
            evt.AppendChild(pms);

                foreach (var prop in PropertiesString)
                {
                    var p = doc.CreateElement("propertyString");
                    pms.AppendChild(p);

                    // convert object to string
                    var ms = new MemoryStream();
                    new BinaryFormatter().Serialize(ms, prop ?? "");
                    p.InnerText = Convert.ToBase64String(ms.ToArray());
                    ms.Close();
                }
                foreach (var prop in PropertiesBool)
                {
                    var p = doc.CreateElement("propertyBool");
                    pms.AppendChild(p);

                    p.InnerText = prop ? "true" : "false";
                }
            //if (PropertiesUInt != null)
            //{
            //    foreach (var prop in PropertiesUInt)
            //    {
            //        var p = doc.CreateElement("propertyUInt");
            //        pms.AppendChild(p);

            //        // convert object to string
            //        var ms = new MemoryStream();
            //        new BinaryFormatter().Serialize(ms, prop);
            //        p.InnerText = Convert.ToBase64String(ms.ToArray());
            //        ms.Close();
            //    }
            //}
            if (PropertiesUInt64 != null)
            {
                foreach (var prop in PropertiesUInt64)
                {
                    var p = doc.CreateElement("propertyUInt64");
                    pms.AppendChild(p);

                    // convert object to string
                    var ms = new MemoryStream();
                    new BinaryFormatter().Serialize(ms, prop);
                    p.InnerText = Convert.ToBase64String(ms.ToArray());
                    ms.Close();
                }
            }
            if (PropertiesByteArray != null)
            {
                foreach (var prop in PropertiesByteArray)
                {
                    var p = doc.CreateElement("propertyByteArray");
                    pms.AppendChild(p);

                    if(prop != null)
                    {
                        // convert object to string
                        var ms = new MemoryStream();
                        new BinaryFormatter().Serialize(ms, prop);
                        p.InnerText = Convert.ToBase64String(ms.ToArray());
                        ms.Close();
                    }
                    else
                    {
                        p.InnerText = string.Empty;
                    }
                }
            }

            return evt;
        }

        public static CallEvent FromXml(XmlNode node)
        {
            CallEvent item = null;
            XmlNode nodeType = node["type"];
            if (nodeType != null)
            {
                var fromString = GetHookTypeFromString(nodeType.InnerText);
                Debug.Assert(fromString != HookType.Invalid);

                item = new CallEvent(false) {Type = fromString};
                XmlNode n = node["callNumber"];
                if (n != null)
                {
                    item.CallNumber = Convert.ToUInt64(n.InnerText, CultureInfo.InvariantCulture);
                }
                n = node["pid"];
                if (n != null)
                    item.Pid = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                n = node["virtualizedCall"];
                if (n != null)
                    item.Virtualized = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);
                n = node["processName"];
                if (n != null)
                    item.ProcessName = n.InnerText;
                n = node["tid"];
                if (n != null)
                    item.Tid = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                n = node["callModule"];
                if (n != null)
                    item.CallModule = n.InnerText;
                n = node["function"];
                if (n != null)
                    item.Function = n.InnerText;
                n = node["win32Function"];
                if (n != null)
                    item.Win32Function = n.InnerText;
                n = node["success"];
                if (n != null)
                    item.Success = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);
                n = node["critical"];
                if (n != null)
                    item.Critical = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);
                n = node["priority"];
                item.Priority = n != null ? Convert.ToInt32(n.InnerText, CultureInfo.InvariantCulture) : 5;

                n = node["generated"];
                if (n != null)
                    item.IsGenerated = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);
                n = node["procMon"];
                if (n != null)
                    item.IsProcMon = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);
                n = node["retValue"];
                if (n != null)
                    item.RetValue = Convert.ToUInt64(n.InnerText, CultureInfo.InvariantCulture);
                n = node["time"];
                if (n != null)
                    item.Time = Convert.ToDouble(n.InnerText, CultureInfo.InvariantCulture);
                n = node["generationTime"];
                if (n != null)
                    item.GenerationTime = Convert.ToDouble(n.InnerText, CultureInfo.InvariantCulture);
                n = node["cookie"];
                if (n != null)
                    item.Cookie = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                n = node["peer"];
                if (n != null)
                    item.Peer = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                n = node["ancestors"];
                if (n != null)
                {
                    foreach (XmlNode ancestor in n.ChildNodes)
                    {
                        item.Ancestors.Add(Convert.ToUInt64(ancestor.InnerText, CultureInfo.InvariantCulture));
                    }
                }
                n = node["before"];
                if (n != null)
                    item.Before = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);
                n = node["onlyBefore"];
                if (n != null)
                    item.OnlyBefore = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);

                item.CallStack = null;
                n = node["callStack"];
                if (n != null)
                {
                    item.CallStack = new List<DeviareTools.DeviareStackFrame>();

                    foreach (XmlNode frameNode in n.ChildNodes)
                    {
                        string nearestSymbol = "", modulePath = "", moduleName = "";
                        UInt64 eip = 0, offset = 0, moduleAddress = 0;

                        XmlNode frameChildNode = frameNode["eip"];
                        if (frameChildNode != null)
                        {
                            eip = UInt64.Parse(frameChildNode.InnerText, NumberStyles.AllowHexSpecifier);
                        }
                        frameChildNode = frameNode["modulePath"];
                        if (frameChildNode != null)
                        {
                            modulePath = frameChildNode.InnerText;
                        }
                        frameChildNode = frameNode["moduleName"];
                        if (frameChildNode != null)
                        {
                            moduleName = frameChildNode.InnerText;
                        }
                        frameChildNode = frameNode["offset"];
                        if (frameChildNode != null)
                        {
                            offset = UInt64.Parse(frameChildNode.InnerText, NumberStyles.AllowHexSpecifier);
                        }
                        frameChildNode = frameNode["moduleAddress"];
                        if (frameChildNode != null)
                        {
                            moduleAddress = UInt64.Parse(frameChildNode.InnerText, NumberStyles.AllowHexSpecifier);
                        }
                        frameChildNode = frameNode["nearestSymbol"];
                        if (frameChildNode != null)
                        {
                            nearestSymbol = frameChildNode.InnerText;
                        }

                        var frame = new DeviareTools.DeviareStackFrame(modulePath, moduleName, nearestSymbol, eip,
                                                                       offset,
                                                                       moduleAddress);
                        item.CallStack.Add(frame);
                    }
                }

                n = node["error"];
                if (n != null)
                    item.Result = n.InnerText;

                n = node["params"];
                if (n != null)
                {
                    var parameters = new List<Param>();

                    var pms = n.SelectNodes("param");

                    if (pms != null)
                    {
                        foreach (XmlNode p in pms)
                        {
                            var param = new Param();
                            parameters.Add(param);

                            XmlNode n1 = p["name"];
                            if (n1 != null)
                            {
                                param.Name = n1.InnerText;
                            }

                            n1 = p["value"];
                            if (n1 != null)
                            {
                                param.Value = n1.InnerText;
                            }
                        }
                        item.Params = parameters.ToArray();
                    }
                }

                n = node["properties"];
                if (n != null)
                {
                    var props = n.SelectNodes("propertyString");
                    if (props != null)
                    {
                        int i = 0;
                        foreach (XmlNode p in props)
                        {
                            var bytes = Convert.FromBase64String(p.InnerText);
                            var ms = new MemoryStream(bytes, 0, bytes.Length);
                            ms.Write(bytes, 0, bytes.Length);
                            ms.Position = 0;
                            item.PropertiesString[i++] = (string) new BinaryFormatter().Deserialize(ms);
                        }
                    }
                    props = n.SelectNodes("propertyBool");

                    if (props != null)
                    {
                        int i = 0;
                        foreach (XmlNode p in props)
                        {
                            item.PropertiesBool[i++] = p.InnerText == "true";
                        }
                    }
                    //props = n.SelectNodes("propertyUInt");

                    //if (props != null)
                    //{
                    //    int i = 0;
                    //    foreach (XmlNode p in props)
                    //    {
                    //        var bytes = Convert.FromBase64String(p.InnerText);
                    //        var ms = new MemoryStream(bytes, 0, bytes.Length);
                    //        ms.Write(bytes, 0, bytes.Length);
                    //        ms.Position = 0;
                    //        item.PropertiesUInt[i++] = (uint) new BinaryFormatter().Deserialize(ms);
                    //    }
                    //}
                    props = n.SelectNodes("propertyUInt64");

                    if (props != null)
                    {
                        int i = 0;
                        foreach (XmlNode p in props)
                        {
                            var bytes = Convert.FromBase64String(p.InnerText);
                            var ms = new MemoryStream(bytes, 0, bytes.Length);
                            ms.Write(bytes, 0, bytes.Length);
                            ms.Position = 0;
                            item.PropertiesUInt64[i++] = (UInt64)new BinaryFormatter().Deserialize(ms);
                        }
                    }
                    props = n.SelectNodes("propertyByteArray");

                    if (props != null)
                    {
                        int i = 0;
                        foreach (XmlNode p in props)
                        {
                            byte[] value;
                            if(!string.IsNullOrEmpty(p.InnerText))
                            {
                                var bytes = Convert.FromBase64String(p.InnerText);
                                var ms = new MemoryStream(bytes, 0, bytes.Length);
                                ms.Write(bytes, 0, bytes.Length);
                                ms.Position = 0;
                                value = (byte[]) new BinaryFormatter().Deserialize(ms);
                            }
                            else
                            {
                                value = null;
                            }
                            item.PropertiesByteArray[i++] = value;
                        }
                    }
                }

                n = node["main"];
                item.ParamMainIndex = n != null ? Convert.ToInt32(n.InnerText, CultureInfo.InvariantCulture) : 0;
            }

            return item;
        }

        public virtual CallEvent Clone()
        {
            var e = new CallEvent(false)
                        {
                            EventId = EventId,
                            Cookie = Cookie,
                            CallModule = CallModule,
                            CallStack = CallStack,
                            Success = Success,
                            Critical = Critical,
                            IsGenerated = IsGenerated,
                            RetValue = RetValue,
                            Time = Time,
                            GenerationTime = GenerationTime,
                            Pid = Pid,
                            ProcessName = ProcessName,
                            Tid = Tid,
                            Type = Type,
                            Function = Function,
                            Win32Function = Win32Function,
                            Before = Before,
                            OnlyBefore = OnlyBefore,
                            Result = Result,
                            ParamMainIndex = ParamMainIndex,
                            Virtualized = Virtualized,
                        };


            if (Params != null)
            {
                e.CreateParams(Params.Length);

                for (var i = 0; i < Params.Length; i++)
                {
                    e.Params[i].Name = Params[i].Name;
                    e.Params[i].Value = Params[i].Value;
                }
            }

            for (int i = 0; i<MaxPropertyCountBool; i++)
            {
                e.PropertiesBool[i] = PropertiesBool[i];
            }
            for (int i = 0; i < MaxPropertyCountString; i++)
            {
                e.PropertiesString[i] = PropertiesString[i];
            }
            for (int i = 0; i < MaxPropertyCountUInt; i++)
            {
                e.PropertiesUInt64[i] = PropertiesUInt64[i];
            }
            for (int i = 0; i < MaxPropertyCountByteArray; i++)
            {
                e.PropertiesByteArray[i] = PropertiesByteArray[i];
            }

            return e;
        }

        public static HookType GetHookTypeFromString(string hookType)
        {
            if (hookType == "CreateFileMapping")
                return HookType.CreateFile;
            try
            {
                return (HookType) Enum.Parse(typeof (HookType), hookType);
            }
            catch (ArgumentException)
            {
                return HookType.Invalid;
            }
        }

        public EventFlags EventFlags
        {
            get
            {
                var ret = EventFlags.None;
                if (IsCom)
                    ret |= EventFlags.Com;
                if (IsWindow)
                    ret |= EventFlags.Window;
                if (IsRegistry)
                    ret |= EventFlags.Registry;
                if (IsFileSystem)
                    ret |= EventFlags.FileSystem;
                if (IsServices)
                    ret |= EventFlags.Services;
                if (Virtualized)
                    ret |= EventFlags.Virtualized;
                if (Critical)
                    ret |= EventFlags.Critical;
                if (IsGenerated)
                    ret |= EventFlags.Generated;
                if (IsProcMon)
                    ret |= EventFlags.ProcMon;
                if (Before)
                    ret |= EventFlags.Before;
                if (OnlyBefore)
                    ret |= EventFlags.OnlyBefore;
                if (TypeProcessed)
                    ret |= EventFlags.TypeProcessed;
                return ret;
            }
            set
            {
                IsCom = (value & EventFlags.Com) != 0;
                IsWindow = (value & EventFlags.Window) != 0;
                IsRegistry = (value & EventFlags.Registry) != 0;
                IsFileSystem = (value & EventFlags.FileSystem) != 0;
                IsServices = (value & EventFlags.Services) != 0;
                Virtualized = (value & EventFlags.Virtualized) != 0;
                Critical = (value & EventFlags.Critical) != 0;
                IsGenerated = (value & EventFlags.Generated) != 0;
                IsProcMon = (value & EventFlags.ProcMon) != 0;
                Before = (value & EventFlags.Before) != 0;
                OnlyBefore = (value & EventFlags.OnlyBefore) != 0;
                TypeProcessed = (value & EventFlags.TypeProcessed) != 0;
            }
        }

        public int PackedBoolProperties
        {
            get
            {
                var ret = 0;
                for (int i = 1; i <= MaxPropertyCount; i++)
                {
                    if (PropertiesBool[i-1])
                        ret += 1 << i;
                }
                return ret;
            }
            set
            {
                for (int i = 1; i <= MaxPropertyCount; i++)
                {
                    PropertiesBool[i-1] = (value & 1 << i) != 0;
                }
            }
        }

        public string FunctionRoot
        {
            get { return Function.EndsWith("DllGetClassObject") ? "DllGetClassObject" : Function; }
        }

        public bool IsClearSignalEvent
        {
            get { return Type == HookType.DummyClear; }
        }
    }

    public class RegOpenKeyEvent : CallEvent
    {
        public static void CreateEventParams(CallEvent e, string key)
        {
            e.CreateParams(1);
            e.Params[0].Value = key;
            e.Params[0].Name = "Key";
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);
        }

        public virtual string Key
        {
            get { return Params[0].Value; }
            set { Params[0].Value = value; }
        }

        public static string GetKey(CallEvent e)
        {
            return e.Params[0].Value;
        }

        public new static CallEvent FromXml(XmlNode node)
        {
            var item = new RegOpenKeyEvent();
            return item;
        }
    }

    public class RegQueryValueEvent : RegOpenKeyEvent
    {
        private const int KeyStringPropIndex = 0;

        private const int IsValueBoolPropIndex = 0;
        private const int DataNullBoolPropIndex = 1;
        private const int DataCompleteBoolPropIndex = 2;
        private const int WriteBoolPropIndex = 3;
        private const int DataAvailableBoolPropIndex = 4;

        private const int ValueTypeUIntPropIndex = 0;

        public static void CreatePath(bool createParams, CallEvent e, string key, string valueName, string valueData,
                                      RegistryValueKind valueType)
        {
            if (createParams)
                e.CreateParams(4);

            e.Params[1].Name = "Name";
            if (!string.IsNullOrEmpty(valueName))
            {
                e.Params[1].Value = valueName;
                e.Params[0].Value = key + "\\" + valueName;
            }
            else
            {
                e.Params[1].Value = "";
                e.Params[0].Value = key;
            }
            e.Params[2].Name = "Data";
            e.Params[2].Value = (valueData ?? "");
            e.Params[3].Name = "Type";
            e.Params[3].Value = RegistryTools.GetValueTypeString(valueType);
            SetDataNull(e, valueData == null);
            SetParentKey(e, key);
            SetValueType(e, valueType);
            e.Params[0].Name = "Path";
            SetDataComplete(e, false);
            SetWrite(e, false);
        }

        public static void CreatePath(CallEvent e, string key, string valueName, string valueData,
                                      RegistryValueKind valueType)
        {
            CreatePath(true, e, key, valueName, valueData, valueType);
        }

        public static string GetName(CallEvent e)
        {
            return e.Params[1].Value;
        }

        public static string GetData(CallEvent e)
        {
            return e.Params[2].Value;
        }

        public static string GetPath(CallEvent e)
        {
            return e.Params[0].Value;
        }

        public static string GetParentKey(CallEvent e)
        {
            return e.PropertiesString[KeyStringPropIndex];
        }

        public static void SetParentKey(CallEvent e, string key)
        {
            e.PropertiesString[KeyStringPropIndex] = key;
        }

        public static bool IsDataNull(CallEvent e)
        {
            return e.PropertiesBool[DataNullBoolPropIndex];
        }
        public static void SetDataNull(CallEvent e, bool isNull)
        {
            e.PropertiesBool[DataNullBoolPropIndex] = isNull;
        }

        public static bool IsDataComplete(CallEvent e)
        {
            return e.PropertiesBool[DataCompleteBoolPropIndex];
        }

        public static void SetDataComplete(CallEvent e, bool complete)
        {
            e.PropertiesBool[DataCompleteBoolPropIndex] = complete;
        }

        public static void SetWrite(CallEvent e, bool write)
        {
            e.PropertiesBool[WriteBoolPropIndex] = write;
        }

        public static bool IsDataAvailable(CallEvent e)
        {
            return e.PropertiesBool[DataAvailableBoolPropIndex];
        }

        public static void SetDataAvailable(CallEvent e, bool dataAvailable)
        {
            e.PropertiesBool[DataAvailableBoolPropIndex] = dataAvailable;
        }

        public static bool IsWrite(CallEvent e)
        {
            return e.PropertiesBool[WriteBoolPropIndex];
        }

        public static bool IsValue(CallEvent e)
        {
            return e.IsRegistry && e.PropertiesBool[IsValueBoolPropIndex];
        }
        public static void SetValue(CallEvent e, bool isValue)
        {
            e.PropertiesBool[IsValueBoolPropIndex] = isValue;
        }

        public static RegistryValueKind GetValueType(CallEvent e)
        {
            return (RegistryValueKind)e.PropertiesUInt64[ValueTypeUIntPropIndex];
        }
        public static void SetValueType(CallEvent e, RegistryValueKind valueType)
        {
            e.PropertiesUInt64[ValueTypeUIntPropIndex] = (UInt64) valueType;
        }

        public override string Key
        {
            get { return GetParentKey(this); }
        }

        public string ValueName
        {
            get { return GetName(this); }
        }

        public string ValueData
        {
            get { return GetData(this); }
        }

    }

    public class FileSystemEvent : CallEvent
    {
        private const int IsDirectoryBoolPropIndex = 0;
        private const int ModuleNotFoundBoolPropIndex = 1;
        private const int FileInfoSetBoolPropIndex = 2;
        private const int QueryAttributesBoolPropIndex = 3;
        private const int InvalidPathBoolPropIndex = 4;
        protected const int ParamMainIsFileIdBoolPropIndex = 5;
        
        private const int FileSystemAccessUIntPropIndex = 0;

        private const int FilepartStringPropIndex = 0;
        private const int CompanyStringPropIndex = 1;
        private const int VersionStringPropIndex = 2;
        private const int DescriptionStringPropIndex = 3;
        private const int ProductStringPropIndex = 4;
        private const int OriginalFileNameStringPropIndex = 5;

        public static bool IsDirectory(CallEvent callEvent)
        {
            return callEvent.GetPropertiesBool(IsDirectoryBoolPropIndex);
        }
        public static void SetDirectory(CallEvent callEvent, bool isDirectory)
        {
            callEvent.PropertiesBool[IsDirectoryBoolPropIndex] = isDirectory;
        }
        public static bool IsQueryAttributes(CallEvent callEvent)
        {
            return callEvent.GetPropertiesBool(QueryAttributesBoolPropIndex);
        }
        public static void SetQueryAttributes(CallEvent callEvent, bool isQueryAttributes)
        {
            callEvent.PropertiesBool[QueryAttributesBoolPropIndex] = isQueryAttributes;
        }
        //Returns true if a file system event references a file in its main parameter.
        static public bool ReferencesFile(CallEvent callEvent)
        {
            return !IsDirectory(callEvent) && !ModuleNotFound(callEvent);
        }
        static public bool ModuleNotFound(CallEvent callEvent)
        {
            return callEvent.GetPropertiesBool(ModuleNotFoundBoolPropIndex);
        }
        static public void SetModuleNotFound(CallEvent callEvent, bool value)
        {
            callEvent.PropertiesBool[ModuleNotFoundBoolPropIndex] = value;
        }
        static public bool IsInvalidPath(CallEvent callEvent)
        {
            return callEvent.GetPropertiesBool(InvalidPathBoolPropIndex);
        }
        static public void SetInvalidPath(CallEvent callEvent, bool value)
        {
            callEvent.PropertiesBool[InvalidPathBoolPropIndex] = value;
        }
        static public FileSystemAccess GetAccess(CallEvent callEvent)
        {
            return (FileSystemAccess)callEvent.GetPropertiesUInt64(FileSystemAccessUIntPropIndex);
        }
        static public void SetAccess(CallEvent callEvent, FileSystemAccess value)
        {
            callEvent.PropertiesUInt64[FileSystemAccessUIntPropIndex] = (UInt64)value;
        }
        public static void SetFileInfo(CallEvent e, string fileSystemPath)
        {
            e.PropertiesBool[FileInfoSetBoolPropIndex] = false;

            string company = string.Empty,
                   version = string.Empty,
                   description = string.Empty,
                   originalFileName = string.Empty,
                   product = string.Empty;
            // filter pipes that don't have any file information and sometimes they hang the application (with Swv driver)
            if (!fileSystemPath.StartsWith(@"\\.") &&
                FileSystemTools.GetFileProperties(fileSystemPath, ref originalFileName, ref product, ref company,
                                                  ref version, ref description))
            {
                e.PropertiesString[CompanyStringPropIndex] = company;
                e.PropertiesString[VersionStringPropIndex] = version;
                e.PropertiesString[DescriptionStringPropIndex] = description;
                e.PropertiesString[OriginalFileNameStringPropIndex] = originalFileName;
                e.PropertiesString[ProductStringPropIndex] = product;
                e.PropertiesBool[FileInfoSetBoolPropIndex] = true;
            }
        }

        /// <summary>
        /// True if the File information was tried to get. Even if there is no information about the file this function will return True after
        /// trying to set.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsFileInfoSet(CallEvent e)
        {
            return e.PropertiesBool[FileInfoSetBoolPropIndex];
        }

        public static string GetCompany(CallEvent e)
        {
            return e.PropertiesString[CompanyStringPropIndex];
        }

        public static string GetVersion(CallEvent e)
        {
            return e.PropertiesString[VersionStringPropIndex];
        }

        public static string GetDescription(CallEvent e)
        {
            return e.PropertiesString[DescriptionStringPropIndex];
        }

        public static string GetProduct(CallEvent e)
        {
            return e.PropertiesString[ProductStringPropIndex];
        }

        public static string GetOriginalFileName(CallEvent e)
        {
            return e.PropertiesString[OriginalFileNameStringPropIndex];
        }

        public static string GetFilepart(CallEvent e)
        {
            return e.PropertiesString[FilepartStringPropIndex];
        }
        public static void SetFilepart(CallEvent e, string filepart)
        {
            e.PropertiesString[FilepartStringPropIndex] = filepart;
        }
    }

    public class LoadLibraryEvent : FileSystemEvent
    {
        private const int AddressUIntPropIndex = 1;

        public static void CreateEventParams(CallEvent e, string dllName, UInt64 address)
        {
            e.CreateParams(2);
            e.Params[0].Value = dllName;
            e.Params[0].Name = "Path";
            e.Params[1].Value = address.ToString("X");
            e.Params[1].Name = "Address";
            SetAddress(e, address);
            SetAccess(e, FileSystemAccess.LoadLibrary);
            e.Success = (e.RetValue == 0);
            e.Result = Declarations.NtStatusToString(e.RetValue);
        }

        public static void CreateEventParams(CallEvent e, string dllName)
        {
            e.CreateParams(1);
            e.Params[0].Value = dllName;
            e.Params[0].Name = "Path";
            SetAddress(e, 0);
            SetAccess(e, FileSystemAccess.LoadLibrary);
        }
        static public UInt64 GetAddress(CallEvent callEvent)
        {
            return callEvent.GetPropertiesUInt64(AddressUIntPropIndex);
        }
        static public void SetAddress(CallEvent callEvent, UInt64 value)
        {
            callEvent.PropertiesUInt64[AddressUIntPropIndex] = value;
        }
    }

    public class CreateFileEvent : FileSystemEvent
    {
        public static void CreateEventParams(CallEvent e, string passedFilename, FileSystemAccess access, bool isFileId)
        {
            e.CreateParams(2);
            e.Params[0].Name = "Path";
            e.Params[0].Value = passedFilename;
            e.Params[1].Name = "Access";
            e.Params[1].Value = FileSystemTools.GetAccessString(access);
            SetAccess(e, access);
        }

        public static void CreateEventParams(CallEvent e, string passedFilename, string returnedFilename, FileSystemAccess access, string desiredAccess,
                                             string attributes, string share, string options, string createDisposition, bool isFileId)
        {
            var list = new List<Param> {Capacity = 10};
            int paramMain = 0;
            e.Success = (e.RetValue == 0);
            if (!isFileId || !e.Success)
                list.Add(new Param("Path", passedFilename ?? string.Empty));
            else
                list.Add(new Param("Path", returnedFilename ?? string.Empty));
            list.Add(new Param("Access", FileSystemTools.GetAccessString(access)));
            if (!string.IsNullOrEmpty(desiredAccess))
                list.Add(new Param("DesiredAccess", desiredAccess));
            if (!string.IsNullOrEmpty(returnedFilename))
            {
                //paramMain = list.Count;
                list.Add(new Param("ReturnedPath", returnedFilename));
            }
            if (!string.IsNullOrEmpty(attributes))
                list.Add(new Param("ReadAttributes", attributes));
            if (!string.IsNullOrEmpty(share))
                list.Add(new Param("ShareMode", share));
            if (!string.IsNullOrEmpty(options))
                list.Add(new Param("Options", options));
            if (!string.IsNullOrEmpty(createDisposition))
                list.Add(new Param("CreateDisposition", createDisposition));
            e.Params = list.ToArray();
            e.ParamMainIndex = paramMain;

            SetAccess(e, access);
            e.Result = Declarations.NtStatusToString(e.RetValue);

            if (isFileId && !e.Success)
                e.PropertiesBool[ParamMainIsFileIdBoolPropIndex] = true;
        }

    }

    public class CreateDirectoryEvent : CallEvent
    {
        public static void CreateEventParams(CallEvent e, string filename)
        {
            e.CreateParams(1);
            e.Params[0].Name = "Path";
            e.Params[0].Value = filename;
            FileSystemEvent.SetAccess(e, FileSystemAccess.CreateDirectory);
            e.Result = (e.RetValue == 0 ? "ERROR" : "SUCCESS");
            e.Success = (e.RetValue != 0);
        }
    }

    public class CreateWindowEvent : CallEvent
    {
        static protected int HModuleUIntPropIndex;
        static protected int StyleUIntPropIndex = 1;
        static protected int ExStyleUIntPropIndex = 2;

        static protected int HModuleStringPropIndex;

        public static void CreateEventParams(CallEvent e, string className, string wndName, UInt64 hModule)
        {
            Common(e, className, null, wndName);
            e.PropertiesUInt64[HModuleUIntPropIndex] = hModule;
        }

        private static readonly Regex GetFileNameRegex = new Regex(@"(?:.*\\)?([^\\]*)");

        private static List<Param> Common(CallEvent e, string className, string wndName, string hModule)
        {
            var parameters = new List<Param>();
            parameters.Add(new Param("ClassName", className));
            if (wndName != null)
                parameters.Add(new Param("WindowName", wndName));

            var match = GetFileNameRegex.Match(hModule);
            if (match.Success)
                hModule = match.Groups[1].ToString();
            e.PropertiesString[HModuleUIntPropIndex] = hModule;
            /*
            e.Result = (e.RetValue != 0 ? "SUCCESS" : "ERROR");
            e.Success = (e.RetValue != 0);
            */
            e.ParamMainIndex = 0;
            return parameters;
        }

        public static void CreateEventParams(CallEvent e, string className, string wndName, string hModule, uint exstyle, uint style, string parent)
        {
            var parameters = Common(e, className, wndName, hModule);
            parameters.Add(new Param("ExStyle", "0x" + exstyle.ToString("X")));
            parameters.Add(new Param("Style", "0x" + style.ToString("X")));
            parameters.Add(new Param("ParentWindow", parent == "0" ? string.Empty : parent));
            e.PropertiesUInt64[StyleUIntPropIndex] = style;
            e.PropertiesUInt64[ExStyleUIntPropIndex] = exstyle;
            e.Params = parameters.ToArray();
        }

        public static void CreateEventParams(CallEvent e, string className, string wndName, string hModule)
        {
            var parameters = Common(e, className, wndName, hModule);
            e.Params = parameters.ToArray();
        }
        static public UInt64 GetHModuleAddress(CallEvent e)
        {
            return e.PropertiesUInt64[HModuleUIntPropIndex];
        }
        static public string GetHModuleString(CallEvent e)
        {
            return e.PropertiesString[HModuleUIntPropIndex];
        }
        static public UInt64 GetStyle(CallEvent e)
        {
            return e.PropertiesUInt64[StyleUIntPropIndex];
        }
        static public UInt64 GetExStyle(CallEvent e)
        {
            return e.PropertiesUInt64[ExStyleUIntPropIndex];
        }

        private const uint WsPopupWindow = 0x80000000 | 0x00800000 | 0x00C00000;

        /// <summary>
        /// Returns true if the window is top-level or parent of dialog or any other property that make think it is 
        /// an important window of the application.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static public bool IsImportantWindow(CallEvent e)
        {
            if (e.Params.Length >= 5)
            {
                if (string.IsNullOrEmpty(e.Params[4].Value))
                {
                    // Style: WS_DLGFRAME 0x00400000L WS_POPUPWINDOW WsPopupWindow WS_SIZEBOX 0x00040000L
                    // WS_SYSMENU 0x00080000L WS_THICKFRAME 0x00040000L
                    if (((GetStyle(e) & (0x00400000 |
                                        0x00040000 | 0x00080000 | 0x00040000)) != 0) ||
                        (GetStyle(e) & WsPopupWindow) == WsPopupWindow)
                    {
                        return true;
                    }
                    // ExStyle: WS_EX_APPWINDOW 0x00040000L 
                    if ((GetExStyle(e) & (0x00040000)) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static readonly HashSet<string> NonVisibleWindows = new HashSet<string>
                                                                        {
                                                                            "WorkerW",
                                                                            "tooltips_class32",
                                                                            "OleMainThreadWndClass"
                                                                        };
        static public bool IsVisibleWindow(CallEvent e)
        {
            if (e.Params.Length >= 5)
            {
                return (e.Params[4].Value != "HWND_MESSAGE" && !NonVisibleWindows.Contains(e.Params[0].Value));
            }
            return false;
        }
    }

    public class CreateProcessEvent : FileSystemEvent
    {
        protected static int NewPidUIntPropIndex = 1;

        public static void CreateEventParams(CallEvent e, string processPath, string cmdLine)
        {
            CreateEventParams(e, processPath, cmdLine, 0);
        }

        public static void CreateEventParams(bool createParams, CallEvent e, string processPath, string cmdLine,
                                             uint newPid, bool success)
        {
            if (createParams)
                e.CreateParams(3);
            e.Params[0].Name = "ProcessPath";
            e.Params[0].Value = processPath;
            e.Params[1].Name = "NewPid";
            e.Params[1].Value = (newPid == 0 ? "<Unknown>" : newPid.ToString(CultureInfo.InvariantCulture));
            e.Params[2].Name = "CommandLine";
            e.Params[2].Value = cmdLine;

            SetAccess(e, (FileSystemAccess.Execute | FileSystemAccess.CreateProcess));
            e.PropertiesUInt64[NewPidUIntPropIndex] = newPid;
            e.Success = success;
            e.Result = (e.Success ? "SUCCESS" : "ERROR");
            e.ParamMainIndex = 0;
        }
        public static void CreateEventParams(bool createParams, CallEvent e, string processPath, string cmdLine,
                                             uint newPid)
        {
            CreateEventParams(createParams, e, processPath, cmdLine, newPid, e.RetValue != 0);
        }

        public static void CreateEventParams(CallEvent e, string processPath, string cmdLine, uint newPid)
        {
            CreateEventParams(true, e, processPath, cmdLine, newPid);
        }
        public static void CreateEventParams(CallEvent e, string processPath, string cmdLine, uint newPid, bool success)
        {
            CreateEventParams(true, e, processPath, cmdLine,
                              newPid, success);
        }
        public static void SetNewPid(CallEvent e, uint newPid)
        {
            e.PropertiesUInt64[NewPidUIntPropIndex] = newPid;
        }

        public static uint GetNewPid(CallEvent e)
        {
            return (uint) e.PropertiesUInt64[NewPidUIntPropIndex];
        }
    }
}