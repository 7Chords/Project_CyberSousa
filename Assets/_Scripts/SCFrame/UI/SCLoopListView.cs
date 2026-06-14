using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SCFrame.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class SCLoopListView : MonoBehaviour
    {
        [SerializeField] private SCLoopListArrangeType _arrangeType = SCLoopListArrangeType.TopToBottom;
        [SerializeField] private List<SCLoopListItemPrefabConfig> _itemPrefabDataList = new List<SCLoopListItemPrefabConfig>();

        private readonly Dictionary<string, SCLoopListItemPool> _itemPoolDict = new Dictionary<string, SCLoopListItemPool>();
        private readonly Dictionary<int, SCLoopListViewItem> _shownItemMap = new Dictionary<int, SCLoopListViewItem>();
        private readonly List<SCLoopListViewItem> _shownItemList = new List<SCLoopListViewItem>();

        private ScrollRect _scrollRect;
        private RectTransform _scrollRectTransform;
        private RectTransform _content;
        private RectTransform _viewport;
        private Func<SCLoopListView, int, SCLoopListViewItem> _onGetItemByIndex;
        private SCLoopListSizeCache _sizeCache;

        private float _distanceForRecycle0 = 300f;
        private float _distanceForNew0 = 200f;
        private float _distanceForRecycle1 = 300f;
        private float _distanceForNew1 = 200f;
        private float _defaultItemSizeWithPadding = 100f;

        private int _itemTotalCount;
        private int _itemIdGenerator;
        private bool _supportScrollBar = true;
        private bool _isListViewInited;
        private bool _isUpdating;

        public SCLoopListArrangeType ArrangeType
        {
            get => _arrangeType;
            set
            {
                _arrangeType = value;
                if (_isListViewInited)
                {
                    AdjustTransformsForArrangeType();
                    UpdateContentSize();
                    UpdateVisibleItems(true);
                }
            }
        }

        public ScrollRect ScrollRect => _scrollRect;
        public RectTransform Content => _content;
        public RectTransform Viewport => _viewport;
        public bool IsListViewInited => _isListViewInited;
        public bool IsVertList => _arrangeType == SCLoopListArrangeType.TopToBottom || _arrangeType == SCLoopListArrangeType.BottomToTop;
        public int ItemTotalCount => _itemTotalCount;
        public int ShownItemCount => _shownItemList.Count;

        private void LateUpdate()
        {
            if (!_isListViewInited || _isUpdating)
                return;

            UpdateVisibleItems(false);
        }

        public void SetItemPrefabDataList(IList<SCLoopListItemPrefabConfig> configs)
        {
            _itemPrefabDataList.Clear();
            if (configs != null)
                _itemPrefabDataList.AddRange(configs);
        }

        public SCLoopListItemPrefabConfig GetItemPrefabConfig(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;

            for (int i = 0; i < _itemPrefabDataList.Count; i++)
            {
                SCLoopListItemPrefabConfig config = _itemPrefabDataList[i];
                if (config?.itemPrefab == null)
                    continue;

                if (config.itemPrefab.name == prefabName)
                    return config;
            }

            return null;
        }

        public void InitListView(int itemTotalCount,
            Func<SCLoopListView, int, SCLoopListViewItem> onGetItemByIndex,
            SCLoopListInitParam initParam = null)
        {
            if (!EnsureReferences())
                return;

            ApplyInitParam(initParam);
            _onGetItemByIndex = onGetItemByIndex;
            _itemTotalCount = itemTotalCount;
            _supportScrollBar = _itemTotalCount >= 0;
            _sizeCache = new SCLoopListSizeCache(_defaultItemSizeWithPadding);
            if (_itemTotalCount >= 0)
                _sizeCache.Reset(_itemTotalCount);

            RecycleAllShownItems();
            ClearAllTmpRecycledItems();
            RebuildItemPools();

            _itemIdGenerator = 0;
            _isListViewInited = true;

            AdjustTransformsForArrangeType();
            SetPrimaryScrollPosition(0f, false);
            UpdateContentSize();
            UpdateVisibleItems(true);
        }

        public void SetListItemCount(int itemCount, bool resetPos = true)
        {
            if (!_isListViewInited)
                return;

            _itemTotalCount = itemCount;
            _supportScrollBar = _itemTotalCount >= 0;

            if (_itemTotalCount >= 0)
            {
                _sizeCache ??= new SCLoopListSizeCache(_defaultItemSizeWithPadding);
                _sizeCache.DefaultSizeWithPadding = _defaultItemSizeWithPadding;
                _sizeCache.Reset(_itemTotalCount);
            }

            RecycleAllShownItems();
            ClearAllTmpRecycledItems();
            UpdateContentSize();

            if (resetPos)
                SetPrimaryScrollPosition(0f, true);
            else
                UpdateVisibleItems(true);
        }

        public SCLoopListViewItem NewListViewItem(string itemPrefabName)
        {
            if (!_itemPoolDict.TryGetValue(itemPrefabName, out SCLoopListItemPool pool))
                return null;

            SCLoopListViewItem item = pool.GetItem();
            if (item == null)
                return null;

            RectTransform rect = item.CachedRectTransform;
            rect.SetParent(_content, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;

            PrepareItemRectTransform(rect);
            item.ParentListView = this;
            return item;
        }

        public SCLoopListViewItem GetShownItemByItemIndex(int itemIndex)
        {
            _shownItemMap.TryGetValue(itemIndex, out SCLoopListViewItem item);
            return item;
        }

        public void RefreshItemByItemIndex(int itemIndex)
        {
            if (!_shownItemMap.TryGetValue(itemIndex, out SCLoopListViewItem current))
                return;

            RemoveShownItem(current);
            RecycleItemTmp(current);

            SCLoopListViewItem refreshed = CreateItemByIndex(itemIndex);
            if (refreshed != null)
            {
                AddShownItem(refreshed);
            }

            SortShownItems();
            UpdateContentSize();
            UpdateAllShownItemsPos();
            ClearAllTmpRecycledItems();
        }

        public void RefreshAllShownItem()
        {
            if (_shownItemList.Count == 0)
            {
                UpdateVisibleItems(true);
                return;
            }

            List<int> indexes = new List<int>(_shownItemList.Count);
            for (int i = 0; i < _shownItemList.Count; i++)
            {
                indexes.Add(_shownItemList[i].ItemIndex);
            }

            for (int i = 0; i < indexes.Count; i++)
            {
                RefreshItemByItemIndex(indexes[i]);
            }
        }

        public void MovePanelToItemIndex(int itemIndex, float offset)
        {
            if (!_isListViewInited)
                return;

            float target = GetItemPrimaryStart(itemIndex) + offset;
            SetPrimaryScrollPosition(target, true);
        }

        public void OnItemSizeChanged(int itemIndex)
        {
            if (!_shownItemMap.TryGetValue(itemIndex, out SCLoopListViewItem item))
                return;

            if (_itemTotalCount < 0)
                return;

            float newSize = MeasureItemPrimarySizeWithPadding(item);
            if (_sizeCache != null && _sizeCache.SetItemSize(itemIndex, newSize))
            {
                UpdateContentSize();
                UpdateAllShownItemsPos();
                UpdateVisibleItems(true);
            }
        }

        public void OnItemPrefabChanged(string prefabName)
        {
            SCLoopListItemPrefabConfig config = GetItemPrefabConfig(prefabName);
            if (config == null)
                return;

            if (_itemPoolDict.TryGetValue(prefabName, out SCLoopListItemPool pool))
            {
                RecycleAllShownItems();
                ClearAllTmpRecycledItems();
                pool.DestroyAllItems();
                pool.Init(config, _content);
            }

            UpdateVisibleItems(true);
        }

        public void Clear()
        {
            RecycleAllShownItems();
            ClearAllTmpRecycledItems();

            foreach (SCLoopListItemPool pool in _itemPoolDict.Values)
            {
                pool.DestroyAllItems();
            }
            _itemPoolDict.Clear();

            _shownItemMap.Clear();
            _shownItemList.Clear();
            _onGetItemByIndex = null;
            _isListViewInited = false;
            _itemTotalCount = 0;
        }

        private void ApplyInitParam(SCLoopListInitParam initParam)
        {
            if (initParam == null)
                initParam = SCLoopListInitParam.CopyDefaultInitParam();

            _distanceForRecycle0 = Mathf.Max(initParam.distanceForRecycle0, initParam.distanceForNew0 + 1f);
            _distanceForNew0 = initParam.distanceForNew0;
            _distanceForRecycle1 = Mathf.Max(initParam.distanceForRecycle1, initParam.distanceForNew1 + 1f);
            _distanceForNew1 = initParam.distanceForNew1;
            _defaultItemSizeWithPadding = Mathf.Max(1f, initParam.itemDefaultWithPaddingSize);
        }

        private bool EnsureReferences()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();

            if (_scrollRect == null)
            {
                Debug.LogError("SCLoopListView init failed: ScrollRect component not found.");
                return false;
            }

            _scrollRectTransform = _scrollRect.transform as RectTransform;
            _content = _scrollRect.content;
            _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRectTransform;

            if (_content == null)
            {
                Debug.LogError("SCLoopListView init failed: ScrollRect.content is null.");
                return false;
            }

            return true;
        }

        private void RebuildItemPools()
        {
            foreach (SCLoopListItemPool pool in _itemPoolDict.Values)
            {
                pool.DestroyAllItems();
            }
            _itemPoolDict.Clear();

            for (int i = 0; i < _itemPrefabDataList.Count; i++)
            {
                SCLoopListItemPrefabConfig config = _itemPrefabDataList[i];
                if (config?.itemPrefab == null)
                    continue;

                string prefabName = config.itemPrefab.name;
                if (_itemPoolDict.ContainsKey(prefabName))
                    continue;

                SCLoopListItemPool pool = new SCLoopListItemPool();
                pool.Init(config, _content);
                _itemPoolDict.Add(prefabName, pool);
            }
        }

        private void AdjustTransformsForArrangeType()
        {
            bool vertical = IsVertList;
            _scrollRect.horizontal = !vertical;
            _scrollRect.vertical = vertical;

            switch (_arrangeType)
            {
                case SCLoopListArrangeType.TopToBottom:
                    ConfigureRect(_content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
                    break;
                case SCLoopListArrangeType.BottomToTop:
                    ConfigureRect(_content, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
                    break;
                case SCLoopListArrangeType.LeftToRight:
                    ConfigureRect(_content, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f));
                    break;
                case SCLoopListArrangeType.RightToLeft:
                    ConfigureRect(_content, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
                    break;
            }

            _content.anchoredPosition3D = Vector3.zero;
        }

        private static void ConfigureRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
        }

        private void PrepareItemRectTransform(RectTransform rectTransform)
        {
            switch (_arrangeType)
            {
                case SCLoopListArrangeType.TopToBottom:
                    ConfigureRect(rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                    break;
                case SCLoopListArrangeType.BottomToTop:
                    ConfigureRect(rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
                    break;
                case SCLoopListArrangeType.LeftToRight:
                    ConfigureRect(rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
                    break;
                case SCLoopListArrangeType.RightToLeft:
                    ConfigureRect(rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
                    break;
            }
        }

        private void UpdateVisibleItems(bool forceRefresh)
        {
            if (!_isListViewInited || _onGetItemByIndex == null || _content == null || _viewport == null)
                return;

            if (_isUpdating)
                return;

            _isUpdating = true;

            if (_itemTotalCount == 0)
            {
                RecycleAllShownItems();
                ClearAllTmpRecycledItems();
                UpdateContentSize();
                _isUpdating = false;
                return;
            }

            GetNeedShowRange(out int minIndex, out int maxIndex);
            RecycleItemsOutOfRange(minIndex, maxIndex);

            bool sizeChanged = false;
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (_shownItemMap.ContainsKey(i))
                    continue;

                SCLoopListViewItem item = CreateItemByIndex(i);
                if (item == null)
                    continue;

                AddShownItem(item);

                if (_itemTotalCount >= 0)
                {
                    float measured = MeasureItemPrimarySizeWithPadding(item);
                    if (_sizeCache.SetItemSize(i, measured))
                        sizeChanged = true;
                }
            }

            SortShownItems();
            if (sizeChanged || forceRefresh)
                UpdateContentSize();

            UpdateAllShownItemsPos();
            ClearAllTmpRecycledItems();
            _isUpdating = false;
        }

        private void GetNeedShowRange(out int minIndex, out int maxIndex)
        {
            float viewStart = Mathf.Max(0f, GetPrimaryScrollPosition() - _distanceForNew0);
            float viewEnd = GetPrimaryScrollPosition() + GetViewportPrimarySize() + _distanceForNew1;

            if (_itemTotalCount < 0)
            {
                float size = Mathf.Max(1f, _defaultItemSizeWithPadding);
                minIndex = Mathf.FloorToInt(viewStart / size);
                maxIndex = Mathf.CeilToInt(viewEnd / size);
                return;
            }

            if (_itemTotalCount == 0)
            {
                minIndex = 0;
                maxIndex = -1;
                return;
            }

            minIndex = _sizeCache.GetItemIndexByPosition(viewStart);
            maxIndex = _sizeCache.GetItemIndexByPosition(viewEnd);
            minIndex = Mathf.Clamp(minIndex, 0, _itemTotalCount - 1);
            maxIndex = Mathf.Clamp(maxIndex, 0, _itemTotalCount - 1);
        }

        private SCLoopListViewItem CreateItemByIndex(int itemIndex)
        {
            SCLoopListViewItem item = _onGetItemByIndex?.Invoke(this, itemIndex);
            if (item == null)
                return null;

            if (!_itemPoolDict.TryGetValue(item.ItemPrefabName, out SCLoopListItemPool pool))
                return null;

            item.ResetRuntimeState(this, itemIndex, ++_itemIdGenerator, item.ItemPrefabName, pool.Padding, pool.StartPosOffset);
            return item;
        }

        private void UpdateAllShownItemsPos()
        {
            for (int i = 0; i < _shownItemList.Count; i++)
            {
                SetItemPrimaryPosition(_shownItemList[i], _shownItemList[i].ItemIndex);
            }
        }

        private void SetItemPrimaryPosition(SCLoopListViewItem item, int itemIndex)
        {
            RectTransform rect = item.CachedRectTransform;
            float start = GetItemPrimaryStart(itemIndex) + item.StartPosOffset;

            switch (_arrangeType)
            {
                case SCLoopListArrangeType.TopToBottom:
                    rect.anchoredPosition = new Vector2(0f, -start);
                    break;
                case SCLoopListArrangeType.BottomToTop:
                    rect.anchoredPosition = new Vector2(0f, start);
                    break;
                case SCLoopListArrangeType.LeftToRight:
                    rect.anchoredPosition = new Vector2(start, 0f);
                    break;
                case SCLoopListArrangeType.RightToLeft:
                    rect.anchoredPosition = new Vector2(-start, 0f);
                    break;
            }
        }

        private float MeasureItemPrimarySizeWithPadding(SCLoopListViewItem item)
        {
            RectTransform rect = item.CachedRectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            float primarySize = IsVertList ? rect.rect.height : rect.rect.width;
            return Mathf.Max(1f, primarySize + item.Padding);
        }

        private void UpdateContentSize()
        {
            if (_content == null)
                return;

            if (_itemTotalCount < 0)
                return;

            float totalSize = _sizeCache != null ? _sizeCache.GetTotalSize() : 0f;
            if (IsVertList)
            {
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _viewport.rect.width);
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(totalSize, _viewport.rect.height));
            }
            else
            {
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(totalSize, _viewport.rect.width));
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _viewport.rect.height);
            }
        }

        private float GetViewportPrimarySize()
        {
            return IsVertList ? _viewport.rect.height : _viewport.rect.width;
        }

        private float GetPrimaryScrollPosition()
        {
            Vector2 anchoredPosition = _content.anchoredPosition;
            switch (_arrangeType)
            {
                case SCLoopListArrangeType.TopToBottom:
                    return Mathf.Max(0f, anchoredPosition.y);
                case SCLoopListArrangeType.BottomToTop:
                    return Mathf.Max(0f, -anchoredPosition.y);
                case SCLoopListArrangeType.LeftToRight:
                    return Mathf.Max(0f, -anchoredPosition.x);
                case SCLoopListArrangeType.RightToLeft:
                    return Mathf.Max(0f, anchoredPosition.x);
                default:
                    return 0f;
            }
        }

        private void SetPrimaryScrollPosition(float value, bool refreshNow)
        {
            if (_content == null)
                return;

            float target = Mathf.Max(0f, value);
            if (_itemTotalCount >= 0)
            {
                float max = Mathf.Max(0f, GetContentPrimarySize() - GetViewportPrimarySize());
                target = Mathf.Clamp(target, 0f, max);
            }

            _scrollRect.StopMovement();
            Vector2 anchoredPosition = _content.anchoredPosition;
            switch (_arrangeType)
            {
                case SCLoopListArrangeType.TopToBottom:
                    anchoredPosition.y = target;
                    break;
                case SCLoopListArrangeType.BottomToTop:
                    anchoredPosition.y = -target;
                    break;
                case SCLoopListArrangeType.LeftToRight:
                    anchoredPosition.x = -target;
                    break;
                case SCLoopListArrangeType.RightToLeft:
                    anchoredPosition.x = target;
                    break;
            }
            _content.anchoredPosition = anchoredPosition;

            if (refreshNow)
                UpdateVisibleItems(true);
        }

        private float GetContentPrimarySize()
        {
            return IsVertList ? _content.rect.height : _content.rect.width;
        }

        private float GetItemPrimaryStart(int itemIndex)
        {
            if (_itemTotalCount < 0 || _sizeCache == null)
                return Mathf.Max(0, itemIndex) * _defaultItemSizeWithPadding;

            return _sizeCache.GetItemStartPos(itemIndex);
        }

        private void RecycleItemsOutOfRange(int minIndex, int maxIndex)
        {
            for (int i = _shownItemList.Count - 1; i >= 0; i--)
            {
                SCLoopListViewItem item = _shownItemList[i];
                if (item.ItemIndex >= minIndex && item.ItemIndex <= maxIndex)
                    continue;

                RemoveShownItem(item);
                RecycleItemTmp(item);
            }
        }

        private void PruneShownItemsOutOfRange()
        {
            if (_itemTotalCount < 0)
                return;

            for (int i = _shownItemList.Count - 1; i >= 0; i--)
            {
                SCLoopListViewItem item = _shownItemList[i];
                if (item.ItemIndex < _itemTotalCount)
                    continue;

                RemoveShownItem(item);
                RecycleItemTmp(item);
            }
            ClearAllTmpRecycledItems();
        }

        private void AddShownItem(SCLoopListViewItem item)
        {
            _shownItemMap[item.ItemIndex] = item;
            _shownItemList.Add(item);
        }

        private void RemoveShownItem(SCLoopListViewItem item)
        {
            _shownItemMap.Remove(item.ItemIndex);
            _shownItemList.Remove(item);
        }

        private void SortShownItems()
        {
            _shownItemList.Sort((a, b) => a.ItemIndex.CompareTo(b.ItemIndex));
        }

        private void RecycleAllShownItems()
        {
            for (int i = _shownItemList.Count - 1; i >= 0; i--)
            {
                RecycleItemTmp(_shownItemList[i]);
            }

            _shownItemList.Clear();
            _shownItemMap.Clear();
        }

        private void RecycleItemTmp(SCLoopListViewItem item)
        {
            if (item == null)
                return;

            if (item.UserObjectData is ISCLoopListItemPanel itemPanel)
            {
                itemPanel.OnRecycleFromList();
            }

            if (_itemPoolDict.TryGetValue(item.ItemPrefabName, out SCLoopListItemPool pool))
            {
                pool.RecycleItem(item);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }

        private void ClearAllTmpRecycledItems()
        {
            foreach (SCLoopListItemPool pool in _itemPoolDict.Values)
            {
                pool.ClearTmpRecycledItems();
            }
        }
    }
}
