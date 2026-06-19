using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SCFrame
{
    /// <summary>
    /// 运行时按快捷键后，在 Hierarchy 中选中鼠标当前指向的 UI 对象。
    /// </summary>
    public static class SCRaycastSelectorTool
    {
        private const string MENU_PATH = "SCFrame/工具/选中鼠标指向的UI %g";

        [MenuItem(MENU_PATH)]
        private static void SelectCurrentHoveredUI()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[RaycastSelector] 请先进入 Play 模式，再使用 Ctrl+G 选中 UI。");
                return;
            }

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow == null || focusedWindow.GetType().Name != "GameView")
            {
                Debug.LogError("[RaycastSelector] 请先聚焦 Game 视图，再使用 Ctrl+G 选中 UI。");
                return;
            }

            try
            {
                DoUIRaycastSelect(Input.mousePosition);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RaycastSelector] 射线拾取失败: {ex}");
            }
        }

        private static void DoUIRaycastSelect(Vector3 screenPos)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("[RaycastSelector] 当前场景中没有可用的 EventSystem。");
                return;
            }

            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = screenPos
            };

            List<RaycastResult> uiResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, uiResults);
            if (uiResults.Count > 0)
            {
                SelectObject(uiResults[0].gameObject);
                return;
            }

            Debug.Log("[RaycastSelector] 当前鼠标位置没有检测到 UI 对象。");
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
