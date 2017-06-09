using System;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.Main;

namespace SpyStudio.Registry.Controls.Compare
{
    public class CompareRegistryTree : RegistryTree
    {
        protected override void UpdateContextMenu()
        {
            if (ContextMenuStrip == null) 
                return;

            ContextMenuStrip.Items.Clear();
            ContextMenuStrip.Items.Add(ExpandDiffsItem);
            
            if (CheckBoxes)
            {
                if (ContextMenuStrip.Items.Count > 0)
                    ContextMenuStrip.Items.Add(new ToolStripSeparator());

                ContextMenuStrip.Items.Add(CheckItem);
                ContextMenuStrip.Items.Add(UncheckItem);
                ContextMenuStrip.Items.Add(CheckWithChildrenItem);
                ContextMenuStrip.Items.Add(UncheckWithChildrenItem);
            }

            if (!(ContextMenuStrip.Items.Cast<ToolStripItem>().Last() is ToolStripSeparator))
                ContextMenuStrip.Items.Add(new ToolStripSeparator());
            if (EntryProperties != null)
                EntryProperties.Close(true);
            EntryProperties = new EntryContextMenu(this);
        }

        protected override void PopulateValuesView()
        {
            if (ValuesView == null)
                return;

            ValuesView.Items.Clear();

            var currentNode = SelectedNode;

            if (currentNode == null || currentNode == Model.Root)
                return;

            var regKey = (CompareRegistryTreeNode)currentNode;
            
            regKey.PopulateValueList(ValuesView, NextSelectedValue);
            
            NextSelectedValue = null;
        }

        protected override RegistryTreeNodeBase GenerateNodeNamed(string aName)
        {
            var newNode = CompareRegistryTreeNode.For(this);

            newNode.Text = aName;

            return newNode;
        }
    }
}