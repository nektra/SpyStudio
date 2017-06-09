using System.Collections.Generic;
using System.Globalization;
using SpyStudio.Export;
using SpyStudio.Export.SWV;
using SpyStudio.FileSystem;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.Dialogs.ExportWizards.SWV
{
    public class SwvExeSelect : ExeSelect
    {
        private IEnumerable<FileEntry> _exeShortcuts;

        protected IEnumerable<FileEntry> ExeShortcuts { get { return _exeShortcuts ?? SetExeShortcuts(); } }

        public SwvExeSelect(VirtualizationExport anExport)
            : base(anExport)
        {
            ExeFilesView.PathNormalizer = SwvPathNormalizer.GetInstance();
        }

        protected override void TryVirtualizationToolSpecificCriteriaOn(FileSystemTreeNode node)
        {
            node.Checked = ExeShortcuts.Any(s => s.TargetPath.Equals(node.FileSystemPath));
        }

        protected IEnumerable<FileEntry> SetExeShortcuts()
        {
            _exeShortcuts =
                Wizard.VirtualPackage.Files.Where(
                    f => f.IsShortcut && f.TargetPath.EndsWith(".exe", true, CultureInfo.InvariantCulture));

            return _exeShortcuts;
        }
    }
}