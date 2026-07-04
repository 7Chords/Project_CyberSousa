using SCFrame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoStart : _ASCUIMonoBase
    {
        public GameObject bg1;
        public GameObject bg2;
        public GameObject btnBox;
        [Header("开始游戏按钮")]
        public Button btnStart;
        [Header("继续游戏按钮")]
        public Button btnContinue;
        [Header("设置按钮")]
        public Button btnSetting;
        [Header("开发者名单按钮")]
        public Button btnDevelopers;
        [Header("退出游戏按钮")]
        public Button btnExit;
    }
}
