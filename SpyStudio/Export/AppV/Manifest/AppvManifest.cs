using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using System.Xml.Serialization;
using File = System.IO.File;
using System.Linq;

namespace SpyStudio.Export.AppV.Manifest
{
    public abstract class SpecialManifestData {}

    public class UrlyGuid : SpecialManifestData
    {
        public Guid Value;

        public static CurlyGuid Create(string g)
        {
            if (g == null || !RegistryTools.GuidRegex.IsMatch(g))
                return null;
            return new CurlyGuid(new Guid(g));
        }
    }

    public class UncurlyGuid : UrlyGuid
    {
        public UncurlyGuid()
        {
            Value = Guid.NewGuid();
        }
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class CurlyGuid : UrlyGuid
    {
        public CurlyGuid()
        {
        }
        public CurlyGuid(Guid g)
        {
            Value = g;
        }
        public override string ToString()
        {
            return "{" + Value.ToString() + "}";
        }
    }

    public class Base64Binary : SpecialManifestData
    {
        public byte[] Value;

        public static Base64Binary Create(byte[] buffer)
        {
            if (buffer == null)
                return null;
            return new Base64Binary(buffer);
        }

        public Base64Binary() { }

        public Base64Binary(byte[] value)
        {
            Value = value;
        }
        public new string ToString()
        {
            return Convert.ToBase64String(Value);
        }
    }

    [XmlInclude(typeof(FileSystemItem))]
    [XmlInclude(typeof(Application))]
    [XmlInclude(typeof(Extension))]
    [XmlInclude(typeof(Include))]
    [XmlInclude(typeof(Font))]
    [XmlInclude(typeof(SoftwareClientsElement))]
    public abstract class ManifestObject : XmlGenerator
    {
        protected readonly HashSet<ManifestObject> References = new HashSet<ManifestObject>();
        protected readonly HashSet<ManifestObject> IsReferencedBy = new HashSet<ManifestObject>();

        protected bool _used;

        public virtual bool Used
        {
            get { return _used; }
            set { _used = value; }
        }

        public void MakeReference(ManifestObject obj)
        {
            if (obj == this)
                return;
            obj.IsReferencedBy.Add(this);
            References.Add(obj);
        }
        public void BreakReference(ManifestObject obj)
        {
            obj.IsReferencedBy.Remove(this);
            References.Remove(obj);
        }

        public void MarkUsed()
        {
            if (Used)
                return;
            Used = true;
            foreach (var manifestObject in IsReferencedBy)
                manifestObject.MarkUsed();
            //foreach (var manifestObject in References)
            //    manifestObject.MarkUsed();
        }

        public virtual IEnumerable<string> Symbols
        {
            get { return Enumerable.Empty<string>(); }
        }

        protected void ConnectObjectRecursiveStep(object Object, MultiMap<string, ManifestObject> symbolTable)
        {
            var type = Object.GetType();
            var fields = type.GetFields();
            foreach (var field in fields)
                ConnectRecursiveStep(field.GetValue(Object), symbolTable);
        }

        public static IEnumerable<string> FindAllPathsInString(string s)
        {
            return AppvPathExtractor.GetAllPathsInString(s);
        }

        protected void ConnectRecursiveStep(object Object, MultiMap<string, ManifestObject> symbolTable)
        {
            if (Object == null)
                return;
            var type = Object.GetType();
            var typeName = type.Name;
            if (type.IsPrimitive || type.IsEnum)
                return;
            const string stringName = "String";
            const string CurlyGuidName = "CurlyGuid";
            const string UncurlyGuidName = "UncurlyGuid";
            const string ListName = "List`1";
            var norm = AppvPathNormalizer.GetInstanceManifestPlusLengthenPath();
            switch (typeName)
            {
                case stringName:
                    bool noPaths = true;
                    var String = (string) Object;
                    foreach (var path in FindAllPathsInString(String))
                    {
                        var unnormalized = norm.Unnormalize(path);
                        var fullPath = FileSystemTools.GetFullPath(unnormalized);
                        var renormalized = norm.Normalize(fullPath);
                        symbolTable.Get(renormalized).ForEach(MakeReference);
                        noPaths = false;
                    }

                    if (noPaths)
                        symbolTable.Get(String).ForEach(MakeReference);
                    break;
                case UncurlyGuidName:
                case CurlyGuidName:
                    {
                        var s = ((UrlyGuid)Object).Value.ToString().ToUpper();
                        var guid = "{" + s + "}";
                        symbolTable.Get(guid).ForEach(MakeReference);
                    }
                    break;
                case ListName:
                    foreach (var item in (IList)Object)
                        ConnectRecursiveStep(item, symbolTable);
                    break;
                default:
                    if (!type.IsClass)
                        return;
                    ConnectObjectRecursiveStep(Object, symbolTable);
                    break;
            }
        }

