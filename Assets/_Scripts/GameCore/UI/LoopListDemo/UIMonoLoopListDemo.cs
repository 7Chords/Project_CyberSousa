using System.Collections.Generic;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoLoopListDemo : _ASCUIMonoBase
    {
        private const string VerticalTemplateAName = "demo_item_vertical_a";
        private const string VerticalTemplateBName = "demo_item_vertical_b";
        private const string HorizontalTemplateName = "demo_item_horizontal_card";

        private static Sprite _whiteSprite;

        private bool _hasBuilt;
        private Font _defaultFont;

        private UIMonoLoopListContainer _verticalContainer;
        private UIMonoLoopListContainer _horizontalContainer;
        private Button _btnReload;
        private Button _btnAppend;
        private Button _btnRemove;
        private Button _btnRefreshVisible;
        private Button _btnScrollToMiddle;
        private Button _btnSwapPrefab;

        private GameObject _verticalTemplateA;
        private GameObject _verticalTemplateAAlt;
        private GameObject _verticalTemplateB;
        private GameObject _horizontalTemplate;

        public UIMonoLoopListContainer verticalContainer => _verticalContainer;
        public UIMonoLoopListContainer horizontalContainer => _horizontalContainer;
        public Button btnReload => _btnReload;
        public Button btnAppend => _btnAppend;
        public Button btnRemove => _btnRemove;
        public Button btnRefreshVisible => _btnRefreshVisible;
        public Button btnScrollToMiddle => _btnScrollToMiddle;
        public Button btnSwapPrefab => _btnSwapPrefab;

        public void EnsureBuilt()
        {
            if (_hasBuilt)
                return;

            RectTransform rootRect = transform as RectTransform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateBackground(rootRect);
            CreateHeader(rootRect);
            RectTransform controlBar = CreateControlBar(rootRect);
            _verticalContainer = CreateListSection(rootRect, "Vertical Loop List", new Vector2(0.04f, 0.53f), new Vector2(0.96f, 0.88f), SCLoopListArrangeType.TopToBottom);
            _horizontalContainer = CreateListSection(rootRect, "Horizontal Loop List", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.46f), SCLoopListArrangeType.LeftToRight);

            Transform templateRoot = CreateTemplateRoot(rootRect);
            _verticalTemplateA = CreateItemTemplate(templateRoot, VerticalTemplateAName, new Color(0.18f, 0.31f, 0.47f, 0.95f), new Color(0.40f, 0.84f, 1f, 1f), false);
            _verticalTemplateAAlt = CreateItemTemplate(templateRoot, VerticalTemplateAName, new Color(0.32f, 0.22f, 0.12f, 0.96f), new Color(1f, 0.67f, 0.30f, 1f), true);
            _verticalTemplateB = CreateItemTemplate(templateRoot, VerticalTemplateBName, new Color(0.17f, 0.23f, 0.18f, 0.96f), new Color(0.62f, 0.93f, 0.49f, 1f), true);
            _horizontalTemplate = CreateItemTemplate(templateRoot, HorizontalTemplateName, new Color(0.28f, 0.16f, 0.32f, 0.96f), new Color(0.96f, 0.56f, 0.92f, 1f), false, true);

            ConfigureListTemplates(_verticalContainer.loopListView, new List<SCLoopListItemPrefabConfig>
            {
                new SCLoopListItemPrefabConfig { itemPrefab = _verticalTemplateA, padding = 14f, initCreateCount = 4, startPosOffset = 0f },
                new SCLoopListItemPrefabConfig { itemPrefab = _verticalTemplateB, padding = 14f, initCreateCount = 4, startPosOffset = 0f },
            });

            ConfigureListTemplates(_horizontalContainer.loopListView, new List<SCLoopListItemPrefabConfig>
            {
                new SCLoopListItemPrefabConfig { itemPrefab = _horizontalTemplate, padding = 18f, initCreateCount = 4, startPosOffset = 0f },
            });

            // Keep the demo controls above the semi-transparent list sections so they remain clickable.
            controlBar.SetAsLastSibling();

            _hasBuilt = true;
        }

        public void SetVerticalATemplateSwapped(bool swapped)
        {
            if (_verticalContainer?.loopListView == null)
                return;

            SCLoopListItemPrefabConfig config = _verticalContainer.loopListView.GetItemPrefabConfig(VerticalTemplateAName);
            if (config == null)
                return;

            config.itemPrefab = swapped ? _verticalTemplateAAlt : _verticalTemplateA;
        }

        private void ConfigureListTemplates(SCLoopListView listView, List<SCLoopListItemPrefabConfig> configs)
        {
            listView.SetItemPrefabDataList(configs);
        }

        private void CreateBackground(RectTransform rootRect)
        {
            GameObject background = CreateUiObject("Background", rootRect);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = background.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = new Color(0.07f, 0.08f, 0.11f, 0.96f);
        }

        private void CreateHeader(RectTransform rootRect)
        {
            Text title = CreateText("Title", rootRect, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.04f, 0.90f);
            rect.anchorMax = new Vector2(0.70f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            title.text = "SCFrame Loop List Demo";
            title.color = new Color(0.95f, 0.97f, 1f, 1f);

            Text subtitle = CreateText("Subtitle", rootRect, 14, TextAnchor.MiddleRight, FontStyle.Normal);
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.55f, 0.90f);
            subtitleRect.anchorMax = new Vector2(0.96f, 0.98f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            subtitle.text = "Virtualized, pooled, multi-prefab";
            subtitle.color = new Color(0.72f, 0.78f, 0.88f, 1f);
        }

        private RectTransform CreateControlBar(RectTransform rootRect)
        {
            GameObject bar = CreateUiObject("Controls", rootRect);
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.79f);
            rect.anchorMax = new Vector2(0.96f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            float buttonWidth = 0.155f;
            float gap = 0.012f;
            _btnReload = CreateButton("Reload", bar.transform as RectTransform, 0f * (buttonWidth + gap), buttonWidth, new Color(0.23f, 0.49f, 0.82f, 1f));
            _btnAppend = CreateButton("Append", bar.transform as RectTransform, 1f * (buttonWidth + gap), buttonWidth, new Color(0.18f, 0.63f, 0.49f, 1f));
            _btnRemove = CreateButton("Remove", bar.transform as RectTransform, 2f * (buttonWidth + gap), buttonWidth, new Color(0.76f, 0.38f, 0.29f, 1f));
            _btnRefreshVisible = CreateButton("Refresh Visible", bar.transform as RectTransform, 3f * (buttonWidth + gap), buttonWidth, new Color(0.47f, 0.38f, 0.84f, 1f));
            _btnScrollToMiddle = CreateButton("Scroll To Middle", bar.transform as RectTransform, 4f * (buttonWidth + gap), buttonWidth, new Color(0.83f, 0.57f, 0.24f, 1f));
            _btnSwapPrefab = CreateButton("Swap Prefab", bar.transform as RectTransform, 5f * (buttonWidth + gap), buttonWidth, new Color(0.64f, 0.23f, 0.52f, 1f));

            return rect;
        }

        private UIMonoLoopListContainer CreateListSection(RectTransform rootRect, string title, Vector2 anchorMin, Vector2 anchorMax, SCLoopListArrangeType arrangeType)
        {
            GameObject section = CreateUiObject(title.Replace(" ", string.Empty), rootRect);
            RectTransform sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = anchorMin;
            sectionRect.anchorMax = anchorMax;
            sectionRect.offsetMin = Vector2.zero;
            sectionRect.offsetMax = Vector2.zero;

            Image sectionBackground = section.AddComponent<Image>();
            sectionBackground.sprite = GetWhiteSprite();
            sectionBackground.color = new Color(0.11f, 0.13f, 0.18f, 0.92f);
            sectionBackground.raycastTarget = false;

            CanvasGroup sectionCanvasGroup = section.AddComponent<CanvasGroup>();
            UIMonoLoopListContainer container = section.AddComponent<UIMonoLoopListContainer>();
            container.canvasGroup = sectionCanvasGroup;
            container.fadeInDuration = 0f;
            container.fadeOutDuration = 0f;

            Text titleText = CreateText("Title", sectionRect, 18, TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.03f, 0.84f);
            titleRect.anchorMax = new Vector2(0.62f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText.text = title;
            titleText.color = new Color(0.95f, 0.97f, 1f, 1f);

            Text statusText = CreateText("Status", sectionRect, 14, TextAnchor.MiddleRight, FontStyle.Normal);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0.62f, 0.84f);
            statusRect.anchorMax = new Vector2(0.97f, 0.98f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            statusText.text = "Count: 0";
            statusText.color = new Color(0.70f, 0.78f, 0.88f, 1f);

            GameObject scrollRoot = CreateUiObject("ScrollRect", sectionRect);
            RectTransform scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.03f, 0.06f);
            scrollRectTransform.anchorMax = new Vector2(0.97f, 0.79f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

            Image scrollBackground = scrollRoot.AddComponent<Image>();
            scrollBackground.sprite = GetWhiteSprite();
            scrollBackground.color = new Color(0.07f, 0.08f, 0.10f, 0.86f);

            ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;
            scrollRect.inertia = true;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            SCLoopListView loopListView = scrollRoot.AddComponent<SCLoopListView>();
            loopListView.ArrangeType = arrangeType;

            GameObject viewport = CreateUiObject("Viewport", scrollRectTransform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUiObject("Content", viewportRect);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            container.loopListView = loopListView;
            container.txtTitle = titleText;
            container.txtStatus = statusText;

            return container;
        }

        private Transform CreateTemplateRoot(RectTransform rootRect)
        {
            GameObject templateRoot = CreateUiObject("TemplateRoot", rootRect);
            RectTransform rect = templateRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = new Vector2(2000f, 2000f);
            templateRoot.SetActive(false);
            return templateRoot.transform;
        }

        private GameObject CreateItemTemplate(Transform parent, string itemName, Color backgroundColor, Color accentColor, bool accentWide, bool horizontal = false)
        {
            GameObject root = CreateUiObject(itemName, parent as RectTransform);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = horizontal ? new Vector2(260f, 0f) : new Vector2(0f, 104f);

            LayoutElement layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = horizontal ? 0f : 104f;
            layoutElement.preferredWidth = horizontal ? 260f : 0f;

            Image background = root.AddComponent<Image>();
            background.sprite = GetWhiteSprite();
            background.color = backgroundColor;

            SCLoopListViewItem listItem = root.AddComponent<SCLoopListViewItem>();
            UIMonoLoopDemoItem mono = root.AddComponent<UIMonoLoopDemoItem>();
            mono.layoutElement = layoutElement;
            mono.imgBackground = background;

            GameObject accent = CreateUiObject("Accent", rootRect);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = horizontal ? new Vector2(0f, 0f) : new Vector2(0f, 0f);
            accentRect.anchorMax = horizontal ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            accentRect.pivot = horizontal ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            if (horizontal)
            {
                accentRect.sizeDelta = new Vector2(accentWide ? 14f : 8f, 0f);
                accentRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                accentRect.sizeDelta = new Vector2(accentWide ? 0f : 6f, 0f);
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
            }
            Image accentImage = accent.AddComponent<Image>();
            accentImage.sprite = GetWhiteSprite();
            accentImage.color = accentColor;
            mono.imgAccent = accentImage;

            Text title = CreateText("Title", rootRect, horizontal ? 18 : 17, TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.55f);
            titleRect.anchorMax = new Vector2(1f, 0.95f);
            titleRect.offsetMin = new Vector2(horizontal ? 24f : 18f, 0f);
            titleRect.offsetMax = new Vector2(horizontal ? -18f : -18f, 0f);
            title.color = Color.white;
            mono.txtTitle = title;

            Text subtitle = CreateText("Subtitle", rootRect, 13, TextAnchor.UpperLeft, FontStyle.Normal);
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 0.08f);
            subtitleRect.anchorMax = new Vector2(1f, 0.58f);
            subtitleRect.offsetMin = new Vector2(horizontal ? 24f : 18f, 0f);
            subtitleRect.offsetMax = new Vector2(-18f, 0f);
            subtitle.color = new Color(0.84f, 0.89f, 0.96f, 1f);
            mono.txtSubtitle = subtitle;

            GameObject tagHolder = CreateUiObject("TagHolder", rootRect);
            RectTransform tagHolderRect = tagHolder.GetComponent<RectTransform>();
            tagHolderRect.anchorMin = new Vector2(1f, 1f);
            tagHolderRect.anchorMax = new Vector2(1f, 1f);
            tagHolderRect.pivot = new Vector2(1f, 1f);
            tagHolderRect.sizeDelta = new Vector2(horizontal ? 86f : 72f, 24f);
            tagHolderRect.anchoredPosition = new Vector2(-14f, -12f);
            Image tagBgImage = tagHolder.AddComponent<Image>();
            tagBgImage.sprite = GetWhiteSprite();
            tagBgImage.color = new Color(1f, 1f, 1f, 0.82f);

            Text tag = CreateText("Tag", tagHolderRect, 12, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform tagRect = tag.rectTransform;
            tagRect.anchorMin = Vector2.zero;
            tagRect.anchorMax = Vector2.one;
            tagRect.offsetMin = Vector2.zero;
            tagRect.offsetMax = Vector2.zero;
            tag.color = new Color(0.09f, 0.11f, 0.15f, 1f);
            mono.txtTag = tag;

            root.SetActive(false);
            listItem.UserObjectData = null;
            return root;
        }

        private Button CreateButton(string text, RectTransform parent, float normalizedX, float normalizedWidth, Color buttonColor)
        {
            GameObject buttonRoot = CreateUiObject(text.Replace(" ", string.Empty), parent);
            RectTransform rect = buttonRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(normalizedX, 0.18f);
            rect.anchorMax = new Vector2(normalizedX + normalizedWidth, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonRoot.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = buttonColor;

            Button button = buttonRoot.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Text buttonText = CreateText("Text", rect, 13, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform textRect = buttonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonText.text = text;
            buttonText.color = Color.white;

            return button;
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, TextAnchor alignment, FontStyle fontStyle)
        {
            GameObject textObject = CreateUiObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = _defaultFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateUiObject(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
            return go;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
            _whiteSprite.name = "SCFrameLoopListWhiteSprite";
            return _whiteSprite;
        }
    }
}
