using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.FileSystem;
using System.Linq;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    public class ThinAppExeSelect : ExeSelect
    {
        public ThinAppExeSelect(VirtualizationExport anExport) : base(anExport)
        {
            ExeFilesView.PathNormalizer = ThinAppPathNormalizer.GetInstance();
        }

        protected override void TryVirtualizationToolSpecificCriteriaOn(FileSystemTreeNode node)
        {
            node.Checked =
                ((ThinAppCapture) Wizard.VirtualPackage).EntryPoints.Any(
                    e => e.FileSystemLocation == node.FileSystemPath);
        }
    }
}