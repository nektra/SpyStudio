using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SpyStudio.Tools;

namespace SpyStudio.Export.AppV.Manifest
{
    public class Fonts : Extension
    {
        public override string Category { get { return "AppV.Fonts"; } }
        //List everything in [{Fonts}]
        public List<Font> List;

        public Fonts() { }

        public Fonts(IEnumerable<string> files)
        {
            List = files
                .Where(x => x.EndsWith(".ttf", StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new Font(x))
                .ToList();
        }

        public override IEnumerable<string> Symbols
        {
            get { return List.Select(x => x.Path); }
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
            xml.WriteStartElement("appv:Fonts");
            List.ForEach(x => x.GenerateXml(xml));
            xml.WriteEndElement();
        }
    }

    public class Font : ManifestObject
    {
        public string Path;

        public Font() { }

        public Font(string s)
        {
            Path = AppvPathNormalizer.GetInstanceManifest().Normalize(s);
        }

        public override void GenerateXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("appv:Font");
            xml.WriteAttributeString("Path", Path);
            xml.WriteEndElement();
        }
    }
}
