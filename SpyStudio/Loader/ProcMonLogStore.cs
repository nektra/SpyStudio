using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Database;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using SpyStudio.Trace;

namespace SpyStudio.Loader
{
    class ProcMonLogStore
    {
        private XmlTextReader _traceInfo;
        private XmlDocument _processDoc;
        XmlDocument _operationInfo;
        private readonly LogStore _loader;

        class ProcessEventsWorkerData
        {
            public Thread Thread;
            //public List<StringBuilder> EventStrings { get; set; }
            public List<string> EventStrings { get; set; }
            //public List<StringBuilder> NextEventString { get; set; }
            public List<string> NextEventString { get; set; }
            public readonly List<CallEvent> Events = new List<CallEvent>();
            public bool Completed;
            public readonly AutoResetEvent WaitEvent = new AutoResetEvent(false);
            public WaitHandle FinishEvent { get; set; }
        }
        public ProcMonLogStore(LogStore loader, XmlTextReader traceInfo)
        {
            _traceInfo = traceInfo;
            _loader = loader;
        }

        public uint TraceId { get; set; }

        private void ProcessEventsThread(object data)
        {
            var threadData = (ProcessEventsWorkerData)data;
            var waitHandles = new WaitHandle[2];

            waitHandles[0] = threadData.FinishEvent;
            waitHandles[1] = threadData.WaitEvent;
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif

            while (WaitHandle.WaitAny(waitHandles) == 1)
            {
                foreach (var eventInnerXml in threadData.EventStrings)
                {
#if DEBUG
                    var previousLoad = sw.Elapsed.TotalMilliseconds;
#endif
                    var callEvent = LoadEvent(eventInnerXml);
                    if (callEvent != null)
                        threadData.Events.Add(callEvent);
#if DEBUG
                    _timeLoad += (sw.Elapsed.TotalMilliseconds - previousLoad);
#endif
                }
                threadData.Completed = true;
            }
        }
        void WaitCompletion(ProcessEventsWorkerData[] processEventsThreads)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            bool completed;
            do
            {
                completed = true;
                foreach(var threadData in processEventsThreads)
                {
                    completed = completed && (threadData.EventStrings == null || threadData.EventStrings.Count == 0 || threadData.Completed);
                }
                if(!completed)
                    Thread.Sleep(100);
            } while (!completed);
#if DEBUG
            _timeWait += sw.Elapsed.TotalMilliseconds;
#endif
        }
        void WaitCompletion(ProcessEventsWorkerData threadData)
        {
            while(threadData.EventStrings != null && threadData.EventStrings.Count != 0 && !threadData.Completed)
            {
                Thread.Sleep(100);
            }
        }
        void ProcessThreadsData(ProcessEventsWorkerData threadData)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            WaitCompletion(threadData);
#if DEBUG
            _timeWait += sw.Elapsed.TotalMilliseconds;
#endif

            var db = EventDatabaseMgr.GetInstance();

            if (threadData.Events.Count > 0)
            {
#if DEBUG
                var previousDb = sw.Elapsed.TotalMilliseconds;
#endif

                db.AddEventRange(threadData.Events, true);
                threadData.Events.Clear();
                threadData.EventStrings = null;
                threadData.NextEventString = new List<string>();
                threadData.Completed = false;
#if DEBUG
                _timeDb += (sw.Elapsed.TotalMilliseconds - previousDb);
#endif
            }
        }

        void ProcessThreadsData(IEnumerable<ProcessEventsWorkerData> processEventsThreads)
        {
            foreach(var threadData in processEventsThreads)
            {
                ProcessThreadsData(threadData);
            }
        }
#if DEBUG
        private double _timeProcesses, _timeModules, _timeEvents, _timeComplete, _timeStack, _timeDb, _timeRefresh, _timeLoad, _timeXml, _timeXmlDoc;
        private double _timeWait, _timeStack1, _timeStack2, _timeStack3, _timeGetInner, _timeMove;
