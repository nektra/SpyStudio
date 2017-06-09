using System.Collections.Generic;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.FileSystem
{
    public interface IFileSystemViewerFileDetailItem : IEntry
    {
        FileSystemAccess Access { get; }
        FileSystemAccess Access1 { get; }
        FileSystemAccess Access2 { get; }
        uint Count { get; }
        uint Count1 { get; }
        uint Count2 { get; }
        double Time { get; }
        double Time1 { get; }
        double Time2 { get; }
        bool CompareMode { get; }
    }
    public interface IFileSystemViewerItem : IFileSystemViewerFileDetailItem
    {
        string FilePath { get; }
        string Company { get; }
        string Version { get; }
        string Description { get; }
        HashSet<string> CallerModules { get; }
        bool IsDirectory { get; set; }
        bool Checked { get; set; }
    }
}