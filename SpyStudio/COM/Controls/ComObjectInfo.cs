using System;
using System.Collections.Generic;
using System.Diagnostics;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Extensions;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;

namespace SpyStudio.COM.Controls
{
    public class ComObjectInfo : IInfo
    {
        #region Implementation of IInfo

        public uint TraceID { get; protected set; }
        public HashSet<CallEventId> CallEventIds { get; protected set; }
        public HashSet<DeviareTraceCompareItem> CompareItems { get; protected set; }
        public bool IsNull { get; protected set; }

        #endregion

        #region Properties

        public string Clsid { get; protected set; }
        public string Description { get; protected set; }
        public string ServerPath { get; protected set; }
        public ulong ReturnValue { get; protected set; }
        public double Time { get; protected set; }
        public string Result { get; protected set; }
        public bool Success { get; protected set; }
        public uint Count { get; protected set; }
        public HashSet<string> CallerModules { get; protected set; }

        #endregion

        #region Instantiation

        public static ComObjectInfo From(CallEvent aCallEvent)
        {
            var info = new ComObjectInfo();
            
            info.TraceID = aCallEvent.TraceId;
            info.CallEventIds.Add(aCallEvent.EventId);
            info.Clsid = aCallEvent.GetClsid();
            info.Description = aCallEvent.GetDescription();
            info.ServerPath = aCallEvent.GetServer();
            info.ReturnValue = aCallEvent.RetValue;
            info.Time = aCallEvent.Time;
            info.Count = 1;
            info.Result = aCallEvent.Result;
            info.Success = aCallEvent.Success;
            info.CallerModules.Add(aCallEvent.CallModule);
            info.IsNull = false;

            return info;
        }

        public ComObjectInfo()
        {
            CallEventIds = new HashSet<CallEventId>();
            CompareItems = new HashSet<DeviareTraceCompareItem>();
            CallerModules = new HashSet<string>();

            IsNull = true;
        }

        #endregion

        public void MergeWith(ComObjectInfo anotherInfo)
        {
            if (anotherInfo.IsNull)
                return;

            Debug.Assert(IsNull || TraceID == anotherInfo.TraceID, "Tried to merge ComObjectInfos from different traces.");

            IsNull = false;

            TraceID = anotherInfo.TraceID;

            CallEventIds.AddRange(anotherInfo.CallEventIds);
            CompareItems.AddRange(anotherInfo.CompareItems);
            CallerModules.AddRange(anotherInfo.CallerModules);

            Time += anotherInfo.Time;
            Count += anotherInfo.Count;

            Success |= anotherInfo.Success;

            if (Success && !anotherInfo.Success)
                return;

            Description = anotherInfo.Description;
            ServerPath = anotherInfo.ServerPath;
            ReturnValue = anotherInfo.ReturnValue;
            Clsid = anotherInfo.Clsid;
            Result = anotherInfo.Result;
        }
    }
}