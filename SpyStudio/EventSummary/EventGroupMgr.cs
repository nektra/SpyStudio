using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.ContextMenu;

namespace SpyStudio.EventSummary
{
    public class GroupLocation
    {
        public EventGroup Group { get; set; }
        public int Row { get; set; }
    }

    class EventGroupMgr
    {
        // lock only to modify it or any content in the ABM thread. In UI thread lock when using to avoid modifications
        // in the other thread
        private readonly Dictionary<ITraceEntry, GroupLocation> _entryLocation =
            new Dictionary<ITraceEntry, GroupLocation>();
        private readonly List<EventGroup> _groups = new List<EventGroup>();
        private readonly int[] _priorityGroupCount = new int[6];
        private readonly float[] _priorityExtraHeight = new float[6];

        private float _totalHeight;
        private int _rowCount;
        private bool _mouseInControl;
        private Point _mouseLocation;

        readonly EventGroup _searchGroup = new EventGroup();
        readonly EventGroup.EventGroupLocationComparer _locationComparer = new EventGroup.EventGroupLocationComparer();

        private Graphics _cachedGraphics, _screenGraphics, _cachedGraphicsBackup;
        private Bitmap _cachedBitmap, _screenBitmap, _cachedBitmapBackup;
        private readonly object _bitmapLock = new object();
        private Rectangle _clientRect, _boundsRect, _displayRect;
        private readonly Dictionary<Color, Brush> _brushes = new Dictionary<Color, Brush>();

        private List<ITraceEntry> _focusEntries = new List<ITraceEntry>();

        private const int ZoomPixelCount = 20;
        private const int ZoomFactor = 4;
        private const int IdealFocusHeight = 16;

        private readonly Pen _focusPen, _mouseOverPen;

        public EventGroupMgr()
        {
            ClearPriorities();
            _focusPen = new Pen(Color.DodgerBlue, 2);
            _mouseOverPen = new Pen(Color.Blue, 1);
        }

        public bool Capture { get; set; }

        public Rectangle ClientRect
        {
            get { return _clientRect; }
        }

        private void IncrementCounters(EventGroup group)
        {
            if (group.Critical)
                _priorityGroupCount[0]++;
            _priorityGroupCount[group.Priority]++;
        }
        private void DecreaseCounters(EventGroup group)
        {
            if (group.Critical)
                _priorityGroupCount[0]--;
            _priorityGroupCount[group.Priority]--;
        }

        bool HasSameProperties(EventGroup group, ITraceEntry entry)
        {
            return (entry.ColorSummary == group.Color);
        }
        private void UpdateEntries(IEnumerable<ITraceEntry> entries, int rowOffset, EventGroup group)
        {
            UpdateEntries(entries, false, rowOffset, group);
        }

        private void UpdateEntries(IEnumerable<ITraceEntry> entries, bool newGroup, int rowOffset, EventGroup group)
        {
            int i = 0;
            foreach (var e in entries)
            {
                var entryLoc = _entryLocation[e];
                entryLoc.Group = group;
                entryLoc.Row = newGroup ? i++ : (entryLoc.Row + rowOffset);
            }
        }

