using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aga.Controls;
using Aga.Controls.Tools;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SpyStudio.ContextMenu;
using SpyStudio.Database;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.EventSummary;

namespace SpyStudio.Main
{
    public class TraceTreeView : TreeViewAdv, IInterpreter
    {
        private class ToolTipProviderPath : IToolTipProvider
        {
            public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
            {
                var traceNode = node.Node as TraceNode;
                return traceNode != null ? StringTools.GetFirstLines(traceNode.CompleteParamMain, 6) : null;
            }
        }

        private class ToolTipProviderDetails : IToolTipProvider
        {
            public string GetToolTip(TreeNodeAdv node, NodeControl nodeControl)
            {
                var traceNode = node.Node as TraceNode;
                return traceNode != null ? StringTools.GetFirstLines(traceNode.CompleteDetails, 6) : null;
            }
        }

        public class TraceNode : Node, ITraceEntry
        {
            private class TraceNodeData
            {
                private string _paramMain, _details;
                private readonly TraceNode _node;
                private readonly NodeBar.NodeBarProperties _nodeBarProps = new NodeBar.NodeBarProperties();

                public TraceNodeData(TraceNode node)
                {
                    _node = node;
                }

                public uint Pid { get; set; }
                public uint Tid { get; set; }
                public string Caller { get; set; }
                public string Function { get; set; }

                public string ParamMain
                {
                    get { return _paramMain; }
                    set
                    {
                        CompleteParamMain = value;
                        _paramMain = StringTools.GetFirstLines(CompleteParamMain, 1, false);
                    }
                }

                public string CompleteParamMain { get; private set; }
                public string CompleteDetails { get; private set; }

                public string Details
                {
                    get { return _details; }
                    set
                    {
                        CompleteDetails = value;
                        _details = StringTools.GetFirstLines(CompleteDetails, 1, false);
                    }
                }

                public string Result { get; set; }
                public string ProcessName { get; set; }
                public double Time { get; set; }

                public NodeBar.NodeBarProperties Relevance
                {
                    get
                    {
                        _nodeBarProps.BarSize = 6 - _node.Priority;
                        //_nodeBarProps.Color = EntryColors.GetColorSummary(_node.Success, _node.Critical, _node.Priority);
                        _nodeBarProps.Color = EntryColors.GetColor(_node.Success, _node.Critical, _node.Priority);
                        return _nodeBarProps;
                    }
                }
            }

            private CallEventId _evBefore, _evAfter;

            private readonly TraceTreeView _tree;
            private TraceNodeData _data;

            public TraceNode(TraceTreeView tree, UInt64 itemNumber)
            {
                _tree = tree;
                CallNumber = itemNumber;
            }

            public string ToolTipText { get; set; }

            public bool IsForCompare
            {
                get { return false; }
            }

            public bool SupportsGoTo()
            {
                return true;
                //return !AfterEvent.IsGenerated;
            }

            public void AddCallEventsTo(TraceTreeView aTraceTreeView)
            {
                aTraceTreeView.AddEventsOfSingleTrace(CallEventIds.FetchEvents());
            }

            public void Accept(IEntryVisitor aVisitor)
            {
                aVisitor.Visit(this);
            }

            bool IEntry.SupportsGoTo
            {
                get { return Interpreter.SupportsGoTo; }
            }

            public IEntry NextVisibleEntry
            {
                get { return _tree.GetNextNode(this) as TraceNode; }
            }

            public IEntry PreviousVisibleEntry
            {
                get { return _tree.GetPreviousNode(this) as TraceNode; }
            }

            public EntryPropertiesDialogBase GetPropertiesDialog()
            {
                return new EntryPropertiesDialog(this);
            }

            public string NameForDisplay
            {
                get { return Function + " @ " + Caller; }
            }

            public HashSet<DeviareTraceCompareItem> CompareItems
            {
                get { return null; }
            }

            public IInterpreter Interpreter { get { return _tree; } }

            public HashSet<CallEventId> CallEventIds
            {
                get
                {
                    var ret = new HashSet<CallEventId>();
                    if (_evBefore != null)
                        ret.Add(_evBefore);
                    if (_evAfter != null)
                        ret.Add(_evAfter);
                    return ret;
                }
            }

            public List<CallEvent> GetCallEvents()
            {
                var ret = new List<CallEvent>();
                if (_evBefore != null)
                    ret.Add(BeforeEvent);
                if (_evAfter != null)
                    ret.Add(AfterEvent);
                return ret;
            }

            public ulong CallNumber { get; set; }

            public NodeBar.NodeBarProperties Relevance
            {
                get
                {
                    return _data.Relevance;
                    //switch (Priority)
                    //{
                    //    case 1:
                    //        return "Highest";
                    //    case 2:
                    //        return "High";
                    //    case 3:
                    //        return "Moderate";
                    //    case 4:
                    //        return "Low";
                    //    default:
                    //        return "Lowest";
                    //}
                }
            }

            public uint Pid
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.Pid;
                }
            }

