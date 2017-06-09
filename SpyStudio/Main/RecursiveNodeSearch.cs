using System;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.Dialogs;
using SpyStudio.Dialogs.Compare;
using System.Diagnostics;
using Aga.Controls.Tree;

namespace SpyStudio.Main
{
    public class RecursiveNodeSearch<T> where T : Node
    {
        public delegate bool SearchTerm(T ci);

        public static T FindNode(SearchTerm lambda, Node node)
        {
            foreach (T child in node.Nodes)
            {
                if (child.Nodes.Any())
                {
                    var ret = FindNode(lambda, child);
                    if (ret != null)
                        return ret;
                }
                if (lambda(child))
                    return child;
            }
            return null;
        }
        //public static TreeNodeAdv FindNode(SearchTerm lambda, Node node)
        //{
        //    foreach (var child in node.Nodes)
        //    {
        //        var tag = (T)child.Tag;
        //        if (child.Children.Any())
        //        {
        //            var ret = FindNode(lambda, child);
        //            if (ret != null)
        //                return ret;
        //        }
        //        if (lambda(tag))
        //            return child;
        //    }
        //    return null;
        //}

    }
}