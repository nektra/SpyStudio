using System.Text.RegularExpressions;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;

namespace SpyStudio.Extensions
{
    public static class RegistryTreeNodeExtensions
    {
        public static bool CheckWholeUUIDGroupIf(this RegistryTreeNode aRegistryTreeNode, bool shouldBeChecked)
        {
            if (!shouldBeChecked)
                return false;

            var mainCLSIDNode = aRegistryTreeNode.GetFirstUUIDParentNode();

            mainCLSIDNode.CheckSelfAndChildrenRecursively();
            mainCLSIDNode.CheckSelfAndParents();

            return true;
        }

        public static RegistryTreeNode GetFirstUUIDParentNode(this RegistryTreeNode aRegistryTreeNode)
        {
            var endsInUUIDRegex =
                new Regex(@"{(\w|-)*}$");

            var firstUUIDParentNode = aRegistryTreeNode;

            while (!endsInUUIDRegex.IsMatch(firstUUIDParentNode.Path))
                firstUUIDParentNode = (RegistryTreeNode) firstUUIDParentNode.Parent;

            return firstUUIDParentNode;
        }
    }
}
