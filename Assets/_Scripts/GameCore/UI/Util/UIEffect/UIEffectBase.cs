using DG.Tweening;
using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    /// <summary>
    /// UI 动效组件基类，负责 Tween 生命周期与目标节点解析。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIEffectBase : MonoBehaviour
    {
        [Header("动效目标（为空则使用自身 RectTransform）")]
        [SerializeField] protected RectTransform target;

        [Header("动效时长")]
        [SerializeField] protected float duration = 0.15f;

        [Header("缓动类型")]
        [SerializeField] protected Ease ease = Ease.OutQuad;

        [Header("使用不受 TimeScale 影响的时间")]
        [SerializeField] protected bool useUnscaledTime = true;

        protected TweenContainer tweenContainer = new TweenContainer();
        protected RectTransform TargetRect { get; private set; }

        private bool _isEffectEnabled = true;

        protected virtual void Awake()
        {
            TargetRect = target != null ? target : transform as RectTransform;
            if (TargetRect == null)
                SCDebugHelper.LogError($"[{GetType().Name}] 需要 RectTransform 目标: {name}");
        }

        protected virtual void OnEnable()
        {
            RegisterEvents();
        }

        protected virtual void OnDisable()
        {
            UnregisterEvents();
            KillAllTweens();
            ResetState();
        }

        protected virtual void OnDestroy()
        {
            KillAllTweens();
        }

        /// <summary>
        /// 运行时开关动效（关闭时会立即复位）。
        /// </summary>
        public void SetEffectEnabled(bool _enabled)
        {
            _isEffectEnabled = _enabled;
            if (!_enabled)
            {
                KillAllTweens();
                ResetState();
            }
        }

        protected bool CanPlayEffect()
        {
            if (!_isEffectEnabled || TargetRect == null)
                return false;

            Selectable selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.interactable)
                return false;

            return true;
        }

        protected void KillAllTweens()
        {
            tweenContainer.KillAllDoTween();
        }

        protected Tween RegTween(Tween _tween)
        {
            if (_tween == null)
                return null;

            if (useUnscaledTime)
                _tween.SetUpdate(true);

            tweenContainer.RegDoTween(_tween);
            return _tween;
        }

        protected abstract void RegisterEvents();
        protected abstract void UnregisterEvents();
        protected virtual void ResetState() { }
    }
}
