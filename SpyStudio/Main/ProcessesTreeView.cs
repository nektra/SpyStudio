using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.Tools;

// ReSharper disable CheckNamespace

namespace SpyStudio.Main
// ReSharper restore CheckNamespace
{
    public class ProcessesTreeView : TreeViewAdv
    {
        private readonly TreeColumn _columnProcessesHeaderName;
        private readonly TreeColumn _columnProcessesHeaderPid;
        private readonly TreeColumn _columnProcessesHeaderCheckBox;
        private readonly NodeStateIcon _columnHeaderFilenameIconNode;
        private readonly NodeCheckBox _columnProcessesHeaderCheckBoxNode;
        private readonly NodeTextBox _columnProcessesHeaderNameNode;
        private readonly NodeTextBox _columnProcessesHeaderPidNode;
        private readonly SortedTreeModel _model;
        private bool _recursiveCheck = true;
        private bool _inCheck;

        public class ProcessNode : Node
        {
            public ProcessNode(string procName)
                : base(procName)
            {
            }

            public string Name
            {
                get { return Text; }
                set { Text = value; }
            }

            public uint Pid { get; set; }
            public Image Icon { get; set; }
        }

        public ProcessesTreeView()
        {
            _columnProcessesHeaderName = new TreeColumn();
            _columnProcessesHeaderPid = new TreeColumn();
            _columnProcessesHeaderCheckBox = new TreeColumn();

            _columnProcessesHeaderName.Header = "Name";
            _columnProcessesHeaderName.Width = 134;
            _columnProcessesHeaderPid.Header = "Pid";
            _columnProcessesHeaderPid.TextAlign = HorizontalAlignment.Right;
            _columnProcessesHeaderPid.Width = 44;

            _columnHeaderFilenameIconNode = new NodeStateIcon
                                                {
                                                    DataPropertyName = "Icon",
                                                    LeftMargin = 1,
                                                    ParentColumn = _columnProcessesHeaderName
                                                };

            Columns.Add(_columnProcessesHeaderCheckBox);
            Columns.Add(_columnProcessesHeaderName);
            Columns.Add(_columnProcessesHeaderPid);

            _columnProcessesHeaderCheckBoxNode = new NodeCheckBox
                                                     {
                                                         ParentColumn = _columnProcessesHeaderCheckBox,
                                                         DataPropertyName = "CheckState",
                                                         EditEnabled = true
                                                     };
            _columnProcessesHeaderNameNode = new NodeTextBox();
            _columnProcessesHeaderNameNode.DataPropertyName = "Name";
            _columnProcessesHeaderNameNode.Trimming = StringTrimming.EllipsisCharacter;
            _columnProcessesHeaderNameNode.IncrementalSearchEnabled = false;
            _columnProcessesHeaderNameNode.LeftMargin = 3;
            _columnProcessesHeaderNameNode.ParentColumn = _columnProcessesHeaderName;

            _columnProcessesHeaderPidNode = new NodeTextBox();
            _columnProcessesHeaderPidNode.DataPropertyName = "Pid";
            _columnProcessesHeaderPidNode.LeftMargin = 3;
            _columnProcessesHeaderPidNode.ParentColumn = _columnProcessesHeaderPid;

            NodeControls.Add(_columnProcessesHeaderCheckBoxNode);
            NodeControls.Add(_columnHeaderFilenameIconNode);
            NodeControls.Add(_columnProcessesHeaderNameNode);
            NodeControls.Add(_columnProcessesHeaderPidNode);

            _columnProcessesHeaderCheckBoxNode.CheckStateChanged += AfterCheck;

            Model = _model = new SortedTreeModel(new TreeNodeAdvTools.FolderItemSorter(SortOrder.Ascending));
        }

        public void AddItem(ProcessNode node)
        {
            _model.Nodes.Add(node);
        }

        public TreeModel GetModel()
        {
            return _model;
        }

        public bool RecursiveCheck
        {
            get { return _recursiveCheck; }
            set { _recursiveCheck = value; }
        }

        public List<uint> GetCheckedItems()
        {
            var ret = new List<uint>();

            foreach (var item in _model.Nodes)
            {
                var procNode = (ProcessNode) item;
                ret.Add(procNode.Pid);
            }
            return ret;
        }

        public void ClearData(object sender, EventArgs e)
        {
            ClearData();
        }

        public delegate void ClearDataDelegate();

        public void ClearData()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ClearDataDelegate(ClearData));
            }
            else
            {
                BeginUpdate();
                _model.Nodes.Clear();
                EndUpdate();
            }
        }

        private void AfterCheck(object sender, TreeModelNodeEventArgs e)
        {
            if (_recursiveCheck && !_inCheck)
            {
                _inCheck = true;
                var n = e.Node;

                if (n.IsChecked)
                    CheckNode(n, n.IsChecked, true);
                _inCheck = false;
            }
        }

        public void CheckNode(Node n, bool checkState, bool recursive)
        {
            if (n.IsChecked != checkState)
            {
                n.CheckState = CheckState.Checked;
            }
            if (recursive)
            {
                foreach (var c in n.Nodes)
                {
                    CheckNode(c, checkState, true);
                }
            }
        }
    }
}