using System.Collections.ObjectModel;
using System.Windows.Forms;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Main;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;

namespace SpyStudio.Tools
{
    class CallEventContainerTools
    {
        public static ListViewItem SelectItemContaining(ListView lv, CallEventId eventId)
        {
            foreach (ListViewItem item in lv.Items)
            {
                var eventContainer = item as IEntry;
                if (eventContainer != null)
                {
                    if (eventContainer.CallEventIds.Contains(eventId))
                    {
                        lv.BeginUpdate();
                        lv.SelectedItems.Clear();
                        item.Selected = true;
                        item.EnsureVisible();
                        lv.EndUpdate();
                        lv.Focus();
                        return item;
                    }
                }
            }
            return null;
        }
        public static Node SelectItemContaining(TreeViewAdv tv, CallEventId eventId)
        {
            return SelectItemContaining(tv, tv.Model.Nodes, eventId);
        }
        public static Node SelectItemContaining(TreeViewAdv tv, Node.NodeCollection nodes, CallEventId eventId)
        {
            foreach (Node n in nodes)
            {
                var eventContainer = n as IEntry;
                if (eventContainer != null)
                {
                    if (eventContainer.CallEventIds.Contains(eventId))
                    {
                        // try to find a child that contains the event id. If it doesn't exist -> this is the deepest
                        // node containing the event
                        var ret = SelectItemContaining(tv, n.Nodes, eventId);
                        if(ret == null)
                        {
                            tv.BeginUpdate();
                            tv.ClearSelection();
                            n.IsSelected = true;
                            tv.EndUpdate();
                            tv.Focus();
                            tv.EnsureVisible(n, TreeViewAdv.ScrollType.Middle);
                            ret = n;
                        }
                        return ret;
                    }
                }
            }
            return null;
        }
        static public void ShowIn(TabControl tabControlData, TabPage page, IInterpreter container, ITraceEntry anEntry)
        {
            var index = tabControlData.TabPages.IndexOf(page);
            if (index != -1)
            {
                tabControlData.SelectedIndex = index;
                var eventId = anEntry.EventId;
                if (eventId != null)
                    container.SelectItemContaining(eventId);
            }
        }
        static public void ShowInRegistry(TabControl tabControlData, TabPage page, RegistryTree treeViewRegistry, ITraceEntry anEntry)
        {
            ShowIn(tabControlData, page, treeViewRegistry, anEntry);
            if (anEntry.IsValue && treeViewRegistry.ValuesView != null)
            {
                treeViewRegistry.ValuesView.SelectItemContaining(anEntry.EventId);
            }
        }
    }
}
