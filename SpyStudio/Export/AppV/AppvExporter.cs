using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Ionic.Zip;
using Ionic.Zlib;
using Microsoft.Win32;
using SpyStudio.Extensions;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV
{
    class AppvExporter : Exporter
    {
        protected AppvExport Export;

        private class ZipEntry
        {
            public readonly List<ulong> CompressedSizes = new List<ulong>();
            public long LfhSize;
            public string FilePath;
            public Stream Stream;
            public byte[] ByteArray;
        }

        static void GetHashesForFile(string path, string pathInArchive, XmlTextWriter xml, ZipEntry entry)
        {
            Stream stream;
            if (path != null)
                stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            else
            {
                if (entry.ByteArray == null)
                {
                    Debug.WriteLine("Skipping " + pathInArchive);
                    return;
                }
                stream = new MemoryStream(entry.ByteArray);
            }
            using (stream)
            {
                xml.WriteStartElement("File");
                xml.WriteAttributeString("Name", HttpUtility.UrlDecode(pathInArchive).Replace('/', '\\'));
                xml.WriteAttributeString("Size", stream.Length.ToString(CultureInfo.InvariantCulture));
                xml.WriteAttributeString("LfhSize", entry.LfhSize.ToString(CultureInfo.InvariantCulture));
                if (stream.Length != 0)
                {
                    var n = stream.Length;
                    const int defBlockSize = 1 << 16;
                    n += defBlockSize - 1;
                    n >>= 16;
                    var buffer = new byte[defBlockSize];
                    for (long i = 0; i < n; i++)
                    {
                        xml.WriteStartElement("Block");
                        stream.Seek(i * defBlockSize, SeekOrigin.Begin);
                        var read = (int)Math.Min(defBlockSize, stream.Length - i * defBlockSize);
                        stream.Read(buffer, 0, read);
                        var hash = System.Security.Cryptography.SHA256.Create();
                        var digest = hash.ComputeHash(buffer, 0, read);
                        xml.WriteAttributeString("Hash", Convert.ToBase64String(digest));
                        //xml.WriteAttributeString("Size", entry.CompressedSizes[(int)i].ToString(CultureInfo.InvariantCulture));
                        xml.WriteEndElement();
                    }
                }
                xml.WriteEndElement();
            }
        }

        private void ZipRound1CompressionOptions(ZipFile zip)
        {
            zip.UseZip64WhenSaving = Zip64Option.EverywhereExceptLFH;
            zip.FlushMode = FlushType.Full;
            //zip.AlwaysForceCompression = true;
            //zip.CompressionLevel = CompressionLevel.Default;
            zip.CompressionLevel = CompressionLevel.Level0;
            zip.BufferSize = 1 << 16;
            zip.ParallelDeflateThreshold = -1;
            zip.EmitTimesInWindowsFormatWhenSaving = false;
            zip.EmitTimesInUnixFormatWhenSaving = false;
        }

        private void PrepareZip(ZipFile zip, Dictionary<string, ZipEntry> entryDict, long totalSize)
        {
            ZipRound1CompressionOptions(zip);
            var lastState = new Int64();
            var totalProgress = new Int64();
            zip.SaveProgress += (sender, eventArgs) =>
            {
                if (eventArgs.CurrentEntry == null || eventArgs.CurrentEntry.IsDirectory)
                    return;
                if (eventArgs.TotalBytesToTransfer > 0)
                {
                    if (lastState < eventArgs.BytesTransferred)
                    {
                        totalProgress += eventArgs.BytesTransferred - lastState;
                        var progress = totalProgress * 100 / totalSize;
                        if (progress < 0)
                            progress = 0;
                        else if (progress > 100)
                            progress = 100;
                        ProgressDialog.SetProgress((int)progress);
                    }
                    lastState = eventArgs.BytesTransferred;
                }

                var path = eventArgs.CurrentEntry.FileName.Replace('/', '\\');
                ZipEntry entry;
                if (!entryDict.TryGetValue(path, out entry))
                {
                    entryDict[path] = entry = new ZipEntry();
                    entry.ByteArray = ((LazyStream)eventArgs.CurrentEntry.InputStream).Buffer;
                }
                if (eventArgs.CompressedBytes != 0)
                    entry.CompressedSizes.Add(eventArgs.CompressedBytes);
                if (eventArgs.HeaderSize != 0)
                    entry.LfhSize = eventArgs.HeaderSize;
            };
        }

        class LazyStream : Stream
        {
            private byte[] _buffer;
            private long _position;
            private Func<byte[]> _callback;
            public LazyStream(Func<byte[]> callback)
            {
                _callback = callback;
                _position = 0;
            }

            public byte[] Buffer
            {
                get
                {
                    if (_buffer != null)
                        return _buffer;
                    return _buffer = _callback();
                }
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _position = offset;
                        break;
                    case SeekOrigin.Current:
                        _position += offset;
                        break;
                    case SeekOrigin.End:
                        _position = Buffer.Length - offset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("origin");
                }
                return _position;
            }

            public override void SetLength(long value)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (Buffer.Length - _position < count)
                    count = (int)(Buffer.Length - _position);
                Array.Copy(Buffer, _position, buffer, offset, count);
                _position += count;
                return count;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Length
            {
                get { return Buffer.Length; }
            }

            public override long Position { get; set; }
        }

        // 1x1 black transparent PNG.
        private static readonly byte[] EmptyLogo = {
	        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
	        0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
	        0x08, 0x04, 0x00, 0x00, 0x00, 0xB5, 0x1C, 0x0C, 0x02, 0x00, 0x00, 0x00,
	        0x0B, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x62, 0x60, 0x00, 0x00,
	        0x00, 0x09, 0x00, 0x03, 0x19, 0x11, 0xD9, 0xE4, 0x00, 0x00, 0x00, 0x00,
	        0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
        };


        public override void GeneratePackage(VirtualizationExport export)
        {
            Export = (AppvExport)export;
            var files = export.GetField<IEnumerable<FileEntry>>(ExportFieldNames.Files).Value.ToList();
            var registry = export.GetField<IEnumerable<RegKeyInfo>>(ExportFieldNames.RegistryKeys).Value;

            var exportedRegistry = ExportRegistry(registry);
            if (exportedRegistry == null)
                return;

            var dstPath = SystemDirectories.CommonDocuments + "\\output.appv";

            var entryDict = new Dictionary<string, ZipEntry>();

            entryDict["Registry.dat"] = new ZipEntry();

            var norm = AppvPathNormalizer.GetInstanceFileSystem();
#if true
            long totalSize = 0;
            foreach (var fileEntry in files)
            {
                if (fileEntry.IsDirectory || !File.Exists(fileEntry.FileSystemPath))
                    continue;
                var entry = new ZipEntry
                {
                    FilePath = fileEntry.FileSystemPath,
                    Stream = new FileStream(fileEntry.FileSystemPath, FileMode.Open, FileAccess.Read),
                };
                totalSize += entry.Stream.Length;
                var path = "Root\\VFS\\" + norm.Normalize(fileEntry.FileSystemPath);
                entryDict[path] = entry;
            }
#else
            long totalSize = 1;
#endif

            try
            {
                ProgressDialog.LogString("Compressing input files...");
                using (var zip = new ZipFile())
                {
                    PrepareZip(zip, entryDict, totalSize);

                    zip.AddEntry("Registry.dat", new FileStream(exportedRegistry, FileMode.Open, FileAccess.Read)).SetBit3OfBitField();
                    entryDict["Registry.dat"].FilePath = exportedRegistry;
                    entryDict.Where(x => x.Key != "Registry.dat").ForEach(x => zip.AddEntry(x.Key, x.Value.Stream).SetBit3OfBitField());
                    zip.AddEntry("FilesystemMetadata.xml", new LazyStream(() => GenerateFilesystemMetadata(files))).SetBit3OfBitField();
                    zip.AddEntry("StreamMap.xml", new LazyStream(() => GenerateStreamMap())).SetBit3OfBitField();
                    zip.AddEntry("PackageHistory.xml", new LazyStream(GeneratePackageHistory)).SetBit3OfBitField();
                    zip.AddEntry("logo.png", new LazyStream(() => EmptyLogo)).SetBit3OfBitField();
                    zip.AddEntry("AppxManifest.xml", new LazyStream(() =>
                    {
                        ProgressDialog.LogString("Waiting for manifest. This may take a while.");
                        return GenerateAppxManifest();
                    })).SetBit3OfBitField();
                    zip.AddEntry("AppxBlockMap.xml", new LazyStream(() => GenerateBlockMap(entryDict))).SetBit3OfBitField();
                    zip.AddEntry("[Content_Types].xml", new LazyStream(() => GenerateContentTypes(entryDict))).SetBit3OfBitField();

                    zip.Save(dstPath);
                }

                ProgressDialog.LogString("Done!");
                ProgressDialog.SetProgress(100);
            }
            finally
            {
                entryDict.Values
                    .Select(x => x.Stream)
                    .Where(x => x != null)
                    .ForEach(x => x.Dispose());
            }
        }

        private class DirectoryElement
        {
            public string Name;
            public string ShortName;
            public bool IsDirectory
            {
                get { return Children != null; }
                set
                {
                    if (!value)
                        Children = null;
                    else if (!IsDirectory)
                        Children = new List<DirectoryElement>();

                }
            }
            public List<DirectoryElement> Children;

            public void Add(string path)
            {
                var newNode = Add(path.SplitAsPath().ToArray(), 0, string.Empty);
            }

            private static readonly Regex LastDirRegex = new Regex(@"^(?:.*\\)?([^\\]+)\\*$");

            private DirectoryElement Add(string[] pathElements, int i, string accum)
            {
                if (pathElements.Length == i)
                    return this;

                accum += pathElements[i] + "\\";

                IsDirectory = true;
                var child =
                    Children.FirstOrDefault(
                        x => x.Name.Equals(pathElements[i], StringComparison.InvariantCultureIgnoreCase));
                if (child != null)
                    return child.Add(pathElements, i + 1, accum);

                var shortPath = FileSystemTools.GetShortPathName(accum);
                var match = LastDirRegex.Match(shortPath);
                Debug.Assert(match.Success);

                var de = new DirectoryElement
                {
                    Name = pathElements[i],
                };
                if (i == 0)
                    de.ShortName = StringTools.RandomLowerCaseString(8) + "." + StringTools.RandomLowerCaseString(3);
                else
                    de.ShortName = match.Groups[1].ToString();
                Children.Add(de);
                return de.Add(pathElements, i + 1, accum);
            }

            public IEnumerable<string> ListEmptyDirectories()
            {
                var queue = new Queue<Stack<DirectoryElement>>();
                queue.Enqueue(new Stack<DirectoryElement>());
                queue.Peek().Push(this);
                while (queue.Count > 0)
                {
                    var stack = queue.Dequeue();
                    var This = stack.Peek();
                    if (!This.IsDirectory)
                        continue;
                    if (This.Children.Count > 0)
                    {
                        foreach (var child in This.Children)
                        {
                            var newStack = new Stack<DirectoryElement>();
                            stack.Reverse().ForEach(newStack.Push);
                            newStack.Push(child);
                            queue.Enqueue(newStack);
                        }
                    }
                    else
                    {
                        var reversed = stack.Reverse().ToArray();
                        yield return reversed.Select(x => x.Name).JoinPaths();
                        yield return reversed.Select(x => x.ShortName).JoinPaths();
                    }
                }
            }

            public void GenerateFilesystemMetadata(XmlWriter xml, Stack<string> path)
            {
                path.Push(Name);
                path.Push(ShortName);
                {
                    xml.WriteStartElement("Entry");
                    var fullPath = path.Reverse().WhereEven().JoinPaths();
                    xml.WriteAttributeString("Long", Name);
                    if (path.Count > 2)
                    {
                        fullPath = path.Reverse().WhereOdd().JoinPaths();
                        xml.WriteAttributeString("Short", ShortName);
                    }

                    if (Children != null)
                        foreach (var directoryElement in Children)
                            directoryElement.GenerateFilesystemMetadata(xml, path);

                    xml.WriteEndElement();
                }
                path.Pop();
                path.Pop();
            }

            public void GenerateFilesystemMetadata(XmlWriter xml)
            {
                GenerateFilesystemMetadata(xml, new Stack<string>());
            }

        }

        private byte[] GenerateFilesystemMetadata(IEnumerable<FileEntry> files)
        {
            var root = new DirectoryElement
            {
                Name = "Root",
                ShortName = "Root",
                IsDirectory = true,
            };
            var vfs = new DirectoryElement
            {
                Name = "VFS",
                ShortName = "VFS",
                IsDirectory = true,
            };
            root.Children.Add(vfs);

            var norm = AppvPathNormalizer.GetInstanceNone();
            files.Select(x => norm.Normalize(x.FileSystemPath)).ForEach(vfs.Add);

            var mem = new MemoryStream();
            using (var xml = new XmlTextWriter(mem, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 2,
                IndentChar = ' ',
            })
            {
                xml.WriteStartElement("Metadata");
                xml.WriteAttributeString("xmlns", "http://schemas.microsoft.com/appv/2010/FilesystemMetadata");

                //Filesystem element
                {
                    xml.WriteStartElement("Filesystem");
                    xml.WriteAttributeString("Root", SystemDirectories.ProgramFiles);
                    xml.WriteAttributeString("Short", FileSystemTools.GetShortPathName(SystemDirectories.ProgramFiles));

                    root.GenerateFilesystemMetadata(xml);

                    xml.WriteEndElement();
                }

                //EmptyDirectories element
                {
                    xml.WriteStartElement("EmptyDirectories");

                    var longs = new List<string>();
                    var shorts = new List<string>();
                    root.ListEmptyDirectories().ForEachNth(longs.Add, shorts.Add);
                    Debug.Assert(longs.Count == shorts.Count);

                    for (int i = 0; i < longs.Count; i++)
                    {
                        xml.WriteStartElement("Entry");
                        xml.WriteAttributeString("Long", longs[i]);
                        xml.WriteAttributeString("Short", shorts[i]);
                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement();
                }

                //OpaqueDirectories element
                {
                }

                xml.WriteEndElement();
            }

            return mem.ToArray();
        }

        private byte[] GenerateStreamMap()
        {
            var s = @"<?xml version=""1.0"" encoding=""utf-8""?>
<StreamMap xmlns=""http://schemas.microsoft.com/appv/2010/streammap"">
  <FeatureBlock Id=""PublishingFeatureBlock"">
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\inficon.exe.3.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\inficon.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\misc.exe.15.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\cagicon.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\misc.exe.5.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\msouc.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\oisicon.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\joticon.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\outicon.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pubs.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\grvicons.exe.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.0.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\ACCICONS.EXE.40.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.61.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.44.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.2.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\ACCICONS.EXE.41.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.65.ico"" />
    <File Name=""Root\VFS\ProgramFilesCommonX86\microsoft shared\OFFICE14\MSOICONS.EXE.6.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.52.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.53.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.9.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.11.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.15.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.13.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.12.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.2.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.9.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.16.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.14.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\outicon.exe.6.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\outicon.exe.2.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\inficon.exe.1.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSACCESS.EXE.42.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.10.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.7.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.49.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.9.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.6.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.8.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.50.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.5.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.60.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.40.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.63.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.58.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.62.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.64.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.41.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.43.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSTORE.EXE.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\outicon.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.20.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.32.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.17.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pubs.exe.4.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\joticon.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\joticon.exe.2.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\joticon.exe.3.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OUTLOOK.EXE.11.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.13.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.7.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.18.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.11.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.14.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.15.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.16.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.4.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.12.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.17.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.9.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.19.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.10.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\outicon.exe.5.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pubs.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\pptico.exe.3.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.10.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\misc.exe.19.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\outicon.exe.3.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\VVIEWER.DLL.2.ico"" />
    <File Name=""Root\VFS\ProgramFilesCommonX86\Microsoft Shared\VSTO\vstoee.dll.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.4.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\wordicon.exe.3.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\accicons.exe.59.ico"" />
    <File Name=""Root\VFS\System\msxml3.dll.0.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.6.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.12.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.5.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.8.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.2.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.28.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.27.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.26.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.30.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.1.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.29.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.14.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.31.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.4.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.7.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\inficon.exe.2.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\INFOPATH.EXE.1.ico"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\GROOVE.EXE.0.ico"" />
    <File Name=""Root\VFS\System\imageres.dll.102.ico"" />
    <File Name=""Root\VFS\Windows\Installer\{90140000-0011-0000-0000-0000000FF1CE}\xlicons.exe.3.ico"" />
    <File Name=""Root\VFS\ProgramFilesCommonX86\microsoft shared\OFFICE14\MSOXMLED.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\excel.exe.manifest"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\EXCEL.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\GFX.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OART.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\GROOVE.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\INFOPATH.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSOCF.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\CDLMSO.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\msaccess.exe.manifest"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSACCESS.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSOHTMED.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSOUC.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\mspub.exe.manifest"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSPUB.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSTORE.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OIS.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OISGRAPH.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OISAPP.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\ONENOTE.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\ONMAIN.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OUTLOOK.EXE.MANIFEST"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OUTLOOK.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\powerpnt.exe.manifest"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\POWERPNT.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\PPCORE.DLL.2.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\SELFCERT.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\SETLANG.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\WINWORD.EXE.config"" />
    <File Name=""Root\VFS\ProgramFilesCommonX86\microsoft shared\OFFICE14\MSOXMLED.EXE"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\EXCEL.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""79"" Count=""2"" />
      <Blocks Index=""255"" Count=""2"" />
      <Blocks Index=""271"" Count=""5"" />
      <Blocks Index=""288"" Count=""2"" />
      <Blocks Index=""305"" Count=""12"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\GFX.DLL"">
      <Blocks Index=""0"" Count=""2"" />
      <Blocks Index=""6"" Count=""2"" />
      <Blocks Index=""22"" Count=""5"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OART.DLL"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""8"" Count=""3"" />
      <Blocks Index=""28"" Count=""2"" />
      <Blocks Index=""177"" Count=""2"" />
      <Blocks Index=""187"" Count=""5"" />
      <Blocks Index=""302"" Count=""11"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\GROOVE.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""35"" Count=""2"" />
      <Blocks Index=""40"" Count=""2"" />
      <Blocks Index=""384"" Count=""2"" />
      <Blocks Index=""389"" Count=""1"" />
      <Blocks Index=""444"" Count=""29"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\INFOPATH.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""5"" Count=""2"" />
      <Blocks Index=""14"" Count=""2"" />
      <Blocks Index=""17"" Count=""6"" />
      <Blocks Index=""24"" Count=""3"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSOCF.DLL"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\CDLMSO.DLL"">
      <Blocks Index=""0"" Count=""6"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSACCESS.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""12"" Count=""4"" />
      <Blocks Index=""51"" Count=""2"" />
      <Blocks Index=""144"" Count=""3"" />
      <Blocks Index=""151"" Count=""2"" />
      <Blocks Index=""156"" Count=""5"" />
      <Blocks Index=""204"" Count=""10"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSOHTMED.EXE"">
      <Blocks Index=""0"" Count=""1"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSOUC.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""2"" Count=""4"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSPUB.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""32"" Count=""2"" />
      <Blocks Index=""111"" Count=""3"" />
      <Blocks Index=""127"" Count=""5"" />
      <Blocks Index=""150"" Count=""7"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\MSTORE.EXE"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OIS.EXE"">
      <Blocks Index=""0"" Count=""2"" />
      <Blocks Index=""3"" Count=""2"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OISGRAPH.DLL"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OISAPP.DLL"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""10"" Count=""4"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\ONENOTE.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""4"" Count=""2"" />
      <Blocks Index=""15"" Count=""3"" />
      <Blocks Index=""20"" Count=""6"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\ONMAIN.DLL"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""21"" Count=""4"" />
      <Blocks Index=""37"" Count=""2"" />
      <Blocks Index=""127"" Count=""3"" />
      <Blocks Index=""131"" Count=""10"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\OUTLOOK.EXE"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""36"" Count=""3"" />
      <Blocks Index=""65"" Count=""2"" />
      <Blocks Index=""205"" Count=""2"" />
      <Blocks Index=""210"" Count=""2"" />
      <Blocks Index=""221"" Count=""5"" />
      <Blocks Index=""230"" Count=""13"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\POWERPNT.EXE"">
      <Blocks Index=""0"" Count=""3"" />
      <Blocks Index=""31"" Count=""2"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\PPCORE.DLL"">
      <Blocks Index=""0"" Count=""1"" />
      <Blocks Index=""6"" Count=""2"" />
      <Blocks Index=""19"" Count=""2"" />
      <Blocks Index=""30"" Count=""2"" />
      <Blocks Index=""126"" Count=""5"" />
      <Blocks Index=""135"" Count=""2"" />
      <Blocks Index=""139"" Count=""9"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\SELFCERT.EXE"">
      <Blocks Index=""0"" Count=""2"" />
      <Blocks Index=""3"" Count=""2"" />
      <Blocks Index=""6"" Count=""2"" />
    </File>
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\SETLANG.EXE"" />
    <File Name=""Root\VFS\ProgramFilesX86\Microsoft Office\Office14\WINWORD.EXE"">
      <Blocks Index=""0"" Count=""3"" />
      <Blocks Index=""20"" Count=""2"" />
    </File>
  </FeatureBlock>
</StreamMap>";
            return Encoding.UTF8.GetBytes(s);
        }

        private byte[] GeneratePackageHistory()
        {
            var mem = new MemoryStream();
            using (var xml = new XmlTextWriter(mem, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 2,
                IndentChar = ' ',
            })
            {
                xml.WriteStartElement("PackageHistory");
                xml.WriteAttributeString("xmlns", "http://schemas.microsoft.com/appv/2010/PackageHistory");
                xml.WriteStartElement("PackageHistoryItem");
                xml.WriteAttributeString("Time", DateTime.UtcNow.ToString("O"));
                xml.WriteAttributeString("PackageVersion", Guid.NewGuid().ToString());
                xml.WriteAttributeString("SequencerVersion", "5.0.285.0");
                xml.WriteAttributeString("SequencerUser", WindowsIdentity.GetCurrent().Name);
                xml.WriteAttributeString("SequencingStation", Environment.MachineName);
                {
                    var key =
                        RegistryTools.GetKeyFromFullPath(
                            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (key == null)
                        throw new KeyNotFoundException();
                    var version = key.GetValue("CurrentVersion") as string;
                    var build = key.GetValue("CurrentBuildNumber") as string;
                    if (version == null || build == null)
                        throw new Exception("Value not found or not of correct type.");
                    xml.WriteAttributeString("WindowsVersion", version + "." + build + ".65536");
                }
                xml.WriteAttributeString("SystemFolder", SystemDirectories.SystemSystem);
                xml.WriteAttributeString("WindowsFolder", SystemDirectories.Windows);
                xml.WriteAttributeString("UserFolder", SystemDirectories.CurrentProfileDirectory);
                xml.WriteAttributeString("SystemType", "1");
                var processorInfo = new Dictionary<string, string>();
                {
                    string info = null;
                    {
                        var searcher =
                            new ManagementObjectSearcher("select NumberOfCores, Caption, Manufacturer, AddressWidth from Win32_Processor");
                        var collection = searcher.Get();
                        var enumerator = collection.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            info = enumerator.Current.GetText(new TextFormat());
                        }
                    }
                    if (info != null)
                    {
                        var regex = new Regex(@"^[ \t]*([A-Za-z]+) \= (\"".*\""|[0-9]+);$");
                        foreach (var line in info.Split('\n'))
                        {
                            var match = regex.Match(line);
                            if (!match.Success)
                                continue;
                            var value = match.Groups[2].ToString();
                            if (value[0] == '"')
                                value = value.Substring(1, value.Length - 2);
                            processorInfo[match.Groups[1].ToString()] = value;
                        }
                    }
                }
                {
                    var sb = new StringBuilder();
                    string processorCaption,
                        coreCount,
                        processorManufacturer;
                    if (processorInfo.TryGetValue("Caption", out processorCaption))
                    {
                        if (processorInfo.TryGetValue("NumberOfCores", out coreCount))
                        {
                            sb.Append(coreCount);
                            sb.Append(" X ");
                        }
                        sb.Append(processorCaption);
                        if (processorInfo.TryGetValue("Manufacturer", out processorManufacturer))
                        {
                            sb.Append(", ");
                            sb.Append(processorManufacturer);
                        }
                    }
                    xml.WriteAttributeString("Processor", sb.ToString());
                }
                xml.WriteAttributeString("LastRebootNormal", "0");
                xml.WriteAttributeString("TerminalServices", "1");
                xml.WriteAttributeString("RemoteSession", "0");
                xml.WriteAttributeString("NetFrameworkVersion", "4.0.30319.1");

                {
                    var key =
                        RegistryTools.GetKeyFromFullPath(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer");
                    if (key != null)
                    {
                        var version = key.GetValue("Version") as string;
                        if (version != null)
                            xml.WriteAttributeString("IEVersion", version);
                    }
                }

                {
                    string bitness;
                    if (processorInfo.TryGetValue("AddressWidth", out bitness))
                        xml.WriteAttributeString("PackageOSBitness", bitness);
                }
                xml.WriteAttributeString("PackagingEngine", "SpyStudio");
                xml.WriteAttributeString("Locale", CultureInfo.CurrentCulture.IetfLanguageTag);
                xml.WriteAttributeString("InUpgrade", "false");
                xml.WriteAttributeString("SaveMode", "eSave");

                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            return mem.ToArray();
        }

        private byte[] GenerateAppxManifest()
        {
            var manifest = Export.WaitForManifest();
            /*
            var manifest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Package IgnorableNamespaces=""appv"" xmlns=""http://schemas.microsoft.com/appx/2010/manifest"" xmlns:appv=""http://schemas.microsoft.com/appv/2010/manifest"">
	<Identity Name=""Reserved"" Publisher=""CN=Reserved"" Version=""0.0.0.1"" appv:PackageId=""1a2ad958-75d0-440b-9e6b-1d5e119713ee"" appv:VersionId=""466ada43-2c70-4757-ae56-9dc9919cf7b2"" />
	<Properties>
		<DisplayName>Office2010</DisplayName>
		<PublisherDisplayName>Reserved</PublisherDisplayName>
		<Description>Reserved</Description>
		<Logo>Reserved.jpeg</Logo>
		<appv:AppVPackageDescription>No description entered</appv:AppVPackageDescription>
	</Properties>
	<Resources>
		<Resource Language=""en-us"" />
	</Resources>
	<Prerequisites>
		<OSMinVersion>6.1</OSMinVersion>
		<OSMaxVersionTested>6.1</OSMaxVersionTested>
		<appv:TargetOSes SequencingStationProcessorArchitecture=""x64"" />
	</Prerequisites>
	<appv:Extensions/>
	<appv:AssetIntelligence>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0016-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Excel MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0018-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office PowerPoint MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-001a-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Outlook MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0116-0409-1000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Shared 64-bit Setup Metadata MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0044-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office InfoPath MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-00a1-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office OneNote MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-002c-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Proofing (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-002a-0409-1000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Shared 64-bit MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0011-0000-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Professional Plus 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID>82503-018-0000106-48421</appv:ProductID>
			<appv:Language>0</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser>admin</appv:RegisteredUser>
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-001f-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Proof (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0015-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Access MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-001b-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Word MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-00ba-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Groove MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0117-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Access Setup Metadata MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-001f-0c0a-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Proof (Spanish) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>3082</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-001f-040c-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Proof (French) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1036</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0115-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Shared Setup Metadata MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-002a-0000-1000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Office 64-bit Components 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>0</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-0019-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Publisher MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
		<appv:AssetIntelligenceProperties>
			<appv:SoftwareCode>{90140000-006e-0409-0000-0000000ff1ce}</appv:SoftwareCode>
			<appv:ProductName>Microsoft Office Shared MUI (English) 2010</appv:ProductName>
			<appv:ProductVersion>14.0.4763.1000</appv:ProductVersion>
			<appv:Publisher>Microsoft Corporation</appv:Publisher>
			<appv:ProductID />
			<appv:Language>1033</appv:Language>
			<appv:ChannelCode />
			<appv:InstallDate>20141010</appv:InstallDate>
			<appv:RegisteredUser />
			<appv:InstalledLocation>[{ProgramFilesX86}]\Microsoft Office\</appv:InstalledLocation>
			<appv:CM_DSLID />
			<appv:VersionMajor>14</appv:VersionMajor>
			<appv:VersionMinor>0</appv:VersionMinor>
			<appv:ServicePack />
			<appv:UpgradeCode />
			<appv:OsComponent>1</appv:OsComponent>
		</appv:AssetIntelligenceProperties>
	</appv:AssetIntelligence>
	<Applications xmlns=""http://schemas.microsoft.com/appv/2010/manifest"">
		<Application Id=""[{ProgramFilesCommonX86}]\microsoft shared\OFFICE14\MSOXMLED.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesCommonX86}]\microsoft shared\OFFICE14\MSOXMLED.EXE</Target>
			<VisualElements>
				<Name>XML Editor</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Internet Explorer\iexplore.exe"" Origin=""Application"" TargetInPackage=""false"">
			<Target>[{ProgramFilesX86}]\Internet Explorer\iexplore.exe</Target>
			<VisualElements>
				<Name>Internet Explorer</Name>
				<Version>9.0.8112.16520</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\EXCEL.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\EXCEL.EXE</Target>
			<VisualElements>
				<Name>Microsoft Excel 2010</Name>
				<Version>14.0.4756.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\GROOVE.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\GROOVE.EXE</Target>
			<VisualElements>
				<Name>Microsoft SharePoint Workspace 2010</Name>
				<Version>14.0.4761.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\INFOPATH.EXE"" Origin=""User"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\INFOPATH.EXE</Target>
			<VisualElements>
				<Name>Microsoft InfoPath Designer 2010</Name>
				<Version>14.0.4763.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\MSACCESS.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\MSACCESS.EXE</Target>
			<VisualElements>
				<Name>Microsoft Access 2010</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\MSOUC.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\MSOUC.EXE</Target>
			<VisualElements>
				<Name>Microsoft Office 2010 Upload Center</Name>
				<Version>14.0.4757.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\MSPUB.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\MSPUB.EXE</Target>
			<VisualElements>
				<Name>Microsoft Publisher 2010</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\MSTORE.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\MSTORE.EXE</Target>
			<VisualElements>
				<Name>Microsoft Clip Organizer</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\OIS.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\OIS.EXE</Target>
			<VisualElements>
				<Name>Microsoft Office Picture Manager</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\ONENOTE.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\ONENOTE.EXE</Target>
			<VisualElements>
				<Name>Microsoft OneNote 2010</Name>
				<Version>14.0.4763.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\OUTLOOK.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\OUTLOOK.EXE</Target>
			<VisualElements>
				<Name>Microsoft Outlook 2010</Name>
				<Version>14.0.4760.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\POWERPNT.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\POWERPNT.EXE</Target>
			<VisualElements>
				<Name>Microsoft PowerPoint 2010</Name>
				<Version>14.0.4754.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\SELFCERT.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\SELFCERT.EXE</Target>
			<VisualElements>
				<Name>Digital Certificate for VBA Projects</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\SETLANG.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\SETLANG.EXE</Target>
			<VisualElements>
				<Name>Microsoft Office 2010 Language Preferences</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\WINWORD.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\WINWORD.EXE</Target>
			<VisualElements>
				<Name>Microsoft Word 2010</Name>
				<Version>14.0.4762.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{System}]\notepad.exe"" Origin=""Application"" TargetInPackage=""false"">
			<Target>[{System}]\notepad.exe</Target>
			<VisualElements>
				<Name>Notepad</Name>
				<Version>6.1.7600.16385</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{System}]\rundll32.exe"" Origin=""User"" TargetInPackage=""false"">
			<Target>[{System}]\rundll32.exe</Target>
			<VisualElements>
				<Name>Windows host process (Rundll32)</Name>
				<Version>6.1.7600.16385</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesCommonX86}]\Microsoft Shared\MSEnv\VSLauncher.exe"" Origin=""Application"" TargetInPackage=""false"">
			<Target>[{ProgramFilesCommonX86}]\Microsoft Shared\MSEnv\VSLauncher.exe</Target>
			<VisualElements>
				<Name>VSLauncher</Name>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesCommonX86}]\Microsoft Shared\VSTO\vstoee.dll"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesCommonX86}]\Microsoft Shared\VSTO\vstoee.dll</Target>
			<VisualElements>
				<Name>Visual Studio Tools for Office Execution Engine</Name>
				<Version>10.0.21022.2</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\MSOHTMED.EXE"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\MSOHTMED.EXE</Target>
			<VisualElements>
				<Name>Microsoft Office 2010 component</Name>
				<Version>14.0.4730.1010</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Office\Office14\OMSMAIN.DLL"" Origin=""Application"" TargetInPackage=""true"">
			<Target>[{ProgramFilesX86}]\Microsoft Office\Office14\OMSMAIN.DLL</Target>
			<VisualElements>
				<Name>Microsoft Outlook Mobile Service</Name>
				<Version>14.0.4750.1000</Version>
			</VisualElements>
		</Application>
		<Application Id=""[{ProgramFilesX86}]\Microsoft Visual Studio 8\Common7\IDE\vsta.exe"" Origin=""Application"" TargetInPackage=""false"">
			<Target>[{ProgramFilesX86}]\Microsoft Visual Studio 8\Common7\IDE\vsta.exe</Target>
			<VisualElements>
				<Name>vsta</Name>
			</VisualElements>
		</Application>
	</Applications>
	<appv:ExtensionsConfiguration />
</Package>";
            */
            return Encoding.UTF8.GetBytes(manifest);

        }

        private byte[] GenerateBlockMap(Dictionary<string, ZipEntry> entryDict)
        {
            var mem = new MemoryStream();
            {
                var s = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>" + "\r\n";
                var array = Encoding.UTF8.GetBytes(s).ToArray();
                mem.Write(array, 0, array.Length);
            }

            using (var xml = new XmlTextWriter(mem, Encoding.UTF8))
            {
                xml.WriteStartElement("BlockMap");
                xml.WriteAttributeString("xmlns", "http://schemas.microsoft.com/appx/2010/blockmap");
                xml.WriteAttributeString("HashMethod", "http://www.w3.org/2001/04/xmlenc#sha256");
                entryDict.ForEach(x => GetHashesForFile(x.Value.FilePath, x.Key, xml, x.Value));
                xml.WriteEndElement();
            }
            return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(mem.ToArray()).Replace("\" />", "\"/>"));
        }

        private byte[] GenerateContentTypes(Dictionary<string, ZipEntry> entryDict)
        {
            var regex = new Regex(@".*\.([^.\\/]+)$");
            var mem = new MemoryStream();
            {
                var s = @"<?xml version=""1.0"" encoding=""UTF-8""?>";
                var array = Encoding.UTF8.GetBytes(s).ToArray();
                mem.Write(array, 0, array.Length);
            }
            const string defaultType = "appv/vfs-file";
            using (var xml = new XmlTextWriter(mem, Encoding.UTF8))
            {
                xml.WriteStartElement("Types");
                xml.WriteAttributeString("xmlns", "http://schemas.openxmlformats.org/package/2006/content-types");
                var extensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var s in entryDict.Keys)
                {
                    var match = regex.Match(s);
                    if (!match.Success)
                    {
                        xml.WriteStartElement("Override");
                        xml.WriteAttributeString("PartName", "/" + s.Replace("\\", "/"));
                    }
                    else
                    {
                        var ext = match.Groups[1].ToString();
                        if (extensions.Contains(ext))
                            continue;
                        extensions.Add(ext);
                        xml.WriteStartElement("Default");
                        xml.WriteAttributeString("Extension", ext);
                    }
                    xml.WriteAttributeString("ContentType", defaultType);
                    xml.WriteEndElement();
                }

                xml.WriteStartElement("Override");
                xml.WriteAttributeString("PartName", "/AppxManifest.xml");
                xml.WriteAttributeString("ContentType", "application/vnd.ms-appx.manifest+xml");
                xml.WriteEndElement();

                xml.WriteStartElement("Override");
                xml.WriteAttributeString("PartName", "/AppxBlockMap.xml");
                xml.WriteAttributeString("ContentType", "application/vnd.ms-appx.blockmap+xml");
                xml.WriteEndElement();

                xml.WriteEndElement();
            }
            return mem.ToArray();
        }

        private string ExportRegistry(IEnumerable<RegKeyInfo> registry)
        {
            var ret = FileSystemTools.GetTempFilename("Registry", "dat");
            try
            {
                AppvRegistryWriter.ExportRegistry(ret, registry);
            }
            catch (AppvRegistryWriter.NativeErrorException)
            {
                //TODO: Report error.
                //throw new NotImplementedException();
                ret = null;
            }
            return ret;
        }

        public override void Stop()
        {
        }
    }
}
