using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.Extensions;

namespace SpyStudio.Main
{
    public class DataInfo : ListViewSorted
    {
        static DataInfo _dataInfo;
        readonly object _dataLock = new object();
        Timer _timer = new Timer();
        readonly List<DataInfoToAdd> _infosToAdd = new List<DataInfoToAdd>();
        private ProcessInfo _procInfo1, _procInfo2, _procInfo;
        private bool _compareMode;
        private uint _traceId1, _traceId2;

        public class DataInfoItem : ListViewItem 
        {
            uint _regEntries1, _fileEntries1, _wndEntries1, _comEntries1, _customEntries1;
            uint _regEntries2, _fileEntries2, _wndEntries2, _comEntries2, _customEntries2;
            double _time1, _time2;
            private bool _compare = false;

            public DataInfoItem(string module)
            {
                Text = module;
                SubItems.Add("0");
                SubItems.Add("0");
                SubItems.Add("0");
                SubItems.Add("0");
                SubItems.Add("0");
                SubItems.Add("0");
                SubItems.Add("0");
            }

            public void AddData(DataInfoToAdd infoToAdd)
            {
                _regEntries1 += infoToAdd.RegEntries;
                _fileEntries1 += infoToAdd.FileEntries;
                _wndEntries1 += infoToAdd.WndEntries;
                _comEntries1 += infoToAdd.ComEntries;
                _customEntries1 += infoToAdd.CustomEntries;
                _time1 += infoToAdd.Time;
                Update();
            }
            public void AddData1(DataInfoToAdd infoToAdd)
            {
                _compare = true;
                _regEntries1 += infoToAdd.RegEntries;
                _fileEntries1 += infoToAdd.FileEntries;
                _wndEntries1 += infoToAdd.WndEntries;
                _comEntries1 += infoToAdd.ComEntries;
                _customEntries1 += infoToAdd.CustomEntries;
                _time1 += infoToAdd.Time;
                Update();
            }
            public void AddData2(DataInfoToAdd infoToAdd)
            {
                _compare = true;
                _regEntries2 += infoToAdd.RegEntries;
                _fileEntries2 += infoToAdd.FileEntries;
                _wndEntries2 += infoToAdd.WndEntries;
                _comEntries2 += infoToAdd.ComEntries;
                _customEntries2 += infoToAdd.CustomEntries;
                _time2 += infoToAdd.Time;
                Update();
            }
            UInt64 Total1()
            {
                return _comEntries1 + _customEntries1 + _fileEntries1 + _regEntries1 + _wndEntries1;
            }
            UInt64 Total2()
            {
                return _comEntries2 + _customEntries2 + _fileEntries2 + _regEntries2 + _wndEntries2;
            }
            void Update()
            {
                if(!_compare)
                {
                    SubItems[1].Text = _comEntries1.ToString(CultureInfo.InvariantCulture);
                    SubItems[2].Text = _regEntries1.ToString(CultureInfo.InvariantCulture);
                    SubItems[3].Text = _fileEntries1.ToString(CultureInfo.InvariantCulture);
                    SubItems[4].Text = _wndEntries1.ToString(CultureInfo.InvariantCulture);
                    SubItems[5].Text = _customEntries1.ToString(CultureInfo.InvariantCulture);
                    SubItems[6].Text = Total1().ToString(CultureInfo.InvariantCulture);
                    SubItems[7].Text = string.Format("{0:N5}", _time1);
                }
                else
                {
                    SubItems[1].Text = _comEntries1.ToString(CultureInfo.InvariantCulture) + " / " + _comEntries2.ToString(CultureInfo.InvariantCulture);
                    SubItems[2].Text = _regEntries1.ToString(CultureInfo.InvariantCulture) + " / " + _regEntries2.ToString(CultureInfo.InvariantCulture);
                    SubItems[3].Text = _fileEntries1.ToString(CultureInfo.InvariantCulture) + " / " + _fileEntries2.ToString(CultureInfo.InvariantCulture);
                    SubItems[4].Text = _wndEntries1.ToString(CultureInfo.InvariantCulture) + " / " + _wndEntries2.ToString(CultureInfo.InvariantCulture);
                    SubItems[5].Text = _customEntries1.ToString(CultureInfo.InvariantCulture) + " / " + _customEntries2.ToString(CultureInfo.InvariantCulture);
                    SubItems[6].Text = Total1().ToString(CultureInfo.InvariantCulture) + " / " + Total2().ToString(CultureInfo.InvariantCulture);
                    SubItems[7].Text = string.Format("{0:N5}", _time1) + " / " + string.Format("{0:N5}", _time2);
                }
            }
        }
        public class DataInfoToAdd
        {
            public uint RegEntries, FileEntries, WndEntries, ComEntries, ComServed, Pid, CustomEntries;
            public UInt64 TraceId;
            public double Time;
            public string Caller;

