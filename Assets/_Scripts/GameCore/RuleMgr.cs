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

            return MatchRuleEffect(ruleRefData, actionType, targetFloor);
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

        // 新增“拒绝类”的规则效果时，继续在 RuleMgr 提供纯业务判定接口，不要在这里直接写 UI 或流程表现。
        public bool IsRefuseBlocked(RuleEffectData ruleEffectData)
        {
            return ruleEffectData != null && ruleEffectData.effectType == ERuleEffectType.FORBID_REFUSE;
        }

        // 新增“改写目标”的规则效果时，继续在 RuleMgr 提供结果解析接口，由外层决定怎么表现。
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
                    // 新增规则效果类型时，先在这里补“是否命中该效果”的纯业务判断。
                    // 这里只负责返回 RuleEffectData，不要在 RuleMgr 里直接改 UI、播动画或推进流程。
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
    }
}
