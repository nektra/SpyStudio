using SpyStudio.Export.ThinApp;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class ProgressPage : IExportProgressControl
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
            this._mainPanel = new System.Windows.Forms.TableLayoutPanel();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._logListView = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this._logContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._mainPanel.SuspendLayout();
            this._logContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(682, 64);
            this.Banner.Subtitle = "Exporting...";
            this.Banner.Title = "Export Progress";
            // 
            // _mainPanel
            // 
            this._mainPanel.ColumnCount = 1;
            this._mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.Controls.Add(this._progressBar, 0, 1);
            this._mainPanel.Controls.Add(this._logListView, 0, 0);
            this._mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mainPanel.Location = new System.Drawing.Point(0, 64);
            this._mainPanel.Name = "_mainPanel";
            this._mainPanel.RowCount = 3;
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this._mainPanel.Size = new System.Drawing.Size(682, 309);
            this._mainPanel.TabIndex = 1;
            // 
            // _progressBar
            // 
            this._progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._progressBar.Location = new System.Drawing.Point(13, 265);
            this._progressBar.Margin = new System.Windows.Forms.Padding(13, 0, 13, 0);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(656, 36);
            this._progressBar.TabIndex = 2;
            // 
            // _logListView
            // 
            this._logListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
            this._logListView.ContextMenuStrip = this._logContextMenu;
            this._logListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._logListView.FullRowSelect = true;
            this._logListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._logListView.Location = new System.Drawing.Point(3, 3);
            this._logListView.Name = "_logListView";
            this._logListView.Size = new System.Drawing.Size(676, 259);
            this._logListView.TabIndex = 0;
            this._logListView.UseCompatibleStateImageBehavior = false;
            this._logListView.View = System.Windows.Forms.View.Details;
            this._logListView.SizeChanged += new System.EventHandler(this.ProgressListViewSizeChanged);
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "";
            this.columnHeaderName.Width = 672;
            // 
            // _logContextMenu
            // 
            this._logContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToClipboardToolStripMenuItem});
            this._logContextMenu.Name = "_logContextMenu";
            this._logContextMenu.Size = new System.Drawing.Size(214, 26);
            // 
            // copyToClipboardToolStripMenuItem
            // 
            this.copyToClipboardToolStripMenuItem.Name = "copyToClipboardToolStripMenuItem";
            this.copyToClipboardToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToClipboardToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.copyToClipboardToolStripMenuItem.Text = "Copy to Clipboard";
            this.copyToClipboardToolStripMenuItem.Click += new System.EventHandler(this.CopyToClipboardToolStripMenuItemClick);
            // 
            // ProgressPage
            // 
            this.Controls.Add(this._mainPanel);
            this.Name = "ProgressPage";
            this.Size = new System.Drawing.Size(682, 373);
            this.SetActive += new WizardPageEventHandler(this.OnSetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this._mainPanel, 0);
            this._mainPanel.ResumeLayout(false);
            this._logContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _mainPanel;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ContextMenuStrip _logContextMenu;
        private System.Windows.Forms.ToolStripMenuItem copyToClipboardToolStripMenuItem;
        protected System.Windows.Forms.ListView _logListView;
        protected System.Windows.Forms.ProgressBar _progressBar;
    }
}
