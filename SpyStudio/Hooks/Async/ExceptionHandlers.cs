using System;
using System.Collections.Generic;
using System.Globalization;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class HandleException : EventHandler
    {
        protected override void ProcessParams(CallContext ctx)
        {
            if (!ctx.Ce.Before)
                return;
            var codeString = ctx.E.GetString();
            var flags = ctx.E.GetString();
            var faultingAddress = ctx.E.GetString();
            ctx.Ce.CreateParams(3);
            ctx.Ce.Params[0].Name = "Code";
            ctx.Ce.Params[0].Value = codeString;
            ctx.Ce.Params[1].Name = "Flags";
            ctx.Ce.Params[1].Value = flags;
            ctx.Ce.Params[2].Name = "Address";
            ctx.Ce.Params[2].Value = faultingAddress;
        }
        protected override void ProcessOther(CallContext ctx)
        {
            if (ctx.Ce.Before)
                ctx.Ce.OnlyBefore = true;
            ctx.Ce.Critical = true;
            ctx.Ce.ParamMainIndex = 0;
        }
    }

    public class HandleRaiseException : HandleException
    {
        protected override HookType GetHookType()
        {
            return HookType.RaiseException;
        }
    }

    public class HandleRaiseHardError : EventHandler
    {
        private static Dictionary<uint, string[]> _map;
        public HandleRaiseHardError()
        {
            if(_map == null)
            {
                _map = new Dictionary<uint, string[]>();
                _map[0xC0000138] = new[] { "Ordinal", "Module" };
                _map[0xC0000139] = new[] { "EntryPoint", "Module" };
                _map[0xC000007B] =
                _map[0xC0000218] = new[] { "Path" };
                _map[0x40000015] = new[] { "Message" };
            }
        }
        protected override void ProcessParams(CallContext ctx)
        {
            var codeString = ctx.E.GetString();
            var codeS = ctx.E.GetString();
            var code = Convert.ToUInt32(codeS, CultureInfo.InvariantCulture);
            var count = ctx.E.GetInt();
            ctx.Ce.CreateParams(1 + count);
            ctx.Ce.Params[0].Name = "Code";
            ctx.Ce.Params[0].Value = string.IsNullOrEmpty(codeString) ? codeS : codeString;
            for (int i = 1; i <= count; i++)
            {
                string[] names = null;
                if (_map.ContainsKey(code))
                    names = _map[code];
                string name = (names != null && names.Length > i ? names[i] : "P" + i);
                ctx.Ce.Params[i].Name = name;
                ctx.Ce.Params[i].Value = ctx.E.GetString();
            }
        }
        protected override HookType GetHookType()
        {
            return HookType.RaiseException;
        }
    }

    public class HandleUnhandledException : HandleException
    {
        protected override HookType GetHookType()
        {
            return HookType.RaiseException;
        }
    }
}
