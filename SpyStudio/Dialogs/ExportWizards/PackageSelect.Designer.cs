using SpyStudio.Extensions;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class PackageSelect
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
            this.PackageList = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this._packageListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newPackageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renamePackageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deletePackageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PackageListLabel = new System.Windows.Forms.Label();
            this._mainPanel = new System.Windows.Forms.TableLayoutPanel();
            this._captureTypeLabel = new System.Windows.Forms.Label();
            this._checkerTypeCombo = new System.Windows.Forms.ComboBox();
            this._packageListContextMenu.SuspendLayout();
            this._mainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(663, 64);
            this.Banner.Subtitle = "Choose a package to modify or create a new one.";
            this.Banner.Title = "Package";
            // 
            // PackageList
            // 
            this.PackageList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
            this._mainPanel.SetColumnSpan(this.PackageList, 3);
            this.PackageList.ContextMenuStrip = this._packageListContextMenu;
            this.PackageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PackageList.FullRowSelect = true;
            this.PackageList.HideSelection = false;
            this.PackageList.LabelEdit = true;
            this.PackageList.Location = new System.Drawing.Point(3, 28);
            this.PackageList.Name = "PackageList";
            this.PackageList.OwnerDraw = true;
            this.PackageList.Size = new System.Drawing.Size(657, 275);
            this.PackageList.TabIndex = 1;
            this.PackageList.UseCompatibleStateImageBehavior = false;
            this.PackageList.View = System.Windows.Forms.View.Details;
            this.PackageList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.PackageListMouseDoubleClick);
            this.PackageList.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.PackageListDrawColumnHeader);
            this.PackageList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.PackageListAfterLabelEdit);
            this.PackageList.SizeChanged += new System.EventHandler(this.PackageListSizeChanged);
            this.PackageList.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.PackageListItemSelectionChanged);
            this.PackageList.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.PackageListDrawSubItem);
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "Name";
            this.columnHeaderName.Width = 649;
            // 
            // _packageListContextMenu
            // 
            this._packageListContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newPackageToolStripMenuItem,
            this.renamePackageToolStripMenuItem,
            this.deletePackageToolStripMenuItem});
            this._packageListContextMenu.Name = "_packageListContextMenu";
            this._packageListContextMenu.Size = new System.Drawing.Size(142, 70);
            this._packageListContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.PackageListContextMenuOpening);
            // 
            // newPackageToolStripMenuItem
            // 
            this.newPackageToolStripMenuItem.Name = "newPackageToolStripMenuItem";
            this.newPackageToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newPackageToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.newPackageToolStripMenuItem.Text = "New";
            this.newPackageToolStripMenuItem.Click += new System.EventHandler(this.NewPackageToolStripMenuItemClick);
            // 
            // renamePackageToolStripMenuItem
            // 
            this.renamePackageToolStripMenuItem.Name = "renamePackageToolStripMenuItem";
            this.renamePackageToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.renamePackageToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.renamePackageToolStripMenuItem.Text = "Rename";
            this.renamePackageToolStripMenuItem.Click += new System.EventHandler(this.RenamePackageToolStripMenuItemClick);
            // 
            // deletePackageToolStripMenuItem
            // 
            this.deletePackageToolStripMenuItem.Name = "deletePackageToolStripMenuItem";
            this.deletePackageToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deletePackageToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.deletePackageToolStripMenuItem.Text = "Delete";
            this.deletePackageToolStripMenuItem.Click += new System.EventHandler(this.DeletePackageToolStripMenuItemClick);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this.renameToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.renameToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.renameToolStripMenuItem.Text = "Rename";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            // 
            // PackageListLabel
            // 
            this.PackageListLabel.AutoSize = true;
            this.PackageListLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PackageListLabel.Location = new System.Drawing.Point(3, 0);
            this.PackageListLabel.Name = "PackageListLabel";
            this.PackageListLabel.Size = new System.Drawing.Size(375, 25);
            this.PackageListLabel.TabIndex = 2;
            this.PackageListLabel.Text = "Available packages:";
            this.PackageListLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _mainPanel
            // 
            this._mainPanel.ColumnCount = 3;
            this._mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 131F));
            this._mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 151F));
            this._mainPanel.Controls.Add(this.PackageListLabel, 0, 0);
            this._mainPanel.Controls.Add(this.PackageList, 0, 1);
            this._mainPanel.Controls.Add(this._captureTypeLabel, 1, 0);
            this._mainPanel.Controls.Add(this._checkerTypeCombo, 2, 0);
            this._mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mainPanel.Location = new System.Drawing.Point(0, 64);
            this._mainPanel.Name = "_mainPanel";
            this._mainPanel.RowCount = 2;
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this._mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._mainPanel.Size = new System.Drawing.Size(663, 306);
            this._mainPanel.TabIndex = 4;
            // 
            // _captureTypeLabel
            // 
            this._captureTypeLabel.AutoSize = true;
            this._captureTypeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._captureTypeLabel.Location = new System.Drawing.Point(384, 0);
            this._captureTypeLabel.Name = "_captureTypeLabel";
            this._captureTypeLabel.Size = new System.Drawing.Size(125, 25);
            this._captureTypeLabel.TabIndex = 3;
            this._captureTypeLabel.Text = "SmartChecker:";
            this._captureTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _checkerTypeCombo
            // 
            this._checkerTypeCombo.Dock = System.Windows.Forms.DockStyle.Fill;
            this._checkerTypeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._checkerTypeCombo.FormattingEnabled = true;
            this._checkerTypeCombo.Location = new System.Drawing.Point(515, 3);
            this._checkerTypeCombo.Name = "_checkerTypeCombo";
            this._checkerTypeCombo.Size = new System.Drawing.Size(145, 21);
            this._checkerTypeCombo.TabIndex = 4;
            this._checkerTypeCombo.SelectionChangeCommitted += new System.EventHandler(this.CaptureTypeComboSelectionChangeCommitted);
            // 
            // PackageSelect
            // 
            this.Controls.Add(this._mainPanel);
            this.Name = "PackageSelect";
            this.Size = new System.Drawing.Size(663, 370);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.PackageSelectQueryCancel);
            this.SetActive += new WizardPageEventHandler(this.PackageSelectSetActive);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.OnWizardNext);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this._mainPanel, 0);
            this._packageListContextMenu.ResumeLayout(false);
            this._mainPanel.ResumeLayout(false);
            this._mainPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.ListView PackageList;
        protected System.Windows.Forms.TableLayoutPanel _mainPanel;
        protected System.Windows.Forms.ColumnHeader columnHeaderName;
        protected System.Windows.Forms.ContextMenuStrip _packageListContextMenu;
        protected System.Windows.Forms.ToolStripMenuItem newPackageToolStripMenuItem;
        protected System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        protected System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        protected System.Windows.Forms.Label _captureTypeLabel;
        protected System.Windows.Forms.ComboBox _checkerTypeCombo;
        protected System.Windows.Forms.Label PackageListLabel;
        protected System.Windows.Forms.ToolStripMenuItem renamePackageToolStripMenuItem;
        protected System.Windows.Forms.ToolStripMenuItem deletePackageToolStripMenuItem;
    }
}
