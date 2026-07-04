using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    /// <summary>
    /// UI 空闲状态下的轻微周期摇晃动效，适合直接挂到 Image 或其它 RectTransform 节点。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIIdleShakeEffect : UIEffectBase
    {
        [Header("启用时自动播放")]
        [SerializeField] private bool playOnEnable = true;

        [Header("使用随机间隔")]
        [SerializeField] private bool useRandomInterval = true;

        [Header("固定间隔")]
        [SerializeField] private float fixedInterval = 1.2f;

        [Header("随机间隔范围")]
        [SerializeField] private Vector2 randomIntervalRange = new Vector2(0.8f, 1.8f);

        [Header("位移晃动强度")]
        [SerializeField] private Vector2 positionStrength = new Vector2(5f, 3f);

        [Header("旋转晃动强度")]
        [SerializeField] private Vector3 rotationStrength = new Vector3(0f, 0f, 2f);

        [Header("震动次数")]
        [SerializeField] private int vibrato = 8;

        [Header("随机角度")]
        [SerializeField] private float randomness = 70f;

        [Header("结束时衰减")]
        [SerializeField] private bool fadeOut = true;

        private Vector2 _originAnchoredPos;
        private Quaternion _originRotation;

        protected override void Awake()
        {
            base.Awake();
            CacheOrigin();
        }

        public void Setup(float shakeDuration, Vector2 moveStrength, Vector3 rotateStrength, Vector2 intervalRange, bool randomInterval)
        {
            duration = shakeDuration;
            positionStrength = moveStrength;
            rotationStrength = rotateStrength;
            randomIntervalRange = intervalRange;
            useRandomInterval = randomInterval;
            CacheOrigin();
        }

        public void PlayLoop()
        {
            if (!CanPlayEffect())
                return;

            KillAllTweens();
            ScheduleNextShake();
        }

        public void StopLoop()
        {
            KillAllTweens();
            ResetState();
        }

        public Tween PlayOnce(TweenCallback onComplete = null)
        {
            if (!CanPlayEffect())
                return null;

            return PlayShake(onComplete);
        }

        protected override void RegisterEvents()
        {
            if (playOnEnable)
                PlayLoop();
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

        private void CacheOrigin()
        {
            if (TargetRect == null)
                return;

            _originAnchoredPos = TargetRect.anchoredPosition;
            _originRotation = TargetRect.localRotation;
        }

        private void ScheduleNextShake()
        {
            if (!CanPlayEffect())
                return;

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(GetNextInterval());
            sequence.AppendCallback(() =>
            {
                PlayShake(() =>
                {
                    if (isActiveAndEnabled)
                        ScheduleNextShake();
                });
            });
            RegTween(sequence);
        }

        private Tween PlayShake(TweenCallback onComplete)
        {
            if (TargetRect == null)
                return null;

            ResetState();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(TargetRect.DOShakeAnchorPos(duration, positionStrength, vibrato, randomness, false, fadeOut).SetEase(Ease.Linear));
            sequence.Join(TargetRect.DOShakeRotation(duration, rotationStrength, vibrato, randomness, fadeOut).SetEase(Ease.Linear));
            sequence.OnComplete(() =>
            {
                ResetState();
                onComplete?.Invoke();
            });
            return RegTween(sequence);
        }

        private float GetNextInterval()
        {
            if (!useRandomInterval)
                return Mathf.Max(0f, fixedInterval);

            float min = Mathf.Min(randomIntervalRange.x, randomIntervalRange.y);
            float max = Mathf.Max(randomIntervalRange.x, randomIntervalRange.y);
            return Random.Range(Mathf.Max(0f, min), Mathf.Max(0f, max));
        }
    }
}
