using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aga.Controls.Tree;

namespace Aga.Controls.Tree
{
    partial class TreeViewAdv
    {
        class ExpandedNodeInfo
        {
            public ExpandedNodeInfo()
            {
                VisibleChildrenCount = -1;
            }

            public int VisibleChildrenCount { get; set; }
        }

        private readonly Dictionary<Node, TreeNodeAdv> _treeNodes = new Dictionary<Node, TreeNodeAdv>();
        /// <summary>
        /// All visible nodes are in this array. When a node is visible is has a TreeNodeAdv to draw the Node. Row data is always up-to-date and this
        /// is the main data in Draw cycle.
        /// </summary>
        private Dictionary<int, TreeNodeAdv> _visibleNodes = new Dictionary<int, TreeNodeAdv>();
        private readonly Dictionary<Node, ExpandedNodeInfo> _expandedNodes = new Dictionary<Node, ExpandedNodeInfo>();
        private bool _pendingSelChange = false;

        public void EnsureVisible(Node node)
        {
            EnsureVisible(node, ScrollType.Any);
        }

        public void EnsureVisible(Node node, ScrollType scrollType)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var parent = node.Parent;
            while (parent != Model.Root && parent != null)
            {
                SetIsExpanded(parent, true);
                parent = parent.Parent;
            }

            int row = GetNodeRow(node);
            ScrollTo(row, scrollType);
        }

        public int GetNodeRow(Node n)
        {
            if (n == Model.Root)
                return -1;

            TreeNodeAdv treeNode;
            // we can only guarantee that _treeNodes have correct Row data when they are also visible. Otherwise,
            // we have to calculate the row data again
            if(_treeNodes.TryGetValue(n, out treeNode) && treeNode.Row != -1)
            {
                TreeNodeAdv visibleTreeNode;
                if (_visibleNodes.TryGetValue(treeNode.Row, out visibleTreeNode) && visibleTreeNode == treeNode)
                    return treeNode.Row;
            }

            int visibleChildren = 0;
            // count all visible children that are above this node and belong to a branch which parent is a sibling
            // of this node.
            foreach(var nodePair in _expandedNodes)
            {
                var node = nodePair.Key;
                if(node.Parent == n.Parent && node.Index < n.Index)
                {
                    if(nodePair.Value.VisibleChildrenCount == -1)
                    {
                        nodePair.Value.VisibleChildrenCount = GetVisibleChildrenCount(node);
                    }
                    visibleChildren += nodePair.Value.VisibleChildrenCount;
                }
            }
            return GetNodeRow(n.Parent) + n.Index + 1 + visibleChildren;
        }

        /// <summary>
        /// Get a TreeNodeAdv completely detached to use to measure or draw
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="node"></param>
        /// <param name="addRowInfo"></param>
        /// <returns></returns>
        internal TreeNodeAdv GetTempTreeNode(TreeNodeAdv treeNode, Node node, bool addRowInfo)
        {
            int level = 0;
            var p = node;
            while (p.Parent != null)
            {
                p = p.Parent;
                level++;
            }
            if(treeNode == null)
            {
                treeNode = new TreeNodeAdv(this, node);
            }

            treeNode.Level = level;
            treeNode.Node = node;

            if (addRowInfo)
            {
                treeNode.Row = GetNodeRow(node);
            }

            return treeNode;
        }

        private TreeNodeAdv GetTempTreeNode(Node node, bool addRowInfo)
        {
            return GetTempTreeNode(null, node, addRowInfo);
        }

        internal TreeNodeAdv GetTreeNode(Node node)
        {
            TreeNodeAdv treeNode;
            if (!_treeNodes.TryGetValue(node, out treeNode))
            {
                return null;
            }
            return treeNode;
        }
        void ClearVisibleNodes()
        {
            _treeNodes.Clear();
            _visibleNodes.Clear();
            _firstVisibleNodesRow = 0;
            _lastVisibleNodesRow = -1;
        }
        internal void RecalculateVisibleRowCount()
        {
            _reachableNodeCount = 0;
            var node = Model.Root;
            node = GetNextVisibleNode(node);
            while (node != null)
            {
                _reachableNodeCount++;
                node = GetNextVisibleNode(node);
            }
        }

        internal TreeNodeAdv CreateVisibleTreeNode(Node node, int row)
        {
            Debug.Assert(!_treeNodes.ContainsKey(node));

            int level = 0;
            var p = node;
            while (p.Parent != null)
            {
                p = p.Parent;
                level++;
            }
            var treeNode = new TreeNodeAdv(this, node) {Level = level};
            _treeNodes[node] = treeNode;
            if (row == -1)
            {
                treeNode.Row = GetNodeRow(node);
            }
            else
            {
                treeNode.Row = row;
            }

            _visibleNodes[row] = treeNode;

            return treeNode;
        }

