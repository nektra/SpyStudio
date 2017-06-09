using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Trace;
using Wizard.UI;
using System.Linq;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    class ThinAppCaptureSelect : PackageSelect
    {
        protected override string VirtualizationPackageString { get { return "package"; } }
        
        public ThinAppCaptureSelect()
        {
            Initialize();
        }

        public ThinAppCaptureSelect(VirtualizationExport anExport)
            : base(anExport)
        {
            Initialize();
        }

        protected void Initialize()
        {
            WizardNext += (a, b) => OnWizardNext();
        }

        protected override void LoadAvailablePackages()
        {
            var capturesDirectory = ThinAppExport.ThinAppCapturesPath;
            if (!Directory.Exists(capturesDirectory))
                return;
            
            this.ExecuteInUIThreadAsynchronously(() => Cursor = Cursors.WaitCursor);

            var getDirectoryRegex = new Regex(@"(?:^|.*\\)([^\\]+)\\*$");

            PackageList.BeginUpdate();

            foreach (var path in Directory.GetDirectories(capturesDirectory))
            {
                var match = getDirectoryRegex.Match(path);
                if (!match.Success)
                    continue;

                var capture = ThinAppCapture.At(path);

                PackageList.Items.Add(VirtualPackageListItem.Containing(capture));
            }

            PackageList.EndUpdate();

            this.ExecuteInUIThreadAsynchronously(() => Cursor = Cursors.Arrow);
        }

        protected override IVirtualPackage CreateNewPackage()
        {
            var newCapture = ThinAppCapture.At(ThinAppExport.ThinAppCapturesPath + "\\" + GetNewName());

            //newCapture.SaveAll();

            return newCapture;
        }

        protected override void AddExeSelectPageIfNecessary(WizardPageEventArgs args)
        {
            if (ShouldAddExeSelectPage)
                return;

            var exeSelect = new ThinAppExeSelect(Export);

            Wizard.Pages.Insert(Wizard.Pages.IndexOf(this) + 1, exeSelect);
            args.NewPage = exeSelect.Name;
        }

        private void OnWizardNext()
        {
            ((ThinAppExport) Export).Capture = (ThinAppCapture) Wizard.VirtualPackage;
        }
    }
}