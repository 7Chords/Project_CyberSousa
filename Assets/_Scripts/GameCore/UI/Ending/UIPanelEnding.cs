using GameCore.Flow;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    public class UIPanelEnding : _ASCUIPanelBase<UIMonoEnding>
    {
        public static EGameEndingType pendingEndingType = EGameEndingType.BAD;

        public UIPanelEnding(UIMonoEnding _mono, SCUIShowType _showType) : base(_mono, _showType)
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
            ApplyEndingContent();

            if (mono.btnReturnMain != null)
                mono.btnReturnMain.AddMouseLeftClickDown(OnBtnReturnMainClicked);
        }

        private void ApplyEndingContent()
        {
            switch (pendingEndingType)
            {
                case EGameEndingType.BAD:
                    SetEndingSprite(mono.sprBadEnding);
                    SetText("坏结局：业绩值已经跌破底线，电梯系统终止了你的值班权限。");
                    break;
                case EGameEndingType.ENDING_1:
                    SetEndingSprite(mono.sprEnding1);
                    SetText("结局一：你在最后的关键节点按下确认键，特殊住户的记录被保留下来。");
                    break;
                case EGameEndingType.ENDING_2:
                    SetEndingSprite(mono.sprEnding2);
                    SetText( "结局二：你没有在最后的关键节点按下确认键，特殊住户的记录被归入沉默档案。");
                    break;
                default:
                    SetEndingSprite(null);
                    SetText("本次值班已经结束。");
                    break;
            }
        }

        private void SetEndingSprite(Sprite endingSprite)
        {
            if (mono.imgEnding == null)
                return;

            mono.imgEnding.sprite = endingSprite;
            SCCommon.SetGameObjectEnable(mono.imgEnding.gameObject, endingSprite != null);
        }

        private void SetText(string summary)
        {
            if (mono.txtSummary != null)
                mono.txtSummary.text = summary;
        }

        private void UnbindEvents()
        {
            if (mono.btnReturnMain != null)
                mono.btnReturnMain.RemoveMouseLeftClickDown(OnBtnReturnMainClicked);
        }

        private void OnBtnReturnMainClicked(PointerEventData eventData, object[] args)
        {
            GamePlayerDataMgr.instance.ResetRuntimeData();
            GameFlowController.instance.EnterMainMenu();
        }
    }
}
