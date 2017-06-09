using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aga.Controls.Tree;

namespace Aga.Controls.Tree
{
    partial class TreeViewAdv
    {
        internal new bool CanSelect(Node node)
        {
            if (SelectionMode == TreeSelectionMode.MultiSameParent)
            {
                return (SelectionStart == null || node.Parent == SelectionStart.Parent);
            }
            return true;
        }

        internal void FocusRow(int row, bool shiftPressed)
        {
            var node = GetNodeByRow(row);
            if (shiftPressed)
            {
                if (node != null)
                {
                    if (SelectionMode != TreeSelectionMode.Single && SelectionStart != null)
                    {
                        if (CanSelect(node))
                        {
                            BeginUpdate();
                            SelectAllFromStart(node, row);
                            CurrentNode = node;
                            ScrollTo(row);
                            EndUpdate();
                            OnSelectionChanged();
                        }
                        return;
                    }
                }
            }

            BeginUpdate();
            ClearSelectionInternal();
            ScrollTo(row);
            if (node != null)
            {
                CurrentNode = node;
                SelectionStart = node;
                node.AssignIsSelected(true);
                _selection.Add(node);
            }
            EndUpdate();
            OnSelectionChanged();
        }
        internal void SelectNode(TreeNodeAdv treeNode)
        {
            SelectNode(treeNode.Node);
        }
        public void SelectNode(Node node)
        {
            if (_selection.Contains(node) && _selection.Count == 1)
                return;

            BeginUpdate();
            if (node == null)
            {
                ClearSelectionInternal();
            }
            else
            {
                ClearSelectionInternal();
                node.AssignIsSelected(true);
                _selection.Add(node);
                SelectionStart = node;
                CurrentNode = node;
                EnsureVisible(node);
            }
            EndUpdate();
            OnSelectionChanged();
        }
        private void RemoveNodeFromSelection(Node node)
        {
            if(RemoveNodeFromSelectionRecursive(node) != 0)
            {
                OnSelectionChanged();
            }
        }
        private int RemoveNodeFromSelectionRecursive(Node node)
        {
            int count = 0;
            if (_selection.Contains(node))
            {
                if (_selection.Remove(node))
                    count++;
            }
            foreach (var child in node.Nodes)
            {
                count += RemoveNodeFromSelectionRecursive(child);
            }
            return count;
        }

        public void SelectNodes(IEnumerable<Node> nodes)
        {
            BeginUpdate();
            foreach (var n in nodes)
            {
                n.AssignIsSelected(true);
                _selection.Add(n);
            }
            EndUpdate();
            OnSelectionChanged();
        }
        private void SelectNodesRecursive(IEnumerable<Node> nodes)
        {
            ClearSelectionInternal();
            foreach (var n in nodes)
            {
                n.AssignIsSelected(true);
                _selection.Add(n);
                if (n.IsExpanded)
                    SelectNodesRecursive(n.Nodes);
            }
        }

        public void SelectAllNodes()
        {
            BeginUpdate();
            if (SelectionMode == TreeSelectionMode.MultiSameParent)
            {
                if (CurrentNode != null)
                {
                    foreach (var n in CurrentNode.Parent.Nodes)
                    {
                        n.AssignIsSelected(true);
                        _selection.Add(n);
                    }
                }
            }
            else if (SelectionMode == TreeSelectionMode.Multi)
            {
                SelectNodesRecursive(Model.Root.Nodes);
            }
            EndUpdate();

            OnSelectionChanged();
        }
        public void ClearSelection()
        {
            BeginUpdate();
            if(_selection.Count > 0)
            {
                ClearSelectionInternal();
                OnSelectionChanged();
            }
            EndUpdate();
        }

        internal void ClearSelectionInternal()
        {
            foreach(var node in _selection)
            {
                node.AssignIsSelected(false);
            }
            _selection.Clear();
        }
        internal void SelectAllFromStart(Node node, int row)
        {
            ClearSelectionInternal();
            int a = row;
            int b = GetNodeRow(SelectionStart);
            bool forward = a < b;
            var curNode = node;
            for (int i = Math.Min(a, b); i <= Math.Max(a, b) && curNode != null; i++)
            {
                if (SelectionMode == TreeSelectionMode.Multi || curNode.Parent == node.Parent)
                {
                    curNode.AssignIsSelected(true);
                    if(forward)
                        _selection.Add(curNode);
                    else
                        _selection.Insert(0, curNode);
                }

                curNode = forward ? GetNextVisibleNode(curNode) : GetPreviousVisibleNode(curNode);
            }
        }
    }
}