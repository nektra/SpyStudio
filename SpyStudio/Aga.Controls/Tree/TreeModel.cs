using System;

namespace Aga.Controls.Tree
{
    /// <summary>
    /// Provides a simple ready to use implementation of <see cref="ITreeModel"/>. Warning: this class is not optimized 
    /// to work with big amount of data. In this case create you own implementation of <c>ITreeModel</c>, and pay attention
    /// on GetChildren and IsLeaf methods.
    /// </summary>
    public class TreeModel : ITreeModel
    {
        private Node _root;

        public Node Root
        {
            get { return _root; }
            set
            {
                _root = value;
                _root.Model = this;
            }
        }

        public Node.NodeCollection Nodes
        {
            get { return _root.Nodes; }
        }

        virtual public Node.NodeCollection CreateNodeCollection(Node node)
        {
            return new Node.NodeCollection(node, this);
        }

        public TreeModel()
        {
            _root = new Node(this);
        }

        #region ITreeModel Members
        //private double _time1, _time2, _time3, _time4;

        //public void InitTimes()
        //{
        //    _time1 = _time2 = _time3 = _time4 = 0;
        //}
        //public void DumpTimes()
        //{
        //    Debug.WriteLine("GetChildren: AddNode: Time1\t" + _time1 + "\tTime2\t" + _time2 + "\tTime3\t" + _time3 + "\tTime4\t" + _time4);
        //}

        public event Action<Node> NodeChanged;

        public void OnNodeChanged(Node node)
        {
            if (NodeChanged != null)
                NodeChanged(node);
        }

        public event Action<Node> NodeCheckChanged;

        public void OnNodesCheckChanged(Node node)
        {
            if (NodeCheckChanged != null)
                NodeCheckChanged(node);
        }

        //public event EventHandler<TreeModelNodeEventArgs> ExpandChanged;

        //internal void OnNodesExpandChanged(TreeModelNodeEventArgs args)
        //{
        //    if (ExpandChanged != null)
        //        ExpandChanged(this, args);
        //}

        public event Action<Node> NodeSelectedChanged;

        public void OnNodeSelectedChanged(Node node)
        {
            if (NodeSelectedChanged != null)
                NodeSelectedChanged(node);
        }

        public event Action<Node, bool, bool> NodeExpandedChanged;

        public void OnNodeExpandedChanged(Node node, bool expand, bool ignoreChildren)
        {
            if (NodeExpandedChanged != null)
                NodeExpandedChanged(node, expand, ignoreChildren);
        }

        public event Action<TreePath> StructureChanged;

        protected void OnStructureChanged(TreePath treePath)
        {
            if (StructureChanged != null)
                StructureChanged(treePath);
        }

        public event Action<Node> NodeInserted;

        //private double _time2;
        //public void InitTimes()
        //{
        //    _time2 = 0;
        //}
        //public void DumpTimes()
        //{
        //    Debug.WriteLine("TreeModel: AddNode: Time2\t" + _time2);
        //}

        internal void OnNodeInserted(Node node)
        {
            if (NodeInserted != null)
            {
                //var sw = new Stopwatch();
                //sw.Start();
                //var args = new TreeModelNodeEventArgs(node);
                //_time1 += sw.Elapsed.TotalMilliseconds;
                //var prevCheck = sw.Elapsed.TotalMilliseconds;
                NodeInserted(node);
                //_time2 += sw.Elapsed.TotalMilliseconds;
                //prevCheck = sw.Elapsed.TotalMilliseconds;
                //_time3 += sw.Elapsed.TotalMilliseconds - prevCheck;
                //prevCheck = sw.Elapsed.TotalMilliseconds;
                //_time4 += sw.Elapsed.TotalMilliseconds - prevCheck;
            }
        }

        public event Action<Node> NodesBeforeClear;

        internal void OnNodesBeforeClear(Node parent)
        {
            if (NodesBeforeClear != null)
                NodesBeforeClear(parent);
        }

        public event Action<Node> NodesCleared;

        internal void OnNodesCleared(Node parent)
        {
            if (NodesCleared != null)
                NodesCleared(parent);
        }
        public event Action<Node> NodeBeforeRemove;

        internal void OnNodeBeforeRemove(Node node)
        {
            if (NodeBeforeRemove != null)
                NodeBeforeRemove(node);
        }
        public event Action<Node, int, Node> NodeRemoved;

        internal void OnNodeRemoved(Node parent, int index, Node node)
        {
            if (NodeRemoved != null)
                NodeRemoved(parent, index, node);
        }

        #endregion
    }
}