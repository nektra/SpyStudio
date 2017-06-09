using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Tools
{
    static class StringHash
    {
        public static uint MultiplicationHash(string s)
        {
            uint ret = 0xF00BA8;
            uint factor = 0xDEADBEEF;
            s = s.ToLower();
            foreach (var c in s)
            {
                ret *= factor;
                ret ^= c;
            }
            return ret;
        }
    }
}
