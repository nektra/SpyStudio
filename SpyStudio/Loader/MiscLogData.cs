using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SpyStudio.Tools;

namespace SpyStudio.Loader
{
    [Serializable]
    public class MiscLogData
    {
        [XmlAttribute]
        public string TraceGuid;
        public void Save(Stream stream)
        {
            XmlTools.SerializeClass(this, stream);
        }
        public static MiscLogData Restore(Stream stream)
        {
            return XmlTools.DeserializeClass<MiscLogData>(stream);
        }
    }
}
