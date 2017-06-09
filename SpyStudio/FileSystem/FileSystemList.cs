using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.FileSystem.Compare;
using SpyStudio.Main;
using SpyStudio.Tools;
using System.Linq;
using SpyStudio.Extensions;

namespace SpyStudio.FileSystem
{
    public class FileSystemList : ListViewSorted, IFileSystemViewerControl
    {
        public class FileSystemListItem : InterpreterListItem, IFileSystemViewerItem
        {
            double _time, _time1, _time2;
            private uint _count1, _count2;
            readonly HashSet<string> _callerModules = new HashSet<string>();
            public HashSet<uint> Pids = new HashSet<uint>();
            private string _filepath1, _filepath2;
            private readonly bool _compareMode;
            private readonly int _descriptionIndex = 7, _companyIndex = 6;
            private const int VersionIndex = 5;
            private readonly int _timeIndex = 4, _countIndex = 3;
            private readonly bool _success;
            private readonly HashSet<CallEventId> _callEventIds;
            private readonly HashSet<DeviareTraceCompareItem> _compareItems;

            private readonly Dictionary<string, FileSystemItemDetails> _itemDetails =
                new Dictionary<string, FileSystemItemDetails>();

            public FileSystemListItem(string filestring, string filepath, bool success, bool compareMode)
            {
                Count = _count1 = _count2 = 0;
                _success = success;
                Access = Access1 = Access2 = FileSystemAccess.None;
                _time = _time1 = _time2 = 0;
                _compareMode = compareMode;
                Text = filestring;
                if (compareMode)
                {
                    SubItems.AddRange(new[] { "", "0", "0", "", "", "" });
                    _descriptionIndex = 5;
                    _companyIndex = 4;
                    _timeIndex = 3;
                    _countIndex = 2;
                }
                else
                {
                    SubItems.AddRange(new[] { "", "", "0", "0", "", "", "", filepath});
                }
                NotifyUpdate();
                _callEventIds = new HashSet<CallEventId>();
                _compareItems = new HashSet<DeviareTraceCompareItem>();
                Product = string.Empty;
                OriginalFileName = string.Empty;
            }
            public HashSet<string> CallerModules
            {
                get { return _callerModules; }
            }
            public string Description
            {
                get { return SubItems[_descriptionIndex].Text; }
                set { SubItems[_descriptionIndex].Text = value; }
            }
            public bool CompareMode
            {
                get { return _compareMode; }
            }
            public string Version
            {
                get { if(CompareMode) return string.Empty; return SubItems[VersionIndex].Text; }
                set { if(!CompareMode) SubItems[VersionIndex].Text = value; }
            }

            public string Company
            {
                get { return SubItems[_companyIndex].Text; }
                set { SubItems[_companyIndex].Text = value; }
            }

            public string Product { get; set; }
            public string OriginalFileName { get; set; }
            public string AccessString
            {
                get
                {
                    return FileSystemViewer.GetAccessString(this);
                }
            }

            public string Result
            {
                get
                {
                    if (CompareMode) return string.Empty;
                    return SubItems[2].Text;
                }
                set { if (!CompareMode) SubItems[2].Text = value; }
            }

            public Image Icon { get; set; }

            public FileSystemAccess Access { get; set; }
            public FileSystemAccess Access1 { get; set; }
            public FileSystemAccess Access2 { get; set; }

            public string FilePath
            {
                get
                {
                    return CompareMode ? string.Empty : SubItems[8].Text;
                }
                set { if (!CompareMode) SubItems[8].Text = value; }
            }

            public string FilePath1
            {
                get { return _filepath1; }
                set
                {
                    _filepath1 = value;
                    UpdatePath();
                }
            }

            public string FilePath2
            {
                get { return _filepath2; }
                set
                {
                    _filepath2 = value;
                    UpdatePath();
                }
            }

