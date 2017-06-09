using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Aga.Controls;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.COM.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Forms;
using SpyStudio.Hooks;
using SpyStudio.Main;
using SpyStudio.Properties;
using SpyStudio.Registry.Filters;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.Windows.Controls;

namespace SpyStudio.Dialogs.Compare
{
    public partial class FormDeviareCompare : ICompareInterpreterController
    {
        public class ToolTipProviderGeneric : IToolTipProvider
        {
            private readonly Func<DeviareTraceCompareItem, string> _function;

            public ToolTipProviderGeneric(Func<DeviareTraceCompareItem, string> f)
            {
                _function = f;
            }

            public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
            {
                var item = node.Node as DeviareTraceCompareItem;

                if (item == null)
                    return null;

                var ret = "";
                Debug.Assert(_function(item) != null);
                var lines = StringTools.GetFirstLines(_function(item), 6);

                if (!item.ResultsMatch)
                {
                    ret += "Mismatch: " + StringTools.GetFirstLines(item.ResultMismatchString, 6);
                    if (!string.IsNullOrEmpty(lines))
                        ret += "\n";
                }

                ret += lines;
                return ret;
            }
        }

        private const int MaxEventsFreeVersion = 2000;

        private int _totalEvents, _filteredEvents;
        private DeviareRunTrace _trace1, _trace2;
        private EventManager _eventMgr;
        private XmlDocument _traceInfo, _procMonInfo;
        private TreeModel _model;
        //readonly Dictionary<int, HashSet<string>> _priorityFunctions = new Dictionary<int, HashSet<string>>();
        private int _maxPriority;
        public Dictionary<string, FunctionInfo> FunctionInfoDict;

        private readonly Dictionary<string, FunctionInfo> _functionInfoDictDeviare =
            new Dictionary<string, FunctionInfo>();

        private readonly Dictionary<string, FunctionInfo> _functionInfoDictProcMon =
            new Dictionary<string, FunctionInfo>();

        //private readonly Dictionary<TraceEvent, DeviareTraceCompareItem> _traceItem = new Dictionary<TraceEvent, DeviareTraceCompareItem>();
        private TextSearch _searchDlg = new TextSearch();
        private readonly EventFilter.Filter _filter;

        private readonly SortedDictionary<UInt64, CallEvent> _cleanedEvents1 = new SortedDictionary<ulong, CallEvent>(),
                                         _cleanedEvents2 = new SortedDictionary<ulong, CallEvent>();

        //private EventFilter.Filter _loadFilter;
        private EventFilter _eventFilter;

