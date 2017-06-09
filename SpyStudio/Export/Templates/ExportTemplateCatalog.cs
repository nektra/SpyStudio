using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SpyStudio.Tools;

namespace SpyStudio.Export.Templates
{
    public class ExportTemplateCatalog
    {
        public static string DefaultCatalogURL { get { return "http://www.nektra.com/files/SpyStudio/Catalogs/Default.cat"; } }

        #region Properties

        public string Name { get; protected set; }
        public string Version { get; protected set; }
        public string ReleaseDate { get; protected set; }

        public string PathInSystem { get; protected set; }

        public List<TemplateInfo> TemplateInfos { get; protected set; }

        #endregion

        #region Instantiation and Initialization

        public static ExportTemplateCatalog At(string aPath)
        {
            if (!File.Exists(aPath))
                MessageBox.Show("Template catalog not found at: " + aPath);

            var catalog = new ExportTemplateCatalog {PathInSystem = aPath};

            var catalogAsXML = new XmlDocument();
            catalogAsXML.Load(aPath);

            catalog.InitializeUsing(catalogAsXML);

            return catalog;
        }

        public ExportTemplateCatalog()
        {
            TemplateInfos = new List<TemplateInfo>();
        }

        private void InitializeUsing(XmlDocument aCatalogAsXML)
        {
            try
            {

                var catalogData = aCatalogAsXML.DocumentElement.SelectSingleNode("/templateCatalog/catalogData");

                Name = catalogData.SelectSingleNode("name").InnerText;
                Version = catalogData.SelectSingleNode("version").InnerText;
                ReleaseDate = catalogData.SelectSingleNode("releaseDate").InnerText;

                var templates = aCatalogAsXML.DocumentElement.SelectSingleNode("/templateCatalog/templates");

                foreach (XmlNode template in templates.ChildNodes)
                    TemplateInfos.Add(TemplateInfo.From(template));
            }
            catch(Exception ex)
            {
                Debug.Assert(false, "Error parsing template catalog: " + ex.Message);
                MessageBox.Show("Error parsing template catalog.", "Spy Studio", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}