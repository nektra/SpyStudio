using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Properties
{
    partial class EntryPropertiesDialogBase
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.CallEventDetailsTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.CallEventDetailsTabControl = new System.Windows.Forms.TabControl();
            this.CallEventDetailsTabPage = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.CallEventStackTabPage = new System.Windows.Forms.TabPage();
            this._listViewStack = new SpyStudio.Tools.ListViewSorted();
            this.columnHeaderNumber = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderModule = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderFrame = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderAddress = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPath = new System.Windows.Forms.ColumnHeader();
            this.CallEventStackTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonDown = new System.Windows.Forms.Button();
            this.buttonUp = new System.Windows.Forms.Button();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.CallEventDetailsTableLayout.SuspendLayout();
            this.CallEventDetailsTabControl.SuspendLayout();
            this.CallEventDetailsTabPage.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.CallEventStackTabPage.SuspendLayout();
            this.CallEventStackTableLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.CallEventDetailsTableLayout);
            this.splitContainer1.Size = new System.Drawing.Size(517, 535);
            this.splitContainer1.SplitterDistance = 159;
            this.splitContainer1.TabIndex = 6;
            // 
            // CallEventDetailsTableLayout
            // 
            this.CallEventDetailsTableLayout.ColumnCount = 1;
            this.CallEventDetailsTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CallEventDetailsTableLayout.Controls.Add(this.CallEventDetailsTabControl, 0, 0);
            this.CallEventDetailsTableLayout.Controls.Add(this.CallEventStackTableLayout, 0, 1);
            this.CallEventDetailsTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CallEventDetailsTableLayout.Location = new System.Drawing.Point(0, 0);
            this.CallEventDetailsTableLayout.Name = "CallEventDetailsTableLayout";
            this.CallEventDetailsTableLayout.Padding = new System.Windows.Forms.Padding(3);
            this.CallEventDetailsTableLayout.RowCount = 2;
            this.CallEventDetailsTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CallEventDetailsTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.CallEventDetailsTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.CallEventDetailsTableLayout.Size = new System.Drawing.Size(515, 370);
            this.CallEventDetailsTableLayout.TabIndex = 5;
            // 
            // CallEventDetailsTabControl
            // 
            this.CallEventDetailsTabControl.Controls.Add(this.CallEventDetailsTabPage);
            this.CallEventDetailsTabControl.Controls.Add(this.CallEventStackTabPage);
            this.CallEventDetailsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CallEventDetailsTabControl.Location = new System.Drawing.Point(6, 6);
            this.CallEventDetailsTabControl.Name = "CallEventDetailsTabControl";
            this.CallEventDetailsTabControl.SelectedIndex = 0;
            this.CallEventDetailsTabControl.Size = new System.Drawing.Size(503, 324);
            this.CallEventDetailsTabControl.TabIndex = 0;
            // 
            // CallEventDetailsTabPage
            // 
            this.CallEventDetailsTabPage.Controls.Add(this.splitContainer2);
            this.CallEventDetailsTabPage.Location = new System.Drawing.Point(4, 22);
            this.CallEventDetailsTabPage.Name = "CallEventDetailsTabPage";
            this.CallEventDetailsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.CallEventDetailsTabPage.Size = new System.Drawing.Size(495, 298);
            this.CallEventDetailsTabPage.TabIndex = 0;
            this.CallEventDetailsTabPage.Text = "Event";
            this.CallEventDetailsTabPage.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer2.Size = new System.Drawing.Size(489, 292);
            this.splitContainer2.SplitterDistance = 140;
            this.splitContainer2.TabIndex = 0;
            // 
            // CallEventStackTabPage
            // 
            this.CallEventStackTabPage.Controls.Add(this._listViewStack);
            this.CallEventStackTabPage.Location = new System.Drawing.Point(4, 22);
            this.CallEventStackTabPage.Name = "CallEventStackTabPage";
            this.CallEventStackTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.CallEventStackTabPage.Size = new System.Drawing.Size(495, 298);
            this.CallEventStackTabPage.TabIndex = 1;
            this.CallEventStackTabPage.Text = "Stack";
            this.CallEventStackTabPage.UseVisualStyleBackColor = true;
            // 
            // _listViewStack
            // 
            this._listViewStack.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderNumber,
            this.columnHeaderModule,
            this.columnHeaderFrame,
            this.columnHeaderAddress,
            this.columnHeaderPath});
            this._listViewStack.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewStack.FullRowSelect = true;
            this._listViewStack.IgnoreCase = true;
            this._listViewStack.Location = new System.Drawing.Point(3, 3);
            this._listViewStack.Margin = new System.Windows.Forms.Padding(0);
            this._listViewStack.Name = "_listViewStack";
            this._listViewStack.Size = new System.Drawing.Size(489, 292);
            this._listViewStack.SortColumn = 0;
            this._listViewStack.Sorting = System.Windows.Forms.SortOrder.Descending;
            this._listViewStack.TabIndex = 0;
            this._listViewStack.UseCompatibleStateImageBehavior = false;
            this._listViewStack.View = System.Windows.Forms.View.Details;
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
            // CallEventStackTableLayout
            // 
            this.CallEventStackTableLayout.ColumnCount = 4;
            this.CallEventStackTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.CallEventStackTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.CallEventStackTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CallEventStackTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 83F));
            this.CallEventStackTableLayout.Controls.Add(this.buttonClose, 3, 0);
            this.CallEventStackTableLayout.Controls.Add(this.buttonDown, 1, 0);
            this.CallEventStackTableLayout.Controls.Add(this.buttonUp, 0, 0);
            this.CallEventStackTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CallEventStackTableLayout.Location = new System.Drawing.Point(6, 336);
            this.CallEventStackTableLayout.Name = "CallEventStackTableLayout";
            this.CallEventStackTableLayout.RowCount = 1;
            this.CallEventStackTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CallEventStackTableLayout.Size = new System.Drawing.Size(503, 28);
            this.CallEventStackTableLayout.TabIndex = 1;
            // 
            // buttonClose
            // 
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonClose.Location = new System.Drawing.Point(420, 0);
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
            // CallEventContainerItemPropertiesDialogBase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonClose;
            this.ClientSize = new System.Drawing.Size(517, 535);
            this.Controls.Add(this.splitContainer1);
            this.Name = "EntryPropertiesDialogBase";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Properties";
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.CallEventDetailsTableLayout.ResumeLayout(false);
            this.CallEventDetailsTabControl.ResumeLayout(false);
            this.CallEventDetailsTabPage.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.CallEventStackTabPage.ResumeLayout(false);
            this.CallEventStackTableLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.TableLayoutPanel CallEventDetailsTableLayout;
        protected System.Windows.Forms.TabControl CallEventDetailsTabControl;
        protected System.Windows.Forms.TabPage CallEventDetailsTabPage;
        protected System.Windows.Forms.TabPage CallEventStackTabPage;
        protected ListViewSorted _listViewStack;
        protected System.Windows.Forms.ColumnHeader columnHeaderNumber;
        protected System.Windows.Forms.ColumnHeader columnHeaderModule;
        protected System.Windows.Forms.ColumnHeader columnHeaderFrame;
        protected System.Windows.Forms.ColumnHeader columnHeaderAddress;
        protected System.Windows.Forms.ColumnHeader columnHeaderPath;
        protected System.Windows.Forms.TableLayoutPanel CallEventStackTableLayout;
        protected System.Windows.Forms.Button buttonClose;
        protected System.Windows.Forms.Button buttonDown;
        protected System.Windows.Forms.Button buttonUp;
        protected System.Windows.Forms.SplitContainer splitContainer1;
        protected System.Windows.Forms.SplitContainer splitContainer2;
    }
}