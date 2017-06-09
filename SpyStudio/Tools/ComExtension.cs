using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Extensions;
using SpyStudio.Tools.Registry;
using System.Xml.Serialization;

namespace SpyStudio.Tools
{
//    public class ComExtension : Extension
//    {
//        public override string Category
//        {
//            get { return "AppV.COM"; }
//        }

//        public List<ComExtensionSubElement> SubElements = new List<ComExtensionSubElement>();

//        public static ComExtension CreateRegularCom(string classId)
//        {
//            var ret = new ComExtension();
//            ret.InitX86X64(classId);
//            ret.InitAppIds();
//            ret.InitProgIds();
//            ret.InitTypeLibraries();
//            return ret;
//        }

//        public static IEnumerable<ComExtension> CreateStandaloneAppIds()
//        {
//            var map = new Dictionary<string, RegistryKey>(StringComparer.InvariantCultureIgnoreCase);
//            for (var bitness = 64; bitness >= 32; bitness /= 2)
//            {
//                var appIdsKey = RegistryTools.OpenClassesKey(bitness);
//                if (appIdsKey == null)
//                    throw new KeyNotFoundException();
//                appIdsKey = appIdsKey.OpenSubKeyForRead("AppID");
//                if (appIdsKey == null)
//                    throw new KeyNotFoundException();

//                appIdsKey.GetSubKeyNames()
//                    .Where(x => !map.ContainsKey(x))
//                    .Select(x => appIdsKey.OpenSubKeyForRead(x))
//                    .Where(x => x != null)
//                    .ForEach(x => map[x.GetKeyName()] = x);
//            }

//            return map.Values.Select(x => CreateStandaloneAppId(x)).Where(x => x != null);
//        }

//        public static ComExtension CreateInterface(string classId)
//        {
//            var ret = new ComExtension();
//            ret.InitInterface(classId);
//            return ret;
//        }

//        private static ComExtension CreateStandaloneAppId(RegistryKey key)
//        {
//            var ret = new ComExtension();
//            ret.SubElements.Add(ComExtensionAppIdsElement.CreateForStandaloneElement(key));
//            return ret;
//        }

//        private void InitX86X64(string classId)
//        {
//            var coms = new List<ComExtensionPlatformSpecificSubElement>();
//            if (IntPtr.Size == 8)
//            {
//                var com = ComExtensionPlatformSpecificSubElement.Create(classId, 32);
//                if (com != null)
//                {
//                    SubElements.Add(com);
//                    coms.Add(com);
//                }
//            }
//            {
//                var com = ComExtensionPlatformSpecificSubElement.Create(classId, 64);
//                if (com != null)
//                {
//                    SubElements.Add(com);
//                    coms.Add(com);
//                }
//            }
//            var tuples = coms
//                .Where(com => com.Class != null && com.Class.ProgId != null)
//                .Select(com => new Tuple<RegistryKey, CurlyGuid>(RegistryTools.GetKeyFromFullPath(RegistryTools.GetClassesPath(com.Bitness) + "\\" + com.Class.ProgId), com.Class.ClassId))
//                .Where(x => x.Item1 != null)
//                .ToArray();
//            if (tuples.Length > 0)
//            {
//                var progIds = new ComExtensionProgIdsSubElement(tuples);
//                if (progIds.Count > 0)
//                    SubElements.Add(progIds);
//            }
//        }

//        private void InitInterface(string classId)
//        {
//            var interfaces = new List<ComExtensionPlatformSpecificSubElement>();
//            if (IntPtr.Size == 8)
//            {
//                var @interface = ComExtensionPlatformSpecificSubElement.CreateInterface(classId, 32);
//                if (@interface != null)
//                {
//                    SubElements.Add(@interface);
//                    interfaces.Add(@interface);
//                }
//            }
//            {
//                var @interface = ComExtensionPlatformSpecificSubElement.CreateInterface(classId, 64);
//                if (@interface != null)
//                {
//                    SubElements.Add(@interface);
//                    interfaces.Add(@interface);
//                }
//            }
//            var libs = new ComExtensionTypeLibrariesSubElement();
//            var set = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
//            foreach (var @interface in interfaces)
//            {
//                foreach (var interface2 in @interface.Interfaces)
//                {
//                    if (interface2.TypeLibrary == null)
//                        continue;
//                    var typeLibsKey =
//                        RegistryTools.GetKeyFromFullPath(RegistryTools.GetClassesPath(@interface.Bitness) + "\\TypeLib");
//                    var guid = interface2.TypeLibrary.TypeLibraryId.ToString();
//                    if (set.Contains(guid))
//                        continue;
//                    var key = typeLibsKey.OpenSubKey(guid);
//                    if (key == null)
//                        continue;
//                    set.Add(guid);
//                    libs.List.AddRange(TypeLibrary.CreateFromTypeLibSubKey(key));
//                }
//            }
//            if (libs.Count > 0)
//                SubElements.Add(libs);
//        }

//        private void InitAppIds()
//        {
//            var guids = SubElements
//                .Select(x => x as ComExtensionPlatformSpecificSubElement)
//                .Where(x => x != null && x.Class != null && x.Class.AppId != null)
//                .Select(x => x.Class.AppId.Value);
//            var appIdsKeys = new[]
//                                 {
//                                     RegistryTools.GetKeyFromFullPath(RegistryTools.GetClassesPath(64) + @"\AppID"),
//                                     RegistryTools.GetKeyFromFullPath(RegistryTools.GetClassesPath(32) + @"\AppID"),
//                                 }.Where(x => x != null).ToList();
//            var keys = new List<RegistryKey>();
//            foreach (var guid in guids)
//            {
//                foreach (var appIdsKey in appIdsKeys)
//                {
//                    var key = new[]
//                                  {
//                                      "{" + guid + "}",
//                                      guid.ToString(),
//                                  }
//                        .Select(x => appIdsKey.OpenSubKeyForRead(x))
//                        .FirstOrDefault(x => x != null);
//                    if (key == null)
//                        continue;
//                    keys.Add(key);
//                    break;
//                }
//            }

//            var appIds = ComExtensionAppIdsElement.CreateForRegularElement(keys);
//            if (appIds.Count != 0)
//                SubElements.Add(appIds);
//        }

//        private void InitProgIds()
//        {
//            var coms = SubElements
//                .Select(x => x as ComExtensionPlatformSpecificSubElement)
//                .Where(x => x != null && x.Class != null && x.Class.ClassId != null && x.Class.ProgId != null);

//            var tuples = coms
//                .Select(com => new Tuple<RegistryKey, CurlyGuid>(RegistryTools.GetKeyFromFullPath(RegistryTools.GetClassesPath(com.Bitness) + com.Class.ProgId), com.Class.ClassId))
//                .Where(x => x.Item1 != null);

//            var progIds = new ComExtensionProgIdsSubElement(tuples);
//            if (progIds.Count != 0)
//                SubElements.Add(progIds);
//        }

//        private void InitTypeLibraries()
//        {
//            var tuples = SubElements
//                .Select(x => x as ComExtensionPlatformSpecificSubElement)
//                .Where(x => x != null && x.Class != null && x.Class.TypeLibraryId != null)
//                .Select(x => new Tuple<Guid, ComExtensionPlatformSpecificSubElement>(x.Class.TypeLibraryId.Value, x))
//                .ToList();

//            var typeLibraries = new ComExtensionTypeLibrariesSubElement(tuples);
//            if (typeLibraries.Count != 0)
//                SubElements.Add(typeLibraries);
//        }

//        public override IEnumerable<string> Symbols
//        {
//            get
//            {
//                foreach (var comExtensionSubElement in SubElements)
//                {
//                    foreach (var symbol in comExtensionSubElement.Symbols)
//                        yield return symbol;

//                    var platform = comExtensionSubElement as ComExtensionPlatformSpecificSubElement;
//                    if (platform == null || platform.Class == null || platform.Class.InProcServer == null)
//                        continue;
//                    if (platform.Class.InProcServer.Class != null)
//                        yield return platform.Class.InProcServer.Class;
//                    if (platform.Class.InProcServer.Assembly != null)
//                        yield return platform.Class.InProcServer.Assembly;
//                }
//            }
//        }

//        protected override void InternalGenerateXml(XmlTextWriter xml)
//        {
//            xml.WriteStartElement("appv:COM");

//            SubElements.ForEach(x => x.GenerateXml(xml));

//            xml.WriteEndElement();
//        }
//    }

//    [XmlInclude(typeof(ComExtensionPlatformSpecificSubElement))]
//    [XmlInclude(typeof(ComExtensionAppIdsElement))]
//    [XmlInclude(typeof(ComExtensionProgIdsSubElement))]
//    [XmlInclude(typeof(ComExtensionTypeLibrariesSubElement))]
//    public abstract class ComExtensionSubElement : XmlGenerator
//    {
//        public abstract IEnumerable<string> Symbols { get; }
//    }

//    public class ComExtensionPlatformSpecificSubElement : ComExtensionSubElement
//    {
//        public int Bitness;
//        public ComExtensionClass Class;
//        public readonly List<ComExtensionInterface> Interfaces = new List<ComExtensionInterface>();

//        public static ComExtensionPlatformSpecificSubElement Create(string classId, int bitness)
//        {
//            var basePath = RegistryTools.GetClassesPath(bitness);
//            var classesPath = basePath + @"\CLSID";
//            var interfacesPath = basePath + @"\Interface";
//            var typeLibsPath = basePath + @"\TypeLib";

//            var classKey = RegistryTools.GetKeyFromFullPath(classesPath + "\\" + classId);
//            var interfacesKey = RegistryTools.GetKeyFromFullPath(interfacesPath);
//            var typeLibsKey = RegistryTools.GetKeyFromFullPath(typeLibsPath);
//            if (classKey == null || interfacesKey == null)
//                return null;

//            return new ComExtensionPlatformSpecificSubElement(classKey, interfacesKey, typeLibsKey, bitness);
//        }

//        public static ComExtensionPlatformSpecificSubElement CreateInterface(string classId, int bitness)
//        {
//            var basePath = RegistryTools.GetClassesPath(bitness);
//            //var classesPath = basePath + @"\CLSID";
//            var interfacesPath = basePath + @"\Interface";
//            var typeLibsPath = basePath + @"\TypeLib";

//            var interfaceKey = RegistryTools.GetKeyFromFullPath(interfacesPath + "\\" + classId);
//            var typeLibsKey = RegistryTools.GetKeyFromFullPath(typeLibsPath);
//            if (interfaceKey == null || typeLibsKey == null)
//                return null;

//            return new ComExtensionPlatformSpecificSubElement(interfaceKey, typeLibsKey, bitness);
//        }

//        public ComExtensionPlatformSpecificSubElement() { }

//        public ComExtensionPlatformSpecificSubElement(RegistryKey classKey, RegistryKey interfacesKey, RegistryKey typeLibsKey, int bitness)
//        {
//            Bitness = bitness;
//            Class = new ComExtensionClass(classKey);
//            if (interfacesKey != null)
//            {
//                foreach (var guid in Class.Interfaces.Select(x => x.InterfaceId.ToString()))
//                {
//                    var interfaceKey = interfacesKey.OpenSubKeyForRead(guid);
//                    if (interfaceKey == null)
//                        continue;
//                    Interfaces.Add(new ComExtensionInterface(interfaceKey, typeLibsKey));
//                }
//            }
//        }

//        public ComExtensionPlatformSpecificSubElement(RegistryKey interfaceKey, RegistryKey typeLibsKey, int bitness)
//        {
//            Bitness = bitness;
//            Interfaces.Add(new ComExtensionInterface(interfaceKey, typeLibsKey));
//        }

//        public override IEnumerable<string> Symbols
//        {
//            get
//            {
//                if (Class != null)
//                    yield return Class.ClassId.ToString();
//                foreach (var Interface in Interfaces)
//                    yield return Interface.InterfaceId.ToString();
//            }
//        }

//        public override void GenerateXml(XmlTextWriter xml)
//        {
//            xml.WriteStartElement("appv:x" + (Bitness == 32 ? "86" : "64"));

//            if (Class != null)
//                Class.GenerateXml(xml);

//            if (Interfaces.Count > 0)
//            {
//                xml.WriteStartElement("appv:Interfaces");
//                Interfaces.ForEach(x => x.GenerateXml(xml));
//                xml.WriteEndElement();
//            }

//            xml.WriteEndElement();
//        }
//    }
//    public class IntSerializableField : ManifestSerializationAttribute
//    {
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.ToInt = true;
//        }
//    }
//    public class IgnoreField : ManifestSerializationAttribute
//    {
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.Ignore = true;
//        }
//    }
//    public class OmitAppvNamespace : ManifestSerializationAttribute
//    {
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.OmitAppvNamespace = true;
//        }
//    }
//    public class OmitZeroField : ManifestSerializationAttribute
//    {
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.OmitZero = true;
//        }
//    }
//    public class SerializeAsAttribute : ManifestSerializationAttribute
//    {
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.AsAttribute = true;
//        }
//    }
//    public class OpenList : ManifestSerializationAttribute
//    {
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.OpenList = true;
//        }
//    }
//    public class OverrideName : ManifestSerializationAttribute
//    {
//        public readonly string NewName;
//        public OverrideName(string name)
//        {
//            NewName = name;
//        }
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.NodeName = NewName;
//        }
//    }
//    public class ListElementName : ManifestSerializationAttribute
//    {
//        public readonly string Name;
//        public ListElementName(string name)
//        {
//            Name = name;
//        }
//        public override void SetProperties(AttributeProperties props)
//        {
//            props.ElementName = Name;
//        }
//    }

//    /*
//     * Note: To maximize everyone's sanity, let's use this convention:
//     * * HKCR32 refers to HKEY_CLASSES_ROOT under x86 and to HKEY_CLASSES_ROOT\Wow6432Node
//     *   under x86-64.
//     * * HKCR64 refers to HKEY_CLASSES_ROOT under x86-64.
//     * * HKCRx refers to one or the other, depending on the final type of the
//     *   ComExtensionSubElement instance.
//     */

//    public class ComExtensionClass : XmlGenerator
//    {
//        //These are all XML elements, and are all in the appv namespace.

//        //Taken from key HKCRx\CLSID\<ClassId>
//        public CurlyGuid ClassId,
//                         //Taken from value of HKCRx\CLSID\<ClassId>\AppId
//                         AppId,
//                         //Taken from default value of HKCRx\CLSID\<ClassId>\TypeLib
//                         TypeLibraryId,
//                         //Taken from value of HKCRx\CLSID\<ClassId>\AutoTreatAs
//                         AutoTreatAs,
//                         //Taken from value of HKCRx\CLSID\<ClassId>\TreatAs
//                         TreatAs,
//                         //Taken from value of HKCRx\CLSID\<ClassId>\AutoConvertTo
//                         AutoConvertTo;

//        //Taken from default value of HKCRx\CLSID\<ClassId>
//        public string Name,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\ProgID
//                      ProgId,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\VersionIndependentProgID
//                      VersionIndependentProgId,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\InProcHandler32
//                      InProcHandler,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\AuxUserType\2
//                      ShortDisplayName,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\AuxUserType\3
//                      ApplicationName,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\DefaultIcon
//                      DefaultIcon,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\DefaultExtension
//                      DefaultExtension,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\ToolboxBitmap32
//                      ToolboxBitmap32,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\Version
//                      Version,
//                      //Taken from value HKCRx\CLSID\<ClassId>\DisplayName
//                      DisplayName;

//        //Requires key HKCRx\CLSID\<ClassId>\InprocServer32
//        public InProcServer InProcServer;
//        //Requires key HKCRx\CLSID\<ClassId>\LocalServer32
//        public LocalServer LocalServer;
//        //Requires key HKCRx\CLSID\<ClassId>\DataFormats
//        public DataFormat DataFormat;
//        //True iff key HKCRx\CLSID\<ClassId>\Insertable exists.
//        public bool? InsertableObject,
//                     //True iff key HKCRx\CLSID\<ClassId>\Control exists.
//                     ActiveXControl;
//        //Taken from value of HKCRx\CLSID\<ClassId>\DisableLowIlProcessIsolation
//        //Cast DWORD value to bool.
//        [IntSerializableField]
//        public bool? DisableLowIlProcessIsolation;
//        //Requires key HKCRx\CLSID\<ClassId>\MiscStatus
//        public MiscStatus MiscStatus;
//        //Requires key HKCRx\CLSID\<ClassId>\verb
//        //Pass this key path to each constructor.
//        public List<Verb> Verbs;
//        //Requires key HKCRx\CLSID\<ClassId>\Conversion
//        public Conversion Conversion;

//        public class Interface : XmlGenerator
//        {
//            //Taken from key HKCRx\CLSID\<ClassId>\Interface\<InterfaceId>
//            public CurlyGuid InterfaceId;
//            //Taken from default value of HKCRx\CLSID\<ClassId>\Interface\<InterfaceId>
//            public string Name;

//            public Interface() { }

//            public Interface(RegistryKey key)
//            {
//                InterfaceId = CurlyGuid.Create(key.GetKeyName());
//                Name = key.GetStringValueForAppVManifest("");
//            }
//        }
//        //Requires key HKCRx\CLSID\<ClassId>\Interface
//        public List<Interface> Interfaces = new List<Interface>();

//        private static readonly string[] GuidStrings = new[]
//                                                           {
//                                                               "AppId",
//                                                               "AutoTreatAs",
//                                                               "TreatAs",
//                                                               "AutoConvertTo",
//                                                           };
//        private static readonly string[] StringStrings = new[]
//                                                           {
//                                                               "ProgId", null,
//                                                               "VersionIndependentProgId", null,
//                                                               "InProcHandler", "InProcHandler32",
//                                                               "DefaultIcon", null,
//                                                               "DefaultExtension", null,
//                                                               "ToolboxBitmap32", null,
//                                                               "Version", null,
//                                                           };
//        private static readonly string[] ObjectStrings = new[]
//                                                           {
//                                                               "InProcServer", "InprocServer32",
//                                                               "LocalServer", "LocalServer32",
//                                                               "DataFormat", null,
//                                                               "MiscStatus", null,
//                                                               "Conversion", null,
//                                                           };

//        public ComExtensionClass() { }

//        public ComExtensionClass(RegistryKey key)
//        {
//            ClassId = CurlyGuid.Create(key.GetKeyName());
//            var type = GetType();
//            InitGuids(key, type);
//            InitStrings(key, type);
//            InitBools(key);
//            InitVerbs(key);
//            InitObjects(key, type);
//            InitInterfaces(key);
//            InitConversion(key);
//        }

//        private void InitGuids(RegistryKey key, Type type)
//        {
//            TypeLibraryId = CurlyGuid.Create(key.GetDefaultStringValueOfSubKeyForAppVManifest("TypeLib"));

//            foreach (var guidString in GuidStrings)
//            {
//                var value = key.GetStringValueForAppVManifest(guidString);
//                if (value == null)
//                    continue;
//                var fieldInfo = type.GetField(guidString);
//                Debug.Assert(fieldInfo != null);
//                fieldInfo.SetValue(this, CurlyGuid.Create(value));
//            }
//        }

//        private void InitStrings(RegistryKey key, Type type)
//        {
//            Name = key.GetStringValueForAppVManifest("");
//            DisplayName = key.GetStringValueForAppVManifest("DisplayName");

//            var auxUserTypeKey = key.OpenSubKeyForRead("AuxUserType");
//            if (auxUserTypeKey != null)
//            {
//                ShortDisplayName = auxUserTypeKey.GetDefaultStringValueOfSubKeyForAppVManifest("2");
//                ApplicationName = auxUserTypeKey.GetDefaultStringValueOfSubKeyForAppVManifest("3");
//            }

//            for (var index = 0; index < StringStrings.Length; index += 2)
//            {
//                var dst = StringStrings[index];
//                var src = StringStrings[index + 1];
//                var fieldInfo = type.GetField(dst);
//                Debug.Assert(fieldInfo != null);
//                fieldInfo.SetValue(this, key.GetDefaultStringValueOfSubKeyForAppVManifest(src ?? dst));
//            }
//        }

//        private void InitBools(RegistryKey key)
//        {
//            InsertableObject = key.ValueExists("Insertable");
//            ActiveXControl = key.ValueExists("Control");
//            int value;
//            key.GetIntValue("DisableLowIlProcessIsolation", out value);
//            DisableLowIlProcessIsolation = 0 != value;
//        }

//        private void InitVerbs(RegistryKey key)
//        {
//            Verbs = RegistryTools.OpenVerbSubKeys(key)
//                .Select(subkey => new Verb(subkey))
//                .ToList();
//        }
        
//        private static readonly Type[] TypeArr = { typeof(RegistryKey) };

//        private void InitObjects(RegistryKey key, Type type)
//        {
//            for (var index = 0; index < ObjectStrings.Length; index += 2)
//            {
//                var dst = ObjectStrings[index];
//                var src = ObjectStrings[index + 1];
//                var subkey = key.OpenSubKeyForRead(src ?? dst);
//                if (subkey == null)
//                    continue;
//                var fieldInfo = type.GetField(dst);
//                var klass = System.Reflection.Assembly.GetCallingAssembly().GetType(GetType().Namespace + "." + dst);
//                Debug.Assert(fieldInfo != null && klass != null);
//                var ctor = klass.GetConstructor(TypeArr);
//                Debug.Assert(ctor != null);
//                var newInstance = ctor.Invoke(new object[] { subkey });
//                fieldInfo.SetValue(this, newInstance);
//            }
//        }

//        private void InitInterfaces(RegistryKey key)
//        {
//            var interfaceKey = key.OpenSubKeyForRead("Interface");
//            if (interfaceKey == null)
//                return;

//            Interfaces = interfaceKey.GetSubKeyNames()
//                .Select(x => interfaceKey.OpenSubKeyForRead(x))
//                .Where(x => x != null)
//                .Select(x => new Interface(x))
//                .ToList();
//        }

//        private void InitConversion(RegistryKey key)
//        {
//            var conversionKey = key.OpenSubKeyForRead("Conversion");
//            if (conversionKey == null)
//                return;

//            Conversion = new Conversion(conversionKey);
//        }
//    }

//    public enum ThreadingModel
//    {
//        Neutral,
//        Free,
//        Apartment,
//        Both,
//    }

//    public class InProcServer : XmlGenerator
//    {
//        //These are all XML elements, and are all in the appv namespace.

//        //App-V-normalized path.
//        //Taken from default value of HKCRx\CLSID\<ClassId>\InprocServer32
//        public string Library,
//            //Taken from value HKCRx\CLSID\<ClassId>\InprocServer32\Class
//            Class,
//            //Taken from value HKCRx\CLSID\<ClassId>\InprocServer32\Assembly
//            Assembly,
//            //Taken from value HKCRx\CLSID\<ClassId>\InprocServer32\RuntimeVersion
//            RuntimeVersion;
//        //Taken from value HKCRx\CLSID\<ClassId>\InprocServer32\ThreadingModel
//        public ThreadingModel ThreadingModel;

//        public InProcServer() { }

//        public InProcServer(RegistryKey key)
//        {
//            Library = key.GetStringValueForAppVManifest("");
//            Class = key.GetStringValueForAppVManifest("Class");
//            Assembly = key.GetStringValueForAppVManifest("Assembly");
//            RuntimeVersion = key.GetStringValueForAppVManifest("RuntimeVersion");
//            var threadingModel = key.GetStringValueForAppVManifest("ThreadingModel");
//            if (!string.IsNullOrEmpty(threadingModel))
//            {
//                threadingModel = threadingModel.Substring(0, 1).ToUpper() + threadingModel.Substring(1).ToLower();
//                try
//                {
//                    ThreadingModel = (ThreadingModel) Enum.Parse(typeof (ThreadingModel), threadingModel);
//                }
//                catch
//                {
//                }
//            }
//        }
//    }

//    public class LocalServer : XmlGenerator
//    {
//        //XML element in the appv namespace.
//        //Taken from default value of HKCRx\CLSID\<ClassId>\LocalServer32
//        public string CommandLine;

//        public LocalServer() { }

//        public LocalServer(RegistryKey key)
//        {
//            CommandLine = key.GetStringValueForAppVManifest("");
//        }
//    }

//    public class DataFormat : XmlGenerator
//    {
//        //These are all XML elements, and are all in the appv namespace.

//        //Taken from default value of HKCRx\CLSID\<ClassId>\DataFormats\DefaultFile
//        public string Default;
//        public class Format : XmlGenerator
//        {
//            //Taken from key HKCRx\CLSID\<ClassId>\DataFormats\<Index>
//            public int Index;
//            //Taken from default value of HKCRx\CLSID\<ClassId>\DataFormats\<Index>
//            public string Value;

//            public Format() { }

//            public Format(RegistryKey key)
//            {
//                Index = Convert.ToInt32(key.GetKeyName());
//                Value = key.GetStringValueForAppVManifest("");
//            }
//        }
//        //Requires key HKCRx\CLSID\<ClassId>\DataFormats\GetSet
//        public List<Format> Formats;

//        public DataFormat() { }

//        public DataFormat(RegistryKey key)
//        {
//            Default = key.GetDefaultStringValueOfSubKeyForAppVManifest("DefaultFile");
//            var getSetKey = key.OpenSubKeyForRead("GetSet");
//            if (getSetKey != null)
//            {
//                Formats = new List<Format>();
//                foreach (var subKeyName in getSetKey.GetSubKeyNames())
//                {
//                    var subKey = getSetKey.OpenSubKeyForRead(subKeyName);
//                    if (subKey == null)
//                        continue;
//                    Format f;
//                    try
//                    {
//                        f = new Format(subKey);
//                    }
//                    catch
//                    {
//                        continue;
//                    }
//                    Formats.Add(f);
//                }
//                if (Formats.Count == 0)
//                    Formats = null;
//            }
//        }

//        protected override void GenerateInnerXml(XmlTextWriter xml)
//        {
//            xml.WriteElementString("appv:Default", Default);
//            if (Formats == null)
//                return;
//            xml.WriteStartElement("appv:Formats");
//            Formats.ForEach(x => x.GenerateXml(xml));
//            xml.WriteEndElement();
//        }
//    }

//    public class MiscStatus : XmlGenerator
//    {
//        //These are all XML elements, and are all in the appv namespace.

//        //Taken from default value of HKCRx\CLSID\<ClassId>\MiscStatus
//        public string Default;
//        public class Aspect : XmlGenerator
//        {
//            //Unknown.
//            //XML name: "Aspect".
//            public string Name;
//            //Taken from key of HKCRx\CLSID\<ClassId>\MiscStatus\<AspectValue>
//            public int AspectValue,
//                       //Taken from default value of HKCRx\CLSID\<ClassId>\<AspectValue>
//                       OleMisc;

//            public Aspect() { }

//            public Aspect(RegistryKey key)
//            {
//                AspectValue = Convert.ToInt32(key.GetKeyName());
//                key.GetIntValue("", out OleMisc);
//            }
//        }
//        //Requires key HKCRx\CLSID\<ClassId>\MiscStatus\#
//        public readonly List<Aspect> AspectList;

//        public MiscStatus() { }

//        public MiscStatus(RegistryKey key)
//        {
//            Default = key.GetKeyName();
//            AspectList = key.GetSubKeyNames()
//                .Where(x => x.All(char.IsDigit))
//                .Select(x => key.OpenSubKeyForRead(x))
//                .Where(x => x != null)
//                .Select(x => new Aspect(x))
//                .ToList();
//            if (AspectList.Count == 0)
//                AspectList = null;
//        }

//        protected override void GenerateInnerXml(XmlTextWriter xml)
//        {
//            xml.WriteElementString("appv:Default", Default);
//            if (AspectList != null)
//            {
//                xml.WriteStartElement("appv:AspecList");
//                AspectList.ForEach(x => x.GenerateXml(xml));
//                xml.WriteEndElement();
//            }
//        }
//    }

//    public class Conversion : XmlGenerator
//    {
//        //Taken from default value of HKCRx\CLSID\<ClassId>\Conversion\Readable\Main
//        public string Readable,
//                      //Taken from default value of HKCRx\CLSID\<ClassId>\Conversion\ReadWritable\Main
//                      ReadWritable;

//        public Conversion() { }

//        public Conversion(RegistryKey key)
//        {
//            var readableKey = key.OpenSubKeyForRead("Readable");
//            if (readableKey != null)
//                Readable = readableKey.GetDefaultStringValueOfSubKeyForAppVManifest("Main");
//            var readWritable = key.OpenSubKeyForRead("ReadWritable");
//            if (readWritable != null)
//                ReadWritable = readWritable.GetDefaultStringValueOfSubKeyForAppVManifest("Main");
//        }
//    }

//    public class ComExtensionInterface : XmlGenerator
//    {
//        //Taken from key HKCRx\Interface\<InterfaceId>
//        public CurlyGuid InterfaceId,
//                         //Taken from default value of HKCRx\Interface\<InterfaceId>\ProxyStubClassId
//                         ProxyStubClassId;
//        //Taken from default value of HKCRx\Interface\<InterfaceId>
//        public string Description,
//                      //Taken from value HKCRx\Interface\<InterfaceId>\BaseInterface
//                      BaseInterface;
//        //Taken from default value of HKCRx\Interface\<InterfaceId>\NumMethods
//        public int NumMethods;
//        //Requires key HKCRx\Interface\<InterfaceId>\TypeLib
//        public ComExtensionInterfaceTypeLibrary TypeLibrary;

//        public ComExtensionInterface() { }

//        public ComExtensionInterface(RegistryKey key, RegistryKey typeLibsKey)
//        {
//            InterfaceId = CurlyGuid.Create(key.GetKeyName());
//            ProxyStubClassId = CurlyGuid.Create(key.GetDefaultStringValueOfSubKeyForAppVManifest("ProxyStubClassId"));
//            Description = key.GetStringValueForAppVManifest("");
//            BaseInterface = key.GetStringValueForAppVManifest("BaseInterface");
//            try
//            {
//                NumMethods = key.GetDefaultIntValueOfSubKey("NumMethods");
//            }
//            catch (RegKeyNotFoundException)
//            {
//                NumMethods = 0;
//            }

//            var typeLibKey = key.OpenSubKeyForRead("TypeLib");
//            if (typeLibKey != null)
//                TypeLibrary = new ComExtensionInterfaceTypeLibrary(typeLibKey);
//        }
//    }

//    public class ComExtensionInterfaceTypeLibrary : XmlGenerator
//    {
//        //Taken from default value of HKCRx\Interface\<InterfaceId>\TypeLib
//        public CurlyGuid TypeLibraryId;
//        //Taken from value HKCRx\Interface\<InterfaceId>\TypeLib\Version
//        public string VersionNumber;

//        public ComExtensionInterfaceTypeLibrary() { }

//        public ComExtensionInterfaceTypeLibrary(RegistryKey key)
//        {
//            TypeLibraryId = CurlyGuid.Create(key.GetStringValueForAppVManifest(""));
//            VersionNumber = key.GetStringValueForAppVManifest("Version");
//        }
//    }

//    public class ComExtensionAppIdsElement : ComExtensionSubElement
//    {
//        public List<AppId> List;

//        private static IEnumerable<RegistryKey> FilterKeys(IEnumerable<RegistryKey> keys)
//        {
//            var map = new Dictionary<string, RegistryKey>(StringComparer.InvariantCultureIgnoreCase);
//            foreach (var key in keys)
//            {
//                key.GetSubKeyNames()
//                    .Where(x => !map.ContainsKey(x))
//                    .Select(x => key.OpenSubKeyForRead(x))
//                    .Where(x => x != null)
//                    .ForEach(x => map[x.GetKeyName()] = x);
//            }
//            return map.Values;
//        }

//        public static ComExtensionAppIdsElement CreateForStandaloneElement(IEnumerable<RegistryKey> keys)
//        {
//            keys = FilterKeys(keys);
//            return new ComExtensionAppIdsElement
//                          {
//                              List = keys
//                                  .Where(x => !RegistryTools.GuidRegex.IsMatch(x.GetKeyName()))
//                                  .Select(x => AppId.CreateForStandaloneElement(x))
//                                  .ToList()
//                          };
//        }

//        public static ComExtensionAppIdsElement CreateForStandaloneElement(RegistryKey key)
//        {
//            return CreateForStandaloneElement(key.ToEnumerable());
//        }

//        public static ComExtensionAppIdsElement CreateForRegularElement(IEnumerable<RegistryKey> keys)
//        {
//            var ret = new ComExtensionAppIdsElement
//                          {
//                              List = keys
//                                  .Select(x => AppId.CreateForRegularElement(x))
//                                  .ToList()
//                          };

//            return ret;
//        }

//        private ComExtensionAppIdsElement() { }

//        public int Count
//        {
//            get { return List == null ? 0 : List.Count; }
//        }

//        public override IEnumerable<string> Symbols
//        {
//            get { return List.Select(x => x.Id.ToString()); }
//        }
//    }

//    public class AppId : XmlGenerator
//    {
//        //XML name: AppId
//        //Taken from key HKCRx\AppID\<AppId>
//        //Or from value HKCRx\AppID\<Name>\AppId
//        public CurlyGuid Id;
//        //Taken from key HKCRx\AppID\<Name> if Name is not a GUID.
//        public string Name;
//        //Taken from value HKCRx\AppID\<AppId>\RunAs
//        public string RunAs;

//        private enum Kind
//        {
//            ForStandaloneElement,
//            ForRegularElement,
//        }

//        public static AppId CreateForStandaloneElement(RegistryKey key)
//        {
//            return new AppId(key, Kind.ForStandaloneElement);
//        }

//        public static AppId CreateForRegularElement(RegistryKey key)
//        {
//            return new AppId(key, Kind.ForRegularElement);
//        }

//        private AppId() { }
//        private AppId(RegistryKey key, Kind kind)
//        {
//            switch (kind)
//            {
//                case Kind.ForStandaloneElement:
//                    RunAs = null;
//                    Name = key.GetKeyName();
//                    Id = CurlyGuid.Create(key.GetStringValueForAppVManifest("AppId"));
//                    break;
//                case Kind.ForRegularElement:
//                    Name = null;
//                    Id = CurlyGuid.Create(key.GetKeyName());
//                    RunAs = key.GetStringValueForAppVManifest("RunAs");
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }
//        }

