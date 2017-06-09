using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SpyStudio.COM.Controls;
using SpyStudio.COM.Controls.Compare;
using SpyStudio.ContextMenu;
using SpyStudio.EventSummary;
using SpyStudio.FileSystem;
using SpyStudio.FileSystem.Compare;
using SpyStudio.Main;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Controls.Compare;
using SpyStudio.Tools;
using SpyStudio.Windows.Controls;
using SpyStudio.Windows.Controls.Compare;

namespace SpyStudio.Dialogs.Compare
{
    partial class FormDeviareCompare : Form
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
                components.Dispose();

            foreach (var disposable in _toBeDisposed)
                disposable.Dispose();

            base.Dispose(disposing);
        }

        public new void Dispose()
        {
            Dispose(true);
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDeviareCompare));
            this.menuStripDeviareCompare = new System.Windows.Forms.MenuStrip();
            this.fToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSystemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemHideAttributes = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.showLayerPathsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mergeWowPathsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showStartupModulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripDeviareCompare = new System.Windows.Forms.ToolStrip();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this._statusTotalEventsLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusFilteredEventsLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.tableLayoutPanelTop = new System.Windows.Forms.TableLayoutPanel();
            this.labelLeftColor = new System.Windows.Forms.Label();
            this.labelRightColor = new System.Windows.Forms.Label();
            this.labelRightFile = new System.Windows.Forms.Label();
            this.labelLeftFile = new System.Windows.Forms.Label();
            this.tabControlData = new System.Windows.Forms.TabControl();
            this.tabCom = new System.Windows.Forms.TabPage();
            this._listViewCom = new SpyStudio.COM.Controls.Compare.CompareComObjectListView();
            this.tabWindow = new System.Windows.Forms.TabPage();
            this._listViewWindow = new SpyStudio.Windows.Controls.Compare.CompareWindowListView();
            this.tabFile = new System.Windows.Forms.TabPage();
            this.splitFiles = new System.Windows.Forms.SplitContainer();
            this._fileSystemViewer = new SpyStudio.FileSystem.Compare.CompareFileSystemViewer();
            this._listViewFileDetails = new SpyStudio.FileSystem.Compare.FileSystemListDetails();
            this._fileSystemDetailsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tabRegistry = new System.Windows.Forms.TabPage();
            this.splitRegistry = new System.Windows.Forms.SplitContainer();
            this._treeViewRegistry = new SpyStudio.Registry.Controls.Compare.CompareRegistryTree();
            this._listViewValues = new SpyStudio.Registry.Controls.RegistryValueList();
            this._valuesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tabTrace = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this._treeViewCompare = new SpyStudio.Dialogs.Compare.DeviareTraceCompareTreeView();
            this.columnHeaderProcessName = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderCaller = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderStackFrame = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderFunction = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderParamMain = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderDetails = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderResult = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderCountNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderProcessNameNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderPidNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderTidNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderCallerNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderFunctionNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderPathNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderDetailsNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderResultNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderStackFrameNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.eventSummaryGraphic1 = new SpyStudio.EventSummary.EventSummaryGraphic();
            this.menuStripDeviareCompare.SuspendLayout();
            this.tableLayoutPanelMain.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tableLayoutPanelTop.SuspendLayout();
            this.tabControlData.SuspendLayout();
            this.tabCom.SuspendLayout();
            this.tabWindow.SuspendLayout();
            this.tabFile.SuspendLayout();
            this.splitFiles.Panel1.SuspendLayout();
            this.splitFiles.Panel2.SuspendLayout();
            this.splitFiles.SuspendLayout();
            this.tabRegistry.SuspendLayout();
            this.splitRegistry.Panel1.SuspendLayout();
            this.splitRegistry.Panel2.SuspendLayout();
            this.splitRegistry.SuspendLayout();
            this.tabTrace.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripDeviareCompare
            // 
            this.menuStripDeviareCompare.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStripDeviareCompare.Location = new System.Drawing.Point(0, 0);
            this.menuStripDeviareCompare.Name = "menuStripDeviareCompare";
            this.menuStripDeviareCompare.Size = new System.Drawing.Size(1056, 24);
            this.menuStripDeviareCompare.TabIndex = 0;
            this.menuStripDeviareCompare.Text = "menuStripDeviareCompare";
            // 
            // fToolStripMenuItem
            // 
            this.fToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fToolStripMenuItem.Name = "fToolStripMenuItem";
            this.fToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "&Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItemClick);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileSystemToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // fileSystemToolStripMenuItem
            // 
            this.fileSystemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemHideAttributes,
            this.toolStripSeparator3,
            this.showLayerPathsToolStripMenuItem,
            this.mergeWowPathsToolStripMenuItem,
            this.showStartupModulesToolStripMenuItem});
            this.fileSystemToolStripMenuItem.Name = "fileSystemToolStripMenuItem";
            this.fileSystemToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.fileSystemToolStripMenuItem.Text = "&File System";
            // 
            // toolStripMenuItemHideAttributes
            // 
            this.toolStripMenuItemHideAttributes.CheckOnClick = true;
            this.toolStripMenuItemHideAttributes.Name = "toolStripMenuItemHideAttributes";
            this.toolStripMenuItemHideAttributes.Size = new System.Drawing.Size(248, 22);
            this.toolStripMenuItemHideAttributes.Text = "Hide QueryAttributes Operations";
            this.toolStripMenuItemHideAttributes.Click += new System.EventHandler(this.HideQueryAttributesOperationsToolStripMenuItemClick);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(245, 6);
            // 
            // showLayerPathsToolStripMenuItem
            // 
            this.showLayerPathsToolStripMenuItem.CheckOnClick = true;
            this.showLayerPathsToolStripMenuItem.Name = "showLayerPathsToolStripMenuItem";
            this.showLayerPathsToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.showLayerPathsToolStripMenuItem.Text = "Show Virtual Paths";
            this.showLayerPathsToolStripMenuItem.Visible = false;
            this.showLayerPathsToolStripMenuItem.Click += new System.EventHandler(this.ShowVirtualPathsToolStripMenuItemClick);
            // 
            // mergeWowPathsToolStripMenuItem
            // 
            this.mergeWowPathsToolStripMenuItem.CheckOnClick = true;
            this.mergeWowPathsToolStripMenuItem.Name = "mergeWowPathsToolStripMenuItem";
            this.mergeWowPathsToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.mergeWowPathsToolStripMenuItem.Text = "Merge Wow Paths";
            this.mergeWowPathsToolStripMenuItem.Visible = false;
            this.mergeWowPathsToolStripMenuItem.Click += new System.EventHandler(this.MergeWowPathsToolStripMenuItemClick);
            // 
            // showStartupModulesToolStripMenuItem
            // 
            this.showStartupModulesToolStripMenuItem.CheckOnClick = true;
            this.showStartupModulesToolStripMenuItem.Name = "showStartupModulesToolStripMenuItem";
            this.showStartupModulesToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.showStartupModulesToolStripMenuItem.Text = "Show Modules Loaded at Startup";
            this.showStartupModulesToolStripMenuItem.Click += new System.EventHandler(this.ShowModulesLoadedAtStartupToolStripMenuItemClick);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.toolStripSeparator1,
            this.selectAllToolStripMenuItem,
            this.toolStripSeparator2,
            this.findToolStripMenuItem,
            this.filterToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
            this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.copyToolStripMenuItem.Text = "&Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyToolStripMenuItemClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(161, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.selectAllToolStripMenuItem.Text = "Select &All";
            this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.SelectAllToolStripMenuItemClick);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(161, 6);
            // 
            // findToolStripMenuItem
            // 
            this.findToolStripMenuItem.Name = "findToolStripMenuItem";
            this.findToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
            this.findToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.findToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.findToolStripMenuItem.Text = "&Find";
            this.findToolStripMenuItem.Click += new System.EventHandler(this.FindToolStripMenuItemClick);
            // 
            // filterToolStripMenuItem
            // 
            this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
            this.filterToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.filterToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.filterToolStripMenuItem.Text = "Filter ...";
            this.filterToolStripMenuItem.Click += new System.EventHandler(this.FilterToolStripMenuItemClick);
            // 
            // toolStripDeviareCompare
            // 
            this.toolStripDeviareCompare.Location = new System.Drawing.Point(0, 24);
            this.toolStripDeviareCompare.Name = "toolStripDeviareCompare";
            this.toolStripDeviareCompare.Size = new System.Drawing.Size(1056, 25);
            this.toolStripDeviareCompare.TabIndex = 1;
            this.toolStripDeviareCompare.Text = "toolStrip1";
            this.toolStripDeviareCompare.Visible = false;
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Controls.Add(this.statusStrip1, 0, 2);
            this.tableLayoutPanelMain.Controls.Add(this.tableLayoutPanelTop, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.tabControlData, 0, 1);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 3;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(1056, 527);
            this.tableLayoutPanelMain.TabIndex = 2;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusTotalEventsLabel,
            this._statusFilteredEventsLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 507);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1056, 20);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // _statusTotalEventsLabel
            // 
            this._statusTotalEventsLabel.Name = "_statusTotalEventsLabel";
            this._statusTotalEventsLabel.Size = new System.Drawing.Size(77, 15);
            this._statusTotalEventsLabel.Text = "Total Events: ";
            // 
            // _statusFilteredEventsLabel
            // 
            this._statusFilteredEventsLabel.Name = "_statusFilteredEventsLabel";
            this._statusFilteredEventsLabel.Size = new System.Drawing.Size(89, 15);
            this._statusFilteredEventsLabel.Text = "Filtered Events: ";
            // 
            // tableLayoutPanelTop
            // 
            this.tableLayoutPanelTop.ColumnCount = 4;
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelTop.Controls.Add(this.labelLeftColor, 0, 0);
            this.tableLayoutPanelTop.Controls.Add(this.labelRightColor, 2, 0);
            this.tableLayoutPanelTop.Controls.Add(this.labelRightFile, 3, 0);
            this.tableLayoutPanelTop.Controls.Add(this.labelLeftFile, 1, 0);
            this.tableLayoutPanelTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelTop.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelTop.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanelTop.Name = "tableLayoutPanelTop";
            this.tableLayoutPanelTop.RowCount = 1;
            this.tableLayoutPanelTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelTop.Size = new System.Drawing.Size(1056, 25);
            this.tableLayoutPanelTop.TabIndex = 1;
            // 
            // labelLeftColor
            // 
            this.labelLeftColor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelLeftColor.AutoSize = true;
            this.labelLeftColor.BackColor = System.Drawing.SystemColors.Highlight;
            this.labelLeftColor.Location = new System.Drawing.Point(4, 4);
            this.labelLeftColor.Margin = new System.Windows.Forms.Padding(4);
            this.labelLeftColor.Name = "labelLeftColor";
            this.labelLeftColor.Size = new System.Drawing.Size(12, 17);
            this.labelLeftColor.TabIndex = 0;
            // 
            // labelRightColor
            // 
            this.labelRightColor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRightColor.AutoSize = true;
            this.labelRightColor.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.labelRightColor.Location = new System.Drawing.Point(532, 4);
            this.labelRightColor.Margin = new System.Windows.Forms.Padding(4);
            this.labelRightColor.Name = "labelRightColor";
            this.labelRightColor.Size = new System.Drawing.Size(12, 17);
            this.labelRightColor.TabIndex = 1;
            // 
            // labelRightFile
            // 
            this.labelRightFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.labelRightFile.AutoSize = true;
            this.labelRightFile.Location = new System.Drawing.Point(551, 2);
            this.labelRightFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.labelRightFile.Name = "labelRightFile";
            this.labelRightFile.Size = new System.Drawing.Size(26, 21);
            this.labelRightFile.TabIndex = 2;
            this.labelRightFile.Text = "file2";
            this.labelRightFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelLeftFile
            // 
            this.labelLeftFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.labelLeftFile.AutoSize = true;
            this.labelLeftFile.Location = new System.Drawing.Point(23, 2);
            this.labelLeftFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.labelLeftFile.Name = "labelLeftFile";
            this.labelLeftFile.Size = new System.Drawing.Size(26, 21);
            this.labelLeftFile.TabIndex = 3;
            this.labelLeftFile.Text = "file1";
            this.labelLeftFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabControlData
            // 
            this.tabControlData.Controls.Add(this.tabCom);
            this.tabControlData.Controls.Add(this.tabWindow);
            this.tabControlData.Controls.Add(this.tabFile);
            this.tabControlData.Controls.Add(this.tabRegistry);
            this.tabControlData.Controls.Add(this.tabTrace);
            this.tabControlData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlData.Location = new System.Drawing.Point(0, 25);
            this.tabControlData.Margin = new System.Windows.Forms.Padding(0);
            this.tabControlData.Name = "tabControlData";
            this.tabControlData.SelectedIndex = 0;
            this.tabControlData.Size = new System.Drawing.Size(1056, 482);
            this.tabControlData.TabIndex = 2;
            // 
            // tabCom
            // 
            this.tabCom.Controls.Add(this._listViewCom);
            this.tabCom.Location = new System.Drawing.Point(4, 22);
            this.tabCom.Name = "tabCom";
            this.tabCom.Size = new System.Drawing.Size(1048, 456);
            this.tabCom.TabIndex = 0;
            this.tabCom.Text = "COM Objects";
            this.tabCom.UseVisualStyleBackColor = true;
            // 
            // _listViewCom
            // 
            this._listViewCom.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._listViewCom.Controller = null;
            this._listViewCom.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewCom.FullRowSelect = true;
            this._listViewCom.HideSelection = false;
            this._listViewCom.IgnoreCase = true;
            this._listViewCom.Location = new System.Drawing.Point(0, 0);
            this._listViewCom.Margin = new System.Windows.Forms.Padding(0);
            this._listViewCom.Name = "_listViewCom";
            this._listViewCom.Size = new System.Drawing.Size(1048, 456);
            this._listViewCom.SortColumn = 0;
            this._listViewCom.Sorting = System.Windows.Forms.SortOrder.Descending;
            this._listViewCom.TabIndex = 0;
            this._listViewCom.UseCompatibleStateImageBehavior = false;
            this._listViewCom.View = System.Windows.Forms.View.Details;
            // 
            // tabWindow
            // 
            this.tabWindow.Controls.Add(this._listViewWindow);
            this.tabWindow.Location = new System.Drawing.Point(4, 22);
            this.tabWindow.Name = "tabWindow";
            this.tabWindow.Size = new System.Drawing.Size(1048, 456);
            this.tabWindow.TabIndex = 4;
            this.tabWindow.Text = "Windows";
            this.tabWindow.UseVisualStyleBackColor = true;
            // 
            // _listViewWindow
            // 
            this._listViewWindow.CallEventIds = null;
            this._listViewWindow.Controller = null;
            this._listViewWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewWindow.FullRowSelect = true;
            this._listViewWindow.HideSelection = false;
            this._listViewWindow.IgnoreCase = true;
            this._listViewWindow.Location = new System.Drawing.Point(0, 0);
            this._listViewWindow.Name = "_listViewWindow";
            this._listViewWindow.Size = new System.Drawing.Size(1048, 456);
            this._listViewWindow.SortColumn = 0;
            this._listViewWindow.Sorting = System.Windows.Forms.SortOrder.Descending;
            this._listViewWindow.TabIndex = 0;
            this._listViewWindow.UseCompatibleStateImageBehavior = false;
            this._listViewWindow.View = System.Windows.Forms.View.Details;
            // 
            // tabFile
            // 
            this.tabFile.Controls.Add(this.splitFiles);
            this.tabFile.Location = new System.Drawing.Point(4, 22);
            this.tabFile.Margin = new System.Windows.Forms.Padding(0);
            this.tabFile.Name = "tabFile";
            this.tabFile.Size = new System.Drawing.Size(1048, 456);
            this.tabFile.TabIndex = 1;
            this.tabFile.Text = "Files";
            this.tabFile.UseVisualStyleBackColor = true;
            // 
            // splitFiles
            // 
            this.splitFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitFiles.Location = new System.Drawing.Point(0, 0);
            this.splitFiles.Margin = new System.Windows.Forms.Padding(0);
            this.splitFiles.Name = "splitFiles";
            this.splitFiles.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitFiles.Panel1
            // 
            this.splitFiles.Panel1.Controls.Add(this._fileSystemViewer);
            // 
            // splitFiles.Panel2
            // 
            this.splitFiles.Panel2.Controls.Add(this._listViewFileDetails);
            this.splitFiles.Size = new System.Drawing.Size(1048, 456);
            this.splitFiles.SplitterDistance = 338;
            this.splitFiles.TabIndex = 0;
            // 
            // _fileSystemViewer
            // 
            this._fileSystemViewer.Controller = null;
            this._fileSystemViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._fileSystemViewer.File1BackgroundColor = System.Drawing.Color.Empty;
            this._fileSystemViewer.File1TraceId = ((uint)(0u));
            this._fileSystemViewer.File2BackgroundColor = System.Drawing.Color.Empty;
            this._fileSystemViewer.File2TraceId = ((uint)(0u));
            this._fileSystemViewer.HideQueryAttributes = false;
            this._fileSystemViewer.Location = new System.Drawing.Point(0, 0);
            this._fileSystemViewer.Margin = new System.Windows.Forms.Padding(0);
            this._fileSystemViewer.MergeLayerPaths = false;
            this._fileSystemViewer.MergeWowPaths = false;
            this._fileSystemViewer.Name = "_fileSystemViewer";
            this._fileSystemViewer.PathNormalizer = nullPathNormalizer1;
            this._fileSystemViewer.ShowIsolationOptions = false;
            this._fileSystemViewer.ShowStartupModules = true;
            this._fileSystemViewer.Size = new System.Drawing.Size(1048, 338);
            this._fileSystemViewer.TabIndex = 0;
            this._fileSystemViewer.TreeMode = false;
            // 
            // _listViewFileDetails
            // 
            this._listViewFileDetails.ContextMenuStrip = this._fileSystemDetailsContextMenu;
            this._listViewFileDetails.Controller = null;
            this._listViewFileDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewFileDetails.FullRowSelect = true;
            this._listViewFileDetails.HideSelection = false;
            this._listViewFileDetails.IgnoreCase = true;
            this._listViewFileDetails.Location = new System.Drawing.Point(0, 0);
            this._listViewFileDetails.Name = "_listViewFileDetails";
            this._listViewFileDetails.Size = new System.Drawing.Size(1048, 114);
            this._listViewFileDetails.SortColumn = 0;
            this._listViewFileDetails.Sorting = System.Windows.Forms.SortOrder.Descending;
            this._listViewFileDetails.TabIndex = 0;
            this._listViewFileDetails.UseCompatibleStateImageBehavior = false;
            this._listViewFileDetails.View = System.Windows.Forms.View.Details;
            // 
            // _fileSystemDetailsContextMenu
            // 
            this._fileSystemDetailsContextMenu.Name = "_fileSystemDetailsContextMenu";
            this._fileSystemDetailsContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // tabRegistry
            // 
            this.tabRegistry.Controls.Add(this.splitRegistry);
            this.tabRegistry.Location = new System.Drawing.Point(4, 22);
            this.tabRegistry.Margin = new System.Windows.Forms.Padding(0);
            this.tabRegistry.Name = "tabRegistry";
            this.tabRegistry.Size = new System.Drawing.Size(1048, 456);
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
            this.splitRegistry.Panel1.Controls.Add(this._treeViewRegistry);
            // 
            // splitRegistry.Panel2
            // 
            this.splitRegistry.Panel2.Controls.Add(this._listViewValues);
            this.splitRegistry.Size = new System.Drawing.Size(1048, 456);
            this.splitRegistry.SplitterDistance = 340;
            this.splitRegistry.TabIndex = 1;
            // 
            // _treeViewRegistry
            // 
            this._treeViewRegistry.BackColor = System.Drawing.SystemColors.Window;
            this._treeViewRegistry.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._treeViewRegistry.Controller = null;
            this._treeViewRegistry.DefaultToolTipProvider = null;
            this._treeViewRegistry.Dock = System.Windows.Forms.DockStyle.Fill;
            this._treeViewRegistry.DragDropMarkColor = System.Drawing.Color.Black;
            this._treeViewRegistry.File1TraceId = ((uint)(0u));
            this._treeViewRegistry.File2TraceId = ((uint)(0u));
            this._treeViewRegistry.FullRowSelect = true;
            this._treeViewRegistry.GotoVisible = true;
            this._treeViewRegistry.Indent = 7;
            this._treeViewRegistry.LineColor = System.Drawing.SystemColors.ControlDark;
            this._treeViewRegistry.LoadOnDemand = true;
            this._treeViewRegistry.Location = new System.Drawing.Point(0, 0);
            this._treeViewRegistry.Margin = new System.Windows.Forms.Padding(0);
            this._treeViewRegistry.MergeLayerPaths = false;
            this._treeViewRegistry.MergeWow = false;
            this._treeViewRegistry.Name = "_treeViewRegistry";
            this._treeViewRegistry.RedirectClasses = false;
            this._treeViewRegistry.SelectedNode = null;
            this._treeViewRegistry.ShowHScrollBar = true;
            this._treeViewRegistry.ShowIsolationOptions = false;
            this._treeViewRegistry.ShowLines = false;
            this._treeViewRegistry.ShowVScrollBar = true;
            this._treeViewRegistry.Size = new System.Drawing.Size(340, 456);
            this._treeViewRegistry.TabIndex = 0;
            this._treeViewRegistry.UseColumns = true;
            this._treeViewRegistry.ValuesView = null;
            // 
            // _listViewValues
            // 
            this._listViewValues.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._listViewValues.ContextMenuStrip = this._valuesContextMenu;
            this._listViewValues.Controller = null;
            this._listViewValues.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewValues.FullRowSelect = true;
            this._listViewValues.HideSelection = false;
            this._listViewValues.IgnoreCase = true;
            this._listViewValues.Location = new System.Drawing.Point(0, 0);
            this._listViewValues.Name = "_listViewValues";
            this._listViewValues.Size = new System.Drawing.Size(704, 456);
            this._listViewValues.SortColumn = 0;
            this._listViewValues.Sorting = System.Windows.Forms.SortOrder.Descending;
            this._listViewValues.TabIndex = 0;
            this._listViewValues.UseCompatibleStateImageBehavior = false;
            this._listViewValues.View = System.Windows.Forms.View.Details;
            // 
            // _valuesContextMenu
            // 
            this._valuesContextMenu.Name = "_valuesContextMenu";
            this._valuesContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // tabTrace
            // 
            this.tabTrace.Controls.Add(this.tableLayoutPanel1);
            this.tabTrace.Location = new System.Drawing.Point(4, 22);
            this.tabTrace.Margin = new System.Windows.Forms.Padding(0);
            this.tabTrace.Name = "tabTrace";
            this.tabTrace.Size = new System.Drawing.Size(1048, 456);
            this.tabTrace.TabIndex = 3;
            this.tabTrace.Text = "Trace";
            this.tabTrace.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this._treeViewCompare, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.eventSummaryGraphic1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1048, 456);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // _treeViewCompare
            // 
            this._treeViewCompare.BackColor = System.Drawing.SystemColors.Window;
            this._treeViewCompare.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._treeViewCompare.Columns.Add(this.columnHeaderProcessName);
            this._treeViewCompare.Columns.Add(this.columnHeaderCaller);
            this._treeViewCompare.Columns.Add(this.columnHeaderStackFrame);
            this._treeViewCompare.Columns.Add(this.columnHeaderFunction);
            this._treeViewCompare.Columns.Add(this.columnHeaderParamMain);
            this._treeViewCompare.Columns.Add(this.columnHeaderDetails);
            this._treeViewCompare.Columns.Add(this.columnHeaderResult);
            this._treeViewCompare.ContextMenuController = null;
            this._treeViewCompare.Controller = null;
            this._treeViewCompare.DefaultToolTipProvider = null;
            this._treeViewCompare.Dock = System.Windows.Forms.DockStyle.Fill;
            this._treeViewCompare.DragDropMarkColor = System.Drawing.Color.Black;
            this._treeViewCompare.FullRowSelect = true;
            this._treeViewCompare.Indent = 7;
            this._treeViewCompare.LineColor = System.Drawing.SystemColors.ControlDark;
            this._treeViewCompare.Location = new System.Drawing.Point(28, 0);
            this._treeViewCompare.Margin = new System.Windows.Forms.Padding(0);
            this._treeViewCompare.Model = null;
            this._treeViewCompare.Name = "_treeViewCompare";
            this._treeViewCompare.NodeControls.Add(this.columnHeaderCountNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderProcessNameNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderPidNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderTidNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderCallerNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderFunctionNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderPathNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderDetailsNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderResultNode);
            this._treeViewCompare.NodeControls.Add(this.columnHeaderStackFrameNode);
            this._treeViewCompare.SelectedNode = null;
            this._treeViewCompare.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.Multi;
            this._treeViewCompare.ShowHScrollBar = false;
            this._treeViewCompare.ShowLines = false;
            this._treeViewCompare.ShowVScrollBar = true;
            this._treeViewCompare.Size = new System.Drawing.Size(1020, 456);
            this._treeViewCompare.TabIndex = 0;
            this._treeViewCompare.Trace1Id = ((uint)(0u));
            this._treeViewCompare.Trace2Id = ((uint)(0u));
            this._treeViewCompare.UseColumns = true;
            // 
            // columnHeaderProcessName
            // 
            this.columnHeaderProcessName.Header = "Process Name";
            this.columnHeaderProcessName.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderProcessName.TooltipText = null;
            this.columnHeaderProcessName.Width = 87;
            // 
            // columnHeaderCaller
            // 
            this.columnHeaderCaller.Header = "Caller";
            this.columnHeaderCaller.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderCaller.TooltipText = null;
            this.columnHeaderCaller.Width = 70;
            // 
            // columnHeaderStackFrame
            // 
            this.columnHeaderStackFrame.Header = "Frame";
            this.columnHeaderStackFrame.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderStackFrame.TooltipText = null;
            this.columnHeaderStackFrame.Width = 100;
            // 
            // columnHeaderFunction
            // 
            this.columnHeaderFunction.Header = "Function";
            this.columnHeaderFunction.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderFunction.TooltipText = null;
            this.columnHeaderFunction.Width = 100;
            // 
            // columnHeaderParamMain
            // 
            this.columnHeaderParamMain.Header = "ParamMain";
            this.columnHeaderParamMain.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderParamMain.TooltipText = null;
            this.columnHeaderParamMain.Width = 330;
            // 
            // columnHeaderDetails
            // 
            this.columnHeaderDetails.Header = "Details";
            this.columnHeaderDetails.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderDetails.TooltipText = null;
            this.columnHeaderDetails.Width = 240;
            // 
            // columnHeaderResult
            // 
            this.columnHeaderResult.Header = "Result";
            this.columnHeaderResult.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderResult.TooltipText = null;
            this.columnHeaderResult.Width = 100;
            // 
            // columnHeaderCountNode
            // 
            this.columnHeaderCountNode.DataPropertyName = "Count";
            this.columnHeaderCountNode.IncrementalSearchEnabled = true;
            this.columnHeaderCountNode.LeftMargin = 3;
            this.columnHeaderCountNode.ParentColumn = null;
            // 
            // columnHeaderProcessNameNode
            // 
            this.columnHeaderProcessNameNode.DataPropertyName = "ProcessName";
            this.columnHeaderProcessNameNode.IncrementalSearchEnabled = true;
            this.columnHeaderProcessNameNode.LeftMargin = 3;
            this.columnHeaderProcessNameNode.ParentColumn = this.columnHeaderProcessName;
            // 
            // columnHeaderPidNode
            // 
            this.columnHeaderPidNode.DataPropertyName = "Pid";
            this.columnHeaderPidNode.IncrementalSearchEnabled = true;
            this.columnHeaderPidNode.LeftMargin = 3;
            this.columnHeaderPidNode.ParentColumn = null;
            // 
            // columnHeaderTidNode
            // 
            this.columnHeaderTidNode.DataPropertyName = "Tid";
            this.columnHeaderTidNode.IncrementalSearchEnabled = true;
            this.columnHeaderTidNode.LeftMargin = 3;
            this.columnHeaderTidNode.ParentColumn = null;
            // 
            // columnHeaderCallerNode
            // 
            this.columnHeaderCallerNode.DataPropertyName = "Caller";
            this.columnHeaderCallerNode.IncrementalSearchEnabled = true;
            this.columnHeaderCallerNode.LeftMargin = 3;
            this.columnHeaderCallerNode.ParentColumn = this.columnHeaderCaller;
            this.columnHeaderCallerNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderFunctionNode
            // 
            this.columnHeaderFunctionNode.DataPropertyName = "Function";
            this.columnHeaderFunctionNode.IncrementalSearchEnabled = true;
            this.columnHeaderFunctionNode.LeftMargin = 3;
            this.columnHeaderFunctionNode.ParentColumn = this.columnHeaderFunction;
            this.columnHeaderFunctionNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderPathNode
            // 
            this.columnHeaderPathNode.DataPropertyName = "ParamMain";
            this.columnHeaderPathNode.IncrementalSearchEnabled = true;
            this.columnHeaderPathNode.LeftMargin = 3;
            this.columnHeaderPathNode.ParentColumn = this.columnHeaderParamMain;
            this.columnHeaderPathNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderDetailsNode
            // 
            this.columnHeaderDetailsNode.DataPropertyName = "Details";
            this.columnHeaderDetailsNode.IncrementalSearchEnabled = true;
            this.columnHeaderDetailsNode.LeftMargin = 3;
            this.columnHeaderDetailsNode.ParentColumn = this.columnHeaderDetails;
            this.columnHeaderDetailsNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderResultNode
            // 
            this.columnHeaderResultNode.DataPropertyName = "Result";
            this.columnHeaderResultNode.IncrementalSearchEnabled = true;
            this.columnHeaderResultNode.LeftMargin = 3;
            this.columnHeaderResultNode.ParentColumn = this.columnHeaderResult;
            this.columnHeaderResultNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderStackFrameNode
            // 
            this.columnHeaderStackFrameNode.DataPropertyName = "StackFrame";
            this.columnHeaderStackFrameNode.IncrementalSearchEnabled = true;
            this.columnHeaderStackFrameNode.LeftMargin = 3;
            this.columnHeaderStackFrameNode.ParentColumn = this.columnHeaderStackFrame;
            this.columnHeaderStackFrameNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // eventSummaryGraphic1
            // 
            this.eventSummaryGraphic1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eventSummaryGraphic1.Location = new System.Drawing.Point(0, 0);
            this.eventSummaryGraphic1.Margin = new System.Windows.Forms.Padding(0);
            this.eventSummaryGraphic1.Name = "eventSummaryGraphic1";
            this.eventSummaryGraphic1.Size = new System.Drawing.Size(28, 456);
            this.eventSummaryGraphic1.TabIndex = 1;
            // 
            // FormDeviareCompare
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1056, 551);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Controls.Add(this.toolStripDeviareCompare);
            this.Controls.Add(this.menuStripDeviareCompare);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStripDeviareCompare;
            this.Name = "FormDeviareCompare";
            this.Text = "Trace Compare";
            this.Load += new System.EventHandler(this.FormDeviareCompareLoad);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormDeviareCompareFormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormDeviareCompareKeyDown);
            this.menuStripDeviareCompare.ResumeLayout(false);
            this.menuStripDeviareCompare.PerformLayout();
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tableLayoutPanelTop.ResumeLayout(false);
            this.tableLayoutPanelTop.PerformLayout();
            this.tabControlData.ResumeLayout(false);
            this.tabCom.ResumeLayout(false);
            this.tabWindow.ResumeLayout(false);
            this.tabFile.ResumeLayout(false);
            this.splitFiles.Panel1.ResumeLayout(false);
            this.splitFiles.Panel2.ResumeLayout(false);
            this.splitFiles.ResumeLayout(false);
            this.tabRegistry.ResumeLayout(false);
            this.splitRegistry.Panel1.ResumeLayout(false);
            this.splitRegistry.Panel2.ResumeLayout(false);
            this.splitRegistry.ResumeLayout(false);
            this.tabTrace.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStripDeviareCompare;
        private System.Windows.Forms.ToolStrip toolStripDeviareCompare;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderCountNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderPidNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderTidNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderCallerNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderFunctionNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderPathNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderResultNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderProcessNameNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderDetailsNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderStackFrameNode;
        private Aga.Controls.Tree.TreeColumn columnHeaderCaller;
        private Aga.Controls.Tree.TreeColumn columnHeaderFunction;
        private Aga.Controls.Tree.TreeColumn columnHeaderParamMain;
        private Aga.Controls.Tree.TreeColumn columnHeaderResult;
        private Aga.Controls.Tree.TreeColumn columnHeaderProcessName;
        private Aga.Controls.Tree.TreeColumn columnHeaderDetails;
        private Aga.Controls.Tree.TreeColumn columnHeaderStackFrame;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem filterToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelTop;
        private System.Windows.Forms.Label labelLeftColor;
        private System.Windows.Forms.Label labelRightColor;
        private System.Windows.Forms.Label labelRightFile;
        private System.Windows.Forms.Label labelLeftFile;
        private System.Windows.Forms.TabControl tabControlData;
        private System.Windows.Forms.TabPage tabCom;
        private CompareComObjectListView _listViewCom;
        private System.Windows.Forms.TabPage tabWindow;
        private CompareWindowListView _listViewWindow;
        private System.Windows.Forms.TabPage tabFile;
        private CompareFileSystemViewer _fileSystemViewer;
        private System.Windows.Forms.TabPage tabRegistry;
        private System.Windows.Forms.SplitContainer splitRegistry;
        private System.Windows.Forms.SplitContainer splitFiles;
        private CompareRegistryTree _treeViewRegistry;
        private RegistryValueList _listViewValues;
        private System.Windows.Forms.TabPage tabTrace;
        private DeviareTraceCompareTreeView _treeViewCompare;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel _statusTotalEventsLabel;
        private System.Windows.Forms.ToolStripStatusLabel _statusFilteredEventsLabel;
        private List<IDisposable> _toBeDisposed;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem fileSystemToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItemHideAttributes;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem showLayerPathsToolStripMenuItem;
        private ToolStripMenuItem mergeWowPathsToolStripMenuItem;
        private ToolStripMenuItem showStartupModulesToolStripMenuItem;
        private ContextMenuStrip _valuesContextMenu;
        private ContextMenuStrip _fileSystemDetailsContextMenu;
        protected readonly EntryContextMenu TraceEntryProperties;
        protected readonly EntryContextMenu ValuesEntryProperties;
        protected readonly EntryContextMenu FileSystemDetailsEntryProperties;
        private TableLayoutPanel tableLayoutPanel1;
        private EventSummaryGraphic eventSummaryGraphic1;
        private FileSystemListDetails _listViewFileDetails;
    }
}