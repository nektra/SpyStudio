using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace SpyStudio.Dialogs
{
    public partial class ProgressDialog : Form
    {
        public event EventHandler CancelPressed;
        string _baseText = "";
        private readonly Stopwatch _lastUpdate;


        public ProgressDialog()
        {
            InitializeComponent();
            progressBar.Maximum = 100;
            progressBar.Minimum = 0;
            _lastUpdate = new Stopwatch();
        }

        public void SetWorker(BackgroundWorker worker)
        {
            worker.WorkerReportsProgress = true;

            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerCompleted +=WorkerRunWorkerCompleted;
        }

        public string Title
        {
            set { Text = value; }
            get { return Text; }
        }
        public string Message
        {
            set { if(value.Length > 30) value = value.Substring(0, 30) + "..."; _baseText = textBoxMessage.Text = value; }
            get { return _baseText; }
        }
        public Int32 Minimum
        {
            set { progressBar.Minimum = value; }
            get { return progressBar.Minimum; }
        }
        public Int32 Maximum
        {
            set { progressBar.Maximum = value; }
            get { return progressBar.Maximum; }
        }
        void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        void WorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Value = progressBar.Maximum;
            DialogResult = DialogResult.OK;
            Close();
        }
        public int Progress
        {
            get { return progressBar.Value; }
            set { progressBar.Value = value; Application.DoEvents(); }
        }
        public void SetDetailedProgress(int progress, long processedItems, long totalItems)
        {
            // avoid flickering
            if (!_lastUpdate.IsRunning || _lastUpdate.ElapsedMilliseconds > 500)
            {
                textBoxMessage.Text = (string.IsNullOrEmpty(_baseText) ? "" : _baseText + " ") + "(" + processedItems + " / " + totalItems + ")";
                _lastUpdate.Reset();
                _lastUpdate.Start();
            }
            Progress = progress;
        }
        public void SetDetailedProgress(int progress, int processedItems, int totalItems)
        {
            // avoid flickering
            if(!_lastUpdate.IsRunning || _lastUpdate.ElapsedMilliseconds > 500)
            {
                textBoxMessage.Text = (string.IsNullOrEmpty(_baseText) ? "" : _baseText + " ") + "(" + processedItems + " / " + totalItems + ")";
                _lastUpdate.Reset();
                _lastUpdate.Start();
            }
            Progress = progress;
        }
        private void ButtonCancelClick(object sender, EventArgs e)
        {
            if (CancelPressed != null)
                CancelPressed(this, e);
            DialogResult = DialogResult.Cancel;

            Close();
        }
        public void Cancel()
        {
            if (CancelPressed != null)
                CancelPressed(this, new EventArgs());
            DialogResult = DialogResult.Cancel;
            Close();
        }
        public bool Cancelled()
        {
            return (DialogResult == DialogResult.Cancel);
        }
    }
}
