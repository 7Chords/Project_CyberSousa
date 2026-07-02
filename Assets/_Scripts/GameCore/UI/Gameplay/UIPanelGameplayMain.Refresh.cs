using System.Collections.Generic;
using System.Text;
using GameCore.RefData;
using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        private void RefreshAllUi()
        {
            RefreshFloorUi();
            RefreshRuleUi();
            RefreshCustomerUi();
            RefreshBottomHintUi();
            RefreshDialogueUi();
            RefreshActionUi();
            RefreshNumberButtonUi();
        }


        private void RefreshFloorUi()
        {
            if (mono.txtCurrentFloor != null)
                mono.txtCurrentFloor.text = $"{_currentFloor}";

            if (mono.txtTime != null)
                mono.txtTime.text = $"第 {_currentDayIndex + 1} 天";

            RefreshClockUi();
        }


        private void RefreshClockUi()
        {
            if (mono.txtClockTime != null)
                mono.txtClockTime.text = GameTimeMgr.instance.GetDisplayText();
        }


        private void RefreshRuleUi()
        {
            List<NoticeBoardItem> noticeBoardList = mono.noticeBoardList;
            if (noticeBoardList == null || noticeBoardList.Count == 0)
            {
                Debug.LogError("Gameplay 告示板列表为空，无法刷新规则 UI。");
                return;
            }

            int ruleCount = _currentRuleIdList != null ? _currentRuleIdList.Count : 0;
            if (ruleCount == 0)
            {
                SetNoticeBoardVisible(noticeBoardList, 0, true, "本关暂无规则");
                for (int index = 1; index < noticeBoardList.Count; index++)
                    SetNoticeBoardVisible(noticeBoardList, index, false, null);
                return;
            }

            int ruleIndexStart = 0;
            int rulesPerBoard = Mathf.CeilToInt(ruleCount / (float)noticeBoardList.Count);
            for (int index = 0; index < noticeBoardList.Count; index++)
            {
                if (ruleIndexStart >= ruleCount)
                {
                    SetNoticeBoardVisible(noticeBoardList, index, false, null);
                    continue;
                }

                int ruleIndexEnd = Mathf.Min(ruleIndexStart + rulesPerBoard, ruleCount);
                SetNoticeBoardVisible(noticeBoardList, index, true, BuildRuleDisplayTextGroup(ruleIndexStart, ruleIndexEnd));
                ruleIndexStart = ruleIndexEnd;
            }
        }


        private string BuildRuleDisplayText(long ruleId)
        {
            if (!_ruleMap.TryGetValue(ruleId, out RuleRefData ruleRefData))
            {
                Debug.LogError($"Gameplay 获取规则配置失败：未找到 ruleId={ruleId}");
                return "规则配置缺失";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(ruleRefData.ruleName);
            if (!string.IsNullOrEmpty(ruleRefData.ruleDesc))
            {
                builder.AppendLine();
                builder.Append(ruleRefData.ruleDesc);
            }

            return builder.ToString();
        }


        private string BuildRuleDisplayTextGroup(int startIndex, int endIndex)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = startIndex; index < endIndex; index++)
            {
                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                builder.Append(BuildRuleDisplayText(_currentRuleIdList[index]));
            }

            return builder.ToString();
        }


        private static void SetNoticeBoardVisible(List<NoticeBoardItem> noticeBoardList, int index, bool visible, string text)
        {
            NoticeBoardItem noticeBoardItem = noticeBoardList[index];
            if (noticeBoardItem.root != null)
                SCCommon.SetGameObjectEnable(noticeBoardItem.root, visible);
            else if (noticeBoardItem.text != null)
                SCCommon.SetGameObjectEnable(noticeBoardItem.text.gameObject, visible);

            if (visible && noticeBoardItem.text != null && text != null)
                noticeBoardItem.text.text = text;
        }


        private void RefreshCustomerUi()
        {
            if (_currentCustomerRefData == null || !_isCustomerInfoVisible)
                return;

            if (mono.txtCustomerName != null)
                mono.txtCustomerName.text = _currentCustomerRefData.customerName;

            if (mono.txtAnimalInfo != null)
                mono.txtAnimalInfo.text = BuildCustomerInfoText(_currentCustomerRefData);
        }


        private string BuildCustomerInfoText(CustomerRefData customerRefData)
        {
            if (customerRefData == null)
                return string.Empty;

            if (customerRefData.customerInfoList == null || customerRefData.customerInfoList.Count == 0)
                return customerRefData.customerName;

            return string.Join("\n", customerRefData.customerInfoList);
        }


        private void RefreshBottomHintUi()
        {
            if (mono.txtBottomHint == null)
                return;

            if (_canCloseDoor)
            {
                string feedbackText = BuildBottomFeedbackText();
                mono.txtBottomHint.text = string.IsNullOrEmpty(feedbackText)
                    ? "当前住户已处理完成，可以点击关门送走。"
                    : $"{feedbackText}\n点击关门送走当前住户。";
                return;
            }

            if (_isDialogueRunning)
            {
                mono.txtBottomHint.text = "点击屏幕继续推进对话。";
                return;
            }

            if (!string.IsNullOrEmpty(_ruleFeedbackText))
            {
                mono.txtBottomHint.text = _ruleFeedbackText;
                return;
            }

            if (_recentFloorInputData.recentFloorList.Count > 0)
            {
                mono.txtBottomHint.text = $"当前输入：{string.Join(",", _recentFloorInputData.recentFloorList)}";
                return;
            }

            if (_selectedFloor > 0)
            {
                mono.txtBottomHint.text = $"当前已选择楼层：{_selectedFloor} 楼。";
                return;
            }

            if (TryGetPeekPendingNeedRefData(out CustomerNeedRefData pendingNeedRefData, out TimeEffectData pendingNeedTime))
            {
                string needTimeText = GameTimeMgr.FormatClockTime(pendingNeedTime);
                int needFloor = pendingNeedRefData.needFloor > 0 ? pendingNeedRefData.needFloor : _currentFloor;
                if (!GameTimeMgr.instance.HasReachedNeedTime(pendingNeedTime))
                {
                    mono.txtBottomHint.text =
                        $"约 {needTimeText} 将在 {needFloor} 楼发出需求，请提前前往。";
                    return;
                }

                if (pendingNeedRefData.needFloor > 0 && _currentFloor != pendingNeedRefData.needFloor)
                {
                    mono.txtBottomHint.text =
                        $"已在 {needTimeText} 于 {needFloor} 楼发出需求，请前往接待。";
                    return;
                }

                mono.txtBottomHint.text = $"正在 {needFloor} 楼等待住户出现。";
                return;
            }

            if (_currentNeedRefData != null)
            {
                string needTimeText = GameTimeMgr.FormatClockTime(_currentNeedTime);
                if (_currentNeedRefData.needFloor > 0)
                {
                    mono.txtBottomHint.text =
                        $"住户于 {needTimeText} 在 {_currentNeedRefData.needFloor} 楼发出需求。";
                    return;
                }

                mono.txtBottomHint.text = $"住户于 {needTimeText} 发出需求。";
                return;
            }

            mono.txtBottomHint.text =
                "动物信息档案，点击弹出（用于确认动物的档案，\n" +
                "如果与所住房屋不符合，需要拒绝或询问原因）";
        }

        private string BuildBottomFeedbackText()
        {
            if (string.IsNullOrEmpty(_ruleFeedbackText))
                return _serviceFeedbackText;

            if (string.IsNullOrEmpty(_serviceFeedbackText))
                return _ruleFeedbackText;

            return $"{_ruleFeedbackText}\n{_serviceFeedbackText}";
        }


        private void RefreshDialogueUi()
        {
            if (mono.dialogueSection == null)
            {
                RefreshDialogueAdvanceClickArea();
                return;
            }

            bool hasCurrentDialogue = _isDialogueRunning && (_currentDialogueRefData != null || _awaitDialogueAdvanceAfterPlayerLine);
            bool showPortraitOnly = !hasCurrentDialogue
                && _currentCustomerRefData != null
                && ResolveCustomerPortraitResName(_useCustomerBackPortrait) != null;

            mono.dialogueSection.SetActive(hasCurrentDialogue || showPortraitOnly);
            if (!hasCurrentDialogue)
            {
                if (showPortraitOnly)
                {
                    HideDialogueTextAndOptions();
                    RefreshCustomerPortrait();
                }

                RefreshDialogueAdvanceClickArea();
                return;
            }

            if (_awaitDialogueAdvanceAfterPlayerLine)
            {
                UpdateDialogueStripVisibility();
                RefreshDialogueAdvanceClickArea();
                RefreshDialogueOptionUi();
                return;
            }

            if (_currentDialogueRefData == null)
            {
                RefreshDialogueAdvanceClickArea();
                return;
            }

            bool isPlayerDialogue = IsPlayerDialogue(_currentDialogueRefData);
            string currentContent = _currentDialogueRefData.content;

            PlayCurrentDialogueTypewriter(isPlayerDialogue, currentContent);
            RefreshCustomerPortrait();

            if (mono.dialogueLeftArea != null)
                mono.dialogueLeftArea.raycastTarget = false;

            if (mono.dialogueRightArea != null)
                mono.dialogueRightArea.raycastTarget = false;

            UpdateDialogueStripVisibility();

            RefreshDialogueAdvanceClickArea();
            RefreshDialogueOptionUi();
        }


        private void RefreshDialogueOptionUi()
        {
            HideAllDialogueOptionItems();

            if (mono.dialogueOptionRoot == null)
            {
                if (_currentOptionDialogueList.Count > 0)
                    Debug.LogError("Gameplay 对话选项容器为空，无法显示选项。");
                return;
            }

            bool showOptions = _currentOptionDialogueList.Count > 0 && !_isDialogueTypewriterPlaying;
            mono.dialogueOptionRoot.gameObject.SetActive(showOptions);
            if (!showOptions)
                return;

            EnsureDialogueOptionPrefab();
            if (_dialogueOptionItemPrefab == null)
            {
                Debug.LogError("Gameplay 对话选项预制体未加载完成，无法显示选项。");
                return;
            }

            for (int index = 0; index < _currentOptionDialogueList.Count; index++)
            {
                UIMonoDialogueOption optionItem = GetOrCreateDialogueOptionItem(index);
                if (optionItem == null)
                    continue;

                DialogueRefData optionDialogueRefData = _currentOptionDialogueList[index];
                optionItem.gameObject.SetActive(true);
                optionItem.SetContent(optionDialogueRefData.content);
                BindDialogueOptionItem(optionItem, index);
            }

            RebuildDialogueOptionLayoutsImmediate();
            ScheduleDialogueOptionLayoutRebuildNextFrame();
        }


        private void HideDialogueTextAndOptions()
        {
            StopDialogueTypewriter();
            mono.txtDialogueLeft.text = string.Empty;
            mono.txtDialogueRight.text = string.Empty;

            if (mono.dialogueLeftArea != null)
            {
                mono.dialogueLeftArea.raycastTarget = false;
                mono.dialogueLeftArea.enabled = false;
            }

            if (mono.dialogueRightArea != null)
            {
                mono.dialogueRightArea.raycastTarget = false;
                mono.dialogueRightArea.enabled = false;
            }

            HideAllDialogueOptionItems();
            if (mono.dialogueOptionRoot != null)
                mono.dialogueOptionRoot.gameObject.SetActive(false);
        }


        private void RefreshActionUi()
        {
            bool canOperateElevator = !_isDialogueRunning && !_canCloseDoor && !_isTransitionPlaying;
            bool hasCustomer = _currentCustomerRefData != null;

            if (mono.btnReject != null)
                mono.btnReject.interactable = canOperateElevator && hasCustomer;

            if (mono.btnConfirm != null)
                mono.btnConfirm.interactable = canOperateElevator && IsFinalDayLastSpecialCustomer() && !GamePlayerDataMgr.instance.hasConfirmedFinalSpecialCustomer;

            if (mono.btnCloseDoor != null)
                mono.btnCloseDoor.interactable = canOperateElevator && (hasCustomer || _selectedFloor > 0);
        }


        private void RefreshNumberButtonUi()
        {
            bool canSelectFloor = !_isDialogueRunning && !_canCloseDoor && !_isTransitionPlaying;
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button button = mono.numberButtons[index];
                if (button == null)
                    continue;

                button.interactable = canSelectFloor;
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage == null)
                    continue;

                int floor = GetFloorFromButton(button);
                buttonImage.color = floor == _selectedFloor
                    ? new Color(0.74f, 0.88f, 0.76f, 1f)
                    : new Color(0.89f, 0.89f, 0.89f, 1f);
            }
        }
    }
}