            public DataInfoToAdd(uint regEntries, uint fileEntries, uint wndEntries,
                uint comEntries, uint customEntries, double time, uint pid, string caller,
                UInt64 traceId)
            {
                RegEntries = regEntries;
                FileEntries = fileEntries;
                WndEntries = wndEntries;
                ComEntries = comEntries;
                Time = time;
                Pid = pid;
                Caller = caller;
                CustomEntries = customEntries;
                TraceId = traceId;
            }
        }

        public DataInfo()
        {
            _dataInfo = this;
            _timer.Interval = Properties.Settings.Default.DataInfoUpdateInterval;
            _timer.Enabled = true;
            _timer.Tick += ProcessChanges;
            _timer.Start();
        }
        public void SetTrace(DeviareRunTrace devRunTrace)
        {
            _compareMode = false;
            _procInfo = devRunTrace.GetProcessInfo();
        }
        public void SetTraces(DeviareRunTrace trace1, DeviareRunTrace trace2)
        {
            _compareMode = true;
            _traceId1 = trace1.TraceId;
            _traceId2 = trace2.TraceId;
            _procInfo1 = trace1.GetProcessInfo();
            _procInfo2 = trace2.GetProcessInfo();
        }
        public void SetTraces(ProcessInfo procInfo1, ProcessInfo procInfo2)
        {
            _compareMode = true;
            _traceId1 = 0;
            _traceId2 = 1;
            _procInfo1 = procInfo1;
            _procInfo2 = procInfo2;
        }

        public void AddOpenKey(object sender, CallEventArgs e)
        {
            if(!e.Event.IsGenerated)
                AddRegistry(e.Event);
        }
        public void AddQueryValue(object sender, CallEventArgs e)
        {
            if (!e.Event.IsGenerated)
                AddRegistry(e.Event);
        }
        public void AddCoCreate(object sender, CallEventArgs e)
        {
            if (!e.Event.IsGenerated)
                AddCoCreate(e.Event);
        }
        public void AddFile(object sender, CallEventArgs e)
        {
            if (!e.Event.IsGenerated)
                AddFile(e.Event);
        }
        public void AddCreateWindow(object sender, CallEventArgs e)
        {
            if (!e.Event.IsGenerated)
                AddCreateWindow(e.Event);
        }
        public void AddCustom(object sender, CallEventArgs e)
        {
            if (!e.Event.IsGenerated)
                AddCustom(e.Event);
        }
        public void ClearData(object sender, EventArgs e)
        {
            ClearData();
        }

        public new void BeginUpdate()
        {
            this.ExecuteInUIThreadAsynchronously(base.BeginUpdate);
        }
        public new void EndUpdate()
        {
            this.ExecuteInUIThreadAsynchronously(base.EndUpdate);
        }
        public void AddDataInfo(uint regEntries, uint fileEntries, uint wndEntries,
            uint comEntries, uint customEntries, CallEvent e)
        {
            lock (_dataLock)
            {
                AddDataInfo(new DataInfoToAdd(regEntries, fileEntries, wndEntries,
                    comEntries, customEntries, e.Time, e.Pid, e.CallModule, e.TraceId));
            }
        }
        public void AddEvent1(CallEvent e)
        {
            e.TraceId = _traceId1;
            AddEvent(e);
        }
        public void AddEvent2(CallEvent e)
        {
            e.TraceId = _traceId2;
            AddEvent(e);
        }

