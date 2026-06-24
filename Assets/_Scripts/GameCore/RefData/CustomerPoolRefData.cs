using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class CustomerPoolRefData : SCRefDataCore
    {
        public long id;
        public List<long> customerIdList;

        protected override void _parseFromString()
        {
            id = getLong("id");
            customerIdList = getList<long>("customerIdList");
        }

        public static readonly string assetPath = "RefData/ExportTxt";
        public static readonly string sheetName = "customerPool";
    }
}
