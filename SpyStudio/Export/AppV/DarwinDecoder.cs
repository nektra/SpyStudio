using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Export.AppV
{
    static class DarwinDecoder
    {
        private static readonly byte[] TableDec85 = new byte[]{
            0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
            0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
            0xFF,0x00,0xFF,0xFF,0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08,0x09,0x0A,0x0B,0xFF,
            0x0C,0x0D,0x0E,0x0F,0x10,0x11,0x12,0x13,0x14,0x15,0xFF,0xFF,0xFF,0x16,0xFF,0x17,
            0x18,0x19,0x1A,0x1B,0x1C,0x1D,0x1E,0x1F,0x20,0x21,0x22,0x23,0x24,0x25,0x26,0x27,
            0x28,0x29,0x2A,0x2B,0x2C,0x2D,0x2E,0x2F,0x30,0x31,0x32,0x33,0xFF,0x34,0x35,0x36,
            0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,0x3F,0x40,0x41,0x42,0x43,0x44,0x45,0x46,
            0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,0x4F,0x50,0x51,0x52,0xFF,0x53,0x54,0xFF,
        };

        public static Guid? Decode(string s)
        {
            var guid = new uint[4];
            uint val = 0,
                 _base = 1;

            for (var i = 0; i < 20; i++)
            {
                if ((i % 5) == 0)
                {
                    val = 0;
                    _base = 1;
                }
                val += TableDec85[s[i]] * _base;
                if (s[i] >= 0x80)
                    return null;
                if (TableDec85[s[i]] == 0xFF)
                    return null;
                if ((i % 5) == 4)
                    guid[i/5] = val;
                _base *= 85;
            }

            var bytes = new byte[16];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((guid[i/4] >> (i % 4 * 8)) & 0xFF);
            }

            return new Guid(bytes);
        }



        public static byte[] Decode2(string s)
        {
            var list = new List<uint>();
            uint val = 0,
                 _base = 1;

            for (var i = 0; i < s.Length; i++)
            {
                if ((i % 5) == 0)
                {
                    val = 0;
                    _base = 1;
                }
                val += TableDec85[s[i]] * _base;
                if (s[i] >= 0x80)
                    return null;
                if (TableDec85[s[i]] == 0xFF)
                    return null;
                if ((i % 5) == 4)
                    list.Add(val);
                _base *= 85;
            }

            var bytes = new byte[list.Count * 4];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((list[i / 4] >> (i % 4 * 8)) & 0xFF);
            }

            return bytes;
        }
    }
}
