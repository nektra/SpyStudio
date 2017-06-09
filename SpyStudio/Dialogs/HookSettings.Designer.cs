namespace SpyStudio.Dialogs
{
    partial class HookSettings
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
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitContainerTopLeft = new System.Windows.Forms.SplitContainer();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.splitContainerTopLeft.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerTopLeft);
            this.splitContainerMain.Size = new System.Drawing.Size(648, 357);
            this.splitContainerMain.SplitterDistance = 165;
            this.splitContainerMain.TabIndex = 0;
            // 
            // splitContainerTopLeft
            // 
            this.splitContainerTopLeft.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainerTopLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerTopLeft.Location = new System.Drawing.Point(0, 0);
            this.splitContainerTopLeft.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerTopLeft.Name = "splitContainerTopLeft";
            this.splitContainerTopLeft.Size = new System.Drawing.Size(648, 165);
            this.splitContainerTopLeft.SplitterDistance = 305;
            this.splitContainerTopLeft.TabIndex = 0;
            // 
            // HookSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(648, 357);
            this.Controls.Add(this.splitContainerMain);
            this.Name = "HookSettings";
            this.ShowIcon = false;
            this.Text = "Hook Settings";
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.ResumeLayout(false);
            this.splitContainerTopLeft.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.SplitContainer splitContainerTopLeft;

    }
}