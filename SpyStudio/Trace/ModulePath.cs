using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using SpyStudio.Tools;

namespace SpyStudio.Trace
{
    [Serializable]
    public class ModulePath
    {
        [Serializable]
        public class ModuleInfo
        {
            public ModuleInfo()
            {
                Path = "";
                Pid = 0;
                Address = 0;
                Size = 0;
                Company = "";
                Description = "";
                Certificate = null;
                Signed = false;
            }

            public ModuleInfo(string path, uint pid, UInt64 address, string company, string description,
                              X509Certificate certificate)
            {
                Path = path;
                Pid = pid;
                Address = address;
                Company = company;
                Description = description;
                Certificate = certificate;
                Signed = (certificate != null);
                Name = ExtractModuleName(path).ToLower();
            }

            public string Path { get; set; }

            public string Name { get; set; }

            public uint Pid { get; set; }

            public ulong Address { get; set; }

            public ulong Size { get; set; }

            public string Company { get; set; }

            public string Description { get; set; }

            public bool Signed { get; set; }

            public X509Certificate Certificate { get; private set; }
        }

        private readonly ReaderWriterLock _dataLock = new ReaderWriterLock();
        //readonly Dictionary<KeyValuePair<uint, UInt64>, ModuleInfo> _moduleAddress = new Dictionary<KeyValuePair<uint, UInt64>, ModuleInfo>();
        //readonly SortedDictionary<KeyValuePair<uint, UInt64>, ModuleInfo> _moduleAddress = new SortedDictionary<KeyValuePair<uint, UInt64>, ModuleInfo>();
        private readonly Dictionary<uint, Dictionary<string, ModuleInfo>> _modulesByPIDByPath =
            new Dictionary<uint, Dictionary<string, ModuleInfo>>();

        private readonly SortedDictionary<uint, SortedDictionary<UInt64, ModuleInfo>> _procModules =
            new SortedDictionary<uint, SortedDictionary<ulong, ModuleInfo>>();

        private Dictionary<uint, UInt64[]> _moduleCacheKeys;
        private Dictionary<uint, ModuleInfo[]> _moduleCacheValues;

        public ModuleInfo[] Modules
        {
            set
            {
                foreach (var modInfo in value)
                {
                    AddModule(modInfo);
                }
            }
        }

        public void AcquireReaderLock()
        {
            _dataLock.AcquireReaderLock(-1);
        }

        public void AcquireWriterLock()
        {
            _dataLock.AcquireWriterLock(-1);
        }

        public void ReleaseReaderLock()
        {
            _dataLock.ReleaseReaderLock();
        }

        public void ReleaseWriterLock()
        {
            _dataLock.ReleaseWriterLock();
        }

        public void Clear()
        {
            AcquireWriterLock();
            _procModules.Clear();
            _modulesByPIDByPath.Clear();

            _moduleCacheKeys = null;
            if (_moduleCacheValues != null)
            {
                _moduleCacheValues.Clear();
                _moduleCacheValues = null;
            }
            ReleaseWriterLock();
        }

        public SortedDictionary<uint, SortedDictionary<UInt64, ModuleInfo>> ModuleAddress
        {
            get { return _procModules; }
        }

        private void SetModuleAddress(uint pid, UInt64 address, ModuleInfo modInfo)
        {
            _procModules[pid][address] = modInfo;
        }

        private bool ContainsModule(uint pid, UInt64 address)
        {
            var ret = false;
            SortedDictionary<UInt64, ModuleInfo> modules;
            if (_procModules.TryGetValue(pid, out modules))
            {
                ret = modules.ContainsKey(address);
            }
            return ret;
        }

        public void AddUnknownModule(string module, uint pid, UInt64 address)
        {
            AcquireWriterLock();
            var modInfo = new ModuleInfo("", pid, address, "",
                                         "Address 0x" + address.ToString("X") + " in Process Id " + pid, null);
            //modulePath.Add(module, modInfo);
            SetModuleAddress(pid, address, modInfo);
            //_moduleAddress[new KeyValuePair<uint, UInt64>(pid, address)] = modInfo;
            ReleaseWriterLock();
        }

