using System.Drawing;
using System.Windows.Forms;

namespace Wizard.UI
{
	/// <summary>
	/// The wizard side bar is used for the welcome and complete pages.
	/// </summary>
	public sealed partial class WizardSidebar : UserControl
	{
		public WizardSidebar()
		{
			Dock = DockStyle.Left;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Set a default image.
			Bitmap image = new Bitmap(GetType(), "Bitmaps.ExampleSidebar.bmp");
			BackgroundImage = image;

			// Avoid getting the focus.
			SetStyle(ControlStyles.Selectable, false);
		}
	}
}
