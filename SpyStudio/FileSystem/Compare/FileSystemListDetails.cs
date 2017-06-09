using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Main;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.FileSystem.Compare
{
    public class FileSystemItemDetails : ListViewItem, IFileSystemViewerFileDetailItem
    {
        readonly HashSet<DeviareTraceCompareItem> _compareItems = new HashSet<DeviareTraceCompareItem>();
        private FileSystemListDetails _listView;
        public FileSystemItemDetails()
        {
            CallEventIds = new HashSet<CallEventId>();
            Access1 = Access2 = FileSystemAccess.None;
        }
        public string Version { get; set; }
        public string Path { get; set; }

        public uint Count
        {
            get { throw new System.NotImplementedException(); }
        }

        public uint Count1 { get; set; }
        public uint Count2 { get; set; }

        public double Time
        {
            get { throw new System.NotImplementedException(); }
        }

        public double Time1 { get; set; }
        public double Time2 { get; set; }

        public bool CompareMode
        {
            get { return true; }
        }

        public FileSystemAccess Access
        {
            get { throw new System.NotImplementedException(); }
        }

        public FileSystemAccess Access1 { get; set; }
        public FileSystemAccess Access2 { get; set; }
        public string Result { get; set; }
        public CompareItemType CompareItemType { get; set; }

        public IInterpreter Interpreter { get { return _listView; } }
        public HashSet<CallEventId> CallEventIds { get; private set; }
        public HashSet<DeviareTraceCompareItem> CompareItems { get { return _compareItems; } }
        public bool Success { get; set; }

        public void PrepareToShow(FileSystemListDetails listView)
        {
            _listView = listView;

            if (SubItems.Count != _listView.Columns.Count)
            {
                Text = Path;
                SubItems.Add(FileSystemViewer.GetAccessString(this));
                SubItems.Add(Version);
                SubItems.Add(Result);
                SubItems.Add(FileSystemViewer.GetCountString(this));
                SubItems.Add(FileSystemViewer.GetTimeString(this));
                BackColor = listView.BackColor;
                if (Success)
                {
                    if (Count1 > 0 && Count2 > 0)
                    {
                        ForeColor = EntryColors.MatchSuccessColor;
                        BackColor = listView.BackColor;
                    }
                    else
                    {
                        ForeColor = EntryColors.NoMatchSuccessColor;
                        BackColor = Count1 > 0 ? EntryColors.File1Color : EntryColors.File2Color;
                    }
                }
                else
                {
                    if (Count1 > 0 && Count2 > 0)
                    {
                        ForeColor = EntryColors.MatchErrorColor;
                        BackColor = listView.BackColor;
                    }
                    else
                    {
                        ForeColor = EntryColors.NoMatchErrorColor;
                        BackColor = Count1 > 0 ? EntryColors.File1Color : EntryColors.File2Color;
                    }
                }
            }
        }
        public IEntry NextVisibleEntry
        {
            get
            {
                if (ListView == null)
                    return null;

                var nextItemIndex = ListView.Items.IndexOf(this) + 1;

                if (nextItemIndex >= ListView.Items.Count)
                    return null;
                return (IEntry)ListView.Items[nextItemIndex];
            }
        }

        public IEntry PreviousVisibleEntry
        {
            get
            {
                if (ListView == null)
                    return null;

                var previousItemIndex = ListView.Items.IndexOf(this) - 1;

                if (previousItemIndex < 0)
                    return null;
                return (IEntry)ListView.Items[previousItemIndex];
            }
        }

        public string NameForDisplay
        {
            get { return Path; }
        }

        public virtual bool IsForCompare
        {
            get { return CompareItems.Any(); }
        }

        public CompareItemType ItemType { get; set; }

        public EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryComparePropertiesDialog(this);
        }

        public bool SupportsGoTo 
        {
            get { return false; }
        }

        public void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            foreach (var eventId in CallEventIds)
                aTraceTreeView.InsertNode(eventId);
        }

        public void Accept(IEntryVisitor aVisitor)
        {
            //aVisitor.Visit(this);
        }
    }

    public class FileSystemListDetails : ListViewSorted, IInterpreter
    {
        public uint File1TraceId;
        public uint File2TraceId;

        private readonly ColumnHeader _columnHeaderDetailsPath;
        private readonly ColumnHeader _columnHeaderAccess;
        private readonly ColumnHeader _columnHeaderVersion;
        private readonly ColumnHeader _columnHeaderDetailsResult;
        private readonly ColumnHeader _columnHeaderDetailsCount;
        private readonly ColumnHeader _columnHeaderDetailsTime;

        public FileSystemListDetails()
        {
            FullRowSelect = true;

            View = View.Details;

            _columnHeaderDetailsPath = new ColumnHeader();
            _columnHeaderAccess = new ColumnHeader();
            _columnHeaderVersion = new ColumnHeader();
            _columnHeaderDetailsResult = new ColumnHeader();
            _columnHeaderDetailsTime = new ColumnHeader();
            _columnHeaderDetailsCount = new ColumnHeader();

            _columnHeaderAccess.Text = "Access";
            _columnHeaderAccess.Width = 148;
            _columnHeaderVersion.Text = "Version";
            _columnHeaderDetailsResult.Text = "Result";
            _columnHeaderDetailsTime.Text = "Time";
            _columnHeaderDetailsCount.Text = "Count";
            _columnHeaderDetailsPath.Text = "Path";
            _columnHeaderDetailsPath.Width = 260;

            Columns.AddRange(new[] {
            _columnHeaderDetailsPath,
            _columnHeaderAccess,
            _columnHeaderVersion,
            _columnHeaderDetailsResult,
            _columnHeaderDetailsCount,
            _columnHeaderDetailsTime});
        }

        public IInterpreterController Controller { get; set; }

        public EntryContextMenu ContextMenuController { get { return null; } }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return SelectedItems.Cast<IEntry>(); }
        }

        public Control ParentControl
        {
            get { return Parent; }
        }

        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            var nextEntry = (ListViewItem) anEntry.NextVisibleEntry;

            if (nextEntry == null)
                return;

            SelectedItems.Clear();
            nextEntry.Selected = true;
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            var previousEntry = ((ListViewItem)anEntry.PreviousVisibleEntry);

            if (previousEntry == null)
                return;

            SelectedItems.Clear();
            previousEntry.Selected = true;
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }
        public void FindEvent(FindEventArgs e)
        {
        }
        public void CopySelectionToClipboard()
        {
        }
        public void SelectAll()
        {
        }

    }
}