        public void AddModule(ModuleInfo modInfo)
        {
            AcquireWriterLock();
            //var modAddressKey = new KeyValuePair<uint, UInt64>(modInfo.Pid, modInfo.Address);
            if (!ContainsModule(modInfo.Pid, modInfo.Address))
            {
                //_moduleCacheKeys = null;
                SortedDictionary<UInt64, ModuleInfo> modDict;
                if (!_procModules.TryGetValue(modInfo.Pid, out modDict))
                {
                    modDict = _procModules[modInfo.Pid] = new SortedDictionary<ulong, ModuleInfo>();
                }
                modDict[modInfo.Address] = modInfo;
            }
            ReleaseWriterLock();
        }

        public void AddModuleByAddressAndPath(ModuleInfo modInfo)
        {
            AcquireWriterLock();
            //var modAddressKey = new KeyValuePair<uint, UInt64>(modInfo.Pid, modInfo.Address);
            if (!ContainsModule(modInfo.Pid, modInfo.Address))
            {
                //_moduleCacheKeys = null;
                SortedDictionary<UInt64, ModuleInfo> modDict;
                if (!_procModules.TryGetValue(modInfo.Pid, out modDict))
                {
                    modDict = _procModules[modInfo.Pid] = new SortedDictionary<ulong, ModuleInfo>();
                }
                modDict[modInfo.Address] = modInfo;
                //_moduleAddress[modAddressKey] = modInfo;
                if (!_modulesByPIDByPath.ContainsKey(modInfo.Pid))
                {
                    _modulesByPIDByPath[modInfo.Pid] = new Dictionary<string, ModuleInfo>();
                }
                _modulesByPIDByPath[modInfo.Pid][modInfo.Path.ToLower()] = modInfo;
            }
            ReleaseWriterLock();
        }

        public ModuleInfo AddModule( /*string module, */ string modPath, uint pid, UInt64 address)
        {
            AcquireWriterLock();
            //var modAddressKey = new KeyValuePair<uint, UInt64>(pid, address);

            ModuleInfo modInfo;
            //if(!_moduleAddress.ContainsKey(modAddressKey))
            if (!TryGetModuleByAddress(pid, address, out modInfo))
            {
                string company = "", description = "";
                //System.Security.Cryptography.X509Certificates.X509Certificate cert = null;

                //_moduleCacheKeys = null;

                if (modPath != "")
                {
                    if (modPath.StartsWith("\""))
                        modPath = modPath.Remove(0, 1);
                    if (modPath.EndsWith("\""))
                        modPath = modPath.Remove(modPath.Length - 1);

                    // get long name if short
                    //string oldPath = modPath;
                    /*
                    modPath = Path.GetFullPath(modPath);
                    try
                    {
                        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(modPath);
                        company = fileInfo.CompanyName;
                        description = fileInfo.FileDescription;
                    }
                    catch (System.Exception)
                    {
                        company = description = "";
                    }
                    */
                    company = description = "";
                    //try
                    //{
                    //    FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(modPath);
                    //    company = fileInfo.CompanyName;
                    //    description = fileInfo.FileDescription;
                    //try
                    //{
                    //    cert = X509Certificate.CreateFromSignedFile(modPath);
                    //    signed = true;
                    //}
                    //catch (System.Security.Cryptography.CryptographicException)
                    //{
                    //    cert = null;
                    //}
                    //catch (System.Exception)
                    //{
                    //    return;
                    //}
                    //}
                    //catch (System.Exception)
                    //{
                    //}
                }

                modInfo = new ModuleInfo(modPath, pid, address, company, description, null);
                //ModuleInfo modInfo = new ModuleInfo(modPath, address, company, description, cert);
                //modulePath[module] = modInfo;
                // if there is another overwrite it
                //_moduleAddress[modAddressKey] = modInfo;
                if (!_procModules.ContainsKey(pid))
                    _procModules[pid] = new SortedDictionary<ulong, ModuleInfo>();
                _procModules[pid][address] = modInfo;
            }
            ReleaseWriterLock();

            return modInfo;
        }

