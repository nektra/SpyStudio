using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using IWshRuntimeLibrary;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;
using SpyStudio.Tools;

namespace SpyStudio.Export.AppV.Manifest
{
    public class Shortcut : Extension
    {
        public override string Category { get { return "AppV.Shortcut"; } }

        //These are all XML elements, and are all in the appv namespace.

        //App-V-normalized path to a .lnk.
        public string File,
                      //App-V-normalized path to the shortcut's target.
                      Target,
                      //Follows the following format: %1%.%2%.ico
                      //%1% = App-V-normalized path to the executable that contains the icon.
                      //%2% = integer index on the icon table of the executable.
                      //TODO: Research what the format should be if the icon is just a .ico file.
                      Icon,
                      //Not really sure what this is supposed to be. Presumably just the "command line" field after removing the path.
                      Arguments,
                      //App-V-normalized path.
                      WorkingDirectory;
        //No idea what this is. Leave it out for now.
        //public string AppUserModelId;

        //Serialize as {0|1}.
        //Also no idea what this is.
        [IntSerializableField]
        public bool ShowCommand = true;
        //Presumably it's a foreign key to an Application.
        public string ApplicationId,
                      //I don't know the format for this.
                      HotKey,
                      //???
                      //public bool AppUserModelExcludeFromShowInNewInstall;
                      //???
                      //public int BrowserFlags;
                      Description;

        private static readonly Regex IconSplitter = new Regex(@"(.*)\,([0-9]+)");

        public Shortcut() { }

        public Shortcut(IWshShortcut shortcut)
        {
            var norm = AppvPathNormalizer.GetInstanceManifest();
            File = norm.Normalize(shortcut.FullName);
            ApplicationId = Target = norm.Normalize(shortcut.GetRealTarget());
            var match = IconSplitter.Match(shortcut.IconLocation);
            if (match.Success)
            {
                var path = norm.Normalize(match.Groups[1].ToString());
                var index = match.Groups[2].ToString();
                if (path.ToLower().EndsWith(".ico"))
                    Icon = path;
                else
                    Icon = path + "." + index + ".ico";
            }
            Arguments = shortcut.Arguments;
            WorkingDirectory = norm.Normalize(shortcut.WorkingDirectory);
            Description = shortcut.Description;
            HotKey = shortcut.Hotkey;
        }

        public override IEnumerable<string> Symbols
        {
            get { return File.ToEnumerable(); }
        }

        protected override void InternalGenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:Shortcut");

            XmlTools.SerializeAppvManifestObject(this, xml);

            xml.WriteEndElement();
        }
    }
}