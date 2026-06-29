using System;
using SCFrame;

namespace GameCore.RefData
{

    public class DialogueEffectData : _AEffectObjBase
    {
        public EConditionType conditionType;
        public int compareValue;
        public long conditionCustomerId;
        public int conditionFloor;
        public long dialogueStartId;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length == 3)
            {
                conditionType = ParseSingleValueConditionType(strs[0]);
                compareValue = int.Parse(strs[1]);
                dialogueStartId = long.Parse(strs[2]);
                return;
            }

            if (strs.Length == 4)
            {
                conditionType = (EConditionType)Enum.Parse(typeof(EConditionType), strs[0]);
                conditionCustomerId = long.Parse(strs[1]);
                conditionFloor = int.Parse(strs[2]);
                dialogueStartId = long.Parse(strs[3]);
                return;
            }

            throw new FormatException($"DialogueEffectData 解析失败：配置格式错误，原始数据：{_str}");
        }

        protected override string OnSerialise()
        {
            if (conditionType == EConditionType.CUSTOMER_DELIVERED_TO_FLOOR
                || conditionType == EConditionType.CUSTOMER_NOT_DELIVERED_TO_FLOOR)
            {
                return $"{conditionType}:{conditionCustomerId}:{conditionFloor}:{dialogueStartId}";
            }

            return $"{conditionType}:{compareValue}:{dialogueStartId}";
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
