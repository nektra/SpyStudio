using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SpyStudio.Database;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Hooks;
using SpyStudio.Properties;
using SpyStudio.Trace;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class WelcomePage : ExternalWizardPage
    {
        #region Fields

        private TemplateSelect _templateSelect;

        #endregion

        #region Properties

        protected readonly ExportWizard Wizard;
        protected ExportField<PortableTemplate> VirtualizationTemplate { get; set; }
        protected readonly VirtualizationExport Export;
        protected bool DefaultOptionsWereLoaded { get; set; }
        protected DeviareRunTrace MainWindowTrace { get; set; }

        protected DeviareRunTrace FileTrace { get; set; }

        protected bool NextPageIsTemplateSelect
        {
            get { return Wizard.Pages[1] is TemplateSelect; }
        }

        protected DeviareRunTrace SelectedTrace
        {
            get
            {
                if (UseMainWindowTraceRadioButton.Checked)
                    return MainWindowTrace;
                    
                if (LoadTraceFromDiskRadioButton.Checked)
                    return FileTrace;

                return null;
            }
        }

        protected bool MainWindowTraceIsValid { get { return MainWindowTrace != null && !MainWindowTrace.IsEmpty();}}

        protected bool LastSelectionsExists 
        {
            get
            {
                return (SelectedTrace != null && LastTemplates.GetLastTemplate(SelectedTrace.ObjectId, true) != null);
            }
        }

        protected bool ReadyToProceed
        {
            get
            {
                return ((SelectedTrace != null) || DoNotUseTraceRadioButton.Checked) &&
                       (CreateEditOrUpdateRadioButton.Checked || CreateFromTemplateRadioButton.Checked);
            }
        }

    #endregion

        #region Instantiation and Initialization

        public WelcomePage(ExportWizard aWizard, VirtualizationExport anExport, string aWelcomeMessage)
        {
            Wizard = aWizard;
            Export = anExport;
            MainWindowTrace = (DeviareRunTrace)Export.GetFieldValue(ExportFieldNames.Trace);
            VirtualizationTemplate = anExport.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate);

            InitializeComponent();

            introductionLabel.Text = aWelcomeMessage;

            new ToolTip().SetToolTip(CreateEditOrUpdateRadioButton,
                                     "Create or edit a " +
                                     "virtual application from the current application log.");
            new ToolTip().SetToolTip(LoadPreviousSelectionsCheckBox,
                                     "Continue creating a package from the exact point you were.");
            new ToolTip().SetToolTip(CreateFromTemplateRadioButton,
                                     "Create a new virtual application from a Template.");

            SetStateOptionsVisibility(true);

            WizardNext += OnWizardNext;
        }

        private void InitializePackageButtons()
        {
            CreateEditOrUpdateRadioButton.Enabled = true;
            LoadPreviousSelectionsCheckBox.Enabled = LastSelectionsExists;
            CreateFromTemplateRadioButton.Enabled = true;
            
        }

        private void InitializeTraceButtons()
        {
            LoadTraceFromDiskRadioButton.Enabled = true;
            BrowseTraceFileButton.Enabled = true;
            TracePathTextBox.Enabled = true;

            UseMainWindowTraceRadioButton.Enabled = MainWindowTraceIsValid;

            DoNotUseTraceRadioButton.Enabled = true;
        }

        #endregion

        #region Event Handling

        protected void OnWizardNext(object sender, WizardPageEventArgs e)
        {
            EnableNextButton(false);
            Export.SetFieldValue(ExportFieldNames.Trace, SelectedTrace);

            if (CreateEditOrUpdateRadioButton.Checked)
            {
                VirtualizationTemplate.Value = LoadPreviousSelectionsCheckBox.Checked
                    ? GetPreviousSelectionsFor(SelectedTrace)
                    : new PortableTemplate();

                if (NextPageIsTemplateSelect)
                {
                    Wizard.Pages.RemoveAt(1);
                    e.NewPage = Wizard.Pages[1].Name;
                }

                return;
            }

            VirtualizationTemplate.Value.IsInUse = true;

            if (NextPageIsTemplateSelect) 
                return;

            var statePage = _templateSelect ?? (_templateSelect = new TemplateSelect(Wizard, Export));
            Wizard.Pages.Insert(1, statePage);
            e.NewPage = statePage.Name;
        }

        protected void WelcomePageSetActive(object sender, WizardPageEventArgs e)
        {
            UpdateNavigationButtons();

            if (DefaultOptionsWereLoaded)
                return;

            InitializeTraceButtons();
            InitializePackageButtons();

            // NOTE: Checking an option in a group disables the remaining options in the same group automatically.
            LoadTraceFromDiskRadioButton.Checked = true;
            UseMainWindowTraceRadioButton.Checked = UseMainWindowTraceRadioButton.Enabled;
            CreateEditOrUpdateRadioButton.Checked = true;
            LoadPreviousSelectionsCheckBox.Checked = LoadPreviousSelectionsCheckBox.Enabled;

            DefaultOptionsWereLoaded = true;
        }
        
        protected void WelcomePageQueryCancel(object sender, CancelEventArgs e)
        {
            Export.Cancel();
        }

        protected void BrowseButtonClick(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Filter = "SpyStudio File (*.spy)|*.spy|Xml File (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = Settings.Default.LastDefaultLoadExtension == "xml" ? 2 : 1,
                Multiselect = false,
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Load Trace"
            };
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedLogFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedLogFolder;

            if (openDlg.ShowDialog(this) != DialogResult.OK)
                return;

            bool success;
            string error;
            var devRunTrace = new DeviareRunTrace(new ProcessInfo(), new ModulePath());

            var result = devRunTrace.LoadLog(FindForm(), false, false, openDlg.FileName, out success, out error);
            if (result == DialogResult.Cancel)
            {
                return;
            }
            if (!success)
            {
                MessageBox.Show(this, "Error loading Trace: " + error,
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Settings.Default.PathLastSelectedLogFolder = Path.GetDirectoryName(openDlg.FileName);
                Settings.Default.Save();
            }
            catch
            {
            }

            if(FileTrace != null && FileTrace.TraceId != MainWindowTrace.TraceId)
                EventDatabaseMgr.GetInstance().ClearDatabase(FileTrace.TraceId);

            //Export.TraceId = devRunTrace.TraceId;
            //Export.SetFieldValue(ExportFieldNames.Trace, devRunTrace);

            FileTrace = devRunTrace;

            if (CreateEditOrUpdateRadioButton.Checked && LastSelectionsExists)
                LoadPreviousSelectionsCheckBox.Enabled = LoadPreviousSelectionsCheckBox.Checked = true;

            TracePathTextBox.Text = openDlg.FileName;

            UpdateNavigationButtons();
        }

        #region Trace Options

        protected void UseMainWindowTraceOptionCheckedChanged(object sender, EventArgs e)
        {
            if (!UseMainWindowTraceRadioButton.Checked)
                return;

            LoadTraceFromDiskRadioButton.Checked = false;
            DoNotUseTraceRadioButton.Checked = false;

            UpdateNavigationButtons();
        }

        protected void LoadTraceFromDiskOptionCheckedChanged(object sender, EventArgs e)
        {
            TracePathTextBox.Enabled = LoadTraceFromDiskRadioButton.Checked;
            BrowseTraceFileButton.Enabled = LoadTraceFromDiskRadioButton.Checked;
            LoadPreviousSelectionsCheckBox.Enabled = CreateEditOrUpdateRadioButton.Checked && LastSelectionsExists;

            if (!LoadTraceFromDiskRadioButton.Checked)
                return;

            UseMainWindowTraceRadioButton.Checked = false;
            DoNotUseTraceRadioButton.Checked = false;
            LoadPreviousSelectionsCheckBox.Checked = LastSelectionsExists;

            UpdateNavigationButtons();
        }

        protected void DoNotUseTraceOptionCheckedChanged(object sender, EventArgs e)
        {
            if (!DoNotUseTraceRadioButton.Checked)
                return;

            UseMainWindowTraceRadioButton.Checked = false;
            LoadTraceFromDiskRadioButton.Checked = false;
            LoadPreviousSelectionsCheckBox.Checked = false;

            UpdateNavigationButtons();
        }

        #endregion

        #region Package Options

        protected void CreateEditOrUpdateOptionCheckedChanged(object sender, EventArgs e)
        {
            LoadPreviousSelectionsCheckBox.Enabled = CreateEditOrUpdateRadioButton.Checked && LastSelectionsExists;

            if (!CreateEditOrUpdateRadioButton.Checked)
            {
                LoadPreviousSelectionsCheckBox.Checked = false;
                return;
            }

            UseMainWindowTraceRadioButton.Enabled = MainWindowTraceIsValid;
            LoadTraceFromDiskRadioButton.Enabled = true;
            BrowseTraceFileButton.Enabled = LoadTraceFromDiskRadioButton.Checked;
            TracePathTextBox.Enabled = LoadTraceFromDiskRadioButton.Checked;
            LoadPreviousSelectionsCheckBox.Checked = LastSelectionsExists;

            UpdateNavigationButtons();
        }

        protected void LoadPreviousSelectionsOptionCheckedChanged(object sender, EventArgs e)
        {
            UpdateNavigationButtons();
        }

        protected void CreateFromTemplateOptionCheckedChanged(object sender, EventArgs e)
        {
            if (!CreateFromTemplateRadioButton.Checked)
                return;

            UseMainWindowTraceRadioButton.Checked = false;
            UseMainWindowTraceRadioButton.Enabled = false;
            LoadTraceFromDiskRadioButton.Checked = false;
            LoadTraceFromDiskRadioButton.Enabled = false;
            BrowseTraceFileButton.Enabled = false;
            TracePathTextBox.Enabled = false;
            DoNotUseTraceRadioButton.Checked = true;

            UpdateNavigationButtons();
        }

        #endregion

        #endregion

        #region Control

        protected void UpdateNavigationButtons()
        {
            EnableNextButton(ReadyToProceed);
            EnableCancelButton(true);
        }

        protected void SetStateOptionsVisibility(bool visibility)
        {
            CreateEditOrUpdateRadioButton.Visible = visibility;
            LoadPreviousSelectionsCheckBox.Visible = visibility;
            CreateFromTemplateRadioButton.Visible = visibility;
        }

        #endregion

        private PortableTemplate GetPreviousSelectionsFor(DeviareRunTrace aTrace)
        {
            using (var file = LastTemplates.GetLastTemplate(aTrace, true))
            using (var reader = new StreamReader(file))
            {
                var templateIsValid = file != null;

                VirtualizationTemplate.Value = templateIsValid
                                                   ? PortableTemplate.RestoreTemplate(reader)
                                                   : new PortableTemplate();

                VirtualizationTemplate.Value.IsInUse = templateIsValid;
            }

            return VirtualizationTemplate.Value;
        }
    }
}
