using System;
using SCFrame;

namespace GameCore.RefData
{

    public class DialogueEffectData : _AEffectObjBase
    {
        public ECompareOperator compareOperator;
        public int threshold;
        public long dialogueStartId;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 3)
                return;

            compareOperator = (ECompareOperator)Enum.Parse(typeof(ECompareOperator), strs[0]);
            threshold = int.Parse(strs[1]);
            dialogueStartId = long.Parse(strs[2]);
        }

        protected override string OnSerialise()
        {
            return $"{compareOperator}:{threshold}:{dialogueStartId}";
        }
    }
}
