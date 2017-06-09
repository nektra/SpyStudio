using System;
using System.Text;
using Nektra.Deviare2;
using System.Collections.Generic;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class CallEventPair
    {
        public class SimplifiedEvent
        {
            public string Result;
            public string Module;
            public string StackTraceString;
            public string[] Stack;
            public Param[] Params;
            public int ParamMainIndex;
#if DEBUG
            public StringBuilder Buffer;
#endif
        }

        public SimplifiedEvent Before;
        public SimplifiedEvent After;
    }
    public class ProcessState
    {
        public IntPtr NextBuffer = (IntPtr)0;
        public AsyncStream Stream = new AsyncStream();
        public IntPtr Mutex;
        public NktProcess Proc;
        public Dictionary<uint, CallEventPair> CookieToCepDict = new Dictionary<uint, CallEventPair>();
        public ProcessState() {}
    }
}
