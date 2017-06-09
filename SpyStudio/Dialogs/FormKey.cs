using System;
using System.Windows.Forms;

namespace SpyStudio.Dialogs
{
    public partial class FormKey : Form
    {
        public FormKey()
        {
            InitializeComponent();
        }
        
        public string Key
        {
            get { return _textBoxKey.Text; }
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
