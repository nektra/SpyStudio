namespace Aga.Controls.Tree.Input
{
	internal class InputWithControl: NormalInputState
	{
		public InputWithControl(TreeViewAdv tree): base(tree)
		{
		}

		protected override void DoMouseOperation(TreeNodeAdvMouseEventArgs args)
		{
			if (Tree.SelectionMode == TreeSelectionMode.Single)
			{
				base.DoMouseOperation(args);
			}
            else if (args.TreeNode != null && Tree.CanSelect(args.TreeNode.Node))
			{
				args.TreeNode.Node.IsSelected = !args.TreeNode.Node.IsSelected;
			}
		}

		protected override void MouseDownAtEmptySpace(TreeNodeAdvMouseEventArgs args)
		{
		}
	}
}
