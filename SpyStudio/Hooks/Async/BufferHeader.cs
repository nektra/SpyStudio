using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class BufferHeader
    {
        public bool Active{ get; private set; }

        public UInt64 LongPid { get; private set; }
        public UInt64 OptionalMutex { get; private set; }
        public UInt64 Order { get; private set; }
        public const int UlongCount = 3;

        public UInt32 Length { get; private set; }
        public UInt32 EventCount { get; private set; }
        public const int UintCount = 2;

        public const int HeaderLength = 1 + 8 * UlongCount + 4 * UintCount;

        private static uint Read32Bits(byte[] buffer, int index)
        {
            uint size = 0;
            int k = HeaderLength - (UintCount - index) * 4;
            for (int i = 0; i < 4; i++)
                size |= (uint)buffer[i + k] << (i * 8);
            return size;
        }

        private static ulong Read64Bits(byte[] buffer, int index)
        {
            ulong ret = 0;
            int k = 1 + index*8;
            for (int i = 0; i < 8; i++)
                ret |= (ulong)buffer[i + k] << (i * 8);
            return ret;
        }

        private void ReadHeader(byte[] header)
        {
            if (header == null || header.Length < HeaderLength)
                throw new Exception("Bad buffer header.");

            Active = header[0] != 0;

            LongPid = Read64Bits(header, 0);
            OptionalMutex = Read64Bits(header, 1);
            Order = Read64Bits(header, 2);

            Length = Read32Bits(header, 0);
            EventCount = Read32Bits(header, 1);
        }

        private static byte[] ReadBufferHeader(IntPtr handle)
        {
            IntPtr memory = IntPtr.Zero;
            const int tiny = HeaderLength;
            memory = Declarations.MapViewOfFile(handle, Declarations.FileMapAccess.FileMapAllAccess,
                                                0, 0,
                                                (UIntPtr)tiny);
            if (memory == IntPtr.Zero)
                return null;
            try
            {
                var ret = new byte[tiny];
                Marshal.Copy(memory, ret, 0, tiny);
                return ret;
            }
            finally
            {
                Declarations.UnmapViewOfFile(memory);
            }
        }


        public BufferHeader(IntPtr handle)
        {
            ReadHeader(ReadBufferHeader(handle));
        }

        public BufferHeader(byte[] header)
        {
            ReadHeader(header);
        }
    }
}
