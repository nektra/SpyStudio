using System;
using System.Windows.Forms;
using SpyStudio.Hooks;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Dialogs
{
    public partial class ProcessProperties : Form
    {
        private readonly uint _pid;
        private readonly HookMgr _hookMgr;
        public ProcessProperties(uint pid, ProcessInfo procInfo, HookMgr hookMgr)
        {
            InitializeComponent();
            _pid = pid;
            textBoxProcName.Text = procInfo.GetName(pid);
            textBoxProcPath.Text = procInfo.GetPath(pid);

            _hookMgr = hookMgr;
            _hookMgr.ConnectListView(listViewHooks, pid);
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ProcessPropertiesFormClosed(object sender, FormClosedEventArgs e)
        {
            _hookMgr.DisconnectListView(listViewHooks, _pid);
        }

        private void CopyToolStripMenuItemClick(object sender, EventArgs e)
        {
            ListViewTools.CopySelectionToClipboard(listViewHooks);
        }

        private void SelectAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            ListViewTools.SelectAll(listViewHooks);
            listViewHooks.Focus();
        }
    }
}
