using System;
using System.Collections.Generic;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public abstract class HandleWindowEvent : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.RetValue = ctx.E.GetULong();
            ctx.Ce.Success = ctx.Ce.RetValue != 0;
            ctx.Ce.Result = ctx.Ce.Success ? "SUCCESS" : "ERROR";
        }
        protected void ProcessWindowParams(CallContext ctx)
        {
            var wndName = ctx.E.GetString();
            var hModule = ctx.E.GetString();
            var className = ctx.E.GetString();
            var exStyle = ctx.E.GetUInt();
            var style = ctx.E.GetUInt();
            var parentClassName = ctx.E.GetString();
            CreateWindowEvent.CreateEventParams(ctx.Ce, className, wndName, hModule, exStyle, style, parentClassName);
        }
    }
    public class HandleCreateWindowEx : HandleWindowEvent
    {
        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
            {
                var className = ctx.E.GetString();
                CreateWindowEvent.CreateEventParams(ctx.Ce, className, "", 0);
            }
            else
                ProcessWindowParams(ctx);
        }
        protected override HookType GetHookType()
        {
            return HookType.CreateWindow;
        }
    }

    public class HandleCreateDialogIndirectParam : HandleWindowEvent
    {
        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
                return;
            ProcessWindowParams(ctx);
        }
        protected override HookType GetHookType()
        {
            return HookType.CreateDialog;
        }
    }

    public class HandleDialogBoxIndirectParamParam : HandleCreateDialogIndirectParam
    {
        protected override void ProcessResult(CallContext ctx)
        {
            long ret = ctx.E.GetLong();
            ctx.Ce.RetValue = (ulong)ret;
            ctx.Ce.Success = ret >= 0;
            ctx.Ce.Result = (ctx.Ce.RetValue >= 0 ? "SUCCESS" : "ERROR");
        }
    }
}
