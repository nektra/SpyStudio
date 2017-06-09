using System.Windows.Forms;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs
{
    partial class StatsReport
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.labelFile2 = new System.Windows.Forms.Label();
            this.labelFile1 = new System.Windows.Forms.Label();
            this.labelFile2Color = new System.Windows.Forms.Label();
            this.labelFile1Color = new System.Windows.Forms.Label();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.tabControlStats = new System.Windows.Forms.TabControl();
            this.tabPageFunction = new System.Windows.Forms.TabPage();
            this.listViewFunction = new ListViewSorted();
            this.columnHeaderFunction = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderCount = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderTime = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderAverage = new System.Windows.Forms.ColumnHeader();
            this.tabPageModule = new System.Windows.Forms.TabPage();
            this.listViewProcessesData = new DataInfo();
            this.columnLVDataName = new System.Windows.Forms.ColumnHeader();
            this.columnLVDataCOM = new System.Windows.Forms.ColumnHeader();
            this.columnLVDataRegistry = new System.Windows.Forms.ColumnHeader();
            this.columnLVDataFiles = new System.Windows.Forms.ColumnHeader();
            this.columnLVDataWnd = new System.Windows.Forms.ColumnHeader();
            this.columnLVOther = new System.Windows.Forms.ColumnHeader();
            this.columnLVTotal = new System.Windows.Forms.ColumnHeader();
            this.columnLVDataTime = new System.Windows.Forms.ColumnHeader();
            this.tabPageWindow = new System.Windows.Forms.TabPage();
            this.listViewWindow = new ListViewSorted();
            this.columnHeaderClass = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderWndTime = new System.Windows.Forms.ColumnHeader();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tabControlStats.SuspendLayout();
            this.tabPageFunction.SuspendLayout();
            this.tabPageModule.SuspendLayout();
            this.tabPageWindow.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 83F));
            this.tableLayoutPanel2.Controls.Add(this.buttonClose, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 483);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(680, 28);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // buttonClose
            // 
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonClose.Location = new System.Drawing.Point(597, 0);
            this.buttonClose.Margin = new System.Windows.Forms.Padding(0);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(83, 28);
            this.buttonClose.TabIndex = 0;
            this.buttonClose.Text = "&Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.ButtonCloseClick);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 4;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.labelFile2, 3, 0);
            this.tableLayoutPanel3.Controls.Add(this.labelFile1, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.labelFile2Color, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.labelFile1Color, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(686, 20);
            this.tableLayoutPanel3.TabIndex = 9;
            // 
            // labelFile2
            // 
            this.labelFile2.AutoSize = true;
            this.labelFile2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFile2.Location = new System.Drawing.Point(366, 0);
            this.labelFile2.Name = "labelFile2";
            this.labelFile2.Size = new System.Drawing.Size(317, 20);
            this.labelFile2.TabIndex = 0;
            this.labelFile2.Text = "label2";
            this.labelFile2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelFile1
            // 
            this.labelFile1.AutoSize = true;
            this.labelFile1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFile1.Location = new System.Drawing.Point(23, 0);
            this.labelFile1.Name = "labelFile1";
            this.labelFile1.Size = new System.Drawing.Size(317, 20);
            this.labelFile1.TabIndex = 1;
            this.labelFile1.Text = "label4";
            this.labelFile1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelFile2Color
            // 
            this.labelFile2Color.AutoSize = true;
            this.labelFile2Color.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.labelFile2Color.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFile2Color.Location = new System.Drawing.Point(347, 4);
            this.labelFile2Color.Margin = new System.Windows.Forms.Padding(4);
            this.labelFile2Color.Name = "labelFile2Color";
            this.labelFile2Color.Size = new System.Drawing.Size(12, 12);
            this.labelFile2Color.TabIndex = 2;
            // 
            // labelFile1Color
            // 
            this.labelFile1Color.AutoSize = true;
            this.labelFile1Color.BackColor = System.Drawing.SystemColors.ControlDark;
            this.labelFile1Color.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFile1Color.Location = new System.Drawing.Point(4, 4);
            this.labelFile1Color.Margin = new System.Windows.Forms.Padding(4);
            this.labelFile1Color.Name = "labelFile1Color";
            this.labelFile1Color.Size = new System.Drawing.Size(12, 12);
            this.labelFile1Color.TabIndex = 3;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.tabControlStats, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(686, 514);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // tabControlStats
            // 
            this.tabControlStats.Controls.Add(this.tabPageFunction);
            this.tabControlStats.Controls.Add(this.tabPageModule);
            this.tabControlStats.Controls.Add(this.tabPageWindow);
            this.tabControlStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlStats.Location = new System.Drawing.Point(3, 23);
            this.tabControlStats.Name = "tabControlStats";
            this.tabControlStats.SelectedIndex = 0;
            this.tabControlStats.Size = new System.Drawing.Size(680, 454);
            this.tabControlStats.TabIndex = 10;
            // 
            // tabPageFunction
            // 
            this.tabPageFunction.Controls.Add(this.listViewFunction);
            this.tabPageFunction.Location = new System.Drawing.Point(4, 22);
            this.tabPageFunction.Name = "tabPageFunction";
            this.tabPageFunction.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageFunction.Size = new System.Drawing.Size(672, 428);
            this.tabPageFunction.TabIndex = 0;
            this.tabPageFunction.Text = "By Function";
            this.tabPageFunction.UseVisualStyleBackColor = true;
            // 
            // listViewFunction
            // 
            this.listViewFunction.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderFunction,
            this.columnHeaderCount,
            this.columnHeaderTime,
            this.columnHeaderAverage});
            this.listViewFunction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewFunction.FullRowSelect = true;
            this.listViewFunction.Location = new System.Drawing.Point(3, 3);
            this.listViewFunction.Name = "listViewFunction";
            this.listViewFunction.Size = new System.Drawing.Size(666, 422);
            this.listViewFunction.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewFunction.TabIndex = 1;
            this.listViewFunction.UseCompatibleStateImageBehavior = false;
            this.listViewFunction.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderFunction
            // 
            this.columnHeaderFunction.Text = "Function";
            this.columnHeaderFunction.Width = 218;
            // 
            // columnHeaderCount
            // 
            this.columnHeaderCount.Tag = "Numeric";
            this.columnHeaderCount.Text = "Count";
            this.columnHeaderCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeaderCount.Width = 78;
            // 
            // columnHeaderTime
            // 
            this.columnHeaderTime.Tag = "Double";
            this.columnHeaderTime.Text = "Time";
            this.columnHeaderTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeaderTime.Width = 75;
            // 
            // columnHeaderAverage
            // 
            this.columnHeaderAverage.Tag = "Double";
            this.columnHeaderAverage.Text = "Average (Time / Count)";
            this.columnHeaderAverage.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeaderAverage.Width = 135;
            // 
            // tabPageModule
            // 
            this.tabPageModule.Controls.Add(this.listViewProcessesData);
            this.tabPageModule.Location = new System.Drawing.Point(4, 22);
            this.tabPageModule.Name = "tabPageModule";
            this.tabPageModule.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageModule.Size = new System.Drawing.Size(672, 428);
            this.tabPageModule.TabIndex = 1;
            this.tabPageModule.Text = "By Module";
            this.tabPageModule.UseVisualStyleBackColor = true;
            // 
            // listViewProcessesData
            // 
            this.listViewProcessesData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listViewProcessesData.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnLVDataName,
            this.columnLVDataCOM,
            this.columnLVDataRegistry,
            this.columnLVDataFiles,
            this.columnLVDataWnd,
            this.columnLVOther,
            this.columnLVTotal,
            this.columnLVDataTime});
            this.listViewProcessesData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewProcessesData.FullRowSelect = true;
            this.listViewProcessesData.Location = new System.Drawing.Point(3, 3);
            this.listViewProcessesData.Name = "listViewProcessesData";
            this.listViewProcessesData.ShowItemToolTips = true;
            this.listViewProcessesData.Size = new System.Drawing.Size(666, 422);
            this.listViewProcessesData.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewProcessesData.TabIndex = 7;
            this.listViewProcessesData.UseCompatibleStateImageBehavior = false;
            this.listViewProcessesData.View = System.Windows.Forms.View.Details;
            // 
            // columnLVDataName
            // 
            this.columnLVDataName.Tag = "";
            this.columnLVDataName.Text = "Name";
            this.columnLVDataName.Width = 115;
            // 
            // columnLVDataCOM
            // 
            this.columnLVDataCOM.Tag = "Numeric";
            this.columnLVDataCOM.Text = "Com Instanced";
            this.columnLVDataCOM.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnLVDataCOM.Width = 102;
            // 
            // columnLVDataRegistry
            // 
            this.columnLVDataRegistry.Tag = "Numeric";
            this.columnLVDataRegistry.Text = "Registry Entries";
            this.columnLVDataRegistry.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnLVDataRegistry.Width = 87;
            // 
            // columnLVDataFiles
            // 
            this.columnLVDataFiles.Tag = "Numeric";
            this.columnLVDataFiles.Text = "Files";
            this.columnLVDataFiles.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // columnLVDataWnd
            // 
            this.columnLVDataWnd.Tag = "Numeric";
            this.columnLVDataWnd.Text = "Windows";
            this.columnLVDataWnd.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnLVDataWnd.Width = 82;
            // 
            // columnLVOther
            // 
            this.columnLVOther.Tag = "Numeric";
            this.columnLVOther.Text = "Other";
            this.columnLVOther.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // columnLVTotal
            // 
            this.columnLVTotal.Tag = "Numeric";
            this.columnLVTotal.Text = "Total";
            this.columnLVTotal.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnLVTotal.Width = 70;
            // 
            // columnLVDataTime
            // 
            this.columnLVDataTime.Tag = "Double";
            this.columnLVDataTime.Text = "Time";
            this.columnLVDataTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnLVDataTime.Width = 114;
            // 
            // tabPageWindow
            // 
            this.tabPageWindow.Controls.Add(this.listViewWindow);
            this.tabPageWindow.Location = new System.Drawing.Point(4, 22);
            this.tabPageWindow.Name = "tabPageWindow";
            this.tabPageWindow.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageWindow.Size = new System.Drawing.Size(672, 428);
            this.tabPageWindow.TabIndex = 2;
            this.tabPageWindow.Text = "By Window";
            this.tabPageWindow.UseVisualStyleBackColor = true;
            // 
            // listViewWindow
            // 
            this.listViewWindow.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderClass,
            this.columnHeaderName,
            this.columnHeaderWndTime});
            this.listViewWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewWindow.FullRowSelect = true;
            this.listViewWindow.Location = new System.Drawing.Point(3, 3);
            this.listViewWindow.Name = "listViewWindow";
            this.listViewWindow.Size = new System.Drawing.Size(666, 422);
            this.listViewWindow.TabIndex = 0;
            this.listViewWindow.UseCompatibleStateImageBehavior = false;
            this.listViewWindow.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderClass
            // 
            this.columnHeaderClass.Text = "Class Name";
            this.columnHeaderClass.Width = 225;
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "Name";
            this.columnHeaderName.Width = 177;
            // 
            // columnHeaderWndTime
            // 
            this.columnHeaderWndTime.Tag = "Double";
            this.columnHeaderWndTime.Text = "Time from Start";
            this.columnHeaderWndTime.Width = 165;
            // 
            // StatsReport
            // 
            this.AcceptButton = this.buttonClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonClose;
            this.ClientSize = new System.Drawing.Size(686, 514);
            this.Controls.Add(this.tableLayoutPanel4);
            this.Name = "StatsReport";
            this.ShowIcon = false;
            this.Text = "Statistics";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.StatsReportKeyDown);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tabControlStats.ResumeLayout(false);
            this.tabPageFunction.ResumeLayout(false);
            this.tabPageModule.ResumeLayout(false);
            this.tabPageWindow.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button buttonClose;
        private ListViewSorted listViewFunction;
        private System.Windows.Forms.ColumnHeader columnHeaderFunction;
        private System.Windows.Forms.ColumnHeader columnHeaderCount;
        private System.Windows.Forms.ColumnHeader columnHeaderTime;
        private System.Windows.Forms.ColumnHeader columnHeaderAverage;
        private DataInfo listViewProcessesData;
        private System.Windows.Forms.ColumnHeader columnLVDataName;
        private System.Windows.Forms.ColumnHeader columnLVDataCOM;
        private System.Windows.Forms.ColumnHeader columnLVDataRegistry;
        private System.Windows.Forms.ColumnHeader columnLVDataFiles;
        private System.Windows.Forms.ColumnHeader columnLVDataWnd;
        private System.Windows.Forms.ColumnHeader columnLVOther;
        private System.Windows.Forms.ColumnHeader columnLVTotal;
        private System.Windows.Forms.ColumnHeader columnLVDataTime;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label labelFile2;
        private System.Windows.Forms.Label labelFile1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TabControl tabControlStats;
        private System.Windows.Forms.TabPage tabPageFunction;
        private System.Windows.Forms.TabPage tabPageModule;
        private System.Windows.Forms.TabPage tabPageWindow;
        private ListViewSorted listViewWindow;
        private System.Windows.Forms.ColumnHeader columnHeaderClass;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderWndTime;
        private Label labelFile2Color;
        private Label labelFile1Color;
    }
}