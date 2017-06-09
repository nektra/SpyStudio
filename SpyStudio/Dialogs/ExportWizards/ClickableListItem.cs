using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpyStudio.Dialogs.ExportWizards
{
    public class ClickableListItem : ListViewItem
    {
        public Action OnClickAction { get; set; }

        public ClickableListItem(string aName) : base(aName)
        {
            OnClickAction = () => { };
        }
    }
}