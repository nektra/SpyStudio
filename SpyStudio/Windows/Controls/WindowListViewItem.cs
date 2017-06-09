using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Tools;

namespace SpyStudio.Windows.Controls
{
    public class WindowListViewItem : WindowListViewItemBase
    {
        public WindowInfo WindowInfo { get; set; }

        #region Instantiation

        public static WindowListViewItemBase Named(string aName)
        {
            return new WindowListViewItem(aName);
        }

        public WindowListViewItem(string aName)
            : base(aName)
        {
            WindowInfo = new WindowInfo();
        }

        #endregion

        #region InterpreterListItem implementation

        public override string NameForDisplay
        {
            get { return !String.IsNullOrEmpty(WindowInfo.ClassName) ? WindowInfo.ClassName : WindowInfo.WindowName; }
        }

        public override bool Success
        {
            get { return WindowInfo.Success; }
        }

        public override HashSet<CallEventId> CallEventIds
        {
            get { return WindowInfo.CallEventIds; }
        }

        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { Debug.Assert(false, "Tried to get compare items from a non-compare entry."); return new HashSet<DeviareTraceCompareItem>(); }
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryPropertiesDialog(this);
        }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        #endregion

        #region Overrides of WindowListViewItemBase

        public override void UpdateAppearance()
        {
            ClassName = WindowInfo.ClassName;
            WindowName = WindowInfo.WindowName;
            ModuleHandle = WindowInfo.ModuleHandle;
            Result = WindowInfo.Result;
            Count = WindowInfo.Count.ToString(CultureInfo.InvariantCulture);
            Time = String.Format("{0:N2}", WindowInfo.Time);

            ForeColor = Success ? Color.Black : Color.Red;
        }

        public override void Merge(WindowInfo aWindowInfo)
        {
            WindowInfo.MergeWith(aWindowInfo);

            UpdateAppearance();
        }

        #endregion
    }
}