using SCFrame;

namespace GameCore.Flow
{
    public class GameFlowGameplayState : StateBase
    {
        public override void OnEnter()
        {
            GameFlowController.instance.ShowGameplayUi();
        }

        public override void OnExit()
        {
            GameFlowController.instance.HideGameplayUi();
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
