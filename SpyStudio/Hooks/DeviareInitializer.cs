using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Nektra.Deviare2;
using SpyStudio.Dialogs;
using SpyStudio.Properties;
using SpyStudio.Tools;
using RegistryWin32 = Microsoft.Win32.Registry;

namespace SpyStudio.Hooks
{
    public class DeviareInitializer
    {
        private static DeviareInitializer _instance;

        public static DeviareInitializer GetInstance()
        {
            return _instance ?? (_instance = new DeviareInitializer());
        }

        public class InitializeFinishedEventArgs : EventArgs
        {
            public InitializeFinishedEventArgs(bool success, int min, int max)
            {
                Success = success;
                MinimumProgress = min;
                MaximumProgress = max;
            }

            public bool Success { get; private set; }
            public int MinimumProgress { get; private set; }
            public int MaximumProgress { get; private set; }
        }

        public delegate void InitializeFinishedEventHandler(object sender, InitializeFinishedEventArgs e);

        public event InitializeFinishedEventHandler InitializationFinished;

        private Thread _deviareThread;
        private static NktSpyMgr _spyMgr;
        private static DeviareLiteInterop.HookLib _miniHookLib;
        private ManualResetEvent _mainDeviareThreadExitSignal;
        private int _progress;

        public NktSpyMgr SpyMgr
        {
            get { return _spyMgr; }
        }

        public DeviareLiteInterop.HookLib MiniHookLib
        {
            get { return _miniHookLib; }
        }

        public void Start()
        {
            _deviareThread = new Thread(MainDeviareThread);
            _deviareThread.SetApartmentState(ApartmentState.MTA);
            _mainDeviareThreadExitSignal = new ManualResetEvent(false);
            _deviareThread.Start();
            InitializingForm.ShowForm();
        }

        public void Shutdown()
        {
            _mainDeviareThreadExitSignal.Set();
            _deviareThread.Join();
        }

        public int Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                InitializingForm.SetProgress(_progress);
            }
        }

        public void MainDeviareThread()
        {
            var res = InitializeDeviare();

            InitializationFinished?.Invoke(this, new InitializeFinishedEventArgs(res, 70, 100));

            if (!res)
            {
                return;
            }

            _mainDeviareThreadExitSignal.WaitOne();
            ShutdownDeviare();
        }

        public bool InitializeDeviare()
        {
            _spyMgr = new NktSpyMgr();
            _miniHookLib = new DeviareLiteInterop.HookLib();

            const string appPath = ""; //default path is where COM dll resides

            _spyMgr.DatabasePath = appPath;
            _spyMgr.AgentPath = appPath;

            Progress = 10;

            long res = _spyMgr.Initialize();
            if (res < 0)
                return false;

            return true;
        }

        private void ShutdownDeviare()
        {
            Marshal.ReleaseComObject(_spyMgr);
            _miniHookLib = null;
            //Marshal.ReleaseComObject(_miniHookLib);
        }
    }
}