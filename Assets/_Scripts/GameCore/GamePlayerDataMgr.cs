using System;
using System.Collections.Generic;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 游戏全局玩家数据。
    /// </summary>
    public class GamePlayerDataMgr : Singleton<GamePlayerDataMgr>
    {
        private const int DefaultPerformanceValue = 100;
        private const string SaveDataPrefKey = "GameCore.SaveData";

        private int _performanceValue;
        private bool _hasConfirmedFinalSpecialCustomer;
        private int _startDayIndex;
        private readonly Dictionary<long, int> _npcFavorDict = new Dictionary<long, int>();

        public int performanceValue => _performanceValue;
        public bool hasConfirmedFinalSpecialCustomer => _hasConfirmedFinalSpecialCustomer;
        public int startDayIndex => _startDayIndex;
        public bool hasSaveData => HasValidSaveData();

        public override void OnInitialize()
        {
            ResetRuntimeData();
        }

        public override void OnDiscard()
        {
            _performanceValue = 0;
            _hasConfirmedFinalSpecialCustomer = false;
            _startDayIndex = 0;
            _npcFavorDict.Clear();
        }

        public void ResetRuntimeData()
        {
            _performanceValue = DefaultPerformanceValue;
            _hasConfirmedFinalSpecialCustomer = false;
            _startDayIndex = 0;
            _npcFavorDict.Clear();
            Debug.Log($"[GamePlayerDataMgr] 绩效值已重置：{_performanceValue}");
        }

        public void BeginNewGame()
        {
            ClearSaveData();
            ResetRuntimeData();
        }

        public bool TryLoadSaveData()
        {
            if (!TryReadSaveData(out SaveData saveData))
                return false;

            ApplySaveData(saveData);
            Debug.Log($"[GamePlayerDataMgr] 存档读取完成：nextDayIndex={_startDayIndex}，绩效值={_performanceValue}");
            return true;
        }

        public void ClearSaveData()
        {
            if (!PlayerPrefs.HasKey(SaveDataPrefKey))
                return;

            PlayerPrefs.DeleteKey(SaveDataPrefKey);
            PlayerPrefs.Save();
            Debug.Log("[GamePlayerDataMgr] 存档已清除。");
        }

        private bool HasValidSaveData()
        {
            return TryReadSaveData(out _);
        }

        private bool TryReadSaveData(out SaveData saveData, bool clearInvalidSave = true)
        {
            saveData = null;
            if (!PlayerPrefs.HasKey(SaveDataPrefKey))
                return false;

            string saveJson = PlayerPrefs.GetString(SaveDataPrefKey, string.Empty);
            if (string.IsNullOrEmpty(saveJson))
            {
                Debug.LogError("[GamePlayerDataMgr] 读取存档失败：存档内容为空。");
                if (clearInvalidSave)
                    ClearSaveData();
                return false;
            }

            try
            {
                saveData = JsonUtility.FromJson<SaveData>(saveJson);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GamePlayerDataMgr] 读取存档失败：{exception.Message}");
                if (clearInvalidSave)
                    ClearSaveData();
                return false;
            }

            if (saveData == null)
            {
                Debug.LogError("[GamePlayerDataMgr] 读取存档失败：反序列化结果为空。");
                if (clearInvalidSave)
                    ClearSaveData();
                return false;
            }

            if (!IsSaveDataValid(saveData))
            {
                Debug.LogError($"[GamePlayerDataMgr] 读取存档失败：nextDayIndex={saveData.nextDayIndex} 不是有效进度。");
                if (clearInvalidSave)
                    ClearSaveData();
                return false;
            }

            return true;
        }

        private bool IsSaveDataValid(SaveData saveData)
        {
            if (saveData.nextDayIndex < 0)
                return false;

            int levelCount = SCRefDataMgr.instance.levelRefList?.refDataList?.Count ?? 0;
            return levelCount <= 0 || saveData.nextDayIndex < levelCount;
        }

        private void ApplySaveData(SaveData saveData)
        {
            _performanceValue = saveData.performanceValue;
            _hasConfirmedFinalSpecialCustomer = saveData.hasConfirmedFinalSpecialCustomer;
            _startDayIndex = saveData.nextDayIndex;
            _npcFavorDict.Clear();

            if (saveData.npcFavorList != null)
            {
                for (int index = 0; index < saveData.npcFavorList.Count; index++)
                {
                    NpcFavorSaveData favorSaveData = saveData.npcFavorList[index];
                    if (favorSaveData == null)
                        continue;

                    _npcFavorDict[favorSaveData.npcId] = favorSaveData.favorValue;
                }
            }
        }

        public void SaveDailyProgress(int nextDayIndex)
        {
            _startDayIndex = Mathf.Max(0, nextDayIndex);
            SaveData saveData = new SaveData
            {
                nextDayIndex = _startDayIndex,
                performanceValue = _performanceValue,
                hasConfirmedFinalSpecialCustomer = _hasConfirmedFinalSpecialCustomer,
                npcFavorList = new List<NpcFavorSaveData>()
            };

            foreach (KeyValuePair<long, int> pair in _npcFavorDict)
            {
                saveData.npcFavorList.Add(new NpcFavorSaveData
                {
                    npcId = pair.Key,
                    favorValue = pair.Value
                });
            }

            string saveJson = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SaveDataPrefKey, saveJson);
            PlayerPrefs.Save();

            Debug.Log($"[GamePlayerDataMgr] 每日存档完成：nextDayIndex={saveData.nextDayIndex}，绩效值={_performanceValue}，NPC好感度数量={saveData.npcFavorList.Count}");
        }

        public void AddNpcFavor(long npcId, int value)
        {
            if (npcId <= 0 || value == 0)
                return;

            _npcFavorDict.TryGetValue(npcId, out int oldValue);
            _npcFavorDict[npcId] = oldValue + value;
            Debug.Log($"[GamePlayerDataMgr] NPC好感度变化：npcId={npcId}，变化={value}，当前={_npcFavorDict[npcId]}");
        }

        public int GetNpcFavor(long npcId)
        {
            if (_npcFavorDict.TryGetValue(npcId, out int favorValue))
                return favorValue;

            return 0;
        }

        public void AddPerformance(int value)
        {
            if (value <= 0)
            {
                Debug.LogError($"[GamePlayerDataMgr] 增加绩效值失败：value={value}");
                return;
            }

            _performanceValue += value;
            Debug.Log($"[GamePlayerDataMgr] 绩效值增加 {value}，当前绩效值={_performanceValue}");
        }

        public void DeductPerformance(int value)
        {
            if (value <= 0)
            {
                Debug.LogError($"[GamePlayerDataMgr] 扣除绩效值失败：value={value}");
                return;
            }

            int oldValue = _performanceValue;
            _performanceValue -= value;
            Debug.Log($"[GamePlayerDataMgr] 绩效值扣除 {oldValue - _performanceValue}，当前绩效值={_performanceValue}");
        }

        public void MarkFinalSpecialCustomerConfirmed()
        {
            if (_hasConfirmedFinalSpecialCustomer)
                return;

            _hasConfirmedFinalSpecialCustomer = true;
            Debug.Log("[GamePlayerDataMgr] 已记录最终特殊住户确认选择。");
        }

        [Serializable]
        private class SaveData
        {
            public int nextDayIndex;
            public int performanceValue;
            public bool hasConfirmedFinalSpecialCustomer;
            public List<NpcFavorSaveData> npcFavorList;
        }

        [Serializable]
        private class NpcFavorSaveData
        {
            public long npcId;
            public int favorValue;
        }
    }
}
