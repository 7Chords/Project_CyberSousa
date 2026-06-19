using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    /// <summary>
    /// 运行时主动控制的位移缩放动效。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIRuntimeMoveScaleEffect : UIEffectBase
    {
        [SerializeField] private Vector2 enterOffset = new Vector2(0f, 36f);
        [SerializeField] private float enterScaleMultiplier = 0.84f;
        [SerializeField] private Vector2 exitOffset = new Vector2(0f, -64f);
        [SerializeField] private float exitScaleMultiplier = 1.06f;

        private Vector2 _originAnchoredPos;
        private Vector3 _originScale;

        protected override void Awake()
        {
            base.Awake();
            if (TargetRect != null)
            {
                _originAnchoredPos = TargetRect.anchoredPosition;
                _originScale = TargetRect.localScale;
            }
        }

        public void Setup(float tweenDuration, Vector2 moveInOffset, float moveInScale, Vector2 moveOutOffset, float moveOutScale)
        {
            duration = tweenDuration;
            enterOffset = moveInOffset;
            enterScaleMultiplier = moveInScale;
            exitOffset = moveOutOffset;
            exitScaleMultiplier = moveOutScale;
        }

        public void SetOriginInstant()
        {
            KillAllTweens();
            if (TargetRect == null)
                return;

            TargetRect.anchoredPosition = _originAnchoredPos;
            TargetRect.localScale = _originScale;
        }

        public void SetEnterStateInstant()
        {
            KillAllTweens();
            if (TargetRect == null)
                return;

            TargetRect.anchoredPosition = _originAnchoredPos + enterOffset;
            TargetRect.localScale = _originScale * enterScaleMultiplier;
        }

        public Tween PlayEnter(TweenCallback onComplete = null)
        {
            return playTo(_originAnchoredPos + enterOffset, _originScale * enterScaleMultiplier, onComplete);
        }

        public Tween PlayExit(TweenCallback onComplete = null)
        {
            return playTo(_originAnchoredPos + exitOffset, _originScale * exitScaleMultiplier, onComplete);
        }

        protected override void RegisterEvents()
        {
        }

        protected override void UnregisterEvents()
        {
        }

        protected override void ResetState()
        {
            SetOriginInstant();
        }

        private Tween playTo(Vector2 targetPosition, Vector3 targetScale, TweenCallback onComplete)
        {
            if (!CanPlayEffect())
                return null;

            KillAllTweens();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(TargetRect.DOAnchorPos(targetPosition, duration).SetEase(ease));
            sequence.Join(TargetRect.DOScale(targetScale, duration).SetEase(ease));
            if (onComplete != null)
                sequence.OnComplete(onComplete);
            return RegTween(sequence);
        }
    }
}
