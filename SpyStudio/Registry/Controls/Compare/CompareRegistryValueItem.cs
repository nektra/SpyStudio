using System;
using System.Collections.Generic;
using System.Drawing;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Extensions;
using System.Linq;

namespace SpyStudio.Registry.Controls.Compare
{
    public class CompareRegistryValueItem : RegistryValueItemBase
    {
        public uint Trace1ID { get { return List.File1TraceId; } }
        public uint Trace2ID { get { return List.File2TraceId; } }

        protected RegValueInfo ValueInfo1;
        protected RegValueInfo ValueInfo2;

        private CompareRegistryValueItem(string aName)
        {
            Name = aName;
            Text = string.IsNullOrEmpty(aName) ? "(Default)" : aName;
        }

        protected RegValueInfo ValidInfo
        {
            get { return ValueInfo2.IsNull ? ValueInfo1 : ValueInfo2; }
        }

        protected string ValueType
        {
            get
            {
                if (IsUnmatched)
                    return ValidInfo.ValueType.AsSpyStudioString();

                return ValueInfo1.ValueType == ValueInfo2.ValueType ? ValueInfo1.ValueType.AsSpyStudioString() : ValueInfo1.ValueType.AsSpyStudioString() + " / " + ValueInfo2.ValueType.AsSpyStudioString();
            }
        }

        protected string ValueData
        {
            get
            {
                if (IsUnmatched)
                    return ValidInfo.Data;

                return ValueInfo1.Data == ValueInfo2.Data ? ValueInfo1.Data : ValueInfo1.Data.ForCompareString() + " / " + ValueInfo2.Data.ForCompareString();
            }
        }

        public override HashSet<CallEventId> CallEventIds
        {
            get { return new HashSet<CallEventId>(ValueInfo1.CallEventIds.Concat(ValueInfo2.CallEventIds)); }
        }

        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return new HashSet<DeviareTraceCompareItem>(ValueInfo1.CompareItems.Concat(ValueInfo2.CompareItems)); }
        }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryComparePropertiesDialog(this);
        }

        public override bool Success { get { return ValueInfo1.Success && ValueInfo2.Success; } }

        public override void UpdateAppearance()
        {
            if (IsUnmatched)
            {
                BackColor = ValidInfo.TraceID == List.File1TraceId ? EntryColors.File1Color : EntryColors.File2Color;
                ForeColor = ValidInfo.Success ? EntryColors.NoMatchSuccessColor : EntryColors.NoMatchErrorColor;
                Font = new Font(Font, FontStyle.Regular);
            }
            else
            {
                BackColor = List.BackColor;

                if (ValueInfo1.Success == ValueInfo2.Success)
                {
                    ForeColor = ValueInfo1.Success ? EntryColors.MatchSuccessColor : EntryColors.MatchErrorColor;
                }
                else
                {
                    ForeColor = EntryColors.MatchResultMismatchColor;
                }

                Font = ValueInfo1.Data != ValueInfo2.Data
                               ? new Font(Font, FontStyle.Bold)
                               : new Font(Font, FontStyle.Regular);
            }
        }

        protected bool IsUnmatched
        {
            get { return ValueInfo1.IsNull || ValueInfo2.IsNull; }
        }

        public void InitializeUsing(RegValueInfo aValueInfo)
        {
            Name = aValueInfo.Name;
            Text = string.IsNullOrEmpty(aValueInfo.Name) ? "(Default)" : aValueInfo.Name;

            SubItems.Add("Null");
            SubItems.Add("Null");

            ValueInfo1 = RegValueInfo.ForTraceID(Trace1ID);
            ValueInfo2 = RegValueInfo.ForTraceID(Trace2ID);

            DataDiffers = false;
            Path = aValueInfo.NormalizedPath;
        }

        public void Merge(RegValueInfo aValueInfo)
        {
            var valueInfo = ValueInfoWithSameTraceIdAs(aValueInfo);

            if (!valueInfo.IsNull)
            {
                valueInfo.MergeWith(aValueInfo);
                SubItems[1].Text = ValueType;
                SubItems[2].Text = ValueData;
                UpdateAppearance();
                return;
            }

            if (aValueInfo.TraceID == Trace1ID)
                ValueInfo1 = aValueInfo;
            else
                ValueInfo2 = aValueInfo;

            SubItems[1].Text = ValueType;
            SubItems[2].Text = ValueData;

            UpdateAppearance();
        }

        private RegValueInfo ValueInfoWithSameTraceIdAs(RegValueInfo aValueInfo)
        {
            if (aValueInfo.TraceID == Trace1ID)
                return ValueInfo1;

            if (aValueInfo.TraceID == Trace2ID)
                return ValueInfo2;

            throw new Exception("Unknown Trace ID.");
        }

        public static CompareRegistryValueItem Named(string aName)
        {
            return new CompareRegistryValueItem(aName);
        }
    }
}