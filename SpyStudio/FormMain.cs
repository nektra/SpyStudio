using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Aga.Controls;
using Nektra.Deviare2;
using SpyStudio.Dialogs;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Dialogs.ExportWizards.MassExports;
using SpyStudio.Export.SWV;
using SpyStudio.Export.ThinApp;
using SpyStudio.Forms;
using SpyStudio.Hooks;
using SpyStudio.Properties;
using SpyStudio.Registry.Controls;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using SpyStudio.Trace;
using Timer = System.Windows.Forms.Timer;

// ReSharper disable LocalizableElement

namespace SpyStudio
{
    public partial class FormMain : MonitorForm
    {
        public FormMain()
        {
            InitializeComponent();

            UpdateClass.Check("/spystudio/start");

            PendingEventsCountManager.GetInstance().FormMain = this;

            if (Settings.Default.FileSystemShowTree)
                toolStripMenuItemFileSystemTreeMode.Checked = true;
            else
                toolStripMenuItemFileSystemFlatMode.Checked = true;

            callDump.listViewFileSystem.MergeLayerPaths = Settings.Default.FileSystemMergeLayerPaths;
            callDump.listViewFileSystem.MergeWowPaths = Settings.Default.FileSystemMergeWowPaths;
            callDump.listViewFileSystem.ShowStartupModules = Settings.Default.FileSystemShowStartupModules;
            callDump.treeViewRegistry.MergeLayerPaths = Settings.Default.FileSystemMergeLayerPaths;
            callDump.listViewFileSystem.HideQueryAttributes = Settings.Default.FileSystemHideQueryAttributes;

            callDump.treeViewRegistry.RedirectClasses =
                mergeCOMClassesToolStripMenuItem.Checked = Settings.Default.RegistryMergeClassPaths;
            callDump.treeViewRegistry.MergeWow =
                mergeWowKeyPathsToolStripMenuItem.Checked = Settings.Default.RegistryMergeWowPaths;

            toolStripMenuItemHideAttributes.Checked = callDump.listViewFileSystem.HideQueryAttributes;
            showLayerPathsToolStripMenuItem.Checked = !callDump.listViewFileSystem.MergeLayerPaths;
            mergeWowPathsToolStripMenuItem.Checked = callDump.listViewFileSystem.MergeWowPaths;
            showStartupModulesToolStripMenuItem.Checked = callDump.listViewFileSystem.ShowStartupModules;

            callDump.listViewFileSystem.TreeMode = Settings.Default.FileSystemShowTree;

            callDump.treeViewRegistry.ExportRequest += TreeViewRegistryOnExportRequest;
            callDump.OnTabSelected += OnTabSelected;

            _mainForm = this;
            KeyPreview = true;
            _updateProcessInListViewTimer.Interval = 100;
            _updateProcessInListViewTimer.Enabled = true;
            _updateProcessInListViewTimer.Tick += UpdateProcessInListView;
            _updateProcessInListViewTimer.Start();

            WindowState = FormWindowState.Maximized;

            _hookMgr = new HookMgr();
            _hookMgr.InitializationFinished += HookMgrOnInitializationFinished;
            _hookMgr.MonitoringChange += HookMgrOnMonitoringChange;

            fullStackInfoToolStripMenuItem.Checked = Settings.Default.FullStackInfo;
            fullStackInfoToolStripMenuItem.CheckOnClick = true;

            GC.AddMemoryPressure(140000000);
            PendingEventsCountManager.GetInstance().InitStatus(statusStrip1);
#if DEBUG
            var item = analysisToolStripMenuItem.DropDownItems.Add("Simulate (debugging)");
            item.Click += (sender, args) => _hookMgr.AsyncHookMgr.StartSimulation();
#endif
        }

        private void OnTabSelected(CallDumpTabPage atabpage)
        {
            var goToItem =
                editToolStripMenuItem.DropDownItems.Cast<ToolStripItem>().First(
                    i => i.Text.Replace("&", "").Equals("Go To..."));
            goToItem.Enabled = callDump.CanDoGoTo;
        }

        #region UI

        private readonly Timer _updateProcessInListViewTimer = new Timer();
        private readonly HookStateInfo _updateProcessInListViewHookStateInfo = new HookStateInfo();
        private uint _updateProcessInListViewTick;
        private bool _updateProcessInListViewTick2;
        private readonly List<ToolStripMenuItem> _selectableItems = new List<ToolStripMenuItem>(); 

        private static FormMain _mainForm;
        private const bool AppShutdown = false;
        private static bool _collectingData = true;
        private TextSearch _searchDlg;
        private EventFilter _eventFilter;
        private ListViewGroup _hookedProcesses;
        private ListViewGroup _unhookedProcesses;
        private FormMonitorGroupManager _groupManager;
        private bool _currentSyncImage1 = true;
        private int _iconChangeCount;
        private int _beginUpdateCount, _filteredEvents, _totalEvents;

        public static bool ApplicationShutdown
        {
            get { return AppShutdown; }
        }

        public static bool CollectingData
        {
            get { return _collectingData; }
        }

        private class ProcessListViewItemInfo
        {
            public readonly uint Pid;
            public readonly bool Hookeable;

            public ProcessListViewItemInfo(uint pid, bool hookeable)
            {
                Pid = pid;
                Hookeable = hookeable;
            }
        }

        public delegate void FindEventHandler(object sender, FindEventArgs e);

// ReSharper disable UnusedMethodReturnValue.Local
        private ListViewItem InsertProcessInListView(int procId, string procName, bool hookeable)
// ReSharper restore UnusedMethodReturnValue.Local
        {
            // System process is blank
            if (string.IsNullOrEmpty(procName) && procId == 4)
            {
                procName = "System";
            }

            var item = new ListViewItem(procName);
            var path = _processInfo.GetPath((uint) procId);

            if (path != "")
            {
                path = path.ToLower();
                var index = listViewProcesses.SmallImageList.Images.IndexOfKey(path);
                if (index != -1)
                {
                    item.ImageIndex = index;
                }
                else
                {
                    var procIcon = _processInfo.GetIcon((uint) procId);
                    if (procIcon != null)
                    {
                        listViewProcesses.SmallImageList.Images.Add(path, procIcon);
                        index = listViewProcesses.SmallImageList.Images.IndexOfKey(path);
                        item.ImageIndex = index;
                    }
                }
            }
            if (!hookeable)
            {
                item.ForeColor = Color.FromArgb(192, 192, 192);
                item.Tag = new ProcessListViewItemInfo((uint) procId, false);
            }
            else
            {
                item.Tag = new ProcessListViewItemInfo((uint) procId, true);
            }
            //_processListChanging = true;
            item.Group = _unhookedProcesses;
            item.SubItems.Add(procId.ToString(CultureInfo.InvariantCulture));
            listViewProcesses.Items.Add(item);
            //_processListChanging = false;
            return item;
        }

        private void RemoveProcessFromListView(uint pid)
        {
            foreach (
                var item in
                    listViewProcesses.Items.Cast<ListViewItem>().Where(
                        item => ((ProcessListViewItemInfo) item.Tag).Pid == pid))
            {
                listViewProcesses.Items.Remove(item);
                break;
            }
        }

        private delegate void UpdateButtonsStateDelegate();

