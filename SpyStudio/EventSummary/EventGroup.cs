using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using SpyStudio.ContextMenu;

namespace SpyStudio.EventSummary
{
    public class EventGroup
    {
        public class EventGroupLocationComparer : IComparer<EventGroup>
        {
            public int Compare(EventGroup a, EventGroup b)
            {
                Debug.Assert(a != null, "a != null");
                Debug.Assert(b != null, "b != null");

                return a.Location.CompareTo(b.Location);
            }
        }

        public EventGroup()
        {
        }
        public EventGroup(ITraceEntry entry)
        {
            Entries = new List<ITraceEntry>();
            Color = entry.ColorSummary;
            Priority = entry.Priority;
            Critical = entry.Critical;
            Entries.Add(entry);
        }
        public EventGroup(List<ITraceEntry> entries)
        {
            Entries = entries;
            Debug.Assert(entries.Count > 0);
            var entry = entries[0];
            Color = entry.ColorSummary;
            Priority = entry.Priority;
            Critical = entry.Critical;
        }

        public void Update(ITraceEntry entry)
        {
            Color = entry.ColorSummary;
            Priority = entry.Priority;
            Critical = entry.Critical;
        }
        public bool Critical { get; private set; }
        public int Priority { get; private set; }

        public int PriorityAbsolute
        {
            get
            {
                if (Critical)
                    return Priority;
                return Priority + 5;
            }
        }

        public Color Color { get; private set; }
        public int RowCount { get { return Entries.Count; } }
        public List<ITraceEntry> Entries { get; private set; }
        public float Location { get; set; }
        public float Height { get; set; }
    }
}
