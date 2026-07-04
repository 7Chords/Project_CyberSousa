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
            SaveCurrentProgressIfNeeded();
            Discard();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentProgressIfNeeded();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveCurrentProgressIfNeeded();
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
            GamePlayerDataMgr.instance.Initialize();
            GameTimeMgr.instance.Initialize();
            RuleMgr.instance.Initialize();
            CustomerPoolMgr.instance.Initialize();
            CustomerNeedMgr.instance.Initialize();
            UINodeMgr.instance.Initialize();
            GameFlowController.instance.InitializeFlow();
            AudioMgr.instance.PlayBgm(AudioKeys.MainBgm);

            startGame();
        }

        public override void OnDiscard()
        {
            GameFlowController.instance.DiscardFlow();
            UINodeMgr.instance.Discard();
            CustomerNeedMgr.instance.Discard();
            CustomerPoolMgr.instance.Discard();
            RuleMgr.instance.Discard();
            GameTimeMgr.instance.Discard();
            GamePlayerDataMgr.instance.Discard();
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

        private static void SaveCurrentProgressIfNeeded()
        {
            UIPanelGameplayMain.SaveCurrentDayProgressIfActive();
        }
    }
}
