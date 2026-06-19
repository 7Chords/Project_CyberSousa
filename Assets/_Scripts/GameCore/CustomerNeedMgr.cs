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

            if (result != null)
            {
                // TODO: 这里接真实的住户需求生效逻辑，例如好感度变化、状态更新和后续流程推进。
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

            Debug.LogError($"CustomerNeedMgr 未命中 GOTO 配置，开始回退 DEFAULT：needId={needId}，targetFloor={targetFloor}");
            return EvaluateDefault(needId);
        }

        public JudgmentEffectData EvaluateRefuse(long needId)
        {
            JudgmentEffectData result = FindJudgmentByOperator(needId, EElevatorOperator.REFUSE);
            if (result != null)
                return result;

            Debug.LogError($"CustomerNeedMgr 未命中 REFUSE 配置，开始回退 DEFAULT：needId={needId}");
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

        public DialogueEffectData EvaluateDialogue(long needId, int affectValue)
        {
            CustomerNeedRefData needRefData = GetNeedRefData(needId);
            if (needRefData == null)
                return null;

            List<DialogueEffectData> dialogueList = needRefData.dialogueList;
            if (dialogueList == null || dialogueList.Count == 0)
            {
                Debug.LogError($"CustomerNeedMgr 判断需求对话失败：needId={needId} 的 dialogueList 为空。");
                return null;
            }

            for (int index = 0; index < dialogueList.Count; index++)
            {
                DialogueEffectData dialogueEffectData = dialogueList[index];
                if (dialogueEffectData == null)
                {
                    Debug.LogError($"CustomerNeedMgr 判断需求对话失败：needId={needId} 的 dialogueList 第 {index} 项为空。");
                    continue;
                }

                if (!IsDialogueMatched(dialogueEffectData, affectValue))
                    continue;

                // TODO: 这里接真实的需求对话触发逻辑，例如打开对话、写入状态和推进后续流程。
                return dialogueEffectData;
            }

            Debug.LogError($"CustomerNeedMgr 未命中对话需求：needId={needId}，affectValue={affectValue}");
            return null;
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

        private bool IsDialogueMatched(DialogueEffectData dialogueEffectData, int affectValue)
        {
            switch (dialogueEffectData.compareOperator)
            {
                case ECompareOperator.GREATER:
                    return affectValue > dialogueEffectData.threshold;
                case ECompareOperator.LESS:
                    return affectValue < dialogueEffectData.threshold;
                default:
                    Debug.LogError($"CustomerNeedMgr 判断需求对话失败：未支持的比较符 {dialogueEffectData.compareOperator}");
                    return false;
            }
        }
    }
}
