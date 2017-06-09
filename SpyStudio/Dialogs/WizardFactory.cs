using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Dialogs.ExportWizards.MassExports;
using SpyStudio.Dialogs.ExportWizards.SWV;
using SpyStudio.Dialogs.ExportWizards.TemplateEditor;
using SpyStudio.Dialogs.ExportWizards.ThinApp;
using SpyStudio.Export.MassExports;
using SpyStudio.Export.SWV;
using SpyStudio.Export.ThinApp;
using Wizard.UI;

namespace SpyStudio.Dialogs
{
    public static class WizardFactory
    {
        #region Constants

        private const string SwvExportWizardTitle = "Export to SWV Layer";
        private const string ThinAppExportWizardTitle = "Export to ThinApp Package";
        private const string AppvExportWizardTitle = "Export to App-V Package";

        private const string SwvWelcomeText =
            "This wizard will guide you through the process of exporting the data collected by SpyStudio to a Symantec Workstation Virtualization layer.";

        private const string ThinAppWelcomeMessage =
            "This wizard will guide you through the process of creating a ThinApp package from the data collected by SpyStudio.";

        private const string AppvWelcomeMessage =
            "This wizard will guide you through the process of creating an App-V package from the data collected by SpyStudio.";

        private const string SwvFileSelectText = "Select the files to include in the layer.";
        private const string ThinAppFileSelectText = "Select the files to include in the package.";
        private const string AppvFileSelectText = "Select the files to include in the package.";

        private const string SwvRegistrySelectText = "Select the keys to include in the layer.";
        private const string ThinAppRegistrySelectText = "Select the keys to include in the package.";
        private const string AppvRegistrySelectText = "Select the keys to include in the package.";

        private const string SwvProgressText = "Exporting to SWV layer...";
        private const string ThinAppProgressText = "Exporting to ThinApp package...";
        private const string AppvProgressText = "Exporting to App-V package...";

        private const string SwvFileDestinationSelect = "Select a destination for the selected files";

        private const string ThinAppEntryPointSelectText = "Select the entry points to include in the package.";

        #endregion

        #region Export Wizard Creation

        public static ExportWizard CreateWizardFor(SwvExport anSwvExport)
        {
            var wizard = new ExportWizard(SwvExportWizardTitle, anSwvExport);

            wizard.Pages.Add(new WelcomePage(wizard, anSwvExport, SwvWelcomeText));
            wizard.Pages.Add(new SwvPackageSelect(anSwvExport));
            //wizard.Pages.Add(new LayerSelect(wizard, anSwvExport));
            wizard.Pages.Add(new SwvFileSelect(anSwvExport, SwvFileSelectText, SwvPathNormalizer.GetInstance()));
            wizard.Pages.Add(new FileDestinationSelect(wizard, anSwvExport, SwvFileDestinationSelect));
            wizard.Pages.Add(new SwvRegistrySelect(wizard, anSwvExport, SwvRegistrySelectText));
            wizard.Pages.Add(new IsolationRulesSelect(wizard, anSwvExport));
            wizard.Pages.Add(new ProgressPage(wizard, anSwvExport));

            return wizard;
        }

        public static ExportWizard CreateEditorWizardFor(SwvExport anSwvExport)
        {
            var wizard = new ExportWizard(SwvExportWizardTitle, anSwvExport);

            wizard.Pages.Add(new EditorWelcomePage(wizard, anSwvExport, SwvWelcomeText));
            wizard.Pages.Add(new SwvFileSelect(anSwvExport, SwvFileSelectText, SwvPathNormalizer.GetInstance()));
            wizard.Pages.Add(new FileDestinationSelect(wizard, anSwvExport, SwvFileDestinationSelect));
            wizard.Pages.Add(new SwvRegistrySelect(wizard, anSwvExport, SwvRegistrySelectText));
            wizard.Pages.Add(new IsolationRulesSelect(wizard, anSwvExport));
            wizard.Pages.Add(new TemplateEditorSave(wizard, anSwvExport));

            return wizard;
        }

        public static ExportWizard CreateWizardFor(ThinAppExport aThinAppExport)
        {
            var wizard = new ExportWizard(ThinAppExportWizardTitle, aThinAppExport);

            wizard.Pages.Add(new ThinAppWelcomePage(wizard, aThinAppExport, ThinAppWelcomeMessage));
            wizard.Pages.Add(new ThinAppCaptureSelect(aThinAppExport));
            //wizard.Pages.Add(new ThinAppPreferencesPage(wizard, aThinAppExport));
            wizard.Pages.Add(new ThinAppFileSelect(aThinAppExport, ThinAppFileSelectText));
            wizard.Pages.Add(new EntryPointSelect(wizard, aThinAppExport, ThinAppEntryPointSelectText));
            wizard.Pages.Add(new ThinAppRegistrySelect(wizard, aThinAppExport, ThinAppRegistrySelectText));
            wizard.Pages.Add(new ProgressPage(wizard, aThinAppExport));
            
            return wizard;
        }

        #endregion

        #region Advanced Settings Wizard Creation

        public static WizardSheet CreateAdvancedSettingsWizardFor(ThinAppExport anExport)
        {
            var wizard = new ExportWizard(ThinAppExportWizardTitle, anExport);

            //wizard.Pages.Add(FileSelect.ForThinApp(anExport, ThinAppFileSelectText));
            wizard.Pages.Add(new EntryPointSelect(wizard, anExport, ThinAppEntryPointSelectText));
            wizard.Pages.Add(RegistrySelect.ForThinApp(wizard, anExport, ThinAppRegistrySelectText));

            return wizard;
        }

        #endregion

        #region Mass Export Wizard Creation

        public static MassExportWizard CreateWizardFor(MassVirtualizationExport aMassExport)
        {
            var wizard = new MassExportWizard(aMassExport);
            
            wizard.Pages.Add(new MassWelcomePage());
            wizard.Pages.Add(MassSettingsPage.For(aMassExport));
            wizard.Pages.Add(MassProgressPage.For(aMassExport));

            return wizard;
        }

        #endregion
    }
}