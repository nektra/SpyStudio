using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV.Manifest
{
    public class URLProtocol : Extension
    {
        //Required: key "HKCRx\<Name>\URL Protocol" must exist

        public override string Category { get { return "AppV.URLProtocol"; } }
        //Taken from key HKCRx\<Name>
        public string Name;
        public ApplicationURLProtocol ApplicationURLProtocol;

        public static IEnumerable<URLProtocol> Create(IEnumerable<FileTypeAssociation> existingAssociations)
        {
            var classes = RegistryTools.OpenClassesKey();
            if (classes == null)
                throw new KeyNotFoundException();
            var set = new HashSet<string>(existingAssociations.Select(x => x.FileExtension.ProgId));
            var protocolKeys = classes
                .GetSubKeyNames()
                .Where(set.Contains)
                .Select(x => classes.OpenSubKey(x))
                .Where(x => x != null && x.GetSubKeyNames().Contains("URL Protocol", StringComparer.InvariantCultureIgnoreCase));

            return protocolKeys.Select(x => new URLProtocol(x));
        }

        public URLProtocol() { }

        private URLProtocol(RegistryKey key)
        {
            Name = key.GetKeyName();
            ApplicationURLProtocol = new ApplicationURLProtocol(key);
        }

        public override IEnumerable<string> Symbols
        {
            get { return Name.ToEnumerable(); }
        }

        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:URLProtocol");

            xml.WriteElementString("appv:Name", Name);

            ApplicationURLProtocol.GenerateXml(xml);

            xml.WriteEndElement();
        }
    }

    public class ApplicationURLProtocol : XmlGenerator
    {
        //Taken from default value of HKCRx\<Name>
        public string Description,
            //App-V-normalized path.
            //Taken from default value of HKCRx\<Name>\DefaultIcon
            DefaultIcon,
            //GUESSED: Taken from value HKCRx\<Name>\FriendlyTypeName
            FriendlyTypeName;
        //GUESSED: Taken from value HKCRx\<Name>\EditFlags
        public int EditFlags;
        //Requires key HKCRx\<Name>\shell
        public ShellCommands ShellCommands;
        //Taken from value HKCRx\<Name>\ShellFolder
        public CurlyGuid ShellFolder,
            //Unknown.
            SourceFilter;

        public ApplicationURLProtocol() { }

        public ApplicationURLProtocol(RegistryKey key)
        {
            Description = key.GetStringValueForAppVManifest("");
            DefaultIcon = key.GetDefaultStringValueOfSubKeyForAppVManifest("DefaultIcon");
            FriendlyTypeName = key.GetStringValueForAppVManifest("FriendlyTypeName");
            key.GetIntValue("EditFlags", out EditFlags);

            InitShellCommands(key);

            ShellFolder = CurlyGuid.Create(key.GetStringValueForAppVManifest("ShellFolder"));
            SourceFilter = null;
        }

        private void InitShellCommands(RegistryKey key)
        {
            var shell = key.OpenSubKey("shell");
            if (shell == null)
                return;
            ShellCommands = new ShellCommands(shell);
        }
    }

}