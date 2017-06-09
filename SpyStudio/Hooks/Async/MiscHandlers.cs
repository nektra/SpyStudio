using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Nektra.Deviare2;
using SpyStudio.COM.Controls;
using SpyStudio.Tools;
using SpyStudio.COM;

namespace SpyStudio.Hooks.Async
{
    public class HandleCustomHook : EventHandler
    {
        private string PostProcessValue(CallContext ctx)
        {
            var pp = ctx.E.GetString();
            var s = ctx.E.GetString();
            if (pp.Length == 0)
                return s;
            if (pp.StartsWith("CONTEXT "))
            {
                var strings = pp.Split(' ');
                if (strings.Length > 2)
                    return s + " (" + strings[1] + ", " + strings[2] + ")";

                return s + " (" + strings[1] + ")";
            }
            if (!ctx.Hm.PostProcessors.ContainsKey(pp))
                return s;
            return ctx.Hm.PostProcessors[pp](s, ctx);
        }

        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.RetValue = ctx.E.GetULong();
            ctx.Ce.Result = PostProcessValue(ctx);
            ctx.Ce.Success = ctx.E.GetInt() != 0;
        }

        protected override string GetDisplayName(CallContext ctx, string functionName, string displayName)
        {
            return ctx.E.GetString();
        }

