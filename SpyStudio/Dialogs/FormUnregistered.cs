using System;
using System.Windows.Forms;

namespace SpyStudio.Dialogs
{
    public partial class FormUnregistered : Form
    {
        Timer _timer = new Timer();
        public FormUnregistered(int time)
        {
            InitializeComponent();

            _timer.Interval = time;
            _timer.Enabled = true;
            _timer.Tick += TimeExpired;
            _timer.Start();

            Closed += OnClosed;
        }

        private void OnClosed(object sender, EventArgs args)
        {
            _timer.Tick -= TimeExpired;
            _timer.Stop();
        }

        private void TimeExpired(object sender, EventArgs e)
        {
            Close();
        }
    }
}
