using SCFrame;
using GameCore.RefData;

namespace GameCore
{
    /// <summary>
    /// 配表管理器
    /// </summary>
    public class SCRefDataMgr : Singleton<SCRefDataMgr>
    {

        public SCRefDataList<LevelRefData> levelRefList;
        public SCRefDataList<CustomerRefData> customerRefList;
        public SCRefDataList<CustomerNeedRefData> customerNeedRefList;
        public SCRefDataList<DialogueRefData> dialogueRefList;

        public override void OnInitialize()
        {

            levelRefList = new SCRefDataList<LevelRefData>(LevelRefData.assetPath, LevelRefData.sheetName);
            levelRefList.readFromTxt();

            customerRefList = new SCRefDataList<CustomerRefData>(CustomerRefData.assetPath, CustomerRefData.sheetName);
            customerRefList.readFromTxt();

            customerNeedRefList = new SCRefDataList<CustomerNeedRefData>(CustomerNeedRefData.assetPath, CustomerNeedRefData.sheetName);
            customerNeedRefList.readFromTxt();

            dialogueRefList = new SCRefDataList<DialogueRefData>(DialogueRefData.assetPath, DialogueRefData.sheetName);
            dialogueRefList.readFromTxt();
        }
    }
}
