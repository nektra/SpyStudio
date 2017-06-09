using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml;
using Nektra.Deviare2;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using SpyStudio.Trace;
using SpyStudio.Windows.Controls;

namespace SpyStudio.Hooks
{
    /// Contains the information of the xml that defines the deviare hook loaded from xml
    public class DeviareHook
    {
        public class ParamInfo
        {
            public string HelpString = "";
            public string Type = "";
            public int Index;
            public string Context = "";
            public bool Evaluate;

            public ParamInfo()
            {
            }

            public ParamInfo(NktDbObject obj, NktDbObject parentObj, int index)
            {
                Type = obj.Name;
                if (parentObj != null)
                    HelpString = parentObj.ItemName(index);
                Index = index;
            }

            public ParamInfo Clone()
            {
                var ret = (ParamInfo) MemberwiseClone();
                return ret;
            }
        }

        public enum ReturnValueType
        {
            VOID,
            INT,
            UINT,
            HEX,
            HRESULT,
            NTSTATUS,
            BOOL
        }

        public class Parameter
        {
            public Parameter()
            {
                Before = false;
                Index = -1;
                Result = false;
            }

            public Parameter(DeviareTools.DeviareParameterPath path)
            {
                Before = false;
                Index = -1;
                Result = false;

                path.GetParameterTree();
            }

            public void Truncate(ParamInfo info, bool includeInfo)
            {
                var oldParamTree = ParamTree;
                ParamTree = new List<ParamInfo>();
                foreach (var p in oldParamTree)
                {
                    if (!includeInfo && p == info)
                        break;
                    ParamTree.Add(p);
                    if (includeInfo && p == info)
                        break;
                }
            }

            public bool Before { get; set; }
            public int Index { get; set; }
            public bool Result { get; set; }

            public List<ParamInfo> ParamTree = new List<ParamInfo>();

            public Parameter Clone()
            {
                var ret = (Parameter) MemberwiseClone();
                ret.ParamTree = new List<ParamInfo>();

                foreach (var p in ParamTree)
                {
                    ret.ParamTree.Add(p.Clone());
                }
                return ret;
            }
        }

        public class FunctionParameters
        {
            public List<Parameter> Parameters = new List<Parameter>();
        }

        private readonly List<Parameter> _parameters = new List<Parameter>();
        private NktHook _ihook;
        private readonly HashSet<string> _skipFrames = new HashSet<string>();
        private readonly NktSpyMgr _spyMgr;
        private readonly ParamHandlerManager _paramHandlerMgr;
        
        public DeviareHook(NktSpyMgr spyMgr)
        {
            OnlyAfter = true;
            OnlyBefore = false;
            ParamsBefore = false;
            _spyMgr = spyMgr;
        }

