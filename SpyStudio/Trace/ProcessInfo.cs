using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Xml;
using Nektra.Deviare2;
using SpyStudio.Tools;

namespace SpyStudio.Trace
{
    [Serializable]
    public class ProcessInfo
    {
        [Serializable]
        public class ProcessData
        {
            public readonly string Path;
            public readonly string Name;
            public readonly uint Pid;
            public readonly Image Icon;
            public readonly bool Is64Bits;
            public uint ParentPid;
            public string Integrity, CommandLine, CompanyName, Version, Description, Owner;

            public ProcessData(string path, string name, uint pid, Image icon)
            {
                Path = path;
                Name = name;
                Pid = pid;
                Icon = icon;
                Is64Bits = PlatformTools.Is64Bits((int) Pid);
            }

            public ProcessData(string path, string name, uint pid, Image icon, bool is64Bits)
            {
                Path = path;
                Name = name;
                Pid = pid;
                Icon = icon;
                Is64Bits = is64Bits;
            }

            public ProcessData(string path, string name, uint pid, uint parentPid,
                               bool is64Bits, string integrity, string owner, string commandLine,
                               string version, string description)
            {
                Path = path;
                Name = name;
                Pid = pid;
                Icon = null;
                Is64Bits = is64Bits;
                ParentPid = parentPid;
                Integrity = integrity;
                Owner = owner;
                CommandLine = commandLine;
                Version = version;
                Description = description;
            }
        }

        [Serializable]
        public class ProcessIcon
        {
            public readonly Image Icon;
            public uint RefCount;

            public ProcessIcon(Image icon)
            {
                RefCount = 1;
                Icon = icon;
            }
        }

        private Dictionary<uint, ProcessData> _processes = new Dictionary<uint, ProcessData>();
        private Dictionary<string, ProcessIcon> _processIcons = new Dictionary<string, ProcessIcon>();
        private static readonly Dictionary<uint, IntPtr> ProcHandles = new Dictionary<uint, IntPtr>();
        private static INktSpyMgr _spyMgr;

        public object ProcessesLock
        {
            get { return _processes; }
        }

        public Dictionary<uint, ProcessData> Processes
        {
            get { return _processes; }
            set { _processes = value; }
        }

        public Dictionary<string, ProcessIcon> ProcessIcons
        {
            get { return _processIcons; }
        }

        ~ProcessInfo()
        {
            foreach (var procHandle in ProcHandles)
            {
                Declarations.CloseHandle(procHandle.Value);
            }
        }

        public void SetSpyMgr(INktSpyMgr spyMgr)
        {
            _spyMgr = spyMgr;
        }
        public void Add(string name, string path, uint pid)
        {
            string newPath;
            var procIcon = IconGetter.GetIcon(out newPath, (int)pid, path, this);
            Add(newPath, path, pid, procIcon);
        }

        public void Add(string name, string path, uint pid, Image icon)
        {
            lock (_processes)
            {
                _processes[pid] = new ProcessData(path, name, pid, icon);
                AddIconPath(path, icon);
            }
        }

        public void Add(string name, string path, uint pid, Image icon, bool is64)
        {
            lock (_processes)
            {
                _processes[pid] = new ProcessData(path, name, pid, icon, is64);
                AddIconPath(path, icon);
            }
        }

        private void AddIconPath(string path, Image icon)
        {
            path = path.ToLower();
            if (!_processIcons.ContainsKey(path))
            {
                _processIcons[path] = new ProcessIcon(icon);
            }
            else
            {
                _processIcons[path].RefCount++;
            }
        }

        public void Add(string name, string path, uint pid, uint parentPid, bool is64, string integrity,
                        string owner, string commandLine, string companyName, string version, string description)
        {
            lock (_processes)
            {
                _processes[pid] = new ProcessData(path, name, pid, parentPid, is64, integrity, owner, commandLine,
                                                  version,
                                                  description);
            }
        }

        public void Remove(uint pid)
        {
            lock (_processes)
            {
                ProcessData procData;

                if (_processes.TryGetValue(pid, out procData))
                {
                    var path = procData.Path.ToLower();

                    if (_processIcons.ContainsKey(path))
                    {
                        if (_processIcons[path].RefCount > 1)
                        {
                            _processIcons[path].RefCount--;
                        }
                        else
                        {
                            _processIcons.Remove(path);
                        }
                    }
                    _processes.Remove(pid);
                }
            }
        }

        public void ClearData()
        {
            lock (_processes)
            {
                _processes.Clear();
            }
        }

        public bool Contains(uint pid)
        {
            lock (_processes)
            {
                return _processes.ContainsKey(pid);
            }
        }

