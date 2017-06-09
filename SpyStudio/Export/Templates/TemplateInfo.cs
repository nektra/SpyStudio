using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Ionic.Zip;
using SpyStudio.Extensions;
using SpyStudio.Tools;

namespace SpyStudio.Export.Templates
{
    public class TemplateInfo
    {
        #region Properties

        public string ID { get { return Name + Version; } }

        [XmlElement]
        public string Name { get; set; }
        [XmlElement]
        public string Description { get; set; }
        [XmlElement]
        public string Version { get; set; }
        [XmlElement]
        public DateTime ReleaseDate { get; set; }
        [XmlElement]
        public string URI { get; set; }

        [XmlIgnore]
        public bool IsLocal { get; set; }
        [XmlIgnore]
        public bool IsAvailable { get; set; }

        [XmlAttribute("IsLocal")]
        public string IsLocalString
        {
            get { return IsLocal.ToString(CultureInfo.InvariantCulture); }
            set { IsLocal = Convert.ToBoolean(value); }
        }

        [XmlAttribute("IsAvailable")]
        public string IsAvailableString
        {
            get { return IsAvailable.ToString(CultureInfo.InvariantCulture); }
            set { IsAvailable = Convert.ToBoolean(value); }
        }

        [DefaultValue("")]
        [XmlElement]
        public string License;
        [DefaultValue("")]
        [XmlElement]
        public string Copyright;

        #endregion

        #region Instantiation and Initialization

        public static TemplateInfo ForTemplateAt(string aPath)
        {
            var templateInfo = From(GetInfoNodeFromTemplateAt(aPath));
            if (templateInfo.IsLocal)
                templateInfo.URI = aPath;

            return templateInfo;
        }

        public static TemplateInfo From(XmlNode aTemplateAsXMLNode)
        {
            var templateInfo = new TemplateInfo();
            templateInfo.InitializeUsing(aTemplateAsXMLNode);
            return templateInfo;
        }

        public TemplateInfo()
        {
            Name = string.Empty;
            Description = string.Empty;
            Version = string.Empty;
            //ReleaseDate = string.Empty;
            URI = string.Empty;
        }

        private void InitializeUsing(XmlNode aTemplateAsXMLElement)
        {
            if (aTemplateAsXMLElement != null)
            {
                Name = aTemplateAsXMLElement.SelectSingleNode("Name").InnerText;
                Description = aTemplateAsXMLElement.SelectSingleNode("Description").InnerText;
                Version = aTemplateAsXMLElement.SelectSingleNode("Version").InnerText;
                try
                {
                    var date = aTemplateAsXMLElement.SelectSingleNode("ReleaseDate").InnerText;
                    ReleaseDate = Convert.ToDateTime(date);
                }
                catch
                {
                }
                URI = aTemplateAsXMLElement.SelectSingleNode("URI").InnerText;
                IsLocal = bool.Parse(aTemplateAsXMLElement.Attributes["IsLocal"].Value);
                IsAvailable = bool.Parse(aTemplateAsXMLElement.Attributes["IsAvailable"].Value);
            }
            else
            {
                Name = "(null)";
                Description = "(null)";
                Version = "(null)";
                //ReleaseDate = null;
                URI = "(null)";
                IsLocal = true;
                IsAvailable = true;
            }
        }

        protected static XmlNode GetInfoNodeFromTemplateAt(string aTemplatePath)
        {
            string xmlText = null;
            using (var file = new StreamReader(new FileStream(aTemplatePath, FileMode.Open, FileAccess.Read), Encoding.UTF8))
            using (var xml = XmlReader.Create(file))
            {
                while (xml.Read())
                {
                    if (xml.Name == "TemplateInfo" && xml.NodeType == XmlNodeType.Element)
                    {
                        xmlText = xml.ReadOuterXml();
                        break;
                    }
                }
            }
            if (xmlText == null)
                return null;
            var doc = new XmlDocument();
            doc.LoadXml(xmlText);
            return doc.DocumentElement;
        }

        #endregion

        #region Control

        public PortableTemplate Retrieve()
        {
            return IsLocal ? RetrieveLocally(URI) : RetrieveFromWeb();
        }

        public PortableTemplate Restore(StreamReader stream)
        {
            var ret = PortableTemplate.RestoreTemplate(stream);
            if(ret != null)
                ret.IsInUse = true;
            return ret;
        }

        public void SetToSaveAt(string aPath)
        {
            Name = Path.GetFileNameWithoutExtension(aPath);
            Description = "Saved by user";
            Version = "1.0.0.0";
            ReleaseDate = DateTime.Now.Date;
                //.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            //ReleaseDate = DateTime.Now.Date.ToString();
            URI = aPath;
            IsLocal = true;
            IsAvailable = true;
        }

        public void MergeWith(TemplateInfo aTemplateInfo)
        {
            Debug.Assert(Name == aTemplateInfo.Name && Description == aTemplateInfo.Description && Version == aTemplateInfo.Version && ReleaseDate == aTemplateInfo.ReleaseDate, "Tried to merge non-matching TemplateInfos.");

            URI = aTemplateInfo.URI;
            IsLocal = aTemplateInfo.IsLocal;
            IsAvailable = aTemplateInfo.IsAvailable;
        }

        #endregion

        #region Protected members

        protected string DownloadToTempLocationAndUnzip()
        {
            var client = new WebClient();

            var stsPath = string.Empty;
            var downloadPath = SpyStudioConstants.CatalogsTempDirectory + "\\" + Name;

            try
            {
                client.DownloadFile(URI, downloadPath);

                stsPath = SpyStudioConstants.CatalogsTempDirectory + "\\" +
                              Path.GetFileNameWithoutExtension(downloadPath) + ".sts";

                using (var templateWriter = new BinaryWriter(new FileStream(stsPath, FileMode.Create, FileAccess.Write))
                    )
                using (var zippedTemplate = ZipFile.Read(downloadPath))
                    zippedTemplate.First().Extract(templateWriter.BaseStream);
            }
            catch (BadPasswordException)
            {
                using (var templateWriter = new BinaryWriter(new FileStream(stsPath, FileMode.Create, FileAccess.Write))
                    )
                using (var zippedTemplate = ZipFile.Read(downloadPath))
                    zippedTemplate.First().ExtractWithPassword(templateWriter.BaseStream,
                                                               SpyStudioConstants.GetZipPassword());
            }
            catch (WebException ex)
            {
                MessageBox.Show("Can't download file " + URI + " Error: " + ex.Message, "Spy Studio", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening " + downloadPath + " Error: " +ex.Message, "Spy Studio", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                Debug.Assert(false, "Error opening template: " + ex.Message);
            }
            finally
            {
                File.Delete(downloadPath);
            }

            return stsPath;
        }

        protected PortableTemplate RetrieveFromWeb()
        {
            var tempPath = DownloadToTempLocationAndUnzip();
            if (string.IsNullOrEmpty(tempPath))
                return null;

            PortableTemplate ret = RetrieveLocally(tempPath);
            File.Delete(tempPath);
            return ret;
        }

        protected PortableTemplate RetrieveLocally(string aPath)
        {
            using (var file = new FileStream(aPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(file))
                return Restore(reader);
        }

        #endregion
    }
}