        protected override void ProcessParams(CallContext ctx)
        {
            Debug.Assert(ctx.E.Strings.Count % 3 == 0);
            ctx.Ce.CreateParams(ctx.E.Strings.Count / 3);
            var i = 0;
            while (ctx.E.Strings.Count >= 3)
            {
                ctx.Ce.Params[i].Name = ctx.E.GetString();
                ctx.Ce.Params[i].Value = PostProcessValue(ctx);
                i++;
            }
        }
    }

    public class HandleLoadLibrary : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.Result = Declarations.NtStatusToString(ctx.Ce.RetValue = ctx.E.GetULong());
            ctx.Ce.Success = ctx.Ce.RetValue < 1UL << 31;
        }

        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
            {
                LoadLibraryEvent.CreateEventParams(ctx.Ce, ctx.E.GetString(), 0);
                return;
            }
            var dllName = ctx.E.GetString();
            var ulongAddr = ctx.E.GetULong();
            LoadLibraryEvent.CreateEventParams(ctx.Ce, dllName, ulongAddr);
        }

        protected override HookType GetHookType()
        {
            return HookType.LoadLibrary;
        }
    }

    public class HandleCoCreateInstance : EventHandler
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var s = ctx.E.GetString();
            if (s.Length != 0 && (s.Length < 2 || s[0] != '{' || s[s.Length - 1] != '}'))
                s = "{" + s + "}";
            ctx.Ce.CreateEventParams(s);
        }

        protected override HookType GetHookType()
        {
            return HookType.CoCreate;
        }
    }

    public class HandleCreateProcessInternal : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.RetValue = ctx.E.GetULong();
            ctx.Ce.Result = ctx.Ce.RetValue == 0 ? "ERROR" : "SUCCESS";
            ctx.Ce.Success = ctx.Ce.RetValue != 0;
        }
        protected override void ProcessParams(CallContext ctx)
        {
            var processPath = ctx.E.GetString();
            var cmdLine = ctx.E.GetString();
            if (ctx.Ce.Before)
            {
                //Discard pid
                ctx.E.Discard();
                CreateProcessEvent.CreateEventParams(ctx.Ce, processPath, cmdLine);
            }
            else
            {
                var pid = ctx.E.GetUInt();
                CreateProcessEvent.CreateEventParams(ctx.Ce, processPath, cmdLine, pid);
                var pi = ctx.Hm.HookMgr.ProcessInfo;
                lock (pi)
                {
                    if (!pi.Contains(ctx.Ce.Pid))
                        pi.Add(ctx.Ce.ProcessName, ctx.Ce.ProcessPath, ctx.Ce.Pid);
                }
            }
        }

        protected override HookType GetHookType()
        {
            return HookType.CreateProcess;
        }
    }

    public class HandleServices : EventHandler
    {
        protected virtual int ParamCount
        {
            get { return 1; }
        }
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.RetValue = ctx.E.GetULong();
            ctx.Ce.Result = Win32Error2String.ToString((uint)ctx.Ce.RetValue);
            if (ctx.Ce.Result == null)
                ctx.Ce.Result = ctx.Ce.RetValue == 0 ? "ERROR" : "SUCCESS";
            ctx.Ce.Success = ctx.Ce.RetValue == 0;
        }
        protected virtual void ProcessParamsInner(CallContext ctx){}

        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
            {
                ctx.Ce.CreateParams(ParamCount);
                ctx.Ce.Params[0].Name = "ServiceName";
                ctx.Ce.Params[0].Value = ctx.E.GetString();
                ProcessParamsInner(ctx);
                ctx.Ce.ParamMainIndex = 0;
            }
            else
                ctx.Ce.CreateParams(0);
        }
    }

    public class HandleCreateService : HandleServices
    {
        protected override int ParamCount
        {
            get { return 2; }
        }
        protected override void ProcessParamsInner(CallContext ctx)
        {
            ctx.Ce.Params[1].Name = "CommandLine";
            ctx.Ce.Params[1].Value = ctx.E.GetString();
        }

        protected override HookType GetHookType()
        {
            return HookType.CreateService;
        }
    }

    public class HandleOpenService : HandleServices
    {
        protected override HookType GetHookType()
        {
            return HookType.OpenService;
        }
    }

    public class HandleFindResource : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            var ret = ctx.E.GetULong();
            ctx.Ce.RetValue = ret;
            ctx.Ce.Success = ret != 0;
            ctx.Ce.Result = ctx.Ce.Success ? "SUCCESS" : "ERROR";
        }

        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
                return;

            var ce = ctx.Ce;

            var type = ctx.E.GetString();
            var name = ctx.E.GetString();
            var language = ctx.E.GetString();
            if (string.IsNullOrEmpty(language))
                language = CultureInfo.CurrentCulture.ToString();
            var dllPath = ctx.E.GetString();

            UInt64 addr;
            if (UInt64.TryParse(dllPath, out addr) && !ctx.Hm.HookMgr.ModulePath.TryGetPathByAddress((uint)ctx.Proc.Id, addr, out dllPath))
            {
                dllPath = "<0x" + addr.ToString("X") + ">";
                FileSystemEvent.SetModuleNotFound(ce, true);
            }

            ce.CreateParams(4);
            ce.Params[0] = new Param("Path", dllPath);
            ce.Params[1] = new Param("Type", type);
            ce.Params[2] = new Param("Name", name);
            ce.Params[3] = new Param("Language", language);
            FileSystemEvent.SetAccess(ce, FileSystemAccess.Resource);
        }

        protected override HookType GetHookType()
        {
            return HookType.FindResource;
        }
    }

    public class HandleLoadResource : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            var ret = ctx.E.GetULong();
            ctx.Ce.RetValue = ret;
            ctx.Ce.Success = ret != 0;
            ctx.Ce.Result = ctx.Ce.Success ? "SUCCESS" : "ERROR";
        }

        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
                return;

            
            var ce = ctx.Ce;

            var dllPath = ctx.E.GetString();

            UInt64 addr;
            if (UInt64.TryParse(dllPath, out addr) && !ctx.Hm.HookMgr.ModulePath.TryGetPathByAddress((uint)ctx.Proc.Id, addr, out dllPath))
            {
                dllPath = "<0x" + addr.ToString("X") + ">";
                FileSystemEvent.SetModuleNotFound(ce, true);
            }

            ce.CreateParams(1);
            ce.Params[0] = new Param("hModule", dllPath);
            FileSystemEvent.SetAccess(ce, FileSystemAccess.Resource);
        }

        protected override HookType GetHookType()
        {
            return HookType.FindResource;
        }
    }

    public class HandleGetClassObject : EventHandler
    {
        readonly private string _modulePath;
        public HandleGetClassObject()
        {
        }

        public HandleGetClassObject(string mod)
        {
            _modulePath = mod;
        }

        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.RetValue = ctx.E.GetULong();
            ctx.Ce.Success = ctx.Ce.RetValue != 0;
            ctx.Ce.Result = ctx.Ce.Success ? "SUCCESS" : "ERROR";
        }

        protected override void ProcessParams(CallContext ctx)
        {
            var clsid = ctx.E.GetString();
            ctx.Ce.CreateEventParams(clsid);
            var modPath = _modulePath;
            if (string.IsNullOrEmpty(modPath))
                modPath = ctx.Ce.Function.Substring(0, ctx.Ce.Function.LastIndexOf('.'));
            if (string.IsNullOrEmpty(ctx.Ce.GetServer()) && string.IsNullOrEmpty(ctx.Ce.GetDescription()))
                ComServerInfoMgr.GetInstance().SetComServer(clsid, modPath);
            // force this module as server
            ctx.Ce.SetServer(modPath);
        }

        protected override string GetDisplayName(CallContext ctx, string functionName, string displayName)
        {
            if (functionName == "")
                return functionName;
            var dot = functionName.IndexOf('!');
            if (dot > 0)
                return functionName.Substring(0, 1).ToUpper() + functionName.Substring(1, dot - 1).ToLower() + "." + functionName.Substring(dot + 1);
            return functionName.Substring(dot + 1);
        }

        protected override HookType GetHookType()
        {
            return HookType.GetClassObject;
        }
    }

    //public class HandleQTObject : EventHandler
    //{
    //    protected override void ProcessResult(CallContext ctx)
    //    {
    //    }

    //    protected override void ProcessParams(CallContext ctx)
    //    {
    //    }

    //    protected override string GetDisplayName(CallContext ctx, string functionName, string displayName)
    //    {
    //        if (functionName.Contains("??0QObject@@QAE@PAV0@@Z"))
    //            return "Default QObject constructor";
    //        if (functionName.Contains("??0QObject@@IAE@AAVQObjectPrivate@@PAV0@@Z"))
    //            return "Initialized QObject constructor";
    //        if (functionName.Contains("??0QTableWidget@@QAE@PAVQWidget@@@Z"))
    //            return "Default QTableWidget constructor";
    //        if (functionName.Contains("??0QTableView@@QAE@PAVQWidget@@@Z"))
    //            return "Default QTableView constructor";
    //        if (functionName.Contains("??0QWidget@@QAE@PAV0@V?$QFlags@W4WindowType@Qt@@@@@Z"))
    //            return "Initialized QWidget constructor";
    //        if (functionName.Contains("??0QWidget@@IAE@AAVQWidgetPrivate@@PAV0@V?$QFlags@W4WindowType@Qt@@@@@Z"))
    //            return "Initialized QWidget constructor (private)";
    //        if (functionName.Contains("?setModel@QTableView@@UAEXPAVQAbstractItemModel@@@Z"))
    //            return "QTableView::setModel";
    //        return "Unknown QT Object function";
    //    }

    //    protected override HookType GetHookType()
    //    {
    //        return HookType.QTObject;
    //    }
    //}

    public class HandleThreadDetach : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.RetValue = 0;
            ctx.Ce.Success = true;
            ctx.Ce.Result = "SUCCESS";
        }

        protected override void ProcessParams(CallContext ctx)
        {
            ctx.Ce.Params = new[]
                                {
                                    new Param("ThreadId", ctx.Ce.Tid.ToString(CultureInfo.InvariantCulture))
                                };
            ctx.Ce.ParamMainIndex = 0;
        }

        public override CallEvent ProcessEvent(CallContext ctx)
        {
            var pid = ctx.E.GetUInt();
            var tid = ctx.E.GetUInt();
            var timestamp = ctx.E.GetDouble();
            var relativeTimestamp = ctx.E.GetDouble();
            var receivedFunctionName = ctx.E.GetString();
            string functionName = receivedFunctionName;
            string displayName = receivedFunctionName;
            const uint eventkind = 0;

            const bool before = (eventkind & 1) == 1;
            const bool virtualized = (eventkind & 2) == 2;

            ctx.Ce = new CallEvent(GetHookType(), 0, pid, tid)
            {
                Time = 0,
                GenerationTime = relativeTimestamp,
                TimeStamp = timestamp,
                Win32Function = functionName,
                Function = displayName,
                Before = before,
                ProcessPath = string.Empty,
                Virtualized = virtualized,
                OnlyBefore = true,
                ChainDepth = 0,
            };

            ProcessParams(ctx);
            ProcessResult(ctx);

            return ctx.Ce;
        }

        protected override HookType GetHookType()
        {
            return HookType.ThreadDetach;
        }
    }
}
