using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class LevelRefData : SCRefDataCore
    {
        public long id;
        public List<CustomerEffectData> customerEffectList;
        public List<long> ruleIdList;

        protected override void _parseFromString()
        {
            id = getLong("id");
            customerEffectList = getList<CustomerEffectData>("customerEffectList");
            ruleIdList = getList<long>("ruleIdList");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "level";
    }
}
