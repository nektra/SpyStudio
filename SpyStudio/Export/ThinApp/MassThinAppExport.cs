using System.Diagnostics;
using System.Windows.Forms;
using SpyStudio.Dialogs.ExportWizards.MassExports;
using SpyStudio.Export.MassExports;
using SpyStudio.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Export.ThinApp
{
    public class MassThinAppExport : MassVirtualizationExport
    {
        public MassThinAppExport(DeviareRunTrace aTrace) : base(aTrace)
        {
        }

        #region Implementation of MassVirtualizationExport

        protected override VirtualizationExport GenerateExportFrom(DeviareRunTrace aTrace)
        {
            var export = new ThinAppExport(aTrace);

            export.CheckerType = CheckerType.Application;

            return export;
        }

        public override void Initialize(ExportSettingsTable aSettingsTable)
        {
            aSettingsTable.InitializeFor(this);
        }

        public override Exporter GetExporter()
        {
            return new ThinAppExporter();
        }

        public override bool SystemMeetsRequirements(MassExportWizard aWizard)
        {
            if (!PlatformTools.IsRunningAsLocalAdmin())
            {
                MessageBox.Show(aWizard, Resources.Swv_Export_Wizard_Not_Admin_Error,
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        #endregion
    }
}