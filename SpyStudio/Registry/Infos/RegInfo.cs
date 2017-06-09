using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Win32;
using SpyStudio.Dialogs;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Export.ThinApp;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Registry.Infos
{
    [Serializable]
    [XmlInclude(typeof(RegKeyInfo))]
    [XmlInclude(typeof(RegValueInfo))]
    [XmlInclude(typeof(ThinAppRegKeyInfo))]
    public abstract class RegInfo : IInfo
    {
        [XmlElement]
        protected string _name;
        [XmlElement]
        private string _normalizedPath;
        [XmlElement]
        private string _subKey;
        [XmlElement]
        private ulong _basicKeyHandle;
        [XmlElement]
        protected string _path;
        [XmlElement]
        private HashSet<CallEventId> _callEventIds;
        [XmlIgnore]
        private HashSet<DeviareTraceCompareItem> _compareItems;

        [XmlElement]
        public virtual bool IsNonCaptured { get; set; }

        public virtual string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [XmlElement]
        public string NormalizedName;
        [XmlElement]
        //Note: This is always null when the RegInfo instance is part of a RegistryTreeNode.
        public string OriginalPath;
        [XmlElement]
        //When opening a registry key from a RegInfo, the NormalizedName/OriginalPath may
        //fail due to Wow6432Node not being in the path. In such cases, AlternatePath should
        //be used instead.
        public string AlternatePath;
        //Note: This is always a path to a key. If this is RegValueInfo,
        //      Path is the path to the key that contains that value.
        public virtual string Path
        {
            get { return _path; }
            set
            {
                NormalizedPath = null;
                SubKey = null;
                _path = RegistryTools.FixBackSlashesIn(value);
            }
        }
        public string NormalizedPath
        {
            get
            {
                if (string.IsNullOrEmpty(_normalizedPath))
                    _normalizedPath = GetNormalizedPath();

                return _normalizedPath;
            }
            set { _normalizedPath = value; }
        }
        public string SubKey
        {
            get
            {
                if (string.IsNullOrEmpty(_subKey))
                    _subKey = GetSubKey();

                return _subKey;
            }
            set { _subKey = value; }
        }
        public UInt64 BasicKeyHandle
        {
            get
            {
                if (_basicKeyHandle == 0)
                    _basicKeyHandle = GetBasicKeyHandle();

                return _basicKeyHandle;
            }
            set { _basicKeyHandle = value; }
        }

        protected bool IsBasicKey { get { return !Path.Contains("\\"); } }

        [XmlElement]
        public uint TraceID { get; set; }
        public HashSet<CallEventId> CallEventIds
        {
            get { return _callEventIds ?? (_callEventIds = new HashSet<CallEventId>()); }
            set { _callEventIds = value; }
        }

        public HashSet<DeviareTraceCompareItem> CompareItems
        {
            get { return _compareItems ?? (_compareItems = new HashSet<DeviareTraceCompareItem>()); }
            set { _compareItems = value; }
        }

        [XmlElement]
        public bool IsNull { get; set; }

        [XmlElement]
        public RegistryKeyAccess Access { get; set; }
        [XmlElement]
        public bool Success { get; set; }

        public abstract IEnumerable<CallEventId> GetAllCallEventIds();

        public abstract SerializableRegInfo CreateSerializable();
        public virtual SerializableRegInfo Serialize()
        {
            return SerializableRegInfo.Serialize(this);
        }

        #region Utils

        protected string GetNormalizedName()
        {
            return Name.ToLower();
        }

        private string GetSubKey()
        {
            return Path.Contains('\\') ? Path.Substring(Path.IndexOf('\\')) : "";
        }

        protected static string ExtractPathFrom(CallEvent aCallEvent)
        {
            return RegOpenKeyEvent.GetKey(aCallEvent);
        }

        protected static string ExtractSubKeyFrom(IEnumerable<string> splitPath)
        {
            return string.Join("\\", splitPath.Skip(1).ToArray());
        }

        public string GetResultString()
        {
            return Success ? "SUCCESS" : "ERROR";
        }

        public static string NormalizePath(string path)
        {
            return path.ToLower();
        }

        public string GetNormalizedPath()
        {
            return NormalizePath(Path);
        }

        public UInt64 GetBasicKeyHandle()
        {
            if (!IsBasicKey)
                return 0;

            UInt64 basicKey = 0;

            foreach (var keyPair in RegistryTools.StandardSwvKeys)
            {
                if (Path.StartsWith(keyPair.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    basicKey = keyPair.Key;
                    break;
                }
            }
            if (basicKey == 0)
            {
                foreach (var keyPair in RegistryTools.StandardKeys)
                {
                    if (Path.StartsWith(keyPair.Value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        basicKey = keyPair.Key;
                        break;
                    }
                }
            }
            return basicKey;
        }

        #endregion
    }
}