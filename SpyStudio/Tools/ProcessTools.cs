using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SpyStudio.Tools
{
    public static class ProcessTools
    {
        public static uint PROCESS_ALL_ACCESS = 0x001FFFFF;
        public static bool ReadProcessMemory<T>(out T dst, IntPtr proc, IntPtr address) where T : new()
        {
            var size = (int)Marshal.SizeOf(typeof(T));
            IntPtr raw = Marshal.AllocHGlobal(size);
            IntPtr ignored;
            //Console.WriteLine("0x" + ((uint)address).ToString("X8"));
            if (!Declarations.ReadProcessMemory(proc, address, raw, size, out ignored))
            {
                Marshal.FreeHGlobal(raw);
                dst = new T();
                return false;
            }
            dst = (T)Marshal.PtrToStructure(raw, typeof(T));
            Marshal.FreeHGlobal(raw);
            return true;
        }

        //TODO: Fix x64->x86
        public static string GetCommandLineFromHandle(IntPtr proc)
        {
            if (proc == IntPtr.Zero)
                return null;

            const int ProcessBasicInformation = 0;
            var size = Declarations.PROCESS_BASIC_INFORMATION.Size;
            IntPtr rawPbi = Marshal.AllocHGlobal(size);
            int ntstatus = Declarations.NtQueryInformationProcess(proc, ProcessBasicInformation, rawPbi, (uint)size, IntPtr.Zero);
            if (ntstatus != 0)
            {
                Marshal.FreeHGlobal(rawPbi);
                Declarations.CloseHandle(proc);
                return null;
            }
            var pbi = (Declarations.PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(rawPbi, typeof(Declarations.PROCESS_BASIC_INFORMATION));
            Marshal.FreeHGlobal(rawPbi);
            Declarations.PEB peb;
            if (!ReadProcessMemory<Declarations.PEB>(out peb, proc, pbi.PebBaseAddress))
            {
                Declarations.CloseHandle(proc);
                return null;
            }
            Declarations.RTL_USER_PROCESS_PARAMETERS rtl;
            if (!ReadProcessMemory<Declarations.RTL_USER_PROCESS_PARAMETERS>(out rtl, proc, peb.ProcessParameters))
            {
                Declarations.CloseHandle(proc);
                return null;
            }

            string commandLine = "";
            if (rtl.CommandLine.Buffer != IntPtr.Zero && rtl.CommandLine.Length > 0)
            {
                size = rtl.CommandLine.Length;
                IntPtr raw = Marshal.AllocHGlobal(size);
                byte[] buffer = new byte[size];
                IntPtr ignored;
                //Console.WriteLine("0x" + ((uint)rtl.CommandLine_Buffer).ToString("X8"));
                if (!Declarations.ReadProcessMemory(proc, rtl.CommandLine.Buffer, raw, size, out ignored))
                {
                    Marshal.FreeHGlobal(raw);
                    Declarations.CloseHandle(proc);
                    return null;
                }
                Marshal.Copy(raw, buffer, 0, size);
                Marshal.FreeHGlobal(raw);
                commandLine = Encoding.Unicode.GetString(buffer);
            }
            return commandLine;
        }

        public static string GetCommandLineFromPid(uint pid)
        {
            IntPtr proc = Declarations.OpenProcess(PROCESS_ALL_ACCESS, 0, pid);
            string ret;
            try
            {
                ret = GetCommandLineFromHandle(proc);
            }
            finally
            {
                Declarations.CloseHandle(proc);
            }
            return ret;
        }
    }
}
