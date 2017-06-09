using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32;
using SpyStudio.Export.Templates;
using SpyStudio.Registry.Infos;
using SpyStudio.Swv;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using System.Linq;

namespace SpyStudio.Export.SWV
{
    class SwvExporter : Exporter
    {
        protected bool Cancelled { get; set; }

        protected SwvExport Export;

        protected ExportField<List<FileEntry>> Files;
        protected ExportField<List<RegKeyInfo>> RegistryKeys;
        protected ExportField<List<SwvIsolationRuleEntry>> IsolationRules;
        protected ExportField<SwvLayer> SelectedSymantecLayer;
        protected ExportField<SwvLayers> SymantecLayers;

        #region Overrides of Exporter

        protected void PrepareToWorkWith(SwvExport anExport)
        {
            Export = anExport;

            Files = Export.GetField<List<FileEntry>>(ExportFieldNames.Files);
            RegistryKeys = Export.GetField<List<RegKeyInfo>>(ExportFieldNames.RegistryKeys);
            IsolationRules = Export.GetField<List<SwvIsolationRuleEntry>>(ExportFieldNames.IsolationRules);
            SelectedSymantecLayer = Export.GetField<SwvLayer>(ExportFieldNames.SelectedSymantecLayer);
            SymantecLayers = Export.GetField<SwvLayers>(ExportFieldNames.SymantecLayers);
        }

        private static RegistryKey FindLayerRegistryKey(RegistryKey layersRoot, string layerGuid)
        {
            foreach (var subKeyName in layersRoot.GetSubKeyNames())
            {
                var key = layersRoot.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (key == null)
                    continue;
                var id = key.GetValue("ID") as string;
                if (id == null)
                    continue;
                id = id.ToLower();
                if (id == layerGuid)
                    return key;
            }
            return null;
        }

