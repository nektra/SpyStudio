using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.FileSystem;
using SpyStudio.Loader;
using SpyStudio.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Swv;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Export.SWV
{
    public class SwvExport : VirtualizationExport
    {
        public SwvExport(DeviareRunTrace aTrace) : base (aTrace)
        {
            SetFieldValue(ExportFieldNames.OriginalFiles, new List<FileEntry>());
            SetFieldValue(ExportFieldNames.Files, new List<FileEntry>());
            SetFieldValue(ExportFieldNames.RegistryKeys, new List<RegKeyInfo>());
            SetFieldValue(ExportFieldNames.IsolationRules, new List<SwvIsolationRuleEntry>());
            SetFieldValue(ExportFieldNames.SymantecLayers, new SwvLayers());
            SetFieldValue(ExportFieldNames.LayerName, "LayerName");
            SetFieldValue(ExportFieldNames.Platform, "x86");
        }

        private IEnumerable<FileSystemTreeChecker> _fileCheckers;

        public override IEnumerable<FileSystemTreeChecker> FileCheckers
        {
            get { return _fileCheckers ?? GenerateFileCheckers(); }
        }

        private IEnumerable<FileSystemTreeChecker> GenerateFileCheckers()
        {
            var appBehaviourAnalyzers = (IEnumerable<AppBehaviourAnalyzer>)GetFieldValue(ExportFieldNames.ApplicationBehaviourAnalizers);

            return _fileCheckers = appBehaviourAnalyzers.Select(analyzer => FileSystemTreeChecker.For(this, analyzer));
        }

        private IEnumerable<RegistryChecker> _registryCheckers;

        public override IEnumerable<RegistryChecker> RegistryCheckers
        {
            get { return _registryCheckers ?? GenerateRegistryCheckers(); }
        }

        private IEnumerable<RegistryChecker> GenerateRegistryCheckers()
        {
            var appBehaviourAnalyzers = (IEnumerable<AppBehaviourAnalyzer>)GetFieldValue(ExportFieldNames.ApplicationBehaviourAnalizers);

            return _registryCheckers = appBehaviourAnalyzers.Select(analyzer => RegistryChecker.For(this, analyzer));
        }

        public override string Name { get; set; }

        public bool PathsWereUpdated { get; set; }
        public bool IsolationRulesWereUpdated { get; set; }

        public override bool ShowFileSystemIsolationOptions { get { return false; } }

        public override DialogResult ShowAdvancedSettingsDialog()
        {
            throw new System.NotImplementedException();
        }

        public override Exporter CreateExporter()
        {
            return new SwvExporter();
        }

        public override bool SystemMeetsRequirements(ExportWizard anExportWizard)
        {
            if (!PlatformTools.IsRunningAsLocalAdmin())
            {
                MessageBox.Show(anExportWizard, Resources.Swv_Export_Wizard_Not_Admin_Error,
                                Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (SpyStudioIsx86 && PlatformTools.IsPlatform64Bits())
            {
                MessageBox.Show(anExportWizard, "Run SpyStudio x64 to create SWV™ layers in a x64 environment.",
                                Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!SWVIsInstalled)
            {
                MessageBox.Show(anExportWizard, "SWV™ is not installed or you are using SpyStudio x86 in a x64 environment. You need SWV™ to create layers.",
                                Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        protected bool SpyStudioIsx86 { get { return IntPtr.Size == 4; } }

        private bool SWVIsInstalled
        {
            get { return File.Exists(Path.Combine(Environment.SystemDirectory, "fsllib32.dll")); }
        }

        

        public SwvLayer Layer { get; set; }
    }

    public class SwvTemplateEditorExport : SwvExport
    {
        public SwvTemplateEditorExport(DeviareRunTrace aTrace)
            : base(aTrace)
        {
        }
        public override bool SystemMeetsRequirements(ExportWizard anExportWizard)
        {
            return true;
        }
    }
}