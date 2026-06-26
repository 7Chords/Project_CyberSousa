using GameCore.Flow;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIPanelStart : _ASCUIPanelBase<UIMonoStart>
    {
        public UIPanelStart(UIMonoStart _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
        }

        public override void BeforeDiscard()
        {
        }

        public override void OnHidePanel()
        {
            if (mono.btnStart != null)
                mono.btnStart.RemoveMouseLeftClickDown(onBtnStartClicked);
            if (mono.btnContinue != null)
                mono.btnContinue.RemoveMouseLeftClickDown(onBtnContinueClicked);
            if (mono.btnSetting != null)
                mono.btnSetting.RemoveMouseLeftClickDown(onBtnSettingClicked);
            if (mono.btnExit != null)
                mono.btnExit.RemoveMouseLeftClickDown(onBtnExitClicked);
        }

        public override void OnShowPanel()
        {
            if (mono.btnStart != null)
                mono.btnStart.AddMouseLeftClickDown(onBtnStartClicked);
            if (mono.btnContinue != null)
                mono.btnContinue.AddMouseLeftClickDown(onBtnContinueClicked);
            if (mono.btnSetting != null)
                mono.btnSetting.AddMouseLeftClickDown(onBtnSettingClicked);
            if (mono.btnExit != null)
                mono.btnExit.AddMouseLeftClickDown(onBtnExitClicked);

            RefreshContinueButton();
        }

        private void onBtnStartClicked(PointerEventData _data, object[] _objs)
        {
            GamePlayerDataMgr.instance.BeginNewGame();
            GameFlowController.instance.EnterGameplay();
        }

        private void onBtnContinueClicked(PointerEventData _data, object[] _objs)
        {
            if (!GamePlayerDataMgr.instance.TryLoadSaveData())
            {
                SCDebugHelper.Log("没有可继续的存档。");
                RefreshContinueButton();
                return;
            }

            GameFlowController.instance.EnterGameplay();
        }

        private void onBtnExitClicked(PointerEventData arg1, object[] arg2)
        {
            SCDebugHelper.Log("退出游戏");
        }

        private void onBtnSettingClicked(PointerEventData arg1, object[] arg2)
        {
            UINodeMgr.instance.AddNode(new UINodeSetting(SCUIShowType.ADDITION, false));
        }

        private void RefreshContinueButton()
        {
            if (mono.btnContinue == null)
                return;

            SCCommon.SetGameObjectEnable(mono.btnContinue.gameObject, GamePlayerDataMgr.instance.hasSaveData);
        }
    }
}