        public DeviareHook(XmlNode h, string functionName, NktSpyMgr spyMgr, ParamHandlerManager paramHandlerMgr)
        {
            _paramHandlerMgr = paramHandlerMgr;
            OnlyAfter = true;
            OnlyBefore = false;
            ParamsBefore = false;
            _spyMgr = spyMgr;

            Debug.Assert(h.Attributes != null, "h.ReadAttributes != null");
            XmlAttribute b = h.Attributes["before"];
            if (b != null)
            {
                OnlyAfter = (b.InnerText.ToLower() != "true");
            }
            b = h.Attributes["onlyBefore"];
            if (b != null)
            {
                OnlyBefore = (b.InnerText.ToLower() == "true");
                OnlyAfter = false;
            }
            b = h.Attributes["paramsAfter"];
            if (b != null)
            {
                ParamsAfter = (b.InnerText.ToLower() == "true");
            }
            else
            {
                // default
                ParamsAfter = true;
            }
            b = h.Attributes["stackBefore"];
            if (b != null)
            {
                StackBefore = (b.InnerText.ToLower() == "true");
            }
            b = h.Attributes["paramsBefore"];
            if (b != null)
            {
                ParamsBefore = (b.InnerText.ToLower() == "true");
            }
            ReturnValue = ReturnValueType.INT;
            XmlNode n = h["return"];
            if (n != null)
            {
                if (n.InnerText == "BOOL")
                {
                    ReturnValue = ReturnValueType.BOOL;
                }
                else if (n.InnerText == "HRESULT")
                {
                    ReturnValue = ReturnValueType.HRESULT;
                }
                else if (n.InnerText == "NTSTATUS")
                {
                    ReturnValue = ReturnValueType.NTSTATUS;
                }
                else if (n.InnerText == "UINT")
                {
                    ReturnValue = ReturnValueType.UINT;
                }
                else if (n.InnerText == "INT")
                {
                    ReturnValue = ReturnValueType.INT;
                }
                else if (n.InnerText == "HEX")
                {
                    ReturnValue = ReturnValueType.HEX;
                }
                else if (n.InnerText == "VOID")
                {
                    ReturnValue = ReturnValueType.VOID;
                }
            }

            Function = functionName;

            var fncNode = h["function"];
            if (fncNode != null)
            {
                b = fncNode.Attributes["displayName"];
                DisplayName = b != null ? b.InnerText : Function;
                b = fncNode.Attributes["coCreate"];
                if (b != null)
                {
                    CoCreate = (b.InnerText.ToLower() == "true");
                }

                if (!CoCreate && fncNode.InnerText.EndsWith("DllGetClassObject"))
                {
                    CoCreate = true;
                }
            }
            else
            {
                DisplayName = Function;
                CoCreate = false;
            }
            if (Function.EndsWith("W") || Function.EndsWith("A"))
            {
                string root;
                if (Function.EndsWith("ExW") || Function.EndsWith("ExA"))
                {
                    root = Function.Substring(0, Function.Length - 3);
                    _skipFrames.Add(root + "ExA");
                    _skipFrames.Add(root + "ExW");
                }
                else
                {
                    root = Function.Substring(0, Function.Length - 1);
                    _skipFrames.Add(root);
                    _skipFrames.Add(root + "A");
                    _skipFrames.Add(root + "W");
                }
            }
            else if (Function.EndsWith("Ex"))
            {
                string root = Function.Substring(0, Function.Length - 2);
                _skipFrames.Add(root + "ExA");
                _skipFrames.Add(root + "ExW");
            }

            n = h["skipCalls"];
            if(n != null)
            {
                var frames = n.SelectNodes("callerFrame");
                if (frames != null)
                {
                    foreach (XmlNode f in frames)
                    {
                        _skipFrames.Add(f.InnerText);
                    }
                }
            }
            if (_skipFrames.Count > 0)
            {
                StackBefore = true;
            }

            n = h["group"];
            Group = n != null ? n.InnerText : "None";

            var nodeList = h.SelectNodes("param");
            if (nodeList != null)
            {
                foreach (XmlNode n1 in nodeList)
                {
                    var p = ResolveParam(n1);
                    _parameters.Add(p);
                }
            }
        }

        public NktSpyMgr SpyMgr
        {
            get { return _spyMgr; }
        }

        public string DisplayName { get; set; }
        public string Group { get; set; }
        public ReturnValueType ReturnValue { get; set; }
        public bool CoCreate;
        public string Function { get; set; }
        public NktExportedFunction FunctionObject { get; set; }
        public ModulePath ModulePath { get; set; }
        public ProcessInfo ProcessInfo { get; set; }
        public WindowClassNames WindowClassNames { get; set; }

        public List<Parameter> Parameters
        {
            get { return _parameters; }
        }

        private void AddParameter(Parameter p)
        {
            _parameters.Add(p);
        }

        public Parameter CreateParameter(int index, DeviareTools.DeviareParameterPath path)
        {
            var paramTree = path.GetParameterTree();
            var newParam = paramTree.Clone();
            AddParameter(newParam);
            return newParam;

            //var param = new Parameter {Index = _parameters.Count};





            //path.
            //var paramInfo = new ParamInfo {HelpString = p.Name, Evaluate = false, Result = false, Type = p.Name, Index = index};
            ////param.ParamTree.Add(paramInfo);
            //AddParameter(param);
            //return param;
        }

