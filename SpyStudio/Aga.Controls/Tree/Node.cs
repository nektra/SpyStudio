using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Aga.Controls.Tree
{
    public class Node
    {
        #region NodeCollection

        public class NodeCollection : IEnumerable<Node>
        {
            protected readonly Node Owner;
            protected readonly TreeModel Model;
            protected readonly List<Node> List = new List<Node>();

            public NodeCollection(Node owner, TreeModel model)
            {
                Owner = owner;
                Model = model;
            }

            public int Count
            {
                get { return List.Count; }
            }

            public void Clear()
            {
                if (Count > 0)
                {
                    Model.OnNodesBeforeClear(Owner);
                    List.Clear();
                    Model.OnNodesCleared(Owner);
                }
            }
            public virtual void Add(Node node)
            {
                if (node == null)
                    throw new ArgumentNullException("node");

                if (node.Parent != Owner)
                {
                    if (node.Parent != null)
                        node.Parent.Nodes.Remove(node);

                    List.Add(node);
                    
                    node._parent = Owner;

                    node._index = List.Count - 1;
                    Model.OnNodeInserted(node);
                }
            }
            public virtual void Insert(int index, Node node)
            {
                if (node == null)
                    throw new ArgumentNullException("node");
                if (index > Count)
                    throw new ArgumentOutOfRangeException();

                if (node.Parent != null)
                    node.Parent.Nodes.Remove(node);

                List.Insert(index, node);
                for (int i = index+1; i < Count; i++ )
                {
                    List[i]._index++;
                }

                node._parent = Owner;
                node._index = index;
                Model.OnNodeInserted(node);
            }

            public bool RemoveAt(int index)
            {
                if (index >= 0 && index < List.Count)
                {
                    var node = List[index];

                    Model.OnNodeBeforeRemove(node);

                    node._parent = null;
                    node._index = -1;
                    for (var i = index + 1; i < List.Count; i++)
                        List[i]._index--;

                    List.RemoveAt(index);
                    Model.OnNodeRemoved(Owner, index, node);

                    return true;
                }

                return false;
            }

            public bool Remove(Node node)
            {
                var index = List.IndexOf(node);
                return RemoveAt(index);
            }

            public Node this[int index]
            {
                get { return List[index]; }
            }

            public int IndexOf(Node node)
            {
                return List.IndexOf(node);
            }

            public bool Contains(Node node)
            {
                return List.Contains(node);
            }

            public IEnumerator<Node> GetEnumerator()
            {
                return List.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        #region Properties

        public bool ParentIsRoot
        {
            get { return Parent.Parent == null; }
        }
        public bool IsRoot
        {
            get { return Parent == null; }
        }

        public virtual int Depth
        {
            get
            {
                if (_depth == -1)
                    _depth = ParentIsRoot ? 1 : 1 + Parent.Depth;

                return _depth;
            }
        }

        public bool ThreeState = false;

        protected internal ITreeModel Model { get; set; }

        private NodeCollection _nodes;

        public virtual NodeCollection Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    var model = FindModel();
                    if (model == null)
                        return null;
                    _nodes = model.CreateNodeCollection(this);
                }
                return _nodes;
            }
        }

        private Node _parent;

        public void SetParent(Node parent)
        {
            _parent = parent;
        }

        public Node Parent
        {
            get { return _parent; }
            private set
            {
                if (value != _parent)
                {
                    if (_parent != null)
                        _parent.Nodes.Remove(this);

                    if (value != null)
                        value.Nodes.Add(this);
                }
            }
        }

        private int _index = -1;

        public int Index
        {
            get { return _index; }
            internal set { _index = value; }
        }

        public Node PreviousNode
        {
            get
            {
                var index = Index;
                return index > 0 ? _parent.Nodes[index - 1] : null;
            }
        }

        public Node NextNode
        {
            get
            {
                var index = Index;
                if (index >= 0 && index < _parent.Nodes.Count - 1)
                    return _parent.Nodes[index + 1];

                return null;
            }
        }

        internal Node BottomNode
        {
            get
            {
                var parent = Parent;
                if (parent != null)
                {
                    if (parent.NextNode != null)
                        return parent.NextNode;
                    return parent.BottomNode;
                }
                return null;
            }
        }

        private string _text;

        public virtual string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    NotifyUpdate();
                }
            }
        }

        private CheckState _checkState;

        public CheckState CheckState
        {
            get { return _checkState; }
            set
            {
                if (_checkState != value)
                {
                    _checkState = value;
                    NotifyModelCheckChange();
                }
            }
        }

        private bool _isExpanded;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (!IsLeaf && _isExpanded != value)
                {
                    SetIsExpanded(value, true);
                }
            }
        }

        public bool CanExpand
        {
            get { return Nodes.Count > 0; }
        }


        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                // not Root
                if (IsRoot)
                    return;
                if (_isSelected != value)
                {
                    NotifyModelNodeSelectedChange();
                }
            }
        }

        private Image _image;

        public Image Image
        {
            get { return _image; }
            set
            {
                if (_image != value)
                {
                    _image = value;
                    NotifyUpdate();
                }
            }
        }

        public object Tag { get; set; }

        public bool IsChecked
        {
            get { return CheckState != CheckState.Unchecked; }
            set { CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
        }

        public bool Checked
        {
            get { return IsChecked; }
            set
            {
                if(value != IsChecked)
                {
                    if (value)
                    {
                        CheckState = CheckState.Checked;
                        IsChecked = true;
                    }
                    else
                    {
                        CheckState = CheckState.Unchecked;
                        IsChecked = false;
                    }
                    NotifyModelCheckChange();
                }
            }
        }

        public virtual bool IsLeaf
        {
            get { return false; }
        }

        private Color _foreColor;
        private bool _foreColorSet;

        public Color ForeColor
        {
            get { return _foreColor; }
            set
            {
                _foreColorSet = true;
                _foreColor = value;
            }
        }

        public bool ForeColorSet
        {
            get { return _foreColorSet; }
        }

        private bool _bold;
        private bool _boldSet;
        private int _depth = -1;

        public bool Bold
        {
            get { return _bold; }
            set
            {
                _boldSet = true;
                _bold = value;
            }
        }

        public bool BoldSet
        {
            get { return _boldSet; }
        }

        public Brush Brush { get; set; }

        #endregion

        protected string GetPathFromParent()
        {
            return ParentIsRoot ? Text : Parent.GetPathFromParent() + "\\" + Text;
        }

        public Node(TreeModel model)
            : this(string.Empty)
        {
            Model = model;
        }

        public Node()
            : this(string.Empty)
        {
        }

        public Node(string text)
        {
            _text = text;
        }

        public override string ToString()
        {
            return Text;
        }

        private ITreeModel FindModel()
        {
            var node = this;
            while (node != null)
            {
                if (node.Model != null)
                {
                    return node.Model;
                }
                node = node.Parent;
            }
            return null;
        }

        public void Collapse()
        {
            if (_isExpanded)
                Collapse(true);
        }

        public void CollapseAll()
        {
            Collapse(false);
        }

        public void Collapse(bool ignoreChildren)
        {
            SetIsExpanded(false, ignoreChildren);
        }

        private bool CanBeExpanded()
        {
            return !IsLeaf;
        }

        public void Expand()
        {
            if (!_isExpanded && CanBeExpanded())
                Expand(true);
        }

        public void ExpandAll()
        {
            if (CanBeExpanded())
                Expand(false);
        }

        public void Expand(bool ignoreChildren)
        {
            if (CanBeExpanded())
                SetIsExpanded(true, ignoreChildren);
        }

        private void SetIsExpanded(bool value, bool ignoreChildren)
        {
            NotifyModelNodeExpandedChange(value, ignoreChildren);
        }

        internal void AssignIsExpanded(bool value)
        {
            _isExpanded = value;
        }

        internal void AssignIsSelected(bool value)
        {
            _isSelected = value;
        }

        protected void NotifyModelCheckChange()
        {
            var model = FindModel();
            if (model != null && Parent != null)
                model.OnNodesCheckChanged(this);
        }

        protected void NotifyModelNodeSelectedChange()
        {
            var model = FindModel();
            if (model != null)
                model.OnNodeSelectedChanged(this);
        }

        protected void NotifyModelNodeExpandedChange(bool expand, bool ignoreChildren)
        {
            var model = FindModel();
            if (model != null)
                model.OnNodeExpandedChanged(this, expand, ignoreChildren);
        }

        public virtual void NotifyUpdate()
        {
            var model = FindModel();
            if (model != null && Parent != null)
                model.OnNodeChanged(this);
        }

        #region Events

        public event EventHandler<TreeModelNodeEventArgs> Collapsing;

        internal void OnCollapsing()
        {
            if (Collapsing != null)
                Collapsing(this, new TreeModelNodeEventArgs(this));
        }

        public event EventHandler<TreeModelNodeEventArgs> Collapsed;

        internal void OnCollapsed()
        {
            if (Collapsed != null)
                Collapsed(this, new TreeModelNodeEventArgs(this));
        }

        public event EventHandler<TreeModelNodeEventArgs> Expanding;

        internal void OnExpanding()
        {
            if (Expanding != null)
                Expanding(this, new TreeModelNodeEventArgs(this));
        }

        public event EventHandler<TreeModelNodeEventArgs> Expanded;

        internal void OnExpanded()
        {
            if (Expanded != null)
                Expanded(this, new TreeModelNodeEventArgs(this));
        }

        #endregion

        protected void SetForeColorForSelfAndAncestors(Color aColor)
        {
            ForeColor = aColor;

            if (!IsRoot)
                Parent.SetForeColorForSelfAndAncestors(aColor);
        }

        protected void ExpandSelfAndAncestors()
        {
            Expand();

            if (!IsRoot)
                Parent.ExpandSelfAndAncestors();
        }
    }
}