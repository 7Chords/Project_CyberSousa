using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using GameCore.RefData;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIPanelGameplayMain : _ASCUIPanelBase<UIMonoGameplayMain>
    {
        private const string PlayerSpeakerName = "玩家";
        private const long DefaultLevelId = 1001;
        private const string SettlementNodeName = "UINodeSettlement";
        private const float CustomerFadeDuration = 0.30f;

        private readonly Dictionary<long, DialogueRefData> _dialogueMap = new Dictionary<long, DialogueRefData>();
        private readonly Dictionary<long, CustomerRefData> _customerMap = new Dictionary<long, CustomerRefData>();
        private readonly Dictionary<long, RuleRefData> _ruleMap = new Dictionary<long, RuleRefData>();
        private readonly List<LevelRefData> _levelSequence = new List<LevelRefData>();
        private readonly Queue<long> _pendingCustomerIds = new Queue<long>();

        private LevelRefData _currentLevelRefData;
        private CustomerRefData _currentCustomerRefData;
        private CustomerNeedRefData _currentNeedRefData;
        private DialogueRefData _currentDialogueRefData;
        private DialogueRefData _option1DialogueRefData;
        private DialogueRefData _option2DialogueRefData;
        private JudgmentEffectData _lastJudgmentEffectData;
        private RuleEffectData _lastRuleEffectData;

        private List<long> _currentRuleIdList = new List<long>();
        private int _currentFloor = 1;
        private int _selectedFloor;
        private int _currentAffectValue;
        private int _currentDayIndex;
        private bool _isDialogueRunning;
        private bool _canCloseDoor;
        private bool _isSettlementShowing;
        private bool _isTransitionPlaying;
        private bool _hasInitializedRuntime;

        public UIPanelGameplayMain(UIMonoGameplayMain _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            InitializeRuntimeData();
            RefreshAllUi();
        }

        public override void BeforeDiscard()
        {
            UnbindButtons();
        }

        public override void OnHidePanel()
        {
            UnbindButtons();
        }

        public override void OnShowPanel()
        {
            if (!_hasInitializedRuntime)
                InitializeRuntimeData();

            BindButtons();
            RefreshAllUi();
        }

        private void InitializeRuntimeData()
        {
            if (_hasInitializedRuntime)
                return;

            EnsureRuntimeReferences();
            BuildLookupMaps();
            InitializeLevelRuntime();
            _hasInitializedRuntime = true;
        }

        private void EnsureRuntimeReferences()
        {
            if (mono.txtCurrentFloor == null)
                mono.txtCurrentFloor = FindChildText("CurrentFloorPanel_Text");

            if (mono.txtNoticeBoard01 == null)
                mono.txtNoticeBoard01 = FindChildText("NoticeBoard01_Text");

            if (mono.txtNoticeBoard02 == null)
                mono.txtNoticeBoard02 = FindChildText("NoticeBoard02_Text");

            if (mono.txtCustomerName == null)
                mono.txtCustomerName = FindChildText("AnimalLabel");

            if (mono.dialogueLeftArea == null)
                mono.dialogueLeftArea = FindChildImage("Dialogue01");

            if (mono.dialogueRightArea == null)
                mono.dialogueRightArea = FindChildImage("Dialogue02");

            if (mono.elevatorRoot == null)
                mono.elevatorRoot = FindChildRect("ElevatorRoot");

            if (mono.elevatorDoorLeft == null)
                mono.elevatorDoorLeft = FindChildRect("ElevatorDoorLeft");

            if (mono.elevatorDoorRight == null)
                mono.elevatorDoorRight = FindChildRect("ElevatorDoorRight");

            if (mono.customerCanvasGroup == null)
            {
                Transform customerLayer = FindChildTransformRecursive(mono.transform, "CustomerLayer");
                if (customerLayer != null)
                    mono.customerCanvasGroup = customerLayer.GetComponent<CanvasGroup>();
            }

            if (mono.currentFloorCanvasGroup == null && mono.txtCurrentFloor != null)
                mono.currentFloorCanvasGroup = mono.txtCurrentFloor.GetComponent<CanvasGroup>();

            if (mono.customerFadeEffect == null && mono.customerCanvasGroup != null)
                mono.customerFadeEffect = mono.customerCanvasGroup.GetComponent<UIRuntimeFadeEffect>();

            if (mono.customerMoveScaleEffect == null && mono.customerCanvasGroup != null)
                mono.customerMoveScaleEffect = mono.customerCanvasGroup.GetComponent<UIRuntimeMoveScaleEffect>();

            if (mono.elevatorDoorEffect == null && mono.elevatorRoot != null)
                mono.elevatorDoorEffect = mono.elevatorRoot.GetComponent<UISlidingDoorEffect>();

            if (mono.elevatorTravelShakeEffect == null && mono.elevatorRoot != null)
                mono.elevatorTravelShakeEffect = mono.elevatorRoot.GetComponent<UITravelShakeEffect>();

            if (mono.floorTextEffect == null && mono.txtCurrentFloor != null)
                mono.floorTextEffect = mono.txtCurrentFloor.GetComponent<UITextChangeMoveEffect>();
        }

        private void BuildLookupMaps()
        {
            _dialogueMap.Clear();
            List<DialogueRefData> dialogueRefDataList = SCRefDataMgr.instance.dialogueRefList?.refDataList;
            if (dialogueRefDataList != null)
            {
                for (int index = 0; index < dialogueRefDataList.Count; index++)
                {
                    DialogueRefData dialogueRefData = dialogueRefDataList[index];
                    if (dialogueRefData == null)
                    {
                        Debug.LogError($"Gameplay 对话表初始化失败：第 {index} 条对话配置为空。");
                        continue;
                    }

                    if (_dialogueMap.ContainsKey(dialogueRefData.id))
                    {
                        Debug.LogError($"Gameplay 对话表初始化失败：重复的 dialogueId={dialogueRefData.id}");
                        continue;
                    }

                    _dialogueMap.Add(dialogueRefData.id, dialogueRefData);
                }
            }

            _customerMap.Clear();
            List<CustomerRefData> customerRefDataList = SCRefDataMgr.instance.customerRefList?.refDataList;
            if (customerRefDataList != null)
            {
                for (int index = 0; index < customerRefDataList.Count; index++)
                {
                    CustomerRefData customerRefData = customerRefDataList[index];
                    if (customerRefData == null)
                    {
                        Debug.LogError($"Gameplay 住户表初始化失败：第 {index} 条住户配置为空。");
                        continue;
                    }

                    if (_customerMap.ContainsKey(customerRefData.id))
                    {
                        Debug.LogError($"Gameplay 住户表初始化失败：重复的 customerId={customerRefData.id}");
                        continue;
                    }

                    _customerMap.Add(customerRefData.id, customerRefData);
                }
            }

            _ruleMap.Clear();
            List<RuleRefData> ruleRefDataList = SCRefDataMgr.instance.ruleRefList?.refDataList;
            if (ruleRefDataList != null)
            {
                for (int index = 0; index < ruleRefDataList.Count; index++)
                {
                    RuleRefData ruleRefData = ruleRefDataList[index];
                    if (ruleRefData == null)
                    {
                        Debug.LogError($"Gameplay 规则表初始化失败：第 {index} 条规则配置为空。");
                        continue;
                    }

                    if (_ruleMap.ContainsKey(ruleRefData.id))
                    {
                        Debug.LogError($"Gameplay 规则表初始化失败：重复的 ruleId={ruleRefData.id}");
                        continue;
                    }

                    _ruleMap.Add(ruleRefData.id, ruleRefData);
                }
            }

            _levelSequence.Clear();
            List<LevelRefData> levelRefDataList = SCRefDataMgr.instance.levelRefList?.refDataList;
            if (levelRefDataList != null)
            {
                for (int index = 0; index < levelRefDataList.Count; index++)
                {
                    LevelRefData levelRefData = levelRefDataList[index];
                    if (levelRefData == null)
                    {
                        Debug.LogError($"Gameplay 关卡序列初始化失败：第 {index} 条关卡配置为空。");
                        continue;
                    }

                    _levelSequence.Add(levelRefData);
                }
            }
        }

        private void InitializeLevelRuntime()
        {
            _currentLevelRefData = FindInitialLevelRefData();
            _currentDayIndex = FindLevelIndex(_currentLevelRefData);
            StartCurrentDay();
        }

        private void StartCurrentDay()
        {
            _currentRuleIdList = _currentLevelRefData != null && _currentLevelRefData.ruleIdList != null
                ? new List<long>(_currentLevelRefData.ruleIdList)
                : new List<long>();

            _pendingCustomerIds.Clear();
            if (_currentLevelRefData != null && _currentLevelRefData.customerIdList != null)
            {
                for (int index = 0; index < _currentLevelRefData.customerIdList.Count; index++)
                    _pendingCustomerIds.Enqueue(_currentLevelRefData.customerIdList[index]);
            }

            _currentFloor = 1;
            _selectedFloor = 0;
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _isSettlementShowing = false;
            _isTransitionPlaying = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;

            SetCurrentFloorDisplayInstant(_currentFloor);
            SetElevatorDoorClosedInstant();
            SetCustomerHiddenInstant();
            AdvanceToNextCustomer();
            OpenElevatorDoor();
        }

        private LevelRefData FindInitialLevelRefData()
        {
            List<LevelRefData> levelRefDataList = SCRefDataMgr.instance.levelRefList?.refDataList;
            if (levelRefDataList == null || levelRefDataList.Count == 0)
            {
                Debug.LogError("Gameplay 初始化失败：关卡表为空。");
                return null;
            }

            for (int index = 0; index < levelRefDataList.Count; index++)
            {
                LevelRefData levelRefData = levelRefDataList[index];
                if (levelRefData != null && levelRefData.id == DefaultLevelId)
                    return levelRefData;
            }

            Debug.LogError($"Gameplay 未找到默认关卡 id={DefaultLevelId}，将回退到第一条关卡配置。");
            return levelRefDataList[0];
        }

        private void BindButtons()
        {
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button numberButton = mono.numberButtons[index];
                if (numberButton != null)
                    numberButton.AddMouseLeftClickDown(OnNumberButtonClicked);
            }

            mono.btnOption1?.AddMouseLeftClickDown(OnOption1Clicked);
            mono.btnOption2?.AddMouseLeftClickDown(OnOption2Clicked);
            mono.btnReject?.AddMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.AddMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.AddMouseLeftClickDown(OnCloseDoorClicked);
            mono.dialogueLeftArea?.AddMouseLeftClickDown(OnDialogueAreaClicked);
            mono.dialogueRightArea?.AddMouseLeftClickDown(OnDialogueAreaClicked);
        }

        private void UnbindButtons()
        {
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button numberButton = mono.numberButtons[index];
                if (numberButton != null)
                    numberButton.RemoveMouseLeftClickDown(OnNumberButtonClicked);
            }

            mono.btnOption1?.RemoveMouseLeftClickDown(OnOption1Clicked);
            mono.btnOption2?.RemoveMouseLeftClickDown(OnOption2Clicked);
            mono.btnReject?.RemoveMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.RemoveMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.RemoveMouseLeftClickDown(OnCloseDoorClicked);
            mono.dialogueLeftArea?.RemoveMouseLeftClickDown(OnDialogueAreaClicked);
            mono.dialogueRightArea?.RemoveMouseLeftClickDown(OnDialogueAreaClicked);
        }

        private void AdvanceToNextCustomer()
        {
            _currentCustomerRefData = null;
            _currentNeedRefData = null;
            _currentDialogueRefData = null;
            _option1DialogueRefData = null;
            _option2DialogueRefData = null;
            _selectedFloor = 0;
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _isTransitionPlaying = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;

            while (_pendingCustomerIds.Count > 0)
            {
                long customerId = _pendingCustomerIds.Dequeue();
                CustomerRefData customerRefData = GetCustomerRefData(customerId);
                if (customerRefData == null)
                    continue;

                CustomerNeedRefData needRefData = FindNeedRefDataForCurrentLevel(customerRefData);
                if (needRefData == null)
                {
                    Debug.LogError($"Gameplay 住户初始化失败：customerId={customerId} 未找到当前关卡需求配置。");
                    continue;
                }

                _currentCustomerRefData = customerRefData;
                _currentNeedRefData = needRefData;

                long dialogueStartId = GetDialogueStartId(needRefData);
                if (dialogueStartId > 0)
                    StartDialogue(dialogueStartId);
                else
                    Debug.LogError($"Gameplay 对话初始化失败：needId={needRefData.id} 未找到有效的开始对话 id。");

                ShowCurrentCustomer();
                return;
            }

            mono.txtCustomerName.text = "本关已完成";
            mono.txtAnimalInfo.text = "当前没有新的住户。";
            mono.txtBottomHint.text = "本关住户处理完成。";
            mono.dialogueSection?.SetActive(false);
            SetCustomerHiddenInstant();
            ShowSettlementPanel();
        }

        private CustomerRefData GetCustomerRefData(long customerId)
        {
            if (_customerMap.TryGetValue(customerId, out CustomerRefData customerRefData))
                return customerRefData;

            Debug.LogError($"Gameplay 获取住户配置失败：未找到 customerId={customerId}");
            return null;
        }

        private CustomerNeedRefData FindNeedRefDataForCurrentLevel(CustomerRefData customerRefData)
        {
            if (customerRefData == null || customerRefData.needList == null)
                return null;

            for (int index = 0; index < customerRefData.needList.Count; index++)
            {
                NeedEffectData needEffectData = customerRefData.needList[index];
                if (needEffectData == null)
                {
                    Debug.LogError($"Gameplay 住户需求解析失败：customerId={customerRefData.id} 的 needList 第 {index} 项为空。");
                    continue;
                }

                if (_currentLevelRefData != null && needEffectData.levelId != _currentLevelRefData.id)
                    continue;

                CustomerNeedRefData needRefData = CustomerNeedMgr.instance.GetNeedRefData(needEffectData.needId);
                if (needRefData != null)
                    return needRefData;
            }

            return null;
        }

        private long GetDialogueStartId(CustomerNeedRefData needRefData)
        {
            if (needRefData == null || needRefData.dialogueList == null || needRefData.dialogueList.Count == 0)
                return 0;

            DialogueEffectData dialogueEffectData = needRefData.dialogueList[0];
            if (dialogueEffectData == null)
            {
                Debug.LogError($"Gameplay 对话入口解析失败：needId={needRefData.id} 的第一条 dialogueList 为空。");
                return 0;
            }

            return dialogueEffectData.dialogueStartId;
        }

        private void StartDialogue(long dialogueStartId)
        {
            _option1DialogueRefData = null;
            _option2DialogueRefData = null;
            _isDialogueRunning = true;
            SetCurrentDialogue(dialogueStartId);
        }

        private void SetCurrentDialogue(long dialogueId)
        {
            if (!_dialogueMap.TryGetValue(dialogueId, out DialogueRefData dialogueRefData))
            {
                Debug.LogError($"Gameplay 获取对话配置失败：未找到 dialogueId={dialogueId}");
                EndDialogue();
                return;
            }

            _currentDialogueRefData = dialogueRefData;
            _currentAffectValue += dialogueRefData.affectValue;

            if (dialogueRefData.dialogueType == EDialogueType.SELECT)
            {
                ShowDialogueSelectionFromNode(dialogueRefData);
                return;
            }

            _option1DialogueRefData = null;
            _option2DialogueRefData = null;
            if (dialogueRefData.nextList != null && dialogueRefData.nextList.Count == 2)
                PrepareSelection(dialogueRefData);

            RefreshAllUi();
        }

        private void PrepareSelection(DialogueRefData dialogueRefData)
        {
            if (dialogueRefData == null || dialogueRefData.nextList == null || dialogueRefData.nextList.Count != 2)
                return;

            _option1DialogueRefData = GetDialogueRefData(dialogueRefData.nextList[0]);
            _option2DialogueRefData = GetDialogueRefData(dialogueRefData.nextList[1]);

            if (_option1DialogueRefData == null || _option2DialogueRefData == null)
            {
                Debug.LogError($"Gameplay 对话分支初始化失败：dialogueId={dialogueRefData.id} 的分支节点不完整。");
                _option1DialogueRefData = null;
                _option2DialogueRefData = null;
                return;
            }

            if (_option1DialogueRefData.dialogueType != EDialogueType.SELECT || _option2DialogueRefData.dialogueType != EDialogueType.SELECT)
                Debug.LogError($"Gameplay 对话分支结构异常：dialogueId={dialogueRefData.id} 的 nextList 期望指向 SELECT 节点。");
        }

        private DialogueRefData GetDialogueRefData(long dialogueId)
        {
            if (_dialogueMap.TryGetValue(dialogueId, out DialogueRefData dialogueRefData))
                return dialogueRefData;

            Debug.LogError($"Gameplay 获取对话节点失败：未找到 dialogueId={dialogueId}");
            return null;
        }

        private void ShowDialogueSelectionFromNode(DialogueRefData dialogueRefData)
        {
            _currentDialogueRefData = dialogueRefData;
            RefreshAllUi();
        }

        private void EndDialogue()
        {
            _isDialogueRunning = false;
            _currentDialogueRefData = null;
            _option1DialogueRefData = null;
            _option2DialogueRefData = null;

            // TODO: 这里可以接 CustomerNeedMgr.EvaluateDialogue，根据当前好感度触发额外需求对话。

            RefreshAllUi();
        }

        private void RefreshAllUi()
        {
            RefreshFloorUi();
            RefreshRuleUi();
            RefreshCustomerUi();
            RefreshBottomHintUi();
            RefreshDialogueUi();
            RefreshActionUi();
            RefreshNumberButtonUi();
        }

        private void RefreshFloorUi()
        {
            if (mono.txtCurrentFloor != null)
                mono.txtCurrentFloor.text = $"当前楼层：{_currentFloor}";

            if (mono.txtTime != null)
                mono.txtTime.text = "10 : 29（时间）";
        }

        private void RefreshRuleUi()
        {
            if (mono.txtNoticeBoard01 == null)
                return;

            if (_currentRuleIdList == null || _currentRuleIdList.Count == 0)
            {
                mono.txtNoticeBoard01.text = "本关暂无规则";
                if (mono.txtNoticeBoard02 != null)
                    mono.txtNoticeBoard02.text = "告示2";
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < _currentRuleIdList.Count; index++)
            {
                long ruleId = _currentRuleIdList[index];
                if (!_ruleMap.TryGetValue(ruleId, out RuleRefData ruleRefData))
                {
                    Debug.LogError($"Gameplay 获取规则配置失败：未找到 ruleId={ruleId}");
                    continue;
                }

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(ruleRefData.ruleName);
                if (!string.IsNullOrEmpty(ruleRefData.ruleDesc))
                {
                    builder.AppendLine();
                    builder.Append(ruleRefData.ruleDesc);
                }
            }

            mono.txtNoticeBoard01.text = builder.Length > 0 ? builder.ToString() : "本关暂无可显示规则";
            if (mono.txtNoticeBoard02 != null)
                mono.txtNoticeBoard02.text = "告示2";
        }

        private void RefreshCustomerUi()
        {
            if (_currentCustomerRefData == null)
                return;

            if (mono.txtCustomerName != null)
                mono.txtCustomerName.text = _currentCustomerRefData.customerName;

            if (mono.txtAnimalInfo != null)
                mono.txtAnimalInfo.text = BuildCustomerInfoText(_currentCustomerRefData);
        }

        private string BuildCustomerInfoText(CustomerRefData customerRefData)
        {
            if (customerRefData == null)
                return "动物扫脸信息";

            if (customerRefData.customerInfoList == null || customerRefData.customerInfoList.Count == 0)
                return customerRefData.customerName;

            return string.Join("\n", customerRefData.customerInfoList);
        }

        private void RefreshBottomHintUi()
        {
            if (mono.txtBottomHint == null)
                return;

            if (_canCloseDoor)
            {
                mono.txtBottomHint.text = "当前住户已处理完成，可以点击关门送走。";
                return;
            }

            if (_isDialogueRunning)
            {
                mono.txtBottomHint.text = "点击对话框继续推进对话。";
                return;
            }

            if (_selectedFloor > 0)
            {
                mono.txtBottomHint.text = $"当前已选择楼层：{_selectedFloor} 楼。";
                return;
            }

            mono.txtBottomHint.text =
                "动物信息档案，点击弹出（用于确认动物的档案，\n" +
                "如果与所住房屋不符合，需要拒绝或询问原因）";
        }

        private void RefreshDialogueUi()
        {
            if (mono.dialogueSection == null)
                return;

            bool hasCurrentDialogue = _isDialogueRunning && _currentDialogueRefData != null;
            mono.dialogueSection.SetActive(hasCurrentDialogue);
            if (!hasCurrentDialogue)
                return;

            bool isPlayerDialogue = IsPlayerDialogue(_currentDialogueRefData);
            string currentContent = _currentDialogueRefData.content;

            mono.txtDialogueLeft.text = isPlayerDialogue ? string.Empty : currentContent;
            mono.txtDialogueRight.text = isPlayerDialogue ? currentContent : string.Empty;

            if (mono.dialogueLeftArea != null)
            {
                mono.dialogueLeftArea.raycastTarget = !isPlayerDialogue && CanAdvanceDialogueByClick();
                mono.dialogueLeftArea.enabled = !string.IsNullOrEmpty(mono.txtDialogueLeft.text);
            }

            if (mono.dialogueRightArea != null)
            {
                mono.dialogueRightArea.raycastTarget = isPlayerDialogue && CanAdvanceDialogueByClick();
                mono.dialogueRightArea.enabled = !string.IsNullOrEmpty(mono.txtDialogueRight.text);
            }

            bool showOptions = _option1DialogueRefData != null && _option2DialogueRefData != null;
            mono.btnOption1?.gameObject.SetActive(showOptions);
            mono.btnOption2?.gameObject.SetActive(showOptions);

            if (showOptions)
            {
                SetButtonText(mono.btnOption1, _option1DialogueRefData.content);
                SetButtonText(mono.btnOption2, _option2DialogueRefData.content);
            }
        }

        private void RefreshActionUi()
        {
            bool canOperateMainButtons = !_isDialogueRunning && _currentCustomerRefData != null;
            if (_isTransitionPlaying)
                canOperateMainButtons = false;

            if (mono.btnReject != null)
                mono.btnReject.interactable = canOperateMainButtons && !_canCloseDoor;

            if (mono.btnConfirm != null)
                mono.btnConfirm.interactable = canOperateMainButtons && !_canCloseDoor;

            if (mono.btnCloseDoor != null)
                mono.btnCloseDoor.interactable = canOperateMainButtons && _canCloseDoor;
        }

        private void RefreshNumberButtonUi()
        {
            bool canSelectFloor = !_isDialogueRunning && !_canCloseDoor && !_isTransitionPlaying;
            for (int index = 0; index < mono.numberButtons.Count; index++)
            {
                Button button = mono.numberButtons[index];
                if (button == null)
                    continue;

                button.interactable = canSelectFloor;
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage == null)
                    continue;

                int floor = GetFloorFromButton(button);
                buttonImage.color = floor == _selectedFloor
                    ? new Color(0.74f, 0.88f, 0.76f, 1f)
                    : new Color(0.89f, 0.89f, 0.89f, 1f);
            }
        }

        private bool IsPlayerDialogue(DialogueRefData dialogueRefData)
        {
            return dialogueRefData != null && dialogueRefData.name == PlayerSpeakerName;
        }

        private bool CanAdvanceDialogueByClick()
        {
            if (_currentDialogueRefData == null)
                return false;

            if (_option1DialogueRefData != null || _option2DialogueRefData != null)
                return false;

            if (_currentDialogueRefData.flagType == EDialogueFlagType.END)
                return true;

            return _currentDialogueRefData.nextList != null && _currentDialogueRefData.nextList.Count == 1;
        }

        private void OnDialogueAreaClicked(PointerEventData eventData, object[] args)
        {
            if (!_isDialogueRunning || _currentDialogueRefData == null)
                return;

            if (!CanAdvanceDialogueByClick())
                return;

            if (_currentDialogueRefData.flagType == EDialogueFlagType.END)
            {
                EndDialogue();
                return;
            }

            SetCurrentDialogue(_currentDialogueRefData.nextList[0]);
        }

        private void OnNumberButtonClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能选楼层。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能选楼层。");
                return;
            }

            Button clickedButton = ResolveClickedButton(eventData);
            if (clickedButton == null)
            {
                Debug.LogError("Gameplay 选楼失败：未获取到点击的楼层按钮。");
                return;
            }

            _selectedFloor = GetFloorFromButton(clickedButton);
            SCDebugHelper.Log($"点击动物编号按钮：{clickedButton.name}，当前选择楼层={_selectedFloor}");
            RefreshAllUi();
        }

        private int GetFloorFromButton(Button button)
        {
            if (button == null)
                return 0;

            GameplayFloorButton floorButton = button.GetComponent<GameplayFloorButton>();
            if (floorButton != null)
                return floorButton.floor;

            string digits = string.Empty;
            for (int index = 0; index < button.name.Length; index++)
            {
                char current = button.name[index];
                if (char.IsDigit(current))
                    digits += current;
            }

            if (int.TryParse(digits, out int floor))
                return floor;

            Debug.LogError($"Gameplay 解析楼层按钮失败：buttonName={button.name}");
            return 0;
        }

        private Button ResolveClickedButton(PointerEventData eventData)
        {
            if (eventData == null)
                return null;

            if (eventData.pointerPress != null)
            {
                Button button = eventData.pointerPress.GetComponent<Button>();
                if (button != null)
                    return button;

                button = eventData.pointerPress.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                Button button = eventData.pointerCurrentRaycast.gameObject.GetComponent<Button>();
                if (button != null)
                    return button;

                button = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            if (eventData.pointerEnter != null)
            {
                Button button = eventData.pointerEnter.GetComponent<Button>();
                if (button != null)
                    return button;

                button = eventData.pointerEnter.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            return null;
        }

        private void OnOption1Clicked(PointerEventData eventData, object[] args)
        {
            HandleDialogueOptionSelection(_option1DialogueRefData);
        }

        private void OnOption2Clicked(PointerEventData eventData, object[] args)
        {
            HandleDialogueOptionSelection(_option2DialogueRefData);
        }

        private void HandleDialogueOptionSelection(DialogueRefData optionDialogueRefData)
        {
            if (optionDialogueRefData == null)
            {
                Debug.LogError("Gameplay 选择对话选项失败：选项节点为空。");
                return;
            }

            _currentAffectValue += optionDialogueRefData.affectValue;
            mono.txtDialogueLeft.text = string.Empty;
            mono.txtDialogueRight.text = optionDialogueRefData.content;

            if (optionDialogueRefData.nextList == null || optionDialogueRefData.nextList.Count == 0 || optionDialogueRefData.nextList[0] == 0)
            {
                EndDialogue();
                return;
            }

            SetCurrentDialogue(optionDialogueRefData.nextList[0]);
        }

        private void OnRejectClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能拒绝。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能拒绝。");
                return;
            }

            if (_currentNeedRefData == null)
            {
                Debug.LogError("Gameplay 拒绝失败：当前住户需求配置为空。");
                return;
            }

            _lastRuleEffectData = RuleMgr.instance.EvaluateRuleList(_currentRuleIdList, EElevatorOperator.REFUSE, 0);
            if (_lastRuleEffectData != null && _lastRuleEffectData.effectType == ERuleEffectType.FORBID_REFUSE)
            {
                SCDebugHelper.Log("当前规则禁止拒绝该住户。");
                RefreshBottomHintUi();
                return;
            }

            _lastJudgmentEffectData = CustomerNeedMgr.instance.EvaluateRefuse(_currentNeedRefData.id);
            _canCloseDoor = _lastJudgmentEffectData != null;
            if (_canCloseDoor)
                SCDebugHelper.Log($"已拒绝当前住户，影响值={_lastJudgmentEffectData.affectValue}");

            RefreshAllUi();
        }

        private void OnConfirmClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能确认。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能确认。");
                return;
            }

            if (_currentNeedRefData == null)
            {
                Debug.LogError("Gameplay 确认失败：当前住户需求配置为空。");
                return;
            }

            if (_selectedFloor <= 0)
            {
                Debug.LogError("Gameplay 确认失败：当前尚未选择目标楼层。");
                return;
            }

            int finalFloor = _selectedFloor;
            _lastRuleEffectData = RuleMgr.instance.EvaluateRuleList(_currentRuleIdList, EElevatorOperator.GOTO, _selectedFloor);
            if (_lastRuleEffectData != null)
            {
                if (RuleMgr.instance.IsGotoBlocked(_lastRuleEffectData))
                {
                    SCDebugHelper.Log($"当前规则禁止前往 {_selectedFloor} 楼。");
                    RefreshAllUi();
                    return;
                }

                if (RuleMgr.instance.TryGetRedirectFloor(_lastRuleEffectData, out int redirectFloor))
                {
                    finalFloor = redirectFloor;
                    SCDebugHelper.Log($"规则将楼层从 {_selectedFloor} 改写为 {finalFloor}。");
                }
            }

            _lastJudgmentEffectData = CustomerNeedMgr.instance.EvaluateGoto(_currentNeedRefData.id, finalFloor);
            _canCloseDoor = _lastJudgmentEffectData != null;
            if (_canCloseDoor)
                SCDebugHelper.Log($"已确认前往 {finalFloor} 楼，影响值={_lastJudgmentEffectData.affectValue}");

            RefreshAllUi();
            if (_canCloseDoor)
                StartCustomerDepartureFlow(finalFloor, true);
        }

        private void OnCloseDoorClicked(PointerEventData eventData, object[] args)
        {
            if (_isDialogueRunning)
            {
                SCDebugHelper.Log("当前对话未结束，暂时不能关门。");
                return;
            }

            if (_isTransitionPlaying)
            {
                SCDebugHelper.Log("当前电梯正在移动，暂时不能重复关门。");
                return;
            }

            if (!_canCloseDoor)
            {
                Debug.LogError("Gameplay 关门失败：当前住户尚未完成处理，不能关门。");
                return;
            }

            int targetFloor = ResolveCustomerDepartureFloor();
            StartCustomerDepartureFlow(targetFloor, false);
        }

        private void ShowSettlementPanel()
        {
            if (_isSettlementShowing || UINodeMgr.instance.GetNodeByName(SettlementNodeName) != null)
                return;

            _isSettlementShowing = true;
            UIPanelSettlement.pendingTitle = $"第 {_currentDayIndex + 1} 天结算";
            UIPanelSettlement.pendingSummary = $"第 {_currentDayIndex + 1} 天的客人已经全部接待完成。";
            UIPanelSettlement.onNextDayClicked = HandleNextDayClicked;

            UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoSettlement, UIPanelSettlement>(
                SCUIShowType.ADDITION,
                "panel_settlement",
                SettlementNodeName,
                false,
                false,
                false,
                false));
        }

        private void HandleNextDayClicked()
        {
            int nextDayIndex = _currentDayIndex + 1;
            if (nextDayIndex >= _levelSequence.Count)
            {
                SCDebugHelper.Log("所有天数都已完成，游戏结束。");
                return;
            }

            _currentDayIndex = nextDayIndex;
            _currentLevelRefData = _levelSequence[_currentDayIndex];
            StartCurrentDay();
            RefreshAllUi();
        }

        private int FindLevelIndex(LevelRefData targetLevelRefData)
        {
            if (targetLevelRefData == null)
                return 0;

            for (int index = 0; index < _levelSequence.Count; index++)
            {
                if (_levelSequence[index] != null && _levelSequence[index].id == targetLevelRefData.id)
                    return index;
            }

            return 0;
        }

        private int ResolveCustomerDepartureFloor()
        {
            if (_lastJudgmentEffectData != null && _lastJudgmentEffectData.elevatorOperator == EElevatorOperator.GOTO)
            {
                if (_lastRuleEffectData != null && RuleMgr.instance.TryGetRedirectFloor(_lastRuleEffectData, out int redirectFloor))
                    return redirectFloor;

                if (_selectedFloor > 0)
                    return _selectedFloor;
            }

            return 1;
        }

        private void StartCustomerDepartureFlow(int targetFloor, bool needBoardAnimation)
        {
            _isTransitionPlaying = true;
            RefreshAllUi();
            TweenCallback afterBoardAction = () =>
            {
                PlayElevatorTravelToFloor(targetFloor, () =>
                {
                    OpenElevatorDoor(() =>
                    {
                        TweenCallback afterArriveLeave = () =>
                        {
                            CloseElevatorDoor(() =>
                            {
                                PlayElevatorTravelToFloor(1, () =>
                                {
                                    OpenElevatorDoor(() =>
                                    {
                                        AdvanceToNextCustomer();
                                        _isTransitionPlaying = false;
                                        RefreshAllUi();
                                    });
                                });
                            });
                        };

                        if (needBoardAnimation)
                            PlayCustomerLeaveElevator(afterArriveLeave);
                        else
                            HideCurrentCustomer(afterArriveLeave);
                    });
                });
            };

            if (needBoardAnimation)
            {
                PlayCustomerBoardElevator(() =>
                {
                    CloseElevatorDoor(afterBoardAction);
                });
                return;
            }

            CloseElevatorDoor(() =>
            {
                HideCurrentCustomer(afterBoardAction);
            });
        }

        public void OpenElevatorDoor(TweenCallback onComplete = null)
        {
            Tween tween = mono.elevatorDoorEffect?.PlayOpen(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }

        public void CloseElevatorDoor(TweenCallback onComplete = null)
        {
            Tween tween = mono.elevatorDoorEffect?.PlayClose(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }

        private void SetElevatorDoorClosedInstant()
        {
            mono.elevatorDoorEffect?.SetClosedInstant();
        }

        private void ShowCurrentCustomer()
        {
            if (_currentCustomerRefData == null)
                return;

            mono.customerMoveScaleEffect?.SetOriginInstant();
            Tween tween = mono.customerFadeEffect?.PlayFadeIn();
            if (tween == null && mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 1f;
        }

        private void HideCurrentCustomer(TweenCallback onComplete = null)
        {
            Tween tween = mono.customerFadeEffect?.PlayFadeOut(onComplete);
            if (tween == null)
            {
                if (mono.customerCanvasGroup != null)
                    mono.customerCanvasGroup.alpha = 0f;
                onComplete?.Invoke();
            }
        }

        private void SetCustomerHiddenInstant()
        {
            mono.customerMoveScaleEffect?.SetOriginInstant();
            mono.customerFadeEffect?.SetVisibleInstant(false);
            if (mono.customerCanvasGroup != null)
                mono.customerCanvasGroup.alpha = 0f;
        }

        private void PlayCustomerBoardElevator(TweenCallback onComplete = null)
        {
            mono.customerMoveScaleEffect?.SetOriginInstant();
            mono.customerFadeEffect?.SetVisibleInstant(true);
            Tween moveTween = mono.customerMoveScaleEffect?.PlayEnter();
            Tween fadeTween = mono.customerFadeEffect?.PlayFadeOut();

            if (moveTween == null && fadeTween == null)
            {
                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (moveTween != null)
                sequence.Join(moveTween);
            if (fadeTween != null)
                sequence.Join(fadeTween);
            if (onComplete != null)
                sequence.OnComplete(onComplete);
        }

        private void PlayCustomerLeaveElevator(TweenCallback onComplete = null)
        {
            mono.customerMoveScaleEffect?.SetEnterStateInstant();
            mono.customerFadeEffect?.SetVisibleInstant(true);
            Tween moveTween = mono.customerMoveScaleEffect?.PlayExit();
            Tween fadeTween = mono.customerFadeEffect?.PlayFadeOut();

            if (moveTween == null && fadeTween == null)
            {
                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            if (moveTween != null)
                sequence.Join(moveTween);
            if (fadeTween != null)
                sequence.Join(fadeTween);
            if (onComplete != null)
                sequence.OnComplete(onComplete);
        }

        private void PlayElevatorTravelToFloor(int targetFloor, TweenCallback onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            int fromFloor = _currentFloor;
            if (fromFloor != targetFloor)
            {
                sequence.AppendCallback(() => AnimateFloorText(targetFloor));
                Tween shakeTween = mono.elevatorTravelShakeEffect?.Play();
                if (shakeTween != null)
                    sequence.Join(shakeTween);
            }

            sequence.AppendInterval(0.08f);
            sequence.AppendCallback(() =>
            {
                _currentFloor = targetFloor;
                RefreshFloorUi();
                onComplete?.Invoke();
            });
        }

        private void AnimateFloorText(int floor)
        {
            string text = $"当前楼层：{floor}";
            if (mono.floorTextEffect != null)
            {
                mono.floorTextEffect.PlayTextChange(text);
                return;
            }

            if (mono.txtCurrentFloor != null)
                mono.txtCurrentFloor.text = text;
        }

        private void SetCurrentFloorDisplayInstant(int floor)
        {
            _currentFloor = floor;
            string text = $"当前楼层：{floor}";
            if (mono.floorTextEffect != null)
                mono.floorTextEffect.SetTextInstant(text);
            else if (mono.txtCurrentFloor != null)
                mono.txtCurrentFloor.text = text;
        }

        private void SetButtonText(Button button, string content)
        {
            if (button == null)
                return;

            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = content;
        }

        private Text FindChildText(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            if (target == null)
                return null;

            return target.GetComponent<Text>();
        }

        private Image FindChildImage(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            if (target == null)
                return null;

            return target.GetComponent<Image>();
        }

        private RectTransform FindChildRect(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            if (target == null)
                return null;

            return target as RectTransform;
        }

        private Transform FindChildTransformRecursive(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            if (parent.name == objectName)
                return parent;

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                Transform result = FindChildTransformRecursive(child, objectName);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
