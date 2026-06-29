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
                txtContent.text = content ?? string.Empty;
        }
    }
}
