//#define SHOW_DETAILS
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpyStudio.Hooks.Async;

namespace SpyStudio.Tools
{
    public class PendingEventsCountManager
    {
        public const int BufferPhase = 0;
        public const int ProcessNewEventPhase = 1;
        public const int CompleteEventPhase = 2;
        public const int DatabasePhase = 3;
        public const int GuiPhase = 4;

        private readonly int[] _eventsInPhases = new int[5];
        private int max = 0;
        public FormMain FormMain { set; private get; }
#if SHOW_DETAILS
        private ToolStripLabel[] _labels;
#endif
        public class TimeLinePoint
        {
            public double Timestamp;
            public int[] Counts;
            public TimeLinePoint(int[] counts)
            {
                Timestamp = AsyncHookMgr.GetTimestamp();
                Counts = (int[])counts.Clone();
            }
        }

        private readonly List<TimeLinePoint> TimeLine = new List<TimeLinePoint>();

        public void EventsEnter(int count, int phase)
        {
            if(count > 0)
            {
                lock (_eventsInPhases)
                {
                    _eventsInPhases[phase] += (int)count;
#if SHOW_DETAILS
                    TimeLine.Add(new TimeLinePoint(_eventsInPhases));
#endif
                }
                if (FormMain != null)
                    FormMain.UpdateStatusBarPendingEvents();
            }
        }

        public void EventsLeave(int count, int phase)
        {
            if (count <= 0)
                return;
            lock (_eventsInPhases)
            {
                if (count > _eventsInPhases[phase])
                {
                    //Debug.Assert(false);
                    _eventsInPhases[phase] = 0;
                }
                else
                    _eventsInPhases[phase] -= count;
#if SHOW_DETAILS
                TimeLine.Add(new TimeLinePoint(_eventsInPhases));
#endif
            }
            if (FormMain != null)
                FormMain.UpdateStatusBarPendingEvents();
        }

        public string GetTimeLineString()
        {
            var ret = new StringBuilder();
            double firstTimeStamp = -1;
            foreach (var timeLinePoint in TimeLine)
            {
                if (firstTimeStamp < 0)
                    firstTimeStamp = timeLinePoint.Timestamp;
                ret.Append(timeLinePoint.Timestamp - firstTimeStamp);
                foreach (var count in timeLinePoint.Counts)
                {
                    ret.Append('\t');
                    ret.Append(count);
                }
                ret.Append("\r\n");
            }
            return ret.ToString();
        }

        public bool NoEventsLeft
        {
            get
            {
                bool ret;
                lock(_eventsInPhases)
                {
                    ret = _eventsInPhases.Sum() == 0;
                }
                if (ret)
                    UpdateWindow();
                return ret;
            }
        }

        public void UpdateWindow()
        {
#if SHOW_DETAILS
            int n = _eventsInPhases.Length;
            for (int i = 0; i < n; i++)
            {
                _labels[i].Text = _eventsInPhases[i].ToString(CultureInfo.InvariantCulture);
            }
            _labels[n].Text = "Max: " + max.ToString(CultureInfo.InvariantCulture);
#endif
        }

        public override string ToString()
        {
            lock (_eventsInPhases)
            {
                var sum = _eventsInPhases.Sum();
                if (sum > max)
                    max = sum;
                UpdateWindow();
                var ret = sum.ToString(CultureInfo.InvariantCulture);
                return ret;
            }
        }

        private static readonly PendingEventsCountManager GlobalInstance = new PendingEventsCountManager();

        public static PendingEventsCountManager GetInstance()
        {
            return GlobalInstance;
        }

        public void InitStatus(StatusStrip statusStrip1)
        {
#if SHOW_DETAILS
            int n = _eventsInPhases.Length + 1;
            _labels = new ToolStripLabel[n];

            for (int i = n; i-- != 0; )
            {
                _labels[i] = new ToolStripLabel();
                _labels[i].TextAlign = ContentAlignment.TopRight;
                _labels[i].AutoSize = false;
                _labels[i].Width = 50;
                statusStrip1.Items.Insert(2, _labels[i]);
            }
            _labels[n - 1].Width = 100;
#endif
        }
    }
}
