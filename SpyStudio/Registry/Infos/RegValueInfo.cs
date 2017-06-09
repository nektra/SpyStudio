using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using SpyStudio.Dialogs.Compare;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Registry.Infos
{
    [Serializable]
    public class RegValueInfo : RegInfo
    {
        public override string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NormalizedName = GetNormalizedName();
            }
        }

        [XmlElement]
        public RegistryValueKind ValueType { get; set; }
        [XmlElement]
        public string Data { get; set; }
        [XmlElement]
        public bool WasCalledToCheckExistence { get; set; }
        [XmlElement]
        public bool IsDataComplete { get; set; }
        [XmlElement]
        public bool IsDataNull { get; set; }
        [XmlElement]
        public bool IsWrite { get; set; }

        #region Initialization

        public RegValueInfo()
        {
            IsNull = true;
            CallEventIds = new HashSet<CallEventId>();
            CompareItems = new HashSet<DeviareTraceCompareItem>();
        }

        public static RegValueInfo From(RegistryKey key, string valueName)
        {
            try
            {
                string valueData = string.Empty;
                var valueKind = key.GetValueKind(valueName);
                var data = key.GetValue(valueName);
                switch (valueKind)
                {
                    case RegistryValueKind.Binary:
                        {
                            var valueDataBinary = data as byte[];
                            Debug.Assert(valueDataBinary != null);

                            for (int i = 0; i < valueDataBinary.Length; i++)
                            {
                                if (i != 0)
                                    valueData += " ";
                                valueData += Convert.ToByte(valueDataBinary[i]).ToString("X2");
                            }
                        }
                        break;

                    case RegistryValueKind.MultiString:
                        {
                            var valueDataStringArray = data as string[];
                            Debug.Assert(valueDataStringArray != null);
                            int i = 0;
                            foreach (var str in valueDataStringArray)
                            {
                                if (i++ != 0)
                                    valueData += Convert.ToChar(0);
                                valueData += str;
                            }
                            valueData += Convert.ToChar(0);
                            valueData += Convert.ToChar(0);
                        }
                        break;
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        var valueDataString = data as string;
                        Debug.Assert(valueDataString != null);
                        valueData = valueDataString;
                        break;

                    case RegistryValueKind.DWord:
                        {
                            var valueDataDword = (uint) (int) data;
                            valueData = RegistryTools.GetRegValueRepresentation(valueDataDword);
                        }
                        break;

                    case RegistryValueKind.QWord:
                        {
                            var valueDataQword = (UInt64) (Int64) data;
                            valueData = RegistryTools.GetRegValueRepresentation(valueDataQword);
                        }
                        break;
                }
                var keyName = RegistryTools.FixBackSlashesIn(key.Name);
                var valueInfo = new RegValueInfo
                                {
                                    Name = valueName,
                                    ValueType = valueKind,
                                    Data = valueData,
                                    WasCalledToCheckExistence = true,
                                    IsDataComplete = true,
                                    IsDataNull = false,
                                    IsWrite = false,
                                    IsNonCaptured = true,
                                    OriginalPath = keyName,
                                    Path = keyName,
                                    Success = true,
                                };
                return valueInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static RegValueInfo From(CallEvent aCallEvent)
        {
            var valueInfo = new RegValueInfo();

            valueInfo.InitializeUsing(aCallEvent);

            return valueInfo;
        }

        public static RegValueInfo ForTraceID(uint aTraceID)
        {
            var valueInfo = new RegValueInfo();

            valueInfo.TraceID = aTraceID;

            return valueInfo;
        }

        private void InitializeUsing(CallEvent aCallEvent)
        {
            IsNull = false;

            TraceID = aCallEvent.TraceId;
            Access = aCallEvent.Type.ToRegistryKeyAccess();
            ValueType = RegQueryValueEvent.GetValueType(aCallEvent);
            IsWrite = RegQueryValueEvent.IsWrite(aCallEvent);
            IsDataNull = RegQueryValueEvent.IsDataNull(aCallEvent);
            IsDataComplete = RegQueryValueEvent.IsDataComplete(aCallEvent);
            Success = aCallEvent.Success;
            CallEventIds.Add(new CallEventId(aCallEvent));

            try
            {
                OriginalPath = Path = RegQueryValueEvent.GetParentKey(aCallEvent);
                Name = RegQueryValueEvent.GetName(aCallEvent);
                Data = RegQueryValueEvent.GetData(aCallEvent);
            }
            catch (IndexOutOfRangeException)
            {
                
            }
        }

        #endregion

        public override IEnumerable<CallEventId> GetAllCallEventIds()
        {
            return CallEventIds;
        }

        public void MergeWith(RegValueInfo aValueInfo)
        {
            Debug.Assert(NormalizedPath == aValueInfo.NormalizedPath, "SpyStudio tried to merge two RegValueInfo with different paths!");

            IsNull = false;

            Access |= aValueInfo.Access;
            ValueType = aValueInfo.ValueType;
            IsWrite |= aValueInfo.IsWrite;
            Success |= aValueInfo.Success;
            if (aValueInfo.Success && !aValueInfo.IsDataNull)
            {
                Data = aValueInfo.Data;
                IsDataNull = aValueInfo.IsDataNull;
                IsDataComplete = aValueInfo.IsDataComplete;
            }
            CallEventIds.AddRange(aValueInfo.CallEventIds);
            CompareItems.AddRange(aValueInfo.CompareItems);
        }

        public static IEnumerable<RegValueInfo> FromMultipleQuery(CallEvent aCallEvent)
        {
            throw new NotImplementedException();
        }

        public string NormalizedData
        {
            get
            {
                if (!Data.Any(Char.IsControl))
                    return "0" + Data;
                return "1" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Data));
            }
            set
            {
                if (value.Length < 1)
                {
                    Data = string.Empty;
                    return;
                }
                if (value[0] == '0')
                {
                    Data = value.Substring(1);
                    return;
                }
                Data = Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(1)));
            }
        }

        public override SerializableRegInfo CreateSerializable()
        {
            return new SerializableRegValueInfo();
        }

        public override SerializableRegInfo Serialize()
        {
            return SerializableRegValueInfo.Serialize(this);
        }

        public void NormalizeDataForExport(PathNormalizer normalizer)
        {
            switch (ValueType)
            {
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    Data = RegistryTools.NormalizePathsInValue(Data, OriginalPath, Name, normalizer);
                    break;
            }
        }
    }
}
