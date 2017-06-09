using SpyStudio.FileSystem;
using SpyStudio.Main;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class FileDestinationSelect
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
            this.listViewFiles = new SpyStudio.FileSystem.FileSystemExplorer();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(663, 64);
            this.Banner.Title = "File Destination";
            // 
            // listViewFiles
            // 
            this.listViewFiles.BackColor = System.Drawing.SystemColors.Window;
            this.listViewFiles.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listViewFiles.DefaultToolTipProvider = null;
            this.listViewFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewFiles.DragDropMarkColor = System.Drawing.Color.Black;
            this.listViewFiles.FullRowSelect = true;
            this.listViewFiles.Indent = 7;
            this.listViewFiles.LineColor = System.Drawing.SystemColors.ControlDark;
            this.listViewFiles.Location = new System.Drawing.Point(3, 73);
            this.listViewFiles.Name = "listViewFiles";
            this.listViewFiles.SearchPaths = null;
            this.listViewFiles.SelectedNode = null;
            this.listViewFiles.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.Multi;
            this.listViewFiles.ShowHScrollBar = true;
            this.listViewFiles.ShowLines = false;
            this.listViewFiles.ShowNodeToolTips = true;
            this.listViewFiles.ShowVScrollBar = true;
            this.listViewFiles.Size = new System.Drawing.Size(657, 294);
            this.listViewFiles.TabIndex = 1;
            this.listViewFiles.UseColumns = true;
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 663F));
            this.tableLayoutPanelMain.Controls.Add(this.listViewFiles, 0, 1);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 2;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(663, 370);
            this.tableLayoutPanelMain.TabIndex = 2;
            // 
            // FileDestinationSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Name = "FileDestinationSelect";
            this.Size = new System.Drawing.Size(663, 370);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.FileDestinationSelectQueryCancel);
            this.SetActive += new WizardPageEventHandler(this.FileDestinationSelectSetActive);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.OnWizardNext);
            this.Controls.SetChildIndex(this.tableLayoutPanelMain, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private FileSystemExplorer listViewFiles;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
    }
}