        public virtual void Connect(MultiMap<string, ManifestObject> symbolTable)
        {
            ConnectRecursiveStep(this, symbolTable);
        }
    }

    //Fixed atttributes:
    //IgnorableNamespaces="appv" xmlns="http://schemas.microsoft.com/appx/2010/manifest" xmlns:appv="http://schemas.microsoft.com/appv/2010/manifest"
    public class Package
    {
        [OmitAppvNamespace]
        public Identity Identity = new Identity();
        [OmitAppvNamespace]
        public Properties Properties = new Properties();
        [OmitAppvNamespace]
        public Resources Resources = new Resources();
        [OmitAppvNamespace]
        public Prerequisites Prerequisites = new Prerequisites();
        //Namespace: appv
        public AssetIntelligence AssetIntelligence;
        public Applications Applications = new Applications();
        public List<FileSystemItem> NonApplications;
        public ExtensionsConfiguration ExtensionsConfiguration = new ExtensionsConfiguration();

        //Namespace: appv
        public List<Extension> Extensions = new List<Extension>();

        //==============//
        private static Application CreateApplication(string path)
        {
            var normalized = Normalizer.Normalize(path);
            return new Application
            {
                Id = normalized,
                Target = normalized,
                VisualElements = VisualElements.Create(normalized),
            };
        }

        private static bool IsExecutable(string path)
        {
            var lower = path.ToLower();
            return lower.EndsWith(".exe")/* ||
                   lower.EndsWith(".dll")*/;
        }

        private static bool IsExe(string path)
        {
            var lower = path.ToLower();
            return lower.EndsWith(".exe");
        }

        private static readonly AppvPathNormalizer Normalizer = AppvPathNormalizer.GetInstanceManifest();

        private static readonly string[] AppPathPaths =
        {
            @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\App Paths",
            @"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\App Paths",
        };

        private static readonly Regex FileExtensionRegex = new Regex(@"^\.[^.]+$");
        //==============//

        public Package() { }

        public Package(List<string> selectedFiles, List<string> selectedDirs, List<string> services)
        {
            PopulateEverything(selectedFiles, selectedDirs, services);
            BuildSymbolTable();
            ConnectEverything();

            Applications.Elements.ForEach(x => x.MarkUsed());
            NonApplications.ForEach(x => x.MarkUsed());
            Extensions = Extensions.Where(x => x.Used).ToList();
            Extensions.ForEach(x => x.DoInternalFiltering());
        }

        #region Populators

        private void PopulateEverything(List<string> selectedFiles, List<string> selectedDirs, List<string> services)
        {
            Extensions.Clear();
            var canonicalSelectedFiles = new HashSet<string>(selectedFiles, new PathComparer());

            foreach (var exec in selectedFiles.Where(IsExecutable))
            {
                var app = CreateApplication(exec);
                Applications.Elements.Add(app);
                string name = null;
                try
                {
                    name = System.Reflection.AssemblyName.GetAssemblyName(exec).ToString();
                }
                catch (BadImageFormatException) {}
                if (name != null)
                    _symbolTable.Add(name, app);
            }

            //Applications.Elements.AddRange(selectedFiles.Where(IsExecutable).Select(x => CreateApplication(x)));
            NonApplications = selectedFiles.Where(x => !IsExecutable(x)).Select(x => new FileSystemItem(x)).ToList();
            NonApplications.AddRange(selectedDirs.Select(x => new FileSystemItem(x)));

            PopulateWithAppPaths(canonicalSelectedFiles);
            PopulateWithShortcuts(selectedFiles);
            var assocs = PopulateWithFileTypeAssociations();
            PopulateWithCOMExtensions();
            PopulateWithInterfaces();
            PopulateWithURLProtocols(assocs);
            PopulateWithApplicationCapabilities();
            PopulateWithSoftwareClients();
            PopulateWithFonts(selectedFiles);
            PopulateWithServices(services);
            PopulateWithAssetIntelligences(selectedFiles);
        }

