using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    /// <summary>
    /// 运行时主动控制的电梯行进晃动动效。
    /// </summary>
    [DisallowMultipleComponent]
    public class UITravelShakeEffect : UIEffectBase
    {
        [SerializeField] private RectTransform shakeRoot;
        [SerializeField] private Vector2 punchStrength = new Vector2(20f, 10f);
        [SerializeField] private int vibrato = 12;
        [SerializeField] private float elasticity = 0.7f;
        [SerializeField] private Vector3 rotationPunch = new Vector3(0f, 0f, 3.5f);
        [SerializeField] private int rotationVibrato = 10;
        [SerializeField] [Range(0.1f, 1f)] private float cabinShakeMultiplier = 0.45f;
        [SerializeField] private float travelScaleMultiplier = 1.015f;
        [SerializeField] private float arrivePunchScale = 0.025f;

        private Vector2 _originAnchoredPos;
        private Quaternion _originRotation;
        private Vector3 _originScale;
        private RectTransform ShakeTarget => shakeRoot != null ? shakeRoot : TargetRect;

        protected override void Awake()
        {
            base.Awake();
            CacheOrigin();
        }

        public void SetShakeRoot(RectTransform root)
        {
            if (root == null || shakeRoot == root)
                return;

            KillAllTweens();
            ResetState();
            shakeRoot = root;
            CacheOrigin();
        }

        private void CacheOrigin()
        {
            RectTransform shakeTarget = ShakeTarget;
            if (shakeTarget != null)
            {
                _originAnchoredPos = shakeTarget.anchoredPosition;
                _originRotation = shakeTarget.localRotation;
                _originScale = shakeTarget.localScale;
            }
        }

        public void Setup(float tweenDuration, Vector2 movePunch, Vector3 rotatePunch)
        {
            duration = tweenDuration;
            punchStrength = movePunch;
            rotationPunch = rotatePunch;
            CacheOrigin();
        }

        public Tween Play(TweenCallback onComplete = null)
        {
            return Play(duration, onComplete);
        }

        public Tween Play(float travelDuration, TweenCallback onComplete = null)
        {
            if (!CanPlayEffect() || ShakeTarget == null)
                return null;

            KillAllTweens();
            float actualDuration = Mathf.Max(0.1f, travelDuration);
            Vector2 microPunchStrength = punchStrength * cabinShakeMultiplier;
            Vector3 microRotationPunch = rotationPunch * cabinShakeMultiplier;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(ShakeTarget.DOShakeAnchorPos(actualDuration, microPunchStrength, vibrato, 80f, false, true).SetEase(Ease.Linear));
            sequence.Join(ShakeTarget.DOShakeRotation(actualDuration, microRotationPunch, rotationVibrato, 80f, true).SetEase(Ease.Linear));
            sequence.Join(ShakeTarget.DOScale(_originScale * travelScaleMultiplier, actualDuration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            sequence.Append(ShakeTarget.DOPunchScale(Vector3.one * arrivePunchScale, 0.16f, 5, 0.35f).SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                ResetState();
                onComplete?.Invoke();
            });
            return RegTween(sequence);
        }

        protected override void RegisterEvents()
        {
        }

        protected override void UnregisterEvents()
        {
        }

        protected override void ResetState()
        {
            RectTransform shakeTarget = ShakeTarget;
            if (shakeTarget == null)
                return;

            shakeTarget.anchoredPosition = _originAnchoredPos;
            shakeTarget.localRotation = _originRotation;
            shakeTarget.localScale = _originScale;
        }
    }
}
