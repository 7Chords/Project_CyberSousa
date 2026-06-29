using System.Collections.Generic;
using SCFrame;
using GameCore;
using UnityEngine;

namespace GameCore.RefData
{
    public class DialogueRefData : SCRefDataCore
    {
        public long id;
        public string name;
        public string content;
        public EDialogueType dialogueType;
        public EDialogueFlagType flagType;
        public List<long> nextList;
        public int affectValue;
        public List<NpcFavorEffectData> extraFavorEffectList;

        protected override void _parseFromString()
        {
            id = getLong("id");
            name = getString("name");
            content = getString("content");
            dialogueType = (EDialogueType)getEnum("dialogueType", typeof(EDialogueType));
            flagType = (EDialogueFlagType)getEnum("flagType", typeof(EDialogueFlagType));
            nextList = getList<long>("nextList");
            affectValue = getInt("affectValue");
            extraFavorEffectList = getList<NpcFavorEffectData>("extraFavorEffectList");
        }

        public static readonly string assetPath = "RefData/ExportTxt";

        public static readonly string sheetName = "dialogue";
    }

    public class NpcFavorEffectData : _AEffectObjBase
    {
        public long npcId;
        public int favorValue;

        protected override void OnDeserialize(string _str)
        {
            string[] strs = _str.Split(new char[] { ':' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length < 2)
            {
                Debug.LogError($"NpcFavorEffectData 解析失败：配置格式错误，原始数据：{_str}");
                return;
            }

            npcId = long.Parse(strs[0]);
            favorValue = int.Parse(strs[1]);
        }

        protected override string OnSerialise()
        {
            return $"{npcId}:{favorValue}";
        }
    }
}
