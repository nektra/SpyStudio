using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.Tools;
using SpyStudio.Trace;
using Wizard.UI;
using System.Linq;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    public partial class ThinAppEntryPointSelect : InternalWizardPage
    {
        protected ExportWizard Wizard;
        protected VirtualizationExport Export;

        protected ExportField<DeviareRunTrace> Trace;
        protected ExportField<IEnumerable<FileEntry>> Files;
        protected ExportField<IEnumerable<ThinAppEntryPoint>> EntryPoints;

        private string _mainProcessName, _mainProcessPath;
        private BackgroundWorker _worker;

        public ThinAppEntryPointSelect(ExportWizard aWizard, VirtualizationExport anExport)
        {
            Wizard = aWizard;
            Export = anExport;

            Trace = anExport.GetField<DeviareRunTrace>(ExportFieldNames.Trace);
            Files = anExport.GetField<IEnumerable<FileEntry>>(ExportFieldNames.OriginalFiles);
            EntryPoints = anExport.GetField<IEnumerable<ThinAppEntryPoint>>(ExportFieldNames.EntryPoints);

            WizardNext += OnWizardNext;
            
            InitializeComponent();
        }

        protected IEnumerable<ThinAppEntryPoint> CheckedEntryPoints
        {
            get
            {
                var checkedItems = _entryPointListView.CheckedItems.Cast<ListViewItem>();
                return checkedItems.Select(x => (ThinAppEntryPoint) x.Tag).ToList();
            }
        }

        private void OnWizardNext(object sender, WizardPageEventArgs e)
        {
            EntryPoints.Value = CheckedEntryPoints;
        }

        private void OnWorkerCompletion(IEnumerable<ListViewItem> entryPoints, RunWorkerCompletedEventArgs args)
        {
            if (args.Cancelled)
                return;

            _entryPointListView.BeginUpdate();
            foreach (var listViewItem in entryPoints)
            {
                _entryPointListView.Items.Add(listViewItem);

                if (!args.Cancelled) 
                    continue;

                _entryPointListView.EndUpdate();
                return;
            }

            _entryPointListView.EndUpdate();

            foreach (var entryPoint in ((ThinAppCapture)Wizard.VirtualPackage).EntryPoints)
            {
                if (_entryPointListView.Items.ContainsKey(entryPoint.Location))
                    _entryPointListView.Items[entryPoint.Location].Checked = true;
            }

            _entryPointListView.ItemChecked += (s, a) => ((ThinAppExport)Export).EntryPointsWereUpdated = true;

            if (args.Cancelled)
                return;

            EnableNextButton(true);
        }

        private ListViewItem MakeEntryPointListViewItem(ThinAppEntryPoint epoint)
        {
            var item = new ListViewItem(epoint.Name);
            item.Name = epoint.Location;
            item.SubItems.Add(epoint.Location);
            item.Tag = epoint;
            item.Checked = epoint.LikelyMainEntryPoint;
            if (!item.Checked || !string.IsNullOrEmpty(_mainProcessName))
            {
                var epointName = epoint.Name.ToLower();
                item.Checked = epointName == _mainProcessName || _mainProcessPath.EndsWith(epointName);
            }
            return item;
        }

        private void LoadEntryPoints(ICollection<ListViewItem> resultList, DoWorkEventArgs args)
        {
            this.ExecuteInUIThreadAsynchronously(() => Cursor = Cursors.WaitCursor);
            this.ExecuteInUIThreadAsynchronously(() => UserInput.Disable(this));

            var entryPoints = ThinAppEntryPointFinder.Find(Export.TraceId, Files.Value, Trace.Value, _worker);

            if (_worker.CancellationPending)
            {
                args.Cancel = true;
                return;
            }

            foreach (var entryPoint in entryPoints.Select(x => MakeEntryPointListViewItem(x)))
            {
                resultList.Add(entryPoint);

                if (_worker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }
            }

            this.ExecuteInUIThreadAsynchronously(() => Cursor = Cursors.Default);
            this.ExecuteInUIThreadAsynchronously(() => UserInput.Enable(this));

            Export.EntryPointsNeedUpdate = false;
        }

        private void EntryPointListViewSizeChanged(object sender, EventArgs e)
        {
            var columns = _entryPointListView.Columns.Cast<ColumnHeader>().ToList();

            var fixedColumnsWidth = columns.Sum(c => c.Width) - columns.Last().Width;
            
            columns.Last().Width = _entryPointListView.ClientSize.Width - fixedColumnsWidth;
        }

        private void ThinAppEntryPointSelectSetActive(object sender, CancelEventArgs e)
        {
            if (!Export.EntryPointsNeedUpdate) 
                return;

            this.ExecuteInUIThreadAsynchronously(() => EnableNextButton(false));

            var appBehavior = Export.GetField<ApplicationBehaviourAnalyzer>(ExportFieldNames.ApplicationBehaviourAnalizer);
            var val = appBehavior.Value;
            _mainProcessPath = val.MainProcessFileName == null ? string.Empty : val.MainProcessFileName.ToLower();
            _mainProcessName = val.MainProcessName == null ? string.Empty : val.MainProcessName.ToLower();

            _worker = new BackgroundWorker { WorkerReportsProgress = true };
            var resultList = new List<ListViewItem>();
            _worker.DoWork += ((unused1, args) => LoadEntryPoints(resultList, args));
            _worker.RunWorkerCompleted += (x, args) => this.ExecuteInUIThreadAsynchronously(() => OnWorkerCompletion(resultList, args));
            _worker.WorkerSupportsCancellation = true;
            _entryPointListView.Items.Clear();
            _worker.RunWorkerAsync();
        }

        private void ThinAppEntryPointSelectQueryCancel(object sender, CancelEventArgs e)
        {
            if (_worker != null)
                _worker.CancelAsync();
        }
    }
}
