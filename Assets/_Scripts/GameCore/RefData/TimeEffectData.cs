using System;
using SCFrame;

namespace GameCore.RefData
{
    public class TimeEffectData : _AEffectObjBase
    {
        public int hour;
        public int minute;
        public int second;

        public int TotalSeconds => hour * 3600 + minute * 60 + second;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 3)
                return;

            hour = int.Parse(strs[0]);
            minute = int.Parse(strs[1]);
            second = int.Parse(strs[2]);
        }

        protected override string OnSerialise()
        {
            return $"{hour}:{minute}:{second}";
        }

        public static TimeEffectData Create(int hourValue, int minuteValue, int secondValue)
        {
            return new TimeEffectData
            {
                hour = hourValue,
                minute = minuteValue,
                second = secondValue
            };
        }
    }
}
