using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Aga.Controls.Tree.NodeControls;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Aga.Controls.Tree
{
	internal class IncrementalSearch
	{
		private const int SearchTimeout = 300; //end of incremental search timeot in msec

		private TreeViewAdv _tree;
		private Node _currentNode;
		private string _searchString = "";
		private DateTime _lastKeyPressed = DateTime.Now;

		public IncrementalSearch(TreeViewAdv tree)
		{
			_tree = tree;
		}

		public void Search(Char value)
		{
			if (!Char.IsControl(value))
			{
				Char ch = Char.ToLowerInvariant(value);
				DateTime dt = DateTime.Now;
				TimeSpan ts = dt - _lastKeyPressed;
				_lastKeyPressed = dt;
				if (ts.TotalMilliseconds < SearchTimeout)
				{
					if (_searchString == value.ToString())
						FirstCharSearch(ch);
					else
						ContinuousSearch(ch);
				}
				else
				{
					FirstCharSearch(ch);
				}
			}
		}

		private void ContinuousSearch(Char value)
		{
			if (value == ' ' && String.IsNullOrEmpty(_searchString))
				return; //Ingnore leading space

			_searchString += value;
			DoContinuousSearch();
		}

        private Node FirstLabelSearch(string searchString)
        {
            Node node = null;
            Node initialNode = null;
            if (_tree.SelectedNode != null)
            {
                initialNode = _tree.SelectedNode;
                node = _tree.GetNextVisibleNode(initialNode);
            }
            if (node == null)
            {
                initialNode = _tree.Model.Root;
                node = _tree.GetNextVisibleNode(initialNode);
            }
            
            Debug.Assert(initialNode != null);

            TreeNodeAdv treeNode = null;
            while(node != null)
            {
                treeNode = _tree.GetTempTreeNode(treeNode, node, false);
                var label = GetNodeFirstLabel(treeNode);
                if (label.StartsWith(_searchString))
                {
                    _currentNode = node;
                    return _currentNode;
                }
                node = _tree.GetNextVisibleNode(node);
            }

            //for (int i = 0; i < 2; i++)
            //{
            //    if (node == null)
            //        return null;
            //    foreach (string label in IterateNodeFirstLabels(node))
            //    {
            //        if (_currentNode == initialNode)
            //            return null;
            //        if (label.StartsWith(_searchString))
            //            return _currentNode;
            //    }
            //    node = _tree.Model.Root;
            //}
            return null;
        }

		private void FirstCharSearch(Char value)
		{
			if (value == ' ')
				return;

			_searchString = value.ToString();
		    var result = FirstLabelSearch(_searchString);
            if (result != null)
                _tree.SelectNode(result);
		}

        private bool DoContinuousSearch()
        {
            if (String.IsNullOrEmpty(_searchString))
                return false;
            var result = FirstLabelSearch(_searchString);
            if (result == null)
                return false;
            _tree.SelectNode(_currentNode);
            return true;
        }

		public virtual void EndSearch()
		{
			_currentNode = null;
			_searchString = "";
		}

        //protected IEnumerable<string> IterateNodeLabels(TreeNodeAdv start)
        //{
        //    _currentNode = start;
        //    while(_currentNode != null)
        //    {
        //        foreach (string label in GetNodeLabels(_currentNode))
        //            yield return label;

        //        _currentNode = _currentNode.NextVisibleNode;
        //        if (_currentNode == null)
        //            //_currentNode = _tree.Root;
        //            break;

        //        if (start == _currentNode)
        //            break;
        //    } 
        //}

        //protected IEnumerable<string> IterateNodeFirstLabels(Node start)
        //{
        //    _currentNode = start;
        //    while (_currentNode != null)
        //    {
        //        var label = GetNodeFirstLabel(_currentNode);
        //        if (label != null)
        //            yield return label;

        //        _currentNode = _currentNode.NextVisibleNode;
        //        if (_currentNode == null)
        //            break;

        //        if (start == _currentNode)
        //            break;
        //    }
        //}

        //private IEnumerable<string> GetNodeLabels(TreeNodeAdv node)
        //{
        //    foreach (NodeControl nc in _tree.NodeControls)
        //    {
        //        BindableControl bc = nc as BindableControl;
        //        if (bc != null && bc.IncrementalSearchEnabled)
        //        {
        //            object obj = bc.GetValue(node);
        //            if (obj != null)
        //                yield return obj.ToString().ToLowerInvariant();
        //        }
        //    }
        //}

        private string GetNodeFirstLabel(TreeNodeAdv node)
        {
            foreach (NodeControl nc in _tree.NodeControls)
            {
                var bc = nc as BindableControl;
                if (bc != null && bc.IncrementalSearchEnabled)
                {
                    object obj = bc.GetValue(node);
                    if (obj != null)
                        return obj.ToString().ToLowerInvariant();
                }
            }
            return null;
        }
	}
}
