using System.Collections.Generic;
using GameCore.RefData;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 住户池解析器：根据关卡配置从住户池中取出具体住户 id。
    /// </summary>
    public class CustomerPoolMgr : Singleton<CustomerPoolMgr>
    {
        private readonly Dictionary<long, CustomerPoolRefData> _poolRefDataMap = new Dictionary<long, CustomerPoolRefData>();

        public override void OnInitialize()
        {
            _poolRefDataMap.Clear();

            List<CustomerPoolRefData> poolRefDataList = SCRefDataMgr.instance.customerPoolRefList?.refDataList;
            if (poolRefDataList == null)
            {
                Debug.LogError("CustomerPoolMgr 初始化失败：customerPoolRefList 为空。");
                return;
            }

            for (int index = 0; index < poolRefDataList.Count; index++)
            {
                CustomerPoolRefData poolRefData = poolRefDataList[index];
                if (poolRefData == null)
                {
                    Debug.LogError($"CustomerPoolMgr 初始化失败：第 {index} 条住户池配置为空。");
                    continue;
                }

                if (_poolRefDataMap.ContainsKey(poolRefData.id))
                {
                    Debug.LogError($"CustomerPoolMgr 初始化失败：住户池 id 重复，id={poolRefData.id}");
                    continue;
                }

                _poolRefDataMap.Add(poolRefData.id, poolRefData);
            }
        }

        public override void OnDiscard()
        {
            _poolRefDataMap.Clear();
        }

        public CustomerPoolRefData GetPoolRefData(long poolId)
        {
            if (_poolRefDataMap.TryGetValue(poolId, out CustomerPoolRefData poolRefData))
                return poolRefData;

            Debug.LogError($"CustomerPoolMgr 获取住户池失败：未找到 poolId={poolId}");
            return null;
        }

        public long ResolveCustomerId(CustomerEffectData customerEffectData)
        {
            if (customerEffectData == null)
            {
                Debug.LogError("CustomerPoolMgr 解析住户失败：customerEffectData 为空。");
                return 0;
            }

            CustomerPoolRefData poolRefData = GetPoolRefData(customerEffectData.poolId);
            if (poolRefData == null || poolRefData.customerIdList == null || poolRefData.customerIdList.Count == 0)
            {
                Debug.LogError($"CustomerPoolMgr 解析住户失败：poolId={customerEffectData.poolId} 的住户列表为空。");
                return 0;
            }

            switch (customerEffectData.customerType)
            {
                case ECustomerType.SPECIAL:
                    return poolRefData.customerIdList[0];
                case ECustomerType.RANDOM:
                    int randomIndex = Random.Range(0, poolRefData.customerIdList.Count);
                    return poolRefData.customerIdList[randomIndex];
                default:
                    Debug.LogError($"CustomerPoolMgr 解析住户失败：未支持的住户类型 {customerEffectData.customerType}");
                    return 0;
            }
        }

    }
}
