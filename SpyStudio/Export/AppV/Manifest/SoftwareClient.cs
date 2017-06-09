using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using Shell32;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using System.Xml.Serialization;

namespace SpyStudio.Export.AppV.Manifest
{
    public class SoftwareClients : Extension
    {
        public override string Category { get { return "AppV.SoftwareClient"; } }
        public List<SoftwareClientsElement> List;

        public SoftwareClients() { }

        public SoftwareClients(IEnumerable<RegistryKey> keys)
        {
            List = keys
                    .Select(x => SoftwareClientsElement.Create(x))
                    .Where(x => x != null)
                    .ToList();
        }

        public int Count
        {
            get { return List.Count; }
        }

        public override IEnumerable<string> Symbols
        {
            get { return List.Select(x => x.Name); }
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
            xml.WriteStartElement("appv:SoftwareClients");

            List.ForEach(x => x.GenerateXml(xml));

            xml.WriteEndElement();
        }
    }

    [XmlInclude(typeof(EmailSoftwareClient))]
    [XmlInclude(typeof(BrowserSoftwareClient))]
    public abstract class SoftwareClientsElement : ManifestObject
    {
        //Attribute
        [OmitZeroField]
        [SerializeAsAttribute]
        public bool MakeDefault;

        //Taken from key HKLM_Software*\Clients\<*>\<Name>
        public string Name,
                      //Taken from default value of HKLM_Software*\Clients\<*>\<Name>
                      Description,
                      //Taken from default value of HKLM_Software*\Clients\<*>\<Name>\DefaultIcon
                      DefaultIcon;
        //Requires key HKLM_Software*\Clients\<*>\<Name>\InstallInfo
        public InstallationInformation InstallationInformation;

        public SoftwareClientsShellCommands ShellCommands;

        private static readonly Regex TypeRegex = new Regex(@".*\\+([^\\]+)\\+[^\\]+");

        public static SoftwareClientsElement Create(RegistryKey key)
        {
            var match = TypeRegex.Match(key.Name);
            if (!match.Success)
                return null;
            var type = match.Groups[1].ToString();
            switch (type.ToLower())
            {
                case "mail":
                    return new EmailSoftwareClient(key);
                case "startmenuinternet":
                    return new BrowserSoftwareClient(key);
            }
            return null;
        }

        public SoftwareClientsElement() { }

        protected SoftwareClientsElement(RegistryKey key)
        {
            Name = key.GetKeyName();
            Description = key.GetStringValueForAppVManifest("");
            DefaultIcon = key.GetDefaultStringValueOfSubKeyForAppVManifest("DefaultIcon");
            InstallationInformation = InstallationInformation.Create(key);
            InitShellCommands(key);

            var parent = key.OpenParentKey();
            if (parent != null)
            {
                var defaultClient = parent.GetStringValueForAppVManifest("");
                MakeDefault = defaultClient == Name;
            }
        }

        private void InitShellCommands(RegistryKey key)
        {
            ShellCommands = SoftwareClientsShellCommands.Create(key.OpenSubKey("shell"));
        }
    }

    public class EmailSoftwareClient : SoftwareClientsElement
    {
        //Taken from value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\DLLPath
        public string MAPILibrary,
                      //App-V-normalized path.
                      //Taken from value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\DLLPathEx
                      ExtendedMAPILibrary;
        public MailToProtocol MailToProtocol;

        public EmailSoftwareClient() { }

        public EmailSoftwareClient(RegistryKey key) : base(key)
        {
            MAPILibrary = key.GetStringValueForAppVManifest("DLLPath");
            ExtendedMAPILibrary = key.GetStringValueForAppVManifest("DLLPathEx");
            var classesKey = RegistryTools.OpenClassesKey();
            if (classesKey == null)
                throw new KeyNotFoundException();
            var mailtoKey = classesKey.OpenSubKey("mailto");
            if (mailtoKey != null)
                MailToProtocol = new MailToProtocol(mailtoKey);
        }

        protected override string ClassName
        {
            get { return "EMail"; }
        }
    }

