using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Export.Templates;
using SpyStudio.FileSystem;
using SpyStudio.Registry.Controls;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Export
{
    public abstract class VirtualizationExport : IFieldContainer<ExportFieldNames>
    {
        #region Properties

        private bool _canceled;

        public ExportSettings Settings { get; set; }

        protected Dictionary<ExportFieldNames, object> Fields;

        public bool Canceled { get { return _canceled; } }
        public uint TraceId { get; set; }
        public uint MainWindowTraceId { get; set; }
        public Guid MainWindowObjectId { get; set; }

        public bool FilesWereUpdated { get; set; }
        public bool RegistryWasUpdated { get; set; }

        public bool FilesNeedUpdate { get; set; }
        public bool EntryPointsNeedUpdate { get; set; }
        public bool RegistryNeedUpdate { get; set; }

        public List<string> RuntimesExported { get; set; }

        public virtual IEnumerable<FileSystemTreeChecker> FileCheckers
        {
            get { throw new Exception("This export has no file checker."); }
        }

        public virtual IEnumerable<RegistryChecker> RegistryCheckers
        {
            get { throw new Exception("This export has no registry checker."); }
        }

        public abstract string Name { get; set; }

        public virtual bool ShowFileSystemIsolationOptions { get { return false; } }
        public CheckerType CheckerType { get; set; }

        #endregion

        #region Instatiation

        protected VirtualizationExport(DeviareRunTrace aTrace)
        {
            Fields = new Dictionary<ExportFieldNames, object>();
            SetFieldValue(ExportFieldNames.Trace, aTrace);
            TraceId = aTrace.TraceId;
            FilesNeedUpdate = EntryPointsNeedUpdate = RegistryNeedUpdate = true;
            MainWindowTraceId = aTrace.TraceId;
            MainWindowObjectId = aTrace.ObjectId;
            PortableTemplate template = null;
            using (var file = LastTemplates.GetLastTemplate(aTrace, true))
            {
                if (file != null)
                {
                    using (var reader = new StreamReader(file))
                        template = PortableTemplate.RestoreTemplate(reader);
                }
                else
                    template = new PortableTemplate();
            }
            SetFieldValue(ExportFieldNames.VirtualizationTemplate, template);
            SetFieldValue(ExportFieldNames.ApplicationBehaviourAnalizers, new List<AppBehaviourAnalyzer>());
        }

        #endregion

        #region Field operations

        public object GetFieldValue(ExportFieldNames aFieldName)
        {
            if (!Fields.ContainsKey(aFieldName))
                return null;
            return Fields[aFieldName];
        }

        public void SetFieldValue(ExportFieldNames aFieldName, object aValue)
        {
            if (!Fields.ContainsKey(aFieldName))
                Fields.Add(aFieldName, null);

            Fields[aFieldName] = aValue;
        }

        public ExportField<T> GetField<T>(ExportFieldNames aFieldName)
        {
            return new ExportField<T>(this, aFieldName);
        }

        #endregion

        public void Cancel()
        {
            _canceled = true;
        }

        public abstract DialogResult ShowAdvancedSettingsDialog();

        public abstract Exporter CreateExporter();

        public abstract bool SystemMeetsRequirements(ExportWizard anExportWizard);
    }
}
