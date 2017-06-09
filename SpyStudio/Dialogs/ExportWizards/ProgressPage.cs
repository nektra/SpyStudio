using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using SpyStudio.Database;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Properties;
using SpyStudio.Trace;
using Wizard.UI;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class ProgressPage : InternalWizardPage
    {
        #region Fields

        private Button _saveTemplateButton;
        private Button _save2TemplateButton;

        #endregion

        #region Properties

        protected BackgroundWorker Worker;
        protected WizardSheet Wizard;
        protected VirtualizationExport Export;
        protected Exporter Exporter;

        #endregion

        #region Instantiation and Initialization

        // For Designer to work on subclasses
        public ProgressPage()
        {
            InitializeComponent();
        }

        public ProgressPage(WizardSheet aWizard, VirtualizationExport export)
        {
            Wizard = aWizard;
            Export = export;

            InitializeComponent();

            WizardBack += (sender, e) => Exporter.Stop();
            QueryCancel += (sender, e) => Exporter.Stop();
            WizardFinish += OnWizardNext;
            _logListView.Enabled = true;
        }

        private void InitializeSettingsButton()
        {
            if (_saveTemplateButton != null)
                return;
            _saveTemplateButton = new Button
            {
                Parent = GetWizard().ButtonPanel,
                Text = "Sa&ve",
                Visible = false,
                Location = new Point(8, 8),
            };
            new ToolTip().SetToolTip(_saveTemplateButton, "Save virtualization settings to a file.");
            _saveTemplateButton.Click += OnSaveTemplateButtonClick;

            _save2TemplateButton = new Button
            {
                Parent = GetWizard().ButtonPanel,
                Text = "Save space-&optimized",
                Visible = false,
                Location = new Point(8 + _saveTemplateButton.Size.Width + 8, 8),
                AutoSize = true,
            };
            new ToolTip().SetToolTip(_save2TemplateButton, "Save virtualization settings to a file, minimizing size.");
            _save2TemplateButton.Click += OnSave2TemplateButtonClick;
        }

        #endregion

        #region Event Handling

        private void Save()
        {
            var state = Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value;
            state.SaveWithDialog(this);
        }

        private void OnSaveTemplateButtonClick(object sender, EventArgs e)
        {
            Save();
        }

        private void OnSave2TemplateButtonClick(object sender, EventArgs e)
        {
            var state = Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value;
            state.OptimizeTrees();
            Save();
        }

        private void OnWizardNext(object sender, CancelEventArgs e)
        {
            SaveVirtualizationState();
        }

        private void OnSetActive(object sender, CancelEventArgs cancelEventArgs)
        {
            InitializeSettingsButton();
            _saveTemplateButton.Visible = true;
            _save2TemplateButton.Visible = true;
            //SetWizardButtons(WizardButtons.Back | WizardButtons.Finish);

            Wizard.CancelBtnText = "Stop";
            Wizard.EnableFinishButton(false);

            _logListView.Items.Clear();
            _progressBar.Value = 0;

            StartExport();
        }

        protected void OnWorkerComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Exporter.Stop();
            Wizard.EnableFinishButton(true);
            Wizard.EnableCancelButton(false);
            Banner.Subtitle = e.Cancelled ? "Export cancelled." : "Export complete!";
            LogString(e.Cancelled ? "Export cancelled." : "Export complete!");
        }

        private void CopyToClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            var text =
                _logListView.SelectedItems.Cast<ListViewItem>().
                Select(i => i.Text).
                Aggregate("", (acum, line) => acum += line + Environment.NewLine).TrimEnd();

            Clipboard.SetText(text);
        }

        private void ProgressListViewSizeChanged(object sender, EventArgs e)
        {
            var columns = _logListView.Columns.Cast<ColumnHeader>().ToList();

            var fixedColumnsWidth = columns.Sum(c => c.Width) - columns.Last().Width;

            columns.Last().Width = _logListView.ClientSize.Width - fixedColumnsWidth;
        }

        #endregion

        #region Control

        public void Start()
        {
            // Do nothing
        }

        #region Logging

        private void LogStringCallback(string s)
        {
            var item = _logListView.Items.Add(s);
            item.EnsureVisible();    
        }

        public void LogString(string s)
        {
            this.ExecuteInUIThreadAsynchronously(() => LogStringCallback(s));
        }

        private void LogErrorCallback(string s)
        {
            var item = _logListView.Items.Add(new ListViewItem(s) { ForeColor = Color.Red });
            item.EnsureVisible();
        }

        public void LogError(string s)
        {
            this.ExecuteInUIThreadAsynchronously(() => LogErrorCallback(s));
        }

        public void SetProgress(int value)
        {
            this.ExecuteInUIThreadAsynchronously(() => _progressBar.Value = value);
        }

        #endregion

        #region Exporting

        protected void StartExport()
        {
            Worker = new BackgroundWorker {WorkerReportsProgress = true};

            Exporter = Export.CreateExporter();
            Exporter.ProgressDialog = this;

            //Worker.ProgressChanged += (sender, args) => Add(ThinAppProgressEvent.From(args));
            Worker.RunWorkerCompleted += OnWorkerComplete;

            Worker.DoWork += (sender, args) => Exporter.GeneratePackage(Export);
            Worker.WorkerSupportsCancellation = true;

            Worker.RunWorkerAsync();
        }

        #endregion

        #endregion

        #region Templates

        private void SaveVirtualizationState()
        {
            /*
            var trace = Export.GetField<DeviareRunTrace>(ExportFieldNames.Trace).Value;
            using (var file = LastTemplates.GetLastTemplate(trace, false))
            {
                Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value.SaveTemplate(file);
            }
             * */
        }

        #endregion

        public void RaiseOnKeyDownEvent(KeyEventArgs args)
        {
            OnKeyDown(args);
        }
    }
}