    public class SoftwareClientsShellCommands: XmlGenerator
    {
        //App-V-normalized path.
        //Unknown. Cross-reference with another datum?
        public string ApplicationId,
                      //App-V-normalized path.
                      //Taken from value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\shell\<*>\command
                      Open;

        public SoftwareClientsShellCommands() {}

        public SoftwareClientsShellCommands(ShellCommand command)
        {
            ApplicationId = command.ApplicationId;
            Open = command.CommandLine;
        }

        public static SoftwareClientsShellCommands Create(RegistryKey shellKey)
        {
            if (shellKey == null)
                return null;

            var command = shellKey
                .GetSubKeyNames()
                .Select(x => shellKey.OpenSubKey(x))
                .Select(x =>
                {
                    if (x == null)
                        return null;
                    var cmd = new ShellCommand(x);
                    if (!cmd.Name.Equals("open", StringComparison.InvariantCultureIgnoreCase))
                        return null;
                    return cmd;
                })
                .FirstOrDefault(x => x != null);
            if (command != null)
                return new SoftwareClientsShellCommands(command);
            return null;
        }
    }

    public class InstallationInformation : XmlGenerator
    {
        public RegistrationCommands RegistrationCommands;
        // {0|1}
        //Taken from DWORD value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\InstallInfo\IconsVisible
        [IntSerializableField]
        public bool IconsVisible;
        //Unknown.
        public string OEMSettings;

        public static InstallationInformation Create(RegistryKey key)
        {
            var installInfoKey = key.OpenSubKey("InstallInfo");
            if (installInfoKey == null)
                return null;

            return new InstallationInformation(installInfoKey);
        }

        private InstallationInformation() { }

        private InstallationInformation(RegistryKey key)
        {
            int value;
            key.GetIntValue("IconsVisible", out value);
            IconsVisible = value != 0;
            RegistrationCommands = new RegistrationCommands(key);
        }
    }

    public class RegistrationCommands : XmlGenerator
    {
        //App-V-normalized path.
        //Taken from value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\InstallInfo\ReinstallCommand
        public string Reinstall,
                      //App-V-normalized path.
                      //Taken from value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\InstallInfo\HideIconsCommand
                      HideIcons,
                      //App-V-normalized path.
                      //Taken from value HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\InstallInfo\ShowIconsCommand
                      ShowIcons;

        public RegistrationCommands() { }

        public RegistrationCommands(RegistryKey key)
        {
            var type = GetType();
            foreach (var fieldInfo in type.GetFields())
            {
                var value = key.GetStringValueForAppVManifest(fieldInfo.Name + "Command");
                if (value == null)
                    continue;
                fieldInfo.SetValue(this, value);
            }
        }
    }

    public class MailToProtocol : XmlGenerator
    {
        //Taken from default value of HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\Protocols\mailto
        public string Description,
                      //Taken from default value of HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\Protocols\mailto\DefaultIcon
                      DefaultIcon;
        //Taken from value of HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\Protocols\mailto\EditFlags
        //The value may be a binary, in which case it encodes the integer in little-endian.
        public int EditFlags;
        //Taken from HKLM_Software*\Clients\<*>\<SoftwareClientsElement.Name>\Protocols\mailto\shell
        public SoftwareClientsShellCommands ShellCommands;

        public MailToProtocol() {}

        public MailToProtocol(RegistryKey key)
        {
            Description = key.GetStringValueForAppVManifest("");
            DefaultIcon = key.GetDefaultStringValueOfSubKeyForAppVManifest("DefaultIcon");
            key.GetIntValue("EditFlags", out EditFlags);
            ShellCommands = SoftwareClientsShellCommands.Create(key.OpenSubKey(@"shell"));
        }
    }

    public class BrowserSoftwareClient : SoftwareClientsElement
    {
        public BrowserSoftwareClient() { }

        public BrowserSoftwareClient(RegistryKey key) : base(key)
        {
        }

        protected override string ClassName
        {
            get { return "Browser"; }
        }
    }

}