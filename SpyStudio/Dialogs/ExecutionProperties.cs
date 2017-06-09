using System;
using System.IO;
using System.Windows.Forms;
using SpyStudio.Properties;

namespace SpyStudio.Dialogs
{
    public partial class ExecutionProperties : Form
    {
        public enum ShowDialog2Result
        {
            SaveAndExec,
            Save,
            Cancel
        }

        private ShowDialog2Result _result;

        public string ProgramPath
        {
            get { return tbExecPath.Text; }
            set { tbExecPath.Text = value; }
        }
        public string Parameters
        {
            get { return tbParameters.Text; }
        }
        public string User
        {
            get { return tbUser.Text; }
        }
        public string Password
        {
            get { return tbPassword.Text; }
        }

        public ExecutionProperties(string parameters, string user, string password)
        {
            InitializeComponent();
            _result = ShowDialog2Result.Cancel;
            tbParameters.Text = parameters;
            tbUser.Text = user;
            tbPassword.Text = password;
        }

        public ShowDialog2Result ShowDialog(Form owner)
        {
            base.ShowDialog(owner);
            return _result;
        }

        private void ButtonExecClick(object sender, EventArgs e)
        {
            _result = ShowDialog2Result.SaveAndExec;
            Close();
        }

        private void ButtonSaveClick(object sender, EventArgs e)
        {
            _result=ShowDialog2Result.Save;
            Close();
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            _result=ShowDialog2Result.Cancel;
            Close();
        }

        private void ButtonBrowseClick(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                DefaultExt = "exe",
                Filter = "Executable file (*.exe)|*.exe|All files (*.*)|*.*",
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Execute"
            };
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedAppFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedAppFolder;

            if (openDlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Settings.Default.PathLastSelectedAppFolder = Path.GetDirectoryName(openDlg.FileName);
                    Settings.Default.Save();
                }
                catch (Exception)
                {
                }

                tbExecPath.Text = openDlg.FileName;
                //UpdateButtonsState();
            }
        }
    }
}