        public Parameter ResolveParam(XmlNode n)
        {
            var ret = new Parameter();
            Debug.Assert(n.Attributes != null, "Node.ReadAttributes != null");
            XmlAttribute b = n.Attributes["before"];
            if (b != null)
            {
                ret.Before = (b.InnerText.ToLower() == "true");
            }

            ParamInfo rootParamInfo = null;
            XmlNode p = n;
            while (p != null)
            {
                var paramInfo = new ParamInfo();
                if (rootParamInfo == null)
                    rootParamInfo = paramInfo;

                Debug.Assert(p.Attributes != null, "p.ReadAttributes != null");
                XmlAttribute a = p.Attributes["type"];
                if (a != null)
                {
                    paramInfo.Type = a.InnerText;
                }
                a = n.Attributes["result"];
                if (a != null)
                {
                    ret.Result = (a.InnerText.ToLower() == "true");
                }
                a = p.Attributes["helpString"];
                if (a != null && string.IsNullOrEmpty(rootParamInfo.HelpString))
                {
                    rootParamInfo.HelpString = a.InnerText;
                }
                a = p.Attributes["context"];
                if (a != null)
                {
                    paramInfo.Context = a.InnerText;
                }
                a = p.Attributes["index"];
                if (a != null)
                {
                    paramInfo.Index = Convert.ToInt32(a.InnerText, CultureInfo.InvariantCulture);
                }
                else
                {
                    paramInfo.Index = -1;
                }
                a = p.Attributes["pointer"];
                if (a != null)
                {
                    paramInfo.Evaluate = (a.InnerText.ToLower() == "true");
                }
                ret.ParamTree.Add(paramInfo);
                //ret.Add(paramInfo);
                p = p["param"];
            }
            return ret;
        }

        public bool StackBefore { get; set; }
        public bool ParamsBefore { get; set; }
        public bool OnlyBefore { get; set; }
        public bool OnlyAfter { get; set; }
        public bool ParamsAfter { get; set; }

        public string FunctionName
        {
            get
            {
                if (FunctionObject != null) return FunctionObject.CompleteName;
                return Function;
            }
        }

        public IntPtr CreateDeviareObject()
        {
            return CreateDeviareObject(0);
        }

        public IntPtr CreateDeviareObject(int otherProps)
        {
            return CreateDeviareObject(otherProps, out otherProps);
        }

        private void SetProps(int otherProps, out int props)
        {
            if (OnlyAfter)
                otherProps |= (int)eNktHookFlags.flgOnlyPostCall;
            else if (OnlyBefore)
                otherProps |= (int)eNktHookFlags.flgOnlyPreCall;
            props = otherProps;
        }

        public IntPtr CreateDeviareObject(int otherProps, out int props)
        {
            SetProps(otherProps, out props);
            _ihook = FunctionObject == null
                                ? _spyMgr.CreateHook(Function, props)
                                : _spyMgr.CreateHook(FunctionObject, props);
            return _ihook.Id;
        }

        public IntPtr CreateDeviareObject(IntPtr function, string functionName, int otherProps, out int props)
        {
            SetProps(otherProps, out props);
            _ihook = _spyMgr.CreateHookForAddress(function, functionName, props);
            return _ihook.Id;
        }

        public void Start()
        {
            _ihook.Hook(true);
        }

        public void Stop()
        {
            _ihook.Unhook(false);
        }

        public void Attach(NktProcess proc)
        {
            _ihook.Attach(proc, true);
        }

        public void Detach(NktProcess proc)
        {
            _ihook.Detach(proc, false);
        }

        public NktHook HookObject
        {
            get { return _ihook; }
        }

        public virtual bool ProcessEvent(bool before, NktHookCallInfo callInfo, CallEvent callEvent, NktProcess proc)
        {
            bool ret = ProcessFunction(before, callInfo, callEvent, proc);
            if (ret)
                ret = ProcessParameters(before, callInfo, callEvent, proc);
            return ret;
        }

