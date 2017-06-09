using System;
using SpyStudio.Tools;

namespace SpyStudio.Registry
{
    public static class HookTypeExtensions
    {
        public static RegistryKeyAccess ToRegistryKeyAccess(this HookType aHookType)
        {
            var registryKeyAccess = RegistryKeyAccess.None;

            if (aHookType == HookType.RegDeleteKey
                || aHookType == HookType.RegDeleteValue
                || aHookType == HookType.RegSetValue
                || aHookType == HookType.RegCreateKey
                || aHookType == HookType.RegRenameKey)
                registryKeyAccess |= RegistryKeyAccess.Write;

            if (aHookType == HookType.RegEnumerateKey
                || aHookType == HookType.RegEnumerateValueKey
                || aHookType == HookType.RegOpenKey
                || aHookType == HookType.RegQueryKey
                || aHookType == HookType.RegQueryMultipleValues
                || aHookType == HookType.RegQueryValue)
                registryKeyAccess |= RegistryKeyAccess.Read;

            if (registryKeyAccess == RegistryKeyAccess.None)
                throw new Exception("HookType " + aHookType + " is not translatable to a RegistryKeyAccess object.");

            return registryKeyAccess;
        }
    }
}