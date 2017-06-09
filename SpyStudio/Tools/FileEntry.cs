using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using SpyStudio.Export;
using SpyStudio.FileSystem;

namespace SpyStudio.Tools
{
    [Serializable]
    public class FileEntry
    {
        public static FileEntry From(Declarations.WIN32_FIND_DATA fileData)
        {
            var entry = new FileEntry();

            entry.Path = fileData.cFileName;
            entry.IsDirectory = (fileData.dwFileAttributes & 16) > 0;

            return entry;
        }

        public static FileEntry ForPath(string aFileSystemPath, string normalizedPath)
        {
            var fileEntry = new FileEntry
                                {
                                    Access = FileSystemAccess.None,
                                    FileSystemPath = aFileSystemPath,
                                    IsShortcut = normalizedPath.EndsWith(".lnk"),
                                    IsFileCreated = false,
                                    IsRecursive = false,
                                    Path = normalizedPath,
                                    PathType = PathTypes.Absolute,
                                    Success = true,
                                    Icon = FileSystemTools.GetIcon(aFileSystemPath)
                                };
            
            fileEntry.TryToSetFileInfoUsing(aFileSystemPath);
            
            return fileEntry;
        }

        public void TryToSetFileInfoUsing(string aPath)
        {
            IsDirectory = Directory.Exists(aPath);
            TargetPath = aPath.EndsWith(".lnk") ? FileSystemTools.GetShortcutTarget(aPath) : null;

            if (IsDirectory)
                return;

            try
            {
                string originalFilename = null, company = null, description = null, version = null, product = null;
                FileSystemTools.GetFileProperties(aPath, ref originalFilename, ref product, ref company,
                                                  ref version, ref description);
                Company = company ?? string.Empty;
                Description = description ?? string.Empty;
                Version = version ?? "0.0.0.0";
                OriginalFileName = originalFilename ?? string.Empty;
                Product = product ?? string.Empty;
            }
            catch (Exception)
            {
                Company = string.Empty;
                Description = string.Empty;
                Version = string.Empty;
                OriginalFileName = string.Empty;
                Product = string.Empty;
            }
        }

        public enum PathTypes
        {
            Absolute,
            Search
        }

        [XmlIgnore]
        public string ValidPath
        {
            get { return FileSystemPath ?? Path; }
        }

        [XmlElement]
        public string Path { get; set; }
        [XmlElement]
        public FileSystemAccess Access { get; set; }

        [XmlAttribute]
        public bool Success { get; set; }
        public bool CompanySpecified
        {
            get { return !string.IsNullOrEmpty(Company); }
        }
        [XmlElement]
        public string Company { get; set; }
        public bool VersionSpecified
        {
            get { return !string.IsNullOrEmpty(Version); }
        }
        [XmlElement]
        public string Version { get; set; }
        public bool DescriptionSpecified
        {
            get { return !string.IsNullOrEmpty(Description); }
        }
        [XmlElement]
        public string Description { get; set; }
        public bool ProductSpecified
        {
            get { return !string.IsNullOrEmpty(Product); }
        }
        [XmlElement]
        public string Product { get; set; }
        public bool OriginalFileNameSpecified
        {
            get { return !string.IsNullOrEmpty(OriginalFileName); }
        }
        [XmlElement]
        public string OriginalFileName { get; set; }
        [XmlIgnore]
        public Image Icon { get; set; }
        [XmlElement]
        public string FileSystemPath { get; set; }

        [XmlElement]
        public string TargetPath { get; set; }
        [XmlAttribute]
        [DefaultValueAttribute(false)]
        public bool IsDirectory { get; set; }
        [XmlAttribute]
        [DefaultValueAttribute(false)]
        public bool IsShortcut { get; set; }
        [XmlAttribute]
        [DefaultValueAttribute(false)]
        public bool IsFileCreated { get; set; }
        [XmlAttribute]
        [DefaultValueAttribute(false)]
        public bool IsRecursive { get; set; }
        [XmlElement]
        public PathTypes PathType { get; set; }
        [XmlIgnore]
        public object Tag { get; set; }

        public FileEntry()
        {
        }

