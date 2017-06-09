using System;
using System.Windows.Forms;
using SpyStudio.Properties;

namespace SpyStudio.Dialogs
{
    public partial class FormLicense : Form
    {
        public FormLicense()
        {
            InitializeComponent();
            SetCommercialVersion(true);
        }

        public void SetCommercialVersion(bool commercial)
        {
            textBoxLicense.Text = commercial ? 
                Resources.LicenseCommercial : Resources.LicenseNonCommercial;
        }

        private void ButtonAgreeClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