            void UpdatePath()
            {
                if (CompareMode)
                    return;
                if (Count1 > 0 && Count2 > 0)
                {
                    if (String.Compare(_filepath1, _filepath2, StringComparison.OrdinalIgnoreCase) == 0)
                        SubItems[8].Text = (_filepath1.ForCompareString() + " / " + _filepath2.ForCompareString());
                }
                else if (Count1 > 0)
                {
                    SubItems[8].Text = _filepath1;
                }
                else if (Count2 > 0)
                {
                    SubItems[8].Text = _filepath2;
                }
                else
                    SubItems[8].Text = "";
            }
            public string FileString
            {
                get { return SubItems[0].Text; }
                set { SubItems[0].Text = value; }
            }

            public override bool Success { get { return _success; } }

            public void NotifyUpdate()
            {
                SubItems[_countIndex].Text = CountString;
                SubItems[1].Text = AccessString;
                SubItems[_timeIndex].Text = TimeString;
            }
            public string CountString
            {
                get
                {
                    return FileSystemViewer.GetCountString(this);
                }
            }

            public uint Count { get; set; }

            public uint Count1
            {
                get { return _count1; }
                set
                {
                    _count1 = value;
                    UpdateColor();
                }
            }

            public uint Count2
            {
                get { return _count2; }
                set
                {
                    _count2 = value;
                    UpdateColor();
                }
            }

            void UpdateColor()
            {
                if (_count1 > 0 && _count2 > 0)
                {
                    var exactMatch = false;
                    var majorVersionMatch = false;
                    var versions1 = new HashSet<string>();
                    var versions2 = new HashSet<string>();
                    var majorVersions1 = new HashSet<string>();
                    var majorVersions2 = new HashSet<string>();
                    bool success1 = false, success2 = false;

                    foreach (var itemDetails in _itemDetails)
                    {
                        string minorVersion = null;
                        if (itemDetails.Value.CompareItemType == CompareItemType.Item1)
                        {
                            versions1.Add(itemDetails.Value.Version);
                            majorVersions1.Add(FileSystemTools.GetMajorVersion(itemDetails.Value.Version,
                                                                               ref minorVersion));
                            success1 |= itemDetails.Value.Success;
                        }
                        else if (itemDetails.Value.CompareItemType == CompareItemType.Item2)
                        {
                            versions2.Add(itemDetails.Value.Version);
                            majorVersions2.Add(FileSystemTools.GetMajorVersion(itemDetails.Value.Version,
                                                                               ref minorVersion));
                            success2 |= itemDetails.Value.Success;
                        }
                        else
                        {
                            success1 |= itemDetails.Value.Success;
                            success2 |= itemDetails.Value.Success;
                            exactMatch = true;
                        }
                    }
                    var intersection = versions1.Intersect(versions2);
                    if (intersection.Any())
                        exactMatch = true;
                    else
                    {
                        intersection = majorVersions1.Intersect(majorVersions2);
                        if (intersection.Any())
                            majorVersionMatch = true;
                    }
                    string matchType;
                    if(success1 == success2)
                    {
                        BackColor = ListView.BackColor;
                        Font = ListView.Font;

                        if (success1)
                        {
                            if (exactMatch)
                                ForeColor = EntryColors.SuccessColorByPrioritySummary[2];
                            else if (majorVersionMatch)
                                ForeColor = EntryColors.SuccessColorByPrioritySummary[1];
                            else
                                ForeColor = EntryColors.SuccessColorByPrioritySummary[0];
                            matchType = exactMatch
                                             ? "Equal Version"
                                             : majorVersionMatch ? "Different Minor Version" : "Different Version";
                        }
                        else
                        {
                            if (exactMatch)
                                ForeColor = EntryColors.ErrorColorByPrioritySummary[2];
                            else if (majorVersionMatch)
                                ForeColor = EntryColors.ErrorColorByPrioritySummary[1];
                            else
                                ForeColor = EntryColors.ErrorColorByPrioritySummary[0];
                            matchType = "Both Failed";
                        }
                    }
                    else
                    {
                        BackColor = ListView.BackColor;
                        Font = new Font(ListView.Font, FontStyle.Bold);
                        ForeColor = EntryColors.MatchResultMismatchColor;
                        matchType = "Different Results";
                    }
                    SubItems[6].Text = matchType;
                }
                else if (_count1 > 0)
                {
                    BackColor = ((FileSystemList)ListView).Viewer.File1BackgroundColor;
                    SubItems[6].Text = "Only First Trace";
                }
                else
                {
                    BackColor = ((FileSystemList)ListView).Viewer.File2BackgroundColor;
                    SubItems[6].Text = "Only Second Trace";
                }
            }
            public double Time
            {
                get
                {
                    return _time;
                }
            }
            public string TimeString
            {
                get
                {
                    return FileSystemViewer.GetTimeString(this);
                }
            }
            public double Time1
            {
                get
                {
                    return _time1;
                }
            }
            public double Time2
            {
                get
                {
                    return _time2;
                }
            }

