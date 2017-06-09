using System.ComponentModel;
using System.Windows.Forms;

namespace Wizard.UI
{
   /// <summary>
   /// This class implements the banner across the top of the wizard pages.
   /// </summary>
	public sealed partial class WizardBanner : UserControl
	{
		public WizardBanner()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Avoid getting the focus.
			SetStyle(ControlStyles.Selectable, false);
		}

		[Category("Appearance")]
		public string Title
		{
			get { return titleLabel.Text; }
			set { titleLabel.Text = value; }
		}

		[Category("Appearance")]
		public string Subtitle
		{
			get { return subtitleLabel.Text; }
			set { subtitleLabel.Text = value; }
		}
	}
}
