using System.Collections;
using System.Collections.Generic;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;

namespace SpyStudio.Export.ThinApp
{
    public interface IVirtualPackage
    {
        string Name { get; }
        bool IsNew { get; }

        void Rename(string aName);
        bool Delete();

        HashSet<FileEntry> Files { get; }
        HashSet<RegInfo> RegInfos { get; } 

        void RefreshAll();
        void SaveAll();
    }
}