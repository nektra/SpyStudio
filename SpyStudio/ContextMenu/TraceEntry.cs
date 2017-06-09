using System.Drawing;
using SpyStudio.Tools;

namespace SpyStudio.ContextMenu
{
    public interface ITraceEntry : IEntry
    {
        bool IsCom { get; set; }
        bool IsWindow { get; set; }
        bool IsFile { get; set; }
        bool IsDirectory { get; set; }
        bool IsRegistry { get; set; }
        bool IsValue { get; set; }
        bool IsQueryAttributes { get; set; }
        CallEventId EventId { get; }
        bool Critical { get; }
        int Priority { get; }
        int Depth { get; }
        Color Color { get; }
        Color ColorSummary { get; }
        string Text { get; }
    }

    public class TraceEntryTools
    {
        static public void FillITraceInterpreterEntry(CallEvent ev, ITraceEntry traceEntry)
        {
            traceEntry.IsCom = ev.IsCom;
            traceEntry.IsWindow = ev.IsWindow;
            traceEntry.IsFile = ev.IsFileSystem;
            traceEntry.IsDirectory = FileSystemEvent.IsDirectory(ev);
            traceEntry.IsRegistry = ev.IsRegistry;
            traceEntry.IsValue = RegQueryValueEvent.IsValue(ev);
            traceEntry.IsQueryAttributes = FileSystemEvent.IsQueryAttributes(ev);
        }
    }
}