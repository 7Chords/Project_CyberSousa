using System;
using GameCore;
using SCFrame;

namespace GameCore.RefData
{
    public class CustomerEffectData : _AEffectObjBase
    {
        public ECustomerType customerType;
        public long poolId;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2)
                return;

            customerType = (ECustomerType)Enum.Parse(typeof(ECustomerType), strs[0]);
            poolId = long.Parse(strs[1]);
        }

        protected override string OnSerialise()
        {
            return $"{customerType}:{poolId}";
        }
    }
}
