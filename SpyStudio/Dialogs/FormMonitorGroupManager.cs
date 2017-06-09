using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using Nektra.Deviare2;
using SpyStudio.FunctionPropertyGrid;
using SpyStudio.GroupManager;
using SpyStudio.Hooks;

namespace SpyStudio.Dialogs
{
    public partial class FormMonitorGroupManager : Form
    {
        readonly NktSpyMgr _spyMgr;
        FunctionScanner _modScanner;
        private readonly DataTable _devDbModules = new DataTable("DeviareDbModules");
        private readonly DataTable _devDbFunctions = new DataTable("DeviareDbFunctions");
        private readonly DataTable _spyStudioModules = new DataTable("SpyStudioModules");
        private readonly DataTable _spyStudioFunctions = new DataTable("SpyStudioFunctions");
        private readonly Dictionary<string, DbFunctionListItem> _deviareFunctionMap = new Dictionary<string, DbFunctionListItem>();
        private readonly Dictionary<string, DbFunctionListItem> _spyStudioFunctionMap = new Dictionary<string, DbFunctionListItem>();
        private uint _objId = 0;
        private readonly Dictionary<uint, Object> _objectMap = new Dictionary<uint, object>();
        

        public FormMonitorGroupManager(NktSpyMgr spyMgr)
        {
            _spyMgr = spyMgr;
            InitializeComponent();

            _devDbModules.Columns.Add("ModuleName");
            _devDbModules.Columns.Add("Count");
            _devDbModules.Columns.Add("Complete");
            _devDbModules.Columns.Add("Object");

            _devDbFunctions.Columns.Add("ModuleName");
            _devDbFunctions.Columns.Add("Function");
            _devDbFunctions.Columns.Add("Object");

            _spyStudioModules.Columns.Add("ModuleName");
            _spyStudioModules.Columns.Add("Count");
            _spyStudioModules.Columns.Add("Complete");
            _spyStudioModules.Columns.Add("Object");

            _spyStudioFunctions.Columns.Add("ModuleName");
            _spyStudioFunctions.Columns.Add("Function");
            _spyStudioFunctions.Columns.Add("Object");
        }

        private void MonitorGroupManagerDialogLoad(object sender, EventArgs e)
        {
            _modScanner = new FunctionScanner(_spyMgr);
            _modScanner.MonitorModuleScanStart += MonitorModuleScanStart;
            _modScanner.MonitorModuleScanned += MonitorModuleScanned;
            _modScanner.MonitorModuleScanEnd += MonitorModuleScanEnd;

            _modScanner.MonitorFunctionScanStart += MonitorFunctionScanStart;
            _modScanner.MonitorFunctionScanned += MonitorFunctionScanned;
            _modScanner.MonitorFunctionScanEnd += MonitorFunctionScanEnd;
            
            _modScanner.ScanModules();
            _modScanner.ScanHooks();
        }
        uint AddObject(Object obj)
        {
            lock(_objectMap)
            {
                _objectMap[_objId] = obj;
                return _objId++;
            }
        }
        Object GetObject(uint objId)
        {
            lock(_objectMap)
            {
                return _objectMap[objId];
            }
        }
        Object GetObject(string objId)
        {
            lock (_objectMap)
            {
                return GetObject(Convert.ToUInt32(objId, CultureInfo.InvariantCulture));
            }
        }

        private delegate void ClearListViewDelegate(ListView listView);

        private delegate void AddModulesDelegate(MonitorGroup[] modules);

        #region DeviareControls
        void AddDeviareModules(MonitorGroup[] modules)
        {
            lock (_devDbModules)
            {
                foreach (var module in modules)
                {
                    _devDbModules.Rows.Add(new object[] { module.Name, module.Count, AddObject(module) });
                }
            }
            _modScanner.ScanFunctions(modules);

            AddDeviareModulesToControl(modules);
        }

