using DG.Tweening;
using GameCore.RefData;
using SCFrame;
using SCFrame.UI;
using UnityEngine;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        private void TrySpawnNextPendingCustomer()
        {
            if (_currentCustomerRefData != null)
                return;

            if (_pendingCustomers.Count == 0)
            {
                ShowDayCompleteState();
                return;
            }

            PendingCustomerRuntimeData pendingCustomerData = _pendingCustomers.Peek();
            long customerId = pendingCustomerData.customerId;
            CustomerRefData customerRefData = GetCustomerRefData(customerId);
            if (customerRefData == null)
            {
                _pendingCustomers.Dequeue();
                TrySpawnNextPendingCustomer();
                return;
            }

            CustomerNeedRefData needRefData = FindNeedRefDataForCustomer(customerRefData);
            if (needRefData == null)
            {
                Debug.LogError($"Gameplay 住户初始化失败：levelId={_currentLevelRefData?.id}，customerId={customerId} 未找到当前关卡需求配置。");
                _pendingCustomers.Dequeue();
                TrySpawnNextPendingCustomer();
                return;
            }

            if (!CustomerNeedMgr.instance.CanSpawnCustomerNeed(needRefData, pendingCustomerData.needTime, _currentFloor))
            {
                if (GameTimeMgr.instance.HasReachedNeedTime(pendingCustomerData.needTime))
                {
                    StartPickupTravelToNextCustomer(needRefData, pendingCustomerData.needTime, false);
                    return;
                }

                TryAutoAdvanceToPendingNeedTime(pendingCustomerData.needTime);
                SetCustomerHiddenInstant();
                RefreshAllUi();
                return;
            }

            _pendingCustomers.Dequeue();
            EnsureElevatorDoorOpenForCustomer(() =>
            {
                SpawnCustomer(customerRefData, needRefData, pendingCustomerData.needTime);
            });
        }


        private void TryAutoAdvanceToPendingNeedTime(TimeEffectData needTime)
        {
            if (needTime == null || GameTimeMgr.instance.IsAdvancing)
                return;

            if (GameTimeMgr.instance.HasReachedNeedTime(needTime))
                return;

            int remainingSeconds = GameTimeMgr.instance.GetRemainingSecondsUntil(needTime);
            if (remainingSeconds <= 0)
                return;

            GameTimeMgr.instance.PlayAdvanceToTime(needTime, CalculateIdleAdvanceDuration(remainingSeconds));
        }


        private float CalculateIdleAdvanceDuration(int remainingGameSeconds)
        {
            int travelSeconds = GameTimeMgr.instance.GetElevatorTravelSeconds();
            if (travelSeconds <= 0)
                return 1f;

            float duration = remainingGameSeconds * (ElevatorTravelAnimDuration / travelSeconds);
            return Mathf.Clamp(duration, 0.35f, 8f);
        }


        private void SpawnCustomer(CustomerRefData customerRefData, CustomerNeedRefData needRefData, TimeEffectData needTime)
        {
            ClearDialoguePortraits();
            _selectedFloor = 0;
            _resolvedTargetFloor = 0;
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _hasConfirmedCurrentCustomer = false;
            _hasRejectedCurrentCustomer = false;
            _hasCurrentOperationProblem = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;
            _serviceFeedbackText = null;
            _ruleFeedbackText = null;
            _isCustomerInfoVisible = false;
            _currentCustomerRefData = customerRefData;
            _currentNeedRefData = needRefData;
            _currentNeedTime = needTime;

            long dialogueStartId = GetDialogueStartId(customerRefData, needRefData);
            if (dialogueStartId > 0)
                StartDialogue(dialogueStartId);
            else
                Debug.LogError($"Gameplay 对话初始化失败：customerId={customerRefData.id}，needId={needRefData.id} 未找到有效的开始对话 id。");

            ShowCurrentCustomer();
            RefreshAllUi();
        }


        private void ShowDayCompleteState()
        {
            mono.txtCustomerName.text = "本关已完成";
            mono.txtAnimalInfo.text = "当前没有新的住户。";
            mono.txtBottomHint.text = "本关住户处理完成。";
            mono.dialogueSection?.SetActive(false);
            SetCustomerHiddenInstant();

            if (TryShowEndingOnDayComplete())
                return;

            ShowSettlementPanel();
        }


        private void FinishCurrentCustomerService()
        {
            SettleCurrentCustomerPerformance("FinishCurrentCustomerService");
            ClearCurrentCustomerState();
            if (_pendingCustomers.Count == 0)
            {
                _isTransitionPlaying = false;
                ShowDayCompleteState();
                return;
            }

            if (!TryGetPeekPendingNeedRefData(out CustomerNeedRefData nextNeedRefData, out TimeEffectData nextNeedTime))
            {
                _isTransitionPlaying = false;
                ShowDayCompleteState();
                return;
            }

            StartPickupTravelToNextCustomer(nextNeedRefData, nextNeedTime, false);
        }


        private void ClearCurrentCustomerState()
        {
            ClearDialoguePortraits();
            _isCustomerInfoVisible = false;
            _currentCustomerRefData = null;
            _currentNeedRefData = null;
            _currentNeedTime = null;
            _currentDialogueRefData = null;
            ClearDialogueOptions();
            _selectedFloor = 0;
            _resolvedTargetFloor = 0;
            _recentFloorInputData.Clear();
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _hasConfirmedCurrentCustomer = false;
            _hasRejectedCurrentCustomer = false;
            _hasCurrentOperationProblem = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;
            _serviceFeedbackText = null;
            _ruleFeedbackText = null;
            SetCustomerHiddenInstant();
        }


        private void StartPickupTravelToNextCustomer(CustomerNeedRefData needRefData, TimeEffectData needTime, bool prepareRejectTravelTime)
        {
            if (needRefData == null)
                return;

            int targetFloor = needRefData.needFloor > 0 ? needRefData.needFloor : 1;
            if (prepareRejectTravelTime)
            {
                if (_currentFloor != targetFloor)
                    GameTimeMgr.instance.SetClockTimeBeforeElevatorTravel(needTime);
                else
                    GameTimeMgr.instance.SetClockTime(needTime);
            }

            if (_currentFloor == targetFloor)
            {
                _isTransitionPlaying = false;
                TrySpawnNextPendingCustomer();
                RefreshAllUi();
                return;
            }

            _isTransitionPlaying = true;
            _selectedFloor = 0;
            _resolvedTargetFloor = 0;
            RefreshAllUi();
            CloseElevatorDoor(() =>
            {
                PlayElevatorTravelToFloor(targetFloor, () =>
                {
                    OpenElevatorDoor(() =>
                    {
                        _isTransitionPlaying = false;
                        TrySpawnNextPendingCustomer();
                        RefreshAllUi();
                    });
                });
            });
        }


        private void StartRejectAndPickupNextFlow()
        {
            _isTransitionPlaying = true;
            RefreshAllUi();
            CloseElevatorDoor(() =>
            {
                HideCurrentCustomer(() =>
                {
                    SettleCurrentCustomerPerformance("StartRejectAndPickupNextFlow");
                    ClearCurrentCustomerState();
                    if (!TryGetPeekPendingNeedRefData(out CustomerNeedRefData nextNeedRefData, out TimeEffectData nextNeedTime))
                    {
                        _isTransitionPlaying = false;
                        ShowDayCompleteState();
                        return;
                    }

                    StartPickupTravelToNextCustomer(nextNeedRefData, nextNeedTime, true);
                });
            });
        }


        private bool TryGetPeekPendingNeedRefData(out CustomerNeedRefData needRefData, out TimeEffectData needTime)
        {
            needRefData = null;
            needTime = null;
            if (_pendingCustomers.Count == 0)
                return false;

            PendingCustomerRuntimeData pendingCustomerData = _pendingCustomers.Peek();
            long customerId = pendingCustomerData.customerId;
            CustomerRefData customerRefData = GetCustomerRefData(customerId);
            if (customerRefData == null)
                return false;

            needRefData = FindNeedRefDataForCustomer(customerRefData);
            needTime = pendingCustomerData.needTime;
            return needRefData != null;
        }


        private CustomerNeedRefData FindNeedRefDataForCustomer(CustomerRefData customerRefData)
        {
            if (customerRefData == null)
                return null;

            long levelId = _currentLevelRefData != null ? _currentLevelRefData.id : 0;
            int favorValue = GamePlayerDataMgr.instance.GetNpcFavor(customerRefData.id);
            return CustomerNeedMgr.instance.ResolveCustomerNeed(customerRefData, levelId, favorValue);
        }


        private CustomerRefData GetCustomerRefData(long customerId)
        {
            if (_customerMap.TryGetValue(customerId, out CustomerRefData customerRefData))
                return customerRefData;

            Debug.LogError($"Gameplay 获取住户配置失败：未找到 customerId={customerId}");
            return null;
        }


        private long GetDialogueStartId(CustomerRefData customerRefData, CustomerNeedRefData needRefData)
        {
            if (customerRefData == null || needRefData == null)
                return 0;

            int favorValue = GamePlayerDataMgr.instance.GetNpcFavor(customerRefData.id);
            return CustomerNeedMgr.instance.ResolveDialogueStartId(needRefData, favorValue);
        }


        private void ApplyServiceFeedbackByJudgment(JudgmentEffectData judgmentEffectData)
        {
            if (judgmentEffectData == null)
                return;

            AddCurrentCustomerFavor(judgmentEffectData.affectValue);

            if (judgmentEffectData.affectValue < 0)
            {
                MarkCurrentCustomerOperationProblem("服务结果错误，本轮不计入绩效奖励。");
                GamePlayerDataMgr.instance.DeductPerformance(-judgmentEffectData.affectValue);
                _serviceFeedbackText =
                    $"处理错误：绩效值 -{-judgmentEffectData.affectValue}，当前绩效值 {GamePlayerDataMgr.instance.performanceValue}。";
                return;
            }

            _serviceFeedbackText = $"处理正确：影响值 +{judgmentEffectData.affectValue}。";
        }


        private void ApplyGotoViolationDepartureFeedback(int departureFloor)
        {
            MarkCurrentCustomerOperationProblem("违反规则，本轮不计入绩效奖励。");
            GamePlayerDataMgr.instance.DeductPerformance(1);
            _serviceFeedbackText = $"违反规则，绩效-1，当前绩效值 {GamePlayerDataMgr.instance.performanceValue}。";

            string ruleEffectType = _lastRuleEffectData != null ? _lastRuleEffectData.effectType.ToString() : "NONE";
            long customerId = _currentCustomerRefData != null ? _currentCustomerRefData.id : 0;
            long needId = _currentNeedRefData != null ? _currentNeedRefData.id : 0;
            Debug.LogWarning(
                $"Gameplay 违反规则，绩效-1，仍放走当前住户。customerId={customerId}，needId={needId}，selectedFloor={_selectedFloor}，departureFloor={departureFloor}，ruleEffectType={ruleEffectType}");
        }


        private int ResolveFailedCustomerDepartureFloor(int selectedFloor, int resolvedFloor)
        {
            int departureFloor = resolvedFloor > 0 ? resolvedFloor : selectedFloor;
            if (departureFloor > 0)
                return departureFloor;

            Debug.LogError(
                $"Gameplay 规则异常：无法解析住户离开楼层，selectedFloor={selectedFloor}，resolvedFloor={resolvedFloor}，currentFloor={_currentFloor}");
            return _currentFloor > 0 ? _currentFloor : 1;
        }


        private void AddCurrentCustomerFavor(int favorValue)
        {
            if (_currentCustomerRefData == null || favorValue == 0)
                return;

            GamePlayerDataMgr.instance.AddNpcFavor(_currentCustomerRefData.id, favorValue);
        }


        private void StartEmptyElevatorTravel(int targetFloor)
        {
            if (targetFloor <= 0)
            {
                Debug.LogError("Gameplay 空梯移动失败：目标楼层无效。");
                return;
            }

            _isTransitionPlaying = true;
            _selectedFloor = 0;
            _resolvedTargetFloor = 0;
            RefreshAllUi();
            CloseElevatorDoor(() =>
            {
                PlayElevatorTravelToFloor(targetFloor, () =>
                {
                    OpenElevatorDoor(() =>
                    {
                        _isTransitionPlaying = false;
                        TrySpawnNextPendingCustomer();
                        RefreshAllUi();
                    });
                });
            });
        }


        private void ShowSettlementPanel()
        {
            if (_isSettlementShowing)
                return;

            _isSettlementShowing = true;
            GamePlayerDataMgr.instance.SaveDailyProgress(_currentDayIndex + 1);
            UIPanelSettlement.pendingTitle = $"第 {_currentDayIndex + 1} 天结算";
            UIPanelSettlement.pendingSummary = $"第 {_currentDayIndex + 1} 天的客人已经全部接待完成。";
            UIPanelSettlement.onNextDayClicked = HandleNextDayClicked;

            UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoSettlement, UIPanelSettlement>(
                SCUIShowType.ADDITION,
                "panel_settlement",
                SettlementNodeName,
                false,
                false,
                false,
                false));
        }


        private bool TryShowEndingOnDayComplete()
        {
            if (GamePlayerDataMgr.instance.performanceValue < 0)
            {
                ShowEndingPanel(EGameEndingType.BAD);
                return true;
            }

            if (!IsFinalDay())
                return false;

            ShowEndingPanel(GamePlayerDataMgr.instance.hasConfirmedFinalSpecialCustomer
                ? EGameEndingType.ENDING_1
                : EGameEndingType.ENDING_2);
            return true;
        }


        private void ShowEndingPanel(EGameEndingType endingType)
        {
            _hasRunEnded = true;
            GamePlayerDataMgr.instance.ClearSaveData();
            UIPanelEnding.pendingEndingType = endingType;

            UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoEnding, UIPanelEnding>(
                SCUIShowType.FULL,
                "panel_ending",
                EndingNodeName,
                true,
                true,
                false,
                false));
        }


        private void HandleNextDayClicked()
        {
            int nextDayIndex = _currentDayIndex + 1;
            if (nextDayIndex >= _levelSequence.Count)
            {
                SCDebugHelper.Log("所有天数都已完成，游戏结束。");
                return;
            }

            _currentDayIndex = nextDayIndex;
            _currentLevelRefData = _levelSequence[_currentDayIndex];
            GamePlayerDataMgr.instance.SaveDailyProgress(_currentDayIndex);
            StartCurrentDay();
            RefreshAllUi();
        }


        private int ResolveCustomerDepartureFloor()
        {
            if (_lastJudgmentEffectData != null && _lastJudgmentEffectData.elevatorOperator == EElevatorOperator.GOTO)
                return _resolvedTargetFloor > 0 ? _resolvedTargetFloor : _selectedFloor;

            return 1;
        }


        private void StartCustomerDepartureFlow(int targetFloor, bool needBoardAnimation)
        {
            if (targetFloor <= 0)
            {
                Debug.LogError($"Gameplay ס���뿪ʧ�ܣ�Ŀ��¥����Ч��targetFloor={targetFloor}");
                targetFloor = _currentFloor > 0 ? _currentFloor : 1;
            }

            if (_currentCustomerRefData != null)
                GamePlayerDataMgr.instance.RecordCustomerDeliveredFloor(_currentCustomerRefData.id, targetFloor);

            _canCloseDoor = false;
            _isTransitionPlaying = true;
            RefreshAllUi();
            TweenCallback afterBoardAction = () =>
            {
                PlayElevatorTravelToFloor(targetFloor, () =>
                {
                    OpenElevatorDoor(() =>
                    {
                        TweenCallback afterArriveLeave = () =>
                        {
                            CloseElevatorDoor(() =>
                            {
                                FinishCurrentCustomerService();
                            });
                        };

                        if (needBoardAnimation)
                            PlayCustomerLeaveElevator(afterArriveLeave);
                        else
                            HideCurrentCustomer(afterArriveLeave);
                    });
                });
            };

            if (needBoardAnimation)
            {
                PlayCustomerBoardElevator(() =>
                {
                    CloseElevatorDoor(afterBoardAction);
                });
                return;
            }

            CloseElevatorDoor(() =>
            {
                HideCurrentCustomer(afterBoardAction);
            });
        }
    }
}
