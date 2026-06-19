using System.Collections.Generic;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoGameplayMain : _ASCUIMonoBase
    {
        private static Sprite _whiteSprite;

        private readonly List<Button> _numberButtons = new List<Button>();
        private bool _hasBuilt;
        private Font _defaultFont;

        private Text _txtTime;
        private Text _txtAnimalInfo;
        private Text _txtBottomHint;
        private Button _btnOption1;
        private Button _btnOption2;
        private Button _btnReject;
        private Button _btnConfirm;
        private Button _btnCloseDoor;

        public IReadOnlyList<Button> numberButtons => _numberButtons;
        public Button btnOption1 => _btnOption1;
        public Button btnOption2 => _btnOption2;
        public Button btnReject => _btnReject;
        public Button btnConfirm => _btnConfirm;
        public Button btnCloseDoor => _btnCloseDoor;
        public Text txtTime => _txtTime;
        public Text txtAnimalInfo => _txtAnimalInfo;
        public Text txtBottomHint => _txtBottomHint;

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
            CreateStageFrame(rootRect);
            CreateLeftColumn(rootRect);
            CreateCenterColumn(rootRect);
            CreateRightColumn(rootRect);
            CreateBottomHint(rootRect);

            _hasBuilt = true;
        }

        private void CreateBackground(RectTransform rootRect)
        {
            Image background = CreatePanel("Background", rootRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.94f, 0.94f, 0.92f, 0.98f));
            background.raycastTarget = false;
        }

        private void CreateStageFrame(RectTransform rootRect)
        {
            CreatePanel("LeftBand", rootRect, new Vector2(0.00f, 0.00f), new Vector2(0.18f, 1.00f), new Color(0.91f, 0.91f, 0.92f, 0.75f));
            CreatePanel("CenterBand", rootRect, new Vector2(0.18f, 0.00f), new Vector2(0.78f, 1.00f), new Color(1f, 1f, 1f, 0.30f));
            CreatePanel("RightBand", rootRect, new Vector2(0.78f, 0.00f), new Vector2(1.00f, 1.00f), new Color(0.95f, 0.95f, 0.96f, 0.78f));
        }

        private void CreateLeftColumn(RectTransform rootRect)
        {
            CreateCardWithTitle("NoticeBoard01", rootRect, new Vector2(0.02f, 0.64f), new Vector2(0.11f, 0.88f), "告示1", 28, FontStyle.Bold);
            CreateCardWithTitle("NoticeBoard02", rootRect, new Vector2(0.06f, 0.32f), new Vector2(0.15f, 0.55f), "告示2", 28, FontStyle.Bold);

            CreateDialogueStrip("Dialogue01", rootRect, new Vector2(0.15f, 0.74f), new Vector2(0.33f, 0.82f), "对话1");
        }

        private void CreateCenterColumn(RectTransform rootRect)
        {
            Image animalPanel = CreatePanel("AnimalView", rootRect, new Vector2(0.36f, 0.14f), new Vector2(0.60f, 0.84f), new Color(0.80f, 0.80f, 0.80f, 0.92f));
            CreateText("AnimalLabel", animalPanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), "动物", 40, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.25f, 0.25f, 0.25f, 1f));

            Image standShadow = CreatePanel("AnimalStandShadow", rootRect, new Vector2(0.34f, 0.10f), new Vector2(0.62f, 0.12f), new Color(0.70f, 0.70f, 0.70f, 0.55f));
            standShadow.raycastTarget = false;

            CreateDialogueStrip("Dialogue02", rootRect, new Vector2(0.62f, 0.63f), new Vector2(0.83f, 0.71f), "对话2");
        }

        private void CreateRightColumn(RectTransform rootRect)
        {
            _txtTime = CreateTextPanel("TimePanel", rootRect, new Vector2(0.81f, 0.87f), new Vector2(0.97f, 0.95f), "10 : 29（时间）", 28, TextAnchor.MiddleCenter, FontStyle.Bold);
            _txtAnimalInfo = CreateTextPanel("AnimalInfoPanel", rootRect, new Vector2(0.81f, 0.68f), new Vector2(0.97f, 0.80f), "动物扫脸信息", 30, TextAnchor.MiddleCenter, FontStyle.Bold);

            CreateNumberButtonGrid(rootRect);

            _btnOption1 = CreateTextButton("Option01", rootRect, new Vector2(0.63f, 0.25f), new Vector2(0.79f, 0.31f), "选项1", 18, new Color(0.88f, 0.88f, 0.88f, 0.98f));
            _btnOption2 = CreateTextButton("Option02", rootRect, new Vector2(0.63f, 0.18f), new Vector2(0.79f, 0.24f), "选项2", 18, new Color(0.88f, 0.88f, 0.88f, 0.98f));

            _btnReject = CreateTextButton("ActionReject", rootRect, new Vector2(0.80f, 0.17f), new Vector2(0.86f, 0.25f), "拒绝", 18, new Color(0.86f, 0.86f, 0.86f, 1f));
            _btnConfirm = CreateTextButton("ActionConfirm", rootRect, new Vector2(0.87f, 0.17f), new Vector2(0.93f, 0.25f), "确认", 18, new Color(0.86f, 0.86f, 0.86f, 1f));
            _btnCloseDoor = CreateTextButton("ActionCloseDoor", rootRect, new Vector2(0.94f, 0.17f), new Vector2(0.99f, 0.25f), "关门", 18, new Color(0.86f, 0.86f, 0.86f, 1f));
        }

        private void CreateBottomHint(RectTransform rootRect)
        {
            Image hintPanel = CreatePanel("BottomHintPanel", rootRect, new Vector2(0.27f, 0.02f), new Vector2(0.70f, 0.10f), new Color(0.82f, 0.82f, 0.82f, 0.95f));
            _txtBottomHint = CreateText(
                "BottomHintText",
                hintPanel.rectTransform,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.92f),
                "动物信息档案，点击弹出（用于确认动物的档案，\n如果与所住房屋不符合，需要拒绝或询问原因）",
                16,
                TextAnchor.MiddleCenter,
                FontStyle.Normal,
                new Color(0.20f, 0.20f, 0.20f, 1f));
        }

        private void CreateNumberButtonGrid(RectTransform rootRect)
        {
            int[] numbers = { 10, 11, 12, 7, 8, 9, 4, 5, 6, 1, 2, 3 };
            float startX = 0.83f;
            float startY = 0.55f;
            float cellWidth = 0.055f;
            float cellHeight = 0.09f;

            for (int index = 0; index < numbers.Length; index++)
            {
                int row = index / 3;
                int column = index % 3;
                Vector2 anchorMin = new Vector2(startX + column * cellWidth, startY - row * cellHeight);
                Vector2 anchorMax = anchorMin + new Vector2(0.042f, 0.07f);
                Button button = CreateCircleButton($"AnimalChoice{numbers[index]}", rootRect, anchorMin, anchorMax, numbers[index].ToString());
                _numberButtons.Add(button);
            }
        }

        private Image CreateCardWithTitle(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, FontStyle fontStyle)
        {
            Image card = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.84f, 0.84f, 0.84f, 0.95f));
            CreateText($"{name}_Text", card.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), text, fontSize, TextAnchor.MiddleCenter, fontStyle, new Color(0.18f, 0.18f, 0.18f, 1f));
            return card;
        }

        private Image CreateDialogueStrip(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string text)
        {
            Image strip = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.86f, 0.86f, 0.86f, 0.95f));
            CreateText($"{name}_Text", strip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), text, 18, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.18f, 0.18f, 0.18f, 1f));
            return strip;
        }

        private Text CreateTextPanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, TextAnchor anchor, FontStyle fontStyle)
        {
            Image panel = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.86f, 0.86f, 0.86f, 0.98f));
            return CreateText($"{name}_Text", panel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), text, fontSize, anchor, fontStyle, new Color(0.18f, 0.18f, 0.18f, 1f));
        }

        private Button CreateCircleButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.89f, 0.89f, 0.89f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.73f, 0.73f, 0.73f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateText($"{name}_Text", rect, new Vector2(0f, 0f), new Vector2(1f, 1f), label, 18, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.20f, 0.20f, 0.20f, 1f));
            return button;
        }

        private Button CreateTextButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label, int fontSize, Color backgroundColor)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = backgroundColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText($"{name}_Text", rect, new Vector2(0f, 0f), new Vector2(1f, 1f), label, fontSize, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.16f, 0.16f, 0.16f, 1f));
            return button;
        }

        private Image CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panelObject = CreateUiObject(name, parent);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string content, int fontSize, TextAnchor anchor, FontStyle fontStyle, Color color)
        {
            GameObject textObject = CreateUiObject(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.font = _defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = color;
            text.text = content;
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
            _whiteSprite.name = "GameplayMainWhiteSprite";
            return _whiteSprite;
        }
    }
}
