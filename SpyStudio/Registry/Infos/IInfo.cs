using System.Collections.Generic;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Tools;

namespace SpyStudio.Registry.Infos
{
    public interface IInfo
    {
        uint TraceID { get; }
        HashSet<CallEventId> CallEventIds { get; }
        HashSet<DeviareTraceCompareItem> CompareItems { get; }
        bool IsNull { get; }
    }
}