using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Tools;

namespace SpyStudio.FileSystem
{
    /// <summary>
    /// Implementation of a control that shows file system events.
    /// </summary>
    public interface IFileSystemViewerControl : IInterpreter
    {
        void ProcessEvent(string processedPath, string fileSystemPath, CallEvent callEvent, Image icon,
                          DeviareTraceCompareItem item);

        bool InvokeRequired { get; }
        void BeginUpdate();
        void EndUpdate();
        bool CheckedChanged { get; set; }
        bool CheckBoxes { get; set; }
        bool CompareMode { get; set; }
        void Clear();
        bool IsEmpty();
        List<FileEntry> GetAccessedFiles();
        List<FileEntry> GetCheckedFiles();
        List<FileEntry> GetCheckedItems();
        List<string> GetCheckedPaths(string rootPath);
        //Return true iff any items were checked.
        bool Find(FindEventArgs findEvent);
        void SelectFirstItem();
        void SelectLastItem();
        PathNormalizer PathNormalizer { get; set; }

        Control Control { get; }
        void Accept(FileSystemTreeChecker aFileChecker);
        bool ExistItem(string path, out bool isChecked);
        void CheckPath(string path, bool checkedState, bool recursive);
        IEnumerable<IFileSystemViewerItem> GetAllItems();
        IFileSystemViewerItem AddFileEntry(FileEntry aFileEntry);
        IFileSystemViewerItem AddFileEntryUncolored(FileEntry aFileEntry);
        void ArrangeForExportWizard();
    }
}