//    }

//    public class ComExtensionProgIdsSubElement : ComExtensionSubElement
//    {
//        public List<ComProgId> List = new List<ComProgId>();

//        public ComExtensionProgIdsSubElement() { }

//        public ComExtensionProgIdsSubElement(IEnumerable<Tuple<RegistryKey, CurlyGuid>> tuples)
//        {
//            List = tuples
//                .Select(x => new ComProgId(x))
//                .ToList();
//        }

//        public int Count
//        {
//            get { return List == null ? 0 : List.Count; }
//        }

//        public override IEnumerable<string> Symbols
//        {
//            get { return List.Select(x => x.Name); }
//        }

//        protected override void GenerateInnerXml(XmlTextWriter xml)
//        {
//            List.ForEach(x => x.GenerateXml(xml));
//        }

//        protected override string ClassName
//        {
//            get { return "ProgIds"; }
//        }
//    }

//    public class ComProgId : ProgId
//    {
//        //Name: Taken from default value of HKCRx\CLSID\<ClassId>
//        //Description: Taken from default value of HKCRx\<Name>
//        //CurrentVersionProgId: Taken from default value of HKCRx\<Name>\CurVer

//        //Taken from key HKCRx\CLSID\<ClassId>
//        public CurlyGuid ClassId;
//        //Taken from existence of key HKCRx\<Name>\Insertable
//        public bool InsertableObject;
//        //Taken from HKCRx\<Name>\protocol
//        public StdFileEditingProtocol StdFileEditingProtocol;

