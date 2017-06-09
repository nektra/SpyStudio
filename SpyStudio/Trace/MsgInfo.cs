using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace SpyStudio.Trace
{
    class MessageInfo
    {
        public class Message
        {
            public string Msg;
            public uint Code;
            public string LparamType = "";
            public string WparamType = "";
        }

        readonly Dictionary<uint, Message> _msgMap = new Dictionary<uint, Message>();

        public MessageInfo()
        {
            var msgInfoDoc = new XmlDocument();
            msgInfoDoc.LoadXml(Properties.Resources.FunctionTypes);
            var msgList = msgInfoDoc.SelectNodes("/messages/msg");
            foreach (XmlNode f in msgList)
            {
                var msg = new Message
                              {
                                  Code = Convert.ToUInt32(f.Attributes["value"].InnerText, 16),
                                  Msg = f["name"].InnerText
                              };

                if (f["lparam"].Attributes["value"] != null)
                    msg.LparamType = f["lparam"].Attributes["value"].InnerText;
                if (f["wparam"].Attributes["value"] != null)
                    msg.WparamType = f["wparam"].Attributes["value"].InnerText;
                _msgMap[msg.Code] = msg;
            }
        }
        public string GetMessageString(uint code)
        {
            var ret = "";
            Message msg;
            if (_msgMap.TryGetValue(code, out msg))
            {
                ret = msg.Msg;
            }
            return ret;
        }
    }
}
