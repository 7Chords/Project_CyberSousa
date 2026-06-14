using System.Collections.Generic;
using SCFrame.UI;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoLoopListContainer : UIMonoCommonContainer
    {
        public SCLoopListView loopListView;
        public Text txtTitle;
        public Text txtStatus;
    }

    public abstract class UIPanelLoopListBase<TContainerMono, TItemPanel, TItemMono>
        where TContainerMono : UIMonoLoopListContainer
        where TItemPanel : UIPanelLoopItemBase<TItemMono>, new()
        where TItemMono : UIMonoLoopItemBase
    {
        protected TContainerMono mono;

        private bool _hasBoundContainer;

        public void BindContainer(TContainerMono targetMono)
        {
            mono = targetMono;
            _hasBoundContainer = true;

            if (mono.loopListView != null && !mono.loopListView.IsListViewInited)
            {
                mono.loopListView.InitListView(GetItemCount(), OnGetItemByIndex, CreateInitParam());
            }

            OnBindContainer();
            UpdateStatusText();
        }

        public void Dispose()
        {
            if (mono?.loopListView != null)
                mono.loopListView.Clear();

            _hasBoundContainer = false;
            mono = null;
            OnDispose();
        }

        public void ReloadData(bool resetPos = true)
        {
            if (!_hasBoundContainer || mono.loopListView == null)
                return;

            if (!mono.loopListView.IsListViewInited)
            {
                mono.loopListView.InitListView(GetItemCount(), OnGetItemByIndex, CreateInitParam());
            }
            else
            {
                mono.loopListView.SetListItemCount(GetItemCount(), resetPos);
                mono.loopListView.RefreshAllShownItem();
            }

            UpdateStatusText();
        }

        public void RefreshVisibleItems()
        {
            mono?.loopListView?.RefreshAllShownItem();
            UpdateStatusText();
        }

        public void RefreshItem(int index)
        {
            mono?.loopListView?.RefreshItemByItemIndex(index);
            UpdateStatusText();
        }

        public void ScrollToIndex(int index, float offset = 0f)
        {
            mono?.loopListView?.MovePanelToItemIndex(index, offset);
        }

        public void NotifyItemSizeChanged(int index)
        {
            mono?.loopListView?.OnItemSizeChanged(index);
        }

        public void ReplacePrefabAndRefresh(string prefabName)
        {
            mono?.loopListView?.OnItemPrefabChanged(prefabName);
        }

        public SCLoopListViewItem GetShownItemByItemIndex(int index)
        {
            if (mono?.loopListView == null)
                return null;
            return mono.loopListView.GetShownItemByItemIndex(index);
        }

        protected virtual void OnBindContainer()
        {
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual SCLoopListInitParam CreateInitParam()
        {
            return null;
        }

        protected abstract int GetItemCount();

        protected abstract string GetItemPrefabName(int index);

        protected abstract void BindItem(TItemPanel panel, int index, SCLoopListViewItem item);

        protected virtual string GetStatusText()
        {
            return $"Count: {GetItemCount()}";
        }

        private void UpdateStatusText()
        {
            if (mono?.txtStatus == null)
                return;
            mono.txtStatus.text = GetStatusText();
        }

        private SCLoopListViewItem OnGetItemByIndex(SCLoopListView listView, int index)
        {
            if (index < 0 || index >= GetItemCount())
                return null;

            string prefabName = GetItemPrefabName(index);
            if (string.IsNullOrEmpty(prefabName))
                return null;

            SCLoopListViewItem item = listView.NewListViewItem(prefabName);
            if (item == null)
                return null;

            TItemMono itemMono = item.GetCachedComponent<TItemMono>();
            if (itemMono == null)
                return null;

            TItemPanel itemPanel = item.UserObjectData as TItemPanel;
            if (itemPanel == null)
            {
                itemPanel = new TItemPanel();
                itemPanel.InitializeIfNeeded(itemMono);
                item.UserObjectData = itemPanel;
            }

            itemPanel.Bind(item);
            BindItem(itemPanel, index, item);
            return item;
        }

        protected List<SCLoopListViewItem> GetShownItemsSnapshot()
        {
            List<SCLoopListViewItem> result = new List<SCLoopListViewItem>();
            if (mono?.loopListView == null)
                return result;

            for (int i = 0; i < GetItemCount(); i++)
            {
                SCLoopListViewItem item = mono.loopListView.GetShownItemByItemIndex(i);
                if (item != null)
                    result.Add(item);
            }
            return result;
        }
    }
}
