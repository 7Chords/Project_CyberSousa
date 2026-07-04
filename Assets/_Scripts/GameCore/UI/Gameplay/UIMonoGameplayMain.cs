using UnityEngine;
using System.Collections.Generic;
using SCFrame.UI;
using UnityEngine.UI;

namespace GameCore.UI
{
    [System.Serializable]
    public class NoticeBoardItem
    {
        [Header("告示板根节点")]
        public GameObject root;
        [Header("告示板文本")]
        public Text text;
    }

    public class UIMonoGameplayMain : _ASCUIMonoBase
    {
        [Header("电梯视图")]
        public UIElevatorView elevatorView;
        [Header("住户层 CanvasGroup")]
        public CanvasGroup customerCanvasGroup;
        [Header("住户头像")]
        public Image imgCustomerPortrait;
        [Header("当前楼层 CanvasGroup")]
        public CanvasGroup currentFloorCanvasGroup;
        [Header("住户淡入淡出动效")]
        public UIRuntimeFadeEffect customerFadeEffect;
        [Header("住户移动缩放松动效")]
        public UIRuntimeMoveScaleEffect customerMoveScaleEffect;
        [Header("楼层文字切换动效")]
        public UITextChangeMoveEffect floorTextEffect;
        [Header("楼层场景背景")]
        public Image imgFloorSceneBackground;
        [Header("楼层场景背景叠层")]
        public Image imgFloorSceneBackgroundOverlay;
        [Header("楼层场景背景资源名（1-12楼）")]
        public List<string> floorSceneBackgroundResNames = new List<string>
        {
            "spr_scene_1",
            "spr_scene_2",
            "spr_scene_3",
            "spr_scene_4",
            "spr_scene_5",
            "spr_scene_6",
            "spr_scene_7",
            "spr_scene_8",
            "spr_scene_9",
            "spr_scene_10",
            "spr_scene_11",
            "spr_scene_12",
        };
        [Header("对话区域根节点")]
        public GameObject dialogueSection;
        [Header("左侧对话条背景")]
        public Image dialogueLeftArea;
        [Header("右侧对话条背景")]
        public Image dialogueRightArea;
        [Header("对话推进全屏点击区域")]
        public Image dialogueAdvanceClickArea;
        [Header("对话头像")]
        public Image imgDialoguePortrait;
        [Header("左侧对话文本")]
        public Text txtDialogueLeft;
        [Header("右侧对话文本")]
        public Text txtDialogueRight;
        [Header("当前楼层文本")]
        public Text txtCurrentFloor;
        [Header("天数文本")]
        public Text txtTime;
        [Header("时钟文本")]
        public Text txtClockTime;
        [Header("绩效值文本")]
        public Text txtPerformance;
        [Header("告示板列表")]
        public List<NoticeBoardItem> noticeBoardList = new List<NoticeBoardItem>();
        [Header("住户名称文本")]
        public Text txtCustomerName;
        [Header("动物档案信息文本")]
        public Text txtAnimalInfo;
        [Header("需求楼层文本")]
        public Text txtDemandFloor;
        [Header("底部提示文本")]
        public Text txtBottomHint;
        [Header("对话选项容器")]
        public RectTransform dialogueOptionRoot;
        [Header("对话选项 Addressables 资源名")]
        public string dialogueOptionItemResName = "item_dialogue_option";
        [Header("拒绝按钮")]
        public Button btnReject;
        [Header("确认按钮")]
        public Button btnConfirm;
        [Header("关门按钮")]
        public Button btnCloseDoor;
        [Header("设置按钮")]
        public Button btnSetting;
        [Header("楼层数字按键列表")]
        public List<Button> numberButtons = new List<Button>();


        public string GetFloorSceneBackgroundResName(int floor)
        {
            if (floor < 1)
                return null;

            if (floorSceneBackgroundResNames == null || floorSceneBackgroundResNames.Count == 0)
                return $"spr_scene_{floor}";

            int index = floor - 1;
            if (index >= floorSceneBackgroundResNames.Count)
                return null;

            string resName = floorSceneBackgroundResNames[index];
            return string.IsNullOrEmpty(resName) ? null : resName;
        }
    }
}
