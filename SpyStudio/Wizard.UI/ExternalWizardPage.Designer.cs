using System;
using System.Collections.Generic;
using System.Text;

namespace Wizard.UI
{
   partial class ExternalWizardPage
   {
      public Wizard.UI.WizardSidebar Sidebar;
      private System.ComponentModel.IContainer components = null;
      
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Sidebar = new Wizard.UI.WizardSidebar();
			this.SuspendLayout();
			// 
			// Sidebar
			// 
			this.Sidebar.Dock = System.Windows.Forms.DockStyle.Left;
			this.Sidebar.Location = new System.Drawing.Point(0, 0);
			this.Sidebar.Name = "Sidebar";
			this.Sidebar.Size = new System.Drawing.Size(165, 208);
			this.Sidebar.TabIndex = 0;
			// 
			// ExternalWizardPage
			// 
			this.BackColor = System.Drawing.SystemColors.Window;
			this.Controls.Add(this.Sidebar);
			this.Name = "ExternalWizardPage";
			this.Size = new System.Drawing.Size(424, 208);
			this.ResumeLayout(false);

		}
		#endregion
   }
}
