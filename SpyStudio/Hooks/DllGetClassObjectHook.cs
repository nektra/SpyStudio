using Nektra.Deviare2;
using SpyStudio.COM.Controls;
using SpyStudio.Main;
using SpyStudio.Tools;
using SpyStudio.COM;

namespace SpyStudio.Hooks
{
    public class DllGetClassObjectHook : DeviareHook
    {
        //private static XmlDocument _doc = new XmlDocument();
        private readonly string _modPath;

        public DllGetClassObjectHook(NktSpyMgr spyMgr, string modName, string modPath, NktExportedFunction function)
            : base(spyMgr)
        {
            OnlyAfter = false;
            OnlyBefore = false;
            ParamsBefore = false;
            ReturnValue = ReturnValueType.HRESULT;
            DisplayName = char.ToUpper(modName[0]) + modName.Substring(1).ToLower() + ".DllGetClassObject";
            CoCreate = true;
            FunctionObject = function;
            _modPath = modPath;
        }
        public override bool ProcessEvent(bool before, NktHookCallInfo callInfo, CallEvent callEvent, NktProcess proc)
        {
            var ret = ProcessFunction(before, callInfo, callEvent, proc);
            //var ret = base.ProcessEvent(before, callInfo, callEvent, proc);

            callEvent.Type = HookType.GetClassObject;
            NktParam p = callInfo.Params().GetAt(0);
            if (p.IsNullPointer == false)
            {
                p = p.Evaluate();
                string clsid = DeviareTools.ClsIdStruct2String(p);
                callEvent.CreateEventParams(clsid);
                if (string.IsNullOrEmpty(callEvent.GetServer()) && string.IsNullOrEmpty(callEvent.GetDescription()))
                {
                    ComServerInfoMgr.GetInstance().SetComServer(clsid, _modPath);
                }
                // force this module as server
                callEvent.SetServer(_modPath);
            }
            else
            {
                ret = false;
            }

            return ret;
        }
    }
}