        public FormDeviareCompare()
        {
            InitializeComponent();

            _listViewCom.Controller = this;
            _fileSystemViewer.Controller = this;
            _listViewValues.Controller = this;
            _listViewWindow.Controller = this;
            _treeViewCompare.Controller = this;
            _treeViewRegistry.Controller = this;


            KeyPreview = true;

            _fileSystemViewer.TreeMode = false;
            _fileSystemViewer.CompareMode = true;
            _treeViewRegistry.PathFilters.Add(new RegInfoMergeWowFilter());
            _treeViewRegistry.PathFilters.Add(new RegInfoRedirectClassesFilter());
            _fileSystemViewer.MergeLayerPaths = Settings.Default.FileSystemMergeLayerPathsCompare;
            _fileSystemViewer.MergeWowPaths = Settings.Default.FileSystemMergeWowPathsCompare;
            _fileSystemViewer.ListView.SetDetailsListView(_listViewFileDetails);
            _listViewFileDetails.Controller = this;

            _listViewCom.InitializeComponent();
            _listViewWindow.InitializeComponent();
            //_listViewFileSystem.InitializeComponent();

            ValuesEntryProperties = new EntryContextMenu(_listViewValues);
            FileSystemDetailsEntryProperties = new EntryContextMenu(_listViewFileDetails);

            _treeViewRegistry.ValuesView = _listViewValues;
            _fileSystemViewer.File1BackgroundColor = EntryColors.File1Color;
            _fileSystemViewer.File2BackgroundColor = EntryColors.File2Color;

            _fileSystemViewer.MergeLayerPaths = Settings.Default.FileSystemMergeLayerPathsCompare;
            _fileSystemViewer.MergeWowPaths = Settings.Default.FileSystemMergeWowPathsCompare;
            _fileSystemViewer.ShowStartupModules = Settings.Default.FileSystemShowStartupModulesCompare;
            _treeViewRegistry.MergeLayerPaths = Settings.Default.FileSystemMergeLayerPathsCompare;
            _fileSystemViewer.HideQueryAttributes = Settings.Default.FileSystemHideQueryAttributesCompare;
            toolStripMenuItemHideAttributes.Checked = _fileSystemViewer.HideQueryAttributes;
            showLayerPathsToolStripMenuItem.Checked = !_fileSystemViewer.MergeLayerPaths;
            mergeWowPathsToolStripMenuItem.Checked = _fileSystemViewer.MergeWowPaths;
            showStartupModulesToolStripMenuItem.Checked = _fileSystemViewer.ShowStartupModules;

            columnHeaderCallerNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.Caller);
            columnHeaderStackFrameNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.StackFrame);
            columnHeaderFunctionNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.Function);
            columnHeaderProcessNameNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.ProcessName);
            columnHeaderResultNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.Result);
            columnHeaderPathNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.CompleteParamMain);
            columnHeaderDetailsNode.ToolTipProvider = new ToolTipProviderGeneric(x => x.CompleteDetails);
            _treeViewCompare.ShowNodeToolTips = true;
            TraceEntryProperties = new TraceEntryContextMenu(_treeViewCompare, this);
            _treeViewCompare.ContextMenuController = TraceEntryProperties;

            _filter = EventFilter.Filter.GetCompareWindowFilter();
            _filter.Change += FilterChange;
            Closing += FormDeviareCompareClosing;

            _toBeDisposed = new List<IDisposable>
                                {
                                    _listViewCom,
                                    _fileSystemViewer,
                                    _listViewFileDetails,
                                    _listViewValues,
                                    _listViewWindow
                                };

            _treeViewRegistry.CheckBoxes = false;
            _treeViewRegistry.RecursiveCheck = true;
            _treeViewCompare.SetEventSummary(eventSummaryGraphic1);
        }

        private void FormDeviareCompareClosing(object sender, CancelEventArgs e)
        {
            _trace1.EventAdd -= Trace1OnEventAdd;
            _trace2.EventAdd -= Trace2OnEventAdd;

            _trace2.Clear();
            _trace1.DetachFromDatabase();
            _trace1 = _trace2 = null;
            _filter.Change -= FilterChange;
            //ClearData();
        }

        private void FilterChange(object sender, EventArgs e)
        {
            RefreshEvents();
        }

        private void RefreshEvents()
        {
            bool success;
            string error;

            ClearData();

            if (LoadTrace(out success, out error) == DialogResult.Abort)
            {
                MessageBox.Show(this, "Error refreshing events: " + error,
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SetTraces(DeviareRunTrace trace1, DeviareRunTrace trace2)
        {
            _trace1 = trace1;
            _trace2 = trace2;

            _model = new TreeModel();
            _treeViewCompare.Model = _model;
            _treeViewCompare.Trace1Id = _trace1.TraceId;
            _treeViewCompare.Trace2Id = _trace2.TraceId;
        }

        public DialogResult LoadTrace(out bool success, out string error)
        {
            return LoadTrace(out success, out error, "", "");
        }

        public delegate void LoadTraceSyncDelegate(ProgressReporter worker,
                                                   int minimum, int maximum);


        public DialogResult LoadTrace(out bool success, out string error, string filename1, string filename2)
        {
            //BackgroundWorker worker = new BackgroundWorker();
            var dlg = new ProgressDialog();
            var workerParams = new ProgressReporter {Filename1 = filename1, Filename2 = filename2};

            //dlg.SetWorker(worker);
            dlg.Title = "Trace Compare";
            dlg.Message = "Comparing Traces ...";

            //worker.DoWork += new DoWorkEventHandler(LoadTraceThread);
            //worker.RunWorkerAsync(workerParams);
            //worker.WorkerSupportsCancellation = true;

            workerParams.ProgressDlg = dlg;
            dlg.Shown += DlgOnShown;
            dlg.Tag = workerParams;
            var result = dlg.ShowDialog();

            _statusFilteredEventsLabel.Text = String.Format("Filtered Events: {0:N0}", _filteredEvents);
            _statusTotalEventsLabel.Text = String.Format("Total Events: {0:N0}", _totalEvents);

            success = workerParams.Success;
            error = workerParams.Error;

            if (result == DialogResult.Cancel)
                Dispose();

            return result;
        }

        private void DlgOnShown(object sender, EventArgs eventArgs)
        {
            var progressDlg = (ProgressDialog) sender;

            progressDlg.Shown -= DlgOnShown;

            progressDlg.BeginInvoke(new LoadTraceSyncDelegate(LoadTraceSync), (ProgressReporter) progressDlg.Tag, 0, 100);
        }

        public void InitializePriorityArrays()
        {
            var xpath = "/hooks/hook";
            var hookFunctionList = _traceInfo.SelectNodes(xpath);

            Debug.Assert(hookFunctionList != null, "hookFunctionList != null");
            foreach (XmlNode f in hookFunctionList)
            {
                Debug.Assert(f.Attributes != null, "f.ReadAttributes != null");
                var functionInfo = new FunctionInfo
                                       {
                                           Priority =
                                               f.Attributes["priority"] == null
                                                   ? 5
                                                   : Convert.ToInt32(f.Attributes["priority"].InnerText,
                                                                     CultureInfo.InvariantCulture)
                                       };

                XmlNode colorNode = f.Attributes["color"];
                if (colorNode != null)
                    functionInfo.Color = Color.FromName(colorNode.InnerText);
                var fnc = f.SelectSingleNode("function");
                Debug.Assert(fnc != null);
                Debug.Assert(fnc.Attributes != null, "fnc.ReadAttributes != null");
                var function = fnc.Attributes["displayName"].Value;

                functionInfo.MatchFunctionResult = true;
                Debug.Assert(f.Attributes != null, "f.ReadAttributes != null");
                var a = f.Attributes["matchFunctionResult"];
                if (a != null && a.InnerText.ToLower() == "false")
                    functionInfo.MatchFunctionResult = false;

                var paramList = f.SelectNodes("param");
                Debug.Assert(paramList != null, "paramList!= null");
                var index = 0;
                foreach (XmlNode p in paramList)
                {
                    var m = p.SelectSingleNode("match");
                    if (m != null)
                    {
                        var match = new MatchInfo {Index = index, IsCaseSensitive = false, IsResult = false};

                        Debug.Assert(m.Attributes != null, "m.ReadAttributes != null");
                        a = m.Attributes["case"];
                        if (a != null && a.InnerText.ToLower() == "true")
                            match.IsCaseSensitive = true;
                        a = m.Attributes["result"];
                        if (a != null && a.InnerText.ToLower() == "true")
                            match.IsResult = true;
                        a = m.Attributes["onlyFilename"];
                        if (a != null && a.InnerText.ToLower() == "true")
                            match.OnlyFilename = true;
                        functionInfo.MatchInfos.Add(match);
                    }

                    index++;
                }
                if (functionInfo.Priority > _maxPriority)
                    _maxPriority = functionInfo.Priority;

                _functionInfoDictDeviare[function] = functionInfo;
            }

            // initialize ProcMon function array
            xpath = "/operations/operation[@priority]";
            hookFunctionList = _procMonInfo.SelectNodes(xpath);

            Debug.Assert(hookFunctionList != null, "hookFunctionList != null");
            foreach (XmlNode f in hookFunctionList)
            {
                Debug.Assert(f.Attributes != null, "f.ReadAttributes != null");
                var functionInfo = new FunctionInfo
                                       {
                                           Priority =
                                               Convert.ToInt32(f.Attributes["priority"].InnerText,
                                                               CultureInfo.InvariantCulture)
                                       };

                XmlNode colorNode = f.Attributes["color"];
                if (colorNode != null)
                    functionInfo.Color = Color.FromName(colorNode.InnerText);
                var function = f.Attributes["name"].InnerText;

                functionInfo.MatchFunctionResult = true;
                var a = f.Attributes["matchFunctionResult"];
                if (a != null && a.InnerText.ToLower() == "false")
                    functionInfo.MatchFunctionResult = false;

                var matchList = f.SelectNodes("match");
                Debug.Assert(matchList != null, "matchList != null");
                foreach (XmlNode m in matchList)
                {
                    Debug.Assert(m.Attributes != null, "m.ReadAttributes != null");
                    var match = new MatchInfo
                                    {
                                        Index = Convert.ToInt32(m.Attributes["index"].Value, CultureInfo.InvariantCulture),
                                        IsCaseSensitive = false,
                                        IsResult = false
                                    };

                    Debug.Assert(m.Attributes != null, "m.ReadAttributes != null");
                    a = m.Attributes["case"];
                    if (a != null && a.InnerText.ToLower() == "true")
                        match.IsCaseSensitive = true;
                    a = m.Attributes["result"];
                    if (a != null && a.InnerText.ToLower() == "true")
                        match.IsResult = true;
                    a = m.Attributes["onlyFilename"];
                    if (a != null && a.InnerText.ToLower() == "true")
                        match.OnlyFilename = true;
                    functionInfo.MatchInfos.Add(match);
                }
                if (functionInfo.Priority > _maxPriority)
                    _maxPriority = functionInfo.Priority;

                _functionInfoDictProcMon[function] = functionInfo;
            }
        }

        private void UpdateTabs()
        {
            if (_trace1.HasComEvents() || _trace2.HasComEvents())
                ShowTab(tabCom);
            else if (tabControlData.TabPages.Contains(tabCom))
                tabControlData.TabPages.Remove(tabCom);
            if (_trace1.HasWindowEvents() || _trace2.HasWindowEvents())
                ShowTab(tabWindow);
            else if (tabControlData.TabPages.Contains(tabWindow))
                tabControlData.TabPages.Remove(tabWindow);
            if (_trace1.HasFileSystemEvents() || _trace2.HasFileSystemEvents())
                ShowTab(tabFile);
            else if (tabControlData.TabPages.Contains(tabFile))
                tabControlData.TabPages.Remove(tabFile);
            if (_trace1.HasRegistryEvents() || _trace2.HasRegistryEvents())
                ShowTab(tabRegistry);
            else if (tabControlData.TabPages.Contains(tabRegistry))
                tabControlData.TabPages.Remove(tabRegistry);
        }

        private void ShowTab(TabPage page)
        {
            if (tabControlData.InvokeRequired)
            {
                tabControlData.BeginInvoke(new CallDump.ShowTabDelegate(ShowTab), page);
            }
            else
            {
                if (!tabControlData.TabPages.Contains(page))
                {
                    var index = 0;

                    if ((page == tabWindow || page == tabFile || page == tabRegistry) &&
                        tabControlData.TabPages.Contains(tabCom))
                        index++;
                    if ((page == tabFile || page == tabRegistry) && tabControlData.TabPages.Contains(tabWindow))
                        index++;
                    if ((page == tabRegistry) && tabControlData.TabPages.Contains(tabFile))
                        index++;

                    tabControlData.TabPages.Insert(index, page);
                }
            }
        }

        public void DisplayTraceTab()
        {
            /*
             * Watch out! This assumes the Trace tab is always the last one.
             * -Victor
             */
            tabControlData.SelectedIndex = tabControlData.TabCount - 1;
        }

        public void SelectItemWithCallEventId(CallEventId aCallEventId)
        {
            //Find node by matching id:
            RecursiveNodeSearch<DeviareTraceCompareItem>.SearchTerm f = ci => ci.MainCallEventIds.Any(id => id.TraceId == aCallEventId.TraceId && id.CallNumber == aCallEventId.CallNumber);
            var child = RecursiveNodeSearch<DeviareTraceCompareItem>.FindNode(f, _treeViewCompare.Model.Root);

            Debug.Assert(child != null);
            _treeViewCompare.SelectedNode = child;
            _treeViewCompare.EnsureVisible(child, TreeViewAdv.ScrollType.Middle);
        }

        private void ClearData()
        {
            if (_treeViewCompare.InvokeRequired)
            {
                _treeViewCompare.Invoke(new MethodInvoker(ClearData));
            }
            else
            {
                if (TraceEntryProperties != null)
                    TraceEntryProperties.Close(false);
                if (ValuesEntryProperties != null)
                    ValuesEntryProperties.Close(false);
                if (FileSystemDetailsEntryProperties != null)
                    FileSystemDetailsEntryProperties.Close(false);

                _treeViewCompare.BeginUpdate();
                _listViewCom.BeginUpdate();
                _fileSystemViewer.BeginUpdate();
                _listViewWindow.BeginUpdate();
                _treeViewRegistry.BeginUpdate();

                _model.Nodes.Clear();
                _treeViewCompare.ClearData();
                _insertedItems.Clear();
                _listViewCom.ClearData();
                _listViewWindow.ClearData();
                _fileSystemViewer.ClearData();
                _treeViewRegistry.ClearData();

                _cleanedEvents1.Clear();
                _cleanedEvents2.Clear();

                //_traceItem.Clear();

                _eventMgr = null;
                _listViewWindow.EndUpdate();
                _fileSystemViewer.EndUpdate();
                _listViewCom.EndUpdate();
                _treeViewCompare.EndUpdate();
                _treeViewRegistry.EndUpdate();
                //ResetTabs();

                GCTools.AsyncCollectDelayed(10000);
            }
        }

        public void LoadTraceSync(ProgressReporter progressReporter,
                                  int minimum, int maximum)
        {
            progressReporter.Success = false;
            progressReporter.Error = "";

            if (_traceInfo == null)
            {
                _traceInfo = new XmlDocument();
                try
                {
                    _traceInfo.LoadXml(Resources.Hooks);
                }
                catch (Exception ex)
                {
                    progressReporter.Error = "Error loading information xml: " + ex.Message;
                    progressReporter.Finish(DialogResult.Abort);
                    return;
                }
            }
            if (_procMonInfo == null)
            {
                _procMonInfo = new XmlDocument();
                try
                {
                    _procMonInfo.LoadXml(Resources.ProcMon);
                }
                catch (Exception ex)
                {
                    progressReporter.Error = "Error loading information xml: " + ex.Message;
                    progressReporter.Finish(DialogResult.Abort);
                    return;
                }
            }

            _filteredEvents = _totalEvents = 0;

            if (_trace1 == null && _trace2 == null)
            {
                //ResetTabs();

                progressReporter.ProgressDlg.Progress = 1;

                _trace1 = new DeviareRunTrace(new ProcessInfo(), new ModulePath());
                _trace2 = new DeviareRunTrace(new ProcessInfo(), new ModulePath());

                // WORKAROUND: here we can collect the CallEvent objects and store them saving space
                _trace1.EventAdd += Trace1OnEventAdd;
                _trace2.EventAdd += Trace2OnEventAdd;

                progressReporter.ProgressDlg.Progress = 2;

                _trace1.SetFilter(_filter, false);
                _trace2.SetFilter(_filter, false);

                _treeViewRegistry.ValuesView.File1TraceId = _trace1.TraceId;
                _treeViewRegistry.ValuesView.File2TraceId = _trace2.TraceId;
                _treeViewRegistry.File1TraceId = _trace1.TraceId;
                _treeViewRegistry.File2TraceId = _trace2.TraceId;
                _fileSystemViewer.File1TraceId = _trace1.TraceId;
                _fileSystemViewer.File2TraceId = _trace2.TraceId;

                progressReporter.ProgressDlg.Message = "Loading " + Path.GetFileName(progressReporter.Filename1);
                //_trace1.SetLoadFilter(_loadFilter);
                if (
                    _trace1.LoadLogSync(progressReporter.Filename1, out progressReporter.Success, out progressReporter.Error,
                                        progressReporter.ProgressDlg, 2, 35) == DialogResult.Abort)
                    return;
                if (progressReporter.CancellationPending)
                    return;
                progressReporter.ProgressDlg.Message = "Loading " + Path.GetFileName(progressReporter.Filename2);
                //_trace2.SetLoadFilter(_loadFilter);
                if (
                    _trace2.LoadLogSync(progressReporter.Filename2, out progressReporter.Success, out progressReporter.Error,
                                        progressReporter.ProgressDlg, 35, 70) == DialogResult.Abort)
                    return;

                if (progressReporter.CancellationPending)
                    return;
                SetTraces(_trace1, _trace2);

                labelLeftColor.BackColor = EntryColors.File1Color;
                labelLeftFile.Text = Path.GetFileName(progressReporter.Filename1);
                labelRightColor.BackColor = EntryColors.File2Color;
                labelRightFile.Text = Path.GetFileName(progressReporter.Filename2);

                InitializePriorityArrays();
            }
            else
            {
                ClearData();

                Debug.Assert(_trace1 != null, "_trace1 != null");
                progressReporter.ProgressDlg.Message = "Applying filter in trace 1";
                _trace1.RefreshControls(progressReporter.ProgressDlg, 0, 35);
                progressReporter.ProgressDlg.Message = "Applying filter in trace 2";
                _trace2.RefreshControls(progressReporter.ProgressDlg, 35, 70);
                progressReporter.ReportProgress(70);
            }

            var cancelled = false;

            try
            {
                UpdateTabs();

                if (_trace1.ProcMonLog != _trace2.ProcMonLog)
                {
                    progressReporter.Error =
                        "You are trying to compare a log took from SpyStudio with another log took from a different application. Both logs should be of the same source.";
                    progressReporter.Finish(DialogResult.Abort);
                    return;
                }
                FunctionInfoDict = _trace1.ProcMonLog ? _functionInfoDictProcMon : _functionInfoDictDeviare;

                _eventMgr = new EventManager();

                _eventMgr.Trace1EventAdded += _treeViewCompare.LoadStrategy.OnTrace1EventAdded;
                _eventMgr.Trace2EventAdded += _treeViewCompare.LoadStrategy.OnTrace2EventAdded;
                _treeViewCompare.LoadStrategy.OnEventProcessed += AddEventToControls;

#if DEBUG
                var timer = new Stopwatch();
                timer.Start();
#endif
                progressReporter.ProgressDlg.Message = "Processing trace 1";

                _eventMgr.ProcessTrace1(_allEvents1, _cleanedEvents1.Values.ToArray(), progressReporter, 70, 75, ref cancelled);
                if (cancelled)
                    return;

                progressReporter.ProgressDlg.Message = "Processing trace 2";

                _eventMgr.ProcessTrace2(_allEvents2, _cleanedEvents2.Values.ToArray(), progressReporter, 75, 80, ref cancelled);
                if (cancelled)
                    return;

#if DEBUG
                double timeProcess = timer.Elapsed.TotalMilliseconds;
                var previous = timer.Elapsed.TotalMilliseconds;
#endif
                //_filteredEvents = _cleanedEvents1.Count(e => !e.Value.IsGenerated) +
                //                  _cleanedEvents2.Count(e => !e.Value.IsGenerated);
                _filteredEvents = _cleanedEvents1.Count +
                                  _cleanedEvents2.Count;

                progressReporter.ProgressDlg.Message = "Calculating Sync Points";

                _treeViewCompare.BeginUpdate();
                _treeViewRegistry.BeginUpdate();
                _listViewCom.BeginUpdate();
                _fileSystemViewer.BeginUpdate();
                _listViewWindow.BeginUpdate();

                progressReporter.ProgressDlg.Message = "Inserting events";

                _eventMgr.InsertEvents(progressReporter, 80, 90, ref cancelled);

                if (cancelled)
                    return;

#if DEBUG
                double timeInsert = timer.Elapsed.TotalMilliseconds - previous;
                previous = timer.Elapsed.TotalMilliseconds;
#endif

                _treeViewCompare.LoadStrategy.Perform(progressReporter, 90, 100, ref cancelled);

                _treeViewCompare.EndUpdate();
                _treeViewRegistry.EndUpdate();
                _listViewCom.EndUpdate();
                _fileSystemViewer.EndUpdate();
                _listViewWindow.EndUpdate();

                Debug.Assert(!_treeViewCompare.IsUpdating);
                Debug.Assert(!_treeViewRegistry.IsUpdating);

                if(!cancelled)
                    progressReporter.ReportProgress(100);

                _eventMgr.Trace1EventAdded -= _treeViewCompare.LoadStrategy.OnTrace1EventAdded;
                _eventMgr.Trace2EventAdded -= _treeViewCompare.LoadStrategy.OnTrace2EventAdded;
                _treeViewCompare.LoadStrategy.OnEventProcessed -= AddEventToControls;

#if DEBUG
                double timePerform = timer.Elapsed.TotalMilliseconds - previous;

                Debug.WriteLine("\nCompare Times:" +
                                "\nTime Process:\t" + timeProcess +
                                "\nTime Insert:\t" + timeInsert +
                                "\nTime Perform:\t" + timePerform +
                                "\nTotal compare load time:\t" + timer.Elapsed.TotalMilliseconds);
#endif
                if (cancelled)
                    return;

            }
            catch (Exception ex)
            {
                progressReporter.Error = ex.Message;
                progressReporter.Finish(DialogResult.Abort);
            }
        }

        private void Trace1OnEventAdd(object sender, CallEventArgs callEventArgs)
        {
            _allEvents1[callEventArgs.Event.CallNumber] = callEventArgs.Event;

            //if (!callEventArgs.Event.IsGenerated)
                _totalEvents++;
            if (!callEventArgs.Filtered)
            {
                //if (!callEventArgs.Event.IsGenerated)
                //{
                    _filteredEvents++;
                    _cleanedEvents1[callEventArgs.Event.CallNumber] = callEventArgs.Event;
                //}
            }
            else
            {
                CallEvent callEvent;
                if(_cleanedEvents1.TryGetValue(callEventArgs.Event.Peer, out callEvent))
                {
                    _cleanedEvents1.Remove(callEventArgs.Event.Peer);
                }
            }
        }

        private void Trace2OnEventAdd(object sender, CallEventArgs callEventArgs)
        {
            _allEvents2[callEventArgs.Event.CallNumber] = callEventArgs.Event;

            //if (!callEventArgs.Event.IsGenerated)
                _totalEvents++;
            if (!callEventArgs.Filtered)
            {
                //if (!callEventArgs.Event.IsGenerated)
                //{
                    _filteredEvents++;
                    _cleanedEvents2[callEventArgs.Event.CallNumber] = callEventArgs.Event;
                //}
            }
            else
            {
                CallEvent callEvent;
                if (_cleanedEvents2.TryGetValue(callEventArgs.Event.Peer, out callEvent))
                {
                    _cleanedEvents2.Remove(callEventArgs.Event.Peer);
                }
            }
        }

        private readonly Dictionary<EventInfo, DeviareTraceCompareItem> _insertedItems =
            new Dictionary<EventInfo, DeviareTraceCompareItem>();

        private readonly Dictionary<ulong, CallEvent> _allEvents1 = new Dictionary<ulong, CallEvent>();
        private readonly Dictionary<ulong, CallEvent> _allEvents2 = new Dictionary<ulong, CallEvent>();

        private void AddEventToControls(CallEvent aCallEvent, DeviareTraceCompareItem aCompareItem)
        {
            if (aCallEvent.Before)
                return;

            switch (aCallEvent.Type)
            {
                case HookType.Custom:
                    switch (aCallEvent.Function)
                    {
                        case "RpcRT4.NdrDllGetClassObject":
                        case "Ole32.CoGetClassObject":
                            var comObjectInfo = ComObjectInfo.From(aCallEvent);
                            comObjectInfo.CompareItems.Add(aCompareItem);
                            _listViewCom.Add(comObjectInfo);
                            break;
                        case "LoadResource":
                            _fileSystemViewer.AddEvent(aCallEvent, aCompareItem);
                            break;
                    }
                    break;
                case HookType.GetClassObject:
                case HookType.CoCreate:
                    var comInfo = ComObjectInfo.From(aCallEvent);
                    comInfo.CompareItems.Add(aCompareItem);
                    _listViewCom.Add(comInfo);
                    break;
                case HookType.CreateWindow:
                case HookType.CreateDialog:
                    var modulePath = aCallEvent.TraceId == Trace1ID ? _trace1.ModulePath : _trace2.ModulePath;
                    var windowInfo = WindowInfo.From(aCallEvent, modulePath);
                    windowInfo.CompareItems.Add(aCompareItem);
                    _listViewWindow.Add(windowInfo);
                    break;
                case HookType.CreateFile:
                case HookType.OpenFile:
                case HookType.ReadFile:
                case HookType.WriteFile:
                case HookType.CreateDirectory:
                case HookType.LoadLibrary:
                case HookType.CreateProcess:
                case HookType.ProcessStarted:
                case HookType.FindResource:
                case HookType.QueryDirectoryFile:
                case HookType.QueryAttributesFile:
                    _fileSystemViewer.AddEvent(aCallEvent, aCompareItem);
                    break;
                case HookType.RegOpenKey:
                case HookType.RegCreateKey:
                case HookType.RegEnumerateKey:
                case HookType.RegQueryKey:
                    var keyInfo = RegKeyInfo.From(aCallEvent);
                    keyInfo.CompareItems.Add(aCompareItem);
                    _treeViewRegistry.Add(keyInfo);
                    break;
                case HookType.RegEnumerateValueKey:
                case HookType.RegQueryValue:
                case HookType.RegSetValue:
                case HookType.RegDeleteValue:
                    var valueInfo = RegValueInfo.From(aCallEvent);
                    valueInfo.CompareItems.Add(aCompareItem);
                    _treeViewRegistry.Add(valueInfo);
                    break;
            }
        }

        private void CopyToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (tabControlData.SelectedTab == tabCom)
            {
                _listViewCom.CopySelectionToClipboard();
            }
            else if (tabControlData.SelectedTab == tabWindow)
            {
                _listViewWindow.CopySelectionToClipboard();
            }
            else if (tabControlData.SelectedTab == tabFile)
            {
                _fileSystemViewer.CopySelectionToClipboard();
            }
            else if (tabControlData.SelectedTab == tabRegistry)
            {
                _treeViewRegistry.CopySelectionToClipboard();
            }
            else if (tabControlData.SelectedTab == tabTrace)
            {
                _treeViewCompare.CopySelectionToClipboard();
            }
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        private void FindToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_searchDlg != null && _searchDlg.Visible)
            {
                _searchDlg.Focus();
            }
            else
            {
                _searchDlg = new TextSearch();
                _searchDlg.FindClick += SearchDlgFindClick;
                _searchDlg.Show(this);
            }
        }

        public void SearchDlgFindClick(object sender, FindEventArgs e)
        {
            if (tabControlData.SelectedTab == tabCom)
            {
                _listViewCom.FindEvent(e);
            }
            else if (tabControlData.SelectedTab == tabWindow)
            {
                _listViewWindow.FindEvent(e);
            }
            else if (tabControlData.SelectedTab == tabFile)
            {
                _fileSystemViewer.FindEvent(e);
            }
            else if (tabControlData.SelectedTab == tabRegistry)
            {
                _treeViewRegistry.Find(e);
            }
            else if (tabControlData.SelectedTab == tabTrace)
            {
                _treeViewCompare.Find(e);
            }
        }

        public void SelectAll()
        {
            if (tabControlData.SelectedTab == tabCom)
            {
                _listViewCom.SelectAll();
            }
            else if (tabControlData.SelectedTab == tabWindow)
            {
                _listViewWindow.SelectAll();
            }
            else if (tabControlData.SelectedTab == tabFile)
            {
                _fileSystemViewer.SelectAll();
            }
            else if (tabControlData.SelectedTab == tabRegistry)
            {
                _treeViewRegistry.SelectAll();
            }
            else if (tabControlData.SelectedTab == tabTrace)
            {
                _treeViewCompare.SelectAll();
            }
        }

        private void FilterToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_eventFilter != null && _eventFilter.Visible)
            {
                _eventFilter.Focus();
            }
            else
            {
                _eventFilter = new EventFilter(EventFilter.FilterForm.Compare, _filter) {Text = "Compare Traces Filter"};
                _eventFilter.Show(this);
            }
        }

        private void FormDeviareCompareFormClosed(object sender, FormClosedEventArgs e)
        {
            _filter.Change -= FilterChange;
        }

        private void FormDeviareCompareKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                if (_searchDlg != null && _searchDlg.Visible)
                {
                    var fc = FormTools.FindFocusedControl(this);

                    _searchDlg.Focus();
                    if (e.Shift)
                        _searchDlg.Previous();
                    else
                        _searchDlg.Next();
                    if (fc != null)
                        fc.Focus();
                }
                else
                {
                    _searchDlg = new TextSearch {SearchDown = !e.Shift};
                    _searchDlg.FindClick += SearchDlgFindClick;
                    _searchDlg.Show(this);
                }
            }
            else if(e.KeyCode == Keys.Enter)
            {
                //FormTools.FindFocusedControl(this);
                
            }
        }

        private void SelectAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            SelectAll();
        }

        private void FormDeviareCompareLoad(object sender, EventArgs e)
        {
            _fileSystemViewer.CompareMode = true;
        }

        private void ShowVirtualPathsToolStripMenuItemClick(object sender, EventArgs e)
        {
            _fileSystemViewer.MergeLayerPaths = !showLayerPathsToolStripMenuItem.Checked;
            Settings.Default.FileSystemMergeLayerPathsCompare = _fileSystemViewer.MergeLayerPaths;
            Settings.Default.Save();
            RefreshEvents();
        }

        private void MergeWowPathsToolStripMenuItemClick(object sender, EventArgs e)
        {
            _fileSystemViewer.MergeWowPaths = mergeWowPathsToolStripMenuItem.Checked;
            Settings.Default.FileSystemMergeWowPathsCompare = _fileSystemViewer.MergeWowPaths;
            Settings.Default.Save();
            RefreshEvents();
        }

        private void ShowModulesLoadedAtStartupToolStripMenuItemClick(object sender, EventArgs e)
        {
            _fileSystemViewer.ShowStartupModules = showStartupModulesToolStripMenuItem.Checked;
            Settings.Default.FileSystemShowStartupModulesCompare = _fileSystemViewer.ShowStartupModules;
            Settings.Default.Save();
            RefreshEvents();
        }

        private void HideQueryAttributesOperationsToolStripMenuItemClick(object sender, EventArgs e)
        {
            _fileSystemViewer.HideQueryAttributes = toolStripMenuItemHideAttributes.Checked;
            Settings.Default.FileSystemHideQueryAttributesCompare = _fileSystemViewer.HideQueryAttributes;
            Settings.Default.Save();
            RefreshEvents();
        }

        #region Implementation of ICompareInterpreterController

        public uint Trace1ID { get { return _trace1.TraceId; } }
        public uint Trace2ID { get { return _trace2.TraceId; } }

        public void ShowInCom(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowIn(tabControlData, tabCom, _listViewCom, anEntry);
        }
        public void ShowInWindows(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowIn(tabControlData, tabWindow, _listViewWindow, anEntry);
        }
        public void ShowInFiles(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowIn(tabControlData, tabFile, _fileSystemViewer, anEntry);
        }
        public void ShowInRegistry(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowInRegistry(tabControlData, tabRegistry, _treeViewRegistry, anEntry);
        }
        public bool ShowQueryAttributesInFiles
        {
            get { return !_fileSystemViewer.HideQueryAttributes; }
        }

        public bool ShowDirectoriesInFiles
        {
            get { return _fileSystemViewer.TreeMode; }
        }

        public bool PropertiesGoToVisible { get { return true; } }

        public bool PropertiesVisible
        {
            get { return true; }
        }

        #endregion IInterpreterController
    }
}