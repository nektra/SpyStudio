using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;

namespace SpyStudio.Tools
{
    internal class TreeNodeAdvTools
    {
        public class FolderItemSorter : IComparer<Node>
        {
            //private string _mode;
            private readonly SortOrder _order = SortOrder.Ascending;

            public FolderItemSorter(SortOrder order)
            {
                _order = order;
            }

            public int Compare(Node a, Node b)
            {
                Debug.Assert(a != null, "a != null");
                Debug.Assert(b != null, "b != null");
                var res = CultureInfo.CurrentCulture.CompareInfo.Compare(a.Text, b.Text, CompareOptions.IgnoreCase);

                if (_order == SortOrder.Descending)
                    return -res;
                return res;
            }
        }

        public interface ITreeViewAdvComparableNode
        {
            string Text { get; }
            string Version { get; }
            string CompareCache { get; set; }
        }

        public class FileSystemTreeItemSorter : IComparer<Node>
        {
            //private string _mode;
            private readonly SortOrder _order = SortOrder.Ascending;

            public FileSystemTreeItemSorter(SortOrder order)
            {
                _order = order;
            }

            public bool SortByVersion { get; set; }

            public int Compare(Node x, Node y)
            {
                var a = x as ITreeViewAdvComparableNode;
                var b = y as ITreeViewAdvComparableNode;

                Debug.Assert(a != null, "a != null");
                Debug.Assert(b != null, "b != null");

                int res;

                if (SortByVersion)
                {
                    if (string.IsNullOrEmpty(a.CompareCache))
                        a.CompareCache = a.Version + " " + a.Text;
                    if (string.IsNullOrEmpty(b.CompareCache))
                        b.CompareCache = b.Version + " " + b.Text;
                    res = CultureInfo.CurrentCulture.CompareInfo.Compare(a.CompareCache, b.CompareCache,
                                                                         CompareOptions.IgnoreCase);
                }
                else
                {
                    res = CultureInfo.CurrentCulture.CompareInfo.Compare(a.Text, b.Text, CompareOptions.IgnoreCase);
                }

                if (_order == SortOrder.Descending)
                    return -res;
                return res;
            }
        }

        //public static Node GetFirstChild(Node node)
        //{
        //    if (node.Nodes.Count > 0)
        //        return node.Nodes[0];

        //    var nextNode = 
        //    // force children creation for on demand trees.
        //    if (node.Tree.LoadOnDemand)
        //    {
        //        var children = GetChildren(node.Tree, (Node) node.Node);
        //        if (children.Any())
        //        {
        //            node.Tree.BeginUpdate();
        //            node.IsExpanded = true;
        //            node.Tree.EndUpdate();
        //            if (node.Children.Count > 0)
        //                return node.Children[0];
        //        }
        //    }
        //    return null;
        //}

        //public static TreeNodeAdv GetLastChild(TreeNodeAdv node)
        //{
        //    if (node.Children.Count > 0)
        //        return node.Children.Last();
        //    // force children creation for on demand trees.
        //    if (node.Tree.LoadOnDemand)
        //    {
        //        var children = GetChildren(node.Tree, (Node) node.Node);
        //        if (children.Any())
        //        {
        //            node.Tree.BeginUpdate();
        //            node.IsExpanded = true;
        //            node.Tree.EndUpdate();
        //            if (node.Children.Count > 0)
        //                return node.Children.Last();
        //        }
        //    }
        //    return null;
        //}

        //public static TreeNodeAdv GetNextNode(TreeNodeAdv curNode)
        //{
        //    // go to current children
        //    var child = GetFirstChild(curNode);
        //    if (child != null)
        //        curNode = child;
        //        // go to next child
        //    else if (curNode.NextNode != null)
        //        curNode = curNode.NextNode;
        //        // go to parent next node or parent parent next ...
        //    else
        //    {
        //        var parentNode = curNode.Parent;
        //        while (parentNode != null && parentNode.NextNode == null)
        //        {
        //            parentNode = parentNode.Parent;
        //        }
        //        curNode = parentNode != null 
        //            ? parentNode.NextNode 
        //            : null;
        //    }
        //    return curNode;
        //}

        //public static TreeNodeAdv GetPreviousNode(TreeNodeAdv curNode)
        //{
        //    if (curNode.PreviousNode == null && curNode.Parent == null)
        //    {
        //        curNode = null;
        //    }
        //    else
        //    {
        //        if (curNode.PreviousNode != null)
        //        {
        //            curNode = curNode.PreviousNode;
        //            var lastChild = GetLastChild(curNode);
        //            while (lastChild != null)
        //            {
        //                curNode = lastChild;
        //                lastChild = GetLastChild(lastChild);
        //            }
        //        }
        //        else
        //        {
        //            curNode = curNode.Parent != curNode.Tree.Root ? curNode.Parent : null;
        //        }
        //    }
        //    return curNode;
        //}