            public override EntryPropertiesDialogBase GetPropertiesDialog()
            {
                return CompareMode ? (EntryPropertiesDialogBase) new EntryComparePropertiesDialog(this) : new EntryPropertiesDialog(this) ;
            }

            public override string NameForDisplay
            {
                get { return Text; }
            }

            public bool IsForCompare
            {
                get { return CompareMode; }
            }

            public bool IsDirectory { get; set; }

            public IInterpreter Interpreter { get { return (IInterpreter) ListView; } }
            public override HashSet<CallEventId> CallEventIds
            {
                get { return _callEventIds; }
            }

            public override HashSet<DeviareTraceCompareItem> CompareItems
            {
                get { return _compareItems; }
            }

            public void GetChecked()
            {
                throw new NotImplementedException();
            }

            public void GetUnchecked()
            {
                throw new NotImplementedException();
            }

            public override void Accept(IEntryVisitor aVisitor)
            {
                aVisitor.Visit(this);
            }

            public bool SupportsGoTo()
            {
                throw new NotImplementedException();
            }

            public void AddCallEventsTo(TraceTreeView aTraceTreeView)
            {
                foreach (var eventId in CallEventIds)
                    aTraceTreeView.InsertNode(eventId);
            }

            public void AddAccess(FileSystemAccess access)
            {
                Access = Access | access;
            }
            public void AddAccess1(FileSystemAccess access)
            {
                Access1 = Access1 | access;
            }
            public void AddAccess2(FileSystemAccess access)
            {
                Access2 = Access2 | access;
            }
            public void AddCall(CallEvent callEvent)
            {
                CallEventIds.Add(callEvent.EventId);
                _time += callEvent.Time;
                Count++;
            }
            public void AddCall(CompareItemType itemType, double time, string path, FileSystemAccess access,
                string version, bool success, string result, CallEventId eventId, DeviareTraceCompareItem compareItem)
            {
                var pathLower = path.ToLower();
                var key = pathLower + "|" + version + "|" + result;
                FileSystemItemDetails itemDetails;
                if (!_itemDetails.TryGetValue(key, out itemDetails))
                {
                    itemDetails = new FileSystemItemDetails
                                      {
                                          CompareItemType = itemType,
                                          Path = path,
                                          Version = version,
                                          Success = success,
                                          Result = result,
                                      };
                    _itemDetails.Add(key, itemDetails);
                }
                itemDetails.CallEventIds.Add(eventId);
                itemDetails.CompareItems.Add(compareItem);

                if (itemType == CompareItemType.Item1)
                {
                    itemDetails.Access1 |= access;
                    itemDetails.CompareItemType |= CompareItemType.Item1;
                    itemDetails.Count1++;
                    itemDetails.Time1 += time;
                    _time1 += time;
                    AddAccess1(access);
                    Count1++;
                }
                else
                {
                    itemDetails.Access2 |= access;
                    itemDetails.CompareItemType |= CompareItemType.Item2;
                    itemDetails.Count2++;
                    itemDetails.Time2 += time;
                    _time2 += time;
                    AddAccess2(access);
                    Count2++;
                }
            }

            public void Populate(FileSystemListDetails detailsList)
            {
                detailsList.BeginUpdate();
                detailsList.Items.Clear();
                foreach(var itemDetails in _itemDetails)
                {
                    itemDetails.Value.PrepareToShow(detailsList);
                    detailsList.Items.Add(itemDetails.Value);
                }
                detailsList.EndUpdate();
            }
        }

