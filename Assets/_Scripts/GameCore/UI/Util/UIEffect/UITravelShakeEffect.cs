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
        [SerializeField] private Vector2 punchStrength = new Vector2(20f, 10f);
        [SerializeField] private int vibrato = 12;
        [SerializeField] private float elasticity = 0.7f;
        [SerializeField] private Vector3 rotationPunch = new Vector3(0f, 0f, 3.5f);
        [SerializeField] private int rotationVibrato = 10;

        private Vector2 _originAnchoredPos;
        private Quaternion _originRotation;

        protected override void Awake()
        {
            base.Awake();
            if (TargetRect != null)
            {
                _originAnchoredPos = TargetRect.anchoredPosition;
                _originRotation = TargetRect.localRotation;
            }
        }

        public void Setup(float tweenDuration, Vector2 movePunch, Vector3 rotatePunch)
        {
            duration = tweenDuration;
            punchStrength = movePunch;
            rotationPunch = rotatePunch;
        }

        public Tween Play(TweenCallback onComplete = null)
        {
            if (!CanPlayEffect())
                return null;

            KillAllTweens();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(TargetRect.DOPunchAnchorPos(punchStrength, duration, vibrato, elasticity).SetEase(ease));
            sequence.Join(TargetRect.DOPunchRotation(rotationPunch, duration, rotationVibrato, elasticity).SetEase(ease));
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
            if (TargetRect == null)
                return;

            TargetRect.anchoredPosition = _originAnchoredPos;
            TargetRect.localRotation = _originRotation;
        }
    }
}
