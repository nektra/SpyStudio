using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Export.AppV.Manifest;
using SpyStudio.FileSystem;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Export.AppV
{
    public class AppvExport : VirtualizationExport
    {
        public AppvExport(DeviareRunTrace aTrace) : base(aTrace)
        {
            SetFieldValue(ExportFieldNames.OriginalFiles, new List<FileEntry>());
            SetFieldValue(ExportFieldNames.Files, new List<FileEntry>());
            SetFieldValue(ExportFieldNames.RegistryKeys, new List<RegKeyInfo>());
            SetFieldValue(ExportFieldNames.ApplicationBehaviourAnalizers, new List<AppBehaviourAnalyzer>());
        }

        private Thread ManifestThread;

        public override IEnumerable<FileSystemTreeChecker> FileCheckers
        {
            get { return null; /*Export.FileChecker.For(this);*/ }
        }

        public override string Name { get; set; }

        public override bool ShowFileSystemIsolationOptions { get { return false; } }

        public override DialogResult ShowAdvancedSettingsDialog()
        {
            throw new System.NotImplementedException();
        }

        public override Exporter CreateExporter()
        {
            return new AppvExporter();
        }

        public override bool SystemMeetsRequirements(ExportWizard anExportWizard)
        {
            return true;
        }

        private string _computedManifest;

        public void ComputeManifest(IEnumerable<FileEntry> files, List<string> services)
        {
            //return;
            CancelManifestComputation();
            _computedManifest = null;
            _manifestThreadMethodCancelled = new EventWaitHandle(false, EventResetMode.ManualReset);
            ManifestThread = new Thread(() => ManifestThreadMethod(files, services), 0);
            ManifestThread.Start();
        }

        public string WaitForManifest()
        {
            //return null;
            lock (this)
            {
                if (ManifestThread == null)
                    return null;
                ManifestThread.Join();
                ManifestThread = null;
                return _computedManifest;
            }
        }

        public void CancelManifestComputation()
        {
            //return;
            lock (this)
            {
                if (ManifestThread != null)
                {
                    _manifestThreadMethodCancelled.Set();
                    ManifestThread.Join();
                }
                ManifestThread = null;
            }
        }

        private EventWaitHandle _manifestThreadMethodCancelled;

        private void ManifestThreadMethod(IEnumerable<FileEntry> files, List<string> services)
        {
            Process proc;
            string pipeName;
            using (var pipe = PipeUtils.CreateRandomlyNamedServerPipe(PipeDirection.InOut, out pipeName))
            {
                var eventName = "SpyStudioEvent_" + StringTools.RandomLowerCaseString(8);
                var ev = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);

                proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "SpyStudio.exe",
                        Arguments = Program.ManifestModeString + " " + pipeName + " " + eventName,
                    }
                };
                proc.Start();

                var fileList = new List<string>();
                var dirList = new List<string>();
                foreach (var fileEntry in files)
                    (fileEntry.IsDirectory ? dirList : fileList).Add(fileEntry.FileSystemPath);

                pipe.WaitForConnection();

                foreach (var list in new[]{fileList, dirList, services})
                {
                    var serializer = new XmlSerializer(list.GetType());
                    var sb = new StringBuilder();
                    using (var stringWriter = new StringWriter(sb))
                    {
                        serializer.Serialize(stringWriter, list);
                    }
                    pipe.SendStringAndWait(sb.ToString());
                }

                var events = new WaitHandle[]
                {
                    ev,
                    _manifestThreadMethodCancelled,
                };

                int which = WaitHandle.WaitAny(events);

                if (which == 1)
                {
                    proc.Kill();
                    return;
                }

                _computedManifest = pipe.ReceiveString();

            }

            proc.WaitForExit();
        }

        public static void RemoteProcessManifestHandler(string pipeName, string eventName)
        {
            var ev = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);
            using (var pipe = new NamedPipeClientStream(pipeName))
            {
                pipe.Connect();
                var serializer = new XmlSerializer(typeof (List<string>));
                var files = (List<string>) serializer.Deserialize(new StringReader(pipe.ReceiveString()));
                var dirs = (List<string>) serializer.Deserialize(new StringReader(pipe.ReceiveString()));
                var services = (List<string>) serializer.Deserialize(new StringReader(pipe.ReceiveString()));

                var pack = new Package(files, dirs, services).GenerateManifestString();

                ev.Set();

                pipe.SendStringAndWait(pack);
            }
        }
    }
}
