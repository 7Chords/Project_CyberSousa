using System.Collections.Generic;
using UnityEngine;

namespace SCFrame.UI
{
    public class SCLoopListViewItem : MonoBehaviour
    {
        private readonly Dictionary<System.Type, Component> _componentCache = new Dictionary<System.Type, Component>();
        private RectTransform _cachedRectTransform;

        public int ItemIndex { get; set; } = -1;
        public int ItemId { get; set; } = -1;
        public string ItemPrefabName { get; set; }
        public object UserObjectData { get; set; }
        public SCLoopListView ParentListView { get; set; }
        public float Padding { get; set; }
        public float StartPosOffset { get; set; }

        public RectTransform CachedRectTransform
        {
            get
            {
                if (_cachedRectTransform == null)
                    _cachedRectTransform = transform as RectTransform;
                return _cachedRectTransform;
            }
        }

        public T GetCachedComponent<T>() where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out Component component))
                return component as T;

            T target = GetComponent<T>();
            _componentCache[type] = target;
            return target;
        }

        internal void ResetRuntimeState(SCLoopListView parentListView, int itemIndex, int itemId, string prefabName, float padding, float startPosOffset)
        {
            ParentListView = parentListView;
            ItemIndex = itemIndex;
            ItemId = itemId;
            ItemPrefabName = prefabName;
            Padding = padding;
            StartPosOffset = startPosOffset;
        }
    }
}
