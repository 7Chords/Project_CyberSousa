using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class CustomerRefData : SCRefDataCore
    {
        public long id;
        public string customerName;
        public List<string> customerInfoList;
        public List<string> customerTagList;
        public List<NeedEffectData> needList;
        public string portraitFrontResName;
        public string portraitBackResName;

        protected override void _parseFromString()
        {
            id = getLong("id");
            customerName = getString("customerName");
            customerInfoList = getList<string>("customerInfoList");
            customerTagList = getList<string>("customerTagList");
            needList = getList<NeedEffectData>("needList");
            portraitFrontResName = getString("portraitFrontResName");
            portraitBackResName = getString("portraitBackResName");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "customer";
    }
}
