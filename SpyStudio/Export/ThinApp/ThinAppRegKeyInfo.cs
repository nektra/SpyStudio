using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SpyStudio.Registry;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppRegKeyInfo : RegKeyInfo
    {
        [XmlElement]
        public ThinAppIsolationOption Isolation;

        public ThinAppRegKeyInfo()
        {
            Isolation = ThinAppIsolationOption.Inherit;
        }

        public static ThinAppRegKeyInfo From(RegKeyInfo aKeyInfo)
        {
            var thinnAppKeyInfo = new ThinAppRegKeyInfo();

            thinnAppKeyInfo.InitializeAsCopyOf(aKeyInfo);

            return thinnAppKeyInfo;
        }

        public override SerializableRegInfo CreateSerializable()
        {
            return new SerializableThinAppRegKeyInfo();
        }
        public override SerializableRegInfo Serialize()
        {
            return SerializableThinAppRegKeyInfo.Serialize(this);
        }
    }
}
