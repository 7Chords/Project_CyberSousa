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

        private int _performanceValue;
        private bool _hasConfirmedFinalSpecialCustomer;

        public int performanceValue => _performanceValue;
        public bool hasConfirmedFinalSpecialCustomer => _hasConfirmedFinalSpecialCustomer;

        public override void OnInitialize()
        {
            ResetRuntimeData();
        }

        public override void OnDiscard()
        {
            _performanceValue = 0;
            _hasConfirmedFinalSpecialCustomer = false;
        }

        public void ResetRuntimeData()
        {
            _performanceValue = DefaultPerformanceValue;
            _hasConfirmedFinalSpecialCustomer = false;
            Debug.Log($"[GamePlayerDataMgr] 绩效值已重置：{_performanceValue}");
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
    }
}