        public string GetName(uint pid)
        {
            var ret = "";

            lock (_processes)
            {
                ProcessData procData;

                if (_processes.TryGetValue(pid, out procData))
                    ret = procData.Name;
            }
            return ret;
        }

        public string GetPath(uint pid)
        {
            string ret;

            lock (_processes)
            {
                ProcessData procData;

                if (_processes.TryGetValue(pid, out procData))
                {
                    ret = procData.Path;
                }
                else
                {
                    var proc = _spyMgr.ProcessFromPID((int) pid);
                    ret = proc == null ? string.Empty : proc.Path;
                }
            }
            return ret;
        }

        public string GetStartTime(uint pid)
        {
            var ret = "";

            lock (_processes)
            {
                ProcessData procData;

                if (_processes.TryGetValue(pid, out procData))
                    ret = procData.Name;
            }
            return ret;
        }

        public Image GetIcon(uint pid)
        {
            Image ret = null;

            lock (_processes)
            {
                ProcessData procData;

                if (_processes.TryGetValue(pid, out procData))
                    ret = procData.Icon;
            }
            return ret;
        }

        public Image GetIcon(string path)
        {
            Image ret = null;

            lock (_processes)
            {
                ProcessIcon procIcon;

                if (_processIcons.TryGetValue(path.ToLower(), out procIcon))
                    ret = procIcon.Icon;
            }
            return ret;
        }

        public bool Is64Bits(uint pid)
        {
            var ret = false;

            lock (_processes)
            {
                ProcessData procData;

                if (_processes.TryGetValue(pid, out procData))
                    ret = procData.Is64Bits;
            }
            return ret;
        }

        public static IntPtr GetProcessHandle(uint pid)
        {
            IntPtr hProc;
            lock (ProcHandles)
            {
                if (!ProcHandles.TryGetValue(pid, out hProc))
                {
                    try
                    {
                        hProc = _spyMgr.ProcessHandle((int) pid, 0x40);
                        ProcHandles[pid] = hProc;
                    }
                    catch (Exception)
                    {
                        hProc = IntPtr.Zero;
                    }
                }
            }
            return hProc;
        }

        public static void ProcessTerminated(uint pid)
        {
            lock (ProcHandles)
            {
                IntPtr hProc;
                if (ProcHandles.TryGetValue(pid, out hProc))
                {
                    ProcHandles.Remove(pid);
                    Declarations.CloseHandle(hProc);
                }
            }
        }

        public bool FromXml(XmlNode rootNode)
        {
            var loadsuccess = true;
            XmlNode processesNode = rootNode["process-info"];

            if (processesNode == null)
                return false;

            lock (_processes)
            {
                try
                {
                    // remove previous modules
                    _processes = new Dictionary<uint, ProcessData>();
                    _processIcons = new Dictionary<string, ProcessIcon>();

                    foreach (XmlNode procNode in processesNode.ChildNodes)
                    {
                        string path = "", name = "";
                        uint pid = 0;
                        Image icon = null;

                        XmlNode n = procNode["name"];
                        if (n != null)
                            name = n.InnerText;
                        n = procNode["pid"];
                        if (n != null)
                            pid = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                        n = procNode["path"];
                        if (n != null)
                            path = n.InnerText;

                        n = procNode["icon"];
                        if (n != null)
                        {
                            var array = Convert.FromBase64String(n.InnerText);
                            icon = Image.FromStream(new MemoryStream(array));
                        }
                        Add(name, path, pid, icon);
                    }
                }
                catch (Exception)
                {
                    loadsuccess = false;
                }
            }
            return loadsuccess;
        }

        public XmlNode ToXml(XmlDocument doc)
        {
            var procRoot = doc.CreateElement("process-info");

            lock (_processes)
            {
                foreach (var item in _processes)
                {
                    var procData = item.Value;
                    var procNode = doc.CreateElement("process");
                    procRoot.AppendChild(procNode);

                    XmlNode n = doc.CreateElement("name");
                    n.InnerText = procData.Name;
                    procNode.AppendChild(n);
                    n = doc.CreateElement("pid");
                    n.InnerText = procData.Pid.ToString(CultureInfo.InvariantCulture);
                    procNode.AppendChild(n);
                    n = doc.CreateElement("path");
                    n.InnerText = procData.Path;
                    procNode.AppendChild(n);

                    if (procData.Icon != null)
                    {
                        n = doc.CreateElement("icon");

                        // serialize icon in base 64
                        using (var ms = new MemoryStream())
                        {
                            procData.Icon.Save(ms, ImageFormat.Png);
                            var array = ms.ToArray();

                            n.InnerText = Convert.ToBase64String(array);
                        }
                    }

                    procNode.AppendChild(n);
                }
            }

            return procRoot;
        }
    }
}