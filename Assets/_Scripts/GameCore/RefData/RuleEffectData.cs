using System;
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
            param1 = int.Parse(strs[2]);
            param2 = int.Parse(strs[3]);
        }

        protected override string OnSerialise()
        {
            return $"{elevatorOperator}:{effectType}:{param1}:{param2}";
        }
    }
}
