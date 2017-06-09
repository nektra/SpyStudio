using System;
using System.Diagnostics;
using Microsoft.Win32;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using System.Linq;

namespace SpyStudio.Swv.Registry
{
    public class SwvLayerRegistryBaseKey : SwvLayerRegistryKey
    {
        #region Properties

        protected override SwvLayerRegistryBaseKey BaseKey { get { return this; } set { Debug.Assert(false, "BaseKey is not settable for base keys.");} }

        #endregion

        #region Instantiation

        private static string GetLastNumber(Declarations.FSL2_INFO aLayerInfo)
        {
            return aLayerInfo.regPath.SplitAsPath().Last();
        }

        public static SwvLayerRegistryBaseKey LocalMachineFrom(Declarations.FSL2_INFO aLayerInfo)
        {
            var baseKey = new SwvLayerRegistryBaseKey
                {
                    Name = RegistryTools.HkeyLocalMachineString,
                    NameInLayer = @"HKEY_LOCAL_MACHINE\_SWV_LAYER_" + GetLastNumber(aLayerInfo) + @"\HLM"
                };

            return baseKey;
        }

        public static SwvLayerRegistryBaseKey CurrentUserFrom(Declarations.FSL2_INFO aLayerInfo)
        {
            var baseKey = new SwvLayerRegistryBaseKey
            {
                Name = RegistryTools.HkeyCurrentUserString,
                NameInLayer = @"HKEY_LOCAL_MACHINE\_SWV_LAYER_" + GetLastNumber(aLayerInfo) + @"\HU",
            };

            return baseKey;
        }

        public static SwvLayerRegistryBaseKey LocalMachine64From(Declarations.FSL2_INFO aLayerInfo)
        {
            var baseKey = new SwvLayerRegistryBaseKey
            {
                Name = RegistryTools.HkeyLocalMachineString,
                NameInLayer = @"HKEY_LOCAL_MACHINE\_SWV_LAYER_" + GetLastNumber(aLayerInfo) + @"\HLM64"
            };

            return baseKey;
        }

        public static SwvLayerRegistryBaseKey CurrentUser64From(Declarations.FSL2_INFO aLayerInfo)
        {
            var baseKey = new SwvLayerRegistryBaseKey
            {
                Name = RegistryTools.HkeyCurrentUserString,
                NameInLayer = @"HKEY_LOCAL_MACHINE\_SWV_LAYER_" + GetLastNumber(aLayerInfo) + @"\HU64",
            };

            return baseKey;
        }

        #endregion

        #region Access

        public SwvLayerRegistryKey GetSubKey(string aSubKey)
        {
            return SwvLayerRegistrySubKey.Under(this, aSubKey);
        }

        #endregion

        #region Control

        public override SwvLayerRegistryKey CreateSubKey(string aSubKey)
        {
            if (!TryOpen())
                return null;

            try
            {
                UIntPtr newHandle;
                UIntPtr disposition;
                int error = Declarations.RegCreateKeyEx(Handle, aSubKey, 0, null, 0, 0xF003F,
                                                        IntPtr.Zero, out newHandle,
                                                        out disposition);
                if (error != 0)
                    return null;

                CloseKey(newHandle);

                return SwvLayerRegistrySubKey.Under(this, aSubKey);

            }
            finally
            {
                Close();
            }
        }

        #endregion
    }
}