        /// <summary>
        /// Split group leaving in group entries from 0 to index and groupPart2 will contain entries from index + 1 to end
        /// </summary>
        /// <param name="group"></param>
        /// <param name="index"></param>
        /// <param name="groupPart2"></param>
        private void SplitGroup(EventGroup group, int index, out EventGroup groupPart2)
        {
            var groupPart2EntryCount = group.Entries.Count - index - 1;
            var groupPart2Entries = group.Entries.GetRange(index + 1, groupPart2EntryCount);
            group.Entries.RemoveRange(index + 1, groupPart2EntryCount);
            groupPart2 = new EventGroup(groupPart2Entries);
            IncrementCounters(groupPart2);

            UpdateEntries(groupPart2Entries, true, 0, groupPart2);
        }
        public void UpdateRange(IEnumerable<ITraceEntry> entries)
        {
            foreach (var entry in entries)
            {
                GroupLocation groupLocation;
                if (_entryLocation.TryGetValue(entry, out groupLocation))
                {
                    EventGroup group = groupLocation.Group;
                    // if something changed
                    if (!HasSameProperties(group, entry))
                    {
                        lock (_entryLocation)
                        {
                            if (group.RowCount == 1)
                            {
                                DecreaseCounters(group);
                                group.Update(entry);
                                IncrementCounters(group);
                            }
                            else
                            {
                                var groupsToInsert = new List<EventGroup>();
                                var groupIndex = _groups.LastIndexOf(group);

                                var newGroup = new EventGroup(entry);
                                groupsToInsert.Add(newGroup);

                                IncrementCounters(newGroup);

                                group.Entries.RemoveAt(groupLocation.Row);

                                // if the entry isn't the last one -> the group should be splitted
                                if (groupLocation.Row != group.Entries.Count && groupLocation.Row != 0)
                                {
                                    EventGroup groupPart2;
                                    SplitGroup(group, groupLocation.Row - 1, out groupPart2);
                                    groupsToInsert.Add(groupPart2);
                                    _groups.InsertRange(groupIndex + 1, groupsToInsert);
                                }
                                else if (groupLocation.Row == 0)
                                {
                                    _groups.InsertRange(groupIndex, groupsToInsert);
                                    UpdateEntries(group.Entries, -1, group);
                                }
                                else
                                {
                                    _groups.InsertRange(groupIndex + 1, groupsToInsert);
                                }

                                groupLocation.Group = newGroup;
                                groupLocation.Row = 0;
                            }
                        }
                    }
                }
            }

        }

        public void AddRange(IEnumerable<ITraceEntry> entries)
        {
            foreach (var entry in entries)
            {
                Add(entry);
            }
        }
        public void Add(ITraceEntry entry)
        {
#if DEBUG
            if (_entryLocation.ContainsKey(entry))
            {
                Debug.Assert(false);
            }
#endif
            EventGroup group = null;
            if (_groups.Count > 0)
            {
                group = _groups.Last();
                if (HasSameProperties(group, entry))
                {
                    group.Entries.Add(entry);
                }
                else
                    group = null;
            }
            if (group == null)
            {
                group = new EventGroup(entry);
                _groups.Add(group);
                if (group.Critical)
                    _priorityGroupCount[0]++;
                _priorityGroupCount[group.Priority]++;
            }

            lock (_entryLocation)
            {
                _entryLocation[entry] = new GroupLocation {Group = group, Row = group.RowCount - 1};
            }

            _rowCount++;
            //UpdateControl(true);
        }

        public void RemoveRange(IEnumerable<ITraceEntry> entries)
        {
            foreach (var entry in entries)
            {
                Remove(entry);
            }
        }
        public bool Remove(ITraceEntry entry)
        {
            lock (_entryLocation)
            {
                GroupLocation groupLocation;
                if (!_entryLocation.TryGetValue(entry, out groupLocation))
                {
                    return false;
                }

                var group = groupLocation.Group;
                if (group.RowCount == 1)
                {
                    // remove group
                    DecreaseCounters(group);
                    _groups.Remove(group);
                }
                else
                {
                    for (int i = groupLocation.Row + 1; i < group.RowCount; i++)
                    {
                        var sibling = group.Entries[i];
                        _entryLocation[sibling].Row--;
                    }
                    group.Entries.RemoveAt(groupLocation.Row);
                }
                _entryLocation.Remove(entry);

                _rowCount--;
            }
            //UpdateControl(true);
            return true;
        }

        public void InsertRange(ITraceEntry entryBefore, IEnumerable<ITraceEntry> entries)
        {
            GroupLocation groupLocation = null;
            if (entryBefore != null && !_entryLocation.TryGetValue(entryBefore, out groupLocation))
            {
                return;
            }

            EventGroup group = null;
            var entryIndex = -1;
            if (groupLocation != null)
            {
                group = groupLocation.Group;
                entryIndex = group.Entries.LastIndexOf(entryBefore);
            }

            foreach (var entry in entries)
            {
#if DEBUG
                if (_entryLocation.ContainsKey(entry))
                {
                    Debug.Assert(false);
                }
#endif

                if (group != null && HasSameProperties(group, entry))
                {
                    Debug.Assert(entryIndex != -1);
                    group.Entries.Insert(++entryIndex, entry);
                    lock(_entryLocation)
                    {
                        _entryLocation[entry] = new GroupLocation { Group = group, Row = entryIndex };
                    }
                }
                else
                {
                    var groupsToInsert = new List<EventGroup>();
                    var groupIndex = group == null ? -1 : _groups.LastIndexOf(group);

                    var newGroup = new EventGroup(entry);
                    groupsToInsert.Add(newGroup);

                    IncrementCounters(newGroup);

                    lock(_entryLocation)
                    {
                        _entryLocation[entry] = new GroupLocation { Group = newGroup, Row = 0 };
                    }

                    // if the previous entry isn't the last one -> the group should be splitted
                    if (group != null && entryIndex != group.Entries.Count - 1)
                    {
                        EventGroup groupPart2;
                        SplitGroup(group, entryIndex, out groupPart2);
                        groupsToInsert.Add(groupPart2);
                    }

                    _groups.InsertRange(groupIndex + 1, groupsToInsert);
                    group = newGroup;
                    entryIndex = 0;
                }

                _rowCount++;
            }
            //UpdateControl(true);
        }

