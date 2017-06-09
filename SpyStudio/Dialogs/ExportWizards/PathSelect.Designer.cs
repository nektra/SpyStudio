using System.ComponentModel;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class PathSelect
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this._treeViewPath = new Aga.Controls.Tree.TreeViewAdv();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.columnHeaderType = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderProcess = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderKey = new System.Windows.Forms.ColumnHeader();
            this.tableLayoutPanelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(637, 64);
            this.Banner.Subtitle = "Select paths where layer files could be located";
            this.Banner.Title = "Search Paths";
            // 
            // _treeViewPath
            // 
            this._treeViewPath.BackColor = System.Drawing.SystemColors.Window;
            this._treeViewPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._treeViewPath.DefaultToolTipProvider = null;
            this._treeViewPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this._treeViewPath.DragDropMarkColor = System.Drawing.Color.Black;
            this._treeViewPath.FullRowSelect = true;
            this._treeViewPath.Indent = 7;
            this._treeViewPath.LineColor = System.Drawing.SystemColors.ControlDark;
            this._treeViewPath.Location = new System.Drawing.Point(3, 73);
            this._treeViewPath.Model = null;
            this._treeViewPath.Name = "_treeViewPath";
            this._treeViewPath.SelectedNode = null;
            this._treeViewPath.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.Multi;
            this._treeViewPath.Size = new System.Drawing.Size(631, 294);
            this._treeViewPath.TabIndex = 1;
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 637F));
            this.tableLayoutPanelMain.Controls.Add(this._treeViewPath, 0, 1);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 2;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(637, 370);
            this.tableLayoutPanelMain.TabIndex = 2;
            // 
            // columnHeaderType
            // 
            this.columnHeaderType.Text = "Type";
            this.columnHeaderType.Width = 147;
            // 
            // columnHeaderProcess
            // 
            this.columnHeaderProcess.Text = "Process Wildcard";
            this.columnHeaderProcess.Width = 207;
            // 
            // columnHeaderKey
            // 
            this.columnHeaderKey.Text = "Key Wildcard";
            this.columnHeaderKey.Width = 234;
            // 
            // PathSelect
            // 
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Name = "PathSelect";
            this.Size = new System.Drawing.Size(637, 370);
            this.SetActive += new WizardPageEventHandler(this.OnSetActive);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.OnWizardNext);
            this.Controls.SetChildIndex(this.tableLayoutPanelMain, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TreeViewAdv _treeViewPath;
        private TableLayoutPanel tableLayoutPanelMain;
        private ColumnHeader columnHeaderType;
        private ColumnHeader columnHeaderProcess;
        private ColumnHeader columnHeaderKey;
    }
}