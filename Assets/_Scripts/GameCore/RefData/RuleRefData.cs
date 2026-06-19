using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class RuleRefData : SCRefDataCore
    {
        public long id;
        public string ruleName;
        public string ruleDesc;
        public List<RuleEffectData> effectList;

        protected override void _parseFromString()
        {
            id = getLong("id");
            ruleName = getString("ruleName");
            ruleDesc = getString("ruleDesc");
            effectList = getList<RuleEffectData>("effectList");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "rule";
    }
}
