//#define DISABLE_STATEPAGE

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Ionic.Zip;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Properties;
using SpyStudio.Tools;
using Wizard.UI;
using System.Linq;
using SpyStudio.Extensions;

namespace SpyStudio.Dialogs.ExportWizards
{
#if !DISABLE_STATEPAGE
    public partial class TemplateSelect : InternalWizardPage
    {
        #region Properties

        protected ExportWizard Wizard;

        protected readonly VirtualizationExport Export;

        protected BackgroundWorker WebCatalogLoader { get; set; }

        protected bool ReadyForNextStep { get { return SelectedTemplateInfo != null && SelectedTemplateInfo.IsAvailable; } }

        protected TemplateInfo SelectedTemplateInfo
        {
            get
            {
                var selectedItem = TemplateList.SelectedItems.Cast<TemplateListItem>().FirstOrDefault();

                return selectedItem == null ? null : selectedItem.TemplateInfo;
            }
        }

        #endregion

        #region Instatiation

        public TemplateSelect(ExportWizard aWizard, VirtualizationExport anExport)
        {
            Export = anExport;
            Wizard = aWizard;

            //WizardNext += (a, b) => OnWizardNext();
            SetActive += OnSetActive;
            QueryCancel += (a, b) => OnQueryCancel();

            InitializeComponent();
        }

        #endregion

        #region Event Handling

        private void OnSetActive(object sender, WizardPageEventArgs e)
        {
            if (e.IsBackActionIn(Wizard))
                return;

            SetWizardButtons(WizardButtons.Next | WizardButtons.Back);
            EnableNextButton(ReadyForNextStep);
            TemplateList.ItemSelectionChanged += (a, b) => EnableNextButton(ReadyForNextStep);
            EnableCancelButton(true);
        }

        private void OnQueryCancel()
        {
            Export.Cancel();
            CancelWebCatalogLoading();
        }

