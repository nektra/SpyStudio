using System;
using SpyStudio.Tools;

namespace SpyStudio.Swv.Registry
{
    public class SwvLayerRegistrySubKey : SwvLayerRegistryKey
    {
        #region Properties

        protected override SwvLayerRegistryBaseKey BaseKey { get; set; }

        protected string SubKey { get; set; }

        #endregion

        #region Instantiation

        public static SwvLayerRegistrySubKey Under(SwvLayerRegistryBaseKey aBaseKey, string subKeyName)
        {
            var subKey = new SwvLayerRegistrySubKey
                {
                    BaseKey = aBaseKey, 
                    Name = aBaseKey.Name + @"\" + subKeyName,
                    NameInLayer = aBaseKey.NameInLayer + @"\" + subKeyName,
                    SubKey = subKeyName
                };

            return subKey;
        }

        #endregion

        #region Control

        public override SwvLayerRegistryKey CreateSubKey(string aSubKey)
        {
            TryOpen();

            UIntPtr newHandle;
            UIntPtr disposition;
            Declarations.RegCreateKeyEx(Handle, aSubKey, 0, null, 0, 0xF003F, IntPtr.Zero, out newHandle, out disposition);

            Declarations.RegCloseKey(newHandle);
            Close();

            return SwvLayerRegistrySubKey.Under(BaseKey, SubKey + "//" + aSubKey);
        }

        #endregion
    }
}