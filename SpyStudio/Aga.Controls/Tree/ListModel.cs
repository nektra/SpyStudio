using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aga.Controls.Tree
{
    //public class ListModel : TreeModelBase
    //{
    //    private IList _list;

    //    public int Count
    //    {
    //        get { return _list.Count; }
    //    }

    //    public ListModel()
    //    {
    //        _list = new List<object>();
    //    }

    //    public ListModel(IList list)
    //    {
    //        _list = list;
    //    }

    //    public override Collection<Node> GetChildren(TreePath treePath)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override bool IsLeaf(TreePath treePath)
    //    {
    //        return true;
    //    }

    //    public void AddRange(IEnumerable items)
    //    {
    //        foreach (var obj in items)
    //            _list.Add(obj);
    //        OnStructureChanged(new TreePathEventArgs(TreePath.Empty));
    //    }

    //    public void Add(object item)
    //    {
    //        _list.Add(item);
    //        OnNodesInserted(new TreeModelEventArgs(TreePath.Empty, new[] {_list.Count - 1}, new[] {item}));
    //    }

    //    public void Clear()
    //    {
    //        _list.Clear();
    //        OnStructureChanged(new TreePathEventArgs(TreePath.Empty));
    //    }
    //}
}