        private void CreateSearchCache()
        {
            if (_moduleCacheKeys == null)
            {
                //_moduleCacheKeys = new Dictionary<uint, UInt64>>();
                var procKeys = new Dictionary<uint, SortedList<UInt64, ModuleInfo>>();
                _moduleCacheKeys = new Dictionary<uint, UInt64[]>();
                _moduleCacheValues = new Dictionary<uint, ModuleInfo[]>();

                //foreach (var mod in _moduleAddress)
                //{
                //    SortedList<UInt64, ModuleInfo> dict;
                //    if(!procKeys.TryGetValue(mod.Value.Pid, out dict))
                //    {
                //        dict = procKeys[mod.Value.Pid] = new SortedList<UInt64, ModuleInfo>();
                //    }
                //    dict.Add(mod.Value.Address, mod.Value);
                //}
                foreach (var proc in _procModules)
                {
                    foreach (var mod in proc.Value)
                    {
                        SortedList<UInt64, ModuleInfo> dict;
                        if (!procKeys.TryGetValue(mod.Value.Pid, out dict))
                        {
                            dict = procKeys[mod.Value.Pid] = new SortedList<UInt64, ModuleInfo>();
                        }
                        dict.Add(mod.Value.Address, mod.Value);
                    }
                }

                foreach (var p in procKeys)
                {
                    _moduleCacheKeys[p.Key] = p.Value.Keys.ToArray();
                    _moduleCacheValues[p.Key] = p.Value.Values.ToArray();
                }
            }
        }

        /// <summary>
        /// Get the module that contains this address
        /// </summary>
        /// <returns></returns>
        public ModuleInfo GetModuleByAddress(uint pid, UInt64 address)
        {
            AcquireWriterLock();
            CreateSearchCache();
            ReleaseWriterLock();

            AcquireReaderLock();
            // ModuleInfo ret = GetModuleInfo(pid, address);

            //if(_procModules.TryGetValue(pid, out modules))

            UInt64[] addresses;
            ModuleInfo ret = null;

            if (_moduleCacheKeys.TryGetValue(pid, out addresses) && addresses.Length > 0)
            {
                var index = Array.BinarySearch(addresses, address);
                if (index < 0)
                {
                    index = ~index;
                    // greater than last one
                    if (index >= addresses.Length)
                    {
                        index = addresses.Length - 1;
                    }
                    else if (index != 0)
                    {
                        index--;
                    }
                    else
                    {
                        index = -1;
                    }
                }
                if (index != -1)
                {
                    var mod = _moduleCacheValues[pid][index];
                    if (address >= mod.Address && address < mod.Address + mod.Size)
                    {
                        ret = mod;
                    }
                }
            }
            ReleaseReaderLock();

            return ret;
        }

        public static string ExtractModuleName(string modPath)
        {
            var dllName = FileSystemTools.GetFileName(modPath);

            // assume .dll if there is no extension
            var index = dllName.LastIndexOf(".", StringComparison.Ordinal);
            if (index == -1 && !string.IsNullOrEmpty(dllName))
                dllName += ".dll";
            return dllName.ToLower();
        }

        public bool TryGetModuleByAddress(uint pid, UInt64 address, out ModuleInfo modInfo)
        {
            modInfo = null;
            AcquireReaderLock();
            SortedDictionary<UInt64, ModuleInfo> modules;
            //ret = _moduleAddress.TryGetValue(new KeyValuePair<uint, UInt64>(pid, address), out modInfo);
            var ret = _procModules.TryGetValue(pid, out modules);
            if (ret)
            {
                ret = modules.TryGetValue(address, out modInfo);
            }
            ReleaseReaderLock();
            return ret;
        }

        //public bool ContainsPath(string module)
        //{
        //    return Contains(ExtractModuleName(module));
        //}
        public bool TryGetPathByAddress(uint pid, UInt64 address, out string modPath)
        {
            AcquireReaderLock();
            SortedDictionary<UInt64, ModuleInfo> modules;
            //ret = _moduleAddress.TryGetValue(new KeyValuePair<uint, UInt64>(pid, address), out modInfo);
            var ret = _procModules.TryGetValue(pid, out modules);
            modPath = "";
            if (ret)
            {
                ModuleInfo modInfo;
                ret = modules.TryGetValue(address, out modInfo);
                if (ret)
                    modPath = modInfo.Path;
            }
            ReleaseReaderLock();
            return ret;
        }

        //public bool Contains(string module)
        //{
        //    bool ret;
        //    lock (dataLock)
        //    {
        //        ret = modulePath.ContainsKey(module);
        //    }
        //    return ret;
        //}
        public bool Contains(uint pid, UInt64 address)
        {
            AcquireReaderLock();
            //_moduleAddress.ContainsKey(new KeyValuePair<uint, UInt64>(pid, address)));
            var ret = ContainsModule(pid, address);
            ReleaseReaderLock();
            return ret;
        }

