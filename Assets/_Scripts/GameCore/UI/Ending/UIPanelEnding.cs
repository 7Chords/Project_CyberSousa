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
                    SetText("End：不合格按键员\n你按错了太多次。\n管理员委婉地表示，\n你和住户之间可能存在沟通障碍。\n第二天，\n你被安排去看守安置区外侧的小门。\n工作内容简单很多：\n门开了就关。\n门关了就看着。\n直到有一天，\n你看到曾经那栋公寓。\n拔地而起。");
                    break;
                case EGameEndingType.ENDING_1:
                    SetEndingSprite(mono.sprEnding1);
                    SetText("End：一切正常\n你按下了确认键。\n第二天，\n你照常上班。\n没有处分，没有奖励，\n也没有人提起昨晚。\n考考仍然带着面包。\n来福仍然制造噪音。\n电梯仍然上行下行。");
                    break;
                case EGameEndingType.ENDING_2:
                    SetEndingSprite(mono.sprEnding2);
                    SetText("End：集体迁徙\n你没有按下确认键。\n系统没有进行最后一次的检查。\n管理员没有发现夹层。\n而那栋楼，终于不再扮演一栋楼。\n你听到一阵轰鸣。\n公寓拔地而起。\n灯光、房间、住户、沉默，\n地球的眼睛、胃、羽毛、根须，\n罪恶、无法辩白的使命，\n全都被火焰托举到夜空里。\n只有上行。");
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
            AudioMgr.instance.PlaySfx(AudioKeys.ButtonClick);
            GamePlayerDataMgr.instance.ResetRuntimeData();
            GameFlowController.instance.EnterMainMenu();
        }
    }
}
