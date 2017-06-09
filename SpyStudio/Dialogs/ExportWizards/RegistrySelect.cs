using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Aga.Controls.Tree;
using Microsoft.Win32;
using SpyStudio.ContextMenu;
using SpyStudio.Database;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Hooks;
using SpyStudio.Properties;
using SpyStudio.Registry;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using SpyStudio.Trace;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class RegistrySelect : TemplatedVirtualizationPage, IInterpreterController
    {
        protected VirtualizationExport Export;

        protected ExportWizard Wizard;

        protected ExportField<List<RegKeyInfo>> RegistryKeys;
        protected ExportField<ThinAppIsolationOption> RegistryIsolation;
        protected ExportField<IEnumerable<AppBehaviourAnalyzer>> AppAnalyzers;

        private ExportField<PortableTemplate> CompleteTemplate;

        private List<string> _checkedClasses;
        private const string ClsidPath = @"HKEY_CLASSES_ROOT\CLSID";
        private const string TypeLibPath = @"HKEY_CLASSES_ROOT\TypeLib";
        private const string InterfacePath = @"HKEY_CLASSES_ROOT\Interface";

        // Context Menu
        private readonly ToolStripMenuItem _importItem = new ToolStripMenuItem("Import Contents From Base");
        private readonly ToolStripMenuItem _importRegItem = new ToolStripMenuItem("Import Key From Path");
        private readonly ToolStripSeparator _importRegItemSep = new ToolStripSeparator();

        private HashSet<RegistryTreeNodeBase> _relatedToApplicationUser = new HashSet<RegistryTreeNodeBase>();
        private HashSet<RegistryTreeNodeBase> _relatedToApplicationMachine = new HashSet<RegistryTreeNodeBase>();

        private List<RegistryTreeNodeBase> _relatedToApplicationUserAdded;
        private List<RegistryTreeNodeBase> _relatedToApplicationMachineAdded;

        #region Instatiation

        public static RegistrySelect ForThinApp(ExportWizard aWizard, ThinAppExport aThinAppExport, string bannerText)
        {
            var registrySelect = new RegistrySelect(aWizard, aThinAppExport, bannerText, true);

            return registrySelect;
        }

        private ExportField<DeviareRunTrace> Trace;

        private void SetFields(VirtualizationExport anExport)
        {
            Export.GetField<IEnumerable<FileEntry>>(ExportFieldNames.Files);
            RegistryKeys = anExport.GetField<List<RegKeyInfo>>(ExportFieldNames.RegistryKeys);
            AppAnalyzers = Export.GetField<IEnumerable<AppBehaviourAnalyzer>>(ExportFieldNames.ApplicationBehaviourAnalizers);
            Trace = anExport.GetField<DeviareRunTrace>(ExportFieldNames.Trace);
            RegistryIsolation = anExport.GetField<ThinAppIsolationOption>(ExportFieldNames.ThinAppRegistryIsolation);
            CompleteTemplate = anExport.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate);
        }

        public RegistrySelect(ExportWizard aWizard, VirtualizationExport anExport, string aPageDescription)
            : this(aWizard, anExport, aPageDescription, false)
        {
        }

        public RegistrySelect(ExportWizard aWizard, VirtualizationExport anExport, string aPageDescription,
                              bool showIsolationOptions)
            : base(aPageDescription, anExport)
        {
            Initialize(aWizard, anExport, showIsolationOptions);
        }

        private void Initialize(ExportWizard aWizard, VirtualizationExport anExport, bool showIsolationOptions)
        {
            Wizard = aWizard;
            Export = anExport;

            SetFields(anExport);

            InitializeRequiredStateRegisters();

            InitializeComponent();
            SetCustomMode(false);

            registryTreeView.Controller = this;
            registryTreeView.FullRowSelect = true;
            registryTreeView.CheckBoxes = registryTreeView.RecursiveCheck = true;

            registryTreeView.ShowIsolationOptions = showIsolationOptions;

            if (!showIsolationOptions)
            {
                _defaultIsolationModeLabel.Visible = false;
                _defaultIsolationModeCombo.Visible = false;
                _customPanel.RowStyles[0].Height = 0;
            }

            KeyPressed += OnKeyPressed;

            registryTreeView.MergeWow = false;
            registryTreeView.MergeLayerPaths = false;
            registryTreeView.RedirectClasses = true;
            registryTreeView.GotoVisible = false;

            if (registryTreeView.Columns.Count > 0)
            {
                var keyColumn = registryTreeView.Columns.First();
                keyColumn.Width = 170;
            }
            if (listViewValues.Columns.Count > 2)
            {
                listViewValues.Columns[0].Width = 100;
                listViewValues.Columns[1].Width = 100;
                listViewValues.Columns[2].Width = 100;
            }

            _importItem.Click += ImportItemOnClick;
            _importRegItem.Click += ImportRegItemOnClick;

            registryTreeView.ContextMenuStrip.Opening += ContextMenuStripOnOpening;
            registryTreeView.ContextMenuStrip.Items.Insert(0, _importRegItemSep);
            registryTreeView.ContextMenuStrip.Items.Insert(0, _importRegItem);

            registryTreeView.OnUserRequestedCheckStateChange += OnUserRequestedCheckStateChangeHandler;
            registryTreeView.ContextMenuStrip.ItemClicked += this.ToolStripItemClickedEventHandler;

            AddToRelatedToApplicationMachineAdded = node => _relatedToApplicationMachineAdded.Add(node);
            AddToRelatedToApplicationUserAdded = node => _relatedToApplicationUserAdded.Add(node);

            foreach (var c in registryTreeView.Columns)
            {
                if (c.Header != "Isolation")
                    continue;
                Debug.WriteLine(c);
            }

        }

        #endregion

        private void ToolStripItemClickedEventHandler(object sender, ToolStripItemClickedEventArgs e)
        {
            /*
              TODO: Eventually, this bit will be used to mark a node as
                    "include all children" to minimize the size of generated
                    templates.
            */
#if false
            var clicked = (int)registryTreeView.GetContextMenuItemClicked(e.ClickedItem);
            if (clicked < 1 || clicked > 4)
                return;
            CheckState state = clicked < 3 ? CheckState.Checked : CheckState.Unchecked;
            bool recursive = clicked % 2 == 0;
            foreach (var node in registryTreeView.SelectedNodes)
            {
                var selectedNode = node as RegistryTreeNode;
                if (selectedNode == null)
                    continue;
                
                //SaveRegistryCheckStateChange(selectedNode.Path, state, recursive);
            }
#endif
        }

        private void OnUserRequestedCheckStateChangeHandler(TreeNodeAdv node, CheckState checkState)
        {
#if false
            var n = node.Node as RegistryTreeNodeBase;
            if (n == null)
                return;
            SaveRegistryCheckStateChange(n.Path, checkState, checkState == CheckState.Checked);
#endif
        }

        private void ImportRegItemOnClick(object sender, EventArgs e)
        {
            var selKey = new SelectRegistryKey();
            if (selKey.ShowDialog(this) == DialogResult.OK)
            {
                var inputs = selKey.KeyPath.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var input in inputs)
                {
                    ImportKey(input, true);
                }
            }
        }

        private void ImportKey(string path, bool messageOnError)
        {
            var key = RegistryTools.GetKeyFromFullPath(path);
            if (key == null)
            {
                if (messageOnError)
                    MessageBox.Show(this, "Cannot open key: " + path,
                                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ImportKey(key, true, true, true, true);
        }

        private void ContextMenuStripOnOpening(object sender, CancelEventArgs e)
        {
            var selectedNode = registryTreeView.SelectedNode as RegistryTreeNodeBase;
            if (selectedNode != null)
            {
                registryTreeView.ContextMenuStrip.Items.Insert(0, _importItem);
            }
            else
            {
                registryTreeView.ContextMenuStrip.Items.Remove(_importItem);
            }
        }

        public void AddRelatedToApplicationNode(RegistryTreeNodeBase node)
        {
            if (RegistryTools.PathIsUnder(node.Path, "HKEY_LOCAL_MACHINE"))
                _relatedToApplicationMachine.Add(node);
            else if (RegistryTools.PathIsUnder(node.Path, "HKEY_CURRENT_USER"))
                _relatedToApplicationUser.Add(node);
        }

        public bool ImportNode(RegistryTreeNodeBase node)
        {
            return ImportNode(node, null);
        }

        private bool ImportNode(RegistryTreeNodeBase node, RegistryTreeNodeBase currentNode)
        {
            var success = false;
            if (currentNode == null)
                currentNode = node;
            foreach (var keyPath in currentNode.GetOriginalPathsFromBranch())
            {
                var key = RegistryTools.GetKeyFromFullPath(keyPath);
                if (key != null)
                {
                    var nodeKey = key;
                    if (currentNode != node)
                    {
                        // if currentNode != node => currentNode is a child or grand child of node
                        // so try to build node path based on currentNode extracting last part
                        var n = currentNode;
                        while (n != node && n != null && nodeKey != null)
                        {
                            n = n.Parent as RegistryTreeNodeBase;
                            if (n != null)
                                nodeKey = nodeKey.OpenParentKey();
                        }
                        if (n == node && nodeKey != null)
                        {
                            success = true;
                        }
                    }
                    else
                    {
                        success = true;
                    }
                    if (success)
                    {
                        ImportKey(nodeKey, true, true, true, true);
                    }
                }
            }
            foreach (var childNode in currentNode.Nodes)
            {
                var registryNode = childNode as RegistryTreeNodeBase;
                if (ImportNode(node, registryNode))
                {
                    success = true;
                    break;
                }
            }
            return success;
        }

        private void ImportItemOnClick(object sender, EventArgs e)
        {
            var selectedNode = registryTreeView.SelectedNode as RegistryTreeNodeBase;
            if (selectedNode == null)
                return;
            if (!ImportNode(selectedNode))
                MessageBox.Show(this, "Cannot open key",
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected virtual void OnWizardNext(object sender, WizardPageEventArgs wizardPageEventArgs)
        {
            EnableNextButton(false);
            RegistryKeys.Value = registryTreeView.GetCheckedKeys().ToList();

            var defaultIsolation = ThinAppIsolationOption.DefaultRegistryIsolation;
            try
            {
                defaultIsolation = (ThinAppIsolationOption) _defaultIsolationModeCombo.SelectedItem;
            }
            catch (InvalidCastException)
            {
#if DEBUG
                throw;
#endif
            }
            catch
            {
            }
            VirtualizationTemplate.Value.SaveRegistry(registryTreeView, defaultIsolation);
        }

        public RegistryTreeNodeBase ImportKey(RegistryKey key, bool recursive, bool checkRoot, bool includeValues, bool includeSubKeys)
        {
            registryTreeView.BeginUpdate();
            var keyNode = ImportKey(key, string.Empty, recursive, includeValues, includeSubKeys);
            if (checkRoot)
            {
                TreeNodeAdvTools.CheckNode(keyNode, true, true);
            }
            registryTreeView.EndUpdate();
            return keyNode;
        }

        private RegistryTreeNodeBase ImportKey(RegistryKey key, string subKey, bool recursive, bool includeValues, bool includeSubKeys)
        {
            RegistryTreeNodeBase ret = null;
            try
            {
                var keyToImport = key.OpenSubKey(subKey);
                if (keyToImport != null)
                {
                    var keyInfo = RegKeyInfo.From(keyToImport);
                    keyInfo.IsNonCaptured = true;

                    ret = registryTreeView.Add(keyInfo);
                    ret.IsImported = true;

                    if (includeValues)
                    {
                        foreach (var valueName in keyToImport.GetValueNames())
                        {
                            var valueInfo = RegValueInfo.From(keyToImport, valueName);
                            if (valueInfo != null)
                                registryTreeView.Add(valueInfo);
                        }
                    }
                    if (recursive && includeSubKeys)
                    {
                        var childrenSubKeys = keyToImport.GetSubKeyNames();
                        foreach (var childSubKey in childrenSubKeys)
                        {
                            ImportKey(keyToImport, childSubKey, true, true, true);
                        }
                    }
                }
            }
            catch
            {
            }

            return ret;
        }

        private RegistryTreeNode FindNode(string path)
        {
            return registryTreeView.GetNodeByTreePath<RegistryTreeNode>(path);
        }

        private bool Analyze()
        {
            foreach (var appAnalyzer in AppAnalyzers.Value)
            {
                if (Export.Canceled)
                    return false;

                appAnalyzer.Analyze(registryTreeView);

                if (Export.Canceled)
                    return false;
            }

            return true;
        }

        void DoNormalRegistryLoading()
        {
            registryTreeView.ClearData();

            LoadRegistryFromDataBase();
            var packageFiles = LoadRegistryFromSelectedPackage();

            if (Export.Canceled)
                return;

            if (Wizard.VirtualPackage == null || Wizard.VirtualPackage.IsNew)
            {
                Analyze();
                foreach (var regChecker in Export.RegistryCheckers)
                {
                    regChecker.RegistrySelect = this;
                    regChecker.PerformCheckingOn(registryTreeView);
                }
            }

            foreach (var packageFile in packageFiles)
                packageFile.Checked = true;

            registryTreeView.Model.NodeCheckChanged += node => Export.RegistryWasUpdated = true;
        }

        protected virtual IEnumerable<RegistryTreeNodeBase> LoadRegistryFromSelectedPackage()
        {
            Debug.Assert(false, "Subclass responsibility");

            return null;
        }

        void SelectServicesUsed()
        {
#if false
            foreach (var service in ServicesUsed)
            {
                if (service.Length == 0)
                    continue;
                var keyPath = @"SYSTEM\CurrentControlSet\services\" + service;
                var key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                if (key == null)
                    continue;
                ImportKey(key, true, true);
            }
#endif
        }

        private static RegValueInfo TransformValueNode(IntermediateTreeNode valueNode, string keyPath)
        {
            if (valueNode.Data.ImportAtLoadTime)
            {
                var key = RegistryTools.GetKeyFromFullPath(keyPath);
                if (key == null)
                    return null;
                var data = key.GetValue(valueNode.Name);
                if (data == null)
                    return null;
                valueNode.Data.Data = RegistryTools.ToSpyStudioDisplayFormat(data,
                                                                             valueNode.Data.DataType =
                                                                             key.GetValueKind(valueNode.Name));
            }
            var ret = new RegValueInfo
                          {
                              Name = valueNode.Name,
                              Path = keyPath,
                              AlternatePath = keyPath,
                              Access = RegistryKeyAccess.None,
                              BasicKeyHandle = RegistryTools.GetBasicHandleFromPath(keyPath),
                              ValueType = valueNode.Data.DataType,
                              Data = valueNode.Data.Data,
                              IsDataComplete = true,
                              IsDataNull = string.IsNullOrEmpty(valueNode.Data.Data),
                              IsNonCaptured = true,
                              Success = true,
                          };
            return ret;
        }

        private void RestoreKey(IntermediateTreeNode key, Stack<string> pathStack)
        {
            pathStack.Push(key.Name);
            var keyPath = StringTools.JoinStack(pathStack);
            try
            {
                if (!(key.AllLeaves || key.AllBranches))
                {
                    var thisKey = new RegKeyInfo
                                      {
                                          Name = key.Name,
                                          Access = RegistryKeyAccess.None,
                                          Path = keyPath,
                                          OriginalPath = keyPath,
                                          AlternatePath = keyPath,
                                          BasicKeyHandle = RegistryTools.GetBasicHandleFromPath(keyPath),
                                          Success = true,
                                      };
                    thisKey.OriginalKeyPaths[keyPath] = true;
                    var values = key.Children.Where(x => x.IsLeaf).Select(x => TransformValueNode(x, keyPath));
                    foreach (var regValueInfo in values.Where(x => x != null))
                        thisKey.ValuesByName[regValueInfo.Name] = regValueInfo;
                    Debug.Assert(key.IsChecked != null);
                    registryTreeView.Add(thisKey).IsChecked = key.IsChecked.Value;
                }
                else
                {
                    var registryKey = RegistryTools.GetKeyFromFullPath(keyPath);
                    if (registryKey != null)
                    {
                        ImportKey(registryKey, true, true, key.AllLeaves, key.AllBranches)
                            .SetCheckStateForSelfAndDescendants(true);
                        if (key.UncheckedDescendants != null)
                            ChangeCheckedStateByList(keyPath, key.UncheckedDescendants, registryTreeView, false, null);
                        if (key.CheckedDescendants != null)
                            ChangeCheckedStateByList(keyPath, key.CheckedDescendants, registryTreeView, true, null);
                    }
                }

                var subkeys = key.Children.Where(x => !x.IsLeaf);
                foreach (var intermediateTreeNode in subkeys)
                    RestoreKey(intermediateTreeNode, pathStack);
            }
            finally
            {
                pathStack.Pop();
            }
        }

        private void RestoreRegistry()
        {
            var registry = VirtualizationTemplate.Value.Registry;
            if (registry == null)
                return;
            Debug.Assert(registry.IsRoot && !registry.IsLeaf);
            var pathStack = new Stack<string>();
            _defaultIsolationModeCombo.SelectedItem = registry.Isolation;
            foreach (var intermediateTreeNode in registry.Children)
            {
                RestoreKey(intermediateTreeNode, pathStack);
            }
        }

        public void LoadRegistry()
        {
            Export.RegistryNeedUpdate = false;
            Application.DoEvents();

            BeginLoad();
            if (CompleteTemplate.Value.IsInUse)
            {
                RestoreRegistry();
                //SaveInitialTree();
            }
            else
            {
                DoNormalRegistryLoading();
                SelectServicesUsed();
            }
            EndLoad();

            if (Export.Canceled)
                return;


            this.ExecuteInUIThreadSynchronously(() => EnableNextButton(true));
        }

        private void RegistrySelectQueryCancel(object sender, CancelEventArgs e)
        {
            Export.Cancel();
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            // arrow up and down should select first or last item
            if (keyPressedEventArgs.KeyData == Keys.Up || keyPressedEventArgs.KeyData == Keys.Down)
            {
                if (!registryTreeView.Focused)
                    registryTreeView.Focus();
                if (registryTreeView.SelectedNodes.Count == 0)
                {
                    if (keyPressedEventArgs.KeyData == Keys.Up)
                        TreeNodeAdvTools.SelectLastNode(registryTreeView);
                    else
                        TreeNodeAdvTools.SelectFirstNode(registryTreeView);
                    keyPressedEventArgs.Handled = true;
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control)
            {
                registryTreeView.CopySelectionToClipboard();
            }
            if (e.KeyCode == Keys.A && e.Control)
            {
                registryTreeView.SelectAll();
            }
        }

        private void InitializeRequiredStateRegisters()
        {
        }

        private HashSet<string> ServicesUsed;

        private void LoadRegistryFromDataBase()
        {
            ServicesUsed = new HashSet<string>();
            var refreshData = new EventsReportData(Export.TraceId)
                {
                    EventsToReport = EventType.Registry | EventType.Services,
                    ReportBeforeEvents = false,
                    EventResultsIncluded = EventsReportData.EventResult.Success,
                    ControlInvoker = this
                };
            refreshData.EventsReady += ReportEvents;
            EventDatabaseMgr.GetInstance().RefreshEvents(refreshData);
            EventDatabaseMgr.GetInstance().WaitProcessEvents(refreshData);
        }

        private void ReportEvents(object sender, EventsRefreshArgs e)
        {
            var evToReport = e.Events;
            foreach (var ev in evToReport)
            {
                Debug.Assert(ev.IsRegistry || ev.IsServices);

                switch (ev.Type)
                {
                    case HookType.RegOpenKey:
                    case HookType.RegCreateKey:
                    case HookType.RegEnumerateKey:
                    case HookType.RegQueryKey:
                        registryTreeView.Add(RegKeyInfo.From(ev));
                        break;
                    case HookType.RegEnumerateValueKey:
                    case HookType.RegQueryValue:
                    case HookType.RegSetValue:
                    case HookType.RegDeleteValue:
                        registryTreeView.Add(RegValueInfo.From(ev));
                        break;
                    case HookType.OpenService:
                    case HookType.CreateService:
                        ServicesUsed.Add(ev.ParamMain);
                        break;
                }

                if (Export.Canceled)
                {
                    e.Canceled = true;
                    return;
                }
            }
        }

        private void BeginLoad()
        {
            checkBoxRelatedToAppMachine.CheckedChanged -= CheckBoxRelatedToAppLocalMachineCheckedChanged;
            checkBoxRelatedToAppUser.CheckedChanged -= CheckBoxRelatedToAppCurrentUserCheckedChanged;

            registryTreeView.BeginUpdate();
        }

        private void EndLoad()
        {
            _relatedToApplicationUserAdded = _relatedToApplicationMachineAdded = null;
            this.ExecuteInUIThreadSynchronously(() => checkBoxRelatedToAppMachine.Checked = true);
            CheckBoxIncludeWholeKeysRelatedToApplicationLocalMachineChanged();

            checkBoxRelatedToAppMachine.CheckedChanged += CheckBoxRelatedToAppLocalMachineCheckedChanged;
            checkBoxRelatedToAppUser.CheckedChanged += CheckBoxRelatedToAppCurrentUserCheckedChanged;

            if (Export.RuntimesExported != null)
            {
                const string baseWinners = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide\Winners";
                try
                {
                    var baseWinnersKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(baseWinners,
                                                                                          RegistryKeyPermissionCheck
                                                                                              .ReadSubTree);
                    if (baseWinnersKey != null)
                    {
                        registryTreeView.BeginUpdate();

                        var winnersSubKeys = baseWinnersKey.GetSubKeyNames();
                        foreach (var runtime in Export.RuntimesExported)
                        {
                            var runtimeMatched = winnersSubKeys.Where(r => r.Contains(runtime));
                            foreach (var keyMatched in runtimeMatched)
                            {
                                var rootKey = ImportKey(baseWinnersKey, keyMatched, true, true, true);
                                Debug.Assert(rootKey != null);
                                rootKey.CheckSelfAndParentsAndChildrenIf(true);
                            }
                        }

                        registryTreeView.EndUpdate();
                    }
                }
                catch (Exception)
                {
                }
            }

            /*
            if (!VirtualizationTemplate.Value.IsInUse)
            {
                foreach (var appAnalyzer in AppAnalyzers.Value)
                {
                    if (appAnalyzer.MainRegistryKey != null)
                        ImportKey(appAnalyzer.MainRegistryKey, false);
                }
            }
            */

            this.ExecuteInUIThreadSynchronously(registryTreeView.EndUpdate);
        }

        private void RegistrySelectSetActive(object sender, WizardPageEventArgs e)
        {
            if (e.IsBackActionIn(Wizard))
                return;

            EnableNextButton(false);
            EnableCancelButton(true);

            if (Export.RegistryNeedUpdate)
            {
                this.DisableUI();
                UseWaitCursor = true;
                Threading.ExecuteAsynchronously(LoadRegistry, args => { 
                    this.EnableUI();
                    UseWaitCursor = false;
                    EnableNextButton(true);
                });
            }
            else
                EnableNextButton(true);
        }

        private void RegistryTreeViewSizeChanged(object sender, EventArgs e)
        {
            var columns = registryTreeView.Columns.ToList();
            if (columns.Count > 1)
            {
                var keyColumn = columns.First(c => c.Index == 1);

                var fixedColumnsWidth = columns.Sum(c => c.Width) - keyColumn.Width;

                keyColumn.Width = registryTreeView.ClientSize.Width - fixedColumnsWidth;
            }
        }

        private void SetCustomMode(bool custom)
        {
            if (custom)
            {
                checkBoxStandard.Checked = false;
                checkBoxCustom.Checked = true;
                if (_standardPanel.Visible)
                {
                    _standardPanel.Hide();
                }
                if (!_customPanel.Visible)
                {
                    _customPanel.Show();
                }
            }
            else
            {
                checkBoxStandard.Checked = true;
                checkBoxCustom.Checked = false;
                if (_customPanel.Visible)
                {
                    _customPanel.Hide();
                }
                if (!_standardPanel.Visible)
                {
                    _standardPanel.Show();
                }
            }
            FlowLayoutPanelRightSizeChanged(null, null);
        }

        private void CheckBoxStandardClick(object sender, EventArgs e)
        {
            SetCustomMode(false);
        }

        private void CheckBoxCustomClick(object sender, EventArgs e)
        {
            SetCustomMode(true);
        }
        private void FlowLayoutPanelRightSizeChanged(object sender, EventArgs e)
        {
            _customPanel.Size = new Size(flowLayoutPanelRight.Size.Width - 2,
                                         flowLayoutPanelRight.Size.Height - 2);
            _standardPanel.Size = new Size(flowLayoutPanelRight.Size.Width - 2, flowLayoutPanelRight.Size.Height - 2);
        }

        private void CheckBoxClassesClick(object sender, EventArgs e)
        {
            if (checkBoxClasses.Checked)
            {
                _checkedClasses = registryTreeView.GetCheckedPaths(ClsidPath);
                _checkedClasses.AddRange(registryTreeView.GetCheckedPaths(TypeLibPath));
                _checkedClasses.AddRange(registryTreeView.GetCheckedPaths(InterfacePath));
                registryTreeView.CheckPath(ClsidPath, true, true);
                registryTreeView.CheckPath(TypeLibPath, true, true);
                registryTreeView.CheckPath(InterfacePath, true, true);
            }
            else
            {
                registryTreeView.CheckPath(ClsidPath, false, true);
                registryTreeView.CheckPath(TypeLibPath, false, true);
                registryTreeView.CheckPath(InterfacePath, false, true);
                if (_checkedClasses != null)
                {
                    foreach (var path in _checkedClasses)
                    {
                        registryTreeView.CheckPath(path, true, false);
                    }
                }
            }
        }

        private void CheckBoxRelatedToAppCurrentUserCheckedChanged(object sender, EventArgs e)
        {
            CheckBoxIncludeWholeKeysRelatedToApplicationCurrentUserChanged();
        }

        private void CheckBoxRelatedToAppLocalMachineCheckedChanged(object sender, EventArgs e)
        {
            CheckBoxIncludeWholeKeysRelatedToApplicationLocalMachineChanged();
        }

        private bool AnyParentIncluded(RegistryTreeNodeBase node, HashSet<RegistryTreeNodeBase> hashSet)
        {
            while (node != null)
            {
                node = node.Parent as RegistryTreeNodeBase;
                if (node != null && hashSet.Contains(node))
                    return true;
            }
            return false;
        }

        private Action<RegistryTreeNodeBase> AddToRelatedToApplicationMachineAdded;

        private void CheckBoxIncludeWholeKeysRelatedToApplicationLocalMachineChanged()
        {
            if (_relatedToApplicationMachineAdded == null)
            {
                _relatedToApplicationMachineAdded = new List<RegistryTreeNodeBase>();
                registryTreeView.OnNodeAdded += AddToRelatedToApplicationMachineAdded;
                foreach (var relatedToAppNode in _relatedToApplicationMachine)
                {
                    if (!AnyParentIncluded(relatedToAppNode, _relatedToApplicationMachine))
                    {
                        ImportNode(relatedToAppNode);
                    }
                }
                registryTreeView.OnNodeAdded -= AddToRelatedToApplicationMachineAdded;
            }

            foreach (var item in _relatedToApplicationMachineAdded)
                item.Checked = checkBoxRelatedToAppMachine.Checked;
        }

        private Action<RegistryTreeNodeBase> AddToRelatedToApplicationUserAdded;

        private void CheckBoxIncludeWholeKeysRelatedToApplicationCurrentUserChanged()
        {
            if (_relatedToApplicationUserAdded == null)
            {
                _relatedToApplicationUserAdded = new List<RegistryTreeNodeBase>();
                registryTreeView.OnNodeAdded += AddToRelatedToApplicationUserAdded;
                foreach (var relatedToAppNode in _relatedToApplicationUser)
                {
                    if (!AnyParentIncluded(relatedToAppNode, _relatedToApplicationUser))
                    {
                        ImportNode(relatedToAppNode);
                    }
                }
                registryTreeView.OnNodeAdded -= AddToRelatedToApplicationUserAdded;
            }
            foreach (var item in _relatedToApplicationUserAdded)
                item.Checked = checkBoxRelatedToAppUser.Checked;
        }

        #region Implementation of IInterpreterController

        public void ShowInCom(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public void ShowInWindows(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public void ShowInFiles(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public void ShowInRegistry(ITraceEntry anEntry)
        {
            throw new NotImplementedException();
        }

        public bool ShowQueryAttributesInFiles { get; private set; }
        public bool ShowDirectoriesInFiles { get; private set; }

        public bool PropertiesGoToVisible
        {
            get { return false; }
        }

        public bool PropertiesVisible
        {
            get { return true; }
        }

        #endregion
    }
}