//        public ComProgId() { }

//        public ComProgId(Tuple<RegistryKey, CurlyGuid> tuple): base(tuple.Item1)
//        {
//            var key = tuple.Item1;
//            ClassId = tuple.Item2;
//            InsertableObject = key.ValueExists("Insertable");
//            var protocolKey = key.OpenSubKeyForRead("protocol");
//            if (protocolKey == null)
//                return;
//            var stdFileEditingKey = protocolKey.OpenSubKeyForRead("StdFileEditing");
//            if (stdFileEditingKey == null)
//                return;
//            StdFileEditingProtocol = new StdFileEditingProtocol(stdFileEditingKey);
//        }

//        protected override string ClassName
//        {
//            get { return "ProgId"; }
//        }
//    }

//    public class StdFileEditingProtocol : XmlGenerator
//    {
//        //App-V-normalized path.
//        //Taken from HKCRx\<Name>\protocol\StdFileEditing\server
//        public string Server;
//        //Taken from HKCRx\<Name>\protocol\StdFileEditing\Verb
//        //Pass this key path to each constructor.
//        public readonly List<Verb> Verbs;

//        public StdFileEditingProtocol() { }

//        public StdFileEditingProtocol(RegistryKey key)
//        {
//            Server = key.GetDefaultStringValueOfSubKeyForAppVManifest("server");
//            Verbs = RegistryTools.OpenVerbSubKeys(key)
//                .Select(x => new Verb(x))
//                .ToList();
//        }
//    }

