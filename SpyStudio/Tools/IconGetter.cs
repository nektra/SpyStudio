using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SpyStudio.Tools
{
    public static class IconGetter
    {
        public static Image GetIcon(out string newPath, int procId, string procPath, Trace.ProcessInfo processInfo)
        {
            Image procIcon = null;

            string path = procPath;
            newPath = path;
            if (path != "")
            {
                procIcon = processInfo.GetIcon(path);
                if (procIcon == null)
                {
                    procIcon = FileSystemTools.GetIcon(path);
                    //// the path is wrong -> try to get the correct one
                    //if (!path.Contains(@"\"))
                    //{
                    //    try
                    //    {
                    //        var p = Process.GetProcessById(procId);
                    //        if (p.Modules.Count > 0)
                    //        {
                    //            path = p.MainModule.FileName;
                    //        }
                    //    }
                    //    catch (Exception)
                    //    {
                    //        path = "";
                    //    }
                    //    newPath = path;
                    //}
                    //if (!string.IsNullOrEmpty(path))
                    //{
                    //    Icon bigIcon;
                    //    try
                    //    {
                    //        bigIcon = Icon.ExtractAssociatedIcon(path);
                    //    }
                    //    catch (Exception)
                    //    {
                    //        bigIcon = null;
                    //    }
                    //    if (bigIcon != null)
                    //    {
                    //        // Fix a smaller version with interpolation
                    //        var bm = new Bitmap(bigIcon.ToBitmap());
                    //        procIcon = new Bitmap(16, 16);
                    //        Graphics g = Graphics.FromImage(procIcon);
                    //        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    //        g.DrawImage(bm, new Rectangle(0, 0, 16, 16),
                    //                    new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
                    //        g.Dispose();
                    //        bm.Dispose();
                    //    }
                    //}
                }
            }
            return procIcon;
        }
    }
}