        readonly Dictionary<string, FileSystemListItem> _fileInfos = new Dictionary<string, FileSystemListItem>();
        readonly ColumnHeader _columnName = new ColumnHeader();
        readonly ColumnHeader _columnAccess = new ColumnHeader();
        readonly ColumnHeader _columnResult = new ColumnHeader();
        readonly ColumnHeader _columnCount = new ColumnHeader();
        readonly ColumnHeader _columnTime = new ColumnHeader();
        readonly ColumnHeader _columnVersion = new ColumnHeader();
        readonly ColumnHeader _columnCompany = new ColumnHeader();
        readonly ColumnHeader _columnDescription = new ColumnHeader();
        readonly ColumnHeader _columnPath = new ColumnHeader();
        readonly ColumnHeader _columnMatchType = new ColumnHeader();
        private FileSystemListDetails _detailsList;

        public FileSystemList()
        {
            
            _columnName.Text = "Filename";
            _columnName.Width = 200;

            _columnAccess.Text = "Access";
            _columnAccess.Width = 150;

            _columnResult.Text = "Result";
            _columnResult.Width = 80;

            _columnCount.Text = "Count";
            _columnCount.Width = 60;
            _columnCount.Tag = "Numeric";
            _columnCount.TextAlign = HorizontalAlignment.Right;

            _columnTime.Text = "Time";
            _columnTime.Width = 60;
            _columnTime.Tag = "Double";
            _columnTime.TextAlign = HorizontalAlignment.Right;

            _columnVersion.Text = "Version";
            _columnVersion.Width = 100;

            _columnCompany.Text = "Company";
            _columnCompany.Width = 150;

            _columnDescription.Text = "Description";
            _columnDescription.Width = 200;

            _columnPath.Text = "Path";
            _columnPath.Width = 200;

            _columnMatchType.Text = "Match Type";
            _columnMatchType.Width = 80;
            
            Columns.AddRange(new[] {
            _columnName,
            _columnAccess,
            _columnResult,
            _columnCount,
            _columnTime,
            _columnVersion,
            _columnCompany,
            _columnDescription,
            _columnPath});

            FullRowSelect = true;
            HideSelection = false;
            Margin = new Padding(0, 0, 0, 0);
            Name = "listViewFileSystem";
            Sorting = SortOrder.Descending;
            View = View.Details;

            SmallImageList = new ImageList();

            SelectedIndexChanged += OnSelectedIndexChanged;
            ItemChecked += OnItemChecked;
            CreateContextMenu();
        }

        private void OnSelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            if(_detailsList !=  null)
            {
                if(SelectedItems.Count > 0)
                {
                    var selectedItem = SelectedItems[0] as FileSystemListItem;
                    if(selectedItem != null)
                    {
                        selectedItem.Populate(_detailsList);
                    }
                }
            }
        }

        public void SetDetailsListView(FileSystemListDetails detailsList)
        {
            _detailsList = detailsList;
        }
        private void OnItemChecked(object sender, ItemCheckedEventArgs itemCheckedEventArgs)
        {
            CheckedChanged = true;
        }

        private void CreateContextMenu()
        {
            if (ContextMenuStrip != null) return;
            ContextMenuStrip = new ContextMenuStrip();
        }

        public FileSystemViewer Viewer
        {
            get
            {
                var parent = Parent;
                while((parent as FileSystemViewer) == null && parent != null)
                {
                    parent = parent.Parent;
                }
                Debug.Assert(parent != null);
                return (FileSystemViewer) parent; 
            }
        }


        #region IFileSystemViewerControl
        public new void Clear()
        {
            _fileInfos.Clear();
            BeginUpdate();
            Items.Clear();
            EndUpdate();
        }
        public PathNormalizer PathNormalizer { get; set; }

