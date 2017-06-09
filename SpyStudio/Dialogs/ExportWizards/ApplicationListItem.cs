using System.Windows.Forms;
using SpyStudio.Export;

namespace SpyStudio.Dialogs.ExportWizards
{
    class ApplicationListItem : ListViewItem
    {
        public VirtualizationExport Export { get; set; }

        public ApplicationListItem(string aProcessName, VirtualizationExport anExportSettings) : base (aProcessName)
        {
            Export = anExportSettings;
            Export.SetFieldValue(ExportFieldNames.Name, aProcessName);
        }
    }
}
