using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Dialogs.ExportWizards.MassExports;

namespace SpyStudio.Export
{
    public abstract class ExportSettings
    {
        protected bool UsingAdvancedSettings { get; set; }

        public abstract void ShowIn(ExportSettingsTable aSettingsTable);

        public abstract void SaveSettingsFrom(ExportSettingsTable aSettingsTable);
    }
}