        public virtual bool ProcessParameters(bool before, NktHookCallInfo callInfo, CallEvent callEvent,
                                              NktProcess proc)
        {
            var parameters = new List<Param>();
            foreach (var p in _parameters)
            {
                if ((((!before && ParamsAfter) || (before && (OnlyBefore || ParamsBefore || p.Before))) && (!p.Result || callEvent.Success)))
                {
                    string name = string.Empty;
                    string newVal = GetParamValue(p.ParamTree, callInfo, callEvent.Pid, callEvent.Tid, proc,
                                                  ref name);
                    //if (string.IsNullOrEmpty(name))
                    //    name = "param" + i.ToString(CultureInfo.InvariantCulture);

                    parameters.Add(new Param(name, newVal));
                }
            }
            callEvent.CreateParams(parameters.ToArray());

            return true;
        }

        public virtual bool ProcessFunction(bool before, NktHookCallInfo callInfo, CallEvent callEvent,
                                            NktProcess proc)
        {
            callEvent.Function = DisplayName;
            if (_skipFrames.Count > 0)
            {
                if (callEvent.CallStack.Count > 0 && _skipFrames.Contains(callEvent.CallStack[0].NearestSymbol))
                {
                    return false;
                }
            }

            if (!before)
            {
                switch (ReturnValue)
                {
                    case ReturnValueType.BOOL:
                        callEvent.Result = (callEvent.RetValue != 0 ? "SUCCESS" : "FAIL");
                        callEvent.Success = (callEvent.RetValue != 0);
                        break;
                    case ReturnValueType.VOID:
                        callEvent.Result = "";
                        callEvent.Success = true;
                        break;
                    case ReturnValueType.INT:
                        callEvent.Result = callEvent.RetValue.ToString(CultureInfo.InvariantCulture);
                        callEvent.Success = true;
                        break;
                    case ReturnValueType.UINT:
                        if (Properties.Settings.Default.ShowUINTAsHex)
                            callEvent.Result = "0x" + callEvent.RetValue.ToString("X");
                        else
                            callEvent.Result = callEvent.RetValue.ToString(CultureInfo.InvariantCulture);
                        callEvent.Success = true;
                        break;
                    case ReturnValueType.HEX:
                        callEvent.Result = "0x" + callEvent.RetValue.ToString("X");
                        callEvent.Success = true;
                        break;
                    case ReturnValueType.HRESULT:
                        callEvent.Result = Declarations.HresultErrorToString(callEvent.RetValue);
                        callEvent.Success = (callEvent.RetValue == 0);
                        break;
                    case ReturnValueType.NTSTATUS:
                        callEvent.Result = Declarations.NtStatusToString(callEvent.RetValue);
                        callEvent.Success = (callEvent.RetValue < 0x80000000);
                        break;
                }
            }
            else
            {
                callEvent.OnlyBefore = OnlyBefore;
            }

            return true;
        }

