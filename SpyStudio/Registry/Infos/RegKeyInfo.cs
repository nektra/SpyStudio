using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Aga.Controls.Tree;
using Microsoft.Win32;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Registry.Controls;
using SpyStudio.Tools;
using SpyStudio.Extensions;
using System.Linq;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Registry.Infos
{
    [Serializable]
    public class RegKeyInfo : RegInfo
    {
        [XmlElement]
        private Dictionary<string, bool> _originalKeys;
        [XmlElement]
        private Dictionary<string, RegValueInfo> _valuesByName;
        
        public override string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NormalizedName = GetNormalizedName();
            }
        }

        public Dictionary<string, bool> OriginalKeyPaths
        {
            get { return _originalKeys ?? (_originalKeys = new Dictionary<string, bool>()); }
            set { _originalKeys = value; }
        }

        public Dictionary<string, RegValueInfo> ValuesByName
        {
            get { return _valuesByName ?? (_valuesByName = new Dictionary<string, RegValueInfo>()); }
            set { _valuesByName = value; }
        }

        #region Instantiation

        public static RegKeyInfo CopyOf(RegKeyInfo anotherKeyInfo)
        {
            var keyInfo = new RegKeyInfo();

            keyInfo.InitializeAsCopyOf(anotherKeyInfo);

            return keyInfo;
        }

        public static RegKeyInfo ForTraceID(uint aTraceID)
        {
            var keyInfo = new RegKeyInfo();

            keyInfo.TraceID = aTraceID;

            return keyInfo;
        }

        public static RegKeyInfo For(RegistryTreeNode aRegistryNode)
        {
            var keyInfo = new RegKeyInfo();

            keyInfo.InitializeUsing(aRegistryNode);

            return keyInfo;
        }

        public static RegKeyInfo From(RegistryKey key)
        {
            var keyInfo = new RegKeyInfo
                              {
                                  IsNull = false,
                                  Success = true,
                              };
            var keyName = RegistryTools.FixBackSlashesIn(key.Name);
            keyInfo.OriginalPath = keyInfo.Path = keyName;
            keyInfo.Name = keyInfo.Path.Substring(keyInfo.Path.LastIndexOf('\\') + 1);

            return keyInfo;
        }
        public static RegKeyInfo From(CallEvent aCallEvent)
        {
            var keyInfo = new RegKeyInfo();

            keyInfo.InitializeUsing(aCallEvent);
            
            return keyInfo;
        }

        public static RegKeyInfo ParentOf(RegValueInfo aRegValueInfo)
        {
            var regKeyInfo = new RegKeyInfo();

            regKeyInfo.InitializeAsParentOf(aRegValueInfo);

            return regKeyInfo;
        }

        public RegKeyInfo()
        {
            IsNull = true;
        }
        
        private void InitializeUsing(CallEvent aCallEvent)
        {
            IsNull = false;

            TraceID = aCallEvent.TraceId;
            Success = aCallEvent.Success;
            Access = aCallEvent.Type.ToRegistryKeyAccess();
            OriginalPath = Path = ExtractPathFrom(aCallEvent);
            var param = aCallEvent.GetParamByName("ReturnedKey");
            if (param != null)
                AlternatePath = param.Value;
            Name = Path.Substring(Path.LastIndexOf('\\') + 1);
            CallEventIds.Add(new CallEventId(aCallEvent));
        }

        private void InitializeAsParentOf(RegValueInfo aValueInfo)
        {
            IsNull = false;
            
            TraceID = aValueInfo.TraceID;
            OriginalPath = Path = aValueInfo.Path;
            IsNonCaptured = aValueInfo.IsNonCaptured;
            Name = Path.Substring(aValueInfo.Path.LastIndexOf('\\') + 1);
            Access = Access;
            Success = aValueInfo.Success;
        }

        private void InitializeUsing(Node aRegistryNode)
        {
            Name = aRegistryNode.Text;
        }
        
        protected void InitializeAsCopyOf(RegKeyInfo aKeyInfo)
        {
            IsNull = false;

            Name = aKeyInfo.Name;
            Path = aKeyInfo.Path;
            NormalizedPath = aKeyInfo.NormalizedPath;
            SubKey = aKeyInfo.SubKey;
            BasicKeyHandle = aKeyInfo.BasicKeyHandle;
            OriginalKeyPaths = aKeyInfo.OriginalKeyPaths;
            OriginalPath = aKeyInfo.OriginalPath;

            Access = aKeyInfo.Access;
            Success = aKeyInfo.Success;

            ValuesByName = aKeyInfo.ValuesByName;
        }

        #endregion

        public override IEnumerable<CallEventId> GetAllCallEventIds()
        {
            var valuesCallEventIds = ValuesByName.Values.SelectMany(v => v.CallEventIds);

            return valuesCallEventIds.Concat(CallEventIds);
        }

        public IEnumerable<DeviareTraceCompareItem> GetAllCompareItems()
        {
            var valuesCompareItems = ValuesByName.Values.SelectMany(v => v.CompareItems);

            return valuesCompareItems.Concat(CompareItems);
        }
        
        public void Add(RegValueInfo aValueInfo)
        {
            Debug.Assert(aValueInfo.NormalizedPath == NormalizedPath,
                         "Spy Studio tried to add a registry value to a registry key with another path.");

            RegValueInfo valueInfo;
            if (ValuesByName.TryGetValue(aValueInfo.NormalizedName, out valueInfo))
                valueInfo.MergeWith(aValueInfo);
            else
                ValuesByName.Add(aValueInfo.NormalizedName, aValueInfo);
        }

        public void MergeWith(RegKeyInfo aKeyInfo)
        {
            IsNonCaptured = IsNull
                                ? aKeyInfo.IsNonCaptured
                                : IsNonCaptured && aKeyInfo.IsNonCaptured;

            IsNull = false;

            Success |= aKeyInfo.Success;
            Access |= aKeyInfo.Access;

            foreach (var dictEntry in aKeyInfo.ValuesByName)
                ValuesByName[dictEntry.Key] = dictEntry.Value;

            CallEventIds.AddRange(aKeyInfo.CallEventIds);
            CompareItems.AddRange(aKeyInfo.CompareItems);

            if (aKeyInfo.OriginalPath == null)
                return;

            bool success;
            if (!OriginalKeyPaths.TryGetValue(aKeyInfo.OriginalPath, out success) || !success)
                OriginalKeyPaths[aKeyInfo.OriginalPath] = aKeyInfo.Success;

            if (OriginalPath == null)
                OriginalPath = aKeyInfo.OriginalPath;
        }

        public void MergeAsAncestorWith(RegKeyInfo aKeyInfo)
        {
            IsNull = false;

            Success |= aKeyInfo.Success;
            Access |= aKeyInfo.Access;

            if (!aKeyInfo.OriginalKeyPaths.Any())
                return;

            var childOriginalPath = aKeyInfo.OriginalKeyPaths.First().Key;

            OriginalPath = childOriginalPath.Substring(0, childOriginalPath.Length - aKeyInfo.Name.Length - 1);
            OriginalKeyPaths[OriginalPath] = aKeyInfo.OriginalKeyPaths[childOriginalPath];
        }

        public override SerializableRegInfo CreateSerializable()
        {
            return new SerializableRegKeyInfo();
        }
        public override SerializableRegInfo Serialize()
        {
            return SerializableRegKeyInfo.Serialize(this);
        }
    }

}
