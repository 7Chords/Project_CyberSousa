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
    public partial class UIPanelGameplayMain : _ASCUIPanelBase<UIMonoGameplayMain>
    {
        private static UIPanelGameplayMain _activePanel;

        private const string PlayerSpeakerName = "玩家";
        private const long DefaultLevelId = 1001;
        private const string SettlementNodeName = "UINodeSettlement";
        private const string EndingNodeName = "UINodeEnding";
        private const float ElevatorTravelAnimDuration = 0.90f;

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
        private bool _hasInitializedRuntime;
        private bool _hasRunEnded;
        private string _serviceFeedbackText;
        private string _ruleFeedbackText;
        private string _activeDialoguePortraitResName;
        private readonly Dictionary<string, Sprite> _portraitSpriteCache = new Dictionary<string, Sprite>();

        public UIPanelGameplayMain(UIMonoGameplayMain _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            _activePanel = this;
            GameTimeMgr.instance.OnTimeChanged += OnGameTimeChanged;
            InitializeRuntimeData();
            RefreshAllUi();
        }

        public override void BeforeDiscard()
        {
            if (_hasInitializedRuntime && !_isSettlementShowing && !_hasRunEnded)
                GamePlayerDataMgr.instance.SaveDailyProgress(_currentDayIndex);

            if (_activePanel == this)
                _activePanel = null;

            GameTimeMgr.instance.OnTimeChanged -= OnGameTimeChanged;
            GameTimeMgr.instance.StopAdvanceTween();
            _dialogueCoroutineContainer.KillAll();
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

        private void RefreshDialogueAdvanceClickArea()
        {
            if (mono.dialogueAdvanceClickArea == null)
                return;

            bool canClickDialogue = _isDialogueRunning
                && (_isDialogueTypewriterPlaying || CanAdvanceDialogueByClick() || _awaitDialogueAdvanceAfterPlayerLine);
            mono.dialogueAdvanceClickArea.raycastTarget = canClickDialogue;
            mono.dialogueAdvanceClickArea.enabled = canClickDialogue;
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
