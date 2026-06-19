using SCFrame;

namespace GameCore.RefData
{
    public class RuleRefData : SCRefDataCore
    {
        public long id;
        public string ruleName;
        public string ruleDesc;

        protected override void _parseFromString()
        {
            id = getLong("id");
            ruleName = getString("ruleName");
            ruleDesc = getString("ruleDesc");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "rule";
    }
}
