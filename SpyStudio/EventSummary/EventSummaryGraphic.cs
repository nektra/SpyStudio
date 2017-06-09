using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SpyStudio.ContextMenu;
using SpyStudio.Extensions;

namespace SpyStudio.EventSummary
{
    public class EventSummaryTooltipArgs
    {
        public EventSummaryTooltipArgs(List<ITraceEntry> entries)
        {
            Entries = entries;
        }

        public List<ITraceEntry> Entries { get; private set; }
        public string EntriesText { get; set; }
    }

    public class EventSummaryEntryArgs
    {
        public EventSummaryEntryArgs(ITraceEntry entry)
        {
            Entry = entry;
        }

        public ITraceEntry Entry { get; private set; }
    }

    public partial class EventSummaryGraphic : Panel
    {
        private Point _mouseLocation;
        private readonly ToolTip _toolTip;

        private readonly EventSummaryMgr _eventSummaryMgr = new EventSummaryMgr();

        private bool _inToolTipPopup;
        private int _tooltipY = -1;
        private bool _shutdown;

        // Event triggered when the user keeps the mouse for more than one second. The controller of this object can
        // set a tooltip text for the focused entries;
        public event Action<EventSummaryTooltipArgs> RequestEventsSummary;

        // Event triggered when the user clicks on the control and changes current visible rows
        public event Action<EventSummaryEntryArgs> FocusChange;

        public EventSummaryGraphic()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            InitializeComponent();

            _toolTip = new ToolTip {ShowAlways = true};
            _toolTip.SetToolTip(this, "Summary Control");
            _toolTip.InitialDelay = 500;
            _toolTip.ReshowDelay = 500;
            _toolTip.Popup += ToolTipOnPopup;
            _mouseLocation.X = _mouseLocation.Y = 0;
            MouseLeave += OnMouseLeave;
            MouseClick += OnMouseClick;
            SizeChanged += OnSizeChanged;

            _eventSummaryMgr.FocusChange += EventSummaryMgrOnFocusChange;
            _eventSummaryMgr.Invalidated += EventSummaryMgrOnInvalidated;
            _eventSummaryMgr.TooltipRequested += EventSummaryMgrOnTooltipRequested;
        }

        private void EventSummaryMgrOnTooltipRequested(EventSummaryTooltipArgs eventSummaryTooltipArgs)
        {
            if (RequestEventsSummary != null)
            {
                RequestEventsSummary(eventSummaryTooltipArgs);
                this.ExecuteInUIThreadAsynchronously(() =>
                                                         {
                                                             if (_tooltipY != -1)
                                                                 SetTooltip(eventSummaryTooltipArgs.EntriesText);
                                                         });
            }
        }
        private void SetTooltip(string tooltipText)
        {
            _inToolTipPopup = true;
            _toolTip.SetToolTip(this, tooltipText);
            _inToolTipPopup = false;
        }

        private void ToolTipOnPopup(object sender, PopupEventArgs popupEventArgs)
        {
            if (!_inToolTipPopup)
            {
                _tooltipY = _mouseLocation.Y;
                _eventSummaryMgr.RequestTooltip(_mouseLocation);
                popupEventArgs.Cancel = true;
            }
        }
        private void EventSummaryMgrOnInvalidated(EventArgs eventArgs)
        {
            if(InvokeRequired)
            {
                if (_shutdown)
                    return;
                try
                {
                    BeginInvoke(new MethodInvoker(Invalidate));
                }
                catch (InvalidOperationException)
                {
                }
            }
            else
            {
                Invalidate();
            }
        }
        private void EventSummaryMgrOnFocusChange(EventSummaryEntryArgs eventSummaryEntryArgs)
        {
            if (InvokeRequired)
            {
                if (_shutdown)
                    return;
                BeginInvoke(new MethodInvoker(() => EventSummaryMgrOnFocusChange(eventSummaryEntryArgs)));
            }
            else
            {
                if (FocusChange != null)
                    FocusChange(eventSummaryEntryArgs);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            _shutdown = true;
            _eventSummaryMgr.Shutdown();
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnSizeChanged(object sender, EventArgs eventArgs)
        {
            _eventSummaryMgr.SetBounds(Bounds);
            //UpdateControl(true, true);
        }

        public void BeginUpdate()
        {
            _eventSummaryMgr.BeginUpdate();
        }
        public void EndUpdate()
        {
            _eventSummaryMgr.EndUpdate();
        }
        private void OnMouseLeave(object sender, EventArgs eventArgs)
        {
            _eventSummaryMgr.SetMouseOut();
        }   

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_tooltipY != -1 && e.Y != _tooltipY)
            {
                _toolTip.Hide(this);
                _tooltipY = -1;
            }

            _eventSummaryMgr.SetMouseIn(e.Location);
            _mouseLocation = e.Location;

            if (e.Button == MouseButtons.Left)
            {
                _eventSummaryMgr.MouseDown(e.Location);
            }
            else
            {
                _eventSummaryMgr.MouseUp(e.Location);
            }
        }

        private void OnMouseClick(object sender, MouseEventArgs mouseEventArgs)
        {
            _eventSummaryMgr.MouseClick(mouseEventArgs.Location);
            _tooltipY = _mouseLocation.Y;

            // clicking on the control twice breaks the tooltip -> do a request to avoid this
            _eventSummaryMgr.RequestTooltip(_mouseLocation);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            _eventSummaryMgr.OnPaint(pe);
        }

        public void Clear()
        {
            _eventSummaryMgr.Clear();
        }

        public void SetFocusRect(List<ITraceEntry> entries)
        {
            _eventSummaryMgr.SetFocusEntries(entries);
        }
        //[Conditional("DEBUG")]
        //public void Verify(TraceTreeView tree)
        //{
        //    TraceTreeView.TraceNode prevNode = null;
        //    foreach(var g in _groups)
        //    {
        //        foreach(var entry in g.Entries)
        //        {
        //            var node = entry as TraceTreeView.TraceNode;
        //            if (prevNode != null && tree.GetNextNode(prevNode) != node)
        //            {
        //                //Console.WriteLine();
        //                Debug.Assert(false);
        //            }
        //            prevNode = node;
        //        }
        //    }
        //}
        public void AddRange(List<ITraceEntry> entries)
        {
            _eventSummaryMgr.AddRange(entries);
        }

        public void RemoveRange(List<ITraceEntry> entries)
        {
            _eventSummaryMgr.RemoveRange(entries);
        }
        public void InsertRange(ITraceEntry entryBefore, List<ITraceEntry> entries)
        {
            _eventSummaryMgr.InsertRange(entryBefore, entries);
        }
        public void Update(ITraceEntry entry)
        {
            _eventSummaryMgr.Update(entry);
        }

    }

}
