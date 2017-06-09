using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    partial class MassProgressPage
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
            this._mainPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.ProgressDetails = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this._mainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(722, 64);
            this.Banner.Subtitle = "Exporting...";
            this.Banner.Title = "Export Progress";
            // 
            // _mainPanel
            // 
            this._mainPanel.ColumnCount = 1;
            this._mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.Controls.Add(this.ProgressBar, 0, 1);
            this._mainPanel.Controls.Add(this.ProgressDetails, 0, 0);
            this._mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mainPanel.Location = new System.Drawing.Point(0, 64);
            this._mainPanel.Name = "_mainPanel";
            this._mainPanel.RowCount = 3;
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this._mainPanel.Size = new System.Drawing.Size(722, 392);
            this._mainPanel.TabIndex = 2;
            // 
            // ProgressBar
            // 
            this.ProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgressBar.Location = new System.Drawing.Point(13, 348);
            this.ProgressBar.Margin = new System.Windows.Forms.Padding(13, 0, 13, 0);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(696, 36);
            this.ProgressBar.TabIndex = 2;
            // 
            // ProgressDetails
            // 
            this.ProgressDetails.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
            this.ProgressDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgressDetails.FullRowSelect = true;
            this.ProgressDetails.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.ProgressDetails.Location = new System.Drawing.Point(3, 3);
            this.ProgressDetails.Name = "ProgressDetails";
            this.ProgressDetails.Size = new System.Drawing.Size(716, 342);
            this.ProgressDetails.TabIndex = 0;
            this.ProgressDetails.UseCompatibleStateImageBehavior = false;
            this.ProgressDetails.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "";
            this.columnHeaderName.Width = 672;
            // 
            // MassProgressPage
            // 
            this.Controls.Add(this._mainPanel);
            this.Name = "MassProgressPage";
            this.Size = new System.Drawing.Size(722, 456);
            this.SetActive += new WizardPageEventHandler(this.MassProgressPage_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this._mainPanel, 0);
            this._mainPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _mainPanel;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.ListView ProgressDetails;
        private System.Windows.Forms.ColumnHeader columnHeaderName;

    }
}
