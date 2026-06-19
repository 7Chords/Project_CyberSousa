using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class LevelRefData : SCRefDataCore
    {
        public long id;
        public List<long> customerIdList;
        public List<long> ruleIdList;

        protected override void _parseFromString()
        {
            id = getLong("id");
            customerIdList = getList<long>("customerIdList");
            ruleIdList = getList<long>("ruleIdList");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "level";
    }
}
