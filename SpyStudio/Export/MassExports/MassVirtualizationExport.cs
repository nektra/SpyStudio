using System.Collections.Generic;
using System.Linq;
using SpyStudio.Database;
using SpyStudio.Dialogs.ExportWizards.MassExports;
using SpyStudio.Trace;

namespace SpyStudio.Export.MassExports
{
    public abstract class MassVirtualizationExport
    {
        #region Properties

        public Dictionary<string, VirtualizationExport> Exports { get; protected set; }

        #endregion

        #region Instantiation

        protected MassVirtualizationExport(DeviareRunTrace aTrace)
        {
            Exports = new Dictionary<string, VirtualizationExport>();

            GenerateExportsFrom(aTrace);
        }

        #endregion

        #region Initialization

        public abstract void Initialize(ExportSettingsTable aSettingsTable);

        protected void GenerateExportsFrom(DeviareRunTrace aTrace)
        {
            var allEvents = EventDatabaseMgr.GetInstance().GetAllEvents(aTrace.TraceId);

            foreach (var group in allEvents.GroupBy(e => e.ProcessName))
            {
                var export = GenerateExportFrom(aTrace);
                export.Name = group.Key;
                Exports.Add(group.Key, GenerateExportFrom(aTrace));
            }
        }

        #endregion

        protected abstract VirtualizationExport GenerateExportFrom(DeviareRunTrace aTrace);
        public abstract Exporter GetExporter();
        public abstract bool SystemMeetsRequirements(MassExportWizard aWizard);
    }
}