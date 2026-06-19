using System.Collections.Generic;
using GameCore.RefData;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 规则处理器
    /// </summary>
    public class RuleMgr : Singleton<RuleMgr>
    {
        private readonly Dictionary<long, RuleRefData> _ruleRefDataMap = new Dictionary<long, RuleRefData>();

        public override void OnInitialize()
        {
            _ruleRefDataMap.Clear();

            List<RuleRefData> ruleRefDataList = SCRefDataMgr.instance.ruleRefList?.refDataList;
            if (ruleRefDataList == null)
            {
                Debug.LogError("RuleMgr 初始化失败：ruleRefList 为空。");
                return;
            }

            for (int index = 0; index < ruleRefDataList.Count; index++)
            {
                RuleRefData ruleRefData = ruleRefDataList[index];
                if (ruleRefData == null)
                {
                    Debug.LogError($"RuleMgr 初始化失败：第 {index} 条规则配置为空。");
                    continue;
                }

                if (_ruleRefDataMap.ContainsKey(ruleRefData.id))
                {
                    Debug.LogError($"RuleMgr 初始化失败：规则配置 id 重复，id={ruleRefData.id}");
                    continue;
                }

                _ruleRefDataMap.Add(ruleRefData.id, ruleRefData);
            }
        }

        public override void OnDiscard()
        {
            _ruleRefDataMap.Clear();
        }

        public RuleRefData GetRuleRefData(long ruleId)
        {
            if (_ruleRefDataMap.TryGetValue(ruleId, out RuleRefData ruleRefData))
                return ruleRefData;

            Debug.LogError($"RuleMgr 获取规则配置失败：未找到 ruleId={ruleId}");
            return null;
        }

        public RuleEffectData EvaluateRule(long ruleId, EElevatorOperator actionType, int targetFloor)
        {
            RuleRefData ruleRefData = GetRuleRefData(ruleId);
            if (ruleRefData == null)
                return null;

            RuleEffectData result = MatchRuleEffect(ruleRefData, actionType, targetFloor);
            if (result != null)
                ExecuteRuleEffect(result, actionType, targetFloor);

            return result;
        }

        public RuleEffectData EvaluateRuleList(List<long> ruleIdList, EElevatorOperator actionType, int targetFloor)
        {
            if (ruleIdList == null || ruleIdList.Count == 0)
            {
                Debug.LogError("RuleMgr 批量判断规则失败：ruleIdList 为空。");
                return null;
            }

            for (int index = 0; index < ruleIdList.Count; index++)
            {
                RuleEffectData result = EvaluateRule(ruleIdList[index], actionType, targetFloor);
                if (result != null)
                    return result;
            }

            return null;
        }

        public RuleEffectData EvaluateGotoRule(long ruleId, int targetFloor)
        {
            return EvaluateRule(ruleId, EElevatorOperator.GOTO, targetFloor);
        }

        public RuleEffectData EvaluateRefuseRule(long ruleId)
        {
            return EvaluateRule(ruleId, EElevatorOperator.REFUSE, 0);
        }

        public bool IsGotoBlocked(RuleEffectData ruleEffectData)
        {
            return ruleEffectData != null && ruleEffectData.effectType == ERuleEffectType.FORBID_TARGET_FLOOR;
        }

        public bool TryGetRedirectFloor(RuleEffectData ruleEffectData, out int redirectFloor)
        {
            redirectFloor = 0;
            if (ruleEffectData == null || ruleEffectData.effectType != ERuleEffectType.REDIRECT_TARGET_FLOOR)
                return false;

            redirectFloor = ruleEffectData.param2;
            return true;
        }

        private RuleEffectData MatchRuleEffect(RuleRefData ruleRefData, EElevatorOperator actionType, int targetFloor)
        {
            List<RuleEffectData> effectList = ruleRefData.effectList;
            if (effectList == null || effectList.Count == 0)
            {
                Debug.LogError($"RuleMgr 判断规则失败：ruleId={ruleRefData.id} 的 effectList 为空。");
                return null;
            }

            for (int index = 0; index < effectList.Count; index++)
            {
                RuleEffectData effectData = effectList[index];
                if (effectData == null)
                {
                    Debug.LogError($"RuleMgr 判断规则失败：ruleId={ruleRefData.id} 的 effectList 第 {index} 项为空。");
                    continue;
                }

                if (effectData.elevatorOperator != actionType)
                    continue;

                switch (effectData.effectType)
                {
                    case ERuleEffectType.FORBID_TARGET_FLOOR:
                        if (actionType == EElevatorOperator.GOTO && effectData.param1 == targetFloor)
                            return effectData;
                        break;
                    case ERuleEffectType.REDIRECT_TARGET_FLOOR:
                        if (actionType == EElevatorOperator.GOTO && effectData.param1 == targetFloor)
                            return effectData;
                        break;
                    case ERuleEffectType.FORBID_REFUSE:
                        if (actionType == EElevatorOperator.REFUSE)
                            return effectData;
                        break;
                    default:
                        Debug.LogError($"RuleMgr 判断规则失败：未支持的规则效果类型 {effectData.effectType}，ruleId={ruleRefData.id}");
                        break;
                }
            }

            return null;
        }

        private void ExecuteRuleEffect(RuleEffectData effectData, EElevatorOperator actionType, int targetFloor)
        {
            switch (effectData.effectType)
            {
                case ERuleEffectType.FORBID_TARGET_FLOOR:
                    HandleForbidTargetFloor(effectData, actionType, targetFloor);
                    break;
                case ERuleEffectType.REDIRECT_TARGET_FLOOR:
                    HandleRedirectTargetFloor(effectData, actionType, targetFloor);
                    break;
                case ERuleEffectType.FORBID_REFUSE:
                    HandleForbidRefuse(effectData, actionType);
                    break;
                default:
                    Debug.LogError($"RuleMgr 执行规则失败：未支持的规则效果类型 {effectData.effectType}");
                    break;
            }
        }

        private void HandleForbidTargetFloor(RuleEffectData effectData, EElevatorOperator actionType, int targetFloor)
        {
            if (actionType != EElevatorOperator.GOTO)
            {
                Debug.LogError($"RuleMgr 执行禁止楼层规则失败：操作类型不匹配，actionType={actionType}");
                return;
            }

            if (effectData.param1 != targetFloor)
            {
                Debug.LogError($"RuleMgr 执行禁止楼层规则失败：目标楼层不匹配，targetFloor={targetFloor}，ruleFloor={effectData.param1}");
                return;
            }

            // TODO: 这里接真实的禁止前往楼层逻辑，例如拦截按钮确认、弹出提示并阻止流程继续推进。
        }

        private void HandleRedirectTargetFloor(RuleEffectData effectData, EElevatorOperator actionType, int targetFloor)
        {
            if (actionType != EElevatorOperator.GOTO)
            {
                Debug.LogError($"RuleMgr 执行改写楼层规则失败：操作类型不匹配，actionType={actionType}");
                return;
            }

            if (effectData.param1 != targetFloor)
            {
                Debug.LogError($"RuleMgr 执行改写楼层规则失败：目标楼层不匹配，targetFloor={targetFloor}，ruleFloor={effectData.param1}");
                return;
            }

            // TODO: 这里接真实的楼层改写逻辑，例如把当前请求楼层从 param1 改成 param2，并通知后续流程使用新楼层。
        }

        private void HandleForbidRefuse(RuleEffectData effectData, EElevatorOperator actionType)
        {
            if (actionType != EElevatorOperator.REFUSE)
            {
                Debug.LogError($"RuleMgr 执行禁止拒绝规则失败：操作类型不匹配，actionType={actionType}");
                return;
            }

            // TODO: 这里接真实的禁止拒绝逻辑，例如拦截拒绝操作、弹出提示并要求玩家改为其他处理方式。
        }
    }
}
