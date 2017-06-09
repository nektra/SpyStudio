using System.Collections.Generic;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Extensions;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Registry.Controls
{

    public class RegistryValueItem : RegistryValueItemBase
    {
        #region Instantiation

        public static RegistryValueItem For(RegValueInfo aValueInfo)
        {
            var valueItem = new RegistryValueItem();

            valueItem.InitializeUsing(aValueInfo);

            return valueItem;
        }

        protected void InitializeUsing(RegValueInfo aValueInfo)
        {
            ValueInfo = aValueInfo;

            Name = Text = string.IsNullOrEmpty(aValueInfo.Name) ? "(Default)" : aValueInfo.Name;
            Path = aValueInfo.Path + "\\" + Name;
            SubItems.Add(RegistryTools.GetValueTypeString(aValueInfo.ValueType));
            SubItems.Add(aValueInfo.Success || aValueInfo.IsNonCaptured ? aValueInfo.Data : string.Empty);
            Tag = aValueInfo.Name;
            CallEventIds.AddRange(aValueInfo.CallEventIds);
            CompareItems.AddRange(aValueInfo.CompareItems);
        }

        #endregion

        protected RegValueInfo ValueInfo;

        public override HashSet<CallEventId> CallEventIds
        {
            get { return ValueInfo.CallEventIds; }
        }

        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return ValueInfo.CompareItems; }
        }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryPropertiesDialog(this);
        }

        public override bool Success { get { return ValueInfo.Success; } }

        public override void UpdateAppearance()
        {
            ForeColor = ValueInfo.IsNonCaptured
                            ? EntryColors.NonCaptured
                            : (ValueInfo.Success ? EntryColors.SimpleSuccess : EntryColors.SimpleError);
        }
    }
}