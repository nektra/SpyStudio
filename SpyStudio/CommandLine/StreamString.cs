using System;
using System.IO;
using System.Text;

namespace SpyStudio.CommandLine
{
    public class StreamString
    {
        private readonly Stream _ioStream;
        private readonly UnicodeEncoding _streamEncoding;

        public StreamString(Stream ioStream)
        {
            _ioStream = ioStream;
            _streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = _ioStream.ReadByte() * 256;
            len += _ioStream.ReadByte();
            var inBuffer = new byte[len];
            _ioStream.Read(inBuffer, 0, len);

            return _streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = _streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = UInt16.MaxValue;
            }
            _ioStream.WriteByte((byte)(len / 256));
            _ioStream.WriteByte((byte)(len & 255));
            _ioStream.Write(outBuffer, 0, len);
            _ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
    public class ReadFileToStream
    {
        private readonly string _fn;
        private readonly StreamString _ss;

        public ReadFileToStream(StreamString str, string filename)
        {
            _fn = filename;
            _ss = str;
        }

        public void Start()
        {
            string contents = File.ReadAllText(_fn);
            _ss.WriteString(contents);
        }
    }
}