        public void Insert(ITraceEntry entryBefore, ITraceEntry entry)
        {
            InsertRange(entryBefore, new List<ITraceEntry> { entry });
        }

        float DrawGroup(Graphics graphics, EventGroup group, float y, float zoomFactor)
        {
            return DrawGroup(graphics, group, y, zoomFactor, 0,
                      group.RowCount);
        }
        float DrawGroup(Graphics graphics, EventGroup group, float y, float zoomFactor, int rowStart, int rowCount)
        {
            float h = group.RowCount * _totalHeight / RowCount;

            if (group.Critical)
                h += _priorityExtraHeight[0];
            h += _priorityExtraHeight[group.Priority];

            h = (h * (rowCount - rowStart) / group.RowCount) * zoomFactor;

            graphics.FillRectangle(GetBrush(group.Color),
                                   _clientRect.X, y,
                                   _clientRect.Width, h);
            return h;
        }

        private void CalculateExtraHeight()
        {
            float currentHeight = 4;
            for (int i = 0; i < _priorityGroupCount.Length - 1; i++)
            {
                _priorityExtraHeight[i] = currentHeight;
                if (_totalHeight / 2 > _priorityGroupCount[i] * _priorityExtraHeight[i])
                {
                    _totalHeight -= _priorityGroupCount[i] * _priorityExtraHeight[i];
                }
                else
                {
                    float reserved = _totalHeight / 2;
                    _priorityExtraHeight[i] = reserved / _priorityGroupCount[i];
                    _totalHeight -= _priorityGroupCount[i] * _priorityExtraHeight[i];
                }
                currentHeight -= i < 2 ? 1 : 0.5F;
            }
        }
        private void ClearPriorities()
        {
            for (int i = 0; i < _priorityGroupCount.Length; i++)
            {
                _priorityGroupCount[i] = 0;
                _priorityExtraHeight[i] = 0;
            }
        }
        private Brush GetBrush(Color c)
        {
            Brush b;
            if (!_brushes.TryGetValue(c, out b))
                _brushes[c] = b = new SolidBrush(c);
            return b;
        }
        private int GetGroupContaining(int y)
        {
            int row;
            return GetGroupContaining(y, out row);
        }
        private int GetGroupContaining(int y, out int row)
        {
            row = 0;
            if (_groups.Count == 0)
                return -1;
            _searchGroup.Location = y;
            var index = _groups.BinarySearch(_searchGroup, _locationComparer);
            if (index < 0)
            {
                if (index == -1)
                    return index;
                index = ~index - 1;

                var group = _groups[index];
                //Debug.Assert(group.Location <= y && Math.Ceiling(group.Location + group.Height) >= y);

                var yInsideGroup = y - group.Location;
                row = (int)Math.Round(yInsideGroup / (group.Height / group.RowCount));
                if (row >= group.RowCount)
                    row = group.RowCount - 1;
            }
            return index;
        }
        public int RowCount { get { return _rowCount; } }

