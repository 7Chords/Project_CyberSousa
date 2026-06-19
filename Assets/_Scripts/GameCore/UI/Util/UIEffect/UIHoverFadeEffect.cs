using DG.Tweening;
using SCFrame;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.UI
{
    /// <summary>
    /// 鼠标移入移出时的透明度动效。需要目标节点上有 CanvasGroup。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIHoverFadeEffect : UIEffectBase
    {
        [Header("CanvasGroup 目标（为空则使用自身）")]
        [SerializeField] private CanvasGroup targetCanvasGroup;

        [Header("移入 Alpha")]
        [SerializeField] [Range(0f, 1f)] private float hoverAlpha = 1f;

        private float _originAlpha;
        private Action<PointerEventData, object[]> _onEnter;
        private Action<PointerEventData, object[]> _onExit;

        protected override void Awake()
        {
            base.Awake();
            if (targetCanvasGroup == null)
                targetCanvasGroup = GetComponent<CanvasGroup>();

            if (targetCanvasGroup == null)
                SCDebugHelper.LogError($"[{nameof(UIHoverFadeEffect)}] 未找到 CanvasGroup 组件: {name}");
            else
                _originAlpha = targetCanvasGroup.alpha;
        }

        protected override void RegisterEvents()
        {
            _onEnter = onMouseEnter;
            _onExit = onMouseExit;
            this.AddMouseEnter(_onEnter);
            this.AddMouseExit(_onExit);
        }

        protected override void UnregisterEvents()
        {
            if (_onEnter != null)
                this.RemoveMouseEnter(_onEnter);
            if (_onExit != null)
                this.RemoveMouseExit(_onExit);
        }

        protected override void ResetState()
        {
            if (targetCanvasGroup != null)
                targetCanvasGroup.alpha = _originAlpha;
        }

        private void onMouseEnter(PointerEventData _data, object[] _args)
        {
            if (!CanPlayEffect() || targetCanvasGroup == null)
                return;

            playAlpha(hoverAlpha);
        }

        private void onMouseExit(PointerEventData _data, object[] _args)
        {
            if (targetCanvasGroup == null)
                return;

            if (!CanPlayEffect())
            {
                ResetState();
                return;
            }

            playAlpha(_originAlpha);
        }

        private void playAlpha(float _targetAlpha)
        {
            KillAllTweens();
            RegTween(targetCanvasGroup.DOFade(_targetAlpha, duration).SetEase(ease));
        }
    }
}
