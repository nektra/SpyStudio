//#define DISABLE_STATEPAGE
namespace SpyStudio.Dialogs.ExportWizards
{
#if !DISABLE_STATEPAGE
    partial class TemplateSelect
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
            this.browseButton = new System.Windows.Forms.Button();
            this.TemplatesPanel = new System.Windows.Forms.TableLayoutPanel();
            this.TemplateList = new System.Windows.Forms.ListView();
            this.ApplicationHeader = new System.Windows.Forms.ColumnHeader();
            this.DescriptionHeader = new System.Windows.Forms.ColumnHeader();
            this.VersionHeader = new System.Windows.Forms.ColumnHeader();
            this.ReleaseDateHeader = new System.Windows.Forms.ColumnHeader();
            this.TemplatesLabel = new System.Windows.Forms.Label();
            this.MainPanel = new System.Windows.Forms.TableLayoutPanel();
            this.TemplatesPanel.SuspendLayout();
            this.MainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(637, 64);
            this.Banner.Subtitle = "Choose a virtualization template.";
            this.Banner.Title = "Templates";
            // 
            // browseButton
            // 
            this.browseButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.browseButton.Location = new System.Drawing.Point(456, 305);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(132, 23);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "Add From File";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButtonClick);
            // 
            // TemplatesPanel
            // 
            this.TemplatesPanel.ColumnCount = 1;
            this.TemplatesPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TemplatesPanel.Controls.Add(this.browseButton, 0, 2);
            this.TemplatesPanel.Controls.Add(this.TemplateList, 0, 1);
            this.TemplatesPanel.Controls.Add(this.TemplatesLabel, 0, 0);
            this.TemplatesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TemplatesPanel.Location = new System.Drawing.Point(23, 67);
            this.TemplatesPanel.Name = "TemplatesPanel";
            this.TemplatesPanel.RowCount = 3;
            this.TemplatesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TemplatesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TemplatesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.TemplatesPanel.Size = new System.Drawing.Size(591, 331);
            this.TemplatesPanel.TabIndex = 3;
            // 
            // TemplateList
            // 
            this.TemplateList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ApplicationHeader,
            this.DescriptionHeader,
            this.VersionHeader,
            this.ReleaseDateHeader});
            this.TemplateList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TemplateList.FullRowSelect = true;
            this.TemplateList.Location = new System.Drawing.Point(3, 23);
            this.TemplateList.MultiSelect = false;
            this.TemplateList.Name = "TemplateList";
            this.TemplateList.Size = new System.Drawing.Size(585, 276);
            this.TemplateList.TabIndex = 3;
            this.TemplateList.UseCompatibleStateImageBehavior = false;
            this.TemplateList.View = System.Windows.Forms.View.Details;
            // 
            // ApplicationHeader
            // 
            this.ApplicationHeader.Text = "Application";
            this.ApplicationHeader.Width = 122;
            // 
            // DescriptionHeader
            // 
            this.DescriptionHeader.Text = "Description";
            this.DescriptionHeader.Width = 288;
            // 
            // VersionHeader
            // 
            this.VersionHeader.Text = "Version";
            this.VersionHeader.Width = 78;
            // 
            // ReleaseDateHeader
            // 
            this.ReleaseDateHeader.Text = "Release Date";
            this.ReleaseDateHeader.Width = 93;
            // 
            // TemplatesLabel
            // 
            this.TemplatesLabel.AutoSize = true;
            this.TemplatesLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TemplatesLabel.Location = new System.Drawing.Point(3, 0);
            this.TemplatesLabel.Name = "TemplatesLabel";
            this.TemplatesLabel.Size = new System.Drawing.Size(585, 20);
            this.TemplatesLabel.TabIndex = 4;
            this.TemplatesLabel.Text = "Templates:";
            this.TemplatesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MainPanel
            // 
            this.MainPanel.ColumnCount = 3;
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainPanel.Controls.Add(this.TemplatesPanel, 1, 1);
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.RowCount = 2;
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainPanel.Size = new System.Drawing.Size(637, 401);
            this.MainPanel.TabIndex = 4;
            // 
            // StatePage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainPanel);
            this.Name = "TemplateSelect";
            this.Size = new System.Drawing.Size(637, 401);
            this.Load += new System.EventHandler(this.OnLoad);
            this.Controls.SetChildIndex(this.MainPanel, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.TemplatesPanel.ResumeLayout(false);
            this.TemplatesPanel.PerformLayout();
            this.MainPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TableLayoutPanel TemplatesPanel;
        private System.Windows.Forms.TableLayoutPanel MainPanel;
        private System.Windows.Forms.ListView TemplateList;
        private System.Windows.Forms.Label TemplatesLabel;
        private System.Windows.Forms.ColumnHeader ApplicationHeader;
        private System.Windows.Forms.ColumnHeader DescriptionHeader;
        private System.Windows.Forms.ColumnHeader VersionHeader;
        private System.Windows.Forms.ColumnHeader ReleaseDateHeader;

    }
#endif
}