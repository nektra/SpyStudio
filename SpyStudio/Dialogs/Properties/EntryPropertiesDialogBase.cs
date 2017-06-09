using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs.Properties
{
    public partial class EntryPropertiesDialogBase : Form, IInterpreterController
    {
        public event Action<IEntry> UpClick;
        public event Action<IEntry> DownClick;

        public IEntry Entry;
        protected ListViewItem CallCount, ProcessName, PID, Tid, Function, Win32Function, Caller;
        protected EntryPropertiesDialogContextMenu CallEventsContextMenu;
        protected ListView ListViewParams;

        public EntryPropertiesDialogBase(IEntry anEntry)
        {
            Entry = anEntry;

            InitializeComponent();
            KeyPreview = true;

            Load += (sender, args) =>
                {
                    DisplayPropertiesOf(anEntry);
                    UpdateNavigationButtonsState();
                };
        }

        protected virtual void DisplayPropertiesOf(IEntry anEntry)
        {
            // Should be implemented by subclasses.
            throw new NotImplementedException();
        }
    
        protected void ButtonCloseClick(object sender, EventArgs e)
        {
            Close();
        }

        protected void ButtonUpClick(object sender, EventArgs e)
        {
            buttonUp.Enabled = false;

            UpClick(Entry);

            var previousEntry = Entry.PreviousVisibleEntry;

            DisplayPropertiesOf(previousEntry);

            UpdateNavigationButtonsState();
        }

        protected void ButtonDownClick(object sender, EventArgs e)
        {
            buttonDown.Enabled = false;

            DownClick(Entry);
            
            var nextEntry = Entry.NextVisibleEntry;

            DisplayPropertiesOf(nextEntry);

            UpdateNavigationButtonsState();
        }

        protected void UpdateNavigationButtonsState()
        {
            buttonUp.Enabled = Entry.PreviousVisibleEntry != null;
            buttonDown.Enabled = Entry.NextVisibleEntry != null;
        }

        protected void AddStack(CallEvent e)
        {
            if (_listViewStack.Items.Count == 0 && e.CallStack != null)
            {
                _listViewStack.BeginUpdate();
                var i = 1;
                foreach (var f in e.CallStack)
                {
                    var item = new ListViewItem(i++.ToString(CultureInfo.InvariantCulture));
                    item.SubItems.Add(f.ModuleName);
                    item.SubItems.Add(f.StackTraceString);
                    item.SubItems.Add("0x" + f.Eip.ToString("X"));
                    item.SubItems.Add(f.ModulePath);
                    _listViewStack.Items.Add(item);
                }
                _listViewStack.EndUpdate();
            }
        }

        protected void AddParams(CallEvent e)
        {
            ListViewParams.Items.Clear();

            if (e.Params == null)
                return;

            var i = 1;
            foreach (var p in e.Params)
            {
                var lvItem =
                    new ListViewItem(string.IsNullOrEmpty(p.Name)
                                         ? ("param" + i.ToString(CultureInfo.InvariantCulture))
                                         : p.Name);
                lvItem.SubItems.Add(p.Value.Replace('\0', ' '));
                ListViewParams.Items.Add(lvItem);
                i++;
            }
        }

        #region Implementation of IInterpreterController


        public void ShowInCom(ITraceEntry anEntry)
        {
            Debug.Assert(false);
        }

        public void ShowInWindows(ITraceEntry anEntry)
        {
            Debug.Assert(false);
        }

        public void ShowInFiles(ITraceEntry anEntry)
        {
            Debug.Assert(false);
        }

        public void ShowInRegistry(ITraceEntry anEntry)
        {
            Debug.Assert(false);
        }

        public bool ShowQueryAttributesInFiles
        {
            get { return false; }
        }

        public bool ShowDirectoriesInFiles
        {
            get { return false; }
        }

        public bool PropertiesGoToVisible
        {
            get { return false; }
        }

        public bool PropertiesVisible
        {
            get { return false; }
        }

        #endregion
    }
}
