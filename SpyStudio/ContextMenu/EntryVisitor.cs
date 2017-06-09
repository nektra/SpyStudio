using SpyStudio.COM.Controls;
using SpyStudio.COM.Controls.Compare;
using SpyStudio.Dialogs.Compare;
using SpyStudio.FileSystem;
using SpyStudio.Main;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Controls.Compare;
using SpyStudio.Windows.Controls;
using SpyStudio.Windows.Controls.Compare;

namespace SpyStudio.ContextMenu
{
    public interface IEntryVisitor
    {
        void Visit(TraceTreeView.TraceNode aTraceNode);
        void Visit(DeviareTraceCompareItem aCompareTraceNode);
        void Visit(RegistryTreeNode aRegistryTreeNode);
        void Visit(FileSystemTreeNode aFileSystemTreeNode);
        void Visit(FileSystemList.FileSystemListItem aFileSystemListItem);
        void Visit(ComObjectListViewItem aComListItem);
        void Visit(WindowListViewItem aWindowListItem);
        void Visit(RegistryValueItem aRegistryValueItem);
        void Visit(CompareComObjectListViewItem aFileSystemTreeNode);
        void Visit(CompareRegistryValueItem aCompareRegistryValueItem);
        void Visit(CompareWindowListViewItem aCompareWindowListViewItem);
    }
}