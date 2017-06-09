using System.Collections.Generic;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Loader;
using SpyStudio.Swv;
using SpyStudio.Tools;

namespace SpyStudio.Export.SWV
{
    public class SwvExportData : ExportData
    {

        public SwvExportData(ExportWizard wizard)
        {
            Wizard = wizard;
            Type = TemplateStore.DataType.CollectedData;
            IsolationRulesChanged = true;
            IsolationRules = new List<SymLayers.RuleEntry>();
            OriginalFiles = new List<FileEntry>();
            SearchPaths = new List<TemplateStore.SourcePath>();
            HttpPaths = new List<TemplateStore.SourcePath>();
        }

        public bool LoadedTemplate { get { return Type != TemplateStore.DataType.CollectedData; } }
        public TemplateStore.DataType Type { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }

        public List<FileEntry> OriginalFiles { get; set; }
        public bool FilesUpdate { get; set; }
        public bool FilesDestinationUpdate { get; set; }
        public bool RegistryUpdate { get; set; }
        public bool CheckedFilesUpdate { get; set; }

        public List<TemplateStore.SourcePath> TemporarySearchPaths { get; set; }
        public List<TemplateStore.SourcePath> SearchPaths { get; set; }
        public List<TemplateStore.SourcePath> HttpPaths { get; set; }

        public List<string> FilesToRemove { get; set; }
        public List<string> DirsToRemove { get; set; }

        public List<SymLayers.RuleEntry> IsolationRules { get; set; }

        public bool IsolationRulesChanged { get; set; }

        public SymLayers SymLayers { get; set; }
        public string LayerGuid { get; set; }
    }
}