            public uint Tid
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.Tid;
                }
            }

            public string Caller
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.Caller;
                }
            }

            public string Function
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.Function;
                }
            }

            public string Result
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.Result;
                }
            }

            public string ProcessName
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.ProcessName;
                }
            }

            public string ParamMain
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.ParamMain;
                }
            }

            public string CompleteParamMain
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.CompleteParamMain;
                }
            }

            public string CompleteDetails
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.CompleteDetails;
                }
            }

            public string Details
            {
                get
                {
                    Debug.Assert(_data != null);
                    return _data.Details;
                }
            }

            public string Time
            {
                get
                {
                    Debug.Assert(_data != null);
                    return string.Format("{0:N5}", _data.Time);
                }
            }

            public override bool IsLeaf
            {
                get { return (Nodes.Count == 0); }
            }

            public void UpdateEvent(CallEvent ev)
            {
                if (ev.Before)
                {
                    BeforeEvent = ev;
                }
                else
                {
                    AfterEvent = ev;
                }
            }

            public void UpdateEventId(CallEvent ev)
            {
                if (ev.Before)
                {
                    BeforeEventId = ev.EventId;
                }
                else
                {
                    AfterEventId = ev.EventId;
                }
            }

            private uint _objFlags;

            [Flags]
            private enum BooleanData
            {
                IsCom = 1 << 9,
                IsWindow = 1 << 10,
                IsFile = 1 << 11,
                IsQueryAttributes = 1 << 12,
                IsDirectory = 1 << 13,
                IsRegistry = 1 << 14,
                IsValue = 1 << 15,
                Success = 1 << 16,
                Critical = 1 << 17,
                InsertedInSummary = 1 << 18,
            }

            public bool IsCom
            {
                get { return (_objFlags & (uint) BooleanData.IsCom) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint) BooleanData.IsCom;
                    else
                        _objFlags &= ~((uint)BooleanData.IsCom);
                }
            }
            public bool IsWindow
            {
                get { return (_objFlags & (uint)BooleanData.IsWindow) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.IsWindow;
                    else
                        _objFlags &= ~((uint)BooleanData.IsWindow);
                }
            }
            public bool IsFile
            {
                get { return (_objFlags & (uint)BooleanData.IsFile) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.IsFile;
                    else
                        _objFlags &= ~((uint)BooleanData.IsFile);
                }
            }
            public bool IsQueryAttributes
            {
                get { return (_objFlags & (uint)BooleanData.IsQueryAttributes) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.IsQueryAttributes;
                    else
                        _objFlags &= ~((uint)BooleanData.IsQueryAttributes);
                }
            }
            public bool IsDirectory
            {
                get { return (_objFlags & (uint)BooleanData.IsDirectory) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.IsDirectory;
                    else
                        _objFlags &= ~((uint)BooleanData.IsDirectory);
                }
            }
            public bool IsRegistry
            {
                get { return (_objFlags & (uint)BooleanData.IsRegistry) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.IsRegistry;
                    else
                        _objFlags &= ~((uint)BooleanData.IsRegistry);
                }
            }
            public bool IsValue
            {
                get { return (_objFlags & (uint)BooleanData.IsValue) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.IsValue;
                    else
                        _objFlags &= ~((uint)BooleanData.IsValue);
                }
            }
            public bool Success
            {
                get { return (_objFlags & (uint)BooleanData.Success) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.Success;
                    else
                        _objFlags &= ~((uint)BooleanData.Success);
                }
            }
            public bool Critical
            {
                get { return (_objFlags & (uint)BooleanData.Critical) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.Critical;
                    else
                        _objFlags &= ~((uint)BooleanData.Critical);
                }
            }
            public bool InsertedInSummary
            {
                get { return (_objFlags & (uint)BooleanData.InsertedInSummary) != 0; }
                set
                {
                    if (value)
                        _objFlags |= (uint)BooleanData.InsertedInSummary;
                    else
                        _objFlags &= ~((uint)BooleanData.InsertedInSummary);
                }
            }

            // Priority is a number between 1 and 5
            public int Priority
            {
                get { return (int) (_objFlags & 0xff); }
                set { _objFlags = (_objFlags & ~((uint) 0xff)) + (uint) value; }
            }

            public bool IsFilled
            {
                get { return _data != null; }
            }

            public CallEventId EventId
            {
                get { return _evAfter; }
            }

            public Color Color
            {
                get { return EntryColors.GetColor(Success, Critical, Priority); }
            }

            public Color ColorSummary
            {
                get
                {
                    return EntryColors.GetColorSummary(Success, Critical, Priority);
                }
            }
            public bool HasEvent(CallEventId evId)
            {
                return (_evBefore != null && evId == _evBefore || _evAfter != null && evId == _evAfter);
            }

            public bool HasEvent(UInt64 callNumber)
            {
                return (_evBefore != null && callNumber == _evBefore.CallNumber ||
                        _evAfter != null && callNumber == _evAfter.CallNumber);
            }

            public CallEventId BeforeEventId
            {
                get { return _evBefore; }
                set { _evBefore = value; }
            }

            public CallEventId AfterEventId
            {
                get { return _evAfter; }
                set { _evAfter = value; }
            }

            public CallEvent BeforeEvent
            {
                get { return _evBefore != null ? EventDatabaseMgr.GetInstance().GetEvent(_evBefore, true) : null; }
                set
                {
                    var ev = value;

                    if(_data == null)
                        _data = new TraceNodeData(this);

                    Text = CallNumber.ToString(CultureInfo.InvariantCulture);

                    _data.Pid = ev.Pid;
                    _data.Tid = ev.Tid;
                    _data.Function = ev.Function;
                    _data.ParamMain = ev.ParamMain;
                    _data.Details = ev.ParamDetails;
                    _data.ProcessName = ev.ProcessName;

                    if (!string.IsNullOrEmpty(ev.CallModule))
                        _data.Caller = ModulePath.ExtractModuleName(ev.CallModule);
                    if (ev.Critical)
                    {
                        Critical = true;
                        Bold = true;
                    }
                    Priority = ev.Priority;
                    ForeColor = Color;
                }
            }
            public CallEvent AfterEvent
            {
                get { return _evAfter != null ? EventDatabaseMgr.GetInstance().GetEvent(_evAfter, true) : null; }
                set
                {
                    var ev = value;

                    if (_data == null)
                        _data = new TraceNodeData(this);

                    Success = ev.Success;
                    if (ev.Critical)
                    {
                        Critical = true;
                        Bold = true;
                    }
                    _data.Pid = ev.Pid;
                    _data.Tid = ev.Tid;
                    _data.Function = ev.Function;
                    _data.ProcessName = ev.ProcessName;

                    _data.Result = ev.Result;

                    TraceEntryTools.FillITraceInterpreterEntry(ev, this);

                    // if NullParams is null don't override before event parameters with after event parameters 
                    // (e.g.: DestroyWindow has valid parameters only before)
                    _data.ParamMain = ((!ev.NullParams || _evBefore == null) ? ev.ParamMain : ParamMain);
                    _data.Details = ((!ev.NullParams || _evBefore == null) ? ev.ParamDetails : Details);
                    _data.Time = ev.Time;
                    _data.Caller = ModulePath.ExtractModuleName(ev.CallModule);
                    Priority = ev.Priority;
                    ForeColor = Color;
                }
            }


            public void CleanEventInfo()
            {
                _data = null;
                Text = string.Empty;
            }

            public void Dump()
            {
                Debug.WriteLine("TraceNode: Before " + (_evBefore == null
                                                            ? "null"
                                                            : _evBefore.CallNumber.ToString(CultureInfo.InvariantCulture)) +
                                " After " +
                                (_evAfter == null ? "null" : _evAfter.CallNumber.ToString(CultureInfo.InvariantCulture)));
                //Debug.WriteLine(_function + " " + ProcessName + " " + _result + " " + ParamMain + " " + Details);
            }
        }

        public bool SupportsGoTo { get { return false; } }

        protected EntryContextMenu EntryProperties;

        private readonly Dictionary<UInt64, TraceNode> _itemNumberToItem = new Dictionary<UInt64, TraceNode>();
        private UInt64 _itemNumber = 1;
        private readonly object _dataLock = new object();
        private TreeModel _model;
        private int _updateCount;
        private HashSet<TraceNode> _visibleTraceNodes = new HashSet<TraceNode>();
        private readonly HashSet<CallEventId> _visibleCallEventIds = new HashSet<CallEventId>();

        private readonly Dictionary<UInt64, TraceNode> _eventToNode = new Dictionary<UInt64, TraceNode>();
        private readonly HashSet<UInt64> _callNumbers = new HashSet<ulong>();
        private UInt64 _lastFindCallNumber;
        private uint _traceId;
        private EventSummaryGraphic _eventSummary;
        private readonly List<TraceNode> _eventSummaryEventsToAdd = new List<TraceNode>();
        private readonly Dictionary<TraceNode, TraceNode> _eventSummaryEventsToInsert = new Dictionary<TraceNode,TraceNode>();
        private readonly List<TraceNode> _eventSummaryEventsToRemove = new List<TraceNode>();
        private readonly List<TraceNode> _eventsToUpdate = new List<TraceNode>();
        private readonly Dictionary<UInt64, TraceNode> _pendingEvents = new Dictionary<ulong, TraceNode>();
        private readonly Dictionary<UInt64, uint> _threadsPendingEventsCount = new Dictionary<ulong, uint>();
        private readonly Dictionary<UInt64, uint> _openThreadEventsInOtherThreadsAfterLastEvent = new Dictionary<ulong, uint>();

        private readonly NodeTextBox _columnHeaderCountNode;
        private readonly NodeBar _columnHeaderRelevanceNode;
        private readonly NodeTextBox _columnHeaderPidNode;
        private readonly NodeTextBox _columnHeaderTidNode;
        private readonly NodeTextBox _columnHeaderCallerNode;
        private readonly NodeTextBox _columnHeaderFunctionNode;
        private readonly NodeTextBox _columnHeaderParamMainNode;
        private readonly NodeTextBox _columnHeaderResultNode;
        private readonly NodeTextBox _columnHeaderProcessNameNode;
        private readonly NodeTextBox _columnHeaderDetailsNode;
        private readonly NodeTextBox _columnHeaderTimeNode;

        private readonly TreeColumn _columnHeaderCount;
        private readonly TreeColumn _columnHeaderRelevance;
        private readonly TreeColumn _columnHeaderPid;
        private readonly TreeColumn _columnHeaderTid;
        private readonly TreeColumn _columnHeaderCaller;
        private readonly TreeColumn _columnHeaderFunction;
        private readonly TreeColumn _columnHeaderParamMain;
        private readonly TreeColumn _columnHeaderResult;
        private readonly TreeColumn _columnHeaderProcessName;
        private readonly TreeColumn _columnHeaderDetails;
        private readonly TreeColumn _columnHeaderTimeCol;

