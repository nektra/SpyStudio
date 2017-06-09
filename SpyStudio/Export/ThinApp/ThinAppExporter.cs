using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SpyStudio.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.ThinApp
{
    internal class ThinAppExporter : Exporter
    {
        protected ThinAppExport Export { get; set; }

        private bool _stopRequested;

        public ThinAppExporter()
        {
            _stopRequested = false;
        }

        private static string CompressionType
        {
            get { return "None"; }
        }

        public ThinAppIsolationOption DefaultFileIsolationMode;

        public ThinAppIsolationOption DefaultRegistryIsolationMode;

        private static string ThinAppCaptureVersion
        {
            get { return "4.7.3-891762"; }
        }

        private static string IsolationRuleToString(ThinAppIsolationOption isolation)
        {
            switch (isolation)
            {
                case ThinAppIsolationOption.Merged:
                    return "Merged";
                case ThinAppIsolationOption.WriteCopy:
                    return "WriteCopy";
                case ThinAppIsolationOption.Full:
                    return "Full";
                default:
                    return null;
            }
        }

        public override void Stop()
        {
            _stopRequested = true;
        }

        public void PerformExport(string destination, string sandboxName, IEnumerable<FileEntry> files, IEnumerable<EntryPoint> entryPoints, IEnumerable<RegKeyInfo> registry, ThinAppOptions packageOptions)
        {
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            if (Export.RegistryWasUpdated || Export.Capture.IsNew)
            {
                ProgressDialog.LogString("Exporting registry...");
                ExportRegistry(destination, registry);
            }

            ProgressDialog.SetProgress(5);
            
            if (Export.Capture.IsNew)
            {
                ProgressDialog.LogString("Writing build.bat...");
                using (var file = new StreamWriter(destination + @"\build.bat"))
                {
                    file.Write(Resources.ThinApp_build_bat);
                }
            }
            ProgressDialog.SetProgress(10);
            

            if (Export.Capture.IsNew || Export.FilesWereUpdated)
            {
                ProgressDialog.LogString("Exporting files...");
                ExportFiles(destination, files);
            }

            if (Export.FilesWereUpdated || Export.Capture.IsNew || Export.EntryPointsWereUpdated)
            {
                if (!_stopRequested)
                {
                    var filesSize = Export.FilesWereUpdated
                                        ? files.Where(f => !f.IsDirectory).Sum(f => TryGetSizeForFileAt(f.Path))
                                        : Export.Capture.TotalFileBytes;

                    ProgressDialog.LogString("Writing Package.ini...");
                    WritePackageDotIni(destination, sandboxName, entryPoints, packageOptions, filesSize);
                }
            }

            ProgressDialog.SetProgress(100);
        }

        private long TryGetSizeForFileAt(string aPath)
        {
            try
            {
                return new FileInfo(aPath).Length;
            }
            catch
            {
                
            }

            return 0;
        }

        private static void WriteAttributesFile(ThinAppFileEntry entry, string location)
        {
            if (entry.IsolationRule == ThinAppIsolationOption.Inherit)
                return;
            var output = "[Isolation]" + Environment.NewLine + "DirectoryIsolationMode=" +
                         IsolationRuleToString(entry.IsolationRule);
            using (var file = new StreamWriter(location + @"\##Attributes.ini"))
                file.WriteLine(output);
        }

        private void WritePackageDotIni(string destination, string sandboxName, IEnumerable<EntryPoint> entryPoints, ThinAppOptions packageOptions, long totalBytes)
        {
            if (File.Exists(destination + @"\Package.ini"))
                File.Delete(destination + @"\Package.ini");

            var output = new StringBuilder();
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            const string formatString =
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
            var s = String.Format(
                formatString,
                CompressionType,
                IsolationRuleToString(DefaultFileIsolationMode),
                IsolationRuleToString(DefaultRegistryIsolationMode),
                ThinAppCaptureVersion,
                sandboxName,
                sandboxName,
                textinfo.ANSICodePage,
                textinfo.LCID,
                textinfo.CultureName,
                packageOptions.RemoveSandboxOnExit ? 1 : 0,
                packageOptions.RemoveSandboxOnStart ? 1 : 0
                );
            output.Append(s);
            WriteEntryPointsToPackageDotIni(output, entryPoints, totalBytes ,sandboxName);
            using (var file = new StreamWriter(destination + @"\Package.ini"))
            {
                file.WriteLine(output.ToString());
            }
        }

        private void WriteEntryPointsToPackageDotIni(StringBuilder dst, IEnumerable<EntryPoint> entryPoints, long totalBytes, string sandboxName)
        {
            var entryPointsList = entryPoints.ToList();
            if (entryPointsList.Count < 1)
                return;
            string formatString;
            if (entryPointsList.Count < 2 && totalBytes < (1024 - 128) * 1024 * 1024)
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

", sandboxName, entryPointsList[0].Location));
            }
            var onlyPathRegex = new Regex(@"(.*)\\[^\\]*");
            foreach (var entryPoint in entryPointsList)
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
                    sandboxName
                );
                dst.Append(s + workingDirectory + fileTypes + protocols + Environment.NewLine);
            }
        }

        private static readonly Regex Wow6432NodeRegex = new Regex(@"\\wow6432node(?:\\|$)", RegexOptions.IgnoreCase);

        private static IEnumerable<string> GetPossibleSystemPaths(RegKeyInfo info)
        {
            if (info.OriginalKeyPaths.Count == 1)
            {
                yield return info.OriginalKeyPaths.Keys.ToList()[0];
                yield break;
            }
            var path = RegistryTools.GetStandardKey(info.BasicKeyHandle);
            if (info.SubKey == string.Empty)
            {
                if (path.ToUpper() != "HKEY_CLASSES_ROOT")
                    yield return path;
                yield break;
            }
            var possiblePaths = info.OriginalKeyPaths.Keys.ToList();
            if (possiblePaths.Any(x => Wow6432NodeRegex.IsMatch(x)))
                possiblePaths = possiblePaths.Where(x => Wow6432NodeRegex.IsMatch(x)).ToList();
            foreach (var possiblePath in possiblePaths)
            {
                yield return possiblePath;
            }
        }

        public static IEnumerable<ThinAppRegKeyInfo> GetPossibleSystemRegKeys(RegKeyInfo info)
        {
            var paths = GetPossibleSystemPaths(info);

            foreach (var path in paths)
            {
                var newInfo = new ThinAppRegKeyInfo();
                newInfo.Path = path;
                newInfo.Isolation = (info is ThinAppRegKeyInfo)
                                    ? ((ThinAppRegKeyInfo) info).Isolation
                                    : ThinAppIsolationOption.Full;

                newInfo.ValuesByName = info.ValuesByName;
                yield return newInfo;
            }
        }

        private void ExportRegistry(string destination, IEnumerable<RegKeyInfo> keyInfosForExport)
        {
            // Delete old registry keys
            foreach (var file in new DirectoryInfo(Export.Capture.Path).GetFiles("HKEY*"))
                file.Delete();

            var mainKeyRegex = new Regex(@"^([^\\]+)(?:\\|$)");
            var keyInfosBySystemPath = new Dictionary<string, List<ThinAppRegKeyInfo>>();
            var pathNormalizer = ThinAppPathNormalizer.GetInstance();
            foreach (var info in keyInfosForExport)
            {
                foreach (var regkey in GetPossibleSystemRegKeys(info))
                {
                    var match = mainKeyRegex.Match(regkey.Path);
                    Debug.Assert(match.Success, "Malformed path.");
                    var mainKey = match.Groups[1].ToString().ToUpper();
                    List<ThinAppRegKeyInfo> l;
                    if (!keyInfosBySystemPath.ContainsKey(mainKey))
                        l = keyInfosBySystemPath[mainKey] = new List<ThinAppRegKeyInfo>();
                    else
                        l = keyInfosBySystemPath[mainKey];
                    l.Add(regkey);
                }
            }

            foreach (var keyInfos in keyInfosBySystemPath)
            {
                var output = new StringBuilder();
                var key = keyInfos.Key;
                foreach (var regKey in keyInfos.Value)
                {
                    output.Append("isolation_");
                    output.Append(IsolationRuleToString(regKey.Isolation).ToLower());
                    output.Append(" ");
                    output.Append(regKey.Path);
                    output.Append(Environment.NewLine);
                    if (regKey.ValuesByName.Count != 0)
                    {
                        foreach (var regValueInfo in regKey.ValuesByName.Values)
                            WriteRegistryValue(output, regValueInfo, pathNormalizer);
                    }
                    else
                        output.Append(Environment.NewLine);
                    output.Append(Environment.NewLine);
                }
                using (var file = new StreamWriter(String.Format(@"{0}\{1}.txt", destination, key)))
                    file.WriteLine(output);
            }
        }

        private static long MeasureTotalBytes(IEnumerable<FileEntry> fileList)
        {
            long ret = 0;
            foreach (var fileEntry in fileList)
            {
                if (DetermineDirectoriness(fileEntry))
                    continue;
                try
                {
                    var fileinfo = new FileInfo(fileEntry.FileSystemPath);
                    ret += fileinfo.Length;
                }
                catch (Exception)
                {
                }
            }
            return ret;
        }

        private long _sizesTotal, _accumulatedBytes;

        private void ProgressCallback(long partialProgress, long fileSize)
        {
            ProgressDialog.SetProgress(10 + (int)((_accumulatedBytes + partialProgress) * 85 / _sizesTotal));
        }

        private long ExportFiles(string destination, IEnumerable<FileEntry> files)
        {
            // Delete old files
            foreach (var directory in new DirectoryInfo(Export.Capture.Path).GetDirectories().Where(d => d.Name.StartsWith("%")))
                directory.Delete(true);

            var noDuplicates = new Dictionary<string, FileEntry>();
            foreach (var fileEntry in files)
            {
                noDuplicates[fileEntry.Path] = fileEntry;
            }
            var n = noDuplicates.Count;
            var fileList = noDuplicates.Values.OrderBy(x => x.Path).ToList();
            var upperDirectory = new Regex(@"^(.*)\\[^\\]+$");

            var ret = _sizesTotal = MeasureTotalBytes(fileList);
            _accumulatedBytes = 0;

            for (var i = 0; i < n; i++)
            {
                if (_stopRequested)
                    break;
                //SetProgress(10 + i*85/n);
                var fileEntry = fileList[i];
                var combined = destination + "\\" + fileEntry.Path;
                var destinationFilepath = combined;
                if (!fileEntry.IsDirectory)
                {
                    var match = upperDirectory.Match(destinationFilepath);
                    if (!match.Success)
                        continue;
                    combined = match.Groups[1].ToString();
                }

                if (!Directory.Exists(combined))
                    Directory.CreateDirectory(combined);

                if (!DetermineDirectoriness(fileEntry))
                {
                    if (File.Exists(destinationFilepath))
                        File.Delete(destinationFilepath);
                    try
                    {
                        var size = FileSystemTools.CopyFileWithoutAttributes(destinationFilepath,
                                                                             fileEntry.FileSystemPath, ProgressCallback);

                        _accumulatedBytes += size;
                        ret += size;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ProgressDialog.LogError(String.Format("Access denied on file: {0}", fileEntry.FileSystemPath));
                    }
                    catch (DirectoryNotFoundException)
                    {
                        ProgressDialog.LogError(String.Format("Could not find file: {0}", fileEntry.FileSystemPath));
                    }
                    catch (FileNotFoundException e)
                    {
                        ProgressDialog.LogError(String.Format("Could not find file: {0}", e.FileName));
                    }
                    catch(IOException)
                    {
                        ProgressDialog.LogError(String.Format("File busy: {0}", fileEntry.FileSystemPath));
                    }
                }
                else if (fileEntry is ThinAppFileEntry)
                    WriteAttributesFile((ThinAppFileEntry) fileEntry, combined);
            }

            return ret;
        }

        private static bool DetermineDirectoriness(FileEntry entry)
        {
            return entry.IsDirectory || Directory.Exists(entry.FileSystemPath);
        }

        string GetValueTypeString(RegistryValueKind type)
        {
            if (type == RegistryValueKind.Unknown)
                return "REG_NONE";
            return RegistryTools.GetValueTypeString(type);
        }

        //private static readonly Regex QuotedStringRegex = new Regex(@"(^"")([^""]*)("".*)");

        private static string DoIntraValueStringReplacement(string data, string keyPath, string valueName, ThinAppPathNormalizer norm, out bool replacementPerformed)
        {
            var ret = norm.Normalize(data);
            replacementPerformed = ret.Contains("%");
            return ret;
        }

        private void WriteRegistryValue(StringBuilder output, RegValueInfo value, ThinAppPathNormalizer norm)
        {
            bool replacementPerformed = true;
            //var replaced = DoIntraValueStringReplacement(value.Name, norm, out replacementPerformed);
            var replaced = value.Name;
            output.Append("  Value" + (replacementPerformed ? "~" : "="));
            output.Append(replaced);
            output.Append(Environment.NewLine + "  ");
            output.Append(GetValueTypeString(value.ValueType));
            output.Append(EqualsSign(value));
            SerializeData(output, value, norm);
            output.Append(Environment.NewLine);
        }

        private static void BinaryToThinAppFormat(StringBuilder dst, string s)
        {
            var twoHexDigitsRegex = new Regex(@"[0-9A-Fa-f]{2}");
            foreach (var match in twoHexDigitsRegex.Matches(s))
                dst.Append("#" + ((Match) match).Groups[0].ToString().ToUpper());
        }

        private static string MakeHexRegex(int i)
        {
            var ret = new StringBuilder();
            ret.Append(@"0[xX]");
            for (; i != 0; i--)
                ret.Append("([0-9A-Fa-f]{2})");
            ret.Append(@"(?: \([0-9]+\))?");
            return ret.ToString();
        }

        private static Regex HexRegex = new Regex("0x([0-9A-Fa-f]+) .*");

        private static void DWordToThinAppFormat(StringBuilder dst, string s, int bytes)
        {
            var match = HexRegex.Match(s);
            Debug.Assert(match.Success);
            var hexPart = match.Groups[1].ToString();
            var builder = new StringBuilder();
            for (int i = hexPart.Length; i < bytes * 2; i++)
                builder.Append('0');
            builder.Append(hexPart);

            s = builder.ToString();

            var generatedRegex = new Regex(MakeHexRegex(bytes));
            match = generatedRegex.Match(s);
            for (; bytes != 0; bytes--)
                dst.Append("#" + match.Groups[bytes]);
        }

        private static void SerializeData(StringBuilder dst, RegValueInfo value, ThinAppPathNormalizer norm)
        {
            switch (value.ValueType)
            {
                case RegistryValueKind.MultiString:
                    {
                        var s = value.Data;
                        bool replacementPerformed;
                        var temp = s.Split('\0').ToList();
                        if (temp.Count >= 2 && temp[temp.Count - 1] == "" && temp[temp.Count - 2] == "")
                        {
                            temp.RemoveAt(temp.Count - 1);
                            temp.RemoveAt(temp.Count - 1);
                        }
                        temp = temp.Select(x => DoIntraValueStringReplacement(x, value.OriginalPath, value.Name, norm, out replacementPerformed)).ToList();
                        s = temp.Aggregate("", (x, y) => x + y + "\0") + "\0";
                        SerializeString(dst, s);
                    }
                    break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    {
                        var s = value.Data;
                        if (value.ValueType == RegistryValueKind.ExpandString)
                            s = Environment.ExpandEnvironmentVariables(s);
                        if (s.Length == 0 || s[s.Length - 1] != '\0')
                            s += '\0';
                        bool replacementPerformed;
                        s = DoIntraValueStringReplacement(s, value.OriginalPath, value.Name, norm, out replacementPerformed);
                        SerializeString(dst, s);
                    }
                    break;
                case RegistryValueKind.Binary:
                    BinaryToThinAppFormat(dst, value.Data);
                    break;
                case RegistryValueKind.DWord:
                    DWordToThinAppFormat(dst, value.Data, 4);
                    break;
                case RegistryValueKind.QWord:
                    DWordToThinAppFormat(dst, value.Data, 8);
                    break;
            }
        }

        private static bool IsControlCharacter(char c)
        {
            return c < 32;
        }

        private static bool NeedsTwoBytes(char c)
        {
            return c > 255;
        }

        private static string ToHexString(char character, int n)
        {
            return ((ulong) character).ToString("X" + n);
        }

        private static void SerializeString(StringBuilder output, string data)
        {
            foreach (var character in data)
            {
                if (IsControlCharacter(character))
                {
                    output.Append("#23");
                    output.Append(ToHexString(character, 2));
                }
                else if (character == '#')
                    output.Append("#2323");
                else if (NeedsTwoBytes(character))
                {
                    output.Append("#23#23");
                    output.Append(ToHexString(character, 4));
                }
                else
                    output.Append(character);
            }
        }

        private static string EqualsSign(RegValueInfo value)
        {
            switch (value.ValueType)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.MultiString:
                case RegistryValueKind.ExpandString:
                    return "~";
                default:
                    return "=";
            }
        }

        #region Overrides of Exporter

        public override void GeneratePackage(VirtualizationExport export)
        {
            Export = (ThinAppExport) export;

            var files = export.GetField<IEnumerable<FileEntry>>(ExportFieldNames.OriginalFileEntries).Value;
            var entryPoints =
                export.GetField<IEnumerable<EntryPoint>>(ExportFieldNames.EntryPoints).Value;
            var registry =
                export.GetField<IEnumerable<RegKeyInfo>>(ExportFieldNames.RegistryKeys).Value;
            //TODO: Turn this back on once the preferences page is enabled again.
            //var packageOptions = aThinAppExport.GetField<ThinAppOptions>(ExportFieldNames.ThinAppOptions).Value;
            var packageOptions = new ThinAppOptions();

            DefaultFileIsolationMode =
                export.GetField<ThinAppIsolationOption>(ExportFieldNames.ThinAppDirectoryIsolation).Value;
            DefaultRegistryIsolationMode =
                export.GetField<ThinAppIsolationOption>(ExportFieldNames.ThinAppRegistryIsolation).Value;
            var name = export.GetField<string>(ExportFieldNames.Name).Value;
            var destination = ThinAppExport.ThinAppCapturesPath + "\\" + name;

            ProgressDialog.Start();
            
            PerformExport(destination, name, files, entryPoints, registry, packageOptions);
        }

        #endregion
    }
}
