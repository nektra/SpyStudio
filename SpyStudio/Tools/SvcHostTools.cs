using System;
using System.Runtime.InteropServices;

namespace SpyStudio.Tools
{
    public class SvcHostTools
    {
        public static int FindDCOMSvcHostPid()
        {
            const uint SC_MANAGER_ENUMERATE_SERVICE = 4;
            IntPtr hSCM = IntPtr.Zero;
            int ret = 0;
            try
            {
                hSCM = Declarations.OpenSCManager(null, null, SC_MANAGER_ENUMERATE_SERVICE);
                if (hSCM == IntPtr.Zero)
                    return 0;
                ret = FindDCOMSvcHostPidHelper(hSCM);
            }
            finally
            {
                Declarations.CloseServiceHandle(hSCM);
            }
            return ret;
        }
        private static int FindDCOMSvcHostPidHelper(IntPtr hSCM)
        {
            const int SC_ENUM_PROCESS_INFO = 0;
            const int SERVICE_WIN32 = 48;
            const int SERVICE_STATE_ALL = 3;
            uint bytesNeeded;
            uint servicesReturned;
            uint lpResumeHandle = 0;
            Declarations.EnumServicesStatusEx(
                hSCM,
                SC_ENUM_PROCESS_INFO,
                SERVICE_WIN32,
                SERVICE_STATE_ALL,
                IntPtr.Zero,
                0,
                out bytesNeeded,
                out servicesReturned,
                ref lpResumeHandle,
                null
            );

            var buffer = Marshal.AllocHGlobal((int)bytesNeeded);

            if (!Declarations.EnumServicesStatusEx(hSCM, SC_ENUM_PROCESS_INFO, SERVICE_WIN32, SERVICE_STATE_ALL, buffer, bytesNeeded, out bytesNeeded, out servicesReturned, ref lpResumeHandle, null))
                return 0;

            int structSize = IntPtr.Size == 4 ? Declarations.ENUM_SERVICE_STATUS_PROCESS.SizePack4 : Declarations.ENUM_SERVICE_STATUS_PROCESS.SizePack8;

            long pointer = buffer.ToInt64();
            for (uint i = 0; i < servicesReturned; i++, pointer += structSize)
            {
                var serviceStatus = (Declarations.ENUM_SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(new IntPtr(pointer), typeof(Declarations.ENUM_SERVICE_STATUS_PROCESS));
                if (serviceStatus.pServiceName.ToLower() == "dcomlaunch")
                    return serviceStatus.ServiceStatus.processId;
            }
            return 0;
        }
    }
}