#if DEBUG
        private const int MaxEventCountOfOpenEventsAfterNoEvents = 10;
#else
        private const int MaxEventCountOfOpenEventsAfterNoEvents = 50;
#endif
        public TraceTreeView()
        {
            _columnHeaderCount = new TreeColumn();
            _columnHeaderRelevance = new TreeColumn();
            _columnHeaderProcessName = new TreeColumn();
            _columnHeaderPid = new TreeColumn();
            _columnHeaderTid = new TreeColumn();
            _columnHeaderCaller = new TreeColumn();
            _columnHeaderFunction = new TreeColumn();
            _columnHeaderParamMain = new TreeColumn();
            _columnHeaderDetails = new TreeColumn();
            _columnHeaderResult = new TreeColumn();
            _columnHeaderTimeCol = new TreeColumn();
            _columnHeaderCountNode = new NodeTextBox();
            _columnHeaderRelevanceNode = new NodeBar();
            _columnHeaderProcessNameNode = new NodeTextBox();
            _columnHeaderPidNode = new NodeTextBox();
            _columnHeaderTidNode = new NodeTextBox();
            _columnHeaderCallerNode = new NodeTextBox();
            _columnHeaderFunctionNode = new NodeTextBox();
            _columnHeaderParamMainNode = new NodeTextBox {ToolTipProvider = new ToolTipProviderPath()};
            _columnHeaderDetailsNode = new NodeTextBox {ToolTipProvider = new ToolTipProviderDetails()};
            _columnHeaderResultNode = new NodeTextBox();
            _columnHeaderTimeNode = new NodeTextBox();
            ShowNodeToolTips = true;
            ShowLines = false;
            BorderStyle = BorderStyle.FixedSingle;

            SelectionChanged += OnSelectionChanged;
            VisibleNodesChanged += OnVisibleNodesChanged;
            //LoadOnDemand = true;

            Columns.Add(_columnHeaderCount);
            Columns.Add(_columnHeaderRelevance);
            Columns.Add(_columnHeaderProcessName);
            Columns.Add(_columnHeaderPid);
            Columns.Add(_columnHeaderTid);
            Columns.Add(_columnHeaderCaller);
            Columns.Add(_columnHeaderFunction);
            Columns.Add(_columnHeaderParamMain);
            Columns.Add(_columnHeaderDetails);
            Columns.Add(_columnHeaderResult);
            Columns.Add(_columnHeaderTimeCol);
            NodeControls.Add(_columnHeaderCountNode);
            NodeControls.Add(_columnHeaderRelevanceNode);
            NodeControls.Add(_columnHeaderProcessNameNode);
            NodeControls.Add(_columnHeaderPidNode);
            NodeControls.Add(_columnHeaderTidNode);
            NodeControls.Add(_columnHeaderCallerNode);
            NodeControls.Add(_columnHeaderFunctionNode);
            NodeControls.Add(_columnHeaderParamMainNode);
            NodeControls.Add(_columnHeaderDetailsNode);
            NodeControls.Add(_columnHeaderResultNode);
            NodeControls.Add(_columnHeaderTimeNode);

            // 
            // columnHeaderCount
            // 
            _columnHeaderCount.Header = "#";
            _columnHeaderCount.SortOrder = SortOrder.None;
            _columnHeaderCount.TooltipText = null;
            _columnHeaderCount.Width = 70;

            // 
            // columnHeaderRelevance
            // 
            _columnHeaderRelevance.Header = "Relevance";
            _columnHeaderRelevance.SortOrder = SortOrder.None;
            _columnHeaderRelevance.TooltipText = null;
            _columnHeaderRelevance.Width = 60;
            _columnHeaderRelevance.TextAlign = HorizontalAlignment.Center;


            // 
            // columnHeaderProcessName
            // 
            _columnHeaderProcessName.Header = "Process Name";
            _columnHeaderProcessName.SortOrder = SortOrder.None;
            _columnHeaderProcessName.TooltipText = null;
            _columnHeaderProcessName.Width = 87;
            // 
            // columnHeaderPid
            // 
            _columnHeaderPid.Header = "Pid";
            _columnHeaderPid.SortOrder = SortOrder.None;
            _columnHeaderPid.TooltipText = null;
            _columnHeaderPid.Width = 43;
            // 
            // columnHeaderTid
            // 
            _columnHeaderTid.Header = "Tid";
            _columnHeaderTid.SortOrder = SortOrder.None;
            _columnHeaderTid.TooltipText = null;
            _columnHeaderTid.Width = 46;
            // 
            // columnHeaderCaller
            // 
            _columnHeaderCaller.Header = "Caller";
            _columnHeaderCaller.SortOrder = SortOrder.None;
            _columnHeaderCaller.TooltipText = null;
            // 
            // columnHeaderFunction
            // 
            _columnHeaderFunction.Header = "Function";
            _columnHeaderFunction.SortOrder = SortOrder.None;
            _columnHeaderFunction.TooltipText = null;
            _columnHeaderFunction.Width = 70;
            // 
            // columnHeaderPath
            // 
            _columnHeaderParamMain.Header = "ParamMain";
            _columnHeaderParamMain.SortOrder = SortOrder.None;
            _columnHeaderParamMain.TooltipText = null;
            _columnHeaderParamMain.Width = 210;
            // 
            // columnHeaderDetails
            // 
            _columnHeaderDetails.Header = "ParamDetails";
            _columnHeaderDetails.SortOrder = SortOrder.None;
            _columnHeaderDetails.TooltipText = null;
            _columnHeaderDetails.Width = 250;
            // 
            // columnHeaderResult
            // 
            _columnHeaderResult.Header = "Result";
            _columnHeaderResult.SortOrder = SortOrder.None;
            _columnHeaderResult.TooltipText = null;
            _columnHeaderResult.Width = 80;
            // 
            // columnHeaderTimeCol
            // 
            _columnHeaderTimeCol.Header = "Time";
            _columnHeaderTimeCol.SortOrder = SortOrder.None;
            _columnHeaderTimeCol.TooltipText = null;
            _columnHeaderTimeCol.TextAlign = HorizontalAlignment.Right;
            _columnHeaderTimeCol.Width = 60;
            // 
            // columnHeaderCountNode
            // 
            _columnHeaderCountNode.DataPropertyName = "CallNumber";
            _columnHeaderCountNode.LeftMargin = 3;
            _columnHeaderCountNode.ParentColumn = _columnHeaderCount;
            _columnHeaderCountNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderRelevanceNode
            // 
            _columnHeaderRelevanceNode.DataPropertyName = "Relevance";
            _columnHeaderRelevanceNode.LeftMargin = 3;
            _columnHeaderRelevanceNode.ParentColumn = _columnHeaderRelevance;
            _columnHeaderRelevanceNode.MaxBarSize = 5;

            // 
            // columnHeaderProcessNameNode
            // 
            _columnHeaderProcessNameNode.DataPropertyName = "ProcessName";
            _columnHeaderProcessNameNode.LeftMargin = 3;
            _columnHeaderProcessNameNode.ParentColumn = _columnHeaderProcessName;
            _columnHeaderProcessNameNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderPidNode
            // 
            _columnHeaderPidNode.DataPropertyName = "Pid";
            _columnHeaderPidNode.LeftMargin = 3;
            _columnHeaderPidNode.ParentColumn = _columnHeaderPid;
            _columnHeaderPidNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderTidNode
            // 
            _columnHeaderTidNode.DataPropertyName = "Tid";
            _columnHeaderTidNode.LeftMargin = 3;
            _columnHeaderTidNode.ParentColumn = _columnHeaderTid;
            _columnHeaderTidNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderCallerNode
            // 
            _columnHeaderCallerNode.DataPropertyName = "Caller";
            _columnHeaderCallerNode.LeftMargin = 3;
            _columnHeaderCallerNode.ParentColumn = _columnHeaderCaller;
            _columnHeaderCallerNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderFunctionNode
            // 
            _columnHeaderFunctionNode.DataPropertyName = "Function";
            _columnHeaderFunctionNode.IncrementalSearchEnabled = true;
            _columnHeaderFunctionNode.LeftMargin = 3;
            _columnHeaderFunctionNode.ParentColumn = _columnHeaderFunction;
            _columnHeaderFunctionNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderPathNode
            // 
            _columnHeaderParamMainNode.DataPropertyName = "ParamMain";
            _columnHeaderParamMainNode.LeftMargin = 3;
            _columnHeaderParamMainNode.ParentColumn = _columnHeaderParamMain;
            _columnHeaderParamMainNode.Trimming = StringTrimming.Character;
            _columnHeaderParamMainNode.CompactString = true;
            // 
            // columnHeaderDetailsNode
            // 
            _columnHeaderDetailsNode.DataPropertyName = "Details";
            _columnHeaderDetailsNode.LeftMargin = 3;
            _columnHeaderDetailsNode.ParentColumn = _columnHeaderDetails;
            _columnHeaderDetailsNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderResultNode
            // 
            _columnHeaderResultNode.DataPropertyName = "Result";
            _columnHeaderResultNode.LeftMargin = 3;
            _columnHeaderResultNode.ParentColumn = _columnHeaderResult;
            _columnHeaderResultNode.Trimming = StringTrimming.Character;
            // 
            // columnHeaderTimeNode
            // 
            _columnHeaderTimeNode.DataPropertyName = "Time";
            _columnHeaderTimeNode.LeftMargin = 3;
            _columnHeaderTimeNode.ParentColumn = _columnHeaderTimeCol;
            _columnHeaderTimeNode.TextAlign = HorizontalAlignment.Right;
        }

        private void EventSummaryOnRequestEventsSummary(EventSummaryTooltipArgs eventEntriesArgs)
        {
            var text = new StringBuilder();
            var evIdsToGetFromDb = new List<CallEventId>();
            var nodes = new List<TraceNode>();
            bool moreEntries = false;

            eventEntriesArgs.EntriesText = string.Empty;

            // get information about the first 10 nodes
            int i = 0;
            foreach (var entry in eventEntriesArgs.Entries)
            {
                if (++i == 10)
                {
                    moreEntries = true;
                    break;
                }
                var traceNode = entry as TraceNode;
                if (traceNode != null)
                {
                    nodes.Add(traceNode);
                    if (!traceNode.IsFilled)
                    {
                        if (traceNode.BeforeEventId != null)
                            evIdsToGetFromDb.Add(traceNode.BeforeEventId);
                        if (traceNode.AfterEventId != null)
                            evIdsToGetFromDb.Add(traceNode.AfterEventId);
                    }
                }
            }
            var evIdsMap = new Dictionary<CallEventId, CallEvent>();
            if (evIdsToGetFromDb.Count > 0)
            {
                var callEvents = evIdsToGetFromDb.FetchEvents(false);
                foreach (var callEvent in callEvents)
                {
                    evIdsMap[callEvent.EventId] = callEvent;
                }
            }
            foreach(var traceNode in nodes)
            {
                if(text.Length != 0)
                {
                    text.Append("\n");
                }
                var filled = false;
                if(!traceNode.IsFilled)
                {
                    filled = true;
                    CallEvent callEvent;
                    if(traceNode.BeforeEventId != null)
                    {
                        if(evIdsMap.TryGetValue(traceNode.BeforeEventId, out callEvent))
                        {
                            traceNode.BeforeEvent = callEvent;
                        }
                        else
                        {
                            Debug.Assert(false, "Cannot get event from database");
                        }
                    }
                    if (traceNode.AfterEventId != null)
                    {
                        if (evIdsMap.TryGetValue(traceNode.AfterEventId, out callEvent))
                        {
                            traceNode.AfterEvent = callEvent;
                        }
                        else
                        {
                            Debug.Assert(false, "Cannot get event from database");
                        }
                    }
                }
                text.Append(traceNode.Function);
                text.Append(" ");
                if (!string.IsNullOrEmpty(traceNode.ParamMain))
                {

                    text.Append(StringTools.GetFirstLines(traceNode.CompleteParamMain, 1, 50));
                    text.Append(" ");
                }
                if (!string.IsNullOrEmpty(traceNode.Details))
                {
                    text.Append(StringTools.GetFirstLines(traceNode.CompleteDetails, 1, 50));
                }

                if(filled)
                {
                    traceNode.CleanEventInfo();
                }
            }
            if (text.Length != 0)
            {
                if (moreEntries)
                    text.Append("\n...");
                eventEntriesArgs.EntriesText = text.ToString();
            }
        }
        private void EventSummaryOnFocusChange(EventSummaryEntryArgs eventEntryArgs)
        {
            var traceNode = eventEntryArgs.Entry as TraceNode;
            Debug.Assert(traceNode != null);
            EnsureVisible(traceNode, ScrollType.Middle);
        }

        private void OnSelectionChanged(object sender, EventArgs eventArgs)
        {
            _lastFindCallNumber = 0;
        }

        public void InitializeComponent()
        {
            _model = new TreeModel();
            Model = _model;

            if(EntryProperties == null)
            {
                EntryProperties = new TraceEntryContextMenu(this, Controller);
            }
        }

        private void OnVisibleNodesChanged(object sender, EventArgs eventArgs)
        {
            //var sw = new Stopwatch();
            //sw.Start();
            var newVisibleNodesOrdered = new List<Node>();
            var visibleNodes = VisibleNodes;
            var evIdsToGetFromDb = new List<CallEventId>();
            var alreadyVisibleEvIds = new HashSet<CallEventId>();

#if DEBUG
            ulong firstCallNumber = 0, lastCallNumber = 0;
#endif
            // evIdsToGetFromDb: will have all the CallEventIds that are not in _visibleCallEventIds and should
            // be retrieved
            // alreadyVisibleEvIds: will contain those CallEventIds which are already visible and should be retrieved
            // from Db
            foreach (var treeNode in visibleNodes)
            {
                var traceNode = treeNode.Node as TraceNode;
                if (traceNode != null)
                {
                    newVisibleNodesOrdered.Add(traceNode);

                    // if both events are in the _visibleCallEventIds -> add not null to the alreadyVisibleEvIds
                    // otherwise, get both again (not null) because we need both CallEvents in order to complete the TraceNode
                    if ((traceNode.BeforeEventId == null || _visibleCallEventIds.Contains(traceNode.BeforeEventId)) &&
                        (traceNode.AfterEventId == null || _visibleCallEventIds.Contains(traceNode.AfterEventId)))
                    {
                        if (traceNode.BeforeEventId != null)
                            alreadyVisibleEvIds.Add(traceNode.BeforeEventId);
                        if (traceNode.AfterEventId != null)
                            alreadyVisibleEvIds.Add(traceNode.AfterEventId);
                    }
                    else
                    {
                        if (traceNode.BeforeEventId != null)
                            evIdsToGetFromDb.Add(traceNode.BeforeEventId);
                        if (traceNode.AfterEventId != null)
                            evIdsToGetFromDb.Add(traceNode.AfterEventId);
                    }
                }
#if DEBUG
                var currTraceNode = (TraceNode) treeNode.Node;
                if(firstCallNumber == 0 || firstCallNumber > currTraceNode.CallNumber)
                {
                    firstCallNumber = currTraceNode.CallNumber;
                }
                if(lastCallNumber == 0 || lastCallNumber < currTraceNode.CallNumber)
                {
                    lastCallNumber = currTraceNode.CallNumber;
                }
#endif
            }
#if DEBUG
            //Debug.WriteLine("TraceTreeView OnVisibleNodesChanged:\nFirst: " + firstCallNumber + " Last: " +
            //                lastCallNumber);
#endif
            // clean those TraceNodes that aren't visible anymore
            var newVisibleTraceNodes = new HashSet<TraceNode>();
            foreach (var traceNode in _visibleTraceNodes)
            {
                if (traceNode.BeforeEventId != null && !alreadyVisibleEvIds.Contains(traceNode.BeforeEventId) ||
                    traceNode.AfterEventId != null && !alreadyVisibleEvIds.Contains(traceNode.AfterEventId))
                {
                    traceNode.CleanEventInfo();
                    if (traceNode.BeforeEventId != null)
                        _visibleCallEventIds.Remove(traceNode.BeforeEventId);
                    if (traceNode.AfterEventId != null)
                        _visibleCallEventIds.Remove(traceNode.AfterEventId);
                }
                else
                {
                    newVisibleTraceNodes.Add(traceNode);
                }
            }
            if (evIdsToGetFromDb.Count > 0)
            {
                var evIdsMap = new Dictionary<CallEventId, CallEvent>();
                var callEvents = evIdsToGetFromDb.FetchEvents(false);
                foreach (var callEvent in callEvents)
                {
                    evIdsMap[callEvent.EventId] = callEvent;
                }

                foreach (var treeNode in visibleNodes)
                {
                    var traceNode = treeNode.Node as TraceNode;
                    if (traceNode != null && !newVisibleTraceNodes.Contains(traceNode))
                    {
                        CallEvent callEvent;
                        if (traceNode.BeforeEventId != null &&
                            evIdsMap.TryGetValue(traceNode.BeforeEventId, out callEvent))
                        {
                            traceNode.BeforeEvent = callEvent;
                            _visibleCallEventIds.Add(traceNode.BeforeEventId);
                        }
                        if (traceNode.AfterEventId != null &&
                            evIdsMap.TryGetValue(traceNode.AfterEventId, out callEvent))
                        {
                            traceNode.AfterEvent = callEvent;
                            _visibleCallEventIds.Add(traceNode.AfterEventId);
                        }
                        newVisibleTraceNodes.Add(traceNode);
                    }
                }
            }
            _visibleTraceNodes = newVisibleTraceNodes;
            //Debug.WriteLine("Database: " + sw.Elapsed.TotalMilliseconds);

            if(_eventSummary != null)
            {
                TreeViewAdvTools.SortByTreeRow(newVisibleNodesOrdered);
                var entries = new List<ITraceEntry>();
                Node prev = null;
                foreach(var n in newVisibleNodesOrdered)
                {
                    if(prev != null)
                    {
                        Debug.Assert(TreeViewAdvTools.NodeComparer.Compare(prev, n) < 0);
                    }
                    entries.Add((ITraceEntry)n);
                    prev = n;
                }
                //var entries = newVisibleNodesOrdered.Cast<ITraceEntry>().ToList();
                _eventSummary.SetFocusRect(entries);
            }
        }

        public void Attach(DeviareRunTrace devRunTrace)
        {
            devRunTrace.EventAdd += AddEvent;
            devRunTrace.EventUpdated += UpdateEvent;
            devRunTrace.UpdateBegin += (sender, args) => this.ExecuteInUIThreadAsynchronously(BeginUpdate);
            devRunTrace.UpdateEnd += (sender, args) => this.ExecuteInUIThreadAsynchronously(EndUpdate);
            devRunTrace.TraceClear += ClearData;
            _traceId = devRunTrace.TraceId;
        }

        public void SetEventSummary(EventSummaryGraphic eventSummary)
        {
            if (_eventSummary != null)
            {
                _eventSummary.RequestEventsSummary -= EventSummaryOnRequestEventsSummary;
                _eventSummary.FocusChange -= EventSummaryOnFocusChange;
            }
            _eventSummary = eventSummary;
            if(eventSummary != null)
            {
                _eventSummary.RequestEventsSummary += EventSummaryOnRequestEventsSummary;
                _eventSummary.FocusChange += EventSummaryOnFocusChange;
                ShowVScrollBar = false;
            }
            ShowVScrollBar = (_eventSummary == null);
        }

        [Conditional("DEBUG")]
        void DumpTimes()
        {
#if DEBUG
            Debug.WriteLine("Trace Times:");
            Debug.WriteLine("Count:\t" + _count + "\n" +
                            "Time1:\t " + _time1 + "\n" +
                            "Time2:\t" + _time2 + "\n" +
                            "Time3:\t" + _time3 + "\n" +
                            "Time4:\t" + _time4 + "\n" +
                            "Time5:\t" + _time5 + "\n" +
                            "Time6:\t" + _time6 + "\n" +
                            "Time7:\t" + _time7 + "\n" +
                            "Time8:\t" + _time8 + "\n" +
                            "Time9:\t" + _time9 + "\n" +
                            "Time10:\t" + _time10
                );
#endif
        }

        [Conditional("DEBUG")]
        void InitTimes()
        {
#if DEBUG
            _time1 = _time2 = _time3 = _time4 = _time5 = _time6 = _time7 = _time8 = _time9 = _time10 = 0;
            _count = 0;
#endif
        }
        public new void BeginUpdate()
        {
            if (_updateCount++ == 0)
            {
                InitTimes();
                //_model.InitTimes();
                base.BeginUpdate();
                if (_eventSummary != null)
                    _eventSummary.BeginUpdate();
            }
        }

        public new void EndUpdate()
        {
            if (--_updateCount == 0)
            {
                base.EndUpdate();
#if DEBUG
                var sw = new Stopwatch();
                sw.Start();
#endif
                UpdateSummary(true);
#if DEBUG
                _time7 += sw.Elapsed.TotalMilliseconds;
                //if (_eventSummary != null)
                //    _eventSummary.Verify(this);
                var previous = sw.Elapsed.TotalMilliseconds;
#endif

                if (_eventSummary != null)
                    _eventSummary.EndUpdate();
#if DEBUG
                _time10 += sw.Elapsed.TotalMilliseconds - previous;

                //DumpTimes();
                //_model.DumpTimes();
#endif
            }
        }

        public void AddEvent(object sender, CallEventArgs e)
        {
            AddEvent(e.Event, e.Filtered, false, CompareItemType.Item1AndItem2);
        }

        private void UpdateEvent(object sender, CallEventArgs e)
        {
            TraceNode node;
            if(_eventToNode.TryGetValue(e.Event.CallNumber, out node))
            {
                node.Critical = e.Event.Critical;
                node.Priority = e.Event.Priority;
                _eventSummary.Update(node);
                node.NotifyUpdate();
            }
        }

        public void ClearData(object sender, EventArgs e)
        {
            ClearData();
        }

        public void AddEvent(CallEvent callEvent, bool filtered, bool ignoreGenerated,
                             CompareItemType cit)
        {
            // don't add generated events, only real calls of the application/s
            if (ignoreGenerated && callEvent.IsGenerated)
                return;

            ProcessEvent(callEvent, filtered, cit);
        }

        private void FilterItem(TraceNode node)
        {
            if (node.Nodes.Count > 0)
            {
                var newParent = node.Parent;

                Debug.Assert(newParent != null);

                // find the first node that should be after the first child of node
                int i = newParent.Nodes.Count - 1;
                var childNode = (TraceNode)node.Nodes[0];
                while (((TraceNode)newParent.Nodes[i]).CallNumber > childNode.CallNumber && i > 0)
                {
                    i--;
                }

                while (node.Nodes.Count != 0)
                {
                    childNode = (TraceNode)node.Nodes[0];
                    node.Nodes.RemoveAt(0);
                    RemoveEventFromSummary(childNode);
                    //i++;
                    while (((TraceNode)newParent.Nodes[i]).CallNumber < childNode.CallNumber && i < newParent.Nodes.Count - 1)
                    {
                        i++;
                    }
                    //if (((TraceNode)newParent.Nodes[i]).CallNumber > childNode.CallNumber)
                    //    i--;
                    newParent.Nodes.Insert(i, childNode);
                    // previousNode == Root it will be null and EventSummaryGraphics will insert it at the top
                    TraceNode previousNode;
                    // if previous node is the old parent get the previous of the old parent
                    if(childNode.PreviousNode == node)
                    {
                        previousNode = GetPreviousNode(node) as TraceNode;
                    }
                    else
                    {
                        previousNode = GetPreviousNode(childNode) as TraceNode;
                    }
                    InsertNodeToSummary(previousNode, childNode);
                    i++;
                }
            }

            node.Parent.Nodes.Remove(node);
            _itemNumberToItem.Remove(node.CallNumber);

            RemoveEventFromSummary(node);

            var itemToUpdate = node.CallNumber + 1;
            while (itemToUpdate < _itemNumber)
            {
                node = _itemNumberToItem[itemToUpdate];
                _itemNumberToItem.Remove(itemToUpdate++);
                node.CallNumber--;
                _itemNumberToItem[node.CallNumber] = node;
                node.NotifyUpdate();
            }
            _itemNumber--;
        }

