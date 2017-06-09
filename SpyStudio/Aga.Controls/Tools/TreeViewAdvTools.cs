using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace Aga.Controls.Tools
{
    public class TreeViewAdvTools
    {
        public class FolderItemSorter : IComparer
        {
            //private string _mode;
            private readonly SortOrder _order = SortOrder.Ascending;

            public FolderItemSorter(SortOrder order)
            {
                _order = order;
            }

            public int Compare(object x, object y)
            {
                var a = x as Node;
                var b = y as Node;

                Debug.Assert(a != null, "a != null");
                Debug.Assert(b != null, "b != null");
                int res = CultureInfo.CurrentCulture.CompareInfo.Compare(a.Text, b.Text, CompareOptions.IgnoreCase);

                if (_order == SortOrder.Descending)
                    return -res;
                return res;
            }

            //private string GetData(object x)
            //{
            //    return (x as Node).Text;
            //}
        }

        static public Node GetFirstNodeToSearch(TreeViewAdv tree, bool startTop, bool useSelection)
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

        static public int GetLevel(Node n)
        {
            int level = 0;
            n = n.Parent;
            while(n != null)
            {
                n = n.Parent;
                level++;
            }
            // do not count root as 1
            return (level - 1);
        }
        static public bool IsAncestor(Node child, Node ancestor)
        {
            child = child.Parent;
            while (ancestor != child && child != null)
            {
                child = child.Parent;
            }
            return (child != null);
        }
        static public void CheckNode(Node n, bool checkState, bool recursive)
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

        public static bool IsRoot(Node aNode)
        {
            return aNode.Parent == null;
        }

        public static Node GetNextModelNodeInView(Node aNode)
        {
            // go to current children
            var children = aNode.Nodes;

            var childrenEnum = children.GetEnumerator();
            if (childrenEnum.MoveNext())
                return childrenEnum.Current;

            // go to next child
            var nextSibling = GetNextSiblingInView(aNode);
            if (nextSibling != null)
            {
                return nextSibling;
            }

            // go to parent next node or parent parent next ...
            var parentNode = aNode.Parent;
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
            var sw = new Stopwatch();
            sw.Start();

            if (IsRoot(aNode))
                return null;

            var siblings = aNode.Parent.Nodes;

            Node ret = null;
            var enumSiblings = siblings.GetEnumerator();
            while (enumSiblings.MoveNext())
            {
                if (enumSiblings.Current == aNode)
                {
                    if (enumSiblings.MoveNext())
                    {
                        ret = enumSiblings.Current;
                    }
                    break;
                }
            }

            return ret;
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
            var childrenEnum = children.GetEnumerator();
            while (childrenEnum.MoveNext())
            {
                do
                {
                    aNode = childrenEnum.Current;
                }
                while (childrenEnum.MoveNext());
                if (aNode == null)
                    break;
                childrenEnum = aNode.Nodes.GetEnumerator();
            }
            return aNode;
        }

        private static Node GetPreviousSiblingInView(Node aNode)
        {
            var siblings = aNode.Parent.Nodes;
            Node ret = null, prevNode = null;
            var enumSiblings = siblings.GetEnumerator();
            while (enumSiblings.MoveNext())
            {
                if (enumSiblings.Current == aNode)
                {
                    if (prevNode != null)
                    {
                        ret = prevNode;
                    }
                    break;
                }
                prevNode = enumSiblings.Current;
            }
            return ret;
        }
        static readonly List<Node> XAncestors = new List<Node>();
        static readonly List<Node> YAncestors = new List<Node>();
        static readonly public NodeRowComparer NodeComparer = new NodeRowComparer();
        public class NodeRowComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                Debug.Assert(y != null, "a != null");
                Debug.Assert(x != null, "b != null");

                XAncestors.Clear();
                YAncestors.Clear();

                var n = x;
                do
                {
                    XAncestors.Add(n);
                    n = n.Parent;
                } while (!n.IsRoot);

                n = y;
                do
                {
                    YAncestors.Add(n);
                    n = n.Parent;
                } while (!n.IsRoot);

                int hx = XAncestors.Count - 1, hy = YAncestors.Count - 1;
                int res = XAncestors[hx--].Index.CompareTo(YAncestors[hy--].Index);
                while(res == 0 && hx >= 0 && hy >= 0)
                {
                    res = XAncestors[hx--].Index.CompareTo(YAncestors[hy--].Index);
                }
                if(res == 0)
                {
                    if (hx >= 0)
                        res = 1;
                    else if (hy >= 0)
                        res = -1;
                }

                XAncestors.Clear();
                YAncestors.Clear();

                return res;
            }
        }

        public static void SortByTreeRow(List<Node> nodeList)
        {
            nodeList.Sort(NodeComparer);
        }
    }
}
