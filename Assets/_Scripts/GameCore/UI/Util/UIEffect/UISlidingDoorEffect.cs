using DG.Tweening;
using SCFrame;
using UnityEngine;

namespace GameCore.UI
{
    /// <summary>
    /// 运行时主动控制的电梯门板左右开合动效。
    /// </summary>
    [DisallowMultipleComponent]
    public class UISlidingDoorEffect : UIEffectBase
    {
        [SerializeField] private RectTransform leftDoor;
        [SerializeField] private RectTransform rightDoor;
        [SerializeField] private float doorTravelDistance = 110f;
        [SerializeField] private Vector2 closeFramePunch = new Vector2(7f, 0f);
        [SerializeField] private Vector2 openFramePunch = new Vector2(0f, 4f);
        [SerializeField] private float framePunchDuration = 0.14f;

        private Vector2 _leftClosedPos;
        private Vector2 _rightClosedPos;
        private Vector2 _leftOpenPos;
        private Vector2 _rightOpenPos;
        private Vector2 _rootOriginPos;
        private bool _cacheReady;
        private bool _isOpen;

        protected override void Awake()
        {
            base.Awake();
            cachePositions();
        }

        public void Setup(RectTransform leftDoorRect, RectTransform rightDoorRect, float travelDistance, float tweenDuration)
        {
            leftDoor = leftDoorRect;
            rightDoor = rightDoorRect;
            doorTravelDistance = travelDistance;
            duration = tweenDuration;
            cachePositions();
        }

        public bool IsOpen => _isOpen;

        public void SetClosedInstant()
        {
            _isOpen = false;
            KillAllTweens();
            if (!_cacheReady)
                cachePositions();

            if (leftDoor != null)
                leftDoor.anchoredPosition = _leftClosedPos;
            if (rightDoor != null)
                rightDoor.anchoredPosition = _rightClosedPos;
        }

        public void SetOpenInstant()
        {
            _isOpen = true;
            KillAllTweens();
            if (!_cacheReady)
                cachePositions();

            if (leftDoor != null)
                leftDoor.anchoredPosition = _leftOpenPos;
            if (rightDoor != null)
                rightDoor.anchoredPosition = _rightOpenPos;
        }

        public Tween PlayOpen(TweenCallback onComplete = null)
        {
            _isOpen = true;
            return playDoor(_leftOpenPos, _rightOpenPos, onComplete);
        }

        public Tween PlayClose(TweenCallback onComplete = null)
        {
            _isOpen = false;
            return playDoor(_leftClosedPos, _rightClosedPos, onComplete);
        }

        protected override void RegisterEvents()
        {
        }

        protected override void UnregisterEvents()
        {
        }

        protected override void ResetState()
        {
            if (_isOpen)
                SetOpenInstant();
            else
                SetClosedInstant();
        }

        private void cachePositions()
        {
            if (leftDoor == null || rightDoor == null)
            {
                SCDebugHelper.LogError($"[{nameof(UISlidingDoorEffect)}] 门板引用缺失: {name}");
                _cacheReady = false;
                return;
            }

            _leftClosedPos = leftDoor.anchoredPosition;
            _rightClosedPos = rightDoor.anchoredPosition;
            _leftOpenPos = _leftClosedPos + Vector2.left * doorTravelDistance;
            _rightOpenPos = _rightClosedPos + Vector2.right * doorTravelDistance;
            if (TargetRect != null)
                _rootOriginPos = TargetRect.anchoredPosition;
            _cacheReady = true;
        }

        private Tween playDoor(Vector2 leftTarget, Vector2 rightTarget, TweenCallback onComplete)
        {
            if (!_cacheReady)
                cachePositions();

            if (!_cacheReady)
                return null;

            KillAllTweens();
            if (TargetRect != null)
                TargetRect.anchoredPosition = _rootOriginPos;

            Sequence sequence = DOTween.Sequence().SetEase(ease);
            sequence.Join(leftDoor.DOAnchorPos(leftTarget, duration).SetEase(ease));
            sequence.Join(rightDoor.DOAnchorPos(rightTarget, duration).SetEase(ease));
            Vector2 punch = _isOpen ? openFramePunch : closeFramePunch;
            if (TargetRect != null && punch.sqrMagnitude > 0.01f && framePunchDuration > 0f)
                sequence.Append(TargetRect.DOPunchAnchorPos(punch, framePunchDuration, 5, 0.45f).SetEase(Ease.OutQuad));
            sequence.OnKill(() =>
            {
                if (TargetRect != null)
                    TargetRect.anchoredPosition = _rootOriginPos;
            });
            if (onComplete != null)
                sequence.OnComplete(onComplete);
            return RegTween(sequence);
        }
    }
}