        public void ProcessEvent(string eventPath, string fileSystemPath, CallEvent callEvent, Image icon, DeviareTraceCompareItem traceItem)
        {
            if (FileSystemEvent.IsDirectory(callEvent))
                return;

            var filepart = FileSystemEvent.GetFilepart(callEvent);

            FileSystemListItem item;

            var pathLower = eventPath.ToLower();
            string fileInfoKey;

            if (!CompareMode)
            {
                // group success events by file version is possible
                string key;
                if (!callEvent.IsProcMon && callEvent.Success && FileSystemEvent.IsFileInfoSet(callEvent))
                {
                    key = filepart + "!" + FileSystemEvent.GetVersion(callEvent);
                }
                else
                {
                    key = pathLower;
                }
                fileInfoKey = key + callEvent.Success;
            }
            else
            {
                fileInfoKey = filepart.ToLower();
            }

            if (!_fileInfos.TryGetValue(fileInfoKey, out item))
            {
                if (item == null)
                {
                    item = new FileSystemListItem(filepart, eventPath, callEvent.Success, CompareMode)
                               {
                                   Result = callEvent.Result,
                                   ForeColor = !callEvent.Success ? Color.Red : Color.Black,
                                   Icon = icon
                               };

                    var index = SmallImageList.Images.IndexOfKey(pathLower);
                    if (index != -1)
                    {
                        item.ImageIndex = index;
                    }
                    else if (icon != null)
                    {
                        SmallImageList.Images.Add(pathLower, icon);
                        index = SmallImageList.Images.IndexOfKey(pathLower);
                        item.ImageIndex = index;
                    }

                    if (FileSystemEvent.IsFileInfoSet(callEvent))
                    {
                        item.Company = FileSystemEvent.GetCompany(callEvent);
                        item.Version = FileSystemEvent.GetVersion(callEvent);
                        item.Description = FileSystemEvent.GetDescription(callEvent);
                        item.Product = FileSystemEvent.GetProduct(callEvent);
                        item.OriginalFileName = FileSystemEvent.GetOriginalFileName(callEvent);
                    }

                    item.IsDirectory = FileSystemEvent.IsDirectory(callEvent);

                    Items.Add(item);

                    _fileInfos[fileInfoKey] = item;
                }
            }

            var access = FileSystemTools.GetEventAccess(callEvent);

            item.CallEventIds.Add(callEvent.EventId);
            item.CompareItems.Add(traceItem);

            if (!CompareMode)
            {
                item.AddCall(callEvent);

                item.CallerModules.Add(callEvent.CallModule);
                item.Pids.Add(callEvent.Pid);
                item.AddAccess(access);
            }
            else
            {
                string version = null;
                if (FileSystemEvent.IsFileInfoSet(callEvent))
                {
                    version = FileSystemEvent.GetVersion(callEvent);
                }
                Debug.Assert(Viewer.File1TraceId == callEvent.TraceId || Viewer.File2TraceId == callEvent.TraceId);
                item.AddCall(
                    Viewer.File1TraceId == callEvent.TraceId ? CompareItemType.Item1 : CompareItemType.Item2,
                    callEvent.Time, eventPath, access,
                    string.IsNullOrEmpty(version) ? string.Empty : version, callEvent.Success,
                    callEvent.Result, callEvent.EventId, traceItem);
            }

            item.NotifyUpdate();
        }

        private bool _compareMode;

        public bool CompareMode
        {
            get { return _compareMode; }
            set
            {
                if (value == _compareMode)
                    return;

                _compareMode = value;
                if (_compareMode)
                {
                    Columns.Remove(_columnVersion);
                    Columns.Remove(_columnPath);
                    Columns.Remove(_columnResult);
                    Columns.Add(_columnMatchType);
                }
                else
                {
                    Columns.Remove(_columnMatchType);
                    Columns.Insert(2, _columnResult);
                    Columns.Insert(5, _columnVersion);
                    Columns.Add(_columnPath);
                }
            }
        }

        public bool CheckedChanged { get; set; }

        public bool IsEmpty()
        {
            return Items.Count == 0;
        }

        public List<FileEntry> GetAccessedFiles()
        {
            var files = new List<FileEntry>();

            foreach (var item in _fileInfos)
            {
                var fileEntry = new FileEntry(item.Value.FilePath, item.Value.FilePath, item.Value.Access, item.Value.Success,
                                              item.Value.Company, item.Value.Version, item.Value.Description,
                                              item.Value.Product, item.Value.OriginalFileName, item.Value.Icon);
                files.AddRange(fileEntry.GetEntries());
            }

            return files;
        }

