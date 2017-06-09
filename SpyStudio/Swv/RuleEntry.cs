using System.Xml.Serialization;
using SpyStudio.Dialogs.ExportWizards;

namespace SpyStudio.Swv
{
    [XmlRoot]
    public class RuleEntry
    {
        public enum RuleType
        {
            BaseCannotSeeLayerKey,
            LayerCannotSeeBaseKey
        }

        public RuleEntry()
        {
        }

        public RuleEntry(IsolationRulesSelect.RuleNode node)
        {
            Type = node.Type == RuleType.BaseCannotSeeLayerKey.ToString()
                       ? RuleType.BaseCannotSeeLayerKey
                       : RuleType.LayerCannotSeeBaseKey;
            ProcessWildcard = node.ProcessWildcard;
            KeyWildcard = node.KeyWildcard;
        }

        public void SetNode(IsolationRulesSelect.RuleNode node)
        {
            node.Type = Type.ToString();
            node.ProcessWildcard = ProcessWildcard;
            node.KeyWildcard = KeyWildcard;
        }

        [XmlAttribute]
        public RuleType Type { get; set; }
        [XmlAttribute]
        public string ProcessWildcard { get; set; }
        [XmlAttribute]
        public string KeyWildcard { get; set; }
    }
}