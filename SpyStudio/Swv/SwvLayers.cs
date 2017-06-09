using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using IWshRuntimeLibrary;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using SpyStudio.Export.SWV;
using SpyStudio.Export.ThinApp;
using SpyStudio.Registry.Infos;
using SpyStudio.Swv.Registry;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using File = System.IO.File;
using RegistryWin32 = Microsoft.Win32.Registry;
using System.Linq;
using SpyStudio.Extensions;

namespace SpyStudio.Swv
{
    public class SwvLayers
    {
        #region Fields

        private static bool _fslLibLoaded, _fslSdkSuccess;
        private static IntPtr _hFsl;

        private uint _lastErrorCode = Declarations.FSL2_ERROR_SUCCESS;
        private Declarations.FSL2_FIND _findData;
        private StringBuilder _findLayerId;
        private bool _cancelled;

        private static readonly HashSet<string> SystemFiles = new HashSet<string>
                                                                  {
                                                                      "kernel32.dll",
                                                                      "user32.dll",
                                                                      "gdi32.dll",
                                                                      "ntdll.dll",
                                                                      "advapi32.dll",
                                                                      "ole32.dll",
                                                                      "kernelbase.dll",
                                                                      "rpcrt4.dll",                                                                      
                                                                      "olecc.dll"
                                                                  };

        #endregion

        #region Events

        public event Action<string, string> EventExportStart;
        public event Action<string, string, string, bool> EventExportEnd;

        #endregion

        #region Properties

        public uint LastErrorCode
        {
            get { return _lastErrorCode; }
        }

        public List<FileEntry> AccessedFiles { get; set; }

        #endregion

        #region Instantiation

