using System.Windows.Forms;
using SpyStudio.Forms;
using SpyStudio.Tools;

namespace SpyStudio
{
    partial class FormMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogfilteredToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToLineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSystemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemFileSystemFlatMode = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemFileSystemTreeMode = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItemHideAttributes = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.showLayerPathsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mergeWowPathsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showStartupModulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.registryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mergeCOMClassesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mergeWowKeyPathsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.eventToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.processToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.terminateProcessMenuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparatorProcess1 = new System.Windows.Forms.ToolStripSeparator();
            this.processPropertiesMenuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.swvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToThinAppToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.massExportToThinAppToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createOrEditTemplateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.monitorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.analysisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemStart = new System.Windows.Forms.ToolStripMenuItem();
            this.executeInstallerAndHookToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.executionPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemStop = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.statisticsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.collectingDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullStackInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.hookNewProcessesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearFailedHooksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compareToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tracesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filteredTracesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.statisticsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutAppStudioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAllFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tableLayoutLeft = new System.Windows.Forms.TableLayoutPanel();
            this.panelOpenFile = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.tableLayoutLeftTop = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxProgramExec = new System.Windows.Forms.TextBox();
            this.buttonOpenFile = new System.Windows.Forms.Button();
            this.panelProcesses = new System.Windows.Forms.Panel();
            this.tableLayoutLeftBottom = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.listViewProcesses = new SpyStudio.Tools.ListViewSorted();
            this.columnName = new System.Windows.Forms.ColumnHeader();
            this.columnPid = new System.Windows.Forms.ColumnHeader();
            this.contextMenuStripProcess = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.unhookToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hookToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.terminateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.processPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label5 = new System.Windows.Forms.Label();
            this.callDump = new SpyStudio.Forms.CallDump();
            this.contextMenuStripCom = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeComToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAllComToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripWindow = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeWindowAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripRegistry = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAllKeysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripTrace = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeTraceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAllTraceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.propertiesTraceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageListTraceListView = new System.Windows.Forms.ImageList(this.components);
            this.columnHeaderCount = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderProcessName = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderPid = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderTid = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderCaller = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderFunction = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderPath = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderDetails = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderResult = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderTimeCol = new Aga.Controls.Tree.TreeColumn();
            this.columnHeaderCountNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderProcessNameNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderPidNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderTidNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderCallerNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderFunctionNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderPathNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderDetailsNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderResultNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.columnHeaderTimeNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this._statusBarTotalEvents = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusBarFilteredEvents = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusBarMiddle = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusBarProcessing = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusBarSyncIcon = new System.Windows.Forms.ToolStripStatusLabel();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.menuStripMain.SuspendLayout();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.tableLayoutLeft.SuspendLayout();
            this.panelOpenFile.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutLeftTop.SuspendLayout();
            this.panelProcesses.SuspendLayout();
            this.tableLayoutLeftBottom.SuspendLayout();
            this.contextMenuStripProcess.SuspendLayout();
            this.contextMenuStripCom.SuspendLayout();
            this.contextMenuStripWindow.SuspendLayout();
            this.contextMenuStripRegistry.SuspendLayout();
            this.contextMenuStripTrace.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tableLayoutPanelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripMain
            // 
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.eventToolStripMenuItem,
            this.processToolStripMenuItem,
            this.exportToolStripMenuItem,
            this.monitorToolStripMenuItem,
            this.analysisToolStripMenuItem,
            this.compareToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.Size = new System.Drawing.Size(1240, 24);
            this.menuStripMain.TabIndex = 0;
            this.menuStripMain.Text = "menuStrip2";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openLogToolStripMenuItem,
            this.openLogfilteredToolStripMenuItem,
            this.saveLogToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openLogToolStripMenuItem
            // 
            this.openLogToolStripMenuItem.Name = "openLogToolStripMenuItem";
            this.openLogToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openLogToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.openLogToolStripMenuItem.Text = "&Open Trace";
            this.openLogToolStripMenuItem.Click += new System.EventHandler(this.OpenLogToolStripMenuItemClick);
            // 
            // openLogfilteredToolStripMenuItem
            // 
            this.openLogfilteredToolStripMenuItem.Name = "openLogfilteredToolStripMenuItem";
            this.openLogfilteredToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.openLogfilteredToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.openLogfilteredToolStripMenuItem.Text = "Open Trace F&iltered";
            this.openLogfilteredToolStripMenuItem.ToolTipText = "Load log after filtering";
            this.openLogfilteredToolStripMenuItem.Click += new System.EventHandler(this.OpenLogfilteredToolStripMenuItemClick);
            // 
            // saveLogToolStripMenuItem
            // 
            this.saveLogToolStripMenuItem.Name = "saveLogToolStripMenuItem";
            this.saveLogToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveLogToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.saveLogToolStripMenuItem.Text = "&Save Trace";
            this.saveLogToolStripMenuItem.Click += new System.EventHandler(this.SaveLogToolStripMenuItemClick);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.toolStripSeparator3,
            this.selectAllToolStripMenuItem,
            this.toolStripSeparator9,
            this.findToolStripMenuItem,
            this.goToLineToolStripMenuItem,
            this.toolStripSeparator6,
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
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(161, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.selectAllToolStripMenuItem.Text = "Select &All";
            this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.FormMainSelectAll);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(161, 6);
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
            // goToLineToolStripMenuItem
            // 
            this.goToLineToolStripMenuItem.Name = "goToLineToolStripMenuItem";
            this.goToLineToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
            this.goToLineToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.goToLineToolStripMenuItem.Text = "&Go To...";
            this.goToLineToolStripMenuItem.Click += new System.EventHandler(this.GoToToolStripMenuItemClick);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(161, 6);
            // 
            // filterToolStripMenuItem
            // 
            this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
            this.filterToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.filterToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.filterToolStripMenuItem.Text = "Fi&lter";
            this.filterToolStripMenuItem.Click += new System.EventHandler(this.FilterToolStripMenuItemClick);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileSystemToolStripMenuItem,
            this.registryToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // fileSystemToolStripMenuItem
            // 
            this.fileSystemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemFileSystemFlatMode,
            this.toolStripMenuItemFileSystemTreeMode,
            this.toolStripSeparator10,
            this.toolStripMenuItemHideAttributes,
            this.toolStripSeparator11,
            this.showLayerPathsToolStripMenuItem,
            this.mergeWowPathsToolStripMenuItem,
            this.showStartupModulesToolStripMenuItem});
            this.fileSystemToolStripMenuItem.Name = "fileSystemToolStripMenuItem";
            this.fileSystemToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.fileSystemToolStripMenuItem.Text = "&File System";
            // 
            // toolStripMenuItemFileSystemFlatMode
            // 
            this.toolStripMenuItemFileSystemFlatMode.Name = "toolStripMenuItemFileSystemFlatMode";
            this.toolStripMenuItemFileSystemFlatMode.Size = new System.Drawing.Size(248, 22);
            this.toolStripMenuItemFileSystemFlatMode.Text = "&Flat";
            this.toolStripMenuItemFileSystemFlatMode.Click += new System.EventHandler(this.ToolStripMenuItemFileSystemFlatModeClick);
            // 
            // toolStripMenuItemFileSystemTreeMode
            // 
            this.toolStripMenuItemFileSystemTreeMode.Name = "toolStripMenuItemFileSystemTreeMode";
            this.toolStripMenuItemFileSystemTreeMode.Size = new System.Drawing.Size(248, 22);
            this.toolStripMenuItemFileSystemTreeMode.Text = "&Tree";
            this.toolStripMenuItemFileSystemTreeMode.Click += new System.EventHandler(this.ToolStripMenuItemFileSystemTreeModeClick);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(245, 6);
            // 
            // toolStripMenuItemHideAttributes
            // 
            this.toolStripMenuItemHideAttributes.CheckOnClick = true;
            this.toolStripMenuItemHideAttributes.Name = "toolStripMenuItemHideAttributes";
            this.toolStripMenuItemHideAttributes.Size = new System.Drawing.Size(248, 22);
            this.toolStripMenuItemHideAttributes.Text = "Hide QueryAttributes Operations";
            this.toolStripMenuItemHideAttributes.Click += new System.EventHandler(this.ToolStripMenuItemAttribtuesClick);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(245, 6);
            // 
            // showLayerPathsToolStripMenuItem
            // 
            this.showLayerPathsToolStripMenuItem.CheckOnClick = true;
            this.showLayerPathsToolStripMenuItem.Name = "showLayerPathsToolStripMenuItem";
            this.showLayerPathsToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.showLayerPathsToolStripMenuItem.Text = "Show Virtual Paths";
            this.showLayerPathsToolStripMenuItem.Click += new System.EventHandler(this.ShowLayerPathsToolStripMenuItemClick);
            // 
            // mergeWowPathsToolStripMenuItem
            // 
            this.mergeWowPathsToolStripMenuItem.CheckOnClick = true;
            this.mergeWowPathsToolStripMenuItem.Name = "mergeWowPathsToolStripMenuItem";
            this.mergeWowPathsToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.mergeWowPathsToolStripMenuItem.Text = "Merge Wow Paths";
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
            // registryToolStripMenuItem
            // 
            this.registryToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mergeCOMClassesToolStripMenuItem,
            this.mergeWowKeyPathsToolStripMenuItem});
            this.registryToolStripMenuItem.Name = "registryToolStripMenuItem";
            this.registryToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.registryToolStripMenuItem.Text = "&Registry";
            // 
            // mergeCOMClassesToolStripMenuItem
            // 
            this.mergeCOMClassesToolStripMenuItem.CheckOnClick = true;
            this.mergeCOMClassesToolStripMenuItem.Name = "mergeCOMClassesToolStripMenuItem";
            this.mergeCOMClassesToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.mergeCOMClassesToolStripMenuItem.Text = "Merge COM Class Keys";
            this.mergeCOMClassesToolStripMenuItem.Click += new System.EventHandler(this.MergeComClassesToolStripMenuItemClick);
            // 
            // mergeWowKeyPathsToolStripMenuItem
            // 
            this.mergeWowKeyPathsToolStripMenuItem.CheckOnClick = true;
            this.mergeWowKeyPathsToolStripMenuItem.Name = "mergeWowKeyPathsToolStripMenuItem";
            this.mergeWowKeyPathsToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.mergeWowKeyPathsToolStripMenuItem.Text = "Merge Wow Key Paths";
            this.mergeWowKeyPathsToolStripMenuItem.Click += new System.EventHandler(this.MergeWowKeyPathsToolStripMenuItemClick);
            // 
            // eventToolStripMenuItem
            // 
            this.eventToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.propertiesToolStripMenuItem});
            this.eventToolStripMenuItem.Name = "eventToolStripMenuItem";
            this.eventToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.eventToolStripMenuItem.Text = "E&vent";
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+P";
            this.propertiesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.propertiesToolStripMenuItem.Text = "&Properties";
            this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.PropertiesToolStripMenuItemClick);
            // 
            // processToolStripMenuItem
            // 
            this.processToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.terminateProcessMenuToolStripMenuItem,
            this.toolStripSeparatorProcess1,
            this.processPropertiesMenuToolStripMenuItem});
            this.processToolStripMenuItem.Name = "processToolStripMenuItem";
            this.processToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.processToolStripMenuItem.Text = "&Process";
            // 
            // terminateProcessMenuToolStripMenuItem
            // 
            this.terminateProcessMenuToolStripMenuItem.Name = "terminateProcessMenuToolStripMenuItem";
            this.terminateProcessMenuToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.E)));
            this.terminateProcessMenuToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.terminateProcessMenuToolStripMenuItem.Text = "&Terminate";
            this.terminateProcessMenuToolStripMenuItem.Click += new System.EventHandler(this.ProcessTerminate);
            // 
            // toolStripSeparatorProcess1
            // 
            this.toolStripSeparatorProcess1.Name = "toolStripSeparatorProcess1";
            this.toolStripSeparatorProcess1.Size = new System.Drawing.Size(161, 6);
            // 
            // processPropertiesMenuToolStripMenuItem
            // 
            this.processPropertiesMenuToolStripMenuItem.Name = "processPropertiesMenuToolStripMenuItem";
            this.processPropertiesMenuToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.processPropertiesMenuToolStripMenuItem.Text = "&Properties";
            this.processPropertiesMenuToolStripMenuItem.Click += new System.EventHandler(this.ProcessesProperties);
            // 
            // exportToolStripMenuItem
            // 
            this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.swvToolStripMenuItem,
            this.exportToThinAppToolStripMenuItem,
            this.massExportToThinAppToolStripMenuItem,
            this.createOrEditTemplateToolStripMenuItem});
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.exportToolStripMenuItem.Text = "E&xport";
            // 
            // swvToolStripMenuItem
            // 
            this.swvToolStripMenuItem.Name = "swvToolStripMenuItem";
            this.swvToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.swvToolStripMenuItem.Text = "Export to SWV™";
            this.swvToolStripMenuItem.Click += new System.EventHandler(this.ExportToSwvToolStripMenuItemClick);
            // 
            // exportToThinAppToolStripMenuItem
            // 
            this.exportToThinAppToolStripMenuItem.Name = "exportToThinAppToolStripMenuItem";
            this.exportToThinAppToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.exportToThinAppToolStripMenuItem.Text = "Export to ThinApp™";
            this.exportToThinAppToolStripMenuItem.Click += new System.EventHandler(this.ExportToThinAppToolStripMenuItemClick);
            // 
            // massExportToThinAppToolStripMenuItem
            // 
            this.massExportToThinAppToolStripMenuItem.Enabled = false;
            this.massExportToThinAppToolStripMenuItem.Name = "massExportToThinAppToolStripMenuItem";
            this.massExportToThinAppToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.massExportToThinAppToolStripMenuItem.Text = "Mass Export to ThinApp™";
            this.massExportToThinAppToolStripMenuItem.Visible = false;
            this.massExportToThinAppToolStripMenuItem.Click += new System.EventHandler(this.MassExportToThinAppToolStripMenuItemClick);
            // 
            // createOrEditTemplateToolStripMenuItem
            // 
            this.createOrEditTemplateToolStripMenuItem.Enabled = false;
            this.createOrEditTemplateToolStripMenuItem.Name = "createOrEditTemplateToolStripMenuItem";
            this.createOrEditTemplateToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.createOrEditTemplateToolStripMenuItem.Text = "Create or Edit Template";
            this.createOrEditTemplateToolStripMenuItem.Visible = false;
            this.createOrEditTemplateToolStripMenuItem.Click += new System.EventHandler(this.CreateOrEditTemplateToolStripMenuItemClick);
            // 
            // monitorToolStripMenuItem
            // 
            this.monitorToolStripMenuItem.Name = "monitorToolStripMenuItem";
            this.monitorToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.monitorToolStripMenuItem.Text = "&Monitor";
            // 
            // analysisToolStripMenuItem
            // 
            this.analysisToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemStart,
            this.executeInstallerAndHookToolStripMenuItem,
            this.executionPropertiesToolStripMenuItem,
            this.toolStripMenuItemStop,
            this.toolStripSeparator7,
            this.statisticsToolStripMenuItem1,
            this.toolStripSeparator12,
            this.collectingDataToolStripMenuItem,
            this.fullStackInfoToolStripMenuItem,
            this.toolStripSeparator1,
            this.hookNewProcessesToolStripMenuItem,
            this.clearFailedHooksToolStripMenuItem,
            this.clearDataToolStripMenuItem});
            this.analysisToolStripMenuItem.Name = "analysisToolStripMenuItem";
            this.analysisToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.analysisToolStripMenuItem.Text = "&Analysis";
            // 
            // toolStripMenuItemStart
            // 
            this.toolStripMenuItemStart.Enabled = false;
            this.toolStripMenuItemStart.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemStart.Image")));
            this.toolStripMenuItemStart.Name = "toolStripMenuItemStart";
            this.toolStripMenuItemStart.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.toolStripMenuItemStart.Size = new System.Drawing.Size(253, 22);
            this.toolStripMenuItemStart.Text = "&Execute and Hook";
            this.toolStripMenuItemStart.ToolTipText = "Execute selected process and its children";
            this.toolStripMenuItemStart.Click += new System.EventHandler(this.ToolStripMenuItemStartClick);
            // 
            // executeInstallerAndHookToolStripMenuItem
            // 
            this.executeInstallerAndHookToolStripMenuItem.Name = "executeInstallerAndHookToolStripMenuItem";
            this.executeInstallerAndHookToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.executeInstallerAndHookToolStripMenuItem.Text = "Execute Installer and Hook";
            this.executeInstallerAndHookToolStripMenuItem.Click += new System.EventHandler(this.ExecuteInstallerAndHookToolStripMenuItemClick);
            // 
            // executionPropertiesToolStripMenuItem
            // 
            this.executionPropertiesToolStripMenuItem.Name = "executionPropertiesToolStripMenuItem";
            this.executionPropertiesToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.executionPropertiesToolStripMenuItem.Text = "Execution Parameters ...";
            this.executionPropertiesToolStripMenuItem.Click += new System.EventHandler(this.ExecutionPropertiesToolStripMenuItemClick);
            // 
            // toolStripMenuItemStop
            // 
            this.toolStripMenuItemStop.Enabled = false;
            this.toolStripMenuItemStop.Name = "toolStripMenuItemStop";
            this.toolStripMenuItemStop.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F5)));
            this.toolStripMenuItemStop.Size = new System.Drawing.Size(253, 22);
            this.toolStripMenuItemStop.Text = "S&top All";
            this.toolStripMenuItemStop.Click += new System.EventHandler(this.ToolStripMenuItemStopClick);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(250, 6);
            // 
            // statisticsToolStripMenuItem1
            // 
            this.statisticsToolStripMenuItem1.Name = "statisticsToolStripMenuItem1";
            this.statisticsToolStripMenuItem1.Size = new System.Drawing.Size(253, 22);
            this.statisticsToolStripMenuItem1.Text = "&Statistics";
            this.statisticsToolStripMenuItem1.Click += new System.EventHandler(this.StatisticsToolStripMenuItem1Click);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(250, 6);
            // 
            // collectingDataToolStripMenuItem
            // 
            this.collectingDataToolStripMenuItem.Checked = true;
            this.collectingDataToolStripMenuItem.CheckOnClick = true;
            this.collectingDataToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.collectingDataToolStripMenuItem.Name = "collectingDataToolStripMenuItem";
            this.collectingDataToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.collectingDataToolStripMenuItem.Text = "C&ollecting Data";
            this.collectingDataToolStripMenuItem.Click += new System.EventHandler(this.CollectingDataToolStripMenuItemClick);
            // 
            // fullStackInfoToolStripMenuItem
            // 
            this.fullStackInfoToolStripMenuItem.Name = "fullStackInfoToolStripMenuItem";
            this.fullStackInfoToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.fullStackInfoToolStripMenuItem.Text = "Full stac&k information";
            this.fullStackInfoToolStripMenuItem.Click += new System.EventHandler(this.FullStackInfoToolStripMenuItemClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(250, 6);
            // 
            // hookNewProcessesToolStripMenuItem
            // 
            this.hookNewProcessesToolStripMenuItem.Name = "hookNewProcessesToolStripMenuItem";
            this.hookNewProcessesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.hookNewProcessesToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.hookNewProcessesToolStripMenuItem.Text = "&Hook New User Processes";
            this.hookNewProcessesToolStripMenuItem.ToolTipText = "Hook all new processes created by the user";
            this.hookNewProcessesToolStripMenuItem.Click += new System.EventHandler(this.HookNewProcessesToolStripMenuItemClick);
            // 
            // clearFailedHooksToolStripMenuItem
            // 
            this.clearFailedHooksToolStripMenuItem.Name = "clearFailedHooksToolStripMenuItem";
            this.clearFailedHooksToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.clearFailedHooksToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.clearFailedHooksToolStripMenuItem.Text = "C&lear Failed Hooks";
            this.clearFailedHooksToolStripMenuItem.Click += new System.EventHandler(this.ClearFailedHooksToolStripMenuItemClick);
            // 
            // clearDataToolStripMenuItem
            // 
            this.clearDataToolStripMenuItem.Name = "clearDataToolStripMenuItem";
            this.clearDataToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.clearDataToolStripMenuItem.Size = new System.Drawing.Size(253, 22);
            this.clearDataToolStripMenuItem.Text = "&Clear Data";
            this.clearDataToolStripMenuItem.Click += new System.EventHandler(this.ClearDataToolStripMenuItemClick);
            // 
            // compareToolStripMenuItem
            // 
            this.compareToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tracesToolStripMenuItem,
            this.filteredTracesToolStripMenuItem,
            this.toolStripSeparator2,
            this.statisticsToolStripMenuItem});
            this.compareToolStripMenuItem.Name = "compareToolStripMenuItem";
            this.compareToolStripMenuItem.Size = new System.Drawing.Size(68, 20);
            this.compareToolStripMenuItem.Text = "&Compare";
            // 
            // tracesToolStripMenuItem
            // 
            this.tracesToolStripMenuItem.Name = "tracesToolStripMenuItem";
            this.tracesToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.tracesToolStripMenuItem.Text = "&Traces";
            this.tracesToolStripMenuItem.ToolTipText = "Compare traces";
            this.tracesToolStripMenuItem.Click += new System.EventHandler(this.CompareTracesToolStripMenuItemClick);
            // 
            // filteredTracesToolStripMenuItem
            // 
            this.filteredTracesToolStripMenuItem.Name = "filteredTracesToolStripMenuItem";
            this.filteredTracesToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.filteredTracesToolStripMenuItem.Text = "&Filtered Traces";
            this.filteredTracesToolStripMenuItem.ToolTipText = "Filter traces before comparing";
            this.filteredTracesToolStripMenuItem.Click += new System.EventHandler(this.FilteredTracesToolStripMenuItemClick);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(147, 6);
            // 
            // statisticsToolStripMenuItem
            // 
            this.statisticsToolStripMenuItem.Name = "statisticsToolStripMenuItem";
            this.statisticsToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.statisticsToolStripMenuItem.Text = "&Statistics";
            this.statisticsToolStripMenuItem.Click += new System.EventHandler(this.CompareStatisticsToolStripMenuItemClick);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem,
            this.toolStripSeparator13,
            this.aboutAppStudioToolStripMenuItem});
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "A&bout";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.helpToolStripMenuItem.Text = "&Help";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.HelpToolStripMenuItemClick);
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            this.toolStripSeparator13.Size = new System.Drawing.Size(160, 6);
            // 
            // aboutAppStudioToolStripMenuItem
            // 
            this.aboutAppStudioToolStripMenuItem.Name = "aboutAppStudioToolStripMenuItem";
            this.aboutAppStudioToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.aboutAppStudioToolStripMenuItem.Text = "About SpyStudio";
            this.aboutAppStudioToolStripMenuItem.Click += new System.EventHandler(this.AboutAppStudioToolStripMenuItemClick);
            // 
            // contextMenuStripFiles
            // 
            this.contextMenuStripFiles.Name = "contextMenuStripFiles";
            this.contextMenuStripFiles.Size = new System.Drawing.Size(61, 4);
            // 
            // removeFileToolStripMenuItem
            // 
            this.removeFileToolStripMenuItem.Name = "removeFileToolStripMenuItem";
            this.removeFileToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeFileToolStripMenuItem.Text = "&Remove";
            // 
            // removeAllFilesToolStripMenuItem
            // 
            this.removeAllFilesToolStripMenuItem.Name = "removeAllFilesToolStripMenuItem";
            this.removeAllFilesToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeAllFilesToolStripMenuItem.Text = "Remove &All";
            // 
            // splitMain
            // 
            this.splitMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(3, 3);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.tableLayoutLeft);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.callDump);
            this.splitMain.Size = new System.Drawing.Size(1234, 492);
            this.splitMain.SplitterDistance = 206;
            this.splitMain.TabIndex = 1;
            this.splitMain.TabStop = false;
            // 
            // tableLayoutLeft
            // 
            this.tableLayoutLeft.ColumnCount = 1;
            this.tableLayoutLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutLeft.Controls.Add(this.panelOpenFile, 0, 0);
            this.tableLayoutLeft.Controls.Add(this.panelProcesses, 0, 1);
            this.tableLayoutLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutLeft.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutLeft.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutLeft.Name = "tableLayoutLeft";
            this.tableLayoutLeft.RowCount = 2;
            this.tableLayoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutLeft.Size = new System.Drawing.Size(204, 490);
            this.tableLayoutLeft.TabIndex = 0;
            // 
            // panelOpenFile
            // 
            this.panelOpenFile.Controls.Add(this.tableLayoutPanel1);
            this.panelOpenFile.Controls.Add(this.tableLayoutLeftTop);
            this.panelOpenFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelOpenFile.Location = new System.Drawing.Point(0, 0);
            this.panelOpenFile.Margin = new System.Windows.Forms.Padding(0);
            this.panelOpenFile.Name = "panelOpenFile";
            this.panelOpenFile.Size = new System.Drawing.Size(204, 67);
            this.panelOpenFile.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 2F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.tableLayoutPanel1.Controls.Add(this.buttonPlay, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 43);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(204, 24);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // buttonPlay
            // 
            this.buttonPlay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPlay.Image = ((System.Drawing.Image)(resources.GetObject("buttonPlay.Image")));
            this.buttonPlay.Location = new System.Drawing.Point(2, 0);
            this.buttonPlay.Margin = new System.Windows.Forms.Padding(0);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(201, 24);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.ButtonPlayClick);
            // 
            // tableLayoutLeftTop
            // 
            this.tableLayoutLeftTop.ColumnCount = 2;
            this.tableLayoutLeftTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutLeftTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutLeftTop.Controls.Add(this.label2, 0, 0);
            this.tableLayoutLeftTop.Controls.Add(this.textBoxProgramExec, 0, 1);
            this.tableLayoutLeftTop.Controls.Add(this.buttonOpenFile, 1, 1);
            this.tableLayoutLeftTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutLeftTop.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutLeftTop.Margin = new System.Windows.Forms.Padding(3, 0, 0, 2);
            this.tableLayoutLeftTop.Name = "tableLayoutLeftTop";
            this.tableLayoutLeftTop.RowCount = 3;
            this.tableLayoutLeftTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutLeftTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutLeftTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutLeftTop.Size = new System.Drawing.Size(204, 43);
            this.tableLayoutLeftTop.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(3, 3);
            this.label2.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(171, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Execute and Hook";
            // 
            // textBoxProgramExec
            // 
            this.textBoxProgramExec.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxProgramExec.Location = new System.Drawing.Point(3, 21);
            this.textBoxProgramExec.Margin = new System.Windows.Forms.Padding(3, 0, 3, 2);
            this.textBoxProgramExec.Name = "textBoxProgramExec";
            this.textBoxProgramExec.Size = new System.Drawing.Size(171, 20);
            this.textBoxProgramExec.TabIndex = 0;
            this.textBoxProgramExec.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxProgramExecKeyPress);
            // 
            // buttonOpenFile
            // 
            this.buttonOpenFile.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonOpenFile.Location = new System.Drawing.Point(177, 20);
            this.buttonOpenFile.Margin = new System.Windows.Forms.Padding(0, 0, 1, 1);
            this.buttonOpenFile.Name = "buttonOpenFile";
            this.buttonOpenFile.Size = new System.Drawing.Size(26, 22);
            this.buttonOpenFile.TabIndex = 1;
            this.buttonOpenFile.Text = "...";
            this.buttonOpenFile.UseVisualStyleBackColor = true;
            this.buttonOpenFile.Click += new System.EventHandler(this.ButtonOpenFileClick);
            // 
            // panelProcesses
            // 
            this.panelProcesses.Controls.Add(this.tableLayoutLeftBottom);
            this.panelProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelProcesses.Location = new System.Drawing.Point(0, 67);
            this.panelProcesses.Margin = new System.Windows.Forms.Padding(0);
            this.panelProcesses.MinimumSize = new System.Drawing.Size(0, 50);
            this.panelProcesses.Name = "panelProcesses";
            this.panelProcesses.Size = new System.Drawing.Size(204, 423);
            this.panelProcesses.TabIndex = 1;
            // 
            // tableLayoutLeftBottom
            // 
            this.tableLayoutLeftBottom.ColumnCount = 1;
            this.tableLayoutLeftBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutLeftBottom.Controls.Add(this.label1, 0, 1);
            this.tableLayoutLeftBottom.Controls.Add(this.listViewProcesses, 0, 2);
            this.tableLayoutLeftBottom.Controls.Add(this.label5, 0, 0);
            this.tableLayoutLeftBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutLeftBottom.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutLeftBottom.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutLeftBottom.Name = "tableLayoutLeftBottom";
            this.tableLayoutLeftBottom.RowCount = 3;
            this.tableLayoutLeftBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutLeftBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutLeftBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutLeftBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutLeftBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutLeftBottom.Size = new System.Drawing.Size(204, 423);
            this.tableLayoutLeftBottom.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 13);
            this.label1.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(204, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Running Process";
            // 
            // listViewProcesses
            // 
            this.listViewProcesses.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName,
            this.columnPid});
            this.listViewProcesses.ContextMenuStrip = this.contextMenuStripProcess;
            this.listViewProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewProcesses.FullRowSelect = true;
            this.listViewProcesses.HideSelection = false;
            this.listViewProcesses.IgnoreCase = true;
            this.listViewProcesses.Location = new System.Drawing.Point(3, 30);
            this.listViewProcesses.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.listViewProcesses.MinimumSize = new System.Drawing.Size(102, 50);
            this.listViewProcesses.Name = "listViewProcesses";
            this.listViewProcesses.Size = new System.Drawing.Size(198, 393);
            this.listViewProcesses.SortColumn = 0;
            this.listViewProcesses.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewProcesses.TabIndex = 4;
            this.listViewProcesses.UseCompatibleStateImageBehavior = false;
            this.listViewProcesses.View = System.Windows.Forms.View.Details;
            this.listViewProcesses.ClientSizeChanged += new System.EventHandler(this.ListViewProcessesClientSizeChanged);
            this.listViewProcesses.SelectedIndexChanged += new System.EventHandler(this.ListViewProcessesSelectedIndexChanged);
            // 
            // columnName
            // 
            this.columnName.Text = "Name";
            this.columnName.Width = 134;
            // 
            // columnPid
            // 
            this.columnPid.Tag = "Numeric";
            this.columnPid.Text = "Pid";
            this.columnPid.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnPid.Width = 44;
            // 
            // contextMenuStripProcess
            // 
            this.contextMenuStripProcess.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.unhookToolStripMenuItem,
            this.hookToolStripMenuItem,
            this.toolStripSeparator8,
            this.terminateToolStripMenuItem,
            this.toolStripSeparator5,
            this.processPropertiesToolStripMenuItem});
            this.contextMenuStripProcess.Name = "contextMenuStripProcess";
            this.contextMenuStripProcess.Size = new System.Drawing.Size(165, 104);
            // 
            // unhookToolStripMenuItem
            // 
            this.unhookToolStripMenuItem.Name = "unhookToolStripMenuItem";
            this.unhookToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.unhookToolStripMenuItem.Text = "&Unhook";
            this.unhookToolStripMenuItem.Click += new System.EventHandler(this.UnhookToolStripMenuItemClick);
            // 
            // hookToolStripMenuItem
            // 
            this.hookToolStripMenuItem.Name = "hookToolStripMenuItem";
            this.hookToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.hookToolStripMenuItem.Text = "&Hook";
            this.hookToolStripMenuItem.Click += new System.EventHandler(this.HookToolStripMenuItemClick);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(161, 6);
            // 
            // terminateToolStripMenuItem
            // 
            this.terminateToolStripMenuItem.Name = "terminateToolStripMenuItem";
            this.terminateToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.E)));
            this.terminateToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.terminateToolStripMenuItem.Text = "&Terminate";
            this.terminateToolStripMenuItem.Click += new System.EventHandler(this.ProcessTerminate);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(161, 6);
            // 
            // processPropertiesToolStripMenuItem
            // 
            this.processPropertiesToolStripMenuItem.Name = "processPropertiesToolStripMenuItem";
            this.processPropertiesToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.processPropertiesToolStripMenuItem.Text = "&Properties";
            this.processPropertiesToolStripMenuItem.Click += new System.EventHandler(this.ProcessesProperties);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label5.Location = new System.Drawing.Point(3, 5);
            this.label5.Margin = new System.Windows.Forms.Padding(3, 5, 3, 1);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(198, 4);
            this.label5.TabIndex = 6;
            // 
            // callDump
            // 
            this.callDump.Dock = System.Windows.Forms.DockStyle.Fill;
            this.callDump.Location = new System.Drawing.Point(0, 0);
            this.callDump.Margin = new System.Windows.Forms.Padding(4);
            this.callDump.Name = "callDump";
            this.callDump.SelectedTabIndex = 0;
            this.callDump.Size = new System.Drawing.Size(1022, 490);
            this.callDump.TabIndex = 6;
            // 
            // contextMenuStripCom
            // 
            this.contextMenuStripCom.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeComToolStripMenuItem,
            this.removeAllComToolStripMenuItem});
            this.contextMenuStripCom.Name = "contextMenuStripCom";
            this.contextMenuStripCom.Size = new System.Drawing.Size(135, 48);
            // 
            // removeComToolStripMenuItem
            // 
            this.removeComToolStripMenuItem.Name = "removeComToolStripMenuItem";
            this.removeComToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeComToolStripMenuItem.Text = "&Remove";
            // 
            // removeAllComToolStripMenuItem
            // 
            this.removeAllComToolStripMenuItem.Name = "removeAllComToolStripMenuItem";
            this.removeAllComToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeAllComToolStripMenuItem.Text = "Remove &All";
            // 
            // contextMenuStripWindow
            // 
            this.contextMenuStripWindow.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeWindowToolStripMenuItem,
            this.removeWindowAllToolStripMenuItem});
            this.contextMenuStripWindow.Name = "contextMenuStripWindow";
            this.contextMenuStripWindow.Size = new System.Drawing.Size(135, 48);
            // 
            // removeWindowToolStripMenuItem
            // 
            this.removeWindowToolStripMenuItem.Name = "removeWindowToolStripMenuItem";
            this.removeWindowToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeWindowToolStripMenuItem.Text = "&Remove";
            // 
            // removeWindowAllToolStripMenuItem
            // 
            this.removeWindowAllToolStripMenuItem.Name = "removeWindowAllToolStripMenuItem";
            this.removeWindowAllToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeWindowAllToolStripMenuItem.Text = "Remove &All";
            // 
            // contextMenuStripRegistry
            // 
            this.contextMenuStripRegistry.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeKeyToolStripMenuItem,
            this.removeAllKeysToolStripMenuItem,
            this.addKeyToolStripMenuItem});
            this.contextMenuStripRegistry.Name = "contextMenuStripRegistry";
            this.contextMenuStripRegistry.Size = new System.Drawing.Size(135, 70);
            // 
            // removeKeyToolStripMenuItem
            // 
            this.removeKeyToolStripMenuItem.Name = "removeKeyToolStripMenuItem";
            this.removeKeyToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeKeyToolStripMenuItem.Text = "&Remove";
            // 
            // removeAllKeysToolStripMenuItem
            // 
            this.removeAllKeysToolStripMenuItem.Name = "removeAllKeysToolStripMenuItem";
            this.removeAllKeysToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeAllKeysToolStripMenuItem.Text = "Remove &All";
            // 
            // addKeyToolStripMenuItem
            // 
            this.addKeyToolStripMenuItem.Name = "addKeyToolStripMenuItem";
            this.addKeyToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.addKeyToolStripMenuItem.Text = "&Add";
            // 
            // contextMenuStripTrace
            // 
            this.contextMenuStripTrace.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeTraceToolStripMenuItem,
            this.removeAllTraceToolStripMenuItem,
            this.toolStripSeparator4,
            this.propertiesTraceToolStripMenuItem});
            this.contextMenuStripTrace.Name = "contextMenuStripTrace";
            this.contextMenuStripTrace.Size = new System.Drawing.Size(135, 76);
            // 
            // removeTraceToolStripMenuItem
            // 
            this.removeTraceToolStripMenuItem.Name = "removeTraceToolStripMenuItem";
            this.removeTraceToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeTraceToolStripMenuItem.Text = "&Remove";
            // 
            // removeAllTraceToolStripMenuItem
            // 
            this.removeAllTraceToolStripMenuItem.Name = "removeAllTraceToolStripMenuItem";
            this.removeAllTraceToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.removeAllTraceToolStripMenuItem.Text = "Remove &All";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(131, 6);
            // 
            // propertiesTraceToolStripMenuItem
            // 
            this.propertiesTraceToolStripMenuItem.Name = "propertiesTraceToolStripMenuItem";
            this.propertiesTraceToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.propertiesTraceToolStripMenuItem.Text = "&Properties";
            // 
            // imageListTraceListView
            // 
            this.imageListTraceListView.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListTraceListView.ImageStream")));
            this.imageListTraceListView.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListTraceListView.Images.SetKeyName(0, "main.ico");
            // 
            // columnHeaderCount
            // 
            this.columnHeaderCount.Header = "#";
            this.columnHeaderCount.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderCount.TooltipText = null;
            this.columnHeaderCount.Width = 70;
            // 
            // columnHeaderProcessName
            // 
            this.columnHeaderProcessName.Header = "Process Name";
            this.columnHeaderProcessName.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderProcessName.TooltipText = null;
            this.columnHeaderProcessName.Width = 87;
            // 
            // columnHeaderPid
            // 
            this.columnHeaderPid.Header = "Pid";
            this.columnHeaderPid.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderPid.TooltipText = null;
            this.columnHeaderPid.Width = 43;
            // 
            // columnHeaderTid
            // 
            this.columnHeaderTid.Header = "Tid";
            this.columnHeaderTid.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderTid.TooltipText = null;
            this.columnHeaderTid.Width = 46;
            // 
            // columnHeaderCaller
            // 
            this.columnHeaderCaller.Header = "Caller";
            this.columnHeaderCaller.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderCaller.TooltipText = null;
            // 
            // columnHeaderFunction
            // 
            this.columnHeaderFunction.Header = "Function";
            this.columnHeaderFunction.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderFunction.TooltipText = null;
            this.columnHeaderFunction.Width = 70;
            // 
            // columnHeaderPath
            // 
            this.columnHeaderPath.Header = "Path";
            this.columnHeaderPath.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderPath.TooltipText = null;
            this.columnHeaderPath.Width = 210;
            // 
            // columnHeaderDetails
            // 
            this.columnHeaderDetails.Header = "Details";
            this.columnHeaderDetails.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderDetails.TooltipText = null;
            this.columnHeaderDetails.Width = 190;
            // 
            // columnHeaderResult
            // 
            this.columnHeaderResult.Header = "Result";
            this.columnHeaderResult.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderResult.TooltipText = null;
            this.columnHeaderResult.Width = 80;
            // 
            // columnHeaderTimeCol
            // 
            this.columnHeaderTimeCol.Header = "Time";
            this.columnHeaderTimeCol.SortOrder = System.Windows.Forms.SortOrder.None;
            this.columnHeaderTimeCol.TooltipText = null;
            this.columnHeaderTimeCol.Width = 60;
            // 
            // columnHeaderCountNode
            // 
            this.columnHeaderCountNode.CompactString = false;
            this.columnHeaderCountNode.DataPropertyName = "Count";
            this.columnHeaderCountNode.IncrementalSearchEnabled = true;
            this.columnHeaderCountNode.LeftMargin = 3;
            this.columnHeaderCountNode.ParentColumn = this.columnHeaderCount;
            this.columnHeaderCountNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderProcessNameNode
            // 
            this.columnHeaderProcessNameNode.CompactString = false;
            this.columnHeaderProcessNameNode.DataPropertyName = "ProcessName";
            this.columnHeaderProcessNameNode.IncrementalSearchEnabled = true;
            this.columnHeaderProcessNameNode.LeftMargin = 3;
            this.columnHeaderProcessNameNode.ParentColumn = this.columnHeaderProcessName;
            this.columnHeaderProcessNameNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderPidNode
            // 
            this.columnHeaderPidNode.CompactString = false;
            this.columnHeaderPidNode.DataPropertyName = "Pid";
            this.columnHeaderPidNode.IncrementalSearchEnabled = true;
            this.columnHeaderPidNode.LeftMargin = 3;
            this.columnHeaderPidNode.ParentColumn = this.columnHeaderPid;
            this.columnHeaderPidNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderTidNode
            // 
            this.columnHeaderTidNode.CompactString = false;
            this.columnHeaderTidNode.DataPropertyName = "Tid";
            this.columnHeaderTidNode.IncrementalSearchEnabled = true;
            this.columnHeaderTidNode.LeftMargin = 3;
            this.columnHeaderTidNode.ParentColumn = this.columnHeaderTid;
            this.columnHeaderTidNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderCallerNode
            // 
            this.columnHeaderCallerNode.CompactString = false;
            this.columnHeaderCallerNode.DataPropertyName = "Caller";
            this.columnHeaderCallerNode.IncrementalSearchEnabled = true;
            this.columnHeaderCallerNode.LeftMargin = 3;
            this.columnHeaderCallerNode.ParentColumn = this.columnHeaderCaller;
            this.columnHeaderCallerNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderFunctionNode
            // 
            this.columnHeaderFunctionNode.CompactString = false;
            this.columnHeaderFunctionNode.DataPropertyName = "Function";
            this.columnHeaderFunctionNode.IncrementalSearchEnabled = true;
            this.columnHeaderFunctionNode.LeftMargin = 3;
            this.columnHeaderFunctionNode.ParentColumn = this.columnHeaderFunction;
            this.columnHeaderFunctionNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderPathNode
            // 
            this.columnHeaderPathNode.CompactString = false;
            this.columnHeaderPathNode.DataPropertyName = "Path";
            this.columnHeaderPathNode.IncrementalSearchEnabled = true;
            this.columnHeaderPathNode.LeftMargin = 3;
            this.columnHeaderPathNode.ParentColumn = this.columnHeaderPath;
            this.columnHeaderPathNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderDetailsNode
            // 
            this.columnHeaderDetailsNode.CompactString = false;
            this.columnHeaderDetailsNode.DataPropertyName = "Details";
            this.columnHeaderDetailsNode.IncrementalSearchEnabled = true;
            this.columnHeaderDetailsNode.LeftMargin = 3;
            this.columnHeaderDetailsNode.ParentColumn = this.columnHeaderDetails;
            this.columnHeaderDetailsNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderResultNode
            // 
            this.columnHeaderResultNode.CompactString = false;
            this.columnHeaderResultNode.DataPropertyName = "Result";
            this.columnHeaderResultNode.IncrementalSearchEnabled = true;
            this.columnHeaderResultNode.LeftMargin = 3;
            this.columnHeaderResultNode.ParentColumn = this.columnHeaderResult;
            this.columnHeaderResultNode.Trimming = System.Drawing.StringTrimming.Character;
            // 
            // columnHeaderTimeNode
            // 
            this.columnHeaderTimeNode.CompactString = false;
            this.columnHeaderTimeNode.DataPropertyName = "Time";
            this.columnHeaderTimeNode.IncrementalSearchEnabled = true;
            this.columnHeaderTimeNode.LeftMargin = 3;
            this.columnHeaderTimeNode.ParentColumn = this.columnHeaderTimeCol;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusBarTotalEvents,
            this._statusBarFilteredEvents,
            this._statusBarMiddle,
            this._statusBarProcessing,
            this._statusBarSyncIcon});
            this.statusStrip1.Location = new System.Drawing.Point(0, 498);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1240, 20);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // _statusBarTotalEvents
            // 
            this._statusBarTotalEvents.Name = "_statusBarTotalEvents";
            this._statusBarTotalEvents.Size = new System.Drawing.Size(83, 15);
            this._statusBarTotalEvents.Text = "Total Events: 0";
            // 
            // _statusBarFilteredEvents
            // 
            this._statusBarFilteredEvents.Name = "_statusBarFilteredEvents";
            this._statusBarFilteredEvents.Size = new System.Drawing.Size(95, 15);
            this._statusBarFilteredEvents.Text = "Filtered Events: 0";
            // 
            // _statusBarMiddle
            // 
            this._statusBarMiddle.Name = "_statusBarMiddle";
            this._statusBarMiddle.Size = new System.Drawing.Size(940, 15);
            this._statusBarMiddle.Spring = true;
            this._statusBarMiddle.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _statusBarProcessing
            // 
            this._statusBarProcessing.Name = "_statusBarProcessing";
            this._statusBarProcessing.Size = new System.Drawing.Size(107, 15);
            this._statusBarProcessing.Text = "No Pending Events";
            // 
            // _statusBarSyncIcon
            // 
            this._statusBarSyncIcon.Image = global::SpyStudio.Properties.Resources.syncing;
            this._statusBarSyncIcon.Name = "_statusBarSyncIcon";
            this._statusBarSyncIcon.Size = new System.Drawing.Size(16, 15);
            this._statusBarSyncIcon.Visible = false;
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Controls.Add(this.splitMain, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.statusStrip1, 0, 1);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanelMain.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 2;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(1240, 518);
            this.tableLayoutPanelMain.TabIndex = 7;
            // 
            // FormMain
            // 
            this.ClientSize = new System.Drawing.Size(1240, 542);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Controls.Add(this.menuStripMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStripMain;
            this.Name = "FormMain";
            this.Text = "SpyStudio";
            this.Load += new System.EventHandler(this.formMain_OnLoad);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.formMain_OnClose);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormMainKeyDown);
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            this.splitMain.ResumeLayout(false);
            this.tableLayoutLeft.ResumeLayout(false);
            this.panelOpenFile.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutLeftTop.ResumeLayout(false);
            this.tableLayoutLeftTop.PerformLayout();
            this.panelProcesses.ResumeLayout(false);
            this.tableLayoutLeftBottom.ResumeLayout(false);
            this.tableLayoutLeftBottom.PerformLayout();
            this.contextMenuStripProcess.ResumeLayout(false);
            this.contextMenuStripCom.ResumeLayout(false);
            this.contextMenuStripWindow.ResumeLayout(false);
            this.contextMenuStripRegistry.ResumeLayout(false);
            this.contextMenuStripTrace.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripFiles;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TableLayoutPanel tableLayoutLeft;
        private System.Windows.Forms.TableLayoutPanel tableLayoutLeftTop;
        private System.Windows.Forms.Panel panelOpenFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem analysisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemStart;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemStop;
        private System.Windows.Forms.ToolStripMenuItem clearDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutAppStudioToolStripMenuItem;
        //private System.Windows.Forms.ColumnHeader columnHeaderProcessName;
        //private System.Windows.Forms.ColumnHeader columnHeaderPid;
        //private System.Windows.Forms.ColumnHeader columnHeaderFunction;
        //private System.Windows.Forms.ColumnHeader columnHeaderDetails;
        //private System.Windows.Forms.ColumnHeader columnHeaderCaller;
        //private System.Windows.Forms.ColumnHeader columnHeaderTid;
        //private System.Windows.Forms.ColumnHeader columnHeaderResult;
        //private System.Windows.Forms.ColumnHeader columnHeaderCount;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderCountNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderPidNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderTidNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderCallerNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderFunctionNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderPathNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderResultNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderProcessNameNode;

        private Aga.Controls.Tree.TreeColumn columnHeaderCount;
        private Aga.Controls.Tree.TreeColumn columnHeaderPid;
        private Aga.Controls.Tree.TreeColumn columnHeaderTid;
        private Aga.Controls.Tree.TreeColumn columnHeaderCaller;
        private Aga.Controls.Tree.TreeColumn columnHeaderFunction;
        private Aga.Controls.Tree.TreeColumn columnHeaderPath;
        private Aga.Controls.Tree.TreeColumn columnHeaderResult;
        private Aga.Controls.Tree.TreeColumn columnHeaderProcessName;
        private System.Windows.Forms.ToolStripMenuItem removeFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAllFilesToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripRegistry;
        private System.Windows.Forms.ToolStripMenuItem removeKeyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAllKeysToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTrace;
        private System.Windows.Forms.ToolStripMenuItem removeTraceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAllTraceToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripCom;
        private System.Windows.Forms.ToolStripMenuItem removeComToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAllComToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripWindow;
        private System.Windows.Forms.ToolStripMenuItem removeWindowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeWindowAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem swvToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addKeyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ImageList imageListTraceListView;
        private Aga.Controls.Tree.TreeColumn columnHeaderDetails;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderDetailsNode;
        private System.Windows.Forms.ToolStripMenuItem openLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collectingDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem eventToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem monitorToolStripMenuItem;
        private Aga.Controls.Tree.NodeControls.NodeTextBox columnHeaderTimeNode;
        private Aga.Controls.Tree.TreeColumn columnHeaderTimeCol;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripProcess;
        private System.Windows.Forms.ToolStripMenuItem processPropertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processPropertiesMenuToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem propertiesTraceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem terminateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem terminateProcessMenuToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        public CallDump callDump;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem unhookToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hookToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.TextBox textBoxProgramExec;
        private System.Windows.Forms.Button buttonOpenFile;
        private System.Windows.Forms.ToolStripMenuItem compareToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tracesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem statisticsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem statisticsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem hookNewProcessesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLogfilteredToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filteredTracesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem clearFailedHooksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileSystemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFileSystemFlatMode;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFileSystemTreeMode;
        private System.Windows.Forms.ToolStripMenuItem goToLineToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemHideAttributes;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorProcess1;
        private System.Windows.Forms.ToolStripMenuItem showLayerPathsToolStripMenuItem;
        private System.Windows.Forms.Panel panelProcesses;
        private System.Windows.Forms.TableLayoutPanel tableLayoutLeftBottom;
        private System.Windows.Forms.Label label1;
        private ListViewSorted listViewProcesses;
        private System.Windows.Forms.ColumnHeader columnName;
        private System.Windows.Forms.ColumnHeader columnPid;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolStripMenuItem executionPropertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripMenuItem mergeWowPathsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showStartupModulesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem registryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mergeCOMClassesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mergeWowKeyPathsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToThinAppToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel _statusBarTotalEvents;
        private System.Windows.Forms.ToolStripStatusLabel _statusBarFilteredEvents;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.ToolStripStatusLabel _statusBarMiddle;
        private System.Windows.Forms.ToolStripStatusLabel _statusBarProcessing;
        private System.Windows.Forms.ToolStripStatusLabel _statusBarSyncIcon;
        private System.Windows.Forms.ToolStripMenuItem executeInstallerAndHookToolStripMenuItem;
        private ToolStripMenuItem massExportToThinAppToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator13;
        private ToolStripMenuItem fullStackInfoToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem createOrEditTemplateToolStripMenuItem;
    }
}

