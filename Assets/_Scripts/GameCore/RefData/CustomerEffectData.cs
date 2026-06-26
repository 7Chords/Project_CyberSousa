using System;
using GameCore;
using SCFrame;

namespace GameCore.RefData
{
    public class CustomerEffectData : _AEffectObjBase
    {
        public ECustomerType customerType;
        public long poolId;
        public TimeEffectData needTime;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2)
                return;

            customerType = (ECustomerType)Enum.Parse(typeof(ECustomerType), strs[0]);
            poolId = long.Parse(strs[1]);
            if (strs.Length >= 5)
                needTime = TimeEffectData.Create(int.Parse(strs[2]), int.Parse(strs[3]), int.Parse(strs[4]));
        }

        protected override string OnSerialise()
        {
            return needTime != null
                ? $"{customerType}:{poolId}:{needTime.hour}:{needTime.minute}:{needTime.second}"
                : $"{customerType}:{poolId}";
        }
    }
}
