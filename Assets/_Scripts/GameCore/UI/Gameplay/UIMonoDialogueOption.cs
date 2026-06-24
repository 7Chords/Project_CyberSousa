using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoDialogueOption : MonoBehaviour
    {
        [Header("选项按钮")]
        public Button btnOption;
        [Header("选项文本")]
        public Text txtContent;
        [Header("选项背景图")]
        public Image imgBackground;
        [Header("文本布局根节点")]
        public RectTransform textLayoutRoot;

        [Header("背景九宫格 PPU")]
        [Tooltip("参考宽度下使用的 Pixels Per Unit Multiplier")]
        public float backgroundPixelsPerUnitBase = 2.3f;
        [Tooltip("背景外观最佳的参考宽度")]
        public float backgroundReferenceWidth = 600f;
        public float backgroundMinPixelsPerUnitMultiplier = 1f;
        public float backgroundMaxPixelsPerUnitMultiplier = 4f;

        public void EnsureReferences()
        {
            if (btnOption == null)
                btnOption = GetComponent<Button>();

            if (btnOption == null)
                btnOption = GetComponentInChildren<Button>(true);

            if (imgBackground == null)
                imgBackground = GetComponent<Image>();

            if (imgBackground == null && btnOption != null)
                imgBackground = btnOption.targetGraphic as Image;

            if (txtContent == null && btnOption != null)
                txtContent = btnOption.GetComponentInChildren<Text>(true);

            if (textLayoutRoot == null && txtContent != null)
                textLayoutRoot = txtContent.transform.parent as RectTransform;
        }

        public void SetContent(string content)
        {
            EnsureReferences();
            if (txtContent != null)
                txtContent.text = content ?? string.Empty;

            RefreshLayoutImmediate();
        }

        public void RefreshLayoutImmediate()
        {
            EnsureReferences();

            if (txtContent != null)
            {
                txtContent.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(txtContent.rectTransform);
            }

            if (textLayoutRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(textLayoutRoot);

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        public void RefreshBackgroundPixelsPerUnitMultiplier()
        {
            EnsureReferences();
            ApplyBackgroundPixelsPerUnitMultiplier();
        }

        private void ApplyBackgroundPixelsPerUnitMultiplier()
        {
            if (imgBackground == null)
                return;

            RectTransform rootRect = transform as RectTransform;
            if (rootRect == null)
                return;

            float width = rootRect.rect.width;
            if (width <= 0f)
                return;

            float ratio = width / Mathf.Max(backgroundReferenceWidth, 1f);
            float multiplier = backgroundPixelsPerUnitBase * ratio;
            multiplier = Mathf.Clamp(multiplier, backgroundMinPixelsPerUnitMultiplier, backgroundMaxPixelsPerUnitMultiplier);
            imgBackground.pixelsPerUnitMultiplier = multiplier;
        }
    }
}
