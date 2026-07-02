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
        public SCRefDataList<GameplayConfigRefData> gameplayConfigRefList;

        public GameplayConfigRefData gameplayConfig
        {
            get
            {
                if (gameplayConfigRefList == null || gameplayConfigRefList.refDataList == null || gameplayConfigRefList.refDataList.Count <= 0)
                    return null;

                return gameplayConfigRefList.refDataList[0];
            }
        }

        public override void OnInitialize()
        {
            itemRefList = new SCRefDataList<ItemRefData>(ItemRefData.assetPath, ItemRefData.sheetName);
            itemRefList.readFromTxt();

            gameplayConfigRefList = new SCRefDataList<GameplayConfigRefData>(GameplayConfigRefData.assetPath, GameplayConfigRefData.sheetName);
            gameplayConfigRefList.readFromTxt();
        }
    }
}
