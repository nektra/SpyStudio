namespace SpyStudio.Dialogs.ExportWizards.MassExports
{
    partial class MassWelcomePage
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.introductionLabel = new System.Windows.Forms.Label();
            this.DiscardChangesButton = new System.Windows.Forms.Button();
            this.promptLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Sidebar
            // 
            this.Sidebar.Size = new System.Drawing.Size(165, 401);
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(171, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(499, 28);
            this.titleLabel.TabIndex = 4;
            this.titleLabel.Text = "Welcome";
            // 
            // introductionLabel
            // 
            this.introductionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.introductionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.introductionLabel.Location = new System.Drawing.Point(172, 90);
            this.introductionLabel.Name = "introductionLabel";
            this.introductionLabel.Size = new System.Drawing.Size(499, 82);
            this.introductionLabel.TabIndex = 5;
            this.introductionLabel.Text = "Welcome message.";
            // 
            // DiscardChangesButton
            // 
            this.DiscardChangesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardChangesButton.Location = new System.Drawing.Point(436, 375);
            this.DiscardChangesButton.Name = "DiscardChangesButton";
            this.DiscardChangesButton.Size = new System.Drawing.Size(178, 23);
            this.DiscardChangesButton.TabIndex = 8;
            this.DiscardChangesButton.TabStop = false;
            this.DiscardChangesButton.Text = "Discard previous changes";
            this.DiscardChangesButton.UseVisualStyleBackColor = true;
            this.DiscardChangesButton.Visible = false;
            // 
            // promptLabel
            // 
            this.promptLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.promptLabel.Location = new System.Drawing.Point(171, 356);
            this.promptLabel.Name = "promptLabel";
            this.promptLabel.Size = new System.Drawing.Size(401, 16);
            this.promptLabel.TabIndex = 9;
            this.promptLabel.Text = "Press Next to continue.";
            // 
            // MassWelcomePage
            // 
            this.Controls.Add(this.promptLabel);
            this.Controls.Add(this.DiscardChangesButton);
            this.Controls.Add(this.introductionLabel);
            this.Controls.Add(this.titleLabel);
            this.Name = "MassWelcomePage";
            this.Size = new System.Drawing.Size(617, 401);
            this.Controls.SetChildIndex(this.Sidebar, 0);
            this.Controls.SetChildIndex(this.titleLabel, 0);
            this.Controls.SetChildIndex(this.introductionLabel, 0);
            this.Controls.SetChildIndex(this.DiscardChangesButton, 0);
            this.Controls.SetChildIndex(this.promptLabel, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label introductionLabel;
        private System.Windows.Forms.Button DiscardChangesButton;
        private System.Windows.Forms.Label promptLabel;
    }
}
