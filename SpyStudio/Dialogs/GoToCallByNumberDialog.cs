using System;
using System.Globalization;
using System.Windows.Forms;
using SpyStudio.Forms;

namespace SpyStudio.Dialogs
{
    public partial class GoToCallByNumberDialog : Form
    {
        public class VerifiedNumericUpDown : NumericUpDown
        {
            protected override void OnTextBoxTextChanged(object source, EventArgs e)
            {
                base.OnTextBoxTextChanged(source, e);
                //if (Value > Maximum)
                //{
                //    Value = Maximum;
                //}
            }
        }

        public ulong Input
        {
            get { return Convert.ToUInt64(lineNumberBox.Value, CultureInfo.InvariantCulture); }
        }

        public GoToCallByNumberDialog()
        {
            InitializeComponent();

            lineNumberBox.Maximum = 1000000000000;
        }

        private void AcceptButtonClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void GoToCallByNumberDialogShown(object sender, EventArgs e)
        {
            lineNumberBox.Select(0, lineNumberBox.Text.Length);
            ActiveControl = lineNumberBox;
        }
    }
}