        public bool IsPathExpanded(Node n)
        {
            while(n.IsExpanded && n.Parent != null)
            {
                n = n.Parent;
            }

            return n.IsExpanded;
        }
        private void ApplyOffsetToVisibleNode(int row, int offset)
        {
            var treeNode = _visibleNodes[row];
            Debug.Assert(row == treeNode.Row);
            _visibleNodes.Remove(row);
            treeNode.Row += offset;
            _visibleNodes[treeNode.Row] = treeNode;
        }
        private void ApplyOffsetToRowArray(int startRow, int endRow, int offset)
        {
            if(offset > 0)
            {
                for (int j = endRow; j >= startRow; j--)
                {
                    ApplyOffsetToVisibleNode(j, offset);
                }
            }
            else
            {
                for (int j = startRow; j <= endRow; j++)
                {
                    ApplyOffsetToVisibleNode(j, offset);
                }
            }
        }

        private void RemoveTreeNodeAt(int row)
        {
            TreeNodeAdv treeNode = _visibleNodes[row];
            treeNode.Row = -1;
            _visibleNodes.Remove(row);
            var n = treeNode.Node;
            Debug.Assert(n != null);
            _treeNodes.Remove(n);
        }

        private void ModelNodeInserted(Node node)
        {
            var parent = node.Parent;
            if (parent != null)
            {
                var newNode = node;
                Debug.Assert(newNode != null);

                // update VisibleChildrenCount to all expanded nodes in node path
                var current = node;
                while (current.Parent != null && current.Parent.IsExpanded)
                {
                    ExpandedNodeInfo expandedNodeInfo;
                    if (_expandedNodes.TryGetValue(current.Parent, out expandedNodeInfo))
                    {
                        if (expandedNodeInfo.VisibleChildrenCount != -1)
                            expandedNodeInfo.VisibleChildrenCount++;
                    }
                    else
                    {
                        Debug.Assert(false, "Expanded nodes must be in _expandedNodes dictionary");
                    }

                    current = current.Parent;
                }

                int row = GetNodeRow(newNode);

                if (IsPathExpanded(newNode.Parent))
                {
                    _reachableNodeCount++;


                    // verify if it is in the display window and adjust _rowTreeNodeMap 
                    if (row < _firstVisibleNodesRow)
                    {
                        ApplyOffsetToRowArray(_firstVisibleNodesRow, _lastVisibleNodesRow, 1);
                        _firstVisibleNodesRow++;
                        _lastVisibleNodesRow++;
                        UpdateControl(true, true);
                    }
                    // new node should be displayed now
                    else if (row <= _lastVisibleNodesRow)
                    {
                        if (_visibleNodes.Count > 0)
                        {
                            // remove the last one since it's no longer displayed
                            RemoveTreeNodeAt(_lastVisibleNodesRow);

                            ApplyOffsetToRowArray(row, _lastVisibleNodesRow - 1, 1);           

                            CreateVisibleTreeNode(newNode, row);
                        }

                        UpdateControl(true, true);
                    }
                    else if(_emptySpaceInDisplay)
                    {
                        UpdateControl(true, true);
                    }
                    else
                    {
                        // the node is added after the last visible node so only update scrollbars
                        UpdateControl(true, false);
                    }
                    if(ReachableNodeInserted != null)
                    {
                        ReachableNodeInserted(this, new ReachableNodeInsertedEventArgs(row, newNode));
                    }
                }
            }
        }
        int GetVisibleChildrenCount(Node n)
        {
            List<Node> expandedNodes = null;
            return GetVisibleChildrenCount(n, ref expandedNodes);
        }
        int GetVisibleChildrenCount(Node n, ref List<Node> expandedNodes)
        {
            int ret = 0;
            if(n.IsExpanded)
            {
                if(expandedNodes != null)
                    expandedNodes.Add(n);
                foreach(var c in n.Nodes)
                {
                    ret += GetVisibleChildrenCount(c, ref expandedNodes) + 1;
                }
            }
            return ret;
        }
        /// <summary>
        /// Removes a node, its children or both.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="removeParentNode"></param>
        /// <param name="removeChildren"></param>
        void RemoveNode(Node parent, bool removeParentNode, bool removeChildren)
        {
            var visibleRowsRemoved = 0;

            if (removeParentNode && IsPathExpanded(parent.Parent))
                visibleRowsRemoved++;

            var current = parent;
            while (current != null && current.IsExpanded)
            {
                ExpandedNodeInfo expandedNodeInfo;
                if (_expandedNodes.TryGetValue(current, out expandedNodeInfo))
                {
                    if (expandedNodeInfo.VisibleChildrenCount != -1)
                        expandedNodeInfo.VisibleChildrenCount--;
                }
                else
                {
                    Debug.Assert(false, "Expanded nodes must be in _expandedNodes dictionary");
                }

                current = current.Parent;
            }

            var expandedNodes = new List<Node>();
            if (removeChildren && IsPathExpanded(parent))
            {
                visibleRowsRemoved += GetVisibleChildrenCount(parent, ref expandedNodes);
            }

            if (visibleRowsRemoved > 0)
            {
                var startRow = removeParentNode ? GetNodeRow(parent) : GetNodeRow(parent) + 1;
                int endRow = startRow + visibleRowsRemoved;
                int selChange = 0;

                foreach (var e in expandedNodes)
                    if (!e.IsRoot)
                        _expandedNodes.Remove(e);

                // if the first row to remove is greater than _lastVisibleNodesRow there are no visible nodes to remove
                if (startRow <= _lastVisibleNodesRow)
                {
                    // remove all visible nodes that are now removed 
                    for (int i = Math.Max(startRow, _firstVisibleNodesRow);
                         i <= Math.Min(startRow + visibleRowsRemoved - 1, _lastVisibleNodesRow);
                         i++)
                    {
                        var treeNode = _visibleNodes[i];
                        if (_selection.Remove(treeNode.Node))
                            selChange++;
                        _visibleNodes.Remove(i);
                        _treeNodes.Remove(treeNode.Node);
                    }

                    if (endRow < FirstVisibleRow)
                    {
                        FirstVisibleRow = _reachableNodeCount > endRow ? endRow : _reachableNodeCount;
                    }

                    ApplyOffsetToRowArray(endRow < _firstVisibleNodesRow ? _firstVisibleNodesRow : endRow, _lastVisibleNodesRow, -visibleRowsRemoved);

                    _firstVisibleNodesRow = _lastVisibleNodesRow = -1;
                    var rows = _visibleNodes.Keys;
                    // get first and last row in the array
                    foreach (var row in rows)
                    {
                        if (_firstVisibleNodesRow == -1 || row < _firstVisibleNodesRow)
                            _firstVisibleNodesRow = row;
                        if (row > _lastVisibleNodesRow)
                            _lastVisibleNodesRow = row;
                    }
                    // no items
                    if (_firstVisibleNodesRow == -1)
                    {
                        FirstVisibleRow = 0;
                        _firstVisibleNodesRow = 0;
                        _lastVisibleNodesRow = -1;
                    }
                }

                _reachableNodeCount -= visibleRowsRemoved;

                _pendingSelChange = (selChange != 0);
                    OnSelectionChanged();
            }

            if (_firstVisibleNodesRow == 0 && _lastVisibleNodesRow == -1)
                _emptySpaceInDisplay = true;

        }
        private void ModelNodeBeforeRemove(Node node)
        {
            RemoveNode(node, true, true);
        }
        private void ModelOnNodeRemoved(Node node, int i, Node arg3)
        {
            UpdateControl(true, true);
            if (_pendingSelChange)
            {
                OnSelectionChanged();
                _pendingSelChange = false;
            }
        }

