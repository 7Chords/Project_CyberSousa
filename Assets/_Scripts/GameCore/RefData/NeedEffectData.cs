using System;
using GameCore;
using SCFrame;

namespace GameCore.RefData
{
    public class NeedEffectData : _AEffectObjBase
    {
        public long levelId;
        public long needId;
        public bool hasFavorCondition;
        public ECompareOperator compareOperator;
        public int favorThreshold;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2)
                return;

            levelId = long.Parse(strs[0]);
            needId = long.Parse(strs[1]);

            if (strs.Length >= 4)
            {
                hasFavorCondition = true;
                compareOperator = (ECompareOperator)Enum.Parse(typeof(ECompareOperator), strs[2]);
                favorThreshold = int.Parse(strs[3]);
            }
        }

        protected override string OnSerialise()
        {
            return hasFavorCondition
                ? $"{levelId}:{needId}:{compareOperator}:{favorThreshold}"
                : $"{levelId}:{needId}";
        }
    }
}
