using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.Database;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Properties
{
    public partial class EntryPropertiesDialog : EntryPropertiesDialogBase
    {
        public EntryPropertiesDialog(IEntry anEntry)
            : base(anEntry)
        {
            InitializeComponent();
            CallEventsView.InitializeComponent();

            KeyDown += EntryPropertiesKeyDown;
            CallEventsView.NodeMouseDoubleClick += CallEventsViewOnDoubleClick;

            ListViewParams = _listViewParams;

            CallEventsView.InitializeComponent();
            CallEventsView.Controller = this;
            CallEventsView.SelectionChanged += OnCallEventSelectionChanged;

            if (CallEventsView.ContextMenuStrip == null)
                CallEventsView.ContextMenuStrip = new ContextMenuStrip();

            CallEventsContextMenu = new EntryPropertiesDialogContextMenu(this, CallEventsView);
        }

        private void CallEventsViewOnDoubleClick(object sender, EventArgs eventArgs)
        {
            if(CallEventsContextMenu.GoToEnabled)
                CallEventsContextMenu.GoTo();
        }

        protected void OnCallEventSelectionChanged(object sender, EventArgs e)
        {
            if (!CallEventsView.SelectedNodes.Any())
            {
                _listViewEvent.Items.Clear();
                _listViewParams.Items.Clear();
                _listViewStack.Items.Clear();
                return;
            }

            var curNode = CallEventsView.SelectedNodes[0];
            var item = (TraceTreeView.TraceNode)curNode;
            var eventIds = new List<CallEventId>();
            if(item.BeforeEventId != null)
                eventIds.Add(item.BeforeEventId);
            if(item.AfterEventId != null)
                eventIds.Add(item.AfterEventId);
            var callEvents = EventDatabaseMgr.GetInstance().GetEvents(eventIds, true);

            SetEventProperties(callEvents);
        }

        protected override void DisplayPropertiesOf(IEntry anEntry)
        {
            CallEventsView.BeginUpdate();

            CallEventsView.ClearData();

            Entry = anEntry;

            Text = Entry.NameForDisplay;

            CallEventsView.AddCallEventsOf(anEntry);

            CallEventsView.ClearSelection();

            if (CallEventsView.Model.Root.Nodes.Any())
                CallEventsView.Model.Root.Nodes.First().IsSelected = true;

            CallEventsView.ExpandAll();

            CallEventsView.EndUpdate();
        }

        protected void EntryPropertiesKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                if (CallEventsView.Focused)
                {
                    CallEventsView.SelectAll();
                }
                else if (_listViewEvent.Focused)
                {
                    ListViewTools.SelectAll(_listViewEvent);
                }
                else if (ListViewParams.Focused)
                {
                    ListViewTools.SelectAll(ListViewParams);
                }
                else if (_listViewStack.Focused)
                {
                    ListViewTools.SelectAll(_listViewStack);
                }
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                if (CallEventsView.Focused)
                {
                    CallEventsView.CopySelectionToClipboard();
                }
                else if (_listViewEvent.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(_listViewEvent);
                }
                else if (ListViewParams.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(ListViewParams);
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
            else if(e.KeyCode == Keys.Enter)
            {
                if(CallEventsContextMenu.GoToEnabled)
                {
                    CallEventsContextMenu.GoTo();
                }
            }
        }

        #region CallDetails dialog logic

        protected void SetEventProperties(IEnumerable<CallEvent> callEvents)
        {
            CallEvent evBefore = null, evAfter = null;
            foreach(var callEvent in callEvents)
            {
                if (callEvent.Before)
                    evBefore = callEvent;
                else
                    evAfter = callEvent;
            }
            SetEventProperties(evBefore, evAfter);
        }

        protected void SetEventProperties(CallEvent evBefore, CallEvent evAfter)
        {
            ListViewItem item;

#if DEBUG
            ListViewItem cookie = null;
            ListViewItem parent = null;
            ListViewItem peer = null;
#endif
            _listViewEvent.BeginUpdate();
            _listViewStack.BeginUpdate();
            _listViewParams.BeginUpdate();

            _listViewEvent.Items.Clear();
            _listViewStack.Items.Clear();
            CallCount = ProcessName = PID = Tid = Function = Caller = Win32Function = null;

            if (evBefore != null)
            {
                CallCount = item = _listViewEvent.Items.Add("Call number:");
                if (evAfter == null)
                    item.SubItems.Add(evBefore.CallNumber.ToString(CultureInfo.InvariantCulture));
                
#if DEBUG
                cookie = _listViewEvent.Items.Add("Cookie:");
                cookie.SubItems.Add(evBefore.Cookie.ToString(CultureInfo.InvariantCulture));

                parent = _listViewEvent.Items.Add("Parent:");
                parent.SubItems.Add(evBefore.Parent.ToString(CultureInfo.InvariantCulture));

                peer = _listViewEvent.Items.Add("Peer:");
                peer.SubItems.Add(evBefore.Peer.ToString(CultureInfo.InvariantCulture));
#endif
                ProcessName = item = _listViewEvent.Items.Add("Process Name:");
                item.SubItems.Add(evBefore.ProcessName);
                PID = item = _listViewEvent.Items.Add("Pid:");
                item.SubItems.Add(evBefore.Pid.ToString(CultureInfo.InvariantCulture) + " [0x" + evBefore.Pid.ToString("X") + "]");
                Tid = item = _listViewEvent.Items.Add("Tid:");
                item.SubItems.Add(evBefore.Tid.ToString(CultureInfo.InvariantCulture) + " [0x" + evBefore.Tid.ToString("X") + "]");
                Function = item = _listViewEvent.Items.Add("Function:");
                item.SubItems.Add(evBefore.Function);
                Win32Function = item = _listViewEvent.Items.Add("Win32 Function:");
                item.SubItems.Add(evBefore.Win32Function);
                Caller = item = _listViewEvent.Items.Add("Caller module:");
                item.SubItems.Add(evBefore.CallModule);

                AddParams(evBefore);

                if (!string.IsNullOrEmpty(evBefore.CallModule))
                {
                    item.SubItems.Add(evBefore.CallModule);
                }
                AddStack(evBefore);
            }
            if (evAfter != null)
            {
                if (CallCount == null)
                    CallCount = _listViewEvent.Items.Add("Call number:");
            
                CallCount.SubItems.Add(evAfter.CallNumber.ToString(CultureInfo.InvariantCulture));
#if DEBUG
                if (cookie == null)
                    cookie = _listViewEvent.Items.Add("Cookie:");
                else
                    cookie.SubItems.RemoveAt(1);
                cookie.SubItems.Add(evAfter.Cookie.ToString(CultureInfo.InvariantCulture));

                if (parent == null)
                    parent = _listViewEvent.Items.Add("Parent:");
                else
                    parent.SubItems.RemoveAt(1);
                parent.SubItems.Add(evAfter.Parent.ToString(CultureInfo.InvariantCulture));

                if (peer == null)
                    peer = _listViewEvent.Items.Add("Peer:");
                else
                    peer.SubItems.RemoveAt(1);
                peer.SubItems.Add(evAfter.Peer.ToString(CultureInfo.InvariantCulture));
#endif

                if (ProcessName == null)
                {
                    ProcessName = item = _listViewEvent.Items.Add("Process Name:");
                    item.SubItems.Add(evAfter.ProcessName);
                }
                if (PID == null)
                {
                    PID = item = _listViewEvent.Items.Add("Pid:");
                    item.SubItems.Add(evAfter.Pid.ToString(CultureInfo.InvariantCulture) + " [0x" + evAfter.Pid.ToString("X") + "]");
                }
                if (Tid == null)
                {
                    Tid = item = _listViewEvent.Items.Add("Tid:");
                    item.SubItems.Add(evAfter.Tid.ToString(CultureInfo.InvariantCulture) + " [0x" + evAfter.Tid.ToString("X") + "]");
                }
                if (Function == null)
                {
                    Function = item = _listViewEvent.Items.Add("Function:");
                    item.SubItems.Add(evAfter.Function);
                }
                if (Win32Function == null)
                {
                    Win32Function = item = _listViewEvent.Items.Add("Win32 Function:");
                    item.SubItems.Add(evAfter.Win32Function);
                }

                if (Caller == null)
                {
                    Caller = item = _listViewEvent.Items.Add("Caller module:");
                    item.SubItems.Add(evAfter.CallModule);
                }
                else
                {
                    Caller.SubItems[1].Text = evAfter.CallModule;
                }
#if DEBUG
                _listViewEvent.Items.Add("RetValue:").SubItems.Add(evAfter.RetValue.ToString(CultureInfo.InvariantCulture));
#endif
                AddParams(evAfter);
                AddStack(evAfter);
            }
            if (CallEventsContextMenu != null)
            {
                CallEventsContextMenu.GoToEnabled =
                    !((evBefore == null) && (evAfter == null));
                //CallEventsContextMenu.GoToEnabled =
                //    !((evBefore == null || evBefore.IsGenerated) && (evAfter == null || evAfter.IsGenerated));
            }

            _listViewParams.EndUpdate();
            _listViewStack.EndUpdate();
            _listViewEvent.EndUpdate();
        }

        #endregion

    }
}
