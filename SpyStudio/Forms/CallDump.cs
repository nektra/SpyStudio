using System;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Main;
using SpyStudio.Tools;
using SpyStudio.Trace;
using System.Linq;

namespace SpyStudio.Forms
{
    public partial class CallDump : UserControl, IInterpreterController
    {
        protected EntryContextMenu ValuesEntryProperties;

        public CallDump()
        {
            InitializeComponent();

            listViewCom.Controller = this;
            listViewFileSystem.Controller = this;
            listViewValues.Controller = this;
            listViewWindow.Controller = this;
            treeViewRegistry.Controller = this;
            treeViewTrace.Controller = this;

            tabCom.Tag = listViewCom;
            tabFile.Tag = listViewFileSystem;
            tabWindow.Tag = listViewWindow;
            tabRegistry.Tag = treeViewRegistry;
            tabTrace.Tag = treeViewTrace;

            ResetTabs();

            treeViewRegistry.ValuesView = listViewValues;
            //registryTreeView.CheckBoxes = true;
            
            //listViewFileSystem.InitializeComponent();
            treeViewTrace.InitializeComponent();
            treeViewTrace.ShowLines = false;
            treeViewTrace.SetEventSummary(eventSummary);
            listViewCom.InitializeComponent();
            listViewWindow.InitializeComponent();

            listViewFileSystem.MergeLayerPaths = Properties.Settings.Default.FileSystemMergeLayerPaths;
            listViewFileSystem.MergeWowPaths = Properties.Settings.Default.FileSystemMergeWowPaths;
            treeViewRegistry.MergeLayerPaths = Properties.Settings.Default.FileSystemMergeLayerPaths;
            tabControlData.SelectedTab = tabTrace;

            ValuesEntryProperties = new EntryContextMenu(treeViewRegistry.ValuesView);
        }

        public void Attach(DeviareRunTrace devRunTrace)
        {
            treeViewRegistry.Attach(devRunTrace);
            listViewCom.Attach(devRunTrace);
            listViewWindow.Attach(devRunTrace);
            listViewFileSystem.AttachTo(devRunTrace);
            treeViewTrace.Attach(devRunTrace);

            devRunTrace.WindowClear += RemoveWindowTab;
            devRunTrace.ComClear += RemoveComTab;
            devRunTrace.FileSystemClear += RemoveFileTab;
            devRunTrace.RegistryClear += RemoveRegistryTab;
            devRunTrace.FirstCoCreateEvent += FirstCoCreateEvent;
            devRunTrace.FirstFileSystemEvent += FirstFileSystemEvent;
            devRunTrace.FirstRegistryEvent += FirstRegistryEvent;
            devRunTrace.FirstWindowEvent += FirstWindowEvent;
        }

        public delegate void ShowTabDelegate(TabPage page);

        private void ShowTab(TabPage page)
        {
            if (tabControlData.InvokeRequired)
            {
                var async = tabControlData.BeginInvoke(new ShowTabDelegate(ShowTab), page);
                tabControlData.EndInvoke(async);
            }
            else
            {
                var index = 0;

                if ((page == tabWindow || page == tabFile || page == tabRegistry) && tabControlData.TabPages.Contains(tabCom))
                    index++;
                if ((page == tabFile || page == tabRegistry) && tabControlData.TabPages.Contains(tabWindow))
                    index++;
                if ((page == tabRegistry) && tabControlData.TabPages.Contains(tabFile))
                    index++;

                tabControlData.TabPages.Insert(index, page);
            }
        }
        #region IInterpreterController

        public void ShowInCom(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowIn(tabControlData, tabCom, listViewCom, anEntry);
        }
        public void ShowInWindows(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowIn(tabControlData, tabWindow, listViewWindow, anEntry);
        }
        public void ShowInFiles(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowIn(tabControlData, tabFile, listViewFileSystem, anEntry);
        }
        public void ShowInRegistry(ITraceEntry anEntry)
        {
            CallEventContainerTools.ShowInRegistry(tabControlData, tabRegistry, treeViewRegistry, anEntry);
        }

        public bool ShowQueryAttributesInFiles
        {
            get { return !listViewFileSystem.HideQueryAttributes; }
        }

        public bool ShowDirectoriesInFiles
        {
            get { return listViewFileSystem.TreeMode; }
        }

        public bool PropertiesGoToVisible { get { return true; } }

        #endregion IInterpreterController

        private void FirstCoCreateEvent(object sender, EventArgs e)
        {
            ShowTab(tabCom);
        }

        private void FirstWindowEvent(object sender, EventArgs e)
        {
            ShowTab(tabWindow);
        }

        private void FirstRegistryEvent(object sender, EventArgs e)
        {
            ShowTab(tabRegistry);
        }

