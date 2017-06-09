using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using SpyStudio.Dialogs;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Properties;
using SpyStudio.Registry.Controls;
using SpyStudio.Swv;
using SpyStudio.Tools;

namespace SpyStudio.Export.Templates
{
    public class PortableTemplate
    {
        public TemplateInfo Info = new TemplateInfo();
        private List<IntermediateListItem> _fileList = new List<IntermediateListItem>();
        private readonly List<IntermediateListItem> _looseFiles = new List<IntermediateListItem>();
        public readonly List<EntryPoint> EntryPoints = new List<EntryPoint>();
        public IntermediateTreeNode Registry = null;
        private readonly List<FileEntry> _fileDestinationEntries = new List<FileEntry>();
        public readonly List<SwvIsolationRuleEntry> ExplicitIsolationRules = new List<SwvIsolationRuleEntry>();
        public readonly List<string> Services = new List<string>();
        public int Priority;

        public IEnumerable<FileEntry>  GetFileDestinations(PathNormalizer normalizer)
        {
            return _fileDestinationEntries.Select(x => x.CopyForDeserialization(normalizer)).ToList();
        }

        public bool IsInUse { get; set; }

        public void SaveFilesSelected(FileSystemTree tree, PathNormalizer normalizer, ThinAppIsolationOption defaultIsolation)
        {
            _fileList = IntermediateListItem.Create(tree, normalizer,defaultIsolation);
        }

        public void SaveEntryPoints(IEnumerable<ListViewItem> items)
        {
            EntryPoints.Clear();
            foreach (var entryPointListViewItem in items)
            {
                var entryPoint = new EntryPoint((EntryPoint) entryPointListViewItem.Tag)
                                     {Checked = entryPointListViewItem.Checked};
                entryPoint.Location =
                    GeneralizedPathNormalizer.GetInstance().Normalize(
                        ThinAppPathNormalizer.GetInstance().Unnormalize(entryPoint.Location));
                entryPoint.FileSystemLocation =
                    GeneralizedPathNormalizer.GetInstance().Normalize(entryPoint.FileSystemLocation);
                EntryPoints.Add(entryPoint);
            }
        }

        public void SaveRegistry(RegistryTree tree, ThinAppIsolationOption defaultIsolation)
        {
            Registry = IntermediateTreeNode.ToIntermediateTreeNode(IntermediateListItem.Create(tree, defaultIsolation));
        }

        public void SaveFileDestinations(IEnumerable<FileEntry> entries, PathNormalizer normalizer)
        {
            _fileDestinationEntries.Clear();
            _fileDestinationEntries.AddRange(entries.Select(x => x.CopyForSerialization(normalizer)));
        }

        public void SaveExplicitIsolationRules(IEnumerable<SwvIsolationRuleEntry> ruleEntries)
        {
            ExplicitIsolationRules.Clear();
            ExplicitIsolationRules.AddRange(ruleEntries);
        }

