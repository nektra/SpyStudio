using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpyStudio.Hooks;

namespace SpyStudio.CommandLine
{
    public enum ConsoleModeCmdType
    {
        WatchProcessName,
        WatchUserProcesses,
        HookProcessByName,
        TerminateProcessByName,
        ExecuteAndHook,
        SaveLog,
        StopAnalysis,
        Shutdown
    }
    public class ConsoleModeCmd
    {
        public ConsoleModeCmd()
        {
        }
        public ConsoleModeCmd(ConsoleModeCmdType type)
        {
            Type = type;
            Parameters = "";
        }
        public ConsoleModeCmd(ConsoleModeCmdType type, string parameters)
        {
            Type = type;
            Parameters = parameters;
        }
        public virtual bool Execute(HookMgr hookMgr)
        {
            return true;
        }

        public string Error { get; set; }
        public ConsoleModeCmdType Type { get; set; }
        public string Parameters { get; set; }
    }

    public class WatchProcessNameCmd : ConsoleModeCmd
    {
        public WatchProcessNameCmd(string parameters)
            : base(ConsoleModeCmdType.WatchProcessName, parameters)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            hookMgr.WatchProcessesByName(Parameters);
            return true;
        }
    }

    public class WatchUserProcessesCmd : ConsoleModeCmd
    {
        public WatchUserProcessesCmd()
            : base(ConsoleModeCmdType.WatchUserProcesses)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            hookMgr.WatchAllUserProcesses();
            return true;
        }
    }
    public class HookProcessByNameCmd : ConsoleModeCmd
    {
        public HookProcessByNameCmd(string parameters)
            : base(ConsoleModeCmdType.HookProcessByName, parameters)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            hookMgr.HookProcessByName(Parameters);
            return true;
        }
    }
    public class ExecuteAndHookCmd : ConsoleModeCmd
    {
        public ExecuteAndHookCmd(string parameters)
            : base(ConsoleModeCmdType.ExecuteAndHook, parameters)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            bool ret = true;
            if (!hookMgr.ExecuteProgramAndHook(Parameters))
            {
                Error = "Cannot execute " + Parameters;
                ret = false;
            }
            return ret;
        }
    }
    public class StopCmd : ConsoleModeCmd
    {
        public StopCmd()
            : base(ConsoleModeCmdType.StopAnalysis)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            hookMgr.StopAnalysis(true);
            return true;
        }
    }
    public class ShutdownCmd : ConsoleModeCmd
    {
        public ShutdownCmd()
            : base(ConsoleModeCmdType.Shutdown)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            return true;
        }
    }
    public class SaveLogCmd : ConsoleModeCmd
    {
        public SaveLogCmd(string parameters)
            : base(ConsoleModeCmdType.SaveLog, parameters)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            bool ret;
            string error;
            hookMgr.DeviareRunTrace.SaveLog(Parameters, out ret, out error);
            if(!ret)
            {
                Error = "Cannot Save log to: " + Parameters + " Error: " + error;
            }
            return ret;
        }
    }
    public class TerminateProcessCmd : ConsoleModeCmd
    {
        public TerminateProcessCmd(string parameters)
            : base(ConsoleModeCmdType.TerminateProcessByName, parameters)
        {
        }
        public override bool Execute(HookMgr hookMgr)
        {
            hookMgr.TerminateProcess(Parameters);
            return true;
        }
    }
}
