using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace SpyStudio.Extensions
{
    public static class NodeExtensions
    {
        public static void CheckSelfAndChildren(this Node aNode)
        {
            aNode.Checked = true;

            foreach (var node in aNode.Nodes)
                node.Checked = true;
        }

        public static void CheckSelfAndChildrenRecursively(this Node aNode)
        {
            aNode.Checked = true;

            foreach (var node in aNode.Nodes)
                node.CheckSelfAndChildrenRecursively();
        }

        public static bool UncheckSelfAndParentsWithoutCheckedChildrenIf(this Node aNode, bool shouldBeUnchecked)
        {
            if (shouldBeUnchecked)
                aNode.UncheckSelfAndParentsWithoutCheckedChildren();

            return shouldBeUnchecked;
        }

        public static bool CheckSelfAndParentsIf(this Node aNode, bool shouldBeChecked)
        {
            if (shouldBeChecked)
                aNode.CheckSelfAndParents();

            return shouldBeChecked;
        }

        public static bool CheckSelfAndParentsAndChildrenIf(this Node aNode, bool shouldBeChecked)
        {
            if (shouldBeChecked)
            {
                aNode.CheckSelfAndParents();
                aNode.CheckSelfAndChildrenRecursively();
            }

            return shouldBeChecked;
        }

        public static void CheckSelfAndParents(this Node aNode)
        {
            aNode.Checked = true;

            if (aNode.Parent != null)
                aNode.Parent.CheckSelfAndParents();
        }

        public static void UncheckSelfAndParentsWithoutCheckedChildren(this Node aNode)
        {
            aNode.Checked = false;

            var parent = aNode.Parent;
            
            while (parent != null)
            {
                if (parent.Nodes.All(n => !n.Checked))
                    parent.Checked = false;

                parent = parent.Parent;        
            }
        }

        public static void SetCheckStateForSelfAndDescendants(this Node node, bool state)
        {
            node.IsChecked = state;
            foreach (var child in node.Nodes)
                child.SetCheckStateForSelfAndDescendants(state);
        }
    }
}