        public void RedrawGroups(bool sizeChanged)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
// ReSharper disable JoinDeclarationAndInitializer
            double graphic;
// ReSharper restore JoinDeclarationAndInitializer
#endif
            lock (_bitmapLock)
            {
                if (_cachedGraphics == null || sizeChanged)
                {
                    if (_cachedBitmap != null && _cachedGraphics != null)
                    {
                        _cachedBitmap.Dispose();
                        _cachedGraphics.Dispose();
                        _screenBitmap.Dispose();
                        _screenGraphics.Dispose();
                    }

                    _cachedBitmap = new Bitmap(_displayRect.Width, _displayRect.Height);
                    _cachedGraphics = Graphics.FromImage(_cachedBitmap);

                    _cachedBitmapBackup = new Bitmap(_displayRect.Width, _displayRect.Height);
                    _cachedGraphicsBackup = Graphics.FromImage(_cachedBitmapBackup);

                    _screenBitmap = new Bitmap(_displayRect.Width, _displayRect.Height);
                    _screenGraphics = Graphics.FromImage(_screenBitmap);
                }

            }

            _cachedGraphicsBackup.FillRectangle(new SolidBrush(SystemColors.Window), _displayRect);
            _cachedGraphicsBackup.DrawRectangle(new Pen(SystemColors.WindowFrame), _boundsRect.X, _boundsRect.Y,
                                          _boundsRect.Width, _boundsRect.Height - 1);

            _totalHeight = _clientRect.Height;
            float y = _clientRect.Y;

            CalculateExtraHeight();

