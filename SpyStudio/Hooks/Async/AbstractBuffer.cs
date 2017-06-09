using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpyStudio.Hooks.Async
{
    public abstract class AbstractBuffer
    {
        public BufferHeader Header;
        public abstract byte[] Buffer { get; }
        public abstract bool Discard { get; }

        protected AbstractBuffer(BufferHeader header)
        {
            Header = header;
        }

        public virtual bool IsSimulated { get { return false; } }

        public abstract void DiscardBuffer();
    }

    public class CheapSharedBuffer : AbstractBuffer
    {
        public IntPtr Handle;
        private byte[] _buffer;
        public override byte[] Buffer
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = AsyncHookMgr.ReadAndDiscardEntireBuffer(Handle, Header.Length);
                    Handle = IntPtr.Zero;
                    if (_buffer == null)
                        throw new Exception("Something has gone horribly wrong.");
                }
                return _buffer;
            }
        }

        public override void DiscardBuffer()
        {
            if (Handle == IntPtr.Zero)
                return;
            AsyncHookMgr.DiscardBuffer(Handle);
            Handle = IntPtr.Zero;
        }
        
        public bool IsClear { get; private set; }
        private readonly bool _discard;
        public override bool Discard
        {
            get { return _discard; }
        }
        
        public CheapSharedBuffer(IntPtr handle, BufferHeader header, bool discard): base(header)
        {
            Handle = handle;
            _discard = discard;
        }
    }

    public class SimulatedBuffer : AbstractBuffer
    {
        private long _rowId;
        private ulong _longPid;
        private BufferHeader _header;
        private byte[] _buffer;

        public override byte[] Buffer
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = BufferFileSource.ReadBuffer(_rowId);
                    if (_buffer == null)
                        throw new Exception("Error restoring buffer from file.");
                }
                return _buffer;
            }
        }

        public override bool Discard
        {
            get { return false; }
        }

        public override void DiscardBuffer(){}

        public SimulatedBuffer(BufferFileSource.Buffer buffer): base(buffer.Header)
        {
            _rowId = buffer.RowId;
            _longPid = buffer.LongPid;
            _header = buffer.Header;
        }

        public override bool IsSimulated { get { return true; } }

    }
}
