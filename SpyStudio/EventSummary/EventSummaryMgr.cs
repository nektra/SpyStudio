using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SpyStudio.ContextMenu;

namespace SpyStudio.EventSummary
{
    class EventSummaryMgr
    {
        private readonly Thread _groupDrawThread;
        private readonly AutoResetEvent _wakeupEvent;
        private readonly AutoResetEvent _shutdownEvent;

        private bool _redrawGroups = true;
        private bool _sizeChanged;

        private List<EventSummaryCmd> _pendingCmds = new List<EventSummaryCmd>();
        private int _focusHeightPending = -1;
        private Point _updateTooltipPos = new Point(0, 0);
        private int _updateCount;
        private bool _invalidatePending;

        private readonly EventGroupMgr _groupMgr = new EventGroupMgr();

        public event Action<EventSummaryEntryArgs> FocusChange;
        public event Action<EventArgs> Invalidated;
        public event Action<EventSummaryTooltipArgs> TooltipRequested;

        public EventSummaryMgr()
        {
            _wakeupEvent = new AutoResetEvent(false);
            _shutdownEvent = new AutoResetEvent(false);
            _groupDrawThread = new Thread(GroupCalculationBackgroundThread);
            _groupDrawThread.Start();
        }
        public void Shutdown()
        {
            _shutdownEvent.Set();
            _groupDrawThread.Join();
        }

        private void GroupCalculationBackgroundThread()
        {
            var waitHandles = new WaitHandle[2];
            waitHandles[0] = _shutdownEvent;
            waitHandles[1] = _wakeupEvent;
            int index = WaitHandle.WaitAny(waitHandles);
            while (index == 1)
            {
                bool moreCmds, redraw = false;
                do
                {
                    List<EventSummaryCmd> cmdsToProcess = null;
                    lock (_pendingCmds)
                    {
                        if (_pendingCmds.Count > 0)
                        {
                            cmdsToProcess = _pendingCmds;
                            _pendingCmds = new List<EventSummaryCmd>();
                            redraw = true;
                        }
                    }
                    if (cmdsToProcess != null)
                    {
                        foreach (var cmdToProcess in cmdsToProcess)
                        {
                            ProcessCmd(cmdToProcess);
                        }
                    }
                    lock (_pendingCmds)
                    {
                        moreCmds = (_pendingCmds.Count > 0);
                    }
                } while (moreCmds && !_shutdownEvent.WaitOne(0));

                if (_redrawGroups || redraw || _sizeChanged)
                {
                    if (_updateCount > 0)
                    {
                        _redrawGroups = _invalidatePending = true;
                    }
                    else
                    {
                        _groupMgr.RedrawGroups(_sizeChanged);
                    }
                }

                ProcessTooltip();

                if (_updateCount == 0 && (redraw || _sizeChanged || ProcessFocusHeight()))
                {
                    Invalidate();
                }
                _sizeChanged = _redrawGroups = false;

                index = WaitHandle.WaitAny(waitHandles);
            }
        }

