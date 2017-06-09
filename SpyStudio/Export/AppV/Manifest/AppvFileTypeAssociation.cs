using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Export.AppV.Manifest;
using SpyStudio.Extensions;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;
using System.Xml.Serialization;

namespace SpyStudio.Export.AppV.Manifest
{
    public class FileTypeAssociation : Extension
    {
        public override string Category { get { return "AppV.FileTypeAssociation"; } }

        //These are all XML elements, and are all in the appv namespace.

        public FileExtension FileExtension;
        public ProgId ProgId;

        public static FileTypeAssociation Create(string extension)
        {
            var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKeyForRead(extension);
            if (key == null)
                return null;
            var ret = new FileTypeAssociation();
            using (var _ = key)
                ret.FileExtension = new FileExtension(key);
            if (string.IsNullOrEmpty(ret.FileExtension.ProgId))
                return null;

            key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKeyForRead(ret.FileExtension.ProgId);
            if (key == null)
                return null;
            using (var _ = key)
                ret.ProgId = new ProgId(key);

            return ret;
        }

        public override IEnumerable<string> Symbols
        {
            get { return FileExtension.Name.ToEnumerable(); }
        }
        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:FileTypeAssociation");
            if (FileExtension != null)
                FileExtension.GenerateXml(xml);
            if (ProgId != null)
                ProgId.GenerateXml(xml);
            xml.WriteEndElement();
        }
    }

    public class FileExtension : XmlGenerator
    {
        //XML attributes. All in the appv namespace.

        //Serialized as {false|true}.
        //true if the key HKEY_CLASSES_ROOT\MIME\Database\ContentType\<ContentType> exists
        //The value Extension may need to be set to <Name>.
        [SerializeAsAttribute]
        public bool MimeAssociation;


        //XML elements. All in the appv namespace.

        //Taken from key HKEY_CLASSES_ROOT\<extension>
        public string Name,
            //Foreign key to FileTypeAssociation.ProgId
            //Taken from default value of HKEY_CLASSES_ROOT\<Name>
            ProgId,
            //Taken from value HKEY_CLASSES_ROOT\<extension>\ContentType
            ContentType,
            //Taken from value HKEY_CLASSES_ROOT\<extension>\PerceivedType
            PerceivedType;
        //Items go as <appv:ClassId> subelements. The content is part of the element text.
        [ListElementName("ClassId")]
        public List<CurlyGuid> PreviewHandler;
        //Unknown format.
        //GUESSED: Taken from default value of HKEY_CLASSES_ROOT\<extension>\CLSID
        public string ClassId;
        //List of foreign keys to FileTypeAssociation.ProgId.
        //Items go as <appv:ProgId> subelements (that is, as subelements named
        //"appv:ProgId", not as subelements of <appv:ProgId>).
        [ListElementName("ProgId")]
        public List<string> OpenWithProgIds = new List<string>();
        public ProgIdShellNew ProgIdShellNew;

        private static HashSet<Guid> _previewHandlers;

        public FileExtension() { }

        public FileExtension(RegistryKey key)
        {
            Name = key.GetKeyName();
            ProgId = key.GetStringValueForAppVManifest("");
            ContentType = key.GetStringValueForAppVManifest("ContentType");
            PerceivedType = key.GetStringValueForAppVManifest("PerceivedType");

            if (_previewHandlers == null)
                _previewHandlers = RegistryTools.GetPreviewHandlers();

            {
                var shellEx = key.OpenSubKeyForRead("ShellEx");
                if (shellEx != null)
                {
                    var curlyHandlers = shellEx.GetSubKeyNames()
                        .Select(subKeyName => shellEx.OpenSubKeyForRead(subKeyName))
                        .Where(subKey => subKey != null)
                        .Select(subKey => subKey.GetStringValueForAppVManifest(""))
                        .Where(value => value != null && RegistryTools.GuidRegex.IsMatch(value))
                        .Select(value => new Guid(value))
                        .Where(guid => _previewHandlers.Contains(guid))
                        .Select(guid => new CurlyGuid(guid))
                        .ToList();
                    if (curlyHandlers.Count != 0)
                    {
                        if (PreviewHandler == null)
                            PreviewHandler = new List<CurlyGuid>();
                        PreviewHandler.AddRange(curlyHandlers);
                    }
                }
            }

            ClassId = key.GetStringValueForAppVManifest("CLSID");

            {
                var openWithProgIdsKey = key.OpenSubKeyForRead("OpenWithProgids");
                if (openWithProgIdsKey != null)
                {
                    var shellNew = openWithProgIdsKey.OpenSubKeyForRead("ShellNew");
                    if (shellNew != null)
                        ProgIdShellNew = ProgIdShellNew.Create(shellNew);
                }
            }

            MimeAssociation = null !=
                              RegistryTools.GetKeyFromFullPath(@"HKEY_CLASSES_ROOT\MIME\Database\ContentType\" +
                                                               ContentType);
        }
    }

    public class ProgIdShellNew : XmlGenerator
    {
        //App-V-normalized path.
        //Taken from value HKEY_CLASSES_ROOT\<extension>\<ProgId>\ShellNew\FileName
        public string FileName,
            //GUESSED!
            //App-V-normalized.
            //Taken from value HKEY_CLASSES_ROOT\<extension>\<ProgId>\ShellNew\Command
            Command,
            //GUESSED!
            //App-V-normalized path.
            //Taken from value HKEY_CLASSES_ROOT\<extension>\<ProgId>\ShellNew\ItemName
            ItemName;
        //Serialized as {false|true}.
        //Skip if false.
        [OmitZeroField]
        public bool NullFile;

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(FileName) && string.IsNullOrEmpty(Command) && string.IsNullOrEmpty(ItemName) &&
                       !NullFile;
            }
        }

        public ProgIdShellNew Create(RegistryKey shellNew)
        {
            var ret = new ProgIdShellNew(shellNew);
            return ret.IsEmpty ? null : ret;
        }

        public ProgIdShellNew() { }

        public ProgIdShellNew(RegistryKey key)
        {
            FileName = key.GetStringValueForAppVManifest("FileName");
            Command = key.GetStringValueForAppVManifest("Command");
            ItemName = key.GetStringValueForAppVManifest("ItemName");
            var s = key.GetStringValueForAppVManifest("NullFile");
            NullFile = Boolean.Parse(s);
        }
    }

    [XmlInclude(typeof(ComProgId))]
    public class ProgId : XmlGenerator
    {
        //These are all XML elements, and are all in the appv namespace.

        //Taken from key HKEY_CLASSES_ROOT\<FileExtension.ProgId>
        public string Name,
                      //Taken from default value of HKEY_CLASSES_ROOT\<Name>
                      Description,
                      //Taken from value HKEY_CLASSES_ROOT\<Name>\FriendlyTypeName
                      FriendlyTypeName,
                      //Taken from default value of HKEY_CLASSES_ROOT\<Name>\DefaultIcon
                      DefaultIcon,
                      //Take from default value of HKEY_CLASSES_ROOT\<Name>\CurVer
                      CurrentVersionProgId;

        //Required: HKEY_CLASSES_ROOT\<Name>\shell
        public ShellCommands ShellCommands;
        //Taken from value HKEY_CLASSES_ROOT\<Name>\EditFlags
        public int EditFlags;

        public ProgId() { }

        public ProgId(RegistryKey key)
        {
            Name = key.GetKeyName();
            Description = key.GetStringValueForAppVManifest("");
            FriendlyTypeName = key.GetStringValueForAppVManifest("FriendlyTypeName");
            DefaultIcon = key.GetStringValueForAppVManifest("DefaultIcon");
            CurrentVersionProgId = key.GetStringValueForAppVManifest("CurVer");
            key.GetIntValue("EditFlags", out EditFlags);
            ShellCommands = ShellCommands.Create(key);
        }

        private static readonly Regex DefaultIconRegex = new Regex(@"^(.*),[0-9]+$");
    }

    public class ShellCommands : XmlGenerator
    {
        //XML element in the appv namespace.

        //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell
        public string DefaultCommand;
        //This is not a container node. Items go directly in <appv:ShellCommands>.
        [OpenList]
        public List<ShellCommand> Commands = new List<ShellCommand>();

        public static ShellCommands Create(RegistryKey key)
        {
            var shellCommands = key.OpenSubKeyForRead("shell");
            if (shellCommands != null)
                return new ShellCommands(shellCommands);
            return null;
        }

        public ShellCommands() { }

        public ShellCommands(RegistryKey key)
        {
            DefaultCommand = key.GetStringValueForAppVManifest("");
            var shellCommands = key.GetSubKeyNames()
                .Select(x => key.OpenSubKeyForRead(x))
                .Where(x => x != null)
                .Select(x => new ShellCommand(x));
            Commands.AddRange(shellCommands);
        }
    }

    public class ShellCommand : XmlGenerator
    {
        //These are all XML elements, and are all in the appv namespace.

        //Taken from key HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<Name>
        public string Name,
                      //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<Name>
                      FriendlyName,
                      //Unsure.
                      ApplicationId,
                      //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<Name>\command
                      //If present, use value HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<Name>\command\command
                      //instead.
                      //This last value contains a "Darwin descriptor", which needs to be decoded.
                      //Either way, paths in this field need to be App-V-normalized.
                      CommandLine;
        //Encoded as {false|true}.
        //Taken from value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<Name>\Extended
        //True iff the value exists.
        public bool Extended;

        public DdeExec DdeExec;

        public ShellCommand() { }

        public ShellCommand(RegistryKey key)
        {
            Name = key.GetKeyName();
            FriendlyName = key.GetStringValueForAppVManifest("");

            {
                var command = key.OpenSubKeyForRead("command");
                if (command != null)
                {
                    CommandLine = command.GetStringValueForAppVManifest("command");
                    if (CommandLine == null)
                        CommandLine = command.GetStringValueForAppVManifest("");
                    else
                    {
                        CommandLine = DarwinPathDecoder.ExpandDarwinDescriptor(CommandLine);
                        CommandLine = AppvPathNormalizer.GetInstanceManifest().Normalize(CommandLine);
                    }
                }
            }

            if (CommandLine != null)
                ApplicationId = FileSystemTools.GetExecutablePathFromCommandLine(CommandLine);

            Extended = key.ValueExists("Extended");

            {
                var ddeExec = key.OpenSubKeyForRead("ddeexec");
                if (ddeExec != null)
                    DdeExec = new DdeExec(ddeExec);
            }
        }
    }

    public class DdeExec : XmlGenerator
    {
        //These are all XML elements, and are all in the appv namespace.

        //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<ShellCommand.Name>\ddeexec
        public string DdeCommand,
            //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<ShellCommand.Name>\ddeexec\application
            Application,
            //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<ShellCommand.Name>\ddeexec\topic
            Topic,
            //Taken from default value of HKEY_CLASSES_ROOT\<ProgId.Name>\shell\<ShellCommand.Name>\ddeexec\ifexec
            //Write node even if string is empty or null.
            IfExec,
            //Unknown source.
            NoActivateHandler;

        public DdeExec() { }

        public DdeExec(RegistryKey key)
        {
            DdeCommand = key.GetStringValueForAppVManifest("");
            Application = key.GetDefaultStringValueOfSubKeyForAppVManifest("application");
            Topic = key.GetDefaultStringValueOfSubKeyForAppVManifest("topic");
            IfExec = key.GetDefaultStringValueOfSubKeyForAppVManifest("ifexec");
            IfExec = IfExec ?? string.Empty;
        }
    }
}
