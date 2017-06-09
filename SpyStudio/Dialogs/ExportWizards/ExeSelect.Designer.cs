using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    partial class ExeSelect
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.MainPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ExeFilesView = new SpyStudio.FileSystem.FileSystemViewer();
            this.ExeFilesViewLabel = new System.Windows.Forms.Label();
            this.MainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(580, 64);
            this.Banner.Subtitle = "Select the main executables for the application you want to include in this packa" +
                "ge.";
            this.Banner.Title = "Smartcheck";
            // 
            // MainPanel
            // 
            this.MainPanel.ColumnCount = 1;
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainPanel.Controls.Add(this.ExeFilesView, 0, 1);
            this.MainPanel.Controls.Add(this.ExeFilesViewLabel, 0, 0);
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 64);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.RowCount = 2;
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainPanel.Size = new System.Drawing.Size(580, 351);
            this.MainPanel.TabIndex = 1;
            // 
            // ExeFilesView
            // 
            this.ExeFilesView.BackColor = System.Drawing.SystemColors.Window;
            this.ExeFilesView.CompareMode = false;
            this.ExeFilesView.Controller = null;
            this.ExeFilesView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ExeFilesView.Location = new System.Drawing.Point(3, 23);
            this.ExeFilesView.Name = "ExeFilesView";
            this.ExeFilesView.PathNormalizer = null;
            this.ExeFilesView.ShowIsolationOptions = false;
            this.ExeFilesView.Size = new System.Drawing.Size(574, 325);
            this.ExeFilesView.TabIndex = 0;
            this.ExeFilesView.Text = "fileSystemTree1";
            // 
            // ExeFilesViewLabel
            // 
            this.ExeFilesViewLabel.AutoSize = true;
            this.ExeFilesViewLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ExeFilesViewLabel.Location = new System.Drawing.Point(3, 0);
            this.ExeFilesViewLabel.Name = "ExeFilesViewLabel";
            this.ExeFilesViewLabel.Size = new System.Drawing.Size(574, 20);
            this.ExeFilesViewLabel.TabIndex = 1;
            this.ExeFilesViewLabel.Text = "Executable files found:";
            this.ExeFilesViewLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ExeSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainPanel);
            this.Name = "ExeSelect";
            this.Size = new System.Drawing.Size(580, 415);
            this.SetActive += new WizardPageEventHandler(this.ExeSelectSetActive);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.ExeSelectWizardNext);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.MainPanel, 0);
            this.MainPanel.ResumeLayout(false);
            this.MainPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel MainPanel;
        private System.Windows.Forms.Label ExeFilesViewLabel;
        protected SpyStudio.FileSystem.FileSystemViewer ExeFilesView;
    }
}
