using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoStart : _ASCUIMonoBase
    {
        [Header("开始游戏按钮")]
        public Button btnStart;
        [Header("继续游戏按钮")]
        public Button btnContinue;
        [Header("设置按钮")]
        public Button btnSetting;
        [Header("退出游戏按钮")]
        public Button btnExit;
    }
}
