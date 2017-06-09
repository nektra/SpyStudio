using System;

namespace Wizard.UI
{
    public partial class InternalWizardPage : WizardPage
    {
        protected InternalWizardPage()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call
        }

        protected InternalWizardPage(string aPageDescription)
        {
            InitializeComponent();
            Banner.Subtitle = aPageDescription;
        }
    }
}