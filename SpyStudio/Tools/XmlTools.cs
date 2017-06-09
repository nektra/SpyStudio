using System.Text;
using System.Xml;
using System.IO;

namespace SpyStudio.Tools
{
    public class CustomXmlWriter : XmlTextWriter
    {
        public CustomXmlWriter(TextWriter writer) : base(writer)
        {
            Initialize();
        }
        public CustomXmlWriter(Stream stream, Encoding encoding) : base(stream, encoding)
        {
            Initialize();
        }
        public CustomXmlWriter(string file, Encoding encoding)
            : base(file, encoding)
        {
            Initialize();
        }
        void Initialize()
        {
            var settings = new XmlWriterSettings();
            Indentation = 4;
        }
        public override void WriteString(string text)
        {
            Encoding utfencoder = Encoding.GetEncoding("UTF-8", new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
            byte[] bytText = utfencoder.GetBytes(text);
            string strEncodedText = utfencoder.GetString(bytText);
            base.WriteString(strEncodedText);
        }
    }
    public static partial class Extensions
    {
        public static bool MoveTo(this XmlTextReader reader, string tag, int depth)
        {
            return MoveTo(reader, tag, XmlNodeType.None, depth);
        }
        public static bool MoveTo(this XmlTextReader reader, string tag, XmlNodeType nodeType, int depth)
        {
            while (reader.Read())
            {
                if (reader.Depth == depth && (nodeType == reader.NodeType || nodeType == XmlNodeType.None) && reader.Name == tag)
                    return true;
            }
            return false;
        }
        public static bool MoveTo(this XmlTextReader reader, string tag, XmlNodeType nodeType, int depth, out StringBuilder xmlPassedBuilder)
        {
            xmlPassedBuilder = new StringBuilder(string.Empty);
            var ret = false;
            while (reader.Read())
            {
                var currentNodeType = reader.NodeType;
                if (reader.Depth == depth && (nodeType == currentNodeType || nodeType == XmlNodeType.None) && reader.Name == tag)
                {
                    ret = true;
                    break;
                }
                if(currentNodeType == XmlNodeType.Element)
                {
                    xmlPassedBuilder.Append("<" + reader.Name + ">");
                }
                if (currentNodeType == XmlNodeType.EndElement)
                {
                    xmlPassedBuilder.Append("</" + reader.Name + ">");
                }
                else if (currentNodeType == XmlNodeType.Text)
                {
                    xmlPassedBuilder.Append(reader.Value);
                }
            }

            return ret;
        }
        public static bool MoveToStartElement(this XmlTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.IsStartElement())
                    return true;
            }
            return false;
        }
    }

}