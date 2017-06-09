//#define DISABLE_STATEPAGE

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Ionic.Zip;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Properties;
using SpyStudio.Tools;
using Wizard.UI;
using System.Linq;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.ExportWizards.TemplateEditor
{
#if !DISABLE_STATEPAGE
    public partial class TemplateEditorSave : InternalWizardPage
    {
        #region Properties

        protected readonly VirtualizationExport Export;

        protected BackgroundWorker WebCatalogLoader { get; set; }

        protected WizardSheet Wizard;

        #endregion

        #region Instatiation

        public TemplateEditorSave(WizardSheet wizard, VirtualizationExport anExport)
        {
            Export = anExport;
            Wizard = wizard;

            InitializeComponent();
            Wizard.FinishBtnText = "&Save";
        }

        #endregion

        #region Event Handling

        public override void OnSetActive(WizardPageEventArgs e)
        {
            SetWizardButtons(WizardButtons.Finish | WizardButtons.Back);
            EnableCancelButton(true);
            base.OnSetActive(e);
        }

        public override void OnWizardFinish(CancelEventArgs e)
        {
            var state = Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value;
            state.SaveWithDialog(this);
            base.OnWizardFinish(e);
        }

        #endregion

    }
#endif
}