using System;
using System.Collections.Generic;
using SpyStudio.Hooks;
using SpyStudio.Tools;

namespace SpyStudio.CommandLine
{
    public class ConsoleModeMgr
    {
        private HookMgr _hookMgr;
        private List<ConsoleModeCmd> _cmdList;
        private bool _verbose;
        private string _filename;
        private bool _initialized;

        public ConsoleModeMgr()
        {
            IsServer = false;
        }
        public void InitializeDeviare()
        {
            if(!_initialized)
            {
                _initialized = true;
                _hookMgr = new HookMgr();
                _hookMgr.Initialize(null);
            }
        }

        public bool IsServer { get; set; }

        public string Groups { get; set; }

        public bool Execute(string[] args, out string error, out string helpString)
        {
            bool shutdown;
            return Execute(args, out error, out helpString, out shutdown);
        }

        public bool ExecuteCommands(string groups, out string error, out bool shutdown)
        {
            bool ret = true;

            shutdown = false;
            error = "";

            if (!string.IsNullOrEmpty(_filename))
            {
                Error.SetOutputToFile(_filename);
                _filename = null;
            }

            if (!string.IsNullOrEmpty(groups))
            {
                groups = groups.Trim(new[] { '\'', '\"' });
                
                if (_verbose)
                {
                    Error.WriteLine("Groups: " + groups);
                }
                var groupArray = groups.Split(new[] { ',' });
                var activeGroups = new HashSet<string>();

                foreach (var g in groupArray)
                {
                    activeGroups.Add(g);
                }

                _hookMgr.ActiveGroups = activeGroups;
            }

            _hookMgr.Verbose = _verbose;
            
            foreach (var cmd in _cmdList)
            {
                if (cmd.Type == ConsoleModeCmdType.Shutdown)
                {
                    if(_verbose)
                        Error.WriteLine("SpyStudio shutting down");
                    shutdown = true;
                    break;
                }

                if (!cmd.Execute(_hookMgr))
                {
                    if (_verbose)
                        Error.WriteLine("Error: " + cmd.Type + (string.IsNullOrEmpty(cmd.Parameters) ? "" : (" " + cmd.Parameters + " ")) + error);
                    error = cmd.Error;
                    ret = false;
                    break;
                }
                if(_verbose)
                {
                    Error.WriteLine("Successfull executed: " + cmd.Type + (string.IsNullOrEmpty(cmd.Parameters) ? "" : (" " + cmd.Parameters + " ")));
                }
            }
            return ret;
        }

        bool ExecuteRemoteCommands(string[] args, out string error, out string helpString, out bool shutdown)
        {
            shutdown = false;
            string groups;
            bool ret = ParseParameters(args, out groups, out error, out helpString);
            if (!ret)
            {
                Error.WriteLine("Error " + error + "\n" + helpString);
            }
            else
            {
                ret = ExecuteCommands(groups, out error, out shutdown);
            }
            return ret;
        }
        /// <summary>
        /// Execute command line
        /// </summary>
        /// <param name="args"></param>
        /// <param name="error"></param>
        /// <param name="helpString"></param>
        /// <param name="shutdown"></param>
        /// <returns></returns>
        public bool Execute(string[] args, out string error, out string helpString, out bool shutdown)
        {
            shutdown = true;
            string groups;
            bool ret = ParseParameters(args, out groups, out error, out helpString);
            if (!ret)
            {
                Error.WriteLine("Error " + error + "\n" + helpString);
            }
            if (!IsServer)
            {
                var pipeClient = new NamedPipeClient();

                ret = pipeClient.TryToConnect();
                if(!ret)
                {
                    error = @"Cannot connect to the server, you must execute 'SpyStudio -server'";
                    Error.WriteLine(error);
                    helpString = "";
                }
                else
                {
                    pipeClient.SendParameters(args, out error, out helpString);
                    ret = string.IsNullOrEmpty(error);
                    if (!ret)
                    {
                        Error.WriteLine("Error " + error + "\n" + helpString);
                    }
                }
            }
            else
            {
                InitializeDeviare();

                ret = ExecuteCommands(groups, out error, out shutdown);
                if (ret && !shutdown)
                {
                    Error.WriteLine("Server started");

                    var pipeServer = new NamedPipeServer();
                    pipeServer.ClientRequest += PipeServerOnClientRequest;
                    pipeServer.Start();
                    pipeServer.Join();
                }
                else if (!ret)
                {
                    Error.WriteLine(error);
                }

                _hookMgr.Shutdown();
            }

            return ret;
        }

        private void PipeServerOnClientRequest(object sender, NamedPipeServer.ClientRequestEventArgs clientRequestEventArgs)
        {
            string error, helpString;
            bool shutdown;

            if (_verbose)
            {
                Console.WriteLine("Executing commands from client: " + string.Join(" ", clientRequestEventArgs.Args));
            }
            clientRequestEventArgs.Success = ExecuteRemoteCommands(clientRequestEventArgs.Args, out error, out helpString, out shutdown);
            clientRequestEventArgs.HelpString = helpString;
            clientRequestEventArgs.Error = error;
            clientRequestEventArgs.Shutdown = shutdown;
        }

