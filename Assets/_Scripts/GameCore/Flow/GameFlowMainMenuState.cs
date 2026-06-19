using SCFrame;

namespace GameCore.Flow
{
    public class GameFlowMainMenuState : StateBase
    {
        public override void OnEnter()
        {
            GameFlowController.instance.ShowMainMenuUi();
        }

        public override void OnExit()
        {
            GameFlowController.instance.HideMainMenuUi();
        }

        public override void OnUpdate()
        {
        }

        public override void OnLateUpdate()
        {
        }

        public override void OnFixedUpdate()
        {
        }
    }
}
