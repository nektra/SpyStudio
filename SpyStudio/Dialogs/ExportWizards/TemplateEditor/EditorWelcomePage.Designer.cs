using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.TemplateEditor
{
    partial class EditorWelcomePage
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
            this._groupBoxPackage = new System.Windows.Forms.GroupBox();
            this._textBoxFile = new System.Windows.Forms.TextBox();
            this._buttonBrowse = new System.Windows.Forms.Button();
            this._radioButtonDisk = new System.Windows.Forms.RadioButton();
            this._radioButtonCurrentTrace = new System.Windows.Forms.RadioButton();
            this._captureTypeLabel = new System.Windows.Forms.Label();
            this._captureTypeCombo = new System.Windows.Forms.ComboBox();
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
            // _groupBoxPackage
            // 
            this._groupBoxPackage.Controls.Add(this._captureTypeLabel);
            this._groupBoxPackage.Controls.Add(this._textBoxFile);
            this._groupBoxPackage.Controls.Add(this._buttonBrowse);
            this._groupBoxPackage.Controls.Add(this._radioButtonDisk);
            this._groupBoxPackage.Controls.Add(this._captureTypeCombo);
            this._groupBoxPackage.Controls.Add(this._radioButtonCurrentTrace);
            this._groupBoxPackage.Location = new System.Drawing.Point(180, 119);
            this._groupBoxPackage.Name = "_groupBoxPackage";
            this._groupBoxPackage.Size = new System.Drawing.Size(440, 132);
            this._groupBoxPackage.TabIndex = 18;
            this._groupBoxPackage.TabStop = false;
            this._groupBoxPackage.Text = "Template";
            // 
            // _textBoxFile
            // 
            this._textBoxFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._textBoxFile.Location = new System.Drawing.Point(40, 96);
            this._textBoxFile.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this._textBoxFile.Name = "_textBoxFile";
            this._textBoxFile.ReadOnly = true;
            this._textBoxFile.Size = new System.Drawing.Size(315, 20);
            this._textBoxFile.TabIndex = 4;
            // 
            // _buttonBrowse
            // 
            this._buttonBrowse.Location = new System.Drawing.Point(361, 95);
            this._buttonBrowse.Name = "_buttonBrowse";
            this._buttonBrowse.Size = new System.Drawing.Size(63, 22);
            this._buttonBrowse.TabIndex = 5;
            this._buttonBrowse.Text = "Browse";
            this._buttonBrowse.UseVisualStyleBackColor = true;
            this._buttonBrowse.Click += new System.EventHandler(this.ButtonBrowseClick);
            // 
            // _radioButtonDisk
            // 
            this._radioButtonDisk.AutoSize = true;
            this._radioButtonDisk.Location = new System.Drawing.Point(20, 75);
            this._radioButtonDisk.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            this._radioButtonDisk.Name = "_radioButtonDisk";
            this._radioButtonDisk.Size = new System.Drawing.Size(137, 17);
            this._radioButtonDisk.TabIndex = 2;
            this._radioButtonDisk.TabStop = true;
            this._radioButtonDisk.Text = "Load template from disk";
            this._radioButtonDisk.UseVisualStyleBackColor = true;
            this._radioButtonDisk.CheckedChanged += new System.EventHandler(this.RadioButtonDiskCheckedChanged);
            // 
            // _radioButtonCurrentTrace
            // 
            this._radioButtonCurrentTrace.AutoSize = true;
            this._radioButtonCurrentTrace.Location = new System.Drawing.Point(20, 19);
            this._radioButtonCurrentTrace.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            this._radioButtonCurrentTrace.Name = "_radioButtonCurrentTrace";
            this._radioButtonCurrentTrace.Size = new System.Drawing.Size(217, 17);
            this._radioButtonCurrentTrace.TabIndex = 1;
            this._radioButtonCurrentTrace.TabStop = true;
            this._radioButtonCurrentTrace.Text = "Create template from main window Trace";
            this._radioButtonCurrentTrace.UseVisualStyleBackColor = true;
            // 
            // _captureTypeLabel
            // 
            this._captureTypeLabel.AutoSize = true;
            this._captureTypeLabel.Location = new System.Drawing.Point(37, 48);
            this._captureTypeLabel.Name = "_captureTypeLabel";
            this._captureTypeLabel.Size = new System.Drawing.Size(74, 13);
            this._captureTypeLabel.TabIndex = 23;
            this._captureTypeLabel.Text = "Capture Type:";
            this._captureTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _captureTypeCombo
            // 
            this._captureTypeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._captureTypeCombo.FormattingEnabled = true;
            this._captureTypeCombo.Location = new System.Drawing.Point(117, 45);
            this._captureTypeCombo.Name = "_captureTypeCombo";
            this._captureTypeCombo.Size = new System.Drawing.Size(307, 21);
            this._captureTypeCombo.TabIndex = 24;
            // 
            // EditorWelcomePage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._groupBoxPackage);
            this.Controls.Add(this.introductionLabel);
            this.Controls.Add(this.titleLabel);
            this.Name = "EditorWelcomePage";
            this.Size = new System.Drawing.Size(637, 401);
            this.QueryCancel += new System.ComponentModel.CancelEventHandler(this.WelcomePageQueryCancel);
            this.SetActive += new WizardPageEventHandler(this.WelcomePageSetActive);
            this.Controls.SetChildIndex(this.titleLabel, 0);
            this.Controls.SetChildIndex(this.introductionLabel, 0);
            this.Controls.SetChildIndex(this._groupBoxPackage, 0);
            this.Controls.SetChildIndex(this.Sidebar, 0);
            this._groupBoxPackage.ResumeLayout(false);
            this._groupBoxPackage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label introductionLabel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.GroupBox _groupBoxPackage;
        private System.Windows.Forms.RadioButton _radioButtonCurrentTrace;
        private System.Windows.Forms.RadioButton _radioButtonDisk;
        private System.Windows.Forms.TextBox _textBoxFile;
        private System.Windows.Forms.Button _buttonBrowse;
        protected System.Windows.Forms.Label _captureTypeLabel;
        protected System.Windows.Forms.ComboBox _captureTypeCombo;
    }
}