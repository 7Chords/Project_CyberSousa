#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using GameCore.RefData;
using GameCore.UI;
using UnityEngine;

namespace GameCore.Test
{
    /// <summary>
    /// 运行时 IMGUI 测试工具面板。
    /// </summary>
    public class GameDebugToolUI : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F8;
        private static GameDebugToolUI _instance;

        private Rect _windowRect = new Rect(16f, 16f, 420f, 500f);
        private Vector2 _scrollPosition;
        private bool _isVisible = false;
        private string _jumpDayInput = "1";
        private string _dialogueIdInput = string.Empty;
        private bool _dialogueSelected = true;
        private string _performanceInput = "100";
        private string _npcIdInput = string.Empty;
        private string _npcFavorInput = "0";
        private string _message = "F8 可隐藏或显示测试面板。";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null)
                return;

            GameObject root = new GameObject(nameof(GameDebugToolUI));
            root.AddComponent<GameDebugToolUI>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SyncInputFromRuntime();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                _isVisible = !_isVisible;
        }

        private void OnGUI()
        {
            if (!_isVisible)
                return;

            _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "测试工具 / GM 面板");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(402f), GUILayout.Height(462f));

            DrawStatusSection();
            GUILayout.Space(8f);
            DrawJumpDaySection();
            GUILayout.Space(8f);
            DrawDialogueSection();
            GUILayout.Space(8f);
            DrawPerformanceSection();
            GUILayout.Space(8f);
            DrawNpcFavorSection();
            GUILayout.Space(8f);
            DrawBottomButtons();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawStatusSection()
        {
            GUILayout.Label("当前状态");
            GUILayout.Label($"存档起始天：第 {GamePlayerDataMgr.instance.startDayIndex + 1} 天");
            GUILayout.Label($"当前绩效值：{GamePlayerDataMgr.instance.performanceValue}");

            if (UIPanelGameplayMain.TryGetActiveDayIndexForDebug(out int activeDayIndex))
                GUILayout.Label($"当前 Gameplay 天数：第 {activeDayIndex + 1} 天");
            else
                GUILayout.Label("当前 Gameplay 天数：未进入 Gameplay");

            GUILayout.Label(_message, GUI.skin.box);
        }

        private void DrawJumpDaySection()
        {
            GUILayout.Label("跳转某天");
            GUILayout.BeginHorizontal();
            GUILayout.Label("天数", GUILayout.Width(44f));
            _jumpDayInput = GUILayout.TextField(_jumpDayInput);
            if (GUILayout.Button("跳转并重开", GUILayout.Width(110f)))
                ApplyJumpDay();
            GUILayout.EndHorizontal();
        }

        private void DrawDialogueSection()
        {
            GUILayout.Label("对话选项设定");
            GUILayout.BeginHorizontal();
            GUILayout.Label("对话ID", GUILayout.Width(60f));
            _dialogueIdInput = GUILayout.TextField(_dialogueIdInput);
            GUILayout.EndHorizontal();

            _dialogueSelected = GUILayout.Toggle(_dialogueSelected, "设为已选择");
            if (GUILayout.Button("应用对话选项设定"))
                ApplyDialogueSelection();
        }

        private void DrawPerformanceSection()
        {
            GUILayout.Label("绩效值设定");
            GUILayout.BeginHorizontal();
            GUILayout.Label("绩效值", GUILayout.Width(60f));
            _performanceInput = GUILayout.TextField(_performanceInput);
            if (GUILayout.Button("设定绩效", GUILayout.Width(110f)))
                ApplyPerformanceValue();
            GUILayout.EndHorizontal();
        }

        private void DrawNpcFavorSection()
        {
            GUILayout.Label("NPC 好感度设定");
            GUILayout.BeginHorizontal();
            GUILayout.Label("NPC ID", GUILayout.Width(60f));
            _npcIdInput = GUILayout.TextField(_npcIdInput);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("好感度", GUILayout.Width(60f));
            _npcFavorInput = GUILayout.TextField(_npcFavorInput);
            if (GUILayout.Button("设定好感", GUILayout.Width(110f)))
                ApplyNpcFavor();
            GUILayout.EndHorizontal();
        }

        private void DrawBottomButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("同步当前值"))
                SyncInputFromRuntime();

            if (GUILayout.Button("刷新 Gameplay UI"))
            {
                UIPanelGameplayMain.RefreshActivePanelForDebug();
                SetMessage("已请求刷新当前 Gameplay UI。");
            }

            GUILayout.EndHorizontal();
        }

        private void ApplyJumpDay()
        {
            if (!TryParseInt(_jumpDayInput, "跳转天数", out int dayNumber))
                return;

            if (dayNumber <= 0)
            {
                SetMessage($"跳转失败：天数必须大于 0，当前输入={dayNumber}");
                return;
            }

            int dayIndex = dayNumber - 1;
            if (!UIPanelGameplayMain.RestartGameplayFromDayForDebug(dayIndex))
            {
                SetMessage($"跳转失败：第 {dayNumber} 天不合法，或当前运行时未就绪。");
                return;
            }

            SetMessage($"已跳转到第 {dayNumber} 天，并重新进入 Gameplay。");
        }

        private void ApplyDialogueSelection()
        {
            if (!TryParseLong(_dialogueIdInput, "对话ID", out long dialogueId))
                return;

            if (!HasDialogue(dialogueId))
            {
                SetMessage($"设定失败：未找到 dialogueId={dialogueId} 的对话配置。");
                return;
            }

            GamePlayerDataMgr.instance.SetDialogueOptionSelection(dialogueId, _dialogueSelected);
            UIPanelGameplayMain.RefreshActivePanelForDebug();
            SetMessage($"已设定对话选项：dialogueId={dialogueId}，selected={_dialogueSelected}");
        }

        private void ApplyPerformanceValue()
        {
            if (!TryParseInt(_performanceInput, "绩效值", out int performanceValue))
                return;

            GamePlayerDataMgr.instance.SetPerformanceValue(performanceValue);
            UIPanelGameplayMain.RefreshActivePanelForDebug();
            SetMessage($"已设定绩效值：{performanceValue}");
        }

        private void ApplyNpcFavor()
        {
            if (!TryParseLong(_npcIdInput, "NPC ID", out long npcId))
                return;

            if (!TryParseInt(_npcFavorInput, "NPC 好感度", out int favorValue))
                return;

            if (!HasCustomer(npcId))
            {
                SetMessage($"设定失败：未找到 npcId={npcId} 的住户配置。");
                return;
            }

            GamePlayerDataMgr.instance.SetNpcFavor(npcId, favorValue);
            UIPanelGameplayMain.RefreshActivePanelForDebug();
            SetMessage($"已设定 NPC 好感度：npcId={npcId}，favor={favorValue}");
        }

        private void SyncInputFromRuntime()
        {
            _jumpDayInput = (GamePlayerDataMgr.instance.startDayIndex + 1).ToString();
            _performanceInput = GamePlayerDataMgr.instance.performanceValue.ToString();
            SetMessage("已同步当前运行时数值到输入框。");
        }

        private void SetMessage(string message)
        {
            _message = message;
            Debug.Log($"[GameDebugToolUI] {message}");
        }

        private bool TryParseInt(string input, string fieldName, out int value)
        {
            if (int.TryParse(input, out value))
                return true;

            SetMessage($"{fieldName} 解析失败：输入内容=\"{input}\"");
            return false;
        }

        private bool TryParseLong(string input, string fieldName, out long value)
        {
            if (long.TryParse(input, out value))
                return true;

            SetMessage($"{fieldName} 解析失败：输入内容=\"{input}\"");
            return false;
        }

        private static bool HasDialogue(long dialogueId)
        {
            List<DialogueRefData> dialogueList = SCRefDataMgr.instance.dialogueRefList?.refDataList;
            if (dialogueList == null)
                return false;

            for (int index = 0; index < dialogueList.Count; index++)
            {
                DialogueRefData dialogueRefData = dialogueList[index];
                if (dialogueRefData != null && dialogueRefData.id == dialogueId)
                    return true;
            }

            return false;
        }

        private static bool HasCustomer(long customerId)
        {
            List<CustomerRefData> customerList = SCRefDataMgr.instance.customerRefList?.refDataList;
            if (customerList == null)
                return false;

            for (int index = 0; index < customerList.Count; index++)
            {
                CustomerRefData customerRefData = customerList[index];
                if (customerRefData != null && customerRefData.id == customerId)
                    return true;
            }

            return false;
        }
    }
}
#endif