#endif
        private const int MaxEventThread = 3;
        public bool LoadLog()
        {
#if DEBUG
            _timeComplete =
                _timeStack =
                _timeDb =
                _timeLoad =
                _timeXml =
                _timeXmlDoc = _timeWait = _timeStack1 = _timeStack2 = _timeStack3 = _timeGetInner = _timeMove = 0;
            var sw = new Stopwatch();
            sw.Start();
#endif
            var db = EventDatabaseMgr.GetInstance();

            var finishEvent = new ManualResetEvent(false);
            var threadsData = new ProcessEventsWorkerData[MaxEventThread];
            for(int i = 0; i<threadsData.Length; i++)
            {
                threadsData[i] = new ProcessEventsWorkerData
                                              {Thread = new Thread(ProcessEventsThread), FinishEvent = finishEvent, Completed = true};
                threadsData[i].Thread.Start(threadsData[i]);
                threadsData[i].NextEventString = new List<string>();
            }

            int minimum = _loader.CurrentProgress();

            _loader.ReportProgress(minimum);

            if (!FillProcessInfoProcMon())
            {
                _loader.ReportError("Error loading xml: Cannot load processes information");
                return false;
            }

#if DEBUG
            _timeProcesses = sw.Elapsed.TotalMilliseconds;
            var previous = sw.Elapsed.TotalMilliseconds;
#endif
            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int)((_loader.MaximumProgress - _loader.MinimumProgress) *
                                          ((double)_loader.Position / _loader.TotalSize)));

            if (_loader.Canceled)
                return false;

            if (!FillModulePathProcMon())
            {
                _loader.ReportError("Error loading xml: Cannot find modules");
                return false;
            }

#if DEBUG
            _timeModules = sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int)((_loader.MaximumProgress - _loader.MinimumProgress) *
                                          ((double)_loader.Position / _loader.TotalSize)));

            if (!_loader.RestartXml(ref _traceInfo))
                return false;

            if (!_traceInfo.MoveTo("eventlist", 1))
            {
                _loader.ReportError("Error loading xml : Cannot find events");
                return false;
            }

            _loader.OperationBegin();

            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int)((_loader.MaximumProgress - _loader.MinimumProgress) * 0.9 *
                                          ((double)_loader.Position / _loader.TotalSize)));

            // for testing
            //var count = 300000;
            //const bool limitEntries = false;
            // for testing
            int doReport = 0;
#if DEBUG
            var previousXml = sw.Elapsed.TotalMilliseconds;
#endif
            int currentThread = 0;
            while (_traceInfo.MoveTo("event", XmlNodeType.Element, 2))
            {
#if DEBUG
                _timeMove += sw.Elapsed.TotalMilliseconds - previousXml;
#endif
                var innerXml = _traceInfo.ReadInnerXml();
#if DEBUG
                _timeXml += sw.Elapsed.TotalMilliseconds - previousXml;
#endif

                if (threadsData[currentThread].NextEventString.Count <
                    EventDatabaseMgr.MaxEventCountProcessPerAction/threadsData.Length)
                {
                    threadsData[currentThread].NextEventString.Add(innerXml);
                }
                else
                {
                    threadsData[currentThread].EventStrings = threadsData[currentThread].NextEventString;
                    threadsData[currentThread].NextEventString = new List<string>();
                    threadsData[currentThread].Completed = false;
                    threadsData[currentThread].WaitEvent.Set();

                    if (++currentThread >= threadsData.Length)
                    {
                        currentThread = 0;
                    }
                    if (threadsData[currentThread].EventStrings != null &&
                        threadsData[currentThread].EventStrings.Count != 0)
                    {
                        ProcessThreadsData(threadsData[currentThread]);
                    }
                }
                if ((++doReport) >= 128)
                {
                    doReport = 0;
                    _loader.ReportProgress(_loader.MinimumProgress +
                                           (int) ((_loader.MaximumProgress - _loader.MinimumProgress)*0.9*
                                                  ((double) _loader.Position/_loader.TotalSize)));
                }
                if (_loader.Canceled)
                {
                    break;
                }
                // for testing
                //if (limitEntries && count-- <= 0)
                //    break;
                // for testing
#if DEBUG
                previousXml = sw.Elapsed.TotalMilliseconds;
#endif
            }

            ProcessThreadsData(threadsData);

            WaitCompletion(threadsData);

#if DEBUG
            _timeEvents = sw.Elapsed.TotalMilliseconds - previous;
            previous = sw.Elapsed.TotalMilliseconds;
#endif

            finishEvent.Set();
            foreach (var processEventsThread in threadsData)
            {
                processEventsThread.Thread.Join();
            }

            //var refreshData = new EventsReportData(TraceId, false, _loader.ProgressDlg);

            int minimumWait = _loader.MinimumProgress +
                          (int) ((_loader.MaximumProgress - _loader.MinimumProgress)*0.9);
            int initialCount = db.PendingEventsToAdd();
            while(db.AnyUpdateOperationPending())
            {
                Thread.Sleep(250);
                Application.DoEvents();
                _loader.ReportProgress(minimumWait +
                                       (int) ((_loader.MaximumProgress - _loader.MinimumProgress)*0.1*
                                              ((double) (initialCount - db.PendingEventsToAdd())/initialCount)));
            }

            _loader.OperationEnd();

