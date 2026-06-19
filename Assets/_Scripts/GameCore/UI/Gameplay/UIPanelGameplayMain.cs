using SCFrame;
using SCFrame.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIPanelGameplayMain : _ASCUIPanelBase<UIMonoGameplayMain>
    {
        public UIPanelGameplayMain(UIMonoGameplayMain _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            ApplyPlaceholderData();
        }

        public override void BeforeDiscard()
        {
            UnbindButtons();
        }

        public override void OnHidePanel()
        {
            UnbindButtons();
        }

        public override void OnShowPanel()
        {
            BindButtons();
        }

        private void ApplyPlaceholderData()
        {
            if (mono.txtTime != null)
                mono.txtTime.text = "10 : 29（时间）";

            if (mono.txtAnimalInfo != null)
                mono.txtAnimalInfo.text = "动物扫脸信息";

            if (mono.txtBottomHint != null)
            {
                mono.txtBottomHint.text =
                    "动物信息档案，点击弹出（用于确认动物的档案，\n" +
                    "如果与所住房屋不符合，需要拒绝或询问原因）";
            }
        }

        private void BindButtons()
        {
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button numberButton = mono.numberButtons[index];
                if (numberButton != null)
                    numberButton.AddMouseLeftClickDown(OnNumberButtonClicked);
            }

            mono.btnOption1?.AddMouseLeftClickDown(OnOption1Clicked);
            mono.btnOption2?.AddMouseLeftClickDown(OnOption2Clicked);
            mono.btnReject?.AddMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.AddMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.AddMouseLeftClickDown(OnCloseDoorClicked);
        }

        private void UnbindButtons()
        {
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button numberButton = mono.numberButtons[index];
                if (numberButton != null)
                    numberButton.RemoveMouseLeftClickDown(OnNumberButtonClicked);
            }

            mono.btnOption1?.RemoveMouseLeftClickDown(OnOption1Clicked);
            mono.btnOption2?.RemoveMouseLeftClickDown(OnOption2Clicked);
            mono.btnReject?.RemoveMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.RemoveMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.RemoveMouseLeftClickDown(OnCloseDoorClicked);
        }

        private void OnNumberButtonClicked(PointerEventData eventData, object[] args)
        {
            Button clickedButton = eventData.pointerPress != null ? eventData.pointerPress.GetComponent<Button>() : null;
            string buttonName = clickedButton != null ? clickedButton.name : "UnknownNumber";
            SCDebugHelper.Log($"点击动物编号按钮：{buttonName}");

            // TODO: 这里接真实的动物编号选择、读数据和界面刷新逻辑。
        }

        private void OnOption1Clicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击选项1");

            // TODO: 这里接选项1对应的对话分支或交互逻辑。
        }

        private void OnOption2Clicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击选项2");

            // TODO: 这里接选项2对应的对话分支或交互逻辑。
        }

        private void OnRejectClicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击拒绝");

            // TODO: 这里接拒绝原因、状态更新和后续流程逻辑。
        }

        private void OnConfirmClicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击确认");

            // TODO: 这里接确认结果、数据落库和后续流程逻辑。
        }

        private void OnCloseDoorClicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击关门");

            // TODO: 这里接关门动画、阶段结束或流程推进逻辑。
        }
    }
}
