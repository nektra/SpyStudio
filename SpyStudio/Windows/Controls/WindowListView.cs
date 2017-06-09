using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.Extensions;
using System.Linq;

namespace SpyStudio.Windows.Controls
{
    public class WindowListView : ListViewSorted, IInterpreter
    {
        public Control ParentControl
        {
            get { return this; }
        }

        public IInterpreterController Controller { get; set; }

        public List<CallEventId> CallEventIds { get; set; }

        public IEnumerable<IEntry> SelectedEntries
        {
            get { return SelectedItems.Cast<IEntry>(); }
        }

        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }
        
        #region Column Headers

        private readonly ColumnHeader _columnHeaderWindowClass;
        private readonly ColumnHeader _columnHeaderWindowName;
        private readonly ColumnHeader _columnHeaderWindowResult;
        private readonly ColumnHeader _columnHeaderhModulePath;
        private readonly ColumnHeader _columnHeaderWindowTime;
        private readonly ColumnHeader _columnHeaderWindowCount;

        #endregion

        #region Instantiation

        public WindowListView()
        {
            _columnHeaderWindowClass = new ColumnHeader();
            _columnHeaderWindowName = new ColumnHeader();
            _columnHeaderhModulePath = new ColumnHeader();
            _columnHeaderWindowResult = new ColumnHeader();
            _columnHeaderWindowTime = new ColumnHeader();
            _columnHeaderWindowCount = new ColumnHeader();

            Columns.AddRange(new[] {
            _columnHeaderWindowClass,
            _columnHeaderWindowName,
            _columnHeaderhModulePath,
            _columnHeaderWindowResult,
            _columnHeaderWindowCount,
            _columnHeaderWindowTime
            });
            // 
            // columnHeaderWindowClass
            // 
            _columnHeaderWindowClass.Text = "Class";
            _columnHeaderWindowClass.Width = 228;
            // 
            // columnHeaderWindowName
            // 
            _columnHeaderWindowName.Text = "Name";
            _columnHeaderWindowName.Width = 191;
            // 
            // columnHeaderhModulePath
            // 
            _columnHeaderhModulePath.Text = "hModule";
            // 
            // columnHeaderWindowResult
            // 
            _columnHeaderWindowResult.Text = "Result";
            // 
            // columnHeaderWindowTime
            // 
            _columnHeaderWindowTime.Tag = "Double";
            _columnHeaderWindowTime.Text = "Time";
            _columnHeaderWindowTime.TextAlign = HorizontalAlignment.Right;

            // 
            // columnHeaderWindowCount
            // 
            _columnHeaderWindowCount.Tag = "Numeric";
            _columnHeaderWindowCount.Text = "Count";
            _columnHeaderWindowCount.TextAlign = HorizontalAlignment.Right;
        }

        public void InitializeComponent()
        {
            //ContextMenuStrip.Opening += ContextMenuOpening;
            //ContextMenuStrip.Items[0].Click += ContextMenuRemoveCurrent;
            //ContextMenuStrip.Items[1].Click += ContextMenuRemoveAll;
            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuController = new EntryContextMenu(this);
        }

        #endregion

        #region Control

        public void Attach(DeviareRunTrace devRunTrace)
        {
            devRunTrace.CreateWindowAdd += Add;
            devRunTrace.UpdateBegin += (sender, args) => this.ExecuteInUIThreadAsynchronously(BeginUpdate);
            devRunTrace.UpdateEnd += (sender, args) => this.ExecuteInUIThreadAsynchronously(EndUpdate);
            devRunTrace.WindowClear += ClearData;
        }

        public void Add(WindowInfo aWindowInfo)
        {
            WindowListViewItemBase windowItem;
            if (!Items.ContainsKey(aWindowInfo.ID))
            {
                windowItem = CreateNewItemNamed(aWindowInfo.ID);

                Items.Add(windowItem);

                windowItem.Merge(aWindowInfo);

                return;
            }

            windowItem = (WindowListViewItemBase)Items[aWindowInfo.ID];

            windowItem.Merge(aWindowInfo);
        }

        protected virtual WindowListViewItemBase CreateNewItemNamed(string aName)
        {
            return WindowListViewItem.Named(aName);
        }

