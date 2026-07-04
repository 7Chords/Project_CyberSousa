using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoEnding : _ASCUIMonoBase
    {
        [Header("结局插图")]
        public Image imgEnding;
        public Sprite sprBadEnding;
        public Sprite sprEnding1;
        public Sprite sprEnding2;
        [Header("电视遮罩")]
        public Image imgTvMask;
        public float tvMaskFadeDuration = 1.2f;
        [Header("结局文本")]
        public Text txtSummary;
        [Header("返回按钮")]
        public Button btnReturnMain;
    }
}
