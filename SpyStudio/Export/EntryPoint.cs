using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using IWshRuntimeLibrary;
using System.Xml.Serialization;

namespace SpyStudio.Export
{
    public class EntryPoint
    {
        public bool NameIsSpecified
        {
            get { return !(Location == Name || Location.EndsWith("\\" + Name)); }
        }
        [XmlElement]
        public string Name;
        [XmlElement]
        public string Location;
        [XmlElement]
        public string ProductName;
        public bool ProtocolsSpecified
        {
            get { return Protocols != null && Protocols.Count > 0; }
        }
        [XmlArray]
        public List<string> Protocols = new List<string>();
        public bool FileTypesSpecified
        {
            get { return FileTypes != null && FileTypes.Count > 0; }
        }
        [XmlArray]
        public List<string> FileTypes = new List<string>();
        [XmlElement]
        public string FileTypesString;
        [XmlElement]
        public string ProtocolsString;
        [XmlAttribute]
        public int Suitability = -1;
        [XmlElement]
        public string InferredName;
        [XmlAttribute]
        [DefaultValue(false)]
        //Used exclusively for templates.
        public bool Checked = false;
        [XmlIgnore]
        public List<IWshShortcut> Shortcuts = new List<IWshShortcut>();
        [XmlIgnore]
        public string FileSystemLocation;

        public EntryPoint(){}

        public EntryPoint(EntryPoint point)
        {
            Name = point.Name;
            Location = point.Location;
            ProductName = point.ProductName;
            Protocols = point.Protocols;
            FileTypes = point.FileTypes;
            FileTypesString = point.FileTypesString;
            ProtocolsString = point.ProtocolsString;
            Suitability = point.Suitability;
            Checked = point.Checked;
            FileSystemLocation = point.FileSystemLocation;
        }

        public bool LikelyMainEntryPoint
        {
            get { return FileTypes.Count > 0; }
        }

        public string GetFileTypesString()
        {
            return FileTypesString ?? (FileTypesString = MakeFileTypesString());
        }

        private string MakeFileTypesString()
        {
            if (FileTypes.Count == 0)
                return string.Empty;
            var ret = new StringBuilder();
            foreach (var fileType in FileTypes)
                ret.Append(fileType);
            return ret.ToString();
        }

        public string GetProtocolsString()
        {
            return ProtocolsString ?? (ProtocolsString = MakeProtocolsString());
        }

        private string MakeProtocolsString()
        {
            if (Protocols.Count == 0)
                return string.Empty;
            var ret = new StringBuilder();
            for (var i = 0; ; )
            {
                ret.Append(Protocols[i]);
                if (++i == Protocols.Count)
                    break;
                ret.Append(';');
            }
            return ret.ToString();
        }

        public int GetSuitability()
        {
            if (Suitability >= 0)
                return Suitability;
            return Suitability = MakeSuitability();
        }

        private int MakeSuitability()
        {
            return FileTypes.Count*3 + Shortcuts.Count;
        }

        public string GetInferredName()
        {
            return InferredName ?? (InferredName = MakeInferredName());
        }

        private string MakeInferredName()
        {
            var ret = Name;
            if (!string.IsNullOrEmpty(ProductName))
                ret = ProductName + ".exe";
            if (Shortcuts.Count == 0)
                return ret;
            var removeDirAndExtRegex = new Regex(@"(?:.*\\)?([^\\]+)\.lnk", RegexOptions.IgnoreCase);
            string shortcutName = null;
            foreach (var shortcut in Shortcuts)
            {
                var match = removeDirAndExtRegex.Match(shortcut.FullName);
                if (!match.Success)
                    return ret;
                var newName = match.Groups[1].ToString();
                if (shortcutName == null)
                    shortcutName = newName;
                else if (shortcutName != newName)
                    return ret;
            }
            return shortcutName + ".exe";
        }
    }
}