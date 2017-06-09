using System;
using System.Collections.Generic;
using SpyStudio.COM.Controls;
using SpyStudio.Tools;

namespace SpyStudio.COM
{
    class ComServerInfoMgr
    {
        readonly Dictionary<string, ClsidRegistryInfo> _clsidRegistryInformation = new Dictionary<string, ClsidRegistryInfo>();
        private static ComServerInfoMgr _instance;

        public static ComServerInfoMgr GetInstance()
        {
            return _instance ?? (_instance = new ComServerInfoMgr());
        }

        static public string GetIID(string keyPath)
        {
            var eindex = keyPath.LastIndexOf('}');
            if (eindex != -1)
            {
                var sindex = keyPath.LastIndexOf('{');
                if (sindex != -1 && (eindex - sindex) <= 37)
                {
                    return keyPath.Substring(sindex, eindex - sindex + 1);
                }
            }
            return string.Empty;
        }
        public void AddComServerInfo(CallEvent aCallEvent)
        {
            if (aCallEvent.Success)
            {
                // verify that the value name is empty 
                if (string.IsNullOrEmpty(RegQueryValueEvent.GetName(aCallEvent)))
                {
                    var data = RegQueryValueEvent.GetData(aCallEvent);
                    if (!string.IsNullOrEmpty(data))
                    {
                        var lowerParentKey = RegQueryValueEvent.GetParentKey(aCallEvent).ToLower();
                        // the path contains CLSID at any place
                        if (lowerParentKey.IndexOf("clsid", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (lowerParentKey.EndsWith("inprocserver32", StringComparison.InvariantCultureIgnoreCase)
                                || lowerParentKey.EndsWith("localserver32", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var iid = GetIID(lowerParentKey);
                                if (string.IsNullOrEmpty(iid))
                                    SetComServer(iid, RegQueryValueEvent.GetData(aCallEvent));
                            }
                            // the path contains CLSID at any place
                            else if (lowerParentKey.IndexOf("clsid", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var eindex = lowerParentKey.LastIndexOf('}');
                                if (eindex != -1)
                                {
                                    var sindex = lowerParentKey.LastIndexOf('{');
                                    if (sindex != -1 && (eindex - sindex) <= 37)
                                    {
                                        var iid = lowerParentKey.Substring(sindex, eindex - sindex + 1);
                                        SetComDescription(iid, RegQueryValueEvent.GetData(aCallEvent));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void SetComDescription(string iid, string description)
        {
            lock (_clsidRegistryInformation)
            {
                ClsidRegistryInfo retInfo;
                var iidUpr = iid.ToUpper();
                if (!_clsidRegistryInformation.TryGetValue(iidUpr, out retInfo))
                {
                    _clsidRegistryInformation[iidUpr] = new ClsidRegistryInfo(description ?? string.Empty, "");
                }
                else
                {
                    _clsidRegistryInformation[iidUpr].Description = description;
                }
            }
        }
        public void SetComServer(string iid, string serverPath)
        {
            lock (_clsidRegistryInformation)
            {
                ClsidRegistryInfo retInfo;
                var iidUpr = iid.ToUpper();
                if (!_clsidRegistryInformation.TryGetValue(iidUpr, out retInfo))
                {
                    _clsidRegistryInformation[iidUpr] = new ClsidRegistryInfo("", serverPath ?? string.Empty);
                }
                else
                {
                    _clsidRegistryInformation[iidUpr].ServerPath = serverPath;
                }
            }

        }
        public void GetRegistryInfo(string iid, out string serverPath, out string description, CallEvent callEvent)
        {
            var iidUpr = iid.ToUpper();

            serverPath = "";
            description = "";

            lock (_clsidRegistryInformation)
            {
                ClsidRegistryInfo retInfo;
                if (_clsidRegistryInformation.TryGetValue(iidUpr, out retInfo))
                {
                    serverPath = retInfo.ServerPath;
                    description = retInfo.Description;
                    return;
                }

                if (PlatformTools.IsPlatform64Bits())
                {
                    if (!PlatformTools.Is64Bits((int)callEvent.Pid))
                    {
                        if (!GetRegistryInfoInWow64(iid, out serverPath, out description))
                            GetRegistryInfoDefault(iid, out serverPath, out description);
                    }
                }
                else
                {
                    GetRegistryInfoDefault(iid, out serverPath, out description);
                }
            }

            _clsidRegistryInformation[iid] = new ClsidRegistryInfo(description, serverPath);
        }

        public bool GetRegistryInfoInWow64(string iid, out string serverPath, out string description)
        {
            var keyPath = "Wow6432Node\\CLSID\\" + iid;

            serverPath = description = null;

            var key = Microsoft.Win32.Registry.ClassesRoot;
            var subKey = key.OpenSubKey(keyPath);
            if (subKey != null)
            {
                description = subKey.GetValue("") as string;
                var inprocServer32Key = subKey.OpenSubKey("InprocServer32");
                if (inprocServer32Key != null)
                {
                    serverPath = inprocServer32Key.GetValue("") as string;
                    inprocServer32Key.Close();
                }
                else
                {
                    inprocServer32Key = subKey.OpenSubKey("LocalServer32");
                    if (inprocServer32Key != null)
                    {
                        serverPath = inprocServer32Key.GetValue("") as string;
                        inprocServer32Key.Close();
                    }
                }

                subKey.Close();
            }
            key.Close();

            if (description == null)
                description = "";
            if (serverPath == null)
                serverPath = "";

            return !string.IsNullOrEmpty(description) || !string.IsNullOrEmpty(serverPath);
        }

        public void GetRegistryInfoDefault(string iid, out string serverPath, out string description)
        {
            var keyPath = "CLSID\\" + iid;

            description = string.Empty;
            serverPath = string.Empty;

            var key = Microsoft.Win32.Registry.ClassesRoot;
            var subKey = key.OpenSubKey(keyPath);
            if (subKey != null)
            {
                description = subKey.GetValue("") as string;
                var inprocServer32Key = subKey.OpenSubKey("InprocServer32");
                if (inprocServer32Key != null)
                {
                    serverPath = inprocServer32Key.GetValue("") as string;
                    inprocServer32Key.Close();
                }
                else
                {
                    inprocServer32Key = subKey.OpenSubKey("LocalServer32");
                    if (inprocServer32Key != null)
                    {
                        serverPath = inprocServer32Key.GetValue("") as string;
                        inprocServer32Key.Close();
                    }
                }

                subKey.Close();
            }
            key.Close();
            if (description == null)
                description = string.Empty;
            if (serverPath == null)
                serverPath = string.Empty;
        }
        public void Clear()
        {
            lock (_clsidRegistryInformation)
            {
                _clsidRegistryInformation.Clear();
            }
        }
    }
}