        private void FirstFileSystemEvent(object sender, EventArgs e)
        {
            ShowTab(tabFile);
        }
        private void ResetTabs()
        {
            tabControlData.TabPages.Remove(tabCom);
            tabControlData.TabPages.Remove(tabWindow);
            tabControlData.TabPages.Remove(tabFile);
            tabControlData.TabPages.Remove(tabRegistry);
        }
        private void RemoveComTab()
        {
            tabControlData.TabPages.Remove(tabCom);
        }
        private void RemoveWindowTab()
        {
            tabControlData.TabPages.Remove(tabWindow);
        }
        private void RemoveFileTab()
        {
            tabControlData.TabPages.Remove(tabFile);
        }
        private void RemoveRegistryTab()
        {
            if (ValuesEntryProperties != null)
                ValuesEntryProperties.Close(false);
            tabControlData.TabPages.Remove(tabRegistry);
        }
        public int SelectedTabIndex
        {
            get { return tabControlData.SelectedIndex; }
            set
            {
                if (tabControlData.SelectedIndex < tabControlData.TabPages.Count)
                    tabControlData.SelectedIndex = value;
            }
        }

        public void OpenFindDialog(FindEventArgs e)
        {
            var currentControl = tabControlData.SelectedTab.Tag as IInterpreter;
            if(currentControl != null)
            {
                currentControl.FindEvent(e);
            }
        }
        public void Copy()
        {
            var currentControl = tabControlData.SelectedTab.Tag as IInterpreter;
            if (currentControl != null)
            {
                currentControl.CopySelectionToClipboard();
            }
        }
        public void SelectAll()
        {
            var currentControl = tabControlData.SelectedTab.Tag as IInterpreter;
            if (currentControl != null)
            {
                currentControl.SelectAll();
            }
        }
        public void ShowItemProperties()
        {
            var currentControl = tabControlData.SelectedTab.Tag as IInterpreter;
            if (currentControl != null)
            {
                currentControl.ContextMenuController.ShowItemProperties();
            }
        }

        private void OnColumnClicked(object sender, TreeColumnEventArgs e)
        {
            
            //var model = (treeViewTrace.Model as SortedTreeModel);
            //var comparer = (model.Comparer as ListViewColumnSorter);

            //if (comparer == null)
            //    model.Comparer = comparer = new ListViewColumnSorter()
            //                        {
            //                            SortColumn = e.Column.Index,
            //                            View = treeViewTrace
            //                        };

            //if (e.Column.SortOrder == SortOrder.None || e.Column.SortOrder == SortOrder.Descending)
            //{
            //    e.Column.SortOrder = SortOrder.Ascending;
            //    comparer.Order = SortOrder.Ascending;
            //}
            //else
            //{
            //    e.Column.SortOrder = SortOrder.Descending;
            //    comparer.Order = SortOrder.Descending;
            //}

            //(treeViewTrace.Model as SortedTreeModel).Comparer = comparer;
        }

        public void OpenGoToDialog()
        {
            tabControlData.
                Controls.Cast<Control>().
                Where(c => c is CallDumpTabPage).
                Cast<CallDumpTabPage>().
                First(c => c.Visible).
                GoToClicked();
        }

        public bool CanDoGoTo
        {
            get { return (tabControlData.SelectedTab as CallDumpTabPage).CanDoGoTo; }
        }

        private void TabControlDataSelected(object sender, TabControlEventArgs e)
        {
            TriggerOnTabSelectedEvent(e.TabPage as CallDumpTabPage);
        }

        public delegate void TabSelectedHandler(CallDumpTabPage aTabPage);
        public event TabSelectedHandler OnTabSelected;

        public void TriggerOnTabSelectedEvent(CallDumpTabPage aTabPage)
        {
            var handler = OnTabSelected;
            if (handler != null) handler(aTabPage);
        }

        private static bool NodeUnreadable(TraceTreeView.TraceNode node)
        {
            return node.BeforeEventId == null && node.AfterEventId == null;
        }

        private static bool BeforeOrAfterHasCallNumber(TraceTreeView.TraceNode node, ulong callNumber)
        {
            return node.HasEvent(callNumber);
        }

        public void SelectItemWithCallEventId(CallEventId aCallEventId)
        {
            //Find node by matching call number:
            RecursiveNodeSearch<TraceTreeView.TraceNode>.SearchTerm f = node =>
                !NodeUnreadable(node) && BeforeOrAfterHasCallNumber(node, aCallEventId.CallNumber);
            var child = RecursiveNodeSearch<TraceTreeView.TraceNode>.FindNode(f, treeViewTrace.Model.Root);

            if(child != null)
            {
                treeViewTrace.SelectedNode = child;
                treeViewTrace.EnsureVisible(child, TreeViewAdv.ScrollType.Middle);
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

    }
}