        private string GetParamValue(List<ParamInfo> paramPath, NktHookCallInfo callInfo, uint pid, uint tid,
                                     NktProcess proc, ref string name)
        {
            string ret = "";
            bool nullPointer = false;
            if (paramPath.Count > 0)
            {
                NktParam param = null;
                ParamInfo lastParamInfo = null;
                foreach (ParamInfo paramInfo in paramPath)
                {
                    if (param == null)
                    {
                        if (!string.IsNullOrEmpty(paramInfo.HelpString))
                            name = paramInfo.HelpString;

                        NktParamsEnum pms = callInfo.Params();
                        if (pms != null && paramInfo.Index < pms.Count)
                            param = pms.GetAt(paramInfo.Index);
                    }
                    else
                    {
                        // if -1, apply to the same parameter. Useful when you need to Evaluate and then CastTo
                        if (paramInfo.Index != -1)
                            param = param.FieldsCount != 0 ? param.Field(paramInfo.Index) : null;
                    }
                    if (param == null)
                        break;

                    if (paramInfo.Type != "")
                        param = param.CastTo(paramInfo.Type);
                    if (paramInfo.Evaluate)
                    {
                        if (param.IsNullPointer)
                        {
                            nullPointer = true;
                            ret = "(null)";
                            break;
                        }
                        param = param.Evaluate();
                    }
                    lastParamInfo = paramInfo;
                }
                if (param != null && !nullPointer)
                {
                    object val;

                    if (!param.IsAnsiString && !param.IsWideString && param.IsPointer)
                    {
                        val = !param.IsNullPointer ? param.PointerVal : IntPtr.Zero;
                    }
                    else
                    {
                        if (param.FieldsCount == 0)
                            val = param.Value;
                        else
                        {
                            // structures here
                            if (lastParamInfo.Context == "IID")
                                val = DeviareTools.ClsIdStruct2String(param);
                            else
                            {
                                Debug.Assert(false);
                                val = "";
                            }
                        }
                    }
                    
                    if (val is int)
                    {
                        ret = "";
                        var valueCasted = (int) val;

                        if (lastParamInfo.Context != "")
                        {
                            ret = _paramHandlerMgr.TranslateParam(lastParamInfo.Context, valueCasted);
                        }
                        if (ret == "")
                        {
                            ret = valueCasted.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    else if (val is uint)
                    {
                        ret = "";
                        var valueCasted = (uint) val;

                        if (lastParamInfo.Context != "")
                        {
                            ret = _paramHandlerMgr.TranslateParam(lastParamInfo.Context, valueCasted);

                            switch (lastParamInfo.Context)
                            {
                                case "HWND":
                                    ret = WindowClassNames.GetClassName((UIntPtr) valueCasted);
                                    break;
                                case "HMODULE":
                                    ret = DeviareTools.GetParamModule(ModulePath, callInfo, param, proc);
                                    break;
                                case "HKEY":
                                    //var hKey = ToUInt64(p.SizeTVal);
                                    ret = RegistryTools.GetFullKey(RegistryTools.ToUInt64(param.SizeTVal), "", pid, tid);
                                    break;
                                case "HFILE":
                                    //var hKey = ToUInt64(p.SizeTVal);
                                    ret = FileSystemTools.GetFileHandlePath(param.SizeTVal, pid);
                                    break;
                            }
                            //else if (lastParamInfo.context == "HINTERNET")
                            //{
                            //    ret = GetUrl(valueCasted, callInfo, pid, tid);
                            //}
                        }
                        if (ret == "")
                        {
                            if (Properties.Settings.Default.ShowUINTAsHex)
                                ret = "0x" + valueCasted.ToString("X");
                            else
                                ret = valueCasted.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    else if (val is string)
                    {
                        if (lastParamInfo.Context == "IID")
                        {
                            ret = ((string) val).ToUpper();
                        }
                        else if (lastParamInfo.Context == "CLASSNAME")
                        {
                            var valInt = param.IntResourceString; //return > 0 if atom

                            if (valInt > 0)
                            {
                                ret = "0x" + valInt.ToString("X");
                            }
                            else
                            {
                                ret = param.ReadString();
                            }
                        }
                        else
                        {
                            ret = val.ToString();
                        }
                    }
                    else if (val is IntPtr)
                    {
                        bool dontUseDefault = false;
                        var valueCasted = (IntPtr) val;
                        switch (lastParamInfo.Context)
                        {
                            case "ADDRESS":
                                {
                                    ret = DeviareTools.GetAddressRepresentation(callInfo, valueCasted);
                                }
                                break;
                            case "HTHREAD":
                                {
                                    var hRemoteProc = ProcessInfo.GetProcessHandle(pid);
                                    // interpret NULL as current thread
                                    if (valueCasted == IntPtr.Zero)
                                    {
                                        ret = tid.ToString(CultureInfo.InvariantCulture);
                                    }
                                    else if (hRemoteProc != IntPtr.Zero)
                                    {
                                        IntPtr localhThread;
                                        if (Declarations.DuplicateHandle(hRemoteProc, valueCasted,
                                                                         Process.GetCurrentProcess().Handle,
                                                                         out localhThread, 0, false, 0x2))
                                        {
                                            var tbi = new Declarations.THREAD_BASIC_INFORMATION();

                                            var ntStatus = Declarations.NtQueryInformationThread(localhThread, 0,
                                                                                                 ref tbi,
                                                                                                 Marshal.SizeOf(tbi),
                                                                                                 IntPtr.Zero);
                                            if (ntStatus == 0)
                                            {
                                                ret = tbi.ClientId.UniqueThread.ToString(CultureInfo.InvariantCulture);
                                            }
                                            Declarations.CloseHandle(localhThread);
                                        }
                                    }
                                }
                                break;
                            case "LPINTERNET_BUFFERS":
                                {
                                    if (Properties.Settings.Default.ReportInternetReadFileBuffers)
                                    {
                                        NktParam s = param.Evaluate();
                                        ret += GetInternetBufferString(s);

                                        NktParam p1 = s.Field(1);
                                        while (!p1.IsNullPointer)
                                        {
                                            p1 = p1.Evaluate();
                                            ret += GetInternetBufferString(p1);

                                            p1 = p1.Field(1);
                                        }
                                        dontUseDefault = true;
                                    }
                                    break;
                                }
                            case "FILE_POBJECT_ATTRIBUTES":
                                {
                                    ret = FileSystemTools.GetFileHandlePath(param, pid);
                                    ret = FileSystemTools.GetCanonicalPathName((uint) proc.Id, ret, ProcessInfo);
                                    dontUseDefault = true;
                                }
                                break;
                            case "PUNICODE_STRING":
                                {
                                    ret = NativeApiTools.GetUnicodeString(param);
                                    dontUseDefault = true;
                                }
                                break;
                            case "PANSI_STRING":
                                {
                                    ret = NativeApiTools.GetAnsiString(param);
                                    dontUseDefault = true;
                                }
                                break;
                        }
                        if (ret == "" && !dontUseDefault)
                        {
                            ret = "0x" + valueCasted.ToString("X");
                        }
                    }
                    else if (val is UIntPtr)
                    {
                        ret = "";
                        var valueCasted = (UIntPtr)val;
                        if (lastParamInfo.Context != "")
                        {
                            if (lastParamInfo.Context == "HWND")
                            {
                                ret = WindowClassNames.GetClassName(valueCasted);
                            }
                            else if (lastParamInfo.Context == "HMODULE")
                            {
                                ret = DeviareTools.GetParamModule(ModulePath, callInfo, param, proc);
                            }
                            //else if (lastParamInfo.context == "HINTERNET")
                            //{
                            //    ret = GetUrl(valueCasted, pid, tid);
                            //}
                        }
                        if (string.IsNullOrEmpty(ret))
                        {
                            ret = "0x" + valueCasted.ToUInt64().ToString("X");
                        }
                    }
                    else if (val is Int64)
                    {
                        var valueCasted = (Int64)val;
                        ret = valueCasted.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (val is UInt64)
                    {
                        var valueCasted = (UInt64)val;
                        switch (lastParamInfo.Context)
                        {
                            case "HWND":
                                ret = WindowClassNames.GetClassName((UIntPtr)valueCasted);
                                break;
                            case "HMODULE":
                                ret = DeviareTools.GetParamModule(ModulePath, callInfo, param, proc);
                                break;
                            case "HKEY":
                                //var hKey = ToUInt64(p.SizeTVal);
                                ret = RegistryTools.GetKeyPath(RegistryTools.ToUInt64(param.SizeTVal), pid);
                                break;
                            case "HFILE":
                                //var hKey = ToUInt64(p.SizeTVal);
                                ret = FileSystemTools.GetFileHandlePath(param.SizeTVal, pid);
                                break;
                        }
                        if (string.IsNullOrEmpty(ret))
                        {
                            if (Properties.Settings.Default.ShowUINTAsHex)
                                ret = "0x" + valueCasted.ToString("X");
                            else
                                ret = valueCasted.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    else
                    {
                        ret = val.GetType() + ": " + val;
                    }
                }
            }
            return ret;
        }

        private string GetInternetBufferString(NktParam p)
        {
            string ret = "";
            var p1 = p.Field(2);
            if (!p1.IsNullPointer)
            {
                if (!string.IsNullOrEmpty(ret))
                    ret += "\n";
                ret += p1.ReadString();
            }
            p1 = p.Field(5);
            if (!p1.IsNullPointer)
            {
                if (!string.IsNullOrEmpty(ret))
                    ret += "\n";
                p1 = p1.CastTo("LPSTR");
                ret += p1.ReadString();
            }
            return ret;
        }
    }
}