        public override void OnWizardNext(WizardPageEventArgs e)
        {
            base.OnWizardNext(e);

            var templateInfo = SelectedTemplateInfo.Retrieve();
            if(templateInfo != null)
            {
                EnableNextButton(false);
                Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value = SelectedTemplateInfo.Retrieve();
            }
            else
            {
                EnableNextButton(false);
                MessageBox.Show(this, "Error loading template", Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            this.ExecuteInUIThreadSynchronously(() =>
                {
                    this.DisableUI();
                    UseWaitCursor = true;
                });

            WebCatalogLoader = Threading.ExecuteAsynchronously(LoadCatalogFromWeb, args => this.ExecuteInUIThreadSynchronously(() =>
                {
                    this.EnableUI();
                    UseWaitCursor = false;
                }));
        }

        private void BrowseButtonClick(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                DefaultExt = "cat",
                Filter = "Catalog or Template file (*.cat, *.tpt, *.sts)|*.cat;*.tpt;*.sts|All files (*.*)|*.*",
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Open File"
            };
            if (!string.IsNullOrEmpty(Settings.Default.PathLastSelectedTemplateFolder))
                openDlg.InitialDirectory = Settings.Default.PathLastSelectedTemplateFolder;

            if (openDlg.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                Settings.Default.PathLastSelectedTemplateFolder = Path.GetDirectoryName(openDlg.FileName);
                Settings.Default.Save();
            }
            catch (Exception)
            {
            }

            foreach (var filePath in openDlg.FileNames)
            {

                switch (Path.GetExtension(filePath))
                {
                    case ".cat": // is zipped catalog
                        LoadZippedCatalogAt(filePath);
                        break;

                    case ".tpt": // is zipped template
                        LoadZippedTemplateAt(filePath);
                        break;

                    case ".sts": // is template
                        LoadTemplateAt(filePath);
                        break;

                    default:
                        MessageBox.Show(this, "Unknown file type: " + Path.GetExtension(filePath), 
                            Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
        }

        #endregion

        #region Control

        protected void LoadCatalogFromWeb()
        {
            string generalWebCatalogPath = null;
            string userWebCatalogPath = null;
            try
            {
                generalWebCatalogPath = DownloadGeneralCatalog();
            }
            catch (WebException)
            {
            }

            if (generalWebCatalogPath != null)
            {
                LoadZippedCatalogAt(generalWebCatalogPath);
                File.Delete(generalWebCatalogPath);
            }
            if (userWebCatalogPath != null)
            {
                LoadZippedCatalogAt(userWebCatalogPath);
                File.Delete(userWebCatalogPath);
            }
        }

        protected void LoadCatalog(ExportTemplateCatalog aCatalog)
        {
            LoadTemplatesFrom(aCatalog);
        }

        protected void LoadTemplateAt(string aPath)
        {
            var templateListItem = TemplateListItem.For(TemplateInfo.ForTemplateAt(aPath));

            this.ExecuteInUIThreadAsynchronously(() => TemplateList.Items.Add(templateListItem));
        }

        protected void LoadZippedTemplateAt(string aZippedTemplatePath)
        {
            var templateTempPath = SpyStudioConstants.TemplatesTempDirectory + "\\" + Path.GetFileNameWithoutExtension(aZippedTemplatePath) + ".sts";

            if (File.Exists(templateTempPath))
                File.Delete(templateTempPath);

            try
            {
                using (var templateWriter = new BinaryWriter(new FileStream(templateTempPath, FileMode.Create, FileAccess.Write)))
                using (var zippedTemplate = ZipFile.Read(aZippedTemplatePath))
                    zippedTemplate.First().Extract(templateWriter.BaseStream);

                LoadTemplateAt(templateTempPath);
            }
            catch (BadPasswordException)
            {
                using (var templateWriter = new BinaryWriter(new FileStream(templateTempPath, FileMode.Create, FileAccess.Write)))
                using (var zippedTemplate = ZipFile.Read(aZippedTemplatePath))
                    zippedTemplate.First().ExtractWithPassword(templateWriter.BaseStream, SpyStudioConstants.GetZipPassword());

                LoadTemplateAt(templateTempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error opening " + aZippedTemplatePath, Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.Assert(false, "Error opening template: " + ex.Message);
            }
        }

        protected void LoadZippedCatalogAt(string aZippedCatalogPath)
        {
            var catalogTempPath = SpyStudioConstants.CatalogsTempDirectory + "\\" + Path.GetFileNameWithoutExtension(aZippedCatalogPath) + "Temp.cat";

            try
            {
                using (var catalogWriter = new BinaryWriter(new FileStream(catalogTempPath, FileMode.Create, FileAccess.Write)))
                using (var zippedCatalog = ZipFile.Read(aZippedCatalogPath))
                    zippedCatalog.First().Extract(catalogWriter.BaseStream);

                LoadCatalog(ExportTemplateCatalog.At(catalogTempPath));
            }
            catch (BadPasswordException)
            {
                try
                {
                    using (
                        var catalogWriter =
                            new BinaryWriter(new FileStream(catalogTempPath, FileMode.Create, FileAccess.Write)))
                    using (var zippedCatalog = ZipFile.Read(aZippedCatalogPath))
                        zippedCatalog.First().ExtractWithPassword(catalogWriter.BaseStream,
                                                                  SpyStudioConstants.GetZipPassword());
                    LoadCatalog(ExportTemplateCatalog.At(catalogTempPath));
                }
                catch (BadPasswordException)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error opening " + aZippedCatalogPath, Settings.Default.AppName, 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.Assert(false, "Error opening catalog: " + ex.Message);
            }
            finally
            {
                File.Delete(catalogTempPath);
            }
        }

        protected void LoadTemplatesFrom(ExportTemplateCatalog aCatalog)
        {
            this.ExecuteInUIThreadAsynchronously(() =>
                {
                    foreach (var templateInfo in aCatalog.TemplateInfos)
                    {
                        if (!TemplateList.Items.ContainsKey(templateInfo.ID))
                        {
                            TemplateList.Items.Add(TemplateListItem.For(templateInfo));
                            continue;
                        }

                        var existingItem = ((TemplateListItem)TemplateList.Items[templateInfo.ID]);
                        existingItem.MergeInfoWith(templateInfo);
                        existingItem.UpdateAppearence();
                    }
                });
        }

        protected void CancelWebCatalogLoading()
        {
            if (!WebCatalogLoader.IsBusy)
                return;

            WebCatalogLoader.CancelAsync();
            Threading.WaitForCompletionOf(WebCatalogLoader, true);
        }

        #endregion

        #region Utils

        protected string DownloadGeneralCatalog()
        {
            var client = new WebClient();

            var defaultCatalogTempPath = SpyStudioConstants.CatalogsTempDirectory + "\\Default.cat";

            client.DownloadFile(ExportTemplateCatalog.DefaultCatalogURL, defaultCatalogTempPath);

            return defaultCatalogTempPath;
        }

        #endregion
    }
#endif
}