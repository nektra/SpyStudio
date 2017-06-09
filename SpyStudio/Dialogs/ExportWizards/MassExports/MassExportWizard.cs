using System.Windows.Forms;
using System.Linq;
using SpyStudio.Export.MassExports;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    public partial class MassExportWizard : Wizard.UI.WizardSheet
    {
        protected MassVirtualizationExport MassExport;

        private MassSettingsPage _settingsPage;

        public MassExportWizard()
        {
            InitializeComponent();
        }

        public MassExportWizard(MassVirtualizationExport aMassExport)
        {
            MassExport = aMassExport;

            InitializeComponent();
        }

        public bool Canceled { get; set; }

        public ListView ApplicationList
        {
            get
            {
                if (_settingsPage == null)
                    _settingsPage = (MassSettingsPage) Pages.First(p => p is MassSettingsPage);

                return _settingsPage.ApplicationList;
            }
        }

        public bool SystemMeetsRequirements()
        {
            return MassExport.SystemMeetsRequirements(this);
        }
    }
}
