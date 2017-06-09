using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV.Manifest
{
    public class Services : Extension
    {
        public override string Category { get { return "AppV.Service"; } }

        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            List.ForEach(x => x.GenerateXml(xml));
        }

        public List<ServiceService> List;

        public Services() { }

        public Services(IEnumerable<string> services)
        {
            const string path = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\services\";
            List = services
                .Select(x => RegistryTools.GetKeyFromFullPath(path + x))
                .Where(x => x != null)
                .Select(x => new ServiceService(x))
                .ToList();
        }

        public override IEnumerable<string> Symbols
        {
            get { return List.Select(x => "Service:" + x.Name); }
        }
    }

    public class ServiceService : XmlGenerator
    {
        //XML attributes.

        //Taken from key HKLM\System\CurrentControlSet\services\<Name>
        [SerializeAsAttribute]
        public string Name;

        //Taken from value HKLM\System\CurrentControlSet\services\<Name>\DisplayName
        [SerializeAsAttribute]
        public string DisplayName;

        //Taken from value HKLM\System\CurrentControlSet\services\<Name>\Description
        [SerializeAsAttribute]
        public string Description;

        //App-V-normalized path
        //Taken from value HKLM\System\CurrentControlSet\services\<Name>\ImagePath
        [SerializeAsAttribute]
        public string ImagePath;
        //Taken from value HKLM\System\CurrentControlSet\services\<Name>\ObjectName
        [SerializeAsAttribute]
        public string ObjectName;

        //Taken from default value of HKLM\System\CurrentControlSet\services\<Name>\Security
        [SerializeAsAttribute]
        public Base64Binary Security;

        //Taken from value HKLM\System\CurrentControlSet\services\<Name>\Start
        [SerializeAsAttribute]
        public int StartType;

        //Taken from value HKLM\System\CurrentControlSet\services\<Name>\Type
        [SerializeAsAttribute]
        public int ServiceType;

        public class DependentService : XmlGenerator
        {
            //Unknown.
            public string Name;

            public DependentService() { }

            public DependentService(string s)
            {
                Name = s;
            }
        }

        public List<DependentService> DependentServices = new List<DependentService>();

        public ServiceService() { }

        public ServiceService(RegistryKey key)
        {
            Name = key.GetKeyName();
            DisplayName = key.GetStringValueForAppVManifest("DisplayName");
            Description = key.GetStringValueForAppVManifest("Description");
            ImagePath = key.GetStringValueForAppVManifest("ImagePath");
            ObjectName = key.GetStringValueForAppVManifest("ObjectName");
            Security = Base64Binary.Create(key.GetValue("Security") as byte[]);

            key.GetIntValue("Start", out StartType);
            key.GetIntValue("Type", out ServiceType);

            {
                var obj = key.GetValue("DependOnService");
                var dependOnService = obj as string[];
                if (dependOnService == null)
                {
                    var s = obj as string;
                    if (s != null)
                        dependOnService = new[] {s};
                }
                if (dependOnService != null)
                    DependentServices = dependOnService
                        .Select(x => new DependentService(x))
                        .ToList();
            }
        }
    }
}