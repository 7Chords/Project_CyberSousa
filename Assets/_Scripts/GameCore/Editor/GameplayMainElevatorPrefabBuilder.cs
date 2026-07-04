using GameCore.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Editor
{
    public static partial class GameplayMainPrefabBuilder
    {
        private static void CreateElevatorView(RectTransform parent)
        {
            RectTransform elevatorRoot = CreateContainer("ElevatorRoot", parent, new Vector2(0.00f, -0.46f), new Vector2(1.00f, 1.46f));
            elevatorRoot.gameObject.AddComponent<CanvasGroup>();

            CreatePanel("ElevatorBack", elevatorRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.70f, 0.70f, 0.72f, 1f));
            CreatePanel("ElevatorInnerShadow", elevatorRoot, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), new Color(0.18f, 0.18f, 0.18f, 0.30f));
            CreateCustomerLayer(elevatorRoot);

            RectTransform doorMaskRoot = CreateContainer("ElevatorDoorMask", elevatorRoot, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
            RectTransform doorLeft = CreateDoor("ElevatorDoorLeft", doorMaskRoot, new Vector2(0.00f, 0.00f), new Vector2(0.50f, 1.00f), true);
            RectTransform doorRight = CreateDoor("ElevatorDoorRight", doorMaskRoot, new Vector2(0.50f, 0.00f), new Vector2(1.00f, 1.00f), false);

            UISlidingDoorEffect slidingDoorEffect = elevatorRoot.gameObject.AddComponent<UISlidingDoorEffect>();
            slidingDoorEffect.Setup(doorLeft, doorRight, 450f, 0.32f);

            UITravelShakeEffect travelShakeEffect = elevatorRoot.gameObject.AddComponent<UITravelShakeEffect>();
            travelShakeEffect.Setup(0.55f, new Vector2(0f, 6f), Vector3.zero);

            UIElevatorView elevatorView = elevatorRoot.gameObject.AddComponent<UIElevatorView>();
            elevatorView.Setup(elevatorRoot, doorLeft, doorRight, slidingDoorEffect, travelShakeEffect, parent);
            _currentMono.elevatorView = elevatorView;
        }

        private static void CreateCustomerLayer(RectTransform elevatorRoot)
        {
            RectTransform customerLayer = CreateContainer("CustomerLayer", elevatorRoot, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f));
            CanvasGroup customerCanvasGroup = customerLayer.gameObject.AddComponent<CanvasGroup>();
            _currentMono.customerCanvasGroup = customerCanvasGroup;

            UIRuntimeFadeEffect customerFadeEffect = customerLayer.gameObject.AddComponent<UIRuntimeFadeEffect>();
            customerFadeEffect.Setup(customerCanvasGroup, 0.30f);
            _currentMono.customerFadeEffect = customerFadeEffect;

            UIRuntimeMoveScaleEffect customerMoveScaleEffect = customerLayer.gameObject.AddComponent<UIRuntimeMoveScaleEffect>();
            customerMoveScaleEffect.Setup(0.28f, new Vector2(0f, -72f), 1.10f, new Vector2(0f, 68f), 0.84f);
            _currentMono.customerMoveScaleEffect = customerMoveScaleEffect;

            Image customerShadow = CreatePanel("CustomerShadow", customerLayer, new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.12f), new Color(0f, 0f, 0f, 0.16f));
            customerShadow.raycastTarget = false;
            _currentMono.imgCustomerPortrait = CreatePortraitImage("CustomerPortrait", customerLayer, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.95f));
            _currentMono.txtCustomerName = CreateText("AnimalLabel", customerLayer, new Vector2(0.00f, 0.12f), new Vector2(1.00f, 0.96f), "动物", 40, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.25f, 0.25f, 0.25f, 1f));
        }

        private static RectTransform CreateDoor(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, bool isLeft)
        {
            RectTransform door = CreateContainer(name, parent, anchorMin, anchorMax);
            Color faceColor = isLeft ? new Color(0.86f, 0.86f, 0.88f, 1f) : new Color(0.84f, 0.84f, 0.86f, 1f);
            CreatePanel($"{name}Face", door, new Vector2(0f, 0f), new Vector2(1f, 1f), faceColor);

            Vector2 edgeMin = isLeft ? new Vector2(0.94f, 0f) : new Vector2(0f, 0f);
            Vector2 edgeMax = isLeft ? new Vector2(1f, 1f) : new Vector2(0.06f, 1f);
            CreatePanel($"{name}Edge", door, edgeMin, edgeMax, new Color(0.68f, 0.68f, 0.70f, 1f));
            return door;
        }
    }
}