        //public static TreeNodeAdv GetNextVisibleNode(TreeNodeAdv curNode)
        //{
        //    // search for the first visible level up
        //    if (!IsRootOrNull(curNode.Parent) && !curNode.Parent.IsExpanded)
        //    {
        //        while (!IsRootOrNull(curNode.Parent) && !curNode.Parent.IsExpanded)
        //            curNode = curNode.Parent;
        //        return curNode;
        //    }

        //    // go to current children
        //    if (curNode.IsExpanded)
        //    {
        //        Debug.Assert(curNode.Children.Count != 0);
        //        curNode = curNode.Children[0];
        //    }
        //    else if (curNode.NextNode != null)
        //        // go to next child
        //        curNode = curNode.NextNode;
        //        // go to parent next node or parent parent next ...
        //    else
        //    {
        //        var parentNode = curNode.Parent;
        //        while (parentNode != null && parentNode.NextNode == null)
        //        {
        //            parentNode = parentNode.Parent;
        //        }
        //        curNode = parentNode != null 
        //            ? parentNode.NextNode 
        //            : null;
        //    }
        //    return curNode;
        //}

        //public static TreeNodeAdv GetPreviousVisibleNode(TreeNodeAdv curNode)
        //{
        //    // search for the first visible level up
        //    if (!IsRootOrNull(curNode.Parent) && !curNode.Parent.IsExpanded)
        //    {
        //        while (!IsRootOrNull(curNode.Parent) && !curNode.Parent.IsExpanded)
        //            curNode = curNode.Parent;
        //        return curNode;
        //    }

        //    if (curNode.PreviousNode == null && curNode.Parent == null)
        //    {
        //        curNode = null;
        //    }
        //    else
        //    {
        //        if (curNode.PreviousNode != null)
        //        {
        //            curNode = curNode.PreviousNode;
        //            while (curNode.IsExpanded)
        //            {
        //                curNode = curNode.Children.Last();
        //            }
        //        }
        //        else
        //        {
        //            curNode = curNode.Parent != curNode.Tree.Root ? curNode.Parent : null;
        //        }
        //    }
        //    return curNode;
        //}

        //private static bool IsRootOrNull(TreeNodeAdv aNode)
        //{
        //    return aNode == null || aNode.Parent == null;
        //}

        public static Node GetFirstNodeToSearch(TreeViewAdv tree, bool startTop, bool useSelection)
        {
            if (tree.SelectedNode != null)
                return tree.SelectedNode;

            if (tree.Model.Root.Nodes.Any())
                return startTop ? tree.Model.Root.Nodes[0] : LastNode(tree);

            return null;
        }

        public static Node LastNode(TreeViewAdv tree)
        {
            var curNode = tree.Model.Root;

            while (curNode.Nodes.Any())
                curNode = curNode.Nodes.Last();

            return curNode;
        }

        public static int GetLevel(Node n)
        {
            var level = 0;
            n = n.Parent;
            while (n != null)
            {
                n = n.Parent;
                level++;
            }
            // do not count root as 1
            return (level - 1);
        }

        public static bool IsAncestor(Node child, Node ancestor)
        {
            child = child.Parent;
            while (ancestor != child && child != null)
            {
                child = child.Parent;
            }
            return (child != null);
        }

        public static bool IsAncestor(Node child, List<RegistryTreeNode> ancestors)
        {
            return ancestors.Any(k => IsAncestor(child, k));
        }

        public static void SelectAllNodes(TreeViewAdv tree)
        {
            tree.SelectNodes(tree.Model.Root.Nodes);
        }

        public static void SelectFirstNode(TreeViewAdv tree)
        {
            if (tree.Model.Root.Nodes.Count > 0)
            {
                tree.Model.Root.Nodes[0].IsSelected = true;
            }
        }

        public static void SelectLastNode(TreeViewAdv tree)
        {
            if (tree.Model.Root.Nodes.Count > 0)
                tree.Model.Root.Nodes[tree.Model.Root.Nodes.Count - 1].IsSelected = true;
        }

        public static void CheckNodes(Node n, bool checkState)
        {
            if (n.IsChecked != checkState)
            {
                n.CheckState = (checkState ? CheckState.Checked : CheckState.Unchecked);
            }
        }

        /// <summary>
        /// Chech node list
        /// </summary>
        /// <param name="nodes"> </param>
        /// <param name="checkState">new check state</param>
        /// <param name="recursive">if true the action is applied to children, grand children and so</param>
        public static void CheckNodes(ReadOnlyCollection<Node> nodes, bool checkState, bool recursive)
        {
            foreach (var node in nodes)
            {
                CheckNode(node, checkState, recursive);
            }
        }

