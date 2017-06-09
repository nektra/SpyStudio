using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using Ionic.Zip;
using Ionic.Zlib;
using SpyStudio.Database;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Loader
{
    class DeviareLogStore
    {
        private XmlTextReader _traceInfo;
        private readonly LogStore _loader;
        private int _eventsMinimumProgress, _eventsMaximumProgress;
        private int _stackMinimumProgress, _stackMaximumProgress;
        private readonly string _filename;
        private int _processedEvents;
        private int _minimumProgressInvoker, _maximumProgressInvoker, _minimumProgressZipExtract, _maximumProgressZipExtract;
        private XmlDocument _eventsDoc;
        private XmlTextWriter _tw;
        private int _reportsPending;

        public DeviareLogStore(LogStore loader, XmlTextReader traceInfo)
        {
            _loader = loader;
            _traceInfo = traceInfo;
            TraceId = 0;
        }
        public DeviareLogStore(LogStore loader, string filename)
        {
            _loader = loader;
            _filename = filename;
            TraceId = 0;
        }
        public DeviareLogStore(LogStore loader)
        {
            _loader = loader;
            TraceId = 0;
        }

        public bool XmlMode { get; set; }
        public uint TraceId { get; set; }
        public bool Win32Function { get; set; }
        public bool StackTraceString { get; set; }
        public bool LoadStackDb { get; set; }
        public bool RefreshEvents { get; set; }

        public bool LoadLog()
        {
            if (XmlMode)
                return LoadLogXml();
            return LoadLogDb();
        }

        bool LoadLogDb()
        {
            var db = EventDatabaseMgr.GetInstance();

            var databaseDir = EventDatabaseMgr.GetDatabaseDirectory();
            var modFilename = databaseDir + @"\info" + TraceId + ".xml";
            var miscDataFilename = databaseDir + @"\misc" + TraceId + ".xml";

            db.LockDatabase(TraceId);

            _loader.ProgressDlg.Message = "Extracting files...";
            var dbInfo = db.GetDatabasePath(TraceId, true);

            _minimumProgressZipExtract = _loader.MinimumProgress;
            _maximumProgressZipExtract = _loader.MinimumProgress +
                                         (int)((_loader.MaximumProgress - _loader.MinimumProgress) *
                                                (RefreshEvents
                                                     ? 0.2
                                                     : 0.9));

            var extractionItems = new List<ArchivalItem>
                                      {
                                          new ArchivalItem
                                              {
                                                  UncompressedPath = dbInfo.EventDbPath,
                                                  CompressedPattern = new Regex(@"EventsDb.*", RegexOptions.IgnoreCase),
                                              },
                                          new ArchivalItem
                                              {
                                                  Enable = LoadStackDb,
                                                  UncompressedPath = dbInfo.StackDbPath,
                                                  CompressedPattern = new Regex(@"StackDb.*", RegexOptions.IgnoreCase),
                                                  PerformInParallel = true,
                                              },
                                          new ArchivalItem
                                              {
                                                  UncompressedPath = modFilename,
                                                  CompressedPattern = new Regex(@"info\.xml", RegexOptions.IgnoreCase),
                                              },
                                          new ArchivalItem
                                              {
                                                  UncompressedPath = miscDataFilename,
                                                  CompressedPattern = new Regex(@"misc\.xml", RegexOptions.IgnoreCase),
                                              },
                                      };
            EventsReportData refreshData;

            using (var extractor = new ZipExtractor(_filename, extractionItems))
            {
                extractor.SetExtractProgressFunction(ZipOnExtractProgress);
                if (!extractor.Perform())
                {
                    db.UnlockDatabase(TraceId);
                    db.ClearDatabase(TraceId);
                    return false;
                }
                if (_loader.Canceled)
                {
                    extractor.Join();
                    return false;
                }

                _loader.ReportProgress(_maximumProgressZipExtract);

                if (!LoadModData(modFilename, db) || !LoadMiscData(miscDataFilename, db))
                {
                    return false;
                }
                if (RefreshEvents)
                {
                    _loader.ProgressDlg.Message = "Loading events...";

                    _loader.OperationBegin();

                    _minimumProgressInvoker =
                        (int) (_loader.MinimumProgress + (_loader.MaximumProgress - _loader.MinimumProgress)*
                               0.42);
                    _maximumProgressInvoker = _loader.MaximumProgress;

                    _reportsPending = 0;
                    refreshData = new EventsReportData(TraceId) {StackTraceString = true};
                    refreshData.EventsReady += DBOnEventsInvokerReportDbThread;
                    db.RefreshEvents(refreshData);

                    Application.DoEvents();

                    while (!refreshData.Event.WaitOne(50))
                    {
                        Application.DoEvents();
                    }
                    refreshData.EventsReady -= DBOnEventsInvokerReportDbThread;
                }

                extractor.Join();

                if (_loader.Canceled)
                {
                    if (RefreshEvents)
                        _loader.OperationEnd();
                    return false;
                }
            }

            _loader.ReportProgress(_loader.MaximumProgress);

            if (RefreshEvents)
                _loader.OperationEnd();

            if (_loader.Canceled)
                return false;

            return true;
        }

        private bool LoadModData(string modFilename, EventDatabaseMgr db)
        {
            var streamReader = File.OpenRead(modFilename);
            _traceInfo = new XmlTextReader(streamReader);

            if (!FillProcessInfoDeviare())
            {
                _loader.ReportError("Error loading xml: Cannot load processes information");
                db.UnlockDatabase(TraceId);
                db.ClearDatabase(TraceId);
                return false;
            }

            db.UnlockDatabase(TraceId);

            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int) ((_loader.MaximumProgress - _loader.MinimumProgress)*
                                          (RefreshEvents
                                               ? 0.41
                                               : 0.93)));

            if (_loader.Canceled)
                return false;

            if (!FillModulePathDeviare())
            {
                _loader.ReportError("Error loading xml: Cannot find modules");
                db.ClearDatabase(TraceId);
                return false;
            }

            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int) ((_loader.MaximumProgress - _loader.MinimumProgress)*
                                          (RefreshEvents
                                               ? 0.42
                                               : 0.95)));

            _traceInfo.Close();
            try
            {
                File.Delete(modFilename);
            }
            catch (Exception ex)
            {
                _loader.ReportError("Error loading xml: Cannot delete file " + modFilename + ": " + ex.Message);
                db.ClearDatabase(TraceId);
                return false;
            }
            return true;
        }

        public MiscLogData MiscLogData;

        private bool LoadMiscData(string miscDataFilename, EventDatabaseMgr db)
        {
            try
            {
                using (var file = new FileStream(miscDataFilename, FileMode.Open, FileAccess.Read))
                {
                    MiscLogData = MiscLogData.Restore(file);
                }
                File.Delete(miscDataFilename);
            }
            catch (Exception ex)
            {
                _loader.ReportError("Error loading xml: Cannot delete file " + miscDataFilename + ": " + ex.Message);
                db.ClearDatabase(TraceId);
                return false;
            }
            return true;
        }
        
