using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using SpyStudio.Export.ThinApp;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Registry.Infos
{
    [XmlInclude(typeof(SerializableRegKeyInfo))]
    [XmlInclude(typeof(SerializableRegValueInfo))]
    [XmlInclude(typeof(SerializableThinAppRegKeyInfo))]
    [XmlRoot]
    public abstract class SerializableRegInfo
    {
        private const int ReservedBits = 8;
        [Flags]
        public enum FlagValues
        {
            IsNonCaptured = 1 << (ReservedBits + 0),
            IsNull = 1 << (ReservedBits + 1),
            Success = 1 << (ReservedBits + 2),
            WasCalledToCheckExistence = 1 << (ReservedBits + 3),
            IsDataComplete = 1 << (ReservedBits + 4),
            IsDataNull = 1 << (ReservedBits + 5),
            IsWrite = 1 << (ReservedBits + 6),
            Checked = 1 << (ReservedBits + 7),
        }

        [XmlElement]
        public string Name;
        [XmlIgnore]
        public uint Flags;
        [XmlAttribute(AttributeName = "Flags")]
        public string HexFlags
        {
            get
            {
                return Flags.ToString("x");
            }
            set
            {
                Flags = uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }
        }

        [XmlIgnore]
        public ulong BasicKeyHandle
        {
            get
            {
                ulong ret = Flags;
                ret &= (uint) (1 << ReservedBits) - 1;
                ret |= RegistryTools.HkeyMask;
                return ret;
            }
            set
            {
                Flags |= (uint)(value & ~RegistryTools.HkeyMask);
            }
        }

        [XmlIgnore]
        public bool IsNonCaptured
        {
            get { return (Flags & (uint)FlagValues.IsNonCaptured) != 0; }
            set { Flags |= (value ? (uint)FlagValues.IsNonCaptured : 0); }
        }

        [XmlIgnore]
        public bool IsNull
        {
            get { return (Flags & (uint)FlagValues.IsNull) != 0; }
            set { Flags |= (value ? (uint)FlagValues.IsNull : 0); }
        }

        [XmlIgnore]
        public bool Success
        {
            get { return (Flags & (uint)FlagValues.Success) != 0; }
            set { Flags |= (value ? (uint)FlagValues.Success : 0); }
        }

        [XmlIgnore]
        public bool WasCalledToCheckExistence
        {
            get { return (Flags & (uint)FlagValues.WasCalledToCheckExistence) != 0; }
            set { Flags |= (value ? (uint)FlagValues.WasCalledToCheckExistence : 0); }
        }

        [XmlIgnore]
        public bool IsDataComplete
        {
            get { return (Flags & (uint)FlagValues.IsDataComplete) != 0; }
            set { Flags |= (value ? (uint)FlagValues.IsDataComplete : 0); }
        }

        [XmlIgnore]
        public bool IsDataNull
        {
            get { return (Flags & (uint)FlagValues.IsDataNull) != 0; }
            set { Flags |= (value ? (uint)FlagValues.IsDataNull : 0); }
        }

        [XmlIgnore]
        public bool IsWrite
        {
            get { return (Flags & (uint)FlagValues.IsWrite) != 0; }
            set { Flags |= (value ? (uint)FlagValues.IsWrite : 0); }
        }

        [XmlIgnore]
        public bool Checked
        {
            get { return (Flags & (uint)FlagValues.Checked) != 0; }
            set { Flags |= (value ? (uint)FlagValues.Checked : 0); }
        }

        protected abstract RegInfo Create();
        public static SerializableRegInfo Serialize(RegInfo info)
        {
            var ret = info.CreateSerializable();
            ret.Name = info.Name;
            ret.IsNonCaptured = info.IsNonCaptured;
            ret.IsNull = info.IsNull;
            ret.Success = info.Success;
            return ret;
        }
        public virtual RegInfo Deserialize()
        {
            var ret = Create();
            ret.Name = Name;
            ret.IsNonCaptured = IsNonCaptured;
            ret.IsNull = IsNull;
            ret.Access = RegistryKeyAccess.Write;
            ret.Success = Success;
            return ret;
        }
    }

    public class SerializableRegKeyInfo : SerializableRegInfo
    {
        [XmlElement]
        public string Path;
        [XmlArray]
        public List<string> OriginalKeyPaths = new List<string>();
        [XmlArray]
        public List<SerializableRegValueInfo> ValuesByName = new List<SerializableRegValueInfo>();
        [XmlAttribute]
        public int OriginalPath;
        protected override RegInfo Create()
        {
            return new RegKeyInfo();
        }
        public static SerializableRegInfo Serialize(RegKeyInfo info)
        {
            var ret = (SerializableRegKeyInfo)Serialize((RegInfo)info);
            ret.BasicKeyHandle = info.BasicKeyHandle;
            ret.Path = info.Path;
            ret.OriginalKeyPaths.AddRange(info.OriginalKeyPaths.Keys);
            ret.ValuesByName = info.ValuesByName.Values.Select(x => (SerializableRegValueInfo)x.Serialize()).ToList();
            var index = -1;
            for (var i = 0; i < ret.OriginalKeyPaths.Count; i++)
            {
                var originalKeyPath = ret.OriginalKeyPaths[i];
                if (info.OriginalPath == originalKeyPath)
                {
                    index = i;
                    break;
                }
            }
            Debug.Assert(index >= 0 || info.OriginalPath == null);
            ret.OriginalPath = index;
            ret.ReduceOriginalKeyPaths();
            return ret;
        }

        private void ReduceOriginalKeyPaths()
        {
            if (OriginalKeyPaths.Count <= 1)
                return;
            var unlinkedPaths = new Dictionary<string, int>();
            var linkedPaths = new Dictionary<string, int>();
            for (int i = 0; i < OriginalKeyPaths.Count; i++)
                SingleLoopRound(i, unlinkedPaths, linkedPaths, OriginalKeyPaths[i], true);

            if (unlinkedPaths.Count == 0)
            {
                ConsolidatePaths(linkedPaths);
                return;
            }

            foreach (var linkedPath in linkedPaths)
            {
                var pathA = linkedPath.Key.Replace("hkey_classes_root", @"hkey_local_machine\software\classes");
                var pathB = linkedPath.Key.Replace("hkey_classes_root", @"hkey_current_user\software\classes");
                bool aExists = unlinkedPaths.ContainsKey(pathA);
                bool bExists = unlinkedPaths.ContainsKey(pathB);
                if (aExists || bExists)
                    continue;
                unlinkedPaths[linkedPath.Key] = linkedPath.Value;
            }

            ConsolidatePaths(unlinkedPaths);
        }

        private void SingleLoopRound(int structurePosition, Dictionary<string, int> dict, Dictionary<string, int> linkedPaths, string testedPath, bool write)
        {
            string path = testedPath.ToLower();
            if (path.StartsWith("hkey_classes_root"))
                dict = linkedPaths;
            int index;
            if (dict.TryGetValue(path, out index))
            {
                if (OriginalPath == structurePosition)
                    OriginalPath = index;
            }
            else if (write)
            {
                dict[path] = structurePosition;
            }
        }

        private void ConsolidatePaths(Dictionary<string, int> dict)
        {
            var oldPaths = OriginalKeyPaths;
            OriginalKeyPaths = new List<string>();
            foreach (var path in dict)
            {
                if (OriginalPath == path.Value)
                    OriginalPath = OriginalKeyPaths.Count;
                OriginalKeyPaths.Add(oldPaths[path.Value]);
            }
        }

        public override RegInfo Deserialize()
        {
            var ret = (RegKeyInfo)base.Deserialize();
            ret.Path = Path;
            ret.BasicKeyHandle = BasicKeyHandle;

            foreach (var originalKeyPath in OriginalKeyPaths)
                ret.OriginalKeyPaths[originalKeyPath] = true;

            foreach (var value in ValuesByName)
            {
                var second = value.Deserialize(ret);
                var first = second.Name;
                ret.ValuesByName[first] = second;
            }

            if (OriginalPath >= 0)
                ret.OriginalPath = OriginalKeyPaths[OriginalPath];

            return ret;
        }
    }

    [Serializable]
    public class SerializableRegValueInfo : SerializableRegInfo
    {
        [XmlAttribute]
        public int ValueType;
        [XmlElement]
        public string Data;
        protected override RegInfo Create()
        {
            return new RegValueInfo();
        }
        public static SerializableRegInfo Serialize(RegValueInfo info)
        {
            var ret = (SerializableRegValueInfo)Serialize((RegInfo)info);
            ret.ValueType = (int)info.ValueType;
            ret.Data = info.NormalizedData;
            ret.WasCalledToCheckExistence = info.WasCalledToCheckExistence;
            ret.IsDataComplete = info.IsDataComplete;
            ret.IsDataNull = info.IsDataNull;
            ret.IsWrite = info.IsWrite;
            return ret;
        }
        public RegValueInfo Deserialize(RegKeyInfo parent)
        {
            var ret = (RegValueInfo)Deserialize();
            ret.ValueType = (RegistryValueKind)ValueType;
            ret.NormalizedData = Data;
            ret.WasCalledToCheckExistence = WasCalledToCheckExistence;
            ret.IsDataComplete = IsDataComplete;
            ret.IsDataNull = IsDataNull;
            ret.IsWrite = IsWrite;

            ret.NormalizedPath = parent.NormalizedPath;
            ret.SubKey = parent.SubKey;
            ret.BasicKeyHandle = parent.BasicKeyHandle;
            ret.Path = parent.Path;
            ret.OriginalPath = parent.OriginalPath;
            ret.TraceID = parent.TraceID;
            return ret;
        }
    }

    [Serializable]
    public class SerializableThinAppRegKeyInfo : SerializableRegKeyInfo
    {
        [XmlElement]
        public ThinAppIsolationOption Isolation;
        protected override RegInfo Create()
        {
            return new ThinAppRegKeyInfo();
        }
        public static SerializableRegInfo Serialize(ThinAppRegKeyInfo info)
        {
            var ret = (SerializableThinAppRegKeyInfo)Serialize((RegInfo)info);
            ret.Isolation = info.Isolation;
            return ret;
        }
        public override RegInfo Deserialize()
        {
            var ret = (ThinAppRegKeyInfo)base.Deserialize();
            ret.Isolation = Isolation;
            return ret;
        }
    }
}
