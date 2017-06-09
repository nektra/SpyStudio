using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SpyStudio.Export.MassExports;
using SpyStudio.Export.ThinApp;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    public partial class ExportSettingsTable : UserControl
    {
        #region Properties

        private int _actualRow;
        private Form _advancedExportSettingsDialog;

        public readonly List<CheckBox> SimpleSettingsCheckBoxes;
        public Form AdvancedExportSettingsDialog
        {
            get { return _advancedExportSettingsDialog; }
            set
            {
                if (value == null)
                    DisableAdvancedSettings();

                _advancedExportSettingsDialog = value;
                EnableAdvancedSettings();
            }
        }

        protected MassVirtualizationExport MassExport { get; set; }
        protected MassExportWizard Wizard { get { return ((MassExportWizard) ParentForm); } }

        #endregion

        #region Instatiation

        public ExportSettingsTable()
        {
            InitializeComponent();

            SimpleSettingsCheckBoxes = new List<CheckBox>();
            DisableAdvancedSettings();
        }

        #endregion

        #region Initialization

        public void InitializeFor(MassThinAppExport anExport)
        {
            // Store export

            MassExport = anExport;

            // Generate CheckBoxes.

            AddCheckBoxLabeled("Use runtime dlls.");
            AddCheckBoxLabeled("Something about the registry.");
            AddCheckBoxLabeled("Something about the entry points.");

            EnableAdvancedSettings();
        }

        private void AddCheckBoxLabeled(string aLabel)
        {
            var checkbox = new CheckBox();

            checkbox.Text = checkbox.Name = aLabel;
            checkbox.Checked = false;
            checkbox.Dock = DockStyle.Fill;
            checkbox.CheckAlign = checkbox.ImageAlign = checkbox.TextAlign = ContentAlignment.MiddleLeft;

            SimpleSettingsPanel.RowCount++;

            // Modify last row to contain a new checkbox
            SimpleSettingsPanel.RowStyles[_actualRow].SizeType = SizeType.Absolute;
            SimpleSettingsPanel.RowStyles[_actualRow].Height = 25F;

            // Add a row to fill the extra space
            SimpleSettingsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Add the checkbox to the new row
            SimpleSettingsPanel.Controls.Add(checkbox, 0, _actualRow++);

            SimpleSettingsCheckBoxes.Add(checkbox);
        }

        #endregion

        #region Control

        private void EnableAdvancedSettings()
        {
            AdvancedBtn.Enabled = true;
            AdvancedBtn.Show();

            UsingAdvancedSettingsCheckBox.Enabled = true;
            UsingAdvancedSettingsCheckBox.Show();
        }

        private void DisableAdvancedSettings()
        {
            AdvancedBtn.Enabled = false;
            AdvancedBtn.Hide();

            UsingAdvancedSettingsCheckBox.Enabled = false;
            UsingAdvancedSettingsCheckBox.Hide();
        }

        private void EnableSimpleSettings()
        {
            foreach (var checkbox in SimpleSettingsCheckBoxes)
                checkbox.Enabled = true;
        }

        private void DisableSimpleSettings()
        {
            foreach (var checkbox in SimpleSettingsCheckBoxes)
                checkbox.Enabled = false;

        }

        public void Clear()
        {
            foreach (var checkbox in SimpleSettingsCheckBoxes)
            {
                checkbox.Enabled = true;
                checkbox.Checked = false;
            }

            UsingAdvancedSettingsCheckBox.Enabled = false;
            UsingAdvancedSettingsCheckBox.Checked = false;
        }

        #endregion

        #region Event Handlers

        private void AdvancedBtnClick(object sender, EventArgs e)
        {
            if (Wizard.ApplicationList.SelectedItems.Count == 0)
                return;

            var selectedApp = Wizard.ApplicationList.SelectedItems[0].Text;

            var result = MassExport.Exports[selectedApp].ShowAdvancedSettingsDialog();

            if (result == DialogResult.Cancel)
                return;

            UsingAdvancedSettingsCheckBox.Enabled = true;
            UsingAdvancedSettingsCheckBox.Checked = true;

            DisableSimpleSettings();
        }

        private void UsingAdvancedSettingsCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            if (UsingAdvancedSettingsCheckBox.Checked)
                return;

            EnableSimpleSettings();
        }

        #endregion
    }
}
