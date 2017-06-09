using System;
using System.Windows.Forms;

namespace Aga.Controls.Tree.Input
{
    internal class NormalInputState : InputState
    {
        private bool _mouseDownFlag;

        public NormalInputState(TreeViewAdv tree) : base(tree)
        {
        }

        public override void KeyDown(KeyEventArgs args)
        {
            if (Tree.CurrentNode == null && Tree.Model.Root.Nodes.Count > 0)
                Tree.SelectNode(Tree.Model.Root.Nodes[0]);

            if (Tree.CurrentNode != null)
            {
                switch (args.KeyCode)
                {
                    case Keys.Right:
                        if (!Tree.CurrentNode.IsExpanded && Tree.CurrentNode.CanExpand)
                        {
                            Tree.CurrentNode.IsExpanded = true;
                        }
                        else if (Tree.CurrentNode.IsExpanded)
                        {
                            if(Tree.CurrentNode.Nodes.Count > 0)
                                Tree.SelectNode(Tree.CurrentNode.Nodes[0]);
                        }
                        args.Handled = true;
                        break;
                    case Keys.Left:
                        if (Tree.CurrentNode.IsExpanded)
                            Tree.CurrentNode.IsExpanded = false;
                        else if (Tree.CurrentNode.Parent != Tree.Model.Root)
                            Tree.SelectNode(Tree.CurrentNode.Parent);
                        args.Handled = true;
                        break;
                    case Keys.Down:
                        NavigateForward(1);
                        args.Handled = true;
                        break;
                    case Keys.Up:
                        NavigateBackward(1);
                        args.Handled = true;
                        break;
                    case Keys.PageDown:
                        NavigateForward(Math.Max(1, Tree.CurrentPageSize - 1));
                        args.Handled = true;
                        break;
                    case Keys.PageUp:
                        NavigateBackward(Math.Max(1, Tree.CurrentPageSize - 1));
                        args.Handled = true;
                        break;
                    case Keys.Home:
                        if (Tree.ReachableNodeCount > 0)
                            FocusRow(0);
                        args.Handled = true;
                        break;
                    case Keys.End:
                        if (Tree.ReachableNodeCount > 0)
                            FocusRow(Tree.ReachableNodeCount - 1);
                        args.Handled = true;
                        break;
                    case Keys.Subtract:
                        Tree.CurrentNode.Collapse();
                        args.Handled = true;
                        args.SuppressKeyPress = true;
                        break;
                    case Keys.Add:
                        Tree.CurrentNode.Expand();
                        args.Handled = true;
                        args.SuppressKeyPress = true;
                        break;
                    case Keys.Multiply:
                        Tree.CurrentNode.ExpandAll();
                        args.Handled = true;
                        args.SuppressKeyPress = true;
                        break;
                    case Keys.A:
                        if (args.Modifiers == Keys.Control)
                            Tree.SelectAllNodes();
                        break;
                }
            }
        }

        public override void MouseDown(TreeNodeAdvMouseEventArgs args)
        {
            if (args.TreeNode != null)
            {
                Tree.ItemDragMode = true;
                Tree.ItemDragStart = args.Location;

                if (args.Button == MouseButtons.Left || args.Button == MouseButtons.Right)
                {
                    Tree.BeginUpdate();
                    try
                    {
                        Tree.CurrentNode = args.TreeNode.Node;
                        if (args.TreeNode.Node.IsSelected)
                            _mouseDownFlag = true;
                        else
                        {
                            _mouseDownFlag = false;
                            DoMouseOperation(args);
                        }
                    }
                    finally
                    {
                        Tree.EndUpdate();
                    }
                }

            }
            else
            {
                Tree.ItemDragMode = false;
                MouseDownAtEmptySpace(args);
            }
        }

        public override void MouseUp(TreeNodeAdvMouseEventArgs args)
        {
            Tree.ItemDragMode = false;
            if (_mouseDownFlag && args.TreeNode != null)
            {
                if (args.Button == MouseButtons.Left)
                    DoMouseOperation(args);
                else if (args.Button == MouseButtons.Right)
                    Tree.CurrentNode = args.TreeNode == null ? null : args.TreeNode.Node;
            }
            _mouseDownFlag = false;
        }


        private void NavigateBackward(int n)
        {
            int curRow = Tree.GetNodeRow(Tree.CurrentNode);
            int row = Math.Max(curRow - n, 0);
            if (row != curRow)
                FocusRow(row);
        }

        private void NavigateForward(int n)
        {
            int curRow = Tree.GetNodeRow(Tree.CurrentNode);
            int row = Math.Min(curRow + n, Tree.ReachableNodeCount - 1);
            if (row != curRow)
                FocusRow(row);
        }

        protected virtual void MouseDownAtEmptySpace(TreeNodeAdvMouseEventArgs args)
        {
            Tree.ClearSelection();
        }

        protected virtual void FocusRow(int row)
        {
            Tree.FocusRow(row, false);
        }

        protected virtual void DoMouseOperation(TreeNodeAdvMouseEventArgs args)
        {
            Tree.SelectNode(args.TreeNode);
        }
    }
}
