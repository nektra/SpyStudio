using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SpyStudio.Dialogs;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.FileSystem;
using SpyStudio.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Trace;
using System.Linq;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppExport : VirtualizationExport
    {
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

        public override string Name
        {
            get { return (string) GetFieldValue(ExportFieldNames.Name); }
            
            set { SetFieldValue(ExportFieldNames.Name, value); }
        }

        public override bool ShowFileSystemIsolationOptions { get { return true; } }

        public override DialogResult ShowAdvancedSettingsDialog()
        {
            return WizardFactory.CreateAdvancedSettingsWizardFor(this).ShowDialog();
        }

        public ThinAppExport(DeviareRunTrace aTrace) : base(aTrace)
        {
            SetFieldValue(ExportFieldNames.OriginalFiles, new List<FileEntry>());
            SetFieldValue(ExportFieldNames.Files, new List<FileEntry>());
            SetFieldValue(ExportFieldNames.EntryPoints, new List<EntryPoint>());
            SetFieldValue(ExportFieldNames.RegistryKeys, new List<RegKeyInfo>());

            Settings = new ThinAppExportSettings();
        }

        private IEnumerable<FileSystemTreeChecker> _fileCheckers;

        public static string ThinAppPath
        {
            get { return SystemDirectories.ValidProgramFiles86 + @"\vmware\vmware thinapp\"; }
        }

        public static string ThinAppCapturesPath
        {
            get { return ThinAppPath + @"captures\"; }
        }

        public ThinAppCapture Capture { get; set; }

        public bool EntryPointsWereUpdated { get; set; }

        public override Exporter CreateExporter()
        {
            return new ThinAppExporter();
        }

        public override bool SystemMeetsRequirements(ExportWizard anExportWizard)
        {
            if (!PlatformTools.IsRunningAsLocalAdmin() && !Debugger.IsAttached)
            {
                MessageBox.Show(anExportWizard, Resources.Swv_Export_Wizard_Not_Admin_Error,
                                Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            //if (!ThinAppIsInstalled)
            //{
            //    MessageBox.Show(anExportWizard, "VMware ThinApp 5.0 or later must be installed to use this function.",
            //                    Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return false;
            //}

            return true;
        }

        //protected bool ThinAppIsInstalled 
        //{ 
        //    get
        //    {
        //        const string registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Products";
        //        using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryKey))
        //        {
        //            foreach (var subkeyName in key.GetSubKeyNames())
        //            {
        //                using (var subkey = key.OpenSubKey(subkeyName))
        //                {
        //                    var productName = subkey.GetValue("ProductName");
        //                    var version = subkey.GetValue("Version");

        //                    if (productName != null && version != null && productName.Equals("VMware ThinApp") && (int)version > 0x05000000)
        //                        return true;
        //                }
        //            }
        //        }

        //        return false;
        //    } 
        //}
    }
}