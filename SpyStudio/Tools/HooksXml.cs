using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace SpyStudio.Tools
{
    public class HookXml
    {
        static public XmlDocument GetHooksXml()
        {
            var hooks = new XmlDocument();

            var filepath = FileSystemTools.GetUserDataPath() + @"\hooks.txt";
            var loaded = false;

            if (File.Exists(filepath))
            {
                try
                {
                    var xmlReader = new XmlTextReader(filepath);
                    hooks.Load(xmlReader);
                    loaded = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(null, "Error loading hook information file: " + ex.Message + " " + filepath,
                        Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            if (!loaded)
            {
                try
                {
                    hooks.LoadXml(Properties.Resources.Hooks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(null, "Error loading default hook information file: " + ex.Message,
                        Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            return hooks;
        }
        static public void SaveHooksXml(XmlDocument hooks)
        {
            var filepath = FileSystemTools.GetUserDataPath() + @"\hooks.txt";
            try
            {
                hooks.Save(filepath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(null, "Error saving hook information file: " + ex.Message + " " + filepath,
                    Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}