using System.Windows.Forms;
using SpyStudio.Export.ThinApp;

namespace SpyStudio.Dialogs.ExportWizards
{
    public class VirtualPackageListItem : ClickableListItem
    {
        public IVirtualPackage VirtualPackage { get; protected set; }

        public static VirtualPackageListItem Containing(IVirtualPackage aPackage)
        {
            return new VirtualPackageListItem(aPackage);
        }

        public VirtualPackageListItem(string aName) :  base(aName)
        {
            
        }

        protected VirtualPackageListItem(IVirtualPackage aVirtualPackage)
            : base(aVirtualPackage != null ? aVirtualPackage.Name : "(null)")
        {
            VirtualPackage = aVirtualPackage;
            Name = aVirtualPackage != null ? aVirtualPackage.Name : "(null)";
        }
    }
}