        private void PopulateWithCOMExtensions()
        {
            var classes = RegistryTools.OpenClassesKey(64);
            classes = classes.OpenSubKeyForRead("CLSID");
            if (classes == null)
                throw new RegKeyNotFoundException();
            RegistryKey classesWow64 = null;
            if (IntPtr.Size > 4)
            {
                classesWow64 = RegistryTools.OpenClassesKey(32);
                classesWow64 = classesWow64.OpenSubKeyForRead("CLSID");
                if (classesWow64 == null)
                    throw new RegKeyNotFoundException();
            }

            var coms = classes.GetSubKeyNames()
                .Where(x => RegistryTools.GuidRegex.IsMatch(x));
            var comsWow64 = Enumerable.Empty<string>();
            if (classesWow64 != null)
                comsWow64 = classesWow64.GetSubKeyNames()
                    .Where(x => RegistryTools.GuidRegex.IsMatch(x));

            var set = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            coms.ForEach(x => set.Add(x));
            comsWow64.ForEach(x => set.Add(x));

            Extensions.AddRange(set.Select(x => (Extension)ComExtension.CreateRegularCom(x)));
            Extensions.AddRange(ComExtension.CreateStandaloneAppIds().Cast<Extension>());
        }

        private void PopulateWithInterfaces()
        {
            var Interface = RegistryTools.OpenClassesKey(64);
            Interface = Interface.OpenSubKeyForRead("Interface");
            if (Interface == null)
                throw new RegKeyNotFoundException();
            RegistryKey InterfaceWow64 = null;
            if (IntPtr.Size > 4)
            {
                InterfaceWow64 = RegistryTools.OpenClassesKey(32);
                InterfaceWow64 = InterfaceWow64.OpenSubKeyForRead("Interface");
                if (InterfaceWow64 == null)
                    throw new RegKeyNotFoundException();
            }

            var Interfaces = Interface.GetSubKeyNames()
                .Where(x => RegistryTools.GuidRegex.IsMatch(x));
            var InterfacesWow64 = Enumerable.Empty<string>();
            if (InterfaceWow64 != null)
                InterfacesWow64 = InterfaceWow64.GetSubKeyNames()
                    .Where(x => RegistryTools.GuidRegex.IsMatch(x));

            var set = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            Interfaces.ForEach(x => set.Add(x));
            InterfacesWow64.ForEach(x => set.Add(x));

            Extensions.AddRange(set.Select(x => (Extension)ComExtension.CreateInterface(x)));
        }

        private IEnumerable<FileTypeAssociation> PopulateWithFileTypeAssociations()
        {
            var extensions = Microsoft.Win32.Registry.ClassesRoot.GetSubKeyNames()
                .Where(x => FileExtensionRegex.IsMatch(x));
            var assocs = extensions
                .Select(x => FileTypeAssociation.Create(x))
                .Where(x => x != null)
                .ToList();
            Extensions.AddRange(assocs.Cast<Extension>());
            return assocs;
        }

        private void PopulateWithShortcuts(IEnumerable<string> selectedFiles)
        {
            var shortcuts =
                FileSystemTools.ScanForShortcuts(selectedFiles.Where(IsExe).ToList(), new WshShell()).SelectMany(x => x);
            var toAdd = shortcuts.Select(x => (Extension) new Shortcut(x)).ToList();
            Extensions.AddRange(toAdd);
        }

        private void PopulateWithAppPaths(HashSet<string> selectedFiles)
        {
            var toAdd = new List<Extension>();
            foreach (var appPathPath in AppPathPaths)
            {
                var key = RegistryTools.GetKeyFromFullPath(appPathPath);
                if (key == null)
                    continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var subkey = key.OpenSubKeyForRead(subKeyName);
                    if (subkey == null)
                        continue;

                    var path = subkey.GetStringValue("");
                    path = FileSystemTools.GetFullPath(path);
                    if (!selectedFiles.Contains(path))
                        continue;

                    toAdd.Add(new AppPath(subkey));
                }
            }
            Extensions.AddRange(toAdd);
        }

        private void PopulateWithURLProtocols(IEnumerable<FileTypeAssociation> associations)
        {
            var toAdd = URLProtocol.Create(associations).Cast<Extension>().ToList();
            Extensions.AddRange(toAdd);
        }

