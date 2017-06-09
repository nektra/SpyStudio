using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using SpyStudio.CommandLine;
using SpyStudio.Dialogs;
using SpyStudio.Export.AppV;
using SpyStudio.Extensions;
using SpyStudio.Properties;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using Application = System.Windows.Forms.Application;

namespace SpyStudio
{
    static class Program
    {

        static IEnumerable<RegKeyInfo> Traverse(RegistryKey key)
        {
            var a = RegKeyInfo.From(key).ToEnumerable();
            var b = key.GetSubKeyNames()
                .Select(x =>
                {
                    try
                    {
                        return key.OpenSubKey(x);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .Select(x => Traverse(x))
                .Aggregate(Enumerable.Empty<RegKeyInfo>(), (x, y) => x.Concat(y));
            return a.Concat(b);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

            ////in windows forms you need also to add these two lines
            Application.ThreadException += OnApplicationThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            if(args.Length > 0)
                HandleCommandLineArguments(args);

            if (!Settings.Default.DlgPeep)
            {
                var dlg = new FormLicense();
                if(dlg.ShowDialog() == DialogResult.Cancel)
                {
                    return;
                }
                Settings.Default.DlgPeep = true;
                Settings.Default.Save();
            }
            Application.Run(new FormMain());
        }

        public const string ManifestModeString = "manifest";

        private static void HandleCommandLineArguments(string[] args)
        {
            Debug.Assert(args.Length > 0);
            switch (args[0])
            {
                case ManifestModeString:
                    Debug.Assert(args.Length == 3);
                    AppvExport.RemoteProcessManifestHandler(args[1], args[2]);
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }

            var consoleMode = new ConsoleModeMgr();
            string error, help;
            consoleMode.Execute(args, out error, out help);
            Application.Exit();
        }

        private static void OnApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Error.WriteLine("Unhandled Exception: " + e.Exception.Message);
            Error.MessageBox("Unhandled Exception: " + e.Exception.Message + "\nSpyStudio will shutdown");
            Application.Exit();
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Error.WriteLine("Unhandled Exception: " + e.ExceptionObject);
            Error.MessageBox("Unhandled Exception: " + e.ExceptionObject + "\nSpyStudio will shutdown");
            Application.Exit();
        }
    }
}
