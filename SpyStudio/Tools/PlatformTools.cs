using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using SpyStudio.Trace;
using RegistryWin32 = Microsoft.Win32.Registry;

namespace SpyStudio.Tools
{
    public class PlatformTools
    {
        private static int _bits64 = -1;

        public static bool IsPlatform64Bits()
        {
            if (_bits64 == -1)
            {
                var rkIdentifier =
                    RegistryWin32.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
                if (rkIdentifier == null)
                    _bits64 = 0;
                else
                    _bits64 = rkIdentifier.GetValue("Identifier").ToString().IndexOf("64", StringComparison.Ordinal) > 0
                                  ? 1
                                  : 0;
            }
            return (_bits64 == 1);
        }

        public static bool IsW7OrBelow()
        {
            return (Environment.OSVersion.Version.Major < 6 ||
                    Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1);
        }

        public static bool Is64Bits(int pid)
        {
            if (IsPlatform64Bits() && ((Environment.OSVersion.Version.Major > 5)
                                       ||
                                       ((Environment.OSVersion.Version.Major == 5) &&
                                        (Environment.OSVersion.Version.Minor >= 1))))
            {
                IntPtr processHandle;
                bool retVal;

                try
                {
                    processHandle = ProcessInfo.GetProcessHandle((uint) pid);
                }
                catch
                {
                    return false; // access is denied to the process
                }

                var ret = !(Declarations.IsWow64Process(processHandle, out retVal) && retVal);

                return ret;
            }

            return false; // not on 64-bit Windows
        }

        public static bool IsCurrentProcess64Bits()
        {
            var pid = Process.GetCurrentProcess().Id;
            return Is64Bits(pid);
        }

        public static bool IsRunningAsLocalAdmin()
        {
            var cur = WindowsIdentity.GetCurrent();
            Debug.Assert(cur != null, "cur != null");
            Debug.Assert(cur.Groups != null, "cur.Groups != null");
            foreach (var role in cur.Groups)
            {
                if (role.IsValidTargetType(typeof (SecurityIdentifier)))
                {
                    var sid = (SecurityIdentifier) role.Translate(typeof (SecurityIdentifier));
                    if (sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                        sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                        return true;
                }
            }
            return false;
        }

        private static readonly Dictionary<uint, short[]> TidArrays = new Dictionary<uint, short[]>();
        private const int MaxKeySize = 500;

        /// <summary>
        /// Don't create an array for each call: create an array for each thread
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static IntPtr GetProcessHandle(uint pid)
        {
            return ProcessInfo.GetProcessHandle(pid);
        }

        /// <summary>
        /// Don't create an array for each call: create an array for each thread
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="tid"> </param>
        /// <param name="array"></param>
        /// <returns></returns>
        public static IntPtr GetProcessHandleAndBuffer(uint pid, uint tid, out short[] array)
        {
            var hProc = ProcessInfo.GetProcessHandle(pid);
            lock (TidArrays)
            {
                if (!TidArrays.TryGetValue(tid, out array))
                {
                    array = TidArrays[tid] = new short[MaxKeySize];
                }
            }
            return hProc;
        }

        public static void GetBuffer(uint pid, uint tid, out short[] array)
        {
            lock (TidArrays)
            {
                if (!TidArrays.TryGetValue(tid, out array))
                {
                    array = TidArrays[tid] = new short[MaxKeySize];
                }
            }
        }
    }
}