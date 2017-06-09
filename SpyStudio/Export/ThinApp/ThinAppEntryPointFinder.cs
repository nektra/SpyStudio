using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IWshRuntimeLibrary;
using SpyStudio.Database;
using SpyStudio.Hooks;
using SpyStudio.Tools;
using SpyStudio.Trace;
using SpyStudio.Extensions;

namespace SpyStudio.Export.ThinApp
{
    public static class ThinAppEntryPointFinder
    {
        private static readonly Regex RegistryClassRegex =
            new Regex(@"(?:HKEY_CURRENT_USER|HKEY_LOCAL_MACHINE)\\software\\classes\\([^\\]+).*",
                      RegexOptions.IgnoreCase);

        public static List<string> FindProtocolsForExecutable(List<CallEvent> events, string path)
        {
            var ret = new HashSet<string>();

#if false
            foreach (var callEvent in events)
            {
                if (callEvent.IsRegistry && !callEvent.Before)
                {
                    if (callEvent.ParamMain.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        callEvent.ParamDetails.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var match = RegistryClassRegex.Match(callEvent.ParamMain);
                        if (match.Success)
                        {
                            ret.Add(match.Groups[1].ToString());
                        }
                    }
                }
            }
#else
            var temp1 = new List<string>();
            var temp2 = new List<CallEvent>();
            foreach (var callEvent in events)
            {
                if (callEvent.IsRegistry && !callEvent.Before)
                {
                    if (callEvent.ParamMain.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        callEvent.ParamDetails.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        temp1.Add(callEvent.ParamMain);
                        temp2.Add(callEvent);
                    }
                }
            }
            foreach (var callEvent in temp2)
            {
                var match = RegistryClassRegex.Match(callEvent.ParamMain);
                if (match.Success)
                {
                    ret.Add(match.Groups[1].ToString());
                }
            }
#endif
            return ret.ToList();
        }

        public static List<string> FindFiletypesForProtocols(List<CallEvent> events, List<string> protocols)
        {
            var ret = new HashSet<string>();
#if false
            foreach (var protocol in protocols)
            {
                foreach (var callEvent in events)
                {
                    if (callEvent.IsRegistry && !callEvent.Before)
                    {
                        var match = RegistryClassRegex.Match(callEvent.ParamMain);
                        if (match.Success)
                        {
                            if (callEvent.Params.Select(x => x.Value).Any(
                                parameter => parameter.ToLower() == protocol.ToLower()))
                                ret.Add(match.Groups[1].ToString());
                        }
                    }
                }
            }
#else
            var temp = new List<Tuple<string, CallEvent>>();
            foreach (var callEvent in events)
            {
                if (callEvent.IsRegistry && !callEvent.Before)
                    temp.Add(new Tuple<string, CallEvent>(callEvent.ParamMain, callEvent));
            }
            foreach (var protocol in protocols)
            {
                foreach (var callEvent in temp.Select(x => x.Item2))
                {
                    var match = RegistryClassRegex.Match(callEvent.ParamMain);
                    if (match.Success)
                    {
                        if (callEvent.Params.Select(x => x.Value).Any(
                            parameter => parameter.ToLower() == protocol.ToLower()))
                            ret.Add(match.Groups[1].ToString());
                    }
                }
            }
#endif
            return ret.ToList();
        }

        static Dictionary<string, EntryPoint> _entryPoints;
        private static IEnumerable<FileEntry> _files;
        static private BackgroundWorker _worker;

