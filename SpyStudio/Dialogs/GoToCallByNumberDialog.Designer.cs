namespace SpyStudio.Dialogs
{
    partial class GoToCallByNumberDialog
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
            this.mainTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.textBoxLabel = new System.Windows.Forms.Label();
            this.buttonsTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.acceptButton = new System.Windows.Forms.Button();
            this.lineNumberBox = new SpyStudio.Dialogs.GoToCallByNumberDialog.VerifiedNumericUpDown();
            this.mainTableLayout.SuspendLayout();
            this.buttonsTableLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lineNumberBox)).BeginInit();
            this.SuspendLayout();
            // 
            // mainTableLayout
            // 
            this.mainTableLayout.AutoSize = true;
            this.mainTableLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mainTableLayout.ColumnCount = 1;
            this.mainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayout.Controls.Add(this.textBoxLabel, 0, 0);
            this.mainTableLayout.Controls.Add(this.buttonsTableLayout, 0, 2);
            this.mainTableLayout.Controls.Add(this.lineNumberBox, 0, 1);
            this.mainTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayout.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayout.Name = "mainTableLayout";
            this.mainTableLayout.RowCount = 3;
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayout.Size = new System.Drawing.Size(284, 84);
            this.mainTableLayout.TabIndex = 0;
            // 
            // textBoxLabel
            // 
            this.textBoxLabel.AutoSize = true;
            this.textBoxLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLabel.Location = new System.Drawing.Point(3, 0);
            this.textBoxLabel.Name = "textBoxLabel";
            this.textBoxLabel.Size = new System.Drawing.Size(278, 20);
            this.textBoxLabel.TabIndex = 0;
            this.textBoxLabel.Text = "Call Number";
            this.textBoxLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonsTableLayout
            // 
            this.buttonsTableLayout.AutoSize = true;
            this.buttonsTableLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonsTableLayout.ColumnCount = 3;
            this.buttonsTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 37.05036F));
            this.buttonsTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32.73381F));
            this.buttonsTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30.57554F));
            this.buttonsTableLayout.Controls.Add(this.cancelButton, 2, 0);
            this.buttonsTableLayout.Controls.Add(this.acceptButton, 1, 0);
            this.buttonsTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonsTableLayout.Location = new System.Drawing.Point(3, 53);
            this.buttonsTableLayout.Name = "buttonsTableLayout";
            this.buttonsTableLayout.RowCount = 1;
            this.buttonsTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.buttonsTableLayout.Size = new System.Drawing.Size(278, 29);
            this.buttonsTableLayout.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(200, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButtonClick);
            // 
            // acceptButton
            // 
            this.acceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.acceptButton.Location = new System.Drawing.Point(114, 3);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(75, 23);
            this.acceptButton.TabIndex = 1;
            this.acceptButton.Text = "Accept";
            this.acceptButton.UseVisualStyleBackColor = true;
            this.acceptButton.Click += new System.EventHandler(this.AcceptButtonClick);
            // 
            // lineNumberBox
            // 
            this.lineNumberBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lineNumberBox.Location = new System.Drawing.Point(3, 23);
            this.lineNumberBox.Maximum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.lineNumberBox.Name = "lineNumberBox";
            this.lineNumberBox.Size = new System.Drawing.Size(278, 20);
            this.lineNumberBox.TabIndex = 3;
            this.lineNumberBox.ThousandsSeparator = true;
            // 
            // GoToCallByNumberDialog
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(284, 84);
            this.Controls.Add(this.mainTableLayout);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GoToCallByNumberDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Go To Call";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.GoToCallByNumberDialogShown);
            this.mainTableLayout.ResumeLayout(false);
            this.mainTableLayout.PerformLayout();
            this.buttonsTableLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.lineNumberBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayout;
        private System.Windows.Forms.Label textBoxLabel;
        private System.Windows.Forms.TableLayoutPanel buttonsTableLayout;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button acceptButton;
        private VerifiedNumericUpDown lineNumberBox;
    }
}