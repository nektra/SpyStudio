using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SpyStudio.Trace;

namespace SpyStudio.Hooks
{
    public class ParamHandlerManager
    {
        private readonly Dictionary<string, UIntParamHandler> _uintHandlers =
            new Dictionary<string, UIntParamHandler>();

        private readonly Dictionary<string, IntParamHandler> _intHandlers =
            new Dictionary<string, IntParamHandler>();


        public void ParseContexts(XmlNode n)
        {
            XmlNodeList contextList = n.SelectNodes("/hooks/context");
            Debug.Assert(contextList != null, "contextList != null");
            foreach (XmlNode f in contextList)
            {
                if (f.Attributes["type"].InnerText.ToLower() == "uint")
                {
                    UIntParamHandler handler = UIntParamHandler.FromXml(f);
                    _uintHandlers[handler.Context] = handler;
                }
                else if (f.Attributes["type"].InnerText.ToLower() == "int")
                {
                    IntParamHandler handler = IntParamHandler.FromXml(f);
                    _intHandlers[handler.Context] = handler;
                }
            }
            _uintHandlers["MSGCODE"] = new MsgCodeParamHandler();
        }

        public string TranslateParam(string context, uint val)
        {
            UIntParamHandler handler;
            string ret;
            if (_uintHandlers.TryGetValue(context, out handler))
            {
                ret = handler.Translate(val);
            }
            else
                ret = "";
            return ret;
        }

        public string TranslateParam(string context, int val)
        {
            IntParamHandler handler;
            string ret;
            if (_intHandlers.TryGetValue(context, out handler))
            {
                ret = handler.Translate(val);
            }
            else
                ret = "";
            return ret;
        }
    }

    public class ParamHandler
    {
        private string context;

        public ParamHandler()
        {
        }

        public virtual string Translate(object o)
        {
            return "";
        }

        public string Context
        {
            set { context = value; }
            get { return context; }
        }
    }

    public class MsgCodeParamHandler : UIntParamHandler
    {
        private MessageInfo _messageInfo;

        public MsgCodeParamHandler()
        {
            _messageInfo = new MessageInfo();
        }

        public override string Translate(uint val)
        {
            var ret = _messageInfo.GetMessageString(val);
            return ret;
        }
    }

    public class UIntParamHandler : ParamHandler
    {
        private readonly Dictionary<uint, string> _values = new Dictionary<uint, string>();
        public bool Flag = false;

        public virtual string Translate(uint val)
        {
            string ret = "";
            if (!Flag)
            {
                if (!_values.TryGetValue(val, out ret))
                {
                    ret = "";
                }
            }
            else
            {
                uint i = 1;
                for (int j = 0; j < Marshal.SizeOf(i)*8; j++)
                {
                    if ((val & i) != 0)
                    {
                        string mapVal;
                        if (_values.TryGetValue(i, out mapVal))
                        {
                            ret += string.IsNullOrEmpty(ret) ? mapVal : " | " + mapVal;
                        }
                        val -= i;
                        if (val == 0)
                            break;
                    }

                    i *= 2;
                }
            }
            return ret;
        }

        public static UIntParamHandler FromXml(XmlNode n, UIntParamHandler ret)
        {
            XmlAttribute a;
            var values = n.SelectNodes("value");
            Debug.Assert(values != null, "values != null");
            foreach (XmlNode v in values)
            {
                Debug.Assert(v.Attributes != null, "v.ReadAttributes != null");
                a = v.Attributes["name"];
                if (a != null)
                {
                    uint val;
                    if (v.InnerText.ToLower().StartsWith("0x"))
                    {
                        val = Convert.ToUInt32(v.InnerText, 16);
                    }
                    else
                    {
                        val = Convert.ToUInt32(v.InnerText, CultureInfo.InvariantCulture);
                    }

                    ret._values[val] = a.InnerText;
                }
            }
            Debug.Assert(n.Attributes != null, "n.ReadAttributes != null");
            a = n.Attributes["flag"];
            if (a != null)
            {
                ret.Flag = (a.InnerText.ToLower() == "true");
            }
            a = n.Attributes["name"];
            if (a != null)
            {
                ret.Context = a.InnerText;
            }
            else
                ret = null;

            return ret;
        }

        public static UIntParamHandler FromXml(XmlNode n)
        {
            var ret = new UIntParamHandler();
            return FromXml(n, ret);
        }
    }

    public class IntParamHandler : ParamHandler
    {
        private readonly Dictionary<int, string> _values = new Dictionary<int, string>();
        public bool Flag = false;

        public virtual string Translate(int val)
        {
            string ret = "";
            if (!Flag)
            {
                if (!_values.TryGetValue(val, out ret))
                {
                    ret = "";
                }
            }
            else
            {
                int i = 1;
                for (int j = 0; j < Marshal.SizeOf(i)*8; j++)
                {
                    if ((val & i) != 0)
                    {
                        string mapVal;
                        if (_values.TryGetValue(i, out mapVal))
                        {
                            ret += string.IsNullOrEmpty(ret) ? mapVal : " | " + mapVal;
                        }
                        val -= i;
                        if (val == 0)
                            break;
                    }

                    i *= 2;
                }
            }
            return ret;
        }

        public static IntParamHandler FromXml(XmlNode n)
        {
            var ret = new IntParamHandler();
            return FromXml(n, ret);
        }

        public static IntParamHandler FromXml(XmlNode n, IntParamHandler ret)
        {
            XmlAttribute a;
            var values = n.SelectNodes("value");
            Debug.Assert(values != null, "values != null");
            foreach (XmlNode v in values)
            {
                Debug.Assert(v.Attributes != null, "v.ReadAttributes != null");
                a = v.Attributes["name"];
                if (a != null)
                {
                    int val;
                    if (v.InnerText.ToLower().StartsWith("0x"))
                    {
                        val = Convert.ToInt32(v.InnerText, 16);
                    }
                    else
                    {
                        val = Convert.ToInt32(v.InnerText, CultureInfo.InvariantCulture);
                    }

                    ret._values[val] = a.InnerText;
                }
            }
            Debug.Assert(n.Attributes != null, "n.ReadAttributes != null");
            a = n.Attributes["flag"];
            if (a != null)
            {
                ret.Flag = (a.InnerText.ToLower() == "true");
            }
            a = n.Attributes["name"];
            if (a != null)
            {
                ret.Context = a.InnerText;
            }
            else
                ret = null;

            return ret;
        }
    }
}
