namespace SpyStudio.Dialogs.Properties
{
    partial class EntryPropertiesDialog
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
            this.CallEvensTracePanel = new System.Windows.Forms.TableLayoutPanel();
            this.CallEventsTraceLabel = new System.Windows.Forms.Label();
            this.CallEventsView = new SpyStudio.Main.TraceTreeView();
            this._listViewEvent = new System.Windows.Forms.ListView();
            this.propertyName = new System.Windows.Forms.ColumnHeader();
            this.propertyValue = new System.Windows.Forms.ColumnHeader();
            this._listViewParams = new System.Windows.Forms.ListView();
            this.parameterName = new System.Windows.Forms.ColumnHeader();
            this.parameterValue = new System.Windows.Forms.ColumnHeader();
            this.CallEventDetailsTabPage.SuspendLayout();
            this.CallEventStackTabPage.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.CallEvensTracePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // CallEventDetailsTabPage
            // 
            this.CallEventDetailsTabPage.Size = new System.Drawing.Size(495, 297);
            // 
            // CallEventStackTabPage
            // 
            this.CallEventStackTabPage.Size = new System.Drawing.Size(495, 297);
            // 
            // _listViewStack
            // 
            this._listViewStack.Size = new System.Drawing.Size(489, 291);
            // 
            // splitContainer1
            // 
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.CallEvensTracePanel);
            this.splitContainer1.SplitterDistance = 160;
            // 
            // splitContainer2
            // 
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this._listViewEvent);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this._listViewParams);
            this.splitContainer2.Size = new System.Drawing.Size(489, 291);
            this.splitContainer2.SplitterDistance = 139;
            // 
            // CallEvensTracePanel
            // 
            this.CallEvensTracePanel.ColumnCount = 1;
            this.CallEvensTracePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CallEvensTracePanel.Controls.Add(this.CallEventsTraceLabel, 0, 0);
            this.CallEvensTracePanel.Controls.Add(this.CallEventsView, 0, 1);
            this.CallEvensTracePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CallEvensTracePanel.Location = new System.Drawing.Point(0, 0);
            this.CallEvensTracePanel.Name = "CallEvensTracePanel";
            this.CallEvensTracePanel.RowCount = 2;
            this.CallEvensTracePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.CallEvensTracePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CallEvensTracePanel.Size = new System.Drawing.Size(515, 158);
            this.CallEvensTracePanel.TabIndex = 0;
            // 
            // CallEventsTraceLabel
            // 
            this.CallEventsTraceLabel.AutoSize = true;
            this.CallEventsTraceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CallEventsTraceLabel.Location = new System.Drawing.Point(3, 0);
            this.CallEventsTraceLabel.Name = "CallEventsTraceLabel";
            this.CallEventsTraceLabel.Size = new System.Drawing.Size(509, 20);
            this.CallEventsTraceLabel.TabIndex = 0;
            this.CallEventsTraceLabel.Text = "Related Call Events:";
            this.CallEventsTraceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CallEventsView
            // 
            this.CallEventsView.BackColor = System.Drawing.SystemColors.Window;
            this.CallEventsView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CallEventsView.DefaultToolTipProvider = null;
            this.CallEventsView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CallEventsView.DragDropMarkColor = System.Drawing.Color.Black;
            this.CallEventsView.FullRowSelect = true;
            this.CallEventsView.Indent = 7;
            this.CallEventsView.LineColor = System.Drawing.SystemColors.ControlDark;
            this.CallEventsView.Location = new System.Drawing.Point(3, 23);
            this.CallEventsView.Model = null;
            this.CallEventsView.Name = "CallEventsView";
            this.CallEventsView.SelectedNode = null;
            this.CallEventsView.ShowLines = false;
            this.CallEventsView.ShowNodeToolTips = true;
            this.CallEventsView.Size = new System.Drawing.Size(509, 132);
            this.CallEventsView.TabIndex = 1;
            this.CallEventsView.Text = "traceTreeView1";
            this.CallEventsView.UseColumns = true;
            // 
            // _listViewEvent
            // 
            this._listViewEvent.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.propertyName,
            this.propertyValue});
            this._listViewEvent.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewEvent.FullRowSelect = true;
            this._listViewEvent.Location = new System.Drawing.Point(0, 0);
            this._listViewEvent.Name = "_listViewEvent";
            this._listViewEvent.Size = new System.Drawing.Size(489, 139);
            this._listViewEvent.TabIndex = 0;
            this._listViewEvent.UseCompatibleStateImageBehavior = false;
            this._listViewEvent.View = System.Windows.Forms.View.Details;
            // 
            // propertyName
            // 
            this.propertyName.Text = "Property";
            this.propertyName.Width = 145;
            // 
            // propertyValue
            // 
            this.propertyValue.Text = "Value";
            this.propertyValue.Width = 340;
            // 
            // _listViewParams
            // 
            this._listViewParams.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.parameterName,
            this.parameterValue});
            this._listViewParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewParams.FullRowSelect = true;
            this._listViewParams.Location = new System.Drawing.Point(0, 0);
            this._listViewParams.Name = "_listViewParams";
            this._listViewParams.Size = new System.Drawing.Size(489, 148);
            this._listViewParams.TabIndex = 0;
            this._listViewParams.UseCompatibleStateImageBehavior = false;
            this._listViewParams.View = System.Windows.Forms.View.Details;
            // 
            // parameterName
            // 
            this.parameterName.Text = "Parameter";
            this.parameterName.Width = 142;
            // 
            // parameterValue
            // 
            this.parameterValue.Text = "Value";
            this.parameterValue.Width = 343;
            // 
            // CallEventContainerItemPropertiesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.ClientSize = new System.Drawing.Size(517, 535);
            this.Name = "EntryPropertiesDialog";
            this.CallEventDetailsTabPage.ResumeLayout(false);
            this.CallEventStackTabPage.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.CallEvensTracePanel.ResumeLayout(false);
            this.CallEvensTracePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel CallEvensTracePanel;
        private System.Windows.Forms.Label CallEventsTraceLabel;
        private SpyStudio.Main.TraceTreeView CallEventsView;
        private System.Windows.Forms.ListView _listViewEvent;
        private System.Windows.Forms.ListView _listViewParams;
        private System.Windows.Forms.ColumnHeader propertyName;
        private System.Windows.Forms.ColumnHeader propertyValue;
        private System.Windows.Forms.ColumnHeader parameterName;
        private System.Windows.Forms.ColumnHeader parameterValue;
    }
}
