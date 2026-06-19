using UnityEngine;
using System.Collections.Generic;
using SCFrame.UI;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoGameplayMain : _ASCUIMonoBase
    {
        public GameObject dialogueSection;
        public Image dialogueLeftArea;
        public Image dialogueRightArea;
        public Text txtDialogueLeft;
        public Text txtDialogueRight;
        public Text txtCurrentFloor;
        public Text txtTime;
        public Text txtNoticeBoard01;
        public Text txtNoticeBoard02;
        public Text txtCustomerName;
        public Text txtAnimalInfo;
        public Text txtBottomHint;
        public Button btnOption1;
        public Button btnOption2;
        public Button btnReject;
        public Button btnConfirm;
        public Button btnCloseDoor;
        public List<Button> numberButtons = new List<Button>();
    }
}
