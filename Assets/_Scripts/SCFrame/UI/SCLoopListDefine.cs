using System;
using System.Collections.Generic;
using UnityEngine;

namespace SCFrame.UI
{
    public enum SCLoopListArrangeType
    {
        TopToBottom = 0,
        BottomToTop = 1,
        LeftToRight = 2,
        RightToLeft = 3,
    }

    [Serializable]
    public class SCLoopListItemPrefabConfig
    {
        public GameObject itemPrefab;
        public float padding = 0f;
        public int initCreateCount = 2;
        public float startPosOffset = 0f;
    }

    [Serializable]
    public class SCLoopListInitParam
    {
        public float distanceForRecycle0 = 300f;
        public float distanceForNew0 = 200f;
        public float distanceForRecycle1 = 300f;
        public float distanceForNew1 = 200f;
        public float itemDefaultWithPaddingSize = 100f;

        public static SCLoopListInitParam CopyDefaultInitParam()
        {
            return new SCLoopListInitParam();
        }
    }

    public interface ISCLoopListItemPanel
    {
        void OnRecycleFromList();
    }

    internal sealed class SCLoopListSizeCache
    {
        private readonly List<float> _itemSizes = new List<float>();
        private readonly List<float> _itemStartPositions = new List<float>();

        public float DefaultSizeWithPadding { get; set; }
        public int ItemCount => _itemSizes.Count;

        public SCLoopListSizeCache(float defaultSizeWithPadding)
        {
            DefaultSizeWithPadding = Mathf.Max(1f, defaultSizeWithPadding);
        }

        public void Reset(int itemCount)
        {
            _itemSizes.Clear();
            _itemStartPositions.Clear();

            float start = 0f;
            for (int i = 0; i < itemCount; i++)
            {
                _itemSizes.Add(DefaultSizeWithPadding);
                _itemStartPositions.Add(start);
                start += DefaultSizeWithPadding;
            }
        }

        public bool SetItemSize(int index, float sizeWithPadding)
        {
            if (index < 0 || index >= _itemSizes.Count)
                return false;

            float clamped = Mathf.Max(1f, sizeWithPadding);
            if (Mathf.Approximately(_itemSizes[index], clamped))
                return false;

            _itemSizes[index] = clamped;

            float start = index == 0 ? 0f : _itemStartPositions[index];
            for (int i = index; i < _itemSizes.Count; i++)
            {
                _itemStartPositions[i] = start;
                start += _itemSizes[i];
            }

            return true;
        }

        public float GetItemSize(int index)
        {
            if (index < 0 || index >= _itemSizes.Count)
                return DefaultSizeWithPadding;
            return _itemSizes[index];
        }

        public float GetItemStartPos(int index)
        {
            if (_itemStartPositions.Count == 0 || index <= 0)
                return 0f;
            if (index >= _itemStartPositions.Count)
                return GetTotalSize();
            return _itemStartPositions[index];
        }

        public float GetTotalSize()
        {
            if (_itemSizes.Count == 0)
                return 0f;
            int lastIndex = _itemSizes.Count - 1;
            return _itemStartPositions[lastIndex] + _itemSizes[lastIndex];
        }

        public int GetItemIndexByPosition(float position)
        {
            if (_itemSizes.Count == 0)
                return -1;

            if (position <= 0f)
                return 0;

            float total = GetTotalSize();
            if (position >= total)
                return _itemSizes.Count - 1;

            int low = 0;
            int high = _itemSizes.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                float start = _itemStartPositions[mid];
                float end = start + _itemSizes[mid];
                if (position < start)
                {
                    high = mid - 1;
                }
                else if (position >= end)
                {
                    low = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return Mathf.Clamp(low, 0, _itemSizes.Count - 1);
        }
    }
}
