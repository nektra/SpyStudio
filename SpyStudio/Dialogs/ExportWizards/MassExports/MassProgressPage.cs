using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.Export.MassExports;
using SpyStudio.Export.ThinApp;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    public partial class MassProgressPage : Wizard.UI.InternalWizardPage, IExportProgressControl
    {
        protected MassVirtualizationExport MassExport;

        protected MassExportWizard Wizard { get { return ((MassExportWizard)ParentForm); } }

        public static MassProgressPage For(MassVirtualizationExport aMassExport)
        {
            return new MassProgressPage {MassExport = aMassExport};
        }

        public MassProgressPage()
        {
            InitializeComponent();
        }

        #region Implementation of IExportProgressControl

        public void LogString(string aString)
        {
            ProgressDetails.Items.Add(new ListViewItem(aString));
        }

        public void LogError(string aString)
        {
            ProgressDetails.Items.Add(new ListViewItem(aString) { ForeColor = Color.Red });
        }

        public void SetProgress(int aPercentage)
        {
            ProgressBar.Value = aPercentage;
        }

        public void Start()
        {
            Show();
        }

        #endregion

        private void MassProgressPage_SetActive(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GeneratePackages();
        }

        private void GeneratePackages()
        {
            var exporter = MassExport.GetExporter();

            exporter.ProgressDialog = this;

            foreach (var item in Wizard.ApplicationList.Items.Cast<ApplicationListItem>().Where(item => item.Checked))
                exporter.GeneratePackage(item.Export);
        }

    }
}
