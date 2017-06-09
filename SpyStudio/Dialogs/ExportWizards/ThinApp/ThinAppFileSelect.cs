using System.Collections.Generic;
using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.FileSystem;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    class ThinAppFileSelect : FileSelect
    {
        protected ExportField<ThinAppIsolationOption> DirectoryIsolation;

        protected new ThinAppCapture Capture { get { return (ThinAppCapture) Wizard.VirtualPackage; } }

        public ThinAppFileSelect(ThinAppExport aThinAppExport, string selectTheFilesToIncludeInThePackage) : base(aThinAppExport, selectTheFilesToIncludeInThePackage, ThinAppPathNormalizer.GetInstance())
        {
            DirectoryIsolation =
                aThinAppExport.GetField<ThinAppIsolationOption>(ExportFieldNames.ThinAppDirectoryIsolation);
            filesView.PathNormalizer = ThinAppPathNormalizer.GetInstance();

            SetActive += (a, b) => OnSetActive();
            //WizardNext += (a, b) => OnWizardNext();
        }

        private void OnSetActive()
        {
            _defaultIsolationModeCombo.SelectedItem = Capture.DefaultDirectoryIsolation;
        }

        public override void OnWizardNext(WizardPageEventArgs wizardPageEventArgs)
        {
            base.OnWizardNext(wizardPageEventArgs);
            DirectoryIsolation.Value = (ThinAppIsolationOption)_defaultIsolationModeCombo.SelectedItem;
        }

        protected override IEnumerable<FileSystemTreeNode> LoadFilesFromPackage()
        {
            var packageFiles = new List<FileSystemTreeNode>();

            foreach (var fileEntry in Capture.Files)
            {
                // Load "updated" entry from file system
                filesView.AddFileEntryUncolored(fileEntry.GetUpdatedEntryFromFileSystem());
                packageFiles.Add((FileSystemTreeNode)filesView.AddFileEntryUncolored(fileEntry));
            }

            return packageFiles;
        }
    }
}