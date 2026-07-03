using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using GameCore.Flow;
using GameCore.RefData;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain : _ASCUIPanelBase<UIMonoGameplayMain>
    {
        private static UIPanelGameplayMain _activePanel;
        private static bool _skipSaveOnDiscardOnce;

        private const string PlayerSpeakerName = "玩家";
        private const long DefaultLevelId = 1001;
        private const string SettlementNodeName = "UINodeSettlement";
        private const string EndingNodeName = "UINodeEnding";
        private const float ElevatorTravelAnimDuration = 0.90f;
        private const float CustomerPortraitFlipHalfDuration = 0.18f;
        private const int DefaultMaxPerformance = 100;
        private const int DefaultMissingConfirmPenalty = 10;
        private const int DefaultPerfectOperationBonus = 12;

        private readonly Dictionary<long, DialogueRefData> _dialogueMap = new Dictionary<long, DialogueRefData>();
        private readonly Dictionary<long, CustomerRefData> _customerMap = new Dictionary<long, CustomerRefData>();
        private readonly Dictionary<long, RuleRefData> _ruleMap = new Dictionary<long, RuleRefData>();
        private readonly List<LevelRefData> _levelSequence = new List<LevelRefData>();
        private readonly Queue<PendingCustomerRuntimeData> _pendingCustomers = new Queue<PendingCustomerRuntimeData>();
        private readonly RecentFloorInputData _recentFloorInputData = new RecentFloorInputData();

        private LevelRefData _currentLevelRefData;
        private CustomerRefData _currentCustomerRefData;
        private CustomerNeedRefData _currentNeedRefData;
        private TimeEffectData _currentNeedTime;
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
        private int _resolvedTargetFloor;
        private int _currentAffectValue;
        private int _currentDayIndex;
        private bool _isDialogueRunning;
        private bool _canCloseDoor;
        private bool _isSettlementShowing;
        private bool _isTransitionPlaying;
        private bool _isFloorDisplayAnimating;
        private bool _isCustomerInfoVisible;
        private bool _hasInitializedRuntime;
        private bool _hasRunEnded;
        private int _maxPerformance = DefaultMaxPerformance;
        private int _missingConfirmPenalty = DefaultMissingConfirmPenalty;
        private int _perfectOperationBonus = DefaultPerfectOperationBonus;
        private bool _hasConfirmedCurrentCustomer;
        private bool _hasCurrentOperationProblem;
        private bool _hasPlayedFirstCustomerGuide;
        private string _serviceFeedbackText;
        private string _ruleFeedbackText;
        private string _activePortraitResName;
        private bool _useCustomerBackPortrait;
        private Transform _customerLayerHomeParent;
        private readonly Dictionary<string, Sprite> _portraitSpriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _floorSceneSpriteCache = new Dictionary<string, Sprite>();
        private int _displayedFloorSceneBackgroundFloor;
        private Tween _floorSceneBackgroundTween;

        public UIPanelGameplayMain(UIMonoGameplayMain _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            _activePanel = this;
            GameTimeMgr.instance.OnTimeChanged += OnGameTimeChanged;
            InitializeRuntimeData();
            ApplyGameplayConfig();
            RefreshAllUi();
        }

        public override void BeforeDiscard()
        {
            bool skipSaveOnDiscard = _skipSaveOnDiscardOnce;
            _skipSaveOnDiscardOnce = false;

            if (!skipSaveOnDiscard && _hasInitializedRuntime && !_isSettlementShowing && !_hasRunEnded)
                GamePlayerDataMgr.instance.SaveDailyProgress(_currentDayIndex);

            if (_activePanel == this)
                _activePanel = null;

            GameTimeMgr.instance.OnTimeChanged -= OnGameTimeChanged;
            GameTimeMgr.instance.StopAdvanceTween();
            _dialogueCoroutineContainer.KillAll();
            _dialogueAnimTweenContainer.KillAllDoTween();
            KillFloorSceneBackgroundTween();
            UnbindButtons();
        }

        public static void SaveCurrentDayProgressIfActive()
        {
            if (_activePanel == null || !_activePanel._hasInitializedRuntime)
                return;

            if (_activePanel._isSettlementShowing || _activePanel._hasRunEnded)
                return;

            GamePlayerDataMgr.instance.SaveDailyProgress(_activePanel._currentDayIndex);
        }

        public static void RefreshActivePanelForDebug()
        {
            if (_activePanel == null || !_activePanel._hasInitializedRuntime)
                return;

            _activePanel.RefreshAllUi();
        }

        public static bool TryGetActiveDayIndexForDebug(out int dayIndex)
        {
            if (_activePanel == null || !_activePanel._hasInitializedRuntime)
            {
                dayIndex = -1;
                return false;
            }

            dayIndex = _activePanel._currentDayIndex;
            return true;
        }

        public static bool RestartGameplayFromDayForDebug(int dayIndex)
        {
            if (!GamePlayerDataMgr.instance.TrySetStartDayIndex(dayIndex))
                return false;

            _skipSaveOnDiscardOnce = _activePanel != null
                                     && _activePanel._hasInitializedRuntime
                                     && !_activePanel._isSettlementShowing
                                     && !_activePanel._hasRunEnded;
            GameFlowController.instance.EnterGameplay();
            return true;
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
            ApplyGameplayConfig();
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

        private void RefreshDialogueAdvanceClickArea()
        {
            if (mono.dialogueAdvanceClickArea == null)
                return;

            bool canClickDialogue = _isDialogueRunning
                && (_isDialogueTypewriterPlaying || CanAdvanceDialogueByClick() || _awaitDialogueAdvanceAfterPlayerLine);
            mono.dialogueAdvanceClickArea.raycastTarget = canClickDialogue;
            mono.dialogueAdvanceClickArea.enabled = canClickDialogue;
        }

        private void ApplyGameplayConfig()
        {
            GameplayConfigRefData config = SCRefDataMgr.instance.gameplayConfig;
            if (config == null)
            {
                SCDebugHelper.Log("未读取到 gameplay_config 配置，使用默认绩效配置。");
                _maxPerformance = DefaultMaxPerformance;
                _missingConfirmPenalty = DefaultMissingConfirmPenalty;
                _perfectOperationBonus = DefaultPerfectOperationBonus;
                return;
            }

            _maxPerformance = Mathf.Max(1, config.maxPerformance);
            _missingConfirmPenalty = Mathf.Max(0, config.missingConfirmPenalty);
            _perfectOperationBonus = Mathf.Max(0, config.perfectOperationBonus);
        }

        private void MarkCurrentCustomerOperationProblem(string reason)
        {
            _hasCurrentOperationProblem = true;
            if (!string.IsNullOrEmpty(reason))
                SCDebugHelper.Log(reason);
        }

        private void SettleCurrentCustomerPerformance(string settleSource)
        {
            if (_currentCustomerRefData == null)
                return;

            if (!_hasConfirmedCurrentCustomer)
            {
                if (_missingConfirmPenalty > 0)
                {
                    GamePlayerDataMgr.instance.DeductPerformance(_missingConfirmPenalty);
                    SCDebugHelper.Log(
                        $"当前住户未确认，绩效值 -{_missingConfirmPenalty}。source={settleSource}，当前绩效值 {GamePlayerDataMgr.instance.performanceValue}。");
                }

                return;
            }

            if (_hasCurrentOperationProblem)
                return;

            int currentPerformance = GamePlayerDataMgr.instance.performanceValue;
            int actualBonus = Mathf.Min(_perfectOperationBonus, Mathf.Max(0, _maxPerformance - currentPerformance));
            if (actualBonus <= 0)
            {
                SCDebugHelper.Log($"当前住户已确认，但绩效值已达上限 {_maxPerformance}。");
                return;
            }

            GamePlayerDataMgr.instance.AddPerformance(actualBonus);
            SCDebugHelper.Log(
                $"当前住户处理确认完成，绩效值 +{actualBonus}。source={settleSource}，当前绩效值 {GamePlayerDataMgr.instance.performanceValue}。");
        }

        private class RecentFloorInputData
        {
            public int latestSingleFloor;
            public readonly List<int> recentFloorList = new List<int>();

            public void Clear()
            {
                latestSingleFloor = 0;
                recentFloorList.Clear();
            }

            public void RecordFloor(int floor)
            {
                if (floor <= 0)
                    return;

                latestSingleFloor = floor;
                recentFloorList.Add(floor);
            }
        }
    }
}