        public static void CheckNode(Node n, bool checkState, bool recursive)
        {
            if (n.IsChecked != checkState)
            {
                n.CheckState = (checkState ? CheckState.Checked : CheckState.Unchecked);
            }
            if (recursive)
            {
                foreach (var c in n.Nodes)
                {
                    CheckNode(c, checkState, true);
                }
            }
        }

        public static void ClearSelection(TreeViewAdv tree)
        {
            if (tree.SelectedNodes.Count > 0)
            {
                var selNodes = new Node[tree.SelectedNodes.Count];
                tree.SelectedNodes.CopyTo(selNodes, 0);

                foreach (var n in selNodes)
                {
                    n.IsSelected = false;
                }
            }
        }

        public static void SelectNextNode(Node aNode, TreeViewAdv aTreeView)
        {
            // If this is the last entry, the next extry to select is itself (in case of selection being changed manually)
            var nodeToSelect = aTreeView.GetNextNode(aNode) ?? aNode;

            if(nodeToSelect != aTreeView.Model.Root)
            {
                aTreeView.ClearSelection();
                nodeToSelect.IsSelected = true;
                aTreeView.EnsureVisible(nodeToSelect);
            }
        }

        public static void SelectPreviousNode(Node aNode, TreeViewAdv aTreeView)
        {
            // If this is the last entry, the previous extry to select is itself (in case of selection being changed manually)
            var nodeToSelect = aTreeView.GetPreviousNode(aNode) ?? aNode;
            //var nodeToSelect = GetPreviousNode(nodeToDeselect) ?? nodeToDeselect;

            if (nodeToSelect != aTreeView.Model.Root)
            {
                aTreeView.ClearSelection();
                nodeToSelect.IsSelected = true;
                aTreeView.EnsureVisible(nodeToSelect);
            }
        }

        public static void SelectNextVisibleNode(Node aNode, TreeViewAdv aTreeView)
        {
            // If this is the last entry, the next extry to select is itself (in case of selection being changed manually)
            var nodeToSelect = aTreeView.GetNextVisibleNode(aNode) ?? aNode;

            if (nodeToSelect != aTreeView.Model.Root)
            {
                aTreeView.ClearSelection();
                nodeToSelect.IsSelected = true;
                aTreeView.EnsureVisible(nodeToSelect);
            }
        }

        public static void SelectPreviousVisibleNode(Node aNode, TreeViewAdv aTreeView)
        {
            // If this is the last entry, the previous extry to select is itself (in case of selection being changed manually)
            var nodeToSelect = aTreeView.GetPreviousVisibleNode(aNode) ?? aNode;

            if (nodeToSelect != aTreeView.Model.Root)
            {
                aTreeView.ClearSelection();
                nodeToSelect.IsSelected = true;
                aTreeView.EnsureVisible(nodeToSelect);
            }
        }

        public static bool IsRoot(Node aNode)
        {
            return aNode.Parent == null;
        }


        public static Node GetNextModelNodeInView(Node aNode, TreeViewAdv aTreeView)
        {
            // go to current children
            var children = aNode.Nodes;
            if (children.Any())
                return children.First();
            // go to next child

            var nextSibling = GetNextSiblingInView(aNode);
            if (nextSibling != null)
                return nextSibling;

            // go to parent next node or parent parent next ...
            var parentNode = aNode.Parent;
            nextSibling = null;
            if (!IsRoot(parentNode))
            {
                if (parentNode != null)
                    nextSibling = GetNextSiblingInView(parentNode);
                while (parentNode != null && !IsRoot(parentNode.Parent) && nextSibling == null)
                {
                    parentNode = parentNode.Parent;
                    nextSibling = GetNextSiblingInView(parentNode);
                }
            }
            return parentNode != null
                       ? nextSibling
                       : null;
        }

        private static Node GetNextSiblingInView(Node aNode)
        {
            if (IsRoot(aNode))
                return null;
            var siblings = aNode.Parent.Nodes;
            var index = siblings.IndexOf(aNode);
            return siblings.Count() > index + 1
                       ? siblings.ElementAt(siblings.IndexOf(aNode) + 1)
                       : null;
        }

        public static Node GetPreviousModelNodeInView(Node aNode)
        {
            if (IsRoot(aNode))
                return null;

            var previousSibling = GetPreviousSiblingInView(aNode);

            if (previousSibling != null)
                return LastNodeFrom(previousSibling);

            return IsRoot(aNode.Parent) ? null : aNode.Parent;
        }

        private static Node LastNodeFrom(Node aNode)
        {
            var children = aNode.Nodes;
            while (children.Any())
            {
                aNode = children.Last();
                children = aNode.Nodes;
            }
            return aNode;
        }

        private static Node GetPreviousSiblingInView(Node aNode)
        {
            var siblings = aNode.Parent.Nodes;
            var index = siblings.IndexOf(aNode);
            return index - 1 >= 0
                       ? siblings.ElementAt(siblings.IndexOf(aNode) - 1)
                       : null;
        }
    }
}