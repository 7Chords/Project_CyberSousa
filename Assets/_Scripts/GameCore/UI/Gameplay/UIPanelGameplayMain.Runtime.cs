using System.Collections.Generic;
using GameCore.RefData;
using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        private void EnsureRuntimeReferences()
        {
            if (mono.txtCurrentFloor == null)
                mono.txtCurrentFloor = FindChildText("CurrentFloorPanel_Text");

            if (mono.txtCustomerName == null)
                mono.txtCustomerName = FindChildText("AnimalLabel");

            if (mono.dialogueLeftArea == null)
                mono.dialogueLeftArea = FindChildImage("Dialogue01");

            if (mono.dialogueRightArea == null)
                mono.dialogueRightArea = FindChildImage("Dialogue02");

            if (mono.imgDialoguePortrait == null)
                mono.imgDialoguePortrait = FindChildImage("DialoguePortrait");

            if (mono.imgCustomerPortrait == null)
                mono.imgCustomerPortrait = FindChildImage("CustomerPortrait");

            if (mono.imgFloorSceneBackground == null)
                mono.imgFloorSceneBackground = FindChildImage("AnimalView");

            if (mono.imgFloorSceneBackgroundOverlay == null)
                mono.imgFloorSceneBackgroundOverlay = FindChildImage("FloorSceneBackgroundOverlay");

            if (mono.elevatorView == null)
            {
                Transform elevatorRoot = FindChildTransformRecursive(mono.transform, "ElevatorRoot");
                if (elevatorRoot != null)
                    mono.elevatorView = elevatorRoot.GetComponent<UIElevatorView>();
            }

            mono.elevatorView?.EnsureReferences();

            if (mono.dialogueOptionRoot == null)
                mono.dialogueOptionRoot = FindChildRect("OptionSection");

            if (mono.customerCanvasGroup == null)
            {
                Transform customerLayer = FindChildTransformRecursive(mono.transform, "CustomerLayer");
                if (customerLayer != null)
                    mono.customerCanvasGroup = customerLayer.GetComponent<CanvasGroup>();
            }

            if (mono.currentFloorCanvasGroup == null && mono.txtCurrentFloor != null)
                mono.currentFloorCanvasGroup = mono.txtCurrentFloor.GetComponent<CanvasGroup>();

            if (mono.customerFadeEffect == null && mono.customerCanvasGroup != null)
                mono.customerFadeEffect = mono.customerCanvasGroup.GetComponent<UIRuntimeFadeEffect>();

            if (mono.customerMoveScaleEffect == null && mono.customerCanvasGroup != null)
                mono.customerMoveScaleEffect = mono.customerCanvasGroup.GetComponent<UIRuntimeMoveScaleEffect>();

            if (mono.floorTextEffect == null && mono.txtCurrentFloor != null)
                mono.floorTextEffect = mono.txtCurrentFloor.GetComponent<UITextChangeMoveEffect>();

            if (mono.txtClockTime == null)
                mono.txtClockTime = FindChildText("ClockTimePanel_Text");

            if (mono.txtPerformance == null)
                mono.txtPerformance = FindChildText("PerformanceText");

            if (mono.txtBottomHint == null)
                mono.txtBottomHint = FindChildText("BottomHintText");
            //暂时注释测试tip
            //EnsureBottomHintVisible();

            if (mono.btnSetting == null)
            {
                Transform btnSettingTransform = FindChildTransformRecursive(mono.transform, "BtnSetting");
                if (btnSettingTransform != null)
                    mono.btnSetting = btnSettingTransform.GetComponent<Button>();
            }

            EnsurePerformanceText();
            EnsureDialogueAdvanceClickArea();
        }

        private void EnsurePerformanceText()
        {
            if (mono.txtPerformance != null)
                return;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                Debug.LogError("Gameplay 绩效值文本初始化失败：未找到 LegacyRuntime.ttf。");
                return;
            }

            GameObject performanceObject = new GameObject("PerformanceText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = performanceObject.GetComponent<RectTransform>();
            rect.SetParent(mono.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = new Vector2(0.812f, 0.805f);
            rect.anchorMax = new Vector2(0.982f, 0.855f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Text performanceText = performanceObject.GetComponent<Text>();
            performanceText.font = defaultFont;
            performanceText.fontSize = 22;
            performanceText.fontStyle = FontStyle.Bold;
            performanceText.alignment = TextAnchor.MiddleCenter;
            performanceText.horizontalOverflow = HorizontalWrapMode.Wrap;
            performanceText.verticalOverflow = VerticalWrapMode.Overflow;
            performanceText.raycastTarget = false;
            performanceText.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            mono.txtPerformance = performanceText;
        }

        private void EnsureBottomHintVisible()
        {
            if (mono.txtBottomHint == null)
            {
                Debug.LogError("Gameplay 底部提示初始化失败：未找到 BottomHintText。");
                return;
            }

            if (!mono.txtBottomHint.gameObject.activeSelf)
                mono.txtBottomHint.gameObject.SetActive(true);

            Transform bottomHintPanel = mono.txtBottomHint.transform.parent;
            if (bottomHintPanel != null && !bottomHintPanel.gameObject.activeSelf)
                bottomHintPanel.gameObject.SetActive(true);
        }

        private void EnsureDialogueAdvanceClickArea()
        {
            if (mono.dialogueAdvanceClickArea == null)
                mono.dialogueAdvanceClickArea = FindChildImage("DialogueAdvanceClickArea");

            if (mono.dialogueAdvanceClickArea != null)
                return;

            if (mono.dialogueSection == null)
            {
                Debug.LogError("Gameplay 对话推进点击区域创建失败：对话区域根节点为空。");
                return;
            }

            GameObject clickAreaObject = new GameObject("DialogueAdvanceClickArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform clickAreaRect = clickAreaObject.GetComponent<RectTransform>();
            clickAreaRect.SetParent(mono.dialogueSection.transform, false);
            clickAreaRect.SetAsFirstSibling();
            clickAreaRect.anchorMin = new Vector2(0.08f, 0.12f);
            clickAreaRect.anchorMax = new Vector2(0.92f, 0.98f);
            clickAreaRect.offsetMin = Vector2.zero;
            clickAreaRect.offsetMax = Vector2.zero;
            clickAreaRect.pivot = new Vector2(0.5f, 0.5f);

            Image clickAreaImage = clickAreaObject.GetComponent<Image>();
            clickAreaImage.color = new Color(1f, 1f, 1f, 0f);
            clickAreaImage.raycastTarget = false;
            clickAreaImage.enabled = false;
            mono.dialogueAdvanceClickArea = clickAreaImage;
        }


        private void BuildLookupMaps()
        {
            _dialogueMap.Clear();
            List<DialogueRefData> dialogueRefDataList = SCRefDataMgr.instance.dialogueRefList?.refDataList;
            if (dialogueRefDataList != null)
            {
                for (int index = 0; index < dialogueRefDataList.Count; index++)
                {
                    DialogueRefData dialogueRefData = dialogueRefDataList[index];
                    if (dialogueRefData == null)
                    {
                        Debug.LogError($"Gameplay 对话表初始化失败：第 {index} 条对话配置为空。");
                        continue;
                    }

                    if (_dialogueMap.ContainsKey(dialogueRefData.id))
                    {
                        Debug.LogError($"Gameplay 对话表初始化失败：重复的 dialogueId={dialogueRefData.id}");
                        continue;
                    }

                    _dialogueMap.Add(dialogueRefData.id, dialogueRefData);
                }
            }

            _customerMap.Clear();
            List<CustomerRefData> customerRefDataList = SCRefDataMgr.instance.customerRefList?.refDataList;
            if (customerRefDataList != null)
            {
                for (int index = 0; index < customerRefDataList.Count; index++)
                {
                    CustomerRefData customerRefData = customerRefDataList[index];
                    if (customerRefData == null)
                    {
                        Debug.LogError($"Gameplay 住户表初始化失败：第 {index} 条住户配置为空。");
                        continue;
                    }

                    if (_customerMap.ContainsKey(customerRefData.id))
                    {
                        Debug.LogError($"Gameplay 住户表初始化失败：重复的 customerId={customerRefData.id}");
                        continue;
                    }

                    _customerMap.Add(customerRefData.id, customerRefData);
                }
            }

            _ruleMap.Clear();
            List<RuleRefData> ruleRefDataList = SCRefDataMgr.instance.ruleRefList?.refDataList;
            if (ruleRefDataList != null)
            {
                for (int index = 0; index < ruleRefDataList.Count; index++)
                {
                    RuleRefData ruleRefData = ruleRefDataList[index];
                    if (ruleRefData == null)
                    {
                        Debug.LogError($"Gameplay 规则表初始化失败：第 {index} 条规则配置为空。");
                        continue;
                    }

                    if (_ruleMap.ContainsKey(ruleRefData.id))
                    {
                        Debug.LogError($"Gameplay 规则表初始化失败：重复的 ruleId={ruleRefData.id}");
                        continue;
                    }

                    _ruleMap.Add(ruleRefData.id, ruleRefData);
                }
            }

            _levelSequence.Clear();
            List<LevelRefData> levelRefDataList = SCRefDataMgr.instance.levelRefList?.refDataList;
            if (levelRefDataList != null)
            {
                for (int index = 0; index < levelRefDataList.Count; index++)
                {
                    LevelRefData levelRefData = levelRefDataList[index];
                    if (levelRefData == null)
                    {
                        Debug.LogError($"Gameplay 关卡序列初始化失败：第 {index} 条关卡配置为空。");
                        continue;
                    }

                    _levelSequence.Add(levelRefData);
                }
            }
        }


        private void InitializeLevelRuntime()
        {
            _currentDayIndex = Mathf.Clamp(GamePlayerDataMgr.instance.startDayIndex, 0, Mathf.Max(0, _levelSequence.Count - 1));
            _currentLevelRefData = _levelSequence.Count > 0 ? _levelSequence[_currentDayIndex] : FindInitialLevelRefData();
            StartCurrentDay();
        }


        private void StartCurrentDay()
        {
            _currentRuleIdList = _currentLevelRefData != null && _currentLevelRefData.ruleIdList != null
                ? new List<long>(_currentLevelRefData.ruleIdList)
                : new List<long>();

            _pendingCustomers.Clear();
            if (_currentLevelRefData != null && _currentLevelRefData.customerEffectList != null)
            {
                for (int index = 0; index < _currentLevelRefData.customerEffectList.Count; index++)
                {
                    CustomerEffectData customerEffectData = _currentLevelRefData.customerEffectList[index];
                    if (customerEffectData == null)
                    {
                        Debug.LogError($"Gameplay 关卡住户初始化失败：levelId={_currentLevelRefData.id} 的 customerEffectList 第 {index} 项为空。");
                        continue;
                    }

                    long customerId = CustomerPoolMgr.instance.ResolveCustomerId(customerEffectData);
                    if (customerId <= 0)
                        continue;

                    if (customerEffectData.needTime == null)
                    {
                        Debug.LogError($"Gameplay 关卡住户初始化失败：levelId={_currentLevelRefData.id}，customerId={customerId} 未配置出现时间。");
                        continue;
                    }

                    _pendingCustomers.Enqueue(new PendingCustomerRuntimeData(customerId, customerEffectData.needTime));
                }
            }

            _currentFloor = 1;
            _selectedFloor = 0;
            _resolvedTargetFloor = 0;
            _recentFloorInputData.Clear();
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _isSettlementShowing = false;
            _isTransitionPlaying = false;
            _isCustomerInfoVisible = false;
            _hasRunEnded = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;
            _serviceFeedbackText = null;
            _ruleFeedbackText = null;

            SetCurrentFloorDisplayInstant(_currentFloor);
            GameTimeMgr.instance.ResetToDayStart();
            SetElevatorDoorClosedInstant();
            SetCustomerHiddenInstant();

            if (!TryGetPeekPendingNeedRefData(out CustomerNeedRefData firstNeedRefData, out TimeEffectData firstNeedTime))
            {
                ShowDayCompleteState();
                return;
            }

            StartPickupTravelToNextCustomer(firstNeedRefData, firstNeedTime, true);
        }


        private LevelRefData FindInitialLevelRefData()
        {
            List<LevelRefData> levelRefDataList = SCRefDataMgr.instance.levelRefList?.refDataList;
            if (levelRefDataList == null || levelRefDataList.Count == 0)
            {
                Debug.LogError("Gameplay 初始化失败：关卡表为空。");
                return null;
            }

            for (int index = 0; index < levelRefDataList.Count; index++)
            {
                LevelRefData levelRefData = levelRefDataList[index];
                if (levelRefData != null && levelRefData.id == DefaultLevelId)
                    return levelRefData;
            }

            Debug.LogError($"Gameplay 未找到默认关卡 id={DefaultLevelId}，将回退到第一条关卡配置。");
            return levelRefDataList[0];
        }


        private int FindLevelIndex(LevelRefData targetLevelRefData)
        {
            if (targetLevelRefData == null)
                return 0;

            for (int index = 0; index < _levelSequence.Count; index++)
            {
                if (_levelSequence[index] != null && _levelSequence[index].id == targetLevelRefData.id)
                    return index;
            }

            return 0;
        }


        private bool IsFinalDay()
        {
            return _levelSequence.Count > 0 && _currentDayIndex >= _levelSequence.Count - 1;
        }


        private bool IsFinalDayLastSpecialCustomer()
        {
            if (!IsFinalDay() || _currentCustomerRefData == null || _currentLevelRefData == null)
                return false;

            if (_pendingCustomers.Count > 0)
                return false;

            List<CustomerEffectData> customerEffectList = _currentLevelRefData.customerEffectList;
            if (customerEffectList == null || customerEffectList.Count == 0)
                return false;

            CustomerEffectData lastCustomerEffectData = customerEffectList[customerEffectList.Count - 1];
            if (lastCustomerEffectData == null || lastCustomerEffectData.customerType != ECustomerType.SPECIAL)
                return false;

            long specialCustomerId = CustomerPoolMgr.instance.ResolveCustomerId(lastCustomerEffectData);
            return specialCustomerId == _currentCustomerRefData.id;
        }


        private readonly struct PendingCustomerRuntimeData
        {
            public readonly long customerId;
            public readonly TimeEffectData needTime;

            public PendingCustomerRuntimeData(long customerId, TimeEffectData needTime)
            {
                this.customerId = customerId;
                this.needTime = needTime;
            }
        }
    }
}
