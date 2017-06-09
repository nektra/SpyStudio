using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Win32;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV.Manifest
{
    public class AssetIntelligence : XmlGenerator
    {
        [OpenList]
        public List<AssetIntelligenceProperties> List;

        public bool AnyFileIsUnderThatDirectory(List<string> files, string directory)
        {
            directory = AppvPathNormalizer.GetInstanceManifest().Unnormalize(directory);
            directory = FileSystemTools.GetFullPath(directory);
            return files.Any(x => x.StartsWith(directory));
        }

        public AssetIntelligence() { }

        public AssetIntelligence(List<string> files)
        {
            var key =
                RegistryTools.GetKeyFromFullPath(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products");
            if (key == null)
                return;

            List = key
                .GetSubKeyNames()
                .Select(x => key.OpenSubKey(x))
                .Where(x => x != null)
                .Select(x => new AssetIntelligenceProperties(x))
                .Where(x => AnyFileIsUnderThatDirectory(files, x.InstalledLocation))
                .ToList();
        }
    }

    public class AssetIntelligenceProperties : XmlGenerator
    {
        //All XML elements. All in appv namespace.

        public CurlyGuid SoftwareCode;

        public string ProductName,
                      ProductVersion,
                      Publisher,
                      ProductID,
                      //Unknown.
                      ChannelCode = null,
                      //Unknown
                      RegisteredUser = null,
                      //App-V-normalized path.
                      InstalledLocation,
                      //Unknown
                      CM_DSLID = null,
                      //Unknown
                      ServicePack = null,
                      //Unknown
                      UpgradeCode = null;
        //Format: YYYYMMDD
        public DateTime? InstallDate;

        public int Language,
                   VersionMajor,
                   VersionMinor,
                   OsComponent = 1;

        public AssetIntelligenceProperties() { }

        public AssetIntelligenceProperties(RegistryKey key)
        {
            SoftwareCode = new CurlyGuid(StringTools.UnpackGuid(key.GetKeyName()));
            var installPropertiesKey = key.OpenSubKey("InstallProperties");
            if (installPropertiesKey == null)
                return;

            ProductName = installPropertiesKey.GetStringValueForAppVManifest("DisplayName");
            ProductVersion = installPropertiesKey.GetStringValueForAppVManifest("DisplayVersion");
            Publisher = installPropertiesKey.GetStringValueForAppVManifest("Publisher");
            ProductID = installPropertiesKey.GetStringValueForAppVManifest("ProductID");
            InstalledLocation = installPropertiesKey.GetStringValueForAppVManifest("InstallLocation");
            ProductID = installPropertiesKey.GetStringValueForAppVManifest("ProductID");
            InstallDate = StringTools.ToDateTimeYYYYMMDD(installPropertiesKey.GetStringValueForAppVManifest("InstallDate"));

            installPropertiesKey.GetIntValue("Language", out Language);
            installPropertiesKey.GetIntValue("VersionMajor", out VersionMajor);
            installPropertiesKey.GetIntValue("VersionMinor", out VersionMinor);
            installPropertiesKey.GetIntValue("SystemComponent", out OsComponent);
        }
    }
}