            foreach (var group in _groups)
            {
                group.Location = y;
                group.Height = DrawGroup(_cachedGraphicsBackup, group, y, 1);
                y += group.Height;
            }
#if DEBUG
            graphic = sw.Elapsed.TotalMilliseconds;
#endif
            lock (_bitmapLock)
            {
                var tempBitmap = _cachedBitmap;
                _cachedBitmap = _cachedBitmapBackup;
                _cachedBitmapBackup = tempBitmap;

                var tempGraphics = _cachedGraphics;
                _cachedGraphics = _cachedGraphicsBackup;
                _cachedGraphicsBackup = tempGraphics;
            }
#if DEBUG
            var rest = sw.Elapsed.TotalMilliseconds - graphic;
            Debug.WriteLine("EventSummary Paint event:\nBitmap generation: " + graphic + "\nFocus " + rest);
#endif
        }
        public void DrawCache(PaintEventArgs pe)
        {
            lock (_bitmapLock)
            {
                if (_cachedBitmap == null)
                    return;

                // draw cached bitmap
                _screenGraphics.DrawImage(_cachedBitmap, 0, 0);
                pe.Graphics.DrawImage(_screenBitmap, 0, 0);
            }
        }
        public void DrawFocus(PaintEventArgs pe)
        {
            //Debug.WriteLine("XXXX: " + _mouseLocation);
            // draw focus rect if mouse isn't over the control and the user isn't changing focus rect at the same time
            if (_focusEntries.Count > 0 && (!Capture || !_mouseInControl))
            {
                lock(_entryLocation)
                {
                    var first = GetFirstFocusEntryLocation();
                    var last = GetLastFocusEntryLocation();
                    if (first != null && last != null)
                    {
                        float y = first.Group.Location + first.Group.Height * first.Row / first.Group.RowCount;
                        float h = last.Group.Location - y +
                                  last.Group.Height * (last.Row + 1) / last.Group.RowCount;
                        if (h < IdealFocusHeight)
                        {
                            var newY = y - Math.Abs((IdealFocusHeight - h) / 2);
                            h = IdealFocusHeight;
                            if (newY > _clientRect.Y)
                            {
                                y = newY;
                                if (y + IdealFocusHeight > _clientRect.Bottom)
                                {
                                    h = _clientRect.Bottom - y;
                                }
                            }
                            else
                            {
                                y = _clientRect.Y;
                                h -= _clientRect.Y - newY;
                            }
                        }
                        if (y < _clientRect.Y)
                            y = _clientRect.Y;
                        if (y + h > _clientRect.Bottom)
                            y = _clientRect.Bottom - h;
                        _screenGraphics.DrawRectangle(_focusPen,
                                                      _clientRect.X, y,
                                                      _clientRect.Width, h + 2);
                    }
                }
            }

            lock (_bitmapLock)
            {
                pe.Graphics.DrawImage(_screenBitmap, 0, 0);
            }

            // draw the zoomed mouse over sections
            if (_focusEntries.Count > 0 && _mouseInControl && _mouseLocation.Y >= _clientRect.Y &&
                _mouseLocation.Y <= _clientRect.Bottom)
            {
                int y1Dest = _mouseLocation.Y - ZoomPixelCount*ZoomFactor/2;
                int y2Dest = _mouseLocation.Y + ZoomPixelCount*ZoomFactor/2;
                int y1Source, y2Source;
                if (y1Dest < _clientRect.Y)
                {
                    y1Source = _mouseLocation.Y - (_mouseLocation.Y - (_clientRect.Y))/ZoomFactor;
                    y1Dest = _mouseLocation.Y - (_mouseLocation.Y - (_clientRect.Y));
                }
                else
                {
                    y1Source = _mouseLocation.Y - ZoomPixelCount/2;
                }
                if (y2Dest > _clientRect.Bottom)
                {
                    y2Source = _mouseLocation.Y + (_clientRect.Bottom - _mouseLocation.Y)/ZoomFactor;
                    y2Dest = _mouseLocation.Y + (_clientRect.Bottom - _mouseLocation.Y);
                }
                else
                {
                    y2Source = _mouseLocation.Y + ZoomPixelCount/2;
                }

                var sourceRect = new Rectangle(_clientRect.X - 1, y1Source, _clientRect.Width + 2, y2Source - y1Source);
                var destRect = new Rectangle(_clientRect.X - 1, y1Dest, _clientRect.Width + 2,
                                             y2Dest - y1Dest);

                lock (_bitmapLock)
                {

                    pe.Graphics.DrawImage(_screenBitmap,
                                          destRect,
                                          sourceRect,
                                          GraphicsUnit.Pixel);
                    if (!Capture)
                    {
                        pe.Graphics.DrawLine(_mouseOverPen, _clientRect.X - 1, destRect.Y, _clientRect.Width + 2,
                                             destRect.Y);
                        pe.Graphics.DrawLine(_mouseOverPen, _clientRect.X - 1, destRect.Bottom, _clientRect.Width + 2,
                                             destRect.Bottom);
                    }
                    else
                    {
                        pe.Graphics.DrawRectangle(_focusPen,
                                                  _clientRect.X, destRect.Y,
                                                  _clientRect.Width, destRect.Height + 1);
                    }
                }
                //    if (_toolTip.Active)
                //        UpdateTooltip();
                //}
            }
        }
        public void SetBounds(Rectangle bounds)
        {
            _displayRect = _boundsRect = bounds;
            _clientRect = bounds;
            _clientRect.Y += 2;
            _clientRect.X += 2;
            _clientRect.Width -= 3;
            _clientRect.Height -= 6;
        }
        private GroupLocation GetFirstFocusEntryLocation()
        {
            GroupLocation entryLocation = null;
            int i = 0;
            while (i < _focusEntries.Count)
            {
                if (_entryLocation.TryGetValue(_focusEntries[i++], out entryLocation))
                {
                    break;
                }
            }
            return entryLocation;
        }
        private GroupLocation GetLastFocusEntryLocation()
        {
            GroupLocation entryLocation = null;
            int i = _focusEntries.Count - 1;
            while (i >= 0)
            {
                if (_entryLocation.TryGetValue(_focusEntries[i--], out entryLocation))
                {
                    break;
                }
            }
            return entryLocation;
        }

        public void SetFocusEntries(List<ITraceEntry> entries)
        {
            _focusEntries = entries.ToList();
        }
        public ITraceEntry GetEntry(int y)
        {
            int row;
            var index = GetGroupContaining(y, out row);
            if(index != -1)
            {
                return _groups[index].Entries[row];
            }
            return null;
        }
        public List<ITraceEntry> GetGroupEntries(Point p)
        {
            if(_clientRect.Contains(p))
            {
                var index = GetGroupContaining(p.Y);
                if (index != -1)
                {
                    return _groups[index].Entries;
                }
            }
            return null;
        }

        public void Clear()
        {
            _groups.Clear();
            _entryLocation.Clear();
            _focusEntries.Clear();
            _rowCount = 0;
            ClearPriorities();
            RedrawGroups(false);
        }

        public void SetMouseIn(Point location)
        {
            _mouseInControl = _clientRect.Contains(location);
            _mouseLocation = location;
        }

        public void SetMouseOut()
        {
            _mouseInControl = false;
            //Capture = false;
        }

    }
}
