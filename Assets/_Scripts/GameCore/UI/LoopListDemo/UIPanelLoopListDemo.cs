using System.Collections.Generic;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    public class UIPanelLoopListDemo : _ASCUIPanelBase<UIMonoLoopListDemo>
    {
        private readonly List<DemoEntry> _verticalEntries = new List<DemoEntry>();
        private readonly List<DemoEntry> _horizontalEntries = new List<DemoEntry>();

        private LoopListDemoSectionPanel _verticalSectionPanel;
        private LoopListDemoSectionPanel _horizontalSectionPanel;
        private bool _isVerticalTemplateSwapped;
        private int _refreshRevision;

        public UIPanelLoopListDemo(UIMonoLoopListDemo _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void BeforeInitialize()
        {
            base.BeforeInitialize();
        }

        public override void AfterInitialize()
        {
            mono.EnsureBuilt();

            BuildDemoData();
            _verticalSectionPanel = new LoopListDemoSectionPanel(_verticalEntries, "Vertical Loop List", CreateVerticalInitParam());
            _horizontalSectionPanel = new LoopListDemoSectionPanel(_horizontalEntries, "Horizontal Loop List", CreateHorizontalInitParam());
            _verticalSectionPanel.BindContainer(mono.verticalContainer);
            _horizontalSectionPanel.BindContainer(mono.horizontalContainer);
            _verticalSectionPanel.ReloadData(true);
            _horizontalSectionPanel.ReloadData(true);
        }

        public override void BeforeDiscard()
        {
            UnbindButtons();
            _verticalSectionPanel?.Dispose();
            _horizontalSectionPanel?.Dispose();
            _verticalSectionPanel = null;
            _horizontalSectionPanel = null;
        }

        public override void AfterDiscard()
        {
        }

        public override void OnHidePanel()
        {
            UnbindButtons();
        }

        public override void OnShowPanel()
        {
            BindButtons();
            _verticalSectionPanel?.RefreshVisibleItems();
            _horizontalSectionPanel?.RefreshVisibleItems();
        }

        private void BindButtons()
        {
            mono.btnReload?.AddMouseLeftClickDown(OnReloadClicked);
            mono.btnAppend?.AddMouseLeftClickDown(OnAppendClicked);
            mono.btnRemove?.AddMouseLeftClickDown(OnRemoveClicked);
            mono.btnRefreshVisible?.AddMouseLeftClickDown(OnRefreshVisibleClicked);
            mono.btnScrollToMiddle?.AddMouseLeftClickDown(OnScrollToMiddleClicked);
            mono.btnSwapPrefab?.AddMouseLeftClickDown(OnSwapPrefabClicked);
        }

        private void UnbindButtons()
        {
            mono.btnReload?.RemoveMouseLeftClickDown(OnReloadClicked);
            mono.btnAppend?.RemoveMouseLeftClickDown(OnAppendClicked);
            mono.btnRemove?.RemoveMouseLeftClickDown(OnRemoveClicked);
            mono.btnRefreshVisible?.RemoveMouseLeftClickDown(OnRefreshVisibleClicked);
            mono.btnScrollToMiddle?.RemoveMouseLeftClickDown(OnScrollToMiddleClicked);
            mono.btnSwapPrefab?.RemoveMouseLeftClickDown(OnSwapPrefabClicked);
        }

        private void OnReloadClicked(PointerEventData _data, object[] _objs)
        {
            BuildDemoData();
            _verticalSectionPanel?.ReloadData(true);
            _horizontalSectionPanel?.ReloadData(true);
        }

        private void OnAppendClicked(PointerEventData _data, object[] _objs)
        {
            _verticalEntries.Add(CreateVerticalEntry(_verticalEntries.Count));
            _horizontalEntries.Add(CreateHorizontalEntry(_horizontalEntries.Count));
            _verticalSectionPanel?.ReloadData(false);
            _horizontalSectionPanel?.ReloadData(false);
        }

        private void OnRemoveClicked(PointerEventData _data, object[] _objs)
        {
            if (_verticalEntries.Count > 0)
                _verticalEntries.RemoveAt(_verticalEntries.Count - 1);
            if (_horizontalEntries.Count > 0)
                _horizontalEntries.RemoveAt(_horizontalEntries.Count - 1);

            _verticalSectionPanel?.ReloadData(false);
            _horizontalSectionPanel?.ReloadData(false);
        }

        private void OnRefreshVisibleClicked(PointerEventData _data, object[] _objs)
        {
            _refreshRevision++;

            for (int i = 0; i < _verticalEntries.Count; i++)
            {
                _verticalEntries[i].subtitle = $"Visible refresh revision {_refreshRevision} / item {i}";
            }

            for (int i = 0; i < _horizontalEntries.Count; i++)
            {
                _horizontalEntries[i].subtitle = $"Card refresh {_refreshRevision} / item {i}";
            }

            _verticalSectionPanel?.RefreshVisibleItems();
            _horizontalSectionPanel?.RefreshVisibleItems();
        }

        private void OnScrollToMiddleClicked(PointerEventData _data, object[] _objs)
        {
            if (_verticalEntries.Count > 0)
                _verticalSectionPanel?.ScrollToIndex(_verticalEntries.Count / 2);
            if (_horizontalEntries.Count > 0)
                _horizontalSectionPanel?.ScrollToIndex(_horizontalEntries.Count / 2);
        }

        private void OnSwapPrefabClicked(PointerEventData _data, object[] _objs)
        {
            _isVerticalTemplateSwapped = !_isVerticalTemplateSwapped;
            mono.SetVerticalATemplateSwapped(_isVerticalTemplateSwapped);
            _verticalSectionPanel?.ReplacePrefabAndRefresh("demo_item_vertical_a");
        }

        private void BuildDemoData()
        {
            _verticalEntries.Clear();
            _horizontalEntries.Clear();

            for (int i = 0; i < 28; i++)
            {
                _verticalEntries.Add(CreateVerticalEntry(i));
            }

            for (int i = 0; i < 18; i++)
            {
                _horizontalEntries.Add(CreateHorizontalEntry(i));
            }
        }

        private DemoEntry CreateVerticalEntry(int index)
        {
            bool useA = index % 3 != 1;
            return new DemoEntry
            {
                prefabName = useA ? "demo_item_vertical_a" : "demo_item_vertical_b",
                title = useA ? "Vertical A" : "Vertical B",
                subtitle = $"Entry {index} with pooled mixed-prefab content.",
                tag = useA ? "A" : "B",
                backgroundColor = useA ? new Color(0.18f, 0.30f, 0.47f, 0.95f) : new Color(0.18f, 0.24f, 0.19f, 0.96f),
                accentColor = useA ? new Color(0.41f, 0.84f, 1f, 1f) : new Color(0.61f, 0.92f, 0.49f, 1f),
                primarySize = useA ? 96f + (index % 4) * 14f : 112f + (index % 3) * 18f,
            };
        }

        private DemoEntry CreateHorizontalEntry(int index)
        {
            return new DemoEntry
            {
                prefabName = "demo_item_horizontal_card",
                title = $"Card {index}",
                subtitle = $"Horizontal card item {index} using the same pooled controller.",
                tag = $"#{index:00}",
                backgroundColor = new Color(0.28f + (index % 4) * 0.04f, 0.16f, 0.32f, 0.96f),
                accentColor = new Color(0.92f, 0.52f + (index % 3) * 0.10f, 0.90f, 1f),
                primarySize = 180f + (index % 5) * 26f,
            };
        }

        private static SCLoopListInitParam CreateVerticalInitParam()
        {
            return new SCLoopListInitParam
            {
                distanceForRecycle0 = 360f,
                distanceForRecycle1 = 360f,
                distanceForNew0 = 220f,
                distanceForNew1 = 220f,
                itemDefaultWithPaddingSize = 130f,
            };
        }

        private static SCLoopListInitParam CreateHorizontalInitParam()
        {
            return new SCLoopListInitParam
            {
                distanceForRecycle0 = 420f,
                distanceForRecycle1 = 420f,
                distanceForNew0 = 260f,
                distanceForNew1 = 260f,
                itemDefaultWithPaddingSize = 220f,
            };
        }

        private sealed class DemoEntry
        {
            public string prefabName;
            public string title;
            public string subtitle;
            public string tag;
            public Color backgroundColor;
            public Color accentColor;
            public float primarySize;
        }

        private sealed class UIPanelLoopDemoItem : UIPanelLoopItemBase<UIMonoLoopDemoItem>
        {
            public void SetData(DemoEntry data, int index)
            {
                mono.imgBackground.color = data.backgroundColor;
                mono.imgAccent.color = data.accentColor;
                mono.txtTitle.text = $"{index:00}  {data.title}";
                mono.txtSubtitle.text = data.subtitle;
                mono.txtTag.text = data.tag;

                if (viewItem != null && viewItem.ParentListView != null && viewItem.ParentListView.IsVertList)
                {
                    mono.layoutElement.preferredHeight = data.primarySize;
                    mono.layoutElement.preferredWidth = -1f;
                }
                else
                {
                    mono.layoutElement.preferredWidth = data.primarySize;
                    mono.layoutElement.preferredHeight = -1f;
                }
            }

            protected override void OnRecycle()
            {
                if (mono == null)
                    return;

                mono.txtTitle.text = string.Empty;
                mono.txtSubtitle.text = string.Empty;
                mono.txtTag.text = string.Empty;
            }
        }

        private sealed class LoopListDemoSectionPanel : UIPanelLoopListBase<UIMonoLoopListContainer, UIPanelLoopDemoItem, UIMonoLoopDemoItem>
        {
            private readonly List<DemoEntry> _entries;
            private readonly string _title;
            private readonly SCLoopListInitParam _initParam;

            public LoopListDemoSectionPanel(List<DemoEntry> entries, string title, SCLoopListInitParam initParam)
            {
                _entries = entries;
                _title = title;
                _initParam = initParam;
            }

            protected override void OnBindContainer()
            {
                if (mono.txtTitle != null)
                    mono.txtTitle.text = _title;
            }

            protected override SCLoopListInitParam CreateInitParam()
            {
                return _initParam;
            }

            protected override int GetItemCount()
            {
                return _entries.Count;
            }

            protected override string GetItemPrefabName(int index)
            {
                return _entries[index].prefabName;
            }

            protected override void BindItem(UIPanelLoopDemoItem panel, int index, SCLoopListViewItem item)
            {
                panel.SetData(_entries[index], index);
            }

            protected override string GetStatusText()
            {
                return $"Count: {_entries.Count}";
            }
        }
    }
}
