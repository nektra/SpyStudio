using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpyStudio.Dialogs.ExportWizards.SWV;
using SpyStudio.FileSystem;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppPackage : IPackage
    {
        public string Name { get; set; }

        public static ThinAppPackage Named(string aName)
        {
            return new ThinAppPackage(aName);
        }

        protected ThinAppPackage()
        {
            
        }

        protected ThinAppPackage(string aName)
        {
            Name = aName;
        }
    }
}