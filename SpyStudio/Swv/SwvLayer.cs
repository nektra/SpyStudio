using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using SpyStudio.Export.SWV;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Swv.Registry;
using SpyStudio.Tools;
using System.Linq;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Swv
{
    public class SwvLayer : IVirtualPackage, SwvLayerLikeThing
    {
        #region Fields

        private readonly string _guid;
        private uint _errorCode;
        private ManagementObjectCollection _deviceInfos;

        #endregion

        #region Properties

        protected string MainRegistrySubKey { get { return "SOFTWARE\\Nektra\\SpyStudio"; } }

        public uint ErrorCode
        {
            get { return _errorCode; }
        }

        public string Guid
        {
            get { return _guid; }
        }

        public string PeerGuid;

        public string Name { get; protected set; }

        public bool IsNew { get; set; }

        public string RegistryPath { get; protected set; }
        public string RegistryRwPath { get; protected set; }

        public HashSet<FileEntry> Files { get; protected set; }
        public HashSet<RegInfo> RegInfos { get; protected set; }
        public HashSet<SwvIsolationRuleEntry> IsolationRules { get; protected set; }

        protected ManagementObjectCollection DeviceInfos
        {
            get { return _deviceInfos ?? (_deviceInfos = new ManagementObjectSearcher("Select * from Win32_Volume").Get()); }
        }
        protected ManagementBaseObject CachedDeviceInfo { get; set; }

        #endregion

        #region Instantiation

        private Declarations.FSL2_INFO? GetLayerInfo(string guid)
        {
            var ret = new Declarations.FSL2_INFO
            {
                fslGUID = guid,
            };
            ret.dwStructSize = (uint)Marshal.SizeOf(ret);

            _errorCode = Declarations.FSL2GetLayerInfo(_guid, ref ret);
            if (_errorCode != Declarations.FSL2_ERROR_SUCCESS)
            {
                Error.WriteLine("Error FSL2GetLayerInfo " + Declarations.FslErrorToString(_errorCode));
                return null; 
            }
            return ret;
        }

        public SwvLayer(string layerGuid)
        {
            _guid = layerGuid;

            var info = GetLayerInfo(_guid);

            if (info != null)
            {
                Name = info.Value.name;
                RegistryPath = info.Value.regPath;
                PeerGuid = info.Value.peerGUID;

                var path = @"HKEY_LOCAL_MACHINE\" + info.Value.regPath;
                path = path.Substring(0, path.LastIndexOf('\\'));

                var key = RegistryTools.GetKeyFromFullPath(path);

                if (key != null)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        var subkey = key.OpenSubKey(subKeyName);
                        if (subkey == null)
                            continue;
                        var id = subkey.GetValue("ID") as string;
                        if (id != null && id.Equals(PeerGuid, StringComparison.InvariantCultureIgnoreCase))
                            RegistryRwPath = subkey.Name;
                        subkey.Close();
                        if (RegistryRwPath != null)
                            break;
                    }
                }
            }

            Files = new HashSet<FileEntry>();
            RegInfos = new HashSet<RegInfo>();
            IsolationRules = new HashSet<SwvIsolationRuleEntry>();

            InitializeSettings();
        }

        public void InitializeSettings()
        {
            var layerRegistry = SwvLayerRegistry.Of(Guid);

            using (var editor = layerRegistry.Edit())
            {
                if (layerRegistry.LocalMachine.Contains(MainRegistrySubKey))
                    return;

                var layerKey = layerRegistry.LocalMachine.CreateSubKey(MainRegistrySubKey);
                if (layerKey != null)
                    layerKey.SetValue("IsNew", RegistryValueKind.String, true.ToString());

                IsNew = true;
            }
        }

        #endregion

        #region Control

        public void Rename(string aName)
        {
            if (aName == Name)
                return;

            var result = Declarations.FSL2RenameLayer(Guid, aName);

            Name = aName;

            Debug.Assert(result == Declarations.FSL2_ERROR_SUCCESS, "Error trying to rename a layer: " + result.ToString());
        }

        public bool Delete()
        {
            var hresult = Declarations.FSL2DeleteLayer(Guid, true);

            if (hresult != Declarations.FSL2_ERROR_SUCCESS)
            {
                Debug.Assert(false, "An error occurred when trying to delete a SWV Layer.\nError: " + Declarations.FslErrorToString(hresult) + "(" + hresult + ").");
                MessageBox.Show("An error occurred while trying to delete the ThinApp capture: " + Name, Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);

                return false;
            }

            return true;
        }

        #region Refresh

        public void RefreshAll()
        {
            RefreshSettings();
            RefreshFiles();
            RefreshRegistry();
            RefreshIsolationRules();
        }

        #region Settings
        
        public void RefreshSettings()
        {
            var layerRegistry = SwvLayerRegistry.Of(Guid);

            using (var editor = layerRegistry.Edit())
            {
                var layerKey = layerRegistry.LocalMachine.GetSubKey(MainRegistrySubKey);

                try
                {
                    IsNew = bool.Parse(layerKey.GetValue("IsNew", RegistryValueKind.String));
                }
                catch (RegKeyNotFoundException)
                {
                    IsNew = false;
                }
            }
        }

        #endregion

        #region Files

        protected void RefreshFiles()
        {
            Files.Clear();

            Files.AddRange(GetAllFilesFromLayer());
        }

        protected IEnumerable<FileEntry> GetAllFilesFromLayer()
        {
            var files = new List<FileEntry>();

            foreach (var rootDirectory in GetRootDirectories())
            {
                var filesInRootDirectory = GetFilesRecursivelyAt(rootDirectory.Path);
                if (!filesInRootDirectory.Any())
                    continue;
                files.Add(rootDirectory);
                files.AddRange(filesInRootDirectory);
            }

            return files;
        }

        protected IEnumerable<FileEntry> GetRootDirectories()
        {
            var directories = new List<FileEntry>();

            Declarations.WIN32_FIND_DATA fileData;
            var findHandle = Declarations.FSL2FindFirstFile(Guid, "*", out fileData);

            while (Declarations.FSL2FindNextFile(findHandle, out fileData))
            {
                if (fileData.cFileName == "." || fileData.cFileName == ".." || !fileData.cFileName.StartsWith("[_B_]"))
                    continue;

                var entry = FileEntry.From(fileData);
                entry.FileSystemPath = SwvPathNormalizer.GetInstance().Unnormalize(entry.Path);
                entry.Success = true;
                entry.Version = "";

                directories.Add(entry);
            }

            Declarations.FSL2FindClose(findHandle);

            return directories;
        }

        public string GetNonLayerPath(string layerPath)
        {
            // size of layer path cannot be greater than 100 characters
            var nonLayerPathSB = new StringBuilder(layerPath.Length + 100);

            var error = Declarations.FSL2GetFullNonLayerPath(Guid,
                                                           layerPath, nonLayerPathSB,
                                                         (uint)nonLayerPathSB.Capacity * sizeof(char));

            if (error != 0)
                Debug.Assert(false, "Error getting non layer path.");

            ReplaceVolumeIdWithDriveLetterIn(nonLayerPathSB);

            return nonLayerPathSB.ToString();
        }

        protected IEnumerable<FileEntry> GetFilesRecursivelyAt(string aPath)
        {
            var files = new List<FileEntry>();

            Declarations.WIN32_FIND_DATA fileData;
            var findHandle = Declarations.FSL2FindFirstFile(Guid, aPath + "\\*", out fileData);
            
            while (Declarations.FSL2FindNextFile(findHandle, out fileData))
            {
                if (fileData.cFileName == "." || fileData.cFileName == "..")
                    continue;

                var filePath = aPath + "\\" + fileData.cFileName;
                var fileSystemPath = SwvPathNormalizer.GetInstance().Unnormalize(filePath);

                var entry = FileEntry.ForPath(fileSystemPath, filePath);
                var nonLayerPath = GetNonLayerPath(filePath);
                entry.TryToSetFileInfoUsing(nonLayerPath);

                entry.Success = true;

                if (!entry.IsDirectory)
                    LoadExtraFileDataTo(entry);

                files.Add(entry);

                if (IsFolder(fileData))
                    files.AddRange(GetFilesRecursivelyAt(filePath));
            }

            Declarations.FSL2FindClose(findHandle);

            return files;
        }

        private void LoadExtraFileDataTo(FileEntry aFileEntry)
        {
            var nonLayerPath = GetNonLayerPath(aFileEntry.Path);

            string product, company, version, description;
            var originalFileName = product = company = version = description = string.Empty;
            FileSystemTools.GetFileProperties(nonLayerPath, ref originalFileName, ref product, ref company, ref version,
                                              ref description);

            aFileEntry.OriginalFileName = originalFileName;
            aFileEntry.Product = product;
            aFileEntry.Company = company;
            aFileEntry.Version = version;
            aFileEntry.Description = description;

            Debug.Assert(!string.IsNullOrEmpty(version));
        }

        protected bool IsFolder(Declarations.WIN32_FIND_DATA aFileData)
        {
            return (aFileData.dwFileAttributes & 16) == 16;
        }

        #endregion

        #region Registry

        protected void RefreshRegistry()
        {
            RegInfos.Clear();

            RegInfos.AddRange(GetAllRegistryFromLayer());
        }

        protected IEnumerable<RegInfo> GetAllRegistryFromLayer()
        {
            var regInfos = new List<RegInfo>();

            var layerRegistry = SwvLayerRegistry.Of(Guid);


            using (var editor = new LayerEditor(this))
            {
                regInfos.AddRange(layerRegistry.LocalMachine.GetAllSubKeysRecursively().Select(sk => sk.AsRegInfo()));
                regInfos.AddRange(layerRegistry.CurrentUser.GetAllSubKeysRecursively().Select(sk => sk.AsRegInfo()));
                regInfos.AddRange(
                    layerRegistry.LocalMachine64.GetAllSubKeysRecursively().Select(sk => sk.AsRegInfo()));
                regInfos.AddRange(layerRegistry.CurrentUser64.GetAllSubKeysRecursively().Select(sk => sk.AsRegInfo()));
            }

            return regInfos;
        }

        #endregion 

        #region Isolation Rules

        protected void RefreshIsolationRules()
        {
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryPath, true);
            if (key == null) 
                return;
            
            var rulesAsStrings = key.GetValue("IsolationRules") as string[] ?? new string[]{};

            foreach (var ruleAsString in rulesAsStrings)
                IsolationRules.Add(SwvIsolationRuleEntry.From(ruleAsString));

            key.Close();
        }

        #endregion

        #endregion

        #region Saving

        public void SaveAll()
        {
            SaveSettings();
        }

        #region Settings

        protected void SaveSettings()
        {
            var layerRegistry = SwvLayerRegistry.Of(Guid);

            using (var editor = new LayerEditor(this))
            {
                var settingsKey = layerRegistry.LocalMachine.GetSubKey(MainRegistrySubKey);
                settingsKey.SetValue("IsNew", RegistryValueKind.String, IsNew.ToString(CultureInfo.InvariantCulture));
            }
        }

        #endregion

        #region Files

        public void Add(IEnumerable<FileEntry> fileEntries)
        {
            foreach (var file in fileEntries)
                Add(file);
        }

        public void Add(FileEntry aFileEntry)
        {
            Declarations.FSL2DeleteFile(Guid, aFileEntry.Path);
            var addError = Declarations.FSL2AddFile(Guid, aFileEntry.FileSystemPath, aFileEntry.Path, 0, 0, 0);
            if (addError != Declarations.FSL2_ERROR_SUCCESS &&
                addError != Declarations.FSL2_ERROR_FILEPATH_ALREADY_EXISTS)
            {
                var dirName = Path.GetDirectoryName(aFileEntry.Path);

                addError = !CreateLayerDirectory(Guid, dirName)
                    ? Declarations.FSL2_ERROR_CREATE_DIRECTORY
                    : Declarations.FSL2AddFile(Guid, aFileEntry.FileSystemPath, aFileEntry.Path, 0, 0, 0);
            }
        }

        public void DeleteAllFiles()
        {
            RefreshFiles();
            foreach (var file in Files)
                Declarations.FSL2DeleteFile(Guid, file.Path);
        }

        #endregion

        #endregion

        protected bool CreateLayerDirectory(string aGuid, string aLayerPath)
        {
            var fulldir = "";
            var ret = true;

            if (!string.IsNullOrEmpty(aLayerPath))
            {
                var pathParts = aLayerPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var dir in pathParts)
                {
                    if (fulldir.Length != 0)
                        fulldir += "\\";
                    fulldir += dir;

                    // if special folder don't create it
                    if (!dir.StartsWith("[_B_]"))
                    {
                        if (!Declarations.FSL2CreateDirectory(Guid, fulldir, 0))
                        {
                            Error.WriteLine("Cannot create layer directory " + fulldir);
                            ret = false;
                        }
                    }
                }
            }

            return ret;
        }

        #endregion

        public bool IsValid()
        {
            return (_errorCode == Declarations.FSL2_ERROR_SUCCESS);
        }

        protected void ReplaceVolumeIdWithDriveLetterIn(StringBuilder aPath)
        {
            var deviceId = aPath.ToString().Substring(0, 49);

            RefreshCachedDeviceInfo(deviceId);

            aPath.Replace(deviceId, (string)CachedDeviceInfo["DriveLetter"] + "\\");
        }

        protected void RefreshCachedDeviceInfo(string deviceId)
        {
            if (CachedDeviceInfo != null && CachedDeviceInfo["DeviceId"].Equals(deviceId)) 
                return;

            foreach (var deviceInfo in DeviceInfos)
            {
                if (!deviceInfo["DeviceId"].Equals(deviceId))
                    continue;

                CachedDeviceInfo = deviceInfo;
                break;
            }
        }
    }
}