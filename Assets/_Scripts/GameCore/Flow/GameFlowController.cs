using GameCore.UI;
using SCFrame;
using SCFrame.UI;

namespace GameCore.Flow
{
    public class GameFlowController : Singleton<GameFlowController>, IStateMachineOwner
    {
        private const string MainMenuNodeName = "UINodeStart";
        private const string GameplayNodeName = "UINodeGameplayMain";

        private StateMachine _stateMachine;

        public void InitializeFlow()
        {
            if (_stateMachine != null)
                return;

            _stateMachine = SCPoolMgr.instance.GetObject<StateMachine>();
            _stateMachine.Initialize();
            _stateMachine.SetOwner(this);
        }

        public void DiscardFlow()
        {
            if (_stateMachine == null)
                return;

            _stateMachine.Discard();
            _stateMachine = null;
        }

        public void EnterMainMenu()
        {
            InitializeFlow();
            _stateMachine.ChangeState<GameFlowMainMenuState>((int)GameFlowStateType.MainMenu, true);
        }

        public void EnterGameplay()
        {
            InitializeFlow();
            _stateMachine.ChangeState<GameFlowGameplayState>((int)GameFlowStateType.Gameplay, true);
        }

        internal void ShowMainMenuUi()
        {
            if (UINodeMgr.instance.GetNodeByName(MainMenuNodeName) == null)
            {
                UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoStart, UIPanelStart>(
                    SCUIShowType.FULL,
                    "panel_start",
                    MainMenuNodeName,
                    true,
                    true,
                    false,
                    false));
                return;
            }

            UINodeMgr.instance.ShowNode(MainMenuNodeName);
        }

        internal void HideMainMenuUi()
        {
            if (UINodeMgr.instance.GetNodeByName(MainMenuNodeName) != null)
                UINodeMgr.instance.RemoveNode(MainMenuNodeName);
        }

        internal void ShowGameplayUi()
        {
            if (UINodeMgr.instance.GetNodeByName(GameplayNodeName) == null)
            {
                UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoGameplayMain, UIPanelGameplayMain>(
                    SCUIShowType.FULL,
                    "panel_gameplay_main",
                    GameplayNodeName,
                    true,
                    true,
                    false,
                    false));
                return;
            }

            UINodeMgr.instance.ShowNode(GameplayNodeName);
        }

        internal void HideGameplayUi()
        {
            if (UINodeMgr.instance.GetNodeByName(GameplayNodeName) != null)
                UINodeMgr.instance.RemoveNode(GameplayNodeName);
        }
    }
}
