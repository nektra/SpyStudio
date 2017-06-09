using System.Collections.Generic;
using SpyStudio.COM.Controls;
using SpyStudio.Tools;

namespace SpyStudio.COM
{
    public static class ComCallEventExtensions
    {
        public static void CreateEventParams(this CallEvent aCallEvent, string aClsid)
        {
            aCallEvent.CreateParams(3);
            aCallEvent.Params[0].Name = "Clsid";
            aCallEvent.Params[0].Value = aClsid;

            string detail, detail2;
            GetRegistryInfo(aClsid, out detail, out detail2, aCallEvent);

            aCallEvent.Params[1].Name = "Description";
            aCallEvent.Params[1].Value = detail2;

            aCallEvent.Params[2].Name = "Server";
            aCallEvent.Params[2].Value = detail;

            aCallEvent.Result = Declarations.HresultErrorToString(aCallEvent.RetValue);
            aCallEvent.Success = (aCallEvent.RetValue < 0x80000000);
        }

        public static string GetClsid(this CallEvent aCallEvent)
        {
            return aCallEvent.Params.Length > 0 ? aCallEvent.Params[0].Value : string.Empty;
        }

        public static string GetServer(this CallEvent aCallEvent)
        {
            return aCallEvent.Params.Length > 2 ? aCallEvent.Params[2].Value : string.Empty;
        }

        public static void SetServer(this CallEvent aCallEvent, string server)
        {
            aCallEvent.Params[2].Value = server;
        }

        public static string GetDescription(this CallEvent aCallEvent)
        {
            return aCallEvent.Params.Length > 2 ? aCallEvent.Params[1].Value : string.Empty;
        }

        public static void SetDescription(CallEvent aCallEvent, string desc)
        {
            aCallEvent.Params[1].Value = desc;
        }

        private static void GetRegistryInfo(string iid, out string serverPath, out string description, CallEvent callEvent)
        {
            ComServerInfoMgr.GetInstance().GetRegistryInfo(iid, out serverPath, out description, callEvent);
        }

        private static bool GetRegistryInfoInWow64(string iid, out string serverPath, out string description)
        {
            return ComServerInfoMgr.GetInstance().GetRegistryInfoInWow64(iid, out serverPath, out description);
        }

        private static void GetRegistryInfoDefault(string iid, out string serverPath, out string description)
        {
            ComServerInfoMgr.GetInstance().GetRegistryInfoDefault(iid, out serverPath, out description);
        }
    }
}
