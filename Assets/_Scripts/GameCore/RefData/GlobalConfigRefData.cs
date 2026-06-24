using SCFrame;

namespace GameCore.RefData
{
    /// <summary>
    /// 全局 key-value 配表，使用竖向 txt 格式读取。
    /// </summary>
    public class GlobalConfigRefData : SCRefDataCore
    {
        public TimeEffectData dayStartTime;
        public TimeEffectData elevatorTravelTime;

        public GlobalConfigRefData() : base(assetPath, sheetName)
        {
        }

        protected override void _parseFromString()
        {
            dayStartTime = getEffect<TimeEffectData>("dayStartTime") ?? TimeEffectData.Create(8, 0, 0);
            elevatorTravelTime = getEffect<TimeEffectData>("elevatorTravelTime") ?? TimeEffectData.Create(0, 5, 0);
        }

        public static readonly string assetPath = "RefData/ExportTxt";
        public static readonly string sheetName = "globalConfig";
    }
}