        static private bool CreateEntryPoints()
        {
            foreach (var fileEntry in _files)
            {
                var path = fileEntry.Path;
                if (!path.ToLower().EndsWith(".exe") || _entryPoints.ContainsKey(path))
                    continue;
                var filename = Path.GetFileName(path);
                _entryPoints[path] = new EntryPoint
                {
                    Name = filename,
                    Location = path,
                    FileSystemLocation = fileEntry.FileSystemPath.AsNormalizedPath(),
                    ProductName = fileEntry.Product,
                };

                if (_worker.CancellationPending)
                    return false;
            }
            return true;
        }
        static private void EventsProtocolSearch(object sender, EventsRefreshArgs e)
        {
            var evToReport = e.Events;
            foreach (var entryPoint in _entryPoints)
            {
                entryPoint.Value.Protocols = FindProtocolsForExecutable(evToReport, entryPoint.Value.FileSystemLocation);

                if (_worker.CancellationPending)
                {
                    e.Canceled = true;
                    return;
                }
            }
        }

        static private void EventsFiletypesSearch(object sender, EventsRefreshArgs e)
        {
            var evToReport = e.Events;

            foreach (var fileEntry in _files)
            {
                var path = fileEntry.Path;
                if (!path.ToLower().EndsWith(".exe") || _entryPoints.ContainsKey(path))
                    continue;
                
                var entryPoint = _entryPoints[path];

                entryPoint.FileTypes = FindFiletypesForProtocols(evToReport, entryPoint.Protocols);

                if (_worker.CancellationPending)
                {
                    e.Canceled = true;
                    return;
                }
            }
        }

        public static IEnumerable<EntryPoint> Find(DeviareRunTrace trace, IEnumerable<FileEntry> files,
                                                          BackgroundWorker worker)
        {
            var traceId = trace.TraceId;
#if DEBUG

            var sw = new Stopwatch();
            sw.Start();
#endif

            _files = files;
            _entryPoints = new Dictionary<string, EntryPoint>();
            _worker = worker;

            CreateEntryPoints();

            var data = new EventsReportData(traceId)
                           {
                               EventsToReport = EventType.Registry,
                               ReportBeforeEvents = false,
                               EventResultsIncluded = EventsReportData.EventResult.Success
                           };
            data.EventsReady += EventsProtocolSearch;
            EventDatabaseMgr.GetInstance().RefreshEvents(data);
            data.Event.WaitOne();
#if DEBUG
            var timeProtocol = sw.Elapsed.TotalMilliseconds;
            var prev = sw.Elapsed.TotalMilliseconds;
#endif

            data = new EventsReportData(traceId)
                       {
                           EventsToReport = EventType.Registry,
                           ReportBeforeEvents = false,
                           EventResultsIncluded = EventsReportData.EventResult.Success
                       };
            data.EventsReady += EventsFiletypesSearch;
            EventDatabaseMgr.GetInstance().RefreshEvents(data);
            data.Event.WaitOne();

#if DEBUG
            var timeFileTypes = sw.Elapsed.TotalMilliseconds - prev;
            prev = sw.Elapsed.TotalMilliseconds;
#endif

            var ret = _entryPoints.Values.ToList();
            var shell = new WshShell();

            var list = FileSystemTools.ScanForShortcuts(ret.Select(x => x.FileSystemLocation).ToList(), shell, () => worker.CancellationPending);

#if DEBUG
            var timeShortcuts = sw.Elapsed.TotalMilliseconds - prev;
            prev = sw.Elapsed.TotalMilliseconds;
#endif

            if (worker.CancellationPending)
                return new EntryPoint[0];

            Debug.Assert(list.Count == ret.Count);

            for (var i = 0; i < list.Count; i++)
                ret[i].Shortcuts = list[i];

            ret = ret.OrderByDescending(x => x.GetSuitability()).ToList();

            if (worker.CancellationPending)
                return new EntryPoint[0];

#if DEBUG
            var timeOrder = sw.Elapsed.TotalMilliseconds - prev;
            var totalTime = sw.Elapsed.TotalMilliseconds;

            Error.WriteLine("Total\t" + totalTime + "\tProtocol\t" + timeProtocol + "\tFiletypes\t" + timeFileTypes +
                            "\tScan shortcuts\t" + timeShortcuts + "\tOrder\t" + timeOrder);
#endif

            return ret;
        }
    }
}