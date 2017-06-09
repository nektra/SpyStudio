using System.Collections.Generic;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.FileSystem
{
    public class FileSystemEntry : IInterpreterEntry
    {
        private readonly IFileSystemViewerItem _fileSystemNode;

        public FileSystemEntry(IFileSystemViewerItem aFileSystemNode)
        {
            _fileSystemNode = aFileSystemNode;
        }

        public HashSet<CallEvent> GetCallEvents()
        {
            return CallEvent.GetNonGeneratedEvents(_fileSystemNode.CallEventIds);
        }

        public HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return _fileSystemNode.CompareItems; }
        }

        public IInterpreterEntry NextVisibleEntry
        {
            get
            {
                var n = _fileSystemNode.GetNextVisibleNode();
                return n == null ? null : new FileSystemEntry(n);
            }
        }

        public IInterpreterEntry PreviousVisibleEntry
        {
            get
            {
                var n = _fileSystemNode.GetPreviousVisibleNode();
                return n == null ? null : new FileSystemEntry(n);
            }
        }

        public object Tag
        {
            get { return _fileSystemNode; }
        }

        public string NameForDisplay
        {
            get { return _fileSystemNode.NameforDisplay; }
        }

        public bool IsForCompare
        {
            get { return _fileSystemNode.CompareMode; }
        }

        public CompareItemType ItemType { get; set; }

        public bool SupportsGoTo()
        {
            return _fileSystemNode.SupportsGoTo();
        }

        public void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            aTraceTreeView.AddEventsOfSingleTrace(GetCallEvents());
        }

        public void Accept(IInterpreterEntryVisitor aVisitor)
        {
            _fileSystemNode.Accept(aVisitor);
        }
    }
}