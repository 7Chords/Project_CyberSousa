using System;
using SCFrame;

namespace GameCore.RefData
{

    public class JudgmentEffectData : _AEffectObjBase
    {
        public EElevatorOperator elevatorOperator;
        public int floor;
        public int affectValue;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 3)
                return;

            elevatorOperator = (EElevatorOperator)Enum.Parse(typeof(EElevatorOperator), strs[0]);
            floor = int.Parse(strs[1]);
            affectValue = int.Parse(strs[2]);
        }

        protected override string OnSerialise()
        {
            return $"{elevatorOperator}:{floor}:{affectValue}";
        }
    }
}
