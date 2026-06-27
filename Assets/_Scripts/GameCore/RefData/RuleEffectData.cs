using System;
using System.Collections.Generic;
using SCFrame;
using UnityEngine;

namespace GameCore.RefData
{
    public class RuleEffectData : _AEffectObjBase
    {
        public EElevatorOperator elevatorOperator;
        public ERuleEffectType effectType;
        public int param1;
        public int param2;
        public List<int> comboFloorList = new List<int>();
        public List<string> customerTagList = new List<string>();

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 4)
            {
                Debug.LogError($"RuleEffectData 解析失败：配置格式错误，原始数据：{_str}");
                return;
            }

            elevatorOperator = (EElevatorOperator)Enum.Parse(typeof(EElevatorOperator), strs[0]);
            effectType = (ERuleEffectType)Enum.Parse(typeof(ERuleEffectType), strs[1]);
            comboFloorList.Clear();
            customerTagList.Clear();
            param1 = 0;
            param2 = 0;

            if (effectType == ERuleEffectType.COMBO_TARGET_FLOOR)
            {
                string[] floorStrs = strs[2].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (floorStrs.Length == 0)
                {
                    Debug.LogError($"RuleEffectData 解析失败：组合键楼层为空，原始数据：{_str}");
                    return;
                }

                for (int index = 0; index < floorStrs.Length; index++)
                    comboFloorList.Add(int.Parse(floorStrs[index].Trim()));

                param2 = int.Parse(strs[3]);
                return;
            }

            if (effectType == ERuleEffectType.FORBID_CUSTOMER_TAG_TARGET_FLOOR)
            {
                string[] tagStrs = strs[2].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (tagStrs.Length == 0)
                {
                    Debug.LogError($"RuleEffectData 解析失败：住户标签为空，原始数据：{_str}");
                    return;
                }

                for (int index = 0; index < tagStrs.Length; index++)
                    customerTagList.Add(tagStrs[index].Trim());

                param2 = int.Parse(strs[3]);
                return;
            }

            param1 = int.Parse(strs[2]);
            param2 = int.Parse(strs[3]);
        }

        protected override string OnSerialise()
        {
            if (effectType == ERuleEffectType.COMBO_TARGET_FLOOR)
                return $"{elevatorOperator}:{effectType}:{string.Join(",", comboFloorList)}:{param2}";

            if (effectType == ERuleEffectType.FORBID_CUSTOMER_TAG_TARGET_FLOOR)
                return $"{elevatorOperator}:{effectType}:{string.Join(",", customerTagList)}:{param2}";

            return $"{elevatorOperator}:{effectType}:{param1}:{param2}";
        }
    }
}
