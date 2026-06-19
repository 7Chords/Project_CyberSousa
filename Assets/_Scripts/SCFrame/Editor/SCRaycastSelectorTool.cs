using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SCFrame
{
    /// <summary>
    /// 运行时在 Game 视图中点击对象后，同步在 Hierarchy 中选中目标。
    /// </summary>
    [InitializeOnLoad]
    public static class SCRaycastSelectorTool
    {
        private const string MENU_PATH = "SCFrame/工具/切换游戏视图射线选中";
        private const string PREF_KEY = "SCFrame.SCRaycastSelectorTool.Enabled";

        private static bool s_isEnabled;
        private static int s_lastHandledFrame = -1;

        static SCRaycastSelectorTool()
        {
            s_isEnabled = EditorPrefs.GetBool(PREF_KEY, false);
            Menu.SetChecked(MENU_PATH, s_isEnabled);
            EditorApplication.update += OnEditorUpdate;
        }

        [MenuItem(MENU_PATH)]
        private static void ToggleTool()
        {
            s_isEnabled = !s_isEnabled;
            EditorPrefs.SetBool(PREF_KEY, s_isEnabled);
            Menu.SetChecked(MENU_PATH, s_isEnabled);

            if (s_isEnabled)
            {
                Debug.Log("[RaycastSelector] 已开启。请在运行时聚焦 Game 视图后左键拾取对象。");
                return;
            }

            Debug.Log("[RaycastSelector] 已关闭。");
        }

        private static void OnEditorUpdate()
        {
            if (!s_isEnabled || !EditorApplication.isPlaying)
            {
                return;
            }

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow == null || focusedWindow.GetType().Name != "GameView")
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            int currentFrame = Time.frameCount;
            if (s_lastHandledFrame == currentFrame)
            {
                return;
            }

            s_lastHandledFrame = currentFrame;

            try
            {
                DoRaycastSelect(Input.mousePosition);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RaycastSelector] 射线拾取失败: {ex}");
            }
        }

        private static void DoRaycastSelect(Vector3 screenPos)
        {
            // 优先检测 UI（GraphicRaycaster）
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = screenPos
                };
                List<RaycastResult> uiResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, uiResults);
                if (uiResults.Count > 0)
                {
                    SelectObject(uiResults[0].gameObject);
                    return;
                }
            }

            // 检测 3D 物体
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = UnityEngine.Object.FindObjectOfType<Camera>();
            }

            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    SelectObject(hit.collider.gameObject);
                    return;
                }
            }

            Debug.Log("[RaycastSelector] 未检测到任何物体");
        }

        /// <summary>
        /// 在 Hierarchy 中选中目标物体，并 Ping 使其在面板中高亮可见。
        /// </summary>
        public static void SelectObject(GameObject go)
        {
            if (go == null)
            {
                Debug.LogError("[RaycastSelector] 目标物体为空，无法选中。");
                return;
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
