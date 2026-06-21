using GameCore.Flow;
using GameCore.UI;
using SCFrame;

namespace GameCore
{
    /// <summary>
    /// 游戏初始化器
    /// </summary>
    public class GameInitializer : SingletonPersistent<GameInitializer>
    {

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Discard();
        }

        public override void OnInitialize()
        {
            SCSettingMgr.instance.Initialize();
            SCMsgCenter.instance.Initialize();
            SCTaskHelper.instance.Initialize();
            AudioMgr.instance.Initialize();
            SCPoolMgr.instance.Initialize();
            SCInputListener.instance.Initialize();
            SCRefDataMgr.instance.Initialize();
            RuleMgr.instance.Initialize();
            CustomerNeedMgr.instance.Initialize();
            UINodeMgr.instance.Initialize();
            GameFlowController.instance.InitializeFlow();

            startGame();
        }

        public override void OnDiscard()
        {
            GameFlowController.instance.DiscardFlow();
            UINodeMgr.instance.Discard();
            CustomerNeedMgr.instance.Discard();
            RuleMgr.instance.Discard();
            SCRefDataMgr.instance.Discard();
            SCInputListener.instance.Discard();
            SCPoolMgr.instance.Discard();
            AudioMgr.instance.Discard();
            SCTaskHelper.instance.Discard();
            SCMsgCenter.instance.Discard();
            SCSettingMgr.instance.Discard();
        }

        private void startGame()
        {
            GameFlowController.instance.EnterMainMenu();
        }
    }
}
