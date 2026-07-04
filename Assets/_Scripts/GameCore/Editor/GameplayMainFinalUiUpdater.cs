using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static class GameplayMainFinalUiUpdater
    {
        // Keep a lightweight watcher so the already-open Unity editor can apply the update on request.
        private const string SourceFolderAbsolutePath = "C:\\Users\\20584\\Downloads\\\u6700\u7ec8UI\\\u6700\u7ec8UI";
        private const string ImportedAssetFolder = "Assets/Art/Elevator/FinalUIImported";
        private const string GameplayMainPrefabPath = "Assets/GameRes/UI/panel_gameplay_main.prefab";
        private const string DialogueOptionPrefabPath = "Assets/GameRes/UI/item_dialogue_option.prefab";
        private const string AutoRunRequestFile = "Temp/finalui_update_request.txt";
        private const string AutoRunResultFile = "Temp/finalui_update_result.txt";

        private static bool _isAutoRunQueued;

        private static readonly SpriteImportSpec[] SpriteImportSpecs =
        {
            new("\u8bbe\u7f6e.png", "finalui_setting.png"),
            new("\u65b0\u7684\u65f6\u95f4.png", "finalui_time_panel.png"),
            new("\u65b0\u62d3\u9ebb\u6b4c\u5b50.png", "finalui_pet.png"),
            new("\u4eba\u8138\u8bc6\u522b.png", "finalui_face_panel.png"),
            new("\u4fbf\u7b7e01.png", "finalui_note_01.png"),
            new("\u4fbf\u7b7e02.png", "finalui_note_02.png"),
            new("\u6309\u952e\u5e95\u9762.png", "finalui_keypad_base.png"),
            new("\u4e5d\u5bab\u9009\u9879\u7eb8\u5f20.png", "finalui_option_paper.png"),
            new("\u65b0\u7ea2\u8272.png", "finalui_action_red.png"),
            new("\u65b0\u9ec4\u8272.png", "finalui_action_yellow.png"),
            new("\u65b0\u84dd\u8272.png", "finalui_action_blue.png"),
            new("\u7ee9\u6548.png", "finalui_performance.png"),
            new("1.png", "finalui_floor_1.png"),
            new("2.png", "finalui_floor_2.png"),
            new("3.png", "finalui_floor_3.png"),
            new("4.png", "finalui_floor_4.png"),
            new("5.png", "finalui_floor_5.png"),
            new("6.png", "finalui_floor_6.png"),
            new("7.png", "finalui_floor_7.png"),
            new("8.png", "finalui_floor_8.png"),
            new("9.png", "finalui_floor_9.png"),
            new("10.png", "finalui_floor_10.png"),
            new("11.png", "finalui_floor_11.png"),
            new("12.png", "finalui_floor_12.png"),
        };

        private static readonly Dictionary<string, string> GameplayMainSpriteTargets = new()
        {
            { "BtnSetting", "finalui_setting.png" },
            { "TimePanel", "finalui_time_panel.png" },
            { "Image", "finalui_pet.png" },
            { "AnimalInfoPanel", "finalui_face_panel.png" },
            { "NoticeBoard01", "finalui_note_01.png" },
            { "NoticeBoard02", "finalui_note_02.png" },
            { "go_score", "finalui_performance.png" },
            { "ActionReject", "finalui_action_red.png" },
            { "ActionConfirm", "finalui_action_yellow.png" },
            { "ActionCloseDoor", "finalui_action_blue.png" },
        };

        private static readonly Dictionary<int, string> FloorButtonSpriteTargets = new()
        {
            { 1, "finalui_floor_1.png" },
            { 2, "finalui_floor_2.png" },
            { 3, "finalui_floor_3.png" },
            { 4, "finalui_floor_4.png" },
            { 5, "finalui_floor_5.png" },
            { 6, "finalui_floor_6.png" },
            { 7, "finalui_floor_7.png" },
            { 8, "finalui_floor_8.png" },
            { 9, "finalui_floor_9.png" },
            { 10, "finalui_floor_10.png" },
            { 11, "finalui_floor_11.png" },
            { 12, "finalui_floor_12.png" },
        };

        [MenuItem("GameCore/UI/Apply Final UI Artwork")]
        public static void ApplyFinalUiArtworkFromMenu()
        {
            ApplyFinalUiArtwork();
        }

        [InitializeOnLoadMethod]
        private static void RegisterAutoRunWatcher()
        {
            EditorApplication.update -= AutoRunWatcher;
            EditorApplication.update += AutoRunWatcher;
            QueueAutoRunIfRequested();
        }

        public static void ApplyFinalUiArtwork()
        {
            if (!Directory.Exists(SourceFolderAbsolutePath))
            {
                Debug.LogError($"未找到最终 UI 图片目录：{SourceFolderAbsolutePath}");
                return;
            }

            EnsureDirectoryExists(ImportedAssetFolder);
            CopySourceSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Dictionary<string, Sprite> spriteMap = ConfigureAndLoadSprites();
            if (spriteMap.Count == 0)
            {
                Debug.LogError("最终 UI 图片导入后没有成功加载到任何 Sprite。");
                return;
            }

            UpdateGameplayMainPrefab(spriteMap);
            UpdateDialogueOptionPrefab(spriteMap);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Gameplay 主界面最终 UI 图片已更新完成。");
        }

        private static void AutoRunWatcher()
        {
            QueueAutoRunIfRequested();
        }

        private static void QueueAutoRunIfRequested()
        {
            if (_isAutoRunQueued)
                return;

            string requestPath = GetProjectAbsolutePath(AutoRunRequestFile);
            if (!File.Exists(requestPath))
                return;

            _isAutoRunQueued = true;
            Debug.Log("检测到最终 UI 自动更新请求，准备执行 prefab 图片替换。");
            EditorApplication.delayCall += RunAutoRequestedUpdate;
        }

        private static void RunAutoRequestedUpdate()
        {
            string requestPath = GetProjectAbsolutePath(AutoRunRequestFile);
            string resultPath = GetProjectAbsolutePath(AutoRunResultFile);

            try
            {
                if (File.Exists(requestPath))
                    File.Delete(requestPath);

                ApplyFinalUiArtwork();
                File.WriteAllText(resultPath, $"SUCCESS {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.LogError($"最终 UI 自动更新失败：{exception}");
                File.WriteAllText(resultPath, $"FAIL {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{exception}", Encoding.UTF8);
            }
            finally
            {
                _isAutoRunQueued = false;
            }
        }

        private static void CopySourceSprites()
        {
            foreach (SpriteImportSpec spec in SpriteImportSpecs)
            {
                string sourcePath = Path.Combine(SourceFolderAbsolutePath, spec.SourceFileName);
                string targetPath = Path.Combine(ImportedAssetFolder, spec.TargetFileName);
                string fullTargetPath = GetProjectAbsolutePath(targetPath);

                if (!File.Exists(sourcePath))
                {
                    Debug.LogError($"缺少源图片：{sourcePath}");
                    continue;
                }

                File.Copy(sourcePath, fullTargetPath, true);
                Debug.Log($"已复制图片：{sourcePath} -> {targetPath}");
            }
        }

        private static Dictionary<string, Sprite> ConfigureAndLoadSprites()
        {
            Dictionary<string, Sprite> spriteMap = new();
            foreach (SpriteImportSpec spec in SpriteImportSpecs)
            {
                string assetPath = $"{ImportedAssetFolder}/{spec.TargetFileName}";
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"未找到图片导入器：{assetPath}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100f;
                importer.SaveAndReimport();

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    Debug.LogError($"Sprite 加载失败：{assetPath}");
                    continue;
                }

                spriteMap[spec.TargetFileName] = sprite;
            }

            return spriteMap;
        }

        private static void UpdateGameplayMainPrefab(IReadOnlyDictionary<string, Sprite> spriteMap)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(GameplayMainPrefabPath);
            try
            {
                foreach (KeyValuePair<string, string> pair in GameplayMainSpriteTargets)
                    ApplySpriteToNamedObject(prefabRoot.transform, pair.Key, GetSprite(spriteMap, pair.Value));

                EnsureKeypadBackground(prefabRoot.transform, GetSprite(spriteMap, "finalui_keypad_base.png"));
                UpdateFloorButtons(prefabRoot.transform, spriteMap);
                DisableNamedObject(prefabRoot.transform, "BtnSetting_Text");
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, GameplayMainPrefabPath);
                Debug.Log($"已更新 prefab：{GameplayMainPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void UpdateDialogueOptionPrefab(IReadOnlyDictionary<string, Sprite> spriteMap)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(DialogueOptionPrefabPath);
            try
            {
                Image rootImage = prefabRoot.GetComponent<Image>();
                if (rootImage == null)
                {
                    Debug.LogError($"对话选项 prefab 缺少根节点 Image：{DialogueOptionPrefabPath}");
                }
                else
                {
                    rootImage.sprite = GetSprite(spriteMap, "finalui_option_paper.png");
                    rootImage.type = Image.Type.Simple;
                    rootImage.preserveAspect = false;
                    rootImage.raycastTarget = true;
                }

                DisableNamedObject(prefabRoot.transform, "Image");
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, DialogueOptionPrefabPath);
                Debug.Log($"已更新 prefab：{DialogueOptionPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void UpdateFloorButtons(Transform root, IReadOnlyDictionary<string, Sprite> spriteMap)
        {
            foreach (KeyValuePair<int, string> pair in FloorButtonSpriteTargets)
            {
                string objectName = $"AnimalChoice{pair.Key}";
                Transform buttonTransform = FindDeepChild(root, objectName);
                if (buttonTransform == null)
                {
                    Debug.LogError($"未找到楼层按钮：{objectName}");
                    continue;
                }

                Image image = buttonTransform.GetComponent<Image>();
                if (image == null)
                {
                    Debug.LogError($"楼层按钮缺少 Image：{objectName}");
                    continue;
                }

                image.sprite = GetSprite(spriteMap, pair.Value);
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = true;

                DisableNamedObject(buttonTransform, "Text");
            }
        }

        private static void EnsureKeypadBackground(Transform root, Sprite sprite)
        {
            Transform choiceSection = FindDeepChild(root, "AnimalChoiceSection");
            if (choiceSection == null)
            {
                Debug.LogError("未找到 AnimalChoiceSection，无法添加按键底面。");
                return;
            }

            Transform backgroundTransform = choiceSection.Find("KeypadBackground");
            if (backgroundTransform == null)
            {
                GameObject backgroundObject = new GameObject("KeypadBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backgroundTransform = backgroundObject.transform;
                backgroundTransform.SetParent(choiceSection, false);
                backgroundTransform.SetAsFirstSibling();
            }

            RectTransform rectTransform = backgroundTransform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = new Vector2(-20f, -20f);
                rectTransform.offsetMax = new Vector2(20f, 20f);
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            Image image = backgroundTransform.GetComponent<Image>();
            if (image == null)
                image = backgroundTransform.gameObject.AddComponent<Image>();

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static void ApplySpriteToNamedObject(Transform root, string objectName, Sprite sprite)
        {
            Transform target = FindDeepChild(root, objectName);
            if (target == null)
            {
                Debug.LogError($"未找到节点：{objectName}");
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"节点缺少 Image 组件：{objectName}");
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static void DisableNamedObject(Transform root, string childName)
        {
            Transform target = FindDeepChild(root, childName);
            if (target == null)
            {
                Debug.LogWarning($"未找到需要隐藏的节点：{childName}");
                return;
            }

            target.gameObject.SetActive(false);
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null)
                return null;

            Queue<Transform> queue = new();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                if (current.name == targetName)
                    return current;

                for (int index = 0; index < current.childCount; index++)
                    queue.Enqueue(current.GetChild(index));
            }

            return null;
        }

        private static Sprite GetSprite(IReadOnlyDictionary<string, Sprite> spriteMap, string key)
        {
            if (!spriteMap.TryGetValue(key, out Sprite sprite) || sprite == null)
                throw new InvalidOperationException($"未找到 Sprite：{key}");

            return sprite;
        }

        private static void EnsureDirectoryExists(string assetFolderPath)
        {
            string fullPath = GetProjectAbsolutePath(assetFolderPath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        private static string GetProjectAbsolutePath(string assetRelativePath)
        {
            string projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRootPath, assetRelativePath));
        }

        private readonly struct SpriteImportSpec
        {
            public SpriteImportSpec(string sourceFileName, string targetFileName)
            {
                SourceFileName = sourceFileName;
                TargetFileName = targetFileName;
            }

            public string SourceFileName { get; }
            public string TargetFileName { get; }
        }
    }
}
