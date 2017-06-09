using System;

namespace SpyStudio.Swv
{
    public class CallEventExportStatusArgs : EventArgs
    {
        public CallEventExportStatusArgs(string fsPath, string layerPath, string status, bool success)
        {
            FileSystemPath = fsPath;
            LayerPath = layerPath;
            Status = status;
            Success = success;
        }

        public CallEventExportStatusArgs(string fsPath, string layerPath)
        {
            FileSystemPath = fsPath;
            LayerPath = layerPath;
        }

        public string FileSystemPath { get; private set; }
        public string LayerPath { get; private set; }
        public string Status { get; private set; }
        public bool Success { get; private set; }
    }
}