        private void PopulateWithApplicationCapabilities()
        {
            var toAdd = ApplicationCapabilities.Create().Cast<Extension>().ToList();
            Extensions.AddRange(toAdd);
        }

        private static readonly RegistryKey[] ClientKeys =
        {
            RegistryTools.GetKeyFromFullPath(@"HKEY_LOCAL_MACHINE\Software\Clients"),
            RegistryTools.GetKeyFromFullPath(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Clients"),
        };

        private void PopulateWithSoftwareClients()
        {
            if (ClientKeys.Any(x => x == null))
                throw new KeyNotFoundException();

            IEnumerable<RegistryKey> keys = ClientKeys;
            for (int i = 0; i < 2; i++)
                keys = keys
                    .Select(key =>
                        key.GetSubKeyNames()
                            .Select(subkeyname => key.OpenSubKey(subkeyname))
                            .Where(x => x != null)
                            .ToArray())
                    .SelectMany(x => x);

            var softwareClients = new SoftwareClients(keys);
            if (softwareClients.Count != 0)
                Extensions.Add(softwareClients);
        }

        private void PopulateWithFonts(IEnumerable<string> selectedFiles)
        {
            var fonts = new Fonts(selectedFiles);
            Extensions.Add(fonts);
        }

        private void PopulateWithServices(List<string> services)
        {
            if (services.Count == 0)
                return;
            Extensions.Add(new Services(services));
        }

        private void PopulateWithAssetIntelligences(List<string> selectedFiles)
        {
            selectedFiles = selectedFiles.Select(x => FileSystemTools.GetFullPath(x)).ToList();
            AssetIntelligence = new AssetIntelligence(selectedFiles);
        }
        #endregion

        private readonly MultiMap<string, ManifestObject> _symbolTable = new MultiMap<string, ManifestObject>(StringComparer.InvariantCultureIgnoreCase);

        private void BuildSymbolTable()
        {
            Action<ManifestObject> f = x => x.Symbols.ForEach(y => _symbolTable.Add(y, x));
            Applications.Elements.Cast<ManifestObject>().ForEach(f);
            NonApplications.Cast<ManifestObject>().ForEach(f);
            Extensions.Cast<ManifestObject>().ForEach(f);
        }

        private void ConnectEverything()
        {
            foreach (var extension in Extensions)
            {
                extension.Connect(_symbolTable);
                extension.DoInternalConnections(_symbolTable);
            }
        }

        public byte[] GenerateManifestBytes()
        {
            var mem = new MemoryStream();
            {
                var s = @"<?xml version=""1.0"" encoding=""UTF-8""?>" + "\r\n";
                var array = Encoding.UTF8.GetBytes(s).ToArray();
                mem.Write(array, 0, array.Length);
            }
            using (var xml = new XmlTextWriter(mem, Encoding.UTF8)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 1,
                    IndentChar = '\t',
                })
            {
                GenerateManifest(xml);
            }
            return mem.ToArray();
        }

        public string GenerateManifestString()
        {
            var sb = new StringBuilder();
            var s = @"<?xml version=""1.0"" encoding=""UTF-8""?>" + "\r\n";
            sb.Append(s);
            using (var textWriter = new StringWriter(sb))
            using (var xml = new XmlTextWriter(textWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 1,
                IndentChar = '\t',
            })
            {
                GenerateManifest(xml);
            }
            return sb.ToString();
        }

        private void GenerateManifest(XmlTextWriter xml)
        {
            xml.WriteStartElement("Package");

            xml.WriteAttributeString("IgnorableNamespaces", "appv");
            xml.WriteAttributeString("xmlns", "http://schemas.microsoft.com/appx/2010/manifest");
            xml.WriteAttributeString("xmlns:appv", "http://schemas.microsoft.com/appv/2010/manifest");

            //Debugger.Launch();
            Identity.OmitAppv = true;
            Identity.GenerateXml(xml);
            Properties.OmitAppv = true;
            Properties.GenerateXml(xml);
            Resources.OmitAppv = true;
            Resources.GenerateXml(xml);
            Prerequisites.OmitAppv = true;
            Prerequisites.GenerateXml(xml);
            AssetIntelligence.GenerateXml(xml);
            Applications.OmitAppv = true;
            Applications.GenerateXml(xml);

            {
                xml.WriteStartElement("appv:Extensions");
                foreach (var extension in Extensions)
                    extension.GenerateXml(xml);
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }
    }

