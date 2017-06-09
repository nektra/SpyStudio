using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.Windows.Controls.Compare
{
    public class CompareWindowListViewItem : WindowListViewItemBase
    {
        protected uint Trace1ID { get { return ((ICompareInterpreter)ListView).Trace1ID; } }
        protected uint Trace2ID { get { return ((ICompareInterpreter)ListView).Trace2ID; } }

        public WindowInfo WindowInfo1 { get; protected set; }
        public WindowInfo WindowInfo2 { get; protected set; }

        public WindowInfo ValidInfo { get { return WindowInfo1.IsNull ? WindowInfo2 : WindowInfo1; } }

        protected bool IsUnmatched { get { return WindowInfo1.IsNull || WindowInfo2.IsNull; } }

        #region Instantiation

        protected CompareWindowListViewItem(string aName) : base(aName)
        {
            WindowInfo1 = new WindowInfo();
            WindowInfo2 = new WindowInfo();
        }

        public static WindowListViewItemBase Named(string aName)
        {
            return new CompareWindowListViewItem(aName);
        }

        #endregion

        #region Overrides of InterpreterListItem

        public override HashSet<CallEventId> CallEventIds
        {
            get { return new HashSet<CallEventId>(WindowInfo1.CallEventIds.Concat(WindowInfo2.CallEventIds)); }
        }

        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return new HashSet<DeviareTraceCompareItem>(WindowInfo1.CompareItems.Concat(WindowInfo2.CompareItems)); }
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryComparePropertiesDialog(this);
        }

        public override string NameForDisplay
        {
            get { return string.IsNullOrEmpty(ValidInfo.ClassName) ? ValidInfo.WindowName : ValidInfo.ClassName; }
        }

        public override bool Success
        {
            get { return WindowInfo1.Success || WindowInfo2.Success; }
        }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        #endregion

        #region Overrides of WindowListViewItemBase

        public override void UpdateAppearance()
        {
            if (IsUnmatched)
            {
                var baseInfo = ValidInfo;

                ClassName = baseInfo.ClassName;
                WindowName = baseInfo.WindowName;
                ModuleHandle = baseInfo.ModuleHandle;
                Result = baseInfo.Result;
                Count = baseInfo.Count.ToString(CultureInfo.InvariantCulture);
                Time = String.Format("{0:N2}", baseInfo.Time);

                ForeColor = Success ? Color.Black : Color.Red;

                BackColor = WindowInfo1.IsNull ? EntryColors.File2Color : EntryColors.File1Color;

                return;
            }

            if (WindowInfo1.Success == WindowInfo2.Success)
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

            ClassName = BuildDiffStringFrom(WindowInfo1.ClassName, WindowInfo2.ClassName);
            WindowName = BuildDiffStringFrom(WindowInfo1.WindowName, WindowInfo2.WindowName);
            ModuleHandle = BuildDiffStringFrom(WindowInfo1.ModuleHandle, WindowInfo2.ModuleHandle);
            Result = BuildDiffStringFrom(WindowInfo1.Result, WindowInfo2.Result);
            Count = WindowInfo1.Count.ToString(CultureInfo.InvariantCulture) + " / " + WindowInfo2.Count.ToString(CultureInfo.InvariantCulture);
            Time = String.Format("{0:N2}", WindowInfo1.Time) + " / " + String.Format("{0:N2}", WindowInfo2.Time);
        }

        public override void Merge(WindowInfo aComInfo)
        {
            GetComInfoInSameTraceAs(aComInfo).MergeWith(aComInfo);

            UpdateAppearance();
        }

        #endregion

        private WindowInfo GetComInfoInSameTraceAs(WindowInfo aComInfo)
        {
            if (aComInfo.TraceID == Trace1ID)
                return WindowInfo1;
            if (aComInfo.TraceID == Trace2ID)
                return WindowInfo2;

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
    }
}