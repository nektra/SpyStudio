using System.ComponentModel;
using System.Windows.Forms;
using System.Linq;

namespace Wizard.UI
{
	[DefaultEvent("SetActive")]
	public partial class WizardPage : UserControl
	{
        public class KeyPressedEventArgs
        {
            public KeyPressedEventArgs(Keys keyData)
            {
                KeyData = keyData;
                Handled = false;
            }

            public Keys KeyData { get; set; }
            public bool Handled { get; set; }
        }
        public delegate void KeyPressedEventHandler(object sender, KeyPressedEventArgs e);

	    protected WizardPage()
		{
			InitializeComponent();
		}

	    [Category("Appearance")]
	    public string Caption { get; set; }

	    protected WizardSheet GetWizard()
		{
			var wizard = (WizardSheet)ParentForm;
			return wizard;
		}

		protected void SetWizardButtons(WizardButtons buttons)
		{
			GetWizard().SetWizardButtons(buttons);
		}

		protected void EnableCancelButton(bool enableCancelButton)
		{
			GetWizard().EnableCancelButton(enableCancelButton);
		}
        protected void EnableFinishButton(bool enable)
        {
            GetWizard().EnableFinishButton(enable);
        }
        protected void EnableNextButton(bool enable)
        {
            GetWizard().EnableNextButton(enable);
        }

		protected void PressButton(WizardButtons buttons)
		{
			GetWizard().PressButton(buttons);
		}

		[Category("Wizard")]
		public event WizardPageEventHandler SetActive;

	    public WizardButtons AvailableButtons
	    {
	        get
	        {
                if (GetWizard().Pages.Last() == this) // is last page
                    return WizardButtons.Back | WizardButtons.Finish;

	            return WizardButtons.Back | WizardButtons.Next;
	        }
	    }
	

		public virtual void OnSetActive(WizardPageEventArgs e)
		{
            SetWizardButtons(AvailableButtons);

            Focus();

			if (SetActive != null)
				SetActive(this, e);
		}

		[Category("Wizard")]
		public event WizardPageEventHandler WizardNext;

		public virtual void OnWizardNext(WizardPageEventArgs e)
		{
			if (WizardNext != null)
				WizardNext(this, e);
		}

		[Category("Wizard")]
		public event WizardPageEventHandler WizardBack;

		public virtual void OnWizardBack(WizardPageEventArgs e)
		{
			if (WizardBack != null)
				WizardBack(this, e);
		}

		[Category("Wizard")]
		public event CancelEventHandler WizardFinish;

		public virtual void OnWizardFinish(CancelEventArgs e)
		{
			if (WizardFinish != null)
				WizardFinish(this, e);
		}

		[Category("Wizard")]
		public event CancelEventHandler QueryCancel;
        [Category("Wizard")]
        public event KeyPressedEventHandler KeyPressed;

		public virtual void OnQueryCancel(CancelEventArgs e)
		{
			if (QueryCancel != null)
				QueryCancel(this, e);
		}

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var handled = false;
            if(KeyPressed != null)
            {
                var args = new KeyPressedEventArgs(keyData);
                KeyPressed(this, args);
                handled = args.Handled;
            }
            if(!handled)
                handled = base.ProcessCmdKey(ref msg, keyData);
            return handled;
        }
	}
}
