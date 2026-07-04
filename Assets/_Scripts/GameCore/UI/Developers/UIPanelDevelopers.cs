using GameCore;
using SCFrame;
using SCFrame.UI;
using UnityEngine.EventSystems;


namespace GameCore.UI
{
    public class UIPanelDevelopers : _ASCUIPanelBase<UIMonoDevelopers>
    {
        public UIPanelDevelopers(UIMonoDevelopers _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void OnShowPanel()
        {
            if (mono.btnClose != null)
                mono.btnClose.AddMouseLeftClickDown(onBtnCloseClicked);
        }

        public override void OnHidePanel()
        {
            if (mono.btnClose != null)
                mono.btnClose.RemoveMouseLeftClickDown(onBtnCloseClicked);
        }

        private void onBtnCloseClicked(PointerEventData _data, object[] _objs)
        {
            AudioMgr.instance.PlaySfx(AudioKeys.ButtonClick);
            UINodeMgr.instance.CloseTopNode();
        }

        public override void BeforeDiscard()
        {
            throw new System.NotImplementedException();
        }

        public override void AfterInitialize()
        {
            throw new System.NotImplementedException();
        }
    }
}
