using System.Collections.Generic;
using System.ComponentModel;
using Nektra.Deviare2;
using SpyStudio.Tools;

namespace SpyStudio.GroupManager
{
    public class MonitorGroup
    {
        public MonitorGroup(string name, int count, object obj)
        {
            Name = name;
            Count = count;
            Object = obj;
        }

        public string Name { get; set; }
        public int Count { get; set; }
        public object Object { get; set; }
    }

    public class MonitorFunction
    {
        public MonitorFunction(string name, MonitorGroup group, object obj)
        {
            Name = name;
            Group = group;
            Object = obj;
        }

        public string Name { get; set; }
        public MonitorGroup Group { get; set; }
        public object Object { get; set; }
    }


    public class FunctionScanner
    {
        public enum MonitorGroupType
        {
            DeviareDbData,
            SpyStudioData,
            LoadedData
        }

        public class MonitorModuleEventArgs
        {
            public MonitorModuleEventArgs(MonitorGroup[] modules, MonitorGroupType dataType)
            {
                Modules = modules;
                DataType = dataType;
            }

            public MonitorModuleEventArgs(MonitorGroupType dataType)
            {
                DataType = dataType;
            }

            public MonitorGroup[] Modules { get; set; }
            public MonitorGroupType DataType { get; set; }
        }

        public class MonitorFunctionEventArgs
        {
            public MonitorFunctionEventArgs(MonitorFunction[] functions, MonitorGroupType dataType)
            {
                Functions = functions;
                DataType = dataType;
            }

            public MonitorFunctionEventArgs(MonitorGroupType dataType)
            {
                Functions = null;
                DataType = dataType;
            }

            public MonitorFunction[] Functions { get; set; }
            public MonitorGroupType DataType { get; set; }
        }

        private readonly NktSpyMgr _spyMgr;
        private BackgroundWorker _worker;
        private readonly int _platformBits;

        public delegate void MonitorModuleHandler(object sender, MonitorModuleEventArgs e);

        public delegate void MonitorFunctionHandler(object sender, MonitorFunctionEventArgs e);

        public event MonitorModuleHandler MonitorModuleScanStart;
        public event MonitorModuleHandler MonitorModuleScanned;
        public event MonitorModuleHandler MonitorModuleScanEnd;

        public event MonitorFunctionHandler MonitorFunctionScanStart;
        public event MonitorFunctionHandler MonitorFunctionScanned;
        public event MonitorFunctionHandler MonitorFunctionScanEnd;

        public FunctionScanner(NktSpyMgr spyMgr)
        {
            _spyMgr = spyMgr;
            MaxItemsPerEvent = 50;
            _platformBits = DeviareTools.GetPlatformBits(_spyMgr);
        }

        public int MaxItemsPerEvent { get; set; }

        public void ScanHooks()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += ScanHooksWorker;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync();
        }