        public void AddEvent(CallEvent e)
        {
            if(e.IsRegistry)
            {
                AddRegistry(e);
            }
            else if(e.IsFileSystem)
            {
                AddFile(e);
            }
            else if(e.IsCom)
            {
                AddCoCreate(e);
            }
            else if(e.IsWindow)
            {
                AddCreateWindow(e);
            }
            else
            {
                AddCustom(e);
            }
        }
        public void AddRegistry(CallEvent e)
        {
            AddDataInfo(1, 0, 0, 0, 0, e);
        }
        public void AddFile(CallEvent e)
        {
            AddDataInfo(0, 1, 0, 0, 0, e);
        }
        public void AddCreateWindow(CallEvent e)
        {
            AddDataInfo(0, 0, 1, 0, 0, e);
        }
        public void AddCoCreate(CallEvent e)
        {
            AddDataInfo(0, 0, 0, 1, 0, e);
        }
        public void AddCustom(CallEvent e)
        {
            AddDataInfo(0, 0, 0, 0, 1, e);
        }

        public delegate void ClearDataDelegate();
        public void ClearData()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ClearDataDelegate(ClearData));
            }
            else
            {
                lock (_dataLock)
                {
                    _infosToAdd.Clear();
                    BeginUpdate();
                    Items.Clear();
                    Groups.Clear();
                    EndUpdate();
                }
            }
        }
        public delegate void ProcessDataInfoDelegate(DataInfoToAdd infoToAdd);

        public void AddDataInfo(DataInfoToAdd infoToAdd)
        {
            if (InvokeRequired)
            {
                lock (_dataLock)
                {
                    _infosToAdd.Add(infoToAdd);
                }
            }
            else
            {
                ProcessDataInfo(infoToAdd);
            }
        }
        ProcessInfo GetProcessInfo(DataInfoToAdd infoToAdd)
        {
            ProcessInfo procInfo;
            if (_compareMode)
            {
                if (infoToAdd.TraceId == _traceId1)
                    procInfo = _procInfo1;
                else if (infoToAdd.TraceId == _traceId2)
                    procInfo = _procInfo2;
                else
                {
                    Debug.Assert(false, "Invalid trace id");
                    return null;
                }
            }
            else
            {
                procInfo = _procInfo;
            }
            return procInfo;
        }
        public void ProcessDataInfo(DataInfoToAdd infoToAdd)
        {
            ProcessInfo procInfo = GetProcessInfo(infoToAdd);
            if (procInfo == null)
                return;

            var procName = procInfo.GetName(infoToAdd.Pid);
            if(String.IsNullOrEmpty(procName))
            {
                procName = infoToAdd.Pid.ToString(CultureInfo.InvariantCulture);
            }
            ListViewGroup procGroup = Groups[procName.ToLower()];
            if (procGroup == null)
            {
                procGroup = Groups.Add(procName.ToLower(), procName);
            }
            string module = infoToAdd.Caller;
            if (string.IsNullOrEmpty(module))
                module = procName;

            var item = (DataInfoItem)procGroup.Items[module.ToLower()];
            if (item == null)
            {
                item = new DataInfoItem(module)
                           {
                               Name = module.ToLower()
                           };
                // use the module name in lower as key
                Items.Add(item);
                procGroup.Items.Add(item);
            }
            if (_compareMode)
            {
                if (infoToAdd.TraceId == _traceId1)
                    item.AddData1(infoToAdd);
                else
                    item.AddData2(infoToAdd);
            }
            else
                item.AddData(infoToAdd);
        }
        public void ProcessChanges(object sender, EventArgs eArgs)
        {
            lock (_dataLock)
            {
                ShowItemToolTips = Properties.Settings.Default.ShowTooltip;

                if (_infosToAdd.Count > 0)
                {
                    BeginUpdate();
                    foreach (DataInfoToAdd infoToAdd in _infosToAdd)
                    {
                        ProcessDataInfo(infoToAdd);
                    }
                    _infosToAdd.Clear();
                    EndUpdate();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //dispose managed ressources
                    base.Dispose(true);
                }

                if (_timer != null)
                {
                    _timer.Tick -= ProcessChanges;
                    _timer.Stop();
                }
            }

            base.Dispose(disposing);
            //dispose unmanaged ressources
            _disposed = true;
        }

        private bool _disposed;

        public new void Dispose()
        {
            Dispose(true);
        }
    }
}