        public void ClearData()
        {
            if (ContextMenuController != null)
                ContextMenuController.Close(false);

            BeginUpdate();
            Items.Clear();
            EndUpdate();
        }

        public void FindEvent(FindEventArgs e)
        {
            ListViewTools.Find(this, e);
        }

        public void CopySelectionToClipboard()
        {
            ListViewTools.CopySelectionToClipboard(this);
        }

        public void SelectAll()
        {
            ListViewTools.SelectAll(this);
        }

        public void SelectItemContaining(CallEventId eventId)
        {
            CallEventContainerTools.SelectItemContaining(this, eventId);
        }

        #endregion

        #region Context Menu

        public EntryContextMenu ContextMenuController { get; protected set; }

        public void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            // enable delete only if there are selected items
            ContextMenuStrip.Items[0].Visible = SelectedItems.Count >= 1;
        }

        public void ContextMenuRemoveCurrent(object sender, EventArgs e)
        {
            foreach (ListViewItem item in SelectedItems)
            {
                item.Remove();
            }
        }

        public void ContextMenuRemoveAll(object sender, EventArgs e)
        {
            ClearData();
        }

        #endregion

        #region IInterpreter implementation

        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            int curIndex;

            var entryToDeselect = (WindowListViewItemBase)anEntry;
            if (entryToDeselect != null)
            {
                curIndex = entryToDeselect.Index + 1;
            }
            else
            {
                curIndex = 0;
            }

            if (curIndex >= 0 && curIndex < Items.Count)
            {
                SelectedItems.Clear();
                Items[curIndex].Selected = true;
            }
        }

        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            int curIndex;

            var entryToDeselect = (WindowListViewItemBase)anEntry;
            if (entryToDeselect != null)
            {
                curIndex = entryToDeselect.Index - 1;
            }
            else
            {
                curIndex = Items.Count - 1;
            }

            if (curIndex >= 0 && curIndex < Items.Count)
            {
                SelectedItems.Clear();
                Items[curIndex].Selected = true;
            }
        }

        public virtual EntryPropertiesDialogBase GetPropertiesDialogFor(IEntry anEntry)
        {
            return new EntryPropertiesDialog(anEntry);
        }

        #endregion
    }
}

//if (!aCallEvent.Success)
//{
//    viewItem.ForeColor = Color.Red;
//}

//if (!CompareMode)
//{
//    viewItem.AddCall(aCallEvent);
//    viewItem.SubItems[4].Text = viewItem.Count.ToString(CultureInfo.InvariantCulture);
//    viewItem.SubItems[5].Text = string.Format("{0:N2}", viewItem.Time);
//}
//else
//{
//    if (File1TraceId == aCallEvent.TraceId)
//    {
//        viewItem.AddCall1(aCallEvent);
//    }
//    else if (File2TraceId == aCallEvent.TraceId)
//    {
//        viewItem.AddCall2(aCallEvent);
//    }
//    else
//    {
//        return;
//    }

//    if (viewItem.Count1 > 0 && viewItem.Count2 > 0)
//    {
//        viewItem.BackColor = BackColor;
//        viewItem.SubItems[4].Text = string.Format("{0} / {1}",
//                                              viewItem.Count1.ToString(CultureInfo.InvariantCulture),
//                                              viewItem.Count2.ToString(CultureInfo.InvariantCulture));
//        viewItem.SubItems[5].Text = string.Format("{0:N2} / {1:N2}", viewItem.Time1, viewItem.Time2);
//    }
//    else if (viewItem.Count1 > 0)
//    {
//        viewItem.BackColor = File1BackgroundColor;
//        viewItem.SubItems[4].Text = viewItem.Count1.ToString(CultureInfo.InvariantCulture);
//        viewItem.SubItems[5].Text = string.Format("{0:N2}", viewItem.Time1);
//    }
//    else
//    {
//        viewItem.BackColor = File2BackgroundColor;
//        viewItem.SubItems[4].Text = viewItem.Count2.ToString(CultureInfo.InvariantCulture);
//        viewItem.SubItems[5].Text = string.Format("{0:N2}", viewItem.Time2);
//    }
//}
