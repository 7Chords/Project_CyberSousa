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
        private const string EndingNodeName = "UINodeEnding";
        private const float CustomerFadeDuration = 0.30f;
        private const float ElevatorTravelAnimDuration = 0.90f;

        private readonly Dictionary<long, DialogueRefData> _dialogueMap = new Dictionary<long, DialogueRefData>();
        private readonly Dictionary<long, CustomerRefData> _customerMap = new Dictionary<long, CustomerRefData>();
        private readonly Dictionary<long, RuleRefData> _ruleMap = new Dictionary<long, RuleRefData>();
        private readonly List<LevelRefData> _levelSequence = new List<LevelRefData>();
        private readonly Queue<long> _pendingCustomerIds = new Queue<long>();

        private LevelRefData _currentLevelRefData;
        private CustomerRefData _currentCustomerRefData;
        private CustomerNeedRefData _currentNeedRefData;
        private DialogueRefData _currentDialogueRefData;
        private readonly List<DialogueRefData> _currentOptionDialogueList = new List<DialogueRefData>();
        private readonly List<UIMonoDialogueOption> _dialogueOptionItemPool = new List<UIMonoDialogueOption>();
        private GameObject _dialogueOptionItemPrefab;
        private bool _dialogueOptionPrefabReady;
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
        private string _serviceFeedbackText;
        private string _activeDialoguePortraitResName;
        private readonly Dictionary<string, Sprite> _portraitSpriteCache = new Dictionary<string, Sprite>();

        public UIPanelGameplayMain(UIMonoGameplayMain _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            GameTimeMgr.instance.OnTimeChanged += OnGameTimeChanged;
            InitializeRuntimeData();
            RefreshAllUi();
        }

        public override void BeforeDiscard()
        {
            GameTimeMgr.instance.OnTimeChanged -= OnGameTimeChanged;
            GameTimeMgr.instance.StopAdvanceTween();
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

            GameTimeMgr.instance.OnTimeChanged -= OnGameTimeChanged;
            GameTimeMgr.instance.OnTimeChanged += OnGameTimeChanged;
            BindButtons();
            RefreshAllUi();
        }

        private void OnGameTimeChanged()
        {
            RefreshClockUi();
            if (_currentCustomerRefData == null && !_isTransitionPlaying)
                TrySpawnNextPendingCustomer();
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

            EnsureNoticeBoardReferences();

            if (mono.txtCustomerName == null)
                mono.txtCustomerName = FindChildText("AnimalLabel");

            if (mono.dialogueLeftArea == null)
                mono.dialogueLeftArea = FindChildImage("Dialogue01");

            if (mono.dialogueRightArea == null)
                mono.dialogueRightArea = FindChildImage("Dialogue02");

            if (mono.imgDialoguePortrait == null)
                mono.imgDialoguePortrait = FindChildImage("DialoguePortrait");

            if (mono.elevatorView == null)
            {
                Transform elevatorRoot = FindChildTransformRecursive(mono.transform, "ElevatorRoot");
                if (elevatorRoot != null)
                    mono.elevatorView = elevatorRoot.GetComponent<UIElevatorView>();
            }

            mono.elevatorView?.EnsureReferences();

            if (mono.dialogueOptionRoot == null)
                mono.dialogueOptionRoot = FindChildRect("OptionSection");

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

            if (mono.floorTextEffect == null && mono.txtCurrentFloor != null)
                mono.floorTextEffect = mono.txtCurrentFloor.GetComponent<UITextChangeMoveEffect>();

            if (mono.txtClockTime == null)
                mono.txtClockTime = FindChildText("ClockTimePanel_Text");

            if (mono.btnSetting == null)
            {
                Transform btnSettingTransform = FindChildTransformRecursive(mono.transform, "BtnSetting");
                if (btnSettingTransform != null)
                    mono.btnSetting = btnSettingTransform.GetComponent<Button>();
            }
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
            _currentDayIndex = Mathf.Clamp(GamePlayerDataMgr.instance.startDayIndex, 0, Mathf.Max(0, _levelSequence.Count - 1));
            _currentLevelRefData = _levelSequence.Count > 0 ? _levelSequence[_currentDayIndex] : FindInitialLevelRefData();
            StartCurrentDay();
        }

        private void StartCurrentDay()
        {
            _currentRuleIdList = _currentLevelRefData != null && _currentLevelRefData.ruleIdList != null
                ? new List<long>(_currentLevelRefData.ruleIdList)
                : new List<long>();

            _pendingCustomerIds.Clear();
            if (_currentLevelRefData != null && _currentLevelRefData.customerEffectList != null)
            {
                List<long> customerIds = CustomerPoolMgr.instance.ResolveCustomerIds(_currentLevelRefData.customerEffectList);
                for (int index = 0; index < customerIds.Count; index++)
                    _pendingCustomerIds.Enqueue(customerIds[index]);
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
            _serviceFeedbackText = null;

            SetCurrentFloorDisplayInstant(_currentFloor);
            GameTimeMgr.instance.ResetToDayStart();
            SetElevatorDoorClosedInstant();
            SetCustomerHiddenInstant();

            if (!TryGetPeekPendingNeedRefData(out CustomerNeedRefData firstNeedRefData))
            {
                ShowDayCompleteState();
                return;
            }

            _isTransitionPlaying = true;
            GameTimeMgr.instance.SetClockTime(firstNeedRefData.needTime);
            int firstFloor = firstNeedRefData.needFloor > 0 ? firstNeedRefData.needFloor : 1;
            PlayElevatorTravelToFloor(firstFloor, () =>
            {
                OpenElevatorDoor(() =>
                {
                    TrySpawnNextPendingCustomer();
                    _isTransitionPlaying = false;
                    RefreshAllUi();
                });
            });
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

            mono.btnReject?.AddMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.AddMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.AddMouseLeftClickDown(OnCloseDoorClicked);
            mono.btnSetting?.AddMouseLeftClickDown(OnSettingClicked);
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

            UnbindAllDialogueOptionItems();
            mono.btnReject?.RemoveMouseLeftClickDown(OnRejectClicked);
            mono.btnConfirm?.RemoveMouseLeftClickDown(OnConfirmClicked);
            mono.btnCloseDoor?.RemoveMouseLeftClickDown(OnCloseDoorClicked);
            mono.btnSetting?.RemoveMouseLeftClickDown(OnSettingClicked);
            mono.dialogueLeftArea?.RemoveMouseLeftClickDown(OnDialogueAreaClicked);
            mono.dialogueRightArea?.RemoveMouseLeftClickDown(OnDialogueAreaClicked);
        }

        private void TrySpawnNextPendingCustomer()
        {
            if (_currentCustomerRefData != null)
                return;

            if (_pendingCustomerIds.Count == 0)
            {
                ShowDayCompleteState();
                return;
            }

            long customerId = _pendingCustomerIds.Peek();
            CustomerRefData customerRefData = GetCustomerRefData(customerId);
            if (customerRefData == null)
            {
                _pendingCustomerIds.Dequeue();
                TrySpawnNextPendingCustomer();
                return;
            }

            CustomerNeedRefData needRefData = FindNeedRefDataForCurrentLevel(customerRefData);
            if (needRefData == null)
            {
                Debug.LogError($"Gameplay 住户初始化失败：customerId={customerId} 未找到当前关卡需求配置。");
                _pendingCustomerIds.Dequeue();
                TrySpawnNextPendingCustomer();
                return;
            }

            if (!CustomerNeedMgr.instance.CanSpawnCustomerNeed(needRefData, _currentFloor))
            {
                if (GameTimeMgr.instance.HasReachedNeedTime(needRefData.needTime))
                {
                    StartPickupTravelToNextCustomer(needRefData, false);
                    return;
                }

                SetCustomerHiddenInstant();
                RefreshAllUi();
                return;
            }

            _pendingCustomerIds.Dequeue();
            SpawnCustomer(customerRefData, needRefData);
        }

        private void SpawnCustomer(CustomerRefData customerRefData, CustomerNeedRefData needRefData)
        {
            ClearDialoguePortraits();
            _selectedFloor = 0;
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;
            _serviceFeedbackText = null;
            _currentCustomerRefData = customerRefData;
            _currentNeedRefData = needRefData;

            long dialogueStartId = GetDialogueStartId(needRefData);
            if (dialogueStartId > 0)
                StartDialogue(dialogueStartId);
            else
                Debug.LogError($"Gameplay 对话初始化失败：needId={needRefData.id} 未找到有效的开始对话 id。");

            ShowCurrentCustomer();
            RefreshAllUi();
        }

        private void ShowDayCompleteState()
        {
            mono.txtCustomerName.text = "本关已完成";
            mono.txtAnimalInfo.text = "当前没有新的住户。";
            mono.txtBottomHint.text = "本关住户处理完成。";
            mono.dialogueSection?.SetActive(false);
            SetCustomerHiddenInstant();

            if (TryShowEndingOnDayComplete())
                return;

            ShowSettlementPanel();
        }

        private void FinishCurrentCustomerService()
        {
            ClearCurrentCustomerState();
            if (_pendingCustomerIds.Count == 0)
            {
                ShowDayCompleteState();
                return;
            }

            if (!TryGetPeekPendingNeedRefData(out CustomerNeedRefData nextNeedRefData))
            {
                ShowDayCompleteState();
                return;
            }

            StartPickupTravelToNextCustomer(nextNeedRefData, false);
        }

        private void ClearCurrentCustomerState()
        {
            ClearDialoguePortraits();
            _currentCustomerRefData = null;
            _currentNeedRefData = null;
            _currentDialogueRefData = null;
            ClearDialogueOptions();
            _selectedFloor = 0;
            _currentAffectValue = 0;
            _isDialogueRunning = false;
            _canCloseDoor = false;
            _lastJudgmentEffectData = null;
            _lastRuleEffectData = null;
            _serviceFeedbackText = null;
            SetCustomerHiddenInstant();
        }

        private void StartPickupTravelToNextCustomer(CustomerNeedRefData needRefData, bool prepareRejectTravelTime)
        {
            if (needRefData == null)
                return;

            int targetFloor = needRefData.needFloor > 0 ? needRefData.needFloor : 1;
            if (prepareRejectTravelTime)
            {
                if (_currentFloor != targetFloor)
                    GameTimeMgr.instance.SetClockTimeBeforeElevatorTravel(needRefData.needTime);
                else
                    GameTimeMgr.instance.SetClockTime(needRefData.needTime);
            }

            if (_currentFloor == targetFloor)
            {
                _isTransitionPlaying = false;
                TrySpawnNextPendingCustomer();
                RefreshAllUi();
                return;
            }

            _isTransitionPlaying = true;
            _selectedFloor = 0;
            RefreshAllUi();
            CloseElevatorDoor(() =>
            {
                PlayElevatorTravelToFloor(targetFloor, () =>
                {
                    OpenElevatorDoor(() =>
                    {
                        _isTransitionPlaying = false;
                        TrySpawnNextPendingCustomer();
                        RefreshAllUi();
                    });
                });
            });
        }

        private void StartRejectAndPickupNextFlow()
        {
            _isTransitionPlaying = true;
            RefreshAllUi();
            CloseElevatorDoor(() =>
            {
                HideCurrentCustomer(() =>
                {
                    ClearCurrentCustomerState();
                    if (!TryGetPeekPendingNeedRefData(out CustomerNeedRefData nextNeedRefData))
                    {
                        _isTransitionPlaying = false;
                        ShowDayCompleteState();
                        return;
                    }

                    StartPickupTravelToNextCustomer(nextNeedRefData, true);
                });
            });
        }

        private bool TryGetPeekPendingNeedRefData(out CustomerNeedRefData needRefData)
        {
            needRefData = null;
            if (_pendingCustomerIds.Count == 0)
                return false;

            long customerId = _pendingCustomerIds.Peek();
            CustomerRefData customerRefData = GetCustomerRefData(customerId);
            if (customerRefData == null)
                return false;

            needRefData = FindNeedRefDataForCurrentLevel(customerRefData);
            return needRefData != null;
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
            ClearDialogueOptions();
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
            AddCurrentCustomerFavor(dialogueRefData.affectValue);

            if (dialogueRefData.dialogueType == EDialogueType.SELECT)
            {
                ClearDialogueOptions();
                ShowDialogueSelectionFromNode(dialogueRefData);
                return;
            }

            ClearDialogueOptions();
            if (ShouldPrepareDialogueOptions(dialogueRefData))
                PrepareDialogueOptions(dialogueRefData);

            RefreshAllUi();
        }

        private bool ShouldPrepareDialogueOptions(DialogueRefData dialogueRefData)
        {
            if (dialogueRefData?.nextList == null || dialogueRefData.nextList.Count == 0)
                return false;

            for (int index = 0; index < dialogueRefData.nextList.Count; index++)
            {
                long nextDialogueId = dialogueRefData.nextList[index];
                if (nextDialogueId == 0)
                    continue;

                DialogueRefData nextDialogueRefData = GetDialogueRefData(nextDialogueId);
                if (nextDialogueRefData != null && nextDialogueRefData.dialogueType == EDialogueType.SELECT)
                    return true;
            }

            return false;
        }

        private void PrepareDialogueOptions(DialogueRefData dialogueRefData)
        {
            ClearDialogueOptions();
            if (dialogueRefData == null || dialogueRefData.nextList == null || dialogueRefData.nextList.Count == 0)
                return;

            for (int index = 0; index < dialogueRefData.nextList.Count; index++)
            {
                long nextDialogueId = dialogueRefData.nextList[index];
                if (nextDialogueId == 0)
                    continue;

                DialogueRefData optionDialogueRefData = GetDialogueRefData(nextDialogueId);
                if (optionDialogueRefData == null)
                    continue;

                if (optionDialogueRefData.dialogueType != EDialogueType.SELECT)
                {
                    Debug.LogError($"Gameplay 对话分支结构异常：dialogueId={dialogueRefData.id} 的 nextList[{index}]={nextDialogueId} 不是 SELECT 节点。");
                    continue;
                }

                _currentOptionDialogueList.Add(optionDialogueRefData);
            }

            if (_currentOptionDialogueList.Count == 0)
                Debug.LogError($"Gameplay 对话分支初始化失败：dialogueId={dialogueRefData.id} 没有可用的选项节点。");
        }

        private void ClearDialogueOptions()
        {
            _currentOptionDialogueList.Clear();
            HideAllDialogueOptionItems();
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
            ClearDialogueOptions();

            GameTimeMgr.instance.AddTimeInstant(_currentNeedRefData?.timeEffect);

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
                mono.txtTime.text = $"第 {_currentDayIndex + 1} 天";

            RefreshClockUi();
        }

        private void RefreshClockUi()
        {
            if (mono.txtClockTime != null)
                mono.txtClockTime.text = GameTimeMgr.instance.GetDisplayText();
        }

        private void RefreshRuleUi()
        {
            EnsureNoticeBoardReferences();

            List<NoticeBoardItem> noticeBoardList = mono.noticeBoardList;
            if (noticeBoardList == null || noticeBoardList.Count == 0)
            {
                Debug.LogError("Gameplay 告示板列表为空，无法刷新规则 UI。");
                return;
            }

            int ruleCount = _currentRuleIdList != null ? _currentRuleIdList.Count : 0;
            if (ruleCount == 0)
            {
                SetNoticeBoardVisible(noticeBoardList, 0, true, "本关暂无规则");
                for (int index = 1; index < noticeBoardList.Count; index++)
                    SetNoticeBoardVisible(noticeBoardList, index, false, null);
                return;
            }

            if (ruleCount > noticeBoardList.Count)
                Debug.LogError($"Gameplay 告示板数量不足：需要 {ruleCount} 个，当前只有 {noticeBoardList.Count} 个。请重新生成 panel_gameplay_main Prefab。");

            int visibleCount = Mathf.Min(ruleCount, noticeBoardList.Count);
            for (int index = 0; index < visibleCount; index++)
            {
                long ruleId = _currentRuleIdList[index];
                SetNoticeBoardVisible(noticeBoardList, index, true, BuildRuleDisplayText(ruleId));
            }

            for (int index = visibleCount; index < noticeBoardList.Count; index++)
                SetNoticeBoardVisible(noticeBoardList, index, false, null);
        }

        private string BuildRuleDisplayText(long ruleId)
        {
            if (!_ruleMap.TryGetValue(ruleId, out RuleRefData ruleRefData))
            {
                Debug.LogError($"Gameplay 获取规则配置失败：未找到 ruleId={ruleId}");
                return "规则配置缺失";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(ruleRefData.ruleName);
            if (!string.IsNullOrEmpty(ruleRefData.ruleDesc))
            {
                builder.AppendLine();
                builder.Append(ruleRefData.ruleDesc);
            }

            return builder.ToString();
        }

        private static void SetNoticeBoardVisible(List<NoticeBoardItem> noticeBoardList, int index, bool visible, string text)
        {
            NoticeBoardItem noticeBoardItem = noticeBoardList[index];
            if (noticeBoardItem.root != null)
                SCCommon.SetGameObjectEnable(noticeBoardItem.root, visible);
            else if (noticeBoardItem.text != null)
                SCCommon.SetGameObjectEnable(noticeBoardItem.text.gameObject, visible);

            if (visible && noticeBoardItem.text != null && text != null)
                noticeBoardItem.text.text = text;
        }

        private void EnsureNoticeBoardReferences()
        {
            if (mono.noticeBoardList == null)
                mono.noticeBoardList = new List<NoticeBoardItem>();

            for (int index = 0; index < mono.noticeBoardList.Count; index++)
            {
                NoticeBoardItem noticeBoardItem = mono.noticeBoardList[index];
                if (noticeBoardItem == null)
                    continue;

                if (noticeBoardItem.text == null && noticeBoardItem.root != null)
                    noticeBoardItem.text = noticeBoardItem.root.GetComponentInChildren<Text>(true);
            }

            if (mono.noticeBoardList.Count > 0)
                return;

            Transform leftBand = FindChildTransformRecursive(mono.transform, "LeftBand");
            if (leftBand == null)
            {
                Debug.LogError("Gameplay 未找到 LeftBand，无法自动收集告示板。");
                return;
            }

            Transform noticeBoardRoot = leftBand.Find("NoticeBoardRoot");
            if (noticeBoardRoot != null)
                CollectNoticeBoardsFromTransform(noticeBoardRoot);
            else
                CollectNoticeBoardsFromTransform(leftBand);
        }

        private void CollectNoticeBoardsFromTransform(Transform parent)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (!child.name.StartsWith("NoticeBoard"))
                    continue;

                if (TryGetNoticeBoardItem(child, out NoticeBoardItem noticeBoardItem))
                    mono.noticeBoardList.Add(noticeBoardItem);
            }

            mono.noticeBoardList.Sort((left, right) =>
            {
                string leftName = left.root != null ? left.root.name : string.Empty;
                string rightName = right.root != null ? right.root.name : string.Empty;
                return string.CompareOrdinal(leftName, rightName);
            });
        }

        private static bool TryGetNoticeBoardItem(Transform boardRoot, out NoticeBoardItem noticeBoardItem)
        {
            noticeBoardItem = null;
            if (boardRoot == null)
                return false;

            Transform textTransform = boardRoot.Find($"{boardRoot.name}_Text");
            Text text = textTransform != null ? textTransform.GetComponent<Text>() : boardRoot.GetComponentInChildren<Text>(true);
            noticeBoardItem = new NoticeBoardItem
            {
                root = boardRoot.gameObject,
                text = text
            };
            return true;
        }

        private void RefreshCustomerUi()
        {
            if (_currentCustomerRefData == null)
            {
                if (_pendingCustomerIds.Count > 0)
                {
                    CustomerRefData pendingCustomer = GetCustomerRefData(_pendingCustomerIds.Peek());
                    if (pendingCustomer != null)
                    {
                        if (mono.txtCustomerName != null)
                            mono.txtCustomerName.text = pendingCustomer.customerName;

                        if (mono.txtAnimalInfo != null)
                            mono.txtAnimalInfo.text = BuildCustomerInfoText(pendingCustomer);
                    }
                }

                return;
            }

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
                mono.txtBottomHint.text = string.IsNullOrEmpty(_serviceFeedbackText)
                    ? "当前住户已处理完成，可以点击关门送走。"
                    : $"{_serviceFeedbackText}\n点击关门送走当前住户。";
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

            if (TryGetPeekPendingNeedRefData(out CustomerNeedRefData pendingNeedRefData))
            {
                string needTimeText = GameTimeMgr.FormatClockTime(pendingNeedRefData.needTime);
                int needFloor = pendingNeedRefData.needFloor > 0 ? pendingNeedRefData.needFloor : _currentFloor;
                if (!GameTimeMgr.instance.HasReachedNeedTime(pendingNeedRefData.needTime))
                {
                    mono.txtBottomHint.text =
                        $"约 {needTimeText} 将在 {needFloor} 楼发出需求，请提前前往。";
                    return;
                }

                if (pendingNeedRefData.needFloor > 0 && _currentFloor != pendingNeedRefData.needFloor)
                {
                    mono.txtBottomHint.text =
                        $"已在 {needTimeText} 于 {needFloor} 楼发出需求，请前往接待。";
                    return;
                }

                mono.txtBottomHint.text = $"正在 {needFloor} 楼等待住户出现。";
                return;
            }

            if (_currentNeedRefData != null)
            {
                string needTimeText = GameTimeMgr.FormatClockTime(_currentNeedRefData.needTime);
                if (_currentNeedRefData.needFloor > 0)
                {
                    mono.txtBottomHint.text =
                        $"住户于 {needTimeText} 在 {_currentNeedRefData.needFloor} 楼发出需求。";
                    return;
                }

                mono.txtBottomHint.text = $"住户于 {needTimeText} 发出需求。";
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
            bool showPortraitOnly = !hasCurrentDialogue
                && _currentCustomerRefData != null
                && IsValidPortraitResName(_activeDialoguePortraitResName);

            mono.dialogueSection.SetActive(hasCurrentDialogue || showPortraitOnly);
            if (!hasCurrentDialogue)
            {
                if (showPortraitOnly)
                {
                    HideDialogueTextAndOptions();
                    ApplyDialoguePortrait(mono.imgDialoguePortrait, _activeDialoguePortraitResName);
                }

                return;
            }

            bool isPlayerDialogue = IsPlayerDialogue(_currentDialogueRefData);
            string currentContent = _currentDialogueRefData.content;

            mono.txtDialogueLeft.text = isPlayerDialogue ? string.Empty : currentContent;
            mono.txtDialogueRight.text = isPlayerDialogue ? currentContent : string.Empty;
            RefreshDialoguePortrait(_currentDialogueRefData);

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

            RefreshDialogueOptionUi();
        }

        private void RefreshDialogueOptionUi()
        {
            HideAllDialogueOptionItems();

            if (mono.dialogueOptionRoot == null)
            {
                if (_currentOptionDialogueList.Count > 0)
                    Debug.LogError("Gameplay 对话选项容器为空，无法显示选项。");
                return;
            }

            bool showOptions = _currentOptionDialogueList.Count > 0;
            mono.dialogueOptionRoot.gameObject.SetActive(showOptions);
            if (!showOptions)
                return;

            EnsureDialogueOptionPrefab();
            if (_dialogueOptionItemPrefab == null)
            {
                Debug.LogError("Gameplay 对话选项预制体未加载完成，无法显示选项。");
                return;
            }

            for (int index = 0; index < _currentOptionDialogueList.Count; index++)
            {
                UIMonoDialogueOption optionItem = GetOrCreateDialogueOptionItem(index);
                if (optionItem == null)
                    continue;

                DialogueRefData optionDialogueRefData = _currentOptionDialogueList[index];
                optionItem.gameObject.SetActive(true);
                optionItem.SetContent(optionDialogueRefData.content);
                BindDialogueOptionItem(optionItem, index);
            }

            if (mono.dialogueOptionRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(mono.dialogueOptionRoot);

            for (int index = 0; index < _currentOptionDialogueList.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                if (optionItem != null && optionItem.gameObject.activeSelf)
                    optionItem.RefreshBackgroundPixelsPerUnitMultiplier();
            }
        }

        private void HideDialogueTextAndOptions()
        {
            mono.txtDialogueLeft.text = string.Empty;
            mono.txtDialogueRight.text = string.Empty;

            if (mono.dialogueLeftArea != null)
            {
                mono.dialogueLeftArea.raycastTarget = false;
                mono.dialogueLeftArea.enabled = false;
            }

            if (mono.dialogueRightArea != null)
            {
                mono.dialogueRightArea.raycastTarget = false;
                mono.dialogueRightArea.enabled = false;
            }

            HideAllDialogueOptionItems();
            if (mono.dialogueOptionRoot != null)
                mono.dialogueOptionRoot.gameObject.SetActive(false);
        }

        private void RefreshActionUi()
        {
            bool canOperateElevator = !_isDialogueRunning && !_canCloseDoor && !_isTransitionPlaying;
            bool hasCustomer = _currentCustomerRefData != null;

            if (mono.btnReject != null)
                mono.btnReject.interactable = canOperateElevator && hasCustomer;

            if (mono.btnConfirm != null)
                mono.btnConfirm.interactable = canOperateElevator && IsFinalDayLastSpecialCustomer() && !GamePlayerDataMgr.instance.hasConfirmedFinalSpecialCustomer;

            if (mono.btnCloseDoor != null)
                mono.btnCloseDoor.interactable = canOperateElevator && (hasCustomer || _selectedFloor > 0);
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

            if (_currentOptionDialogueList.Count > 0)
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

        private void OnConfirmClicked(PointerEventData eventData, object[] args)
        {
            if (!IsFinalDayLastSpecialCustomer())
            {
                SCDebugHelper.Log("当前不是最终特殊住户确认节点。");
                return;
            }

            GamePlayerDataMgr.instance.MarkFinalSpecialCustomerConfirmed();
            RefreshAllUi();
        }

        private void OnSettingClicked(PointerEventData eventData, object[] args)
        {
            UINodeMgr.instance.AddNode(new UINodeSetting(SCUIShowType.ADDITION, false));
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

        private void OnDialogueOptionClicked(PointerEventData eventData, object[] args)
        {
            int optionIndex = ParseDialogueOptionIndex(args);
            if (optionIndex < 0 || optionIndex >= _currentOptionDialogueList.Count)
            {
                Debug.LogError($"Gameplay 选择对话选项失败：选项索引无效，index={optionIndex}");
                return;
            }

            HandleDialogueOptionSelection(_currentOptionDialogueList[optionIndex]);
        }

        private static int ParseDialogueOptionIndex(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] == null)
                return -1;

            if (args[0] is int optionIndex)
                return optionIndex;

            if (int.TryParse(args[0].ToString(), out optionIndex))
                return optionIndex;

            return -1;
        }

        private void HandleDialogueOptionSelection(DialogueRefData optionDialogueRefData)
        {
            if (optionDialogueRefData == null)
            {
                Debug.LogError("Gameplay 选择对话选项失败：选项节点为空。");
                return;
            }

            _currentAffectValue += optionDialogueRefData.affectValue;
            AddCurrentCustomerFavor(optionDialogueRefData.affectValue);
            mono.txtDialogueLeft.text = string.Empty;
            mono.txtDialogueRight.text = optionDialogueRefData.content;
            RefreshDialoguePortrait(optionDialogueRefData);

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
            if (_lastJudgmentEffectData == null)
            {
                Debug.LogError("Gameplay 拒绝失败：未命中拒绝判定配置。");
                return;
            }

            ApplyServiceFeedbackByJudgment(_lastJudgmentEffectData);
            SCDebugHelper.Log($"已拒绝当前住户，影响值={_lastJudgmentEffectData.affectValue}");
            StartRejectAndPickupNextFlow();
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

            if (_currentCustomerRefData == null)
            {
                if (_selectedFloor <= 0)
                {
                    SCDebugHelper.Log("请先选择要前往的楼层。");
                    return;
                }

                StartEmptyElevatorTravel(_selectedFloor);
                return;
            }

            if (_canCloseDoor)
            {
                int targetFloor = ResolveCustomerDepartureFloor();
                StartCustomerDepartureFlow(targetFloor, false);
                return;
            }

            if (_currentNeedRefData == null)
            {
                Debug.LogError("Gameplay 关门失败：当前住户需求配置为空。");
                return;
            }

            if (_selectedFloor <= 0)
            {
                Debug.LogError("Gameplay 关门失败：当前尚未选择目标楼层。");
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
            {
                ApplyServiceFeedbackByJudgment(_lastJudgmentEffectData);
                SCDebugHelper.Log($"已关门前往 {finalFloor} 楼，影响值={_lastJudgmentEffectData.affectValue}");
                RefreshAllUi();
                StartCustomerDepartureFlow(finalFloor, true);
                return;
            }

            RefreshAllUi();
        }

        private void ApplyServiceFeedbackByJudgment(JudgmentEffectData judgmentEffectData)
        {
            if (judgmentEffectData == null)
                return;

            AddCurrentCustomerFavor(judgmentEffectData.affectValue);

            if (judgmentEffectData.affectValue < 0)
            {
                GamePlayerDataMgr.instance.DeductPerformance(-judgmentEffectData.affectValue);
                _serviceFeedbackText =
                    $"处理错误：绩效值 -{-judgmentEffectData.affectValue}，当前绩效值 {GamePlayerDataMgr.instance.performanceValue}。";
                return;
            }

            _serviceFeedbackText = $"处理正确：影响值 +{judgmentEffectData.affectValue}。";
        }

        private void AddCurrentCustomerFavor(int favorValue)
        {
            if (_currentCustomerRefData == null || favorValue == 0)
                return;

            GamePlayerDataMgr.instance.AddNpcFavor(_currentCustomerRefData.id, favorValue);
        }

        private void StartEmptyElevatorTravel(int targetFloor)
        {
            if (targetFloor <= 0)
            {
                Debug.LogError("Gameplay 空梯移动失败：目标楼层无效。");
                return;
            }

            _isTransitionPlaying = true;
            _selectedFloor = 0;
            RefreshAllUi();
            CloseElevatorDoor(() =>
            {
                PlayElevatorTravelToFloor(targetFloor, () =>
                {
                    OpenElevatorDoor(() =>
                    {
                        _isTransitionPlaying = false;
                        TrySpawnNextPendingCustomer();
                        RefreshAllUi();
                    });
                });
            });
        }

        private void ShowSettlementPanel()
        {
            if (_isSettlementShowing)
                return;

            _isSettlementShowing = true;
            GamePlayerDataMgr.instance.SaveDailyProgress(_currentDayIndex + 1);
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

        private bool TryShowEndingOnDayComplete()
        {
            if (GamePlayerDataMgr.instance.performanceValue < 0)
            {
                ShowEndingPanel(EGameEndingType.BAD);
                return true;
            }

            if (!IsFinalDay())
                return false;

            ShowEndingPanel(GamePlayerDataMgr.instance.hasConfirmedFinalSpecialCustomer
                ? EGameEndingType.ENDING_1
                : EGameEndingType.ENDING_2);
            return true;
        }

        private void ShowEndingPanel(EGameEndingType endingType)
        {
            UIPanelEnding.pendingEndingType = endingType;

            UINodeMgr.instance.AddNode(new UINodeCommon<UIMonoEnding, UIPanelEnding>(
                SCUIShowType.FULL,
                "panel_ending",
                EndingNodeName,
                true,
                true,
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

        private bool IsFinalDay()
        {
            return _levelSequence.Count > 0 && _currentDayIndex >= _levelSequence.Count - 1;
        }

        private bool IsFinalDayLastSpecialCustomer()
        {
            if (!IsFinalDay() || _currentCustomerRefData == null || _currentLevelRefData == null)
                return false;

            if (_pendingCustomerIds.Count > 0)
                return false;

            List<CustomerEffectData> customerEffectList = _currentLevelRefData.customerEffectList;
            if (customerEffectList == null || customerEffectList.Count == 0)
                return false;

            CustomerEffectData lastCustomerEffectData = customerEffectList[customerEffectList.Count - 1];
            if (lastCustomerEffectData == null || lastCustomerEffectData.customerType != ECustomerType.SPECIAL)
                return false;

            long specialCustomerId = CustomerPoolMgr.instance.ResolveCustomerId(lastCustomerEffectData);
            return specialCustomerId == _currentCustomerRefData.id;
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
                                FinishCurrentCustomerService();
                                _isTransitionPlaying = false;
                                RefreshAllUi();
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
            Tween tween = mono.elevatorView?.PlayOpen(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }

        public void CloseElevatorDoor(TweenCallback onComplete = null)
        {
            Tween tween = mono.elevatorView?.PlayClose(onComplete);
            if (tween == null)
                onComplete?.Invoke();
        }

        private void SetElevatorDoorClosedInstant()
        {
            mono.elevatorView?.SetDoorClosedInstant();
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
                GameTimeMgr.instance.PlayElevatorTravelAdvance(ElevatorTravelAnimDuration);
                sequence.AppendCallback(() => AnimateFloorText(targetFloor));
                Tween shakeTween = mono.elevatorView?.PlayTravelShake();
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

        private void EnsureDialogueOptionPrefab()
        {
            if (_dialogueOptionPrefabReady && _dialogueOptionItemPrefab != null)
                return;

            if (string.IsNullOrEmpty(mono.dialogueOptionItemResName))
            {
                Debug.LogError("Gameplay 对话选项预制体未配置：请在 UIMonoGameplayMain 上填写 Addressables 资源名。");
                return;
            }

            _dialogueOptionItemPrefab = ResourcesHelper.LoadAsset<GameObject>(mono.dialogueOptionItemResName);
            if (_dialogueOptionItemPrefab == null)
            {
                Debug.LogError($"Gameplay 加载对话选项预制体失败：resName={mono.dialogueOptionItemResName}");
                return;
            }

            if (_dialogueOptionItemPrefab.GetComponent<UIMonoDialogueOption>() == null)
            {
                Debug.LogError($"Gameplay 对话选项预制体缺少 UIMonoDialogueOption 组件：resName={mono.dialogueOptionItemResName}");
                _dialogueOptionItemPrefab = null;
                return;
            }

            _dialogueOptionPrefabReady = true;
        }

        private UIMonoDialogueOption GetOrCreateDialogueOptionItem(int index)
        {
            while (_dialogueOptionItemPool.Count <= index)
            {
                if (_dialogueOptionItemPrefab == null || mono.dialogueOptionRoot == null)
                    return null;

                GameObject instanceObject = Object.Instantiate(_dialogueOptionItemPrefab, mono.dialogueOptionRoot);
                instanceObject.name = $"DialogueOption_{index}";
                UIMonoDialogueOption optionItem = instanceObject.GetComponent<UIMonoDialogueOption>();
                if (optionItem == null)
                {
                    Debug.LogError("Gameplay 实例化对话选项失败：缺少 UIMonoDialogueOption 组件。");
                    Object.Destroy(instanceObject);
                    return null;
                }

                optionItem.EnsureReferences();
                optionItem.gameObject.SetActive(false);
                _dialogueOptionItemPool.Add(optionItem);
            }

            return _dialogueOptionItemPool[index];
        }

        private void BindDialogueOptionItem(UIMonoDialogueOption optionItem, int optionIndex)
        {
            if (optionItem == null || optionItem.btnOption == null)
            {
                Debug.LogError("Gameplay 绑定对话选项失败：选项按钮为空。");
                return;
            }

            optionItem.btnOption.RemoveAllListener(ESCEventType.ON_MOUSE_LEFT_CLICK_DOWN);
            optionItem.btnOption.AddMouseLeftClickDown(OnDialogueOptionClicked, optionIndex);
        }

        private void UnbindAllDialogueOptionItems()
        {
            for (int index = 0; index < _dialogueOptionItemPool.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                optionItem?.btnOption?.RemoveAllListener(ESCEventType.ON_MOUSE_LEFT_CLICK_DOWN);
            }
        }

        private void HideAllDialogueOptionItems()
        {
            UnbindAllDialogueOptionItems();
            for (int index = 0; index < _dialogueOptionItemPool.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                if (optionItem != null)
                    optionItem.gameObject.SetActive(false);
            }
        }

        private void RefreshDialoguePortrait(DialogueRefData dialogueRefData)
        {
            if (dialogueRefData == null)
                return;

            ApplyDialoguePortrait(mono.imgDialoguePortrait, dialogueRefData.portraitResName);
        }

        private void ClearDialoguePortraits()
        {
            _activeDialoguePortraitResName = null;
            ApplyDialoguePortrait(mono.imgDialoguePortrait, null);
        }

        private void ApplyDialoguePortrait(Image image, string portraitResName)
        {
            if (image == null)
                return;

            if (!IsValidPortraitResName(portraitResName))
            {
                image.enabled = false;
                image.sprite = null;
                return;
            }

            Sprite sprite = GetOrLoadPortraitSprite(portraitResName);
            if (sprite == null)
            {
                image.enabled = false;
                image.sprite = null;
                return;
            }

            _activeDialoguePortraitResName = portraitResName;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = true;
        }

        private static bool IsValidPortraitResName(string portraitResName)
        {
            return !string.IsNullOrEmpty(portraitResName) && portraitResName != "*";
        }

        private Sprite GetOrLoadPortraitSprite(string portraitResName)
        {
            if (_portraitSpriteCache.TryGetValue(portraitResName, out Sprite cachedSprite))
                return cachedSprite;

            Sprite sprite = ResourcesHelper.LoadAsset<Sprite>(portraitResName);
            if (sprite == null)
            {
                Debug.LogError($"Gameplay 加载对话立绘失败：portraitResName={portraitResName}");
                return null;
            }

            _portraitSpriteCache[portraitResName] = sprite;
            return sprite;
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
