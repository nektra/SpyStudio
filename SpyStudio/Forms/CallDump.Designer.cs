using System.Windows.Forms;
using SpyStudio.COM.Controls;
using SpyStudio.EventSummary;
using SpyStudio.FileSystem;
using SpyStudio.Main;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;
using SpyStudio.Windows.Controls;

namespace SpyStudio.Forms
{
    partial class CallDump
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
            this.components = new System.ComponentModel.Container();
            this.tabControlData = new System.Windows.Forms.TabControl();
            this.tabCom = new SpyStudio.Forms.ComTab();
            this.listViewCom = new ComObjectListView();
            this.tabWindow = new SpyStudio.Forms.WindowTab();
            this.listViewWindow = new WindowListView();
            this.tabFile = new SpyStudio.Forms.FilesTab();
            this.listViewFileSystem = new SpyStudio.FileSystem.FileSystemViewer();
            this.tabRegistry = new SpyStudio.Forms.RegistryTab();
            this.splitRegistry = new System.Windows.Forms.SplitContainer();
            this.treeViewRegistry = new RegistryTree();
            this.listViewValues = new RegistryValueList();
            this._contextMenuStripRegistryValues = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tabTrace = new SpyStudio.Forms.TraceTab();
            this.treeViewTrace = new SpyStudio.Main.TraceTreeView();
            this.tableLayoutTrace = new System.Windows.Forms.TableLayoutPanel();
            this.eventSummary = new EventSummaryGraphic();
            this.contextMenuStripTrace = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStripRegistry = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStripCom = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStripWindow = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStripFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.tableLayoutTrace.SuspendLayout();
            this.tabControlData.SuspendLayout();
            this.tabCom.SuspendLayout();
            this.tabWindow.SuspendLayout();
            this.tabFile.SuspendLayout();
            this.tabRegistry.SuspendLayout();
            this.splitRegistry.Panel1.SuspendLayout();
            this.splitRegistry.Panel2.SuspendLayout();
            this.splitRegistry.SuspendLayout();
            this.tabTrace.SuspendLayout();
            this.contextMenuStripTrace.SuspendLayout();
            this.contextMenuStripCom.SuspendLayout();
            this.contextMenuStripWindow.SuspendLayout();
            this.contextMenuStripFiles.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlData
            // 
            this.tabControlData.Controls.Add(this.tabCom);
            this.tabControlData.Controls.Add(this.tabWindow);
            this.tabControlData.Controls.Add(this.tabFile);
            this.tabControlData.Controls.Add(this.tabRegistry);
            this.tabControlData.Controls.Add(this.tabTrace);
            this.tabControlData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlData.Location = new System.Drawing.Point(0, 0);
            this.tabControlData.Margin = new System.Windows.Forms.Padding(0);
            this.tabControlData.Name = "tabControlData";
            this.tabControlData.Padding = new System.Drawing.Point(0, 0);
            this.tabControlData.SelectedIndex = 0;
            this.tabControlData.Size = new System.Drawing.Size(674, 319);
            this.tabControlData.TabIndex = 0;
            this.tabControlData.Selected += new System.Windows.Forms.TabControlEventHandler(this.TabControlDataSelected);
            // 
            // tabCom
            // 
            this.tabCom.Controls.Add(this.listViewCom);
            this.tabCom.Location = new System.Drawing.Point(4, 22);
            this.tabCom.Name = "tabCom";
            this.tabCom.Size = new System.Drawing.Size(666, 293);
            this.tabCom.TabIndex = 0;
            this.tabCom.Text = "COM Objects";
            this.tabCom.UseVisualStyleBackColor = true;
            // 
            // listViewCom
            // 
            this.listViewCom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewCom.FullRowSelect = true;
            this.listViewCom.HideSelection = false;
            this.listViewCom.IgnoreCase = true;
            this.listViewCom.Location = new System.Drawing.Point(0, 0);
            this.listViewCom.Margin = new System.Windows.Forms.Padding(0);
            this.listViewCom.Name = "listViewCom";
            this.listViewCom.Size = new System.Drawing.Size(666, 293);
            this.listViewCom.SortColumn = 0;
            this.listViewCom.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewCom.TabIndex = 0;
            this.listViewCom.UseCompatibleStateImageBehavior = false;
            this.listViewCom.View = System.Windows.Forms.View.Details;
            // 
            // tabWindow
            // 
            this.tabWindow.Controls.Add(this.listViewWindow);
            this.tabWindow.Location = new System.Drawing.Point(4, 22);
            this.tabWindow.Name = "tabWindow";
            this.tabWindow.Size = new System.Drawing.Size(666, 293);
            this.tabWindow.TabIndex = 4;
            this.tabWindow.Text = "Windows";
            this.tabWindow.UseVisualStyleBackColor = true;
            // 
            // listViewWindow
            // 
            this.listViewWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewWindow.FullRowSelect = true;
            this.listViewWindow.HideSelection = false;
            this.listViewWindow.IgnoreCase = true;
            this.listViewWindow.Location = new System.Drawing.Point(0, 0);
            this.listViewWindow.Name = "listViewWindow";
            this.listViewWindow.Size = new System.Drawing.Size(666, 293);
            this.listViewWindow.SortColumn = 0;
            this.listViewWindow.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewWindow.TabIndex = 0;
            this.listViewWindow.UseCompatibleStateImageBehavior = false;
            this.listViewWindow.View = System.Windows.Forms.View.Details;
            // 
            // tabFile
            // 
            this.tabFile.Controls.Add(this.listViewFileSystem);
            this.tabFile.Location = new System.Drawing.Point(4, 22);
            this.tabFile.Margin = new System.Windows.Forms.Padding(0);
            this.tabFile.Name = "tabFile";
            this.tabFile.Size = new System.Drawing.Size(666, 293);
            this.tabFile.TabIndex = 1;
            this.tabFile.Text = "Files";
            this.tabFile.UseVisualStyleBackColor = true;
            // 
            // listViewFileSystem
            // 
            this.listViewFileSystem.BackColor = System.Drawing.SystemColors.Window;
            this.listViewFileSystem.CheckBoxes = false;
            this.listViewFileSystem.CompareMode = false;
            this.listViewFileSystem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewFileSystem.File1BackgroundColor = System.Drawing.Color.Empty;
            this.listViewFileSystem.File1TraceId = ((uint)(0u));
            this.listViewFileSystem.File2BackgroundColor = System.Drawing.Color.Empty;
            this.listViewFileSystem.File2TraceId = ((uint)(0u));
            this.listViewFileSystem.HideQueryAttributes = false;
            this.listViewFileSystem.Location = new System.Drawing.Point(0, 0);
            this.listViewFileSystem.MergeLayerPaths = false;
            this.listViewFileSystem.MergeWowPaths = false;
            this.listViewFileSystem.Name = "listViewFileSystem";
            this.listViewFileSystem.PathNormalizer = null;
            this.listViewFileSystem.ShowIsolationOptions = false;
            this.listViewFileSystem.ShowStartupModules = true;
            this.listViewFileSystem.Size = new System.Drawing.Size(666, 293);
            this.listViewFileSystem.TabIndex = 0;
            this.listViewFileSystem.TreeMode = true;
            // 
            // tabRegistry
            // 
            this.tabRegistry.Controls.Add(this.splitRegistry);
            this.tabRegistry.Location = new System.Drawing.Point(4, 22);
            this.tabRegistry.Margin = new System.Windows.Forms.Padding(0);
            this.tabRegistry.Name = "tabRegistry";
            this.tabRegistry.Size = new System.Drawing.Size(666, 293);
            this.tabRegistry.TabIndex = 2;
            this.tabRegistry.Text = "Registry";
            this.tabRegistry.UseVisualStyleBackColor = true;
            // 
            // splitRegistry
            // 
            this.splitRegistry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRegistry.Location = new System.Drawing.Point(0, 0);
            this.splitRegistry.Margin = new System.Windows.Forms.Padding(0);
            this.splitRegistry.Name = "splitRegistry";
            // 
            // splitRegistry.Panel1
            // 
            this.splitRegistry.Panel1.Controls.Add(this.treeViewRegistry);
            // 
            // splitRegistry.Panel2
            // 
            this.splitRegistry.Panel2.Controls.Add(this.listViewValues);
            this.splitRegistry.Size = new System.Drawing.Size(666, 293);
            this.splitRegistry.SplitterDistance = 217;
            this.splitRegistry.TabIndex = 1;
            // 
            // treeViewRegistry
            // 
            this.treeViewRegistry.BackColor = System.Drawing.SystemColors.Window;
            this.treeViewRegistry.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.treeViewRegistry.CheckBoxes = false;
            this.treeViewRegistry.DefaultToolTipProvider = null;
            this.treeViewRegistry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewRegistry.DragDropMarkColor = System.Drawing.Color.Black;
            this.treeViewRegistry.File1TraceId = ((uint)(0u));
            this.treeViewRegistry.File2TraceId = ((uint)(0u));
            this.treeViewRegistry.FullRowSelect = true;
            this.treeViewRegistry.GotoVisible = true;
            this.treeViewRegistry.Indent = 7;
            this.treeViewRegistry.LineColor = System.Drawing.SystemColors.ControlDark;
            this.treeViewRegistry.LoadOnDemand = true;
            this.treeViewRegistry.Location = new System.Drawing.Point(0, 0);
            this.treeViewRegistry.MergeLayerPaths = false;
            this.treeViewRegistry.MergeWow = false;
            this.treeViewRegistry.Name = "treeViewRegistry";
            this.treeViewRegistry.RecursiveCheck = true;
            this.treeViewRegistry.RedirectClasses = false;
            this.treeViewRegistry.SelectedNode = null;
            this.treeViewRegistry.ShowIsolationOptions = false;
            this.treeViewRegistry.ShowLines = false;
            this.treeViewRegistry.Size = new System.Drawing.Size(217, 293);
            this.treeViewRegistry.TabIndex = 0;
            this.treeViewRegistry.ValuesView = null;
            // 
            // listViewValues
            // 
            this.listViewValues.ContextMenuStrip = this._contextMenuStripRegistryValues;
            this.listViewValues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewValues.FullRowSelect = true;
            this.listViewValues.HideSelection = false;
            this.listViewValues.IgnoreCase = true;
            this.listViewValues.Location = new System.Drawing.Point(0, 0);
            this.listViewValues.Name = "listViewValues";
            this.listViewValues.Size = new System.Drawing.Size(445, 293);
            this.listViewValues.SortColumn = 0;
            this.listViewValues.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewValues.TabIndex = 0;
            this.listViewValues.UseCompatibleStateImageBehavior = false;
            this.listViewValues.View = System.Windows.Forms.View.Details;
            // 
            // _contextMenuStripRegistryValues
            // 
            this._contextMenuStripRegistryValues.Name = "_contextMenuStripRegistryValues";
            this._contextMenuStripRegistryValues.Size = new System.Drawing.Size(61, 4);
            // 
            // tabTrace
            // 
            this.tabTrace.Controls.Add(this.tableLayoutTrace);
            this.tabTrace.Location = new System.Drawing.Point(4, 22);
            this.tabTrace.Margin = new System.Windows.Forms.Padding(0);
            this.tabTrace.Name = "tabTrace";
            this.tabTrace.Size = new System.Drawing.Size(666, 293);
            this.tabTrace.TabIndex = 3;
            this.tabTrace.Text = "Trace";
            this.tabTrace.UseVisualStyleBackColor = true;
            // 
            // treeViewTrace
            // 
            this.treeViewTrace.AutoRowHeight = false;
            this.treeViewTrace.BackColor = System.Drawing.SystemColors.Window;
            this.treeViewTrace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.treeViewTrace.ContextMenuStrip = this.contextMenuStripTrace;
            this.treeViewTrace.DefaultToolTipProvider = null;
            this.treeViewTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewTrace.DragDropMarkColor = System.Drawing.Color.Black;
            this.treeViewTrace.FullRowSelect = true;
            this.treeViewTrace.Indent = 7;
            this.treeViewTrace.LineColor = System.Drawing.SystemColors.ControlDark;
            this.treeViewTrace.LoadOnDemand = true;
            this.treeViewTrace.Location = new System.Drawing.Point(0, 0);
            this.treeViewTrace.Margin = new System.Windows.Forms.Padding(0);
            this.treeViewTrace.Model = null;
            this.treeViewTrace.Name = "treeViewTrace";
            this.treeViewTrace.SelectedNode = null;
            this.treeViewTrace.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.Multi;
            this.treeViewTrace.ShowLines = false;
            this.treeViewTrace.ShowNodeToolTips = true;
            this.treeViewTrace.Size = new System.Drawing.Size(666, 293);
            this.treeViewTrace.TabIndex = 0;
            this.treeViewTrace.UseColumns = true;
            this.treeViewTrace.ColumnClicked += new System.EventHandler<Aga.Controls.Tree.TreeColumnEventArgs>(this.OnColumnClicked);

