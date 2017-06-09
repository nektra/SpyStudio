using System;
using System.ComponentModel;
using SpyStudio.Export.MassExports;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    public partial class MassSettingsPage : Wizard.UI.InternalWizardPage
    {
        protected MassExportWizard Wizard { get; set; }

        protected MassVirtualizationExport MassExport { get; set; }

        #region Instantiation

        public static MassSettingsPage For(MassVirtualizationExport aMassExport)
        {
            return new MassSettingsPage { MassExport = aMassExport };
        }

        public MassSettingsPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Initialization

        private void Populate()
        {
            //Populate applications list
            foreach (var exportByName in MassExport.Exports)
                ApplicationList.Items.Add(new ApplicationListItem(exportByName.Key, exportByName.Value));
        }

        #endregion

        #region Event Handlers

        private void MassSettingsPageSetActive(object sender, CancelEventArgs e)
        {
            ResizeNameColumnToFitListWidth(null, null);
            ApplicationList.SizeChanged += ResizeNameColumnToFitListWidth;
            ApplicationList.ItemSelectionChanged += (sdr, args) => ShowOrSaveSettingsFor((ApplicationListItem)args.Item);

            MassExport.Initialize(SettingsTable);

            Populate();

            if (ApplicationList.Items.Count > 0)
                ApplicationList.Items[0].Selected = true;
        }

        private void ResizeNameColumnToFitListWidth(object sender, EventArgs eventArgs)
        {
            ApplicationList.Columns[0].Width = ApplicationList.Width - 4;
        }

        private void ShowOrSaveSettingsFor(ApplicationListItem anApplicationItem)
        {
            SettingsTable.Enabled = ApplicationList.SelectedItems.Count != 0;

            if (anApplicationItem.Selected)
                anApplicationItem.Export.Settings.ShowIn(SettingsTable);
            else
                anApplicationItem.Export.Settings.SaveSettingsFrom(SettingsTable);
        }

        private void MassSettingsPageWizardNext(object sender, Wizard.UI.WizardPageEventArgs e)
        {
            
        }

        #endregion

        
    }
}
