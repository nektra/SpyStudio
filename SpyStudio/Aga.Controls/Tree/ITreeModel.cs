using System;

namespace Aga.Controls.Tree
{
	public interface ITreeModel
	{
        Node.NodeCollection CreateNodeCollection(Node node);
        Node.NodeCollection Nodes { get; }
        Node Root { get; set; }
        

        event Action<Node> NodeCheckChanged;
        event Action<Node> NodeChanged;
        event Action<Node> NodeInserted;
        /// <summary>
        /// Triggered before the node is detached from the Model
        /// </summary>
        event Action<Node> NodeBeforeRemove;
        /// <summary>
        /// Triggered after the node is detached from the Model
        /// </summary>
        event Action<Node, int, Node> NodeRemoved;
        event Action<Node> NodesBeforeClear;
        event Action<Node> NodesCleared;
		event Action<TreePath> StructureChanged;
        event Action<Node> NodeSelectedChanged;
        event Action<Node, bool, bool> NodeExpandedChanged;
	    void OnNodeSelectedChanged(Node node);
	    void OnNodesCheckChanged(Node node);
	    void OnNodeExpandedChanged(Node node, bool expand, bool ignoreChildren);
	    void OnNodeChanged(Node node);
	}
}