        public FileEntry(string path, string fileSystemPath, FileSystemAccess access, bool success, string company, string version,
                         string description, string productName, string originalFileName, Image icon)
        {
            Path = path;
            FileSystemPath = fileSystemPath;
            Access = access;
            Success = success;
            Company = company;
            Version = version;
            Description = description;
            Product = productName;
            OriginalFileName = originalFileName;
            Icon = icon;
        }

        public FileEntry(string path, FileSystemAccess access)
        {
            Path = path;
            Access = access;
            Success = true;
            Company = null;
            Version = null;
            Description = null;
            Product = null;
            OriginalFileName = path;
            Icon = null;
        }

        public FileEntry CopyForSerialization(PathNormalizer normalizer)
        {
            var ret = new FileEntry(this)
                       {
                           Path = GeneralizedPathNormalizer.Generalize(Path, normalizer),
                       };
            if (TargetPath != null)
                ret.TargetPath = GeneralizedPathNormalizer.Generalize(TargetPath, normalizer);
            else
                ret.TargetPath = null;
            return ret;
        }

        public FileEntry CopyForDeserialization(PathNormalizer normalizer)
        {
            var ret = new FileEntry(this)
                       {
                           Path = GeneralizedPathNormalizer.Specificize(Path, normalizer),
                       };
            if (TargetPath != null)
                ret.TargetPath = GeneralizedPathNormalizer.Generalize(TargetPath, normalizer);
            else
                ret.TargetPath = null;
            return ret;
        }

        // Contructs a proper FileEntry instance from the this parameter. If
        // this is of a derived type, the return value is still just a
        // FileEntry.
        public virtual FileEntry HalfClone()
        {
            return new FileEntry(this);
        }

        protected FileEntry(FileEntry oldEntry)
        {
            Path = oldEntry.Path;
            FileSystemPath = oldEntry.FileSystemPath;
            Access = oldEntry.Access;
            Success = oldEntry.Success;
            Company = oldEntry.Company;
            Version = oldEntry.Version;
            Description = oldEntry.Description;
            Product = oldEntry.Product;
            OriginalFileName = oldEntry.OriginalFileName;
            Icon = oldEntry.Icon;
            IsDirectory = oldEntry.IsDirectory;
            IsShortcut = oldEntry.IsShortcut;
            IsRecursive = oldEntry.IsRecursive;
            PathType = oldEntry.PathType;
            IsFileCreated = oldEntry.IsFileCreated;
            TargetPath = oldEntry.TargetPath;
        }

        //public FileEntry(string path, FileSystemAccess access, bool success)
        //{
        //    Path = path;
        //    Access = access;
        //    Success = success;
        //}

        private static readonly FileSystemAccess[] GetEntriesAccesses = new[]
                                                                            {
                                                                                FileSystemAccess.Read,
                                                                                FileSystemAccess.Write,
                                                                                FileSystemAccess.Delete,
                                                                                FileSystemAccess.Execute,
                                                                                FileSystemAccess.CreateDirectory,
                                                                                FileSystemAccess.LoadLibrary,
                                                                                FileSystemAccess.Resource,
                                                                                FileSystemAccess.ReadAttributes,
                                                                                FileSystemAccess.Synchronize,
                                                                                FileSystemAccess.WriteAttributes,
                                                                                FileSystemAccess.CreateProcess
                                                                            };

        public IEnumerable<FileEntry> GetEntries()
        {
            if (Access == FileSystemAccess.None)
            {
                yield return this;
                yield break;
            }

            foreach (var testAccess in GetEntriesAccesses)
            {
                if ((Access & testAccess) != testAccess)
                    continue;
                var entry = HalfClone();
                entry.Access = testAccess;
                yield return entry;
            }

            if ((Access & FileSystemAccess.CreateDirectory) != 0)
            {
                var index = Path.LastIndexOf('\\');
                if (index == -1)
                {
                    var entry = HalfClone();
                    entry.Access = FileSystemAccess.ReadWrite;
                    entry.Path = Path.Substring(0, index);
                    yield return entry;
                }
            }
        }

        public virtual FileEntry GetUpdatedEntryFromFileSystem()
        {
            return ForPath(FileSystemPath, Path);
        }
    }
}