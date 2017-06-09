using System;
using System.Collections.Generic;
using System.Linq;
using Nektra.Deviare2;
using System.Diagnostics;
using System.Windows.Forms;

namespace SpyStudio.Hooks
{
    public class HookStateMgr
    {
        /// Represents a hook in a specific process
        public class HookInfo
        {
            readonly uint _processId;
            readonly IntPtr _hookId;
            readonly string _functionName;
            readonly string _moduleName;
            private readonly string _functionPath;
            eNktHookState _state = eNktHookState.stInactive;
            private readonly INktHook _hook;

            public HookInfo(uint processId, INktHook h)
            {
                //Volatile = false;
                _hook = h;
                _processId = processId;
                _hookId = h.Id;
                _functionPath = h.FunctionName;
                string[] hookParts = _functionPath.Split(new[] { '!' });
                _moduleName = hookParts[0];
                _functionName = hookParts[1];
            }

            //public bool Volatile { get; set; }

            public eNktHookState State
            {
                get { return _state; }
                set { _state = value; }
            }
            public uint ProcessId
            {
                get { return _processId; }
            }
            public IntPtr HookId
            {
                get { return _hookId; }
            }
            public INktHook HookObject
            {
                get { return _hook; }
            }
            public string ModuleName
            {
                get { return _moduleName; }
            }
            public string FunctionName
            {
                get { return _functionName; }
            }
        }

        readonly object _hookInfoLock = new object();
        // processId -> moduleName, HookInfo
        readonly Dictionary<uint, Dictionary<string, HashSet<HookInfo>>> _hookStatesByPidAndModule = new Dictionary<uint, Dictionary<string, HashSet<HookInfo>>>();
        readonly Dictionary<uint, HashSet<IntPtr>> _hookActiveCount = new Dictionary<uint, HashSet<IntPtr>>();
        // processId -> hookId, HookInfo 
        readonly Dictionary<uint, Dictionary<IntPtr, HookInfo>> _hookStatesByPid = new Dictionary<uint, Dictionary<IntPtr, HookInfo>>();
        private readonly HookGroupMgr _groupMgr;
        private INktSpyMgr _spyMgr;

        public HookStateMgr(HookGroupMgr groupMgr)
        {
            _groupMgr = groupMgr;
        }
        public void SetSpyMgr(NktSpyMgr spyMgr)
        {
            _spyMgr = spyMgr;
        }

        HookInfo AddHookInfo(IntPtr hId, uint procId)
        {
            NktHook h = _spyMgr.Hooks().GetById(hId);
            Debug.Assert(h != null);
            return AddHookInfo(h, procId);
        }

        HookInfo AddHookInfo(INktHook h, uint procId)
        {
            HookInfo hookInfo;
            lock (_hookInfoLock)
            {
                var hooksByModule = _hookStatesByPidAndModule[procId];
            
                hookInfo = new HookInfo(procId, h);
                
                if (!hooksByModule.ContainsKey(hookInfo.ModuleName))
                    hooksByModule[hookInfo.ModuleName] = new HashSet<HookInfo>();

                hooksByModule[hookInfo.ModuleName].Add(hookInfo);
                _hookStatesByPid[procId][h.Id] = hookInfo;
                
            }
            return hookInfo;
        }
        void RemoveHookInfo(IntPtr hId, uint procId)
        {
            lock (_hookInfoLock)
            {
                Dictionary<string, HashSet<HookInfo>> procHooks;
                if (!_hookStatesByPidAndModule.TryGetValue(procId, out procHooks))
                    return;

                HookInfo hookInfo;
                if(_hookStatesByPid[procId].TryGetValue(hId, out hookInfo))
                {
                    _hookStatesByPid[procId].Remove(hId);
                    procHooks[hookInfo.ModuleName].Remove(hookInfo);

                    // any active hook?
                    if (_hookActiveCount.ContainsKey(procId) && _hookActiveCount[procId].Count == 0)
                    {
                        _hookActiveCount.Remove(procId);
                        _hookStatesByPid.Remove(procId);
                        _hookStatesByPidAndModule.Remove(procId);
                    }
                }
            }
        }
        public void AddHook(INktHook h, uint procId)
        {
            AddHookInfo(h, procId);
        }
        /// <summary>
        /// Volatiles hooks doesn't last after the hook becames unhooked
        /// </summary>
        /// <param name="h"></param>
        /// <param name="procId"></param>
        public void AddVolatileHook(NktHook h, uint procId)
        {
            SetHookState(procId, h, eNktHookState.stInactive);
        }
        public void SetHookState(uint pid, NktHook hook, eNktHookState newState)
        {
            IntPtr hookId = hook.Id;
            lock (_hookInfoLock)
            {
                if (!_hookStatesByPid.ContainsKey(pid)
                    || !_hookStatesByPidAndModule.ContainsKey(pid))
                    InitializeProcessHookStatesRegister(pid);

                if (newState != eNktHookState.stRemoved && !_hookStatesByPid[pid].ContainsKey(hookId))
                    AddHookInfo(hook, pid);

                if (newState == eNktHookState.stActivating || newState == eNktHookState.stActive)
                {
                    HashSet<IntPtr> hookIds;
                    if(!_hookActiveCount.TryGetValue(pid, out hookIds))
                    {
                        _hookActiveCount[pid] = hookIds = new HashSet<IntPtr>();
                    }

                    hookIds.Add(hookId);
                }
                else if(newState == eNktHookState.stRemoved)
                {
                    if (_hookActiveCount.ContainsKey(pid))
                        _hookActiveCount[pid].Remove(hookId);
                }

                if (_hookStatesByPid[pid].ContainsKey(hookId))
                    _hookStatesByPid[pid][hookId].State = newState;

                if (newState == eNktHookState.stRemoved)
                {
                    RemoveHookInfo(hookId, pid);
                }
            }
        }
        public HashSet<INktHook> GetHooksByModule(uint pid, string module)
        {
            var ret = new HashSet<INktHook>();
            lock(_hookInfoLock)
            {
                Dictionary<string, HashSet<HookInfo>> hookInfosMap;

                if (_hookStatesByPidAndModule.TryGetValue(pid, out hookInfosMap))
                {
                    HashSet<HookInfo> hookInfos;
                    if (hookInfosMap.TryGetValue(module, out hookInfos))
                    {
                        foreach(var hookInfo in hookInfos)
                        {
                            ret.Add(hookInfo.HookObject);
                        }
                    }
                }
            }
            return ret;
        }
        public bool AnyActiveHook(uint pid)
        {
            lock(_hookInfoLock)
            {
                bool ret = false;
                Dictionary<IntPtr, HookInfo> states;
                if(_hookStatesByPid.TryGetValue(pid, out states))
                {
                    ret = states.Any(s => s.Value.State == eNktHookState.stActive || s.Value.State == eNktHookState.stActivating);
                }
                return ret;
            }
        }
        public void AddHooks(ListView listView, uint pid)
        {
            var itemMap = new Dictionary<IntPtr, ListViewItem>();
            listView.Tag = itemMap;

            lock (_hookInfoLock)
            {
                if (!_hookStatesByPidAndModule.ContainsKey(pid))
                    InitializeProcessHookStatesRegister(pid);
                foreach (var h in _hookStatesByPid[pid])
                {
                    AddItem(listView, h.Key, h.Value.ModuleName, h.Value.FunctionName, h.Value.State);
                }
            }
        }
        void AddItem(ListView listView, IntPtr hookId, string moduleName, string functionName, eNktHookState state)
        {
            var itemMap = (Dictionary<IntPtr, ListViewItem>) listView.Tag;

            var item = new ListViewItem(moduleName.ToLower());
            item.SubItems.Add(functionName);
            item.SubItems.Add(state.ToString().Substring(2));
            item.Tag = hookId;
            itemMap[hookId] = item;
            listView.Items.Add(item);
        }
        public void ChangeListViewItemHookState(ListView lv, uint pid, IntPtr hookId, eNktHookState newState)
        {
            var itemMap = (Dictionary<IntPtr, ListViewItem>)lv.Tag;
            if(!itemMap.ContainsKey(hookId))
            {
                NktHook h = _spyMgr.Hooks().GetById(hookId);
                if(h != null)
                {
                    var hookInfo = AddHookInfo(hookId, pid);
                    AddItem(lv, hookId, hookInfo.ModuleName, hookInfo.FunctionName, newState);
                }
            }
            if(itemMap.ContainsKey(hookId))
                itemMap[hookId].SubItems[2].Text = newState.ToString().Substring(2);
        }

