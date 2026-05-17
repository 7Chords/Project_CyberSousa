using GameCore.UI;
using SCFrame;

namespace GameCore
{
    /// <summary>
    /// ÓÎÏ·³õÊ¼»¯Æ÷
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
            SCMsgCenter.instance.Initialize();
            SCTaskHelper.instance.Initialize();
            SCPoolMgr.instance.Initialize();
            SCInputListener.instance.Initialize();
            SCRefDataMgr.instance.Initialize();
            UINodeMgr.instance.Initialize();

            startGame();
        }

        public override void OnDiscard()
        {
            UINodeMgr.instance.Discard();
            SCRefDataMgr.instance.Discard();
            SCInputListener.instance.Discard();
            SCPoolMgr.instance.Discard();
            SCTaskHelper.instance.Discard();
            SCMsgCenter.instance.Discard();
        }

        private void startGame()
        {
            UINodeMgr.instance.AddNode(new UINodeStart(SCFrame.UI.SCUIShowType.FULL));
        }
    }
}