//    public class ComExtensionTypeLibrariesSubElement : ComExtensionSubElement
//    {
//        [OpenList]
//        public List<TypeLibrary> List = new List<TypeLibrary>();

//        public ComExtensionTypeLibrariesSubElement() { }

//        public ComExtensionTypeLibrariesSubElement(List<Tuple<Guid, ComExtensionPlatformSpecificSubElement>> tuples)
//        {
//            var map = new Dictionary<int, Tuple<RegistryKey, RegistryKey>>();
//            foreach (var bitness in new HashSet<int>(tuples.Select(x => x.Item2.Bitness)))
//            {
//                var path = RegistryTools.GetClassesPath(bitness) + "\\TypeLib";
//                var path2 = RegistryTools.GetClassesPath(bitness) + "\\CLSID";
//                var typeLibKey = RegistryTools.GetKeyFromFullPath(path);
//                var classesKey = RegistryTools.GetKeyFromFullPath(path2);
//                if (typeLibKey == null || classesKey == null)
//                    throw new KeyNotFoundException();
//                map[bitness] = new Tuple<RegistryKey, RegistryKey>(classesKey, typeLibKey);
//            }

//            foreach (var tuple in tuples)
//            {
//                var keys = map[tuple.Item2.Bitness];
//                var typeLibKey = keys.Item1.OpenSubKeyForRead(tuple.Item2.Class.ClassId.ToString());
//                if (typeLibKey == null)
//                    throw new KeyNotFoundException();
//                typeLibKey = typeLibKey.OpenSubKeyForRead("TypeLib");
//                if (typeLibKey == null)
//                    throw new KeyNotFoundException();
   
