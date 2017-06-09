using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.Properties
{
    public partial class EntryComparePropertiesDialog : EntryPropertiesDialogBase
    {
        protected int CallStack1StartIndex;
        protected int CallStack2StartIndex;
        protected int CallStack1EndIndex;
        protected int CallStack2EndIndex;
        private ColumnHeader _columnHeaderValue2;
        private ColumnHeader _columnValue2;

        public EntryComparePropertiesDialog(IEntry anEntry)
            : base(anEntry)
        {
            InitializeComponent();

            ListViewParams = _listViewParams;

            KeyDown += OnKeyDown;
            CompareItemsView.Model = new TreeModel();
            CompareItemsView.Controller = this;
            CompareItemsView.SelectionChanged += OnCallEventSelectionChanged;
            CompareItemsView.NodeMouseDoubleClick += CallEventsViewOnDoubleClick;

            if (CompareItemsView.ContextMenuStrip == null)
                CompareItemsView.ContextMenuStrip = new ContextMenuStrip();

            CallEventsContextMenu = new EntryPropertiesDialogContextMenu(this, CompareItemsView);
        }
        private void CallEventsViewOnDoubleClick(object sender, EventArgs eventArgs)
        {
            if (CallEventsContextMenu.GoToEnabled)
                CallEventsContextMenu.GoTo();
        }

        protected void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                if (CompareItemsView.Focused)
                {
                    CompareItemsView.SelectAll();
                }
                else if (_listViewStack.Focused)
                {
                    ListViewTools.SelectAll(_listViewStack);
                }
                else if (_listViewEvent.Focused)
                {
                    ListViewTools.SelectAll(_listViewEvent);
                }
                else if (_listViewParams.Focused)
                {
                    ListViewTools.SelectAll(_listViewParams);
                }
                else if (_listViewStack.Focused)
                {
                    ListViewTools.SelectAll(_listViewStack);
                }
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                if (CompareItemsView.Focused)
                {
                    CompareItemsView.CopySelectionToClipboard();
                }
                else if (_listViewStack.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(_listViewStack);
                }
                else if (_listViewEvent.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(_listViewEvent);
                }
                else if (_listViewParams.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(_listViewParams);
                }
                else if (_listViewStack.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(_listViewStack);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (CallEventsContextMenu.GoToEnabled)
                {
                    CallEventsContextMenu.GoTo();
                }
            }
        }

        protected void OnCallEventSelectionChanged(object sender, EventArgs e)
        {
            if (!CompareItemsView.SelectedNodes.Any())
            {
                _listViewEvent.Items.Clear();
                _listViewParams.Items.Clear();
                _listViewStack.Items.Clear();

                return;
            }

            var curNode = CompareItemsView.SelectedNodes[0];
            var item = (DeviareTraceCompareItem)curNode;

            if (item.ItemType == CompareItemType.Item1AndItem2)
                DisplayPropertiesForComparedCallEvents(item);
            else
                DisplayPropertiesForSingleCallEvent(item);
        }

        protected override void DisplayPropertiesOf(IEntry anEntry)
        {
            CompareItemsView.ClearData();

            Entry = anEntry;

            Text = Entry.NameForDisplay;

            CompareItemsView.BeginUpdate();

            foreach (var compareItem in anEntry.CompareItems)
                CompareItemsView.Import(compareItem, false);

            CompareItemsView.ClearSelection();

            if (CompareItemsView.Model.Root.Nodes.Any())
                CompareItemsView.Model.Root.Nodes.First().IsSelected = true;

            CompareItemsView.EndUpdate();

            CompareItemsView.ExpandAll();
        }

        #region CompareItems dialog logic

        public void DisplayPropertiesForComparedCallEvents(DeviareTraceCompareItem aCompareItem)
        {
            var events = aCompareItem.MainCallEventIds.FetchEvents(true).ToArray();
            var callEvent1 = events[0];
            var callEvent2 = events[1];
            var syncPoint = aCompareItem.SyncPoint;

            _listViewEvent.Items.Clear();
            _listViewStack.Items.Clear();

            _listViewEvent.Columns[1].Text = "Value 1";
            _listViewParams.Columns[1].Text = "Value 1";

            if (_listViewEvent.Columns.Count < 3)
                _listViewEvent.Columns.Add(_columnHeaderValue2);

            if (_listViewParams.Columns.Count < 3)
                _listViewParams.Columns.Add(_columnValue2);
            

            LoadComparedCallEventProperty("Call Number:", callEvent1.CallNumber, callEvent2.CallNumber);
            LoadComparedCallEventProperty("Process Name:", callEvent1.ProcessName, callEvent2.ProcessName);
            LoadComparedCallEventProperty("Pid:", callEvent1.Pid + " [0x" + callEvent1.Pid.ToString("X") + "]",
                                          callEvent2.Pid + " [0x" + callEvent2.Pid.ToString("X") + "]");
            LoadComparedCallEventProperty("Tid:", callEvent1.Tid + " [0x" + callEvent1.Tid.ToString("X") + "]",
                                          callEvent2.Tid + " [0x" + callEvent2.Tid.ToString("X") + "]");
            LoadComparedCallEventProperty("Function:", callEvent1.Function, callEvent2.Function);
            LoadComparedCallEventProperty("Win32 Function:", callEvent1.Win32Function,
                                          callEvent2.Win32Function);
            LoadComparedCallEventProperty("Caller module:", callEvent1.CallModule, callEvent2.CallModule);

            AddComparedParams(callEvent1, callEvent2, syncPoint.FunctionInfo);
            AddComparedStack(callEvent1, callEvent2);
        }

        public void DisplayPropertiesForSingleCallEvent(DeviareTraceCompareItem anItem)
        {
            var events = anItem.MainCallEventIds.FetchEvents(true).ToArray();
            Debug.Assert(events != null && events.Any());
            var callEvent = events[0];

            _listViewEvent.Items.Clear();
            _listViewStack.Items.Clear();

            if (_listViewEvent.Columns.Count > 2)
                _listViewEvent.Columns.RemoveAt(2);
            
            _listViewEvent.Columns[1].Text = "Value";

            if (_listViewParams.Columns.Count > 2)
                _listViewParams.Columns.RemoveAt(2);

            _listViewParams.Columns[1].Text = "Value";

            LoadCallEventProperty("Call Number:", callEvent.CallNumber);
            LoadCallEventProperty("Process Name:", callEvent.ProcessName);
            LoadCallEventProperty("Pid:", callEvent.Pid + " [0x" + callEvent.Pid.ToString("X") + "]");
            LoadCallEventProperty("Tid:", callEvent.Tid + " [0x" + callEvent.Tid.ToString("X") + "]");
            LoadCallEventProperty("Function:", callEvent.Function);
            LoadCallEventProperty("Win32 Function:", callEvent.Win32Function);
            LoadCallEventProperty("Caller module:", callEvent.CallModule);

            AddParams(callEvent);

            if (callEvent.CallStack == null && anItem.MainCallEventIds.Count > 1)
            {
                AddStack(events[0]);
            }
            else
                AddStack(callEvent);
        }

        private void LoadComparedCallEventProperty(string aPropertyName, object value1, object value2)
        {
            var string1 = value1 == null ? "" : value1.ToString();
            var string2 = value2 == null ? "" : value2.ToString();

            var propertyListItem = _listViewEvent.Items.Add(aPropertyName);

            propertyListItem.SubItems.Add(string1);
            propertyListItem.SubItems.Add(string2);

            if (aPropertyName.StartsWith("Pid")
                || aPropertyName.StartsWith("Tid")
                || aPropertyName.StartsWith("Call Number"))
                return;

            if (!string1.Equals(string2))
                propertyListItem.Font = new Font(propertyListItem.Font, FontStyle.Bold);
        }

        private void AddComparedParams(CallEvent callEvent1, CallEvent callEvent2, FunctionInfo functionInfo)
        {
            _listViewParams.Items.Clear();

            int paramCount;
            CallEvent validCallEvent;

            if (callEvent1.Params == null && callEvent2.Params == null)
            {
                validCallEvent = callEvent1;
                paramCount = 0;
            }
            else if (callEvent1.Params == null)
            {
                validCallEvent = callEvent2;
                paramCount = callEvent2.Params.Length;
                callEvent2 = new CallEvent(false) {Params = new Param[paramCount]};
            }
            else if (callEvent2.Params == null)
            {
                validCallEvent = callEvent1;
                paramCount = callEvent1.Params.Length;
                callEvent1 = new CallEvent(false) {Params = new Param[paramCount]};
            }
            else
            {
                validCallEvent = callEvent1;
                paramCount = callEvent1.Params.Length;
            }

            Debug.Assert(callEvent1.Params != null && callEvent2.Params != null);

            for (var i = 0; i < paramCount; i++)
            {
                var item =
                    _listViewParams.Items.Add(string.IsNullOrEmpty(callEvent1.Params[i].Name)
                                                 ? ("param" + i.ToString(CultureInfo.InvariantCulture))
                                                 : validCallEvent.Params[i].Name);

                item.SubItems.Add(callEvent1.Params[i].Value);
                item.SubItems.Add(callEvent2.Params.Length > i ? callEvent2.Params[i].Value : string.Empty);

                var matchInfo = functionInfo.MatchInfos.FirstOrDefault(fm => fm.Index == i);

                if (matchInfo == default(MatchInfo))
                    continue;

                var stringToMatch1 = matchInfo.GetMatchStringFor(callEvent1);
                var stringToMatch2 = matchInfo.GetMatchStringFor(callEvent2);

                if (!stringToMatch1.Equals(stringToMatch2))
                    item.Font = new Font(item.Font, FontStyle.Bold);
            }
        }

        private void AddComparedStack(CallEvent callEvent1, CallEvent callEvent2)
        {
            if (_listViewStack.Items.Count != 0 || (callEvent1.CallStack == null && callEvent2.CallStack == null))
                return;

            if (callEvent1.CallStack == null)
            {
                AddStack(callEvent2);
                return;
            }
            if (callEvent2.CallStack == null)
            {
                AddStack(callEvent1);
                return;
            }

            var bestMatchPoint = FindBestMatchIndex(callEvent1.CallStack, callEvent2.CallStack);

            if (bestMatchPoint.ContiguousMatches != 0)
            {
                if (bestMatchPoint.Index1 >= bestMatchPoint.Index2)
                {
                    CallStack1StartIndex = 0;
                    CallStack2StartIndex = bestMatchPoint.Index1 - bestMatchPoint.Index2;
                }
                else
                {
                    CallStack1StartIndex = bestMatchPoint.Index2 - bestMatchPoint.Index1;
                    CallStack2StartIndex = 0;
                }
            }
                // no match
            else
            {
                CallStack1StartIndex = 0;
                CallStack2StartIndex = callEvent1.CallStack.Count;
            }

            CallStack1EndIndex = CallStack1StartIndex + callEvent1.CallStack.Count - 1;
            CallStack2EndIndex = CallStack2StartIndex + callEvent2.CallStack.Count - 1;

            var stackViewLines = CallStack1EndIndex >= CallStack2EndIndex
                                     ? CallStack1EndIndex + 1
                                     : CallStack2EndIndex + 1;

            _listViewStack.BeginUpdate();

            for (var i = 0; i < stackViewLines; i++)
            {
                var item = new ListViewItem((i + 1).ToString(CultureInfo.InvariantCulture));

                var stackFrame1 = CallStack1StartIndex <= i && i <= CallStack1EndIndex
                                      ? callEvent1.CallStack[i - CallStack1StartIndex]
                                      : null;

                var stackFrame2 = CallStack2StartIndex <= i && i <= CallStack2EndIndex
                                      ? callEvent2.CallStack[i - CallStack2StartIndex]
                                      : null;

                if (stackFrame1 == null)
                {
                    item.SubItems.Add(stackFrame2.ModuleName);
                    item.SubItems.Add(stackFrame2.StackTraceString);
                    item.SubItems.Add("0x" + stackFrame2.Eip.ToString("X"));
                    item.SubItems.Add(stackFrame2.ModulePath);
                    item.BackColor = EntryColors.File2Color;
                }
                else if (stackFrame2 == null)
                {
                    item.SubItems.Add(stackFrame1.ModuleName);
                    item.SubItems.Add(stackFrame1.StackTraceString);
                    item.SubItems.Add("0x" + stackFrame1.Eip.ToString("X"));
                    item.SubItems.Add(stackFrame1.ModulePath);
                    item.BackColor = EntryColors.File1Color;
                }
                else
                {
                    InsertStackFramePropertyString(item, i, stackFrame1.ModuleName,
                                                   stackFrame2.ModuleName);

                    InsertStackFramePropertyString(item, i, stackFrame1.StackTraceString,
                                                   stackFrame2.StackTraceString);

                    InsertStackFramePropertyString(item, i, "0x" + stackFrame1.Eip.ToString("X"),
                                                   "0x" + stackFrame2.Eip.ToString("X"));

                    InsertStackFramePropertyString(item, i, stackFrame1.ModulePath,
                                                   stackFrame2.ModulePath);

                    if (stackFrame1.StackTraceString.Equals(stackFrame2.StackTraceString))
                    {
                        item.ForeColor = EntryColors.ExactMatchSuccessColor;
                    }
                    else if (!stackFrame1.ModuleName.Equals(stackFrame2.ModuleName))
                    {
                        item.Font = new Font(item.Font, FontStyle.Bold);
                    }
                }

                _listViewStack.Items.Add(item);
            }

            _listViewStack.EndUpdate();
        }

        private void InsertStackFramePropertyString(ListViewItem item, int lineIndex, string stack1Property,
                                                    string stack2Property)
        {
            var property = "";

            if (CallStack1StartIndex <= lineIndex && lineIndex <= CallStack1EndIndex)
                property += stack1Property;

            if (CallStack2StartIndex <= lineIndex && lineIndex <= CallStack2EndIndex)
            {
                if (!string.IsNullOrEmpty(property))
                {
                    if (property != stack2Property)
                        property = property.ForCompareString() + " / " + stack2Property.ForCompareString();
                }
                else
                    property += stack2Property;
            }

            item.SubItems.Add(property);
        }

        private MatchPoint FindBestMatchIndex(ICollection<DeviareTools.DeviareStackFrame> callStack1,
                                              ICollection<DeviareTools.DeviareStackFrame> callStack2)
        {
            var matchPoints = new List<MatchPoint>();

            var callStack1Count = callStack1.Count;
            var callStack2Count = callStack2.Count;
            for (var index1 = 0; index1 < callStack1Count; index1++)
            {
                for (var index2 = 0; index2 < callStack2Count; index2++)
                    //foreach (var stackFrame2 in callStack2)
                {
                    var startingStackFrame1 = callStack1.ElementAt(index1);
                    var startingStackFrame2 = callStack2.ElementAt(index2);
                    var stackFrame1 = startingStackFrame1;
                    var stackFrame2 = startingStackFrame2;
                    var contiguousMatches = 0;

                    var index1Aux = index1;
                    var index2Aux = index2;

                    while (stackFrame1.MatchesWith(stackFrame2) && index1Aux < callStack1Count &&
                           index2Aux < callStack2Count)
                    {
                        contiguousMatches++;
                        stackFrame1 = callStack1.ElementAt(index1Aux);
                        stackFrame2 = callStack2.ElementAt(index2Aux);

                        index1Aux++;
                        index2Aux++;
                    }

                    matchPoints.Add(new MatchPoint(index1, index2, contiguousMatches));
                }
            }

            const int maxContiguousMatches = 0;
            var bestMatchPoint = new MatchPoint(0, callStack1.Count(), 0);
                // first stack first, then second stack - no matchs.

            foreach (var matchPoint in matchPoints)
            {
                if (matchPoint.ContiguousMatches > maxContiguousMatches)
                    bestMatchPoint = matchPoint;
            }

            return bestMatchPoint;
        }

        private void LoadCallEventProperty(string aPropertyName, object aValue)
        {
            var string1 = aValue == null ? "" : aValue.ToString();

            var propertyListItem = _listViewEvent.Items.Add(aPropertyName);
            propertyListItem.SubItems.Add(string1);
        }

        #endregion
    }
}
