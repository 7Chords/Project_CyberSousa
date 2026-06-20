using System.IO;
using System.Collections.Generic;
using GameCore.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static class GameplayMainPrefabBuilder
    {
        private const string PrefabAssetPath = "Assets/GameRes/UI/panel_gameplay_main.prefab";
        private const string WhiteSpriteAssetPath = "Assets/GameRes/UI/Generated/gameplay_main_white.png";

        private static Sprite _whiteSprite;
        private static Font _defaultFont;

        [MenuItem("GameCore/UI/生成游戏内主界面Prefab")]
        public static void GenerateGameplayMainPrefab()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _whiteSprite = EnsureWhiteSpriteAsset();
            if (_whiteSprite == null)
            {
                Debug.LogError("生成游戏内主界面 Prefab 失败：纯白 Sprite 资源未准备成功。");
                return;
            }

            GameObject root = new GameObject("panel_gameplay_main", typeof(RectTransform), typeof(CanvasGroup), typeof(UIMonoGameplayMain));
            try
            {
                BuildPrefab(root);
                SavePrefab(root);
                Debug.Log("游戏内主界面 Prefab 生成完成。");
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

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            UIMonoGameplayMain mono = root.GetComponent<UIMonoGameplayMain>();
            _currentMono = mono;
            mono.canvasGroup = canvasGroup;
            mono.fadeInDuration = 0.2f;
            mono.fadeOutDuration = 0.2f;
            mono.numberButtons = new List<Button>();

            CreateBackground(rootRect);
            RectTransform leftBand;
            RectTransform centerBand;
            RectTransform rightBand;
            CreateStageFrame(rootRect, out leftBand, out centerBand, out rightBand);
            CreateLeftColumn(leftBand);
            CreateCenterColumn(centerBand);
            CreateTopCenterCurrentFloor(rootRect, mono);
            CreateRightColumn(rightBand, mono);
            CreateDialogueSection(rootRect, mono);
            CreateBottomHint(rootRect, mono);
            _currentMono = null;
        }

        private static void CreateBackground(RectTransform rootRect)
        {
            Image background = CreatePanel("Background", rootRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.94f, 0.94f, 0.92f, 0.98f));
            background.raycastTarget = false;
        }

        private static void CreateStageFrame(RectTransform rootRect, out RectTransform leftBand, out RectTransform centerBand, out RectTransform rightBand)
        {
            leftBand = CreatePanel("LeftBand", rootRect, new Vector2(0.00f, 0.00f), new Vector2(0.18f, 1.00f), new Color(0.91f, 0.91f, 0.92f, 0.75f)).rectTransform;
            centerBand = CreatePanel("CenterBand", rootRect, new Vector2(0.18f, 0.00f), new Vector2(0.78f, 1.00f), new Color(1f, 1f, 1f, 0.30f)).rectTransform;
            rightBand = CreatePanel("RightBand", rootRect, new Vector2(0.78f, 0.00f), new Vector2(1.00f, 1.00f), new Color(0.95f, 0.95f, 0.96f, 0.78f)).rectTransform;
        }

        private static void CreateLeftColumn(RectTransform leftBand)
        {
            Text noticeBoard01Text;
            Text noticeBoard02Text;
            CreateCardWithTitle("NoticeBoard01", leftBand, new Vector2(0.11f, 0.64f), new Vector2(0.67f, 0.88f), "告示1", 22, FontStyle.Bold, out noticeBoard01Text);
            CreateCardWithTitle("NoticeBoard02", leftBand, new Vector2(0.23f, 0.32f), new Vector2(0.79f, 0.55f), "告示2", 22, FontStyle.Bold, out noticeBoard02Text);
            _currentMono.txtNoticeBoard01 = noticeBoard01Text;
            _currentMono.txtNoticeBoard02 = noticeBoard02Text;
        }

        private static void CreateCenterColumn(RectTransform centerBand)
        {
            Image animalPanel = CreatePanel("AnimalView", centerBand, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f), new Color(0.80f, 0.80f, 0.80f, 0.92f));
            animalPanel.gameObject.AddComponent<RectMask2D>();

            RectTransform elevatorRoot = CreateContainer("ElevatorRoot", animalPanel.rectTransform, new Vector2(0.00f, -0.46f), new Vector2(1.00f, 1.46f));
            elevatorRoot.gameObject.AddComponent<CanvasGroup>();
            _currentMono.elevatorRoot = elevatorRoot;

            CreatePanel("ElevatorBack", elevatorRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.70f, 0.70f, 0.72f, 1f));
            CreatePanel("ElevatorInnerShadow", elevatorRoot, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), new Color(0.18f, 0.18f, 0.18f, 0.30f));

            RectTransform customerLayer = CreateContainer("CustomerLayer", elevatorRoot, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f));
            CanvasGroup customerCanvasGroup = customerLayer.gameObject.AddComponent<CanvasGroup>();
            _currentMono.customerCanvasGroup = customerCanvasGroup;
            UIRuntimeFadeEffect customerFadeEffect = customerLayer.gameObject.AddComponent<UIRuntimeFadeEffect>();
            customerFadeEffect.Setup(customerCanvasGroup, 0.30f);
            _currentMono.customerFadeEffect = customerFadeEffect;
            UIRuntimeMoveScaleEffect customerMoveScaleEffect = customerLayer.gameObject.AddComponent<UIRuntimeMoveScaleEffect>();
            customerMoveScaleEffect.Setup(0.28f, new Vector2(0f, -72f), 1.10f, new Vector2(0f, 68f), 0.84f);
            _currentMono.customerMoveScaleEffect = customerMoveScaleEffect;

            Image customerShadow = CreatePanel("CustomerShadow", customerLayer, new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.12f), new Color(0f, 0f, 0f, 0.16f));
            customerShadow.raycastTarget = false;
            _currentMono.txtCustomerName = CreateText("AnimalLabel", customerLayer, new Vector2(0.00f, 0.12f), new Vector2(1.00f, 0.96f), "动物", 40, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.25f, 0.25f, 0.25f, 1f));

            RectTransform doorMaskRoot = CreateContainer("ElevatorDoorMask", elevatorRoot, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
            RectTransform doorLeft = CreateContainer("ElevatorDoorLeft", doorMaskRoot, new Vector2(0.00f, 0.00f), new Vector2(0.50f, 1.00f));
            RectTransform doorRight = CreateContainer("ElevatorDoorRight", doorMaskRoot, new Vector2(0.50f, 0.00f), new Vector2(1.00f, 1.00f));
            CreatePanel("ElevatorDoorLeftFace", doorLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.86f, 0.86f, 0.88f, 1f));
            CreatePanel("ElevatorDoorRightFace", doorRight, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.84f, 0.84f, 0.86f, 1f));
            CreatePanel("ElevatorDoorLeftEdge", doorLeft, new Vector2(0.94f, 0f), new Vector2(1f, 1f), new Color(0.68f, 0.68f, 0.70f, 1f));
            CreatePanel("ElevatorDoorRightEdge", doorRight, new Vector2(0f, 0f), new Vector2(0.06f, 1f), new Color(0.68f, 0.68f, 0.70f, 1f));

            _currentMono.elevatorDoorLeft = doorLeft;
            _currentMono.elevatorDoorRight = doorRight;

            UISlidingDoorEffect slidingDoorEffect = elevatorRoot.gameObject.AddComponent<UISlidingDoorEffect>();
            slidingDoorEffect.Setup(doorLeft, doorRight, 450f, 0.32f);
            _currentMono.elevatorDoorEffect = slidingDoorEffect;

            UITravelShakeEffect travelShakeEffect = elevatorRoot.gameObject.AddComponent<UITravelShakeEffect>();
            travelShakeEffect.Setup(0.55f, new Vector2(18f, 8f), new Vector3(0f, 0f, 3f));
            _currentMono.elevatorTravelShakeEffect = travelShakeEffect;

            Image standShadow = CreatePanel("AnimalStandShadow", centerBand, new Vector2(0.22f, 0.03f), new Vector2(0.78f, 0.06f), new Color(0.70f, 0.70f, 0.70f, 0.55f));
            standShadow.raycastTarget = false;
        }

        private static void CreateRightColumn(RectTransform rightBand, UIMonoGameplayMain mono)
        {
            mono.txtTime = CreateTextPanel("TimePanel", rightBand, new Vector2(0.15f, 0.84f), new Vector2(0.92f, 0.91f), "第 1 天", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
            mono.txtAnimalInfo = CreateTextPanel("AnimalInfoPanel", rightBand, new Vector2(0.15f, 0.62f), new Vector2(0.92f, 0.80f), "动物扫脸信息", 22, TextAnchor.UpperCenter, FontStyle.Bold);

            RectTransform animalChoiceSection = CreateContainer("AnimalChoiceSection", rightBand, new Vector2(0.18f, 0.22f), new Vector2(0.92f, 0.58f));
            CreateNumberButtonGrid(animalChoiceSection, mono);

            RectTransform actionSection = CreateContainer("ActionSection", rightBand, new Vector2(0.10f, 0.13f), new Vector2(0.97f, 0.21f));
            mono.btnReject = CreateTextButton("ActionReject", actionSection, new Vector2(0.36f, 0f), new Vector2(0.56f, 1f), "拒绝", 18, new Color(0.86f, 0.86f, 0.86f, 1f));
            mono.btnConfirm = CreateTextButton("ActionConfirm", actionSection, new Vector2(0.59f, 0f), new Vector2(0.79f, 1f), "确认", 18, new Color(0.86f, 0.86f, 0.86f, 1f));
            mono.btnCloseDoor = CreateTextButton("ActionCloseDoor", actionSection, new Vector2(0.82f, 0f), new Vector2(1.00f, 1f), "关门", 18, new Color(0.86f, 0.86f, 0.86f, 1f));
        }

        private static void CreateTopCenterCurrentFloor(RectTransform rootRect, UIMonoGameplayMain mono)
        {
            Text currentFloorText = CreateTextPanel("CurrentFloorPanel", rootRect, new Vector2(0.42f, 0.90f), new Vector2(0.58f, 0.97f), "当前楼层：1", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
            mono.txtCurrentFloor = currentFloorText;

            CanvasGroup floorCanvasGroup = currentFloorText.gameObject.AddComponent<CanvasGroup>();
            floorCanvasGroup.alpha = 1f;
            mono.currentFloorCanvasGroup = floorCanvasGroup;

            UITextChangeMoveEffect floorTextEffect = currentFloorText.gameObject.AddComponent<UITextChangeMoveEffect>();
            floorTextEffect.Setup(currentFloorText, floorCanvasGroup, 0.26f, 16f);
            mono.floorTextEffect = floorTextEffect;
        }

        private static void CreateDialogueSection(RectTransform rootRect, UIMonoGameplayMain mono)
        {
            RectTransform dialogueSection = CreateContainer("DialogueSection", rootRect, new Vector2(0f, 0f), new Vector2(1f, 1f));
            mono.dialogueSection = dialogueSection.gameObject;

            mono.txtDialogueLeft = CreateDialogueStrip("Dialogue01", dialogueSection, new Vector2(0.15f, 0.74f), new Vector2(0.38f, 0.84f), "对话1", out mono.dialogueLeftArea);
            mono.txtDialogueRight = CreateDialogueStrip("Dialogue02", dialogueSection, new Vector2(0.57f, 0.63f), new Vector2(0.83f, 0.73f), "对话2", out mono.dialogueRightArea);
            mono.imgDialoguePortrait = CreatePortraitImage("DialoguePortrait", dialogueSection, new Vector2(0.16f, 0.85f), new Vector2(0.37f, 0.99f));

            RectTransform optionSection = CreateContainer("OptionSection", dialogueSection, new Vector2(0.63f, 0.18f), new Vector2(0.79f, 0.31f));
            mono.btnOption1 = CreateTextButton("Option01", optionSection, new Vector2(0f, 0.56f), new Vector2(1f, 1f), "选项1", 18, new Color(0.88f, 0.88f, 0.88f, 0.98f));
            mono.btnOption2 = CreateTextButton("Option02", optionSection, new Vector2(0f, 0f), new Vector2(1f, 0.44f), "选项2", 18, new Color(0.88f, 0.88f, 0.88f, 0.98f));

            dialogueSection.gameObject.SetActive(false);
        }

        private static void CreateBottomHint(RectTransform rootRect, UIMonoGameplayMain mono)
        {
            Image hintPanel = CreatePanel("BottomHintPanel", rootRect, new Vector2(0.27f, 0.02f), new Vector2(0.70f, 0.10f), new Color(0.82f, 0.82f, 0.82f, 0.95f));
            mono.txtBottomHint = CreateText(
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

        private static void CreateNumberButtonGrid(RectTransform choiceSection, UIMonoGameplayMain mono)
        {
            int[] numbers = { 10, 11, 12, 7, 8, 9, 4, 5, 6, 1, 2, 3 };
            float startX = 0.00f;
            float startY = 0.77f;
            float cellWidth = 0.35f;
            float cellHeight = 0.24f;

            for (int index = 0; index < numbers.Length; index++)
            {
                int row = index / 3;
                int column = index % 3;
                Vector2 anchorMin = new Vector2(startX + column * cellWidth, startY - row * cellHeight);
                Vector2 anchorMax = anchorMin + new Vector2(0.24f, 0.18f);
                Button button = CreateCircleButton($"AnimalChoice{numbers[index]}", choiceSection, anchorMin, anchorMax, numbers[index].ToString());
                mono.numberButtons.Add(button);
            }
        }

        private static UIMonoGameplayMain _currentMono;

        private static Image CreateCardWithTitle(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, FontStyle fontStyle, out Text titleText)
        {
            Image card = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.84f, 0.84f, 0.84f, 0.95f));
            titleText = CreateText($"{name}_Text", card.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), text, fontSize, TextAnchor.MiddleCenter, fontStyle, new Color(0.18f, 0.18f, 0.18f, 1f));
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.verticalOverflow = VerticalWrapMode.Overflow;
            return card;
        }

        private static Text CreateDialogueStrip(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string text, out Image stripImage)
        {
            stripImage = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.86f, 0.86f, 0.86f, 0.95f));
            Text textComponent = CreateText($"{name}_Text", stripImage.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.88f), text, 18, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.18f, 0.18f, 0.18f, 1f));
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            return textComponent;
        }

        private static Image CreatePortraitImage(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image image = CreatePanel(name, parent, anchorMin, anchorMax, new Color(1f, 1f, 1f, 0f));
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        private static Text CreateTextPanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, TextAnchor anchor, FontStyle fontStyle)
        {
            Image panel = CreatePanel(name, parent, anchorMin, anchorMax, new Color(0.86f, 0.86f, 0.86f, 0.98f));
            return CreateText($"{name}_Text", panel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), text, fontSize, anchor, fontStyle, new Color(0.18f, 0.18f, 0.18f, 1f));
        }

        private static Button CreateCircleButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = new Color(0.89f, 0.89f, 0.89f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.73f, 0.73f, 0.73f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameplayFloorButton floorButton = buttonObject.AddComponent<GameplayFloorButton>();
            int.TryParse(label, out floorButton.floor);

            CreateText($"{name}_Text", rect, new Vector2(0f, 0f), new Vector2(1f, 1f), label, 18, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.20f, 0.20f, 0.20f, 1f));
            return button;
        }

        private static Button CreateTextButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label, int fontSize, Color backgroundColor)
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

        private static Image CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
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

        private static RectTransform CreateContainer(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject containerObject = CreateUiObject(name, parent);
            RectTransform rect = containerObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
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

        private static Sprite GetWhiteSprite()
        {
            return _whiteSprite != null ? _whiteSprite : EnsureWhiteSpriteAsset();
        }

        private static void SavePrefab(GameObject root)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            EnsureAddressableEntry();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
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
                Debug.LogError("生成游戏内主界面 Prefab 后注册 Addressables 失败：未找到 AddressableAssetSettings。");
                return;
            }

            AddressableAssetGroup uiGroup = settings.FindGroup("UI");
            if (uiGroup == null)
            {
                Debug.LogError("生成游戏内主界面 Prefab 后注册 Addressables 失败：未找到 UI 资源组。");
                return;
            }

            string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabAssetPath);
            if (string.IsNullOrEmpty(prefabGuid))
            {
                Debug.LogError($"生成游戏内主界面 Prefab 后注册 Addressables 失败：未找到资源 GUID，path={PrefabAssetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(prefabGuid, uiGroup);
            if (entry == null)
            {
                Debug.LogError($"生成游戏内主界面 Prefab 后注册 Addressables 失败：无法创建或移动条目，guid={prefabGuid}");
                return;
            }

            entry.address = "panel_gameplay_main";
            EditorUtility.SetDirty(uiGroup);
            EditorUtility.SetDirty(settings);
        }
    }
}
