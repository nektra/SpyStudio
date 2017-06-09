using SpyStudio.Tools;

namespace SpyStudio.Dialogs
{
    partial class CallDetails
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>ca
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
            this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageEvent = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.listViewEvent = new ListViewSorted();
            this.columnHeaderProperty = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderValue = new System.Windows.Forms.ColumnHeader();
            this.label1 = new System.Windows.Forms.Label();
            this.listViewParams = new ListViewSorted();
            this.columnParam = new System.Windows.Forms.ColumnHeader();
            this.columnValue = new System.Windows.Forms.ColumnHeader();
            this.tabPageStack = new System.Windows.Forms.TabPage();
            this.listViewStack = new ListViewSorted();
            this.columnHeaderNumber = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderModule = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderFrame = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderAddress = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPath = new System.Windows.Forms.ColumnHeader();
            this.tableLayoutPanelBottom = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonDown = new System.Windows.Forms.Button();
            this.buttonUp = new System.Windows.Forms.Button();
            this.tableLayoutMain.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageEvent.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tabPageStack.SuspendLayout();
            this.tableLayoutPanelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutMain
            // 
            this.tableLayoutMain.ColumnCount = 1;
            this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.Controls.Add(this.tabControl1, 0, 0);
            this.tableLayoutMain.Controls.Add(this.tableLayoutPanelBottom, 0, 1);
            this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutMain.Name = "tableLayoutMain";
            this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutMain.RowCount = 2;
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tableLayoutMain.Size = new System.Drawing.Size(518, 403);
            this.tableLayoutMain.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageEvent);
            this.tabControl1.Controls.Add(this.tabPageStack);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(6, 6);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(506, 357);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageEvent
            // 
            this.tabPageEvent.Controls.Add(this.tableLayoutPanel1);
            this.tabPageEvent.Location = new System.Drawing.Point(4, 22);
            this.tabPageEvent.Name = "tabPageEvent";
            this.tabPageEvent.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEvent.Size = new System.Drawing.Size(498, 331);
            this.tabPageEvent.TabIndex = 0;
            this.tabPageEvent.Text = "Event";
            this.tabPageEvent.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.listViewEvent, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.listViewParams, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(492, 325);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // listViewEvent
            // 
            this.listViewEvent.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderProperty,
            this.columnHeaderValue});
            this.listViewEvent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewEvent.FullRowSelect = true;
            this.listViewEvent.Location = new System.Drawing.Point(0, 0);
            this.listViewEvent.Margin = new System.Windows.Forms.Padding(0);
            this.listViewEvent.Name = "listViewEvent";
            this.listViewEvent.Size = new System.Drawing.Size(492, 100);
            this.listViewEvent.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewEvent.TabIndex = 0;
            this.listViewEvent.UseCompatibleStateImageBehavior = false;
            this.listViewEvent.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderProperty
            // 
            this.columnHeaderProperty.Text = "Property";
            this.columnHeaderProperty.Width = 145;
            // 
            // columnHeaderValue
            // 
            this.columnHeaderValue.Text = "Value";
            this.columnHeaderValue.Width = 255;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label1.Location = new System.Drawing.Point(3, 107);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(486, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Params:";
            // 
            // listViewParams
            // 
            this.listViewParams.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listViewParams.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnParam,
            this.columnValue});
            this.listViewParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewParams.FullRowSelect = true;
            this.listViewParams.HideSelection = false;
            this.listViewParams.Location = new System.Drawing.Point(3, 123);
            this.listViewParams.Name = "listViewParams";
            this.listViewParams.Size = new System.Drawing.Size(486, 199);
            this.listViewParams.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewParams.TabIndex = 2;
            this.listViewParams.UseCompatibleStateImageBehavior = false;
            this.listViewParams.View = System.Windows.Forms.View.Details;
            // 
            // columnParam
            // 
            this.columnParam.Text = "Params";
            this.columnParam.Width = 108;
            // 
            // columnValue
            // 
            this.columnValue.Text = "Value";
            this.columnValue.Width = 349;
            // 
            // tabPageStack
            // 
            this.tabPageStack.Controls.Add(this.listViewStack);
            this.tabPageStack.Location = new System.Drawing.Point(4, 22);
            this.tabPageStack.Name = "tabPageStack";
            this.tabPageStack.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageStack.Size = new System.Drawing.Size(498, 331);
            this.tabPageStack.TabIndex = 1;
            this.tabPageStack.Text = "Stack";
            this.tabPageStack.UseVisualStyleBackColor = true;
            // 
            // listViewStack
            // 
            this.listViewStack.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderNumber,
            this.columnHeaderModule,
            this.columnHeaderFrame,
            this.columnHeaderAddress,
            this.columnHeaderPath});
            this.listViewStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewStack.FullRowSelect = true;
            this.listViewStack.Location = new System.Drawing.Point(3, 3);
            this.listViewStack.Margin = new System.Windows.Forms.Padding(0);
            this.listViewStack.Name = "listViewStack";
            this.listViewStack.Size = new System.Drawing.Size(492, 325);
            this.listViewStack.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.listViewStack.TabIndex = 0;
            this.listViewStack.UseCompatibleStateImageBehavior = false;
            this.listViewStack.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderNumber
            // 
            this.columnHeaderNumber.Tag = "Numeric";
            this.columnHeaderNumber.Text = "#";
            this.columnHeaderNumber.Width = 30;
            // 
            // columnHeaderModule
            // 
            this.columnHeaderModule.Text = "Module";
            this.columnHeaderModule.Width = 67;
            // 
            // columnHeaderFrame
            // 
            this.columnHeaderFrame.Text = "Frame";
            this.columnHeaderFrame.Width = 90;
            // 
            // columnHeaderAddress
            // 
            this.columnHeaderAddress.Text = "Address";
            this.columnHeaderAddress.Width = 78;
            // 
            // columnHeaderPath
            // 
            this.columnHeaderPath.Text = "Path";
            this.columnHeaderPath.Width = 204;
            // 
            // tableLayoutPanelBottom
            // 
            this.tableLayoutPanelBottom.ColumnCount = 4;
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 83F));
            this.tableLayoutPanelBottom.Controls.Add(this.buttonClose, 3, 0);
            this.tableLayoutPanelBottom.Controls.Add(this.buttonDown, 1, 0);
            this.tableLayoutPanelBottom.Controls.Add(this.buttonUp, 0, 0);
            this.tableLayoutPanelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelBottom.Location = new System.Drawing.Point(6, 369);
            this.tableLayoutPanelBottom.Name = "tableLayoutPanelBottom";
            this.tableLayoutPanelBottom.RowCount = 1;
            this.tableLayoutPanelBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBottom.Size = new System.Drawing.Size(506, 28);
            this.tableLayoutPanelBottom.TabIndex = 1;
            // 
            // buttonClose
            // 
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonClose.Location = new System.Drawing.Point(423, 0);
            this.buttonClose.Margin = new System.Windows.Forms.Padding(0);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(83, 28);
            this.buttonClose.TabIndex = 0;
            this.buttonClose.Text = "&Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.ButtonCloseClick);
            // 
            // buttonDown
            // 
            this.buttonDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonDown.Image = global::SpyStudio.Properties.Resources.arrow_down;
            this.buttonDown.Location = new System.Drawing.Point(30, 0);
            this.buttonDown.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.buttonDown.Name = "buttonDown";
            this.buttonDown.Size = new System.Drawing.Size(28, 28);
            this.buttonDown.TabIndex = 2;
            this.buttonDown.UseVisualStyleBackColor = true;
            this.buttonDown.Click += new System.EventHandler(this.ButtonDownClick);
            // 
            // buttonUp
            // 
            this.buttonUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonUp.Image = global::SpyStudio.Properties.Resources.arrow_up;
            this.buttonUp.Location = new System.Drawing.Point(2, 0);
            this.buttonUp.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.buttonUp.Name = "buttonUp";
            this.buttonUp.Size = new System.Drawing.Size(28, 28);
            this.buttonUp.TabIndex = 1;
            this.buttonUp.UseVisualStyleBackColor = true;
            this.buttonUp.Click += new System.EventHandler(this.ButtonUpClick);
            // 
            // CallDetails
            // 
            this.AcceptButton = this.buttonClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonClose;
            this.ClientSize = new System.Drawing.Size(518, 403);
            this.Controls.Add(this.tableLayoutMain);
            this.Name = "CallDetails";
            this.ShowIcon = false;
            this.Text = "Call Properties";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CallDetailsKeyDown);
            this.tableLayoutMain.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPageEvent.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tabPageStack.ResumeLayout(false);
            this.tableLayoutPanelBottom.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageEvent;
        private System.Windows.Forms.TabPage tabPageStack;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private ListViewSorted listViewEvent;
        private System.Windows.Forms.ColumnHeader columnHeaderProperty;
        private System.Windows.Forms.ColumnHeader columnHeaderValue;
        private ListViewSorted listViewStack;
        private System.Windows.Forms.ColumnHeader columnHeaderNumber;
        private System.Windows.Forms.ColumnHeader columnHeaderModule;
        private System.Windows.Forms.ColumnHeader columnHeaderFrame;
        private System.Windows.Forms.ColumnHeader columnHeaderAddress;
        private System.Windows.Forms.ColumnHeader columnHeaderPath;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelBottom;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Button buttonUp;
        private System.Windows.Forms.Button buttonDown;
        private System.Windows.Forms.Label label1;
        private ListViewSorted listViewParams;
        private System.Windows.Forms.ColumnHeader columnParam;
        private System.Windows.Forms.ColumnHeader columnValue;
    }
}