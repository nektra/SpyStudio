namespace SpyStudio.Export.ThinApp
{
    public enum ThinAppIsolationOption
    {
        Inherit,
        Merged,
        DefaultFileSystemIsolation = Merged,
        WriteCopy,
        DefaultRegistryIsolation = WriteCopy,
        Full,
        None
    }
}