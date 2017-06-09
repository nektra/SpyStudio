using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV.Manifest
{
    public class ApplicationCapabilities : Extension
    {
        public override string Category { get { return "AppV.ApplicationCapabilities"; } }

        public Reference Reference;
        public List<Capabilities> CapabilityGroup = new List<Capabilities>();

        public static IEnumerable<ApplicationCapabilities> Create()
        {
            var registeredApplicationsKey =
                RegistryTools.GetKeyFromFullPath(@"HKEY_LOCAL_MACHINE\Software\RegisteredApplications");
            if (registeredApplicationsKey == null)
                return Enumerable.Empty<ApplicationCapabilities>();
            return registeredApplicationsKey.GetValueNames()
                .Select(x => new ApplicationCapabilities(registeredApplicationsKey, x));
        }

        public ApplicationCapabilities() { }

        private ApplicationCapabilities(RegistryKey key, string valueName)
        {
            Reference = new Reference(key, valueName);
            Debug.Assert(Reference.Path.StartsWith(@"software\", StringComparison.InvariantCultureIgnoreCase));
            var capability = Capabilities.Create(false, Reference.Path);
            if (capability != null)
                CapabilityGroup.Add(capability);
        }

        public override IEnumerable<string> Symbols
        {
            get { return Reference.Name.ToEnumerable(); }
        }

        /*
        public override void Connect(Dictionary<string, ManifestObject> symbolTable)
        {
            throw new NotImplementedException();
        }
        */

        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:ApplicationCapabilities");

            if (Reference != null)
                Reference.GenerateXml(xml);

            xml.WriteStartElement("appv:CapabilityGroup");
            CapabilityGroup.ForEach(x => x.GenerateXml(xml));
            xml.WriteEndElement();

            xml.WriteEndElement();
        }
    }

    public class Reference : XmlGenerator
    {
        //Taken from value name HKLM\Software\RegisteredAplications\<Name>
        public string Name,
                      //Path in the registry. NOT normalized.
                      //Taken from value HKLM\Software\RegisteredAplications\<Name>
                      Path;

        public Reference() { }

        public Reference(RegistryKey key, string valueName)
        {
            Name = valueName;
            Path = key.GetStringValueForAppVManifest(valueName);
        }
    }

    public class Capabilities : XmlGenerator
    {
        //Note: <Reference.Path*> means Reference.Path and also
        //@"Software\Wow6432Node\" + Reference.Path.Substring(@"software\".Length)

        //Taken from value HKML\<Reference.Path*>\ApplicationName
        public string Name,
                      //Taken from value HKML\<Reference.Path*>\ApplicationDescription
                      Description,
                      //GUESSED: Taken from value HKML\<Reference.Path*>\StartMenu\Mail
                      EMailSoftwareClient;
        //GUESSED: There's one more variable of unknown name taken from
        //value HKML\<Reference.Path*>\StartMenu\StartMenuInternet

        //True if taken from Wow6432Node.
        //If true, the XML element must be named CapabilitiesWow64, instead of Capabilities.
        [IgnoreField]
        public bool Wow64;

        //Requires HKML\<Reference.Path*>\FileAssociations
        public List<FileAssociation> FileAssociationList;
        //Requires HKML\<Reference.Path*>\MIMEAssociations
        public List<MimeAssociation> MimeAssociationList;
        //Requires HKML\<Reference.Path*>\URLAssociations
        public List<URLAssociation> URLAssociationList;

        public void SetFileAssociationList(List<object> list)
        {
            FileAssociationList = list.Cast<FileAssociation>().ToList();
        }
        public void SetMimeAssociationList(List<object> list)
        {
            MimeAssociationList = list.Cast<MimeAssociation>().ToList();
        }
        public void SetURLAssociationList(List<object> list)
        {
            URLAssociationList = list.Cast<URLAssociation>().ToList();
        }


        public static Capabilities Create(bool wow64, string path)
        {
            if (wow64)
                path = @"Software\Wow6432Node\" + path.Substring(@"software\".Length);
            var key = RegistryTools.GetKeyFromFullPath(@"HKEY_LOCAL_MACHINE\" + path);
            if (key == null)
                return null;
            return new Capabilities(key, wow64);
        }

        public Capabilities() { }

        private Capabilities(RegistryKey key, bool isWow64)
        {
            Name = key.GetStringValueForAppVManifest("ApplicationName");
            Description = key.GetStringValueForAppVManifest("ApplicationDescription");
            var startMenuKey = key.OpenSubKey("StartMenu");
            if (startMenuKey != null)
                EMailSoftwareClient = key.GetStringValueForAppVManifest("Mail");

            Wow64 = isWow64;

            //InitFileAssocs(key);
            InitAssocs(key);
        }

        private static readonly Type[] TypeArr = { typeof(string), typeof(string) };

        private void InitMember(RegistryKey key, string typeName)
        {
            key = key.OpenSubKey(typeName + "s");
            if (key == null)
                return;
            var methodInfo = GetType().GetMethod("Set" + typeName + "List");
            var klass = System.Reflection.Assembly.GetCallingAssembly().GetType(GetType().Namespace + "." + typeName);
            Debug.Assert(methodInfo != null && klass != null);
            var ctor = klass.GetConstructor(TypeArr);

            var list = key
                .GetValueNames()
                .Select(x => Tuple.Create(x, key.GetStringValueForAppVManifest(x)))
                .Where(x => x.Item2 != null)
                .Select(x => ctor.Invoke(new object[]{x.Item1, x.Item2}))
                .ToList();
            methodInfo.Invoke(this, new object[]{list});
        }

        /*
        private void InitFileAssocs(RegistryKey key)
        {
            var fileAssociationsKey = key.OpenSubKey("FileAssociations");
            if (fileAssociationsKey == null)
                return;
            FileAssociationList = fileAssociationsKey
                .GetValueNames()
                .Select(x => Tuple.New(x, key.GetStringValueForAppVManifest(x)))
                .Where(x => x.Second != null)
                .Select(x => new FileAssociation(x.First, x.Second))
                .ToList();
        }
        */

        private static readonly string[] MemberStrings =
        {
            "FileAssociation",
            "MimeAssociation",
            "URLAssociation",
        };

        private void InitAssocs(RegistryKey key)
        {
            foreach (var memberString in MemberStrings)
                InitMember(key, memberString);
        }

        protected override string ClassName
        {
            get { return Wow64 ? "CapabilitiesWow64" : "Capabilities"; }
        }
    }

    public class Assoc : XmlGenerator {}

    public class FileAssociation : Assoc
    {
        //Taken from value name HKML\<Reference.Path*>\FileAssociations\<Extension>
        [SerializeAsAttribute]
        public string Extension;
        //Taken from value HKML\<Reference.Path*>\FileAssociations\<Extension>
        [SerializeAsAttribute]
        public string ProgId;

        public FileAssociation() { }

        public FileAssociation(string ext, string progId)
        {
            Extension = ext;
            ProgId = progId;
        }
    }

    public class MimeAssociation : Assoc
    {
        //Taken from value name HKML\<Reference.Path*>\MIMEAssociations\<Type>
        [SerializeAsAttribute]
        public string Type;
        //Taken from value HKML\<Reference.Path*>\MIMEAssociations\<Type>
        [SerializeAsAttribute]
        public string ProgId;

        public MimeAssociation() { }

        public MimeAssociation(string type, string progId)
        {
            Type = type;
            ProgId = progId;
        }
    }

    public class URLAssociation : Assoc
    {
        //Taken from value name HKML\<Reference.Path*>\FileAssociations\<Extension>
        [SerializeAsAttribute]
        public string Scheme;
        //Taken from value HKML\<Reference.Path*>\FileAssociations\<Extension>
        [SerializeAsAttribute]
        public string ProgId;

        public URLAssociation() { }

        public URLAssociation(string scheme, string progId)
        {
            Scheme = scheme;
            ProgId = progId;
        }
    }
}