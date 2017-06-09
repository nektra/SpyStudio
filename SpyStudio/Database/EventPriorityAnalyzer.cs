using System;
using System.Collections.Generic;
using SpyStudio.COM;
using SpyStudio.Tools;

namespace SpyStudio.Database
{
    class EventPriorityAnalyzer
    {
        private readonly EventDatabaseMgr _db;
        private readonly Dictionary<string, int> _functionPrioritiesSuccess = new Dictionary<string, int>
                                                                           {
                                                                               {"CreateProcess", 1},
                                                                               {"ProcessStarted", 1},
                                                                               {"Process Start", 1},
                                                                               {"Process Create", 1},
                                                                               //{"ExceptionContinue", 4},
                                                                               {"RaiseException", 1},
                                                                               {"RaiseExceptionStatus", 1},
                                                                               {"OpenService", 2},
                                                                               {"CreateService", 1},
                                                                               //{"CreateWindow", 1},
                                                                               {"DestroyWindow", 4},
                                                                               {"CreateDialog", 1},
                                                                               {"CoCreate", 4},
                                                                               {"SetSystemEnvironmentValue", 2},
                                                                               {"SetEnvironmentVariable", 4},
                                                                           };
        private readonly Dictionary<string, int> _functionPrioritiesError = new Dictionary<string, int>
                                                                           {
                                                                               {"CreateProcess", 1},
                                                                               {"ProcessStarted", 1},
                                                                               {"Process Create", 1},
                                                                               //{"ExceptionContinue", 4},
                                                                               {"RaiseException", 1},
                                                                               {"RaiseExceptionStatus", 1},
                                                                               {"OpenService", 1},
                                                                               {"CreateService", 1},
                                                                               //{"CreateWindow", 1},
                                                                               {"DestroyWindow", 4},
                                                                               {"CreateDialog", 1},
                                                                               //{"CoCreate", 2},
                                                                               //{"LoadLibrary", 3},
                                                                               //{"Load Image", 3},
                                                                               {"FindResource", 4},
                                                                               {"LoadResource", 4},
                                                                               {"SetSystemEnvironmentValue", 2},
                                                                               {"SetEnvironmentVariable", 4},
                                                                           };

        private readonly HashSet<string> _functionException = new HashSet<string>
                                                                 {
                                                                     "RaiseException",
                                                                     "RaiseExceptionStatus"                                                                     
                                                                 };
        private readonly HashSet<string> _functionCritical = new HashSet<string>
                                                                 {
                                                                     "RaiseException",
                                                                     "RaiseExceptionStatus"                                                                     
                                                                 };
        private readonly HashSet<string> _functionCriticalError = new HashSet<string>
                                                                 {
                                                                     "RaiseException",
                                                                     "RaiseExceptionStatus",
                                                                     "CreateWindow",
                                                                     "CreateDialog",
                                                                     "OpenService",
                                                                     "CreateService",
                                                                     "CreateProcess"
                                                                 };
        private readonly HashSet<string> _dllsErrorsIgnoredW7Below = new HashSet<string>
                                                                 {
                                                                     "combase.dll",
                                                                 };
        private readonly Dictionary<string, ClsidOpInfo> _clsidCoCreateInfo = new Dictionary<string, ClsidOpInfo>(); 
        private readonly Dictionary<string, ClsidOpInfo> _clsidRegInfo = new Dictionary<string, ClsidOpInfo>(); 

        class FileOpInfo
        {
            public CallEventId EventId { get; set; }
            public int Priority { get; set; }
        }
        class ClsidOpInfo
        {
            public CallEventId EventId { get; set; }
            public int Priority { get; set; }
            public bool Success { get; set; }
        }
        private readonly HashSet<string> _loadedDlls = new HashSet<string>();
        private readonly Dictionary<string, FileOpInfo> _searchedFiles = new Dictionary<string, FileOpInfo>();

        private readonly HashSet<string> _createWindowNotCriticalOnError = new HashSet<string>
                                                                 {
                                                                     "Breadcrumb Parent"
                                                                 };
        private readonly Dictionary<uint, CallEventId> _lastExeptionInThread = new Dictionary<uint, CallEventId>();
        private readonly HashSet<uint> _threadIdsStarted = new HashSet<uint>();

