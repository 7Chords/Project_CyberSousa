using GameCore.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static class GameplayGuideMaskPrefabBuilder
    {
        private const string PrefabAssetPath = "Assets/GameRes/UI/panel_gameplay_guide_mask.prefab";
        private const string PrefabAddress = "panel_gameplay_guide_mask";
        private const string WhiteSpriteAssetPath = "Assets/GameRes/UI/Generated/gameplay_main_white.png";

        private static Sprite _whiteSprite;
        private static Font _defaultFont;

        [MenuItem("GameCore/UI/生成游戏引导遮罩Prefab")]
        public static void GenerateGameplayGuideMaskPrefab()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpriteAssetPath);
            if (_whiteSprite == null)
            {
                Debug.LogError("生成游戏引导遮罩 Prefab 失败：未找到纯白 Sprite，请先生成游戏内主界面 Prefab。");
                return;
            }

            GameObject root = new GameObject(PrefabAddress, typeof(RectTransform), typeof(UIMonoGameplayGuideMask));
            try
            {
                BuildPrefab(root);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
                EnsureAddressableEntry();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                Debug.Log("游戏引导遮罩 Prefab 生成完成。");
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
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            UIMonoGameplayGuideMask mono = root.GetComponent<UIMonoGameplayGuideMask>();
            mono.rootRect = rootRect;
            mono.maskRects.Clear();

            for (int index = 0; index < 4; index++)
            {
                Image mask = CreateImage($"Mask{index + 1:00}", rootRect, new Color(0f, 0f, 0f, 0.68f), true);
                mono.maskRects.Add(mask.rectTransform);
            }

            Image highlight = CreateImage("Highlight", rootRect, new Color(1f, 0.93f, 0.50f, 0.18f), false);
            Outline outline = highlight.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.86f, 0.22f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            mono.highlightRect = highlight.rectTransform;

            Image continueClick = CreateImage("ContinueClickArea", rootRect, new Color(1f, 1f, 1f, 0f), true);
            Button continueButton = continueClick.gameObject.AddComponent<Button>();
            continueButton.transition = Selectable.Transition.None;
            mono.continueButton = continueButton;
            mono.continueClickRect = continueClick.rectTransform;

            Image tipBg = CreateImage("TipBg", rootRect, new Color(0.08f, 0.08f, 0.08f, 0.92f), false);
            RectTransform tipBgRect = tipBg.rectTransform;
            tipBgRect.anchorMin = new Vector2(0.5f, 0.08f);
            tipBgRect.anchorMax = new Vector2(0.5f, 0.08f);
            tipBgRect.pivot = new Vector2(0.5f, 0f);
            tipBgRect.sizeDelta = new Vector2(560f, 116f);
            tipBgRect.anchoredPosition = Vector2.zero;

            mono.tipText = CreateText("TipText", tipBgRect, "先看客人的资料。");
        }

        private static Image CreateImage(string name, RectTransform parent, Color color, bool raycastTarget)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Text CreateText(string name, RectTransform parent, string content)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 14f);
            rect.offsetMax = new Vector2(-24f, -14f);

            Text text = textObject.GetComponent<Text>();
            text.font = _defaultFont;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private static void EnsureAddressableEntry()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("生成游戏引导遮罩 Prefab 后注册 Addressables 失败：未找到 AddressableAssetSettings。");
                return;
            }

            AddressableAssetGroup uiGroup = settings.FindGroup("UI");
            if (uiGroup == null)
            {
                Debug.LogError("生成游戏引导遮罩 Prefab 后注册 Addressables 失败：未找到 UI 资源组。");
                return;
            }

            string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabAssetPath);
            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogError($"生成游戏引导遮罩 Prefab 后注册 Addressables 失败：未找到资源 GUID，path={PrefabAssetPath}");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(prefabGuid, uiGroup);
            if (entry == null)
            {
                Debug.LogError($"生成游戏引导遮罩 Prefab 后注册 Addressables 失败：无法创建或移动条目，guid={prefabGuid}");
                return;
            }

            entry.address = PrefabAddress;
            EditorUtility.SetDirty(uiGroup);
            EditorUtility.SetDirty(settings);
        }
    }
}
