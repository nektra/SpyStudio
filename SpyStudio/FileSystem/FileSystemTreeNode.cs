using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Main;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.FileSystem
{
    public class FileSystemTreeNode : InterpreterNode, TreeNodeAdvTools.ITreeViewAdvComparableNode,
                                      IFileSystemViewerItem
    {
        private double _time, _time1, _time2;
        private uint _count1, _count2;
        private readonly bool _compareMode;
        private readonly FileSystemTree _treeView;
        private readonly HashSet<string> _callerModules = new HashSet<string>();
        public HashSet<uint> Pids = new HashSet<uint>();

        public FileSystemTreeNode(string filestring, string filepath, string fileSystemPath, bool success, FileSystemTree treeView) : base(filestring)
        {
            Tree = treeView;
            Count = _count1 = _count2 = 0;
            FileString = filestring;
            FilePath = filepath;
            FileSystemPath = fileSystemPath;
            Success = success;
            ForeColor = Success ? Color.Black : Color.Red;
            Access = Access1 = Access2 = FileSystemAccess.None;
            _time = _time1 = _time2 = 0;
            _compareMode = treeView.CompareMode;
            _treeView = treeView;
            Description = Company = Version = "";
            _callEventIds = new HashSet<CallEventId>();
            CompareItems = new HashSet<DeviareTraceCompareItem>();
        }

        public HashSet<string> CallerModules
        {
            get { return _callerModules; }
        }

        public string Description { get; set; }
        public string Version { get; set; }
        public string Company { get; set; }
        public string Product { get; set; }
        public string OriginalFileName { get; set; }
        public Image Icon { get; set; }
        public Image IconExpanded { get; set; }

        public bool IsDirectory { get; set; }
        public bool IsDirectoryOrBranch
        {
            get { return IsDirectory || !IsLeaf; }
        }
        private HashSet<CallEventId> _callEventIds;

        public override ThinAppIsolationOption Isolation
        {
            get { return IsDirectoryOrBranch ? base.Isolation : ThinAppIsolationOption.None; }
            set { base.Isolation = value; }
        }

        public string AccessString
        {
            get { return FileSystemViewer.GetAccessString(this); }
        }

        public string Result { get; set; }
        public FileSystemAccess Access { get; set; }
        public FileSystemAccess Access1 { get; set; }
        public FileSystemAccess Access2 { get; set; }

        public override string Text
        {
            get { return FileString; }
        }

        //Why is this called "FilePath" if it doesn't contain the actual path
        //to the file?
        public string FilePath { get; set; }

        public string FileString { get; set; }

        public override bool Success { get; set; }

        public override void NotifyUpdate()
        {
            CompareCache = "";
            base.NotifyUpdate();
        }

        public string CountString
        {
            get { return FileSystemViewer.GetCountString(this); }
        }

        public override bool IsLeaf
        {
            get { return (Nodes.Count == 0); }
        }

        private string _memoizedPath;
        public override string Path
        {
            get
            {
                if (_memoizedPath != null)
                    return _memoizedPath;
                var parent = Parent as InterpreterNode;
                return _memoizedPath = (parent != null ? parent.Path + "\\" + Text : Text);
            }
        }


        public string FileSystemPath { get; set; }

        public override string NormalizedPath
        {
            get { return Path.ToLower(); }
        }

        public IInterpreter Interpreter { get { return (IInterpreter) Tree; } }

        public override HashSet<CallEventId> CallEventIds
        {
            get { return _callEventIds; }
        }

        public bool CompareMode
        {
            get { return _compareMode; }
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

        private void UpdateColor()
        {
            if (_count1 > 0 && _count2 > 0)
            {
                Brush = new SolidBrush(_treeView.BackColor);
            }
            else if (_count1 > 0)
            {
                Brush = new SolidBrush(_treeView.Viewer.File1BackgroundColor);
            }
            else
            {
                Brush = new SolidBrush(_treeView.Viewer.File2BackgroundColor);
            }
        }

        public double Time
        {
            get { return _time; }
        }

        public string TimeString
        {
            get { return FileSystemViewer.GetTimeString(this); }
        }

        public double Time1
        {
            get { return _time1; }
        }

        public double Time2
        {
            get { return _time2; }
        }

        public override IEntry NextVisibleEntry
        {
            get
            {
                var nextVisible = _treeView.GetNextVisibleNode(this);
                return (FileSystemTreeNode) nextVisible;
            }
        }

        public override IEntry PreviousVisibleEntry
        {
            get
            {
                var prevVisible = _treeView.GetPreviousVisibleNode(this);
                return (FileSystemTreeNode)prevVisible;
            }
        }

        public EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryPropertiesDialog(this);
        }

        public override string NameForDisplay
        {
            get { return FilePath; }
        }

        public bool IsForCompare
        {
            get { return CompareMode; }
        }

        public HashSet<DeviareTraceCompareItem> CompareItems { get; private set; }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        public bool SupportsGoTo { get { return Interpreter.SupportsGoTo; } }

        public override void AddCallEventsTo(TraceTreeView aTraceTreeView)
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
            _time += callEvent.Time;
            Count++;
            // root nodes are not FileSystemTreeNode
            if (Parent != null && Parent.Parent != null)
            {
                ((FileSystemTreeNode)Parent).AddCall(callEvent);
            }
        }

        public void AddCall1(double time)
        {
            _time1 += time;
            Count1++;
            // root nodes are not FileSystemTreeNode
            if (Parent != null && Parent.Parent != null)
            {
                ((FileSystemTreeNode)Parent).AddCall1(time);
            }
        }

        public void AddCall2(double time)
        {
            _time2 += time;
            Count2++;
            // root nodes are not FileSystemTreeNode
            if (Parent != null && Parent.Parent != null)
            {
                ((FileSystemTreeNode)Parent).AddCall2(time);
            }
        }

        public string CompareCache { get; set; }

        public IEnumerable<FileSystemTreeNode> AllSubNodes
        {
            get
            {
                var subNodes = new List<FileSystemTreeNode>();
                subNodes.AddRange(Nodes.Cast<FileSystemTreeNode>());
                subNodes.AddRange(Nodes.Cast<FileSystemTreeNode>().SelectMany(n => n.AllSubNodes));
                return subNodes;
            } 
        }

        public FileEntry ToFileEntry()
        {
            return new FileEntry(FilePath, FileSystemPath, Access, Success, Company, Version, Description, Product, OriginalFileName, Icon) { IsDirectory = IsDirectoryOrBranch };
        }

        public ThinAppFileEntry ToThinAppFileEntry()
        {
            return new ThinAppFileEntry(FilePath, FileSystemPath, Access, Success, Company, Version, Description, Product,
                                        OriginalFileName, Icon, Isolation) {IsDirectory = IsDirectoryOrBranch};
        }

        public IEnumerable<string> GetAllProperties()
        {
            var properties = new List<string>();

            properties.AddIfNotNullOrEmpty(FileString);
            properties.AddIfNotNullOrEmpty(Company);
            properties.AddIfNotNullOrEmpty(Product);
            properties.AddIfNotNullOrEmpty(OriginalFileName);

            return properties;
        }

        public void MergeVersions(FileEntry aFileEntry)
        {
            if (Version.Contains(" / "))
                return;

            bool minorVersionMatches;
            var majorVersionMatches = FileSystemTools.MatchVersion(Version, aFileEntry.Version, out minorVersionMatches);

            if (!majorVersionMatches)
            {
                SetForeColorForSelfAndAncestors(EntryColors.WarningMajorVersionColor);
                VersionMismatch = VersionMismatch.Major;
                Version = BuildDiffStringFrom(Version, aFileEntry.Version);
                IsDifference = true;
                ExpandSelfAndAncestors();
            }
            else if (!minorVersionMatches)
            {
                SetForeColorForSelfAndAncestors(EntryColors.WarningMinorVersionColor);
                VersionMismatch = VersionMismatch.Minor;
                Version = BuildDiffStringFrom(Version, aFileEntry.Version);
                IsDifference = true;
                ExpandSelfAndAncestors();
            }
            else
            {
                if (Version == "0.0.0.0")
                    Version = aFileEntry.Version;

                VersionMismatch = VersionMismatch.None;
            }
        }

        private string BuildDiffStringFrom(string aString, string anotherString)
        {
            if (string.IsNullOrEmpty(anotherString) && !string.IsNullOrEmpty(aString))
                return aString + " / <empty>";

            if (string.IsNullOrEmpty(aString) && !string.IsNullOrEmpty(anotherString))
                return "<empty> / " + anotherString;

            if (aString.EqualsIgnoringCase(anotherString))
                return aString;

            return aString + " / " + anotherString;
        }

        protected VersionMismatch VersionMismatch { get; set; }
    }
}