        private void Read(XmlReader xml)
        {
            Services.Clear();
            while (xml.Read())
            {
                switch (xml.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (xml.Name)
                        {
                            case "TemplateInfo":
                                {
                                    var serializer = new XmlSerializer(typeof (TemplateInfo));
                                    Info = (TemplateInfo) serializer.Deserialize(xml);
                                }
                                break;
                            case "Files":
                                _fileList = IntermediateListItem.Read(xml, IntermediateListItem.TreeType.FileSystem);
                                break;
                            case "ImportFile":
                                {
                                    var file = new IntermediateListItem();
                                    file.IsChecked = true;
                                    file.IsLeaf = true;
                                    if (xml.MoveToAttribute("Src"))
                                    {
                                        file.Path = xml.Value;
                                        _looseFiles.Add(file);
                                    }
                                }
                                break;
                            case "EntryPoints":
                            case "EntryPoint":
                                if (xml.Name == "EntryPoint" || !xml.IsEmptyElement)
                                {
                                    while (xml.Name != "EntryPoint")
                                        xml.Read();
                                    var serializer = new XmlSerializer(typeof (EntryPoint));
                                    EntryPoints.Add((EntryPoint) serializer.Deserialize(xml));
                                }
                                break;
                            case "Registry":
                                Registry =
                                    IntermediateTreeNode.ToIntermediateTreeNode(IntermediateListItem.Read(xml,
                                                                                                          IntermediateListItem
                                                                                                              .TreeType.
                                                                                                              Registry));
                                break;
                            case "FileDestinations":
                            case "FileEntry":
                                if (xml.Name == "FileEntry" || !xml.IsEmptyElement)
                                {
                                    while (xml.Name != "FileEntry")
                                        xml.Read();
                                    var serializer = new XmlSerializer(typeof(FileEntry));
                                    _fileDestinationEntries.Add((FileEntry) serializer.Deserialize(xml));
                                }
                                break;
                            case "ExplicitIsolationRules":
                            case "SwvIsolationRuleEntry":
                                if (xml.Name == "SwvIsolationRuleEntry" || !xml.IsEmptyElement)
                                {
                                    while (xml.Name != "SwvIsolationRuleEntry")
                                        xml.Read();
                                    var serializer = new XmlSerializer(typeof(SwvIsolationRuleEntry));
                                    ExplicitIsolationRules.Add((SwvIsolationRuleEntry)serializer.Deserialize(xml));
                                }
                                break;
                            case "Services":
                            case "Service":
                                if (xml.Name == "Service" || !xml.IsEmptyElement)
                                {
                                    while (xml.Name != "Service")
                                        xml.Read();
                                    xml.Read();
                                    if (xml.NodeType == XmlNodeType.Text)
                                    {
                                        Services.Add(xml.Value);
                                    }
                                }
                                break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xml.Name == "Template")
                            return;
                        break;
                }
            }
        }

