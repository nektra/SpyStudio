using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using SpyStudio.Tools;

namespace SpyStudio.CommandLine
{
    class NamedPipeServer
    {
        private Thread _serverThread;

        public class ClientRequestEventArgs
        {
            public ClientRequestEventArgs(string[] args)
            {
                Args = args;
            }

            public string[] Args { get; set; }
            public bool Success { get; set; }
            public string Error { get; set; }
            public string HelpString { get; set; }
            public bool Shutdown { get; set; }
        }

        public event ClientRequestEventHandler ClientRequest;
        public delegate void ClientRequestEventHandler(object sender, ClientRequestEventArgs e);
        
        public void Start()
        {
            _serverThread = new Thread(ServerThread);
            _serverThread.Start();
        }
        public void Join()
        {
            _serverThread.Join();
        }

        public bool Verbose { get; set; }

        private void ServerThread(object data)
        {
            bool cont = true;

            while(cont)
            {
                using (var pipeServer =
                new NamedPipeServerStream("SpyStudioPipe", PipeDirection.InOut))
                {
                    // Wait for a client to connect
                    pipeServer.WaitForConnection();

                    try
                    {
                        var ss = new StreamString(pipeServer);
                        var paramCount = ss.ReadString();
                        int count = Convert.ToInt32(paramCount, CultureInfo.InvariantCulture);
                        var args = new string[count];

                        for (int i = 0; i < count; i++)
                        {
                            args[i] = ss.ReadString();
                        }
                        if (ClientRequest != null)
                        {
                            var eventArgs = new ClientRequestEventArgs(args);
                            ClientRequest(this, eventArgs);
                            ss.WriteString(eventArgs.Error);
                            ss.WriteString(eventArgs.HelpString);
                            if (eventArgs.Shutdown)
                                break;
                        }
                    }
                    catch (IOException e)
                    {
                        Error.WriteLine("FATAL ERROR: {0}" + e.Message);
                        cont = false;
                    }
                    pipeServer.Close();
                }
            }
        }
    }
}