        bool ProcessFocusHeight()
        {
            if(_focusHeightPending != -1)
            {
                var entry = _groupMgr.GetEntry(_focusHeightPending);
                if (entry != null)
                {
                    FocusChange(new EventSummaryEntryArgs(entry));
                }

                _focusHeightPending = -1;
                return true;
            }
            return false;
        }
        void ProcessTooltip()
        {
            if (!_updateTooltipPos.IsEmpty)
            {
                var entries = _groupMgr.GetGroupEntries(_updateTooltipPos);
                if(entries != null && entries.Count > 0)
                {
                    if(TooltipRequested != null)
                        TooltipRequested(new EventSummaryTooltipArgs(entries));
                }

                _updateTooltipPos.X = _updateTooltipPos.Y = 0;
            }
        }
        void ProcessCmd(EventSummaryCmd cmd)
        {
            switch (cmd.CmdType)
            {
                case EventSummaryCmd.EventSummaryCmdType.BeginUpdate:
                    _updateCount++;
                    break;
                case EventSummaryCmd.EventSummaryCmdType.EndUpdate:
                    if (--_updateCount == 0 && _invalidatePending)
                    {
                        Invalidate();
                    }
                    break;
                case EventSummaryCmd.EventSummaryCmdType.Clear:
                    _groupMgr.Clear();
                    break;
                case EventSummaryCmd.EventSummaryCmdType.AddRange:
                    _groupMgr.AddRange(cmd.Entries);
                    break;
                case EventSummaryCmd.EventSummaryCmdType.RemoveRange:
                    _groupMgr.RemoveRange(cmd.Entries);
                    break;
                case EventSummaryCmd.EventSummaryCmdType.InsertRange:
                    {
                        var insertCmd = (InsertRangeEventSummaryCmd) cmd;
                        _groupMgr.InsertRange(insertCmd.EntryBefore, insertCmd.Entries);
                    }
                    break;
                case EventSummaryCmd.EventSummaryCmdType.Update:
                    _groupMgr.UpdateRange(cmd.Entries);
                    break;
            }
        }

        private void AddCommand(EventSummaryCmd cmd)
        {
            lock(_pendingCmds)
            {
                _pendingCmds.Add(cmd);
                _wakeupEvent.Set();
            }
        }
        public void BeginUpdate()
        {
            AddCommand(new BeginUpdateEventSummaryCmd());
        }

        public void EndUpdate()
        {
            AddCommand(new EndUpdateEventSummaryCmd());
        }
        public void Clear()
        {
            AddCommand(new ClearEventSummaryCmd());
        }
        public void AddRange(List<ITraceEntry> entries)
        {
            AddCommand(new AddRangeEventSummaryCmd(entries));
        }

        public void RemoveRange(List<ITraceEntry> entries)
        {
            AddCommand(new RemoveRangeEventSummaryCmd(entries));
        }
        public void InsertRange(ITraceEntry entryBefore, List<ITraceEntry> entries)
        {
            AddCommand(new InsertRangeEventSummaryCmd(entryBefore, entries));
        }
        public void Update(ITraceEntry entry)
        {
            AddCommand(new UpdateEventSummaryCmd(entry));
        }
        public void SetFocusEntries(List<ITraceEntry> entries)
        {
            _groupMgr.SetFocusEntries(entries);
            Invalidate();
        }

        public void SetBounds(Rectangle bounds)
        {
            _sizeChanged = true;
            _groupMgr.SetBounds(bounds);
            _wakeupEvent.Set();
        }

        public void OnPaint(PaintEventArgs pe)
        {
            _groupMgr.DrawCache(pe);
            _groupMgr.DrawFocus(pe);
        }

        public void SetMouseOut()
        {
            _groupMgr.Capture = false;
            _groupMgr.SetMouseOut();
            Invalidate();
        }

        public void SetMouseIn(Point location)
        {
            _groupMgr.SetMouseIn(location);
            Invalidate();
        }
        private void Invalidate()
        {
            if(_updateCount > 0)
            {
                _invalidatePending = true;
                return;
            }
            _invalidatePending = false;
            if (Invalidated != null)
                Invalidated(new EventArgs());
        }

        public void SetFocusedHeight(int y)
        {
            if (y < _groupMgr.ClientRect.Y)
                y = _groupMgr.ClientRect.Y;
            else if (y > _groupMgr.ClientRect.Bottom)
                y = _groupMgr.ClientRect.Bottom;

            _focusHeightPending = y;
            _wakeupEvent.Set();
        }

        public void MouseDown(Point location)
        {
            _groupMgr.Capture = true;
            SetFocusedHeight(location.Y);
        }
        public void MouseClick(Point location)
        {
            SetFocusedHeight(location.Y);
        }

        public void MouseUp(Point location)
        {
            _groupMgr.Capture = false;
        }
        public void RequestTooltip(Point p)
        {
            _updateTooltipPos = p;
            _wakeupEvent.Set();
        }

    }
}
