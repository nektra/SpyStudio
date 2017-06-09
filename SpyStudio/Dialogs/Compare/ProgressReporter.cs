using System.ComponentModel;
using System.Windows.Forms;

namespace SpyStudio.Dialogs.Compare
{
    public class ProgressReporter
    {
        public bool Success;
        public string Error;
        public BackgroundWorker Worker;
        public ProgressDialog ProgressDlg;
        public string Filename1;
        public string Filename2;

        public ProgressReporter()
        {
            Success = true;
            Error = "";
        }

        public void ReportProgress(int progress)
        {
            if (Worker != null)
            {
                Worker.ReportProgress(progress);
            }
            else
            {
                ProgressDlg.Progress = progress;
                if (progress == 100)
                {
                    ProgressDlg.DialogResult = DialogResult.OK;
                    ProgressDlg.Close();
                }
                Application.DoEvents();
            }
        }

        public void ReportProgress(int progress, int itemsProcessed, int totalItems)
        {
            if (Worker != null)
            {
                Worker.ReportProgress(progress);
            }
            else
            {
                ProgressDlg.SetDetailedProgress(progress, itemsProcessed, totalItems);
                if (progress == 100)
                {
                    ProgressDlg.DialogResult = DialogResult.OK;
                    ProgressDlg.Close();
                }
                Application.DoEvents();
            }
        }

        public void Finish(DialogResult res)
        {
            if (Worker == null)
            {
                ProgressDlg.Progress = 100;
                ProgressDlg.DialogResult = res;
                ProgressDlg.Close();
                Application.DoEvents();
            }
        }

        public bool CancellationPending
        {
            get { return (Worker != null ? Worker.CancellationPending : ProgressDlg.Cancelled()); }
        }
    }
}