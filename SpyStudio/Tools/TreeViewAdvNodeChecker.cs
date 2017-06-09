using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Aga.Controls.Tree;
using SpyStudio.Export;
using SpyStudio.FileSystem;

namespace SpyStudio.Tools
{
    public abstract class TreeViewAdvNodeChecker<TreeType, TNodeType>
        where TreeType : TreeViewAdv
        where TNodeType : Node
    {
        #region Debug

        #if DEBUG

        protected StreamWriter Log;
        protected string NodeNameForLog;

        #endif

        #endregion

        #region Properties

        protected readonly List<CheckFunction<TNodeType>> NodeFunctions = new List<CheckFunction<TNodeType>>();
        protected readonly List<CheckFunction<TNodeType>> SecondPassNodeFunctions = new List<CheckFunction<TNodeType>>();

        #endregion

        #region Instantiation

        protected void SpecializeFor(CheckerType aCheckerType)
        {
            switch (aCheckerType)
            {
                case CheckerType.Application:
                    InitializeForApplication();
                    break;

                case CheckerType.Installer:
                    InitializeForInstallation();
                    break;

                case CheckerType.Update:
                    InitializeForAutoUpdate();
                    break;

                case CheckerType.None:
                    break;

                default:
                    throw new Exception("Unknown CaptureType");
            }
        }

        protected abstract void InitializeForAutoUpdate();
        protected abstract void InitializeForInstallation();
        protected abstract void InitializeForApplication();

        #endregion

        #region Checking for Tree Mode

        public virtual void PerformCheckingOn(TreeType aTree)
        {
            var allNodes = aTree.AllModelNodes.Cast<TNodeType>();
            //IEnumerable<TNodeType> notCheckeable;

            var checkableNodes = new List<TNodeType>();
            var allowabilities = GetAllowabilities(allNodes);

            foreach (var allowabilityResult in allowabilities)
            {
                var node = allowabilityResult.Node;
                switch (allowabilityResult.Allowability)
                {
                    case Allowability.Checkable:
                        checkableNodes.Add(node);
                        break;
                    case Allowability.NotAllowed:
                        node.Parent.Nodes.Remove(node);
                        break;
                }
            }

// ReSharper disable PossibleMultipleEnumeration
            ApplyFunctions(checkableNodes, NodeFunctions.Where(f => f.Type != CheckFunctionType.AllowabilityFunction));
            ApplyFunctions(checkableNodes, SecondPassNodeFunctions);
// ReSharper restore PossibleMultipleEnumeration

            aTree.UpdateControl(false, false);
        }

        void ApplyFunctions(IEnumerable<TNodeType> checkableNodes, IEnumerable<CheckFunction<TNodeType>> checkUncheckFunctions)
        {
#if DEBUG
            var checkerTimes = new Dictionary<string, double>();
#endif

            foreach (var node in checkableNodes)
            {
// ReSharper disable PossibleMultipleEnumeration
                foreach (var checkFunction in checkUncheckFunctions)
// ReSharper restore PossibleMultipleEnumeration
                {
#if DEBUG
                    var sw = new Stopwatch();
                    sw.Start();
#endif
                    var retValue = checkFunction.Function(node);
#if DEBUG
                    if ((!checkerTimes.ContainsKey(checkFunction.Function.Method.Name)))
                        checkerTimes[checkFunction.Function.Method.Name] = 0;
                    checkerTimes[checkFunction.Function.Method.Name] += sw.Elapsed.TotalMilliseconds;
#endif
                    if (retValue)
                    {
#if DEBUG
                        var nodeText = node.Text;
                        var fsNode = node as FileSystemTreeNode;
                        if (fsNode != null)
                            nodeText = fsNode.FilePath;

                        Log.Write(NodeNameForLog + " " + nodeText + ": " +
                                  (checkFunction.Type == CheckFunctionType.CheckFunction
                                       ? "Checked by "
                                       : "Unchecked by ") + checkFunction.Function.Method.Name +
                                  Environment.NewLine);
#endif
                    }
                }
            }

#if DEBUG
            Log.Flush();
            Debug.WriteLine("Checker / Unchecker Times:");
            foreach (var checkTime in checkerTimes)
            {
                Debug.WriteLine(checkTime.Key + ": " + checkTime.Value);
            }
#endif
        }

        private IEnumerable<AllowabilityResult<TNodeType>> GetAllowabilities(IEnumerable<TNodeType> allNodesAsFileSystemNodes)
        {
#if DEBUG
            var allowableTimes = new Dictionary<string, double>();
#endif
            var notAllowed = new HashSet<Node>();
            var ret = new List<AllowabilityResult<TNodeType>>();

            foreach (var node in allNodesAsFileSystemNodes)
            {
                if (notAllowed.Contains(node.Parent))
                {
                    notAllowed.Add(node);
                    continue;
                }

                Func<TNodeType, bool> failedAllowabilityRule = null;

                var nodeAllowabilityRules = NodeFunctions.Where(f => f.Type == CheckFunctionType.AllowabilityFunction);
                var allow = Allowability.Max;
                foreach (var nodeAllowabilityRule in nodeAllowabilityRules)
                {
                    var afunction = nodeAllowabilityRule as AllowabilityFunction<TNodeType>;
                    Debug.Assert(afunction != null);
#if DEBUG
                    var sw = new Stopwatch();
                    sw.Start();
#endif
                    var allowability = afunction.AllowFunction(node);
                    if (allowability < allow)
                        allow = allowability;
#if DEBUG
                    if ((!allowableTimes.ContainsKey(nodeAllowabilityRule.Function.Method.Name)))
                        allowableTimes[nodeAllowabilityRule.Function.Method.Name] = 0;
                    allowableTimes[nodeAllowabilityRule.Function.Method.Name] += sw.Elapsed.TotalMilliseconds;
#endif
                    if (allowability == Allowability.Min)
                    {
                        notAllowed.Add(node);
                        failedAllowabilityRule = nodeAllowabilityRule.Function;
                        break;
                    }
                }

                ret.Add(new AllowabilityResult<TNodeType>(node, allow));

#if DEBUG
                if (failedAllowabilityRule != null)
                {
                    var nodeText = node.Text;
                    var fsNode = node as FileSystemTreeNode;
                    if (fsNode != null)
                        nodeText = fsNode.FilePath;
                    Log.WriteLine(NodeNameForLog + " " + nodeText + " was discarded by allowability rule: " + failedAllowabilityRule.Method.Name);
                }
#endif
            }

#if DEBUG
            Log.Flush();
            Debug.WriteLine("Allowability Times:");
            foreach (var checkTime in allowableTimes)
            {
                Debug.WriteLine(checkTime.Key + ": " + checkTime.Value);
            }
#endif

            return ret;
        }

        #endregion
    }
}
