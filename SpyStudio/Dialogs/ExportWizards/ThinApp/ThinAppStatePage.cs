//#define DISABLE_STATEPAGE

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SpyStudio.Export;
using SpyStudio.Export.PortableTemplates;
using SpyStudio.Properties;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
#if !DISABLE_STATEPAGE
    public partial class ThinAppStatePage : InternalWizardPage
    {
        protected VirtualizationExport Export;

        public ThinAppStatePage(VirtualizationExport anExport)
        {
            Export = anExport;

            Banner.Title = "Settings file";
            Banner.Subtitle = "Select the settings file to load";

            InitializeComponent();
        }

        private void OnSetActive(object sender, CancelEventArgs e)
        {
            SetWizardButtons(WizardButtons.Next);
            EnableCancelButton(true);
        }

        private void OnLoad(object sender, EventArgs e)
        {
        }

        private void WelcomePageQueryCancel(object sender, CancelEventArgs e)
        {
            Export.Cancel();
        }

        public override void OnWizardNext(WizardPageEventArgs e)
        {
            //byte[] buffer;
            //using (var file = new FileStream(settingsFileTB.Text, FileMode.Open, FileAccess.Read))
            //{
            //    buffer = new byte[file.Length];
            //    file.Read(buffer, 0, buffer.Length);
            //}
            //Export.GetField<CompleteTemplate>(ExportFieldNames.VirtualizationState).Value = CompleteTemplate.RestoreTemplate(Encoding.UTF8.GetString(buffer));
        }

        private void BrowseButtonClick(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                DefaultExt = "sts",
                Filter = "Settings file (*.sts)|*.sts|All files (*.*)|*.*",
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Open Settings File"
            };
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedAppFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedAppFolder;

            //if (openDlg.ShowDialog(this) == DialogResult.OK)
            //    settingsFileTB.Text = openDlg.FileName;
        }
    }
#endif
}