#if DEBUG
            _timeRefresh = sw.Elapsed.TotalMilliseconds - previous;
            Debug.WriteLine("Processes:\t" + _timeProcesses +
                "\nModules:\t" + _timeModules +
                "\nComplete:\t" + _timeComplete +
                "\nLoad:\t" + _timeLoad +
                "\nLoad Xml:\t" + _timeXml +
                "\nLoad Xml Move:\t" + _timeMove +
                "\nLoad Xml Doc:\t" + _timeXmlDoc +
                "\nStack:\t" + _timeStack +
                "\nStack1:\t" + _timeStack1 +
                "\nStack2:\t" + _timeStack2 +
                "\nStack3:\t" + _timeStack3 +
                "\nGet Inner:\t" + _timeGetInner +
                "\nDb:\t" + _timeDb +
                "\nWait:\t" + _timeWait +
                "\nEvents:\t" + _timeEvents +
                "\nRefresh:\t" + _timeRefresh);
#endif
            
            if (!_loader.Canceled)
                _loader.ReportProgress(_loader.MaximumProgress);
            _traceInfo.Close();

            //refreshData.EventsReady -= DBOnEventsInvokerReport;

            return !_loader.Canceled;
        }

        bool FillProcessInfoProcMon()
        {
            bool loadsuccess = true;

            if (!_traceInfo.MoveTo("processlist", 1))
            {
                return false;
            }

            var processInfo = "<processlist>" + _traceInfo.ReadInnerXml() + "</processlist>";
            _processDoc = new XmlDocument();
            _processDoc.LoadXml(processInfo);
            XmlNodeList processesNodes = _processDoc.SelectNodes("/processlist/process");

            //XmlNodeList processesNodes = rootNode.SelectNodes("/procmon/processlist/process");

            if (processesNodes == null || processesNodes.Count == 0)
                return false;

            var processInfosFilled = 0;

            lock (_loader.ProcessInfo.ProcessesLock)
            {
                try
                {
                    // remove previous modules
                    _loader.ProcessInfo.Processes.Clear();
                    _loader.ProcessInfo.ProcessIcons.Clear();

                    foreach (XmlNode procNode in processesNodes)
                    {
                        string path = "",
                               name = "",
                               integrity = "",
                               owner = "",
                               commandLine = "",
                               companyName = "",
                               version = "",
                               description = "";

                        uint pid = 0, parentPid = 0;
                        bool is64 = false;

                        XmlNode n = procNode["ProcessName"];
                        if (n != null)
                            name = n.InnerText;
                        n = procNode["ProcessId"];
                        if (n != null)
                            pid = StringTools.ConvertToUInt32(n.InnerText);
                        n = procNode["ParentProcessId"];
                        if (n != null)
                            parentPid = StringTools.ConvertToUInt32(n.InnerText);
                        n = procNode["Is64bit"];
                        if (n != null)
                            is64 = (n.InnerText == "1");
                        n = procNode["Integrity"];
                        if (n != null)
                            integrity = n.InnerText;
                        n = procNode["Owner"];
                        if (n != null)
                            owner = n.InnerText;
                        n = procNode["ImagePath"];
                        if (n != null)
                            path = n.InnerText;
                        n = procNode["CommandLine"];
                        if (n != null)
                            commandLine = n.InnerText;
                        n = procNode["CompanyName"];
                        if (n != null)
                            companyName = n.InnerText;
                        n = procNode["Version"];
                        if (n != null)
                            version = n.InnerText;
                        n = procNode["Description"];
                        if (n != null)
                            description = n.InnerText;

                        _loader.ProcessInfo.Add(name, path, pid, parentPid, is64, integrity, owner, commandLine, companyName, version,
                                        description);

                        processInfosFilled++;

                        if (processInfosFilled >= 100)
                        {
                            Application.DoEvents();
                            processInfosFilled = 0;
                        }
                    }
                }
                catch (Exception)
                {
                    loadsuccess = false;
                }
            }
            return loadsuccess;
        }
        public bool FillModulePathProcMon()
        {
            bool loadsuccess = true;
            XmlNodeList processesNodes = _processDoc.SelectNodes("/processlist/process");

            if (processesNodes == null || processesNodes.Count == 0)
                return false;

            try
            {
                _loader.ModulePath.AcquireWriterLock();
                // remove previous modules
                _loader.ModulePath.ModuleAddress.Clear();

                var modulePathsFilled = 0;

                foreach (XmlNode procNode in processesNodes)
                {
                    uint pid = 0;

                    XmlNode n = procNode["ProcessId"];
                    if (n != null)
                        pid = StringTools.ConvertToUInt32(n.InnerText);

                    XmlNodeList modulesNode = procNode.SelectNodes("modulelist/module");
                    if (modulesNode != null)
                    {
                        foreach (XmlNode modNode in modulesNode)
                        {
                            var modInfo = new ModulePath.ModuleInfo();
                            n = modNode["BaseAddress"];
                            if (n != null)
                                modInfo.Address = StringTools.ConvertToUInt64(n.InnerText);
                            n = modNode["Size"];
                            if (n != null)
                                modInfo.Size = StringTools.ConvertToUInt64(n.InnerText);
                            n = modNode["Path"];
                            if (n != null)
                                modInfo.Path = n.InnerText;
                            modInfo.Pid = pid;
                            n = modNode["Company"];
                            if (n != null)
                                modInfo.Company = n.InnerText;
                            n = modNode["Description"];
                            if (n != null)
                                modInfo.Description = n.InnerText;

                            _loader.ModulePath.AddModuleByAddressAndPath(modInfo);

                            modulePathsFilled++;

                            if (modulePathsFilled >= 100)
                            {
                                Application.DoEvents();
                                modulePathsFilled = 0;
                            }
                        }
                    }
                }
                _loader.ModulePath.ReleaseWriterLock();
            }
            catch (Exception)
            {
                loadsuccess = false;
            }

            return loadsuccess;
        }
        void CompleteEvent(CallEvent e, List<Param> parameters)
        {
            if (_operationInfo == null)
            {
                _operationInfo = new XmlDocument();
                _operationInfo.LoadXml(Resources.ProcMon);
            }
            var node = _operationInfo.SelectSingleNode(@"/operations/operation[@name='" + e.Function + "']");
            if (node == null || node.Attributes == null)
            {
                e.Type = HookType.Custom;
            }
            else
            {
                e.Type = CallEvent.GetHookTypeFromString(node.Attributes["type"].InnerText);
            }
            Param p;
            bool handled = false;
            switch (e.Type)
            {
                case HookType.RegDeleteValue:
                case HookType.RegSetValue:
                case HookType.RegQueryValue:
                case HookType.RegEnumerateValueKey:
                    Param dataParam = DeviareTools.GetParamByName("Data", parameters);
                    Param typeParam = DeviareTools.GetParamByName("Type", parameters);
                    //Param lengthParam = DeviareTools.GetParamByName("Length", parameters);
                    RegistryValueKind valueType = typeParam == null
                                                      ? RegistryValueKind.Unknown
                                                      : RegistryTools.GetValueTypeFromString(typeParam.Value);
                    string data = (dataParam == null
                                       ? null
                                       : RegistryTools.GetRegValueRepresentation(dataParam.Value, valueType));

                    if (e.Type == HookType.RegEnumerateValueKey)
                    {
                        Param nameParam = DeviareTools.GetParamByName("Name", parameters);
                        string valueName = (nameParam == null ||
                                            String.Equals(nameParam.Value, "(default)",
                                                          StringComparison.OrdinalIgnoreCase))
                                               ? string.Empty
                                               : nameParam.Value;

                        if (nameParam == null ||
                            String.Equals(valueName, "(default)", StringComparison.OrdinalIgnoreCase))
                            valueName = "";

                        Param indexParam = DeviareTools.GetParamByName("Index", parameters);
                        e.CreateParams(5);
                        RegQueryValueEvent.CreatePath(false, e, parameters[0].Value, valueName, data, valueType);
                        e.Params[4].Name = "Index";
                        e.Params[4].Value = indexParam == null ? string.Empty : indexParam.Value;
                    }
                    else
                    {
                        string key;
                        string valueName = RegistryTools.GetRegValueFromPath(parameters[0].Value, out key);

                        if (String.Equals(valueName, "(default)", StringComparison.OrdinalIgnoreCase))
                            valueName = string.Empty;

                        RegQueryValueEvent.CreatePath(e, key, valueName, data, valueType);
                    }
                    //if (lengthParam != null)
                    //    e.SetProperty("Length", lengthParam.Value);
                    RegQueryValueEvent.SetDataComplete(e, false);
                    handled = true;
                    break;
                case HookType.CreateFile:
                    FileSystemAccess access;
                    if (e.Function == "CreateFileMapping")
                    {
                        access = FileSystemAccess.Read;
                    }
                    else
                    {
                        p = DeviareTools.GetParamByName("Desired Access", parameters);
                        if (p != null)
                        {
                            access = GetFileAccess(p.Value);
                        }
                        else
                        {
                            access = FileSystemAccess.Read;
                            Console.WriteLine(Resources.LogStore_CompleteEvent_Cannot_find_Desired_Access);
                        }
                    }

                    if (access == FileSystemAccess.ReadAttributes)
                    {
                        FileSystemEvent.SetQueryAttributes(e, true);
                    }
                    CreateFileEvent.CreateEventParams(e, parameters[0].Value, access, false);
                    var newParams = new List<Param>();
                    newParams.AddRange(e.Params);
                    // add all params except the Path and the access that were already added
                    newParams.AddRange(parameters.GetRange(1, parameters.Count - 1));
                    e.Params = newParams.ToArray();
                    handled = true;
                    break;
                case HookType.CreateProcess:
                    if (e.Function == "Process Start")
                    {
                        e.Type = HookType.CreateProcess;
                        e.RetValue = 1;
                        var path = _loader.ProcessInfo.Contains(e.Pid)
                                       ? _loader.ProcessInfo.GetPath(e.Pid)
                                       : string.Empty;
                        e.CreateParams(4);
                        e.Params[3].Name = "Parent PID";
                        e.Params[3].Value = parameters.Count > 1 ? parameters[1].Value : string.Empty;
                        CreateProcessEvent.CreateEventParams(false, e,
                                                             path,
                                                             "", e.Pid);

                    }
                    else
                    {
                        string cmdLine = "";
                        uint pid = 0;
                        p = DeviareTools.GetParamByName("Command line", parameters);
                        if (p != null)
                            cmdLine = p.Value;
                        p = DeviareTools.GetParamByName("PID", parameters);
                        if (p != null)
                            pid = StringTools.ConvertToUInt32(p.Value);
                        CreateProcessEvent.CreateEventParams(e, parameters[0].Value, cmdLine, pid, e.Success);
                    }
                    handled = true;
                    break;
                case HookType.LoadLibrary:
                    LoadLibraryEvent.CreateEventParams(e, parameters[0].Value);
                    handled = true;
                    break;
                case HookType.CloseFile:
                    FileSystemEvent.SetAccess(e, FileSystemAccess.None);
                    break;
                case HookType.ReadFile:
                    FileSystemEvent.SetAccess(e, FileSystemAccess.Read);
                    break;
                case HookType.WriteFile:
                    FileSystemEvent.SetAccess(e, FileSystemAccess.Write);
                    break;
                case HookType.QueryAttributesFile:
                    FileSystemEvent.SetAccess(e, FileSystemAccess.Read);
                    FileSystemEvent.SetQueryAttributes(e, true);
                    break;
                case HookType.QueryDirectoryFile:
                    {
                        Param pathParam = DeviareTools.GetParamByName("Path", parameters);
                        Param filterParam = DeviareTools.GetParamByName("Filter", parameters);

                        var parsedFields = new List<FileSystemTools.FileInformation>();
                        int i = 1;
                        Param file = DeviareTools.GetParamByName(i.ToString(CultureInfo.InvariantCulture), parameters);
                        while (file != null)
                        {
                            parsedFields.Add(new FileSystemTools.FileInformation("Name" + i++, file.Value,
                                                                                 FileSystemTools.FileAttribute.
                                                                                     FileAttributeReadonly));
                            file = DeviareTools.GetParamByName(i.ToString(CultureInfo.InvariantCulture), parameters);
                        }

                        e.CreateParams(4 + parsedFields.Count);
                        e.Params[0].Name = "Path";
                        e.Params[0].Value = pathParam == null ? string.Empty : pathParam.Value;
                        e.Params[1].Name = "Wildcard";
                        e.Params[1].Value = filterParam == null ? string.Empty : filterParam.Value;
                        e.Params[2].Name = "FileInfoClass";
                        e.Params[2].Value = string.Empty;
                        e.Params[3].Name = "RestartScan";
                        e.Params[3].Value = pathParam != null ? "TRUE" : "FALSE";
                        FileSystemEvent.SetDirectory(e, true);
                        FileSystemEvent.SetAccess(e, FileSystemAccess.Read);
                        FileSystemEvent.SetQueryAttributes(e, true);

                        //i = 0;
                        //foreach (var param in parsedFields)
                        //{
                        //    e.Params[4 + i].Name = "File" + (i + 1);
                        //    e.Params[4 + i++].Value = param.FieldName;
                        //    e.SetProperty(param.FieldName, (uint) param.Attributes);
                        //}
                        break;
                    }
                case HookType.SetAttributesFile:
                    {
                        FileSystemEvent.SetAccess(e, FileSystemAccess.Read);
                        break;
                    }
                case HookType.RegEnumerateKey:
                    {
                        e.CreateParams(parameters.Count);
                        Param nameParam = DeviareTools.GetParamByName("Name", parameters);
                        if (nameParam != null)
                        {
                            e.Params[0].Value = parameters[0].Value + "\\" + nameParam.Value;
                        }
                        else
                        {
                            e.Params[0].Value = parameters[0].Value;
                        }

                        e.Params[0].Name = "Path";

                        var indexParam = DeviareTools.GetParamByName("Index", parameters);
                        e.Params[1].Name = "Index";
                        e.Params[1].Value = indexParam == null ? string.Empty : indexParam.Value;

                        // add the rest of the parameters
                        int i = 0;
                        foreach (var f in parameters)
                        {
                            if (i > 1)
                            {
                                e.Params[i].Name = f.Name;
                                e.Params[i].Value = f.Value;
                            }
                            i++;
                        }
                    }
                    break;

            }
            e.IsProcMon = true;
            if (!handled && parameters.Count > 0)
            {
                e.CreateParams(parameters.Count);
                e.ParamMainIndex = 0;
                for (var i = 0; i < parameters.Count; i++)
                {
                    e.Params[i] = parameters[i];
                }
            }
            if (e.IsFileSystem && parameters.Count > 0)
            {
                FileSystemEvent.SetDirectory(e, false);
                var filepart = FileSystemTools.GetFileName(parameters[0].Value);
                FileSystemEvent.SetFilepart(e, filepart);
            }
        }
        string GetInnerText(int startIndex, string text, string tag)
        {
            int startIndexResult, endIndexResult;
            return GetInnerText(startIndex, text, tag, out startIndexResult, out endIndexResult);
        }

        string GetInnerText(int startIndex, string text, string tag, out int startIndexResult, out int endIndexResult)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            if (startIndex < 0)
                startIndex = 0;
            string ret = string.Empty;
            var startTag = "<" + tag + ">";
            endIndexResult = -1;
            startIndexResult = text.IndexOf(startTag, startIndex, StringComparison.Ordinal);
            if (startIndexResult != -1)
            {
                endIndexResult = text.IndexOf("</" + tag + ">", startIndexResult, StringComparison.Ordinal);
                if (endIndexResult != -1)
                {
                    startIndexResult = startIndexResult + startTag.Length;
                    ret = text.Substring(startIndexResult, endIndexResult - startIndexResult);
                }
            }
