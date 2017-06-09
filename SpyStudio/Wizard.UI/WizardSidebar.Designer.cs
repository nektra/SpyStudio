using System;
using System.Collections.Generic;
using System.Text;

namespace Wizard.UI
{
   partial class WizardSidebar
   {
      /// <summary> 
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.Container components = null;
      
      /// <summary> 
      /// Clean up any resources being used.
      /// </summary>
      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (components != null)
            {
               components.Dispose();
            }
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
         // 
         // WizardSidebar
         // 
         this.Name = "WizardSidebar";
         this.Size = new System.Drawing.Size(165, 320);
      }
      #endregion
   }
}
