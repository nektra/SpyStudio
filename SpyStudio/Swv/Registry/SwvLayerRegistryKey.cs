using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Swv.Registry
{
    public abstract class SwvLayerRegistryKey
    {
        #region Properties

        public string Name { get; protected set; }
        public string NameInLayer { get; protected set; }

        protected abstract SwvLayerRegistryBaseKey BaseKey { get; set; }

        protected UIntPtr Handle { get; set; }

        #endregion

        #region Accessors

        public IEnumerable<SwvLayerRegistryKey> GetAllSubKeysRecursively()
        {
            var subKeys = new List<SwvLayerRegistryKey>();

            subKeys.Add(this);

            foreach (var subKey in GetAllSubKeys())
                subKeys.AddRange(subKey.GetAllSubKeysRecursively());

            return subKeys;
        }

        public IEnumerable<SwvLayerRegistryKey> GetAllSubKeys()
        {
            var subKeys = new List<SwvLayerRegistryKey>();

            if (!TryOpen())
                return subKeys;

            uint subKeyNameLength = 256;
            var subKeyName = new StringBuilder((int)subKeyNameLength);

            uint index = 0;
            uint notUsed;
            Declarations.FILETIME notUsed2;
            while (0 ==
                   Declarations.RegEnumKeyEx(Handle, index, subKeyName, out subKeyNameLength, 0, null, out notUsed,
                                             out notUsed2))
            {
                var parentSubKey = NameInLayer.Substring(BaseKey.NameInLayer.Length);

                subKeys.Add(SwvLayerRegistrySubKey.Under(BaseKey,
                                                         (parentSubKey == "" ? "" : parentSubKey + @"\") +
                                                         subKeyName));

                subKeyNameLength = 256;
                index++;
            }

            Close();

            return subKeys;
        }

        public IEnumerable<RegValueInfo> GetAllValues()
        {
            var values = new List<RegValueInfo>();

            if (!TryOpen())
                return values;

            uint index = 0;

            uint valueNameLength = 16383;
            var valueName = new StringBuilder((int)valueNameLength);
            uint valueType;

            uint valueDataLength = 0;

            // first call to get data length
            while (0 == Declarations.RegEnumValue(Handle, index, valueName, ref valueNameLength, UIntPtr.Zero, out valueType, (byte[]) null, ref valueDataLength))
            {
                // Call again to get the proper data
                valueNameLength = 16383;
                uint error;
                object valueData;
                // Use a StringBuilder if data is string
                if (valueType == (int)RegistryValueKind.ExpandString || valueType == (int)RegistryValueKind.String || valueType == (int)RegistryValueKind.MultiString)
                {
                    valueData = new StringBuilder((int) valueDataLength);
                    error = Declarations.RegEnumValue(Handle, index, valueName, ref valueNameLength, UIntPtr.Zero,
                                                          out valueType, (StringBuilder)valueData, ref valueDataLength);
                }
                else
                {
                    valueData = new byte[valueDataLength];
                    error = Declarations.RegEnumValue(Handle, index, valueName, ref valueNameLength, UIntPtr.Zero,
                                                          out valueType, (byte[])valueData, ref valueDataLength);
                }

                Debug.Assert(error == 0, "Error getting registry value.");

                var valueInfo = new RegValueInfo
                {
                    Name = valueNameLength == 0 ? "" : valueName.ToString(0, (int)valueNameLength),
                    Path = Name,
                    ValueType = (RegistryValueKind) valueType,
                    Data = GetValueAsString(valueData, (RegistryValueKind)valueType),
                    IsDataComplete = true,
                    Success = true
                };

                values.Add(valueInfo);

                valueNameLength = 16383;
                index++;
            }

            Close();

            return values;
        }

        public bool Contains(string aSubKey)
        {
            if (!TryOpen())
                return false;
            UIntPtr keyPtr = UIntPtr.Zero;
            try
            {
                keyPtr = OpenKey(Handle, aSubKey);
            }
            catch (RegKeyNotFoundException)
            {
            }
            Close();

            if (keyPtr == UIntPtr.Zero)
                return false;

            Declarations.RegCloseKey(keyPtr);
            return true;
        }

        public string GetValue(string aValueName, RegistryValueKind aValueKind)
        {
            Open();
            uint valueDataLength = 0;
            var intPtr = unchecked((IntPtr)(long)(ulong)Handle);
            var valueType = (uint)aValueKind;

            var error = Declarations.RegQueryValueEx(intPtr, aValueName, 0, ref valueType, null, ref valueDataLength);

            Debug.Assert(error == 0, "Error getting a layer registry value data size.");

            var valueData = new StringBuilder((int)valueDataLength);
            error = Declarations.RegQueryValueEx(intPtr, aValueName, 0, ref valueType, valueData, ref valueDataLength);

            Debug.Assert(error == 0, "Error getting a layer registry value data.");

            Close();

            return valueData.ToString();
        }

        #endregion

        #region Setters

        public void SetValue(string aValueName, RegistryValueKind aValueType, string aValue)
        {
            if (!TryOpen())
                return;

            var error = Declarations.RegSetValueEx(Handle, aValueName, 0, aValueType, aValue, aValue.Length);

            Debug.Assert(error == 0, "Error setting a registry value.");

            Close();
        }

        #endregion

        #region Abstract members

        public abstract SwvLayerRegistryKey CreateSubKey(string aSubKey);

        #endregion

        #region Low Level Commands

        protected void Open()
        {
            Handle = OpenKey(RegistryTools.HkeyLocalMachinePtr, NameInLayer.Substring(RegistryTools.HkeyLocalMachineString.Length + 1));
        }

        protected bool TryOpen()
        {
            try
            {
                Open();
            }
            catch (RegKeyNotFoundException)
            {
                Handle = UIntPtr.Zero;
                return false;
            }
            return true;
        }

        protected void Close()
        {
            CloseKey(Handle);
        }

        protected UIntPtr OpenKey(UIntPtr aRootKeyPtr, string aSubKey)
        {
            UIntPtr keyHandle;
            var errorCode = Declarations.RegOpenKeyEx(aRootKeyPtr, aSubKey, 0, 0xF003F, out keyHandle);

            if (errorCode != 0)
                throw new RegKeyNotFoundException();

            return keyHandle;
        }

        protected void CloseKey(UIntPtr aHandle)
        {
            Declarations.RegCloseKey(aHandle);
        }

        protected string GetValueAsString(object valueDataBuffer, RegistryValueKind valueType)
        {
            if (valueDataBuffer is StringBuilder)
                return valueDataBuffer.ToString();

            var valueData = new StringBuilder();
            var valueDataAsByteArray = valueDataBuffer as byte[];

            switch (valueType)
            {
                case RegistryValueKind.Binary:
                    for (var i = 0; i < valueDataAsByteArray.Length; i++)
                    {
                        if (i != 0)
                            valueData.Append(" ");
                        valueData.Append(valueDataAsByteArray[i].ToString("X2"));
                    }

                    return valueData.ToString();

                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.MultiString:
                    Debug.Assert(false, "String values should be processed before calling this function.");
                    return "";

                case RegistryValueKind.DWord:
                    var dword = BitConverter.ToUInt32(valueDataAsByteArray, 0);
                    valueData.Append(RegistryTools.GetRegValueRepresentation(dword));
                    return valueData.ToString();

                case RegistryValueKind.QWord:
                    var qword = BitConverter.ToUInt64(valueDataAsByteArray, 0);
                    valueData.Append(RegistryTools.GetRegValueRepresentation(qword));
                    return valueData.ToString();

                default:
                    Debug.Assert(false, "Unknown RegistryValueKind.");
                    return "";
            }
        }

        #endregion

        #region Conversion

        public RegInfo AsRegInfo()
        {
            var regInfo = new RegKeyInfo();

            var lastBackSlashPosition = Name.LastIndexOf('\\');

            regInfo.Name = lastBackSlashPosition > -1 ? Name.Substring(lastBackSlashPosition + 1) : Name;
            regInfo.Path = regInfo.OriginalPath = Name;
            regInfo.OriginalKeyPaths.Add(Name, true);

            foreach (var value in GetAllValues())
                regInfo.ValuesByName.Add(value.NormalizedName, value);

            regInfo.Success = true;

            return regInfo;
        }

        #endregion
    }
}