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
            //可以用通用的Node 也可以自己写对应的Node（如果有额外需求的话）

            //UINodeMgr.instance.AddNode(new UINodeStart(SCFrame.UI.SCUIShowType.FULL));
            UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoStart, UIPanelStart>
                (SCFrame.UI.SCUIShowType.FULL, 
                "panel_start", 
                "UINodeStart", 
                true, 
                true,
                false,
                false));
        }
    }
}
