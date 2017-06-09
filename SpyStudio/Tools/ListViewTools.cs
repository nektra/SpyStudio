using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using Aga.Controls;

namespace SpyStudio.Tools
{
    class ListViewTools
    {
        static public bool Find(ListView lv, FindEventArgs e)
        {
            bool found = false;
            int curIndex = -1;
            ListViewItem item = null;

            if (lv.SelectedItems.Count > 0)
            {
                item = lv.SelectedItems[0];
                curIndex = item.Index;

                if (e.SearchDown)
                    curIndex++;
                else
                    curIndex--;
            }
            else if (lv.Items.Count > 0)
            {
                curIndex = (e.SearchDown ? 0 : lv.Items.Count - 1);
            }

            if (curIndex >= 0 && curIndex < lv.Items.Count)
            {
                item = lv.Items[curIndex];
            }

            while (item != null)
            {
                for (int i = 0; i < item.SubItems.Count; i++)
                {
                    var subItem = item.SubItems[i];
                    if (StringHelpers.MatchString(subItem.Text, e.Text, e))
                    {
                        lv.SelectedItems.Clear();
                        item.Selected = true;
                        found = true;
                        break;
                    }
                }
                if (found)
                    break;
                if (e.SearchDown)
                    curIndex++;
                else
                    curIndex--;

                item = null;
                if (curIndex > 0 && curIndex < lv.Items.Count)
                {
                    item = lv.Items[curIndex];
                }
            }
            if (lv.SelectedItems.Count > 0)
            {
                lv.EnsureVisible(lv.SelectedIndices[0]);
            }
            return found;
        }
        static public void CopySelectionToClipboard(ListView lv)
        {
            CopySelectionToClipboard(lv, false, false);
        }
        static public void CopySelectionToClipboard(ListView lv, bool separateBars)
        {
            CopySelectionToClipboard(lv, false, separateBars);
        }
        static public void SelectAll(ListView lv)
        {
            foreach (ListViewItem item in lv.Items)
            {
                item.Selected = true;
            }
        }
        static public void CopySelectionToClipboard(ListView lv, bool addGroups, bool separateBars)
        {
            var tempStr = new StringBuilder("");
            ListViewGroup currentGroup = null;
            foreach (ListViewItem item in lv.SelectedItems)
            {
                if(addGroups && currentGroup != item.Group)
                {
                    currentGroup = item.Group;
                    tempStr.Append(item.Group.Name + "\r\n");
                }
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                for (int i = 0; i < item.SubItems.Count; i++)
                {
                    var subItem = item.SubItems[i];
                    if(separateBars && subItem.Text.Contains(" / "))
                    {
                        var index = subItem.Text.IndexOf(" / ", StringComparison.Ordinal);
                        Debug.Assert(index != -1);
                        var first = subItem.Text.Substring(0, index);
                        var second = subItem.Text.Substring(index + 3);
                        tempStr.Append(first);
                        tempStr.Append("\t");
                        tempStr.Append(second);
                        tempStr.Append("\t");
                    }
                    else
                    {
                        tempStr.Append(subItem.Text);
                        tempStr.Append("\t");
                    }
                }
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
        }
    }
}
