using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Tools;
using System.Linq;
using SpyStudio.Extensions;
using SpyStudio.Trace;

namespace SpyStudio.COM.Controls.Compare
{
    public class CompareComObjectListViewItem : ComObjectListViewItemBase
    {
        protected uint Trace1ID { get { return ((ICompareInterpreter)ListView).Trace1ID; } }
        protected uint Trace2ID { get { return ((ICompareInterpreter)ListView).Trace2ID; } }

        protected ComObjectInfo ComInfo1 { get; set; }
        protected ComObjectInfo ComInfo2 { get; set; }

        protected ComObjectInfo ValidComInfo { get { return ComInfo1.IsNull ? ComInfo2 : ComInfo1; } }
        protected bool IsUnmatched { get { return ComInfo1.IsNull || ComInfo2.IsNull; } }

        #region Instatiation

        public static CompareComObjectListViewItem From(ComObjectInfo aComInfo)
        {
            return new CompareComObjectListViewItem(aComInfo.Clsid);
        }

        protected CompareComObjectListViewItem(string aName) : base(aName)
        {
            ComInfo1 = new ComObjectInfo();
            ComInfo2 = new ComObjectInfo();
        }

        #endregion

        #region Overrides of ComObjectListViewItemBase

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryComparePropertiesDialog(this);
        }

        public override string NameForDisplay
        {
            get { return string.IsNullOrEmpty(ValidComInfo.Description) ? ValidComInfo.Clsid : ValidComInfo.Description; }
        }

        public override bool Success
        {
            get { return ComInfo1.Success || ComInfo2.Success; }
        }

        public override HashSet<CallEventId> CallEventIds
        {
            get { return new HashSet<CallEventId>(ComInfo1.CallEventIds.Concat(ComInfo2.CallEventIds)); }
        }

        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return new HashSet<DeviareTraceCompareItem>(ComInfo1.CompareItems.Concat(ComInfo2.CompareItems)); }
        }

        public override void UpdateAppearance()
        {
            if (IsUnmatched)
            {
                var baseInfo = ValidComInfo;

                Description = baseInfo.Description;
                ServerPath = baseInfo.ServerPath;
                Result = baseInfo.Result;
                Count = baseInfo.Count.ToString(CultureInfo.InvariantCulture);
                Time = String.Format("{0:N2}", baseInfo.Time);

                if (Properties.Settings.Default.ShowTooltip)
                    UpdateTooltip();

                ForeColor = Success ? Color.Black : Color.Red;
                BackColor = ComInfo1.IsNull ? EntryColors.File2Color : EntryColors.File1Color;

                return;
            }

            if (ComInfo1.Success == ComInfo2.Success)
            {
                ForeColor = Success
                                ? EntryColors.MatchSuccessColor
                                : EntryColors.MatchErrorColor;

                ForeColor = Success ? Color.Black : Color.Red;
                Font = new Font(Font, FontStyle.Regular);
            }
            else
            {
                ForeColor = EntryColors.MatchResultMismatchColor;
                Font = new Font(Font, FontStyle.Bold);
            }

            BackColor = ListView.BackColor;

            Description = BuildDiffStringFrom(ComInfo1.Description, ComInfo2.Description);
            ServerPath = BuildDiffStringFrom(ComInfo1.ServerPath, ComInfo2.ServerPath);
            Result = BuildDiffStringFrom(ComInfo1.Result, ComInfo2.Result);
            Count = ComInfo1.Count.ToString(CultureInfo.InvariantCulture) + " / " + ComInfo2.Count.ToString(CultureInfo.InvariantCulture);
            Time = String.Format("{0:N2}", ComInfo1.Time) + "/" + String.Format("{0:N2}", ComInfo2.Time);
        }

        public override void Merge(ComObjectInfo aComInfo)
        {
            GetComInfoInSameTraceAs(aComInfo).MergeWith(aComInfo);

            UpdateAppearance();
        }

        #endregion

        private ComObjectInfo GetComInfoInSameTraceAs(ComObjectInfo aComInfo)
        {
            if (aComInfo.TraceID == Trace1ID)
                return ComInfo1;
            if (aComInfo.TraceID == Trace2ID)
                return ComInfo2;

            Debug.Assert(false, "Tried to merge a ComObjectInfo from unknown trace.");

            return null;
        }

        private string BuildDiffStringFrom(string aString, string anotherString)
        {
            if (string.IsNullOrEmpty(anotherString) && !string.IsNullOrEmpty(aString))
                return aString + " / <empty>";

            if (string.IsNullOrEmpty(aString) && !string.IsNullOrEmpty(anotherString))
                return "<empty> / " + anotherString;

            if (aString.EqualsIgnoringCase(anotherString))
                return aString;

            return aString + " / " + anotherString;
        }

        private void UpdateTooltip()
        {
            if (Properties.Settings.Default.MaxTooltipModules == 0)
            {
                ToolTipText = string.Empty;
                return;
            }

            var tooltip = "Called from:";

            foreach (var callerModule in ComInfo1.CallerModules.Concat(ComInfo2.CallerModules).Take(Properties.Settings.Default.MaxTooltipModules))
                tooltip += "\n" + ModulePath.ExtractModuleName(callerModule);

            ToolTipText = tooltip;
        }
    }
}