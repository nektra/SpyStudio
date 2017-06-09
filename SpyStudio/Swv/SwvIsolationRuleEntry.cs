using System.Xml.Serialization;
using SpyStudio.Dialogs.ExportWizards;

namespace SpyStudio.Swv
{
    public class SwvIsolationRuleEntry
    {
        [XmlAttribute]
        public SwvIsolationRuleType Type { get; set; }
        [XmlAttribute]
        public string ProcessWildcard { get; set; }
        [XmlAttribute]
        public string KeyWildcard { get; set; }

        public SwvIsolationRuleEntry()
        {
        }

        public SwvIsolationRuleEntry(IsolationRulesSelect.RuleNode n)
        {
            Type = n.Type == SwvIsolationRuleType.BaseCannotSeeLayerKey.ToString()
                       ? SwvIsolationRuleType.BaseCannotSeeLayerKey
                       : SwvIsolationRuleType.LayerCannotSeeBaseKey;
            ProcessWildcard = n.ProcessWildcard;
            KeyWildcard = n.KeyWildcard;
        }

        public static SwvIsolationRuleEntry From(string aRuleAsString)
        {
            var ruleValues = aRuleAsString.Split('\t');

            return new SwvIsolationRuleEntry
                {
                    Type = ruleValues[1] == "BASE" ? SwvIsolationRuleType.BaseCannotSeeLayerKey : SwvIsolationRuleType.LayerCannotSeeBaseKey,
                    ProcessWildcard = ruleValues[0],
                    KeyWildcard = ruleValues[3].Substring("\\REGISTRY\\".Length)
                };
        }

        public void SetNode(IsolationRulesSelect.RuleNode node)
        {
            node.Type = Type.ToString();
            node.ProcessWildcard = ProcessWildcard;
            node.KeyWildcard = KeyWildcard;
        }
    }
}