        public EventPriorityAnalyzer(EventDatabaseMgr db)
        {
            _db = db;
        }
        private void AnalyzeFileSystemEventAfter(CallEvent callEvent)
        {
            if (callEvent.Success)
            {
                // first time the dll is loaded we add some priority
                if (callEvent.Type == HookType.LoadLibrary)
                {
                    var dllLower = callEvent.Params[0].Value.ToLower();
                    lock (_loadedDlls)
                    {
                        if (!_loadedDlls.Contains(dllLower))
                        {
                            _loadedDlls.Add(dllLower);
                            callEvent.Priority = 4;
                        }
                    }
                }

                bool resetOpInfo = true;
                var filepart = FileSystemEvent.GetFilepart(callEvent).ToLower();
                lock (_searchedFiles)
                {
                    FileOpInfo fileOpInfo;
                    if (_searchedFiles.TryGetValue(filepart, out fileOpInfo))
                    {
                        // there was a failure event associated to this file, update the priority of this event
                        if (fileOpInfo != null)
                        {
                            _db.UpdateEventProperties(fileOpInfo.EventId,
                                                      new EventDatabaseMgr.EventProperties
                                                          {Priority = fileOpInfo.Priority});
                        }
                        else
                            resetOpInfo = false;
                    }
                    if (resetOpInfo)
                        _searchedFiles[filepart] = null;
                }
            }
            else
            {
                // file system events that fail -> mark them as important and remove the mark if there is any
                // call to the file that succeeds (e.g.: a file is searched in different places and finally it is 
                // found is a general success, but if the file isn't located in any place we keep the priority mark)
                if (callEvent.IsFileSystem)
                {
                    var filepart = FileSystemEvent.GetFilepart(callEvent).ToLower();
                    lock (_searchedFiles)
                    {
                        FileOpInfo fileOpInfo;
                        if (!_searchedFiles.TryGetValue(filepart, out fileOpInfo) && !filepart.EndsWith(".local") &&
                             !filepart.EndsWith(".dll.dll") && (!PlatformTools.IsW7OrBelow() ||
                             !_dllsErrorsIgnoredW7Below.Contains(filepart)))
                        {
                            _searchedFiles[filepart] = new FileOpInfo
                                                           {
                                                               EventId = callEvent.EventId,
                                                               Priority = callEvent.Priority
                                                           };
                            // OBJECT_NAME_COLLISION (0xc0000035) or devices less important
                            callEvent.Priority = callEvent.RetValue == 0xc0000035 ||
                                                 callEvent.Params[0].Value.StartsWith(@"\Device")
                                                     ? 3
                                                     : 2;
                        }
                    }
                }
            }
        }
        private void UpdateInfo(CallEvent callEvent, string iid, int errorPriority, Dictionary<string, ClsidOpInfo> infos)
        {
            if (!string.IsNullOrEmpty(iid))
            {
                lock (infos)
                {
                    ClsidOpInfo clsidOpInfo;
                    if (infos.TryGetValue(iid, out clsidOpInfo))
                    {
                        if (callEvent.Success && !clsidOpInfo.Success)
                        {
                            _db.UpdateEventProperties(clsidOpInfo.EventId,
                                                      new EventDatabaseMgr.EventProperties
                                                      {
                                                          EventFlags = callEvent.EventFlags,
                                                          Priority = clsidOpInfo.Priority
                                                      });

                            clsidOpInfo.Success = true;
                            clsidOpInfo.EventId = null;
                        }
                    }
                    else
                    {
                        clsidOpInfo = new ClsidOpInfo();
                        if (callEvent.Success)
                        {
                            clsidOpInfo.EventId = null;
                            clsidOpInfo.Success = true;
                        }
                        else
                        {
                            clsidOpInfo.EventId = callEvent.EventId;
                            clsidOpInfo.Success = false;
                            clsidOpInfo.Priority = callEvent.Priority;
                            callEvent.Priority = errorPriority;
                        }
                        infos[iid] = clsidOpInfo;
                    }
                }
            }
        }
        private void AnalyzeRegistryEventAfter(CallEvent callEvent)
        {
            if (callEvent.Type == HookType.RegOpenKey || callEvent.Type == HookType.RegEnumerateKey)
            {
                var iid = ComServerInfoMgr.GetIID(RegOpenKeyEvent.GetKey(callEvent)).ToLower();
                UpdateInfo(callEvent, iid, 3, _clsidRegInfo);
            }
        }

        private void AnalyzeCoCreateEventAfter(CallEvent callEvent)
        {
            if (callEvent.Type == HookType.GetClassObject || callEvent.Type == HookType.CoCreate)
            {
                var iid = callEvent.GetClsid().ToLower();
                UpdateInfo(callEvent, iid, 2, _clsidCoCreateInfo);
                
                // when success remove any prioritized registry operation since the object was finally created
                if(callEvent.Success)
                    UpdateInfo(callEvent, iid, 2, _clsidRegInfo);
            }
        }

