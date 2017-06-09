using System.Collections.Generic;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV.Manifest
{
    public class AppPath : Extension
    {
        public override string Category { get { return "AppV.AppPath"; } }

        //These are all XML elements, and are all in the appv namespace.

        //Taken from key HKLM\Software[\Wow6432Node]\Microsoft\Windows\CurrentVersion\App Paths\<Name>
        public string Name,
                      //App-V-normalized path.
                      //Taken from value HKLM\Software[\Wow6432Node]\Microsoft\Windows\CurrentVersion\App Paths\<Name>, Default value.
                      ApplicationPath,
                      //App-V-normalized path.
                      //Taken from value HKLM\Software[\Wow6432Node]\Microsoft\Windows\CurrentVersion\App Paths\<Name>\Path.
                      PATHEnvironmentVariablePrefix;
        //Serialized as {0|1}. Omit if false.
        //Taken from value HKLM\Software[\Wow6432Node]\Microsoft\Windows\CurrentVersion\App Paths\<Name>\SaveURL. Set to false if not found.
        [IntSerializableField]
        [OmitZeroField]
        public bool SaveURL;
        //Serialized as {0|1}. Omit if false.
        //Taken from value HKLM\Software[\Wow6432Node]\Microsoft\Windows\CurrentVersion\App Paths\<Name>\useURL. Set to false if not found.
        [IntSerializableField]
        [OmitZeroField]
        public bool CanAcceptUrl;

        //Key must point at HKLM\Software[\Wow6432Node]\Microsoft\Windows\CurrentVersion\App Paths\<Name>
        public AppPath() { }

        public AppPath(RegistryKey key)
        {
            Name = key.GetKeyName();
            ApplicationPath = key.GetStringValueForAppVManifest("");
            PATHEnvironmentVariablePrefix = key.GetStringValueForAppVManifest("Path");

            SaveURL = key.ValueExists("SaveURL");
            CanAcceptUrl = key.ValueExists("useURL");
        }

        public override IEnumerable<string> Symbols
        {
            get { return Name.ToEnumerable(); }
        }

        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:AppPath");

            XmlTools.SerializeAppvManifestObject(this, xml);

            xml.WriteEndElement();
        }
    }
}