        private void UpdateButtonsState()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new UpdateButtonsStateDelegate(UpdateButtonsState));
            }
            else
            {
                toolStripMenuItemStop.Enabled = IsMonitoring;
                //asynchronousToolStripMenuItem.Enabled = IsMonitoring;
                hookNewProcessesToolStripMenuItem.Checked = _hookMgr.HookNewProcesses;
                fullStackInfoToolStripMenuItem.Enabled = !IsMonitoring;

                foreach (var menuItem in _selectableItems)
                {
                    menuItem.Enabled = !IsMonitoring;
                }
            }
            ResetDotNetEnabledFlags();
        }

        #endregion

        #region Analysis

        private void UpdateActiveGroups()
        {
            var newActiveGroups = "";
            var activeGroups = new HashSet<string>();
            if (_monitorDotNet)
            {
                activeGroups.Add(DotNetProfilingGroupName);
                if (newActiveGroups != "")
                    newActiveGroups += ",";
                newActiveGroups += DotNetProfilingGroupName.ToLower();
            }
            
            foreach (ToolStripMenuItem menuItem in monitorToolStripMenuItem.DropDownItems)
            {
                if (menuItem.DropDownItems.Count > 0)
                    continue;
                if (menuItem.Checked)
                {
                    activeGroups.Add(menuItem.Text);
                    if (newActiveGroups != "")
                        newActiveGroups += ",";
                    newActiveGroups += menuItem.Text.ToLower();
                }
            }
            Settings.Default.ActiveHookGroups = newActiveGroups;
            Settings.Default.Save();

            _hookMgr.ActiveGroups = activeGroups;
        }

        private void StopAnalysis(bool waitCompletion)
        {
            _hookMgr.StopAnalysis(waitCompletion);
        }

        #endregion

        #region Deviare

        #region DeviareVars

        private NktSpyMgr _spyMgr;
        private ModulePath _modulePath;
        private DeviareRunTrace _devRunTrace;
        private readonly HookMgr _hookMgr;
        private ProcessInfo _processInfo;
        private EventFilter.Filter _filter;

        #endregion

        public static FormMain GetMainForm()
        {
            return _mainForm;
        }

        private void Deviare_OnAgentLoad(NktProcess proc, int errorcode)
        {
            if (errorcode != 0)
                MarkAsFailedToBeHooked(proc.Id);
        }

        //private void InvokeIfRequiredAndCall(ISynchronizeInvoke aControl, Action anAction)
        //{
        //    if (aControl.InvokeRequired)
        //        BeginInvoke(new MethodInvoker(anAction));
        //    else
        //        anAction();
        //}

        private void MarkAsFailedToBeHooked(int procId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => MarkAsFailedToBeHooked(procId)));
            }
            else
            {
                Func<ListViewItem, bool> f = item => ((ProcessListViewItemInfo) item.Tag).Pid == procId;
                var list = listViewProcesses.Items.Cast<ListViewItem>();
                var processItem = list.Where(f).FirstOrDefault();
                if (processItem != null)
                    processItem.ForeColor = Color.Red;
            }
        }

        private void HookMgrOnInitializationFinished(object sender,
                                                     DeviareInitializer.InitializeFinishedEventArgs
                                                         initializeFinishedEventArgs)
        {
            if (initializeFinishedEventArgs.Success)
            {
                _devRunTrace = _hookMgr.DeviareRunTrace;
                _devRunTrace.EventInfoChanged += DevRunTraceOnEventInfoChanged;
                _devRunTrace.UpdateBegin += DevRunTraceOnUpdateBegin;
                _devRunTrace.UpdateEnd += DevRunTraceOnUpdateEnd;
                _devRunTrace.EventAdd += DevRunTraceOnEventAdd;
                _devRunTrace.LoadEnd += DevRunTraceOnLoadEnd;

                _processInfo = _hookMgr.ProcessInfo;
                _modulePath = _hookMgr.ModulePath;
                _filter = _hookMgr.CurrentFilter;
                _spyMgr = _hookMgr.SpyMgr;

                InitialProcessesListFill(initializeFinishedEventArgs.MinimumProgress,
                                         initializeFinishedEventArgs.MaximumProgress);

                _devRunTrace.SetParent(this);
                callDump.Attach(_devRunTrace);

                _spyMgr.OnProcessStarted += Deviare_OnProcessStarted;
                _spyMgr.OnProcessTerminated += Deviare_OnProcessTerminated;
                _spyMgr.OnHookStateChanged += Deviare_OnStateChanged;
                _spyMgr.OnAgentLoad += Deviare_OnAgentLoad;

                InitializeMonitorMenu();
            }
            else
            {
                DelayedClose();
            }
        }

        private const string DotNetProfilingGroupName = ".NET";

        private void InitializeMonitorMenu()
        {
            ToolStripMenuItem basicMonitoringMenu = null;
            {
                var dotNetMonitorMenu = new ToolStripMenuItem(DotNetProfilingGroupName);
                var dotNetMenuItemNames = new[]
                                 {
                                     "Basic",
                                     "Exception",
                                     "Garbage Collector",
                                     "JIT",
                                     "Object allocation",
                                 };
                dotNetMonitorMenu.DropDown.Closing += DropDownOnClosing;

                var dotNetMenuItemHandlers = new EventHandler[]
                                 {
                                     OnDotNetBasicMonitoringClicked,
                                     OnDotNetExceptionMonitoringClicked,
                                     OnDotNetGcMonitoringClicked,
                                     OnDotNetJitMonitoringClicked,
                                     OnDotNetObjectMonitoringClicked,
                                 };
                var dotNetMenuItemConfig = new[]
                                 {
                                     true,
                                     Settings.Default.MonitorDotNetExceptions,
                                     Settings.Default.MonitorDotNetGc,
                                     Settings.Default.MonitorDotNetJit,
                                     Settings.Default.MonitorDotNetObjectAllocations,
                                 };
                for (int i = 0; i < dotNetMenuItemNames.Length; i++)
                {
                    var submenu = (ToolStripMenuItem)dotNetMonitorMenu.DropDownItems.Add(dotNetMenuItemNames[i]);
                    if (basicMonitoringMenu == null)
                        basicMonitoringMenu = submenu;
                    submenu.Click += dotNetMenuItemHandlers[i];

                    //submenu.MouseDown += MenuItemOnMouseDown;
                    submenu.Checked = dotNetMenuItemConfig[i];
                    submenu.CheckOnClick = true;

                    _selectableItems.Add(submenu);
                }

                monitorToolStripMenuItem.DropDownItems.Add(dotNetMonitorMenu);
            }

            monitorToolStripMenuItem.DropDown.Closing += DropDownOnClosing;

            var activeGroups = Settings.Default.ActiveHookGroups.Split(',');

            var groups = _hookMgr.Groups.ToList();
            groups.Sort();
            foreach (var g in groups)
            {
                bool active = activeGroups.Contains(g.ToLower());
                var state = active ? CheckState.Checked : CheckState.Unchecked;
                if (g == DotNetProfilingGroupName)
                {
                    MonitorDotNet = active;
                    Debug.Assert(basicMonitoringMenu != null);
                    basicMonitoringMenu.CheckState = state;
                    continue;
                }
                var menuItem = new ToolStripMenuItem
                {
                    Checked = true,
                    CheckOnClick = true,
                    CheckState = state,
                    Name = g + "ToolStripMenuItem",
                    Size = new Size(152, 22),
                    Text = char.ToUpper(g[0]) + g.Substring(1)
                };
                AddHookGroup(menuItem);
            }
        }

        private void DropDownOnClosing(object sender, ToolStripDropDownClosingEventArgs toolStripDropDownClosingEventArgs)
        {
            toolStripDropDownClosingEventArgs.Cancel = toolStripDropDownClosingEventArgs.CloseReason ==
                                                       ToolStripDropDownCloseReason.ItemClicked;
        }

        private bool _monitorDotNet;
        private bool MonitorDotNet
        {
            set
            {
                _monitorDotNet = value;
                ResetDotNetEnabledFlags();
            }
            get
            {
                return _monitorDotNet;
            }
        }

        private void ResetDotNetEnabledFlags()
        {
            ToolStripMenuItem submenu = null;
            foreach (var item in monitorToolStripMenuItem.DropDownItems.Cast<ToolStripMenuItem>())
            {
                if (item.Text == DotNetProfilingGroupName)
                {
                    submenu = item;
                    break;
                }
            }
            if (submenu == null)
                return;
            foreach (var item in submenu.DropDownItems.Cast<ToolStripMenuItem>())
            {
                if (item.Text == "Basic")
                    continue;
                item.Enabled = MonitorDotNet;
            }
        }

        private void OnDotNetBasicMonitoringClicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null)
                return;
            MonitorDotNet = item.Checked;
        }

        private void OnDotNetExceptionMonitoringClicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null)
                return;
            Settings.Default.MonitorDotNetExceptions =
            _hookMgr.MonitorDotNetExceptions = item.Checked;
        }

        private void OnDotNetGcMonitoringClicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null)
                return;
            Settings.Default.MonitorDotNetGc =
            _hookMgr.MonitorDotNetGc = item.Checked;
        }

        private void OnDotNetJitMonitoringClicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null)
                return;
            Settings.Default.MonitorDotNetJit =
            _hookMgr.MonitorDotNetJit = item.Checked;
        }

        private void OnDotNetObjectMonitoringClicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null)
                return;
            Settings.Default.MonitorDotNetObjectAllocations =
            _hookMgr.MonitorDotNetObjectCreation = item.Checked;
        }

        private void DevRunTraceOnLoadEnd(object sender, EventArgs e)
        {
            UpdateStatusBarPendingEvents();
        }

        private void DevRunTraceOnUpdateEnd(object sender, EventArgs eventArgs)
        {
            if (--_beginUpdateCount == 0)
            {
                UpdateEventCount();
                UpdateStatusBarPendingEvents();
            }
        }

        private void DevRunTraceOnUpdateBegin(object sender, EventArgs eventArgs)
        {
            _beginUpdateCount++;
        }

        private void DevRunTraceOnEventInfoChanged(object sender,
                                                   DeviareRunTrace.EventInfoChangeArgs eventInfoChangeArgs)
        {
            _totalEvents = eventInfoChangeArgs.TotalEvents;
            _filteredEvents = eventInfoChangeArgs.FilteredEvents;

            if (_beginUpdateCount == 0)
            {
                UpdateEventCount();
            }
        }

        private void UpdateEventCount()
        {
            _statusBarFilteredEvents.Text = String.Format("Filtered Events: {0:N0}", _filteredEvents);
            _statusBarTotalEvents.Text = String.Format("Total Events: {0:N0}", _totalEvents);
        }

        private void HookMgrOnMonitoringChange(object sender,
                                               HookMgr.MonitoringChangeEventArgs monitoringChangeEventArgs)
        {
            IsMonitoring = monitoringChangeEventArgs.IsMonitoring;
            UpdateButtonsState();
        }

        private void DevRunTraceOnEventAdd(object sender, CallEventArgs callEventArgs)
        {
        }

        private delegate void UpdateStatusBarPendingEventsDelegate();

        private bool _anyEventsLeft = false;

        public void UpdateStatusBarPendingEvents()
        {
            if (_beginUpdateCount != 0)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new UpdateStatusBarPendingEventsDelegate(UpdateStatusBarPendingEvents));
            }
            else
            {
                var pecm = PendingEventsCountManager.GetInstance();
                bool nel = pecm.NoEventsLeft;
                if (!nel)
                {
                    var pendingString = PendingEventsCountManager.GetInstance().ToString();
                    _statusBarProcessing.Text = "Processing Events " + pendingString;
                    _statusBarSyncIcon.Visible = true;
                }
                else
                {
                    _statusBarProcessing.Text = "No Pending Events";
                    _statusBarSyncIcon.Visible = false;
                    if (_anyEventsLeft)
                        GCTools.AsyncCollectDelayed(10000);
                }
                _anyEventsLeft = !nel;
            }
        }

        public static void EnableItem(bool enable)
        {
            _mainForm.swvToolStripMenuItem.Enabled = enable;
            _mainForm.tracesToolStripMenuItem.Enabled = enable;
            _mainForm.statisticsToolStripMenuItem.Enabled = enable;
        }

        private delegate void AddHookGroupDelegate(ToolStripMenuItem menuItem);

        private void AddHookGroup(ToolStripMenuItem menuItem)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new AddHookGroupDelegate(AddHookGroup), menuItem);
            }
            else
            {
                monitorToolStripMenuItem.DropDownItems.Add(menuItem);
                _selectableItems.Add(menuItem);
            }
        }

        private delegate void DelayedCloseDelegate();

        private void DelayedClose()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new DelayedCloseDelegate(DelayedClose));
            }
            else
            {
                Close();
            }
        }

        private delegate void InitialProcessesListFillDelegate(int minimum, int maximum);

        private void InitialProcessesListFill(int minimum, int maximum)
        {
            if (InvokeRequired)
            {
                var res = BeginInvoke(new InitialProcessesListFillDelegate(InitialProcessesListFill), minimum, maximum);
                EndInvoke(res);
            }
            else
            {
                listViewProcesses.BeginUpdate();
                listViewProcesses.Items.Clear();

                var enumProcs = _spyMgr.Processes();
                var count = enumProcs.Count;
                var i = 0;
                foreach (NktProcess proc in enumProcs)
                {
                    ProcessStarted(proc.Id, proc.Name, proc.Path, _hookMgr.IsHookeable(proc));

                    //Console.WriteLine(proc.Name + " Elapsed " + sw.Elapsed);

                    InitializingForm.SetProgress(minimum + (++i*(maximum - minimum)/count));
                }

                listViewProcesses.EndUpdate();
            }
        }

        #endregion

        #region DeviareEvents

        private delegate void ProcessStartedDelegate(int procId, string procName, string procPath, bool hookeable);

        private void ProcessStarted(int procId, string procName, string procPath, bool hookeable)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ProcessStartedDelegate(ProcessStarted), procId, procName, procPath, hookeable);
            }
            else
            {
                _hookMgr.InsertProcessData(procId, procName, procPath);
                InsertProcessInListView(procId, procName, hookeable);
            }
        }

        private void Deviare_OnProcessStarted(NktProcess proc)
        {
            if (proc.get_IsActive(10))
            {
                ProcessStarted(proc.Id, proc.Name, proc.Path, _hookMgr.IsHookeable(proc));
            }
        }

        private delegate void ProcessTerminatedDelegate(int procId);

        private void ProcessTerminated(int procId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ProcessTerminatedDelegate(ProcessTerminated), procId);
            }
            else
            {
                var pid = (uint) procId;

                RemoveProcessFromListView(pid);
            }
        }

        private void Deviare_OnProcessTerminated(NktProcess proc)
        {
            ProcessTerminated(proc.Id);
        }

        private string GetItemNameWithCount(string name, uint count)
        {
            var slashPos = name.LastIndexOf("/", StringComparison.Ordinal);
            if (slashPos >= 0)
            {
                name = name.Substring(0, slashPos).Trim();
            }
            if (count > 0)
            {
                name = name + " / " + count.ToString(CultureInfo.InvariantCulture);
            }
            return name;
        }

        private delegate void SignalUpdateProcessInListViewDelegate();

        private void SignalUpdateProcessInListView()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new SignalUpdateProcessInListViewDelegate(SignalUpdateProcessInListView));
            }
            else
            {
                if (_updateProcessInListViewTick2 == false)
                {
                    if (_updateProcessInListViewTick == 0)
                        _updateProcessInListViewTick = 10;
                    _updateProcessInListViewTick2 = true;
                }
            }
        }

        public void UpdateProcessInListView(object sender, EventArgs eArgs)
        {
            if (_updateProcessInListViewTick > 0)
            {
                if ((--_updateProcessInListViewTick) == 0)
                {
                    foreach (ListViewItem item in listViewProcesses.Items)
                    {
                        var count =
                            _updateProcessInListViewHookStateInfo.GetActiveCount(
                                ((ProcessListViewItemInfo) item.Tag).Pid);

                        if (count > 0)
                        {
                            if (item.Group != _hookedProcesses)
                            {
                                listViewProcesses.BeginUpdate();
                                item.Font = new Font(listViewProcesses.Font, FontStyle.Bold);
                                item.Group = _hookedProcesses;

                                // WORKAROUND: add a new item to the group and remove it to force the listview to reorder the items.
                                // otherwise, the item is appended to the end and the ListViewItemSorter is not called
                                var dummyItem = new ListViewItem("Dummy") {Group = _hookedProcesses};
                                dummyItem.SubItems.Add(new ListViewItem.ListViewSubItem(dummyItem, "0"));
                                listViewProcesses.Items.Add(dummyItem);
                                dummyItem.Remove();
                                listViewProcesses.EndUpdate();
                            }
                            item.Text = GetItemNameWithCount(item.Text, count);
                        }
                        else
                        {
                            if (item.Group != _unhookedProcesses)
                            {
                                listViewProcesses.BeginUpdate();
                                item.Font = new Font(listViewProcesses.Font, FontStyle.Regular);
                                item.Text = GetItemNameWithCount(item.Text, 0);
                                item.Group = _unhookedProcesses;
                                item.ForeColor = Color.Black;

                                // WORKAROUND: add a new item to the group and remove it to force the listview to reorder the items.
                                // otherwise, the item is appended to the end and the ListViewItemSorter is not called
                                var dummyItem = new ListViewItem("Dummy") {Group = _unhookedProcesses};
                                dummyItem.SubItems.Add(new ListViewItem.ListViewSubItem(dummyItem, "0"));
                                listViewProcesses.Items.Add(dummyItem);
                                dummyItem.Remove();
                                listViewProcesses.EndUpdate();
                            }
                        }
                    }

                    if (_updateProcessInListViewTick2)
                    {
                        _updateProcessInListViewTick = 10;
                        _updateProcessInListViewTick2 = false;
                    }
                }
            }

            if (_statusBarSyncIcon.Visible && ++_iconChangeCount == 5)
            {
                _iconChangeCount = 0;
                _currentSyncImage1 = !_currentSyncImage1;
                _statusBarSyncIcon.Image = _currentSyncImage1 ? Resources.syncing : Resources.syncing2;
            }
        }

        private void Deviare_OnStateChanged(NktHook hook, NktProcess proc, eNktHookState newState,
                                            eNktHookState oldState)
        {
            switch (newState)
            {
                case eNktHookState.stActive:
                    //System.Diagnostics.Debug.WriteLine("Adding hook (" + counter1.ToString() + ") - " + proc.ToString() + " - " + hook.ToString());
                    _updateProcessInListViewHookStateInfo.SetActive((uint) proc.Id, hook.Id, true);
                    SignalUpdateProcessInListView();
                    break;
                case eNktHookState.stRemoved:
                    //System.Diagnostics.Debug.WriteLine("Removing hook (" + counter2.ToString() + ") - " + proc.ToString() + " - " + hook.ToString());
                    _updateProcessInListViewHookStateInfo.SetActive((uint) proc.Id, hook.Id, false);
                    SignalUpdateProcessInListView();
                    break;

                case eNktHookState.stError:
                    _updateProcessInListViewHookStateInfo.SetActive((uint) proc.Id, hook.Id, true);
                    if (AllHooksFailedForProcessWithPid(proc.Id))
                        MarkAsFailedToBeHooked(proc.Id);
                    SignalUpdateProcessInListView();
                    break;
            }
        }

        private bool AllHooksFailedForProcessWithPid(int aPid)
        {
            // all hooks reported there state and all have state stError

            var hookStates = _hookMgr.HookStatesForPid(aPid);

            return hookStates.All(hInfo =>
                                  (hInfo.State == eNktHookState.stError || hInfo.State == eNktHookState.stInactive));
        }

        #endregion

        #region Events

        private void ButtonOpenFileClick(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog
                {
                    DefaultExt = "exe",
                    Filter = "Executable file (*.exe)|*.exe|All files (*.*)|*.*",
                    AddExtension = true,
                    RestoreDirectory = true,
                    Title = "Execute"
                };
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedAppFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedAppFolder;

            if (openDlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Settings.Default.PathLastSelectedAppFolder = Path.GetDirectoryName(openDlg.FileName);
                    Settings.Default.Save();
                }
                catch (Exception)
                {
                }

                textBoxProgramExec.Text = openDlg.FileName;
                //UpdateButtonsState();
            }
        }

        private void ListViewProcessesSelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void ListViewProcessesClientSizeChanged(object sender, EventArgs e)
        {
            var w = listViewProcesses.Width - 44;

            w -= SystemInformation.VerticalScrollBarWidth;
            if (w < 1)
                w = 1;
            listViewProcesses.Columns[0].Width = w;
            listViewProcesses.Columns[1].Width = 44;
        }

        private void formMain_OnLoad(object sender, EventArgs e)
        {
            InitializingForm.SetParent(this);

            UpdateTitle("");

            var toolTip1 = new ToolTip {AutoPopDelay = 5000, InitialDelay = 1000, ReshowDelay = 500, ShowAlways = true};

            toolTip1.SetToolTip(buttonOpenFile, "Browse");
            toolTip1.SetToolTip(buttonPlay,
                                toolStripMenuItemStart.Text.Trim(new[] {'&'}) + "    " +
                                toolStripMenuItemStart.ShortcutKeys);

            if (!string.IsNullOrEmpty(Settings.Default.LastExecutePath) && File.Exists(Settings.Default.LastExecutePath))
            {
                textBoxProgramExec.Text = Settings.Default.LastExecutePath;
            }

            listViewProcesses.ContextMenuStrip.Opening += ContextMenuProcessesOpening;
            _hookedProcesses = new ListViewGroup("Intercepted");
            _unhookedProcesses = new ListViewGroup("Not Intercepted");
            listViewProcesses.Groups.Add(_hookedProcesses);
            listViewProcesses.Groups.Add(_unhookedProcesses);
            if (listViewProcesses.SmallImageList == null)
            {
                listViewProcesses.SmallImageList = new ImageList();
            }

            toolStripMenuItemStart.Enabled = true;

            _hookMgr.Initialize(this);

            InitializationFinished();
        }

        private void InitializationFinished()
        {
            Activate();
        }

        private void formMain_OnClose(object sender, FormClosedEventArgs e)
        {
            _hookMgr.Shutdown();
            _updateProcessInListViewTimer.Tick -= UpdateProcessInListView;
            _updateProcessInListViewTimer.Stop();
        }

        private void ToolStripMenuItemStartClick(object sender, EventArgs e)
        {
            TestPathAndExecute(false);
        }

        private static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            try
            {
                //get the currently logged in user
                var user = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        private void ExecuteInstallerAndHookToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (IsUserAdministrator())
                TestPathAndExecute(true);
            else
                MessageBox.Show(this,
                                "You need administrator privileges to properly hook installers.",
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void TestPathAndExecute(bool installer)
        {
            if (File.Exists(textBoxProgramExec.Text))
            {
                ExecuteProgramAndHook(
                    textBoxProgramExec.Text,
                    Settings.Default.LastExecuteParameters,
                    Settings.Default.RunAsUserName,
                    Settings.Default.RunAsPassword,
                    installer
                    );
            }
            else
            {
                MessageBox.Show(this,
                                "SpyStudio could not run file: \"" + textBoxProgramExec.Text +
                                "\"\nFile does not exist.",
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToolStripMenuItemStopClick(object sender, EventArgs e)
        {
            StopAnalysis(false);
        }

        #endregion

        #region HookStateInfo

        private class HookStateInfo
        {
            private class PidEntry
            {
                public readonly HashSet<IntPtr> HooksList;

                public PidEntry()
                {
                    HooksList = new HashSet<IntPtr>();
                }
            }

            private readonly Object _listLock;
            private readonly Dictionary<uint, PidEntry> _pidsList;

            public HookStateInfo()
            {
                _listLock = new object();
                _pidsList = new Dictionary<uint, PidEntry>();
            }

            public void SetActive(uint pid, IntPtr hookId, bool isActive)
            {
                lock (_listLock)
                {
                    if (isActive)
                    {
                        if (!_pidsList.ContainsKey(pid))
                            _pidsList.Add(pid, new PidEntry());
                        if (_pidsList[pid].HooksList.Contains(hookId))
                            return;
                        _pidsList[pid].HooksList.Add(hookId);
                    }
                    else
                    {
                        if ((!_pidsList.ContainsKey(pid)) || (!_pidsList[pid].HooksList.Contains(hookId)))
                            return;
                        _pidsList[pid].HooksList.Remove(hookId);
                        if (_pidsList[pid].HooksList.Count == 0)
                            _pidsList.Remove(pid);
                    }
                }
            }

            public uint GetActiveCount(uint pid)
            {
                uint res = 0;

                lock (_listLock)
                {
                    if (_pidsList.ContainsKey(pid))
                        res = (uint) _pidsList[pid].HooksList.Count;
                }
                return res;
            }
        }

        #endregion

        private void DoExport(MassExportWizard aWizard)
        {
            // The requirements will be checked when the proper Exports are created
            if (!aWizard.SystemMeetsRequirements() || aWizard.Canceled)
                return;

            if (!PendingEventsCountManager.GetInstance().NoEventsLeft)
            {
                if (MessageBox.Show(this,
                                    "SpyStudio hasn't finished processing all reported events. Do you still want to export this partial data?",
                                    Settings.Default.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                    DialogResult.No)
                {
                    return;
                }
            }


            aWizard.Disposed += WizardClosed;
            aWizard.Show();
        }

        private void DoExport(ExportWizard aWizard)
        {
            if (!aWizard.SystemMeetsRequirements() || aWizard.Canceled)
                return;

            if (!PendingEventsCountManager.GetInstance().NoEventsLeft)
            {
                if (MessageBox.Show(this,
                                    "SpyStudio hasn't finished processing all reported events. Do you still want to export this partial data?",
                                    Settings.Default.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                    DialogResult.No)
                {
                    CleanUp();
                    return;
                }
            }

            aWizard.Disposed += WizardClosed;

            aWizard.Show();
        }

        private void WizardClosed(object sender, EventArgs e)
        {
            CleanUp();
        }
        private void CleanUp()
        {
            GCTools.AsyncCollectDelayed(10000);
        }

        private void ExportToSwvToolStripMenuItemClick(object sender, EventArgs e)
        {
            DoExport(WizardFactory.CreateWizardFor(new SwvExport(_devRunTrace)));
        }

        private void ExportToThinAppToolStripMenuItemClick(object sender, EventArgs e)
        {
            /*
            if (!DeviareInitializer.CheckFeature(DeviareInitializer.FeatureSupported.CreateApps))
            {
                var trialDialog = new TrialDialog {Feature = "EW"};
                trialDialog.ShowDialog(this);
                return;
            }
            */

            DoExport(WizardFactory.CreateWizardFor(new ThinAppExport(_devRunTrace)));
        }

        private void MassExportToThinAppToolStripMenuItemClick(object sender, EventArgs e)
        {
            DoExport(WizardFactory.CreateWizardFor(new MassThinAppExport(_devRunTrace)));
        }

        public void ClearAllDataAsync()
        {
            ClearAllData(false);
        }

        public void ClearAllDataSync()
        {
            ClearAllData(true);
        }

        private void ClearAllData(bool sync)
        {
            {
                ManualResetEvent ev = null;
                Action f = null;
                if (sync)
                {
                    ev = new ManualResetEvent(false);
                    f = () => ev.Set();
                    _devRunTrace.ClearCompleted += f;
                }
                _devRunTrace.Clear();
                _hookMgr.Clear();
                if (sync)
                {
                    while (!ev.WaitOne(100))
                        Application.DoEvents();
                    _devRunTrace.ClearCompleted -= f;
                }
            }
            if (!IsMonitoring)
                UpdateTitle("");
            GCTools.AsyncCollectDelayed(10000);
        }

        private void UpdateTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Text = "New trace - " + Settings.Default.AppName;
            }
            else
            {
                if (text.Length > 100)
                    text = text.Substring(0, 100) + " ...";
                Text = text + " - " + Settings.Default.AppName;
            }
        }

        private void UpdateDefaultLoadExtension(string filename)
        {
            Settings.Default.LastDefaultLoadExtension = filename.ToLower().EndsWith("xml") ? "xml" : "spy";
            Settings.Default.Save();
        }

        private void OpenLogAndOptionallyFilter(bool doFilter)
        {
            if (_devRunTrace == null)
                return;

            if (!_devRunTrace.IsEmpty())
            {
                if (MessageBox.Show(this, "You will lose all collected data. Are you sure?",
                                    Settings.Default.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                    DialogResult.No)
                    return;
            }
            var openDlg = new OpenFileDialog
            {
                Filter = "SpyStudio File (*.spy)|*.spy|Xml File (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = Settings.Default.LastDefaultLoadExtension == "xml" ? 2 : 1,
                Multiselect = false,
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Load Trace"
            };
            if (doFilter)
                openDlg.Title += " Filtered";
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedLogFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedLogFolder;

            if (openDlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                Settings.Default.PathLastSelectedLogFolder = Path.GetDirectoryName(openDlg.FileName);
                Settings.Default.Save();
            }
            catch (Exception)
            {
            }
            UpdateDefaultLoadExtension(openDlg.FileName);

            if (doFilter)
            {
                var eventFilter = new EventFilter(EventFilter.FilterForm.MainLoad, _devRunTrace.Filter)
                                      {Text = "Trace Filter"};
                eventFilter.ShowDialog(this);
            }

            bool success;
            string error;

            ClearAllDataSync();

            var result = _devRunTrace.LoadLog(this, openDlg.FileName, out success, out error);
            if (result == DialogResult.Cancel)
            {
                if (MessageBox.Show(this,
                                    "The load process was cancelled, but the Trace was partially loaded. Do you want to keep this partial data?",
                                    Settings.Default.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                    DialogResult.No)
                {
                    _devRunTrace.ClearOnLogLoadFinished();
                }
            }
            else if (!success)
            {
                MessageBox.Show(this, "Error loading Trace: " + error,
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                UpdateTitle(Path.GetFileName(openDlg.FileName));
            }
            GCTools.AsyncCollectDelayed(10000);
        }

        private void OpenLogToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenLogAndOptionallyFilter(false);
        }

        private void OpenLogfilteredToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenLogAndOptionallyFilter(true);
        }

        private void SaveLogToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_devRunTrace != null)
            {
                if (!PendingEventsCountManager.GetInstance().NoEventsLeft)
                {
                    if (MessageBox.Show(this,
                                        "SpyStudio hasn't finished processing all reported events. Do you still want to save this partial data?",
                                        Settings.Default.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                        DialogResult.No)
                    {
                        return;
                    }
                }

                var saveDlg = new SaveFileDialog
                    {
                        DefaultExt = "spy",
                        Filter = "SpyStudio File (*.spy)|*.spy|Xml File (*.xml)|*.xml|All files (*.*)|*.*",
                        //DefaultExt = "xml",
                        //Filter = "Xml File (*.xml)|*.xml|All files (*.*)|*.*",
                        AddExtension = true,
                        RestoreDirectory = true,
                        Title = "Save Trace"
                    };
                if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedLogFolder))
                    saveDlg.InitialDirectory = Settings.Default.PathLastSelectedLogFolder;
                if (saveDlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        Settings.Default.PathLastSelectedLogFolder = Path.GetDirectoryName(saveDlg.FileName);
                        Settings.Default.Save();
                    }
                    catch (Exception)
                    {
                    }

                    bool success;
                    string error;
                    var xmlMode = saveDlg.FileName.ToLower().EndsWith("xml");
                    var result = _devRunTrace.SaveLog(this, saveDlg.FileName, xmlMode, out success, out error);
                    if (!success)
                    {
                        if (result != DialogResult.Cancel)
                            MessageBox.Show(this, "Error saving Trace: " + error,
                                            Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        UpdateTitle(Path.GetFileName(saveDlg.FileName));
                    }
                }
            }
        }

        private void ClearDataToolStripMenuItemClick(object sender, EventArgs e)
        {
            ClearAllDataAsync();
        }

        private void CollectingDataToolStripMenuItemClick(object sender, EventArgs e)
        {
            _collectingData = collectingDataToolStripMenuItem.Checked;
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
            callDump.OpenFindDialog(e);
        }

        private void CopyToolStripMenuItemClick(object sender, EventArgs e)
        {
            callDump.Copy();
        }

        private void FilterToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_eventFilter != null && _eventFilter.Visible)
            {
                _eventFilter.Focus();
            }
            else
            {
                _eventFilter = new EventFilter(EventFilter.FilterForm.Main, _filter);
                _eventFilter.Show(this);
            }
        }

        #region ListViewProcess_ContextMenu

        public void ContextMenuProcessesOpening(object sender, CancelEventArgs e)
        {
            // enable delete only if there are selected items
            bool hook = false, unhook = false;
            if (listViewProcesses.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in listViewProcesses.SelectedItems)
                {
                    if (_hookedProcesses.Items.Contains(item))
                        unhook = true;
                    else if (((ProcessListViewItemInfo) item.Tag).Hookeable && _unhookedProcesses.Items.Contains(item))
                        hook = true;
                }
            }
            listViewProcesses.ContextMenuStrip.Items[0].Visible = unhook;
            listViewProcesses.ContextMenuStrip.Items[1].Visible = hook;
            listViewProcesses.ContextMenuStrip.Items[2].Visible = (hook || unhook);
            listViewProcesses.ContextMenuStrip.Items[3].Visible = listViewProcesses.SelectedItems.Count >= 1;
            listViewProcesses.ContextMenuStrip.Items[4].Visible = listViewProcesses.SelectedItems.Count == 1;
            listViewProcesses.ContextMenuStrip.Items[5].Visible = listViewProcesses.SelectedItems.Count == 1;

            if (!hook && !unhook)
                e.Cancel = true;
        }

        private void ProcessesProperties(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewProcesses.SelectedItems)
            {
                var procProperties = new ProcessProperties(((ProcessListViewItemInfo) item.Tag).Pid, _processInfo,
                                                           _hookMgr);
                procProperties.Show(this);
                break;
            }
        }

        private void ProcessTerminate(object sender, EventArgs e)
        {
            var items = listViewProcesses.SelectedItems;

            foreach (ListViewItem item in items)
            {
                var pid = (int) ((ProcessListViewItemInfo) item.Tag).Pid;
                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.Kill();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error terminating process: " + ex.Message,
                                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void HookToolStripMenuItemClick(object sender, EventArgs e)
        {
            //if (DeviareInitializer.ShowNonCommercialDialog())
            //{
            //    var trialDialog = new TrialDialog
            //    {
            //        Feature = "HP",
            //        MainText = "This is a NON-COMMERCIAL version. To use SpyStudio for commercial purposes buy a commercial license.",
            //        ExitButtonText = "Continue"
            //    };

            //    trialDialog.ShowDialog(this);
            //}

            if (!IsMonitoring)
                UpdateActiveGroups();

            foreach (
                var item in
                    listViewProcesses.SelectedItems.Cast<ListViewItem>().Where(p => p.Group.Equals(_unhookedProcesses)))
            {
                var info = (ProcessListViewItemInfo) item.Tag;
                _hookMgr.Attach(info.Pid, true);
            }
            UpdateButtonsState();
        }

        private void UnhookToolStripMenuItemClick(object sender, EventArgs e)
        {
            UnhookProcessesFromListView(
                listViewProcesses.SelectedItems.Cast<ListViewItem>().Where(p => p.Group.Equals(_hookedProcesses)));
        }

        private void UnhookProcessesFromListView(IEnumerable<ListViewItem> processesToUnhook)
        {
            foreach (var item in processesToUnhook)
            {
                var info = (ProcessListViewItemInfo) item.Tag;
                _hookMgr.Detach(info.Pid, true);
            }
        }

        #endregion ListViewProcess_ContextMenu

        private void FormMainKeyDown(object sender, KeyEventArgs e)
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
            else
            {
                bool controlIsPressed = ((uint) e.Modifiers & (uint) Keys.Control) == (uint) Keys.Control;
                bool shiftIsPressed = ((uint) e.Modifiers & (uint) Keys.Shift) == (uint) Keys.Shift;
                if (e.KeyCode == Keys.P && controlIsPressed && shiftIsPressed)
                {
                    var text = PendingEventsCountManager.GetInstance().GetTimeLineString();
                    if (!string.IsNullOrEmpty(text))
                        Clipboard.SetText(text);
                }
            }
        }

        private void FormMainSelectAll(object sender, EventArgs e)
        {
            callDump.SelectAll();
        }

        private void TreeViewRegistryOnExportRequest(List<RegistryTreeNode> registryNodes)
        {
            UpdateClass.Check("/spystudio/export-reg");

            var parentNodes = registryNodes;
            if (parentNodes.Count > 0)
            {
                var saveDlg = new SaveFileDialog
                    {
                        DefaultExt = "reg",
                        Filter = "Registry File (*.reg)|*.reg|All files (*.*)|*.*",
                        AddExtension = true,
                        RestoreDirectory = true,
                        Title = "Export Registry File"
                    };

                if (saveDlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    var filename = saveDlg.FileName;
                    using (var file = new StreamWriter(filename, false, Encoding.Unicode))
                    {
                        var keyInfos = callDump.treeViewRegistry.GetKeys(parentNodes);
                        if (!RegistryTools.ExportToReg(keyInfos, file))
                            MessageBox.Show(this, "Error Exporting Keys: Please report to Nektra Support",
                                            Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        file.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error Exporting Keys: " + ex.Message,
                                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private void AboutAppStudioToolStripMenuItemClick(object sender, EventArgs e)
        {
            var aboutForm = new AboutSpyStudio();
            aboutForm.SetVersionInfo("1.0");
            aboutForm.ShowDialog(this);
        }

        private void GroupManagerToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenGroupManager();
        }

        public delegate void OpenGroupManagerDelegate();

        public void OpenGroupManager()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new OpenGroupManagerDelegate(OpenGroupManager));
            }
            else
            {
                _groupManager = new FormMonitorGroupManager(_spyMgr);
                _groupManager.Closed += GroupManagerClosed;
                _groupManager.Show();

                //_groupManager.ShowDialog();
            }
        }

        private void GroupManagerClosed(object sender, EventArgs e)
        {
            _groupManager = null;
        }

        private void CompareTracesToolStripMenuItemClick(object sender, EventArgs e)
        {
            CompareTraces();
        }

        private void FilteredTracesToolStripMenuItemClick(object sender, EventArgs e)
        {
            CompareTraces();
        }

        private void CompareTraces()
        {
            string filename1 = "", filename2 = "";
            int i;

            UpdateClass.Check("/spystudio/compare");

            for (i = 0; i < 2; i++)
            {
                var openDlg = new OpenFileDialog
                    {
                        Filter = "SpyStudio File (*.spy)|*.spy|Xml File (*.xml)|*.xml|All files (*.*)|*.*",
                        FilterIndex = Settings.Default.LastDefaultLoadExtension == "xml" ? 2 : 1,
                        Multiselect = false,
                        AddExtension = true,
                        RestoreDirectory = true,
                        Title = i == 0 ? "Select first Trace to compare" : "Select second Trace to compare"
                    };
                if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedLogFolder))
                    openDlg.InitialDirectory = Settings.Default.PathLastSelectedLogFolder;

                if (openDlg.ShowDialog(this) != DialogResult.OK)
                    break;
                if (i == 0)
                    filename1 = openDlg.FileName;
                else
                    filename2 = openDlg.FileName;
                try
                {
                    Settings.Default.PathLastSelectedLogFolder = Path.GetDirectoryName(openDlg.FileName);
                    Settings.Default.Save();
                }
                catch (Exception)
                {
                }

                UpdateDefaultLoadExtension(openDlg.FileName);
            }
            //filename1 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\ie.xml";
            //filename2 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\ie2.xml";
            if (i >= 2)
            {
                var loadFilter = EventFilter.Filter.GetCompareWindowFilter();
                var eventFilter = new EventFilter(EventFilter.FilterForm.Compare, loadFilter)
                    {Text = "Compare Traces Filter"};
                eventFilter.ShowDialog(this);

                //loadFilter = applyFilter
                //                 ? EventFilter.Filter.GetCompareWindowLoadFilter()
                //                 : EventFilter.Filter.GetCompareWindowFilter();

                bool success;
                string error;

                var compare = new FormDeviareCompare();
                //if (applyFilter)
                //    compare.SetLoadFilter(loadFilter);
                var result = compare.LoadTrace(out success, out error, filename1, filename2);
                if (result == DialogResult.Abort)
                {
                    MessageBox.Show(this, "Error comparing traces: " + error,
                                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (result != DialogResult.Cancel)
                {
                    compare.Show();
                    compare.FormClosed += CompareOnFormClosed;
                }
                else
                {
                    compare.Dispose();
                    GCTools.AsyncCollectDelayed(10000);
                }
            }
        }

        private void CompareOnFormClosed(object sender, FormClosedEventArgs formClosedEventArgs)
        {
            var compare = sender as FormDeviareCompare;

            Debug.Assert(compare != null, "Expected a FormDeviareCompare object but got something else.");

            if (compare == null) 
                return;

            compare.FormClosed -= CompareOnFormClosed;
    
            compare.Dispose();
            compare = null;
            sender = null;

            GCTools.AsyncCollectDelayed(10 * 1000);
        }

        private void CompareStatisticsToolStripMenuItemClick(object sender, EventArgs e)
        {
            string filename1 = "", filename2 = "";

            int i;
            for (i = 0; i < 2; i++)
            {
                var openDlg = new OpenFileDialog
                    {
                        Filter = "SpyStudio File (*.spy)|*.spy|Xml File (*.xml)|*.xml|All files (*.*)|*.*",
                        FilterIndex = Settings.Default.LastDefaultLoadExtension == "xml" ? 2 : 1,
                        Multiselect = false,
                        AddExtension = true,
                        RestoreDirectory = true,
                        Title = i == 0 ? "Select first Trace to compare" : "Select second Trace to compare"
                    };
                if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedLogFolder))
                    openDlg.InitialDirectory = Settings.Default.PathLastSelectedLogFolder;

                if (openDlg.ShowDialog(this) != DialogResult.OK)
                    break;
                if (i == 0)
                    filename1 = openDlg.FileName;
                else
                    filename2 = openDlg.FileName;
                try
                {
                    Settings.Default.PathLastSelectedLogFolder = Path.GetDirectoryName(openDlg.FileName);
                    Settings.Default.Save();
                }
                catch (Exception)
                {
                }
                UpdateDefaultLoadExtension(openDlg.FileName);
            }
            if (i >= 2)
            {
                var statsDlg = new StatsReport();
                statsDlg.CompareFiles(this, filename1, filename2);
            }
        }

        private void StatisticsToolStripMenuItem1Click(object sender, EventArgs e)
        {
            var statsDlg = new StatsReport();
            statsDlg.SetData(_devRunTrace);
            statsDlg.Show(this);
        }

        private void HookNewProcessesToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (!IsMonitoring)
                UpdateActiveGroups();

            _hookMgr.HookNewProcesses = !_hookMgr.HookNewProcesses;
            UpdateButtonsState();
        }

        private void TextBoxProgramExecKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) 13)
                ToolStripMenuItemStartClick(sender, e);
        }

        private void ClearFailedHooksToolStripMenuItemClick(object sender, EventArgs e)
        {
            var processesFailedToHook = _hookedProcesses.Items.Cast<ListViewItem>().
                Where(item => AllHooksFailedForProcessWithPid((int) ((ProcessListViewItemInfo) item.Tag).Pid));

            UnhookProcessesFromListView(processesFailedToHook);
        }

        private void ToolStripMenuItemFileSystemFlatModeClick(object sender, EventArgs e)
        {
            if (!toolStripMenuItemFileSystemFlatMode.Checked)
            {
                toolStripMenuItemFileSystemTreeMode.Checked = false;
                toolStripMenuItemFileSystemFlatMode.Checked = true;
                Settings.Default.FileSystemShowTree = false;
                Settings.Default.Save();
                callDump.listViewFileSystem.TreeMode = Settings.Default.FileSystemShowTree;
                RefreshFileSystemEvents();
            }
        }

        private void ToolStripMenuItemFileSystemTreeModeClick(object sender, EventArgs e)
        {
            if (toolStripMenuItemFileSystemTreeMode.Checked) return;

            toolStripMenuItemFileSystemTreeMode.Checked = true;
            toolStripMenuItemFileSystemFlatMode.Checked = false;
            Settings.Default.FileSystemShowTree = true;
            Settings.Default.Save();
            callDump.listViewFileSystem.TreeMode = Settings.Default.FileSystemShowTree;
            RefreshFileSystemEvents();
        }

        private void GoToToolStripMenuItemClick(object sender, EventArgs e)
        {
            callDump.OpenGoToDialog();
        }

        private void RefreshFileSystemEvents()
        {
            var selectedTab = callDump.SelectedTabIndex;
            _devRunTrace.RefreshFileSystemEvents();
            callDump.SelectedTabIndex = selectedTab;
        }

        private void RefreshRegistryEvents()
        {
            var selectedTab = callDump.SelectedTabIndex;
            _devRunTrace.RefreshRegistryEvents();
            callDump.SelectedTabIndex = selectedTab;
        }

        private void ShowLayerPathsToolStripMenuItemClick(object sender, EventArgs e)
        {
            callDump.listViewFileSystem.MergeLayerPaths = !showLayerPathsToolStripMenuItem.Checked;
            Settings.Default.FileSystemMergeLayerPaths = callDump.listViewFileSystem.MergeLayerPaths;
            Settings.Default.Save();
            RefreshFileSystemEvents();
        }

        private void MergeWowPathsToolStripMenuItemClick(object sender, EventArgs e)
        {
            callDump.listViewFileSystem.MergeWowPaths = mergeWowPathsToolStripMenuItem.Checked;
            Settings.Default.FileSystemMergeWowPaths = callDump.listViewFileSystem.MergeWowPaths;
            Settings.Default.Save();
            RefreshFileSystemEvents();
        }

        private void ShowModulesLoadedAtStartupToolStripMenuItemClick(object sender, EventArgs e)
        {
            callDump.listViewFileSystem.ShowStartupModules = showStartupModulesToolStripMenuItem.Checked;
            Settings.Default.FileSystemShowStartupModules = callDump.listViewFileSystem.ShowStartupModules;
            Settings.Default.Save();
            RefreshFileSystemEvents();
        }

        private void ToolStripMenuItemAttribtuesClick(object sender, EventArgs e)
        {
            callDump.listViewFileSystem.HideQueryAttributes = toolStripMenuItemHideAttributes.Checked;
            Settings.Default.FileSystemHideQueryAttributes = callDump.listViewFileSystem.HideQueryAttributes;
            Settings.Default.Save();
            RefreshFileSystemEvents();
        }

        private void MergeComClassesToolStripMenuItemClick(object sender, EventArgs e)
        {
            Settings.Default.RegistryMergeClassPaths =
                callDump.treeViewRegistry.RedirectClasses = mergeCOMClassesToolStripMenuItem.Checked;
            Settings.Default.Save();
            RefreshRegistryEvents();
        }

        private void MergeWowKeyPathsToolStripMenuItemClick(object sender, EventArgs e)
        {
            Settings.Default.RegistryMergeWowPaths =
                callDump.treeViewRegistry.MergeWow = mergeWowKeyPathsToolStripMenuItem.Checked;
            Settings.Default.Save();
            RefreshRegistryEvents();
        }

        private void ExecutionPropertiesToolStripMenuItemClick(object sender, EventArgs e)
        {
            var props = new ExecutionProperties(Settings.Default.LastExecuteParameters, Settings.Default.RunAsUserName,
                                                Settings.Default.RunAsPassword) {ProgramPath = textBoxProgramExec.Text};
            var result = props.ShowDialog(this);
            switch (result)
            {
                case ExecutionProperties.ShowDialog2Result.Save:
                case ExecutionProperties.ShowDialog2Result.SaveAndExec:
                    SaveExecuteProperties(props.ProgramPath, props.Parameters, props.User, props.Password);
                    if (result == ExecutionProperties.ShowDialog2Result.SaveAndExec)
                        ExecuteProgramAndHook(props.ProgramPath, props.Parameters, props.User, props.Password, false);
                    break;
                case ExecutionProperties.ShowDialog2Result.Cancel:
                    break;
            }
            props.Dispose();
        }

        private void SaveExecuteProperties(string path, string para, string user, string pass)
        {
            textBoxProgramExec.Text = path;
            Settings.Default.LastExecutePath = path;
            Settings.Default.LastExecuteParameters = para;
            Settings.Default.RunAsUserName = user;
            Settings.Default.RunAsPassword = pass;
            Settings.Default.Save();
        }

        private void ExecuteProgramAndHook(string programPath, string parameters, string user, string password,
                                           bool installer)
        {

            if (programPath != "")
            {
                string cmdLine;
                if (string.IsNullOrEmpty(parameters))
                    cmdLine = "\"" + programPath + "\"";
                else
                    cmdLine = "\"" + programPath + "\" " + parameters;

                UpdateActiveGroups();

                if (!_hookMgr.ExecuteProgramAndHook(cmdLine, user, password, installer))
                {
                    MessageBox.Show(this, "Error executing program",
                                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    SaveExecuteProperties(programPath, parameters, user, password);
                }
            }
            UpdateButtonsState();
        }

        private void ButtonPlayClick(object sender, EventArgs e)
        {
            TestPathAndExecute(false);
        }

        private void PropertiesToolStripMenuItemClick(object sender, EventArgs e)
        {
            callDump.ShowItemProperties();
        }

        private const string HelpUrl = "http://whiteboard.nektra.com/spystudio-2-0-quickstart?SP";

        private void HelpToolStripMenuItemClick(object sender, EventArgs e)
        {
            Process.Start(HelpUrl);
        }

        private void FullStackInfoToolStripMenuItemClick(object sender, EventArgs e)
        {
            Settings.Default.FullStackInfo = 
            _hookMgr.EnableStackLogging = fullStackInfoToolStripMenuItem.Checked;
        }

        private void CreateOrEditTemplateToolStripMenuItemClick(object sender, EventArgs e)
        {
            DoExport(WizardFactory.CreateEditorWizardFor(new SwvTemplateEditorExport(_devRunTrace)));
        }
    }
}