using DG.Tweening;
using SCFrame;
using UnityEngine;

namespace GameCore.UI
{
    /// <summary>
    /// 运行时主动控制的透明度动效。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIRuntimeFadeEffect : UIEffectBase
    {
        [SerializeField] private CanvasGroup targetCanvasGroup;
        [SerializeField] [Range(0f, 1f)] private float visibleAlpha = 1f;
        [SerializeField] [Range(0f, 1f)] private float hiddenAlpha = 0f;

        private bool _isVisible = true;

        protected override void Awake()
        {
            base.Awake();
            if (targetCanvasGroup == null)
                targetCanvasGroup = GetComponent<CanvasGroup>();

            if (targetCanvasGroup == null)
                SCDebugHelper.LogError($"[{nameof(UIRuntimeFadeEffect)}] 未找到 CanvasGroup 组件: {name}");
        }

        public void Setup(CanvasGroup canvasGroup, float fadeDuration, float showAlpha = 1f, float hideAlpha = 0f)
        {
            targetCanvasGroup = canvasGroup;
            duration = fadeDuration;
            visibleAlpha = showAlpha;
            hiddenAlpha = hideAlpha;
        }

        public void SetVisibleInstant(bool visible)
        {
            _isVisible = visible;
            KillAllTweens();
            applyAlpha(visible ? visibleAlpha : hiddenAlpha);
        }

        public Tween PlayFadeIn(TweenCallback onComplete = null)
        {
            _isVisible = true;
            return playAlpha(visibleAlpha, onComplete);
        }

        public Tween PlayFadeOut(TweenCallback onComplete = null)
        {
            _isVisible = false;
            return playAlpha(hiddenAlpha, onComplete);
        }

        protected override void RegisterEvents()
        {
        }

        protected override void UnregisterEvents()
        {
        }

        protected override void ResetState()
        {
            applyAlpha(_isVisible ? visibleAlpha : hiddenAlpha);
        }

        private Tween playAlpha(float targetAlpha, TweenCallback onComplete)
        {
            if (targetCanvasGroup == null)
                return null;

            KillAllTweens();
            Tween tween = targetCanvasGroup.DOFade(targetAlpha, duration).SetEase(ease);
            if (onComplete != null)
                tween.OnComplete(onComplete);
            return RegTween(tween);
        }

        private void applyAlpha(float alpha)
        {
            if (targetCanvasGroup == null)
                return;

            targetCanvasGroup.alpha = alpha;
            targetCanvasGroup.blocksRaycasts = alpha > 0.99f;
            targetCanvasGroup.interactable = alpha > 0.99f;
        }
    }
}
