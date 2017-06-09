using System.Collections.Generic;
using SpyStudio.Main;
using SpyStudio.Registry.Infos;

namespace SpyStudio.Registry.Controls
{
    public class RegistryTreeRootNode : RegistryTreeNode
    {
        public override string Path
        {
            get { return ""; }
        }

        public override string NormalizedPath
        {
            get { return ""; }
        }

        public override SortedDictionary<string, RegistryTreeNodeBase> ChildrenByName
        {
            get { return ((RegistryTree)Tree).BaseNodesByName; }
        }

        public override NodeCollection Nodes
        {
            get { return Model.Root.Nodes; }
        }

        public override int Depth
        {
            get { return 0; }
        }

        public static RegistryTreeNodeBase Of(RegistryTree aTree)
        {
            return new RegistryTreeRootNode(aTree);
        }

        private RegistryTreeRootNode(RegistryTree aTree) : base("Root")
        {
            Tree = aTree;
            Model = aTree.Model;
        }

        public override void Merge(RegKeyInfo aKeyInfo)
        {
            
        }
    }
}