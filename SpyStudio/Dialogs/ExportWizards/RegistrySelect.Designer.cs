using System.ComponentModel;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class RegistrySelect
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
            this.components = new System.ComponentModel.Container();
            this._customPanel = new System.Windows.Forms.TableLayoutPanel();
            this._defaultIsolationModeCombo = new System.Windows.Forms.ComboBox();
            this._defaultIsolationModeLabel = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.registryTreeView = new SpyStudio.Registry.Controls.RegistryTree();
            this.listViewValues = new SpyStudio.Registry.Controls.RegistryValueList();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxStandard = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxCustom = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanelRight = new System.Windows.Forms.FlowLayoutPanel();
            this._standardPanel = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxClasses = new System.Windows.Forms.CheckBox();
            this.checkBoxRelatedToAppMachine = new System.Windows.Forms.CheckBox();
            this.checkBoxRelatedToAppUser = new System.Windows.Forms.CheckBox();
            this._customPanel.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.flowLayoutPanelRight.SuspendLayout();
            this._standardPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(637, 64);
            this.Banner.Title = "Registry Keys";
            // 
            // _customPanel
            // 
            this._customPanel.ColumnCount = 2;
            this._customPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._customPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this._customPanel.Controls.Add(this._defaultIsolationModeCombo, 1, 0);
            this._customPanel.Controls.Add(this._defaultIsolationModeLabel, 0, 0);
            this._customPanel.Controls.Add(this.splitContainer1, 0, 1);
            this._customPanel.Location = new System.Drawing.Point(0, 96);
            this._customPanel.Margin = new System.Windows.Forms.Padding(0);
            this._customPanel.Name = "_customPanel";
            this._customPanel.RowCount = 2;
            this._customPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._customPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._customPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._customPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._customPanel.Size = new System.Drawing.Size(520, 206);
            this._customPanel.TabIndex = 3;
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
            this._defaultIsolationModeCombo.Location = new System.Drawing.Point(353, 3);
            this._defaultIsolationModeCombo.Name = "_defaultIsolationModeCombo";
            this._defaultIsolationModeCombo.Size = new System.Drawing.Size(164, 21);
            this._defaultIsolationModeCombo.TabIndex = 4;
            // 
            // _defaultIsolationModeLabel
            // 
            this._defaultIsolationModeLabel.AutoSize = true;
            this._defaultIsolationModeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._defaultIsolationModeLabel.Location = new System.Drawing.Point(3, 0);
            this._defaultIsolationModeLabel.Name = "_defaultIsolationModeLabel";
            this._defaultIsolationModeLabel.Size = new System.Drawing.Size(344, 26);
            this._defaultIsolationModeLabel.TabIndex = 3;
            this._defaultIsolationModeLabel.Text = "Default Isolation Mode:";
            this._defaultIsolationModeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // splitContainer1
            // 
            this._customPanel.SetColumnSpan(this.splitContainer1, 2);
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 29);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.registryTreeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.listViewValues);
            this.splitContainer1.Size = new System.Drawing.Size(514, 174);
            this.splitContainer1.SplitterDistance = 350;
            this.splitContainer1.TabIndex = 5;
            // 
            // registryTreeView
            // 
            this.registryTreeView.BackColor = System.Drawing.SystemColors.Window;
            this.registryTreeView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.registryTreeView.Controller = null;
            this.registryTreeView.DefaultToolTipProvider = null;
            this.registryTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.registryTreeView.DragDropMarkColor = System.Drawing.Color.Black;
            this.registryTreeView.File1TraceId = ((uint)(0u));
            this.registryTreeView.File2TraceId = ((uint)(0u));
            this.registryTreeView.GotoVisible = true;
            this.registryTreeView.Indent = 7;
            this.registryTreeView.LineColor = System.Drawing.SystemColors.ControlDark;
            this.registryTreeView.LoadOnDemand = true;
            this.registryTreeView.Location = new System.Drawing.Point(0, 0);
            this.registryTreeView.MergeLayerPaths = false;
            this.registryTreeView.MergeWow = false;
            this.registryTreeView.Name = "registryTreeView";
            this.registryTreeView.RedirectClasses = false;
            this.registryTreeView.SelectedNode = null;
            this.registryTreeView.ShowHScrollBar = true;
            this.registryTreeView.ShowIsolationOptions = false;
            this.registryTreeView.ShowLines = false;
            this.registryTreeView.ShowVScrollBar = true;
            this.registryTreeView.Size = new System.Drawing.Size(350, 174);
            this.registryTreeView.TabIndex = 1;
            this.registryTreeView.UseColumns = true;
            this.registryTreeView.ValuesView = this.listViewValues;
            this.registryTreeView.SizeChanged += new System.EventHandler(this.RegistryTreeViewSizeChanged);
            // 
            // listViewValues
            // 
            this.listViewValues.Controller = null;
            this.listViewValues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewValues.FullRowSelect = true;
            this.listViewValues.IgnoreCase = true;
            this.listViewValues.Location = new System.Drawing.Point(0, 0);
            this.listViewValues.Name = "listViewValues";
            this.listViewValues.Size = new System.Drawing.Size(160, 174);
            this.listViewValues.SortColumn = 0;
            this.listViewValues.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewValues.TabIndex = 0;
            this.listViewValues.UseCompatibleStateImageBehavior = false;
            this.listViewValues.View = System.Windows.Forms.View.Details;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(200, 100);
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
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 64);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 306F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(637, 306);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.21862F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 81.78138F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanelRight, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 306F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(637, 306);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.checkBoxStandard, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.checkBoxCustom, 0, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(116, 306);
            this.tableLayoutPanel4.TabIndex = 0;
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
            this.flowLayoutPanelRight.Size = new System.Drawing.Size(521, 306);
            this.flowLayoutPanelRight.TabIndex = 1;
            this.flowLayoutPanelRight.SizeChanged += new System.EventHandler(this.FlowLayoutPanelRightSizeChanged);
            // 
            // _standardPanel
            // 
            this._standardPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._standardPanel.ColumnCount = 1;
            this._standardPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._standardPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._standardPanel.Controls.Add(this.checkBoxClasses, 0, 0);
            this._standardPanel.Controls.Add(this.checkBoxRelatedToAppMachine, 0, 1);
            this._standardPanel.Controls.Add(this.checkBoxRelatedToAppUser, 0, 2);
            this._standardPanel.Location = new System.Drawing.Point(0, 0);
            this._standardPanel.Margin = new System.Windows.Forms.Padding(0);
            this._standardPanel.Name = "_standardPanel";
            this._standardPanel.RowCount = 4;
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this._standardPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._standardPanel.Size = new System.Drawing.Size(356, 96);
            this._standardPanel.TabIndex = 1;
            this._standardPanel.Visible = false;
            // 
            // checkBoxClasses
            // 
            this.checkBoxClasses.AutoSize = true;
            this.checkBoxClasses.Location = new System.Drawing.Point(3, 3);
            this.checkBoxClasses.Name = "checkBoxClasses";
            this.checkBoxClasses.Size = new System.Drawing.Size(264, 17);
            this.checkBoxClasses.TabIndex = 0;
            this.checkBoxClasses.Text = "Register all COM Classes, Interfaces and TypeLibs";
            this.checkBoxClasses.UseVisualStyleBackColor = true;
            this.checkBoxClasses.Click += new System.EventHandler(this.CheckBoxClassesClick);
            // 
            // checkBoxRelatedToAppMachine
            // 
            this.checkBoxRelatedToAppMachine.AutoSize = true;
            this.checkBoxRelatedToAppMachine.Location = new System.Drawing.Point(3, 26);
            this.checkBoxRelatedToAppMachine.Name = "checkBoxRelatedToAppMachine";
            this.checkBoxRelatedToAppMachine.Size = new System.Drawing.Size(291, 17);
            this.checkBoxRelatedToAppMachine.TabIndex = 1;
            this.checkBoxRelatedToAppMachine.Text = "Import Whole Machine\'s Keys Related to the Application";
            this.checkBoxRelatedToAppMachine.UseVisualStyleBackColor = true;
            // 
            // checkBoxRelatedToAppUser
            // 
            this.checkBoxRelatedToAppUser.AutoSize = true;
            this.checkBoxRelatedToAppUser.Location = new System.Drawing.Point(3, 49);
            this.checkBoxRelatedToAppUser.Name = "checkBoxRelatedToAppUser";
            this.checkBoxRelatedToAppUser.Size = new System.Drawing.Size(272, 17);
            this.checkBoxRelatedToAppUser.TabIndex = 1;
            this.checkBoxRelatedToAppUser.Text = "Import Whole User\'s Keys Related to the Application";
            this.checkBoxRelatedToAppUser.UseVisualStyleBackColor = true;
            // 
            // RegistrySelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "RegistrySelect";
            this.Size = new System.Drawing.Size(637, 370);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.RegistrySelectQueryCancel);
            this.SetActive += new WizardPageEventHandler(this.RegistrySelectSetActive);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.OnWizardNext);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
            this._customPanel.ResumeLayout(false);
            this._customPanel.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.flowLayoutPanelRight.ResumeLayout(false);
            this._standardPanel.ResumeLayout(false);
            this._standardPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _customPanel;
        private System.Windows.Forms.Label _defaultIsolationModeLabel;
        protected System.Windows.Forms.ComboBox _defaultIsolationModeCombo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.CheckBox checkBoxStandard;
        private System.Windows.Forms.CheckBox checkBoxCustom;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelRight;
        private System.Windows.Forms.TableLayoutPanel _standardPanel;
        private System.Windows.Forms.CheckBox checkBoxClasses;
        private System.Windows.Forms.SplitContainer splitContainer1;
        protected RegistryTree registryTreeView;
        private RegistryValueList listViewValues;
        private System.Windows.Forms.CheckBox checkBoxRelatedToAppMachine;
        private System.Windows.Forms.CheckBox checkBoxRelatedToAppUser;
    }
}