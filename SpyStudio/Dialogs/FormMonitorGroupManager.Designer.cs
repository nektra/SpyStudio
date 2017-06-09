namespace SpyStudio.Dialogs
{
    partial class FormMonitorGroupManager
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
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.propertyGridFunction = new FunctionPropertyGrid.FunctionPropertyGrid();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enumerationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitContainerGroups = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.listViewSpyStudioGroups = new System.Windows.Forms.ListView();
            this.columnHeaderMonGroup = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderMonFncCount = new System.Windows.Forms.ColumnHeader();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.listViewSpyStudioFunctions = new System.Windows.Forms.ListView();
            this.label5 = new System.Windows.Forms.Label();
            this.tableLayoutPanelProperties = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.splitContainerBottom = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanelEnums = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainerEnums = new System.Windows.Forms.SplitContainer();
            this.listViewEnums = new System.Windows.Forms.ListView();
            this.columnHeaderEnum = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderCount = new System.Windows.Forms.ColumnHeader();
            this.tableLayoutPanelEnumItems = new System.Windows.Forms.TableLayoutPanel();
            this.listViewEnumItems = new System.Windows.Forms.ListView();
            this.columnHeaderItem = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderValue = new System.Windows.Forms.ColumnHeader();
            this.checkBoxEnumFlags = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tableLayoutPanelModules = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainerModules = new System.Windows.Forms.SplitContainer();
            this.listViewDeviareModules = new System.Windows.Forms.ListView();
            this.columnHeaderGroup = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderFncCount = new System.Windows.Forms.ColumnHeader();
            this.listViewDeviareModuleFunctions = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.menuStripMain.SuspendLayout();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.splitContainerGroups.Panel1.SuspendLayout();
            this.splitContainerGroups.Panel2.SuspendLayout();
            this.splitContainerGroups.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanelProperties.SuspendLayout();
            this.splitContainerBottom.Panel1.SuspendLayout();
            this.splitContainerBottom.Panel2.SuspendLayout();
            this.splitContainerBottom.SuspendLayout();
            this.tableLayoutPanelEnums.SuspendLayout();
            this.splitContainerEnums.Panel1.SuspendLayout();
            this.splitContainerEnums.Panel2.SuspendLayout();
            this.splitContainerEnums.SuspendLayout();
            this.tableLayoutPanelEnumItems.SuspendLayout();
            this.tableLayoutPanelModules.SuspendLayout();
            this.splitContainerModules.Panel1.SuspendLayout();
            this.splitContainerModules.Panel2.SuspendLayout();
            this.splitContainerModules.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripMain
            // 
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.groupsToolStripMenuItem,
            this.enumerationToolStripMenuItem});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.Size = new System.Drawing.Size(1052, 24);
            this.menuStripMain.TabIndex = 0;
            this.menuStripMain.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "&Open";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            // 
            // groupsToolStripMenuItem
            // 
            this.groupsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem1,
            this.deleteToolStripMenuItem});
            this.groupsToolStripMenuItem.Name = "groupsToolStripMenuItem";
            this.groupsToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.groupsToolStripMenuItem.Text = "&Groups";
            // 
            // addToolStripMenuItem1
            // 
            this.addToolStripMenuItem1.Name = "addToolStripMenuItem1";
            this.addToolStripMenuItem1.Size = new System.Drawing.Size(107, 22);
            this.addToolStripMenuItem1.Text = "&Add";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.deleteToolStripMenuItem.Text = "&Delete";
            // 
            // enumerationToolStripMenuItem
            // 
            this.enumerationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.deleteToolStripMenuItem1});
            this.enumerationToolStripMenuItem.Name = "enumerationToolStripMenuItem";
            this.enumerationToolStripMenuItem.Size = new System.Drawing.Size(87, 20);
            this.enumerationToolStripMenuItem.Text = "&Enumeration";
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.addToolStripMenuItem.Text = "&Add";
            // 
            // deleteToolStripMenuItem1
            // 
            this.deleteToolStripMenuItem1.Name = "deleteToolStripMenuItem1";
            this.deleteToolStripMenuItem1.Size = new System.Drawing.Size(107, 22);
            this.deleteToolStripMenuItem1.Text = "&Delete";
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 24);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerGroups);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.splitContainerBottom);
            this.splitContainerMain.Size = new System.Drawing.Size(1052, 583);
            this.splitContainerMain.SplitterDistance = 290;
            this.splitContainerMain.TabIndex = 0;
            // 
            // splitContainerGroups
            // 
            this.splitContainerGroups.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerGroups.Location = new System.Drawing.Point(0, 0);
            this.splitContainerGroups.Name = "splitContainerGroups";
            // 
            // splitContainerGroups.Panel1
            // 
            this.splitContainerGroups.Panel1.Controls.Add(this.tableLayoutPanel2);
            // 
            // splitContainerGroups.Panel2
            // 
            this.splitContainerGroups.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainerGroups.Size = new System.Drawing.Size(1052, 290);
            this.splitContainerGroups.SplitterDistance = 195;
            this.splitContainerGroups.TabIndex = 2;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.listViewSpyStudioGroups, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(195, 290);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Left;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Groups:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listViewSpyStudioGroups
            // 
            this.listViewSpyStudioGroups.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderMonGroup,
            this.columnHeaderMonFncCount});
            this.listViewSpyStudioGroups.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewSpyStudioGroups.Location = new System.Drawing.Point(0, 20);
            this.listViewSpyStudioGroups.Margin = new System.Windows.Forms.Padding(0);
            this.listViewSpyStudioGroups.Name = "listViewSpyStudioGroups";
            this.listViewSpyStudioGroups.Size = new System.Drawing.Size(195, 270);
            this.listViewSpyStudioGroups.TabIndex = 0;
            this.listViewSpyStudioGroups.UseCompatibleStateImageBehavior = false;
            this.listViewSpyStudioGroups.View = System.Windows.Forms.View.Details;
            this.listViewSpyStudioGroups.SelectedIndexChanged += new System.EventHandler(this.SpyStudioGroupSelectionChanged);
            // 
            // columnHeaderMonGroup
            // 
            this.columnHeaderMonGroup.Text = "Group";
            this.columnHeaderMonGroup.Width = 109;
            // 
            // columnHeaderMonFncCount
            // 
            this.columnHeaderMonFncCount.Text = "Count";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tableLayoutPanelProperties);
            this.splitContainer1.Size = new System.Drawing.Size(853, 290);
            this.splitContainer1.SplitterDistance = 254;
            this.splitContainer1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.listViewSpyStudioFunctions, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(254, 290);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // listViewSpyStudioFunctions
            // 
            this.listViewSpyStudioFunctions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewSpyStudioFunctions.Location = new System.Drawing.Point(0, 20);
            this.listViewSpyStudioFunctions.Margin = new System.Windows.Forms.Padding(0);
            this.listViewSpyStudioFunctions.Name = "listViewSpyStudioFunctions";
            this.listViewSpyStudioFunctions.Size = new System.Drawing.Size(254, 270);
            this.listViewSpyStudioFunctions.TabIndex = 1;
            this.listViewSpyStudioFunctions.UseCompatibleStateImageBehavior = false;
            this.listViewSpyStudioFunctions.View = System.Windows.Forms.View.List;
            this.listViewSpyStudioFunctions.SelectedIndexChanged += new System.EventHandler(this.SpyStudioFunctionSelectionChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Left;
            this.label5.Location = new System.Drawing.Point(3, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 20);
            this.label5.TabIndex = 2;
            this.label5.Text = "Functions";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanelProperties
            // 
            this.tableLayoutPanelProperties.ColumnCount = 1;
            this.tableLayoutPanelProperties.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelProperties.Controls.Add(this.propertyGridFunction, 0, 1);
            this.tableLayoutPanelProperties.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanelProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelProperties.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelProperties.Name = "tableLayoutPanelProperties";
            this.tableLayoutPanelProperties.RowCount = 2;
            this.tableLayoutPanelProperties.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelProperties.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelProperties.Size = new System.Drawing.Size(595, 290);
            this.tableLayoutPanelProperties.TabIndex = 0;
            // 
            // propertyGridFunction
            // 
            this.propertyGridFunction.CommandsForeColor = System.Drawing.SystemColors.ControlText;
            this.propertyGridFunction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridFunction.Location = new System.Drawing.Point(0, 20);
            this.propertyGridFunction.Margin = new System.Windows.Forms.Padding(0);
            this.propertyGridFunction.Name = "propertyGridFunction";
            this.propertyGridFunction.Size = new System.Drawing.Size(595, 270);
            this.propertyGridFunction.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Left;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 20);
            this.label4.TabIndex = 0;
            this.label4.Text = "Properties";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitContainerBottom
            // 
            this.splitContainerBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerBottom.Location = new System.Drawing.Point(0, 0);
            this.splitContainerBottom.Name = "splitContainerBottom";
            this.splitContainerBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerBottom.Panel1
            // 
            this.splitContainerBottom.Panel1.Controls.Add(this.tableLayoutPanelEnums);
            // 
            // splitContainerBottom.Panel2
            // 
            this.splitContainerBottom.Panel2.Controls.Add(this.tableLayoutPanelModules);
            this.splitContainerBottom.Size = new System.Drawing.Size(1052, 289);
            this.splitContainerBottom.SplitterDistance = 143;
            this.splitContainerBottom.TabIndex = 1;
            // 
            // tableLayoutPanelEnums
            // 
            this.tableLayoutPanelEnums.ColumnCount = 1;
            this.tableLayoutPanelEnums.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelEnums.Controls.Add(this.splitContainerEnums, 0, 1);
            this.tableLayoutPanelEnums.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanelEnums.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelEnums.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelEnums.Name = "tableLayoutPanelEnums";
            this.tableLayoutPanelEnums.RowCount = 2;
            this.tableLayoutPanelEnums.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelEnums.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelEnums.Size = new System.Drawing.Size(1052, 143);
            this.tableLayoutPanelEnums.TabIndex = 2;
            // 
            // splitContainerEnums
            // 
            this.splitContainerEnums.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerEnums.Location = new System.Drawing.Point(3, 23);
            this.splitContainerEnums.Name = "splitContainerEnums";
            // 
            // splitContainerEnums.Panel1
            // 
            this.splitContainerEnums.Panel1.Controls.Add(this.listViewEnums);
            // 
            // splitContainerEnums.Panel2
            // 
            this.splitContainerEnums.Panel2.Controls.Add(this.tableLayoutPanelEnumItems);
            this.splitContainerEnums.Size = new System.Drawing.Size(1046, 117);
            this.splitContainerEnums.SplitterDistance = 193;
            this.splitContainerEnums.TabIndex = 1;
            // 
            // listViewEnums
            // 
            this.listViewEnums.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderEnum,
            this.columnHeaderCount});
            this.listViewEnums.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewEnums.Location = new System.Drawing.Point(0, 0);
            this.listViewEnums.Name = "listViewEnums";
            this.listViewEnums.Size = new System.Drawing.Size(193, 117);
            this.listViewEnums.TabIndex = 0;
            this.listViewEnums.UseCompatibleStateImageBehavior = false;
            this.listViewEnums.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderEnum
            // 
            this.columnHeaderEnum.Text = "Enumeration";
            this.columnHeaderEnum.Width = 112;
            // 
            // columnHeaderCount
            // 
            this.columnHeaderCount.Tag = "Numeric";
            this.columnHeaderCount.Text = "Count";
            // 
            // tableLayoutPanelEnumItems
            // 
            this.tableLayoutPanelEnumItems.ColumnCount = 1;
            this.tableLayoutPanelEnumItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelEnumItems.Controls.Add(this.listViewEnumItems, 0, 0);
            this.tableLayoutPanelEnumItems.Controls.Add(this.checkBoxEnumFlags, 0, 1);
            this.tableLayoutPanelEnumItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelEnumItems.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelEnumItems.Name = "tableLayoutPanelEnumItems";
            this.tableLayoutPanelEnumItems.RowCount = 2;
            this.tableLayoutPanelEnumItems.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelEnumItems.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanelEnumItems.Size = new System.Drawing.Size(849, 117);
            this.tableLayoutPanelEnumItems.TabIndex = 1;
            // 
            // listViewEnumItems
            // 
            this.listViewEnumItems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderItem,
            this.columnHeaderValue});
            this.listViewEnumItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewEnumItems.Location = new System.Drawing.Point(0, 0);
            this.listViewEnumItems.Margin = new System.Windows.Forms.Padding(0);
            this.listViewEnumItems.Name = "listViewEnumItems";
            this.listViewEnumItems.Size = new System.Drawing.Size(849, 93);
            this.listViewEnumItems.TabIndex = 0;
            this.listViewEnumItems.UseCompatibleStateImageBehavior = false;
            this.listViewEnumItems.View = System.Windows.Forms.View.List;
            // 
            // columnHeaderItem
            // 
            this.columnHeaderItem.Text = "Item";
            // 
            // columnHeaderValue
            // 
            this.columnHeaderValue.Text = "Value";
            // 
            // checkBoxEnumFlags
            // 
            this.checkBoxEnumFlags.AutoSize = true;
            this.checkBoxEnumFlags.Location = new System.Drawing.Point(3, 96);
            this.checkBoxEnumFlags.Name = "checkBoxEnumFlags";
            this.checkBoxEnumFlags.Size = new System.Drawing.Size(57, 17);
            this.checkBoxEnumFlags.TabIndex = 1;
            this.checkBoxEnumFlags.Text = "Is Flag";
            this.checkBoxEnumFlags.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Left;
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(74, 20);
            this.label3.TabIndex = 2;
            this.label3.Text = "Enumerations:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanelModules
            // 
            this.tableLayoutPanelModules.ColumnCount = 1;
            this.tableLayoutPanelModules.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelModules.Controls.Add(this.splitContainerModules, 0, 1);
            this.tableLayoutPanelModules.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanelModules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelModules.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelModules.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanelModules.Name = "tableLayoutPanelModules";
            this.tableLayoutPanelModules.RowCount = 1;
            this.tableLayoutPanelModules.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelModules.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelModules.Size = new System.Drawing.Size(1052, 142);
            this.tableLayoutPanelModules.TabIndex = 0;
            // 
            // splitContainerModules
            // 
            this.splitContainerModules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerModules.Location = new System.Drawing.Point(3, 23);
            this.splitContainerModules.Name = "splitContainerModules";
            // 
            // splitContainerModules.Panel1
            // 
            this.splitContainerModules.Panel1.Controls.Add(this.listViewDeviareModules);
            // 
            // splitContainerModules.Panel2
            // 
            this.splitContainerModules.Panel2.Controls.Add(this.listViewDeviareModuleFunctions);
            this.splitContainerModules.Size = new System.Drawing.Size(1046, 116);
            this.splitContainerModules.SplitterDistance = 194;
            this.splitContainerModules.TabIndex = 3;
            // 
            // listViewDeviareModules
            // 
            this.listViewDeviareModules.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderGroup,
            this.columnHeaderFncCount});
            this.listViewDeviareModules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewDeviareModules.Location = new System.Drawing.Point(0, 0);
            this.listViewDeviareModules.Margin = new System.Windows.Forms.Padding(0);
            this.listViewDeviareModules.Name = "listViewDeviareModules";
            this.listViewDeviareModules.Size = new System.Drawing.Size(194, 116);
            this.listViewDeviareModules.TabIndex = 0;
            this.listViewDeviareModules.UseCompatibleStateImageBehavior = false;
            this.listViewDeviareModules.View = System.Windows.Forms.View.Details;
            this.listViewDeviareModules.SelectedIndexChanged += new System.EventHandler(this.DeviareModulesSelectionChanged);
            // 
            // columnHeaderGroup
            // 
            this.columnHeaderGroup.Text = "Group";
            this.columnHeaderGroup.Width = 115;
            // 
            // columnHeaderFncCount
            // 
            this.columnHeaderFncCount.Text = "Count";
            // 
            // listViewDeviareModuleFunctions
            // 
            this.listViewDeviareModuleFunctions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewDeviareModuleFunctions.Location = new System.Drawing.Point(0, 0);
            this.listViewDeviareModuleFunctions.Margin = new System.Windows.Forms.Padding(0);
            this.listViewDeviareModuleFunctions.Name = "listViewDeviareModuleFunctions";
            this.listViewDeviareModuleFunctions.Size = new System.Drawing.Size(848, 116);
            this.listViewDeviareModuleFunctions.TabIndex = 1;
            this.listViewDeviareModuleFunctions.UseCompatibleStateImageBehavior = false;
            this.listViewDeviareModuleFunctions.View = System.Windows.Forms.View.List;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Left;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Modules:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FormMonitorGroupManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1052, 607);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.menuStripMain);
            this.MainMenuStrip = this.menuStripMain;
            this.Name = "FormMonitorGroupManager";
            this.ShowIcon = false;
            this.Text = "SpyStudio Hook Manager";
            this.Load += new System.EventHandler(this.MonitorGroupManagerDialogLoad);
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.ResumeLayout(false);
            this.splitContainerGroups.Panel1.ResumeLayout(false);
            this.splitContainerGroups.Panel2.ResumeLayout(false);
            this.splitContainerGroups.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanelProperties.ResumeLayout(false);
            this.tableLayoutPanelProperties.PerformLayout();
            this.splitContainerBottom.Panel1.ResumeLayout(false);
            this.splitContainerBottom.Panel2.ResumeLayout(false);
            this.splitContainerBottom.ResumeLayout(false);
            this.tableLayoutPanelEnums.ResumeLayout(false);
            this.tableLayoutPanelEnums.PerformLayout();
            this.splitContainerEnums.Panel1.ResumeLayout(false);
            this.splitContainerEnums.Panel2.ResumeLayout(false);
            this.splitContainerEnums.ResumeLayout(false);
            this.tableLayoutPanelEnumItems.ResumeLayout(false);
            this.tableLayoutPanelEnumItems.PerformLayout();
            this.tableLayoutPanelModules.ResumeLayout(false);
            this.tableLayoutPanelModules.PerformLayout();
            this.splitContainerModules.Panel1.ResumeLayout(false);
            this.splitContainerModules.Panel2.ResumeLayout(false);
            this.splitContainerModules.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem groupsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.ListView listViewSpyStudioGroups;
        private System.Windows.Forms.ListView listViewDeviareModules;
        private System.Windows.Forms.ListView listViewSpyStudioFunctions;
        private System.Windows.Forms.ListView listViewDeviareModuleFunctions;
        private System.Windows.Forms.SplitContainer splitContainerGroups;
        private System.Windows.Forms.SplitContainer splitContainerModules;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelModules;
        private System.Windows.Forms.SplitContainer splitContainerBottom;
        private System.Windows.Forms.SplitContainer splitContainerEnums;
        private System.Windows.Forms.ListView listViewEnums;
        private System.Windows.Forms.ListView listViewEnumItems;
        private System.Windows.Forms.ColumnHeader columnHeaderEnum;
        private System.Windows.Forms.ColumnHeader columnHeaderCount;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelEnumItems;
        private System.Windows.Forms.ColumnHeader columnHeaderItem;
        private System.Windows.Forms.ColumnHeader columnHeaderValue;
        private System.Windows.Forms.CheckBox checkBoxEnumFlags;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelEnums;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem enumerationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem1;
        private System.Windows.Forms.ColumnHeader columnHeaderMonGroup;
        private System.Windows.Forms.ColumnHeader columnHeaderGroup;
        private System.Windows.Forms.ColumnHeader columnHeaderFncCount;
        private System.Windows.Forms.ColumnHeader columnHeaderMonFncCount;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private FunctionPropertyGrid.FunctionPropertyGrid propertyGridFunction;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelProperties;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
    }
}
