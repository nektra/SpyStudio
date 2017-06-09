namespace Aga.Controls.Tree.Input
{
	internal class InputWithShift: NormalInputState
	{
		public InputWithShift(TreeViewAdv tree): base(tree)
		{
		}

		protected override void FocusRow(int row)
		{
		    Tree.FocusRow(row, true);
		}

		protected override void DoMouseOperation(TreeNodeAdvMouseEventArgs args)
		{
			if (Tree.SelectionMode == TreeSelectionMode.Single || Tree.SelectionStart == null)
			{
				base.DoMouseOperation(args);
			}
			else if (Tree.CanSelect(args.TreeNode.Node))
			{
                Tree.BeginUpdate();
				Tree.SelectAllFromStart(args.TreeNode.Node, args.TreeNode.Row);
                Tree.EndUpdate();
			}
		}

		protected override void MouseDownAtEmptySpace(TreeNodeAdvMouseEventArgs args)
		{
		}

	}
}