        private void AnalyzeEventBefore(CallEvent callEvent)
        {
            int priority;
            lock (_lastExeptionInThread)
            {
                if (callEvent.Function == "ExceptionContinue")
                {
                    if (!_threadIdsStarted.Contains(callEvent.Tid))
                    {
                        callEvent.Function = "ThreadStarted";
                        _threadIdsStarted.Add(callEvent.Tid);
                    }
                    else if (_lastExeptionInThread.ContainsKey(callEvent.Tid))
                    {
                        var eventId = _lastExeptionInThread[callEvent.Tid];
                        callEvent.Critical = false;
                        _db.UpdateEventProperties(eventId,
                                                  new EventDatabaseMgr.EventProperties
                                                      {EventFlags = callEvent.EventFlags, Priority = 3});
                    }
                }
            }
            if (_functionPrioritiesSuccess.TryGetValue(callEvent.Function, out priority))
            {
                callEvent.Priority = priority;
            }

            if (_functionCritical.Contains(callEvent.Function))
            {
                callEvent.Critical = true;
                if (_functionException.Contains(callEvent.Function))
                {
                    callEvent.Success = false;
                    if (callEvent.ParamMain.StartsWith("THINAPP"))
                    {
                        callEvent.Critical = false;
                        callEvent.Priority = 3;
                    }
                }
            }
            else
            {
                lock (_lastExeptionInThread)
                {
                    _lastExeptionInThread.Remove(callEvent.Tid);
                }
            }
            if (callEvent.Critical)
            {
                lock (_lastExeptionInThread)
                {
                    _lastExeptionInThread[callEvent.Tid] = callEvent.EventId;
                }
            }
        }
        private void AnalyzeEventAfter(CallEvent callEvent)
        {
            if (callEvent.Type == HookType.CreateWindow && callEvent.Function == "CreateWindow")
            {
                if (callEvent.Success)
                {
                    if(CreateWindowEvent.IsImportantWindow(callEvent))
                    {
                        callEvent.Priority = 1;
                    }
                }
                else
                {
                    if (_createWindowNotCriticalOnError.Contains(callEvent.ParamMain))
                    {
                        callEvent.Priority = 4;
                    }
                    else
                    {
                        if (CreateWindowEvent.IsImportantWindow(callEvent))
                        {
                            callEvent.Priority = 1;
                            callEvent.Critical = true;
                        }
                        else
                            callEvent.Priority = 2;
                    }
                }
            }
            else
            {
                int priority;
                if (callEvent.Success)
                {
                    if (_functionPrioritiesSuccess.TryGetValue(callEvent.Function, out priority))
                    {
                        callEvent.Priority = priority;
                    }
                    if (_functionCritical.Contains(callEvent.Function))
                        callEvent.Critical = true;
                }
                else
                {
                    if (_functionPrioritiesError.TryGetValue(callEvent.Function, out priority))
                    {
                        callEvent.Priority = priority;
                    }
                    else if (callEvent.Function.EndsWith("DllGetClassObject"))
                    {
                        callEvent.Priority = 4;
                    }
                    if (_functionCriticalError.Contains(callEvent.Function))
                        callEvent.Critical = true;
                }
            }
        }
        public void AnalyzeEvent(CallEvent callEvent)
        {
            if (callEvent.Priority == 0)
            {
                callEvent.Priority = 5;
            }
            if (callEvent.Before)
            {
                AnalyzeEventBefore(callEvent);
            }
            else
            {
                AnalyzeEventAfter(callEvent);
                if (callEvent.IsFileSystem)
                {
                    AnalyzeFileSystemEventAfter(callEvent);
                }
                else if(callEvent.IsRegistry)
                {
                    AnalyzeRegistryEventAfter(callEvent);
                }
                else if(callEvent.IsCom)
                {
                    AnalyzeCoCreateEventAfter(callEvent);
                }
            }
        }
        public void Clear()
        {
            lock (_loadedDlls)
            {
                _loadedDlls.Clear();
            }
            lock (_searchedFiles)
            {
                _searchedFiles.Clear();
            }
            lock (_lastExeptionInThread)
            {
                _lastExeptionInThread.Clear();
            }
            lock (_clsidRegInfo)
            {
                _clsidRegInfo.Clear();
            }
            lock (_threadIdsStarted)
            {
                _threadIdsStarted.Clear();
            }
        }
    }
}
