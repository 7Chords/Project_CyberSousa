using SCFrame;
using SCFrame.UI;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    public class UIPanelStart : _ASCUIPanelBase<UIMonoStart>
    {
        public UIPanelStart(UIMonoStart _mono, SCUIShowType _showType) : base(_mono, _showType) { }

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
            if (mono.btnSetting != null)
                mono.btnSetting.RemoveMouseLeftClickDown(onBtnSettingClicked);
            if (mono.btnExit != null)
                mono.btnExit.RemoveMouseLeftClickDown(onBtnExitClicked);
        }

        public override void OnShowPanel()
        {
            if (mono.btnStart != null)
                mono.btnStart.AddMouseLeftClickDown(onBtnStartClicked);
            if (mono.btnSetting != null)
                mono.btnSetting.AddMouseLeftClickDown(onBtnSettingClicked);
            if (mono.btnExit != null)
                mono.btnExit.AddMouseLeftClickDown(onBtnExitClicked);
        }

        private void onBtnStartClicked(PointerEventData _data, object[] _objs)
        {
            SCDebugHelper.Log("开始游戏");
        }
        private void onBtnExitClicked(PointerEventData arg1, object[] arg2)
        {
            SCDebugHelper.Log("退出游戏");
        }
        private void onBtnSettingClicked(PointerEventData arg1, object[] arg2)
        {
            SCDebugHelper.Log("设置");
        }
    }
}