#if DEBUG
            _timeGetInner += sw.Elapsed.TotalMilliseconds;
#endif
            return ret;
        }
        CallEvent LoadEvent(string eventText)
        {
            bool kernelCall = false;
            var callEvent = new CallEvent(true) {Before = false};

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            double previous;
#endif

            string path;
            int frameStart, frameEnd;
            string frameString = GetInnerText(0, eventText, "frame", out frameStart, out frameEnd);
            string lastModule = null;

            if (!string.IsNullOrEmpty(frameString))
            {
                kernelCall = true;
                var stack = new List<string>();

                while (!string.IsNullOrEmpty(frameString))
                {
#if DEBUG
                    previous = sw.Elapsed.TotalMilliseconds;
#endif
                    string modulePath = string.Empty,
                           moduleName = string.Empty,
                           nearestSymbol = string.Empty,
                           moduleAddressString = string.Empty,
                           offsetString = string.Empty,
                           eipString = string.Empty;
                    UInt64 eip, offset = 0, moduleAddress;

                    int startIndex, endIndex;
                    var addressString = GetInnerText(0, frameString, "address", out startIndex, out endIndex);

                    path = GetInnerText(endIndex, frameString, "path", out startIndex, out endIndex);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var mod = _loader.ModulePath.GetModuleByPath(callEvent.Pid, path);

                        if (mod != null)
                        {
                            kernelCall = false;
                            modulePath = mod.Path;
                            moduleAddress = mod.Address;
                        }
                        else
                        {
                            modulePath = path;
                            moduleAddress = 0;
                        }

                        moduleName = ModulePath.ExtractModuleName(modulePath);
                        nearestSymbol = moduleName;
                        if (!string.IsNullOrEmpty(addressString))
                        {
                            eipString = addressString;
                            eip = StringTools.ConvertToUInt64(eipString);
                            offset = eip - moduleAddress;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(addressString))
                        {
                            eip = StringTools.ConvertToUInt64(addressString);
                            var mod = _loader.ModulePath.GetModuleByAddress(callEvent.Pid, eip);
                            if (mod != null)
                            {
                                kernelCall = false;
                                modulePath = mod.Path;
                                moduleAddress = mod.Address;
                                moduleName = ModulePath.ExtractModuleName(mod.Path);
                                nearestSymbol = moduleName;
                                offset = eip - moduleAddress;
                            }
                        }

                    }
#if DEBUG
                    _timeStack1 += sw.Elapsed.TotalMilliseconds - previous;
                    previous = sw.Elapsed.TotalMilliseconds;
#endif
                    //if the stack has location -> override default nearest Symbol because location has the nearest exported function
                    var locationString = GetInnerText(endIndex, frameString, "location");
                    if (!string.IsNullOrEmpty(locationString))
                    {
                        var index = locationString.IndexOf('+');
                        if (index != -1)
                        {
                            nearestSymbol = (string.IsNullOrEmpty(moduleName) ||
                                             locationString.ToLower().StartsWith(moduleName.ToLower())
                                                 ? string.Empty
                                                 : (moduleName + "!")) +
                                            locationString.Substring(0, index).Trim();
                            offsetString = locationString.Substring(index + 1, locationString.Length - index - 1).Trim();
                            var indexComma = offsetString.IndexOf(',');
                            if (indexComma != -1)
                            {
                                offsetString = offsetString.Substring(0, indexComma).Trim();
                            }
                        }
                    }
#if DEBUG
                    _timeStack2 += sw.Elapsed.TotalMilliseconds - previous;
                    previous = sw.Elapsed.TotalMilliseconds;
#endif

                    if (!kernelCall || Settings.Default.ProcMonShowKernelStack)
                    {
                        if (string.IsNullOrEmpty(offsetString) && offset != 0)
                            offsetString = offset.ToString(CultureInfo.InvariantCulture);

                        stack.Add(modulePath);
                        stack.Add(moduleAddressString);
                        stack.Add(eipString);
                        stack.Add(nearestSymbol);
                        stack.Add(offsetString);

                        if (string.IsNullOrEmpty(callEvent.CallModule) && !string.IsNullOrEmpty(moduleName))
                        {
                            if (!DeviareTools.IsSystemModule(moduleName))
                                callEvent.CallModule = moduleName;
                            lastModule = moduleName;
                        }

                    }

                    frameString = GetInnerText(frameEnd, eventText, "frame", out frameStart, out frameEnd);
#if DEBUG
                    _timeStack3 += sw.Elapsed.TotalMilliseconds - previous;
#endif
                }
                if (stack.Count > 0)
                {
                    callEvent.CallStackStrings = stack.ToArray();
                    if (string.IsNullOrEmpty(callEvent.CallModule) && !string.IsNullOrEmpty(lastModule))
                        callEvent.CallModule = lastModule;
                }
            }
