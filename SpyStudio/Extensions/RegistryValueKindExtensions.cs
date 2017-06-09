using Microsoft.Win32;

namespace SpyStudio.Extensions
{
    public static class RegistryValueKindExtensions
    {
        public static string AsSpyStudioString(this RegistryValueKind aValueType)
        {
            var ret = string.Empty;
            switch (aValueType)
            {
                case RegistryValueKind.Binary:
                    ret = "REG_BINARY";
                    break;
                case RegistryValueKind.DWord:
                    ret = "REG_DWORD";
                    break;
                case RegistryValueKind.ExpandString:
                    ret = "REG_EXPAND_SZ";
                    break;
                case RegistryValueKind.MultiString:
                    ret = "REG_MULTI_SZ";
                    break;
                //case RegistryValueKind.Unknown:
                //    ret = "";
                //    break;
                case RegistryValueKind.QWord:
                    ret = "REG_QWORD";
                    break;
                case RegistryValueKind.String:
                    ret = "REG_SZ";
                    break;
            }
            return ret;
        }
    }
}
