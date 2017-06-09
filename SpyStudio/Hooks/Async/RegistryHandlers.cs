using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using System.Globalization;
using SpyStudio.COM;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Hooks.Async
{
    public static class TextReplacement
    {
        public static string RenameKey(string key)
        {
            Debug.Assert(key.Length < 2 || key[0] == '\\' && key[1] != key[0]);
            while (key.Length >= 2 && key.Substring(0, 2) == "\\\\")
                key = key.Substring(1);
            var found = false;
            key = RegistryTools.ReplaceKnownKeys(key, ref found);
            if (!found)
                Debug.WriteLine("Cannot find root Key: " + key);
            key = RegistryTools.FixBackSlashesIn(key);
            return key;
        }
    }

    public abstract class HandleRegistry : EventHandler
    {
        private void Rename(CallContext ctx, int index)
        {
            ctx.Ce.Params[index].Value = TextReplacement.RenameKey(ctx.Ce.Params[index].Value);
        }
        protected void ProcessInfoClass(CallContext ctx, int n)
        {
            var e = ctx.Ce;
            var keyInfoClass = ctx.E.GetUInt();
            switch (keyInfoClass)
            {
                case NativeApiTools.KeyBasicInformation:
                    e.CreateParams(n + 1);
                    SetParam(ctx, n + 0, "Name");
                    //Rename(ctx, n + 0);
                    break;
                case NativeApiTools.KeyNodeInformation:
                    e.CreateParams(n + 2);
                    SetParam(ctx, n + 0, "ClassName");
                    SetParam(ctx, n + 1, "Name");
                    //Rename(ctx, n + 1);
                    break;
                case NativeApiTools.KeyFullInformation:
                    e.CreateParams(n + 3);
                    SetParam(ctx, n + 0, "ClassName");
                    SetParam(ctx, n + 1, "SubKeys");
                    SetParam(ctx, n + 2, "Values");
                    break;
                case NativeApiTools.KeyNameInformation:
                    e.CreateParams(n + 1);
                    SetParam(ctx, n + 0, "Name");
                    break;
                case NativeApiTools.KeyCachedInformation:
                    e.CreateParams(n + 3);
                    SetParam(ctx, n + 0, "SubKeys");
                    SetParam(ctx, n + 1, "Values");
                    SetParam(ctx, n + 2, "NameLength");
                    break;
                case NativeApiTools.KeyFlagsInformation:
                    e.CreateParams(n + 0);
                    break;
                case NativeApiTools.KeyVirtualizationInformation:
                    e.CreateParams(n + 5);
                    SetParam(ctx, n + 0, "VirtualizationCandidate");
                    SetParam(ctx, n + 1, "VirtualizationEnabled");
                    SetParam(ctx, n + 2, "VirtualTarget");
                    SetParam(ctx, n + 3, "VirtualStore");
                    SetParam(ctx, n + 4, "VirtualSource");
                    break;
                case Tools.NativeApiTools.KeyHandleTagsInformation:
                    e.CreateParams(n + 0);
                    break;
            }
        }
        protected void ProcessValueInfoValues(out string data, out RegistryValueKind type, CallContext ctx)
        {
            if(ctx.E.IsNull())
            {
                data = null;
                ctx.E.GetString();
            }
            else
            {
                data = ctx.E.GetString();
            }
            type = (RegistryValueKind) ctx.E.GetInt();
        }

        protected void ProcessValueInfoData(out string data, out RegistryValueKind type, out string name, CallContext ctx)
        {
            ProcessValueInfoValues(out data, out type, ctx);
            name = ctx.E.GetString();
        }
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.Result = Declarations.NtStatusToString(ctx.Ce.RetValue = ctx.E.GetULong());
            ctx.Ce.Success = ctx.Ce.RetValue == 0;
        }
    }

    public class HandleOpenKey : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            string truePath = ctx.E.GetString();
            string paramPath = ctx.E.GetString();
            ctx.Ce.CreateParams((truePath.Length != 0 ? 1 : 0) + (paramPath.Length != 0 ? 1 : 0));
            int index = 0;
            if (paramPath.Length != 0)
            {
                ctx.Ce.Params[index].Name = "Key";
                ctx.Ce.Params[index].Value = TextReplacement.RenameKey(paramPath);
                index++;
            }
            if (truePath.Length != 0)
            {
                ctx.Ce.Params[index].Name = "ReturnedKey";
                ctx.Ce.Params[index].Value = TextReplacement.RenameKey(truePath);
                index++;
            }
            ctx.Ce.ParamMainIndex = 0;
        }
        protected override HookType GetHookType()
        {
            return HookType.RegOpenKey;
        }
    }

    public class HandleCreateKey : HandleOpenKey
    {
        protected override HookType GetHookType()
        {
            return HookType.RegCreateKey;
        }
    }

    public class HandleQueryKey : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var e = ctx.Ce;
            var keyName = TextReplacement.RenameKey(ctx.E.GetString());
            ProcessInfoClass(ctx, 1);
            e.Params[0].Name = "Path";
            e.Params[0].Value = keyName;
        }
        protected override HookType GetHookType()
        {
            return HookType.RegQueryKey;
        }
    }

    public class HandleQueryValue : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var ce = ctx.Ce;
            var e = ctx.E;
            ce.CreateParams(4);
            string path = TextReplacement.RenameKey(e.GetString());
            string name = e.GetString();
            string data;
            RegistryValueKind dataType;
            string dataName;

            ProcessValueInfoData(out data, out dataType, out dataName, ctx);
            RegQueryValueEvent.CreatePath(ce, path, name, data, dataType);
            RegQueryValueEvent.SetDataComplete(ce, true);
            ComServerInfoMgr.GetInstance().AddComServerInfo(ce);
        }
        protected override HookType GetHookType()
        {
            return HookType.RegQueryValue;
        }
    }

    public class HandleQueryMultipleValues : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var ce = ctx.Ce;
            var e = ctx.E;
            string keyName = TextReplacement.RenameKey(e.GetString());
            int count = e.GetInt();
            ce.CreateParams(count + 1);
            ce.Params[0].Name = "Path";
            ce.Params[0].Value = keyName;
            RegQueryValueEvent.SetDataAvailable(ce, true);
            for (int i = 1; i <= count; i++)
            {
                SetParam(ctx, i, "Value" + i);
                SetParam(ctx, i, "Type" + i);
                SetParam(ctx, i, "Data" + i);
            }
        }
        protected override HookType GetHookType()
        {
            return HookType.RegQueryMultipleValues;
        }
    }

    public class HandleSetValue : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var ce = ctx.Ce;
            var e = ctx.E;

            string keyName = TextReplacement.RenameKey(e.GetString());
            string valueName = e.GetString();
            string valueData;
            RegistryValueKind type;
            ProcessValueInfoValues(out valueData, out type, ctx);

            RegQueryValueEvent.CreatePath(ce, keyName, valueName, valueData, type);

            RegQueryValueEvent.SetDataComplete(ce, true);
            RegQueryValueEvent.SetWrite(ce, true);
        }
        protected override HookType GetHookType()
        {
            return HookType.RegSetValue;
        }
    }

    public class HandleDeleteValue : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var ce = ctx.Ce;
            var e = ctx.E;

            string keyName = TextReplacement.RenameKey(e.GetString());
            string valueName = e.GetString();

            RegQueryValueEvent.CreatePath(ce, keyName, valueName, null, RegistryValueKind.Unknown);
        }
        protected override HookType GetHookType()
        {
            return HookType.RegDeleteValue;
        }
    }

    public class HandleDeleteKey : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            ctx.Ce.CreateParams(1);
            ctx.Ce.Params[0].Name = "Path";
            ctx.Ce.Params[0].Value = TextReplacement.RenameKey(ctx.E.GetString());
        }
        protected override HookType GetHookType()
        {
            return HookType.RegDeleteKey;
        }
        protected override bool BeforeHasMoreInfo()
        {
            return true;
        }
    }

    public class HandleEnumerateValueKey : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var ce = ctx.Ce;
            var e = ctx.E;
            string keyName = TextReplacement.RenameKey(e.GetString());
            string index = e.GetString();
            //Discard keyValueInfoClass
            e.GetString();
            ce.CreateParams(5);
            string dataName;
            string data;
            RegistryValueKind dataType;
            ProcessValueInfoData(out data, out dataType, out dataName, ctx);
            RegQueryValueEvent.CreatePath(false, ce, keyName, dataName, data, dataType);
            ce.Params[4].Name = "Index";
            ce.Params[4].Value = index;
            ComServerInfoMgr.GetInstance().AddComServerInfo(ce);
        }
        protected override HookType GetHookType()
        {
            return HookType.RegEnumerateValueKey;
        }
    }

    public class HandleEnumerateKey : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var ce = ctx.Ce;
            var e = ctx.E;
            string keyName = TextReplacement.RenameKey(e.GetString());
            string index = e.GetString();

            bool zeroIsSet = false;
            if (ce.Before || ce.RetValue != 0)
            {
                ce.CreateParams(2);
                e.Strings.Clear();
            }
            else
            {
                ProcessInfoClass(ctx, 2);
                foreach (var p in ce.Params)
                {
                    if (p.Name != "Name")
                        continue;
                    ce.Params[0].Value = keyName + "\\" + p.Value;
                    zeroIsSet = true;
                    break;
                }
            }
            if (!zeroIsSet)
                ce.Params[0].Value = keyName;
            ce.Params[0].Name = "Path";
            ce.Params[1].Value = index;
            ce.Params[1].Name = "Index";
        }
        protected override HookType GetHookType()
        {
            return HookType.RegEnumerateKey;
        }
    }

    public class HandleRenameKey : HandleRegistry
    {
        protected override void ProcessParams(CallContext ctx)
        {
            CallEvent e = ctx.Ce;
            e.CreateParams(2);
            e.Params[0].Value = ctx.E.GetString();
            e.Params[0].Name = "Path";
            e.Params[1].Value = ctx.E.GetString();
            e.Params[1].Name = "NewName";
        }
        protected override HookType GetHookType()
        {
            return HookType.RegRenameKey;
        }
    }
}
