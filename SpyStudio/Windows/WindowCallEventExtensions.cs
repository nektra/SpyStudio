using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Windows
{
    public static class WindowCallEventExtensions
    {
        public static string GetWindowClassName(this CallEvent aCallEvent)
        {
            return aCallEvent.Params[0].Value;
        }

        public static string GetWindowName(this CallEvent aCallEvent)
        {
            return aCallEvent.ParamCount > 1 ? aCallEvent.Params[1].Value : string.Empty;
        }

        public static string GetUniqueID(this CallEvent aCallEvent)
        {
            return aCallEvent.GetWindowClassName() + " " + aCallEvent.GetWindowName() + " " + aCallEvent.Result;
        }
        public static string GetModuleHandleUsing(this CallEvent aCallEvent, ModulePath aModulePath)
        {
            var hModulePath = "";

            if (CreateWindowEvent.GetHModuleAddress(aCallEvent) != 0)
            {
                var hInst = CreateWindowEvent.GetHModuleAddress(aCallEvent);
                if (!aModulePath.TryGetPathByAddress(aCallEvent.Pid, hInst, out hModulePath))
                {
                    hModulePath = "0x" + hInst.ToString("X");
                }
                else
                {
                    hModulePath = ModulePath.ExtractModuleName(hModulePath);
                }
            }
            else if (!string.IsNullOrEmpty(CreateWindowEvent.GetHModuleString(aCallEvent)))
                hModulePath = CreateWindowEvent.GetHModuleString(aCallEvent);
            return hModulePath;
        }
    }
}
