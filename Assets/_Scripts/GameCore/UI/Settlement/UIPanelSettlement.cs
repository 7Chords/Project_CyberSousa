using SCFrame;
using SCFrame.UI;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    public class UIPanelSettlement : _ASCUIPanelBase<UIMonoSettlement>
    {
        public static string pendingTitle = "当日结算";
        public static string pendingSummary = "当天客人已全部接待完毕。";
        public static System.Action onNextDayClicked;

        public UIPanelSettlement(UIMonoSettlement _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
        }

        public override void BeforeDiscard()
        {
            UnbindEvents();
        }

        public override void OnHidePanel()
        {
            UnbindEvents();
        }

        public override void OnShowPanel()
        {
            if (mono.txtTitle != null)
                mono.txtTitle.text = pendingTitle;

            if (mono.txtSummary != null)
                mono.txtSummary.text = pendingSummary;

            if (mono.btnNextDay != null)
                mono.btnNextDay.AddMouseLeftClickDown(OnBtnNextDayClicked);
        }

        private void UnbindEvents()
        {
            if (mono.btnNextDay != null)
                mono.btnNextDay.RemoveMouseLeftClickDown(OnBtnNextDayClicked);
        }

        private void OnBtnNextDayClicked(PointerEventData eventData, object[] args)
        {
            onNextDayClicked?.Invoke();
            UINodeMgr.instance.CloseTopAdditionNode();
        }
    }
}