//                List.Add(TypeLibrary.CreateFromClsidSubKey(typeLibKey, keys.Item2));
//            }
//        }

//        public int Count
//        {
//            get { return List == null ? 0 : List.Count; }
//        }

//        public override IEnumerable<string> Symbols
//        {
//            get { return List.Select(x => x.TypeLibraryId.ToString()); }
//        }
//    }

//    public class TypeLibrary : XmlGenerator
//    {
//        //Taken from default value of HKCRx\CLSID\<ClassId>\TypeLib
//        public CurlyGuid TypeLibraryId;
//        public class TypeLibraryVersion : XmlGenerator
//        {
//            //Taken from key HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>
//            public string VersionNumber;
//            //Taken from default value HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>
//            public string Name;
//            //App-V-normalized path.
//            //Taken from default value HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>\HELPDIR
//            public string HelpDirectory;
//            //Taken from default value HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>\FLAGS
//            public int Flags;
//            public class Library : XmlGenerator
//            {
//                //Taken from key HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>\<Index>
//                public int Index;
//                //App-V-normalized path.
//                //Taken from default value of HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>\<Index>\win32
//                public string Win32;
//                //App-V-normalized path.
//                //Taken from default value of HKCRx\TypeLib\<TypeLibraryId>\<VersionNumber>\<Index>\win64
//                public string Win64;