#if DEBUG
        private string lastEntry = null;
#endif
        private void ZipOnExtractProgress(object sender, ExtractProgressEventArgs extractProgressEventArgs)
        {
            if (_loader.Canceled)
            {
                extractProgressEventArgs.Cancel = true;
                return;
            }

#if DEBUG
            if (lastEntry != extractProgressEventArgs.CurrentEntry.FileName)
            {
                lastEntry = extractProgressEventArgs.CurrentEntry.FileName;
                Debug.WriteLine("Extracting: " + lastEntry);
            }
#endif

            // only report the database
            if (extractProgressEventArgs.CurrentEntry != null && !extractProgressEventArgs.CurrentEntry.FileName.EndsWith("xml") && extractProgressEventArgs.TotalBytesToTransfer != 0)
            {
                _loader.ReportProgress(_minimumProgressZipExtract +
                                       (int)
                                       ((_maximumProgressZipExtract - _minimumProgressZipExtract)*
                                        extractProgressEventArgs.BytesTransferred/
                                        extractProgressEventArgs.TotalBytesToTransfer));
            }
        }
        private void DBOnEventsInvokerReportDbThread(object sender, EventsReadyArgs eventsReadyArgs)
        {
            if (_loader.Canceled)
            {
                eventsReadyArgs.Canceled = true;
                return;
            }
            Interlocked.Increment(ref _reportsPending);

            if (_loader.ProgressDlg != null)
            {
                _loader.ProgressDlg.BeginInvoke(new DBOnEventsInvokerReportDelegate(DBOnEventsInvokerReport),
                                                eventsReadyArgs);
                while (_reportsPending > 8)
                {
                    Thread.Sleep(200);
                }
            }
        }
        private delegate void DBOnEventsInvokerReportDelegate(EventsReadyArgs eventsReadyArgs);
        private void DBOnEventsInvokerReport(EventsReadyArgs eventsReadyArgs)
        {
            if (_loader.Canceled)
            {
                _loader.ProgressDlg.Message = "Canceling...";
                eventsReadyArgs.Canceled = true;
                Interlocked.Decrement(ref _reportsPending);
                return;
            }

            if (eventsReadyArgs.TotalEventCount != 0)
            {
                _loader.NewEventsReady(eventsReadyArgs);
                _processedEvents += eventsReadyArgs.Events.Count;
                _loader.ReportProgress(_minimumProgressInvoker +
                                       (_maximumProgressInvoker - _minimumProgressInvoker)*_processedEvents/
                                       eventsReadyArgs.TotalEventCount);
            }
            Interlocked.Decrement(ref _reportsPending);
        }

        bool LoadLogXml()
        {
            Debug.Assert(_traceInfo != null);

            if (!FillProcessInfoDeviare())
            {
                _loader.ReportError("Error loading xml: Cannot load processes information");
                return false;
            }

            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int)((_loader.MaximumProgress - _loader.MinimumProgress) *
                                   ((double)_loader.Position /  _loader.TotalSize)));

            if (_loader.Canceled)
                return false;

            if (!FillModulePathDeviare())
            {
                _loader.ReportError("Error loading xml: Cannot find modules");
                return false;
            }

            _loader.ReportProgress(_loader.MinimumProgress +
                                   (int)((_loader.MaximumProgress - _loader.MinimumProgress) *
                                          ((double)_loader.Position / _loader.TotalSize)));

            if (!_traceInfo.MoveTo("events", 1))
            {
                _loader.ReportError("Error loading xml : Cannot find events");
                return false;
            }

            _loader.OperationBegin();

            var eventDoc = new XmlDocument();
            var eventsReady = new EventsReadyArgs(new List<CallEvent>(), TraceId);

            while (_traceInfo.MoveTo("event", XmlNodeType.Element, 2))
            {
                string lastEventLoaded;

                var eventText = "<event>" + _traceInfo.ReadInnerXml() + "</event>";
                eventDoc.LoadXml(eventText);

                using (var stringWriter = new StringWriter())
                using (var xmlTextWriter = new XmlTextWriter(stringWriter) { Formatting = Formatting.Indented })
                {
                    eventDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    lastEventLoaded = stringWriter.GetStringBuilder().ToString();
                }

                var db = EventDatabaseMgr.GetInstance();
                try
                {
                    var e = CallEvent.FromXml(eventDoc["event"]);
                    if(string.IsNullOrEmpty(e.ProcessName))
                    {
                        e.ProcessName = _loader.ProcessInfo.GetName(e.Pid);
                    }
                    if (!_loader.IsFilteredByLoad(e))
                    {
                        //e.IsLoaded = true;
                        eventsReady.Events.Clear();
                        eventsReady.Events.Add(e);
                        e.TraceId = TraceId;
                        db.AddEvent(e, false);
                        _loader.NewEventsReady(eventsReady);
                    }
                }
                catch (Exception ex)
                {
                    Error.WriteLine("Error parsing event: " + ex.Message + "\n" + lastEventLoaded);
                    Debug.Assert(false);
                }
                _loader.ReportProgress(_loader.MinimumProgress +
                                       (int)((_loader.MaximumProgress - _loader.MinimumProgress) *
                                              ((double)_loader.Position / _loader.TotalSize)));

                if (_loader.Canceled)
                {
                    break;
                }
            }
            _loader.OperationEnd();

            if (!_loader.Canceled)
                _loader.ReportProgress(_loader.MaximumProgress);

            _traceInfo.Close();

            if (_loader.Canceled)
                return false;

            return true;
        }
        public bool FillProcessInfoDeviare()
        {
            bool loadsuccess = true;

            if (!_traceInfo.MoveTo("process-info", 1))
                return false;

            var processInfo = "<processlist>" + _traceInfo.ReadInnerXml() + "</processlist>";
            var processDoc = new XmlDocument();
            processDoc.LoadXml(processInfo);
            XmlNodeList processesNodes = processDoc.SelectNodes("/processlist/process");

            if (processesNodes == null || processesNodes.Count == 0)
                return false;

            lock (_loader.ProcessInfo.ProcessesLock)
            {
                try
                {
                    // remove previous modules
                    _loader.ProcessInfo.Processes.Clear();
                    _loader.ProcessInfo.ProcessIcons.Clear();

                    foreach (XmlNode procNode in processesNodes)
                    {
                        string path = "", name = "";
                        uint pid = 0;
                        Image icon = null;
                        bool is64 = false;

                        XmlNode n = procNode["name"];
                        if (n != null)
                            name = n.InnerText;
                        n = procNode["pid"];
                        if (n != null)
                            pid = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                        n = procNode["path"];
                        if (n != null)
                            path = n.InnerText;
                        n = procNode["is64Bit"];
                        if (n != null)
                            is64 = n.InnerText.ToLower() == "true";

                        n = procNode["icon"];
                        if (n != null)
                        {
                            byte[] array = Convert.FromBase64String(n.InnerText);
                            icon = Image.FromStream(new MemoryStream(array));
                        }
                        _loader.ProcessInfo.Add(name, path, pid, icon, is64);
                    }
                }
                catch (Exception)
                {
                    loadsuccess = false;
                }
            }
            return loadsuccess;
        }
        public bool FillModulePathDeviare()
        {
            bool loadsuccess = true;

            if (!_traceInfo.MoveTo("module-info", 1))
                return false;

            var moduleInfo = "<module-info>" + _traceInfo.ReadInnerXml() + "</module-info>";
            var moduleInfoDoc = new XmlDocument();
            moduleInfoDoc.LoadXml(moduleInfo);
            XmlNodeList modulesNodes = moduleInfoDoc.SelectNodes("/module-info/module");

            if (modulesNodes == null)
                return false;

            try
            {
                _loader.ModulePath.AcquireWriterLock();
                // remove previous modules
                _loader.ModulePath.ModuleAddress.Clear();

                foreach (XmlNode modNode in modulesNodes)
                {
                    var modInfo = new ModulePath.ModuleInfo();
                    XmlNode n = modNode["path"];
                    if (n != null)
                        modInfo.Path = n.InnerText;
                    n = modNode["pid"];
                    if (n != null)
                        modInfo.Pid = Convert.ToUInt32(n.InnerText, CultureInfo.InvariantCulture);
                    n = modNode["address"];
                    if (n != null)
                        modInfo.Address = UInt64.Parse(n.InnerText, NumberStyles.AllowHexSpecifier);
                    n = modNode["company"];
                    if (n != null)
                        modInfo.Company = n.InnerText;
                    n = modNode["description"];
                    if (n != null)
                        modInfo.Description = n.InnerText;
                    n = modNode["signed"];
                    if (n != null)
                        modInfo.Signed = Convert.ToBoolean(n.InnerText, CultureInfo.InvariantCulture);

                    _loader.ModulePath.AddModule(modInfo);
                }
                _loader.ModulePath.ReleaseWriterLock();
            }
            catch (Exception)
            {
                loadsuccess = false;
            }

            return loadsuccess;
        }
        public bool SaveLog(uint traceId)
        {
            bool success;

            _loader.ReportProgress(_loader.MinimumProgress);

            // assume 10% to load file
            // assume 10% to load modules

            _loader.ReportProgress(_loader.MinimumProgress + (_loader.MaximumProgress - _loader.MinimumProgress)*10/100);

            try
            {
                if (XmlMode)
                {
                    using (
                        var tw = new XmlTextWriter(_loader.Filename, Encoding.UTF8) { Formatting = Formatting.Indented })
                    {
                        tw.WriteStartDocument();

                        success = EventsToXml(tw, traceId, _loader.MinimumProgress,
                                              _loader.MinimumProgress +
                                              (_loader.MaximumProgress - _loader.MinimumProgress)*85/100);
                        if(!_loader.Canceled)
                        {
                            tw.Flush();
                            _loader.ReportProgress(_loader.MaximumProgress);
                        }
                    }
                }
                else
                {
                    success = EventsToFile(_loader.Filename, traceId, _loader.MinimumProgress, _loader.MaximumProgress);
                    _loader.ReportProgress(_loader.MaximumProgress);
                }
            }
            catch (Exception ex)
            {
                success = false;
                _loader.ReportError("Error saving xml " + _loader.Filename + ": " + ex.Message);
            }
            return success;
        }

        public XmlNode ProcessInfoToXml(XmlDocument doc)
        {
            XmlElement procRoot = doc.CreateElement("process-info");

            Debug.Assert(_loader.ProcessInfo != null);

            lock (_loader.ProcessInfo.ProcessesLock)
            {
                foreach (KeyValuePair<uint, ProcessInfo.ProcessData> item in _loader.ProcessInfo.Processes)
                {
                    ProcessInfo.ProcessData procData = item.Value;
                    XmlElement procNode = doc.CreateElement("process");
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
                    n = doc.CreateElement("is64Bit");
                    n.InnerText = procData.Is64Bits.ToString(CultureInfo.InvariantCulture);
                    procNode.AppendChild(n);

                    if (procData.Icon != null)
                    {
                        n = doc.CreateElement("icon");

                        // serialize icon in base 64
                        using (var ms = new MemoryStream())
                        {
                            procData.Icon.Save(ms, ImageFormat.Png);
                            byte[] array = ms.ToArray();

                            n.InnerText = Convert.ToBase64String(array);
                        }
                    }

                    procNode.AppendChild(n);
                }
            }

            return procRoot;
        }
        public XmlNode ModulePathToXml(XmlDocument doc)
        {
            XmlElement modRoot = doc.CreateElement("module-info");

            _loader.ModulePath.AcquireReaderLock();
            foreach (KeyValuePair<uint, SortedDictionary<UInt64, ModulePath.ModuleInfo>> procItem in _loader.ModulePath.ModuleAddress)
            {
                foreach(KeyValuePair<UInt64, ModulePath.ModuleInfo> item in procItem.Value)
                {
                    ModulePath.ModuleInfo modInfo = item.Value;
                    XmlElement modNode = doc.CreateElement("module");
                    modRoot.AppendChild(modNode);

                    XmlNode n = doc.CreateElement("path");
                    n.InnerText = modInfo.Path;
                    modNode.AppendChild(n);
                    n = doc.CreateElement("pid");
                    n.InnerText = modInfo.Pid.ToString(CultureInfo.InvariantCulture);
                    modNode.AppendChild(n);
                    n = doc.CreateElement("address");
                    n.InnerText = modInfo.Address.ToString("X");
                    modNode.AppendChild(n);
                    n = doc.CreateElement("company");
                    n.InnerText = modInfo.Company;
                    modNode.AppendChild(n);
                    n = doc.CreateElement("description");
                    n.InnerText = modInfo.Description;
                    modNode.AppendChild(n);
                    n = doc.CreateElement("signed");
                    n.InnerText = modInfo.Signed.ToString(CultureInfo.InvariantCulture);
                    modNode.AppendChild(n);
                }
            }
            _loader.ModulePath.ReleaseReaderLock();

            return modRoot;
        }

        public bool EventsToXml(XmlTextWriter tw, uint traceId, int minimum, int maximum)
        {
            _processedEvents = 0;

            //var saveEvents = new CallEvent[events.Count];
            //events.CopyTo(saveEvents);
            _eventsDoc = new XmlDocument();
            _tw = tw;

            tw.WriteStartElement("deviare-trace");
            tw.WriteAttributeString("deviare-log", _loader.ProcMonLog ? "false" : "true");
            XmlNode processes = ProcessInfoToXml(_eventsDoc);
            processes.WriteTo(tw);

            XmlNode modules = ModulePathToXml(_eventsDoc);
            modules.WriteTo(tw);

            tw.WriteStartElement("events");

            _minimumProgressInvoker = (int)(_loader.MinimumProgress + (_loader.MaximumProgress - _loader.MinimumProgress) *
              0.10);
            _maximumProgressInvoker = _loader.MaximumProgress;

            var db = EventDatabaseMgr.GetInstance();

            var refreshData = new EventsReportData(TraceId) {ControlInvoker = _loader.ProgressDlg};
            refreshData.EventsReady += LoadXmlOnEventsInvokerReport;

            db.RefreshEvents(refreshData);
            
            db.WaitProcessEvents(refreshData);

            refreshData.EventsReady -= LoadXmlOnEventsInvokerReport;

            if (_loader.Canceled)
                return false;

            // events
            tw.WriteEndElement();

            // deviare-trace
            tw.WriteEndElement();

            return true;
        }

        private void LoadXmlOnEventsInvokerReport(object sender, EventsReadyArgs eventsReadyArgs)
        {
            foreach (var item in eventsReadyArgs.Events)
            {
                string lastEventLoaded = "";
                try
                {
                    XmlNode n = item.ToXml(_eventsDoc);
                    using (var stringWriter = new StringWriter())
                    using (var xmlTextWriter = new XmlTextWriter(stringWriter) {Formatting = Formatting.Indented})
                    {
                        n.WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        lastEventLoaded = stringWriter.GetStringBuilder().ToString();
                    }

                    n.WriteTo(_tw);
                    if (_loader.Canceled)
                    {
                        eventsReadyArgs.Canceled = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Error.WriteLine("Error parsing event: " + ex.Message + "\n" + lastEventLoaded);
                    Debug.Assert(false);
                }
            }
            _processedEvents += eventsReadyArgs.Events.Count;
            _loader.ReportProgress(_minimumProgressInvoker +
                                   (_processedEvents * (_maximumProgressInvoker - _minimumProgressInvoker) /
                                    eventsReadyArgs.TotalEventCount),
                                   _processedEvents, eventsReadyArgs.TotalEventCount);
        }

        public bool EventsToFile(string filename, uint traceId, int minimum, int maximum)
        {
            string infoXmlPath = FileSystemTools.GetTempFilename("info", "xml");

            using (
                var tw = new XmlTextWriter(infoXmlPath, Encoding.UTF8) {Formatting = Formatting.Indented})
            {
                var traceInfo = new XmlDocument();
                tw.WriteStartDocument();
                tw.WriteStartElement("deviare-trace");
                tw.WriteAttributeString("deviare-log", _loader.ProcMonLog ? "false" : "true");
                XmlNode processes = ProcessInfoToXml(traceInfo);
                processes.WriteTo(tw);

                XmlNode modules = ModulePathToXml(traceInfo);
                modules.WriteTo(tw);
            }

            var miscXmlPath = FileSystemTools.GetTempFilename("misc", "xml");

            using (var file = new FileStream(miscXmlPath, FileMode.Create, FileAccess.Write))
                MiscLogData.Save(file);

            minimum = minimum + (maximum - minimum)*10/100;
            _loader.ReportProgress(minimum);

            var databaseMgr = EventDatabaseMgr.GetInstance();
            try
            {
                _eventsMinimumProgress = minimum;
                _eventsMaximumProgress = (int) (minimum + (maximum - minimum) * 0.3);
                _stackMinimumProgress = _eventsMaximumProgress;
                _stackMaximumProgress = maximum;

                databaseMgr.LockDatabase(traceId);
                var dbInfo = databaseMgr.GetDatabasePath(traceId);
                using (var zip = new ZipFile())
                {
                    zip.CompressionLevel = CompressionLevel.BestSpeed;
                    zip.SaveProgress += ZipOnSaveProgress;
                    zip.AddFile(dbInfo.EventDbPath, "");
                    zip.AddFile(infoXmlPath, "").FileName = "info.xml";
                    zip.AddFile(dbInfo.StackDbPath, "");
                    zip.AddFile(miscXmlPath, "").FileName = "misc.xml";
                    zip.Save(filename);
                }
                _loader.ReportProgress(maximum);
            }
            catch (Exception ex)
            {
                Error.WriteLine("Error parsing event: " + ex.Message);
                databaseMgr.UnlockDatabase(traceId);
                return false;
            }
            try
            {
                File.Delete(infoXmlPath);
            }
            catch (Exception)
            {
            }
            databaseMgr.UnlockDatabase(traceId);
            return true;
        }

        private void ZipOnSaveProgress(object sender, SaveProgressEventArgs saveProgressEventArgs)
        {
            if (_loader.Canceled)
            {
                saveProgressEventArgs.Cancel = true;
                return;
            }

            if (saveProgressEventArgs.CurrentEntry != null && !saveProgressEventArgs.CurrentEntry.FileName.EndsWith("xml") && saveProgressEventArgs.TotalBytesToTransfer != 0)
            {
                if (saveProgressEventArgs.CurrentEntry.FileName.StartsWith("Stack"))
                {
                    _loader.ReportProgress(
                        (int)
                        (_stackMinimumProgress +
                         (_stackMaximumProgress - _stackMinimumProgress) * saveProgressEventArgs.BytesTransferred /
                         saveProgressEventArgs.TotalBytesToTransfer));
                }
                else
                {
                    _loader.ReportProgress(
                        (int)
                        (_eventsMinimumProgress +
                         (_eventsMaximumProgress - _eventsMinimumProgress) * saveProgressEventArgs.BytesTransferred /
                         saveProgressEventArgs.TotalBytesToTransfer));
                }
            }
        }
    }
}