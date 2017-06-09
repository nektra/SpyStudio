namespace SpyStudio.Dialogs
{
    partial class FormLicense
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormLicense));
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelBottom = new System.Windows.Forms.TableLayoutPanel();
            this.buttonDecline = new System.Windows.Forms.Button();
            this.buttonAgree = new System.Windows.Forms.Button();
            this.textBoxLicense = new System.Windows.Forms.TextBox();
            this.tableLayoutPanelMain.SuspendLayout();
            this.tableLayoutPanelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelMain.Controls.Add(this.tableLayoutPanelBottom, 0, 1);
            this.tableLayoutPanelMain.Controls.Add(this.textBoxLicense, 0, 0);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 2;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 91.82243F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.17757F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(507, 481);
            this.tableLayoutPanelMain.TabIndex = 0;
            // 
            // tableLayoutPanelBottom
            // 
            this.tableLayoutPanelBottom.ColumnCount = 3;
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelBottom.Controls.Add(this.buttonDecline, 2, 0);
            this.tableLayoutPanelBottom.Controls.Add(this.buttonAgree, 1, 0);
            this.tableLayoutPanelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelBottom.Location = new System.Drawing.Point(2, 444);
            this.tableLayoutPanelBottom.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tableLayoutPanelBottom.Name = "tableLayoutPanelBottom";
            this.tableLayoutPanelBottom.RowCount = 1;
            this.tableLayoutPanelBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBottom.Size = new System.Drawing.Size(503, 34);
            this.tableLayoutPanelBottom.TabIndex = 0;
            // 
            // buttonDecline
            // 
            this.buttonDecline.AutoSize = true;
            this.buttonDecline.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonDecline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonDecline.Location = new System.Drawing.Point(426, 3);
            this.buttonDecline.Name = "buttonDecline";
            this.buttonDecline.Size = new System.Drawing.Size(74, 28);
            this.buttonDecline.TabIndex = 4;
            this.buttonDecline.Text = "&Decline";
            this.buttonDecline.UseVisualStyleBackColor = true;
            // 
            // buttonAgree
            // 
            this.buttonAgree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonAgree.Location = new System.Drawing.Point(346, 3);
            this.buttonAgree.Name = "buttonAgree";
            this.buttonAgree.Size = new System.Drawing.Size(74, 28);
            this.buttonAgree.TabIndex = 5;
            this.buttonAgree.Text = "&Agree";
            this.buttonAgree.UseVisualStyleBackColor = true;
            this.buttonAgree.Click += new System.EventHandler(this.ButtonAgreeClick);
            // 
            // textBoxLicense
            // 
            this.textBoxLicense.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxLicense.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLicense.Location = new System.Drawing.Point(3, 3);
            this.textBoxLicense.Multiline = true;
            this.textBoxLicense.Name = "textBoxLicense";
            this.textBoxLicense.ReadOnly = true;
            this.textBoxLicense.Size = new System.Drawing.Size(501, 435);
            this.textBoxLicense.TabIndex = 1;
            this.textBoxLicense.Text = resources.GetString("textBoxLicense.Text");
            // 
            // FormLicense
            // 
            this.AcceptButton = this.buttonAgree;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonDecline;
            this.ClientSize = new System.Drawing.Size(507, 481);
            this.ControlBox = false;
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormLicense";
            this.ShowIcon = false;
            this.Text = "SpyStudio License Agreement";
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.tableLayoutPanelBottom.ResumeLayout(false);
            this.tableLayoutPanelBottom.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelBottom;
        private System.Windows.Forms.TextBox textBoxLicense;
        private System.Windows.Forms.Button buttonDecline;
        private System.Windows.Forms.Button buttonAgree;
    }
}