        private Dictionary<string, bool> WriteServicesToRealRegistry()
        {
            var ret = new Dictionary<string, bool>();
            try
            {
                var template = Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value;
                if (!template.IsInUse || template.Services.Count == 0)
                    return ret;

                foreach (var service in template.Services)
                    ret[service] = false;

                var systemKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("System",
                                                                                 RegistryKeyPermissionCheck.
                                                                                     ReadWriteSubTree);
                if (systemKey == null)
                    return ret;
                var controlSetKey = systemKey.OpenSubKey("CurrentControlSet",
                                                         RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (controlSetKey == null)
                    return ret;
                var srcServicesKey = controlSetKey.OpenSubKey("Services", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (srcServicesKey == null)
                    return ret;
                var fslxKey = srcServicesKey.OpenSubKey("FSLX", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (fslxKey == null)
                    return ret;
                var parametersKey = fslxKey.OpenSubKey("Parameters", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (parametersKey == null)
                    return ret;
                var fslKey = parametersKey.OpenSubKey("FSL", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (fslKey == null)
                    return ret;
                var guid = SelectedSymantecLayer.Value.Guid.ToLower();
                if (guid.StartsWith("{"))
                    guid = guid.Substring(1);
                if (guid.EndsWith("}"))
                    guid = guid.Substring(0, guid.Length - 1);
                var layerKey = FindLayerRegistryKey(fslKey, guid);
                if (layerKey == null)
                    return ret;

                layerKey.SetValue("Priority", template.Priority, RegistryValueKind.DWord);

                if (template.Services.Count == 0)
                    return ret;

                var dstServicesKey = layerKey.OpenOrCreateSubKey("Services", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (dstServicesKey == null)
                    return ret;

                foreach (var service in template.Services)
                {
                    var srcServiceKey = srcServicesKey.OpenSubKey(service, RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if (srcServiceKey == null)
                    {
                        ret[service] = false;
                        break;
                    }
                    var dstServiceKeyName = "FSL_" + service;
                    var dstServiceKey = dstServicesKey.OpenOrCreateSubKey(dstServiceKeyName,
                                                                          RegistryKeyPermissionCheck.ReadWriteSubTree);
                    RegistryTools.CopyKeyContents(srcServiceKey, dstServiceKey);
                    ret[service] = true;
                }
                var successfulList = ret.Where(x => x.Value).Select(x => x.Key).ToList();

                var keys = ret.Keys.ToList();
                foreach (var key in keys)
                    ret[key] = false;

                dstServicesKey.OpenOrCreateSubKey("PreActivate", RegistryKeyPermissionCheck.ReadWriteSubTree);

                var postActivateKey = dstServicesKey.OpenOrCreateSubKey("PostActivate",
                                                                  RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (postActivateKey == null)
                    return ret;
                var preDeactivateKey = dstServicesKey.OpenOrCreateSubKey("PreDeactivate", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (preDeactivateKey == null)
                    return ret;
                var postDeactivateKey = dstServicesKey.OpenOrCreateSubKey("PostDeactivate", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (postDeactivateKey == null)
                    return ret;

                {
                    var indexPostAct = 1;
                    var indexPreDeact = 1;
                    var indexPostDeact = 1;
                    foreach (var service in successfulList)
                    {
                        var postActivate1Key =
                            postActivateKey.OpenOrCreateSubKey((indexPostAct++).ToString(CultureInfo.InvariantCulture),
                                                               RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (postActivate1Key == null)
                            return ret;
                        postActivate1Key.SetValue("Command", 1, RegistryValueKind.DWord);
                        postActivate1Key.SetValue("ServiceName", service, RegistryValueKind.String);

                        var postActivate2Key =
                            postActivateKey.OpenOrCreateSubKey((indexPostAct++).ToString(CultureInfo.InvariantCulture),
                                                               RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (postActivate2Key == null)
                            return ret;
                        postActivate2Key.SetValue("Command", 2, RegistryValueKind.DWord);
                        postActivate2Key.SetValue("ServiceName", service, RegistryValueKind.String);

                        var preDeactivate1Key =
                            preDeactivateKey.OpenOrCreateSubKey(
                                (indexPreDeact++).ToString(CultureInfo.InvariantCulture),
                                RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (preDeactivate1Key == null)
                            return ret;
                        preDeactivate1Key.SetValue("Command", 3, RegistryValueKind.DWord);
                        preDeactivate1Key.SetValue("ServiceName", service, RegistryValueKind.String);

                        var postDeactivate1Key =
                            postDeactivateKey.OpenOrCreateSubKey(
                                (indexPostDeact++).ToString(CultureInfo.InvariantCulture),
                                RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (postDeactivate1Key == null)
                            return ret;
                        postDeactivate1Key.SetValue("Command", 4, RegistryValueKind.DWord);
                        postDeactivate1Key.SetValue("ServiceName", service, RegistryValueKind.String);

                        ret[service] = true;
                    }

                }
            }
            catch
            {
            }
            return ret;
        }

        public override void GeneratePackage(VirtualizationExport export)
        {
            PrepareToWorkWith((SwvExport)export);

            if (Export.RegistryWasUpdated || Export.IsolationRulesWereUpdated)
            {
                SelectedSymantecLayer.Value.Delete();
                var newLayer = SymantecLayers.Value.CreateLayer(SelectedSymantecLayer.Value.Name);
                SelectedSymantecLayer.Value = newLayer;
                SelectedSymantecLayer.Value.RefreshAll();
            }
            else
            {
                if (Export.FilesWereUpdated)
                    SelectedSymantecLayer.Value.DeleteAllFiles();
            }

            if (Cancelled)
                return;

            if (SelectedSymantecLayer.Value.IsNew || Export.FilesWereUpdated)
            {
                ProgressDialog.LogString("Exporting files ...");

                var error = SymantecLayers.Value.ExportFiles(Files.Value, SelectedSymantecLayer.Value, ProgressDialog, 0, 50);

                if (error != Declarations.FSL2_ERROR_SUCCESS)
                {
                    ProgressDialog.LogError("Error exporting files: " + Declarations.FslErrorToString(error));
                    return;
                }
            }
            if (Cancelled)
                return;


            if (SelectedSymantecLayer.Value.IsNew || Export.RegistryWasUpdated)
            {
                ProgressDialog.LogString("Exporting registry ...");
                var error = SymantecLayers.Value.ExportRegistry(RegistryKeys.Value, SelectedSymantecLayer.Value,
                                                            ProgressDialog, 50, 95);

                if (error != null)
                {
                    ProgressDialog.LogError("Error exporting registry keys: " + error);
                    return;
                }
            }

            if (Cancelled)
                return;

            if (SelectedSymantecLayer.Value.IsNew || Export.IsolationRulesWereUpdated)
            {
                var error = SymantecLayers.Value.ExportRules(IsolationRules.Value, SelectedSymantecLayer.Value.Guid, ProgressDialog,
                                                         95, 100);
                if (error != Declarations.FSL2_ERROR_SUCCESS)
                {
                    ProgressDialog.LogError("Error adding isolation rules: " + Declarations.FslErrorToString(error));
                    return;
                }
            }

            SymantecLayers.Value.ReloadLayers();

            if (SymantecLayers.Value.ResetLayer(SelectedSymantecLayer.Value.Guid) != Declarations.FSL2_ERROR_SUCCESS)
            {
                ProgressDialog.LogError("Error resetting layer: " + Declarations.FslErrorToString(Declarations.GetLastError()));
                return;
            }

            {
                var servicesResult = WriteServicesToRealRegistry();

                if (servicesResult != null && servicesResult.Any(x => !x.Value))
                {
                    ProgressDialog.LogError("One or more services failed to be exported.");
                    foreach (var service in servicesResult.Where(x => !x.Value).Select(x => x.Key))
                        ProgressDialog.LogError("Service " + service + " failed to be exported.");
                }
            }

            SelectedSymantecLayer.Value.IsNew = false;
            SelectedSymantecLayer.Value.SaveAll();

            ProgressDialog.SetProgress(100);
        }

        public override void Stop()
        {
            Cancelled = true;
        }

        #endregion
    }
}
