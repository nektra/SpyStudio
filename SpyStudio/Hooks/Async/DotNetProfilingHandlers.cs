using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class HandleDotNetProfiling : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.Result = ctx.E.GetString();
            ctx.Ce.RetValue = ctx.E.GetULong();
            ctx.Ce.Success = ctx.E.GetInt() != 0;
        }

        protected override void ProcessParams(CallContext ctx)
        {
            var parameters = new List<Param>();
            while (ctx.E.Strings.Count >= 2)
            {
                var name = ctx.E.GetString();
                var value = ctx.E.GetString();
                parameters.Add(new Param(name, value));
            }
            ctx.Ce.Params = parameters.ToArray();
            ctx.Ce.ParamMainIndex = 0;
        }

        public override CallEvent ProcessEvent(CallContext ctx)
        {
            var functionId = ctx.E.GetInt();
            var pid = ctx.E.GetUInt();
            var tid = ctx.E.GetUInt();
            var cookie = ctx.E.GetULong();
            var timestamp = ctx.E.GetDouble();
            var relativeTimestamp = ctx.E.GetDouble();
            var elapsedTime = ctx.E.GetDouble();
            var receivedFunctionName = ctx.E.GetString();
            var functionName = receivedFunctionName;
            var displayName = receivedFunctionName;
            uint eventkind = ctx.E.GetUInt();

            bool before = (eventkind & 1) == 1;
            bool virtualized = (eventkind & 2) == 2;
            bool eventHasNoDuration = (eventkind & 4) == 4;
            before = before && !eventHasNoDuration;

            //TODO: Remove this later.
            //before = false;
            //eventHasNoDuration = true;

            ctx.Ce = new CallEvent(GetHookType(), 0, pid, tid)
            {
                Time = elapsedTime,
                Cookie = (uint)cookie,
                GenerationTime = relativeTimestamp,
                TimeStamp = timestamp,
                Win32Function = functionName,
                Function = displayName,
                Before = before,
                ProcessPath = string.Empty,
                Virtualized = virtualized,
                OnlyBefore = false,
                ChainDepth = 0,
            };

            IdentifierProcessing(ctx);
            UnifyCallEvents(ctx);

            if (eventHasNoDuration)
            {
                ctx.Ce.Result = "SUCCESS";
                ctx.Ce.RetValue = 0;
                ctx.Ce.Success = true;
            }

            return ctx.Ce;
        }

        protected override HookType GetHookType()
        {
            return HookType.ThreadDetach;
        }
    }
}
