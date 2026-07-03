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

        public void EnsureReferences()
        {
            if (btnOption == null)
                btnOption = GetComponent<Button>();

            if (btnOption == null)
                btnOption = GetComponentInChildren<Button>(true);

            if (txtContent == null && btnOption != null)
                txtContent = btnOption.GetComponentInChildren<Text>(true);
        }

        public void SetContent(string content)
        {
            EnsureReferences();
            if (txtContent != null)
                txtContent.text = DialogueTextFormatter.PreventLineStartPunctuation(content);

            RefreshLayout();
        }

        public void RefreshLayout()
        {
            EnsureReferences();
            if (txtContent != null)
            {
                txtContent.SetAllDirty();
                LayoutRebuilder.ForceRebuildLayoutImmediate(txtContent.rectTransform);

                RectTransform textLayoutRoot = txtContent.transform.parent as RectTransform;
                if (textLayoutRoot != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(textLayoutRoot);
            }

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        public RectTransform AnimRoot => transform as RectTransform;

        public CanvasGroup EnsureCanvasGroup()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            return canvasGroup;
        }
    }
}
