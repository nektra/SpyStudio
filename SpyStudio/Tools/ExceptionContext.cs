using System;
using System.Runtime.InteropServices;
using Nektra.Deviare2;

namespace SpyStudio.Tools
{
    class ExceptionContext
    {
        public enum CONTEXT_FLAGS_i386 : uint
        {
            CONTEXT_i386 = 0x00010000,    // this assumes that i386 and
            CONTEXT_i486 = 0x00010000,    // i486 have identical context records
            CONTEXT_CONTROL = (CONTEXT_i386 | 0x00000001), // SS:SP, CS:IP, FLAGS, BP
            CONTEXT_INTEGER = (CONTEXT_i386 | 0x00000002), // AX, BX, CX, DX, SI, DI
            CONTEXT_SEGMENTS = (CONTEXT_i386 | 0x00000004), // DS, ES, FS, GS
            CONTEXT_FLOATING_POINT = (CONTEXT_i386 | 0x00000008), // 387 state
            CONTEXT_DEBUG_REGISTERS = (CONTEXT_i386 | 0x00000010), // DB 0-3,6,7
            CONTEXT_EXTENDED_REGISTERS = (CONTEXT_i386 | 0x00000020), // cpu specific extensions
            CONTEXT_FULL = (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS),
            CONTEXT_ALL = (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS),
            CONTEXT_XSTATE = (CONTEXT_i386 | 0x00000040)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FLOATING_SAVE_AREA_i386
        {
            public uint ControlWord;
            public uint StatusWord;
            public uint TagWord;
            public uint ErrorOffset;
            public uint ErrorSelector;
            public uint DataOffset;
            public uint DataSelector;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)] 
            public byte[] RegisterArea;
            public uint Cr0NpxState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONTEXT_i386
        {
            public uint ContextFlags;
            // This section is specified/returned if CONTEXT_DEBUG_REGISTERS is
            // set in ContextFlags.  Note that CONTEXT_DEBUG_REGISTERS is NOT
            // included in CONTEXT_FULL.
            public uint Dr0;
            public uint Dr1;
            public uint Dr2;
            public uint Dr3;
            public uint Dr6;
            public uint Dr7;
            // This section is specified/returned if the
            // ContextFlags word contians the flag CONTEXT_FLOATING_POINT.
            public FLOATING_SAVE_AREA_i386 FloatSave;
            // This section is specified/returned if the
            // ContextFlags word contians the flag CONTEXT_SEGMENTS.
            public uint SegGs;
            public uint SegFs;
            public uint SegEs;
            public uint SegDs;
            // This section is specified/returned if the
            // ContextFlags word contians the flag CONTEXT_INTEGER.
            public uint Edi;
            public uint Esi;
            public uint Ebx;
            public uint Edx;
            public uint Ecx;
            public uint Eax;
            // This section is specified/returned if the
            // ContextFlags word contians the flag CONTEXT_CONTROL.
            public uint Ebp;
            public uint Eip;
            public uint SegCs;              // MUST BE SANITIZED
            public uint EFlags;             // MUST BE SANITIZED
            public uint Esp;
            public uint SegSs;
            // This section is specified/returned if the ContextFlags word
            // contains the flag CONTEXT_EXTENDED_REGISTERS.
            // The format and contexts are processor specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] ExtendedRegisters;
        }

        //--------------------------------------------------------------------------------

