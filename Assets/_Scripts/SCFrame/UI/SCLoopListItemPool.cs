using System.Collections.Generic;
using UnityEngine;

namespace SCFrame.UI
{
    public class SCLoopListItemPool
    {
        private readonly List<SCLoopListViewItem> _tmpRecycledItems = new List<SCLoopListViewItem>();
        private readonly List<SCLoopListViewItem> _recycledItems = new List<SCLoopListViewItem>();

        private GameObject _prefab;
        private string _prefabName;
        private int _initCreateCount;
        private float _padding;
        private float _startPosOffset;
        private RectTransform _parent;
        private static int _itemIdGenerator;

        public string PrefabName => _prefabName;
        public float Padding => _padding;
        public float StartPosOffset => _startPosOffset;

        public void Init(SCLoopListItemPrefabConfig config, RectTransform parent)
        {
            _prefab = config.itemPrefab;
            _prefabName = _prefab != null ? _prefab.name : string.Empty;
            _initCreateCount = Mathf.Max(1, config.initCreateCount);
            _padding = config.padding;
            _startPosOffset = config.startPosOffset;
            _parent = parent;

            if (_prefab == null)
            {
                Debug.LogError("SCLoopListItemPool init failed: itemPrefab is null.");
                return;
            }

            _prefab.SetActive(false);
            for (int i = 0; i < _initCreateCount; i++)
            {
                RecycleItemReal(CreateItem());
            }
        }

        public SCLoopListViewItem GetItem()
        {
            _itemIdGenerator++;

            SCLoopListViewItem item = null;
            if (_tmpRecycledItems.Count > 0)
            {
                int lastIndex = _tmpRecycledItems.Count - 1;
                item = _tmpRecycledItems[lastIndex];
                _tmpRecycledItems.RemoveAt(lastIndex);
            }
            else if (_recycledItems.Count > 0)
            {
                int lastIndex = _recycledItems.Count - 1;
                item = _recycledItems[lastIndex];
                _recycledItems.RemoveAt(lastIndex);
            }
            else
            {
                item = CreateItem();
            }

            if (item != null)
                item.gameObject.SetActive(true);

            return item;
        }

        public void RecycleItem(SCLoopListViewItem item)
        {
            if (item == null)
                return;
            _tmpRecycledItems.Add(item);
        }

        public void ClearTmpRecycledItems()
        {
            for (int i = 0; i < _tmpRecycledItems.Count; i++)
            {
                RecycleItemReal(_tmpRecycledItems[i]);
            }
            _tmpRecycledItems.Clear();
        }

        public void DestroyAllItems()
        {
            ClearTmpRecycledItems();

            for (int i = 0; i < _recycledItems.Count; i++)
            {
                if (_recycledItems[i] != null)
                    Object.Destroy(_recycledItems[i].gameObject);
            }
            _recycledItems.Clear();
        }

        private SCLoopListViewItem CreateItem()
        {
            if (_prefab == null)
                return null;

            GameObject go = Object.Instantiate(_prefab, _parent);
            go.name = _prefabName;
            go.SetActive(true);

            RectTransform rect = go.transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.anchoredPosition3D = Vector3.zero;
            }

            SCLoopListViewItem item = go.GetComponent<SCLoopListViewItem>();
            if (item == null)
                item = go.AddComponent<SCLoopListViewItem>();
            item.ItemPrefabName = _prefabName;
            item.Padding = _padding;
            item.StartPosOffset = _startPosOffset;
            return item;
        }

        private void RecycleItemReal(SCLoopListViewItem item)
        {
            if (item == null)
                return;

            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent, false);
            _recycledItems.Add(item);
        }
    }
}
