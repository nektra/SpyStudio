using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Aga.Controls.Tree;
using SpyStudio.ContextMenu;
using SpyStudio.Export.Templates;
using SpyStudio.Export.ThinApp;
using SpyStudio.Tools;

namespace SpyStudio.Main
{
    public abstract class InterpreterNode : Node
    {
        private ThinAppIsolationOption _isolation;

        public virtual ThinAppIsolationOption Isolation
        {
            get { return _isolation; }
            set { _isolation = value; }
        }

        public abstract bool Success { get; set; }
        public TreeViewAdv Tree;
        public virtual bool IsDifference { get; protected set; }

        public override bool IsLeaf
        {
            get { return (Nodes.Count == 0); }
        }

        public abstract string Path { get; }
        //public abstract string SystemPath { get; }

        public virtual string AlternatePath
        {
            get { return null; }
        }

        public abstract string NormalizedPath { get; }

        public abstract HashSet<CallEventId> CallEventIds { get; }

        public virtual IEntry NextVisibleEntry
        {
            get { return (IEntry)Tree.GetNextVisibleNode(this); }
        }

        public virtual IEntry PreviousVisibleEntry
        {
            get { return (IEntry) Tree.GetPreviousVisibleNode(this); }
        }

        public virtual string NameForDisplay { get; protected set; }

        public bool IsImported;

        protected InterpreterNode(string aName) : base(aName)
        {
            
        }

        public abstract void Accept(IEntryVisitor aVisitor);

        public virtual void AddCallEventsTo(TraceTreeView aTraceTreeView)
        {
            foreach (var eventId in CallEventIds)
                aTraceTreeView.InsertNode(eventId);
        }
    }
}