        private void ModelNodesBeforeClear(Node node)
        {
            RemoveNode(node, false, true);
        }

        private void ModelNodeChanged(Node node)
        {
            if(_treeNodes.ContainsKey(node))
            {
                UpdateControl(false, true);
            }
        }

        public Node GetNextVisibleNode(Node n)
        {
            if (n.Nodes.Count > 0)
            {
                if (n.IsExpanded)
                    return n.Nodes[0];
            }
            var ret = n.NextNode;
            while (ret == null && n.Parent != null)
            {
                n = n.Parent;
                ret = n.NextNode;
            }
            return ret;
        }

        public Node GetPreviousVisibleNode(Node n)
        {
            var ret = n.PreviousNode;
            if (ret != null)
            {
                while (ret.IsExpanded && ret.Nodes.Count > 0)
                {
                    ret = ret.Nodes.Last();
                }
            }
            else
            {
                ret = n.Parent;
            }

            return ret == Model.Root ? null : ret;
        }

        public Node GetNextNode(Node n)
        {
            if (n.Nodes.Count > 0)
            {
                return n.Nodes[0];
            }
            var ret = n.NextNode;
            while (ret == null && n.Parent != null)
            {
                n = n.Parent;
                ret = n.NextNode;
            }
            return ret;
        }

        public Node GetPreviousNode(Node n)
        {
            var ret = n.PreviousNode;
            if (ret != null)
            {
                while (ret.Nodes.Count > 0)
                {
                    ret = ret.Nodes.Last();
                }
            }
            else
            {
                ret = n.Parent;
            }

            return ret;
        }

        private void ModelNodeSelectedChanged(Node node)
        {
            SelectNode(node);
        }

        private void ModelNodeExpandedChanged(Node node, bool expand, bool ignoreChildren)
        {
            SetIsExpanded(node, expand, ignoreChildren);
        }
    }
}