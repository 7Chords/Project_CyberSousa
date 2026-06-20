using System.Collections.Generic;
using SCFrame;
using GameCore;

namespace GameCore.RefData
{
    public class DialogueRefData : SCRefDataCore
    {
        public long id;
        public string name;
        public string portraitResName;
        public string content;
        public EDialogueType dialogueType;
        public EDialogueFlagType flagType;
        public List<long> nextList;
        public int affectValue;

        protected override void _parseFromString()
        {
            id = getLong("id");
            name = getString("name");
            portraitResName = getString("portraitResName");
            content = getString("content");
            dialogueType = (EDialogueType)getEnum("dialogueType", typeof(EDialogueType));
            flagType = (EDialogueFlagType)getEnum("flagType", typeof(EDialogueFlagType));
            nextList = getList<long>("nextList");
            affectValue = getInt("affectValue");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "dialogue";
    }
}
