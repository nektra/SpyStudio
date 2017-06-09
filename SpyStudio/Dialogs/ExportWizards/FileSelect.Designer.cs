using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.ContextMenu;
using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.FileSystem;
using SpyStudio.Main;
using SpyStudio.Extensions;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class FileSelect : IInterpreterController
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
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
                
#if DEBUG
                if (Export.FileCheckers != null)
                    foreach (var checker in Export.FileCheckers)
                        checker.ReleaseLogFile();
#endif
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
            this.components = new System.ComponentModel.Container();
            SpyStudio.FileSystem.NullPathNormalizer nullPathNormalizer1 = new SpyStudio.FileSystem.NullPathNormalizer();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this._standardPanel = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxWholeProgramFiles = new System.Windows.Forms.CheckBox();
            this.checkBoxLocalAppData = new System.Windows.Forms.CheckBox();
            this.checkBoxRoamingData = new System.Windows.Forms.CheckBox();
            this.checkBoxAppRuntimes = new System.Windows.Forms.CheckBox();
            this._customPanel = new System.Windows.Forms.TableLayoutPanel();
            this.filesView = new SpyStudio.FileSystem.FileSystemViewer();
            this.label1 = new System.Windows.Forms.Label();
            this._defaultIsolationModeCombo = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxStandard = new System.Windows.Forms.CheckBox();
            this.checkBoxCustom = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanelRight = new System.Windows.Forms.FlowLayoutPanel();
            this._standardPanel.SuspendLayout();
            this._customPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.flowLayoutPanelRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(637, 64);
            this.Banner.Title = "File System";
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel8.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel8.ColumnCount = 2;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel8.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 5;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel8.TabIndex = 0;
            // 
            // _standardPanel
            // 
            this._standardPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._standardPanel.ColumnCount = 1;
            this._standardPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._standardPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._standardPanel.Controls.Add(this.checkBoxWholeProgramFiles, 0, 3);
            this._standardPanel.Controls.Add(this.checkBoxLocalAppData, 0, 0);
            this._standardPanel.Controls.Add(this.checkBoxRoamingData, 0, 1);
            this._standardPanel.Controls.Add(this.checkBoxAppRuntimes, 0, 2);
            this._standardPanel.Location = new System.Drawing.Point(0, 0);
            this._standardPanel.Margin = new System.Windows.Forms.Padding(0);
            this._standardPanel.Name = "_standardPanel";
            this._standardPanel.RowCount = 4;
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._standardPanel.Size = new System.Drawing.Size(233, 96);
            this._standardPanel.TabIndex = 1;
            this._standardPanel.Visible = false;
            // 
            // checkBoxWholeProgramFiles
            // 
            this.checkBoxWholeProgramFiles.AutoSize = true;
            this.checkBoxWholeProgramFiles.Location = new System.Drawing.Point(3, 72);
            this.checkBoxWholeProgramFiles.Name = "checkBoxWholeProgramFiles";
            this.checkBoxWholeProgramFiles.Size = new System.Drawing.Size(227, 17);
            this.checkBoxWholeProgramFiles.TabIndex = 3;
            this.checkBoxWholeProgramFiles.Text = "Import Whole Program Files Folders Related to the Application";
            this.checkBoxWholeProgramFiles.UseVisualStyleBackColor = true;
            // 
            // checkBoxLocalAppData
            // 
            this.checkBoxLocalAppData.AutoSize = true;
            this.checkBoxLocalAppData.Location = new System.Drawing.Point(3, 3);
            this.checkBoxLocalAppData.Name = "checkBoxLocalAppData";
            this.checkBoxLocalAppData.Size = new System.Drawing.Size(133, 17);
            this.checkBoxLocalAppData.TabIndex = 0;
            this.checkBoxLocalAppData.Text = "Local Application Data";
            this.checkBoxLocalAppData.UseVisualStyleBackColor = true;
            // 
            // checkBoxRoamingData
            // 
            this.checkBoxRoamingData.AutoSize = true;
            this.checkBoxRoamingData.Location = new System.Drawing.Point(3, 26);
            this.checkBoxRoamingData.Name = "checkBoxRoamingData";
            this.checkBoxRoamingData.Size = new System.Drawing.Size(149, 17);
            this.checkBoxRoamingData.TabIndex = 1;
            this.checkBoxRoamingData.Text = "Roaming Application Data";
            this.checkBoxRoamingData.UseVisualStyleBackColor = true;
            // 
            // checkBoxAppRuntimes
            // 
            this.checkBoxAppRuntimes.AutoSize = true;
            this.checkBoxAppRuntimes.Location = new System.Drawing.Point(3, 49);
            this.checkBoxAppRuntimes.Name = "checkBoxAppRuntimes";
            this.checkBoxAppRuntimes.Size = new System.Drawing.Size(89, 17);
            this.checkBoxAppRuntimes.TabIndex = 2;
            this.checkBoxAppRuntimes.Text = "Runtime Files";
            this.checkBoxAppRuntimes.UseVisualStyleBackColor = true;
            // 
            // _customPanel
            // 
            this._customPanel.ColumnCount = 2;
            this._customPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._customPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this._customPanel.Controls.Add(this.filesView, 0, 1);
            this._customPanel.Controls.Add(this.label1, 0, 0);
            this._customPanel.Controls.Add(this._defaultIsolationModeCombo, 1, 0);
            this._customPanel.Location = new System.Drawing.Point(0, 96);
            this._customPanel.Margin = new System.Windows.Forms.Padding(0);
            this._customPanel.Name = "_customPanel";
            this._customPanel.RowCount = 2;
            this._customPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._customPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._customPanel.Size = new System.Drawing.Size(436, 155);
            this._customPanel.TabIndex = 3;
            this._customPanel.Visible = false;
            // 
            // filesView
            // 
            this.filesView.BackColor = System.Drawing.SystemColors.Window;
            this._customPanel.SetColumnSpan(this.filesView, 2);
            this.filesView.Controller = null;
            this.filesView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filesView.File1BackgroundColor = System.Drawing.Color.Empty;
            this.filesView.File1TraceId = ((uint)(0u));
            this.filesView.File2BackgroundColor = System.Drawing.Color.Empty;
            this.filesView.File2TraceId = ((uint)(0u));
            this.filesView.HideQueryAttributes = false;
            this.filesView.Location = new System.Drawing.Point(3, 29);
            this.filesView.MergeLayerPaths = false;
            this.filesView.MergeWowPaths = false;
            this.filesView.MinimumSize = new System.Drawing.Size(500, 0);
            this.filesView.Name = "filesView";
            this.filesView.PathNormalizer = nullPathNormalizer1;
            this.filesView.ShowIsolationOptions = true;
            this.filesView.ShowStartupModules = true;
            this.filesView.Size = new System.Drawing.Size(500, 123);
            this.filesView.TabIndex = 1;
            this.filesView.TreeMode = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(260, 26);
            this.label1.TabIndex = 2;
            this.label1.Text = "Default Isolation Mode:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _defaultIsolationModeCombo
            // 
            this._defaultIsolationModeCombo.Dock = System.Windows.Forms.DockStyle.Fill;
            this._defaultIsolationModeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._defaultIsolationModeCombo.FormattingEnabled = true;
            this._defaultIsolationModeCombo.Items.AddRange(new object[] {
            SpyStudio.Export.ThinApp.ThinAppIsolationOption.Full,
            SpyStudio.Export.ThinApp.ThinAppIsolationOption.Merged,
            SpyStudio.Export.ThinApp.ThinAppIsolationOption.WriteCopy});
            this._defaultIsolationModeCombo.Location = new System.Drawing.Point(269, 3);
            this._defaultIsolationModeCombo.Name = "_defaultIsolationModeCombo";
            this._defaultIsolationModeCombo.Size = new System.Drawing.Size(164, 21);
            this._defaultIsolationModeCombo.TabIndex = 3;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(637, 370);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.21862F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 81.78138F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanelRight, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 70);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(637, 300);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.checkBoxStandard, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.checkBoxCustom, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(116, 300);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // checkBoxStandard
            // 
            this.checkBoxStandard.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxStandard.AutoSize = true;
            this.checkBoxStandard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxStandard.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlDark;
            this.checkBoxStandard.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.ActiveCaption;
            this.checkBoxStandard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxStandard.Location = new System.Drawing.Point(0, 0);
            this.checkBoxStandard.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxStandard.Name = "checkBoxStandard";
            this.checkBoxStandard.Size = new System.Drawing.Size(116, 25);
            this.checkBoxStandard.TabIndex = 0;
            this.checkBoxStandard.Text = "Standard";
            this.checkBoxStandard.UseVisualStyleBackColor = true;
            this.checkBoxStandard.Click += new System.EventHandler(this.CheckBoxStandardClick);
            // 
            // checkBoxCustom
            // 
            this.checkBoxCustom.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxCustom.AutoSize = true;
            this.checkBoxCustom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxCustom.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlDark;
            this.checkBoxCustom.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.ActiveCaption;
            this.checkBoxCustom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxCustom.Location = new System.Drawing.Point(0, 25);
            this.checkBoxCustom.Margin = new System.Windows.Forms.Padding(0);
            this.checkBoxCustom.Name = "checkBoxCustom";
            this.checkBoxCustom.Size = new System.Drawing.Size(116, 25);
            this.checkBoxCustom.TabIndex = 1;
            this.checkBoxCustom.Text = "Custom";
            this.checkBoxCustom.UseVisualStyleBackColor = true;
            this.checkBoxCustom.Click += new System.EventHandler(this.CheckBoxCustomClick);
            // 
            // flowLayoutPanelRight
            // 
            this.flowLayoutPanelRight.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanelRight.Controls.Add(this._standardPanel);
            this.flowLayoutPanelRight.Controls.Add(this._customPanel);
            this.flowLayoutPanelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelRight.Location = new System.Drawing.Point(116, 0);
            this.flowLayoutPanelRight.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelRight.Name = "flowLayoutPanelRight";
            this.flowLayoutPanelRight.Size = new System.Drawing.Size(521, 300);
            this.flowLayoutPanelRight.TabIndex = 1;
            this.flowLayoutPanelRight.SizeChanged += new System.EventHandler(this.FlowLayoutPanelRightSizeChanged);
            // 
            // FileSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(500, 0);
            this.Name = "FileSelect";
            this.Size = new System.Drawing.Size(637, 370);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.FileSelectQueryCancel);
            this.SetActive += new Wizard.UI.WizardPageEventHandler(this.FileSelectSetActive);
            //this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.OnWizardNext);
            this.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this._standardPanel.ResumeLayout(false);
            this._standardPanel.PerformLayout();
            this._customPanel.ResumeLayout(false);
            this._customPanel.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.flowLayoutPanelRight.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.TableLayoutPanel _standardPanel;
        protected System.Windows.Forms.TableLayoutPanel _customPanel;
        protected FileSystemViewer filesView;
        protected System.Windows.Forms.Label label1;
        public System.Windows.Forms.ComboBox _defaultIsolationModeCombo;
        private System.Windows.Forms.CheckBox checkBoxLocalAppData;
        private System.Windows.Forms.CheckBox checkBoxRoamingData;
        private System.Windows.Forms.CheckBox checkBoxAppRuntimes;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.CheckBox checkBoxStandard;
        private System.Windows.Forms.CheckBox checkBoxCustom;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelRight;
        private System.Windows.Forms.CheckBox checkBoxWholeProgramFiles;
    }
}
