using System;
using SCFrame;

namespace GameCore.RefData
{
    public class NeedEffectData : _AEffectObjBase
    {
        public long levelId;
        public long needId;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2)
                return;

            levelId = long.Parse(strs[0]);
            needId = long.Parse(strs[1]);
        }

        protected override string OnSerialise()
        {
            return $"{levelId}:{needId}";
        }
    }
}
