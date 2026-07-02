using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoGameplayGuideMask : MonoBehaviour
    {
        [Header("遮罩根节点")]
        public RectTransform rootRect;
        [Header("四边遮罩")]
        public List<RectTransform> maskRects = new List<RectTransform>();
        [Header("当前高亮区域")]
        public RectTransform highlightRect;
        [Header("透明继续点击按钮")]
        public Button continueButton;
        [Header("透明继续点击区域")]
        public RectTransform continueClickRect;
        [Header("提示文本")]
        public Text tipText;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetTarget(RectTransform target, string tip, bool enableContinueClick)
        {
            if (rootRect == null)
                rootRect = transform as RectTransform;

            if (target == null || rootRect == null || maskRects == null || maskRects.Count < 4 || highlightRect == null)
            {
                Debug.LogError("Gameplay 引导遮罩显示失败：Prefab 引用不完整。");
                SetVisible(false);
                return;
            }

            if (tipText != null)
                tipText.text = tip;

            Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect, target);
            Vector2 min = targetBounds.min;
            Vector2 max = targetBounds.max;
            const float padding = 10f;
            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            Rect root = rootRect.rect;
            SetRect(maskRects[0], root.xMin, max.y, root.width, root.yMax - max.y);
            SetRect(maskRects[1], root.xMin, root.yMin, root.width, min.y - root.yMin);
            SetRect(maskRects[2], root.xMin, min.y, min.x - root.xMin, max.y - min.y);
            SetRect(maskRects[3], max.x, min.y, root.xMax - max.x, max.y - min.y);
            SetRect(highlightRect, min.x, min.y, max.x - min.x, max.y - min.y);

            if (continueClickRect != null)
                SetRect(continueClickRect, min.x, min.y, max.x - min.x, max.y - min.y);

            if (continueButton != null)
                continueButton.gameObject.SetActive(enableContinueClick);
        }

        private static void SetRect(RectTransform rectTransform, float x, float y, float width, float height)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }
    }
}
