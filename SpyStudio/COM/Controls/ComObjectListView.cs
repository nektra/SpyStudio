using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.Extensions;
using System.Linq;

namespace SpyStudio.COM.Controls
{
    public class ComObjectListView : ListViewSorted, IInterpreter
    {
        public Control ParentControl
        {
            get { return this; }
        }
        public IInterpreterController Controller { get; set; }
        public IEnumerable<IEntry> SelectedEntries { get { return SelectedItems.Cast<IEntry>(); } }
        public bool SupportsGoTo { get { return Controller.PropertiesGoToVisible; } }

        #region Column headers

        private readonly ColumnHeader _columnHeaderClsid;
        private readonly ColumnHeader _columnHeaderObject;
        private readonly ColumnHeader _columnHeaderServer;
        private readonly ColumnHeader _columnHeaderClsidTime;
        private readonly ColumnHeader _columnHeaderClsidCount;
        private readonly ColumnHeader _columnHeaderClsidResult;

        #endregion

        #region Instantiation

        public ComObjectListView()
        {
            _columnHeaderClsid = new ColumnHeader();
            _columnHeaderObject = new ColumnHeader();
            _columnHeaderServer = new ColumnHeader();
            _columnHeaderClsidResult = new ColumnHeader();
            _columnHeaderClsidCount = new ColumnHeader();
            _columnHeaderClsidTime = new ColumnHeader();

            Columns.AddRange(new[] {
            _columnHeaderClsid,
            _columnHeaderObject,
            _columnHeaderServer,
            _columnHeaderClsidResult,
            _columnHeaderClsidCount,
            _columnHeaderClsidTime});

            View = View.Details;
            
            // 
            // columnHeaderClsid
            // 
            _columnHeaderClsid.Text = "Clsid";
            _columnHeaderClsid.Width = 208;
            // 
            // columnHeaderObject
            // 
            _columnHeaderObject.Text = "Object";
            _columnHeaderObject.Width = 198;
            // 
            // columnHeaderServer
            // 
            _columnHeaderServer.Text = "Server";
            _columnHeaderServer.Width = 168;
            // 
            // columnHeaderClsidResult
            // 
            _columnHeaderClsidResult.Text = "Result";
            _columnHeaderClsidResult.Width = 77;
            // 
            // columnHeaderClsidCount
            // 
            _columnHeaderClsidCount.Tag = "Numeric";
            _columnHeaderClsidCount.Text = "Count";
            // 
            // columnHeaderClsidTime
            // 
            _columnHeaderClsidTime.Tag = "Double";
            _columnHeaderClsidTime.Text = "Time";
            _columnHeaderClsidTime.TextAlign = HorizontalAlignment.Right;
        }

        public void InitializeComponent()
        {
            ShowItemToolTips = Properties.Settings.Default.ShowTooltip;

            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuController = new EntryContextMenu(this);
        }

        #endregion

        #region Control

        public void Attach(DeviareRunTrace devRunTrace)
        {
            devRunTrace.CoCreateAdd += Add;
            devRunTrace.UpdateBegin += (sender, args) => this.ExecuteInUIThreadAsynchronously(BeginUpdate);
            devRunTrace.UpdateEnd += (sender, args) => this.ExecuteInUIThreadAsynchronously(EndUpdate);
            devRunTrace.ComClear += ClearData;
        }

        public void Add(ComObjectInfo aComInfo)
        {
            ComObjectListViewItemBase comItem;
            if (!Items.ContainsKey(aComInfo.Clsid))
            {
                comItem = CreateNewItemFrom(aComInfo);

                Items.Add(comItem);

                comItem.Merge(aComInfo);
                
                return;
            }

            comItem = (ComObjectListViewItemBase)Items[aComInfo.Clsid];

            comItem.Merge(aComInfo);
        }

        public void ClearData()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(ClearData));
            }
            else
            {
                if (ContextMenuController != null)
                    ContextMenuController.Close(false);

                BeginUpdate();
                Items.Clear();
                EndUpdate();
            }
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
        public void SelectNextVisibleEntry(IEntry anEntry)
        {
            int curIndex;

            var entryToDeselect = anEntry as ComObjectListViewItemBase;
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
                EnsureVisible(curIndex);
            }
        }
        public void SelectPreviousVisibleEntry(IEntry anEntry)
        {
            int curIndex;

            var entryToDeselect = anEntry as ComObjectListViewItemBase;
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
                EnsureVisible(curIndex);
            }
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


        protected virtual ComObjectListViewItemBase CreateNewItemFrom(ComObjectInfo aComInfo)
        {
            return ComObjectListViewItem.From(aComInfo);
        }
    }
}