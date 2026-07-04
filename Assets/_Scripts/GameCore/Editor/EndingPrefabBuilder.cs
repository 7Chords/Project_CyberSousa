using System.IO;
using GameCore.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static class EndingPrefabBuilder
    {
        private const string PrefabAssetPath = "Assets/GameRes/UI/panel_ending.prefab";
        private const string WhiteSpriteAssetPath = "Assets/GameRes/UI/Generated/gameplay_main_white.png";
        private const string BadEndingSpritePath = "Assets/GameRes/EndImage/RetirementEnd.png";
        private const string Ending1SpritePath = "Assets/GameRes/EndImage/ContinueWorkEnd.png";
        private const string Ending2SpritePath = "Assets/GameRes/EndImage/MeiAnQueRenJianEnd.png";

        private static Font _defaultFont;
        private static Sprite _whiteSprite;

        [MenuItem("GameCore/UI/生成结局界面Prefab")]
        public static void GenerateEndingPrefab()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _whiteSprite = EnsureWhiteSpriteAsset();
            if (_whiteSprite == null)
            {
                Debug.LogError("生成结局界面 Prefab 失败：纯白 Sprite 资源未准备成功。");
                return;
            }

            GameObject root = new GameObject("panel_ending", typeof(RectTransform), typeof(CanvasGroup), typeof(UIMonoEnding));
            try
            {
                BuildPrefab(root);
                SavePrefab(root);
                Debug.Log("结局界面 Prefab 生成完成。");
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

            UIMonoEnding mono = root.GetComponent<UIMonoEnding>();
            mono.canvasGroup = root.GetComponent<CanvasGroup>();
            mono.fadeInDuration = 0.2f;
            mono.fadeOutDuration = 0.2f;

            Image background = CreatePanel("Background", rootRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.94f, 0.95f, 0.98f, 1f));
            background.raycastTarget = true;

            Image outerPanel = CreatePanel("OuterPanel", rootRect, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.97f), new Color(0.92f, 0.94f, 0.98f, 0.98f));
            AddOutline(outerPanel.gameObject, new Color(0.11f, 0.11f, 0.11f, 1f), new Vector2(3f, -3f));

            Image innerPanel = CreatePanel("InnerPanel", outerPanel.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.97f), new Color(0.89f, 0.92f, 0.97f, 1f));
            AddOutline(innerPanel.gameObject, new Color(0.11f, 0.11f, 0.11f, 1f), new Vector2(3f, -3f));

            Image imageFrame = CreatePanel("EndingImageFrame", innerPanel.rectTransform, new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.94f), new Color(0.86f, 0.90f, 0.97f, 1f));
            AddOutline(imageFrame.gameObject, new Color(0.11f, 0.11f, 0.11f, 1f), new Vector2(3f, -3f));

            Image endingImage = CreatePanel("EndingImage", imageFrame.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Color.white);
            endingImage.preserveAspect = true;
            endingImage.raycastTarget = false;

            mono.imgEnding = endingImage;
            mono.sprBadEnding = LoadRequiredSprite(BadEndingSpritePath);
            mono.sprEnding1 = LoadRequiredSprite(Ending1SpritePath);
            mono.sprEnding2 = LoadRequiredSprite(Ending2SpritePath);

            mono.txtSummary = CreateText(
                "EndingSummaryText",
                innerPanel.rectTransform,
                new Vector2(0.12f, 0.13f),
                new Vector2(0.88f, 0.24f),
                "结局段落",
                26,
                TextAnchor.MiddleLeft,
                FontStyle.Normal,
                new Color(0.14f, 0.14f, 0.14f, 1f));

            mono.btnReturnMain = CreateButton(
                "ReturnMainButton",
                innerPanel.rectTransform,
                new Vector2(0.40f, 0.03f),
                new Vector2(0.60f, 0.09f),
                "返回主界面");
        }

        private static Sprite LoadRequiredSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                Debug.LogError($"生成结局界面 Prefab 失败：未找到结局图片，path={assetPath}");

            return sprite;
        }

        private static void SavePrefab(GameObject root)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath) != null)
                AssetDatabase.DeleteAsset(PrefabAssetPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            EnsureAddressableEntry();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
        }

        private static Image CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panelObject = CreateUiObject(name, parent);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string content, int fontSize,
            TextAnchor anchor, FontStyle fontStyle, Color color)
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

        private static Button CreateButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = new Color(0.84f, 0.86f, 0.90f, 1f);

            AddOutline(buttonObject, new Color(0.11f, 0.11f, 0.11f, 1f), new Vector2(2f, -2f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText($"{name}_Text", rect, new Vector2(0f, 0f), new Vector2(1f, 1f), label, 22, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(0.14f, 0.14f, 0.14f, 1f));
            return button;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static GameObject CreateUiObject(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
            return go;
        }

        private static Sprite EnsureWhiteSpriteAsset()
        {
            EnsureFolder("Assets/GameRes/UI/Generated");

            if (!File.Exists(WhiteSpriteAssetPath))
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                byte[] pngBytes = texture.EncodeToPNG();
                File.WriteAllBytes(WhiteSpriteAssetPath, pngBytes);
                Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(WhiteSpriteAssetPath, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = AssetImporter.GetAtPath(WhiteSpriteAssetPath) as TextureImporter;
            if (importer != null)
            {
                bool needReimport = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needReimport = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    needReimport = true;
                }

                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    needReimport = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    needReimport = true;
                }

                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    needReimport = true;
                }

                if (needReimport)
                    importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpriteAssetPath);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void EnsureAddressableEntry()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("生成结局界面 Prefab 后注册 Addressables 失败：未找到 AddressableAssetSettings。");
                return;
            }

            AddressableAssetGroup uiGroup = settings.FindGroup("UI");
            if (uiGroup == null)
            {
                Debug.LogError("生成结局界面 Prefab 后注册 Addressables 失败：未找到 UI 资源组。");
                return;
            }

            string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabAssetPath);
            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogError($"生成结局界面 Prefab 后注册 Addressables 失败：未找到资源 GUID，path={PrefabAssetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(prefabGuid, uiGroup);
            if (entry == null)
            {
                Debug.LogError($"生成结局界面 Prefab 后注册 Addressables 失败：无法创建或移动条目，guid={prefabGuid}");
                return;
            }

            entry.address = "panel_ending";
            EditorUtility.SetDirty(uiGroup);
            EditorUtility.SetDirty(settings);
        }
    }
}