        /// <summary>
        /// Create hook state objects for the specified process
        /// </summary>
        /// <param name="procId"></param>
        /// <returns>Union of involved modules</returns>
        public void InitializeProcessHookStatesRegister(uint procId)
        {
            lock (_hookInfoLock)
            {
                _hookStatesByPid[procId] = new Dictionary<IntPtr, HookInfo>();
                _hookStatesByPidAndModule[procId] = new Dictionary<string, HashSet<HookInfo>>();

                var hooksSetToBeMonitored =
                    _groupMgr.Groups.Select(g => _groupMgr.GetHooksByGroup(g)).SelectMany(hookIds => hookIds);

                foreach (var hId in hooksSetToBeMonitored)
                    AddHookInfo(hId, procId);
            }
        }

        public IEnumerable<HookInfo> GetHookStatesForPid(int pid)
        {
            lock(_hookInfoLock)
            {
                return _hookStatesByPid[(uint)pid].Select(hookAndState => hookAndState.Value);
            }
        }
    }
    public class HookGroupMgr
    {
        readonly Dictionary<string, HashSet<IntPtr>> _hookGroups = new Dictionary<string, HashSet<IntPtr>>();
        readonly HashSet<string> _hookGroupsStrings = new HashSet<string>();

        public HookGroupMgr()
        {
            ActiveGroups = null;
        }

        public void AddHookToGroup(IntPtr hookId, string group)
        {
            HashSet<IntPtr> ids;
            if (!_hookGroups.TryGetValue(group.ToLower(), out ids))
            {
                ids = new HashSet<IntPtr>();
                _hookGroups[group.ToLower()] = ids;
                _hookGroupsStrings.Add(group);
            }
            ids.Add(hookId);
        }
        public HashSet<IntPtr> GetHooks(string group)
        {
            HashSet<IntPtr> ret;
                
            group = group.ToLower();
            if(!_hookGroups.TryGetValue(group, out ret))
            {
                ret = new HashSet<IntPtr>();
            }
            return ret;
        }
        public HashSet<IntPtr> GetActiveHooks()
        {
            var ret = new HashSet<IntPtr>();
            foreach (var g in ActiveGroups)
            {
                var hookIds = GetHooks(g);
                foreach (var hookId in hookIds)
                    ret.Add(hookId);
            }
            return ret;
        }
        public bool IsActiveXActive()
        {
            return ActiveGroups.Contains("ActiveX");
        }

        public HashSet<string> Groups
        {
            get { return _hookGroupsStrings; }
        }
        public HashSet<IntPtr> GetHooksByGroup(string g)
        {
            return _hookGroups[g.ToLower()];
        }
        public HashSet<string> ActiveGroups { get; set; }
    }
}
