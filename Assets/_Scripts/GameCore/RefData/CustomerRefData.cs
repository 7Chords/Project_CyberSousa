using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class CustomerRefData : SCRefDataCore
    {
        public long id;
        public string customerName;
        public List<string> customerInfoList;
        public List<NeedEffectData> needList;

        protected override void _parseFromString()
        {
            id = getLong("id");
            customerName = getString("customerName");
            customerInfoList = getList<string>("customerInfoList");
            needList = getList<NeedEffectData>("needList");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "customer";
    }
}
