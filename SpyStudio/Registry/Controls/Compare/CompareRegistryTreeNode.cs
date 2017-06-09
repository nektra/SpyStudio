using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Aga.Controls;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Extensions;
using SpyStudio.Main;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.Registry.Controls.Compare
{
    public class CompareRegistryTreeNode : RegistryTreeNodeBase
    {
        public uint Trace1ID { get { return ((RegistryTree)Tree).File1TraceId; } }
        public uint Trace2ID { get { return ((RegistryTree)Tree).File2TraceId; } }

        protected RegKeyInfo KeyInfo1;
        protected RegKeyInfo KeyInfo2;

        public override HashSet<CallEventId> CallEventIds 
        {
            get
            {
                var childrenCallEvents = Nodes.Cast<CompareRegistryTreeNode>().SelectMany(n => n.CallEventIds);
                var selfCallEvents = KeyInfo1.GetAllCallEventIds().Concat(KeyInfo2.GetAllCallEventIds());

                return new HashSet<CallEventId>(selfCallEvents.Concat(childrenCallEvents));
            } 
        }
        public override HashSet<DeviareTraceCompareItem> CompareItems
        {
            get
            {
                var childrenCompareItems = Nodes.Cast<CompareRegistryTreeNode>().SelectMany(n => n.CompareItems);
                var selfCompareItems = KeyInfo1.GetAllCompareItems().Concat(KeyInfo2.GetAllCompareItems());

                return new HashSet<DeviareTraceCompareItem>(childrenCompareItems.Concat(selfCompareItems));
            }
        }

        public override SortedDictionary<string, RegistryTreeNodeBase> ChildrenByName { get; set; }

        public override bool Success
        {
            get { return IsUnmatched ? KeyInfo1.Success : KeyInfo1.Success || KeyInfo2.Success; }
            set { throw new NotImplementedException(); }
        }
        public bool HardSuccess
        {
            get { return IsUnmatched ? KeyInfo1.Success : KeyInfo1.Success && KeyInfo2.Success; }
            set { throw new NotImplementedException(); }
        }
        public override bool IsDifference
        {
            get { return Bold || IsUnmatched; }
        }
        public override string Path
        {
            get { return ValidInfo.Path; }
        }
        public override string NormalizedPath
        {
            get { return ValidInfo.NormalizedPath; }
        }
        public override bool IsNonCapture
        {
            get { return KeyInfo1.IsNonCaptured && KeyInfo2.IsNonCaptured; }
        }

        protected RegKeyInfo ValidInfo { get { return KeyInfo1.IsNull ? KeyInfo2 : KeyInfo1; } }
        protected bool IsUnmatched { get { return KeyInfo1.IsNull || KeyInfo2.IsNull; } }

        #region Initialization

        private CompareRegistryTreeNode(RegistryTree aTree) : base("Unnamed")
        {
            Tree = aTree;

            KeyInfo1 = RegKeyInfo.ForTraceID(Trace1ID);
            KeyInfo2 = RegKeyInfo.ForTraceID(Trace2ID);
        }

        public static CompareRegistryTreeNode For(CompareRegistryTree aTree)
        {
            var node = new CompareRegistryTreeNode(aTree);

            return node;
        }

        public void InitializeUsing(RegKeyInfo aKeyInfo)
        {
            Text = aKeyInfo.Name;

            if (aKeyInfo.TraceID == Trace1ID)
                KeyInfo1 = aKeyInfo;
            else if (aKeyInfo.TraceID == Trace2ID)
                KeyInfo2 = aKeyInfo;
            else
                throw new Exception("Unknown trace ID");
        }

        #endregion

        #region Registry values

        private IEnumerable<RegValueInfo> GetAllValueInfos()
        {
            return KeyInfo1.ValuesByName.Values.Concat(KeyInfo2.ValuesByName.Values);
        }

        public override void Add(RegValueInfo aValueInfo)
        {
            GetKeyInfoInSameTraceAs(aValueInfo).Add(aValueInfo);

            UpdateAppearance();
        }

        public override void PopulateValueList(RegistryValueList aValuesView, string aSelectedValue)
        {
            var allValueInfos = GetAllValueInfos();

            foreach (var valueInfo in allValueInfos)
            {
                var valueItem =
                    aValuesView.Items.Cast<CompareRegistryValueItem>().FirstOrDefault(i => i.Name.EqualsIgnoringCase(valueInfo.Name));

                if (valueItem == null)
                {
                    valueItem = (CompareRegistryValueItem)aValuesView.Items.Add(CompareRegistryValueItem.Named(valueInfo.Name));
                    valueItem.InitializeUsing(valueInfo);
                }

                valueItem.Merge(valueInfo);
            }
        }

        private bool AnyValueDiffers()
        {
            foreach (var valueName in GetAllValueInfos().Select(valueInfo => valueInfo.NormalizedName).Distinct())
            {
                RegValueInfo value1;
                RegValueInfo value2;
                if (!KeyInfo1.ValuesByName.TryGetValue(valueName, out value1) || !KeyInfo2.ValuesByName.TryGetValue(valueName, out value2))
                    return true;

                if (value1.ValueType != value2.ValueType || value1.Data != value2.Data)
                    return true;
            }

            return false;
        }

        #endregion

        #region Merging

        public override void Merge(RegKeyInfo aKeyInfo)
        {
            GetKeyInfoInSameTraceAs(aKeyInfo).MergeWith(aKeyInfo);
        }

        private void MergeChild(RegKeyInfo aKeyInfo)
        {
            GetKeyInfoInSameTraceAs(aKeyInfo).MergeAsAncestorWith(aKeyInfo);
        }

        public override void PropagateInfoToRoot()
        {
            if (ParentIsRoot)
                return;

            PerformRecursively(node =>
                {
                    var compareNode = (CompareRegistryTreeNode) node;
                    var compareParentNode = (CompareRegistryTreeNode) node.Parent;

                    if (!KeyInfo1.IsNull)
                        compareParentNode.MergeChild(compareNode.KeyInfo1);

                    if (!KeyInfo2.IsNull)
                        compareParentNode.MergeChild(compareNode.KeyInfo2);

                    compareParentNode.UpdateAppearance();

                    return !compareParentNode.ParentIsRoot;
                });
        }

        public override void UpdateKeyInfo()
        {
            if (string.IsNullOrEmpty(KeyInfo1.Path))
            {
                KeyInfo1.Path = GetPathFromParent();
                KeyInfo1.Name = KeyInfo1.Path.Substring(KeyInfo1.Path.LastIndexOf("\\", StringComparison.InvariantCulture) + 1);
            }

            if (string.IsNullOrEmpty(KeyInfo2.Path))
            {
                KeyInfo2.Path = GetPathFromParent();
                KeyInfo2.Name = KeyInfo2.Path.Substring(KeyInfo2.Path.LastIndexOf("\\", StringComparison.InvariantCulture) + 1);
            }
                
        }

        #endregion

        #region UI

        public override void UpdateAppearance()
        {
            if (IsUnmatched)
            {
                ForeColor = ValidInfo.Success ? EntryColors.NoMatchSuccessColor : EntryColors.NoMatchErrorColor;
                Brush = new SolidBrush(ValidInfo.TraceID == Trace1ID ? EntryColors.File1Color : EntryColors.File2Color);
                Bold = false;

                ResultString = ValidInfo.GetResultString();

                return;
            }

            Brush = new SolidBrush(Tree.BackColor);

            if (KeyInfo1.Success == KeyInfo2.Success)
            {
                ForeColor = KeyInfo1.Success
                                ? EntryColors.MatchSuccessColor
                                : EntryColors.MatchErrorColor;

                ResultString = KeyInfo1.GetResultString();

                Bold = AnyValueDiffers();

                IsDifference = Bold;
            }
            else
            {
                ForeColor = EntryColors.MatchResultMismatchColor;
                Bold = true;
                ResultString = KeyInfo1.GetResultString() + "/" + KeyInfo2.GetResultString();
                IsDifference = true;
            }
        }

        #endregion

        public override void Accept(IEntryVisitor aVisitor)
        {
            Debug.Assert(false);
            throw new NotImplementedException();
        }

        public override Dictionary<string, bool> OriginalPaths { get { throw new NotImplementedException(); } }

        protected RegKeyInfo GetKeyInfoInSameTraceAs(IInfo aKeyInfo)
        {
            if (aKeyInfo.TraceID == Trace1ID)
                return KeyInfo1;

            if (aKeyInfo.TraceID == Trace2ID)
                return KeyInfo2;

            throw new Exception("Unknown Trace ID");
        }

        public override string FindValue(string startValue, string text, FindEventArgs e)
        {
            var started = (startValue == null);
            foreach (var valueInfo in (e.SearchDown ? GetAllValueInfos() : GetAllValueInfos().Reverse()))
            {
                if (valueInfo.IsNull)
                    continue;

                if (started)
                {
                    var valueName = valueInfo.Name;

                    if (StringHelpers.MatchString(valueName, text, e) ||
                        StringHelpers.MatchString(valueInfo.Data, text, e))
                    {
                        return valueInfo.NormalizedPath + "\\" + valueInfo.NormalizedName;
                    }
                }
                else if (valueInfo.NormalizedPath + "\\" + valueInfo.NormalizedName == startValue)
                {
                    started = true;
                }
            }

            return null;
        }

        public override bool HasErrors
        {
            get { return !HardSuccess || GetAllValueInfos().Any(x => !x.Success); }
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryComparePropertiesDialog(this);
        }

        public override IEnumerable<RegistryTreeNodeBase> GetCapturedNodesRecursively()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> ReduceOriginalPathsUntil(RegistryTreeNode aNode)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetOriginalPathsFromBranch()
        {
            throw new NotImplementedException();
        }
    }
}