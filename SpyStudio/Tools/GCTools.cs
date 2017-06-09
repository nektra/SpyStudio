using System;
using System.ComponentModel;
using System.Threading;

namespace SpyStudio.Tools
{
    public class GCTools
    {
        private static bool _inCall;
        public static void AsyncCollectDelayed(int milliseconds)
        {
            if (_inCall)
                return;
            _inCall = true;
            var gcWorker = new BackgroundWorker();

            gcWorker.DoWork += (a, b) =>
                {
                    Thread.Sleep(milliseconds);
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    //GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    //GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                    _inCall = false;
                };

            gcWorker.RunWorkerAsync();
        }
    }
}