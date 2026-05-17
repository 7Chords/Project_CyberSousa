using SCFrame;

namespace GameCore.RefData
{
    public class ItemRefData : SCRefDataCore
    {
        public long id;
        public string itemName;
        protected override void _parseFromString()
        {
            id = getLong("id");
            itemName = getString("itemName");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "item";

    }
}
