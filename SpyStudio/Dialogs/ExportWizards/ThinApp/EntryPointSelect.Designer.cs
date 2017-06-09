using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    partial class EntryPointSelect
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
            this._entryPointListLabel = new System.Windows.Forms.Label();
            this._mainPanel = new System.Windows.Forms.TableLayoutPanel();
            this._entryPointListView = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPath = new System.Windows.Forms.ColumnHeader();
            this._mainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(663, 64);
            this.Banner.Title = "Entry Points";
            // 
            // _entryPointListLabel
            // 
            this._entryPointListLabel.AutoSize = true;
            this._entryPointListLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._entryPointListLabel.Location = new System.Drawing.Point(3, 0);
            this._entryPointListLabel.Name = "_entryPointListLabel";
            this._entryPointListLabel.Size = new System.Drawing.Size(657, 20);
            this._entryPointListLabel.TabIndex = 2;
            this._entryPointListLabel.Text = "Entry Points:";
            this._entryPointListLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _mainPanel
            // 
            this._mainPanel.ColumnCount = 1;
            this._mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.Controls.Add(this._entryPointListLabel, 0, 0);
            this._mainPanel.Controls.Add(this._entryPointListView, 0, 1);
            this._mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mainPanel.Location = new System.Drawing.Point(0, 64);
            this._mainPanel.Name = "_mainPanel";
            this._mainPanel.RowCount = 2;
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._mainPanel.Size = new System.Drawing.Size(663, 306);
            this._mainPanel.TabIndex = 3;
            // 
            // _entryPointListView
            // 
            this._entryPointListView.CheckBoxes = true;
            this._entryPointListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderPath});
            this._entryPointListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._entryPointListView.FullRowSelect = true;
            this._entryPointListView.Location = new System.Drawing.Point(3, 23);
            this._entryPointListView.Name = "_entryPointListView";
            this._entryPointListView.Size = new System.Drawing.Size(657, 280);
            this._entryPointListView.TabIndex = 3;
            this._entryPointListView.UseCompatibleStateImageBehavior = false;
            this._entryPointListView.View = System.Windows.Forms.View.Details;
            this._entryPointListView.SizeChanged += new System.EventHandler(this.EntryPointListViewSizeChanged);
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "Name";
            this.columnHeaderName.Width = 224;
            // 
            // columnHeaderPath
            // 
            this.columnHeaderPath.Text = "Path";
            this.columnHeaderPath.Width = 429;
            // 
            // EntryPointSelect
            // 
            this.Controls.Add(this._mainPanel);
            this.Name = "EntryPointSelect";
            this.Size = new System.Drawing.Size(663, 370);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.ThinAppEntryPointSelectQueryCancel);
            this.SetActive += new WizardPageEventHandler(this.ThinAppEntryPointSelectSetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this._mainPanel, 0);
            this._mainPanel.ResumeLayout(false);
            this._mainPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _entryPointListLabel;
        private System.Windows.Forms.TableLayoutPanel _mainPanel;
        private System.Windows.Forms.ListView _entryPointListView;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderPath;
    }
}
