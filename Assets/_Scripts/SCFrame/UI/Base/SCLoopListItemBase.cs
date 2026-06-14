using UnityEngine;

namespace SCFrame.UI
{
    public abstract class UIMonoLoopItemBase : MonoBehaviour
    {
        private RectTransform _cachedRectTransform;

        public RectTransform cachedRectTransform
        {
            get
            {
                if (_cachedRectTransform == null)
                    _cachedRectTransform = transform as RectTransform;
                return _cachedRectTransform;
            }
        }
    }

    public abstract class UIPanelLoopItemBase<TMono> : ISCLoopListItemPanel where TMono : UIMonoLoopItemBase
    {
        private bool _hasInitialized;

        protected TMono mono;
        protected SCLoopListViewItem viewItem;

        public bool hasInitialized => _hasInitialized;

        public void InitializeIfNeeded(TMono targetMono)
        {
            if (_hasInitialized)
                return;

            mono = targetMono;
            _hasInitialized = true;
            OnInitialize();
        }

        public void Bind(SCLoopListViewItem item)
        {
            viewItem = item;
            if (mono == null)
                mono = item.GetCachedComponent<TMono>();
            OnBind(item);
        }

        public void OnRecycleFromList()
        {
            OnRecycle();
            viewItem = null;
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnBind(SCLoopListViewItem item)
        {
        }

        protected virtual void OnRecycle()
        {
        }
    }
}