        bool ParseParameters(string[] args, out string groups, out string error, out string helpString)
        {
            int index = 0;
            bool success = true;
            var procTrim = new[] {'\"', '\'', ' '};
            error = "";
            helpString = "";
            groups = "";
            
            _cmdList = new List<ConsoleModeCmd>();

            while (success && index < args.Length)
            {
                var cmd = args[index];
                switch (cmd)
                {
                        // watch by process name
                    case "-watchprocess":
                    case "-wp":
                        {
                            if(++index == args.Length)
                            {
                                error = cmd + " should be followed by process name";
                                success = false;
                            }
                            else
                            {
                                var procName = args[index].Trim(procTrim);
                                if (string.IsNullOrEmpty(procName))
                                {
                                    error = cmd + " should be followed by process name";
                                    success = false;
                                }
                                else
                                {
                                    _cmdList.Add(new WatchProcessNameCmd(procName));
                                    index++;
                                }
                            }
                            break;
                        }
                        // watch all current user processes
                    case "-watchuser":
                    case "-wu":
                        {
                            ++index;
                            _cmdList.Add(new WatchUserProcessesCmd());
                            break;
                        }
                    case "-groups":
                    case "-g":
                        {
                            if(++index == args.Length)
                            {
                                error = cmd + " should be followed by hook groups";
                                success = false;
                            }
                            else
                            {
                                groups = args[index].Trim(procTrim);
                                while(++index < args.Length)
                                {
                                    if (args[index].StartsWith("-"))
                                        break;
                                    groups += args[index].Trim(procTrim);
                                }
                            }
                            break;
                        }
                        // hook: hook existing processes and executed processes from the watched processes
                    case "-hook":
                    case "-h":
                        {
                            if (++index == args.Length)
                            {
                                error = cmd + " should be followed by a process name";
                                success = false;
                            }
                            else
                            {
                                var procName = args[index++].Trim(procTrim);
                                if (string.IsNullOrEmpty(procName))
                                {
                                    error = cmd + " should be followed by a process name";
                                    success = false;
                                }
                                else
                                {
                                    _cmdList.Add(new HookProcessByNameCmd(procName));
                                }
                            }
                            break;
                        }
                        // execute and hook program
                    case "-executehook":
                    case "-eh":
                        {
                            if (++index == args.Length)
                            {
                                error = cmd + " should be followed by a process path";
                                success = false;
                            }
                            else
                            {
                                var procName = args[index++].Trim(procTrim);
                                if (string.IsNullOrEmpty(procName))
                                {
                                    error = cmd + " should be followed by a process path";
                                    success = false;
                                }
                                else
                                {
                                    _cmdList.Add(new ExecuteAndHookCmd(procName));
                                }
                            }
                            break;
                        }
                    case "-server":
                        {
                            index++;
                            IsServer = true;
                        }
                        break;
                    // save log
                    case "-save":
                        {
                            if (++index == args.Length)
                            {
                                error = cmd + " should be followed by a filename";
                                success = false;
                            }
                            else
                            {
                                var filename = args[index++].Trim(procTrim);
                                if (string.IsNullOrEmpty(filename))
                                {
                                    error = cmd + " should be followed by a filename";
                                    success = false;
                                }
                                else
                                {
                                    _cmdList.Add(new SaveLogCmd(filename));
                                }
                            }
                            break;
                        }
                    case "-verbose":
                    case "-v":
                        {
                            index++;
                            _verbose = true;
                            break;
                        }
                    case "-output":
                    case "-o":
                        {
                            if (++index == args.Length)
                            {
                                error = cmd + " should be followed by a filename";
                                success = false;
                            }
                            else
                            {
                                var filename = args[index++].Trim(procTrim);
                                if (string.IsNullOrEmpty(filename))
                                {
                                    error = cmd + " should be followed by a filename";
                                    success = false;
                                }
                                else
                                {
                                    _filename = filename;
                                }
                            }
                            break;
                        }
                    // stop running instance
                    case "-stop":
                        {
                            index++;
                            _cmdList.Add(new StopCmd());
                            break;
                        }
                    case "-shutdown":
                        {
                            index++;
                            _cmdList.Add(new ShutdownCmd());
                            break;
                        }
                    case "-terminate":
                        {
                            if (++index == args.Length)
                            {
                                error = cmd + " should be followed by process name";
                                success = false;
                            }
                            else
                            {
                                var procName = args[index].Trim(procTrim);
                                if (string.IsNullOrEmpty(procName))
                                {
                                    error = cmd + " should be followed by process name";
                                    success = false;
                                }
                                else
                                {
                                    _cmdList.Add(new TerminateProcessCmd(procName));
                                    index++;
                                }
                            }
                            break;
                        }
                    default:
                        error = cmd + " should be a command";
                        success = false;
                        break;
                }
            }
            if (!success)
                helpString =
                    @"SpyStudio [-server | -watchprocess procName | -watchuser | -groups group1,..., groupN | -stop | 
                           -shutdown | -output filename | -verbose | -save filename | -executehook filename | -hook procName | -terminate procName
-server:                    indicates that this instances will stay running waiting for other commands.
-watchprocess procName:     watch all processes which process name is procName. All process that are created from the watched processes will be hooked.
-watchuser:                 watch all processes owned by current user. All process that are created from the watched processes will be hooked.
-groupsgroup1,..., groupN:  specifies active hook groups. Hook groups are ActiveX, Common Dialogs, Environment, Exceptions, Files, Handles, Internet, Internet Helpers, Localization, Module Handle, Ntdll Strings, Procedure Address, Process, Registry, Resources, Shell, Windows Creation, Windows Hooks, Windows Messages, Windows Properties.
-stop:                      detach all hooks.
-shutdown:                  terminates SpyStudio server.
-output filename:           write output to filename. By default the output is send to Debug Console. You can use DebugView to see the output.
-verbose:                   set verbose mode on.
-save filename:             save generated events to filename.
-executehook filename:      execute filename and hook from startup.
-hook procName:             hook all instances of procName and new executed instances from watched processes.
-terminate procName:        kill all process which process name is procName.";

            return success;
        }
    }
}
