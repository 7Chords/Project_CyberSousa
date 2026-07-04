using System.IO;
using GameCore.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static class DevelopersPrefabBuilder
    {
        private const string DevelopersPrefabAssetPath = "Assets/GameRes/UI/panel_developers.prefab";
        private const string StartPrefabAssetPath = "Assets/GameRes/UI/panel_start.prefab";
        private const string WhiteSpriteAssetPath = "Assets/GameRes/UI/Generated/gameplay_main_white.png";

        private static Font _defaultFont;
        private static Sprite _whiteSprite;

        [MenuItem("GameCore/UI/生成开发者名单Prefab并更新标题页入口")]
        public static void GenerateDevelopersPrefabAndStartEntry()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _whiteSprite = EnsureWhiteSpriteAsset();
            if (_whiteSprite == null)
            {
                Debug.LogError("生成开发者名单 Prefab 失败：纯白 Sprite 资源未准备成功。");
                return;
            }

            GenerateDevelopersPrefab();
            UpdateStartPrefabEntry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("开发者名单 Prefab 与标题页入口更新完成。");
        }

        private static void GenerateDevelopersPrefab()
        {
            GameObject root = new GameObject("panel_developers", typeof(RectTransform), typeof(CanvasGroup), typeof(UIMonoDevelopers));
            try
            {
                BuildDevelopersPrefab(root);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DevelopersPrefabAssetPath);
                EnsureAddressableEntry(DevelopersPrefabAssetPath, "panel_developers", "生成开发者名单 Prefab");
                EditorUtility.SetDirty(prefab);
                Selection.activeObject = prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildDevelopersPrefab(GameObject root)
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            UIMonoDevelopers mono = root.GetComponent<UIMonoDevelopers>();
            mono.canvasGroup = root.GetComponent<CanvasGroup>();
            mono.fadeInDuration = 0.18f;
            mono.fadeOutDuration = 0.16f;

            Image mask = CreatePanel("Mask", rootRect, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.58f));
            mask.raycastTarget = true;

            Image panel = CreatePanel("DevelopersPanel", rootRect, new Vector2(0.28f, 0.18f), new Vector2(0.72f, 0.82f), new Color(0.93f, 0.93f, 0.90f, 0.98f));
            panel.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.22f, 0.22f, 0.85f);

            CreateText("TitleText", panel.rectTransform, new Vector2(0.10f, 0.78f), new Vector2(0.90f, 0.92f), "开发者名单", 32, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.13f, 0.13f, 0.13f, 1f));
            CreateText("DevelopersText", panel.rectTransform, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.74f), "制作团队\n\n策划 / 程序 / 美术 / 音频\nCyberSousa Team\n\n感谢游玩", 23, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.18f, 0.18f, 0.18f, 1f));
            mono.btnClose = CreateButton("CloseButton", panel.rectTransform, new Vector2(0.34f, 0.10f), new Vector2(0.66f, 0.22f), "关闭", 22);
        }

        private static void UpdateStartPrefabEntry()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StartPrefabAssetPath);
            if (root == null)
            {
                Debug.LogError($"更新标题页入口失败：未找到 Prefab，path={StartPrefabAssetPath}");
                return;
            }

            try
            {
                UIMonoStart mono = root.GetComponent<UIMonoStart>();
                if (mono == null)
                {
                    Debug.LogError("更新标题页入口失败：panel_start 根节点缺少 UIMonoStart。");
                    return;
                }

                RectTransform rootRect = root.GetComponent<RectTransform>();
                Transform oldEntry = rootRect.Find("DevelopersButton");
                if (oldEntry != null)
                    Object.DestroyImmediate(oldEntry.gameObject);

                Button developersButton = CreateButton("DevelopersButton", rootRect, new Vector2(0.82f, 0.035f), new Vector2(0.965f, 0.095f), "开发者名单", 18);
                mono.btnDevelopers = developersButton;

                PrefabUtility.SaveAsPrefabAsset(root, StartPrefabAssetPath);
                EnsureAddressableEntry(StartPrefabAssetPath, "panel_start", "更新标题页入口");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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

        private static Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string content, int fontSize, TextAnchor anchor, FontStyle fontStyle, Color color)
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

        private static Button CreateButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label, int fontSize)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = new Color(0.84f, 0.84f, 0.82f, 0.96f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.38f, 0.38f, 0.36f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateText($"{name}_Text", rect, Vector2.zero, Vector2.one, label, fontSize, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.13f, 0.13f, 0.13f, 1f));
            return button;
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
                File.WriteAllBytes(WhiteSpriteAssetPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(WhiteSpriteAssetPath, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = AssetImporter.GetAtPath(WhiteSpriteAssetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
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

        private static void EnsureAddressableEntry(string prefabPath, string address, string actionName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError($"{actionName} 后注册 Addressables 失败：未找到 AddressableAssetSettings。");
                return;
            }

            AddressableAssetGroup uiGroup = settings.FindGroup("UI");
            if (uiGroup == null)
            {
                Debug.LogError($"{actionName} 后注册 Addressables 失败：未找到 UI 资源组。");
                return;
            }

            string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogError($"{actionName} 后注册 Addressables 失败：未找到资源 GUID，path={prefabPath}");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(prefabGuid, uiGroup);
            if (entry == null)
            {
                Debug.LogError($"{actionName} 后注册 Addressables 失败：无法创建或移动条目，guid={prefabGuid}");
                return;
            }

            entry.address = address;
            EditorUtility.SetDirty(uiGroup);
            EditorUtility.SetDirty(settings);
        }
    }
}
