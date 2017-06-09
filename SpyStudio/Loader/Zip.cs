using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Ionic.Zip;

namespace SpyStudio.Loader
{
    public class ArchivalItem
    {
        public bool Enable = true;
        public bool Required = true;
        //ZipCreator ignores this.
        public Regex CompressedPattern;
        //ZipExtractor ignores this.
        public string CompressedPath;
        public string UncompressedPath;
        public bool PerformInParallel;
        public enum Status
        {
            NotStarted,
            Succeeded,
            Failed,
        }
        public Status StatusInfo;
    }

    public abstract class ZipHandler : IDisposable
    {
        protected ZipFile Zip;
        protected ZipFile ParallelZip;
        protected readonly List<ArchivalItem> SerialItems = new List<ArchivalItem>();
        protected readonly List<ArchivalItem> ParallelItems = new List<ArchivalItem>();
        protected readonly Thread ParallelPerformer;
        //0: undefined, <0: failure, >0: success
        protected int ParallelSuccess = 0;
        protected bool ContinueProcessing = true;
        private readonly StringBuilder _errorsToReport = new StringBuilder();
        public Action<string> ErrorReportFunction;
        public ZipHandler(string path, IEnumerable<ArchivalItem> items)
        {
            Zip = Open(path);
            foreach (var archivalItem in items)
            {
                if (!archivalItem.Enable)
                    continue;
                var list = !archivalItem.PerformInParallel || !IsParallelismEnabled ? SerialItems : ParallelItems;
                list.Add(archivalItem);
            }
            if (ParallelItems.Count > 0)
            {
                ParallelZip = Open(path);
                ParallelPerformer = new Thread(Thread);
            }
        }

        public void Dispose()
        {
            Stop();
            Join();
            if (Zip != null)
                Zip.Dispose();
            if (ParallelZip != null)
                ParallelZip.Dispose();
        }

        protected abstract ZipFile Open(string path);

        protected abstract bool IsParallelismEnabled { get; }

        private void Thread()
        {
            ParallelSuccess = PerformInternal(ParallelItems, ParallelZip) ? 1 : -1;
        }
        protected abstract bool PerformInternal(List<ArchivalItem> items, ZipFile zip);
        protected abstract void Rollback();

        public bool Perform()
        {
            if (ParallelPerformer != null)
                ParallelPerformer.Start();
            if (!PerformInternal(SerialItems, Zip))
            {
                Stop();
                Join();
                Rollback();
                ReportErrors();
                return false;
            }
            return true;
        }

        public void Stop()
        {
            ContinueProcessing = false;
        }

        public bool Join()
        {
            if (ParallelPerformer != null)
                ParallelPerformer.Join();
            return ParallelSuccess > 0;
        }

        private void ReportErrors()
        {
            if (ErrorReportFunction == null)
                return;
            ErrorReportFunction(_errorsToReport.ToString());
        }

        protected void ReportError(string error)
        {
            _errorsToReport.Append(error);
        }
    }

    public class ZipExtractor : ZipHandler
    {
        private readonly List<string> _createdFiles = new List<string>();

        public ZipExtractor(string path, IEnumerable<ArchivalItem> items)
            : base(path, items)
        {
        }

        protected override ZipFile Open(string path)
        {
            return ZipFile.Read(path);
        }

        protected override bool IsParallelismEnabled
        {
            get { return true; }
        }

        protected override bool PerformInternal(List<ArchivalItem> items, ZipFile zip)
        {
            foreach (ZipEntry e in zip)
            {
                if (!ContinueProcessing)
                    break;
                var item = items.FirstOrDefault(x => x.CompressedPattern.IsMatch(e.FileName));
                if (item == null)
                    continue;
                if (!Extract(item, e))
                    return false;
            }
            return items.All(x => !x.Required || x.StatusInfo == ArchivalItem.Status.Succeeded);
        }

        protected override void Rollback()
        {
            foreach (var createdFile in _createdFiles)
                File.Delete(createdFile);
        }

        private bool Extract(ArchivalItem item, ZipEntry entry)
        {
            bool ret = true;
            item.StatusInfo = ArchivalItem.Status.Succeeded;
            try
            {
                using (var file = new FileStream(item.UncompressedPath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(file))
                {
                    lock (_createdFiles)
                    {
                        _createdFiles.Add(item.UncompressedPath);
                    }
                    try
                    {
                        entry.Extract(writer.BaseStream);
                    }
                    catch (Exception ex)
                    {
                        ReportError("Error extracting " + entry.FileName + ": " + ex.Message);
                        if (item.Required)
                            ret = false;
                        item.StatusInfo = ArchivalItem.Status.Failed;
                    }
                }
            }
            catch(IOException ex)
            {
                ReportError("Cannot open file " + item.UncompressedPath + ": " + ex.Message);
                if (item.Required)
                    ret = false;
                item.StatusInfo = ArchivalItem.Status.Failed;
            }
            return ret;
        }

        public void SetExtractProgressFunction(EventHandler<ExtractProgressEventArgs> zipOnExtractProgress)
        {
            if (Zip != null)
                Zip.ExtractProgress += zipOnExtractProgress;
        }
    }
}
