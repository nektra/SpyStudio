using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aga.Controls;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Main;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using System.Linq;

namespace SpyStudio.Registry.Controls
{
    public class RegistryTreeNode : RegistryTreeNodeBase
    {
        public virtual RegKeyInfo KeyInfo { get; set; }

        public override bool Success
        {
            get { return KeyInfo.Success; }
            set { throw new NotImplementedException(); }  // This should not be used. This setter exists only to guarantee compatibility with older code.
        }

        public override string Path
        {
            get { return KeyInfo.Path; }
        }
        public override string AlternatePath
        {
            get { return KeyInfo.AlternatePath; }
        }
        public override string NormalizedPath
        {
            get { return KeyInfo.NormalizedPath; }
        }
        protected ulong BasicKeyHandle
        {
            get { return KeyInfo.BasicKeyHandle; }
        }

        public override HashSet<CallEventId> CallEventIds
        {
            get
            {
                var childrenCallEvents = Nodes.Cast<RegistryTreeNode>().SelectMany(n => n.CallEventIds);
                var selfCallEvents = KeyInfo.GetAllCallEventIds();

                return new HashSet<CallEventId>(selfCallEvents.Concat(childrenCallEvents));
            }
        }
        
        public override SortedDictionary<string, RegistryTreeNodeBase> ChildrenByName { get; set; }

        public override bool IsNonCapture
        {
            get { return KeyInfo.IsNonCaptured; }
        }

        public override Dictionary<string, bool> OriginalPaths { get { return KeyInfo.OriginalKeyPaths; } }

        #region Initialization

        public static RegistryTreeNodeBase For(RegistryTree aTree)
        {
            var node = new RegistryTreeNode {Tree = aTree};


            return node;
        }

        public RegistryTreeNode(string aName) : base(aName)
        {
            KeyInfo = RegKeyInfo.For(this);
        }

        public RegistryTreeNode() : base("Unnamed")
        {
            KeyInfo = new RegKeyInfo();
        }

        #endregion

        #region UI

        public override void UpdateAppearance()
        {
            if (KeyInfo.IsNonCaptured)
                ForeColor = EntryColors.NonCaptured;
            else
                ForeColor = Success ? EntryColors.SimpleSuccess : EntryColors.SimpleError;
        }

        #endregion

        #region Values

        public override void Add(RegValueInfo aValueInfo)
        {
            KeyInfo.Add(aValueInfo);
        }

        public override void PopulateValueList(RegistryValueList valuesView, string aSelectedValue)
        {
            foreach (var valueInfo in KeyInfo.ValuesByName.Values)
            {
                var valueItem = RegistryValueItem.For(valueInfo);

                valuesView.Items.Add(valueItem);

                if (aSelectedValue != null && valueInfo.NormalizedName == aSelectedValue)
                    valueItem.Selected = true;

                valueItem.UpdateAppearance();
            }
        }

        public Dictionary<string, RegValueInfo> ValuesByName
        {
            get { return KeyInfo.ValuesByName; }
        }

        #endregion

        #region Profiling

        //static private double _timeInsert = 0;

        //public static void DumpTimes()
        //{
        //    Debug.WriteLine("TreeViewAdv insert:\t" + _timeInsert);
        //}

        #endregion

        #region Key Access

        public List<RegKeyInfo> GetCheckedKeys(ThinAppIsolationOption isolation)
        {
            if (Isolation == ThinAppIsolationOption.Inherit)
                Isolation = isolation;

            var regKeys = new List<RegKeyInfo>();

            if (Nodes.Count > 0)
            {
                foreach (var node in Nodes)
                {
                    var keyNode = (RegistryTreeNode)node;
                    regKeys.AddRange(keyNode.GetCheckedKeys(Isolation));
                }
            }
            if (IsChecked)
            {
                var thinAppKeyInfo = ThinAppRegKeyInfo.From(KeyInfo);
                thinAppKeyInfo.Isolation = isolation;
                regKeys.Add(thinAppKeyInfo);
            }

            return regKeys;
        }

        public List<RegKeyInfo> GetAccessedRegistryKeys(bool getEmptyKeys)
        {
            var regKeys = new List<RegKeyInfo>();

            if (Nodes.Count > 0)
            {
                // if the node has values add this item also
                if (KeyInfo.ValuesByName.Count > 0 || getEmptyKeys)
                    regKeys.Add(KeyInfo);

                foreach (var node in Nodes)
                {
                    var keyNode = (RegistryTreeNode)node;
                    regKeys.AddRange(keyNode.GetAccessedRegistryKeys(getEmptyKeys));
                }
            }
            else
            {
                regKeys.Add(KeyInfo);
            }

            return regKeys;
        }

