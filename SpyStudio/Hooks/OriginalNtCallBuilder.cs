using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpyStudio.Hooks
{
    public class OriginalNtCallBuilder
    {
        public struct API
        {
            public string name;
            public int offset;
        };

        #region Private Structs
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1), Serializable]
        private struct CODE3_BUILDORIGINALNTCALL_API_ITEM
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szApiNameA;
            public int dwOffset;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
        private struct CODE3_BUILDORIGINALNTCALL_API
        {
            public int dwPlatformBits;
            public int dwItemCount;
            public int dwDestSize;
        };
        #endregion

        #region Private Declarations
        [DllImport("DeviareCOM.dll", EntryPoint = "#7", CallingConvention = CallingConvention.StdCall)]
        private static extern int Internal1_32(int code, IntPtr op1, out IntPtr op2);

        [DllImport("DeviareCOM64.dll", EntryPoint = "#7", CallingConvention = CallingConvention.StdCall)]
        private static extern int Internal1_64(int code, IntPtr op1, out IntPtr op2);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalFree(IntPtr hMem);
        #endregion

        static public int Create(API[] apis, int platformBits, out byte[] codeBytes)
        {
            CODE3_BUILDORIGINALNTCALL_API apiHdr = new CODE3_BUILDORIGINALNTCALL_API();
            CODE3_BUILDORIGINALNTCALL_API_ITEM apiItem = new CODE3_BUILDORIGINALNTCALL_API_ITEM();
            IntPtr p, bufPtr, dataPtr;
            int i, err;

            codeBytes = null;
            //----
            if (apis.Length < 1)
                return -1;
            apiHdr = new CODE3_BUILDORIGINALNTCALL_API();
            apiHdr.dwItemCount = apis.Length;
            apiHdr.dwDestSize = 0;
            apiHdr.dwPlatformBits = platformBits;
            //----
            bufPtr = IntPtr.Zero;
            dataPtr = IntPtr.Zero;
            try
            {
                //allocate unmanager memory
                bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(apiHdr) + apiHdr.dwItemCount * Marshal.SizeOf(apiItem));
                //copy items
                Marshal.StructureToPtr(apiHdr, bufPtr, false);
                p = new IntPtr(bufPtr.ToInt64() + Marshal.SizeOf(apiHdr));
                for (i = 0; i < apiHdr.dwItemCount; i++)
                {
                    apiItem.szApiNameA = apis[i].name;
                    apiItem.dwOffset = 0;
                    Marshal.StructureToPtr(apiItem, p, false);
                    p = new IntPtr(p.ToInt64() + Marshal.SizeOf(apiItem));
                }
                //call our magic api
                if (IntPtr.Size == 4)
                    err = Internal1_32(3, bufPtr, out dataPtr);
                else
                    err = Internal1_64(3, bufPtr, out dataPtr);
                //on success copy back to managed
                if (err >= 0)
                {
                    apiHdr = (CODE3_BUILDORIGINALNTCALL_API)Marshal.PtrToStructure(bufPtr, typeof(CODE3_BUILDORIGINALNTCALL_API));
                    p = new IntPtr(bufPtr.ToInt64() + Marshal.SizeOf(apiHdr));
                    for (i = 0; i < apiHdr.dwItemCount; i++)
                    {
                        apiItem = (CODE3_BUILDORIGINALNTCALL_API_ITEM)Marshal.PtrToStructure(p, typeof(CODE3_BUILDORIGINALNTCALL_API_ITEM));
                        apis[i].offset = apiItem.dwOffset;
                        p = new IntPtr(p.ToInt64() + Marshal.SizeOf(apiItem));
                    }
                    codeBytes = new byte[apiHdr.dwDestSize];
                    Marshal.Copy(dataPtr, codeBytes, 0, apiHdr.dwDestSize);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("GetOriginalNtCalls -> " + ex.ToString());
                err = -1;
            }
            if (bufPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(bufPtr);
            if (dataPtr != IntPtr.Zero)
                GlobalFree(dataPtr);
            return err;
        }
    }
}
