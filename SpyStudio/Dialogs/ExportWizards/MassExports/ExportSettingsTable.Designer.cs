using System.Windows.Forms;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    partial class ExportSettingsTable
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SettingsTable = new System.Windows.Forms.TableLayoutPanel();
            this.AdvancedSettingsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.UsingAdvancedSettingsCheckBox = new System.Windows.Forms.CheckBox();
            this.AdvancedBtn = new System.Windows.Forms.Button();
            this.SimpleSettingsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.SettingsTable.SuspendLayout();
            this.AdvancedSettingsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // SettingsTable
            // 
            this.SettingsTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SettingsTable.ColumnCount = 1;
            this.SettingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingsTable.Controls.Add(this.AdvancedSettingsPanel, 0, 1);
            this.SettingsTable.Controls.Add(this.SimpleSettingsPanel, 0, 0);
            this.SettingsTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsTable.Location = new System.Drawing.Point(0, 0);
            this.SettingsTable.Name = "SettingsTable";
            this.SettingsTable.RowCount = 2;
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.SettingsTable.Size = new System.Drawing.Size(457, 354);
            this.SettingsTable.TabIndex = 1;
            // 
            // AdvancedSettingsPanel
            // 
            this.AdvancedSettingsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.AdvancedSettingsPanel.ColumnCount = 2;
            this.AdvancedSettingsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AdvancedSettingsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.AdvancedSettingsPanel.Controls.Add(this.UsingAdvancedSettingsCheckBox, 0, 0);
            this.AdvancedSettingsPanel.Controls.Add(this.AdvancedBtn, 1, 0);
            this.AdvancedSettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AdvancedSettingsPanel.Location = new System.Drawing.Point(3, 317);
            this.AdvancedSettingsPanel.Name = "AdvancedSettingsPanel";
            this.AdvancedSettingsPanel.RowCount = 1;
            this.AdvancedSettingsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AdvancedSettingsPanel.Size = new System.Drawing.Size(451, 34);
            this.AdvancedSettingsPanel.TabIndex = 19;
            // 
            // UsingAdvancedSettingsCheckBox
            // 
            this.UsingAdvancedSettingsCheckBox.AutoSize = true;
            this.UsingAdvancedSettingsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.UsingAdvancedSettingsCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UsingAdvancedSettingsCheckBox.Enabled = false;
            this.UsingAdvancedSettingsCheckBox.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.UsingAdvancedSettingsCheckBox.Location = new System.Drawing.Point(3, 3);
            this.UsingAdvancedSettingsCheckBox.Name = "UsingAdvancedSettingsCheckBox";
            this.UsingAdvancedSettingsCheckBox.Size = new System.Drawing.Size(345, 28);
            this.UsingAdvancedSettingsCheckBox.TabIndex = 18;
            this.UsingAdvancedSettingsCheckBox.Text = "Use advanced settings";
            this.UsingAdvancedSettingsCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.UsingAdvancedSettingsCheckBox.UseVisualStyleBackColor = true;
            this.UsingAdvancedSettingsCheckBox.CheckedChanged += new System.EventHandler(this.UsingAdvancedSettingsCheckBoxCheckedChanged);
            // 
            // AdvancedBtn
            // 
            this.AdvancedBtn.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.AdvancedBtn.Location = new System.Drawing.Point(357, 3);
            this.AdvancedBtn.Name = "AdvancedBtn";
            this.AdvancedBtn.Size = new System.Drawing.Size(91, 28);
            this.AdvancedBtn.TabIndex = 17;
            this.AdvancedBtn.Text = "Advanced >>";
            this.AdvancedBtn.UseVisualStyleBackColor = true;
            this.AdvancedBtn.Click += new System.EventHandler(this.AdvancedBtnClick);
            // 
            // SimpleSettingsPanel
            // 
            this.SimpleSettingsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SimpleSettingsPanel.ColumnCount = 1;
            this.SimpleSettingsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SimpleSettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SimpleSettingsPanel.Location = new System.Drawing.Point(3, 3);
            this.SimpleSettingsPanel.Name = "SimpleSettingsPanel";
            this.SimpleSettingsPanel.RowCount = 1;
            this.SimpleSettingsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SimpleSettingsPanel.Size = new System.Drawing.Size(451, 308);
            this.SimpleSettingsPanel.TabIndex = 20;
            // 
            // ExportSettingsTable
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.SettingsTable);
            this.Name = "ExportSettingsTable";
            this.Size = new System.Drawing.Size(457, 354);
            this.SettingsTable.ResumeLayout(false);
            this.AdvancedSettingsPanel.ResumeLayout(false);
            this.AdvancedSettingsPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel SettingsTable;
        private System.Windows.Forms.Button AdvancedBtn;
        public CheckBox UsingAdvancedSettingsCheckBox;
        private TableLayoutPanel AdvancedSettingsPanel;
        private TableLayoutPanel SimpleSettingsPanel;
    }
}
