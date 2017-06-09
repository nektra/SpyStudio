namespace SpyStudio.Dialogs.Properties
{
    partial class EntryComparePropertiesDialog
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
            this.TracePanel = new System.Windows.Forms.TableLayoutPanel();
            this.ComparedTraceLabel = new System.Windows.Forms.Label();
            this.CompareItemsView = new SpyStudio.Dialogs.Compare.DeviareTraceCompareTreeView();
            this.processNameColumn = new Aga.Controls.Tree.TreeColumn();
            this.callerColumn = new Aga.Controls.Tree.TreeColumn();
            this.frameColumn = new Aga.Controls.Tree.TreeColumn();
            this.functionColumn = new Aga.Controls.Tree.TreeColumn();
            this.paramMainColumn = new Aga.Controls.Tree.TreeColumn();
            this.detailscColumn = new Aga.Controls.Tree.TreeColumn();
            this.resultColumn = new Aga.Controls.Tree.TreeColumn();
            this.processNameNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.callerNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.frameNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.functionNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.paramMainNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.detailsNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.resultNode = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this._columnHeaderValue2 = new System.Windows.Forms.ColumnHeader();
            this._columnValue2 = new System.Windows.Forms.ColumnHeader();
            this._listViewEvent = new System.Windows.Forms.ListView();
            this.propertyName = new System.Windows.Forms.ColumnHeader();
            this.propertyValue = new System.Windows.Forms.ColumnHeader();
            this._listViewParams = new System.Windows.Forms.ListView();
            this.parameterName = new System.Windows.Forms.ColumnHeader();
            this.parameterValue = new System.Windows.Forms.ColumnHeader();
            this.propertyValue2 = new System.Windows.Forms.ColumnHeader();
            this.parameterValue2 = new System.Windows.Forms.ColumnHeader();
            this.CallEventDetailsTabPage.SuspendLayout();
            this.CallEventStackTabPage.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.TracePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // columnHeaderModule
            // 
            this.columnHeaderModule.Width = 83;
            // 
            // splitContainer1
            // 
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.TracePanel);
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
            // 
            // TracePanel
            // 
            this.TracePanel.ColumnCount = 1;
            this.TracePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TracePanel.Controls.Add(this.ComparedTraceLabel, 0, 0);
            this.TracePanel.Controls.Add(this.CompareItemsView, 0, 1);
            this.TracePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TracePanel.Location = new System.Drawing.Point(0, 0);
            this.TracePanel.Name = "TracePanel";
            this.TracePanel.RowCount = 2;
            this.TracePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TracePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TracePanel.Size = new System.Drawing.Size(515, 157);
            this.TracePanel.TabIndex = 0;
            // 
            // ComparedTraceLabel
            // 
            this.ComparedTraceLabel.AutoSize = true;
            this.ComparedTraceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ComparedTraceLabel.Location = new System.Drawing.Point(3, 0);
            this.ComparedTraceLabel.Name = "ComparedTraceLabel";
            this.ComparedTraceLabel.Size = new System.Drawing.Size(509, 20);
            this.ComparedTraceLabel.TabIndex = 0;
            this.ComparedTraceLabel.Text = "Related Compared Call Events:";
            this.ComparedTraceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CompareItemsView
            // 
            this.CompareItemsView.BackColor = System.Drawing.SystemColors.Window;
            this.CompareItemsView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CompareItemsView.Columns.Add(this.processNameColumn);
            this.CompareItemsView.Columns.Add(this.callerColumn);
            this.CompareItemsView.Columns.Add(this.frameColumn);
            this.CompareItemsView.Columns.Add(this.functionColumn);
            this.CompareItemsView.Columns.Add(this.paramMainColumn);
            this.CompareItemsView.Columns.Add(this.detailscColumn);
            this.CompareItemsView.Columns.Add(this.resultColumn);
            this.CompareItemsView.DefaultToolTipProvider = null;
            this.CompareItemsView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CompareItemsView.DragDropMarkColor = System.Drawing.Color.Black;
            this.CompareItemsView.FullRowSelect = true;
            this.CompareItemsView.Indent = 7;
            this.CompareItemsView.LineColor = System.Drawing.SystemColors.ControlDark;
            this.CompareItemsView.Location = new System.Drawing.Point(3, 23);
            this.CompareItemsView.Model = null;
            this.CompareItemsView.Name = "CompareItemsView";
            this.CompareItemsView.NodeControls.Add(this.processNameNode);
            this.CompareItemsView.NodeControls.Add(this.callerNode);
            this.CompareItemsView.NodeControls.Add(this.frameNode);
            this.CompareItemsView.NodeControls.Add(this.functionNode);
            this.CompareItemsView.NodeControls.Add(this.paramMainNode);
            this.CompareItemsView.NodeControls.Add(this.detailsNode);
            this.CompareItemsView.NodeControls.Add(this.resultNode);
            this.CompareItemsView.SelectedNode = null;
            this.CompareItemsView.ShowLines = false;
            this.CompareItemsView.ShowNodeToolTips = true;
            this.CompareItemsView.Size = new System.Drawing.Size(509, 131);
            this.CompareItemsView.TabIndex = 1;
            this.CompareItemsView.Text = "deviareTraceCompareTreeView1";
            this.CompareItemsView.UseColumns = true;
            // 
            // processNameColumn
            // 
            this.processNameColumn.Header = "Process Name";
            this.processNameColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.processNameColumn.TooltipText = null;
            this.processNameColumn.Width = 90;
            // 
            // callerColumn
            // 
            this.callerColumn.Header = "Caller";
            this.callerColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.callerColumn.TooltipText = null;
            // 
            // frameColumn
            // 
            this.frameColumn.Header = "Frame";
            this.frameColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.frameColumn.TooltipText = null;
            // 
            // functionColumn
            // 
            this.functionColumn.Header = "Function";
            this.functionColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.functionColumn.TooltipText = null;
            this.functionColumn.Width = 60;
            // 
            // paramMainColumn
            // 
            this.paramMainColumn.Header = "ParamMain";
            this.paramMainColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.paramMainColumn.TooltipText = null;
            this.paramMainColumn.Width = 65;
            // 
            // detailscColumn
            // 
            this.detailscColumn.Header = "Param Details";
            this.detailscColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.detailscColumn.TooltipText = null;
            this.detailscColumn.Width = 80;
            // 
            // resultColumn
            // 
            this.resultColumn.Header = "Result";
            this.resultColumn.SortOrder = System.Windows.Forms.SortOrder.None;
            this.resultColumn.TooltipText = null;
            this.resultColumn.Width = 90;
            // 
            // processNameNode
            // 
            this.processNameNode.DataPropertyName = "ProcessName";
            this.processNameNode.IncrementalSearchEnabled = true;
            this.processNameNode.LeftMargin = 3;
            this.processNameNode.ParentColumn = this.processNameColumn;
            // 
            // callerNode
            // 
            this.callerNode.DataPropertyName = "Caller";
            this.callerNode.IncrementalSearchEnabled = true;
            this.callerNode.LeftMargin = 3;
            this.callerNode.ParentColumn = this.callerColumn;
            // 
            // frameNode
            // 
            this.frameNode.DataPropertyName = "StackFrame";
            this.frameNode.IncrementalSearchEnabled = true;
            this.frameNode.LeftMargin = 3;
            this.frameNode.ParentColumn = this.frameColumn;
            // 
            // functionNode
            // 
            this.functionNode.DataPropertyName = "Function";
            this.functionNode.IncrementalSearchEnabled = true;
            this.functionNode.LeftMargin = 3;
            this.functionNode.ParentColumn = this.functionColumn;
            // 
            // paramMainNode
            // 
            this.paramMainNode.DataPropertyName = "ParamMain";
            this.paramMainNode.IncrementalSearchEnabled = true;
            this.paramMainNode.LeftMargin = 3;
            this.paramMainNode.ParentColumn = this.paramMainColumn;
            // 
            // detailsNode
            // 
            this.detailsNode.DataPropertyName = "Details";
            this.detailsNode.IncrementalSearchEnabled = true;
            this.detailsNode.LeftMargin = 3;
            this.detailsNode.ParentColumn = this.detailscColumn;
            // 
            // resultNode
            // 
            this.resultNode.DataPropertyName = "Result";
            this.resultNode.IncrementalSearchEnabled = true;
            this.resultNode.LeftMargin = 3;
            this.resultNode.ParentColumn = this.resultColumn;
            // 
            // _columnHeaderValue2
            // 
            this._columnHeaderValue2.Text = "Value 2";
            // 
            // _columnValue2
            // 
            this._columnValue2.Text = "Value 2";
            // 
            // _listViewEvent
            // 
            this._listViewEvent.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.propertyName,
            this.propertyValue,
            this.propertyValue2});
            this._listViewEvent.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewEvent.FullRowSelect = true;
            this._listViewEvent.Location = new System.Drawing.Point(0, 0);
            this._listViewEvent.Name = "_listViewEvent";
            this._listViewEvent.Size = new System.Drawing.Size(489, 140);
            this._listViewEvent.TabIndex = 1;
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
            this.propertyValue.Text = "Value 1";
            this.propertyValue.Width = 172;
            // 
            // _listViewParams
            // 
            this._listViewParams.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.parameterName,
            this.parameterValue,
            this.parameterValue2});
            this._listViewParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listViewParams.FullRowSelect = true;
            this._listViewParams.Location = new System.Drawing.Point(0, 0);
            this._listViewParams.Name = "_listViewParams";
            this._listViewParams.Size = new System.Drawing.Size(489, 148);
            this._listViewParams.TabIndex = 1;
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
            this.parameterValue.Width = 175;
            // 
            // propertyValue2
            // 
            this.propertyValue2.Text = "Value 2";
            this.propertyValue2.Width = 168;
            // 
            // parameterValue2
            // 
            this.parameterValue2.Text = "Value 2";
            this.parameterValue2.Width = 168;
            // 
            // CallEventContainerItemComparePropertiesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.ClientSize = new System.Drawing.Size(517, 535);
            this.Name = "EntryComparePropertiesDialog";
            this.CallEventDetailsTabPage.ResumeLayout(false);
            this.CallEventStackTabPage.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.TracePanel.ResumeLayout(false);
            this.TracePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel TracePanel;
        private System.Windows.Forms.Label ComparedTraceLabel;
        private SpyStudio.Dialogs.Compare.DeviareTraceCompareTreeView CompareItemsView;
        private Aga.Controls.Tree.TreeColumn processNameColumn;
        private Aga.Controls.Tree.TreeColumn callerColumn;
        private Aga.Controls.Tree.TreeColumn frameColumn;
        private Aga.Controls.Tree.TreeColumn functionColumn;
        private Aga.Controls.Tree.TreeColumn paramMainColumn;
        private Aga.Controls.Tree.TreeColumn detailscColumn;
        private Aga.Controls.Tree.TreeColumn resultColumn;
        private Aga.Controls.Tree.NodeControls.NodeTextBox processNameNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox callerNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox frameNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox functionNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox paramMainNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox detailsNode;
        private Aga.Controls.Tree.NodeControls.NodeTextBox resultNode;
        private System.Windows.Forms.ListView _listViewEvent;
        private System.Windows.Forms.ColumnHeader propertyName;
        private System.Windows.Forms.ColumnHeader propertyValue;
        private System.Windows.Forms.ListView _listViewParams;
        private System.Windows.Forms.ColumnHeader parameterName;
        private System.Windows.Forms.ColumnHeader parameterValue;
        private System.Windows.Forms.ColumnHeader propertyValue2;
        private System.Windows.Forms.ColumnHeader parameterValue2;
    }
}
