using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Aga.Controls.Tree;
using Microsoft.Win32;
using SpyStudio.Export.ThinApp;
using SpyStudio.FileSystem;
using SpyStudio.Main;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Export.Templates
{
    public class IntermediateTreeNode
    {
        public string Name;
        public bool IsLeaf;
        public bool? IsChecked;
        public bool IsRoot;
        public bool AllBranches;
        public bool AllLeaves;
        public bool IsImported;
        public ThinAppIsolationOption Isolation = ThinAppIsolationOption.Inherit;
        public IntermediateExtraData Data;
        public List<IntermediateTreeNode> Children = new List<IntermediateTreeNode>();
        public List<string> UncheckedDescendants;
        public List<string> CheckedDescendants;
        public IntermediateTreeNode()
        {
        }

        private IntermediateTreeNode(IntermediateListItem item)
        {
            Name = FileSystemTools.GetLastNameInPath(item.Path);
            IsLeaf = item.IsLeaf;
            IsChecked = item.IsChecked;
            Data = item.Data;
            Isolation = item.Isolation;
            IsRoot = item.Path.Length == 0;
            AllBranches = item.AllBranches;
            AllLeaves = item.AllLeaves;
            IsImported = item.IsImported;
            UncheckedDescendants = item.UncheckedDescendants;
            CheckedDescendants = item.CheckedDescendants;
        }

        private static IntermediateTreeNode ToIntermediateTreeNodeInternal(IEnumerable<IntermediateListItem> list)
        {
            var stack1 = new Stack<IntermediateTreeNode>();
            var stack2 = new Stack<string>();
            IntermediateTreeNode root = null;
            int i = 0;
            foreach (var intermediateListItem in list)
            {
                var s1 = intermediateListItem.Path + '\\';
                while (stack2.Count > 1)
                {
                    var s2 = stack2.Peek() + '\\';
                    if (s1.StartsWith(s2, StringComparison.InvariantCultureIgnoreCase))
                        break;
                    stack1.Pop();
                    stack2.Pop();
                }
                var node = new IntermediateTreeNode(intermediateListItem);
                if (root == null)
                    root = node;
                else if (stack1.Count > 0)
                    stack1.Peek().Children.Add(node);
                if (!intermediateListItem.IsLeaf)
                {
                    stack1.Push(node);
                    stack2.Push(intermediateListItem.Path);
                }
                i++;
            }
            return root;
        }

        private void NormalizeIsImported(bool inIsImported)
        {
            if (inIsImported)
                IsImported = false;
            foreach (var child in Children)
                child.NormalizeIsImported(inIsImported || IsImported);
        }
        
        private int MeasureTreeChromacity(bool previousColor)
        {
            int ret = IsChecked == null ? 0 : (IsChecked != previousColor ? 1 : 0);
            if (Children.Count == 0)
                return ret;
            int max = Children.Select(child => child.MeasureTreeChromacity(IsChecked == null ? previousColor : IsChecked.Value)).Max();
            return ret + max;
        }

        private void GetFirstLevelUncheckedDescendants(List<string> list, Stack<string> stack)
        {
            stack.Push(Name);
            if (IsChecked != null && !IsChecked.Value)
            {
                list.Add(StringTools.JoinStack(stack, 2));
            }
            else
            {
                foreach (var child in Children)
                    child.GetFirstLevelUncheckedDescendants(list, stack);
            }
            stack.Pop();
        }

        private void GetFirstLevelUncheckedDescendants(List<string> list)
        {
            GetFirstLevelUncheckedDescendants(list, new Stack<string>());
        }

        private void GetSecondLevelCheckedDescendants(List<string> list, Stack<string> stack, bool state)
        {
            stack.Push(Name);
            if (IsChecked != null && IsChecked.Value && !state)
            {
                list.Add(StringTools.JoinStack(stack, 2));
            }
            else
            {
                if (IsChecked != null && !IsChecked.Value)
                    state = false;
                foreach (var child in Children)
                    child.GetSecondLevelCheckedDescendants(list, stack, state);
            }
            stack.Pop();
        }

        private void GetSecondLevelCheckedDescendants(List<string> list)
        {
            GetSecondLevelCheckedDescendants(list, new Stack<string>(), true);
        }

        public void EliminateRedundantNodes()
        {
            if (!IsImported)
            {
                foreach (var intermediateTreeNode in Children)
                    intermediateTreeNode.EliminateRedundantNodes();
                return;
            }
            int chromacity = MeasureTreeChromacity(true);
            switch (chromacity)
            {
                case 0:
                    Children = null;
                    break;
                case 1:
                    UncheckedDescendants = new List<string>();
                    GetFirstLevelUncheckedDescendants(UncheckedDescendants);
                    Children = null;
                    break;
                case 2:
                    UncheckedDescendants = new List<string>();
                    GetFirstLevelUncheckedDescendants(UncheckedDescendants);
                    CheckedDescendants = new List<string>();
                    GetSecondLevelCheckedDescendants(CheckedDescendants);
                    Children = null;
                    break;
                default:
                    break;
            }
        }

        public static IntermediateTreeNode ToIntermediateTreeNode(IEnumerable<IntermediateListItem> list)
        {
            var ret = ToIntermediateTreeNodeInternal(list);
            ret.NormalizeIsImported(false);
            ret.EliminateRedundantNodes();
            return ret;
        }

        private static bool IsValidNameStartChar(char ch)
        {
            if (ch == '_')
                return true;
            if (ch >= 'A' && ch <= 'Z')
                return true;
            if (ch >= 'a' && ch <= 'z')
                return true;
            if (ch >= 0xC0 && ch <= 0xD6)
                return true;
            if (ch >= 0xD8 && ch <= 0xF6)
                return true;
            if (ch >= 0xF8 && ch <= 0x2FF)
                return true;
            if (ch >= 0x370 && ch <= 0x37D)
                return true;
            if (ch >= 0x37F && ch <= 0x1FFF)
                return true;
            if (ch >= 0x200C && ch <= 0x200D)
                return true;
            if (ch >= 0x2070 && ch <= 0x218F)
                return true;
            if (ch >= 0x2C00 && ch <= 0x2FEF)
                return true;
            if (ch >= 0x3001 && ch <= 0xD7FF)
                return true;
            if (ch >= 0xF900 && ch <= 0xFDCF)
                return true;
            if (ch >= 0xFDF0 && ch <= 0xFFFD)
                return true;
            return false;
        }

        private static bool IsValidNameChar(char ch)
        {
            if (IsValidNameStartChar(ch))
                return true;
            if (ch == '-' || ch == 0xB7)
                return true;
            if (ch >= '0' && ch <= '9')
                return true;
            if (ch >= 0x0300 && ch <= 0x036F)
                return true;
            if (ch >= 0x203F && ch <= 0x2040)
                return true;
            return false;
        }

        string MangleElementName(string name)
        {
            if (name.Length == 0)
                return name;
            var ret = new StringBuilder();

            if (!IsValidNameStartChar(name[0]) && IsValidNameChar(name[0]))
                ret.Append('_');
            foreach (var c in name)
                ret.Append(IsValidNameChar(c) ? c : '_');
            return ret.ToString();
        }

        string AllLeavesToString(IntermediateListItem.TreeType treeType)
        {
            switch (treeType)
            {
                case IntermediateListItem.TreeType.FileSystem:
                    return "AllFiles";
                case IntermediateListItem.TreeType.Registry:
                    return "AllValues";
            }
            throw new Exception();
        }

        string AllBranchesToString(IntermediateListItem.TreeType treeType)
        {
            switch (treeType)
            {
                case IntermediateListItem.TreeType.FileSystem:
                    return "AllSubFolders";
                case IntermediateListItem.TreeType.Registry:
                    return "AllSubKeys";
            }
            throw new Exception();
        }

        public void Write(XmlWriter xmlWriter, IntermediateListItem.TreeType treeType)
        {
            if (IsRoot)
            {
                xmlWriter.WriteStartElement("Root");
            }
            else
            {
                xmlWriter.WriteStartElement(string.IsNullOrEmpty(Name) ? "Node" : MangleElementName(Name));
                xmlWriter.WriteAttributeString("Name", Name);
                if (IsLeaf)
                    xmlWriter.WriteAttributeString("IsLeaf", IsLeaf.ToString(CultureInfo.InvariantCulture));
                if (IsChecked != null && !IsChecked.Value)
                    xmlWriter.WriteAttributeString("IsChecked", IsChecked.Value.ToString(CultureInfo.InvariantCulture));
                if (AllLeaves)
                    xmlWriter.WriteAttributeString(AllLeavesToString(treeType), AllLeaves.ToString(CultureInfo.InvariantCulture));
                if (AllBranches)
                    xmlWriter.WriteAttributeString(AllBranchesToString(treeType), AllBranches.ToString(CultureInfo.InvariantCulture));
            }
            if (Isolation != ThinAppIsolationOption.Inherit)
                xmlWriter.WriteAttributeString("Isolation", Isolation.ToString());
            if (UncheckedDescendants != null)
            {
                xmlWriter.WriteStartElement("UncheckedDescendants");
                foreach (var descendant in UncheckedDescendants)
                    xmlWriter.WriteElementString("UncheckedDescendant", descendant);
                xmlWriter.WriteEndElement();
            }
            if (CheckedDescendants != null)
            {
                xmlWriter.WriteStartElement("CheckedDescendants");
                foreach (var descendant in CheckedDescendants)
                    xmlWriter.WriteElementString("CheckedDescendant", descendant);
                xmlWriter.WriteEndElement();
            }
            if (Data != null)
                Data.Write(xmlWriter);
            if (Children != null)
                foreach (var intermediateTreeNode in Children)
                    intermediateTreeNode.Write(xmlWriter, treeType);
            xmlWriter.WriteEndElement();
        }
    }

    public class IntermediateListItem
    {
        public string Path;
        public bool IsLeaf;
        public bool? IsChecked;
        public bool AllBranches;
        public bool AllLeaves;
        private bool _isImported;
        public bool IsImported
        {
            get { return _isImported; }
            set
            {
                _isImported = value;
                AllLeaves =
                    AllBranches = _isImported;
            }
        }
        public ThinAppIsolationOption Isolation = ThinAppIsolationOption.Inherit;
        public IntermediateExtraData Data;
        public List<string> UncheckedDescendants;
        public List<string> CheckedDescendants;
        
        public static void ReadNodeAttributes(IntermediateListItem item, XmlReader xmlReader, Stack<string> stack, TreeType treeType)
        {
            item.IsLeaf = false;
            item.IsChecked = true;
            item.Isolation = ThinAppIsolationOption.Inherit;
            item.Path = StringTools.JoinStack(stack, 2);
            while (xmlReader.MoveToNextAttribute())
            {
                var name = xmlReader.Name;
                var value = xmlReader.Value;
                switch (name)
                {
                    case "Name":
                        stack.Push(value);
                        item.Path = StringTools.JoinStack(stack, 2);
                        if (item.Path.EndsWith(":"))
                            item.Path += "\\";
                        break;
                    case "IsLeaf":
                        item.IsLeaf = bool.Parse(value);
                        break;
                    case "IsChecked":
                        item.IsChecked = bool.Parse(value);
                        break;
                    case "AllFiles":
                    case "AllValues":
                        item.AllLeaves = bool.Parse(value);
                        break;
                    case "AllSubFolders":
                    case "AllSubKeys":
                        item.AllBranches = bool.Parse(value);
                        break;
                    case "Isolation":
                        item.Isolation = (ThinAppIsolationOption) Enum.Parse(typeof (ThinAppIsolationOption), value);
                        break;
                }
            }
        }

        public static void ReadValueData(IntermediateListItem item, XmlReader xmlReader)
        {
            bool read = false;
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name != "Data")
                            continue;
                        bool empty = xmlReader.IsEmptyElement;
                        if (!xmlReader.MoveToAttribute("Type"))
                            continue;
                        read = true;
                        item.Data = new IntermediateExtraData(xmlReader, !empty);
                        if (empty || item.Data.DataType == RegistryValueKind.MultiString)
                            return;
                        break;
                    case XmlNodeType.EndElement:
                        if (read)
                            return;
                        break;
                }
            }
        }

        public static void ReadNode(XmlReader xmlReader, Stack<string> stack, List<IntermediateListItem> list, TreeType treeType, bool readContents)
        {
            var item = new IntermediateListItem();
            list.Add(item);
            ReadNodeAttributes(item, xmlReader, stack, treeType);
            /*
            if (!readContents && item.IsLeaf && treeType == TreeType.Registry && Debugger.IsAttached)
                Debugger.Break();
            */
            if (item.IsLeaf && treeType == TreeType.Registry)
            {
                if (readContents)
                    ReadValueData(item, xmlReader);
                else
                {
                    item.Data = new IntermediateExtraData
                                    {
                                        ImportAtLoadTime = true
                                    };
                }
            }
            if (readContents)
                item.FindNode(xmlReader, stack, list, treeType);
            stack.Pop();
        }

        public static List<string> ReadDescendants(XmlReader xmlReader, string stringToCheck)
        {
            var singular = stringToCheck;
            var plural = stringToCheck + "s";
            var Break = false;
            var ret = new List<string>();
            while (xmlReader.Read() && !Break)
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == singular)
                        {
                            xmlReader.Read();
                            ret.Add(xmlReader.Value);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == plural)
                            Break = true;
                        break;
                }
            }
            return ret;
        }

        public void ReadUncheckedDescendants(XmlReader xmlReader)
        {
            UncheckedDescendants = ReadDescendants(xmlReader, "UncheckedDescendant");
        }

        public void ReadCheckedDescendants(XmlReader xmlReader)
        {
            CheckedDescendants = ReadDescendants(xmlReader, "CheckedDescendant");
        }

        public void FindNode(XmlReader xmlReader, Stack<string> stack, List<IntermediateListItem> list, TreeType treeType)
        {
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        var empty = xmlReader.IsEmptyElement;
                        if (AllBranches || AllLeaves)
                        {
                            switch (xmlReader.Name)
                            {
                                case "UncheckedDescendants":
                                    ReadUncheckedDescendants(xmlReader);
                                    continue;
                                case "CheckedDescendants":
                                    ReadCheckedDescendants(xmlReader);
                                    continue;
                            }
                        }
                        ReadNode(xmlReader, stack, list, treeType, !empty);
                        break;
                    case XmlNodeType.EndElement:
                        return;
                }
            }
        }

        public enum TreeType
        {
            FileSystem,
            Registry,
        }

        public static List<IntermediateListItem> Read(XmlReader xmlReader, TreeType treeType, bool Throw)
        {
            try
            {
                var stack = new Stack<string>();
                var ret = new List<IntermediateListItem>();
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType != XmlNodeType.Element)
                        continue;
                    if (xmlReader.Name != "Root")
                    {
                        xmlReader.Skip();
                        continue;
                    }
                    stack.Push(string.Empty);
                    ReadNode(xmlReader, stack, ret, treeType, !xmlReader.IsEmptyElement);
                    break;
                }
                return ret;
            }
            catch
            {
                if (Throw)
                    throw;
                return null;
            }
        }

        public static List<IntermediateListItem> Read(XmlReader xmlReader, TreeType treeType)
        {
            return Read(xmlReader, treeType, false);
        }

        public static List<IntermediateListItem> Create(FileSystemTree tree, PathNormalizer normalizer, ThinAppIsolationOption defaultIsolation)
        {
            var stack = new Stack<FileSystemTreeNode>();
            var ret = new List<IntermediateListItem>();

            ret.Add(new IntermediateListItem
            {
                IsLeaf = false,
                Path = "",
                IsChecked = true,
                Isolation = defaultIsolation,
            });

            foreach (var node in tree.Model.Nodes.Reverse())
                stack.Push((FileSystemTreeNode)node);

            while (stack.Count > 0)
            {
                var head = stack.Pop();
                {
                    
                    ret.Add(new IntermediateListItem
                                  {
                                      Path =
                                          GeneralizedPathNormalizer.GetInstance().Normalize(
                                              normalizer.Unnormalize(head.Path.AsNormalizedPath())),
                                      IsLeaf = !head.IsDirectoryOrBranch,
                                      IsChecked = head.IsChecked,
                                      Isolation = head.Isolation,
                                      IsImported = head.IsImported,
                                  });
                }
                foreach (var node in head.Nodes.Reverse())
                    stack.Push((FileSystemTreeNode)node);
            }

            ret.Sort((x, y) => StringTools.PathComparison(x.Path, y.Path));
            return ret;
        }

        public static List<IntermediateListItem> Create(RegistryTree tree, ThinAppIsolationOption defaultIsolation)
        {
            var ret = new List<IntermediateListItem>();

            ret.Add(new IntermediateListItem
                        {
                            IsLeaf = false,
                            Path = "",
                            IsChecked = true,
                            Isolation = defaultIsolation,
                        });

            var stack = new Stack<object>();
            foreach (var node in tree.Model.Nodes)
                stack.Push(node);

            while (stack.Count > 0)
            {
                var top = stack.Pop();
                var node = top as RegistryTreeNode;
                if (node != null)
                {
                    ret.Add(new IntermediateListItem
                                {
                                    Path = node.Path,
                                    IsLeaf = false,
                                    IsChecked = node.IsChecked,
                                    Isolation = node.Isolation,
                                    IsImported = node.IsImported,
                                });
                    foreach (var value in node.KeyInfo.ValuesByName.Values)
                        stack.Push(value);
                    foreach (var child in node.Nodes)
                        stack.Push(child);
                }
                else
                {
                    var value = top as RegValueInfo;
                    Debug.Assert(value != null);
                    ret.Add(new IntermediateListItem
                                {
                                    Path = value.Path + "\\" + value.Name,
                                    IsLeaf = true,
                                    IsChecked = true, //values are neither checked nor unchecked
                                    Data = new IntermediateExtraData(value),
                                });
                }
            }

            ret.Sort((x, y) => StringTools.PathComparison(x.Path, y.Path));
            return ret;
        }
    }

    public class IntermediateExtraData
    {
        public RegistryValueKind DataType;
        public string Data;
        public bool ImportAtLoadTime;
        public IntermediateExtraData()
        {
        }

        public IntermediateExtraData(XmlReader xmlReader, bool readContents)
        {
            DataType = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), xmlReader.Value);
            if (!readContents)
            {
                Data = string.Empty;
                return;
            }
            if (DataType == RegistryValueKind.MultiString)
                Data = ReadMultistring(xmlReader);
            else
            {
                xmlReader.Read();
                Data = xmlReader.Value;
                if (DataType == RegistryValueKind.DWord)
                {
                    var value = Convert.ToUInt32(Data);
                    Data = "0x" + value.ToString("X8") + "(" + Data + ")";
                }
                else if (DataType == RegistryValueKind.QWord)
                {
                    var value = Convert.ToUInt64(Data);
                    Data = "0x" + value.ToString("X16") + "(" + Data + ")";
                }
            }
        }
        public IntermediateExtraData(RegValueInfo info)
        {
            DataType = info.ValueType;
            Data = info.Data;
        }

        public static string ReadMultistring(XmlReader xmlReader)
        {
            var list = new List<string>();
            while (xmlReader.Read())
            {
                bool Break = false;
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        xmlReader.Read();
                        list.Add(xmlReader.Value);
                        xmlReader.Read();
                        break;
                    case XmlNodeType.EndElement:
                        Break = xmlReader.Name == "Data";
                        break;
                }
                if (Break)
                    break;
            }
            var ret = new StringBuilder();
            foreach (var s in list)
            {
                ret.Append(s);
                ret.Append('\0');
            }
            ret.Append('\0');
            return ret.ToString();
        }

        public static string XmlEscape(string unescaped)
        {
            var doc = new XmlDocument();
            var node = doc.CreateElement("root");
            node.InnerText = unescaped;
            return node.InnerXml;
        }

        public static string XmlUnescape(string escaped)
        {
            var doc = new XmlDocument();
            var node = doc.CreateElement("root");
            node.InnerXml = escaped;
            return node.InnerText;
        }

        private static bool IsValidXmlTextChar(char c)
        {
            var notInValidRange = c != 0x9 && c != 0xA && c != 0xD &&
                                   (c < 0x20 || c > 0xD7FF) &&
                                   (c < 0xE000 || c > 0xFFFD);
            return !(notInValidRange || c == '<' || c == '>' || c == '&');
        }

        private static bool IsValidXmlText(string s)
        {
            return s.All(IsValidXmlTextChar);
        }

        private static string EscapeString(string s)
        {
            var ret = new StringBuilder();
            foreach (var c in s)
            {
                if (c == '\\')
                {
                    ret.Append('\\');
                    continue;
                }
                if (IsValidXmlTextChar(c))
                {
                    ret.Append(c);
                    continue;
                }
                if (c < 0x100)
                {
                    ret.Append("\\x");
                    ret.Append(((uint) c).ToString("X2"));
                }
                else
                {
                    ret.Append("\\u");
                    ret.Append(((uint) c).ToString("X4"));
                }
            }
            return ret.ToString();
        }

        private static string ParseInt(string s)
        {
            int first = s.IndexOf('(');
            if (first == -1)
                return s;
            first++;
            int last = s.LastIndexOf(')');
            if (last == -1)
                return s;
            return s.Substring(first, last - first);
        }

        public void Write(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Data");
            xmlWriter.WriteAttributeString("Type", DataType.ToString());
            switch (DataType)
            {
                case RegistryValueKind.MultiString:
                    {
                        foreach (var s in Data.Split('\0'))
                        {
                            if (String.IsNullOrEmpty(s))
                                continue;
                            xmlWriter.WriteStartElement("String");
                            if (!IsValidXmlText(s))
                                xmlWriter.WriteAttributeString("Escaped", "True");
                            xmlWriter.WriteString(EscapeString(s));
                            xmlWriter.WriteEndElement();
                        }
                    }
                    break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    if (!IsValidXmlText(Data))
                        xmlWriter.WriteAttributeString("Escaped", "True");
                    xmlWriter.WriteString(EscapeString(Data));
                    break;
                case RegistryValueKind.DWord:
                case RegistryValueKind.QWord:
                    xmlWriter.WriteString(ParseInt(Data));
                    break;
                default:
                    xmlWriter.WriteString(Data);
                    break;
            }
            xmlWriter.WriteEndElement();
        }
    }
}
