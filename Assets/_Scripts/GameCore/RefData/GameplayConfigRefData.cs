using SCFrame;

namespace GameCore.RefData
{
    public class GameplayConfigRefData : SCRefDataCore
    {
        public int maxPerformance;
        public int missingConfirmPenalty;
        public int perfectOperationBonus;

        protected override void _parseFromString()
        {
            maxPerformance = getInt("maxPerformance");
            missingConfirmPenalty = getInt("missingConfirmPenalty");
            perfectOperationBonus = getInt("perfectOperationBonus");
        }

        public static readonly string assetPath = "RefData/ExportTxt";
        public static readonly string sheetName = "gameplay_config";
    }
}