        //public string GetPath(string module)
        //{
        //    string ret = "";
        //    lock (dataLock)
        //    {
        //        ModuleInfo info;
        //        if (modulePath.TryGetValue(module, out info))
        //            ret = info.Path;
        //        else
        //            ret = module;
        //    }
        //    return ret;
        //}
        private ModuleInfo GetModuleInfo(uint pid, UInt64 address)
        {
            ModuleInfo modInfo = null;
            SortedDictionary<UInt64, ModuleInfo> modules;
            if (_procModules.TryGetValue(pid, out modules))
            {
                modules.TryGetValue(address, out modInfo);
            }
            return modInfo;
        }

        public string GetPath(uint pid, UInt64 address)
        {
            AcquireReaderLock();
            var modInfo = GetModuleInfo(pid, address);

            //if (_moduleAddress.TryGetValue(new KeyValuePair<uint, UInt64>(pid, address), out modInfo))
            var ret = modInfo != null ? modInfo.Path : "";
            ReleaseReaderLock();
            return ret;
        }

        public string GetCompany(uint pid, UInt64 address)
        {
            AcquireReaderLock();
            var modInfo = GetModuleInfo(pid, address);

            //if (_moduleAddress.TryGetValue(new KeyValuePair<uint, UInt64>(pid, address), out modInfo))
            var ret = modInfo != null ? modInfo.Company : "";
            ReleaseReaderLock();
            return ret;
        }

        public string GetDescription(uint pid, UInt64 address)
        {
            AcquireReaderLock();
            var modInfo = GetModuleInfo(pid, address);

            //if(_moduleAddress.TryGetValue(new KeyValuePair<uint, UInt64>(pid, address), out modInfo))
            var ret = modInfo != null ? modInfo.Description : "";
            ReleaseReaderLock();
            return ret;
        }

        //public bool IsSigned(string module)
        //{
        //    bool ret = false;
        //    lock (dataLock)
        //    {
        //        ModuleInfo info;
        //        if (modulePath.TryGetValue(module, out info))
        //        {
        //            ret = info.Signed;
        //        }
        //    }
        //    return ret;
        //}
        //public X509Certificate GetCertificate(string module)
        //{
        //    X509Certificate ret = null;
        //    lock (dataLock)
        //    {
        //        ModuleInfo info;
        //        if (modulePath.TryGetValue(module, out info))
        //        {
        //            ret = info.Certificate;
        //        }
        //    }
        //    return ret;
        //}
        public string GetBaseTooltip(uint pid, UInt64 address)
        {
            var tooltip = "";
            AcquireReaderLock();
            var modInfo = GetModuleInfo(pid, address);

            //if (_moduleAddress.TryGetValue(new KeyValuePair<uint, UInt64>(pid, address), out modInfo))
            if (modInfo != null)
            {
                tooltip = "Path: " + modInfo.Path;
                if (modInfo.Company != "")
                {
                    //if (IsSigned(module))
                    //    tooltip += "\nCompany: " + company + " (Signed)";
                    //else
                    tooltip += "\nCompany: " + modInfo.Company;
                }
                if (modInfo.Description != "")
                {
                    tooltip += "\nDescription: " + modInfo.Description;
                }
            }
            ReleaseReaderLock();
            return tooltip;
        }

        public ModuleInfo GetModuleByPath(uint pid, string path)
        {
            //return new ModuleInfo("testPAth", 1234, 1234567, "testcompany", "testing", null);

            AcquireReaderLock();

            ModuleInfo module;
            Dictionary<string, ModuleInfo> modulesByPath;

            if (!_modulesByPIDByPath.TryGetValue(pid, out modulesByPath))
            {
                ReleaseReaderLock();
                return null;
            }

            if (!modulesByPath.TryGetValue(path.ToLower(), out module))
            {
                ReleaseReaderLock();
                return null;
            }

            ReleaseReaderLock();
            return module;

            //try
            //{
            //    module = _modulesByPIDByPath[pid][path.ToLower()];
            //}
            //catch (KeyNotFoundException e)
            //{

            //}

            //ReleaseReaderLock();

            //return module;
        }
    }
}