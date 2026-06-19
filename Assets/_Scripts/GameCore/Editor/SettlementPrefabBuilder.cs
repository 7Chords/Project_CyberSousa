using GameCore.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static class SettlementPrefabBuilder
    {
        private const string PrefabAssetPath = "Assets/GameRes/UI/panel_settlement.prefab";

        private static Font _defaultFont;
        private static Sprite _whiteSprite;

        [MenuItem("GameCore/UI/生成结算界面Prefab")]
        public static void GenerateSettlementPrefab()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameRes/UI/Generated/gameplay_main_white.png");
            if (_whiteSprite == null)
            {
                Debug.LogError("生成结算界面 Prefab 失败：请先生成游戏内主界面 Prefab，确保纯白 Sprite 已存在。");
                return;
            }

            GameObject root = new GameObject("panel_settlement", typeof(RectTransform), typeof(CanvasGroup), typeof(UIMonoSettlement));
            try
            {
                BuildPrefab(root);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
                EnsureAddressableEntry();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                Debug.Log("结算界面 Prefab 生成完成。");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildPrefab(GameObject root)
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            UIMonoSettlement mono = root.GetComponent<UIMonoSettlement>();
            mono.canvasGroup = root.GetComponent<CanvasGroup>();
            mono.fadeInDuration = 0.2f;
            mono.fadeOutDuration = 0.2f;

            Image mask = CreatePanel("Mask", rootRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0.45f));
            mask.raycastTarget = true;

            Image panel = CreatePanel("SettlementPanel", rootRect, new Vector2(0.31f, 0.24f), new Vector2(0.69f, 0.72f), new Color(0.94f, 0.94f, 0.92f, 1f));
            mono.txtTitle = CreateText("TitleText", panel.rectTransform, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.90f), "当日结算", 30, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.16f, 0.16f, 0.16f, 1f));
            mono.txtSummary = CreateText("SummaryText", panel.rectTransform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.68f), "当天客人已全部接待完毕。", 22, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.22f, 0.22f, 0.22f, 1f));
            mono.btnNextDay = CreateButton("NextDayButton", panel.rectTransform, new Vector2(0.30f, 0.10f), new Vector2(0.70f, 0.24f), "进入下一天");
        }

        private static Image CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.GetComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string content, int fontSize, TextAnchor anchor, FontStyle fontStyle, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
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

        private static Button CreateButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = new Color(0.84f, 0.84f, 0.84f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            CreateText($"{name}_Text", rect, new Vector2(0f, 0f), new Vector2(1f, 1f), label, 22, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.16f, 0.16f, 0.16f, 1f));
            return button;
        }

        private static void EnsureAddressableEntry()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("生成结算界面 Prefab 后注册 Addressables 失败：未找到 AddressableAssetSettings。");
                return;
            }

            AddressableAssetGroup uiGroup = settings.FindGroup("UI");
            if (uiGroup == null)
            {
                Debug.LogError("生成结算界面 Prefab 后注册 Addressables 失败：未找到 UI 资源组。");
                return;
            }

            string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabAssetPath);
            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogError($"生成结算界面 Prefab 后注册 Addressables 失败：未找到资源 GUID，path={PrefabAssetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(prefabGuid, uiGroup);
            if (entry == null)
            {
                Debug.LogError($"生成结算界面 Prefab 后注册 Addressables 失败：无法创建或移动条目，guid={prefabGuid}");
                return;
            }

            entry.address = "panel_settlement";
            EditorUtility.SetDirty(uiGroup);
            EditorUtility.SetDirty(settings);
        }
    }
}
