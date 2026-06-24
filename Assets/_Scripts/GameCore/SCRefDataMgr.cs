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
        public SCRefDataList<RuleRefData> ruleRefList;
        public GlobalConfigRefData globalConfigRefData;

        public override void OnInitialize()
        {
            globalConfigRefData = new GlobalConfigRefData();
            globalConfigRefData.readFromTxt();

            levelRefList = new SCRefDataList<LevelRefData>(LevelRefData.assetPath, LevelRefData.sheetName);
            levelRefList.readFromTxt();

            customerRefList = new SCRefDataList<CustomerRefData>(CustomerRefData.assetPath, CustomerRefData.sheetName);
            customerRefList.readFromTxt();

            customerNeedRefList = new SCRefDataList<CustomerNeedRefData>(CustomerNeedRefData.assetPath, CustomerNeedRefData.sheetName);
            customerNeedRefList.readFromTxt();

            dialogueRefList = new SCRefDataList<DialogueRefData>(DialogueRefData.assetPath, DialogueRefData.sheetName);
            dialogueRefList.readFromTxt();

            ruleRefList = new SCRefDataList<RuleRefData>(RuleRefData.assetPath, RuleRefData.sheetName);
            ruleRefList.readFromTxt();
        }
    }
}
