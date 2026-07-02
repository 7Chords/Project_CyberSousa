using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIPanelGameplayMain : _ASCUIPanelBase<UIMonoGameplayMain>
    {
        private const int DefaultMaxPerformance = 100;
        private const int DefaultMissingConfirmPenalty = 10;
        private const int DefaultPerfectOperationBonus = 12;

        private int _maxPerformance = DefaultMaxPerformance;
        private int _missingConfirmPenalty = DefaultMissingConfirmPenalty;
        private int _perfectOperationBonus = DefaultPerfectOperationBonus;
        private int _performanceValue = DefaultMaxPerformance;
        private bool _hasActiveAnimal;
        private bool _hasConfirmedCurrentAnimal;
        private bool _hasCurrentOperationProblem;

        public UIPanelGameplayMain(UIMonoGameplayMain _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            ApplyGameplayConfig();
            EnsurePerformanceText();
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

            RefreshPerformanceText();

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

            SettleCurrentOperation(buttonName);
            _hasActiveAnimal = true;
            _hasConfirmedCurrentAnimal = false;
            _hasCurrentOperationProblem = false;

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
            MarkCurrentOperationProblem("点击拒绝，当前轮不计入完全正确奖励。");

            // TODO: 这里接拒绝原因、状态更新和后续流程逻辑。
        }

        private void OnConfirmClicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击确认");
            _hasConfirmedCurrentAnimal = true;

            // TODO: 这里接确认结果、数据落库和后续流程逻辑。
        }

        private void OnCloseDoorClicked(PointerEventData eventData, object[] args)
        {
            SCDebugHelper.Log("点击关门");

            // TODO: 这里接关门动画、阶段结束或流程推进逻辑。
        }

        private void SettleCurrentOperation(string nextButtonName)
        {
            if (!_hasActiveAnimal)
                return;

            if (!_hasConfirmedCurrentAnimal)
            {
                ChangePerformance(-_missingConfirmPenalty);
                MarkCurrentOperationProblem($"切换到 {nextButtonName} 前未点击确认，绩效值扣除 {_missingConfirmPenalty}。");
                return;
            }

            if (_hasCurrentOperationProblem)
                return;

            ChangePerformance(_perfectOperationBonus);
            SCDebugHelper.Log($"本次按键流程完全正确，绩效值增加 {_perfectOperationBonus}。");
        }

        private void MarkCurrentOperationProblem(string reason)
        {
            _hasCurrentOperationProblem = true;
            SCDebugHelper.Log(reason);
        }

        private void ChangePerformance(int delta)
        {
            int oldPerformance = _performanceValue;
            _performanceValue = Mathf.Clamp(_performanceValue + delta, 0, _maxPerformance);
            RefreshPerformanceText();
            SCDebugHelper.Log($"绩效值变化：{oldPerformance} -> {_performanceValue}");
        }

        private void RefreshPerformanceText()
        {
            if (mono.txtPerformance != null)
                mono.txtPerformance.text = $"绩效值：{_performanceValue}/{_maxPerformance}";
        }

        private void ApplyGameplayConfig()
        {
            GameCore.RefData.GameplayConfigRefData config = SCRefDataMgr.instance.gameplayConfig;
            if (config == null)
            {
                SCDebugHelper.Log("未读取到 gameplay_config 配表，使用默认绩效配置。");
                return;
            }

            _maxPerformance = Mathf.Max(1, config.maxPerformance);
            _missingConfirmPenalty = Mathf.Max(0, config.missingConfirmPenalty);
            _perfectOperationBonus = Mathf.Max(0, config.perfectOperationBonus);
            _performanceValue = _maxPerformance;
        }

        private void EnsurePerformanceText()
        {
            if (mono.txtPerformance != null)
                return;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                SCDebugHelper.Log("创建绩效值文本失败：未找到 LegacyRuntime 字体。");
                return;
            }

            GameObject performanceObject = new GameObject("PerformanceText", typeof(RectTransform), typeof(Text));
            RectTransform rect = performanceObject.transform as RectTransform;
            rect.SetParent(mono.transform, false);
            rect.anchorMin = new Vector2(0.812f, 0.805f);
            rect.anchorMax = new Vector2(0.982f, 0.855f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = performanceObject.GetComponent<Text>();
            text.font = defaultFont;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            mono.txtPerformance = text;
        }
    }
}