        public enum CONTEXT_FLAGS_amd64  : uint
        {
            CONTEXT_AMD64 = 0x100000,
            CONTEXT_CONTROL = (CONTEXT_AMD64 | 0x1),
            CONTEXT_INTEGER = (CONTEXT_AMD64 | 0x2),
            CONTEXT_SEGMENTS = (CONTEXT_AMD64 | 0x4),
            CONTEXT_FLOATING_POINT = (CONTEXT_AMD64 | 0x8),
            CONTEXT_DEBUG_REGISTERS = (CONTEXT_AMD64 | 0x10),
            CONTEXT_FULL = (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT),
            CONTEXT_ALL = (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS),
            CONTEXT_XSTATE = (CONTEXT_AMD64 | 0x20),
            CONTEXT_EXCEPTION_ACTIVE = 0x8000000,
            CONTEXT_SERVICE_ACTIVE = 0x10000000,
            CONTEXT_EXCEPTION_REQUEST = 0x40000000,
            CONTEXT_EXCEPTION_REPORTING = 0x80000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct M128A_amd64
        {
            public ulong Low;
            public long High;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONTEXT_amd64
        {
            // Register parameter home addresses.
            //
            // N.B. These fields are for convience - they could be used to extend the
            //      context record in the future.
            public ulong P1Home;
            public ulong P2Home;
            public ulong P3Home;
            public ulong P4Home;
            public ulong P5Home;
            public ulong P6Home;
            // Control flags.
            public uint ContextFlags;
            public uint MxCsr;
            // Segment Registers and processor flags.
            public ushort SegCs;
            public ushort SegDs;
            public ushort SegEs;
            public ushort SegFs;
            public ushort SegGs;
            public ushort SegSs;
            public uint EFlags;
            // Debug registers
            public ulong Dr0;
            public ulong Dr1;
            public ulong Dr2;
            public ulong Dr3;
            public ulong Dr6;
            public ulong Dr7;
            // Integer registers.
            public ulong Rax;
            public ulong Rcx;
            public ulong Rdx;
            public ulong Rbx;
            public ulong Rsp;
            public ulong Rbp;
            public ulong Rsi;
            public ulong Rdi;
            public ulong R8;
            public ulong R9;
            public ulong R10;
            public ulong R11;
            public ulong R12;
            public ulong R13;
            public ulong R14;
            public ulong R15;
            // Program counter.
            public ulong Rip;
            // Floating point state.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] FltSave; //XMM_SAVE_AREA32
            /*
             * UNION {
                public fixed byte FltSave[512]; //XMM_SAVE_AREA32
                struct DUMMYSTRUCTNAME
                {
                    public fixed M128A Header[2];
                    public fixed M128A Legacy[8];
                    public M128A Xmm0;
                    public M128A Xmm1;
                    public M128A Xmm2;
                    public M128A Xmm3;
                    public M128A Xmm4;
                    public M128A Xmm5;
                    public M128A Xmm6;
                    public M128A Xmm7;
                    public M128A Xmm8;
                    public M128A Xmm9;
                    public M128A Xmm10;
                    public M128A Xmm11;
                    public M128A Xmm12;
                    public M128A Xmm13;
                    public M128A Xmm14;
                    public M128A Xmm15;
                };
             * }
             */
            // Vector registers.
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26*8)]
            public byte[] VectorRegister; //public fixed M128A VectorRegister[26];
            public ulong VectorControl;
            // Special debug control registers.
            public ulong DebugControl;
            public ulong LastBranchToRip;
            public ulong LastBranchFromRip;
            public ulong LastExceptionToRip;
            public ulong LastExceptionFromRip;
        }

        //--------------------------------------------------------------------------------

        public static bool ReadContext32(NktParam contextParam, out CONTEXT_i386 ctx)
        {
            bool bRet = false;

            ctx = new CONTEXT_i386();
            if (contextParam != null && contextParam.IsNullPointer == false)
            {
                IntPtr bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CONTEXT_i386)));
                IntPtr res = contextParam.Memory().ReadMem(bufPtr, contextParam.PointerVal, (IntPtr)Marshal.SizeOf(typeof(CONTEXT_i386)));
                if (res == (IntPtr)Marshal.SizeOf(typeof(CONTEXT_i386)))
                {
                    ctx = (CONTEXT_i386)Marshal.PtrToStructure(bufPtr, typeof(CONTEXT_i386));
                    bRet = true;
                }
                Marshal.FreeHGlobal(bufPtr);
            }
            return bRet;
        }

        public static bool ReadContext64(NktParam contextParam, out CONTEXT_amd64 ctx)
        {
            bool bRet = false;

            ctx = new CONTEXT_amd64();
            if (contextParam != null && contextParam.IsNullPointer == false)
            {
                IntPtr bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CONTEXT_amd64)));
                IntPtr res = contextParam.Memory().ReadMem(bufPtr, contextParam.PointerVal, (IntPtr)Marshal.SizeOf(typeof(CONTEXT_amd64)));
                if (res == (IntPtr)Marshal.SizeOf(typeof(CONTEXT_i386)))
                {
                    ctx = (CONTEXT_amd64)Marshal.PtrToStructure(bufPtr, typeof(CONTEXT_amd64));
                    bRet = true;
                }
                Marshal.FreeHGlobal(bufPtr);
            }
            return bRet;
        }
    }
}