        #endregion

        #region Merging

        public override void Merge(RegKeyInfo aKeyInfo)
        {
            KeyInfo.MergeWith(aKeyInfo);
        }

        private void MergeChild(RegKeyInfo aKeyInfo)
        {
            KeyInfo.MergeAsAncestorWith(aKeyInfo);
        }

        public override void PropagateInfoToRoot()
        {
            if (ParentIsRoot)
                return;

            PerformRecursively(node =>
                {
                    var regNode = ((RegistryTreeNode) node);
                    var parentRegNode = (RegistryTreeNode)node.Parent;

                    parentRegNode.MergeChild(regNode.KeyInfo);
                    parentRegNode.UpdateAppearance();

                    return !parentRegNode.ParentIsRoot;
                });
        }

        #endregion

        public override IEnumerable<string> ReduceOriginalPathsUntil(RegistryTreeNode aNode)
        {
            var finalResult = new HashSet<string>(OriginalPaths.Keys.Where(k => OriginalPaths[k]));

            Node ancestor = this;

            var partialResult = new List<string>();

            while (ancestor != aNode)
            {
                partialResult.Clear();                             

                foreach (var path in finalResult)
                    partialResult.Add(path.Substring(0, path.Length - ancestor.Text.Length - 1));

                finalResult.Clear();

                finalResult.AddRange(partialResult);
                ancestor = ancestor.Parent;
            }

            return finalResult;
        }

        public override IEnumerable<string> GetOriginalPathsFromBranch()
        {
            Debug.WriteLine("Getting original paths from a registry branch...");

            var originalPaths = new HashSet<string>();

            var capturedNodes = GetCapturedNodesRecursively();

            Debug.WriteLine("The captured nodes in the branch are:");
            foreach (var capturedNode in capturedNodes)
            {
                Debug.WriteLine(capturedNode.Path);
                originalPaths.AddRange(capturedNode.ReduceOriginalPathsUntil(this));
            }

            Debug.WriteLine("Original paths obtained: ");
#if DEBUG
            foreach (var originalPath in originalPaths)

                Debug.WriteLine(originalPath);
#endif

            return originalPaths;
        }

        public override IEnumerable<RegistryTreeNodeBase> GetCapturedNodesRecursively()
        {
            var capturedNodes = new List<RegistryTreeNodeBase>();

            if (!IsNonCapture)
                capturedNodes.Add(this);

            foreach (var child in ChildrenByName.Values)
                capturedNodes.AddRange(child.GetCapturedNodesRecursively());

            return capturedNodes;
        }

        public override void UpdateKeyInfo()
        {
            KeyInfo.Name = Text;
            KeyInfo.Path = GetPathFromParent();
            KeyInfo.NormalizedPath = ParentIsRoot ? Text.ToLower() : ((RegistryTreeNode)Parent).NormalizedPath + "\\" + Text.ToLower();
            KeyInfo.SubKey = ParentIsRoot ? "" : (((RegistryTreeNode) Parent).SubKey + "\\" + Text).TrimStart('\\');
            KeyInfo.BasicKeyHandle = ParentIsRoot ? 0 : ((RegistryTreeNode)Parent).BasicKeyHandle;
        }

        public override string FindValue(string startValue, string text, FindEventArgs e)
        {
            var started = (startValue == null);
            foreach (var valueInfo in (e.SearchDown ? KeyInfo.ValuesByName.Values : KeyInfo.ValuesByName.Values.Reverse()))
            {
                if (valueInfo.IsNull)
                    continue;

                if (started)
                {
                    if (StringHelpers.MatchString(valueInfo.Name, text, e) ||
                        StringHelpers.MatchString(valueInfo.Data, text, e))
                    {
                        return valueInfo.NormalizedName;
                    }
                }
                else if (valueInfo.NormalizedName == startValue)
                {
                    started = true;
                }
            }

            return null;
        }

        public override bool HasErrors
        {
            get { return !Success || ValuesByName.Values.Any(x => !x.Success); }
        }

        public override EntryPropertiesDialogBase GetPropertiesDialog()
        {
            return new EntryPropertiesDialog(this);
        }

        public override void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            foreach (var callEventId in CallEventIds)
                aTraceTreeView.InsertNode(callEventId);
        }

        public override void Accept(IEntryVisitor aVisitor)
        {
            aVisitor.Visit(this);
        }
    }
}