        public static PortableTemplate RestoreTemplate(StreamReader streamReader)
        {
            PortableTemplate ret = null;
            using (var xml = new XmlTextReader(streamReader))
            {
                bool Break = false;
                while (xml.Read() && !Break)
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xml.Name != "Template")
                            {
                                xml.Skip();
                                continue;
                            }
                            ret = new PortableTemplate();
                            if (xml.MoveToAttribute("Priority"))
                                ret.Priority = Convert.ToInt32(xml.Value);
                            ret.Read(xml);
                            break;
                        case XmlNodeType.EndElement:
                            if (xml.Name == "Template")
                                Break = true;
                            break;
                    }
                }
            }
            return ret;
        }

        private void SaveInfo(XmlWriter xmlWriter)
        {
            var serializer = new XmlSerializer(typeof(TemplateInfo));
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            serializer.Serialize(xmlWriter, Info, ns);
        }

        private void SaveFiles(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Files");
            var root = IntermediateTreeNode.ToIntermediateTreeNode(_fileList);
            root.Write(xmlWriter, IntermediateListItem.TreeType.FileSystem);
            xmlWriter.WriteEndElement();
        }

        private void SaveEntryPoints(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("EntryPoints");
            foreach (var entryPoint in EntryPoints)
            {
                var serializer = new XmlSerializer(typeof(EntryPoint));
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(xmlWriter, entryPoint, ns);
            }
            xmlWriter.WriteEndElement();
        }

        private void SaveRegistry(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Registry");
            Registry.Write(xmlWriter, IntermediateListItem.TreeType.Registry);
            xmlWriter.WriteEndElement();
        }

        private void SaveFileDestinations(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("FileDestinations");
            foreach (var fileDestinationEntry in _fileDestinationEntries)
            {
                var serializer = new XmlSerializer(typeof(FileEntry));
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(xmlWriter, fileDestinationEntry, ns);
            }
            xmlWriter.WriteEndElement();
        }

        private void SaveExplicitIsolationRules(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("ExplicitIsolationRules");
            foreach (var explicitIsolationRule in ExplicitIsolationRules)
            {
                var serializer = new XmlSerializer(typeof(SwvIsolationRuleEntry));
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(xmlWriter, explicitIsolationRule, ns);
            }
            xmlWriter.WriteEndElement();
        }

        public void SaveTemplate(StreamWriter streamWriter)
        {
            using (var xml = new XmlTextWriter(streamWriter)
                                 {
                                     Formatting = Formatting.Indented,
                                     Indentation = 1,
                                     IndentChar = '\t',
                                 })
            {
                xml.WriteStartElement("Template");
                SaveInfo(xml);
                SaveFiles(xml);
                SaveEntryPoints(xml);
                SaveRegistry(xml);
                SaveFileDestinations(xml);
                SaveExplicitIsolationRules(xml);
                xml.WriteEndElement();
            }
        }

        public void SaveWithDialog(Control parent)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    DefaultExt = "sts",
                    Filter = "Settings file (*.sts)|*.sts",
                    AddExtension = true,
                    RestoreDirectory = true,
                    Title = "Save Settings to File"
                };
                if (dialog.ShowDialog(parent) != DialogResult.OK)
                    return;

                if (File.Exists(dialog.FileName))
                    File.Delete(dialog.FileName);
                using (var file = new FileStream(dialog.FileName, FileMode.OpenOrCreate, FileAccess.Write))
                using (var writer = new StreamWriter(file))
                {
                    Info.SetToSaveAt(dialog.FileName);
                    SaveTemplate(writer);
                }
            }
            catch (IOException exception)
            {
                MessageBox.Show(parent,
                                "Saving the file failed. Error message: " + exception.Message,
                                Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public IEnumerable<IntermediateListItem> GetFiles()
        {
            return _fileList.Concat(_looseFiles);
        }

        private static IEnumerable<string> GetFilesInBranches(string path, bool alsoFiles)
        {
            if (alsoFiles)
                foreach (var file in Directory.GetFiles(path))
                    yield return file;
            foreach (var dir in Directory.GetDirectories(path))
                foreach (var branch in GetFilesInBranches(dir, true))
                    yield return branch;
        }

        public IEnumerable<string> GetFileList()
        {
            var files = GetFiles();
            var norm = GeneralizedPathNormalizer.GetInstance();
            var ret = new List<string>();
            foreach (var item in files)
            {
                if (string.IsNullOrEmpty(item.Path))
                    continue;
                var path = norm.Unnormalize(item.Path);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (item.IsLeaf)
                {
                    ret.Add(path);
                    continue;
                }

                try
                {
                    if (item.AllLeaves)
                        ret.AddRange(Directory.GetFiles(path));
                    if (item.AllBranches)
                        ret.AddRange(GetFilesInBranches(path, false));
                }
                catch (DirectoryNotFoundException e)
                {
                    Error.WriteLine(e.Message);
                }
            }
            return ret;
        }

        private static IEnumerable<string> GetMiddleDirectories(string path)
        {
            var dirs = path.SplitAsPath().ToArray();
            Debug.Assert(dirs.Length > 0);
            yield return dirs[0] + "\\";
            for (int i = 2; i < dirs.Length; i++)
                yield return dirs.Take(i).JoinPaths();
        }

        public IEnumerable<string> GetDirList()
        {
            var norm = GeneralizedPathNormalizer.GetInstance();
            var allowed = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var item in GetFiles())
            {
                if (string.IsNullOrEmpty(item.Path) || item.IsLeaf || !(item.AllLeaves || item.AllBranches))
                    continue;

                var path = norm.Unnormalize(item.Path);
                allowed.Add(path);
            }

            var allowedList = allowed.ToList();

            var ret = new List<string>();
            foreach (var item in GetFileList())
                ret.AddRange(GetMiddleDirectories(item).Where(x => allowedList.Any(x.StartsWith)));

            if (ret.Count == 0)
                yield break;

            ret.Sort(StringComparer.InvariantCultureIgnoreCase);
            yield return ret[0];
            for (var i = 1; i < ret.Count; i++)
                if (!ret[i].Equals(ret[i - 1], StringComparison.InvariantCultureIgnoreCase))
                    yield return ret[i];
        }

        public void OptimizeTrees()
        {
            /*
            var tree = FileTemplate.GetInitialTree();
            if (tree != null)
                tree.OptimizeTree();

            tree = RegistryTemplate.GetInitialTree();
            if (tree != null)
                tree.OptimizeTree();
            */
        }
    }
}
