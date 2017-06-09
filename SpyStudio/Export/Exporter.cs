using SpyStudio.Export.ThinApp;

namespace SpyStudio.Export
{
    public abstract class Exporter
    {
        public IExportProgressControl ProgressDialog { get; set; }

        public abstract void GeneratePackage(VirtualizationExport export);
        public abstract void Stop();
    }
}