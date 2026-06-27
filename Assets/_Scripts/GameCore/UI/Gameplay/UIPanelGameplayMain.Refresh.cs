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
            EnsureNoticeBoardReferences();

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

            if (ruleCount > noticeBoardList.Count)
                Debug.LogError($"Gameplay 告示板数量不足：需要 {ruleCount} 个，当前只有 {noticeBoardList.Count} 个。请重新生成 panel_gameplay_main Prefab。");

            int visibleCount = Mathf.Min(ruleCount, noticeBoardList.Count);
            for (int index = 0; index < visibleCount; index++)
            {
                long ruleId = _currentRuleIdList[index];
                SetNoticeBoardVisible(noticeBoardList, index, true, BuildRuleDisplayText(ruleId));
            }

            for (int index = visibleCount; index < noticeBoardList.Count; index++)
                SetNoticeBoardVisible(noticeBoardList, index, false, null);
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


        private void EnsureNoticeBoardReferences()
        {
            if (mono.noticeBoardList == null)
                mono.noticeBoardList = new List<NoticeBoardItem>();

            for (int index = 0; index < mono.noticeBoardList.Count; index++)
            {
                NoticeBoardItem noticeBoardItem = mono.noticeBoardList[index];
                if (noticeBoardItem == null)
                    continue;

                if (noticeBoardItem.text == null && noticeBoardItem.root != null)
                    noticeBoardItem.text = noticeBoardItem.root.GetComponentInChildren<Text>(true);
            }

            if (mono.noticeBoardList.Count > 0)
                return;

            Transform leftBand = FindChildTransformRecursive(mono.transform, "LeftBand");
            if (leftBand == null)
            {
                Debug.LogError("Gameplay 未找到 LeftBand，无法自动收集告示板。");
                return;
            }

            Transform noticeBoardRoot = leftBand.Find("NoticeBoardRoot");
            if (noticeBoardRoot != null)
                CollectNoticeBoardsFromTransform(noticeBoardRoot);
            else
                CollectNoticeBoardsFromTransform(leftBand);
        }


        private void CollectNoticeBoardsFromTransform(Transform parent)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (!child.name.StartsWith("NoticeBoard"))
                    continue;

                if (TryGetNoticeBoardItem(child, out NoticeBoardItem noticeBoardItem))
                    mono.noticeBoardList.Add(noticeBoardItem);
            }

            mono.noticeBoardList.Sort((left, right) =>
            {
                string leftName = left.root != null ? left.root.name : string.Empty;
                string rightName = right.root != null ? right.root.name : string.Empty;
                return string.CompareOrdinal(leftName, rightName);
            });
        }


        private static bool TryGetNoticeBoardItem(Transform boardRoot, out NoticeBoardItem noticeBoardItem)
        {
            noticeBoardItem = null;
            if (boardRoot == null)
                return false;

            Transform textTransform = boardRoot.Find($"{boardRoot.name}_Text");
            Text text = textTransform != null ? textTransform.GetComponent<Text>() : boardRoot.GetComponentInChildren<Text>(true);
            noticeBoardItem = new NoticeBoardItem
            {
                root = boardRoot.gameObject,
                text = text
            };
            return true;
        }


        private void RefreshCustomerUi()
        {
            if (_currentCustomerRefData == null)
            {
                if (_pendingCustomers.Count > 0)
                {
                    CustomerRefData pendingCustomer = GetCustomerRefData(_pendingCustomers.Peek().customerId);
                    if (pendingCustomer != null)
                    {
                        if (mono.txtCustomerName != null)
                            mono.txtCustomerName.text = pendingCustomer.customerName;

                        if (mono.txtAnimalInfo != null)
                            mono.txtAnimalInfo.text = BuildCustomerInfoText(pendingCustomer);
                    }
                }

                return;
            }

            if (mono.txtCustomerName != null)
                mono.txtCustomerName.text = _currentCustomerRefData.customerName;

            if (mono.txtAnimalInfo != null)
                mono.txtAnimalInfo.text = BuildCustomerInfoText(_currentCustomerRefData);
        }


        private string BuildCustomerInfoText(CustomerRefData customerRefData)
        {
            if (customerRefData == null)
                return "动物扫脸信息";

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
                mono.txtBottomHint.text = string.IsNullOrEmpty(_serviceFeedbackText)
                    ? "当前住户已处理完成，可以点击关门送走。"
                    : $"{_serviceFeedbackText}\n点击关门送走当前住户。";
                return;
            }

            if (_isDialogueRunning)
            {
                mono.txtBottomHint.text = "点击屏幕继续推进对话。";
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


        private void RefreshDialogueUi()
        {
            if (mono.dialogueSection == null)
            {
                RefreshDialogueAdvanceClickArea();
                return;
            }

            bool hasCurrentDialogue = _isDialogueRunning && _currentDialogueRefData != null;
            bool showPortraitOnly = !hasCurrentDialogue
                && _currentCustomerRefData != null
                && IsValidPortraitResName(_activeDialoguePortraitResName);

            mono.dialogueSection.SetActive(hasCurrentDialogue || showPortraitOnly);
            if (!hasCurrentDialogue)
            {
                if (showPortraitOnly)
                {
                    HideDialogueTextAndOptions();
                    ApplyDialoguePortrait(mono.imgDialoguePortrait, _activeDialoguePortraitResName);
                }

                RefreshDialogueAdvanceClickArea();
                return;
            }

            bool isPlayerDialogue = IsPlayerDialogue(_currentDialogueRefData);
            string currentContent = _currentDialogueRefData.content;

            mono.txtDialogueLeft.text = isPlayerDialogue ? string.Empty : currentContent;
            mono.txtDialogueRight.text = isPlayerDialogue ? currentContent : string.Empty;
            RefreshDialoguePortrait(_currentDialogueRefData);

            if (mono.dialogueLeftArea != null)
            {
                mono.dialogueLeftArea.raycastTarget = false;
                mono.dialogueLeftArea.enabled = !string.IsNullOrEmpty(mono.txtDialogueLeft.text);
            }

            if (mono.dialogueRightArea != null)
            {
                mono.dialogueRightArea.raycastTarget = false;
                mono.dialogueRightArea.enabled = !string.IsNullOrEmpty(mono.txtDialogueRight.text);
            }

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

            bool showOptions = _currentOptionDialogueList.Count > 0;
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

            if (mono.dialogueOptionRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(mono.dialogueOptionRoot);

            for (int index = 0; index < _currentOptionDialogueList.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                if (optionItem != null && optionItem.gameObject.activeSelf)
                    optionItem.RefreshBackgroundPixelsPerUnitMultiplier();
            }
        }


        private void HideDialogueTextAndOptions()
        {
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
