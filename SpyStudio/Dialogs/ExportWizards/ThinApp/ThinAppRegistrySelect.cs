using System.Collections.Generic;
using System.Linq;
using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;

namespace SpyStudio.Dialogs.ExportWizards.ThinApp
{
    public class ThinAppRegistrySelect : RegistrySelect
    {
        protected new ThinAppCapture Capture { get { return (ThinAppCapture) Wizard.VirtualPackage; } }

        public ThinAppRegistrySelect(ExportWizard aWizard, VirtualizationExport anExport, string aPageDescription) : base (aWizard, anExport, aPageDescription, true)
        {
            _defaultIsolationModeCombo.SelectedItem = ThinAppIsolationOption.DefaultRegistryIsolation;
        }

        protected override IEnumerable<RegistryTreeNodeBase> LoadRegistryFromSelectedPackage()
        {
            var addedNodes = new List<RegistryTreeNodeBase>();

            addedNodes.AddRange(Capture.RegInfos.OfType<RegValueInfo>().Select(valueInfo => registryTreeView.Add(valueInfo)));
            addedNodes.AddRange(Capture.RegInfos.OfType<ThinAppRegKeyInfo>().Select(keyInfo => registryTreeView.Add(keyInfo)));

            return addedNodes;            
        }

        protected override void OnWizardNext(object sender, Wizard.UI.WizardPageEventArgs wizardPageEventArgs)
        {
            base.OnWizardNext(sender, wizardPageEventArgs);
            var isolation = (ThinAppIsolationOption)_defaultIsolationModeCombo.SelectedItem;
            RegistryIsolation.Value = isolation;
            RegistryKeys.Value = GetCheckedKeys(isolation);
        }

        public List<RegKeyInfo> GetCheckedKeys(ThinAppIsolationOption isolation)
        {
            return registryTreeView.GetCheckedKeys(isolation);
        }
    }
}