    #region Misc classes

    public class FileSystemItem : ManifestObject
    {
        public string Path;

        public FileSystemItem() { }
        public FileSystemItem(string s)
        {
            Path = AppvPathNormalizer.GetInstanceManifest().Normalize(s);
        }

        public override IEnumerable<string> Symbols
        {
            get { return Path.ToEnumerable(); }
        }

        public override void Connect(MultiMap<string, ManifestObject> symbolTable)
        {
        }
    }

    public class Identity : XmlGenerator
    {
        //These are all XML attributes.
        [SerializeAsAttribute]
        public string Name = "Reserved";
        [SerializeAsAttribute]
        public string Publisher = "CN=Reserved";

        //This goes in format x.x.x.x
        [SerializeAsAttribute]
        public Version Version = new Version(0, 0, 0, 1);

        [SerializeAsAttribute]
        [OverrideName("appv:PackageId")]
        public UncurlyGuid PackageId = new UncurlyGuid();

        [SerializeAsAttribute]
        [OverrideName("appv:VersionId")]
        public UncurlyGuid VersionId = new UncurlyGuid();
    }

    public class Properties : XmlGenerator
    {
        //These are all XML elements.
        [OmitAppvNamespace]
        public string DisplayName = "mytest";
        [OmitAppvNamespace]
        public string PublisherDisplayName = "Reserved";
        [OmitAppvNamespace]
        public string Description = "Reserved";
        [OmitAppvNamespace]
        public string Logo = "logo.png";
        public string AppVPackageDescription = "No description entered";

        protected override string ClassName
        {
            get { return "Properties"; }
        }
    }

    public class Resources : XmlGenerator
    {
        public string Language = "en-us";

        public override void GenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("Resources");
            xml.WriteStartElement("Resource");
            xml.WriteAttributeString("Language", CultureInfo.CurrentCulture.Name.ToLower());
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
    }

    public class Prerequisites : XmlGenerator
    {
        [OmitAppvNamespace]
        public string OSMinVersion = "6.1";
        [OmitAppvNamespace]
        public string OSMaxVersionTested = "6.1";

        public TargetOSes TargetOSes = new TargetOSes();

        protected override string ClassName
        {
            get { return "Prerequisites"; }
        }
    }

    public enum ProcessorArchitecture
    {
        x86,
        x64,
    }

    public class TargetOSes : XmlGenerator
    {
        [OmitAppvNamespace]
        [SerializeAsAttribute]
        public ProcessorArchitecture SequencingStationProcessorArchitecture = IntPtr.Size == 4 ? ProcessorArchitecture.x86 : ProcessorArchitecture.x64;
    }

    //Fixed attribute: xmlns="http://schemas.microsoft.com/appv/2010/manifest"
    public class Applications : XmlGenerator
    {
        [SerializeAsAttribute]
        public string xmlns = "http://schemas.microsoft.com/appv/2010/manifest";
        //List is non-node.
        [OpenList]
        public List<Application> Elements = new List<Application>();
    }

    public enum ApplicationOrigin
    {
        Application,
        User,
    }

    public class Application : ManifestObject
    {
        /*
         * Notes:
         *
         * Id appears to be always equal to Target.
         * 
         */

        //XML attributes:
        //App-V-normalized path.
        [SerializeAsAttribute]
        public string Id;
        [SerializeAsAttribute]
        public ApplicationOrigin Origin = ApplicationOrigin.Application;
        //Serialized as {false|true}.
        [SerializeAsAttribute]
        public bool TargetInPackage = true;

        //XML Elements
        //App-V-normalized path.
        [OmitAppvNamespace]
        public string Target;
        [OmitAppvNamespace]
        public VisualElements VisualElements;

        public Application()
        {
            OmitAppv = true;
        }

        public override IEnumerable<string> Symbols
        {
            get { return Id.ToEnumerable(); }
        }

        public override void Connect(MultiMap<string, ManifestObject> symbolTable)
        {
        }

        protected override string ClassName
        {
            get { return "Application"; }
        }
    }

    public class VisualElements : XmlGenerator
    {
        [OmitAppvNamespace]
        public string Name;
        [OmitAppvNamespace]
        public Version Version;

