using GameCore.RefData;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        private void BindButtons()
        {
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button numberButton = mono.numberButtons[index];
                if (numberButton != null)
                    numberButton.AddMouseLeftClickDown(OnNumberButtonClicked);
            }

            mono.btnReject?.AddMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.AddMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.AddMouseLeftClickDown(OnCloseDoorClicked);
            mono.btnSetting?.AddMouseLeftClickDown(OnSettingClicked);
            mono.dialogueAdvanceClickArea?.AddMouseLeftClickDown(OnDialogueAreaClicked);
        }


        private void UnbindButtons()
        {
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button numberButton = mono.numberButtons[index];
                if (numberButton != null)
                    numberButton.RemoveMouseLeftClickDown(OnNumberButtonClicked);
            }

            UnbindAllDialogueOptionItems();
            mono.btnReject?.RemoveMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.RemoveMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.RemoveMouseLeftClickDown(OnCloseDoorClicked);
            mono.btnSetting?.RemoveMouseLeftClickDown(OnSettingClicked);
            mono.dialogueAdvanceClickArea?.RemoveMouseLeftClickDown(OnDialogueAreaClicked);
        }


        private void OnDialogueAreaClicked(PointerEventData eventData, object[] args)
        {
            if (!_isDialogueRunning || _currentDialogueRefData == null)
                return;

            if (!CanAdvanceDialogueByClick())
                return;

            if (_currentDialogueRefData.flagType == EDialogueFlagType.END)
            {
                EndDialogue();
                return;
            }

            SetCurrentDialogue(_currentDialogueRefData.nextList[0]);
        }


        private void OnNumberButtonClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能选楼层。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能选楼层。");
                return;
            }

            Button clickedButton = ResolveClickedButton(eventData);
            if (clickedButton == null)
            {
                Debug.LogError("Gameplay 选楼失败：未获取到点击的楼层按钮。");
                return;
            }

            _selectedFloor = GetFloorFromButton(clickedButton);
            SCDebugHelper.Log($"点击动物编号按钮：{clickedButton.name}，当前选择楼层={_selectedFloor}");
            RefreshAllUi();
        }


        private void OnConfirmClicked(PointerEventData eventData, object[] args)
        {
            if (!IsFinalDayLastSpecialCustomer())
            {
                SCDebugHelper.Log("当前不是最终特殊住户确认节点。");
                return;
            }

            GamePlayerDataMgr.instance.MarkFinalSpecialCustomerConfirmed();
            RefreshAllUi();
        }


        private void OnSettingClicked(PointerEventData eventData, object[] args)
        {
            UINodeMgr.instance.AddNode(new UINodeSetting(SCUIShowType.ADDITION, false));
        }


        private int GetFloorFromButton(Button button)
        {
            if (button == null)
                return 0;

            GameplayFloorButton floorButton = button.GetComponent<GameplayFloorButton>();
            if (floorButton != null)
                return floorButton.floor;

            string digits = string.Empty;
            for (int index = 0; index < button.name.Length; index++)
            {
                char current = button.name[index];
                if (char.IsDigit(current))
                    digits += current;
            }

            if (int.TryParse(digits, out int floor))
                return floor;

            Debug.LogError($"Gameplay 解析楼层按钮失败：buttonName={button.name}");
            return 0;
        }


        private Button ResolveClickedButton(PointerEventData eventData)
        {
            if (eventData == null)
                return null;

            if (eventData.pointerPress != null)
            {
                Button button = eventData.pointerPress.GetComponent<Button>();
                if (button != null)
                    return button;

                button = eventData.pointerPress.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                Button button = eventData.pointerCurrentRaycast.gameObject.GetComponent<Button>();
                if (button != null)
                    return button;

                button = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            if (eventData.pointerEnter != null)
            {
                Button button = eventData.pointerEnter.GetComponent<Button>();
                if (button != null)
                    return button;

                button = eventData.pointerEnter.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            return null;
        }


        private void OnDialogueOptionClicked(PointerEventData eventData, object[] args)
        {
            int optionIndex = ParseDialogueOptionIndex(args);
            if (optionIndex < 0 || optionIndex >= _currentOptionDialogueList.Count)
            {
                Debug.LogError($"Gameplay 选择对话选项失败：选项索引无效，index={optionIndex}");
                return;
            }

            HandleDialogueOptionSelection(_currentOptionDialogueList[optionIndex]);
        }


        private void OnRejectClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能拒绝。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能拒绝。");
                return;
            }

            if (_currentNeedRefData == null)
            {
                Debug.LogError("Gameplay 拒绝失败：当前住户需求配置为空。");
                return;
            }

            _lastRuleEffectData = RuleMgr.instance.EvaluateRuleList(_currentRuleIdList, EElevatorOperator.REFUSE, 0);
            if (_lastRuleEffectData != null && _lastRuleEffectData.effectType == ERuleEffectType.FORBID_REFUSE)
            {
                SCDebugHelper.Log("当前规则禁止拒绝该住户。");
                RefreshBottomHintUi();
                return;
            }

            _lastJudgmentEffectData = CustomerNeedMgr.instance.EvaluateRefuse(_currentNeedRefData.id);
            if (_lastJudgmentEffectData == null)
            {
                Debug.LogError("Gameplay 拒绝失败：未命中拒绝判定配置。");
                return;
            }

            ApplyServiceFeedbackByJudgment(_lastJudgmentEffectData);
            SCDebugHelper.Log($"已拒绝当前住户，影响值={_lastJudgmentEffectData.affectValue}");
            StartRejectAndPickupNextFlow();
        }


        private void OnCloseDoorClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能关门。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能重复关门。");
                return;
            }

            if (_currentCustomerRefData == null)
            {
                if (_selectedFloor <= 0)
                {
                    SCDebugHelper.Log("请先选择要前往的楼层。");
                    return;
                }

                StartEmptyElevatorTravel(_selectedFloor);
                return;
            }

            if (_canCloseDoor)
            {
                int targetFloor = ResolveCustomerDepartureFloor();
                StartCustomerDepartureFlow(targetFloor, false);
                return;
            }

            if (_currentNeedRefData == null)
            {
                Debug.LogError("Gameplay 关门失败：当前住户需求配置为空。");
                return;
            }

            if (_selectedFloor <= 0)
            {
                Debug.LogError("Gameplay 关门失败：当前尚未选择目标楼层。");
                return;
            }

            int finalFloor = _selectedFloor;
            _lastRuleEffectData = RuleMgr.instance.EvaluateRuleList(_currentRuleIdList, EElevatorOperator.GOTO, _selectedFloor);
            if (_lastRuleEffectData != null)
            {
                if (RuleMgr.instance.IsGotoBlocked(_lastRuleEffectData))
                {
                    SCDebugHelper.Log($"当前规则禁止前往 {_selectedFloor} 楼。");
                    RefreshAllUi();
                    return;
                }

                if (RuleMgr.instance.TryGetRedirectFloor(_lastRuleEffectData, out int redirectFloor))
                {
                    finalFloor = redirectFloor;
                    SCDebugHelper.Log($"规则将楼层从 {_selectedFloor} 改写为 {finalFloor}。");
                }
            }

            _lastJudgmentEffectData = CustomerNeedMgr.instance.EvaluateGoto(_currentNeedRefData.id, finalFloor);
            _canCloseDoor = _lastJudgmentEffectData != null;
            if (_canCloseDoor)
            {
                ApplyServiceFeedbackByJudgment(_lastJudgmentEffectData);
                SCDebugHelper.Log($"已关门前往 {finalFloor} 楼，影响值={_lastJudgmentEffectData.affectValue}");
                RefreshAllUi();
                StartCustomerDepartureFlow(finalFloor, true);
                return;
            }

            RefreshAllUi();
        }
    }
}
