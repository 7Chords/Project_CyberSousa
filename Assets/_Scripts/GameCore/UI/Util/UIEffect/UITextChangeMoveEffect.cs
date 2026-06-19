using DG.Tweening;
using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    /// <summary>
    /// 运行时主动控制的文本切换动效。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UITextChangeMoveEffect : UIEffectBase
    {
        [SerializeField] private Text targetText;
        [SerializeField] private CanvasGroup targetCanvasGroup;
        [SerializeField] private float moveDistance = 14f;

        private Vector2 _originAnchoredPos;

        protected override void Awake()
        {
            base.Awake();
            if (targetText == null)
                targetText = GetComponent<Text>();
            if (targetCanvasGroup == null)
                targetCanvasGroup = GetComponent<CanvasGroup>();

            if (targetText == null)
                SCDebugHelper.LogError($"[{nameof(UITextChangeMoveEffect)}] 未找到 Text 组件: {name}");
            if (TargetRect != null)
                _originAnchoredPos = TargetRect.anchoredPosition;
        }

        public void Setup(Text text, CanvasGroup canvasGroup, float tweenDuration, float moveOffset)
        {
            targetText = text;
            targetCanvasGroup = canvasGroup;
            duration = tweenDuration;
            moveDistance = moveOffset;
        }

        public void SetTextInstant(string content)
        {
            KillAllTweens();
            if (targetText != null)
                targetText.text = content;

            if (TargetRect != null)
                TargetRect.anchoredPosition = _originAnchoredPos;
            if (targetCanvasGroup != null)
                targetCanvasGroup.alpha = 1f;
        }

        public Tween PlayTextChange(string content, TweenCallback onComplete = null)
        {
            if (targetText == null || TargetRect == null || targetCanvasGroup == null)
                return null;

            KillAllTweens();
            float halfDuration = Mathf.Max(0.05f, duration * 0.5f);
            Vector2 upPos = _originAnchoredPos + Vector2.up * moveDistance;
            Vector2 downPos = _originAnchoredPos + Vector2.down * moveDistance;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(TargetRect.DOAnchorPos(upPos, halfDuration).SetEase(ease));
            sequence.Join(targetCanvasGroup.DOFade(0f, halfDuration).SetEase(ease));
            sequence.AppendCallback(() =>
            {
                targetText.text = content;
                TargetRect.anchoredPosition = downPos;
                targetCanvasGroup.alpha = 0f;
            });
            sequence.Append(TargetRect.DOAnchorPos(_originAnchoredPos, halfDuration).SetEase(ease));
            sequence.Join(targetCanvasGroup.DOFade(1f, halfDuration).SetEase(ease));
            if (onComplete != null)
                sequence.OnComplete(onComplete);
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
            if (TargetRect != null)
                TargetRect.anchoredPosition = _originAnchoredPos;
            if (targetCanvasGroup != null)
                targetCanvasGroup.alpha = 1f;
        }
    }
}