//                public Library() { }

//                public Library(RegistryKey key)
//                {
//                    Index = Convert.ToInt32(key.GetKeyName());
//                    Win32 = key.GetStringValueForAppVManifest("Win32");
//                    Win64 = key.GetStringValueForAppVManifest("Win64");
//                }
//            }
//            public List<Library> Libraries = new List<Library>();

//            public TypeLibraryVersion() { }

//            public TypeLibraryVersion(RegistryKey key)
//            {
//                VersionNumber = key.GetKeyName();
//                Name = key.GetStringValueForAppVManifest("");
//                HelpDirectory = key.GetStringValueForAppVManifest("HELPDIR");
//                key.GetIntValue("FLAGS", out Flags);

//                Libraries = key.GetSubKeyNames()
//                    .Where(x => x.All(char.IsDigit))
//                    .Select(x => key.OpenSubKeyForRead(x))
//                    .Where(x => x != null)
//                    .Select(x => new Library(x))
//                    .ToList();
//            }

//            protected override string ClassName
//            {
//                get { return "Version"; }
//            }
//        }
//        public List<TypeLibraryVersion> Versions = new List<TypeLibraryVersion>();

//        public TypeLibrary() { }

//        public static TypeLibrary CreateFromClsidSubKey(RegistryKey key, RegistryKey typeLibsKey)
//        {
//            var ret = new TypeLibrary
//            {
//                TypeLibraryId = CurlyGuid.Create(key.GetStringValueForAppVManifest(""))
//            };
//            var version = key.GetStringValueForAppVManifest("Version");

