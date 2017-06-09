using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using SpyStudio.Export;
using SpyStudio.Export.PortableTemplates;
using SpyStudio.Properties;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    public class ThinAppProgressPage : ProgressPage
    {

        public ThinAppProgressPage(WizardSheet aWizard, VirtualizationExport export)
            : base(aWizard, export)
        {
            
        }
    }
}