using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aga.Controls.Tree
{
    public class SortedTreeModel : TreeModel
    {
        // Should NOT be used
        private SortedTreeModel()
        {
            
        }

        public SortedTreeModel(IComparer<Node> aComparer)
        {
            Comparer = aComparer;
        }

        #region NodeCollection

        public class SortedNodeCollection : Node.NodeCollection
        {
            private readonly SortedTreeModel _sortedModel;

            public SortedNodeCollection(Node owner, SortedTreeModel model)
                : base(owner, model)
            {
                _sortedModel = model;
            }

            public override void Add(Node node)
            {
                if (node == null)
                    throw new ArgumentNullException("node");

                if (node.Parent != Owner)
                {
                    if (node.Parent != null)
                        node.Parent.Nodes.Remove(node);

                    Debug.Assert(List.Count == 0 || _sortedModel.Comparer != null, "Using a SortedModel with no comparer.");

                    // use binary search
                    var index = List.BinarySearch(node, _sortedModel.Comparer);

                    if(index < 0)
                        index = ~index;

                    for (var i = index; i < List.Count; i++)
                    {
                        List[i].Index++;
                    }
                    List.Insert(index, node);
                    node.SetParent(Owner);
                    node.Index = index;

                    if (Model != null)
                        Model.OnNodeInserted(node);
                }
            }
            public void ComparerChanged()
            {
                if(List.Count > 0)
                {
                    List.Sort(_sortedModel.Comparer);
                    var index = 0;
                    foreach (var node in List)
                    {
                        node.Index = index++;
                        var nodes = node.Nodes as SortedNodeCollection;
                        if(nodes != null)
                        {
                            nodes.ComparerChanged();
                        }
                    }
                }
            }
        }

        #endregion

        private IComparer<Node> _comparer;

        public IComparer<Node> Comparer
        {
            get { return _comparer; }
            set
            {
                _comparer = value;
                var nodes = Root.Nodes as SortedNodeCollection;
                if (nodes != null)
                {
                nodes.ComparerChanged();
                OnStructureChanged(TreePath.Empty);
                }
            }
        }

        override public Node.NodeCollection CreateNodeCollection(Node node)
        {
            return new SortedNodeCollection(node, this);
        }
   }
}