using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SpyStudio.Export.Templates;

namespace SpyStudio.Dialogs.ExportWizards
{
    public class TemplateListItem : ListViewItem
    {
        public TemplateInfo TemplateInfo { get; protected set; }

        public static TemplateListItem For(TemplateInfo aTemplateInfo)
        {
            var item = new TemplateListItem
                {
                    TemplateInfo = aTemplateInfo,
                    Name = aTemplateInfo.ID,
                    Text = aTemplateInfo.Name
                };

            item.SubItems.Add(item.TemplateInfo.Description);
            item.SubItems.Add(item.TemplateInfo.Version);
            item.SubItems.Add(item.TemplateInfo.ReleaseDate.ToShortDateString());

            item.UpdateAppearence();

            return item;
        }

        public void MergeInfoWith(TemplateInfo aTemplateInfo)
        {
            TemplateInfo.MergeWith(aTemplateInfo);
        }

        public void UpdateAppearence()
        {
            ForeColor = TemplateInfo.IsAvailable ? Color.Black : Color.DarkGray;
        }
    }
}