            // 
            // eventSummary
            // 
            this.eventSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eventSummary.Margin = new System.Windows.Forms.Padding(0);
            this.eventSummary.Padding = new System.Windows.Forms.Padding(0);
            // 
            // tableLayoutTrace
            // 
            this.tableLayoutTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutTrace.ColumnCount = 2;
            this.tableLayoutTrace.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutTrace.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutTrace.Controls.Add(this.treeViewTrace, 1, 0);
            this.tableLayoutTrace.Controls.Add(this.eventSummary, 0, 0);
            this.tableLayoutTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutTrace.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutTrace.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutTrace.Padding = new Padding(0);
            this.tableLayoutTrace.Name = "tableLayoutLeft";
            this.tableLayoutTrace.RowCount = 1;
            this.tableLayoutTrace.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutTrace.Size = new System.Drawing.Size(204, 490);
            this.tableLayoutTrace.TabIndex = 0;
            // 
            // contextMenuStripTrace
            // 
            this.contextMenuStripTrace.Name = "contextMenuStripTrace";
            this.contextMenuStripTrace.Size = new System.Drawing.Size(135, 76);
            // 
            // contextMenuStripRegistry
            // 
            this.contextMenuStripRegistry.Name = "contextMenuStripRegistry";
            this.contextMenuStripRegistry.Size = new System.Drawing.Size(61, 4);
            // 
            // contextMenuStripCom
            // 
            this.contextMenuStripCom.Name = "contextMenuStripCom";
            this.contextMenuStripCom.Size = new System.Drawing.Size(135, 48);
            // 
            // contextMenuStripWindow
            // 
            this.contextMenuStripWindow.Name = "contextMenuStripWindow";
            this.contextMenuStripWindow.Size = new System.Drawing.Size(135, 48);
            // 
            // contextMenuStripFiles
            // 
            this.contextMenuStripFiles.Name = "contextMenuStripFiles";
            this.contextMenuStripFiles.Size = new System.Drawing.Size(68, 26);
            // 
            // checkBox1
            // 
            this.checkBox1.Location = new System.Drawing.Point(0, 0);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(104, 24);
            this.checkBox1.TabIndex = 0;
            // 
            // CallDump
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControlData);
            this.Name = "CallDump";
            this.Size = new System.Drawing.Size(674, 319);
            this.tableLayoutTrace.ResumeLayout(false);
            this.tabControlData.ResumeLayout(false);
            this.tabCom.ResumeLayout(false);
            this.tabWindow.ResumeLayout(false);
            this.tabFile.ResumeLayout(false);
            this.tabRegistry.ResumeLayout(false);
            this.splitRegistry.Panel1.ResumeLayout(false);
            this.splitRegistry.Panel2.ResumeLayout(false);
            this.splitRegistry.ResumeLayout(false);
            this.tabTrace.ResumeLayout(false);
            this.contextMenuStripTrace.ResumeLayout(false);
            this.contextMenuStripCom.ResumeLayout(false);
            this.contextMenuStripWindow.ResumeLayout(false);
            this.contextMenuStripFiles.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        private System.Windows.Forms.ContextMenuStrip contextMenuStripFiles;
        private System.Windows.Forms.TabControl tabControlData;
        public RegistryTree treeViewRegistry;
        private System.Windows.Forms.SplitContainer splitRegistry;
        private RegistryValueList listViewValues;
        private TableLayoutPanel tableLayoutTrace;
        private TraceTreeView treeViewTrace;
        private EventSummaryGraphic eventSummary;
        private ComObjectListView listViewCom;

        public FileSystemViewer listViewFileSystem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripRegistry;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTrace;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripCom;
        private WindowListView listViewWindow;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripWindow;

        #endregion
        private System.Windows.Forms.CheckBox checkBox1;
        private ComTab tabCom;
        private FilesTab tabFile;
        private RegistryTab tabRegistry;
        private TraceTab tabTrace;
        private WindowTab tabWindow;
        private System.Windows.Forms.ContextMenuStrip _contextMenuStripRegistryValues;

        public bool PropertiesVisible
        {
            get { return true; }
        }
    }
}