#if DEBUG
        private int _count;
        private double _time1, _time2, _time3, _time4, _time5, _time6, _time7, _time8, _time9, _time10;
#endif


        public void InsertNode(CallEventId callEventId)
        {
            this.ExecuteInUIThreadAsynchronously(() =>
                {
                    var node = new TraceNode(this, _itemNumber++) {AfterEventId = callEventId};

                    _model.Nodes.Add(node);
                    _itemNumberToItem[node.CallNumber] = node;

                    _eventToNode[callEventId.CallNumber] = node;
                    _callNumbers.Add(callEventId.CallNumber);
                });
        }

        public void AddFile1ItemToListView(CallEventId callEventId)
        {
            this.ExecuteInUIThreadAsynchronously( () =>
                {
                    var node = new TraceNode(this, _itemNumber++)
                                   {AfterEventId = callEventId, Brush = new SolidBrush(EntryColors.File1Color)};

                    _model.Nodes.Add(node);
                    _itemNumberToItem[node.CallNumber] = node;

                    _eventToNode[callEventId.CallNumber] = node;
                    _callNumbers.Add(callEventId.CallNumber); 
                });
        }

        public void AddFile2ItemToListView(CallEventId callEventId)
        {
            this.ExecuteInUIThreadAsynchronously(() =>
                {
                    var node = new TraceNode(this, _itemNumber++)
                                   {AfterEventId = callEventId, Brush = new SolidBrush(EntryColors.File2Color)};

                    _model.Nodes.Add(node);
                    _itemNumberToItem[node.CallNumber] = node;

                    _eventToNode[callEventId.CallNumber] = node;
                    _callNumbers.Add(callEventId.CallNumber);
                });
        }

        private void AddNodeRecursiveToListAndMark(TraceNode node, List<ITraceEntry> list)
        {
            list.Add(node);
            node.InsertedInSummary = true;
            foreach (TraceNode child in node.Nodes)
            {
                AddNodeRecursiveToListAndMark(child, list);
            }
        }
        private void InsertNode(CallEvent e, TraceNode node, TraceNode parentItem,
                                            CompareItemType cit)
        {
#if DEBUG
            _count++;
            var sw = new Stopwatch();
            sw.Start();
            double prevCheck;
#endif
            if (node == null)
            {
                node = new TraceNode(this, _itemNumber++)
                           {Critical = e.Critical, Priority = e.Priority, Success = e.Success};
                node.UpdateEventId(e);
                if (cit != CompareItemType.Item1AndItem2)
                {
                    node.Brush = new SolidBrush((cit == CompareItemType.Item1)
                                                    ? EntryColors.File1Color
                                                    : EntryColors.File2Color);
                }
#if DEBUG
                _time1 += sw.Elapsed.TotalMilliseconds;
                prevCheck = sw.Elapsed.TotalMilliseconds;
#endif

                if (parentItem == null)
                {
                    _model.Nodes.Add(node);
                }
                else
                {
                    parentItem.Nodes.Add(node);
                }
                _itemNumberToItem[node.CallNumber] = node;
#if DEBUG
                _time3 += sw.Elapsed.TotalMilliseconds - prevCheck;
                prevCheck = sw.Elapsed.TotalMilliseconds;
#endif
            }
            else
            {
#if DEBUG
                prevCheck = sw.Elapsed.TotalMilliseconds;
#endif
                node.UpdateEventId(e);
                node.Success = e.Success;
                node.Priority = e.Priority;
                node.Critical = e.Critical;
                if (cit != CompareItemType.Item1AndItem2)
                {
                    node.Brush = new SolidBrush((cit == CompareItemType.Item1)
                                                    ? EntryColors.File1Color
                                                    : EntryColors.File2Color);
                }
                node.NotifyUpdate();
#if DEBUG
                _time2 += sw.Elapsed.TotalMilliseconds - prevCheck;
#endif
            }

            _eventToNode[e.CallNumber] = node;
            _callNumbers.Add(e.CallNumber);

            if (e.OnlyBefore || !e.Before)
            {
                if (e.Priority <= 2 && node.Depth > 1)
                    EnsureExpanded(node.Parent);
            }

#if DEBUG
            _time4 += sw.Elapsed.TotalMilliseconds - prevCheck;
            prevCheck = sw.Elapsed.TotalMilliseconds;
#endif

            AddEventToSummary(e, node);
#if DEBUG
            _time5 += sw.Elapsed.TotalMilliseconds - prevCheck;
            prevCheck = sw.Elapsed.TotalMilliseconds;
#endif
            UpdateSummary(false);
#if DEBUG
            _time6 += sw.Elapsed.TotalMilliseconds - prevCheck;
#endif

        }

        private void InsertNodeToSummary(TraceNode nodeBefore, TraceNode node)
        {
            if (_eventSummary != null)
            {
                _eventSummaryEventsToInsert[node] = nodeBefore;
            }
        }

        private void AddEventToSummary(CallEvent e, TraceNode node)
        {
            if (_eventSummary != null)
            {
                bool alreadyAdded = false;
                if (e.OnlyBefore || !e.Before)
                {
                    TraceNode peerNode;
                    if (_pendingEvents.TryGetValue(e.Peer, out peerNode))
                    {
                        _pendingEvents.Remove(e.Peer);
                        if (_threadsPendingEventsCount.ContainsKey(e.Tid))
                        {
                            if (--_threadsPendingEventsCount[e.Tid] == 0)
                                _threadsPendingEventsCount.Remove(e.Tid);
                        }
                        // update event: it was already inserted
                        if(node.InsertedInSummary)
                        {
                            _eventsToUpdate.Add(node);
                        }
                        alreadyAdded = true;
                    }

                    if (_threadsPendingEventsCount.ContainsKey(e.Tid) && _threadsPendingEventsCount[e.Tid] > 0)
                    {
                        // if there is an open event and the new event is ChainDepth == 1, the open event won't be closed
                        // e.g.: an exception was thrown and the after CallEvent will never come
                        if (e.ChainDepth == 1)
                        {
                            _threadsPendingEventsCount.Remove(e.Tid);
                        }
                    }

                    List<ulong> toRemove = null;
                    // increment the event count without events of the threads which have open events. If there more than
                    // EventCountAfterException after the last event in a thread that has an open event, report
                    // the pending events because it could happen that the opened events won't never close
                    foreach (var pending in _threadsPendingEventsCount)
                    {
                        if (pending.Key != e.Tid)
                        {
                            if (!_openThreadEventsInOtherThreadsAfterLastEvent.ContainsKey(pending.Key))
                                _openThreadEventsInOtherThreadsAfterLastEvent[pending.Key] = 1;
                            else
                            {
                                if (++_openThreadEventsInOtherThreadsAfterLastEvent[pending.Key] >
                                    MaxEventCountOfOpenEventsAfterNoEvents)
                                {
                                    if (toRemove == null)
                                    {
                                        toRemove = new List<ulong> {pending.Key};
                                    }
                                }
                            }
                        }
                    }

                    if (toRemove != null)
                    {
                        foreach (var r in toRemove)
                        {
                            _openThreadEventsInOtherThreadsAfterLastEvent.Remove(r);
                            _threadsPendingEventsCount.Remove(r);
                        }
                    }
                    _openThreadEventsInOtherThreadsAfterLastEvent.Remove(e.Tid);
                }
                else
                {
                    _pendingEvents[e.CallNumber] = node;
                    if (_threadsPendingEventsCount.ContainsKey(e.Tid))
                        _threadsPendingEventsCount[e.Tid]++;
                    else
                        _threadsPendingEventsCount[e.Tid] = 1;
                }

                // the parent was already inserted so this node should be inserted instead of appended
                if (!alreadyAdded && !node.ParentIsRoot)
                {
                    var parent = node.Parent as TraceNode;
                    if (parent != null && parent.InsertedInSummary)
                    {
                        var previous = GetPreviousNode(node) as TraceNode;
                        _eventSummaryEventsToInsert[node] = previous;
                        alreadyAdded = true;
                    }
                }

                if (node.Depth == 1 && !alreadyAdded)
                {
                    _eventSummaryEventsToAdd.Add(node);
                }
            }
        }

        private void RemoveEventFromSummary(TraceNode node)
        {
            if (_eventSummary != null)
            {
                // try to find the node in the pending ones. Otherwise, put it to remove
                if (_eventSummaryEventsToAdd.Contains(node))
                {
                    _eventSummaryEventsToAdd.Remove(node);
                }
                else
                {
                    if(_eventSummaryEventsToInsert.ContainsKey(node))
                    {
                        _eventSummaryEventsToInsert.Remove(node);
                    }
                    else
                    {
                        _eventSummaryEventsToRemove.Add(node);                        
                    }
                }
            }
        }

        private void UpdateSummary(bool force)
        {
            if (_eventSummary != null)
            {
#if DEBUG
                var sw = new Stopwatch();
                sw.Start();
                double prevCheck;
#endif

                if (_threadsPendingEventsCount.Count == 0 || force)
                {
                    var reportNodes = new List<ITraceEntry>();
                    foreach (var node in _eventSummaryEventsToRemove)
                    {
                        AddNodeRecursiveToListAndMark(node, reportNodes);
                    }
                    if(reportNodes.Count > 0)
                        _eventSummary.RemoveRange(reportNodes);
                    _eventSummaryEventsToRemove.Clear();
                    reportNodes.Clear();

                    foreach (var pendingNode in _eventSummaryEventsToAdd)
                    {
                        AddNodeRecursiveToListAndMark(pendingNode, reportNodes);
                    }

                    if (reportNodes.Count > 0)
                        _eventSummary.AddRange(reportNodes);
                    _eventSummaryEventsToAdd.Clear();

#if DEBUG
                    _time7 += sw.Elapsed.TotalMilliseconds;
                    prevCheck = sw.Elapsed.TotalMilliseconds;
#endif

                    if (_eventSummaryEventsToInsert.Count > 0)
                    {
                        foreach (var pair in _eventSummaryEventsToInsert)
                        {
                            reportNodes.Clear();
                            AddNodeRecursiveToListAndMark(pair.Key, reportNodes);
                            _eventSummary.InsertRange(pair.Value, reportNodes);
                        }
                        _eventSummaryEventsToInsert.Clear();
                    }

#if DEBUG
                    _time8 += sw.Elapsed.TotalMilliseconds - prevCheck;
#endif
                }
#if DEBUG
                prevCheck = sw.Elapsed.TotalMilliseconds;
#endif

                if (_eventsToUpdate.Count > 0)
                {
                    foreach (var entry in _eventsToUpdate)
                        _eventSummary.Update(entry);
                    _eventsToUpdate.Clear();
                }
#if DEBUG
                _time9 += sw.Elapsed.TotalMilliseconds - prevCheck;
#endif
            }
        }
        public delegate void ClearDataDelegate();

        public void ClearData()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ClearDataDelegate(ClearData));
            }
            else
            {
                if (EntryProperties != null)
                    EntryProperties.Close(false);

                lock (_dataLock)
                {
                    BeginUpdate();
                    _itemNumber = 1;
                    _model.Nodes.Clear();
                    _visibleCallEventIds.Clear();
                    _visibleTraceNodes.Clear();
                    _itemNumberToItem.Clear();
                    _eventToNode.Clear();
                    _callNumbers.Clear();
                    _eventSummaryEventsToAdd.Clear();
                    _threadsPendingEventsCount.Clear();
                    _openThreadEventsInOtherThreadsAfterLastEvent.Clear();
                    _pendingEvents.Clear();
                    if (_eventSummary != null)
                        _eventSummary.Clear();
                    _eventsToUpdate.Clear();
                    _eventSummaryEventsToInsert.Clear();
                    EndUpdate();
                }
            }
        }

        private TraceNode GetParentNode(CallEvent ev)
        {
            TraceNode parentItem = null;
            foreach (var anc in ev.Ancestors)
            {
                if (_eventToNode.TryGetValue(anc, out parentItem))
                {
                    break;
                }
            }
            return parentItem;
        }

        public void ProcessEvent(CallEvent e, bool filtered, CompareItemType cit)
        {
            Debug.Assert(!InvokeRequired, "Entered TraceTreeView ProcessEvent function from a non-UI thread.");
            
            TraceNode item = null;
            var parentItem = GetParentNode(e);

            if (!e.Before)
                _eventToNode.TryGetValue(e.Peer, out item);

            if (!filtered)
                InsertNode(e, item, parentItem, cit);
            else if (item != null)
                // if filter applies and the item was inserted remove it
                FilterItem(item);
            
        }

        public void FindEvent(FindEventArgs e)
        {
            Find(e);
        }

        public override bool Find(FindEventArgs e)
        {
            var text = e.Text;
            if (!e.MatchCase)
                text = text.ToLower();

            UInt64 curCallNumber = 0;
            TraceNode curTraceNode;
            if (_lastFindCallNumber != 0)
            {
                curCallNumber = _lastFindCallNumber;
            }
            else if (SelectedNode != null)
            {
                curTraceNode = SelectedNode as TraceNode;
                if (curTraceNode != null)
                    curCallNumber = curTraceNode.BeforeEventId != null
                                        ? curTraceNode.BeforeEventId.CallNumber
                                        : curTraceNode.AfterEventId.CallNumber;
            }

            var ret = false;
            var callEvents = EventDatabaseMgr.GetInstance().SearchEvents(_traceId, text, e.MatchWhole, e.MatchCase,
                                                                         e.SearchDown, 1,
                                                                         curCallNumber,
                                                                         _callNumbers);
            if (callEvents != null && callEvents.Length > 0)
            {
                if (_eventToNode.TryGetValue(callEvents[0].EventId.CallNumber, out curTraceNode))
                {
                    BeginUpdate();
                    EnsureVisible(curTraceNode, ScrollType.Middle);
                    SelectNode(curTraceNode);
                    EndUpdate();
                    ret = true;
                }
            }
            return ret;
        }

        #region IInterpreter

        public IInterpreterController Controller { get; set; }

        public EntryContextMenu ContextMenuController { get { return EntryProperties; } }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return SelectedNodes.Cast<IEntry>(); }
        }

        public Control ParentControl
        {
            get { return Parent; }
        }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            TreeNodeAdvTools.SelectNextNode((Node) anEntry, this);
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            TreeNodeAdvTools.SelectPreviousNode((Node) anEntry, this);
        }

        public EntryPropertiesDialogBase GetPropertiesDialogFor(IEntry anEntry)
        {
            return new EntryPropertiesDialog(anEntry);
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }

        #endregion IInterpreter

        public void CopySelectionToClipboard()
        {
            var tempStr = new StringBuilder("");
            var nodesToCopy = new List<TraceNode>();
            var nodesToGet = new Dictionary<CallEventId, TraceNode>();
            var nodesToClear = new List<TraceNode>();
            var callsToGet = new List<CallEventId>();

            foreach (var n in SelectedNodes)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = (TraceNode) n;
                if (!node.IsFilled)
                {
                    if (node.BeforeEventId != null)
                    {
                        callsToGet.Add(node.BeforeEventId);
                        nodesToGet[node.BeforeEventId] = node;
                    }
                    if (node.AfterEventId != null)
                    {
                        callsToGet.Add(node.AfterEventId);
                        nodesToGet[node.AfterEventId] = node;
                    }
                    nodesToClear.Add(node);
                }
                nodesToCopy.Add(node);
            }
            if (callsToGet.Count > 0)
            {
                var callEvents = EventDatabaseMgr.GetInstance().GetEvents(callsToGet, false);
                foreach (var callEvent in callEvents)
                {
                    nodesToGet[callEvent.EventId].UpdateEvent(callEvent);
                }
            }

            foreach (var n in nodesToCopy)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = n;
                CopyNode(node, tempStr);
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
            foreach (var node in nodesToClear)
            {
                node.CleanEventInfo();
            }
        }

        private void CopyNode(TraceNode node, StringBuilder tempStr)
        {
            var level = TreeNodeAdvTools.GetLevel(node);
            for (var i = 0; i < level; i++)
                tempStr.Append("\t");
            tempStr.Append(node.CallNumber.ToString(CultureInfo.InvariantCulture));
            tempStr.Append("\t");
            tempStr.Append(node.ProcessName);
            tempStr.Append("\t");
            tempStr.Append(node.Pid);
            tempStr.Append("\t");
            tempStr.Append(node.Tid);
            tempStr.Append("\t");
            tempStr.Append(node.Caller);
            tempStr.Append("\t");
            tempStr.Append(node.Function);
            tempStr.Append("\t");
            tempStr.Append(node.ParamMain);
            tempStr.Append("\t");
            tempStr.Append(node.Details);
            tempStr.Append("\t");
            tempStr.Append(node.Result);
            tempStr.Append("\t");
            tempStr.Append(node.Time);
        }

        public void SelectAll()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //dispose managed ressources
                    base.Dispose(true);
                }
            }

            base.Dispose(disposing);
            //dispose unmanaged ressources
            _disposed = true;
        }

        private bool _disposed;

        public new void Dispose()
        {
            Dispose(true);
        }

        public void SelectAndEnsureVisibleCall(ulong callNumber)
        {
            TraceNode callModelNode;

            if (!_itemNumberToItem.TryGetValue(callNumber, out callModelNode))
            {
                MessageBox.Show(this, "The number entered does not correspond to any call event.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            BeginUpdate();
            ClearSelection();
            callModelNode.IsSelected = true;
            EnsureVisible(callModelNode, ScrollType.Middle);
            EndUpdate();
        }

        public void AddCallEventsOf(IEntry anEntry)
        {
            anEntry.AddCallEventsTo(this);
        }

        public void AddEventsOfSingleTrace(IEnumerable<CallEvent> callEvents)
        {
            foreach (var callEvent in callEvents)
                AddEvent(callEvent, false, false, CompareItemType.Item1AndItem2);
        }

        public void AddEventsOfTrace2(List<CallEvent> trace2Events)
        {
            foreach (var trace2Event in trace2Events)
            {
                AddEvent(trace2Event, false, false, CompareItemType.Item2);
            }
        }

        public void AddEventsOfTrace1(List<CallEvent> trace1Events)
        {
            foreach (var trace1Event in trace1Events)
            {
                AddEvent(trace1Event, false, false, CompareItemType.Item1);
            }
        }
    }
}