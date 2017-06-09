using System.Collections.Generic;
using SpyStudio.Export.SWV;
using System.Linq;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.ExportWizards.SWV
{
    public class SwvRegistrySelect : RegistrySelect
    {
        protected new readonly SwvExport Export;

        public SwvRegistrySelect(ExportWizard wizard, SwvExport anSwvExport, string selectTheKeysToIncludeInTheLayer) : base(wizard, anSwvExport, selectTheKeysToIncludeInTheLayer)
        {
            Export = anSwvExport;
        }

        protected override IEnumerable<RegistryTreeNodeBase> LoadRegistryFromSelectedPackage()
        {
            var layer = Export.Layer;
            IEnumerable<RegKeyInfo> regInfos = layer == null
                                                   ? new List<RegKeyInfo>()
                                                   : layer.RegInfos.Cast<RegKeyInfo>();
            return registryTreeView.Add(regInfos);
        }
    }
}