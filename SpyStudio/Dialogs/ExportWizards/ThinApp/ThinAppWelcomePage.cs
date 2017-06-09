using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpyStudio.Export;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    public class ThinAppWelcomePage : WelcomePage
    {
        public ThinAppWelcomePage(ExportWizard aWizard, VirtualizationExport anExport, string aWelcomeMessage) : base(aWizard, anExport, aWelcomeMessage)
        {
            Initialize();
        }

        void Initialize()
        {
            SetStateOptionsVisibility(true);
        }
    }
}
