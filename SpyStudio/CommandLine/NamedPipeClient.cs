using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace SpyStudio.CommandLine
{
    class NamedPipeClient
    {
        private NamedPipeClientStream _pipeClient;

        public NamedPipeClient()
        {
            Timeout = 10000;
        }
        public int Timeout { get; set; }
        public bool TryToConnect()
        {
            bool ret;
            _pipeClient =
                new NamedPipeClientStream(".", "SpyStudioPipe",
                                          PipeDirection.InOut, PipeOptions.None,
                                          TokenImpersonationLevel.Impersonation);

            try
            {
                _pipeClient.Connect(Timeout);
                ret = true;
            }
            catch (Exception)
            {
                ret = false;
            }
            return ret;
        }
        public void SendParameters(string[] args, out string error, out string helpString)
        {
            var ss = new StreamString(_pipeClient);
            ss.WriteString(args.Length.ToString(CultureInfo.InvariantCulture));
            foreach(var arg in args)
            {
                ss.WriteString(arg);
            }
            error = ss.ReadString();
            helpString = ss.ReadString();
        }
    }
}
