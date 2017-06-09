using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Export.ThinApp;
using SpyStudio.Tools;
using SpyStudio.Trace;
using Wizard.UI;
using System.Linq;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    public partial class EntryPointSelect : TemplatedVirtualizationPage
    {
        protected ExportWizard Wizard;
        protected VirtualizationExport Export;

        protected ExportField<DeviareRunTrace> Trace;
        protected ExportField<IEnumerable<FileEntry>> Files;
        protected ExportField<IEnumerable<EntryPoint>> EntryPoints;

        private List<string> _processNames, _processPaths;
        private BackgroundWorker _worker;

        public EntryPointSelect(ExportWizard aWizard, VirtualizationExport anExport, string description): base(description, anExport)
        {
            Wizard = aWizard;
            Export = anExport;

            _processPaths = new List<string>();
            _processNames = new List<string>();

            Trace = anExport.GetField<DeviareRunTrace>(ExportFieldNames.Trace);
            Files = anExport.GetField<IEnumerable<FileEntry>>(ExportFieldNames.OriginalFiles);
            EntryPoints = anExport.GetField<IEnumerable<EntryPoint>>(ExportFieldNames.EntryPoints);

            WizardNext += OnWizardNext;
            
            InitializeComponent();
        }

        protected IEnumerable<EntryPoint> CheckedEntryPoints
        {
            get
            {
                var checkedItems = _entryPointListView.CheckedItems.Cast<ListViewItem>();
                return checkedItems.Select(x => (EntryPoint) x.Tag).ToList();
            }
        }

        private void OnWizardNext(object sender, WizardPageEventArgs e)
        {
            VirtualizationTemplate.Value.SaveEntryPoints(_entryPointListView.Items.Cast<ListViewItem>());
            EntryPoints.Value = CheckedEntryPoints;
            EnableNextButton(false);
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
                {
                    _entryPointListView.Items[entryPoint.Location].Tag = entryPoint;
                    _entryPointListView.Items[entryPoint.Location].Checked = true;
                    continue;
                }

                var item = _entryPointListView.Items.Add(MakeEntryPointListViewItem(entryPoint, Export.CheckerType));
                item.Checked = true;
            }

            _entryPointListView.ItemChecked += (s, a) => ((ThinAppExport)Export).EntryPointsWereUpdated = true;

            if (args.Cancelled)
                return;

            EnableNextButton(true);
        }

        private ListViewItem MakeEntryPointListViewItem(EntryPoint epoint, CheckerType type)
        {
            var item = new ListViewItem(epoint.Name)
                           {
                               Name = epoint.Location,
                               Tag = epoint,
                               Checked = epoint.LikelyMainEntryPoint || epoint.Checked,
                           };
            item.SubItems.Add(epoint.Location);
            if (type == CheckerType.Application && (!item.Checked || _processNames.Any()))
            {
                var epointName = epoint.Name.ToLower();
                item.Checked = _processNames.Any(name => name.Equals(epointName)) ||
                               _processPaths.Any(path => path.EndsWith(epointName));
            }
            return item;
        }

        private List<ListViewItem> _resultList;

        void SortResultList()
        {
            _resultList.Sort((x, y) =>
            {
                if (x.Checked && !y.Checked)
                    return -1;
                if (y.Checked && !x.Checked)
                    return 1;
                return 0;
            });
        }

        private void RestoreEntryPoints()
        {
            var list = VirtualizationTemplate.Value.EntryPoints;
            foreach (var entryPoint in list)
            {
                var name = entryPoint.Name;
                if (name == null)
                    FileSystemTools.GetFileName(entryPoint.Location);
                var fileSystemLocation = GeneralizedPathNormalizer.GetInstance().Unnormalize(entryPoint.Location);
                var location = ThinAppPathNormalizer.GetInstance().Normalize(fileSystemLocation);
                entryPoint.Name = name;
                entryPoint.FileSystemLocation = fileSystemLocation;
                entryPoint.Location = location;
                _resultList.Add(MakeEntryPointListViewItem(entryPoint, CheckerType.None));
            }
        }

        private void LoadEntryPoints(ICollection<ListViewItem> resultList, DoWorkEventArgs args)
        {
            this.ExecuteInUIThreadSynchronously(() =>
                                                     {
                                                         Cursor = Cursors.WaitCursor;
                                                         this.DisableUI();
                                                     });

            if (VirtualizationTemplate.Value.IsInUse)
            {
                _resultList = new List<ListViewItem>();
                RestoreEntryPoints();
            }
            else
            {
                var entryPoints = ThinAppEntryPointFinder.Find(Trace.Value, Files.Value, _worker);

                if (_worker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

                _resultList = entryPoints.Select(x => MakeEntryPointListViewItem(x, Export.CheckerType)).ToList();
            }

            SortResultList();

            foreach (var entryPoint in _resultList)
            {
                resultList.Add(entryPoint);

                if (_worker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }
            }

            this.ExecuteInUIThreadSynchronously(() =>
                                                     {
                                                         Cursor = Cursors.Default;
                                                         this.EnableUI();
                                                     });

            Export.EntryPointsNeedUpdate = false;
        }

        private void EntryPointListViewSizeChanged(object sender, EventArgs e)
        {
            var columns = _entryPointListView.Columns.Cast<ColumnHeader>().ToList();

            var fixedColumnsWidth = columns.Sum(c => c.Width) - columns.Last().Width;
            
            columns.Last().Width = _entryPointListView.ClientSize.Width - fixedColumnsWidth;
        }

        private void ThinAppEntryPointSelectSetActive(object sender, WizardPageEventArgs e)
        {
            if (e.IsBackActionIn(Wizard))
                return;

            if (!Export.EntryPointsNeedUpdate) 
                return;

            var appBehaviors = Export.GetField<IEnumerable<AppBehaviourAnalyzer>>(ExportFieldNames.ApplicationBehaviourAnalizers);

            foreach (var appBehavior in appBehaviors.Value)
            {
                if (appBehavior.ProcessFileName != null)
                    _processPaths.Add(appBehavior.ProcessFileName.ToLower());

                if (appBehavior.ProcessName != null)
                    _processNames.Add(appBehavior.ProcessName.ToLower());
            }

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
