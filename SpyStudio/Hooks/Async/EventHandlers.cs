using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nektra.Deviare2;
using SpyStudio.Database;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Hooks.Async
{
    public class CallContext
    {
        public readonly AsyncStream.Event E;
        public CallEvent Ce;
        public readonly AsyncHookMgr Hm;
        public readonly NktProcess Proc;
        public IntPtr HookId;
        public bool IgnoreParamCount;
        public Dictionary<uint, CallEventPair> CookieToCepDict;
        public CallContext(AsyncStream.Event e, AsyncHookMgr hm, NktProcess proc)
        {
            E = e;
            Ce = null;
            Hm = hm;
            Proc = proc;
            IgnoreParamCount = false;
        }
    };

    public abstract class EventHandler
    {
        private static void ProcessStack(CallContext ctx)
        {
            ctx.Ce.CallStackStrings = ctx.E.GetStrings(5*ctx.E.GetInt());
        }

        protected void ProcessCallerModule(CallContext ctx)
        {
            ctx.Ce.CallModule = ctx.E.GetString();
        }

        protected void ProcessStackString(CallContext ctx)
        {
            ctx.Ce.StackTraceString = ctx.E.GetString();
        }

        protected virtual void ProcessResult(CallContext ctx)
        {
            ctx.Ce.Result = ctx.E.GetString();
        }

        protected abstract void ProcessParams(CallContext ctx);

        protected virtual HookType GetHookType()
        {
            return HookType.Custom;
        }

        protected virtual void ProcessOther(CallContext ctx) {}

        protected virtual string GetDisplayName(CallContext ctx, string functionName, string displayName)
        {
            return displayName;
        }

        public virtual CallEvent ProcessEvent(CallContext ctx)
        {
            var pid = ctx.E.GetUInt();
            var path = ctx.E.GetString();
            var tid = ctx.E.GetUInt();
            var cookie = ctx.E.GetUInt();
            var chainDepth = ctx.E.GetUInt();
            var timestamp = ctx.E.GetDouble();
            var relativeTimestamp = ctx.E.GetDouble();
            var elapsedTime = ctx.E.GetDouble();
            var receivedFunctionName = ctx.E.GetString();
            string functionName;
            string displayName;
            HookType type;
            int tag;
            int flags;

            ctx.Hm.HookMgr.GetHookProperties(ctx.HookId, out type, out tag, out functionName, out displayName, out flags, ctx.E.IsSimulated);
            displayName = GetDisplayName(ctx, functionName, displayName);
            uint eventkind = ctx.E.GetUInt();
            var before = (eventkind & 1) == 1;
            var virtualized = (eventkind & 2) == 2;
            ctx.Ce = new CallEvent(GetHookType(), cookie, pid, tid, displayName)
                         {
                             Time = elapsedTime,
                             GenerationTime = relativeTimestamp,
                             TimeStamp = timestamp,
                             Before = before,
                             Win32Function = receivedFunctionName,
                             Virtualized = virtualized,
                             Type = type,
                             OnlyBefore = (flags & (int) eNktHookFlags.flgOnlyPreCall) != 0,
                             ChainDepth = chainDepth,
                             ProcessName = FileSystemTools.GetFileName(path),
                         };

            IdentifierProcessing(ctx);

            if (ctx.Ce == null)
            {
                Debug.Assert(false);
                return null;
            }
            ProcessOther(ctx);

#if DEBUG
            if (!ctx.IgnoreParamCount)
            {
                var count = ctx.E.Strings.Count;
                if (count > 0)
                {
                    Debug.WriteLine(count);
                    Debug.WriteLine(functionName);
                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
            }
#endif

            UnifyCallEvents(ctx);

            return ctx.Ce;
        }

        protected void IdentifierProcessing(CallContext ctx)
        {
            string identifier = ctx.E.Strings.Count > 0 ? ctx.E.GetString() : null;
            if (identifier == "stack")
            {
                ProcessStack(ctx);
                identifier = ctx.E.Strings.Count > 0 ? ctx.E.GetString() : null;
            }
            else
                ctx.Ce.CallStack = new List<DeviareTools.DeviareStackFrame>();

            if (identifier == "module")
            {
                ProcessCallerModule(ctx);
                identifier = ctx.E.Strings.Count > 0 ? ctx.E.GetString() : null;
            }
            else
                ctx.Ce.CallModule = string.Empty;

            if (identifier == "stackstring")
            {
                ProcessStackString(ctx);
                identifier = ctx.E.Strings.Count > 0 ? ctx.E.GetString() : null;
            }

            if (identifier == "result")
            {
                ProcessResult(ctx);
                identifier = ctx.E.Strings.Count > 0 ? ctx.E.GetString() : null;
            }
            if (identifier == "params")
                ProcessParams(ctx);
        }

        [Conditional("Debug")]
        private void CheckStack(CallEvent e)
        {
            if ((e.CallStack == null || e.CallStack.Count == 0) && Debugger.IsAttached)
                Debugger.Break();
        }

        protected virtual bool BeforeHasMoreInfo()
        {
            return false;
        }

        protected void UnifyCallEvents(CallContext ctx)
        {
            uint cookie = ctx.Ce.Cookie;
            CallEventPair pair;
            if (!ctx.CookieToCepDict.ContainsKey(cookie))
                pair = ctx.CookieToCepDict[cookie] = new CallEventPair();
            else
                pair = ctx.CookieToCepDict[cookie];

            {
                CallEventPair.SimplifiedEvent se;
                if (ctx.Ce.Before)
                    se = pair.Before = new CallEventPair.SimplifiedEvent();
                else
                    se = pair.After = new CallEventPair.SimplifiedEvent();

                se.Result = ctx.Ce.Result;
                se.Module = ctx.Ce.CallModule;
                se.Stack = ctx.Ce.CallStackStrings;
                se.StackTraceString = ctx.Ce.StackTraceString;
                se.Params = ctx.Ce.Params;
                se.ParamMainIndex = ctx.Ce.ParamMainIndex;
#if DEBUG
                se.Buffer = ctx.E.BufferCopy;
#endif
            }

            if (string.IsNullOrEmpty(ctx.Ce.CallModule) && !ctx.Ce.Before && pair.Before != null && pair.Before.Module != null)
                ctx.Ce.CallModule = pair.Before.Module;

            if (!ctx.Ce.Before && pair.Before != null)
            {
                Debug.Assert(pair.After != null);
                if ((ctx.Ce.Params == null && pair.Before.Params != null) || BeforeHasMoreInfo())
                {
                    ctx.Ce.Params = pair.Before.Params;
                    ctx.Ce.ParamMainIndex = pair.Before.ParamMainIndex;
                }
                if(!string.IsNullOrEmpty(pair.Before.StackTraceString))
                {
                    ctx.Ce.StackTraceString = pair.Before.StackTraceString;
                }
            }
            if (!ctx.Ce.Before || ctx.Ce.OnlyBefore)
                ctx.CookieToCepDict.Remove(cookie);
        }

        protected void SetParam(CallContext ctx, int i, string name)
        {
            var p = ctx.Ce.Params[i];
            p.Name = name;
            p.Value = ctx.E.GetString();
        }
    }
}
