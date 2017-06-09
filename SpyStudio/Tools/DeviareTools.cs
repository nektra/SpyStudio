using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Nektra.Deviare2;
using System.Diagnostics;
using System.Xml;
using ProtoBuf;
using SpyStudio.Hooks;
using SpyStudio.Properties;
using SpyStudio.Trace;

// ReSharper disable CheckNamespace
namespace SpyStudio.Tools
// ReSharper restore CheckNamespace
{
    public class DeviareTools
    {
        //Note: This list is passed to the Deviare agent plugin. The plugin's
        //      behavior is unknown in the case of Unicode names. Specifically,
        //      names with character values >= 0x80.
        public static readonly List<string> SystemModulesList = new List<string>
                                                           {
                                                               "kernel32.dll",
                                                               "kernelbase.dll",
                                                               "ole32.dll",
                                                               "shlwapi.dll",
                                                               "shell32.dll",
                                                               "rpcrt4.dll",
                                                               "ntdll.dll",
                                                               "advapi32.dll",
                                                               "dvagent.dll",
                                                               "dvagentd.dll",
                                                               "iepatch.dll",
                                                               "aclayers.dll",
                                                               "user32.dll",
                                                               "oleaut32.dll",
                                                               "ntoskrnl.exe",
                                                               "wow64.dll",
                                                               "wow64cpu.dll",
                                                               "wow64win.dll",
                                                               "os_exe.dll",
                                                               "nt0_dll.dll",
                                                               //See comment above.
                                                           };

        public static HashSet<string> SystemModules = new HashSet<string>(SystemModulesList);

        static public string GetModule(string hookPath)
        {
            string ret = "";
            int index = hookPath.IndexOf("!", StringComparison.Ordinal);
            if(index != -1)
            {
                ret = hookPath.Substring(0, index).ToLower();
            }
            return ret;
        }
        static public void AddSystemModule(string path)
        {
            string module = ModulePath.ExtractModuleName(path);
            if(!string.IsNullOrEmpty(module))
            {
                if(!SystemModules.Contains(module))
                {
                    //Console.WriteLine(module);
                    SystemModules.Add(module);
                }
            }
        }
        public static int GetPlatformBits(NktSpyMgr spyMgr)
        {
            INktProcess proc = spyMgr.Processes().GetById(Process.GetCurrentProcess().Id);
            Debug.Assert(proc != null);
            return proc.PlatformBits;
        }

        public enum ParameterType
        {
            Pointer,
            Array,
            Struct,
            SignedByte,
            UnsignedByte,
            SignedWord,
            UnsignedWord,
            SignedDoubleWord,
            UnsignedDoubleWord,
            SignedQuadWord,
            UnsignedQuadWord,
            Float,
            Double,
            LongDouble,
            Void,
            String,
            Enumeration,
            FunctionType,
            Null,
            ClassMethod,
            ClassConverter,
            Unknown
        }

        public class DeviareParameterPath
        {
            private readonly NktDbObject _functionObj;
            private NktDbObject _currentObj, _currentParentObj;
            private readonly DeviareHook.Parameter _paramTree;
            private DeviareHook.ParamInfo _currentParameter;
            //private int _currentIndex;
            private bool _evaluated = false;
            readonly private int _currentLevel = -1;
            readonly private bool _onlyVisibleChildren = false;
            private List<DeviareParameterPath> _children;
            private int _index;
            private string _castToType;

            public DeviareParameterPath(NktDbObject functionObj, DeviareHook.Parameter paramTree, int level, bool getOnlyVisibleChildren)
            {
                Debug.Assert(functionObj != null && level > 0);
                _functionObj = functionObj;
                _paramTree = paramTree;
                _currentLevel = level;
                _onlyVisibleChildren = getOnlyVisibleChildren;
                Refresh();
            }
            public DeviareParameterPath(NktDbObject functionObj)
            {
                Debug.Assert(functionObj != null);
                _functionObj = functionObj;
                _paramTree = null;
                _onlyVisibleChildren = true;
                _currentLevel = 0;
                Refresh();
            }
            public DeviareParameterPath(NktDbObject functionObj, bool onlyVisibleChildren)
            {
                Debug.Assert(functionObj != null);
                _functionObj = functionObj;
                _onlyVisibleChildren = onlyVisibleChildren;
                _currentLevel = 0;
                Refresh();
            }
            /// <summary>
            /// This contructor can be used when the path lacks of _paramTree (e.g.: when it's a function)
            /// </summary>
            /// <param name="path"></param>
            /// <param name="level"></param>
            /// <param name="obj"></param>
            /// <param name="index"></param>
            public DeviareParameterPath(DeviareParameterPath path, int level, NktDbObject obj, int index)
            {
                Debug.Assert(path != null);
                _functionObj = path._functionObj;
                _paramTree = path._paramTree;

                // if level != 0 -> it's not a function, so we need a parameter description
                if (level > 0 && _paramTree == null)
                {
                    _paramTree = new DeviareHook.Parameter();
                    _paramTree.ParamTree.Add(new DeviareHook.ParamInfo(obj, _functionObj, index));
                }
                _currentLevel = level;
                _onlyVisibleChildren = path._onlyVisibleChildren;
                Refresh();
            }
            public DeviareParameterPath(DeviareParameterPath path, int level)
            {
                Debug.Assert(path != null);
                _functionObj = path._functionObj;
                _paramTree = path._paramTree;

                // if level != 0 -> it's not a function, so we need a parameter description
                if(level > 0 && _paramTree == null)
                {
                    _paramTree = new DeviareHook.Parameter();
                    _paramTree.ParamTree.Add(new DeviareHook.ParamInfo());
                }
                _currentLevel = level;
                _onlyVisibleChildren = path._onlyVisibleChildren;
                Refresh();
            }
            public void Refresh()
            {
                _children = null;
                NktDbObject dbObj = null;
                NktDbObject parentDbObj = _functionObj;
                NktDbObject nextParentDbObj = _functionObj;
                if(_paramTree != null)
                {
                    int lastIndex = -1;
                    bool nextEvaluate = false;
                    int i = 0;
                    int nextParam = 0;

                    // level means the number of evaluations and children got in the path. The evaluation and getting children eat a level. 
                    do
                    {
                        var p = _paramTree.ParamTree[nextParam];
                        if (nextEvaluate)
                        {
                            parentDbObj = nextParentDbObj;
                            dbObj = dbObj.Evaluate();
                            nextEvaluate = false;
                            nextParam++;
                            lastIndex = 0;
                        }
                        else
                        {
                            parentDbObj = nextParentDbObj;
                            var fieldCount = GetFieldCount(parentDbObj);
                            if(p.Index >= fieldCount)
                            {
                                if(fieldCount == 0)
                                {
                                    _paramTree.Truncate(p, false);
                                    break;
                                }
                                else
                                {
                                    p.Index = fieldCount - 1;
                                }
                            }
                            dbObj = parentDbObj.Items().GetAt(p.Index);
                            lastIndex = p.Index;
                            if (p.Evaluate)
                            {
                                nextEvaluate = true;
                            }
                            else
                            {
                                nextParam++;
                            }
                        }
                        // first evaluate and then cast
                        if (!nextEvaluate && !String.IsNullOrEmpty(p.Type))
                        {
                            dbObj = _spyMgr.DbObjects(GetPlatformBits(_spyMgr)).GetByName(p.Type);
                            Debug.Assert(dbObj != null);
                        }
                        _currentParameter = p;
                        nextParentDbObj = dbObj;
                        i++;
                        // if current item is not a leaf (it has children) and the ParamTree doesn't cover its children -> add a new node
                        // to cover children and select the first item
                        if (nextParam >= _paramTree.ParamTree.Count && IsStruct(dbObj))
                        {
                            NktDbObject childObj = dbObj.Items().GetAt(0);
                            var info = new DeviareHook.ParamInfo(childObj, dbObj, 0);
                            _paramTree.ParamTree.Add(info);
                        }
                        // stop if there are no more parameters or the level was reached
                    } while ((_currentLevel == 0 || i <= _currentLevel) && nextParam < _paramTree.ParamTree.Count);

                    // null object if there are no enough levels to reach _currentLevel
                    if (_currentLevel != -1 && i <= _currentLevel)
                    {
                        _currentObj = null;
                        _currentParentObj = null;
                    }
                    else
                    {
                        Debug.Assert(parentDbObj != null && dbObj != null && lastIndex != -1);

                        _evaluated = nextEvaluate;
                        Name = parentDbObj.ItemName(lastIndex).Trim();
                        Declaration = parentDbObj.ItemDeclaration(lastIndex).Trim();

                        _currentObj = dbObj;
                        _currentParentObj = parentDbObj;
                        _index = lastIndex;
                    }                    
                }
                else
                {
                    _currentObj = _functionObj;
                    _currentParentObj = _functionObj;
                    _index = -1;
                }

                Type = _currentObj != null ? GetType(_currentObj) : ParameterType.Null;
                _castToType = _currentParameter != null ? _currentParameter.Type : "";
            }

            public bool IsNull() { return (_currentObj == null);}

            public int Index
            {
                get { return _index; }
                set
                {
                    _index = value;
                    _currentParameter.Index = Index;
                    _paramTree.Truncate(_currentParameter, true);
                    Refresh();
                }
            }

            public bool IsParameter()
            {
                return (Type != ParameterType.Unknown && Type != ParameterType.Null &&
                        Type != ParameterType.ClassMethod && Type != ParameterType.ClassConverter);
            }

            public static bool IsParameter(NktDbObject p)
            {
                var t = GetType(p);
                return (t != ParameterType.Unknown && t != ParameterType.Null &&
                        t != ParameterType.ClassMethod && t != ParameterType.ClassConverter);
            }
            public bool IsStruct(NktDbObject dbObj)
            {
                return (dbObj.ItemsCount > 0 && Type == ParameterType.Struct);
            }
            public bool IsStruct()
            {
                return (_currentObj.ItemsCount > 0 && Type == ParameterType.Struct);
            }
            public ParameterType Type { get; private set; }

            public string CastToType
            {
                get { return _castToType; }
                set
                {
                    _currentParameter.Type = value;
                    Refresh();
                }
            }

            public string Name { get; private set; }
            public string Declaration { get; private set; }
            public DeviareHook.Parameter GetParameterTree()
            {
                return _paramTree;
            }

            public bool Evaluated
            {
                get { return _evaluated; }
                set
                {
                    if (_evaluated != value)
                    {
                        if (!_evaluated)
                        {
                            Debug.Assert(Type == ParameterType.Pointer);
                            _evaluated = true;
                            _currentParameter.Evaluate = true;
                            _paramTree.Truncate(_currentParameter, true);
                        }
                        else
                        {
                            _evaluated = false;
                            _currentParameter.Evaluate = false;
                            _paramTree.Truncate(_currentParameter, true);
                        }
                        Refresh();
                    }
                }
            }

            public DeviareParameterPath GetEvaluated()
            {
                Debug.Assert(_paramTree != null);
                var evalParam = _paramTree.Clone();
                var info = evalParam.ParamTree[_paramTree.ParamTree.Count - 1];
                info.Evaluate = true;
                return new DeviareParameterPath(_functionObj, evalParam, _currentLevel + 1, _onlyVisibleChildren);
            }

            public List<DeviareParameterPath> GetChildren()
            {
                if(_children == null)
                {
                    _children = new List<DeviareParameterPath>();
                    if (!_onlyVisibleChildren) // _currentLevel == -1 || 
                    {
                        if (Type == ParameterType.Pointer && _evaluated)
                        {
                            _children.Add(GetEvaluated());
                        }
                        else
                        {
                            NktDbObjectsEnum children = _currentObj.Items();
                            int i = 0;
                            foreach (NktDbObject obj in children)
                            {
                                if (IsParameter(obj))
                                {
                                    var newPath = new DeviareParameterPath(this, _currentLevel + 1, obj, i);
                                    //DeviareHook.Parameter newParam = _paramTree != null
                                    //                                             ? _paramTree.Clone()
                                    //                                             : new DeviareHook.Parameter();
                                    //var paramInfo = new DeviareHook.ParamInfo(obj, _currentObj, i);

                                    //newParam.ParamTree.Add(paramInfo);
                                    //_children.Add(new DeviareParameterPath(_functionObj, newParam, _currentLevel + 1, _onlyVisibleChildren));
                                    _children.Add(newPath);
                                }
                                i++;
                            }
                        }
                    }
                    else
                    {
                        var newPath = new DeviareParameterPath(this, _currentLevel + 1);
                        if(!newPath.IsNull())
                        {
                            _children.Add(newPath);
                        }
                    }
                }
                return _children;
            }
            static public int GetFieldCount(NktDbObject parent)
            {
                return parent.Items().Cast<NktDbObject>().Count(IsParameter);
            }
            public int GetFieldCount()
            {
                if (_currentObj != null)
                    return GetFieldCount(_currentObj);
                return 0;
            }

            public int GetParentFieldCount()
            {
                return _currentParentObj.Items().Cast<NktDbObject>().Count(IsParameter);
            }

            public static ParameterType GetType(NktDbObject p)
            {
                if (p.IsAnsiString || p.IsWideString)
                    return ParameterType.String;
                switch (p.Class)
                {
                    case eNktDboClass.clsArray:
                        return ParameterType.Array;
                    case eNktDboClass.clsPointer:
                    case eNktDboClass.clsReference:
                        {
                            NktDbObject evaluated = p.Evaluate();
                            if(evaluated != null && evaluated.Class == eNktDboClass.clsFunctionType)
                                return ParameterType.FunctionType;
                        }
                        return ParameterType.Pointer;
                    case eNktDboClass.clsFunctionType:
                        return ParameterType.FunctionType;
                    case eNktDboClass.clsUnion:
                    case eNktDboClass.clsStruct:
                        return ParameterType.Struct;
                    case eNktDboClass.clsNull:
                        return ParameterType.Null;
                    case eNktDboClass.clsEnumeration:
                        return ParameterType.Enumeration;
                    case eNktDboClass.clsFunction:
                    case eNktDboClass.clsClassConstructor:
                    case eNktDboClass.clsClassDestructor:
                    case eNktDboClass.clsClassOperatorMethod:
                    case eNktDboClass.clsClassMethod:
                        return ParameterType.ClassMethod;
                    case eNktDboClass.clsClassConverter:
                        return ParameterType.ClassConverter;
                    case eNktDboClass.clsFundamental:
                        switch (p.FundamentalType)
                        {
                            case eNktDboFundamentalType.ftSignedByte:
                                return ParameterType.SignedByte;
                            case eNktDboFundamentalType.ftUnsignedByte:
                                return ParameterType.UnsignedByte;
                            case eNktDboFundamentalType.ftSignedWord:
                                return ParameterType.SignedWord;
                            case eNktDboFundamentalType.ftUnsignedWord:
                                return ParameterType.UnsignedWord;
                            case eNktDboFundamentalType.ftSignedDoubleWord:
                                return ParameterType.SignedDoubleWord;
                            case eNktDboFundamentalType.ftUnsignedDoubleWord:
                                return ParameterType.UnsignedDoubleWord;
                            case eNktDboFundamentalType.ftSignedQuadWord:
                                return ParameterType.SignedQuadWord;
                            case eNktDboFundamentalType.ftUnsignedQuadWord:
                                return ParameterType.UnsignedQuadWord;
                            case eNktDboFundamentalType.ftFloat:
                                return ParameterType.Float;
                            case eNktDboFundamentalType.ftDouble:
                                return ParameterType.Double;
                            case eNktDboFundamentalType.ftLongDouble:
                                return ParameterType.LongDouble;
                            case eNktDboFundamentalType.ftVoid:
                                return ParameterType.Void;
                            case eNktDboFundamentalType.ftAnsiChar:
                            case eNktDboFundamentalType.ftWideChar:
                                return ParameterType.String;
                            default:
                                return ParameterType.Unknown;
                        }
                    default:
                        return ParameterType.Unknown;
                }
            }
        }
        static public string ClsIdStruct2String(INktParam p)
        {
            INktParamsEnum pms = p.Fields();

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(pms.GetAt(0).ULongVal.ToString("X8"));
            sb.Append("-");
            sb.Append(pms.GetAt(1).UShortVal.ToString("X4"));
            sb.Append("-");
            sb.Append(pms.GetAt(2).UShortVal.ToString("X4"));
            sb.Append("-");
            p = pms.GetAt(3);
            for (int i = 0; i < 2; i++)
                sb.Append(p.get_ByteValAt(i).ToString("X2"));
            sb.Append("-");
            for (int i = 2; i < 8; i++)
                sb.Append(p.get_ByteValAt(i).ToString("X2"));
            sb.Append("}");
            return sb.ToString();
        }
        static public string GetParamModule(ModulePath modPath, NktHookCallInfo callInfo, INktParam param, INktProcess proc)
        {
            string ret;

            //var value = (IntPtr) param.CastTo("LPVOID").PointerVal;
            var value = param.PointerVal;
            if (!modPath.TryGetPathByAddress((uint)callInfo.Process().Id,
                (UInt64)value, out ret))
            {
                NktModule mod = proc.ModuleByAddress(value, eNktSearchMode.smFindContaining);
                if (mod != null)
                {
                    ret = ModulePath.ExtractModuleName(mod.Path);
                }
            }

            if (string.IsNullOrEmpty(ret))
            {
                ret = "0x" + ((UInt64)value).ToString("X");
            }
            return ret;
        }
        static public string GetAddressRepresentation(NktHookCallInfo callInfo, IntPtr address)
        {
            string ret = "";
            var p = callInfo.Process();
            var module = p.Modules().GetByAddress(address, eNktSearchMode.smFindContaining);
            if (module != null)
            {
                var fnc = module.FunctionByAddress(address, true);
                if (fnc != null)
                {
                    ret = module.Name + "!" + fnc.Name + " + 0x" +
                          ((UInt64)address - (UInt64)fnc.Addr).ToString("X");
                }
                else
                {
                    ret = module.Name + " + 0x" +
                          ((UInt64) address - (UInt64) module.BaseAddress).ToString("X");
                }
            }
            else
            {
                ret = "0x" + address.ToString("X");
            }

            return ret;
        }

        private static List<string> _allTypes;
        private static NktSpyMgr _spyMgr;

        public static void InitTypes(NktSpyMgr spyMgr)
        {
            if (_allTypes == null)
            {
                _allTypes = new List<string>();
                _spyMgr = spyMgr;
                //NktDbObjectsEnum objs = spyMgr.DbObjects(GetPlatformBits(spyMgr));
                //foreach(NktDbObject obj in objs)
                //{
                //    _allTypes.Add(obj.Name);
                //}
            }
        }

        public static List<string> GetTypes()
        {
            return _allTypes;
        }
        public static bool ExistType(string t)
        {
            return (_spyMgr.DbObjects(GetPlatformBits(_spyMgr)).GetByName(t) != null);
        }

        /// <summary>
        /// Save memory re-using stack strings
        /// </summary>
        public class StackCache
        {
            public class NearestSymbolStrings
            {
                public string NearestSymbol;
                public Dictionary<UInt64, string> StackTraceStrings = new Dictionary<ulong, string>();
            }
            private static readonly Dictionary<string, NearestSymbolStrings> ProcessedNearestSymbols = new Dictionary<string, NearestSymbolStrings>();
            private static readonly ReaderWriterLock DataLock = new ReaderWriterLock();

            static public void GetStackStrings(string nearestSymbol, UInt64 offset, out string processedNearestSymbol, out string stackTraceString)
            {
                var index = nearestSymbol.IndexOf('!');
                if (index != -1)
                {
                    processedNearestSymbol = nearestSymbol.Substring(0, index).ToLower() + nearestSymbol.Substring(index);
                }
                else
                {
                    processedNearestSymbol = nearestSymbol;
                }

                stackTraceString = (String.IsNullOrEmpty(nearestSymbol) ? "0x" : nearestSymbol + " + 0x") + offset.ToString("X");
                
                //NearestSymbolStrings nearestSymbolStrings;
                //AcquireReaderLock();
                //if(!ProcessedNearestSymbols.TryGetValue(nearestSymbol, out nearestSymbolStrings))
                //{
                //    LockCookie cookie = UpgradeToWriterLock();
                //    // it can be add after the ReleaseReaderLock
                //    if (!ProcessedNearestSymbols.TryGetValue(nearestSymbol, out nearestSymbolStrings))
                //    {
                //        nearestSymbolStrings = new NearestSymbolStrings();
                //        var index = nearestSymbol.IndexOf('!');
                //        if (index != -1)
                //        {
                //            nearestSymbolStrings.NearestSymbol = nearestSymbol.Substring(0, index).ToLower() + nearestSymbol.Substring(index);
                //        }
                //        else
                //        {
                //            nearestSymbolStrings.NearestSymbol = nearestSymbol;
                //        }

                //        ProcessedNearestSymbols.Add(nearestSymbol, nearestSymbolStrings);
                //    }
                //    DowngradeFromWriterLock(ref cookie);
                //}
                //processedNearestSymbol = nearestSymbolStrings.NearestSymbol;

                //if (!nearestSymbolStrings.StackTraceStrings.TryGetValue(offset, out stackTraceString))
                //{
                //    LockCookie cookie = UpgradeToWriterLock();
                //    // it can be add after the ReleaseReaderLock
                //    if (!nearestSymbolStrings.StackTraceStrings.TryGetValue(offset, out stackTraceString))
                //    {
                //        stackTraceString = (String.IsNullOrEmpty(nearestSymbol) ? "0x" : nearestSymbol + " + 0x") + offset.ToString("X");

                //        nearestSymbolStrings.StackTraceStrings.Add(offset, stackTraceString);
                //    }
                //    DowngradeFromWriterLock(ref cookie);
                //}

                //ReleaseReaderLock();
            }

            static public void Clear()
            {
                AcquireWriterLock();
                ProcessedNearestSymbols.Clear();
                ReleaseWriterLock();
            }
            static public void AcquireReaderLock()
            {
                DataLock.AcquireReaderLock(-1);
            }
            static public LockCookie UpgradeToWriterLock()
            {
                return DataLock.UpgradeToWriterLock(-1);
            }
            static public void DowngradeFromWriterLock(ref LockCookie cookie)
            {
                DataLock.DowngradeFromWriterLock(ref cookie);
            }

            static public void AcquireWriterLock()
            {
                DataLock.AcquireWriterLock(-1);
            }
            static public void ReleaseReaderLock()
            {
                DataLock.ReleaseReaderLock();
            }
            static public void ReleaseWriterLock()
            {
                DataLock.ReleaseWriterLock();
            }
        }

        static public string GetStackFrameString(CallEvent e)
        {
            var ret = "";
            var frame = GetStackFrame(e);
            if (frame != null)
            {
                ret = frame.StackTraceString;
            }
            return ret;
        }
        static public DeviareStackFrame GetStackFrame(CallEvent e)
        {
            DeviareStackFrame ret = null;
            var frames = e.GetNonSystemStack();
            if (frames != null && frames.Any())
            {
                ret = frames.First();
            }
            else
            {
                frames = e.CallStack;
                if (frames != null && frames.Any())
                {
                    ret = frames.First();
                }
            }
            return ret;
        }

        //[Serializable]
        [ProtoContract]
        public class DeviareStackFrame
        {
            [ProtoMember(1)] public string ModulePath;
            [ProtoMember(2)] public string ModuleName;
            [ProtoMember(3)]
            public string NearestSymbol;
            [ProtoMember(4)]
            public UInt64 Eip;
            [ProtoMember(5)]
            public UInt64 Offset;
            [ProtoMember(6)]
            public UInt64 ModuleAddress;

            public string StackTraceString
            {
                get
                {
                    var nonEmpty = Offset != 0;
                    var offset = nonEmpty ? "0x" + Offset.ToString("X") : string.Empty;
                    if (String.IsNullOrEmpty(NearestSymbol))
                        return nonEmpty ? offset : "0";
                    return NearestSymbol + (nonEmpty ? " + " : string.Empty) + offset;
                }
            }

            public DeviareStackFrame()
            {
            }
            public DeviareStackFrame(string modulePath, string moduleName, string nearestSymbol, UInt64 eip, UInt64 offset, UInt64 moduleAddress)
            {
                ModulePath = modulePath;
                ModuleName = moduleName;
                //StackCache.GetStackStrings(nearestSymbol, offset, out NearestSymbol, out StackTraceString);
                Eip = eip;
                Offset = offset;
                ModuleAddress = moduleAddress;
                NearestSymbol = nearestSymbol;

                //StackTraceString = (String.IsNullOrEmpty(nearestSymbol) ? "0x" : nearestSymbol + " + 0x") + offset.ToString("X");
            }

            public bool MatchesWith(DeviareStackFrame anotherStackFrame)
            {
                return ModuleName.Equals(anotherStackFrame.ModuleName)
                       && StackTraceString.Equals(anotherStackFrame.StackTraceString);
            }
        }

        //public static void SetModulePath(ModulePath modPath)
        //{
        //    _modPath = modPath;
        //}

        public static UInt64 GetCallAddress(List<DeviareStackFrame> stackTrace)
        {
            UInt64 address = 0;

            DeviareStackFrame sf = GetNonSystemStackFrame(stackTrace);
            if (sf != null)
                address = sf.ModuleAddress;

            return address;
        }
        public static string GetCallModule(List<DeviareStackFrame> stackTrace)
        {
            var module = "";

            var sf = GetNonSystemStackFrame(stackTrace);
            if (sf != null)
                module = sf.ModuleName;
            return module;
        }
        public static string GetStackFrameString(List<DeviareStackFrame> stackTrace)
        {
            string frame = "";
            DeviareStackFrame sf = GetNonSystemStackFrame(stackTrace);
            if (sf != null)
                frame = sf.StackTraceString;

            return frame;
        }
        /// <summary>
        /// Get the first non system frame if any. Otherwise, it returns the last frame (even if it is system)
        /// </summary>
        /// <param name="stackTrace"></param>
        /// <returns></returns>
        public static DeviareStackFrame GetNonSystemStackFrame(List<DeviareStackFrame> stackTrace)
        {
            DeviareStackFrame retSf = null;

            // find the first module that is not in the hidden modules
            foreach (var sf in stackTrace)
            {
                if (String.IsNullOrEmpty(sf.ModuleName)) continue;
                retSf = sf;
                if(IsSystemStackFrame(sf))
                    break;
            }

            // if not found return the last system module

            return retSf;
        }
        public static bool IsSystemStackFrame(DeviareStackFrame sf)
        {
            return IsSystemModule(sf.ModuleName);
        }
        public static bool IsSystemModule(string modName)
        {
            var lowerModuleName = modName.ToLower();
            return SystemModules.Contains(lowerModuleName) || lowerModuleName.EndsWith(".sys");
        }
        public static void SetStackInfo(CallEvent e, NktHookCallInfo callInfo, ModulePath modulePath)
        {
            SetStackInfo(e, callInfo, modulePath, false);
        }

        public static void SetStackInfo(CallEvent e, NktHookCallInfo callInfo, ModulePath modulePath, bool allFrames)
        {
            var stackTrace = new List<DeviareStackFrame>();
            GetStackTrace(callInfo, stackTrace, modulePath, allFrames);
            string module = GetCallModule(stackTrace);
            e.CallModule = module;
            e.CallStack = stackTrace;
        }
        public static void GetStackTrace(NktHookCallInfo callInfo, ModulePath modulePath)
        {
            GetStackTrace(callInfo, 0, null, modulePath);
        }
        public static void GetStackTrace(NktHookCallInfo callInfo, List<DeviareStackFrame> stackTrace, ModulePath modulePath)
        {
            GetStackTrace(callInfo, stackTrace, modulePath, false);
        }
        public static void GetStackTrace(NktHookCallInfo callInfo, List<DeviareStackFrame> stackTrace, ModulePath modulePath, bool allFrames)
        {
            GetStackTrace(callInfo, allFrames ? 1000 : Settings.Default.MaxStackTraceDepth, stackTrace, modulePath);
        }
        public static void GetStackTrace(NktHookCallInfo callInfo, int count, List<DeviareStackFrame> stackTrace, ModulePath modPath)
        {
            bool foundNonSystem = false;
            INktStackTrace st = callInfo.StackTrace();
            int i;
            for (i = 0; i < Settings.Default.MaxStackTraceDepth; )
            {
                var addr = (UInt64)st.Address(i);
                if (addr == 0)
                    break;

                var offset = (UInt64)st.Offset(i);
                NktModule mod = st.Module(i);
                UInt64 modBaseAddr = 0;
                string modulePath = "", moduleName = "";

                if (mod != null)
                {
                    var pid = (uint)callInfo.Process().Id;
                    modBaseAddr = (IntPtr.Size == 4) ? (UInt32) mod.BaseAddress : (UInt64) mod.BaseAddress;

                    ModulePath.ModuleInfo modInfo;
                    if(!modPath.TryGetModuleByAddress(pid, modBaseAddr, out modInfo))
                    {
                        //Trace.WriteLine("Module " + module + " " + mod.Path + " " + mod.BaseAddress);
                        modInfo = modPath.AddModule(mod.Path, pid, modBaseAddr);
                    }
                    
                    modulePath = modInfo.Path;
                    moduleName = modInfo.Name;

                    if (!SystemModules.Contains(moduleName) && !String.IsNullOrEmpty(moduleName))
                        foundNonSystem = true;
                    //Marshal.ReleaseComObject(mod);
                }
                stackTrace.Add(new DeviareStackFrame(modulePath, moduleName, st.NearestSymbol(i), addr, offset, modBaseAddr));
                i++;
                if (foundNonSystem && i >= Settings.Default.MinStackTraceDepth)
                    break;
            }
        }

        public static Param GetParamByName(string name, IEnumerable<Param> pms)
        {
            return pms.FirstOrDefault(p => p.Name == name);
        }
    }
    public class DeviareHookInfoGenerator
    {
        readonly NktSpyMgr _spyMgr;

        public DeviareHookInfoGenerator(NktSpyMgr spyMgr)
        {
            _spyMgr = spyMgr;
        }
        public int GenerateFunctionInfo(string module, string group, string file, bool before)
        {
            return GenerateFunctionInfo(module, group, file, before, "", 1, true);
        }
        public int GenerateFunctionInfo(string module, string group, string file, bool before, string traceFile, int defaultPriority, bool matchCase)
        {
            int count = 0;
            int pid = Process.GetCurrentProcess().Id;

            INktProcess proc = _spyMgr.ProcessFromPID(pid);
            INktModulesEnum mods = proc.Modules();

            NktModule mod = mods.GetByName(module);
            if (mod == null)
            {
                IntPtr hMod = Declarations.LoadLibrary(module);
                mods = proc.Modules();
                mod = mods.GetByName(module);
            }
            if (mod != null)
            {
                var doc = new XmlDocument();
                var docTrace = new XmlDocument();
                XmlElement hooks = doc.CreateElement("hooks");
                INktExportedFunctionsEnum enumFncs = mod.ExportedFunctions();
                INktExportedFunction function = enumFncs.First();
                XmlElement traceElem = docTrace.CreateElement("trace-info");
                docTrace.AppendChild(traceElem);
                doc.AppendChild(hooks);

                while (function != null)
                {
                    string name = function.Name;
                    if (name != "" && function.IsForwarded == false)
                    {
                        INktDbObject fncDbEntry = function.DbObject();
                        if (fncDbEntry != null)
                        {


                            XmlElement hook = doc.CreateElement("hook");

                            hooks.AppendChild(hook);

                            string displayName;
                            XmlElement n = doc.CreateElement("return");
                            XmlAttribute a;

                            if (before)
                            {
                                a = doc.CreateAttribute("before");
                                a.InnerText = "true";
                                hook.Attributes.Append(a);
                            }
                            hook.AppendChild(n);
                            n.InnerText = "HRESULT";

                            n = doc.CreateElement("function");
                            hook.AppendChild(n);
                            if (name.EndsWith("ExA") || name.EndsWith("ExW"))
                            {
                                displayName = name.Substring(0, name.Length - 3);
                            }
                            else if (name.EndsWith("A") || name.EndsWith("W") || name.EndsWith("2") || name.EndsWith("3"))
                            {
                                displayName = name.Substring(0, name.Length - 1);
                            }
                            else
                                displayName = name;

                            a = doc.CreateAttribute("displayName");
                            a.InnerText = displayName;
                            n.Attributes.Append(a);
                            n.InnerText = function.CompleteName;

                            n = doc.CreateElement("group");
                            n.InnerText = group;
                            hook.AppendChild(n);
                            n = doc.CreateElement("functionString");
                            hook.AppendChild(n);

                            int paramCount = fncDbEntry.ItemsCount;
                            for (int i = 0; i < paramCount; i++)
                            {
                                NktDbObject param = fncDbEntry.Item(i);
                                if (param.Class == eNktDboClass.clsReference && param.ItemsCount > 0)
                                {
                                    param = param.Item(0);
                                    if (param.Name.EndsWith("ID") && param.ItemsCount == 4)
                                    {

                                    }
                                }

                            }
                            name = fncDbEntry.Declaration;
                        }

                        /*
                            if (fncDbEntry != null)
                            {
                                XmlElement hook = doc.CreateElement("hook");
                                DeviareDB.IParamEntries paramEntries = fncDbEntry.Items();
                                DeviareDB.IEnumParamEntries enumParams = paramEntries.Enumerator;
                                DeviareDB.IParamEntry pentry = enumParams.First;

                                hooks.AppendChild(hook);

                                string displayName;
                                XmlElement n = doc.CreateElement("return");
                                XmlAttribute a;

                                if (before)
                                {
                                    a = doc.CreateAttribute("before");
                                    a.InnerText = "true";
                                    hook.ReadAttributes.Append(a);
                                }
                                hook.AppendChild(n);
                                n.InnerText = "HRESULT";

                                n = doc.CreateElement("function");
                                hook.AppendChild(n);
                                if (name.EndsWith("ExA") || name.EndsWith("ExW"))
                                {
                                    displayName = name.Substring(0, name.Length - 3);
                                }
                                else if (name.EndsWith("A") || name.EndsWith("W") || name.EndsWith("2") || name.EndsWith("3"))
                                {
                                    displayName = name.Substring(0, name.Length - 1);
                                }
                                else
                                    displayName = name;

                                a = doc.CreateAttribute("displayName");
                                a.InnerText = displayName;
                                n.ReadAttributes.Append(a);
                                n.InnerText = function.CompleteName;

                                n = doc.CreateElement("group");
                                n.InnerText = group;
                                hook.AppendChild(n);
                                name += "(";
                                n = doc.CreateElement("functionString");
                                hook.AppendChild(n);

                                int paramNumber = 0;
                                int paramIndex = 0;
                                while (pentry != null)
                                {
                                    String paramString = "";
                                    DeviareDB.ITypeEntry tentry = pentry.Type;
                                    XmlElement param = null;
                                    if (tentry != null)
                                    {
                                        if (tentry.Name == pentry.Name)
                                        {
                                            paramString = tentry.Name;
                                        }
                                        else
                                        {
                                            paramString = tentry.Name + " " + pentry.Name;
                                        }

                                        if (tentry.PointerEntry != 0 && (tentry.BasicType.Name == "char" || tentry.BasicType.Name == "wchar_t"))
                                        {
                                            DeviareDB.ITypeEntry anc = tentry.Ancestor;
                                            param = doc.CreateElement("param");

                                            // verify if it's a pointer to a string or a string
                                            if (anc != null && anc.PointerEntry != 0)
                                            {
                                                // handle only pointer to string. If it's a pointer to a pointer to a string skip
                                                if (tentry.Ancestor.Ancestor.PointerEntry == 0)
                                                {
                                                    a = doc.CreateAttribute("pointer");
                                                    a.InnerText = "true";
                                                    param.ReadAttributes.Append(a);
                                                }
                                                else
                                                    param = null;
                                            }
                                        }
                                        else if (tentry.PointerEntry != 0 && tentry.BasicType.Name == "_GUID")
                                        {
                                            param = doc.CreateElement("param");
                                            a = doc.CreateAttribute("context");
                                            a.InnerText = "IID";
                                            param.ReadAttributes.Append(a);
                                            a = doc.CreateAttribute("pointer");
                                            a.InnerText = "true";
                                            param.ReadAttributes.Append(a);
                                        }
                                        else if (tentry.BasicType.Name == "HINSTANCE")
                                        {
                                            param = doc.CreateElement("param");
                                            a = doc.CreateAttribute("context");
                                            a.InnerText = "HMODULE";
                                            param.ReadAttributes.Append(a);
                                        }
                                        else if (tentry.BasicType.Name == "HWND")
                                        {
                                            param = doc.CreateElement("param");
                                            a = doc.CreateAttribute("context");
                                            a.InnerText = "HWND";
                                            param.ReadAttributes.Append(a);
                                        }
                                        else if (tentry.Name == "INTERNETFEATURELIST")
                                        {
                                            param = doc.CreateElement("param");
                                            a = doc.CreateAttribute("context");
                                            a.InnerText = "INTERNETFEATURELIST";
                                            param.ReadAttributes.Append(a);
                                        }
                                    }

                                    if (param != null)
                                    {
                                        XmlElement p = doc.CreateElement("param" + paramNumber.ToString());
                                        a = doc.CreateAttribute("index");
                                        a.InnerText = paramIndex.ToString();
                                        param.ReadAttributes.Append(a);
                                        p.AppendChild(param);
                                        hook.AppendChild(p);
                                        paramNumber++;
                                    }
                                    paramIndex++;
                                    pentry = enumParams.Next;
                                    name += paramString;

                                    if (pentry != null)
                                    {
                                        name += ", ";
                                    }
                                }

                                name += ")";
                                n.InnerText = name;

                                XmlNode traceInfo = docTrace.SelectSingleNode("/trace-info/hook-type[@name = \'" + displayName + "\']");
                                if (traceInfo == null)
                                {
                                    traceInfo = docTrace.CreateElement("hook-type");
                                    a = docTrace.CreateAttribute("name");
                                    a.InnerText = displayName;
                                    traceInfo.ReadAttributes.Append(a);
                                    a = docTrace.CreateAttribute("priority");
                                    a.InnerText = defaultPriority.ToString();
                                    traceInfo.ReadAttributes.Append(a);
                                    traceElem.AppendChild(traceInfo);

                                    string param;

                                    for (int i = 0; i < 2; i++)
                                    {
                                        param = "param" + i.ToString();
                                        if (hook[param] != null)
                                        {
                                            XmlElement match = docTrace.CreateElement("match");
                                            if (i == 0)
                                            {
                                                match.InnerText = "path";
                                            }
                                            else
                                            {
                                                match.InnerText = "detail";
                                            }
                                            traceInfo.AppendChild(match);
                                            a = docTrace.CreateAttribute("case");
                                            a.InnerText = matchCase.ToString();
                                            a = hook[param].ReadAttributes["result"];
                                            if (a != null && a.InnerText.ToLower() == "true")
                                            {
                                                a = docTrace.CreateAttribute("result");
                                                traceInfo.ReadAttributes.Append(a);
                                                a.InnerText = "true";
                                            }
                                        }
                                    }
                                }
                            }
                         */
                    }
                    function = enumFncs.Next();
                }
                var tw = new XmlTextWriter(file, Encoding.UTF8);
                tw.Formatting = Formatting.Indented;

                doc.Save(tw);

                if (traceFile != "")
                {
                    var tf = new XmlTextWriter(traceFile, Encoding.UTF8) {Formatting = Formatting.Indented};

                    docTrace.Save(tf);
                }
            }

            return count;
        }
    }
}
