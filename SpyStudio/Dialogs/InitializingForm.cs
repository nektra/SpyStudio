using System.Windows.Forms;

namespace SpyStudio.Dialogs
{
    public partial class InitializingForm : Form
    {
        public InitializingForm()
        {
            InitializeComponent();
        }

        static private InitializingForm _form;
        static int _progress;
        private static Form _parent;

        //public static event EventHandler Hiding;

        static public void MainLoop()
        {
            _form = new InitializingForm();
            _form.ShowDialog(_parent);
        }
        static public void SetParent(Form parent)
        {
            _parent = parent;
        }
        private delegate void SetProgressDelegate(int progress);

        static public void SetProgress(int progress)
        {
            if (_form != null)
            {
                if (_form.InvokeRequired)
                {
                    _form.BeginInvoke(new SetProgressDelegate(SetProgress), progress);
                }
                else
                {
                    _progress = progress;
                    if (_form != null && !_form.InvokeRequired)
                        _form.progressBarDeviareInit.Value = _progress;
                    if (_progress == 100)
                        HideForm();
                }
            }
        }

        static public void ShowForm()
        {
            MainLoop();
        }

        static public void HideForm()
        {
            _form.Close();
        }
    }
}