        void AddDeviareFunctions(MonitorFunction[] functions)
        {
            lock (_devDbFunctions)
            {
                foreach (var function in functions)
                {
                    _devDbFunctions.Rows.Add(new object[] { function.Group.Name, function.Name, AddObject(function) });
                }
            }
        }
        void AddDeviareModulesToControl(MonitorGroup[] modules)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new AddModulesDelegate(AddDeviareModulesToControl), new Object[] { modules });
            }
            else
            {
                listViewDeviareModules.BeginUpdate();
                foreach (var mod in modules)
                {
                    var item = new GroupListItem(mod.Name, mod.Count) { Tag = mod };
                    listViewDeviareModules.Items.Add(item);
                }
                listViewDeviareModules.EndUpdate();
                Application.DoEvents();
            }
        }
        private void DeviareModulesSelectionChanged(object sender, EventArgs e)
        {
            UpdateDeviareModuleSelection();
        }
        private void UpdateDeviareModuleSelection()
        {
            var modules = new List<MonitorGroup>();

            listViewDeviareModuleFunctions.Items.Clear();

            var filter = "ModuleName IN ('";

            bool first = true;
            foreach (ListViewItem item in listViewDeviareModules.SelectedItems)
            {
                if (first)
                    first = false;
                else
                {
                    filter += ", ";
                }
                var module = (MonitorGroup)item.Tag;
                modules.Add(module);
                filter += module.Name + "'";
            }
            filter += ")";

            if (modules.Count > 0)
            {
                lock (_devDbFunctions)
                {
                    var dataView = new DataView(_devDbFunctions) { RowFilter = filter };
                    _deviareFunctionMap.Clear();

                    listViewDeviareModuleFunctions.BeginUpdate();
                    for (int i = 0; i < dataView.Count; i++)
                    {
                        var row = dataView[i];
                        var function = row["Function"].ToString();
                        DbFunctionListItem item;

                        string functionShortName = GetShortFunctionName(function);

                        if (!_deviareFunctionMap.TryGetValue(functionShortName, out item))
                        {
                            item = new DbFunctionListItem(functionShortName, function, (string)row["Object"]);
                            listViewDeviareModuleFunctions.Items.Add(item);
                            _deviareFunctionMap[functionShortName] = item;
                        }
                        else
                        {
                            item.AddFunction(function, (string) row["Object"]);
                        }
                    }
                    listViewDeviareModuleFunctions.EndUpdate();
                }
            }
            //_modScanner.ScanFunctions(modules.ToArray());
        }
        #endregion
        #region SpyStudioControls

        private void AddSpyStudioModulesToControl(MonitorGroup[] modules)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new AddModulesDelegate(AddSpyStudioModulesToControl), new Object[] { modules });
            }
            else
            {
                listViewSpyStudioGroups.BeginUpdate();
                foreach (var mod in modules)
                {
                    var item = new GroupListItem(mod.Name, mod.Count) { Tag = mod };
                    listViewSpyStudioGroups.Items.Add(item);
                }
                listViewSpyStudioGroups.EndUpdate();
                Application.DoEvents();
            }
        }
        private void AddSpyStudioFunctions(MonitorFunction[] functions)
        {
            lock (_spyStudioFunctions)
            {
                foreach (var function in functions)
                {
                    _spyStudioFunctions.Rows.Add(new object[] { function.Group.Name, function.Name, AddObject(function) });
                }
            }
        }

        private void AddSpyStudioModules(MonitorGroup[] modules)
        {
            lock (_spyStudioModules)
            {
                foreach (var module in modules)
                {
                    _spyStudioModules.Rows.Add(new object[] { module.Name, module.Count, AddObject(module) });
                }
            }

            AddSpyStudioModulesToControl(modules);
        }
        private void SpyStudioGroupSelectionChanged(object sender, EventArgs e)
        {
            UpdateSpyStudioModuleSelection();
        }
        private void SpyStudioFunctionSelectionChanged(object sender, EventArgs e)
        {
            if (listViewSpyStudioFunctions.SelectedItems.Count != 1)
            {
                propertyGridFunction.SelectedObject = null;
            }
            else
            {
                var item = (DbFunctionListItem) listViewSpyStudioFunctions.SelectedItems[0];
                Debug.Assert(item != null);
                var hook = (DeviareHook)((MonitorFunction)GetObject(item.MainObject)).Object;
                Debug.Assert(hook != null);

                //var obj = new CustomObjectType
                //              {
                //                  Name = "Foo",
                //                  Properties =
                //                      {
                //                          new CustomProperty {Name = "Bar", Type = typeof (int), Desc = "I'm a bar"},
                //                          new CustomProperty
                //                              {Name = "When", Type = typeof (DateTime), Desc = "When it happened"},
                //                      }
                //              };
                //Application.Run(new Form { Controls = { new PropertyGrid { SelectedObject = obj, Dock = DockStyle.Fill } } });

                //propertyGridFunction.SelectedObject = obj;
                propertyGridFunction.SelectedObject = new DeviareFunction(hook);
            }
        }
        private void UpdateSpyStudioModuleSelection()
        {
            var modules = new List<MonitorGroup>();

            listViewSpyStudioFunctions.Items.Clear();

            var filter = "ModuleName IN ('";

            bool first = true;
            foreach (ListViewItem item in listViewSpyStudioGroups.SelectedItems)
            {
                if (first)
                    first = false;
                else
                {
                    filter += ", ";
                }
                var module = (MonitorGroup)item.Tag;
                modules.Add(module);
                filter += module.Name + "'";
            }
            filter += ")";

            if (modules.Count > 0)
            {
                lock (_spyStudioFunctions)
                {
                    var dataView = new DataView(_spyStudioFunctions) { RowFilter = filter };
                    _spyStudioFunctionMap.Clear();

                    listViewSpyStudioFunctions.BeginUpdate();
                    for (int i = 0; i < dataView.Count; i++)
                    {
                        var row = dataView[i];
                        var function = row["Function"].ToString();
                        DbFunctionListItem item;

                        string functionShortName = GetShortFunctionName(function);

                        if (!_spyStudioFunctionMap.TryGetValue(functionShortName, out item))
                        {
                            item = new DbFunctionListItem(functionShortName, function, (string) row["Object"]);
                            listViewSpyStudioFunctions.Items.Add(item);
                            _spyStudioFunctionMap[functionShortName] = item;
                        }
                        else
                        {
                            item.AddFunction(function, (string) row["Object"]);
                        }
                    }
                    listViewSpyStudioFunctions.EndUpdate();
                }
            }
            //_modScanner.ScanFunctions(modules.ToArray());
        }
        #endregion


        private delegate void AddFunctionsDelegate(MonitorFunction[] functions);


        private void MonitorModuleScanStart(object sender, FunctionScanner.MonitorModuleEventArgs eventArgs)
        {
            //switch (eventArgs.DataType)
            //{
            //    case FunctionScanner.MonitorGroupType.DeviareDbData:
            //        ClearDeviareDbModules();
            //        break;
            //    case FunctionScanner.MonitorGroupType.SpyStudioData:
            //    case FunctionScanner.MonitorGroupType.LoadedData:
            //        break;
            //}
        }
        private void MonitorModuleScanned(object sender, FunctionScanner.MonitorModuleEventArgs eventArgs)
        {
            switch (eventArgs.DataType)
            {
                case FunctionScanner.MonitorGroupType.DeviareDbData:
                    AddDeviareModules(eventArgs.Modules);
                    break;
                case FunctionScanner.MonitorGroupType.SpyStudioData:
                    AddSpyStudioModules(eventArgs.Modules);
                    break;
                case FunctionScanner.MonitorGroupType.LoadedData:
                    break;
            }
        }

        private void MonitorModuleScanEnd(object sender, FunctionScanner.MonitorModuleEventArgs eventArgs)
        {
        }
        private void MonitorFunctionScanStart(object sender, FunctionScanner.MonitorFunctionEventArgs eventArgs)
        {
            //switch (eventArgs.DataType)
            //{
            //    case FunctionScanner.MonitorGroupType.DeviareDbData:
            //        ClearDeviareDbFunctions();
            //        break;
            //    case FunctionScanner.MonitorGroupType.SpyStudioData:
            //    case FunctionScanner.MonitorGroupType.LoadedData:
            //        break;
            //}
        }
        private void MonitorFunctionScanned(object sender, FunctionScanner.MonitorFunctionEventArgs eventArgs)
        {
            switch (eventArgs.DataType)
            {
                case FunctionScanner.MonitorGroupType.DeviareDbData:
                    AddDeviareFunctions(eventArgs.Functions);
                    break;
                case FunctionScanner.MonitorGroupType.SpyStudioData:
                    AddSpyStudioFunctions(eventArgs.Functions);
                    break;
                case FunctionScanner.MonitorGroupType.LoadedData:
                    break;
            }
        }
        private void MonitorFunctionScanEnd(object sender, FunctionScanner.MonitorFunctionEventArgs eventArgs)
        {
        }

        public static string GetShortFunctionName(string function)
        {
            string functionShortName;
            if (function.EndsWith("A") || function.EndsWith("W"))
            {
                functionShortName = function.Substring(0, function.Length - 1);
            }
            else
            {
                functionShortName = function;
            }
            return functionShortName;
        }

    }
    public class GroupListItem : ListViewItem
    {
        private int _count;

        public GroupListItem(string name)
            : base(name)
        {
            _count = 0;
            SubItems.Add("0");
        }
        public GroupListItem(string name, int count)
            : base(name)
        {
            _count = count;
            SubItems.Add(_count.ToString(CultureInfo.InvariantCulture));
        }
        int Count
        {
            get { return _count; }
            set
            {
                _count = value;
                SubItems[1].Text = _count.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
    public class DbFunctionListItem : ListViewItem
    {
        private readonly Dictionary<string, string> _functionMap = new Dictionary<string, string>();
 
        public DbFunctionListItem(string displayName, string functionName, string objId)
            : base(displayName)
        {
            _functionMap[functionName] = objId;
            MainObject = objId;
        }
        public void AddFunction(string functionName, string objId)
        {
            _functionMap[functionName] = objId;
        }

        public string DisplayName { get; set; }
        public string MainObject { get; set; }

        public Dictionary<string, string> GetObjects()
        {
            return _functionMap;
        }
    }
}
