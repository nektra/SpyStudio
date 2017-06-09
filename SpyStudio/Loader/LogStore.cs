using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SpyStudio.Database;
using SpyStudio.Dialogs;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Loader
{
    public class LogStore
    {
        private Form _parent;
        private FileStream _streamReader;
        XmlTextReader _traceInfo;
        private DialogResult _result;
        private int _progress;

        public LogStore()
        {
            Success = true;
            Error = "";
            MinimumProgress = 0;
            MaximumProgress = 100;
        }

        public string Filename { get; set; }
        public List<CallEvent> Events { get; set; }
        public static LogStore CreateLoadLogStore(ProgressDialog progressDialog, string filename)
        {
            var store = new LogStore
            {
                ProgressDlg = progressDialog,
                Filename = filename,
                Success = true,
                Error = "",
                _parent = null,
                MinimumProgress = 0,
                MaximumProgress = 100
            };
            return store;
        }
        public static LogStore CreateLoadLogStore(Form parent, string filename)
        {
            var store = new LogStore
                            {
                                ProgressDlg = null,
                                Filename = filename,
                                Success = true,
                                Error = "",
                                _parent = parent,
                                MinimumProgress = 0,
                                MaximumProgress = 100
                            };
            return store;
        }
        public static LogStore CreateSaveLogStore(Form parent, string filename, bool procMonLog)
        {
            var store = new LogStore
            {
                ProgressDlg = null,
                _parent = parent,
                Filename = filename,
                Success = true,
                Error = "",
                ProcMonLog = procMonLog,
                MinimumProgress = 0,
                MaximumProgress = 100
            };
            return store;
        }
        /// <summary>
        /// Create UI less Log Store to save log
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="procMonLog"></param>
        /// <returns></returns>
        public static LogStore CreateSaveLogStore(string filename, bool procMonLog)
        {
            var store = new LogStore
                            {
                                ProgressDlg = null,
                                _parent = null,
                                Filename = filename,
                                Success = true,
                                Error = "",
                                ProcMonLog = procMonLog,
                                MinimumProgress = 0,
                                MaximumProgress = 100
                            };
            return store;
        }
        public DialogResult Load()
        {
            //DialogResult result;
            if(ProgressDlg == null)
            {
                ProgressDlg = new ProgressDialog { Title = "SpyStudio Trace Log", Message = "Loading " + Path.GetFileName(Filename) + " ..." };
                _parent.BeginInvoke(new MethodInvoker(LoadSync));
                ProgressDlg.ShowDialog(_parent);
            }
            else
            {
                LoadSync();
            }

            return _result;
        }

        public event Action LoadProcessStarted;
        public event Action LoadProcessFinished;

        protected virtual void OnLoadProcessStarted()
        {
            if (LoadProcessStarted != null)
                LoadProcessStarted();
        }

        protected virtual void OnLoadProcessFinished()
        {
            if (LoadProcessFinished != null)
                LoadProcessFinished();
        }

        void LoadSync()
        {
            OnLoadProcessStarted();
            IsLoading = true;
            LoadLogThread();
            IsLoading = false;
            OnLoadProcessFinished();
            //ProgressDlg.Close();
        }

        public bool IsLoading { get; set; }

        public DialogResult Save()
        {
            DialogResult result;

            if (ProgressDlg == null && _parent != null)
            {
                ProgressDlg = new ProgressDialog
                                  {
                                      Title = "SpyStudio Trace Log",
                                      Message = "Saving " + Path.GetFileName(Filename) + " ..."
                                  };
                _parent.BeginInvoke(new MethodInvoker(() => SaveSync()));

                result = ProgressDlg.ShowDialog(_parent);
            }
            else
            {
                result = SaveSync();
            }

            return result;
        }

        DialogResult SaveSync()
        {
            Error = "";
            _result = DialogResult.OK;

            var devStore = new DeviareLogStore(this)
                               {
                                   XmlMode = XmlMode,
                                   TraceId = TraceId,
                                   MiscLogData = MiscLogData,
                               };
            Success = devStore.SaveLog(TraceId);
            
            if (!Success)
            {
                Success = false;
                if (Canceled)
                {
                    _result = DialogResult.Cancel;
                    return DialogResult.Cancel;
                }
                _result = DialogResult.Abort;
                if (String.IsNullOrEmpty(Error))
                    ReportError("Unknown error");
            }
            return _result;
        }
        
        public void ReportError(string error)
        {
            Error = error;
            if(ProgressDlg != null)
            {
                ProgressDlg.DialogResult = DialogResult.Abort;
                ProgressDlg.Close();
            }

            _result = DialogResult.Abort;
        }
        public long TotalSize
        {
            get { return _streamReader.Length; }
        }
        public long Position
        {
            get { return _streamReader.Position; }
        }
        public int GetNodeCount(XmlTextReader reader, string tag, int depth, int progress, out bool canceled)
        {
            return GetNodeCount(reader, tag, XmlNodeType.None, depth, progress, out canceled);
        }

        public int GetNodeCount(XmlTextReader reader, string tag, XmlNodeType nodeType, int depth, int progress, out bool canceled)
        {
            var count = 0;
            canceled = false;
            while (!reader.EOF)
            {
                if (reader.Name == tag && reader.Depth == depth && (nodeType == reader.NodeType || nodeType == XmlNodeType.None))
                    count++;
                reader.Read();
                ReportProgress(progress);
                if (Canceled)
                {
                    canceled = true;
                    break;
                }
            }
            return count;
        }

        public bool Success { get; private set; }
        public string Error { get; set; }
        public ProgressDialog ProgressDlg { get; set; }
        public int MinimumProgress { get; set; }
        public int MaximumProgress { get; set; }
        public ProcessInfo ProcessInfo { get; set; }
        public ModulePath ModulePath { get; set; }
        public bool ProcMonLog { get; set; }
        public EventFilter.Filter LoadFilter { get; set; }
        public double StartTime { get; set; }
        public uint TraceId { get; set; }
        public bool XmlMode { get; set; }
        public bool Win32Function { get; set; }
        public bool StackTraceString { get; set; }
        public bool LoadStackDb { get; set; }
        public bool RefreshEvents { get; set; }

        public event EventHandler Begin;
        public event EventHandler End;
        //public event CallEventHandler NewEvent;
        public event EventsReadyEventHandler EventsReady;
        public MiscLogData MiscLogData;

        //delegate DialogResult StoreSyncDelegate();
        public bool IsFilteredByLoad(CallEvent callEvent)
        {
            var filtered = false;
            if (LoadFilter != null)
            {
                filtered = LoadFilter.IsFiltered(callEvent);
            }
            return filtered;
        }

        public bool IsProcessNameFiltered(string procName)
        {
            if (LoadFilter != null && LoadFilter.IsProcessNameFiltered(procName))
                return true;
            return false;
        }
        public void OperationBegin()
        {
            if (Begin != null)
                Begin(this, new EventArgs());
        }
        public void OperationEnd()
        {
            if (End != null)
                End(this, new EventArgs());
        }
        //public void NewEventProcessed(CallEvent e)
        //{
        //    if (NewEvent != null)
        //        NewEvent(this, new CallEventArgs(e));
        //}
        public void NewEventsReady(EventsReadyArgs eventsReadyArgs)
        {
            if (EventsReady != null)
                EventsReady(this, eventsReadyArgs);
        }
        public bool RestartXml(ref XmlTextReader traceInfo)
        {
            try
            {
                if (traceInfo != null && traceInfo.ReadState != ReadState.Closed)
                    traceInfo.Close();

                _streamReader = File.OpenRead(Filename);
                traceInfo = new XmlTextReader(_streamReader);
            }
            catch (Exception ex)
            {
                ReportError("Error loading xml: " + ex.Message);
                return false;
            }
            return true;
        }
        void LoadLogThread()
        {
            Error = "";

            ReportProgress(MinimumProgress);

            try
            {
                if(Filename.ToLower().EndsWith("xml"))
                {
                    if (!RestartXml(ref _traceInfo))
                        throw new Exception("Could not restart XML.");

                    // assume 10% to load file
                    // assume 10% to load modules

                    ReportProgress(MinimumProgress + (MaximumProgress - MinimumProgress) * 10 / 100);

                    _traceInfo.MoveToStartElement();
                    switch (_traceInfo.Name)
                    {
                        case "deviare-trace":
                            {
                                var loader = new DeviareLogStore(this, _traceInfo)
                                                 {
                                                     XmlMode = true,
                                                     TraceId = TraceId,
                                                     Win32Function = Win32Function,
                                                     StackTraceString = StackTraceString,
                                                     LoadStackDb = LoadStackDb,
                                                     RefreshEvents = RefreshEvents
                                                 };
                                Success = loader.LoadLog();
                                MiscLogData = loader.MiscLogData;
                                ProcMonLog = false;
                            }
                            break;
                        case "procmon":
                            {
                                var loader = new ProcMonLogStore(this, _traceInfo) { TraceId = TraceId};
                                Success = loader.LoadLog();
                                ProcMonLog = true;
                            }
                            break;
                        default:
                            Error = "Cannot identify xml file " + Filename;
                            break;
                    }
                }
                else
                {
                    var loader = new DeviareLogStore(this, Filename)
                                     {
                                         XmlMode = false,
                                         TraceId = TraceId,
                                         LoadStackDb = LoadStackDb,
                                         RefreshEvents = RefreshEvents
                                     };
                    Success = loader.LoadLog();
                    MiscLogData = loader.MiscLogData;
                    ProcMonLog = false;
                }
                if (Canceled)
                    _result = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                ReportError("Error in xml file: " + ex.Message + " before LineNumber: " + _traceInfo.LineNumber +
                            " Position: " + _traceInfo.LinePosition);
                _result = DialogResult.Abort;
            }
        }

        public int CurrentProgress()
        {
            return _progress;
        }
        public void ReportProgress(int progress)
        {
            _progress = progress;
            if(ProgressDlg != null)
            {
                ProgressDlg.Progress = progress;
                if (progress == 100)
                {
                    ProgressDlg.DialogResult = DialogResult.OK;
                    ProgressDlg.Close();
                }
                Application.DoEvents();
            }
        }
        public void Close()
        {
            if(ProgressDlg != null)
            {
                ProgressDlg.DialogResult = DialogResult.OK;
                ProgressDlg.Close();
                Application.DoEvents();
            }
        }

        public void ReportProgress(int progress, int currentItem, int totalItems)
        {
            if (ProgressDlg != null)
            {
                ProgressDlg.SetDetailedProgress(progress, currentItem, totalItems);
                if (progress == 100)
                {
                    ProgressDlg.DialogResult = DialogResult.OK;
                    ProgressDlg.Close();
                }
                Application.DoEvents();
            }
        }
        public bool Canceled
        {
            get { return (ProgressDlg != null && ProgressDlg.Cancelled()); }
        }
    }
}