using System.ComponentModel;

namespace Wizard.UI
{
	public class WizardPageEventArgs : CancelEventArgs
	{
        public string NewPage { get; set; }
	    public string PreviousPage { get; set; }

	    public WizardPageEventArgs()
        {
            
        }

        public bool IsBackActionIn(WizardSheet aWizard)
        {
            return PreviousPage != null &&
                   aWizard.Pages.IndexOf(aWizard.FindPage(PreviousPage)) > aWizard.Pages.IndexOf(aWizard.FindPage(NewPage));
        }
	}

	public delegate void WizardPageEventHandler(object sender, WizardPageEventArgs e);
}
