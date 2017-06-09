using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SpyStudio.Database;
using SpyStudio.Dialogs.ExportWizards.ThinApp;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Forms;
using SpyStudio.Hooks;
using SpyStudio.Properties;
using SpyStudio.Trace;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.TemplateEditor
{
    public partial class EditorWelcomePage : ExternalWizardPage
    {
        protected ExportWizard Wizard;
        protected ExportField<PortableTemplate> VirtualizationTemplate { get; set; }
        protected VirtualizationExport Export;

        private readonly bool _anyEvent;

        public EditorWelcomePage(ExportWizard aWizard, VirtualizationExport anExport, string aWelcomeMessage)
        {
            Wizard = aWizard;
            Export = anExport;
            VirtualizationTemplate = anExport.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate);
            InitializeComponent();

            introductionLabel.Text = aWelcomeMessage;

            _anyEvent =
                _radioButtonCurrentTrace.Enabled =
                EventDatabaseMgr.GetInstance().GetEventCount(anExport.MainWindowTraceId) != 0;
            _captureTypeCombo.DataSource = Enum.GetValues(typeof(CheckerType));
        }

        private void WelcomePageSetActive(object sender, CancelEventArgs e)
        {
            SetWizardButtons(WizardButtons.Next);
            EnableCancelButton(true);

            (!_anyEvent ? _radioButtonDisk : _radioButtonCurrentTrace).Checked = true;

            UpdateButtons();
        }

        private void WelcomePageQueryCancel(object sender, CancelEventArgs e)
        {
            Export.Cancel();
        }

        public override void OnWizardNext(WizardPageEventArgs e)
        {
            EnableNextButton(false);
            Export.CheckerType = (CheckerType)_captureTypeCombo.SelectedItem;
            base.OnWizardNext(e);
        }

        private void ButtonBrowseClick(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Filter = "SpyStudio Template File (*.sts)|*.sts|All files (*.*)|*.*",
                FilterIndex = Settings.Default.LastDefaultLoadExtension == "sts" ? 2 : 1,
                Multiselect = false,
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Load Template"
            };
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedLogFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedLogFolder;

            if (openDlg.ShowDialog(this) != DialogResult.OK)
                return;

            PortableTemplate template;
            try
            {
                using (var file = new FileStream(openDlg.FileName, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(file))
                {
                    template = PortableTemplate.RestoreTemplate(reader);
                }
            }
            catch (IOException)
            {
                return;
            }
            template.IsInUse = true;
            Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value = template;

            _textBoxFile.Text = openDlg.FileName;

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _radioButtonCurrentTrace.Enabled = _anyEvent;

            EnableNextButton(!_radioButtonDisk.Checked || !string.IsNullOrEmpty(_textBoxFile.Text));
        }

        private void RadioButtonDiskCheckedChanged(object sender, EventArgs e)
        {
            UpdateButtons();
        }

    }
}
