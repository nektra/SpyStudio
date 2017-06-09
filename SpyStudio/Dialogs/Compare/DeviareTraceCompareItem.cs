using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Main;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.Compare
{
    public class DeviareTraceCompareItem : Node, ITraceEntry
    {
        private CompareItemType _itemType;
        private string _paramMain, _details;
        private bool _exactMatch;

        public DeviareTraceCompareItem(DeviareTraceCompareTreeView tree, DeviareTraceCompareItem original)
        {
            Tree = tree;
            Caller = original.Caller;
            StackFrame = original.StackFrame;
            Function = original.Function;
            ParamMain = original.ParamMain;
            Details = original.Details;
            Result = original.Result;
            ProcessName = original.ProcessName;
            Success = original.Success;
            ItemType = original.ItemType;
            Id = original.Id;
            MainCallEventIds = new HashSet<CallEventId>();
            SyncPoint = original.SyncPoint;
            ResultMismatchString = "";
            ResultsMatch = original.ResultsMatch;
        }

        protected DeviareTraceCompareItem(SyncPoint aSyncPoint)
        {
            CallEventIds = new HashSet<CallEventId>();
            MainCallEventIds = new HashSet<CallEventId>();

            if (aSyncPoint.HasSingleEvent)
            {
                var callEvent = aSyncPoint.GetFirstNonNullEvent();

                SyncPoint = aSyncPoint;
                Caller = callEvent.CallModule;
                StackFrame = callEvent.StackTraceString;
                Details = callEvent.ParamDetails;
                Result = callEvent.Result;
                ParamMain = GetParamOrEmpty(callEvent, 0);
                ProcessName = callEvent.ProcessName;
                Function = callEvent.Function;
                Success = callEvent.Success;
                ItemType = aSyncPoint.CompareItemType;

                var callEventId = new CallEventId(callEvent);

                MainCallEventIds.Add(callEventId);
                CallEventIds.Add(callEventId);

                IsCom = callEvent.IsCom;
                IsWindow = callEvent.IsWindow;
                IsFile = callEvent.IsFileSystem;
                IsDirectory = FileSystemEvent.IsDirectory(callEvent);
                IsRegistry = callEvent.IsRegistry;
                IsValue = RegQueryValueEvent.IsValue(callEvent);
                IsQueryAttributes = FileSystemEvent.IsQueryAttributes(callEvent);

                ResultsMatch = true;
                ResultMismatchString = "";

                ItemType = aSyncPoint.CompareItemType;

                Critical = false;
                Priority = 2;

                Debug.Assert(CompleteParamMain != null && CompleteDetails != null);

                return;
            }

            var callEvent1 = aSyncPoint.Event1.Event;
            var callEvent2 = aSyncPoint.Event2.Event;

            SyncPoint = aSyncPoint;

            MainCallEventIds.Add(new CallEventId(callEvent1));
            MainCallEventIds.Add(new CallEventId(callEvent2));
            CallEventIds.Add(new CallEventId(callEvent1));
            CallEventIds.Add(new CallEventId(callEvent2));

            Caller = callEvent1.CallModule;
            StackFrame = callEvent1.StackTraceString;
            Result = callEvent1.Result;
            ProcessName = callEvent1.ProcessName;
            Function = callEvent1.Function;
            Success = callEvent1.Success;
            ItemType = CompareItemType.Item1AndItem2;
            Details = string.Empty;
            Critical = !aSyncPoint.ResultsMatch;

            if (aSyncPoint.Type == EventMatchType.ExactMatch)
                Priority = 5;
            else if (aSyncPoint.Type == EventMatchType.AlmostExactMatch)
                Priority = 2;
            else
                Priority = 1;

            IsCom = callEvent1.IsCom;
            IsWindow = callEvent1.IsWindow;
            IsFile = callEvent1.IsFileSystem;
            IsDirectory = FileSystemEvent.IsDirectory(callEvent1);
            IsRegistry = callEvent1.IsRegistry;
            IsValue = RegQueryValueEvent.IsValue(callEvent1);
            IsQueryAttributes = FileSystemEvent.IsQueryAttributes(callEvent1);

            ResultsMatch = aSyncPoint.ResultsMatch;
            ResultMismatchString = aSyncPoint.ResultMismatchString ?? "";

            var length = callEvent1.ParamCount >= callEvent2.ParamCount ? callEvent1.ParamCount : callEvent2.ParamCount;

            for (var i = 0; i < length; i++)
            {
                var p1 = GetParamOrEmpty(callEvent1, i);
                var p2 = GetParamOrEmpty(callEvent2, i);
                var name = GetParamNameOrEmpty(callEvent1, callEvent2, i);
                if (!string.IsNullOrEmpty(name))
                    name += ": ";

                var value = p1 != p2
                                   ? p1.ForCompareString() + " / " + p2.ForCompareString()
                                   : p1;

                if (i == callEvent1.ParamMainIndex)
                {
                    ParamMain = value;
                }
                else
                {
                    if (!string.IsNullOrEmpty(value))
                        Details += name + value;
                }
            }

            if (callEvent1.ParamMainIndex == -1)
                    ParamMain = string.Empty;

            if (callEvent1.CallModule.ToLower() != callEvent2.CallModule.ToLower())
                Caller = callEvent1.CallModule.ForCompareString() + " / " + callEvent2.CallModule.ForCompareString();
            var sf1 = callEvent1.StackTraceString;
            var sf2 = callEvent2.StackTraceString;
            if (sf1 != sf2)
                StackFrame = sf1.ForCompareString() + " / " + sf2.ForCompareString();
            //if (GetParamOrEmpty(callEvent1, 1).ToLower() != GetParamOrEmpty(callEvent2, 1).ToLower())
            //{
            //    Details = GetParamOrEmpty(callEvent1, 1) + " / " + GetParamOrEmpty(callEvent2, 1);
            //}
            if (callEvent1.Result != callEvent2.Result)
            {
                Result = callEvent1.Result.ForCompareString() + " / " + callEvent2.Result.ForCompareString();
            }
            if (GetParamOrEmpty(callEvent1, 0).ToLower() != GetParamOrEmpty(callEvent2, 0).ToLower())
                ParamMain = GetParamOrEmpty(callEvent1, 0) + " / " + GetParamOrEmpty(callEvent2, 0);

            ExactMatch = aSyncPoint.Type == EventMatchType.ExactMatch;

            Debug.Assert(CompleteParamMain != null && CompleteDetails != null);
        }

        public CompareItemType ItemType
        {
            get { return _itemType; }
            set
            {
                _itemType = value;
                switch (_itemType)
                {
                    case CompareItemType.Item1:
                        Brush = new SolidBrush(EntryColors.File1Color);
                        break;
                    case CompareItemType.Item2:
                        Brush = new SolidBrush(EntryColors.File2Color);
                        break;
                }
            }
        }

        public override bool IsLeaf
        {
            get { return (Nodes.Count == 0); }
        }

        public IInterpreter Interpreter { get { return Tree; } }
        public HashSet<CallEventId> CallEventIds { get; set; }

        public SyncPoint SyncPoint { get; set; }

        public ulong Id { get; set; }

        public string Caller { get; set; }

        public string StackFrame { get; set; }

        public string Function { get; set; }

        public string Result { get; set; }

        public string ResultMismatchString { get; set; }

        public string ProcessName { get; set; }

        public string Win32Function { get; set; }

        public string ParamMain
        {
            get { return _paramMain; }
            set
            {
                CompleteParamMain = value;
                var index = CompleteParamMain.IndexOf('\n');
                _paramMain = index != -1 ? CompleteParamMain.Substring(0, index) : CompleteParamMain;
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
                var index = CompleteDetails.IndexOf('\n');
                _details = index != -1 ? CompleteDetails.Substring(0, index) : CompleteDetails;
            }
        }

        public bool Success { get; set; }

        public bool ExactMatch
        {
            get { return _exactMatch; }
            set { _exactMatch = value; }
        }

        private bool _resultsMatch;

        private string GetParamOrEmpty(CallEvent e, int index)
        {
            return e.ParamCount > index ? e.Params[index].Value : "";
        }

        private string GetParamNameOrEmpty(CallEvent e1, CallEvent e2, int index)
        {
            if (e1.ParamCount > index)
                return e1.Params[index].Name;
            if (e2.ParamCount > index)
                return e2.Params[index].Name;
            return "";
        }

        public bool ResultsMatch
        {
            get { return _resultsMatch; }
            set
            {
                _resultsMatch = value;
                Bold = !_resultsMatch;
            }
        }

        public void Update()
        {
            if (ItemType == CompareItemType.Item1AndItem2)
            {
                if (_exactMatch && ResultsMatch)
                {
                    ForeColor = Success ? EntryColors.ExactMatchSuccessColor : EntryColors.ExactMatchErrorColor;
                    ColorSummary = SystemColors.Window;
                }
                    // items match but results mismatch
                else if (!ResultsMatch)
                {
                    ForeColor = ColorSummary = EntryColors.MatchResultMismatchColor;
                }
                else
                {
                    ForeColor = Success ? EntryColors.MatchSuccessColor : EntryColors.MatchErrorColor;

                    ColorSummary = EntryColors.GetColorSummary(true, false, GetPriorityFromMatchType(SyncPoint.Type));
                }
            }
            else
            {
                ForeColor = Success ? EntryColors.NoMatchSuccessColor : EntryColors.NoMatchErrorColor;
                ColorSummary = BackColor;
            }
        }

        private int GetPriorityFromMatchType(EventMatchType aMatchType)
        {
            switch (aMatchType)
            {
                case EventMatchType.AlmostExactMatch:
                case EventMatchType.ExactMatch:
                case EventMatchType.BasicMatchWithCaller:
                    return 5;
                case EventMatchType.BasicMatch:
                    return 4;
                case EventMatchType.NoneMatch:
                    return 2;

            }
            return ((int) aMatchType) + 1;
        }

        public HashSet<DeviareTraceCompareItem> CompareItems
        {
            get
            {
                var compareItems = new List<DeviareTraceCompareItem> {this};

                foreach (var node in Nodes)
                    compareItems.AddRange(((DeviareTraceCompareItem) node).CompareItems);

                return new HashSet<DeviareTraceCompareItem>(compareItems);
            }
        }

        public IEntry NextVisibleEntry
        {
            get
            {
                return Tree.GetNextNode(this) as DeviareTraceCompareItem;
                //var containerNode = TreeNodeAdvTools.GetTreeNode(_tree, this);
                //var nextTreeNode = TreeNodeAdvTools.GetNextNode(containerNode);
                //return nextTreeNode != null
                //           ? (DeviareTraceCompareItem) nextTreeNode.Node
                //           : null;
            }
        }

        public IEntry PreviousVisibleEntry
        {
            get
            {
                return Tree.GetPreviousNode(this) as DeviareTraceCompareItem;
                //var containerNode = TreeNodeAdvTools.GetTreeNode(_tree, this);
                //var previousTreeNode = TreeNodeAdvTools.GetPreviousNode(containerNode);
                //return previousTreeNode != null
                //           ? (DeviareTraceCompareItem) previousTreeNode.Node
                //           : null;
            }
        }

        public EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryPropertiesDialog(this);
        }

        public string NameForDisplay
        {
            get
            {
                return Function + " @ " + Caller;

                //if (MainCallEvents.Count == 0)
                //    return Function;
                //string win32 = MainCallEvents.First().Win32Function;
                //return win32.Substring(0, win32.IndexOf('!')) + "!" + Function + " @ " + Caller;
            }
        }

        public bool IsForCompare
        {
            get { return true; }
        }

        public bool SupportsGoTo()
        {
            // Está bien que esto busque CompareItems dentro de otros CompareItems?
            RecursiveNodeSearch<DeviareTraceCompareItem>.SearchTerm f = tag => tag.CompareItems.Any(ci => ci.Id == Id);
            var child = RecursiveNodeSearch<DeviareTraceCompareItem>.FindNode(f, Tree.Model.Root);

            return (child != null);
        }

        public void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            var trace1EventIds = MainCallEventIds.Where(e => e.TraceId == Tree.Trace1Id).ToList();
            var trace2EventIds = MainCallEventIds.Where(e => e.TraceId == Tree.Trace2Id).ToList();
            
            foreach (var eventId in trace1EventIds)
                aTraceTreeView.AddFile1ItemToListView(eventId);

            foreach (var eventId in trace2EventIds)
                aTraceTreeView.AddFile2ItemToListView(eventId);
        }

        public void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        bool IEntry.SupportsGoTo
        {
            get { return Interpreter.SupportsGoTo; }
        }

        public bool GotoEnabled
        {
            get { return true; }
        }

        public HashSet<CallEventId> MainCallEventIds { get; set; }

        public bool IsCom { get; set; }
        public bool IsWindow { get; set; }
        public bool IsFile { get; set; }
        public bool IsQueryAttributes { get; set; }
        public bool IsDirectory { get; set; }

        public bool IsRegistry { get; set; }
        public bool IsValue { get; set; }

        public CallEventId EventId
        {
            get { return MainCallEventIds.FirstOrDefault(); }
        }

        public bool Critical { get; set; }

        public int Priority { get; set; }

        public Color Color { get { return EntryColors.GetColor(Success, Critical, Priority); } }

        public Color ColorSummary { get; set; }

        public Color BackColor { get { return ((SolidBrush)Brush).Color; } }

        public DeviareTraceCompareTreeView Tree { get; set; }

        public static DeviareTraceCompareItem From(SyncPoint aSyncPoint)
        {
            return new DeviareTraceCompareItem(aSyncPoint);
        }
    }
}