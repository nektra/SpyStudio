using System;
using System.Collections.Generic;
using System.IO;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Export.Templates
{
    public static class LastTemplates
    {
        private static string GetPath(DeviareRunTrace trace)
        {
            return GetPath(trace.ObjectId);
        }

        private static string GetPath(Guid traceObjectId)
        {
            return SpyStudioConstants.TemplatesTempDirectory + "\\last-" + traceObjectId.ToString() + ".sts";
        }

        public static Stream GetLastTemplate(DeviareRunTrace trace, bool forRead)
        {
            return GetLastTemplate(trace.ObjectId, forRead);
        }

        public static Stream GetLastTemplate(Guid traceObjectId, bool forRead)
        {
            var path = GetPath(traceObjectId);
            Stream ret;
            try
            {
                if (forRead)
                    ret = new FileStream(path, FileMode.Open, FileAccess.Read);
                else
                    ret = new FileStream(path, FileMode.Create, FileAccess.Write);
            }
            catch
            {
                return null;
            }
            ScanAndRemoveOld(path);
            return ret;
        }

        static void ScanAndRemoveOld(string except)
        {
            except = except.ToLower();
            var files = new List<string>(Directory.GetFiles(SpyStudioConstants.TemplatesTempDirectory, "last-*.sts"));
            var now = DateTime.Now;
            foreach (var file in files)
            {
                if (except == file.ToLower())
                    continue;
                var time = File.GetLastWriteTime(file);
                if ((now - time).TotalDays >= 30)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {}
                }
            }
        }

        public static void Delete(DeviareRunTrace trace)
        {
            var path = GetPath(trace);
            try
            {
                File.Delete(path);
            }
            catch
            { }
        }
    }
}