//            var typeLibKey = typeLibsKey.OpenSubKeyForRead(ret.TypeLibraryId.ToString());

//            if (!(typeLibKey == null || version == null))
//                ret.InitFromTypeLibSubKey(typeLibKey, version);
//            return ret;

//        }

//        private void InitFromTypeLibSubKey(RegistryKey typeLibKey, string version)
//        {
//            var versionKey = typeLibKey.OpenSubKeyForRead(version);
//            if (versionKey != null)
//                Versions.Add(new TypeLibraryVersion(versionKey));
//        }

//        public static IEnumerable<TypeLibrary> CreateFromTypeLibSubKey(RegistryKey key)
//        {
//            foreach (var name in key.GetSubKeyNames())
//            {
//                var ret = new TypeLibrary();
//                ret.TypeLibraryId = CurlyGuid.Create(key.GetKeyName());
//                ret.InitFromTypeLibSubKey(key, name);
//                yield return ret;
//            }
//        }
//    }





    public class AttributeProperties
    {
        public bool ToInt = false,
                    OmitZero = false,
                    AsAttribute = false,
                    OpenList = false,
                    Ignore = false,
                    OmitAppvNamespace = false;

        public string NodeName,
                      ElementName;

        public AttributeProperties(string fieldName)
        {
            ElementName = NodeName = fieldName;
        }
    }

    public abstract class ManifestSerializationAttribute : Attribute
    {
        public abstract void SetProperties(AttributeProperties props);
    }
}