        public SwvLayers()
        {
            if (!_fslLibLoaded)
            {
                _fslLibLoaded = true;
                _hFsl = Declarations.LoadLibrary("fsllib32.dll");
                if (_hFsl == IntPtr.Zero)
                {
                    _lastErrorCode = Declarations.FSL2_ERROR_LOAD_LIBRARY;
                }
                else
                {
                    var productKey = new StringBuilder((int) Declarations.FSL2_MAX_KEY_LENGTH);
                    _lastErrorCode = Declarations.FSL2GetProductKey(Declarations.FSL2_PRODUCT_ID_SVS, productKey,
                                                                    (uint) productKey.Capacity);
                    if (_lastErrorCode == Declarations.FSL2_ERROR_SUCCESS)
                    {
                        uint numberOfDays = 0;
                        _lastErrorCode = Declarations.FSL2InitSystem(productKey.ToString(),
                                                                     Declarations.FSL2_PRODUCT_ID_SVS, ref numberOfDays);
                        if (_lastErrorCode == Declarations.FSL2_PRODUCT_KEY_VALID)
                        {
                            _lastErrorCode = Declarations.FSL2_ERROR_SUCCESS;
                            _fslSdkSuccess = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Export

        public uint ExportFiles(List<FileEntry> fileEntries,
                                SwvLayer layer, IExportProgressControl progressDialog, int minimum, int maximum)
        {
            var retCode = Declarations.FSL2_ERROR_SUCCESS;
            var totalItems = fileEntries.Count;
            var itemsProcessed = 0;

            _cancelled = false;

            if (fileEntries.Count > 0)
            {
                try{
                    using (var editor = new LayerEditor(layer))
                    {
                        if (EventExportStart != null)
                            EventExportStart("Exporting File System", "");

                        var status = "OK";
                        var success = true;

                        // First create directories
                        foreach (var entry in fileEntries.Where(f => f.IsDirectory))
                        {
                            success = CreateLayerDirectory(layer.Guid, entry.Path);

                            if (!success && EventExportEnd != null)
                            {
                                if (EventExportStart != null)
                                    EventExportStart(entry.FileSystemPath, entry.Path);
                                EventExportEnd(entry.FileSystemPath, entry.Path, "ERROR", false);
                            }

                            progressDialog.SetProgress(minimum + (maximum - minimum)*++itemsProcessed/totalItems);

                        }

                        foreach (var entry in fileEntries.Where(f => !f.IsDirectory))
                        {
                            var layerPath = entry.Path.AsNormalizedPath();
                            var fileSystemPath = entry.FileSystemPath.AsNormalizedPath();

                            if (_cancelled)
                                break;
                            var filepart = "";

                            success = false;
                            try
                            {
                                fileSystemPath = entry.FileSystemPath;
                                filepart = Path.GetFileName(fileSystemPath);
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                status = "Cannot convert DOS path " + ex.Message;
                            }

                            // skip system files
                            if (filepart != "" && success)
                            {
                                if (entry.IsShortcut)
                                {
                                    success = (CreateShortcut(layer.Guid, entry, out status) ==
                                               Declarations.FSL2_ERROR_SUCCESS);
                                    if (success)
                                        status = "OK";
                                }
                                else if (entry.IsFileCreated)
                                {
                                    success = (CreateFileInLayer(layer.Guid, entry, out status) ==
                                               Declarations.FSL2_ERROR_SUCCESS);
                                    if (success)
                                        status = "OK";
                                }
                                else
                                {
                                    var addError = AddFileToLayer(layer.Guid, fileSystemPath, layerPath);
                                    if (addError != Declarations.FSL2_ERROR_SUCCESS)
                                    {
                                        status = Declarations.FslErrorToString(addError);
                                        success = false;
                                    }
                                }
                            }
                            if (!success && EventExportEnd != null)
                            {
                                if (EventExportStart != null)
                                    EventExportStart(entry.FileSystemPath, entry.Path);
                                EventExportEnd(entry.FileSystemPath, entry.Path, status, false);
                            }

                            progressDialog.SetProgress(minimum + (maximum - minimum)*++itemsProcessed/totalItems);
                        }
                    }
                }
                catch (LayerEditor.CouldNotSwitchLayerEditStateException e)
                {
                    return e.ErrorCode;
                }
                if (_cancelled)
                {
                    retCode = Declarations.FSL2_ERROR_USER_CANCELLED;
                }
            }
            
            progressDialog.SetProgress(maximum);
            
            return retCode;
        }

        private static readonly string[] KeyPaths = new[]
                                                        {
                                                            "hkey_classes_root",
                                                            "hkey_current_user",
                                                            "hkey_local_machine"
                                                        };

        public static IntPtr OpenKey(string path, uint extraOptions)
        {
            path = path.ToLower().AsNormalizedPath() + "\\";
            IntPtr basicKey = IntPtr.Zero;
            uint i;
            for (i = 0; i < KeyPaths.Length; i++)
            {
                var keyPath = KeyPaths[i];
                if (path.StartsWith(keyPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    basicKey = (IntPtr) ((int)-(long)RegistryTools.HkeyMask | i);
                    break;
                }
            }
            if (basicKey == IntPtr.Zero)
                return basicKey;

            path = path.Substring(KeyPaths[i].Length + 1);
            //const uint KEY_ALL_ACCESS = 0x000F003F;
            const uint KEY_READ = 0x00020019;
            IntPtr ret;
            var error = Declarations.RegOpenKeyEx(basicKey, path, 0, KEY_READ | extraOptions, out ret);
            if (error != 0)
                return IntPtr.Zero;
            return ret;
        }

        static readonly Regex Wow64Regex = new Regex(@"(^.*)\\wow6432node(\\.*|$)", RegexOptions.IgnoreCase);

        private bool StripWow6432Node(ref string path)
        {
            var match = Wow64Regex.Match(path);
            if (match.Success)
            {
                path = match.Groups[1].ToString() + match.Groups[2].ToString();
                return true;
            }
            return false;
        }

        public int OverrideKeyBitnessPlacement(string path)
        {
            const uint KEY_WOW64_32KEY = 0x0200;
            string originalRealPath, realPath;
            IntPtr hkey;
            bool isWow;

            /*
            if (path.Contains("{0002034C-0000-0000-C000-000000000046}") != false ||
                path.Contains("{7CCA70DB-DE7A-4FB7-9B2B-52E2335A3B5A}") != false ||
                path.Contains("{3CA78EDC-E48A-4A21-9562-9245BF90CE3F}") != false ||
                path.Contains("KindMap") != false)
            {
                int o = 0;
            }
            */

            //check if original key exists
            hkey = OpenKey(path, 0);
            try
            {
                if (hkey == IntPtr.Zero)
                    return 0;
                try
                {
                    originalRealPath = RegistryTools.GetKeyPath(IntPtrTools.ToUlong(hkey));
                }
                catch (Exception)
                {
                    originalRealPath = "";
                }
            }
            finally
            {
                Declarations.CloseHandle(hkey);
            }
            if (originalRealPath.Length == 0)
                return 0;

            isWow = StripWow6432Node(ref path);

            hkey = OpenKey(path, KEY_WOW64_32KEY);
            try
            {
                if (hkey == IntPtr.Zero)
                    return isWow ? 32 : 64;
                try
                {
                    realPath = RegistryTools.GetKeyPath(IntPtrTools.ToUlong(hkey));
                }
                catch (Exception)
                {
                    realPath = "";
                }
            }
            finally
            {
                Declarations.CloseHandle(hkey);
            }

            if (realPath.Length == 0)
                return isWow ? 32 : 64;

            return realPath == originalRealPath ? 32 : 64;
        }

        public string ExportRegistry(List<RegKeyInfo> accessedKeys, SwvLayer layer,
                                   IExportProgressControl progressDialog, int minimum, int maximum)
        {
            _cancelled = false;
            if (accessedKeys.Count > 0)
            {
                if (!layer.IsValid())
                {
                    return Declarations.FslErrorToString(layer.ErrorCode);
                }

                if (EventExportStart != null)
                    EventExportStart("Exporting Registry", "");

                var tempFilename = FileSystemTools.GetTempFilename("ExpReg", "reg");

                var ret = ExportToDotReg(accessedKeys, layer, progressDialog, minimum, maximum, tempFilename);
                if (ret != null)
                    return ret;

                ret = ImportDotRegIntoLayer(layer, tempFilename);
                if (ret != null)
                    return ret;

                ret = ManuallyMergeRwLayerIntoRoLayer(layer);
                if (ret != null)
                    return ret;

                progressDialog.SetProgress(maximum);

                if (_cancelled)
                {
                    return Declarations.FslErrorToString(Declarations.FSL2_ERROR_USER_CANCELLED);
                }
            }

            progressDialog.SetProgress(maximum);
            
            return null;
        }

        private string ImportDotRegIntoLayer(SwvLayer layer, string expRegTemp)
        {
            if (0 != Declarations.FSL2ActivateLayerW(layer.Guid, true))
            {
                ReportError("Can't activate layer", "", "");
                return "Can't activate layer";
            }

            try
            {
                Declarations.PROCESS_INFORMATION pi;
                var si = new Declarations.STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                uint error = Declarations.VzCreateProcessFromLayerW(
                    layer.Guid,
                    IntPtr.Zero,
                    "regedit.exe /s \"" + expRegTemp + "\"",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    32,
                    IntPtr.Zero,
                    si,
                    out pi
                    );

                if (error != 0)
                {
                    var errorString = "Error executing Regedit (" + error + ")";
                    ReportError("Executing Regedit", "", errorString);
                    return errorString;
                }
                else
                    Declarations.WaitForSingleObject(pi.hProcess, uint.MaxValue);
                Declarations.CloseHandle(pi.hProcess);
                Declarations.CloseHandle(pi.hThread);
            }
            catch (Exception ex)
            {
                ReportError("Executing Regedit", "", ex.Message);
                return ex.Message;
            }
            finally
            {
                Declarations.FSL2DeactivateLayerW(layer.Guid, false, IntPtr.Zero);
            }
            return null;
        }

        private string ExportToDotReg(List<RegKeyInfo> accessedKeys, SwvLayer layer, IExportProgressControl progressDialog, int minimum,
            int maximum, string expRegTemp)
        {
            var thisMaximum = minimum + (maximum - minimum)/2;
            try
            {
                using (var editor = new LayerEditor(layer))
                {
                    ProcessKeysForDotRegExport(accessedKeys, progressDialog, minimum, thisMaximum);
                    progressDialog.SetProgress(thisMaximum);

                    try
                    {
                        File.Delete(expRegTemp);
                    }
                    catch (IOException) {}

                    using (var file = new StreamWriter(expRegTemp, false, Encoding.Unicode))
                    {
                        if (!RegistryTools.ExportToReg(accessedKeys, file))
                        {
                            ReportError("Exporting to reg", "", "Error exporting registry");
                            return "Error creating .REG file.";
                        }
                    }
                }
            }
            catch (LayerEditor.CouldNotSwitchLayerEditStateException e)
            {
                return Declarations.FslErrorToString(e.ErrorCode);
            }
            return null;
        }

        private static readonly string[] LayerBasicKeys =
        {
            "HLM",
            "HLM64",
            "HU",
            "HU64",
        };

        private string ManuallyMergeRwLayerIntoRoLayer(SwvLayer layer)
        {
            string layerDir,
                   layerRwDir;

            try
            {
                var index = layer.RegistryPath.LastIndexOf('\\');
                if (index == -1)
                    return Declarations.FslErrorToString(Declarations.FSL2_ERROR_INVALID_GUID);
                layerDir = layer.RegistryPath.Substring(index + 1);

                index = layer.RegistryRwPath.LastIndexOf('\\');
                if (index == -1)
                    return Declarations.FslErrorToString(Declarations.FSL2_ERROR_INVALID_GUID);
                layerRwDir = layer.RegistryRwPath.Substring(index + 1);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            try
            {
                using (var editor = new LayerEditor(layer))
                using (var editor2 = new LayerEditor(layer.PeerGuid))
                    LayerBasicKeys.ForEach(name => DoManualMerge(layerRwDir, layerDir, name));
            }
            catch (LayerEditor.CouldNotSwitchLayerEditStateException e)
            {
                return Declarations.FslErrorToString(e.ErrorCode);
            }
            return null;
        }

        private static void DoManualMerge(string layerRwDir, string layerDir, string part)
        {
            var sourcePath = @"HKEY_LOCAL_MACHINE\_SWV_LAYER_" + layerRwDir + @"\" + part;
            var sourcePathPattern = @"(HKEY_LOCAL_MACHINE\\_SWV_LAYER_)" + layerRwDir + @"(\\" + part + @"(?:\\.*)?)";

            var filename = FileSystemTools.GetTempFilename("ExpRegB", "reg");
            {
                var proc = new Process();
                var psi = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "regedit.exe",
                    Arguments = "/e \"" + filename + "\" " + sourcePath
                };
                proc.StartInfo = psi;
                proc.Start();
                proc.WaitForExit();
            }

            var lines = new List<string>();
            using (var file = new StreamReader(filename))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                    lines.Add(line);
            }

            var regex = new Regex("^\\[" + sourcePathPattern + "\\]$", RegexOptions.IgnoreCase);

            for (var i = 0; i < lines.Count; i++)
            {
                var match = regex.Match(lines[i]);
                if (!match.Success)
                    continue;

                lines[i] = "[" + match.Groups[1].ToString() + layerDir + match.Groups[2].ToString() + "]";
            }

            using (var file = new StreamWriter(filename, false))
                lines.ForEach(file.WriteLine);

            {
                var proc = new Process();
                var psi = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "regedit.exe",
                    Arguments = "/s \"" + filename + "\""
                };
                proc.StartInfo = psi;
                proc.Start();
                proc.WaitForExit();
            }
        }

#if false
        private void ProcessKeysForDotRegExport(IEnumerable<RegKeyInfo> accessedKeys, IExportProgressControl progressDialog, int minimum, int totalItems,
                              string layerDir, int thisMaximum)
        {
            var itemsProcessed = 0;
            /*
            int index;
            var layerLm32 = @"_SWV_LAYER_" + layerDir + @"\HLM";
            var layerCu = @"_SWV_LAYER_" + layerDir + @"\HU\USER_TEMPLATE";
            var layerLm = layerLm32;
            var layerLm64 = layerLm + "64";
            var layerCu64 = @"_SWV_LAYER_" + layerDir + @"\HU64\USER_TEMPLATE";
            */

            foreach (var regKey in accessedKeys)
            {
                /*
                var regPath = regKey.OriginalPath;
                if (string.IsNullOrEmpty(regPath))
                    regPath = regKey.Path;
                regKey.BasicKeyHandle = RegistryTools.GetBasicHandleFromPath(regPath);
                regPath = RegistryTools.NormalizeWowSubPaths(regPath);

                layerLm = IntPtr.Size == 8 ? layerLm64 : layerLm32;

                {
                    var overridePlacement = OverrideKeyBitnessPlacement(regPath);
                    if (overridePlacement != 0)
                        layerLm = overridePlacement == 64 ? layerLm64 : layerLm32;
                }

                StripWow6432Node(ref regPath);

                var subKey = string.Empty;
                index = regPath.IndexOf('\\');
                if (index != -1)
                {
                    subKey = regPath.Substring(index);
                }

                if (regKey.BasicKeyHandle == RegistryTools.HkeyLocalMachine)
                {
                    regKey.BasicKeyHandle = RegistryTools.HkeyLocalMachine;
                    regKey.Path = RegistryTools.GetBasicKeyHandleString(regKey.BasicKeyHandle) + @"\" +
                                  layerLm +
                                  subKey;
                }
                else if (regKey.BasicKeyHandle == RegistryTools.HkeyLocalMachine64)
                {
                    regKey.BasicKeyHandle = RegistryTools.HkeyLocalMachine;
                    regKey.Path = RegistryTools.GetBasicKeyHandleString(regKey.BasicKeyHandle) + @"\" +
                                  layerLm64 +
                                  subKey;
                }
                else if (regKey.BasicKeyHandle == RegistryTools.HkeyCurrentUser)
                {
                    regKey.BasicKeyHandle = RegistryTools.HkeyLocalMachine;
                    regKey.Path = RegistryTools.GetBasicKeyHandleString(regKey.BasicKeyHandle) + @"\" +
                                  layerCu +
                                  subKey;
                }
                else if (regKey.BasicKeyHandle == RegistryTools.HkeyCurrentUser64)
                {
                    regKey.BasicKeyHandle = RegistryTools.HkeyLocalMachine;
                    regKey.Path = RegistryTools.GetBasicKeyHandleString(regKey.BasicKeyHandle) + @"\" +
                                  layerCu64 +
                                  subKey;
                }
                else if (regKey.BasicKeyHandle == RegistryTools.HkeyClassesRoot)
                {
                    regKey.BasicKeyHandle = RegistryTools.HkeyLocalMachine;
                    regKey.Path = RegistryTools.GetBasicKeyHandleString(regKey.BasicKeyHandle) + @"\" +
                                  layerLm +
                                  @"\Software\Classes" +
                                  subKey;
                }
                else if (regKey.BasicKeyHandle == RegistryTools.HkeyClassesRoot64)
                {
                    regKey.BasicKeyHandle = RegistryTools.HkeyLocalMachine;
                    regKey.Path = RegistryTools.GetBasicKeyHandleString(regKey.BasicKeyHandle) + @"\" +
                                  layerLm64 + @"\Software\Classes" +
                                  subKey;
                }
                */

                foreach (var value in regKey.ValuesByName.Values)
                {
                    value.NormalizeDataForExport(SwvPathNormalizer.GetInstance());
                }

                progressDialog.SetProgress(minimum + (thisMaximum - minimum)*++itemsProcessed/totalItems);
            }
        }
#endif

        private void ProcessKeysForDotRegExport(List<RegKeyInfo> accessedKeys, IExportProgressControl progressDialog, int minimum,
                              int thisMaximum)
        {
            var totalItems = accessedKeys.Count;

            var n = SwvPathNormalizer.GetInstance();
            for (var i = 0; i < accessedKeys.Count; i++)
            {
                accessedKeys[i].ValuesByName.Values.ForEach(value => value.NormalizeDataForExport(n));
                progressDialog.SetProgress(minimum + (thisMaximum - minimum) * ++i / totalItems);
            }
        }

        public uint ExportRules(List<SwvIsolationRuleEntry> ruleEntries,
                                string guid, IExportProgressControl progressDialog, int minimum, int maximum)
        {
            var retCode = Declarations.FSL2_ERROR_SUCCESS;

            //_specialDirs = PlatformTools.IsPlatform64Bits() ? SpecialDirs64 : SpecialDirs32;

            if (ruleEntries.Count > 0)
            {
                if (EventExportStart != null)
                    EventExportStart("Isolation Rules", "");

                retCode = SetRules(guid, ruleEntries);
                var success = (retCode == Declarations.FSL2_ERROR_SUCCESS);

                if (EventExportEnd != null)
                    EventExportEnd("Isolation Rules", "", success ? "OK" : "ERROR",
                                                                 success);

                Declarations.FSL2ReloadIsolationRules();
            }
            
            progressDialog.SetProgress(maximum);
            

            return retCode;
        }

        #endregion

        #region Layer Control

        private uint CreateShortcut(string guid, FileEntry entry, out string error)
        {
            var wshShell = new WshShellClass();
            error = String.Empty;

            string nonLayerPathShortcut;
            var retCode = GetNonLayerPath(guid, entry.Path, out nonLayerPathShortcut);
            if (retCode == Declarations.FSL2_ERROR_SUCCESS)
            {
                var entryShortcut = (IWshShortcut)wshShell.CreateShortcut(nonLayerPathShortcut);

                var targetPath = GetBasePath(entry.TargetPath);
                if (!String.IsNullOrEmpty(targetPath))
                {
                    //entry.FileSystemPath = 
                    try
                    {
                        // HACK: I cannot create a shortcut to an invalid target. The target is invalid because the layer isn't activated at this point 
                        // so the file isn't in the target path. To create the shortcut I create a shortcut to the cmd.exe and put the real target path
                        // to be started by the cmd (e.g.: C:\windows\system32\cmd.exe start "" "c:\program files\internet explorer\iexplore.exe"
                        entryShortcut.TargetPath = Environment.ExpandEnvironmentVariables("%SystemRoot%") +
                                                   @"\System32\cmd.exe";
                        entryShortcut.Arguments = "/c start " + "\"\" \"" + targetPath + "\"";
                        entryShortcut.WindowStyle = 7; // minimized
                        entryShortcut.Description = entry.Description;
                        entryShortcut.IconLocation = targetPath;

                        entryShortcut.Save();
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        retCode = Declarations.FSL2_ERROR_INVALID_ARCHIVE;
                    }
                }
            }

            return retCode;
        }

        private uint CreateFileInLayer(string guid, FileEntry entry, out string error)
        {
            error = String.Empty;

            string nonLayerPath;
            var retCode = GetNonLayerPath(guid, entry.Path, out nonLayerPath);
            if (retCode == Declarations.FSL2_ERROR_SUCCESS)
            {
                //entry.FileSystemPath = 
                try
                {
                    File.Create(nonLayerPath);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    retCode = Declarations.FSL2_ERROR_INVALID_ARCHIVE;
                }
            }

            return retCode;
        }

        public bool CreateLayerDirectory(string guid, string layerpath)
        {
            var fulldir = "";
            var ret = true;

            if (!String.IsNullOrEmpty(layerpath))
            {
                var pathParts = layerpath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var dir in pathParts)
                {
                    if (fulldir.Length != 0)
                        fulldir += "\\";
                    fulldir += dir;

                    // if special folder don't create it
                    if (!dir.StartsWith("[_B_]"))
                    {
                        if (!Declarations.FSL2CreateDirectory(guid, fulldir, 0))
                        {
                            Error.WriteLine("Cannot create layer directory " + fulldir);
                            ret = false;
                        }
                    }
                }
            }

            return ret;
        }

        public uint AddFileToLayer(string guid, string filepath, string layerpath)
        {
            Declarations.FSL2DeleteFile(guid, layerpath);
            var addError = Declarations.FSL2AddFile(guid, filepath, layerpath, 0, 0, 0);
            if (addError != Declarations.FSL2_ERROR_SUCCESS &&
                addError != Declarations.FSL2_ERROR_FILEPATH_ALREADY_EXISTS)
            {
                var dirName = Path.GetDirectoryName(layerpath);

                addError = !CreateLayerDirectory(guid, dirName)
                    ? Declarations.FSL2_ERROR_CREATE_DIRECTORY
                    : Declarations.FSL2AddFile(guid, filepath, layerpath, 0, 0, 0);
            }

            string nonLayer;
            GetNonLayerPath(guid, layerpath, out nonLayer);

            return addError;
        }

        public void ReloadLayers()
        {
            Declarations.FSL2ReloadIsolationRules();
            Declarations.FSL2ReloadKeepInLayerList();
        }

        public uint ResetLayer(string guid)
        {
            uint pid;
            return Declarations.FSL2ResetPeer(guid, true, out pid);
        }

        public SwvLayer CreateLayer(string name)
        {
            SwvLayer ret = null;
            var guid = new StringBuilder((int)Declarations.FSL2_MAXIDLEN);
            _lastErrorCode = Declarations.FSL2CreateLayer(name, 0, true, guid);
            if (_lastErrorCode == Declarations.FSL2_ERROR_SUCCESS)
            {
                ret = new SwvLayer(guid.ToString());
            }
            return ret;
        }

        public void RenameLayer(string guid, string newName)
        {
            _lastErrorCode = Declarations.FSL2RenameLayer(guid, newName);
        }

        public void DeleteLayer(string guid)
        {
            _lastErrorCode = Declarations.FSL2DeleteLayer(guid, true);
        }

        public SwvLayer GetFirstLayer()
        {
            if (!_fslSdkSuccess)
                return null;

            SwvLayer layer = null;

            _findData = new Declarations.FSL2_FIND { dwStructSize = (uint)Marshal.SizeOf(typeof(Declarations.FSL2_FIND)) };

            _findLayerId = new StringBuilder((int)Declarations.FSL2_MAX_KEY_LENGTH);
            _lastErrorCode = Declarations.FSL2FindFirstLayer(ref _findData, _findLayerId, 0);

            if (_lastErrorCode == Declarations.FSL2_ERROR_SUCCESS)
            {
                layer = new SwvLayer(_findLayerId.ToString());
            }

            return layer;
        }

        public SwvLayer GetNextLayer()
        {
            if (!_fslSdkSuccess)
                return null;

            SwvLayer layer = null;
            if (_lastErrorCode == Declarations.FSL2_ERROR_SUCCESS)
            {
                _findData.dwStructSize = (uint)Marshal.SizeOf(typeof(Declarations.FSL2_FIND));

                _lastErrorCode = Declarations.FSL2FindNextLayer(ref _findData, _findLayerId);

                if (_lastErrorCode == Declarations.FSL2_ERROR_SUCCESS)
                {
                    layer = new SwvLayer(_findLayerId.ToString());
                }
            }
            return layer;
        }

        #endregion

        public void CloseFind()
        {
            if (!_fslSdkSuccess)
                return;

            Declarations.FSL2FindCloseLayer(ref _findData);
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        public uint GetNonLayerPath(string fslGuid, string layerPath, out string nonLayerPath)
        {
            // size of layer path cannot be greater than 100 characters
            var nonLayerPathSB = new StringBuilder(layerPath.Length + 100);
        
            nonLayerPath = "";
            var ret = Declarations.FSL2GetFullNonLayerPath(fslGuid,
                                                           layerPath, nonLayerPathSB,
                                                           (uint) nonLayerPathSB.Capacity*sizeof (char));
            if (ret == Declarations.FSL2_ERROR_SUCCESS)
                nonLayerPath = nonLayerPathSB.ToString();
            return ret;
        }

        public void ReportError(string path1, string path2, string error)
        {
            if (EventExportStart != null)
                EventExportStart(path1, path2);
            EventExportEnd(path1, path2, error, false);
        }

        public static string GetBasePath(string path)
        {
            var desvarPath = new StringBuilder(path.Length + 100);
            var errorCode = Declarations.FSL2DevariablizePath(path, desvarPath, (uint) desvarPath.Capacity*sizeof (char));
            if (errorCode != Declarations.FSL2_ERROR_SUCCESS)
            {
                Debug.Assert(true);
                return String.Empty;
            }

            return desvarPath.ToString();
        }

        public static string GetSwvPath(string path, out bool error, out string filepart, out bool systemFile,
                                        out bool foundRootPath)
        {
            string layerPath;

            foundRootPath = true;

            try
            {
                layerPath = Path.GetFullPath(path);
                filepart = Path.GetFileName(layerPath);
                error = false;
            }
            catch (Exception ex)
            {
                Error.WriteLine("Error normalizing path of " + path + " : " + ex.Message);
                systemFile = false;
                error = true;
                filepart = String.Empty;
                return String.Empty;
            }

            Debug.Assert(filepart != null, "filepart != null");
            systemFile = SystemFiles.Contains(filepart.ToLower());

            var varPath = new StringBuilder(path.Length + 100);
            var errorCode = Declarations.FSL2VariablizePath(path, varPath, (uint) varPath.Capacity*sizeof (char));
            if (errorCode != Declarations.FSL2_ERROR_SUCCESS)
            {
                error = true;
                foundRootPath = false;
                layerPath = String.Empty;
            }
            else
            {
                layerPath = varPath.ToString();
            }

            return layerPath;
        }

        public uint SetRules(string guid, IEnumerable<SwvIsolationRuleEntry> rules)
        {
            var layer = new SwvLayer(guid);
            if (!layer.IsValid())
                return layer.ErrorCode;

            var ruleStrings = new List<string>();
            foreach (var r in rules)
            {
                if (r.Type == SwvIsolationRuleType.BaseCannotSeeLayerKey)
                {
                    ruleStrings.Add(r.ProcessWildcard + "\tBASE\t0x0002\t\\REGISTRY\\" + r.KeyWildcard +
                                    "\t*\t" + guid.ToLower());
                }
                else
                {
                    ruleStrings.Add(r.ProcessWildcard + "\t" + guid.ToLower() + "\t0x0002\t\\REGISTRY\\" +
                                    r.KeyWildcard + "\t*\tBASE");
                }
            }
            try
            {
                var key = RegistryWin32.LocalMachine.OpenSubKey(layer.RegistryPath, true);
                if (key != null)
                {
                    key.SetValue("IsolationRules", ruleStrings.ToArray());
                    key.Close();
                }
            }
            catch (Exception)
            {
                return Declarations.FSL2_ERROR_METADATA_KEY;
            }
            return Declarations.FSL2_ERROR_SUCCESS;
        }

        public static SwvIsolationRuleEntry GetRuleFromString(string r)
        {
            SwvIsolationRuleEntry isolationRuleEntry = null;

            // Process Wildcard
            var index = r.IndexOf("\t", StringComparison.InvariantCulture);
            if (index != -1)
            {
                isolationRuleEntry = new SwvIsolationRuleEntry { ProcessWildcard = r.Substring(0, index) };

                // layer guid or BASE
                var lastIndex = index + 1;
                index = r.IndexOf("\t", lastIndex, StringComparison.InvariantCulture);
                if (index != -1)
                {
                    var layerIdentifier = r.Substring(lastIndex, index - lastIndex);
                    isolationRuleEntry.Type =
                        string.Compare(layerIdentifier, "BASE", StringComparison.InvariantCultureIgnoreCase) ==
                        0
                            ? SwvIsolationRuleType.BaseCannotSeeLayerKey
                            : SwvIsolationRuleType.LayerCannotSeeBaseKey;
                    lastIndex = index + 1;

                    // skip 0x0002
                    index = r.IndexOf("\t", lastIndex, StringComparison.InvariantCulture);
                    if (index != -1)
                    {
                        lastIndex = index + 1;
                        // Key Wildcard
                        index = r.IndexOf("\t", lastIndex, StringComparison.InvariantCulture);
                        if (index != -1)
                        {
                            isolationRuleEntry.KeyWildcard = r.Substring(lastIndex, index - lastIndex);
                            if (isolationRuleEntry.KeyWildcard.ToLower().StartsWith(@"\registry\"))
                                isolationRuleEntry.KeyWildcard = isolationRuleEntry.KeyWildcard.Substring(@"\registry\".Length);
                        }
                    }
                }
            }
            return isolationRuleEntry;
        }
    }
}