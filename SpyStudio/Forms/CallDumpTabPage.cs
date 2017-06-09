using System;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.Dialogs;
using SpyStudio.Main;

namespace SpyStudio.Forms
{
    public abstract partial class CallDumpTabPage : TabPage
    {
        public abstract void GoToClicked();

        public virtual bool CanDoGoTo
        { 
            get { return false; }
        }
    }

    class FilesTab : CallDumpTabPage
    {
        public override void GoToClicked()
        {
            throw new NotImplementedException();
        }
    }

    class WindowTab : CallDumpTabPage
    {
        public override void GoToClicked()
        {
            throw new NotImplementedException();
        }
    }

    class RegistryTab : CallDumpTabPage
    {
        public override void GoToClicked()
        {
            throw new NotImplementedException();
        }
    }

    class ComTab : CallDumpTabPage
    {
        public override void GoToClicked()
        {
            throw new NotImplementedException();
        }
    }

    public class TraceTab : CallDumpTabPage
    {
        public override void GoToClicked()
        {
            var goToCallByNumberDialog = new GoToCallByNumberDialog();
            goToCallByNumberDialog.ShowDialog(this);

            if (goToCallByNumberDialog.DialogResult == DialogResult.OK)
                GoToCallNumber(goToCallByNumberDialog.Input);
        }

        private void GoToCallNumber(ulong aCallNumber)
        {
            var traceTreeView = Tag as TraceTreeView;
            if(traceTreeView != null)
                traceTreeView.SelectAndEnsureVisibleCall(aCallNumber);
        }

        public override bool CanDoGoTo
        {
            get { return true; }
        }
    }

}
