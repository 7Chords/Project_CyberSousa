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
        [Header("电梯根节点")]
        public RectTransform elevatorRoot;
        [Header("电梯左门")]
        public RectTransform elevatorDoorLeft;
        [Header("电梯右门")]
        public RectTransform elevatorDoorRight;
        [Header("住户层 CanvasGroup")]
        public CanvasGroup customerCanvasGroup;
        [Header("当前楼层 CanvasGroup")]
        public CanvasGroup currentFloorCanvasGroup;
        [Header("住户淡入淡出动效")]
        public UIRuntimeFadeEffect customerFadeEffect;
        [Header("住户移动缩放松动效")]
        public UIRuntimeMoveScaleEffect customerMoveScaleEffect;
        [Header("电梯开关门动效")]
        public UISlidingDoorEffect elevatorDoorEffect;
        [Header("电梯运行震动感效")]
        public UITravelShakeEffect elevatorTravelShakeEffect;
        [Header("楼层文字切换动效")]
        public UITextChangeMoveEffect floorTextEffect;
        [Header("对话区域根节点")]
        public GameObject dialogueSection;
        [Header("左侧对话条背景")]
        public Image dialogueLeftArea;
        [Header("右侧对话条背景")]
        public Image dialogueRightArea;
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
        [Header("告示板列表")]
        public List<NoticeBoardItem> noticeBoardList = new List<NoticeBoardItem>();
        [Header("住户名称文本")]
        public Text txtCustomerName;
        [Header("动物档案信息文本")]
        public Text txtAnimalInfo;
        [Header("底部提示文本")]
        public Text txtBottomHint;
        [Header("对话选项1按钮")]
        public Button btnOption1;
        [Header("对话选项2按钮")]
        public Button btnOption2;
        [Header("拒绝按钮")]
        public Button btnReject;
        [Header("确认按钮")]
        public Button btnConfirm;
        [Header("关门按钮")]
        public Button btnCloseDoor;
        [Header("楼层数字按键列表")]
        public List<Button> numberButtons = new List<Button>();
    }
}
