using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.Database;
using SpyStudio.Loader;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Dialogs
{
    public partial class StatsReport : Form
    {
        public static Color MatchColor = Color.White;

        private class FunctionReportItem : ListViewItem
        {
            private double _time, _time1, _time2;

            public FunctionReportItem(string function)
                : base(function)
            {
                SubItems.Add(new ListViewSubItem(this, "0"));
                SubItems.Add(new ListViewSubItem(this, "0"));
                SubItems.Add(new ListViewSubItem(this, "0"));

                _time = _time1 = _time2 = 0;
                Count = 0;
            }

            private uint Count { get; set; }

            private uint Count1 { get; set; }

            private uint Count2 { get; set; }

            private double Time
            {
                get { return _time; }
            }

            private double Time1
            {
                get { return _time1; }
            }

            private double Time2
            {
                get { return _time2; }
            }

            public void AddCall(double t)
            {
                Count++;
                _time += t;
                Update();
            }
            void Update()
            {
                SubItems[1].Text = Count.ToString(CultureInfo.InvariantCulture);
                if(Count != 0)
                {
                    SubItems[1].Text = Count.ToString(CultureInfo.InvariantCulture);
                    SubItems[2].Text = string.Format("{0:N5}", Time);
                    SubItems[3].Text = string.Format("{0:N5}", Count == 0 ? 0 : Time / Count);
                }
                else
                {
                    SubItems[1].Text = Count1.ToString(CultureInfo.InvariantCulture) + " / " +
                                       Count2.ToString(CultureInfo.InvariantCulture);
                    SubItems[2].Text = string.Format("{0:N5}", Time1) + " / " + string.Format("{0:N5}", Time2);
                    SubItems[3].Text = string.Format("{0:N5}", Count1 == 0 ? 0 : Time1 / Count1) + " / " + string.Format("{0:N5}", Count2 == 0 ? 0 : Time2 / Count2);
                }
            }

            public void AddCall1(double t)
            {
                Count1++;
                _time1 += t;
                Update();
            }

            public void AddCall2(double t)
            {
                Count2++;
                _time2 += t;
                Update();
            }
        }
        private class WindowReportItem : ListViewItem
        {
            private double _time, _time1, _time2;
            private string _name1, _name2, _name;

            public WindowReportItem(string className)
                : base(className)
            {
                SubItems.Add(new ListViewSubItem(this, ""));
                SubItems.Add(new ListViewSubItem(this, "0"));

                _time = _time1 = _time2 = 0;
            }

            private uint Count { get; set; }

            private uint Count1 { get; set; }
            private uint Count2 { get; set; }

            public string WindowName { get { return _name; } set { _name = value; Update(); } }

            public string WindowName1 { get { return _name1; } set { _name1 = value; Update(); } }
            public string WindowName2 { get { return _name2; } set { _name2 = value; Update(); } }

            public double Time
            {
                get { return _time; }
            }

            public double Time1
            {
                get { return _time1; }
            }

            public double Time2
            {
                get { return _time2; }
            }

            public void AddCall(double t)
            {
                Count++;
                _time += t;
                Update();
            }
            void Update()
            {
                if (Count != 0)
                {
                    BackColor = MatchColor;
                    SubItems[1].Text = _name;
                    SubItems[2].Text = string.Format("{0:N5}", Time);
                }
                else
                {
                    if(Count1 != 0 && Count2 != 0)
                    {
                        BackColor = MatchColor;
                        SubItems[1].Text = (_name1 == _name2 ? _name1 : _name1 + " / " + _name2);
                        SubItems[2].Text = string.Format("{0:N5}", Time1) + " / " + string.Format("{0:N5}", Time2);
                    }
                    else if(Count1 != 0)
                    {
                        BackColor = EntryColors.File1Color;
                        SubItems[1].Text = _name1;
                        SubItems[2].Text = string.Format("{0:N5}", Time1);
                    }
                    else
                    {
                        BackColor = EntryColors.File2Color;
                        SubItems[1].Text = _name2;
                        SubItems[2].Text = string.Format("{0:N5}", Time2);
                    }
                }
            }

            public void AddCall1(double t)
            {
                Count1++;
                _time1 += t;
                Update();
            }

            public void AddCall2(double t)
            {
                Count2++;
                _time2 += t;
                Update();
            }
        }
        class CompareStatsParams
        {
            public string Filename1, Filename2;
            public DialogResult Result;
            public string Error;
        }

        //private CallEvent[] _events;
        //private CallEvent[] _events1, _events2;
        private readonly Dictionary<string, FunctionReportItem> _functionToItem = new Dictionary<string, FunctionReportItem>();
        private readonly List<WindowReportItem> _pendingWindows1 = new List<WindowReportItem>();
        private readonly List<WindowReportItem> _pendingWindows2 = new List<WindowReportItem>();
        private readonly HashSet<string> _windowBlackList = new HashSet<string> { "WorkerW"};
        //private bool _compareMode;
        private int _currentIndex;
        EventFilter.Filter _filter;

        public StatsReport()
        {
            InitializeComponent();
            labelFile1Color.BackColor = EntryColors.File1Color;
            labelFile2Color.BackColor = EntryColors.File2Color;
            MatchColor = listViewWindow.BackColor;
            listViewWindow.SortColumn = 2;

            //tabControlStats.TabPages.Remove(tabPageWindow);
            KeyPreview = true;
            _filter = null;
        }

        public void SetData(DeviareRunTrace trace)
        {
            var refreshData = new EventsReportData(trace.TraceId) {ControlInvoker = this, ReportBeforeEvents = false};
            refreshData.EventsReady += OnEventsReady;
            refreshData.ReportBegin += OnReportBegin;
            refreshData.ReportEnd += OnReportEnd;
            EventDatabaseMgr.GetInstance().RefreshEvents(refreshData);

            //_events = trace.GetFilteredEvents();
            _filter = null;
            listViewProcessesData.SetTrace(trace);
            // hide first row that shows both filenames in compare mode
            tableLayoutPanel4.RowStyles[0] = new RowStyle(SizeType.Absolute, 0F);
            //LoadData();
        }

        private void OnReportBegin(object sender, EventArgs eventArgs)
        {
            Cursor = Cursors.WaitCursor;
            listViewFunction.BeginUpdate();
            listViewProcessesData.BeginUpdate();
            listViewWindow.BeginUpdate();
            listViewFunction.Items.Clear();
            listViewProcessesData.Items.Clear();
            listViewWindow.Items.Clear();
        }
        private void OnReportEnd(object sender, EventArgs eventArgs)
        {
            listViewFunction.EndUpdate();
            listViewProcessesData.EndUpdate();
            listViewWindow.EndUpdate();
            Cursor = Cursors.Default;
        }

        public void CompareFiles(Form parent, string filename1, string filename2)
        {
            //_compareMode = true;
            var compareParams = new CompareStatsParams { Filename1 = filename1, Filename2 = filename2 };
            _filter = EventFilter.Filter.GetCompareWindowLoadFilter();

            var eventFilter = new EventFilter(EventFilter.FilterForm.Compare, _filter) {Text = "Stats Report Filter"};
            eventFilter.ShowDialog(parent);

            var dlg = new ProgressDialog
                          {
                              Title = "Deviare Statistics Compare",
                              Message = "Loading file 1 ...",
                              Tag = compareParams
                          };
            dlg.Shown += CompareStatsDlgOnShown;
            var res = dlg.ShowDialog(parent);
            if (res == DialogResult.Cancel)
                return;
            if (res == DialogResult.Abort)
            {
                MessageBox.Show(this, compareParams.Error,
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Show(parent);
        }
        public DialogResult LoadLogSync(string filename, ProcessInfo procInfo, out bool success, out string error, ProgressDialog progressDlg, int minimum, int maximum)
        {
            var loader = LogStore.CreateLoadLogStore(progressDlg, filename);
            loader.RefreshEvents = true;
            loader.MinimumProgress = minimum;
            loader.MaximumProgress = maximum;
            loader.ModulePath = new ModulePath();
            loader.ProcessInfo = procInfo;
            loader.EventsReady += OnEventsReady;
            var res = loader.Load();

            success = loader.Success;
            error = loader.Error;

            return res;
        }

        private void CompareStatsDlgOnShown(object sender, EventArgs eventArgs)
        {
            var dlg = (ProgressDialog)sender;
            var compareParams = (CompareStatsParams) dlg.Tag;
            bool success;

            labelFile1.Text = ModulePath.ExtractModuleName(compareParams.Filename1);
            labelFile2.Text = ModulePath.ExtractModuleName(compareParams.Filename2);

            _currentIndex = 1;

            var procInfo1 = new ProcessInfo();
            var procInfo2 = new ProcessInfo();

            listViewProcessesData.SetTraces(procInfo1, procInfo2);
            compareParams.Result = LoadLogSync(compareParams.Filename1, procInfo1, out success, out compareParams.Error, dlg, 0, 50);
            if (compareParams.Result == DialogResult.Cancel)
            {
                dlg.Close();
                return;
            }
            if (compareParams.Result == DialogResult.Abort)
            {
                compareParams.Error = "Error loading file 1: " + compareParams.Error;
                dlg.Close();
                return;
            }

            dlg.Message = "Loading file 2 ...";

            _currentIndex = 2;
            compareParams.Result = LoadLogSync(compareParams.Filename2, procInfo2, out success, out compareParams.Error, dlg, 50, 100);
            if (compareParams.Result == DialogResult.Abort)
            {
                compareParams.Error = "Error loading file 2: " + compareParams.Error;
            }

            dlg.Close();
        }

        string GetFriendlyFunctionName(string function)
        {
            string ret = function;
            if(function.EndsWith(".DllGetClassObject"))
            {
                ret = "DllGetClassObject (" + function.Substring(0, function.Length - ".DllGetClassObject".Length) + ")";
            }
            return ret;
        }

        void ProcessEvents(IEnumerable<CallEvent> events, int index)
        {
            foreach (CallEvent e in events)
            {
                ProcessEvent(e, index);
            }
        }
        bool IsFiltered(CallEvent callEvent)
        {
            var filtered = false;
            if (_filter != null)
            {
                filtered = _filter.IsFiltered(callEvent);
            }
            return filtered;
        }
        void OnEventsReady(object sender, EventsReadyArgs eventsReadyArgs)
        {
            foreach(var ev in eventsReadyArgs.Events)
            {
                ProcessEvent(ev, _currentIndex);
            }
        }
        void ProcessEvent(CallEvent e, int index)
        {
            if(!IsFiltered(e) && !e.Before && !e.IsGenerated)
            {
                FunctionReportItem item;
                if (!_functionToItem.TryGetValue(e.Function, out item))
                {
                    item = new FunctionReportItem(GetFriendlyFunctionName(e.Function));
                    listViewFunction.Items.Add(item);
                    _functionToItem[e.Function] = item;
                }
                switch (index)
                {
                    case 0:
                        item.AddCall(e.Time);
                        listViewProcessesData.AddEvent(e);
                        break;
                    case 1:
                        item.AddCall1(e.Time);
                        listViewProcessesData.AddEvent1(e);
                        break;
                    case 2:
                        item.AddCall2(e.Time);
                        listViewProcessesData.AddEvent2(e);
                        break;
                }
                if(e.Type == HookType.CreateWindow || e.Type == HookType.CreateDialog)
                {
                    if (CreateWindowEvent.IsVisibleWindow(e) && !e.ParamMain.StartsWith("0x"))
                    {
                        // !CompareMode
                        if(index == 0)
                        {
                            var wndItem = new WindowReportItem(e.ParamMain)
                                              {WindowName = e.ParamCount == 1 ? "" : e.Params[1].Value};
                            wndItem.AddCall(e.GenerationTime);
                            listViewWindow.Items.Add(wndItem);
                        }
                        else
                        {
                            WindowReportItem wndItem;
                            if(index == 1 && TryGetUnMatchedItem(e.ParamMain, _pendingWindows2, out wndItem))
                            {
                                // insert it again to update location
                                //listViewWindow.Items.Remove(wndItem);
                                wndItem.WindowName1 = e.ParamCount == 1 ? "" : e.Params[1].Value;
                                wndItem.AddCall1(e.GenerationTime);
                            }
                            else if (index == 2 && TryGetUnMatchedItem(e.ParamMain, _pendingWindows1, out wndItem))
                            {
                                // insert it again to update location
                                //listViewWindow.Items.Remove(wndItem);
                                wndItem.WindowName2 = e.ParamCount == 1 ? "" : e.Params[1].Value;
                                wndItem.AddCall2(e.GenerationTime);
                            }
                            else
                            {
                                wndItem = new WindowReportItem(e.ParamMain);
                                if(index == 1)
                                {
                                    wndItem.WindowName1 = e.ParamCount == 1 ? "" : e.Params[1].Value;
                                    wndItem.AddCall1(e.GenerationTime);
                                    _pendingWindows1.Add(wndItem);
                                }
                                else
                                {
                                    wndItem.WindowName2 = e.ParamCount == 1 ? "" : e.Params[1].Value;
                                    wndItem.AddCall2(e.GenerationTime);
                                    _pendingWindows2.Add(wndItem);
                                }
                                listViewWindow.Items.Add(wndItem);
                            }
                        }
                    }
                }
            }
        }
        bool TryGetUnMatchedItem(string wndName, List<WindowReportItem> pendingItems, out WindowReportItem item)
        {
            int index = 0;
            foreach(var wndItem in pendingItems)
            {
                if(wndItem.Text == wndName)
                {
                    item = wndItem;
                    pendingItems.RemoveAt(index);
                    return true;
                }
                index++;
            }
            item = null;
            return false;
        }
        //public void LoadData()
        //{
        //    //if (!_compareMode && !_events.Any())
        //    //    return;
        //    if (_compareMode && !_events1.Any() && !_events2.Any())
        //        return;

        //    listViewFunction.Cursor = Cursors.WaitCursor;

        //    listViewFunction.BeginUpdate();
        //    listViewFunction.Items.Clear();

        //    if(!_compareMode)
        //    {
        //        //ProcessEvents(_events, 0);
        //    }
        //    else
        //    {
        //        ProcessEvents(_events1, 1);
        //        ProcessEvents(_events2, 2);
        //    }
        //    listViewFunction.EndUpdate();

        //    listViewFunction.Cursor = Cursors.Default;
        //}

        private void ButtonCloseClick(object sender, EventArgs e)
        {
            Close();
        }

        private void StatsReportKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                if (listViewFunction.Focused)
                {
                    ListViewTools.SelectAll(listViewFunction);
                }
                else if (listViewProcessesData.Focused)
                {
                    ListViewTools.SelectAll(listViewProcessesData);
                }
                else if (listViewWindow.Focused)
                {
                    ListViewTools.SelectAll(listViewWindow);
                }
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                if (listViewFunction.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(listViewFunction, true);
                }
                else if (listViewProcessesData.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(listViewProcessesData, true, true);
                }
                else if (listViewWindow.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(listViewWindow, true, true);
                }
            }
        }
        void FilterChange(object sender, EventArgs e)
        {
        }

    }
}
