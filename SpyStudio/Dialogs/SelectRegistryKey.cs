using System.Windows.Forms;

namespace SpyStudio.Dialogs
{
    public partial class SelectRegistryKey : Form
    {
        public SelectRegistryKey()
        {
            InitializeComponent();
        }

        public string KeyPath
        {
            get { return textBoxKeyPath.Text; }
        }

        private void ButtonOkClick(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonCancelClick(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
