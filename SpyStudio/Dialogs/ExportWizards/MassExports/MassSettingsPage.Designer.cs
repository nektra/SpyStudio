using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    partial class MassSettingsPage
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SettingsByApplicationSplit = new System.Windows.Forms.SplitContainer();
            this.ApplicationsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ApplicationList = new System.Windows.Forms.ListView();
            this.appNameHeader = new System.Windows.Forms.ColumnHeader();
            this.ApplicationsListLabel = new System.Windows.Forms.Label();
            this.SettingsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.SettingsTable = new ExportSettingsTable();
            this.SettingsByApplicationSplit.Panel1.SuspendLayout();
            this.SettingsByApplicationSplit.Panel2.SuspendLayout();
            this.SettingsByApplicationSplit.SuspendLayout();
            this.ApplicationsPanel.SuspendLayout();
            this.SettingsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(689, 64);
            this.Banner.Subtitle = "Check and/or modify the export settings for each application. Once you are done, " +
                "click \"Next\" to start the export process.";
            this.Banner.Title = "Export Settings";
            // 
            // SettingsByApplicationSplit
            // 
            this.SettingsByApplicationSplit.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.SettingsByApplicationSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsByApplicationSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.SettingsByApplicationSplit.Location = new System.Drawing.Point(0, 64);
            this.SettingsByApplicationSplit.Name = "SettingsByApplicationSplit";
            // 
            // SettingsByApplicationSplit.Panel1
            // 
            this.SettingsByApplicationSplit.Panel1.Controls.Add(this.ApplicationsPanel);
            // 
            // SettingsByApplicationSplit.Panel2
            // 
            this.SettingsByApplicationSplit.Panel2.Controls.Add(this.SettingsPanel);
            this.SettingsByApplicationSplit.Size = new System.Drawing.Size(689, 391);
            this.SettingsByApplicationSplit.SplitterDistance = 267;
            this.SettingsByApplicationSplit.TabIndex = 0;
            // 
            // ApplicationsPanel
            // 
            this.ApplicationsPanel.ColumnCount = 1;
            this.ApplicationsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ApplicationsPanel.Controls.Add(this.ApplicationList, 0, 1);
            this.ApplicationsPanel.Controls.Add(this.ApplicationsListLabel, 0, 0);
            this.ApplicationsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationsPanel.Location = new System.Drawing.Point(0, 0);
            this.ApplicationsPanel.Name = "ApplicationsPanel";
            this.ApplicationsPanel.RowCount = 2;
            this.ApplicationsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.ApplicationsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ApplicationsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.ApplicationsPanel.Size = new System.Drawing.Size(263, 387);
            this.ApplicationsPanel.TabIndex = 1;
            // 
            // ApplicationList
            // 
            this.ApplicationList.CheckBoxes = true;
            this.ApplicationList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.appNameHeader});
            this.ApplicationList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationList.FullRowSelect = true;
            this.ApplicationList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.ApplicationList.HideSelection = false;
            this.ApplicationList.Location = new System.Drawing.Point(3, 23);
            this.ApplicationList.MultiSelect = false;
            this.ApplicationList.Name = "ApplicationList";
            this.ApplicationList.Size = new System.Drawing.Size(257, 361);
            this.ApplicationList.TabIndex = 0;
            this.ApplicationList.UseCompatibleStateImageBehavior = false;
            this.ApplicationList.View = System.Windows.Forms.View.Details;
            // 
            // appNameHeader
            // 
            this.appNameHeader.Text = "Name";
            this.appNameHeader.Width = 184;
            // 
            // ApplicationsListLabel
            // 
            this.ApplicationsListLabel.AutoSize = true;
            this.ApplicationsListLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationsListLabel.Location = new System.Drawing.Point(3, 0);
            this.ApplicationsListLabel.Name = "ApplicationsListLabel";
            this.ApplicationsListLabel.Size = new System.Drawing.Size(257, 20);
            this.ApplicationsListLabel.TabIndex = 1;
            this.ApplicationsListLabel.Text = "Applications:";
            this.ApplicationsListLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SettingsPanel
            // 
            this.SettingsPanel.AutoSize = true;
            this.SettingsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SettingsPanel.ColumnCount = 1;
            this.SettingsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingsPanel.Controls.Add(this.SettingsTable, 0, 0);
            this.SettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsPanel.Location = new System.Drawing.Point(0, 0);
            this.SettingsPanel.Name = "SettingsPanel";
            this.SettingsPanel.RowCount = 1;
            this.SettingsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 379F));
            this.SettingsPanel.Size = new System.Drawing.Size(414, 387);
            this.SettingsPanel.TabIndex = 2;
            // 
            // SettingsTable
            // 
            this.SettingsTable.AdvancedExportSettingsDialog = null;
            this.SettingsTable.AutoSize = true;
            this.SettingsTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SettingsTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsTable.Location = new System.Drawing.Point(3, 3);
            this.SettingsTable.Name = "SettingsTable";
            this.SettingsTable.Size = new System.Drawing.Size(408, 381);
            this.SettingsTable.TabIndex = 0;
            // 
            // MassSettingsPage
            // 
            this.Controls.Add(this.SettingsByApplicationSplit);
            this.Name = "MassSettingsPage";
            this.Size = new System.Drawing.Size(689, 455);
            this.SetActive += new WizardPageEventHandler(this.MassSettingsPageSetActive);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.MassSettingsPageWizardNext);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.SettingsByApplicationSplit, 0);
            this.SettingsByApplicationSplit.Panel1.ResumeLayout(false);
            this.SettingsByApplicationSplit.Panel2.ResumeLayout(false);
            this.SettingsByApplicationSplit.Panel2.PerformLayout();
            this.SettingsByApplicationSplit.ResumeLayout(false);
            this.ApplicationsPanel.ResumeLayout(false);
            this.ApplicationsPanel.PerformLayout();
            this.SettingsPanel.ResumeLayout(false);
            this.SettingsPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer SettingsByApplicationSplit;
        private System.Windows.Forms.TableLayoutPanel ApplicationsPanel;
        public System.Windows.Forms.ListView ApplicationList;
        private System.Windows.Forms.ColumnHeader appNameHeader;
        private System.Windows.Forms.Label ApplicationsListLabel;
        private System.Windows.Forms.TableLayoutPanel SettingsPanel;
        private ExportSettingsTable SettingsTable;
    }
}
