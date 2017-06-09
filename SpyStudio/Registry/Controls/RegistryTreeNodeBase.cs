using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aga.Controls;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Dialogs.Properties;
using SpyStudio.Main;
using SpyStudio.Registry.Infos;

namespace SpyStudio.Registry.Controls
{
    public abstract class RegistryTreeNodeBase : InterpreterNode, IEntry
    {
        public string SubKey;
        public RegistryKeyAccess Access;
        public abstract SortedDictionary<string, RegistryTreeNodeBase> ChildrenByName { get; set; }
        public HashSet<uint> Pids = new HashSet<uint>();
        //public Dictionary<string, bool> OriginalKeys = new Dictionary<string, bool>();
        public string ResultString { get; set; }
        public string KeyString
        {
            get { return Text; }
        }

        protected RegistryTreeNodeBase(string aName) : base(aName)
        {
            ChildrenByName = new SortedDictionary<string, RegistryTreeNodeBase>();
        }

        #region Common interface

        public abstract bool IsNonCapture { get; }

        public abstract void UpdateAppearance();
        public abstract void Add(RegValueInfo aValueInfo);
        public abstract void PopulateValueList(RegistryValueList registryValueList, string aSelectedValue);
        public abstract string FindValue(string startValue, string text, FindEventArgs e);
        public abstract void Merge(RegKeyInfo aKeyInfo);
        public abstract Dictionary<string, bool> OriginalPaths { get; }

        #endregion

        public void AddChild(RegistryTreeNodeBase aRegistryTreeNode)
        {
            Debug.Assert(aRegistryTreeNode != null);
            Nodes.Add(aRegistryTreeNode);
            aRegistryTreeNode.UpdateKeyInfo();
            ChildrenByName.Add(aRegistryTreeNode.Text.ToLower(), aRegistryTreeNode);
        }

        public void PerformRecursively(Func<Node, bool> aFunction)
        {
            var continueRecursion = aFunction(this);

            if (continueRecursion && Parent is RegistryTreeNodeBase)
                ((RegistryTreeNodeBase)Parent).PerformRecursively(aFunction);
        }

        public abstract void PropagateInfoToRoot();

        public abstract void UpdateKeyInfo();

        #region Implementation of IEntry

        public IInterpreter Interpreter { get { return (IInterpreter) Tree; } }
        public virtual HashSet<DeviareTraceCompareItem> CompareItems { get; set; }

        public abstract bool HasErrors { get; }

        public virtual EntryPropertiesDialogBase GetPropertiesDialog()
        {
            // Subclass responsibility
            throw new NotImplementedException();
        }

        public bool SupportsGoTo { get { return Interpreter.SupportsGoTo; } }

        public override string NameForDisplay { get { return Path; } }

        #endregion

        public abstract IEnumerable<RegistryTreeNodeBase> GetCapturedNodesRecursively();
        public abstract IEnumerable<string> ReduceOriginalPathsUntil(RegistryTreeNode aNode);
        public abstract IEnumerable<string> GetOriginalPathsFromBranch();
    }
}