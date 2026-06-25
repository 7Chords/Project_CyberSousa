using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    [DisallowMultipleComponent]
    public class UIElevatorView : MonoBehaviour
    {
        [Header("电梯根节点")]
        public RectTransform elevatorRoot;
        [Header("电梯左门")]
        public RectTransform elevatorDoorLeft;
        [Header("电梯右门")]
        public RectTransform elevatorDoorRight;
        [Header("电梯开关门动效")]
        public UISlidingDoorEffect doorEffect;
        [Header("电梯运行震动感效")]
        public UITravelShakeEffect travelShakeEffect;

        private void Awake()
        {
            EnsureReferences();
        }

        public void Setup(RectTransform root, RectTransform leftDoor, RectTransform rightDoor, UISlidingDoorEffect slidingDoorEffect, UITravelShakeEffect shakeEffect)
        {
            elevatorRoot = root;
            elevatorDoorLeft = leftDoor;
            elevatorDoorRight = rightDoor;
            doorEffect = slidingDoorEffect;
            travelShakeEffect = shakeEffect;
        }

        public void EnsureReferences()
        {
            if (elevatorRoot == null)
                elevatorRoot = transform as RectTransform;

            if (doorEffect == null)
                doorEffect = GetComponent<UISlidingDoorEffect>();

            if (travelShakeEffect == null)
                travelShakeEffect = GetComponent<UITravelShakeEffect>();
        }

        public Tween PlayOpen(TweenCallback onComplete = null)
        {
            EnsureReferences();
            return doorEffect?.PlayOpen(onComplete);
        }

        public Tween PlayClose(TweenCallback onComplete = null)
        {
            EnsureReferences();
            return doorEffect?.PlayClose(onComplete);
        }

        public void SetDoorClosedInstant()
        {
            EnsureReferences();
            doorEffect?.SetClosedInstant();
        }

        public Tween PlayTravelShake()
        {
            EnsureReferences();
            return travelShakeEffect?.Play();
        }
    }
}
