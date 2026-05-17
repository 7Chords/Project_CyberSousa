using SCFrame;
using GameCore.RefData;

namespace GameCore
{
    /// <summary>
    /// 配表管理器
    /// </summary>
    public class SCRefDataMgr : Singleton<SCRefDataMgr>
    {

        public SCRefDataList<ItemRefData> itemRefList;
        public override void OnInitialize()
        {
            itemRefList = new SCRefDataList<ItemRefData>(ItemRefData.assetPath, ItemRefData.sheetName);
            itemRefList.readFromTxt();
        }
    }
}
