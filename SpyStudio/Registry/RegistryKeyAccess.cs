using System;

namespace SpyStudio.Registry
{
    [Flags]
    public enum RegistryKeyAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
    }
}