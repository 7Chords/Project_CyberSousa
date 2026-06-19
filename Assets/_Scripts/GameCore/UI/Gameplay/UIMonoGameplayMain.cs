using UnityEngine;
using System.Collections.Generic;
using SCFrame.UI;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIMonoGameplayMain : _ASCUIMonoBase
    {
        public GameObject dialogueSection;
        public Text txtDialogueLeft;
        public Text txtDialogueRight;
        public Text txtTime;
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
