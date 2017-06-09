using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.COM.Controls
{
    public class ComObjectListViewItem : ComObjectListViewItemBase
    {
        public ComObjectInfo ComInfo { get; protected set; }

        #region Instantiation

        public static ComObjectListViewItem From(ComObjectInfo aComInfo)
        {
            return new ComObjectListViewItem(aComInfo.Clsid);
        }

        protected ComObjectListViewItem(string aName)
            : base(aName)
        {
            ComInfo = new ComObjectInfo();
        }

        #endregion

        #region Overrides of ComObjectListViewItemBase

        public override string NameForDisplay
        {
            get { { return string.IsNullOrEmpty(ComInfo.Description) ? ComInfo.Clsid : ComInfo.Description; } }
        }

        public override bool Success
        {
            get { return ComInfo.Success; }
        }

        public override void UpdateAppearance()
        {
            Description = ComInfo.Description;
            ServerPath = ComInfo.ServerPath;
            Result = ComInfo.Result;
            Count = ComInfo.Count.ToString(CultureInfo.InvariantCulture);
            Time = String.Format("{0:N2}", ComInfo.Time);

            ForeColor = Success ? Color.Black : Color.Red;

            if (Properties.Settings.Default.ShowTooltip)
                UpdateTooltip();
        }

        public override void Merge(ComObjectInfo aComInfo)
        {
            ComInfo.MergeWith(aComInfo);

            UpdateAppearance();
        }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        public override HashSet<CallEventId> CallEventIds
        {
            get { return ComInfo.CallEventIds; }
        }

        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { Debug.Assert(false, "Tried to get compare items from a non-compare entry."); return new HashSet<DeviareTraceCompareItem>();}
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryPropertiesDialog(this);
        }

        #endregion

        protected void UpdateTooltip()
        {
            if (Properties.Settings.Default.MaxTooltipModules == 0)
            {
                ToolTipText = string.Empty;
                return;
            }

            var tooltip = "Called from:";

            foreach (var callerModule in ComInfo.CallerModules.Take(Properties.Settings.Default.MaxTooltipModules))
                tooltip += "\n" + ModulePath.ExtractModuleName(callerModule);

            ToolTipText = tooltip;
        }
    }
}