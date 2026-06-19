using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoSetting : _ASCUIMonoBase
    {
        [Header("音量滑动条")]
        public Slider sldMusic;
        [Header("音效滑动条")]
        public Slider sldSound;
        [Header("关闭按钮")]
        public Button btnClose;
        [Header("返回主页面按钮")]
        public Button btnReturnMain;
        [Header("按钮缩放大小")]
        public float btnEnterScale = 1.1f;
        [Header("按钮缩放时间")]
        public float btnScaleChgTime = 0.15f;
    }
}