        public static VisualElements Create(string path)
        {
            if (!File.Exists(path))
                return null;
            var vi = FileVersionInfo.GetVersionInfo(path);
            Version v = null;
            if (vi.FileMajorPart != 0 || vi.FileMinorPart != 0 || vi.FileBuildPart != 0 || vi.FilePrivatePart != 0)
                v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
            if (vi.ProductName == null && v == null)
                return null;
            return new VisualElements
                       {
                           Name = vi.ProductName,
                           Version = v,
                       };
        }

        protected override string ClassName
        {
            get { return "Application"; }
        }
    }

    public class ExtensionsConfiguration
    {
    }

    [XmlInclude(typeof(ApplicationCapabilities))]
    [XmlInclude(typeof(AppPath))]
    [XmlInclude(typeof(ComExtension))]
    [XmlInclude(typeof(FileTypeAssociation))]
    [XmlInclude(typeof(Objects))]
    [XmlInclude(typeof(EnvironmentVariables))]
    [XmlInclude(typeof(Shortcut))]
    [XmlInclude(typeof(URLProtocol))]
    [XmlInclude(typeof(Fonts))]
    [XmlInclude(typeof(Services))]
    [XmlInclude(typeof(SoftwareClients))]
    public abstract class Extension : ManifestObject
    {
        //XML Attribute.
        [SerializeAsAttribute]
        public abstract string Category { get; }
        //Subclass goes in subelement. The name of the element is the name of
        //the class. The element is in the appv namespace.

        public virtual void DoInternalConnections(MultiMap<string, ManifestObject> symbolTable)
        {
        }

        public virtual void DoInternalFiltering()
        {
        }

        protected abstract void InternalGenerateXml(XmlTextWriter xml);

        public override void GenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:Extension");

            xml.WriteAttributeString("Category", Category);

            InternalGenerateXml(xml);

            xml.WriteEndElement();
        }
    }

    public class Verb : XmlGenerator
    {
        //Taken from key <Passed key>\<Index>
        public int Index;
        //Taken from default value <Passed key>\<Index>
        public string Value;

        public Verb() { }

        public Verb(RegistryKey key)
        {
            Index = Convert.ToInt32(key.GetKeyName());
            Value = key.GetStringValueForAppVManifest("");
        }
    }

    public class XmlGenerator
    {
        public virtual void GenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement((!OmitAppv ? "appv:" : "") + ClassName);

            GenerateInnerXml(xml);

            xml.WriteEndElement();
        }

        [IgnoreField]
        public bool OmitAppv = false;

        protected virtual void GenerateInnerXml(XmlTextWriter xml)
        {
            XmlTools.SerializeAppvManifestObject(this, xml);
        }

        protected virtual string ClassName
        {
            get { return GetType().Name; }
        }
    }
    #endregion

    #region Unknown classes
    //Unknown.
    public class Objects : Extension
    {
        public override string Category { get { return "AppV.Objects"; } }

        public List<Object> List = new List<Object>();

        public override IEnumerable<string> Symbols
        {
            get { return Enumerable.Empty<string>(); }
        }
        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            throw new NotImplementedException();
        }
    }

    //Unknown.
    public class Object
    {
        public NotIsolate NotIsolate;
        public Isolate Isolate;
    }

    //Unknown.
    public class NotIsolate
    {
        public string Name;
    }

    //Unknown.
    public class Isolate
    {
        public string Name;
    }

    //Unknown.
    public class EnvironmentVariables : Extension
    {
        public override string Category { get { return "AppV.EnvironmentVariables"; } }
        public List<Include> List = new List<Include>();

        public override IEnumerable<string> Symbols
        {
            get { return Enumerable.Empty<string>(); }
        }

        public override bool Used
        {
            get { return List.Any(x => x.Used); }
            set { List.ForEach(x => x.Used = value); }
        }

        public override void DoInternalConnections(MultiMap<string, ManifestObject> symbolTable)
        {
            List.ForEach(x => x.Connect(symbolTable));
        }

        public override void DoInternalFiltering()
        {
            List = List.Where(x => x.Used).ToList();
        }
        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            throw new NotImplementedException();
        }
    }

    //Unknown.
    public class Include : ManifestObject
    {
        public List<Variable> List = new List<Variable>();
    }

    //Unknown.
    public class Variable
    {
        public string Name,
                      Value;
    }
    #endregion

}
