using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Aga.Controls;
using Aga.Controls.Tools;
using Aga.Controls.Tree;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.EventSummary;
using SpyStudio.Extensions;
using System.Linq;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Compare
{
    public class DeviareTraceCompareTreeView : TreeViewAdv, IInterpreter
    {
        protected ulong NextItemId = 1;
        private EventSummaryGraphic _eventSummary;
        private readonly List<DeviareTraceCompareItem> _eventSummaryPendingReportEvents = new List<DeviareTraceCompareItem>();
        public uint Trace1Id { get; set; }
        public uint Trace2Id { get; set; }

        public bool SupportsGoTo { get { return false; } }

        public Control ParentControl
        {
            get { return Parent; }
        }

        public EntryContextMenu ContextMenuController { get; set; }

        public IInterpreterController Controller { get; set; }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return SelectedNodes.Cast<IEntry>(); }
        }
        protected Dictionary<Node, Node> ImportedItemsByOriginalItem { get; set; }

        public DeviareTraceCompareTreeView()
        {
            BorderStyle = BorderStyle.FixedSingle;
            ShowLines = false;
            ImportedItemsByOriginalItem = new Dictionary<Node, Node>();

            if (ContextMenuStrip == null)
                ContextMenuStrip = new ContextMenuStrip();

            LoadStrategy = LCSMatchingStrategy.For(this);
            VisibleNodesChanged += (o, args) => OnVisibleNodesChanged();
        }

        #region User interaction

        public void CopySelectionToClipboard()
        {
            var tempStr = new StringBuilder("");

            foreach (var n in SelectedNodes)
            {
                if (tempStr.Length > 0)
                    tempStr.Append("\r\n");
                var node = (DeviareTraceCompareItem)n;
                CopyNode(node, tempStr);
            }
            if (tempStr.Length > 0)
            {
                Clipboard.SetDataObject(tempStr.ToString());
            }
        }

        void CopyNode(DeviareTraceCompareItem node, StringBuilder tempStr)
        {
            var level = TreeNodeAdvTools.GetLevel(node);
            for (var i = 0; i < level; i++)
                tempStr.Append("\t");
            tempStr.Append(node.ProcessName);
            tempStr.Append("\t");
            tempStr.Append(node.Caller);
            tempStr.Append("\t");
            tempStr.Append(node.StackFrame);
            tempStr.Append("\t");
            tempStr.Append(node.Function);
            tempStr.Append("\t");
            tempStr.Append(node.ParamMain);
            tempStr.Append("\t");
            tempStr.Append(node.Details);
            tempStr.Append("\t");
            tempStr.Append(node.Result);
        }

        public void FindEvent(FindEventArgs e)
        {
            Find(e);
        }

        public override bool Find(FindEventArgs e)
        {
            return base.Find(e);
        }

        public override bool IsMatch(string stringToMatch, Node curNode, FindEventArgs findArgs)
        {
            var node = (DeviareTraceCompareItem)curNode;
            if (node != null)
            {
                if (findArgs.MatchCase)
                {
                    if (node.Caller.Contains(stringToMatch) ||
                        node.StackFrame.Contains(stringToMatch) ||
                        node.Function.Contains(stringToMatch) ||
                        node.Result.Contains(stringToMatch) ||
                        node.ProcessName.Contains(stringToMatch) ||
                        node.ParamMain.Contains(stringToMatch) ||
                        node.Details.Contains(stringToMatch)
                        )
                    {
                        return true;
                    }
                }
                else
                {
                    if (node.Caller.ToLower().Contains(stringToMatch) ||
                        node.StackFrame.ToLower().Contains(stringToMatch) ||
                        node.Function.ToLower().Contains(stringToMatch) ||
                        node.Result.ToLower().Contains(stringToMatch) ||
                        node.ProcessName.ToLower().Contains(stringToMatch) ||
                        node.ParamMain.ToLower().Contains(stringToMatch) ||
                        node.Details.ToLower().Contains(stringToMatch)
                        )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Node Selection

        public void SelectAll()
        {
            //TreeNodeAdvTools.SelectAllNodes(this);
        }

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            TreeNodeAdvTools.SelectNextNode((Node)anEntry, this);
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            TreeNodeAdvTools.SelectPreviousNode((Node)anEntry, this);
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }

        #endregion

        #region Node Adding

        public void Add(DeviareTraceCompareItem item, Node parentItem)
        {
            item.Tree = this;

            if (item.Id == 0)
                item.Id = NextItemId++;

            if (parentItem == null)
            {
                ((TreeModel) Model).Nodes.Add(item);
            }
            else
            {
                parentItem.Nodes.Add(item);
            }

            var parentNode = item.Parent;
            
            while (!parentNode.IsRoot)
            {
                ((DeviareTraceCompareItem)parentNode).CallEventIds.AddRange(item.CallEventIds);
                parentNode = parentNode.Parent;
            }

            item.Update();

            if (_eventSummary != null)
            {
                if (parentItem == null)
                {
                    _eventSummaryPendingReportEvents.Add(item);
                }
            }

            if (parentItem != null && item.Critical && !item.IsExpanded)
            {
                EnsureExpanded(parentItem);
            }
        }

        public void InsertionFinished()
        {
            if (_eventSummary != null)
            {
                var reportNodes = new List<ITraceEntry>();
                foreach (var pendingNode in _eventSummaryPendingReportEvents)
                {
                    AddNodeRecursiveToList(pendingNode, reportNodes);
                }
                _eventSummary.AddRange(reportNodes);
                _eventSummaryPendingReportEvents.Clear();
            }
        }

        private void AddNodeRecursiveToList(DeviareTraceCompareItem node, List<ITraceEntry> list)
        {
            list.Add(node);
            foreach (DeviareTraceCompareItem child in node.Nodes)
            {
                AddNodeRecursiveToList(child, list);
            }
        }

        public DeviareTraceCompareItem Import(DeviareTraceCompareItem item, bool includeAncestors)
        {
            //We need to keep the id from the original.
            var importedItem = new DeviareTraceCompareItem(this, item)
                                   {
                                       Bold = item.Bold,
                                       Brush = item.Brush,
                                       CallEventIds = item.CallEventIds,
                                       MainCallEventIds = item.MainCallEventIds,
                                       CheckState = item.CheckState,
                                       ExactMatch = item.ExactMatch,
                                       ForeColor = item.ForeColor,
                                       Image = item.Image,
                                       ItemType = item.ItemType,
                                       ResultMismatchString = item.ResultMismatchString ?? "",
                                       ResultsMatch = item.ResultsMatch,
                                       Tag = item.Tag,
                                       Text = item.Text,
                                       Caller = item.Caller,
                                       StackFrame = item.StackFrame,
                                       Function = item.Function,
                                       Result = item.Result,
                                       ProcessName = item.ProcessName,
                                       Win32Function = item.Win32Function,
                                       ParamMain = item.CompleteParamMain,
                                       Details = item.CompleteDetails,
                                       Id = item.Id
                                   };

            var itemAncestors = new Stack<DeviareTraceCompareItem>();

            if (includeAncestors)
            {
                var itemAncestor = item.Parent ?? item;

                while (!itemAncestor.IsRoot)
                {
                    itemAncestors.Push((DeviareTraceCompareItem) itemAncestor);
                    itemAncestor = itemAncestor.Parent;
                }

                var importedItemAncestor = ((TreeModel) Model).Root;

                foreach (var pathItem in itemAncestors)
                {
                    if (ImportedItemsByOriginalItem.ContainsKey(pathItem))
                        continue;

                    importedItemAncestor = Import(pathItem, true);
                    ImportedItemsByOriginalItem.Add(pathItem, importedItemAncestor);
                }
            }

            Add(importedItem, itemAncestors.Any() ? ImportedItemsByOriginalItem[itemAncestors.Last()] : null);

            return importedItem;
        }

        #endregion

        public void ClearData()
        {
            BeginUpdate();
            ((TreeModel)Model).Nodes.Clear();
            ImportedItemsByOriginalItem.Clear();
            _eventSummaryPendingReportEvents.Clear();
            if (_eventSummary != null)
                _eventSummary.Clear();
            EndUpdate();
        }

        public override void BeginUpdate()
        {
            if (_eventSummary != null)
                _eventSummary.BeginUpdate();

            if (LoadStrategy != null)
                LoadStrategy.BeginUpdate();

            base.BeginUpdate();
        }

        public override void EndUpdate()
        {
            if (_eventSummary != null)
                _eventSummary.EndUpdate();

            if (LoadStrategy != null)
                LoadStrategy.EndUpdate();

            base.EndUpdate();
        }

        public TraceCompareTreeLoadStrategy LoadStrategy { get; set; }

        #region Events Summary

        public void SetEventSummary(EventSummaryGraphic eventSummary)
        {
            if (_eventSummary != null)
            {
                _eventSummary.RequestEventsSummary -= EventSummaryOnRequestEventsSummary;
                _eventSummary.FocusChange -= EventSummaryOnFocusChange;
            }
            _eventSummary = eventSummary;

            if (eventSummary != null)
            {
                _eventSummary.RequestEventsSummary += EventSummaryOnRequestEventsSummary;
                _eventSummary.FocusChange += EventSummaryOnFocusChange;
            }

            ShowVScrollBar = (_eventSummary == null);
        }

        private void EventSummaryOnFocusChange(EventSummaryEntryArgs eventEntryArgs)
        {
            var compareItem = eventEntryArgs.Entry as DeviareTraceCompareItem;
            Debug.Assert(compareItem != null);
            EnsureVisible(compareItem, ScrollType.Middle);
        }

        private void EventSummaryOnRequestEventsSummary(EventSummaryTooltipArgs eventEntriesArgs)
        {
            var text = new StringBuilder();
            var moreThan10Entries = eventEntriesArgs.Entries.Count > 10;

            eventEntriesArgs.EntriesText = string.Empty;

            // get information about the first 10 nodes
            var nodes = eventEntriesArgs.Entries.Take(10).OfType<DeviareTraceCompareItem>();

            foreach (var compareItem in nodes)
            {
                if (text.Length != 0)
                    text.Append("\n");
                
                text.Append(compareItem.Function);
                text.Append(" ");
                if (!string.IsNullOrEmpty(compareItem.ParamMain))
                {
                    text.Append(compareItem.ParamMain.FirstChars(50));
                    text.Append(" ");
                }

                if (!string.IsNullOrEmpty(compareItem.Details))
                {
                    text.Append(compareItem.Details.FirstChars(50));
                }
            }

            if (text.Length == 0) 
                return;

            if (moreThan10Entries)
                text.Append("\n...");

            eventEntriesArgs.EntriesText = text.ToString();
        }

        private void OnVisibleNodesChanged()
        {
            var visibleNodes = VisibleNodes.Select(treeNode => treeNode.Node).ToList();

            if (_eventSummary == null) 
                return;

            TreeViewAdvTools.SortByTreeRow(visibleNodes);
            var entries = new List<ITraceEntry>();
            Node prev = null;
            foreach (var n in visibleNodes)
            {
                if (prev != null)
                {
                    Debug.Assert(TreeViewAdvTools.NodeComparer.Compare(prev, n) < 0);
                }
                entries.Add((ITraceEntry)n);
                prev = n;
            }
            //var entries = newVisibleNodesOrdered.Cast<ITraceEntry>().ToList();
            _eventSummary.SetFocusRect(entries);
        }

        #endregion

    }
}