        public void ScanModules()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += ScanModulesWorker;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync();
        }

        public void ScanFunctions(MonitorGroup[] modules)
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += ScanFunctionsWorker;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerAsync(modules);
        }

        #region Workers

        private void ScanHooksWorker(object sender, DoWorkEventArgs e)
        {
            return;

            //XmlDocument hooks = HookXml.GetHooksXml();
            //if (hooks == null)
            //    return;

            //var processedModules = new Dictionary<string, MonitorGroup>();
            //var modules = new List<MonitorGroup>();
            //var functions = new List<MonitorFunction>();

            //XmlNodeList functionList = hooks.SelectNodes("/hooks/hook");
            //Debug.Assert(functionList != null, "functionList != null");
            //foreach (XmlNode h in functionList)
            //{
            //    MonitorGroup group;
            //    var hook = new DeviareHook(h, _spyMgr);

            //    if(!processedModules.TryGetValue(hook.Group.ToLower(), out group))
            //    {
            //        group = new MonitorGroup(hook.Group, 0, null);
            //        processedModules[hook.Group.ToLower()] = group;
            //        modules.Add(group);
            //    }
            //    else
            //    {
            //        group.Count++;
            //    }

            //    functions.Add(new MonitorFunction(hook.Function, group, hook));
            //    if(functions.Count > MaxItemsPerEvent)
            //    {
            //        if (MonitorFunctionScanned != null)
            //            MonitorFunctionScanned(this,
            //                                   new MonitorFunctionEventArgs(functions.ToArray(),
            //                                                                MonitorGroupType.SpyStudioData));
            //        functions.Clear();
            //    }
            //}
            //if (functions.Count > 0)
            //{
            //    if (MonitorFunctionScanned != null)
            //        MonitorFunctionScanned(this,
            //                         new MonitorFunctionEventArgs(functions.ToArray(),
            //                                                    MonitorGroupType.SpyStudioData));
            //}
            //if(processedModules.Count > 0)
            //{
            //    if (MonitorModuleScanned != null)
            //    {
            //        MonitorModuleScanned(this,
            //                             new MonitorModuleEventArgs(modules.ToArray(),
            //                                                        MonitorGroupType.SpyStudioData));
            //    }
            //}

            //var contexts = new ParamHandlerManager();
            //contexts.ParseContexts(hooks);
        }

        private void ScanModulesWorker(object sender, DoWorkEventArgs e)
        {
            NktDbModulesEnum modEnum = _spyMgr.DbModules(_platformBits);
            if (modEnum == null)
                return;
            int i = 0;

            if (MonitorModuleScanStart != null)
                MonitorModuleScanStart(this, new MonitorModuleEventArgs(MonitorGroupType.DeviareDbData));

            var modules = new List<MonitorGroup>();
            foreach (NktDbModule mod in modEnum)
            {
                modules.Add(new MonitorGroup(mod.Name, mod.DbFunctions().Count, mod));

                if (++i >= MaxItemsPerEvent)
                {
                    i = 0;
                    if (MonitorModuleScanned != null)
                    {
                        MonitorModuleScanned(this,
                                             new MonitorModuleEventArgs(modules.ToArray(),
                                                                        MonitorGroupType.DeviareDbData));
                    }
                    modules.Clear();
                }
            }
            if (i != 0)
            {
                if (MonitorModuleScanned != null)
                    MonitorModuleScanned(this,
                                         new MonitorModuleEventArgs(modules.ToArray(), MonitorGroupType.DeviareDbData));
            }

            if (MonitorModuleScanEnd != null)
                MonitorModuleScanEnd(this, new MonitorModuleEventArgs(MonitorGroupType.DeviareDbData));
        }

        private void ScanFunctionsWorker(object sender, DoWorkEventArgs e)
        {
            var modules = (MonitorGroup[]) e.Argument;
            int i = 0;

            if (MonitorFunctionScanStart != null)
                MonitorFunctionScanStart(this, new MonitorFunctionEventArgs(MonitorGroupType.DeviareDbData));

            foreach (MonitorGroup modGroup in modules)
            {
                var module = (NktDbModule) modGroup.Object;
                NktDbObjectsEnum fncEnum = module.DbFunctions();
                var functions = new List<MonitorFunction>();
                foreach (NktDbObject fnc in fncEnum)
                {
                    functions.Add(new MonitorFunction(fnc.Name, modGroup, fnc));
                    if (++i >= MaxItemsPerEvent)
                    {
                        i = 0;
                        if (MonitorFunctionScanned != null)
                        {
                            MonitorFunctionScanned(this,
                                                   new MonitorFunctionEventArgs(functions.ToArray(),
                                                                                MonitorGroupType.DeviareDbData));
                        }
                        functions.Clear();
                    }
                }
                if (i != 0)
                {
                    if (MonitorFunctionScanned != null)
                        MonitorFunctionScanned(this,
                                               new MonitorFunctionEventArgs(functions.ToArray(),
                                                                            MonitorGroupType.DeviareDbData));
                }
            }

            if (MonitorFunctionScanEnd != null)
                MonitorFunctionScanEnd(this, new MonitorFunctionEventArgs(MonitorGroupType.DeviareDbData));
        }
        #endregion

    }
}