using System;
using System.Globalization;
using System.Windows.Forms;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs
{
    public partial class CallDetails : Form
    {
        public event CallChangeEventHandler UpClick;
        public event CallChangeEventHandler DownClick;
        CallEvent _evBefore = null, _evAfter = null;
        ListViewItem _callCount, _processName, _pid, _tid, _function, _win32Function, _caller;
        
        public CallDetails()
        {
            InitializeComponent();
            KeyPreview = true;

        }
        public void SetEvents(CallEvent evBefore, CallEvent evAfter)
        {
            ListViewItem item;
            
            listViewEvent.Items.Clear();
            listViewStack.Items.Clear();
            _callCount = _processName = _pid = _tid = _function = _caller = _win32Function = null;

            _evBefore = evBefore;
            _evAfter = evAfter;

            if (evBefore != null)
            {
                _callCount = item = listViewEvent.Items.Add("Call number:");
                item.SubItems.Add(evBefore.CallNumber.ToString(CultureInfo.InvariantCulture));
                _processName = item = listViewEvent.Items.Add("Process Name:");
                item.SubItems.Add(evBefore.ProcessName);
                _pid = item = listViewEvent.Items.Add("Pid:");
                item.SubItems.Add(evBefore.Pid.ToString(CultureInfo.InvariantCulture) + " [0x" + evBefore.Pid.ToString("X") + "]");
                _tid = item = listViewEvent.Items.Add("Tid:");
                item.SubItems.Add(evBefore.Tid.ToString(CultureInfo.InvariantCulture) + " [0x" + evBefore.Tid.ToString("X") + "]");
                _function = item = listViewEvent.Items.Add("Function:");
                item.SubItems.Add(evBefore.Function);
                _win32Function = item = listViewEvent.Items.Add("Win32 Function:");
                item.SubItems.Add(evBefore.Win32Function);
                _caller = item = listViewEvent.Items.Add("Caller module:");
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
                if (_callCount == null)
                {
                    _callCount = item = listViewEvent.Items.Add("Call number:");
                    item.SubItems.Add(evAfter.CallNumber.ToString());
                }
                if (_processName == null)
                {
                    _processName = item = listViewEvent.Items.Add("Process Name:");
                    item.SubItems.Add(evAfter.ProcessName);
                }
                if (_pid == null)
                {
                    _pid = item = listViewEvent.Items.Add("Pid:");
                    item.SubItems.Add(evAfter.Pid.ToString() + " [0x" + evAfter.Pid.ToString("X") + "]");
                }
                if (_tid == null)
                {
                    _tid = item = listViewEvent.Items.Add("Tid:");
                    item.SubItems.Add(evAfter.Tid.ToString() + " [0x" + evAfter.Tid.ToString("X") + "]");
                }
                if (_function == null)
                {
                    _function = item = listViewEvent.Items.Add("Function:");
                    item.SubItems.Add(evAfter.Function);
                }
                if (_win32Function == null)
                {
                    _win32Function = item = listViewEvent.Items.Add("Win32 Function:");
                    item.SubItems.Add(evAfter.Win32Function);
                }

                if (_caller == null)
                {
                    _caller = item = listViewEvent.Items.Add("Caller module:");
                    item.SubItems.Add(evAfter.CallModule);
                }
                else
                {
                    _caller.SubItems[1].Text = evAfter.CallModule;
                }
                AddParams(evAfter);
                AddStack(evAfter);
            }
        }

        void AddParams(CallEvent e)
        {
            listViewParams.Items.Clear();

            if(e.Params != null)
            {
                int i = 1;
                foreach (var p in e.Params)
                {
                    var lvItem = listViewParams.Items.Add(string.IsNullOrEmpty(p.Name) ? ("param" + i.ToString(CultureInfo.InvariantCulture)) : p.Name);
                    lvItem.SubItems.Add(p.Value);
                    i++;
                }
            }
        }
        void AddStack(CallEvent e)
        {
            if (listViewStack.Items.Count == 0 && e.CallStack!= null)
            {
                listViewStack.BeginUpdate();
                int i = 1;
                foreach (DeviareTools.DeviareStackFrame f in e.CallStack)
                {
                    var item = new ListViewItem(i++.ToString());
                    item.SubItems.Add(f.ModuleName);
                    item.SubItems.Add(f.StackTraceString);
                    item.SubItems.Add("0x" + f.Eip.ToString("X"));
                    item.SubItems.Add(f.ModulePath);
                    listViewStack.Items.Add(item);
                }
                listViewStack.EndUpdate();
            }
        }

        private void ButtonDownClick(object sender, EventArgs e)
        {
            DownClick(this, new CallChangeEventArgs(false, _evBefore, _evAfter));
        }

        private void ButtonUpClick(object sender, EventArgs e)
        {
            UpClick(this, new CallChangeEventArgs(false, _evBefore, _evAfter));
        }

        private void ButtonCloseClick(object sender, EventArgs e)
        {
            Close();
        }
        private void CallDetailsKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                if(listViewParams.Focused)
                {
                    foreach (ListViewItem item in listViewParams.Items)
                    {
                        item.Selected = true;
                    }
                }
                else if(listViewStack.Focused)
                {
                    foreach (ListViewItem item in listViewStack.Items)
                    {
                        item.Selected = true;
                    }
                }
                else if (listViewEvent.Focused)
                {
                    foreach (ListViewItem item in listViewEvent.Items)
                    {
                        item.Selected = true;
                    }
                }
            }
            else if(e.KeyCode == Keys.C && e.Control)
            {
                if (listViewParams.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(listViewParams);
                }
                else if (listViewStack.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(listViewStack);
                }
                else if (listViewEvent.Focused)
                {
                    ListViewTools.CopySelectionToClipboard(listViewEvent);
                }
            }
        }
    }


    public class CallChangeEventArgs : EventArgs
    {
        readonly CallEvent _evBefore;
        readonly CallEvent _evAfter;
        readonly bool _up;
        public CallChangeEventArgs(bool up, CallEvent evBefore, CallEvent evAfter)
        {
            _evBefore = evBefore;
            _evAfter = evAfter;
            _up = up;
        }
        public CallEvent BeforeEvent
        {
            get { return _evBefore; }
        }
        public CallEvent AfterEvent
        {
            get { return _evAfter; }
        }
        public bool Up
        {
            get { return _up; }
        }
    }

    public delegate void CallChangeEventHandler(object sender, CallChangeEventArgs e);

}