        public List<FileEntry> GetCheckedItems()
        {
            var files = new List<FileEntry>();
            if (!CheckBoxes)
                return files;

            foreach (var item in _fileInfos)
            {
                if (item.Value.Checked)
                {
                    var fileEntry = new FileEntry(item.Value.FilePath, item.Value.FilePath, item.Value.Access, item.Value.Success,
                                                  item.Value.Company, item.Value.Version, item.Value.Description,
                                                  item.Value.Product, item.Value.OriginalFileName, item.Value.Icon);
                    files.AddRange(fileEntry.GetEntries());
                }
            }

            return files;
        }

        public List<string> GetCheckedPaths(string rootPath)
        {
            throw new NotImplementedException();
        }

        public List<FileEntry> GetCheckedFiles()
        {
            return GetCheckedItems();
        }

        public bool Find(FindEventArgs findEvent)
        {
            return ListViewTools.Find(this, findEvent);
        }

        public void CopySelectionToClipboard()
        {
            ListViewTools.CopySelectionToClipboard(this);
        }

        public void SelectAll()
        {
            ListViewTools.SelectAll(this);
        }
        public void SelectFirstItem()
        {
            if (Items.Count > 0)
            {
                Items[0].Selected = true;
                FocusedItem = Items[0];
            }
        }
        public void SelectLastItem()
        {
            if (Items.Count > 0)
            {
                Items[Items.Count - 1].Selected = true;
                FocusedItem = Items[Items.Count - 1];
            }
        }

        public void FindEvent(FindEventArgs findEvent)
        {
            Find(findEvent);
        }

        public IInterpreterController Controller { get; set; }

        public EntryContextMenu ContextMenuController { get; set; }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return SelectedNodes.Select(node => (IEntry) node); }
        }

        public Control ParentControl { get; private set; }
        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        public IEnumerable<IFileSystemViewerItem> SelectedNodes
        {
            get
            {
                return Items.Cast<FileSystemListItem>().Where(item => item.Selected).Cast<IFileSystemViewerItem>();
            }
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }

        public Control Control
        {
            get { return this; }
        }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            var item = anEntry as ListViewItem;
            if (item == null)
            {
                return;
            }
            var itemToDeselect = Items.Cast<ListViewItem>().FirstOrDefault(i => i.Equals(anEntry));
            if(itemToDeselect == null)
            {
                return;
            }

            if (itemToDeselect.Index == Items.Count - 1)
                return;

            var nextEntry = anEntry.NextVisibleEntry;
            var itemToSelect = Items.Cast<ListViewItem>().First(i => i.Equals(nextEntry));

            itemToDeselect.Selected = false;
            itemToSelect.Selected = true;
            itemToSelect.EnsureVisible();
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            var item = anEntry as ListViewItem;
            if(item == null)
            {
                return;
            }
            var itemToDeselect = Items.Cast<ListViewItem>().FirstOrDefault(i => i.Equals(anEntry));
            if (itemToDeselect == null)
            {
                return;
            }

            if (itemToDeselect.Index == 0)
                return;

            var previousEntry = anEntry.PreviousVisibleEntry;
            var itemToSelect = Items.Cast<ListViewItem>().First(i => i.Equals(previousEntry));

            itemToDeselect.Selected = false;
            itemToSelect.Selected = true;
            itemToSelect.EnsureVisible();
        }

        public void Accept(FileSystemTreeChecker aFileChecker)
        {
            throw new NotImplementedException();
        }

        public IEnumerable AllItems
        {
            get { return Items; }
        }

        public bool ExistItem(string path, out bool isChecked)
        {
            throw new NotImplementedException();
        }
        public void CheckPath(string path, bool isChecked, bool isRecursive)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IFileSystemViewerItem> GetAllItems()
        {
            return (IEnumerable<IFileSystemViewerItem>) Items;
        }

        public IFileSystemViewerItem AddFileEntry(FileEntry aFileEntry)
        {
            throw new NotImplementedException();
        }

        public IFileSystemViewerItem AddFileEntryUncolored(FileEntry aFileEntry)
        {
            throw new NotImplementedException();
        }

        public void ArrangeForExportWizard()
        {
            Columns.Clear();
            Columns.Add(_columnName);
            Columns.Add(_columnDescription);
            Columns.Add(_columnVersion);
            Columns.Add(_columnCompany);
        }

        #endregion IFileSystemViewerControl

    }
}