#if DEBUG
            _timeStack += sw.Elapsed.TotalMilliseconds;
#endif
            if (kernelCall && !Settings.Default.ProcMonLoadKernelCalls)
            {
                return null;
            }
            
            string pidString = GetInnerText(0, eventText, "PID");
            if(!string.IsNullOrEmpty(pidString))
            {
                callEvent.Pid = StringTools.ConvertToUInt32(pidString);
                callEvent.ProcessName = _loader.ProcessInfo.GetName(callEvent.Pid);
                if (_loader.IsProcessNameFiltered(callEvent.ProcessName))
                    return null;
            }
            var parameters = new List<Param>();
            string tidString = GetInnerText(0, eventText, "TID");
            if (!string.IsNullOrEmpty(tidString))
                callEvent.Tid = StringTools.ConvertToUInt32(tidString);
            string opString = GetInnerText(0, eventText, "Operation");
            if (!string.IsNullOrEmpty(opString))
                callEvent.Function = opString;
            string durationString = GetInnerText(0, eventText, "Duration");
            if (!string.IsNullOrEmpty(durationString))
            {
                callEvent.Time = Convert.ToDouble(durationString, CultureInfo.InvariantCulture);
            }
            string resultString = GetInnerText(0, eventText, "Result");
            if (!string.IsNullOrEmpty(resultString))
            {
                callEvent.Result = resultString;
                callEvent.Success = (callEvent.Result == "SUCCESS" ||
                                callEvent.Result == "REPARSE");
            }
            path = GetInnerText(0, eventText, "Path");
            if (!string.IsNullOrEmpty(path))
            {
                // modify registry operations to 
                if (callEvent.Function.StartsWith("Reg"))
                {
                    path = RegistryTools.FixBackSlashesIn(path);
                    var index = path.IndexOf('\\');
                    var rootKey = index > 0 ? path.Substring(0, index) : path;
                    string replacementKey;
                    if (RegistryTools.KeyAbbreviations.TryGetValue(rootKey, out replacementKey))
                    {
                        path = index > 0
                                   ? replacementKey + path.Substring(index, path.Length - index)
                                   : replacementKey;
                    }
                }
                parameters.Add(new Param("Path", path));
            }
            string detail = GetInnerText(0, eventText, "Detail");
            if (!string.IsNullOrEmpty(detail))
            {
                int start = 0;

                int index = detail.IndexOf(':');
                while (index != -1)
                {
                    int len = index - start;
                    string name = detail.Substring(start, len);
                    string value = "";
                    start = index + 2;
                    if(start < detail.Length)
                    {
                        // When there is a Command line property it's the last one and very difficult to parse following the generic way because it
                        // has lots of quotes and , : inside.
                        if (name.Trim() == "Command line")
                        {
                            value = detail.Substring(start);
                            index = -1;
                        }
                        else if (name == "CreationTime" || name == "LastAccessTime" || name == "LastWriteTime" || name == "ChangeTime" || name == "VolumeCreationTime")
                        {
                            len = StringTools.IndexOfIgnoreQuotes(detail, ',', start);
                            if (len == -1)
                            {
                                len = detail.Length;
                                index = -1;
                            }
                            else
                            {
                                index = len;
                            }
                            len -= start;
                            value = detail.Substring(start, len);
                        }
                        else
                        {
                            index = StringTools.IndexOfIgnoreQuotes(detail, ',', start);
                            if (index != -1)
                            {
                                // after the , try to find a : that follow the property name, this 2 searches avoid the : in the paths
                                index = StringTools.IndexOfIgnoreQuotes(detail, ':', index);
                            }
                            if (index == -1)
                            {
                                value = detail.Substring(start);
                            }
                            else
                            {
                                // if there is another property len points to the previous ',', everything that is between the ':' and the previous ',' is the value
                                len = StringTools.LastIndexOfIgnoreQuotes(detail, ',', index);

                                // it shouldn't happen because before a new prop there is a ',' but just in case
                                if (len == -1)
                                {
                                    len = StringTools.LastIndexOfIgnoreQuotes(detail, ' ', index);
                                    if (len == -1)
                                    {
                                        Error.WriteLine("Detail field error: " + detail);
                                        break;
                                    }
                                }
                                if (len != -1)
                                {
                                    // it shouldn't happen because after , there is a : but just in case
                                    if (index != -1)
                                        index = len;
                                    len -= start;
                                    value = detail.Substring(start, len);
                                }
                            }
                        }
                    }
                    else
                    {
                        index = -1;
                    }
                    name = name.Trim();
                    value = value.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        parameters.Add(new Param(name, value));
                    }
                    if (index != -1)
                    {
                        while (detail[index] == ' ' || detail[index] == ',')
                            index++;
                        start = index;
                        index = StringTools.IndexOfIgnoreQuotes(detail, ':', index);
                    }
                }
            }
