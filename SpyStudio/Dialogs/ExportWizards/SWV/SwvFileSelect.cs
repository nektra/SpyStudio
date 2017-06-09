using System.Collections.Generic;
using SpyStudio.Export.SWV;
using SpyStudio.Export.ThinApp;
using SpyStudio.FileSystem;

namespace SpyStudio.Dialogs.ExportWizards.SWV
{
    class SwvFileSelect : FileSelect
    {
        protected new readonly SwvExport Export;

        public SwvFileSelect(SwvExport anExport, string aPageDescription, PathNormalizer aPathNormalizer) : base(anExport, aPageDescription, aPathNormalizer)
        {
            Export = anExport;
            filesView.PathNormalizer = SwvPathNormalizer.GetInstance();

            HideIsolationSelectionRow();
        }

        protected override IEnumerable<FileSystemTreeNode> LoadFilesFromPackage()
        {
            var layerFiles = new List<FileSystemTreeNode>();

            if (Export.Layer != null)
            {
                foreach (var file in Export.Layer.Files)
                {
                    // Load "updated" entry from file system
                    filesView.AddFileEntryUncolored(file.GetUpdatedEntryFromFileSystem());

                    layerFiles.Add((FileSystemTreeNode) filesView.AddFileEntryUncolored(file));
                }
            }

            return layerFiles;
        }

        protected override void SetDefaultIsolation(ThinAppIsolationOption option)
        {
        }
    }
}