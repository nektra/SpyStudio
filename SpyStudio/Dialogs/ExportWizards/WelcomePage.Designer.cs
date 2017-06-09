namespace SpyStudio.Dialogs.ExportWizards
{
    partial class WelcomePage
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
            this.introductionLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.CreateEditOrUpdateRadioButton = new System.Windows.Forms.RadioButton();
            this.CreateFromTemplateRadioButton = new System.Windows.Forms.RadioButton();
            this.LoadPreviousSelectionsCheckBox = new System.Windows.Forms.CheckBox();
            this._groupBoxTrace = new System.Windows.Forms.GroupBox();
            this._groupBoxPackage = new System.Windows.Forms.GroupBox();
            this.DoNotUseTraceRadioButton = new System.Windows.Forms.RadioButton();
            this.TracePathTextBox = new System.Windows.Forms.TextBox();
            this.BrowseTraceFileButton = new System.Windows.Forms.Button();
            this.LoadTraceFromDiskRadioButton = new System.Windows.Forms.RadioButton();
            this.UseMainWindowTraceRadioButton = new System.Windows.Forms.RadioButton();
            this._groupBoxTrace.SuspendLayout();
            this._groupBoxPackage.SuspendLayout();
            this.SuspendLayout();
            // 
            // Sidebar
            // 
            this.Sidebar.Size = new System.Drawing.Size(165, 401);
            // 
            // introductionLabel
            // 
            this.introductionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.introductionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.introductionLabel.Location = new System.Drawing.Point(171, 44);
            this.introductionLabel.Name = "introductionLabel";
            this.introductionLabel.Size = new System.Drawing.Size(466, 82);
            this.introductionLabel.TabIndex = 4;
            this.introductionLabel.Text = "Welcome message.";
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(171, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(466, 28);
            this.titleLabel.TabIndex = 3;
            this.titleLabel.Text = "Welcome";
            // 
            // CreateEditOrUpdateRadioButton
            // 
            this.CreateEditOrUpdateRadioButton.AutoSize = true;
            this.CreateEditOrUpdateRadioButton.Location = new System.Drawing.Point(20, 20);
            this.CreateEditOrUpdateRadioButton.Name = "CreateEditOrUpdateRadioButton";
            this.CreateEditOrUpdateRadioButton.Size = new System.Drawing.Size(172, 17);
            this.CreateEditOrUpdateRadioButton.TabIndex = 13;
            this.CreateEditOrUpdateRadioButton.TabStop = true;
            this.CreateEditOrUpdateRadioButton.Text = "Create, edit or update package";
            this.CreateEditOrUpdateRadioButton.UseVisualStyleBackColor = true;
            this.CreateEditOrUpdateRadioButton.CheckedChanged += new System.EventHandler(this.CreateEditOrUpdateOptionCheckedChanged);
            // 
            // CreateFromTemplateRadioButton
            // 
            this.CreateFromTemplateRadioButton.AutoSize = true;
            this.CreateFromTemplateRadioButton.Location = new System.Drawing.Point(20, 62);
            this.CreateFromTemplateRadioButton.Name = "CreateFromTemplateRadioButton";
            this.CreateFromTemplateRadioButton.Size = new System.Drawing.Size(165, 17);
            this.CreateFromTemplateRadioButton.TabIndex = 15;
            this.CreateFromTemplateRadioButton.TabStop = true;
            this.CreateFromTemplateRadioButton.Text = "Create from Template Catalog";
            this.CreateFromTemplateRadioButton.UseVisualStyleBackColor = true;
            this.CreateFromTemplateRadioButton.CheckedChanged += new System.EventHandler(this.CreateFromTemplateOptionCheckedChanged);
            // 
            // LoadPreviousSelectionsCheckBox
            // 
            this.LoadPreviousSelectionsCheckBox.AutoSize = true;
            this.LoadPreviousSelectionsCheckBox.Location = new System.Drawing.Point(40, 39);
            this.LoadPreviousSelectionsCheckBox.Name = "LoadPreviousSelectionsCheckBox";
            this.LoadPreviousSelectionsCheckBox.Size = new System.Drawing.Size(143, 17);
            this.LoadPreviousSelectionsCheckBox.TabIndex = 16;
            this.LoadPreviousSelectionsCheckBox.Text = "Load previous selections";
            this.LoadPreviousSelectionsCheckBox.UseVisualStyleBackColor = true;
            this.LoadPreviousSelectionsCheckBox.CheckedChanged += new System.EventHandler(this.LoadPreviousSelectionsOptionCheckedChanged);
            // 
            // _groupBoxTrace
            // 
            this._groupBoxTrace.Controls.Add(this.CreateFromTemplateRadioButton);
            this._groupBoxTrace.Controls.Add(this.CreateEditOrUpdateRadioButton);
            this._groupBoxTrace.Controls.Add(this.LoadPreviousSelectionsCheckBox);
            this._groupBoxTrace.Location = new System.Drawing.Point(180, 245);
            this._groupBoxTrace.Name = "_groupBoxTrace";
            this._groupBoxTrace.Size = new System.Drawing.Size(440, 89);
            this._groupBoxTrace.TabIndex = 17;
            this._groupBoxTrace.TabStop = false;
            this._groupBoxTrace.Text = "Package";
            // 
            // _groupBoxPackage
            // 
            this._groupBoxPackage.Controls.Add(this.DoNotUseTraceRadioButton);
            this._groupBoxPackage.Controls.Add(this.TracePathTextBox);
            this._groupBoxPackage.Controls.Add(this.BrowseTraceFileButton);
            this._groupBoxPackage.Controls.Add(this.LoadTraceFromDiskRadioButton);
            this._groupBoxPackage.Controls.Add(this.UseMainWindowTraceRadioButton);
            this._groupBoxPackage.Location = new System.Drawing.Point(180, 119);
            this._groupBoxPackage.Name = "_groupBoxPackage";
            this._groupBoxPackage.Size = new System.Drawing.Size(440, 111);
            this._groupBoxPackage.TabIndex = 18;
            this._groupBoxPackage.TabStop = false;
            this._groupBoxPackage.Text = "Trace";
            // 
            // DoNotUseTraceRadioButton
            // 
            this.DoNotUseTraceRadioButton.AutoSize = true;
            this.DoNotUseTraceRadioButton.Location = new System.Drawing.Point(20, 84);
            this.DoNotUseTraceRadioButton.Name = "DoNotUseTraceRadioButton";
            this.DoNotUseTraceRadioButton.Size = new System.Drawing.Size(108, 17);
            this.DoNotUseTraceRadioButton.TabIndex = 6;
            this.DoNotUseTraceRadioButton.TabStop = true;
            this.DoNotUseTraceRadioButton.Text = "Do not use Trace";
            this.DoNotUseTraceRadioButton.UseVisualStyleBackColor = true;
            this.DoNotUseTraceRadioButton.CheckedChanged += new System.EventHandler(this.DoNotUseTraceOptionCheckedChanged);
            // 
            // TracePathTextBox
            // 
            this.TracePathTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TracePathTextBox.Location = new System.Drawing.Point(40, 61);
            this.TracePathTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.TracePathTextBox.Name = "TracePathTextBox";
            this.TracePathTextBox.ReadOnly = true;
            this.TracePathTextBox.Size = new System.Drawing.Size(315, 20);
            this.TracePathTextBox.TabIndex = 4;
            // 
            // _buttonBrowse
            // 
            this.BrowseTraceFileButton.Location = new System.Drawing.Point(361, 60);
            this.BrowseTraceFileButton.Name = "BrowseTraceFileButton";
            this.BrowseTraceFileButton.Size = new System.Drawing.Size(63, 22);
            this.BrowseTraceFileButton.TabIndex = 5;
            this.BrowseTraceFileButton.Text = "Browse";
            this.BrowseTraceFileButton.UseVisualStyleBackColor = true;
            this.BrowseTraceFileButton.Click += new System.EventHandler(this.BrowseButtonClick);
            // 
            // LoadTraceFromDiskRadioButton
            // 
            this.LoadTraceFromDiskRadioButton.AutoSize = true;
            this.LoadTraceFromDiskRadioButton.Location = new System.Drawing.Point(20, 40);
            this.LoadTraceFromDiskRadioButton.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            this.LoadTraceFromDiskRadioButton.Name = "LoadTraceFromDiskRadioButton";
            this.LoadTraceFromDiskRadioButton.Size = new System.Drawing.Size(125, 17);
            this.LoadTraceFromDiskRadioButton.TabIndex = 2;
            this.LoadTraceFromDiskRadioButton.TabStop = true;
            this.LoadTraceFromDiskRadioButton.Text = "Load Trace from disk";
            this.LoadTraceFromDiskRadioButton.UseVisualStyleBackColor = true;
            this.LoadTraceFromDiskRadioButton.CheckedChanged += new System.EventHandler(this.LoadTraceFromDiskOptionCheckedChanged);
            // 
            // UseMainWindowTraceRadioButton
            // 
            this.UseMainWindowTraceRadioButton.AutoSize = true;
            this.UseMainWindowTraceRadioButton.Location = new System.Drawing.Point(20, 19);
            this.UseMainWindowTraceRadioButton.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            this.UseMainWindowTraceRadioButton.Name = "UseMainWindowTraceRadioButton";
            this.UseMainWindowTraceRadioButton.Size = new System.Drawing.Size(185, 17);
            this.UseMainWindowTraceRadioButton.TabIndex = 1;
            this.UseMainWindowTraceRadioButton.TabStop = true;
            this.UseMainWindowTraceRadioButton.Text = "Use Trace loaded in main window";
            this.UseMainWindowTraceRadioButton.UseVisualStyleBackColor = true;
            this.UseMainWindowTraceRadioButton.CheckedChanged += new System.EventHandler(this.UseMainWindowTraceOptionCheckedChanged);
            // 
            // WelcomePage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._groupBoxPackage);
            this.Controls.Add(this._groupBoxTrace);
            this.Controls.Add(this.introductionLabel);
            this.Controls.Add(this.titleLabel);
            this.Name = "WelcomePage";
            this.Size = new System.Drawing.Size(637, 401);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.WelcomePageQueryCancel);
            this.SetActive += new Wizard.UI.WizardPageEventHandler(this.WelcomePageSetActive);
            this.Controls.SetChildIndex(this.Sidebar, 0);
            this.Controls.SetChildIndex(this.titleLabel, 0);
            this.Controls.SetChildIndex(this.introductionLabel, 0);
            this.Controls.SetChildIndex(this._groupBoxTrace, 0);
            this.Controls.SetChildIndex(this._groupBoxPackage, 0);
            this._groupBoxTrace.ResumeLayout(false);
            this._groupBoxTrace.PerformLayout();
            this._groupBoxPackage.ResumeLayout(false);
            this._groupBoxPackage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label introductionLabel;
        private System.Windows.Forms.Label titleLabel;
        protected System.Windows.Forms.RadioButton CreateEditOrUpdateRadioButton;
        protected System.Windows.Forms.RadioButton CreateFromTemplateRadioButton;
        protected System.Windows.Forms.CheckBox LoadPreviousSelectionsCheckBox;
        private System.Windows.Forms.GroupBox _groupBoxTrace;
        private System.Windows.Forms.GroupBox _groupBoxPackage;
        protected System.Windows.Forms.RadioButton UseMainWindowTraceRadioButton;
        protected System.Windows.Forms.RadioButton LoadTraceFromDiskRadioButton;
        protected System.Windows.Forms.TextBox TracePathTextBox;
        private System.Windows.Forms.Button BrowseTraceFileButton;
        protected System.Windows.Forms.RadioButton DoNotUseTraceRadioButton;
    }
}