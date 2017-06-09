using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace SpyStudio.Tools
{
    public static class Threading
    {
        public static BackgroundWorker ExecuteAsynchronously(Action anAction)
        {
            var worker = new BackgroundWorker();

            worker.DoWork += (a, b) => anAction();
            worker.WorkerSupportsCancellation = true;

            worker.RunWorkerAsync();

            return worker;
        }

        public static BackgroundWorker ExecuteAsynchronously(Action anAction, Action<RunWorkerCompletedEventArgs> onCompleteAction)
        {
            var worker = new BackgroundWorker();

            worker.DoWork += (a, b) => anAction();
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerCompleted += (a, b) => onCompleteAction(b);
            worker.RunWorkerAsync();

            return worker;
        }

        public static void WaitForCompletionOf(BackgroundWorker aWorker, bool processEvents)
        {
            while (aWorker.IsBusy)
            {
                Thread.Sleep(100);
                if(processEvents)
                    Application.DoEvents();
            }
        }
    }
}
