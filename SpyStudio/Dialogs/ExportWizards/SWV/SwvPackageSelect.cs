using System.Linq;
using System.Windows.Forms;
using SpyStudio.Export;
using SpyStudio.Export.SWV;
using SpyStudio.Export.ThinApp;
using SpyStudio.Properties;
using SpyStudio.Swv;
using SpyStudio.Tools;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards.SWV
{
    class SwvPackageSelect : PackageSelect
    {
        protected override string VirtualizationPackageString { get { return "layer"; } }

        protected SwvLayers Layers;

        public SwvPackageSelect()
        {
            Initialize();
        }

        public SwvPackageSelect(SwvExport anSwvExport) : base(anSwvExport)
        {
            Initialize();
        }

        protected void Initialize()
        {
            Banner.Title = "Layer Select";
            Banner.Subtitle = "Select a layer to modify or create a new one by right-clicking on the layers list.";
            PackageListLabel.Text = "Available layers:";

            Layers = new SwvLayers();

            WizardNext += (sdr, args) => SaveSelectedLayer();
        }

        protected override void AddExeSelectPageIfNecessary(WizardPageEventArgs args)
        {
            if (!ShouldAddExeSelectPage)
                return;

            var exeSelect = new SwvExeSelect(Export);

            Wizard.Pages.Insert(Wizard.Pages.IndexOf(this) + 1, exeSelect);
            args.NewPage = exeSelect.Name;
        }

        private void SaveSelectedLayer()
        {
            ((SwvExport) Export).Layer = (SwvLayer) PackageList.SelectedItems[0].Tag;
            Export.SetFieldValue(ExportFieldNames.SelectedSymantecLayer, PackageList.SelectedItems[0].Tag);
        }

        protected override void LoadAvailablePackages()
        {
            var layer = Layers.GetFirstLayer();

            if (layer == null && Layers.LastErrorCode != 259)
            {
                MessageBox.Show(this, Resources.SymLayerListView_Error_loading_layer_list_ + Declarations.FslErrorToString(Layers.LastErrorCode),
                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            while (layer != null)
            {
                var item = PackageList.Items.Add(VirtualPackageListItem.Containing(layer));
                item.Tag = layer;
                layer = Layers.GetNextLayer();
            }

            Layers.CloseFind();
        }

        protected override IVirtualPackage CreateNewPackage()
        {
            SwvLayer layer;
            var layerName = "New";
            var i = 1;
            do
            {
                layer = Layers.CreateLayer(layerName);
                layerName = "New " + i++;
            } while (Layers.LastErrorCode == Declarations.FSL2_ERROR_LAYER_ALREADY_EXISTS);

            if (Layers.LastErrorCode != Declarations.FSL2_ERROR_SUCCESS)
            {
                MessageBox.Show(this, "Cannot create layer: " + Declarations.FslErrorToString(Layers.LastErrorCode),
                    Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return layer;
        }
    }
}