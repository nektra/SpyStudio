using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Dialogs.ExportWizards.MassExports;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppExportSettings : ExportSettings
    {
        protected bool Setting1 { get; set; }
        protected bool Setting2 { get; set; }
        protected bool Setting3 { get; set; }

        #region Overrides of ExportSettings

        public override void ShowIn(ExportSettingsTable aSettingsTable)
        {
            aSettingsTable.Clear();

            aSettingsTable.SimpleSettingsCheckBoxes[0].Checked = Setting1;
            aSettingsTable.SimpleSettingsCheckBoxes[1].Checked = Setting2;
            aSettingsTable.SimpleSettingsCheckBoxes[2].Checked = Setting3;

            aSettingsTable.SimpleSettingsCheckBoxes[0].Enabled = !UsingAdvancedSettings;
            aSettingsTable.SimpleSettingsCheckBoxes[1].Enabled = !UsingAdvancedSettings;
            aSettingsTable.SimpleSettingsCheckBoxes[2].Enabled = !UsingAdvancedSettings;

            aSettingsTable.UsingAdvancedSettingsCheckBox.Checked = UsingAdvancedSettings;
            aSettingsTable.UsingAdvancedSettingsCheckBox.Enabled = UsingAdvancedSettings;
        }

        public override void SaveSettingsFrom(ExportSettingsTable aSettingsTable)
        {
            Setting1 = aSettingsTable.SimpleSettingsCheckBoxes[0].Checked;
            Setting2 = aSettingsTable.SimpleSettingsCheckBoxes[1].Checked;
            Setting3 = aSettingsTable.SimpleSettingsCheckBoxes[2].Checked;

            UsingAdvancedSettings = aSettingsTable.UsingAdvancedSettingsCheckBox.Checked;
        }

        #endregion
    }
}