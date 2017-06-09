using System.Globalization;
using SpyStudio.Database;
using SpyStudio.Export;
using SpyStudio.FileSystem;
using SpyStudio.Hooks;
using System.Linq;
using SpyStudio.Tools;
using SpyStudio.Extensions;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class ExeSelect : TemplatedVirtualizationPage
    {
        #region Properties

        protected readonly VirtualizationExport Export;
        protected ExportWizard Wizard { get { return (ExportWizard)GetWizard(); } }

        #endregion

        #region Instantiation and Initialization

        public ExeSelect(VirtualizationExport anExport)
            : base("Select the main executable files for the applications you want to virtualize.", anExport)
        {
            Export = anExport;

            InitializeComponent();

            ExeFilesView.ArrangeForExportWizard();
            ExeFilesView.CheckBoxes = true;
        }

        #endregion

        #region Event Handlers

        protected void ExeSelectSetActive(object sender, WizardPageEventArgs e)
        {
            if (e.IsBackActionIn(Wizard))
                return;

            UseWaitCursor = true;
            this.DisableUI();

            ExeFilesView.OnNodeCheckChanged += args =>
                {
                    var readyToProceed = ExeFilesView.GetCheckedItems().Any(i => !i.IsDirectory);
                    this.ExecuteInUIThreadSynchronously(() => EnableNextButton(readyToProceed));
                };

            Threading.ExecuteAsynchronously( () =>
            {
                LoadExeFilesFromDB();
                LoadExeFilesFromVirtualPackage();
                CheckProbableMainExeFiles();
                ExeFilesView.ExpandAll();
                this.ExecuteInUIThreadSynchronously(() =>
                    {
                        this.EnableUI();
                        UseWaitCursor = false;
                    });
            });
        }

        private void CheckProbableMainExeFiles()
        {
            foreach (var node in ExeFilesView.TreeView.AllModelNodes)
            {
                if ((node.Access & FileSystemAccess.CreateProcess) != 0)
                {
                    node.Checked = true;
                    return;
                }

                TryVirtualizationToolSpecificCriteriaOn(node);
            }
        }

        protected virtual void TryVirtualizationToolSpecificCriteriaOn(FileSystemTreeNode node)
        {
            // Subclass responsibility
            throw new System.NotImplementedException();
        }

        protected void ExeSelectWizardNext(object sender, Wizard.UI.WizardPageEventArgs e)
        {
            var checkedEntries = ExeFilesView.TreeView.AllModelNodes.Where(n => !n.IsDirectory && n.IsChecked).Select(n => n.ToFileEntry());

            Export.SetFieldValue(ExportFieldNames.ApplicationBehaviourAnalizers, checkedEntries.Select(i => AppBehaviourAnalyzer.For(i)).ToList());
            EnableNextButton(false);
        }

        #endregion

        #region Control

        protected void LoadExeFilesFromVirtualPackage()
        {
            foreach (var file in Wizard.VirtualPackage.Files.Where(f => f.Path.EndsWith(".exe", true, CultureInfo.InvariantCulture)))
                ExeFilesView.AddFileEntryUncolored(file);
        }

        protected void LoadExeFilesFromDB()
        {
            var refreshData = new EventsReportData(Export.TraceId)
            {
                ControlInvoker = this,
                EventsToReport = EventType.FileSystem,
                ReportBeforeEvents = false,
                EventResultsIncluded = EventsReportData.EventResult.Success
            };
            refreshData.EventsReady += RefreshDataOnEventsReady;
            EventDatabaseMgr.GetInstance().RefreshEvents(refreshData);
            EventDatabaseMgr.GetInstance().WaitProcessEvents(refreshData);
        }

        #endregion

        #region Utils

        protected void RefreshDataOnEventsReady(object sender, EventsRefreshArgs e)
        {
            foreach (var callEvent in e.Events.Where(call => 
                (call.ParamMain != null && call.ParamMain.EndsWith(".exe", true, CultureInfo.InvariantCulture))
                || (call.ParamCount > 0 && call.Params[0].Value.EndsWith(".exe", true, CultureInfo.InvariantCulture))))
                ExeFilesView.AddEvent(callEvent, null);
        }

        #endregion
    }
}
