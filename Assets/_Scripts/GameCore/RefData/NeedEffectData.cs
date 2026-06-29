using System;
using GameCore;
using SCFrame;
using UnityEngine;

namespace GameCore.RefData
{
    public class NeedEffectData : _AEffectObjBase
    {
        public long levelId;
        public long needId;
        public bool hasCondition;
        public EConditionType conditionType;
        public int compareValue;
        public long conditionCustomerId;
        public int conditionFloor;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2)
            {
                Debug.LogError($"NeedEffectData 解析失败：配置格式错误，原始数据：{_str}");
                return;
            }

            levelId = long.Parse(strs[0]);
            needId = long.Parse(strs[1]);

            hasCondition = strs.Length > 2;
            if (!hasCondition)
                return;

            if (strs.Length == 4)
            {
                conditionType = ParseSingleValueConditionType(strs[2]);
                compareValue = int.Parse(strs[3]);
                return;
            }

            if (strs.Length == 5)
            {
                conditionType = (EConditionType)Enum.Parse(typeof(EConditionType), strs[2]);
                conditionCustomerId = long.Parse(strs[3]);
                conditionFloor = int.Parse(strs[4]);
                return;
            }

            throw new FormatException($"NeedEffectData 解析失败：配置格式错误，原始数据：{_str}");
        }

        protected override string OnSerialise()
        {
            if (!hasCondition)
                return $"{levelId}:{needId}";

            if (conditionType == EConditionType.CUSTOMER_DELIVERED_TO_FLOOR
                || conditionType == EConditionType.CUSTOMER_NOT_DELIVERED_TO_FLOOR)
            {
                return $"{levelId}:{needId}:{conditionType}:{conditionCustomerId}:{conditionFloor}";
            }

            return $"{levelId}:{needId}:{conditionType}:{compareValue}";
        }

        private static EConditionType ParseSingleValueConditionType(string rawConditionType)
        {
            if (Enum.TryParse(rawConditionType, out ECompareOperator compareOperator))
            {
                switch (compareOperator)
                {
                    case ECompareOperator.GREATER:
                        return EConditionType.FAVOR_GREATER;
                    case ECompareOperator.LESS:
                        return EConditionType.FAVOR_LESS;
                    case ECompareOperator.SELECTED:
                        return EConditionType.DIALOGUE_SELECTED;
                    case ECompareOperator.NOT_SELECTED:
                        return EConditionType.DIALOGUE_NOT_SELECTED;
                }
            }

            return (EConditionType)Enum.Parse(typeof(EConditionType), rawConditionType);
        }
    }
}
