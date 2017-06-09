using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using Nektra.Deviare2;
using SpyStudio.Extensions;
using SpyStudio.FileAssociation;
using SpyStudio.Properties;
using SpyStudio.Trace;
using System.Linq;
using File = System.IO.File;

namespace SpyStudio.Tools
{
    [Flags]
    public enum FileSystemAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
        Delete = 4,
        Execute = 8,
        CreateDirectory = 16,
        LoadLibrary = 32,
        Resource = 64,
        ReadAttributes = 128,
        Synchronize = 256,
        WriteAttributes = 512,
        CreateProcess = 1024,
        ReadWrite = Read | Write
    }

    public class ShortcutInfo
    {
        public enum ShowModeOptions
        {
            Normal,
            Minimized,
            Maximized
        }

        public string Name { get; set; }
        public string TargetPath { get; set; }
        public string Args { get; set; }
        public string ShowMode { get; set; }
        public string CurrentDir { get; set; }
        public string IconFile { get; set; }
        public string IconIndex { get; set; }
    }

    public class CreatedFileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Version { get; set; }
    }

    public class FileSystemTools
    {
        [Flags]
        public enum FileAttribute
        {
            FileAttributeNone = 0,
            FileAttributeReadonly = 0x00000001,
            FileAttributeHidden = 0x00000002,
            FileAttributeSystem = 0x00000004,
            FileAttributeDirectory = 0x00000010,
            FileAttributeArchive = 0x00000020,
            FileAttributeDevice = 0x00000040,
            FileAttributeNormal = 0x00000080,
            FileAttributeTemporary = 0x00000100,
            FileAttributeSparseFile = 0x00000200,
            FileAttributeReparsePoint = 0x00000400,
            FileAttributeCompressed = 0x00000800,
            FileAttributeOffline = 0x00001000,
            FileAttributeNotContentIndexed = 0x00002000,
            FileAttributeEncrypted = 0x00004000,
            FileAttributeVirtual = 0x00010000
        }

        public class FileInformation
        {
            public FileInformation(string filename, string fieldName, FileAttribute attributes)
            {
                Filename = filename;
                FieldName = fieldName;
                Attributes = attributes;
            }

            public string Filename { get; set; }
            public string FieldName { get; set; }
            public FileAttribute Attributes { get; set; }

            public bool IsReadonly
            {
                get { return (Attributes & FileAttribute.FileAttributeReadonly) != 0; }
            }

            public bool IsHidden
            {
                get { return (Attributes & FileAttribute.FileAttributeHidden) != 0; }
            }

            public bool IsSystem
            {
                get { return (Attributes & FileAttribute.FileAttributeSystem) != 0; }
            }

            public bool IsDirectory
            {
                get { return (Attributes & FileAttribute.FileAttributeDirectory) != 0; }
            }

            public bool IsArchive
            {
                get { return (Attributes & FileAttribute.FileAttributeArchive) != 0; }
            }

            public bool IsDevice
            {
                get { return (Attributes & FileAttribute.FileAttributeDevice) != 0; }
            }

            public bool IsNormal
            {
                get { return (Attributes & FileAttribute.FileAttributeNormal) != 0; }
            }

            public bool IsTemporary
            {
                get { return (Attributes & FileAttribute.FileAttributeTemporary) != 0; }
            }

            public bool IsSparseFile
            {
                get { return (Attributes & FileAttribute.FileAttributeSparseFile) != 0; }
            }

            public bool IsReparsePoint
            {
                get { return (Attributes & FileAttribute.FileAttributeReparsePoint) != 0; }
            }

            public bool IsCompressed
            {
                get { return (Attributes & FileAttribute.FileAttributeCompressed) != 0; }
            }

            public bool IsOffline
            {
                get { return (Attributes & FileAttribute.FileAttributeOffline) != 0; }
            }

            public bool IsNotContentIndexed
            {
                get { return (Attributes & FileAttribute.FileAttributeNotContentIndexed) != 0; }
            }

            public bool IsEncrypted
            {
                get { return (Attributes & FileAttribute.FileAttributeEncrypted) != 0; }
            }

            public bool IsVirtual
            {
                get { return (Attributes & FileAttribute.FileAttributeVirtual) != 0; }
            }

            public static bool IsAttributeReadonly(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeReadonly) != 0;
            }

            public static bool IsAttributeHidden(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeHidden) != 0;
            }

            public static bool IsAttributeSystem(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeSystem) != 0;
            }

            public static bool IsAttributeDirectory(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeDirectory) != 0;
            }

            public static bool IsAttributeArchive(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeArchive) != 0;
            }

            public static bool IsAttributeDevice(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeDevice) != 0;
            }

            public static bool IsAttributeNormal(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeNormal) != 0;
            }

            public static bool IsAttributeTemporary(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeTemporary) != 0;
            }

            public static bool IsAttributeSparseFile(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeSparseFile) != 0;
            }

            public static bool IsAttributeReparsePoint(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeReparsePoint) != 0;
            }

            public static bool IsAttributeCompressed(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeCompressed) != 0;
            }

            public static bool IsAttributeOffline(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeOffline) != 0;
            }

            public static bool IsAttributeNotContentIndexed(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeNotContentIndexed) != 0;
            }

            public static bool IsAttributeEncrypted(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeEncrypted) != 0;
            }

            public static bool IsAttributeVirtual(FileAttribute attr)
            {
                return (attr & FileAttribute.FileAttributeVirtual) != 0;
            }
        }

        private static readonly NktTools DevTools = new NktTools();

        public static string GetUserDataPath()
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dir = Path.Combine(dir, Settings.Default.AppName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetFileHandlePath(IntPtr hFile, uint pid)
        {
            try
            {
                return DevTools.GetFileNameFromHandle(hFile, (int) pid);
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Function used to parse FileInformation parameter from NtQueryDirectoryFile
        /// </summary>
        /// <returns></returns>
        //static public bool GetFileInfo(int fileInfoClass, NktParam fileInfo, out NktParam nextFileInfo, ref string name, ref int index, ref uint attributes)
        //{
        //    NktProcessMemory mem = fileInfo.Memory();
        //    fileInfo.A
        //    mem.ReadStringN()
        //    nextFileInfo = null;
        //    if(!fileInfo.IsNullPointer)
        //    {
        //        NktParamsEnum fields = fileInfo.Evaluate().Fields();
        //        uint nextOffset = fields.GetAt(0).ULongVal;
        //    }
        //    return (nextFileInfo != null);
        //}
        /// <summary>
        /// Function used to parse OBJECT_ATTRIBUTES structure
        /// </summary>
        /// <returns></returns>
        public static string GetFileHandlePath(NktParam objAttributes, uint pid)
        {
            var filepath = "";
            if (!objAttributes.IsNullPointer)
            {
                var fields = objAttributes.Evaluate().Fields();
                var path = fields.GetAt(2);

                if (!path.IsNullPointer)
                {
                    filepath = NativeApiTools.GetUnicodeString(path);
                }
                var hFile = fields.GetAt(1).SizeTVal;
                if (hFile != IntPtr.Zero)
                {
                    filepath = DevTools.GetFileNameFromHandle(hFile, (int) pid) +
                               (!string.IsNullOrEmpty(filepath) ? @"\" + filepath : "");
                }
            }
            return filepath;
        }

        private static readonly Dictionary<int, StringBuilder> TempStrings = new Dictionary<int, StringBuilder>();

        /// <summary>
        /// StringBuilder cache: create only 1 StringBuilder per thread
        /// </summary>
        /// <returns></returns>
        public static StringBuilder GetTempString()
        {
            StringBuilder ret;
            var tid = Thread.CurrentThread.ManagedThreadId;
            if (!TempStrings.TryGetValue(tid, out ret))
            {
                TempStrings[tid] = ret = new StringBuilder(16000);
            }
            return ret;
        }

        public static string GetDirectory(string path)
        {
            if (path.Length == 0)
                return "";

            if (path.StartsWith("\""))
                path = path.Remove(0, 1);
            if (path.EndsWith("\""))
                path = path.Remove(path.Length - 1);
            var index = path.LastIndexOf("\\", StringComparison.Ordinal);
            if (index == -1)
            {
                index = path.LastIndexOf("/", StringComparison.Ordinal);
            }
            var filename = index != -1 ? path.Substring(0, index) : "";

            return filename;
        }

        public static long CopyFileWithoutAttributes(string dst, string src, Action<long, long> progressCallback)
        {
            var buffer = new byte[4*4096];
            long ret = 0, accum = 0;
            using (var input = new FileStream(src, FileMode.Open, FileAccess.Read))
            {
                using (var output = new FileStream(dst, FileMode.CreateNew, FileAccess.Write))
                {
                    ret = input.Length;
                    while (input.Position < input.Length)
                    {
                        var bytesRead = input.Read(buffer, 0, buffer.Length);
                        accum += bytesRead;
                        if (progressCallback != null)
                            progressCallback(accum, ret);
                        if (bytesRead == 0)
                            break;
                        output.Write(buffer, 0, bytesRead);
                    }
                }
                return ret;
            }
        }

        public static string GetFileName(string path)
        {
            if (path.Length == 0)
                return string.Empty;

            if (path[0] == '"')
            {
                if (path.Length == 1)
                    return string.Empty;
                path = path.Substring(1);
            }
            if (path[path.Length - 1] == '"')
            {
                if (path.Length == 1)
                    return string.Empty;
                path = path.Substring(0, path.Length - 1);
            }
            return GetLastNameInPath(path);
        }

        private static readonly char[] Separators = new[] {'\\', '/'};

        public static string GetLastNameInPath(string path)
        {
            var index = path.LastIndexOfAny(Separators);

            return index != -1 ? path.Substring(index + 1) : path;
        }

        public static string GetNormalizedPath(string path)
        {
            if (path.EndsWith(":favicon"))
            {
                return path.Substring(0, path.Length - 8);
            }
            return path;
        }

        public static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        private class PathReplacement
        {
            public PathReplacement(string original, string replacement, int position)
            {
                Original = original;
                Replacement = replacement;
                Position = position;
            }

            public string Original { get; private set; }
            public string Replacement { get; private set; }
            public int Position { get; private set; }
        }

        public static string GetAccessString(FileSystemAccess access)
        {
            return GetAccessString(access, true);
        }

        public static string GetAccessString(FileSystemAccess access, bool emptyIsNone)
        {
            var ret = "";
            if ((access & FileSystemAccess.LoadLibrary) != 0)
            {
                ret += "LoadLibrary";
            }
            if ((access & FileSystemAccess.CreateProcess) != 0)
            {
                ret += "CreateProcess";
            }
            if ((access & FileSystemAccess.CreateDirectory) != 0)
            {
                ret += "CreateDirectory";
            }
            if ((access & FileSystemAccess.Read) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "Read";
            }
            if ((access & FileSystemAccess.Write) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "Write";
            }
            if ((access & FileSystemAccess.Execute) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "Execute";
            }
            if ((access & FileSystemAccess.Delete) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "Delete";
            }
            if ((access & FileSystemAccess.Resource) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "Resource";
            }
            if ((access & FileSystemAccess.ReadAttributes) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "ReadAttributes";
            }
            if ((access & FileSystemAccess.WriteAttributes) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "WriteAttributes";
            }
            if ((access & FileSystemAccess.Synchronize) != 0)
            {
                if (ret != "")
                    ret += " ";
                ret += "Synchronize";
            }
            if (string.IsNullOrEmpty(ret) && emptyIsNone)
                ret = "None";

            return ret;
        }

        //static Image _defaultIcon;
        //static Image _defaultIconXml;
        //static Image _defaultIconExe;

        public static Image GetSmallIcon(IntPtr handle)
        {
            Image smallIcon;
            using (var ico = Icon.FromHandle(handle))
            {
                using (var bm = new Bitmap(ico.ToBitmap()))
                {

                    smallIcon = new Bitmap(16, 16);
                    using (var g = Graphics.FromImage(smallIcon))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bm, new Rectangle(0, 0, 16, 16), new Rectangle(0, 0, bm.Width, bm.Height),
                                    GraphicsUnit.Pixel);
                    }
                }
                Declarations.DestroyIcon(handle);
            }
            return smallIcon;
        }

        public static Image AddIconOverlay(Image originalImage, Image overlay, Point overlayPosition)
        {
            var bitmap = new Bitmap(16, 16);
            var g = Graphics.FromImage(bitmap);
            g.DrawImage(originalImage, new Point(0, 0));
            g.DrawImage(overlay, overlayPosition);
            g.Save();
            g.Dispose();
            return bitmap;
        }

        /// <summary>
        /// Get shortcut icon merged with baseImage. If baseImage is null it returns a 16x16 shortcut icon
        /// </summary>
        /// <param name="baseImage"></param>
        /// <returns></returns>
        public static Image GetShortcutIcon(Image baseImage)
        {
            return GetShellIcon(-16769, baseImage);
        }

        private static Image SetImage(Image dst, FolderType type)
        {
            if (dst == null)
            {
                using (var icon = GetFolderIcon(IconSize.Small, type))
                {
                    dst = icon.ToBitmap();
                }
            }
            return dst;
        }

        private static Image _folderOpenedIcon;

        public static Image GetFolderOpenedIcon()
        {
            return _folderOpenedIcon = SetImage(_folderOpenedIcon, FolderType.Open);
        }

        private static Image _folderClosedIcon;

        public static Image GetFolderClosedIcon()
        {
            return _folderClosedIcon = SetImage(_folderClosedIcon, FolderType.Closed);
        }

        /// <summary>
        /// Get shell32 icon merged with baseImage. If baseImage is null it returns a 16x16 shell icon
        /// </summary>
        public static Image GetShellIcon(int index)
        {
            return GetShellIcon(index, null);
        }

        /// <summary>
        /// Get shell32 icon merged with baseImage. If baseImage is null it returns a 16x16 shell icon
        /// </summary>
        /// <param name="baseImage">Image to merge with shell icon. Shell image will be 10x10 over baseImage</param>
        /// <param name="index">Index of the icon</param>
        /// <returns></returns>
        public static Image GetShellIcon(int index, Image baseImage)
        {
            Image ret = null;
            var handles = new IntPtr[1];

            // get from the shell32
            if (Declarations.ExtractIconEx(@"%SystemRoot%\system32\shell32.dll", index, null, handles, 1) > 0)
            {
                var handle = handles[0];
                if (handle != IntPtr.Zero)
                {
                    using (var ico = Icon.FromHandle(handle))
                    {
                        using (var bm = new Bitmap(ico.ToBitmap()))
                        {

                            var width = 16;
                            if (baseImage != null)
                            {
                                width = 10;
                            }

                            ret = new Bitmap(width, width);
                            using (var g = Graphics.FromImage(ret))
                            {
                                g.InterpolationMode =
                                    InterpolationMode.HighQualityBicubic;
                                g.DrawImage(bm, new Rectangle(0, 0, width, width),
                                            new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
                            }
                        }
                        Declarations.DestroyIcon(handle);

                        if (baseImage != null)
                        {
                            ret = AddIconOverlay(baseImage, ret, new Point(0, 6));
                        }
                    }
                }
            }
            return ret;
        }

        public static Image GetIcon(string path)
        {
            return GetIcon(path, false);
        }

        // use this program to get exe default icons because it doesn't have a specific icon
        private static string _lsassPath = null;


        public static string SafeGetExtension(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            string ret;
            try
            {
                ret = Path.GetExtension(s) ?? string.Empty;
            }
            catch (ArgumentException)
            {
                ret = string.Empty;
            }
            return ret.ToLower();
        }

        public static Image GetIcon(string path, bool assumeFileDoesntExist)
        {
            string ext = SafeGetExtension(path);
            bool isExe = ext == ".exe";
            {
                Image cached;
                var cache = IconCache.GetInstance();
                bool success = isExe
                              ? cache.TryGetExeIcon(path, assumeFileDoesntExist, out cached)
                              : cache.TryGetNormalIcon(ext, out cached);
                if (success)
                    return cached;
            }

            var ret = GetIconUncached(path, ext, assumeFileDoesntExist);

            if (ret != null)
            {
                var cache = IconCache.GetInstance();

                if (isExe)
                    cache.SetExeIcon(path, ret);
                else
                    cache.SetNormalIcon(ext, ret);
            }
            return ret;
        }
        public static Image GetIconUncached(string path, string ext, bool assumeFileDoesntExist)
        {
            Image ret = null;
            if (string.IsNullOrEmpty(ext))
                return null;

            var fai = new FileAssociationInfo(ext);
            try
            {
                if (fai.Exists)
                {
                    var icon = fai.DefaultIcon;

                    // WORKAROUND: .drv files are equal to .dll in Shell but ExtractAssociatedIcon brings another
                    if (string.IsNullOrEmpty(icon.Path) && ext == ".drv")
                    {
                        fai = new FileAssociationInfo(".dll");
                        if (fai.Exists)
                            icon = fai.DefaultIcon;
                    }

                    if (!string.IsNullOrEmpty(icon.Path))
                    {
                        if (icon.Path == "%1")
                        {
                            // we need the file to get the icon. 
                            // if not present we will use default icon
                            if (!assumeFileDoesntExist && File.Exists(path))
                            {
                                icon.Path = path;
                            }
                            else
                            {
                                if (ext == ".exe")
                                {
                                    var newPath = SearchPath(path);
                                    if (newPath != null)
                                    {
                                        icon.Path = path = newPath;
                                    }
                                    else
                                    {
                                        if (_lsassPath == null)
                                        {
                                            _lsassPath = SearchPath("lsass.exe") ?? string.Empty;
                                        }
                                        if (!string.IsNullOrEmpty(_lsassPath))
                                            icon.Path = path = _lsassPath;
                                        else
                                            icon = null;
                                    }
                                }
                                else
                                {
                                    icon = null;
                                }
                            }
                        }

                        if (icon != null)
                        {
                            var handles = new IntPtr[1];

                            if (Declarations.ExtractIconEx(icon.Path, icon.Index, null, handles, 1) > 0)
                            {
                                var handle = handles[0];
                                if (handle != IntPtr.Zero)
                                {
                                    using (var ico = Icon.FromHandle(handle))
                                    using (var bm = new Bitmap(ico.ToBitmap()))
                                    {
                                        ret = new Bitmap(16, 16);
                                        using (var g = Graphics.FromImage(ret))
                                        {
                                            g.InterpolationMode =
                                                InterpolationMode.HighQualityBicubic;
                                            g.DrawImage(bm, new Rectangle(0, 0, 16, 16),
                                                        new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
                                        }
                                    }
                                    Declarations.DestroyIcon(handle);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                ret = null;
            }
            if (ret == null)
            {
                if (ext == ".xml")
                {
                    var strB = new StringBuilder("msxml3.dll", 1024);
                    ushort uicon = 0;
                    IntPtr handle =
                        Declarations.ExtractAssociatedIcon(Marshal.GetHINSTANCE(typeof (Declarations).Module),
                                                           strB, ref uicon);
                    ret = GetSmallIcon(handle);
                }
                //else if (ext == ".exe")
                //{
                //    var strB = new StringBuilder(path, 1024);
                //    ushort uicon = 0;
                //    IntPtr handle =
                //        Declarations.ExtractAssociatedIcon(Marshal.GetHINSTANCE(typeof(Declarations).Module),
                //                                           strB, ref uicon);
                //    ret = GetSmallIcon(handle);
                //}
                if (ret == null)
                {
                    Icon ico = null;
                    try
                    {
                        if (File.Exists(path))
                        {
                            try
                            {
                                ico = Icon.ExtractAssociatedIcon(path);
                            }
                            catch (Exception)
                            {
                                ico = null;
                            }
                        }

                        if (ico == null)
                        {
                            var strB = new StringBuilder("shell32.dll", 1024);
                            ushort uicon = 0;
                            var handle =
                                Declarations.ExtractAssociatedIcon(
                                    Marshal.GetHINSTANCE(typeof (Declarations).Module), strB, ref uicon);
                            return ret = GetSmallIcon(handle);
                        }

                        var bm = new Bitmap(ico.ToBitmap());

                        ret = new Bitmap(16, 16);
                        var g = Graphics.FromImage(ret);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bm, new Rectangle(0, 0, 16, 16),
                                    new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
                        g.Dispose();
                        bm.Dispose();
                    }
                    finally
                    {
                        if (ico != null)
                            ico.Dispose();
                    }
                }

                return ret;
            }

            return ret;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        };

        public enum FolderType
        {
            Closed,
            Open
        }

        public enum IconSize
        {
            Large,
            Small
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi,
                                                  uint cbFileInfo, uint uFlags);

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        public const uint SHGFI_OPENICON = 0x000000002;
        public const uint SHGFI_SMALLICON = 0x000000001;
        public const uint SHGFI_LARGEICON = 0x000000000;
        public const uint SHGFI_SHELLICONSIZE = 0x000000004;
        
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public static Icon GetFolderIcon(IconSize size, FolderType folderType)
        {
            // Need to add size check, although errors generated at present!    
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            if (FolderType.Open == folderType)
            {
                flags += SHGFI_OPENICON;
            }
            if (IconSize.Small == size)
            {
                flags += SHGFI_SMALLICON;
            }
            else
            {
                flags += SHGFI_LARGEICON;
            }
            // Get the folder icon    
            var shfi = new SHFILEINFO();

            var res = SHGetFileInfo(@"%WINDIR%",
                                    FILE_ATTRIBUTE_DIRECTORY,
                                    out shfi,
                                    (uint) Marshal.SizeOf(shfi),
                                    flags);

            if (res == IntPtr.Zero)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

            Icon ret;
            using (var temp = Icon.FromHandle(shfi.hIcon))
            {
                // Clone the icon, so that it can be successfully stored in an ImageList
                ret = (Icon) temp.Clone();
            }

            Declarations.DestroyIcon(shfi.hIcon); // Cleanup    

            return ret;
        }

        //public enum FileSystemAccess
        //{
        //    None = 0,
        //    ReadControl = 0x00020000,
        //    WriteDac = 0x00040000,
        //    WriteOwner = 0x00080000,
        //    Delete = 0x00010000,
        //    Synchronize = 0x00100000,
        //    StandardRights = 0x000F0000,
        //    FileReadData = 0x0001,
        //    FileWriteData = 0x0002,
        //    FileReadAttributes = 0x0080,
        //    FileReadEa = 0x0008,
        //    FileWriteAttributes = 0x0100,
        //    FileWriteEa = 0x0010,
        //    AppendData = 0x0004,
        //    Execute = 0x0020,
        //}


        public static FileSystemAccess GetEventAccess(CallEvent e)
        {
            // support old logs before considering LoadResource as a FileSystemEvent
            var access = CreateFileEvent.GetAccess(e);
            //switch (e.Type)
            //{
            //    case HookType.CreateDirectory:
            //        access = FileSystemAccess.CreateDirectory;
            //        break;
            //    case HookType.LoadLibrary:
            //        access = FileSystemAccess.LoadLibrary;
            //        break;
            //    case HookType.QueryDirectoryFile:
            //    case HookType.OpenFile:
            //    case HookType.CreateFile:
            //        access = CreateFileEvent.GetAccess(e);
            //        break;
            //    case HookType.CreateProcess:
            //        access = FileSystemAccess.Execute;
            //        break;
            //    case HookType.FindResource:
            //        access = FileSystemAccess.Resource;
            //        break;
            //}
            return access;
        }

        private static readonly IEnumerable<PathReplacement> PathReplacements64 = new[]
                                                                                      {
                                                                                          new PathReplacement(
                                                                                              "\\\\windows\\\\system32",
                                                                                              "$`\\windows\\SysWOW64$'",
                                                                                              2)
                                                                                      };

        public static string GetCanonicalPathName(uint pid, string filepath, ProcessInfo processInfo)
        {
            /*
            var newFilepath = DevicePathToRegularPath(filepath);
            if (newFilepath.StartsWith(@"\\?\"))
                newFilepath = newFilepath.Substring(4);
            else if (newFilepath.StartsWith(@"\??\"))
                newFilepath = newFilepath.Substring(4);

            var longFile = GetTempString();
            if (0 != Declarations.GetLongPathName(newFilepath, longFile, longFile.Capacity))
            {
                newFilepath = longFile.ToString();
            }

            if (PlatformTools.IsPlatform64Bits() && !processInfo.Is64Bits(pid))
            {
                foreach (var pr in PathReplacements64)
                {
                    var match = Regex.Match(newFilepath, pr.Original, RegexOptions.IgnoreCase);
                    if (match.Success && match.Index == pr.Position)
                    {
                        newFilepath = match.Result(pr.Replacement);
                        break;
                    }
                }
            }

            if (newFilepath.EndsWith(@"\") && !newFilepath.EndsWith(@":\"))
                newFilepath = newFilepath.Substring(0, newFilepath.Length - 1);
            */

            filepath = DevicePathToRegularPath(filepath);
            filepath = CanonicalizePath(filepath, "", false);

            if (PlatformTools.IsPlatform64Bits() && !processInfo.Is64Bits(pid))
            {
                foreach (var pr in PathReplacements64)
                {
                    var match = Regex.Match(filepath, pr.Original, RegexOptions.IgnoreCase);
                    if (match.Success && match.Index == pr.Position)
                    {
                        filepath = match.Result(pr.Replacement);
                        break;
                    }
                }
            }

            /*
            if (newFilepath != filepath)
            {
                Debug.WriteLine("###No match: " + newFilepath + " ? " + filepath);
            }
            */
            return filepath;
        }

        public static string GetLongPathName(string path)
        {
            int n = 128;
            var temp = new StringBuilder(128);
            int l = 0;
            while (true)
            {
                l = Declarations.GetLongPathName(path, temp, n);
                if (l == 0)
                    return path;
                if (l <= n)
                    break;
                temp = new StringBuilder(n = l);
            }
            temp.Length = l;
            return temp.ToString();
        }

        public static string GetShortPathName(string path)
        {
            while (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            uint n = 128;
            var temp = new StringBuilder(128);
            uint l = 0;
            while (true)
            {
                l = Declarations.GetShortPathName(path, temp, n);
                if (l == 0)
                    return path;
                if (l <= n)
                    break;
                n = l;
                temp = new StringBuilder((int)n);
            }
            temp.Length = (int)l;
            return temp.ToString();
        }

        public static Tuple<string[], string[]> GetCombinedPath(string path)
        {
            var first = path.SplitAsPath().ToArray();
            var second = GetLongPathName(path).SplitAsPath().ToArray();
            Debug.Assert(first.Length == second.Length);
            return new Tuple<string[], string[]>(first, second);
        }

        private static string HandleExplicitWorkingDirectory(ref string path)
        {
            int firstQuote = path.IndexOf('\"');
            if (firstQuote < 0)
                return null;
            int secondQuote = path.IndexOf('\"', firstQuote + 1);
            if (secondQuote < 0)
                return null;
            var ret = path.Substring(0, firstQuote);
            firstQuote++;
            path = path.Substring(firstQuote, secondQuote - firstQuote);
            return ret;
        }

        private static string IsRooted(string path)
        {
            for (int i = 0; i < path.Length; i++)
            {
                if (!char.IsLetter(path[i]))
                    return path[i] == ':' ? path.Substring(0, i + 1) : null;
            }
            return null;
        }

        public static bool IsPathSeparator(char c)
        {
            return c == '\\' || c == '/';
        }

        private static void AddPathToList(List<string> subpaths, string path)
        {
            int iterator = 0;
            if (string.IsNullOrEmpty(path))
                return;
            while (iterator < path.Length && IsPathSeparator(path[iterator]))
                iterator++;
            bool lastWasSeparator = false;
            var accumulator = new StringBuilder();
            for (; iterator < path.Length; iterator++)
            {
                char c = path[iterator];
                if (lastWasSeparator)
                {
                    if (IsPathSeparator(c))
                        continue;
                    if (path.StartsWith(".\\") || path.StartsWith("./"))
                        continue;
                    if (path.StartsWith("..\\") || path.StartsWith("../"))
                    {
                        if (subpaths.Count > 0)
                            subpaths.RemoveAt(subpaths.Count - 1);
                        iterator++;
                        continue;
                    }
                }
                else if (IsPathSeparator(c))
                {
                    subpaths.Add(accumulator.ToString());
                    accumulator = new StringBuilder();
                    lastWasSeparator = true;
                    continue;
                }
                accumulator.Append(c);
                lastWasSeparator = false;
            }
            if (accumulator.Length > 0)
                subpaths.Add(accumulator.ToString());
        }

        private static string CanonicalizePath(string path, string workingDirectory, bool finalSlash)
        {
            bool isFilesystemPath = true;
            string lowerPath = path.ToLower();

            //WARNING: The order of these blocks matters.
            if (lowerPath.StartsWith("\\??\\pipe") || lowerPath.StartsWith("\\??\\unc"))
            {
                path = path.Substring(3);
                isFilesystemPath = false;
            }
            else if (lowerPath.StartsWith("\\??\\") || lowerPath.StartsWith("\\\\?\\"))
                path = path.Substring(4);
            else if (lowerPath.StartsWith("\\\\"))
                return "\\Device\\Mup" + path.Substring(1);
            else if (lowerPath.StartsWith("\\device\\") || lowerPath == "\\device")
                return path;

            if (path.Length == 0)
                return path;

            path = GetLongPathName(path);
            string newWorkingDirectory = HandleExplicitWorkingDirectory(ref path);
            if (!string.IsNullOrEmpty(newWorkingDirectory))
                workingDirectory = newWorkingDirectory;

            var subpaths = new List<string>();
            var workingDirectoryUnit = IsRooted(workingDirectory);
            var pathUnit = IsRooted(path);

            if (isFilesystemPath)
            {
                if (pathUnit != null)
                {
                    subpaths.Add(pathUnit);
                    path = path.Substring(pathUnit.Length);
                }
                else if (path.Length > 0 && IsPathSeparator(path[0]) && workingDirectoryUnit != null)
                    subpaths.Add(workingDirectoryUnit);
                else
                    AddPathToList(subpaths, workingDirectory);
            }

            AddPathToList(subpaths, path);

            var stringBuilder = new StringBuilder();
            if (isFilesystemPath && subpaths.Count > 0)
            {
                stringBuilder.Append(subpaths[0]);
                if (subpaths.Count == 1)
                    finalSlash = true;
            }

            for (int i = isFilesystemPath ? 1 : 0; i < subpaths.Count; i++)
            {
                stringBuilder.Append('\\');
                stringBuilder.Append(subpaths[i]);
            }

            string ret = stringBuilder.ToString();

            if (finalSlash)
                ret = ret + '\\';
            return ret;
        }

        public static string GetResourcePath(string resPath, string lang)
        {
            var filepart = Path.GetFileName(resPath);
            var dir = Path.GetDirectoryName(resPath);

            // first look something like Path\en-US\file.ext.mui
            // then, like Path\en\file.ext.mui
            // then, like Path\file.ext.mui
            // finally, keep the path as it was
            var fullpath = dir + @"\" + lang + @"\" + filepart + ".mui";
            if (!File.Exists(fullpath))
            {
                fullpath = dir + @"\" + lang.Substring(0, 2) + @"\" + filepart + ".mui";
                if (!File.Exists(fullpath))
                {
                    fullpath = resPath + ".mui";
                    if (!File.Exists(fullpath))
                    {
                        fullpath = resPath;
                    }
                }
            }
            return fullpath;
        }

//        public static bool GetFileNameFromHandle(IntPtr hFile, out string filepath)
//{
//  bool success;
//  TCHAR pszFilename[MAX_PATH+1];
//  HANDLE hFileMap;

//  // Get the file size.
//  DWORD dwFileSizeHi = 0;
//  DWORD dwFileSizeLo = GetFileSize(hFile, &dwFileSizeHi); 

//  if( dwFileSizeLo == 0 && dwFileSizeHi == 0 )
//  {
//     _tprintf(TEXT("Cannot map a file with a length of zero.\n"));
//     return FALSE;
//  }

//  // Create a file mapping object.
//  hFileMap = CreateFileMapping(hFile, 
//                    NULL, 
//                    PAGE_READONLY,
//                    0, 
//                    1,
//                    NULL);

//  if (hFileMap) 
//  {
//    // Create a file mapping to get the file name.
//    void* pMem = MapViewOfFile(hFileMap, FILE_MAP_READ, 0, 0, 1);

//    if (pMem) 
//    {
//      if (GetMappedFileName (GetCurrentProcess(), 
//                             pMem, 
//                             pszFilename,
//                             MAX_PATH)) 
//      {

//        // Translate path with device name to drive letters.
//        TCHAR szTemp[BUFSIZE];
//        szTemp[0] = '\0';

//        if (GetLogicalDriveStrings(BUFSIZE-1, szTemp)) 
//        {
//          TCHAR szName[MAX_PATH];
//          TCHAR szDrive[3] = TEXT(" :");
//          BOOL bFound = FALSE;
//          TCHAR* p = szTemp;

//          do 
//          {
//            // Copy the drive letter to the template string
//            *szDrive = *p;

//            // Look up each device name
//            if (QueryDosDevice(szDrive, szName, MAX_PATH))
//            {
//              size_t uNameLen = _tcslen(szName);

//              if (uNameLen < MAX_PATH) 
//              {
//                bFound = _tcsnicmp(pszFilename, szName, uNameLen) == 0
//                         && *(pszFilename + uNameLen) == _T('\\');

//                if (bFound) 
//                {
//                  // Reconstruct pszFilename using szTempFile
//                  // Replace device path with DOS path
//                  TCHAR szTempFile[MAX_PATH];
//                  StringCchPrintf(szTempFile,
//                            MAX_PATH,
//                            TEXT("%s%s"),
//                            szDrive,
//                            pszFilename+uNameLen);
//                  StringCchCopyN(pszFilename, MAX_PATH+1, szTempFile, _tcslen(szTempFile));
//                }
//              }
//            }

//            // Go to the next NULL character.
//            while (*p++);
//          } while (!bFound && *p); // end of string
//        }
//      }
//      bSuccess = TRUE;
//      UnmapViewOfFile(pMem);
//    } 

//    CloseHandle(hFileMap);
//  }
//  _tprintf(TEXT("File name is %s\n"), pszFilename);
//  return(bSuccess);
//}


        private static readonly IEnumerable<PathReplacement> ProcMonPathReplacements = new[]
                                                                                           {
                                                                                               new PathReplacement(
                                                                                                   "\\\\ProgramData",
                                                                                                   "$`\\Program Files$'",
                                                                                                   2)
                                                                                           };

        public static string ResolveProcMonPath(string filepath)
        {
            foreach (var pr in ProcMonPathReplacements)
            {
                var match = Regex.Match(filepath, pr.Original, RegexOptions.IgnoreCase);
                if (match.Success && match.Index == pr.Position)
                {
                    filepath = match.Result(pr.Replacement);
                    break;
                }
            }
            return filepath;
        }


        public static string TryEnsurePathIsAbsolute(string originalPath, CallEvent e, NktSpyMgr spyMgr)
        {
            return originalPath;
            /*
            var ret = originalPath;
            if (ret.ToLower().StartsWith("nonexisting:"))
                return ret;
            var branch = originalPath != null;
            try
            {
                branch = branch && !Path.IsPathRooted(originalPath);
            }
            catch (ArgumentException)
            {
                return ret;
            }
            if (branch)
            {
                string workingDirectory = null;
                var proc = Process.GetProcessById((int) e.Pid);
                try
                {
                    workingDirectory = proc.StartInfo.WorkingDirectory;
                }
                catch (Exception)
                {
                    workingDirectory = null;
                }
                if (string.IsNullOrEmpty(workingDirectory))
                    workingDirectory = ".\\";
                try
                {
                    workingDirectory = Path.GetFullPath(workingDirectory);
                    ret = Path.Combine(workingDirectory, originalPath);
                }
                catch (Exception)
                {
                    return ret;
                }
            }
            return ret;
            */
        }

        public static bool GetFileProperties(string fileSystemPath, ref string originalFileName, ref string product,
                                             ref string company, ref string version, ref string description)
        {
            try
            {
                // filter pipes that don't have any file information and sometimes they hang the application (with Swv driver)
                if (File.Exists(fileSystemPath) && !fileSystemPath.StartsWith(@"\\."))
                {
                    var vi = FileVersionInfo.GetVersionInfo(fileSystemPath);
                    company = vi.CompanyName ?? string.Empty;
                    version = vi.FileMajorPart + "." + vi.FileMinorPart + "." +
                              vi.FileBuildPart + "." + vi.FilePrivatePart;
                    description = vi.FileDescription ?? string.Empty;

                    product = vi.ProductName;
                    originalFileName = vi.OriginalFilename;


                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static string GetTempFilename()
        {
            return GetTempFilename("tempfile");
        }

        public static string GetTempFilename(string filename)
        {
            return GetTempFilename(filename, "tmp");
        }

        public static string GetTempFilename(string filename, string ext)
        {
            var i = 0;
            var basePath = Environment.ExpandEnvironmentVariables(@"%TEMP%\") + filename;
            var path = basePath + "." + ext;
            while (File.Exists(path))
            {
                path = basePath + " (" + ++i + ")." + ext;
            }
            return path;
        }

        private static Dictionary<string, string> _unitMap;

        private static void ListAllUnits()
        {
            uint maxSize = 100;
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            foreach (var letter in alphabet)
            {
                var unit = letter + ":";
                var mem = Marshal.AllocHGlobal((int) maxSize);
                if (mem != IntPtr.Zero)
                {
                    // mem points to memory that needs freeing
                    try
                    {
                        var bufferSmall = false;
                        do
                        {
                            var returnSize = Declarations.QueryDosDevice(unit, mem, maxSize);
                            if (returnSize != 0)
                            {
                                var allDevices = Marshal.PtrToStringAnsi(mem, (int) returnSize);
                                _unitMap[allDevices.Split('\0')[0]] = unit;
                            }
                            else
                            {
                                var e = Marshal.GetLastWin32Error();
                                if (e == 122) //ERROR_INSUFFICIENT_BUFFER
                                {
                                    bufferSmall = true;
                                    maxSize *= 10;

                                    Marshal.FreeHGlobal(mem);
                                    mem = Marshal.AllocHGlobal((int) maxSize);
                                }
                            }
                        } while (bufferSmall);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(mem);
                    }
                }
                else
                {
                    throw new OutOfMemoryException();
                }
            }
        }


        public static void InitializeUnitMap()
        {
            if (_unitMap != null)
                return;
            _unitMap = new Dictionary<string, string>();
            ListAllUnits();
        }

        public static string DevicePathToRegularPath(string path)
        {
            InitializeUnitMap();
            foreach (var unit in _unitMap)
            {
                if (path.ToLower().StartsWith(unit.Key.ToLower()))
                    return unit.Value + path.Substring(unit.Key.Length);
            }
            return path;
        }

        public static bool IsLinkOrShortcut(string shortcutFilename)
        {
            var pathOnly = Path.GetDirectoryName(shortcutFilename);
            var filenameOnly = Path.GetFileName(shortcutFilename);

            Shell32.Shell shell = new Shell32.Shell();
            var folder = shell.NameSpace(pathOnly);
            var folderItem = folder.ParseName(filenameOnly);
            return folderItem != null && folderItem.IsLink;
        }

        public static string GetShortcutTarget(string shortcutFilename)
        {
            var pathOnly = Path.GetDirectoryName(shortcutFilename);
            var filenameOnly = Path.GetFileName(shortcutFilename);

            Shell32.Shell shell = new Shell32.Shell();
            var folder = shell.NameSpace(pathOnly);
            if (folder == null)
                return null;
            var folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                try
                {
                    if (folderItem.IsLink)
                    {
                        var link = (Shell32.ShellLinkObject)folderItem.GetLink;
                        return link.Path;
                    }
                }
                catch (Exception)
                {
                }
                return shortcutFilename;
            }
            return null;  // not found
        }
        /// <summary>
        /// Return true if the Major version matches. minorVersion returns true if the minor version matches
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <param name="minorVersion"></param>
        /// <returns></returns>
        static public bool MatchVersion(string version1, string version2, out bool minorVersion)
        {
            if (version1 == "0.0.0.0" || version2 == "0.0.0.0")
            {
                minorVersion = true;
                return true;
            }

            var majorVersion = minorVersion = version1 == version2;

            var index1 = version1.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
            var index2 = version2.IndexOf(".",
                                          StringComparison.InvariantCultureIgnoreCase);
            if (index1 != -1 && index2 != -1)
            {
                if (version1.Substring(0, index1).Trim() == version2.Substring(0, index2).Trim())
                {
                    majorVersion = true;
                    var endIndex1 = version1.IndexOf(".", ++index1, StringComparison.InvariantCultureIgnoreCase);
                    var endIndex2 = version2.IndexOf(".", ++index2, StringComparison.InvariantCultureIgnoreCase);
                    if (index1 != -1 && index2 != -1)
                    {
                        minorVersion = version1.Substring(index1, endIndex1 - index1).Trim() ==
                                            version2.Substring(index2, endIndex2 - index2).Trim();
                    }
                }
            }
            return majorVersion;
        }

        static public string GetMajorVersion(string version, ref string minorVersion)
        {
            string ret = string.Empty;
            var index = version.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
            if (index != -1)
            {
                ret = version.Substring(0, index).Trim();
                if(minorVersion != null)
                {
                    var endIndex = version.IndexOf(".", ++index, StringComparison.InvariantCultureIgnoreCase);
                    if (endIndex != -1)
                    {
                        minorVersion = version.Substring(index, endIndex - index).Trim();
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// Expands environment variables and, if unqualified, locates the exe in the working directory
        /// or the evironment's path.
        /// </summary>
        /// <param name="exe">The name of the executable file</param>
        /// <returns>The fully-qualified path to the file</returns>
        /// <exception cref="System.IO.FileNotFoundException">Raised when the exe was not found</exception>
        public static string SearchPath(string exe)
        {
            //exe = Environment.ExpandEnvironmentVariables(exe);
            //if (!File.Exists(exe))
            //{
                if (Path.GetDirectoryName(exe) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    {
                        string path = test.Trim();
                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                            return Path.GetFullPath(path);
                    }
                }
            //    throw new FileNotFoundException(new FileNotFoundException().Message, exe);
            //}
            return null;
            //return Path.GetFullPath(exe);
        }

        // Transforms systems paths (e.g. "\\??\\*", "\\Device\\". etc.) into
        // something starting with "SystemPaths\\*". This does NOT perform
        // backslash normalization.
        public static string NormalizeSystemPaths(string path)
        {
            if (path.StartsWith(@"\PIPE\"))
                path = @"SystemPaths\Device" + path;
            else if (path.StartsWith(@"\??\UNC\"))
                path = @"SystemPaths\UNC" + path.Substring(7);
            else if (path.StartsWith(@"\??\"))
                path = @"SystemPaths\Device" + path.Substring(3);
            else if (path.StartsWith(@"\"))
                path = @"SystemPaths" + path;
            return path;
        }

        public static bool IsPathShort(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;
            var splitPath = path.ToLower().SplitAsPath().ToArray();
            var shortPath = GetShortPathName(path).ToLower().SplitAsPath().ToArray();
            var longPath = GetLongPathName(path).ToLower().SplitAsPath().ToArray();
            Debug.Assert(splitPath.Length == shortPath.Length && shortPath.Length == longPath.Length);

            for (var i = 1; i < splitPath.Length; i++)
                if (shortPath[i] != longPath[i] && shortPath[i] == splitPath[i])
                    return true;
            return false;
        }

        private static readonly KeyValuePair<string, SearchOption>[] ShortcutLocations = new[]
                                          {
                                              new KeyValuePair<string, SearchOption>(
                                                  SystemDirectories.CurrentPrograms,
                                                  SearchOption.AllDirectories),
                                              new KeyValuePair<string, SearchOption>(
                                                  SystemDirectories.CurrentDesktop,
                                                  SearchOption.TopDirectoryOnly),
                                              new KeyValuePair<string, SearchOption>(
                                                  SystemDirectories.CommonPrograms,
                                                  SearchOption.AllDirectories),
                                              new KeyValuePair<string, SearchOption>(
                                                  SystemDirectories.CommonDesktop,
                                                  SearchOption.TopDirectoryOnly)
                                          };

        public static List<List<IWshShortcut>> ScanForShortcuts(List<string> epoints, WshShell shell)
        {
            return ScanForShortcuts(epoints, shell, null);
        }

        public static List<List<IWshShortcut>> ScanForShortcuts(List<string> epoints, WshShell shell, Func<bool> cancelled)
        {
            if (cancelled == null)
                cancelled = () => false;
            var ret = new List<List<IWshShortcut>>();

            for (var i = 0; i < epoints.Count; i++)
            {
                ret.Add(new List<IWshShortcut>());
                if (cancelled())
                    return ret;
            }
#if DEBUG && false
            double time1 = 0, time2 = 0, time3 = 0;
#endif
            foreach (var shortcutLocationsPair in ShortcutLocations)
            {
                var shortcutLocation = shortcutLocationsPair.Key;
                var searchOps = shortcutLocationsPair.Value;
                var info = new DirectoryInfo(shortcutLocation);

#if DEBUG && false
                var sw = new Stopwatch();
                sw.Start();
#endif

                var files = info.GetFiles("*.lnk", searchOps);
#if DEBUG && false
                time1 += sw.Elapsed.TotalMilliseconds;
#endif

                foreach (var fileInfo in files)
                {
#if DEBUG && false
                    var prev = sw.Elapsed.TotalMilliseconds;
#endif
                    var link = (IWshShortcut)shell.CreateShortcut(fileInfo.FullName);
                    var linkPath = link.GetRealTarget().ToLower();
#if DEBUG && false
                    time2 += sw.Elapsed.TotalMilliseconds - prev;
                    prev = sw.Elapsed.TotalMilliseconds;
#endif

                    {
                        var i = 0;
                        foreach (var epoint in epoints)
                        {
                            if (linkPath.Contains(epoint.ToLower()))
                                ret[i].Add(link);
                            i++;

                            if (cancelled())
                                return ret;
                        }
                    }

                    if (cancelled())
                        return ret;
#if DEBUG && false
                    time3 += sw.Elapsed.TotalMilliseconds - prev;
#endif
                }

                if (cancelled())
                    return ret;
            }
#if DEBUG && false
            Error.WriteLine("Shortcuts\t" + time1 + "\t" + time2 + "\t" + time3);
#endif
            return ret;
        }

        public static string GetExecutablePathFromCommandLine(string cmd)
        {
            if (cmd.Length == 0)
                return cmd;

            if (cmd[0] == '"')
                return cmd.Split('"')[1];
            return cmd.Split(' ')[0];
        }

        public static string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            if (path[0] == '"')
            {
                if (path.Length == 1)
                    return string.Empty;
                path = path.Substring(1, path.Length - 2);
            }
            return Path.GetFullPath(path);
        }

        private static void GetDirectoryItems(string directory, bool files, bool directories, List<string> list)
        {
            if (files)
                list.AddRange(Directory.GetFiles(directory));
            foreach (var dir in Directory.GetDirectories(directory))
            {
                if (directories)
                    list.Add(dir);
                GetDirectoryItems(dir, files, directories, list);
            }
        }

        public static List<string> GetFiles(string directory)
        {
            var ret = new List<string>();
            GetDirectoryItems(directory, true, false, ret);
            return ret;
        }

        public static List<string> GetDirs(string directory)
        {
            var ret = new List<string>();
            GetDirectoryItems(directory, false, true, ret);
            return ret;
        }

        private static readonly Regex QuestionMarkPathRegex = new Regex(@"^([^:?]+)\?([\\/].*)$");

        public static string NormalizeQuestionMarkPath(string path)
        {
            var match = QuestionMarkPathRegex.Match(path);
            if (!match.Success)
                return path;
            return match.Groups[1].ToString() + ":" + match.Groups[2].ToString();
        }

        private static string[] GetExecutableExtensions()
        {
            return Environment.GetEnvironmentVariable("PATHEXT").Split(';').ToArray();
        }

        private static readonly string[] ExecutableExtensions = GetExecutableExtensions();

        public static bool DirectoryOrFileOrExecutableExists(string path)
        {
            if (path.EndsWith(" ") || path.Contains('"'))
                return false;
            if (File.Exists(path) || Directory.Exists(path))
                return true;
            var idx = path.IndexOf('\\');
            if (idx < 0 || idx >= path.Length)
                return false;
            if (path.Substring(0, idx).Contains('.'))
                return false;
            return ExecutableExtensions.Any(x => File.Exists(path + x));
        }
    }

    class PathComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return FileSystemTools.GetFullPath(x).Equals(FileSystemTools.GetFullPath(y), StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return FileSystemTools.GetFullPath(obj).ToLower().GetHashCode();
        }
    }
}