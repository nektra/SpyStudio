using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SpyStudio.Tools;
using SpyStudio.Tools.Registry;

namespace SpyStudio.Export.AppV
{
    public static class DarwinPathDecoder
    {
        private static void DecodeShortcut(string shortcut, out Guid? productId, out string featureName, out Guid? componentId, out string restOfShortcut)
        {
            var separator = '\0';
            if (shortcut.Contains('<'))
            {
                if (shortcut.Contains('>'))
                {
                    var a = shortcut.IndexOf('>');
                    var b = shortcut.IndexOf('<');
                    separator = a < b ? '>' : '<';
                }
                else
                    separator = '<';
            }
            else if (shortcut.Contains('>'))
                separator = '>';
            productId = null;
            featureName = null;
            componentId = null;
            restOfShortcut = shortcut;
            if (separator == '\0')
                return;
            productId = DarwinDecoder.Decode(shortcut);
            var index = shortcut.IndexOf(separator);
            featureName = shortcut.Substring(20, index - 20);
            if (featureName.Length == 0)
                featureName = null;
            index++;
            if (separator == '>')
                componentId = DarwinDecoder.Decode(shortcut.Substring(index));
            restOfShortcut = shortcut.Substring(index + 20);
        }

        static string GetComponentPath(Guid componentId, Guid productId)
        {
            var key =
                RegistryTools.GetKeyFromFullPath(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\" +
                    StringTools.PackGuid(componentId));
            if (key == null)
                return null;

            var value = key.GetStringValue(StringTools.PackGuid(productId));
            if (value == null)
                return null;
            return value;
        }

        static string GetProductPath(Guid guid)
        {
            var key =
                RegistryTools.GetKeyFromFullPath(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\" +
                    StringTools.PackGuid(guid) + @"\InstallProperties");
            if (key == null)
                return null;

            var value = key.GetStringValue("InstallLocation");
            if (value == null)
                return null;
            return value;
        }

        private static string ExpandProductAndComponent(Guid? productId, Guid? componentId)
        {
            Debug.Assert(productId != null, "productId != null");
            if (componentId == null)
                return GetProductPath(productId.Value);
            return GetComponentPath(componentId.Value, productId.Value);
        }

        public static string ExpandDarwinDescriptor(string path)
        {
            Guid? productId, componentId;
            string restOfShortcut, featureName;
            DecodeShortcut(path, out productId, out featureName, out componentId, out restOfShortcut);
            if (productId == null)
                return path;

            var expanded = ExpandProductAndComponent(productId, componentId);
            return "\"" + expanded + "\"" + (restOfShortcut ?? "");
        }

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        public extern static uint MsiGetShortcutTargetW(string szShortcutTarget, StringBuilder szProductCode, StringBuilder szFeatureId, StringBuilder szComponentCode);

        public static string GetMsiLinkTarget(string path)
        {
            var product = new StringBuilder(40);
            var feature = new StringBuilder(8192);
            var component = new StringBuilder(40);
            var result = MsiGetShortcutTargetW(path, product, feature, component);
            if (result != 0)
                return null;

            var productId = new Guid(product.ToString());
            var componentId = component.Length != 0 ? new Guid(component.ToString()) : (Guid?)null;
            return ExpandProductAndComponent(productId, componentId);
        }
    }
}
