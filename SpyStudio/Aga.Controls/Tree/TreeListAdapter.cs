using System;
using System.Collections.ObjectModel;

namespace Aga.Controls.Tree
{
    ///// <summary>
    ///// Converts IEnumerable interface to ITreeModel. 
    ///// Allows to display a plain list in the TreeView
    ///// </summary>
    //public class TreeListAdapter : ITreeModel
    //{
    //    private Collection<Node> _list;

    //    public TreeListAdapter(Collection<Node> list)
    //    {
    //        _list = list;
    //    }

    //    #region ITreeModel Members

    //    public Collection<Node> GetChildren(TreePath treePath)
    //    {
    //        return treePath.IsEmpty() ? _list : null;
    //    }

    //    public Collection<Node> Nodes
    //    {
    //        get { return _list; }
    //    }

    //    public bool IsLeaf(TreePath treePath)
    //    {
    //        return true;
    //    }

    //    public event EventHandler<TreeModelEventArgs> NodesChanged;

    //    public void OnNodesChanged(TreeModelEventArgs args)
    //    {
    //        if (NodesChanged != null)
    //            NodesChanged(this, args);
    //    }

    //    public event EventHandler<TreeModelEventArgs> NodesCheckChanged;

    //    public void OnNodesCheckChanged(TreeModelEventArgs args)
    //    {
    //        if (NodesCheckChanged != null)
    //            NodesCheckChanged(this, args);
    //    }

    //    public event EventHandler<TreePathEventArgs> StructureChanged;

    //    public void OnStructureChanged(TreePathEventArgs args)
    //    {
    //        if (StructureChanged != null)
    //            StructureChanged(this, args);
    //    }

    //    public event EventHandler<TreeModelEventArgs> NodesInserted;

    //    public void OnNodeInserted(TreeModelEventArgs args)
    //    {
    //        if (NodesInserted != null)
    //            NodesInserted(this, args);
    //    }

    //    public event EventHandler<TreeModelEventArgs> NodesRemoved;

    //    public void OnNodeRemoved(TreeModelEventArgs args)
    //    {
    //        if (NodesRemoved != null)
    //            NodesRemoved(this, args);
    //    }

    //    #endregion
    //}
}