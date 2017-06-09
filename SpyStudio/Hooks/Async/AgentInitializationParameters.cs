using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ProtoBuf;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    [ProtoContract]
    public class AgentInitializationParameters
    {
        [ProtoMember(1, IsRequired = true)]
        public UInt32 ServerPid;
        [ProtoMember(2, IsRequired = true)]
        public UInt32 HookFlags;
        [ProtoMember(3, IsRequired = false)]
        public UInt64? PrimaryHook;
        [ProtoMember(4, IsRequired = false)]
        public string Xml;
        [ProtoMember(5)]
        public List<UInt32> Offsets32;
        [ProtoMember(6, IsRequired = false)]
        public byte[] Buffer32;
        [ProtoMember(7)]
        public List<UInt32> Offsets64;
        [ProtoMember(8, IsRequired = false)]
        public byte[] Buffer64;
        [ProtoMember(9)]
        public List<string> SystemModules;

        private bool _uniqueDataSent = false;
        public string Serialize()
        {
            if (!_uniqueDataSent)
            {
                GenerateBuffer(32, out Offsets32, out Buffer32);
                if (IntPtr.Size > 4)
                    GenerateBuffer(64, out Offsets64, out Buffer64);
                SystemModules = DeviareTools.SystemModulesList;
                _uniqueDataSent = true;
            }
            string ret;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, this);
                var sb = new StringBuilder();
                foreach (var b in ms.ToArray())
                    sb.Append((char)((int)b + 1));
                ret = sb.ToString();
            }
            Xml = null;
            PrimaryHook = null;
            return ret;
        }

        static void GenerateBuffer(int bits, out List<UInt32> offsets, out byte[] buffer)
        {
            var apis = new[]
                           {
                               new OriginalNtCallBuilder.API
                                   {
                                       name = "NtDuplicateObject",
                                   },
                               new OriginalNtCallBuilder.API
                                   {
                                       name = "NtDelayExecution",
                                   },
                               new OriginalNtCallBuilder.API
                                   {
                                       name = "NtQueryKey",
                                   },
                               new OriginalNtCallBuilder.API
                                   {
                                       name = "NtClose",
                                   },
                           };
            OriginalNtCallBuilder.Create(apis, bits, out buffer);
            offsets = apis.Select(x => (uint) x.offset).ToList();
        }
    }
}
