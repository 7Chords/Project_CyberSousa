using System.Collections.Generic;
using SCFrame;

namespace GameCore.RefData
{
    public class CustomerNeedRefData : SCRefDataCore
    {
        public long id;
        public List<JudgmentEffectData> judgmentList;
        public List<DialogueEffectData> dialogueList;
        public TimeEffectData timeEffect;
        public int needFloor;

        protected override void _parseFromString()
        {
            id = getLong("id");
            judgmentList = getList<JudgmentEffectData>("judgmentList");
            dialogueList = getList<DialogueEffectData>("dialogueList");
            timeEffect = getEffect<TimeEffectData>("timeEffect");
            needFloor = getInt("needFloor");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "customerNeed";
    }
}