#if DEBUG
            previous = sw.Elapsed.TotalMilliseconds;
#endif
            CompleteEvent(callEvent, parameters);
#if DEBUG
            _timeComplete += sw.Elapsed.TotalMilliseconds - previous;
#endif

            callEvent.TraceId = TraceId;

            return callEvent;
        }
        public FileSystemAccess GetFileAccess(string access)
        {
            FileSystemAccess ret = 0;
            if (access.Contains("Generic Write") || access.Contains("Write Data/Add File"))
            {
                ret = FileSystemAccess.Write;
            }
            if (access.Contains("Generic Read/Write"))
            {
                ret |= FileSystemAccess.Read | FileSystemAccess.Write;
            }
            if (access.Contains("Read Control") || access.Contains("Read ReadAttributes") ||
                access.Contains("Read Data/List IsDirectory") || access.Contains("Generic Read") || access.Contains("Generic Read") ||
                access.Contains("Access System Security"))
            {
                ret |= FileSystemAccess.Read;
            }
            if (access.Contains("Execute/Traverse"))
            {
                ret |= FileSystemAccess.Execute;
            }
            if (access.Contains("Read Attributes"))
            {
                ret |= FileSystemAccess.ReadAttributes;
            }
            if (access.Contains("Write Attributes"))
            {
                ret |= FileSystemAccess.WriteAttributes;
            }
            if (access.Contains("Synchronize"))
            {
                ret |= FileSystemAccess.Synchronize;
            }

            return ret;
        }
    }
}