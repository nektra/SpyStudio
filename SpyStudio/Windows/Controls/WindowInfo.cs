using System.Collections.Generic;
using System.Diagnostics;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Extensions;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Windows.Controls
{
    public class WindowInfo : IInfo
    {
        #region Implementation of IInfo

        public uint TraceID { get; protected set; }
        public HashSet<CallEventId> CallEventIds { get; protected set; }
        public HashSet<DeviareTraceCompareItem> CompareItems { get; protected set; }
        public bool IsNull { get; protected set; }

        #endregion

        #region Properties

        public string ID { get; protected set; }
        public ModulePath ModulePath { get; protected set; }
        public string ModuleHandle { get; protected set; }
        public string ClassName { get; protected set; }
        public string WindowName { get; protected set; }
        public bool Success { get; protected set; }
        public double Time { get; protected set; }
        public uint Count { get; set; }
        public ulong ReturnValue { get; set; }
        public string Result { get; set; }

        #endregion

        #region Instantiation

        public static WindowInfo From(CallEvent aCallEvent, ModulePath aModulePath)
        {
            var info = new WindowInfo();

            info.IsNull = false;
            info.TraceID = aCallEvent.TraceId;
            info.ID = aCallEvent.GetUniqueID();
            info.Success = aCallEvent.Success;
            info.ModulePath = aModulePath;
            info.ModuleHandle = aCallEvent.GetModuleHandleUsing(aModulePath);
            info.ClassName = aCallEvent.GetWindowClassName();
            info.WindowName = aCallEvent.GetWindowName();
            info.Time = aCallEvent.Time;
            info.Count = 1;
            info.ReturnValue = aCallEvent.RetValue;
            info.Result = aCallEvent.Result;
            info.CallEventIds.Add(aCallEvent.EventId);

            return info;
        }

        public WindowInfo()
        {
            IsNull = true;

            CallEventIds = new HashSet<CallEventId>();
            CompareItems = new HashSet<DeviareTraceCompareItem>();
        }

        #endregion

        public void MergeWith(WindowInfo anotherInfo)
        {
            if (anotherInfo.IsNull)
                return;

            Debug.Assert(IsNull || TraceID == anotherInfo.TraceID, "Tried to merge WindowInfos from different traces.");

            IsNull = false;

            TraceID = anotherInfo.TraceID;

            CallEventIds.AddRange(anotherInfo.CallEventIds);
            CompareItems.AddRange(anotherInfo.CompareItems);

            ModulePath = anotherInfo.ModulePath;
            
            Time += anotherInfo.Time;
            Count += anotherInfo.Count;

            Success |= anotherInfo.Success;

            if (Success && !anotherInfo.Success)
                return;

            ClassName = anotherInfo.ClassName;
            WindowName = anotherInfo.WindowName;
            ReturnValue = anotherInfo.ReturnValue;
            Result = anotherInfo.Result;
            ModuleHandle = anotherInfo.ModuleHandle;
        }
    }
}