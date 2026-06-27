using System.Collections.Generic;
using GameCore.RefData;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    public class GotoRuleResolutionResult
    {
        public int originalFloor;
        public int finalFloor;
        public bool comboMatched;
        public bool isBlocked;
        public RuleEffectData lastEffectData;
    }

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

        public bool TryResolveGotoRuleSequence(List<long> ruleIdList, int selectedFloor, List<int> recentFloorList,
            out GotoRuleResolutionResult resolutionResult)
        {
            resolutionResult = new GotoRuleResolutionResult
            {
                originalFloor = selectedFloor,
                finalFloor = selectedFloor,
            };
            if (selectedFloor <= 0)
            {
                Debug.LogError($"RuleMgr 顺序判断楼层规则失败：selectedFloor 无效，selectedFloor={selectedFloor}");
                resolutionResult.isBlocked = true;
                return false;
            }

            if (ruleIdList != null && ruleIdList.Count > 0)
            {
                for (int ruleIndex = 0; ruleIndex < ruleIdList.Count; ruleIndex++)
                {
                    RuleRefData ruleRefData = GetRuleRefData(ruleIdList[ruleIndex]);
                    if (ruleRefData == null)
                        continue;

                    List<RuleEffectData> effectList = ruleRefData.effectList;
                    if (effectList == null || effectList.Count == 0)
                    {
                        Debug.LogError($"RuleMgr 顺序判断楼层规则失败：ruleId={ruleRefData.id} 的 effectList 为空。");
                        continue;
                    }

                    for (int effectIndex = 0; effectIndex < effectList.Count; effectIndex++)
                    {
                        RuleEffectData effectData = effectList[effectIndex];
                        if (effectData == null)
                        {
                            Debug.LogError($"RuleMgr 顺序判断楼层规则失败：ruleId={ruleRefData.id} 的 effectList 第 {effectIndex} 项为空。");
                            continue;
                        }

                        if (effectData.elevatorOperator != EElevatorOperator.GOTO)
                            continue;

                        switch (effectData.effectType)
                        {
                            case ERuleEffectType.FORBID_TARGET_FLOOR:
                                if (effectData.param1 == resolutionResult.finalFloor)
                                {
                                    resolutionResult.isBlocked = true;
                                    resolutionResult.lastEffectData = effectData;
                                }
                                break;
                            case ERuleEffectType.REDIRECT_TARGET_FLOOR:
                                if (effectData.param1 == resolutionResult.finalFloor)
                                {
                                    resolutionResult.finalFloor = effectData.param2;
                                    resolutionResult.isBlocked = false;
                                    resolutionResult.lastEffectData = effectData;
                                }
                                break;
                            case ERuleEffectType.COMBO_TARGET_FLOOR:
                                if (!IsComboFloorMatched(effectData.comboFloorList, recentFloorList))
                                    break;

                                if (!TryGetComboTargetFloor(effectData, out int comboTargetFloor))
                                {
                                    Debug.LogError($"RuleMgr 顺序判断楼层规则失败：ruleId={ruleRefData.id} 的组合键目标楼层无效。");
                                    break;
                                }

                                resolutionResult.finalFloor = comboTargetFloor;
                                resolutionResult.comboMatched = true;
                                resolutionResult.isBlocked = false;
                                resolutionResult.lastEffectData = effectData;
                                break;
                            default:
                                Debug.LogError($"RuleMgr 顺序判断楼层规则失败：未支持的规则效果类型 {effectData.effectType}，ruleId={ruleRefData.id}");
                                break;
                        }
                    }
                }
            }

            if (!resolutionResult.isBlocked
                && !resolutionResult.comboMatched
                && resolutionResult.finalFloor == selectedFloor
                && IsComboOnlyTargetFloor(ruleIdList, selectedFloor))
            {
                resolutionResult.isBlocked = true;
                resolutionResult.lastEffectData = new RuleEffectData
                {
                    elevatorOperator = EElevatorOperator.GOTO,
                    effectType = ERuleEffectType.COMBO_TARGET_FLOOR,
                    param2 = selectedFloor
                };
            }

            return !resolutionResult.isBlocked;
        }

        public bool IsComboOnlyTargetFloor(List<long> ruleIdList, int targetFloor)
        {
            if (targetFloor <= 0 || ruleIdList == null || ruleIdList.Count == 0)
                return false;

            for (int index = 0; index < ruleIdList.Count; index++)
            {
                RuleRefData ruleRefData = GetRuleRefData(ruleIdList[index]);
                if (ruleRefData == null || ruleRefData.effectList == null)
                    continue;

                for (int effectIndex = 0; effectIndex < ruleRefData.effectList.Count; effectIndex++)
                {
                    RuleEffectData effectData = ruleRefData.effectList[effectIndex];
                    if (effectData == null)
                    {
                        Debug.LogError($"RuleMgr 判断组合目标楼层失败：ruleId={ruleRefData.id} 的 effectList 第 {effectIndex} 项为空。");
                        continue;
                    }

                    if (effectData.elevatorOperator != EElevatorOperator.GOTO || effectData.effectType != ERuleEffectType.COMBO_TARGET_FLOOR)
                        continue;

                    if (effectData.param2 == targetFloor)
                        return true;
                }
            }

            return false;
        }

        public bool TryGetComboTargetFloor(RuleEffectData ruleEffectData, out int comboTargetFloor)
        {
            comboTargetFloor = 0;
            if (ruleEffectData == null || ruleEffectData.effectType != ERuleEffectType.COMBO_TARGET_FLOOR)
                return false;

            comboTargetFloor = ruleEffectData.param2;
            return comboTargetFloor > 0;
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
                    case ERuleEffectType.COMBO_TARGET_FLOOR:
                        break;
                    default:
                        Debug.LogError($"RuleMgr 判断规则失败：未支持的规则效果类型 {effectData.effectType}，ruleId={ruleRefData.id}");
                        break;
                }
            }

            return null;
        }

        private static bool IsComboFloorMatched(List<int> comboFloorList, List<int> recentFloorList)
        {
            if (comboFloorList == null || comboFloorList.Count == 0 || recentFloorList == null || recentFloorList.Count == 0)
                return false;

            if (recentFloorList.Count < comboFloorList.Count)
                return false;

            List<int> recentTailFloorList = new List<int>(comboFloorList.Count);
            int startIndex = recentFloorList.Count - comboFloorList.Count;
            for (int index = startIndex; index < recentFloorList.Count; index++)
                recentTailFloorList.Add(recentFloorList[index]);

            for (int index = 0; index < comboFloorList.Count; index++)
            {
                int matchFloor = comboFloorList[index];
                if (!recentTailFloorList.Contains(matchFloor))
                    return false;

                recentTailFloorList.Remove(matchFloor);
            }

            return true;
        }
    }
}
