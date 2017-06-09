using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using SpyStudio.FileSystem;
using SpyStudio.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppCapture : IVirtualPackage
    {
        #region Private Fields

        private string _path;
        private string _compressionType;
        #endregion

        #region Properties

        protected string MainRegistrySubKey { get { return "SOFTWARE\\Nektra\\SpyStudio"; } }
        protected string ThinAppCapturesSubKey { get { return MainRegistrySubKey + "\\ThinAppCaptures"; } }

        public string Path { get { return _path; } protected set { _path = value; RefreshName(); } }

        public string Name { get; protected set; }
        public bool IsNew { get; protected set; }

        public HashSet<FileEntry> Files { get; protected set; }
        public HashSet<RegInfo> RegInfos { get; protected set; }
        public HashSet<EntryPoint> EntryPoints { get; protected set; }

        public PathNormalizer PathNormalizer { get { return ThinAppPathNormalizer.GetInstance(); } }

        public ThinAppIsolationOption DefaultDirectoryIsolation { get; set; }
        public ThinAppIsolationOption DefaultRegistryIsolation { get; set; }
        public Dictionary<string, ThinAppIsolationOption> FileIsolationOptionsCache { get; protected set; }

        protected string PackageIniPath { get { return Path + @"\Package.ini"; } }

        public string CompressionType { get { return _compressionType ?? "None"; } set { _compressionType = value; } }
        public string ThinAppVersion { get { return "4.7.3-891762"; } }
        public ThinAppOptions Options { get; protected set; }

        public long TotalFileBytes { get { return new DirectoryInfo(Path).GetDirectories().Where(d => d.Name.StartsWith("%")).Sum(d => d.GetSize()); } }

        protected readonly string[] ThinAppRegValueTypes = new[] { "REG_BINARY", "REG_DWORD", "REG_EXPAND_SZ", "REG_MULTI_SZ", "REG_QWORD", "REG_SZ" };

        #region Package.ini Format

        protected const string PackageIniFormat =
                @"[Compression]
CompressionType={0}

[Isolation]
DirectoryIsolationMode={1}
RegistryIsolationMode={2}

[BuildOptions]
AccessDeniedMsg=You are not currently authorized to run this application. Please contact your administrator.
CapturedUsingVersion={3}
OutDir=bin

SandboxName={4}
InventoryName={5}
RemoveSandboxOnExit={9}
RemoveSandboxOnStart={10}

VirtualDrives=Drive=c, Serial=deadbeef, Type=FIXED

AnsiCodePage={6}
LocaleIdentifier={7}
LocaleName={8}

QualityReportingEnabled=0

";

        #endregion

        #endregion

        #region Instantiation

        public static ThinAppCapture At(string aPath)
        {
            var capture = new ThinAppCapture();

            capture.Path = aPath;
            capture.RefreshSettings();

            return capture;
        }

        protected ThinAppCapture()
        {
            Files = new HashSet<FileEntry>();
            RegInfos = new HashSet<RegInfo>();
            EntryPoints = new HashSet<EntryPoint>();

            FileIsolationOptionsCache = new Dictionary<string, ThinAppIsolationOption>();

            DefaultDirectoryIsolation = ThinAppIsolationOption.DefaultFileSystemIsolation;
            DefaultRegistryIsolation = ThinAppIsolationOption.DefaultRegistryIsolation;

            Options = new ThinAppOptions();
        }

        #endregion

        #region Control

        #region IVirtualPackage Implementation

        public void Rename(string aName)
        {
            Path = Path.Substring(0, Path.Length - Name.Length) + aName;
        }

        public bool Delete()
        {
            // ThinApp captures are not generated until the export is done, so there is nothing to delete for IsNew-flagged captures
            if (IsNew) 
                return true;

            try
            {
                Directory.Delete(Path, true);
            }
            catch (Exception e)
            {
                Debug.Assert(false, "An exception occurred when trying to delete ThinApp capture.\nException: " + e.Message);
                MessageBox.Show("An error occurred while trying to delete ThinApp capture: " + Name, Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return false;
            }

            return true;
        }

        #endregion

        #region Refreshing

        public void RefreshAll()
        {
            RefreshName();
            RefreshFiles();
            RefreshRegistry();
            RefreshEntryPoints();
            RefreshSettings();
        }

        public void RefreshName()
        {
            Name = System.IO.Path.GetFileName(Path);
        }

        #region Settings

        public void RefreshSettings()
        {
            IsNew = !Directory.Exists(Path);
        }

        #endregion

        #region Registry

        public void RefreshRegistry()
        {
            RegInfos.Clear();

            DefaultRegistryIsolation = GetDefaultRegistryIsolation();

            if (!Directory.Exists(Path))
                return;

            var registryFiles = Directory.GetFiles(Path).Where(f => System.IO.Path.GetFileName(f).StartsWith("HKEY"));

            foreach (var registryFile in registryFiles)
                RegInfos.AddRange(GetRegInfosFrom(registryFile));

            Debug.Assert(RegInfos.All(info => info is RegValueInfo || info is ThinAppRegKeyInfo), "All RegKeyInfos in an ThinApp capture should be ThinAppRegKeyInfos.");
        }

        // WARNING: There can be duplicated keys or values in the ThinApp Registry files.
        private IEnumerable<RegInfo> GetRegInfosFrom(string aFile)
        {
            var regInfos = new List<RegInfo>();

            var fileLines = File.ReadAllLines(aFile).ToList();

            for (var i = 0; i < fileLines.Count; i++)
            {
                var line = fileLines[i];

                if (!line.StartsWith("isolation"))
                    continue;

                var isolationAndKey = line.Split(' ');

                var isolation = GetThinAppIsolationFrom(isolationAndKey[0]);
                var key = isolationAndKey[1];
                var isValue = fileLines[i + 1].TrimStart().StartsWith("Value");

                if (isValue)
                {
                    var info = new RegValueInfo();

                    info.Path = info.OriginalPath = key;

                    var valueName = fileLines[++i].TrimStart().Split(' ');

                    info.Name = valueName.Count() == 2 ? valueName[1] : "";

                    i++;

                    string[] valueTypeAndData;

                    if (!fileLines[i].Contains('~'))
                        valueTypeAndData = fileLines[i].TrimStart().Split('=');
                    else if (!fileLines[i].Contains('='))
                        valueTypeAndData = fileLines[i].TrimStart().Split('~');
                    else
                    {
                        valueTypeAndData = fileLines[i].TrimStart().Split('=');

                        if (!ThinAppRegValueTypes.Contains(valueTypeAndData[0]))
                            valueTypeAndData = fileLines[i].TrimStart().Split('~');
                    }

                    info.ValueType = GetValueTypeFrom(valueTypeAndData[0]);

                    info.Data = info.ValueType == RegistryValueKind.DWord || info.ValueType == RegistryValueKind.QWord
                                    ? Decode(valueTypeAndData[1])
                                    : valueTypeAndData[1];

                    info.Success = true;

                    regInfos.Add(info);

                    var parentKeyInfo = ThinAppRegKeyInfo.From(RegKeyInfo.ParentOf(info));
                    parentKeyInfo.Isolation = isolation;
                    regInfos.Add(parentKeyInfo);
                }
                else
                {
                    var info = new ThinAppRegKeyInfo();

                    info.Path = info.OriginalPath = key;
                    info.OriginalKeyPaths.Add(key, true);
                    info.Name = key.TrimEnd('\\').Split('\\').Last();
                    info.Isolation = GetThinAppIsolationFrom(isolationAndKey[0]);
                    info.Success = true;

                    regInfos.Add(info);
                }
            }

            // Since the capture registry files can have the same key several times and we should take the last one, we reverse the list.
            regInfos.Reverse();

            return regInfos;
        }

        private string Decode(string aDorQWord)
        {
            // Notation is in Little-Endian, so we reverse the sequence
            var codedBytes = aDorQWord.Split(new[]{'#'}, StringSplitOptions.RemoveEmptyEntries).Reverse();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("0x");

            foreach (var codedByte in codedBytes)
                stringBuilder.Append(codedByte);

            return stringBuilder.ToString();
        }

        private RegistryValueKind GetValueTypeFrom(string aValueTypeAsString)
        {
            switch (aValueTypeAsString)
            {
                case "REG_BINARY":
                    return RegistryValueKind.Binary;

                case "REG_DWORD":
                    return RegistryValueKind.DWord;

                case "REG_EXPAND_SZ":
                    return RegistryValueKind.ExpandString;

                case "REG_MULTI_SZ":
                    return RegistryValueKind.MultiString;
                
                case "REG_QWORD":
                    return RegistryValueKind.QWord;

                case "REG_SZ":
                    return RegistryValueKind.String;

                default:
                    Debug.Assert(false, "Can't parse registry value type.");
                    return RegistryValueKind.Unknown;
            }
        }

        private ThinAppIsolationOption GetThinAppIsolationFrom(string anIsolationAsString)
        {
            switch (anIsolationAsString)
            {
                case "isolation_writecopy":
                    return ThinAppIsolationOption.WriteCopy;

                case "isolation_merged":
                    return ThinAppIsolationOption.Inherit;

                case "isolation_full":
                    return ThinAppIsolationOption.Full;

                default:
                    Debug.Assert(false, "Can't parse registry isolation type");
                    return ThinAppIsolationOption.None;
            }
        }

        private ThinAppIsolationOption GetDefaultRegistryIsolation()
        {
            if (!File.Exists(PackageIniPath))
                return ThinAppIsolationOption.WriteCopy;

            var packageIniLines = File.ReadAllLines(PackageIniPath);

            var isolationOptionString = packageIniLines.First(line => line.StartsWith(@"RegistryIsolationMode=")).Substring(
                    @"RegistryIsolationMode=".Length);

            return (ThinAppIsolationOption)Enum.Parse(typeof(ThinAppIsolationOption), isolationOptionString);

        }

        #endregion

        #region Entry Points

        public void RefreshEntryPoints()
        {
            EntryPoints.Clear();

            if (!Directory.Exists(Path) || !File.Exists(PackageIniPath))
                return;

            var packageIniFileLines = File.ReadAllLines(PackageIniPath).ToList();

            for (var i = 0; i < packageIniFileLines.Count; i++)
            {
                var line = packageIniFileLines[i];

                if (!line.EndsWith(".exe]", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var inferredName = line.Substring(1, line.Length - 2);

                string source = string.Empty, readOnlyData = string.Empty, workingDirectory = string.Empty;
                var protocols = new List<string>();

                i++;

                line = packageIniFileLines[i];

                while (line.Contains('='))
                {
                    var name = line.Split('=')[0];
                    var value = line.Split('=')[1];

                    switch (name)
                    {
                        case "Source":
                            source = value;
                            break;

                        case "ReadOnlyData":
                            readOnlyData = value;
                            break;

                        case "WorkingDirectory":
                            workingDirectory = value;
                            break;

                        case "Protocols":
                            protocols = value.Split(';').ToList();
                            break;

                        default:
                            Debug.Assert(false, "Error parsing entry points from Package.ini file.");
                            break;
                    }

                    i++;

                    if (i == packageIniFileLines.Count)
                        break;

                    line = packageIniFileLines[i];
                }

                var fileSystemPath = PathNormalizer.Unnormalize(source);
                var fileEntry = FileEntry.ForPath(fileSystemPath, source);
                var filename = System.IO.Path.GetFileName(source);

                var entryPoint = new EntryPoint
                                                    {
                                                        Name = filename,
                                                        Location = fileEntry.Path,
                                                        FileSystemLocation = PathNormalizer.Unnormalize(source),
                                                        ProductName = fileEntry.Product,
                                                        Protocols = new List<string>(),
                                                        InferredName = inferredName
                                                    };

                EntryPoints.Add(entryPoint);
            }
        }

        #endregion

        #region Files

        public void RefreshFiles()
        {
            if (!Directory.Exists(Path))
                return;

            RefreshFileIsolationOptionsCache();

            Files.Clear();

            var rootDirs = Directory.GetDirectories(Path).Where(d => d.EndsWith("%"));

            var allFilePaths = rootDirs.SelectMany(path => GetAllFilePathsRecursivelyFrom(path));

            foreach (var filePath in allFilePaths)
            {
                var normalizedFilePath = filePath.Substring(Path.Length + 1);
                var systemFilePath = PathNormalizer.Unnormalize(normalizedFilePath);

                var isolationOption = GetIsolationOptionForFileAt(filePath);

                var fileEntry = new ThinAppFileEntry(FileEntry.ForPath(systemFilePath, normalizedFilePath),
                                                     isolationOption);

                if (!fileEntry.IsDirectory)
                    LoadExtraFileDataTo(fileEntry);

                Files.Add(fileEntry);
            }
        }

        private void LoadExtraFileDataTo(FileEntry aFileEntry)
        {
            var systemPath = Path + "\\" + aFileEntry.Path;

            string product, company, version, description;
            var originalFileName = product = company = version = description = string.Empty;
            FileSystemTools.GetFileProperties(systemPath, ref originalFileName, ref product, ref company, ref version,
                                              ref description);

            aFileEntry.OriginalFileName = originalFileName;
            aFileEntry.Product = product;
            aFileEntry.Company = company;
            aFileEntry.Version = version;
            aFileEntry.Description = description;

            Debug.Assert(!string.IsNullOrEmpty(version));
        }

        private void RefreshFileIsolationOptionsCache()
        {
            FileIsolationOptionsCache.Clear();

            var attributeFiles = GetAllFilePathsRecursivelyFrom(Path).Where(path => path.EndsWith(@"\##Attributes.ini"));

            foreach (var attributeFile in attributeFiles)
                FileIsolationOptionsCache.Add(Directory.GetParent(attributeFile).FullName.ToLower(), GetFileIsolationOptionFrom(attributeFile));

            DefaultDirectoryIsolation = GetDefaultDirectoryIsolation();
        }

        private ThinAppIsolationOption GetDefaultDirectoryIsolation()
        {
            if (!File.Exists(PackageIniPath))
                return ThinAppIsolationOption.Merged;

            var packageIniLines = File.ReadAllLines(PackageIniPath);

            var isolationOptionString = packageIniLines.First(line => line.StartsWith(@"DirectoryIsolationMode=")).Substring(
                    @"DirectoryIsolationMode=".Length);

            return (ThinAppIsolationOption)Enum.Parse(typeof(ThinAppIsolationOption), isolationOptionString);

        }

        private ThinAppIsolationOption GetIsolationOptionForFileAt(string aPath)
        {
            var normalizedPath = aPath.ToLower();

            var ancestors = FileIsolationOptionsCache.Keys.Where(normalizedPath.StartsWith);

            var closestAncestor = ancestors.Aggregate<string, string>(null, (result, ancestor) 
                => result = result.Length > ancestor.Length ? result : ancestor);

            if (closestAncestor == null)
                return DefaultDirectoryIsolation;

            return FileIsolationOptionsCache[closestAncestor];
        }

        private ThinAppIsolationOption GetFileIsolationOptionFrom(string anAttributesFilePath)
        {
            var isolationOptionString = File.ReadAllLines(anAttributesFilePath)[1].Substring("DirectoryIsolationMode=".Length);

            return (ThinAppIsolationOption)Enum.Parse(typeof(ThinAppIsolationOption), isolationOptionString);
        }

        private IEnumerable<string> GetAllFilePathsRecursivelyFrom(string aDirectoryPath)
        {
            var filePaths = new List<string>();

            if (!Directory.Exists(aDirectoryPath))
                return filePaths;

            filePaths.AddRange(Directory.GetFiles(aDirectoryPath));

            foreach (var subDirPath in Directory.GetDirectories(aDirectoryPath))
                filePaths.AddRange(GetAllFilePathsRecursivelyFrom(subDirPath));

            return filePaths;
        }

        #endregion

        #endregion

        #region Saving

        public void SaveAll()
        {
            //if (!Directory.Exists(Path))
            //    Directory.CreateDirectory(Path);

            //if (!File.Exists(PackageIniPath))
            //    CreatePackageIniFile();

            //SaveFiles();
            //SaveRegistryKeys();
            //SaveEntryPoints();
        }

        protected void SaveSettings()
        {
            var layerKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(ThinAppCapturesSubKey + "\\" + Name);

            layerKey.SetValue("IsNew", IsNew);

            layerKey.Close();
        }

        public void SaveEntryPoints()
        {
            CreatePackageIniFile();
        }

        public void SaveRegistryKeys()
        {

        }

        public void SaveFiles()
        {
            foreach (var fileEntry in Files)
            {
                try
                {
                    File.Copy(fileEntry.FileSystemPath, Path + "//" + fileEntry.Path);
                }
                catch (Exception)
                {
                    Debug.Assert(false, "Unable to copy file to capture folder.");
                }
            }
        }

        private void CreatePackageIniFile()
        {
            var PackageIniContents = new StringBuilder();
            var textinfo = CultureInfo.CurrentCulture.TextInfo;

            var s = String.Format(
                PackageIniFormat,
                CompressionType,
                DefaultDirectoryIsolation,
                DefaultRegistryIsolation,
                ThinAppVersion,
                Name,
                Name,
                textinfo.ANSICodePage,
                textinfo.LCID,
                textinfo.CultureName,
                Options.RemoveSandboxOnExit ? 1 : 0,
                Options.RemoveSandboxOnStart ? 1 : 0
                );

            PackageIniContents.Append(s);

            WriteEntryPointsToPackageDotIni(PackageIniContents);

            using (var file = new StreamWriter(PackageIniPath))
                file.WriteLine(PackageIniContents.ToString());
        }

        private void WriteEntryPointsToPackageDotIni(StringBuilder dst)
        {
            Debug.Assert(false, "The totalBytes count should be revised!");
            var totalBytes = Files.Aggregate(0L, (total, file) => total + new FileInfo(file.FileSystemPath).Length);
            string formatString;
            if (EntryPoints.Count < 2 && totalBytes < (1024 - 128) * 1024 * 1024)
            {
                formatString = @"[{0}]
Source={1}
ReadOnlyData=Package.ro.tvr
";
            }
            else
            {
                formatString = @"[{0}]
Source={1}
Shortcut={2}.dat
";
                dst.Append(
                    String.Format(@"[{0}.dat]
Source={1}
ReadOnlyData=Package.ro.tvr
MetaDataContainerOnly=1

", Name, EntryPoints.First().Location));
            }
            var onlyPathRegex = new Regex(@"(.*)\\[^\\]*");
            foreach (var entryPoint in EntryPoints)
            {
                var workingDirectory = "";

                var match = onlyPathRegex.Match(entryPoint.Location);
                if (match.Success)
                    workingDirectory = "WorkingDirectory=" + match.Groups[1].Value + Environment.NewLine;

                var fileTypes = entryPoint.GetFileTypesString();
                if (fileTypes != "")
                    fileTypes = "FileTypes=" + fileTypes + Environment.NewLine;

                var protocols = entryPoint.GetProtocolsString();
                if (protocols != "")
                    protocols = "Protocols=" + protocols + Environment.NewLine;

                var s = String.Format(
                    formatString,
                    entryPoint.GetInferredName(),
                    entryPoint.Location,
                    Name
                );
                dst.Append(s + workingDirectory + fileTypes + protocols + Environment.NewLine);
            }
        }

        #endregion

        #endregion
    }
}