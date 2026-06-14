using System.Collections;
using System.Collections.Generic;
using GameCore;
using GameCore.UI;
using SCFrame;
using UnityEngine;

public class UIListInit : SingletonPersistent<UIListInit>
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
            // Loop list demo startup entry.
             UINodeMgr.instance.AddNode(new UINodeLoopListDemo(SCFrame.UI.SCUIShowType.FULL));
        }
}
