using System.Collections.Generic;
using GameCore.RefData;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 住户需求处理器
    /// </summary>
    public class CustomerNeedMgr : Singleton<CustomerNeedMgr>
    {
        private readonly Dictionary<long, CustomerNeedRefData> _needRefDataMap = new Dictionary<long, CustomerNeedRefData>();

        public override void OnInitialize()
        {
            _needRefDataMap.Clear();

            List<CustomerNeedRefData> needRefDataList = SCRefDataMgr.instance.customerNeedRefList?.refDataList;
            if (needRefDataList == null)
            {
                Debug.LogError("CustomerNeedMgr 初始化失败：customerNeedRefList 为空。");
                return;
            }

            for (int index = 0; index < needRefDataList.Count; index++)
            {
                CustomerNeedRefData needRefData = needRefDataList[index];
                if (needRefData == null)
                {
                    Debug.LogError($"CustomerNeedMgr 初始化失败：第 {index} 条住户需求配置为空。");
                    continue;
                }

                if (_needRefDataMap.ContainsKey(needRefData.id))
                {
                    Debug.LogError($"CustomerNeedMgr 初始化失败：住户需求配置 id 重复，id={needRefData.id}");
                    continue;
                }

                _needRefDataMap.Add(needRefData.id, needRefData);
            }
        }

        public override void OnDiscard()
        {
            _needRefDataMap.Clear();
        }

        public CustomerNeedRefData GetNeedRefData(long needId)
        {
            if (_needRefDataMap.TryGetValue(needId, out CustomerNeedRefData needRefData))
                return needRefData;

            Debug.LogError($"CustomerNeedMgr 获取住户需求配置失败：未找到 needId={needId}");
            return null;
        }

        public CustomerNeedRefData ResolveCustomerNeed(CustomerRefData customerRefData, long levelId, int favorValue)
        {
            if (customerRefData == null)
                return null;

            if (customerRefData.needList == null || customerRefData.needList.Count == 0)
                return null;

            for (int index = 0; index < customerRefData.needList.Count; index++)
            {
                NeedEffectData needEffectData = customerRefData.needList[index];
                if (needEffectData == null)
                {
                    Debug.LogError($"CustomerNeedMgr 判断住户需求失败：customerId={customerRefData.id} 的 needList 第 {index} 项为空。");
                    continue;
                }

                CustomerNeedRefData needRefData = GetNeedRefData(needEffectData.needId);
                if (needRefData == null)
                    continue;

                if (levelId > 0 && needEffectData.levelId != levelId)
                    continue;

                if (!IsNeedMatched(customerRefData.id, needEffectData, favorValue))
                    continue;

                return needRefData;
            }

            Debug.LogError($"CustomerNeedMgr 未命中住户需求条件：customerId={customerRefData.id}，levelId={levelId}，favorValue={favorValue}");
            return null;
        }

        public bool CanSpawnCustomerNeed(CustomerNeedRefData needRefData, TimeEffectData needTime, int currentFloor)
        {
            if (needRefData == null)
                return false;

            if (!GameTimeMgr.instance.HasReachedNeedTime(needTime))
                return false;

            if (needRefData.needFloor > 0 && currentFloor != needRefData.needFloor)
                return false;

            return true;
        }

        public JudgmentEffectData EvaluateJudgment(long needId, EElevatorOperator actionType, int targetFloor)
        {
            JudgmentEffectData result = null;
            switch (actionType)
            {
                case EElevatorOperator.GOTO:
                    result = EvaluateGoto(needId, targetFloor);
                    break;
                case EElevatorOperator.REFUSE:
                    result = EvaluateRefuse(needId);
                    break;
                case EElevatorOperator.DEFAULT:
                    result = EvaluateDefault(needId);
                    break;
                default:
                    Debug.LogError($"CustomerNeedMgr 判断住户需求失败：未支持的操作类型 {actionType}，needId={needId}");
                    break;
            }

            return result;
        }

        public JudgmentEffectData EvaluateGoto(long needId, int targetFloor)
        {
            CustomerNeedRefData needRefData = GetNeedRefData(needId);
            if (needRefData == null)
                return null;

            List<JudgmentEffectData> judgmentList = needRefData.judgmentList;
            if (judgmentList == null || judgmentList.Count == 0)
            {
                Debug.LogError($"CustomerNeedMgr 判断前往楼层失败：needId={needId} 的 judgmentList 为空。");
                return null;
            }

            for (int index = 0; index < judgmentList.Count; index++)
            {
                JudgmentEffectData judgmentEffectData = judgmentList[index];
                if (judgmentEffectData == null)
                {
                    Debug.LogError($"CustomerNeedMgr 判断前往楼层失败：needId={needId} 的 judgmentList 第 {index} 项为空。");
                    continue;
                }

                if (judgmentEffectData.elevatorOperator == EElevatorOperator.GOTO && judgmentEffectData.floor == targetFloor)
                    return judgmentEffectData;
            }

            Debug.LogWarning($"CustomerNeedMgr 未命中 GOTO 配置，开始回退 DEFAULT：needId={needId}，targetFloor={targetFloor}");
            return EvaluateDefault(needId);
        }

        public JudgmentEffectData EvaluateRefuse(long needId)
        {
            JudgmentEffectData result = FindJudgmentByOperator(needId, EElevatorOperator.REFUSE);
            if (result != null)
                return result;

            Debug.LogWarning($"CustomerNeedMgr 未命中 REFUSE 配置，开始回退 DEFAULT：needId={needId}");
            return EvaluateDefault(needId);
        }

        public JudgmentEffectData EvaluateDefault(long needId)
        {
            JudgmentEffectData result = FindJudgmentByOperator(needId, EElevatorOperator.DEFAULT);
            if (result != null)
                return result;

            Debug.LogError($"CustomerNeedMgr 未命中 DEFAULT 配置：needId={needId}");
            return null;
        }

        public DialogueEffectData EvaluateDialogue(long needId, int favorValue)
        {
            CustomerNeedRefData needRefData = GetNeedRefData(needId);
            if (needRefData == null)
                return null;

            return EvaluateDialogue(needRefData, favorValue);
        }

        public DialogueEffectData EvaluateDialogue(CustomerNeedRefData needRefData, int favorValue)
        {
            if (needRefData == null)
                return null;

            List<DialogueEffectData> dialogueList = needRefData.dialogueList;
            if (dialogueList == null || dialogueList.Count == 0)
            {
                Debug.LogError($"CustomerNeedMgr 判断需求对话失败：needId={needRefData.id} 的 dialogueList 为空。");
                return null;
            }

            for (int index = 0; index < dialogueList.Count; index++)
            {
                DialogueEffectData dialogueEffectData = dialogueList[index];
                if (dialogueEffectData == null)
                {
                    Debug.LogError($"CustomerNeedMgr 判断需求对话失败：needId={needRefData.id} 的 dialogueList 第 {index} 项为空。");
                    continue;
                }

                if (!IsDialogueMatched(needRefData.id, dialogueEffectData, favorValue))
                    continue;

                return dialogueEffectData;
            }

            Debug.LogError($"CustomerNeedMgr 未命中需求对话条件：needId={needRefData.id}，favorValue={favorValue}");
            return null;
        }

        public long ResolveDialogueStartId(CustomerNeedRefData needRefData, int favorValue)
        {
            DialogueEffectData dialogueEffectData = EvaluateDialogue(needRefData, favorValue);
            if (dialogueEffectData != null)
                return dialogueEffectData.dialogueStartId;

            return 0;
        }

        private JudgmentEffectData FindJudgmentByOperator(long needId, EElevatorOperator actionType)
        {
            CustomerNeedRefData needRefData = GetNeedRefData(needId);
            if (needRefData == null)
                return null;

            List<JudgmentEffectData> judgmentList = needRefData.judgmentList;
            if (judgmentList == null || judgmentList.Count == 0)
            {
                Debug.LogError($"CustomerNeedMgr 判断住户需求失败：needId={needId} 的 judgmentList 为空。");
                return null;
            }

            for (int index = 0; index < judgmentList.Count; index++)
            {
                JudgmentEffectData judgmentEffectData = judgmentList[index];
                if (judgmentEffectData == null)
                {
                    Debug.LogError($"CustomerNeedMgr 判断住户需求失败：needId={needId} 的 judgmentList 第 {index} 项为空。");
                    continue;
                }

                if (judgmentEffectData.elevatorOperator == actionType)
                    return judgmentEffectData;
            }

            return null;
        }

        private bool IsNeedMatched(long customerId, NeedEffectData needEffectData, int favorValue)
        {
            if (needEffectData == null)
                return false;

            if (!needEffectData.hasCondition)
                return true;

            return IsConditionMatched(
                needEffectData.conditionType,
                needEffectData.compareValue,
                needEffectData.conditionCustomerId,
                needEffectData.conditionFloor,
                favorValue,
                $"CustomerNeedMgr 判断住户需求条件失败：customerId={customerId}，needId={needEffectData.needId}");
        }

        private bool IsDialogueMatched(long needId, DialogueEffectData dialogueEffectData, int favorValue)
        {
            if (dialogueEffectData == null)
                return false;

            return IsConditionMatched(
                dialogueEffectData.conditionType,
                dialogueEffectData.compareValue,
                dialogueEffectData.conditionCustomerId,
                dialogueEffectData.conditionFloor,
                favorValue,
                $"CustomerNeedMgr 判断需求对话条件失败：needId={needId}，dialogueStartId={dialogueEffectData.dialogueStartId}");
        }

        private bool IsConditionMatched(EConditionType conditionType, int compareValue, long conditionCustomerId, int conditionFloor, int favorValue, string logPrefix)
        {
            switch (conditionType)
            {
                case EConditionType.FAVOR_GREATER:
                    return favorValue > compareValue;
                case EConditionType.FAVOR_LESS:
                    return favorValue < compareValue;
                case EConditionType.DIALOGUE_SELECTED:
                    return GamePlayerDataMgr.instance.HasSelectedDialogueOption(compareValue);
                case EConditionType.DIALOGUE_NOT_SELECTED:
                    return !GamePlayerDataMgr.instance.HasSelectedDialogueOption(compareValue);
                case EConditionType.CUSTOMER_DELIVERED_TO_FLOOR:
                    return GamePlayerDataMgr.instance.HasCustomerDeliveredToFloor(conditionCustomerId, conditionFloor);
                case EConditionType.CUSTOMER_NOT_DELIVERED_TO_FLOOR:
                    return !GamePlayerDataMgr.instance.HasCustomerDeliveredToFloor(conditionCustomerId, conditionFloor);
                default:
                    Debug.LogError($"{logPrefix}：未支持的条件类型 {conditionType}");
                    return false;
            }
        }
    }
}
